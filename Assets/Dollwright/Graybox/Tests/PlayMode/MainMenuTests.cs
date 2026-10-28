using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Dollwright.Graybox.Tests
{
    public sealed class MainMenuTests
    {
        string folder,path;
        IDisposable scope;
        DollCraftingInventory stock;
        [SetUp] public void Begin()
        {
            folder=Path.Combine(Path.GetTempPath(),"DollwrightMenu-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);path=Path.Combine(folder,"save.json");
            stock=new DollCraftingInventory(path);scope=DollCraftingStore.UseTemporary(stock);
            DreamTravel.ResetSession();
        }
        [TearDown] public void End()
        {
            scope.Dispose();DreamTravel.ResetSession();
            foreach(var file in Directory.GetFiles(folder))File.Delete(file);
            Directory.Delete(folder);
        }
        static void AssertFresh(DollCraftingInventory inventory)
        {
            Assert.AreEqual(0,inventory.Dolls.Count);
            foreach(var id in DollMaterialCatalog.Ids)Assert.AreEqual(id=="R1"||id=="F01"?1:0,inventory.Count(id),id);
            Assert.AreEqual(DollHead.None,inventory.Draft.head);
        }
        [Test] public void NewGameClearsDollsMaterialsDraftJourneyAndOldBackup()
        {
            AssertFresh(stock);Assert.IsFalse(stock.HasSavedGame);
            var recipe=new DollRecipe(DollBody.Ceramic,DollFace.SadButton,DollHead.Magician);
            Assert.IsTrue(stock.AddMaterials(recipe.Ingredients));Assert.IsTrue(stock.TryCraft(recipe,out _));
            Assert.IsTrue(stock.BankJourney("old",new[]{"R2","H2"}));
            Assert.IsTrue(stock.StartNewGame());AssertFresh(stock);
            Assert.IsFalse(stock.HasBankedJourney("old"));Assert.IsTrue(stock.HasSavedGame);
            AssertFresh(new DollCraftingInventory(path));
            File.WriteAllText(path,"broken");
            AssertFresh(new DollCraftingInventory(path));
        }
        [Test] public void ContinueReadsDiskAndAcceptsAnEmptyShelfSave()
        {
            Assert.IsFalse(stock.ReloadSavedGame());Assert.IsTrue(stock.StartNewGame());
            var other=new DollCraftingInventory(path);Assert.IsTrue(other.TryCraft(default,out var doll));
            Assert.AreEqual(0,stock.Dolls.Count);Assert.IsTrue(stock.ReloadSavedGame());
            Assert.AreEqual(doll.instanceId,stock.Dolls[0].instanceId);Assert.AreEqual(0,stock.Count("R1"));
            Assert.IsTrue(other.StartNewGame());Assert.IsTrue(stock.ReloadSavedGame());AssertFresh(stock);
        }
        [Test] public void FailedResetPreservesExistingInventory()
        {
            Assert.IsTrue(stock.TryCraft(default,out _));string previous=stock.ExportJson();
            // A locked backup simulates a denied reset without touching the real save.
            File.WriteAllText(path+".bak","locked");
            using(var handle=new FileStream(path+".bak",FileMode.Open,FileAccess.Read,FileShare.None))
                Assert.IsFalse(stock.StartNewGame());
            Assert.AreEqual(previous,stock.ExportJson());
            Assert.AreEqual(previous,new DollCraftingInventory(path).ExportJson());
        }
        IEnumerator Load(string scene)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(scene,new LoadSceneParameters(LoadSceneMode.Single));
            Application.runInBackground=true;yield return new WaitForSeconds(.3f);
        }
        IEnumerator WaitScene(string scene)
        {
            float end=Time.realtimeSinceStartup+20;
            while((SceneManager.GetActiveScene().path!=scene||DreamTravel.IsLoading)&&Time.realtimeSinceStartup<end)yield return null;
            Assert.AreEqual(scene,SceneManager.GetActiveScene().path);Assert.IsFalse(DreamTravel.IsLoading);
            yield return new WaitForSeconds(.3f);
        }
        IEnumerator Capture(string name)
        {
            var output=Path.GetFullPath("Logs/DollwrightGraybox/MainMenu");Directory.CreateDirectory(output);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSeconds(.3f);
        }
        [UnityTest] public IEnumerator MenuNewGameCraftContinueAndEnterDream()
        {
            yield return Load(MainMenuUI.ScenePath);
            var menu=Object.FindObjectOfType<MainMenuUI>();
            Assert.IsFalse(menu.continueButton.interactable);
            yield return Capture("main-menu-no-save");
            menu.settingsButton.onClick.Invoke();Assert.IsTrue(menu.settingsPanel.activeSelf);
            Assert.IsFalse(menu.mainPanel.activeSelf);Assert.IsFalse(File.Exists(path));
            yield return Capture("settings");menu.settingsBackButton.onClick.Invoke();
            Assert.IsTrue(menu.mainPanel.activeSelf);
            // Seed an old run, then click New Game to prove it is discarded.
            stock.AddMaterials(new[]{"H1"});stock.TryCraft(new DollRecipe(DollBody.Cotton,DollFace.HappyPaint,DollHead.Magician),out _);
            menu.newGameButton.onClick.Invoke();
            yield return WaitScene(DreamTravel.WorkbenchScene);AssertFresh(stock);
            var flow=Object.FindObjectOfType<WorkbenchDreamUI>();
            Assert.AreEqual(0,flow.crafting.VisibleDolls.Count);Assert.IsFalse(flow.bedButton.interactable);
            Assert.IsNull(GameObject.Find("Background_2D_PaintedWorkshop"));
            Assert.AreEqual(CameraClearFlags.SolidColor,flow.viewCamera.clearFlags);
            yield return Capture("fresh-workbench");
            Assert.IsTrue(flow.TryOpenCraftingAtScreenPoint(flow.viewCamera.WorldToScreenPoint(flow.craftingPlatform.bounds.center)));
            yield return new WaitForSeconds(1);
            Assert.IsTrue(flow.crafting.makeButton.interactable);yield return Capture("starter-materials");
            BodyWorkshopTests.CompleteBody(flow.crafting);
            flow.crafting.makeButton.onClick.Invoke();yield return new WaitForSeconds(.75f);
            Assert.AreEqual(1,stock.Dolls.Count);var id=stock.Dolls[0].instanceId;
            Assert.AreEqual(0,stock.Count("R1"));Assert.AreEqual(0,stock.Count("F01"));
            Assert.IsTrue(flow.bedButton.interactable);
            flow.CloseSelection();yield return new WaitForSeconds(1);yield return Capture("first-doll");
            Object.FindObjectOfType<WorkbenchMenuButton>().GetComponent<Button>().onClick.Invoke();
            yield return WaitScene(MainMenuUI.ScenePath);menu=Object.FindObjectOfType<MainMenuUI>();
            Assert.IsTrue(menu.continueButton.interactable);yield return Capture("main-menu");
            menu.continueButton.onClick.Invoke();yield return WaitScene(DreamTravel.WorkbenchScene);
            Assert.AreEqual(1,stock.Dolls.Count);Assert.AreEqual(id,stock.Dolls[0].instanceId);Assert.AreEqual(0,stock.Count("R1"));
            flow=Object.FindObjectOfType<WorkbenchDreamUI>();Assert.AreEqual(1,flow.crafting.VisibleDolls.Count);
            flow.bedButton.onClick.Invoke();yield return new WaitForSeconds(1);
            Assert.IsTrue(flow.TrySelectAtScreenPoint(flow.viewCamera.WorldToScreenPoint(flow.dollCollider.bounds.center)));
            yield return WaitScene(DreamTravel.DreamScene);
            Assert.AreEqual(id,DreamTravel.SelectedDollInstanceId);
            Object.FindObjectOfType<GrayboxPlayer2D>().SetInputOverride(0);
        }
    }
}

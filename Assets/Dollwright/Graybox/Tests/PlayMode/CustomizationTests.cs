using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dollwright.Graybox.Tests
{
    [SetUpFixture]
    public sealed class CraftingSaveIsolation
    {
        public static DollCraftingInventory WithStarter()
        {
            var inventory=new DollCraftingInventory();
            inventory.AddMaterials(new[]{"R1","F01"});
            inventory.TryCraft(default,out _);
            return inventory;
        }
        IDisposable scope;
        [OneTimeSetUp] public void Begin() { scope=DollCraftingStore.UseTemporary(WithStarter()); }
        [OneTimeTearDown] public void End() { scope?.Dispose(); }
    }

    public sealed class CustomizationTests
    {
        IDisposable scope;
        DollCraftingInventory stock;
        [SetUp] public void Begin()
        {
            stock=CraftingSaveIsolation.WithStarter();
            scope=DollCraftingStore.UseTemporary(stock);
        }
        [TearDown] public void End() { scope.Dispose(); }
        IEnumerator Load(string path)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));
            Application.runInBackground=true;
            yield return new WaitForSeconds(.25f);
        }
        IEnumerator WaitScene(string path)
        {
            float until=Time.realtimeSinceStartup+20;
            while((SceneManager.GetActiveScene().path!=path||DreamTravel.IsLoading)&&Time.realtimeSinceStartup<until)yield return null;
            Assert.AreEqual(path,SceneManager.GetActiveScene().path);Assert.IsFalse(DreamTravel.IsLoading);
            yield return new WaitForSeconds(.25f);
        }
        void SupplyAll(int copies=1)
        {
            Assert.IsTrue(stock.AddMaterials(Enumerable.Range(0,copies).SelectMany(_=>DollMaterialCatalog.Ids)));
        }
        IEnumerator Capture(string name)
        {
            string folder=Path.GetFullPath("Logs/DollwrightGraybox/Customization");
            Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));
            yield return new WaitForSeconds(.35f);
        }
        [Test]
        public void AllRecipesKeepAppearanceSeparateFromAbilitiesAndNames()
        {
            var names=new HashSet<string>();int recipes=0;
            foreach(DollBody body in Enum.GetValues(typeof(DollBody)))
            foreach(DollFace face in Enum.GetValues(typeof(DollFace)))
            foreach(DollHead head in Enum.GetValues(typeof(DollHead)))
            {
                var recipe=new DollRecipe(body,face,head);Assert.IsTrue(recipe.IsValid);
                names.Add(recipe.DisplayName);recipes++;
                var baseline=new DollRecipe(DollBody.Cotton,DollFace.HappyPaint,head);
                Assert.AreEqual(baseline.DreamPath,recipe.DreamPath);Assert.AreEqual(baseline.Ability,recipe.Ability);
                Assert.AreEqual(head==DollHead.None?2:3,recipe.Ingredients.Length);
            }
            Assert.AreEqual(36,recipes);Assert.AreEqual(18,names.Count);
            Assert.AreEqual("快乐的魔术师布偶",new DollRecipe(DollBody.Cotton,DollFace.HappyButton,DollHead.Magician).DisplayName);
            Assert.AreEqual("悲伤的泡泡瓷偶",new DollRecipe(DollBody.Ceramic,DollFace.SadPaint,DollHead.Bubble).DisplayName);
        }
        [Test]
        public void PreviewIsFreeCraftConsumesExactIngredientsAndInstancesAreUnique()
        {
            SupplyAll(2);var recipe=new DollRecipe(DollBody.Wood,DollFace.SadButton,DollHead.Magician);
            int wood=stock.Count("R2"),face=stock.Count("F04"),head=stock.Count("H1"),cotton=stock.Count("R1");
            Assert.IsTrue(stock.SetDraft(recipe));
            Assert.AreEqual(wood,stock.Count("R2"));Assert.AreEqual(face,stock.Count("F04"));Assert.AreEqual(head,stock.Count("H1"));
            Assert.IsTrue(stock.TryCraft(recipe,out var first));Assert.IsTrue(stock.TryCraft(recipe,out var second));
            Assert.AreEqual(first.DisplayName,second.DisplayName);Assert.AreNotEqual(first.instanceId,second.instanceId);
            Assert.AreEqual(wood-2,stock.Count("R2"));Assert.AreEqual(face-2,stock.Count("F04"));Assert.AreEqual(head-2,stock.Count("H1"));
            Assert.AreEqual(cotton,stock.Count("R1"));
            string before=stock.ExportJson();Assert.IsFalse(stock.TryCraft(recipe,out _));Assert.AreEqual(before,stock.ExportJson());
            Assert.IsFalse(stock.SetDraft(new DollRecipe((DollBody)99,DollFace.HappyPaint,DollHead.None)));
        }
        [Test]
        public void SaveReloadRestoresDraftDollsAndBankedRewardsExactlyOnce()
        {
            string folder=Path.Combine(Path.GetTempPath(),"DollwrightCraft-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);string path=Path.Combine(folder,"save.json");
            try
            {
                var saved=new DollCraftingInventory(path);
                var recipe=new DollRecipe(DollBody.Ceramic,DollFace.SadButton,DollHead.Bubble);
                Assert.IsTrue(saved.AddMaterials(recipe.Ingredients));
                Assert.IsTrue(saved.SetDraft(recipe));Assert.IsTrue(saved.TryCraft(recipe,out var doll));
                Assert.IsTrue(saved.BankJourney("one-journey",new[]{"R2","R2","F02","H2"}));
                Assert.IsFalse(saved.BankJourney("one-journey",new[]{"R2","H2"}));
                var loaded=new DollCraftingInventory(path);
                Assert.AreEqual(recipe.DisplayName,loaded.Draft.DisplayName);
                Assert.AreEqual(doll.instanceId,loaded.Dolls.Last().instanceId);
                Assert.AreEqual(2,loaded.Count("R2"));Assert.AreEqual(1,loaded.Count("H2"));
                Assert.IsTrue(loaded.HasBankedJourney("one-journey"));
                Assert.IsFalse(loaded.BankJourney("one-journey",new[]{"R1"}));
                File.WriteAllText(path,"broken");
                var recovered=new DollCraftingInventory(path);
                Assert.IsNotNull(recovered.FindDoll(doll.instanceId),"Atomic backup must contain the previous completed craft.");
            }
            finally
            {
                foreach(string file in new[]{path,path+".bak",path+".tmp"})if(File.Exists(file))File.Delete(file);
                Directory.Delete(folder);
            }
        }
        [Test]
        public void FailedSaveDoesNotConsumeOrCreateAndInvalidRewardsAreRejected()
        {
            string blocker=Path.Combine(Path.GetTempPath(),"DollwrightBlock-"+Guid.NewGuid().ToString("N"));
            File.WriteAllText(blocker,"file, not directory");
            try
            {
                var blocked=new DollCraftingInventory(Path.Combine(blocker,"save.json"));string before=blocked.ExportJson();
                Assert.IsFalse(blocked.TryCraft(default,out _));Assert.AreEqual(before,blocked.ExportJson());
                Assert.IsFalse(stock.AddMaterials(new[]{"R1","not-a-material"}));
                Assert.IsFalse(stock.BankJourney("bad",new[]{"missing"}));Assert.AreEqual(1,stock.Count("R1"));
            }
            finally { File.Delete(blocker); }
        }
        [UnityTest]
        public IEnumerator WorkbenchButtonsCraftToShelfAndCreatedAppearanceEntersMagicianDream()
        {
            SupplyAll(3);
            yield return Load(DreamTravel.WorkbenchScene);
            var flow=Object.FindObjectOfType<WorkbenchDreamUI>();var ui=flow.crafting;
            Assert.IsNotNull(ui);Assert.IsNull(flow.magicianCollider,"No free debug magician remains.");
            Assert.AreEqual(1,ui.VisibleDolls.Count);Assert.IsFalse(ui.controls.activeSelf);
            var target=flow.viewCamera.WorldToScreenPoint(flow.craftingPlatform.bounds.center);
            Assert.IsTrue(flow.TryOpenCraftingAtScreenPoint(target));
            Assert.IsFalse(flow.TryOpenCraftingAtScreenPoint(target),"Cannot open twice during transition.");
            Assert.IsFalse(ui.Craft());yield return new WaitForSeconds(1);
            Assert.IsTrue(flow.CraftReady);
            int wood=stock.Count("R2"),face=stock.Count("F04"),heads=stock.Count("H1");
            ui.bodyButtons[1].onClick.Invoke();BodyWorkshopTests.CompleteBody(ui);ui.faceButtons[3].onClick.Invoke();ui.headButtons[1].onClick.Invoke();
            Assert.AreEqual("悲伤的魔术师木偶",ui.dollName.text);
            Assert.IsNotNull(ui.Preview.transform.Find("H1_Magician"));Assert.IsTrue(ui.makeButton.interactable);
            yield return Capture("crafting");
            Assert.AreEqual(wood,stock.Count("R2"));Assert.AreEqual(face,stock.Count("F04"));Assert.AreEqual(heads,stock.Count("H1"));
            ui.makeButton.onClick.Invoke();ui.makeButton.onClick.Invoke();
            Assert.AreEqual(2,stock.Dolls.Count,"A double-click cannot craft a second doll.");
            Assert.AreEqual(wood-1,stock.Count("R2"));Assert.AreEqual(face-1,stock.Count("F04"));Assert.AreEqual(heads-1,stock.Count("H1"));
            var made=stock.Dolls.Last();yield return new WaitForSeconds(.75f);
            flow.CloseSelection();yield return new WaitForSeconds(1);
            flow.bedButton.onClick.Invoke();yield return new WaitForSeconds(1);
            var handle=ui.VisibleDolls.Single(x=>x.instanceId==made.instanceId);
            yield return Capture("shelf");
            var point=flow.viewCamera.WorldToScreenPoint(handle.GetComponent<Collider>().bounds.center);
            Assert.IsTrue(flow.TrySelectAtScreenPoint(point));
            yield return WaitScene(made.recipe.DreamPath);
            Assert.AreEqual(made.instanceId,DreamTravel.SelectedDollInstanceId);
            var player=Object.FindObjectOfType<GrayboxPlayer2D>();player.SetInputOverride(0);
            Assert.AreEqual(DollBody.Wood,player.GetComponent<DreamDollAppearance>().AppliedRecipe.body);
            Assert.AreEqual(DollFace.SadButton,player.GetComponent<DreamDollAppearance>().AppliedRecipe.face);
            Assert.AreEqual(4.2f,player.moveSpeed);Assert.AreEqual(11.5f,player.jumpSpeed);
            Assert.IsTrue(player.GetComponent<DreamPhaseAbility>().Activate());
        }
        [UnityTest]
        public IEnumerator BubbleCanBeCraftedAndPagedButUnbuiltDreamDoesNotLoad()
        {
            SupplyAll(4);
            for(int i=0;i<3;i++)Assert.IsTrue(stock.TryCraft(new DollRecipe(DollBody.Ceramic,DollFace.HappyButton,DollHead.Bubble),out _));
            yield return Load(DreamTravel.WorkbenchScene);
            var flow=Object.FindObjectOfType<WorkbenchDreamUI>();var ui=flow.crafting;
            flow.OpenSelection();yield return new WaitForSeconds(1);
            Assert.AreEqual(3,ui.VisibleDolls.Count);Assert.IsTrue(ui.nextPage.gameObject.activeSelf);
            ui.nextPage.onClick.Invoke();yield return null;
            Assert.AreEqual(1,ui.Page);Assert.AreEqual(1,ui.VisibleDolls.Count);Assert.IsFalse(ui.nextPage.interactable);
            var handle=ui.VisibleDolls[0];
            Assert.IsFalse(flow.TrySelectAtScreenPoint(flow.viewCamera.WorldToScreenPoint(handle.GetComponent<Collider>().bounds.center)));
            Assert.AreEqual(DreamTravel.WorkbenchScene,SceneManager.GetActiveScene().path);
            Assert.AreEqual("梦境待制作",ui.shelfName.text);Assert.IsFalse(DreamTravel.IsLoading);
            ui.previousPage.onClick.Invoke();Assert.AreEqual(0,ui.Page);Assert.IsFalse(ui.previousPage.interactable);
        }
        [UnityTest]
        public IEnumerator DreamSegmentRewardsKeepTypesAndBankOnlyWhenWaking()
        {
            const string root="Assets/Dollwright/Graybox/Scenes/";
            SupplyAll();
            var recipe=new DollRecipe(DollBody.Cotton,DollFace.HappyPaint,DollHead.Magician);
            Assert.IsTrue(stock.TryCraft(recipe,out var doll));
            int wood=stock.Count("R2"),buttons=stock.Count("F02"),bubbles=stock.Count("H2");
            var definition=doll.CreateDefinition();Assert.IsTrue(DreamTravel.Enter(definition));
            yield return WaitScene(recipe.DreamPath);Object.Destroy(definition);
            var run=Object.FindObjectOfType<GrayboxRun>();run.player.SetInputOverride(0);
            run.Collect("test-base",GrayboxPickup.Kind.Base,"R2");run.Collect("test-face",GrayboxPickup.Kind.Face,"F02");
            Assert.IsTrue(DreamTravel.MoveWithinDream(root+"Dream02_MagicStage.unity",run));
            yield return WaitScene(root+"Dream02_MagicStage.unity");
            run=Object.FindObjectOfType<GrayboxRun>();run.player.SetInputOverride(0);
            Assert.AreEqual("R2",run.CopyCraftingInventory()["test-base"]);Assert.AreEqual("F02",run.CopyCraftingInventory()["test-face"]);
            Assert.IsFalse(run.Collect("test-base",GrayboxPickup.Kind.Base,"R2"));
            Assert.AreEqual(wood,stock.Count("R2"));Assert.AreEqual(buttons,stock.Count("F02"));
            Assert.IsTrue(run.Collect("H2",GrayboxPickup.Kind.Head,"H2"));
            Assert.IsTrue(run.TryComplete());yield return WaitScene(DreamTravel.WorkbenchScene);
            Assert.AreEqual(wood+1,stock.Count("R2"));Assert.AreEqual(buttons+1,stock.Count("F02"));
            Assert.AreEqual(bubbles+1,stock.Count("H2"));
            yield return Load(DreamTravel.WorkbenchScene);
            Assert.AreEqual(wood+1,stock.Count("R2"),"Loading the workbench cannot bank the same dream twice.");
        }
    }
}


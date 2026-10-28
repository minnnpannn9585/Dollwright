using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Dollwright.Graybox.Tests
{
    public sealed class BodyWorkshopTests
    {
        public static void Solve(BodyWorkshopSession session)
        {
            for(int step=0;step<20000&&!session.Finished;step++)
            {
                if(session.Body==DollBody.Cotton)
                {
                    var d=session.Seam[session.Segment+1]-session.Needle;
                    float angle=Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg;
                    session.Turn(Mathf.DeltaAngle(session.Heading,angle));session.Tick(.05f,true);
                }
                else
                {
                    int row=0;float error=0,side=1;
                    for(int i=0;i<BodyWorkshopSession.Rows;i++)foreach(float sign in new[]{-1f,1f})
                    {
                        float current=sign<0?session.Left[i]:session.Right[i];
                        float e=current-BodyWorkshopSession.Target(i);
                        if(Mathf.Abs(e)>Mathf.Abs(error)){error=e;row=i;side=sign;}
                    }
                    float width=side<0?session.Left[row]:session.Right[row];
                    var p=new Vector2(side*width,BodyWorkshopSession.Height(row));
                    if(session.Body==DollBody.Wood)
                        session.Stroke(p+new Vector2(0,-.015f),p+new Vector2(0,.015f),false);
                    else session.Stroke(p,p+new Vector2(-Mathf.Sign(error)*side*.022f,0),true);
                }
            }
            Assert.IsTrue(session.Finished,"The workpiece must be reachable using regular steering/brush input: "+session.Body+" error="+session.MaxError);
        }
        public static void CompleteBody(WorkbenchCraftingUI ui)
        {Assert.IsTrue(ui.ConfirmBody());Solve(ui.Workshop.Session);Assert.IsTrue(ui.Workshop.Finish());Assert.IsTrue(ui.BodyReady);}
        [Test] public void SewingRequiresPowerAndCorrectSteering()
        {
            var s=new BodyWorkshopSession(DollBody.Cotton);var start=s.Needle;
            s.Turn(90);s.Tick(.05f,false);Assert.AreEqual(start,s.Needle);Assert.AreEqual(0,s.Progress);
            for(int i=0;i<50;i++)s.Tick(.05f,true);
            Assert.IsFalse(s.Finished);Assert.IsTrue(s.OffLine);
            s.Reset();Solve(s);Assert.Greater(s.Stitches.Count,30);
        }
        [Test] public void WoodRequiresBothEdgesAndCannotGrowBackButCanReset()
        {
            var s=new BodyWorkshopSession(DollBody.Wood);
            Assert.IsFalse(s.Stroke(Vector2.zero,new Vector2(.02f,0),false));
            for(int n=0;n<80;n++)s.Stroke(new Vector2(s.Right[6],-.015f),new Vector2(s.Right[6],.015f),false);
            Assert.Less(s.Right[6],BodyWorkshopSession.Target(6)-.085f);Assert.AreEqual(1.12f,s.Left[6]);Assert.IsFalse(s.Finished);
            float carved=s.Right[6];s.Stroke(new Vector2(carved,0),new Vector2(carved+.04f,0),false);Assert.LessOrEqual(s.Right[6],carved);
            s.Reset();Solve(s);
        }
        [Test] public void ClayNeedsSpinningAndAllowsInwardAndOutwardCorrection()
        {
            var s=new BodyWorkshopSession(DollBody.Ceramic);var p=new Vector2(.64f,0);
            Assert.IsFalse(s.Stroke(p,p+new Vector2(.04f,0),false));Assert.AreEqual(.64f,s.Right[6]);
            Assert.IsTrue(s.Stroke(p,p+new Vector2(.04f,0),true));Assert.Greater(s.Right[6],.64f);
            p=new Vector2(s.Right[6],0);s.Stroke(p,p-new Vector2(.08f,0),true);Assert.Less(s.Right[6],.64f);
            s.Reset();Solve(s);for(int i=0;i<BodyWorkshopSession.Rows;i++)Assert.AreEqual(s.Left[i],s.Right[i]);
        }
        IEnumerator Capture(string name)
        {
            string folder=Path.GetFullPath("Logs/DollwrightGraybox/BodyWorkshop");Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return new WaitForSeconds(.25f);
        }
        [UnityTest] public IEnumerator BodyConfirmationPrecedesCosmeticsAndOnlyFinalCraftSpendsMaterials()
        {
            var inventory=new DollCraftingInventory();inventory.AddMaterials(DollMaterialCatalog.Ids);
            using(DollCraftingStore.UseTemporary(inventory))
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(DreamTravel.WorkbenchScene,new LoadSceneParameters(LoadSceneMode.Single));
                Application.runInBackground=true;yield return null;
                var flow=Object.FindObjectOfType<WorkbenchDreamUI>();var ui=flow.crafting;
                flow.OpenCrafting();yield return new WaitForSeconds(1);
                Assert.IsFalse(ui.BodyReady);Assert.IsFalse(ui.faceButtons[1].interactable);Assert.IsFalse(ui.Craft());
                ui.SelectFace(3);Assert.AreEqual(DollFace.HappyPaint,inventory.Draft.face);
                foreach(DollBody body in Enum.GetValues(typeof(DollBody)))
                {
                    ui.SelectBody((int)body);string before=inventory.ExportJson();
                    Assert.IsTrue(ui.ConfirmBody());Assert.IsTrue(ui.IsProcessingBody);Assert.IsFalse(ui.controls.activeSelf);
                    Assert.IsFalse(ui.Workshop.Finish());Assert.IsFalse(ui.Craft());
                    ui.Workshop.Cancel();Assert.IsFalse(ui.BodyReady);Assert.AreEqual(before,inventory.ExportJson());
                    Assert.IsTrue(ui.ConfirmBody());yield return null;
                    if(body!=DollBody.Cotton)
                    {
                        float beforeWidth=ui.Workshop.Session.Right[6];var surface=ui.Workshop.Surface;
                        var e=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
                        e.position=surface.ScreenPoint(new Vector2(beforeWidth,-.02f));surface.OnPointerDown(e);
                        e.position=surface.ScreenPoint(new Vector2(beforeWidth,.02f));surface.OnDrag(e);surface.OnPointerUp(e);
                        if(body==DollBody.Wood)Assert.Less(ui.Workshop.Session.Right[6],beforeWidth,"Actual UI drag must reach the workpiece model.");
                        else Assert.AreEqual(beforeWidth,ui.Workshop.Session.Right[6],"Pointer motion without spinning cannot shape clay.");
                        ui.Workshop.ResetWorkpiece();
                    }
                    yield return Capture(body+"-start");
                    Solve(ui.Workshop.Session);ui.Workshop.RefreshView();yield return null;yield return Capture(body+"-finished");
                    Assert.IsTrue(ui.Workshop.Finish());Assert.IsFalse(ui.Workshop.Finish());
                    Assert.IsTrue(ui.BodyReady);Assert.AreEqual(before,inventory.ExportJson());
                    ui.SelectFace(3);ui.SelectHead(1);
                    Assert.AreEqual(DollFace.SadButton,inventory.Draft.face);Assert.AreEqual(DollHead.Magician,inventory.Draft.head);
                    Assert.IsTrue(ui.BodyReady);ui.SelectFace(0);ui.SelectHead(0);
                }
                yield return Capture("cosmetics-after-body");
                int count=inventory.Count("R3");Assert.IsTrue(ui.Craft());Assert.IsFalse(ui.Craft());
                Assert.AreEqual(1,inventory.Dolls.Count);Assert.AreEqual(count-1,inventory.Count("R3"));
                Assert.IsFalse(ui.BodyReady);Assert.IsFalse(ui.faceButtons[0].interactable);
            }
        }
        [UnityTest] public IEnumerator UnownedBodyCannotStartAndClosingWorkshopDoesNotSpendAnything()
        {
            var inventory=new DollCraftingInventory();
            using(DollCraftingStore.UseTemporary(inventory))
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(DreamTravel.WorkbenchScene,new LoadSceneParameters(LoadSceneMode.Single));
                Application.runInBackground=true;yield return null;
                var flow=Object.FindObjectOfType<WorkbenchDreamUI>();var ui=flow.crafting;
                flow.OpenCrafting();yield return new WaitForSeconds(1);ui.SelectBody(1);Assert.IsFalse(ui.ConfirmBody());
                ui.SelectBody(0);Assert.IsTrue(ui.ConfirmBody());flow.CloseCrafting();
                Assert.IsFalse(ui.IsProcessingBody);Assert.IsFalse(ui.controls.activeSelf);
                Assert.AreEqual(1,inventory.Count("R1"));Assert.AreEqual(1,inventory.Count("F01"));Assert.AreEqual(0,inventory.Dolls.Count);
            }
        }
    }
}

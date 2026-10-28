using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
namespace Dollwright.Graybox.Tests
{
    public sealed class SawAndRewindTests
    {
        const string Root="Assets/Dollwright/Graybox/Scenes/";
        GrayboxPlayer2D player; GrayboxRun run;
        Vector2 regionOrigin;
        Dream02ContinuousLayout layout;
        bool doubleRegion;
        Vector2 Position(float x,float y) => regionOrigin+new Vector2(x,y);
        DreamPressurePlate Plate => GameObject.Find("PressurePlate_"+(doubleRegion?"P2":"P1")).GetComponent<DreamPressurePlate>();
        DreamSawGate Gate => GameObject.Find("SawGate_"+(doubleRegion?"P2":"P1")).GetComponent<DreamSawGate>();
        IEnumerator Load(string section)
        {
            DreamTravel.ClearJourney();
            bool saw=section=="Saw",doubles=section=="Double";
            string scene=saw||doubles?"Dream02_MagicStage":section;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Root+scene+".unity",new LoadSceneParameters(LoadSceneMode.Single));
            Application.runInBackground=true;RefreshPlayer();yield return new WaitForSeconds(.25f);
            layout=Object.FindObjectOfType<Dream02ContinuousLayout>();
            doubleRegion=doubles;regionOrigin=doubles?layout.DoubleOrigin:saw?layout.SawOrigin:Vector2.zero;
            if(saw||doubles)
            {
                var spawn=Position(doubles?-1:0,.76f);
                player.Teleport(spawn);run.SetCheckpoint(spawn,"test section checkpoint");
                Camera.main.GetComponent<GrayboxCamera2D>().SnapToTarget();
                yield return new WaitForSeconds(.2f);
            }
        }
        void RefreshPlayer(){player=Object.FindObjectOfType<GrayboxPlayer2D>();run=player.run;player.SetInputOverride(0);}
        IEnumerator Walk(float x)
        {
            x+=regionOrigin.x;
            float dir=Mathf.Sign(x-player.Body.position.x),until=Time.time+Mathf.Abs(x-player.Body.position.x)/4.2f+2;
            player.SetInputOverride(dir);
            while(dir*(x-player.Body.position.x)>.04f&&Time.time<until)yield return new WaitForFixedUpdate();
            player.SetInputOverride(0);Assert.That(player.Body.position.x,Is.EqualTo(x).Within(.16f),"Walk blocked at "+x);
        }
        IEnumerator Jump(float launch,float landing,float top)
        {
            yield return Walk(launch);yield return new WaitForSeconds(.1f);Assert.IsTrue(player.IsGrounded,"Launch "+launch);
            float dir=Mathf.Sign(landing-launch);player.SetInputOverride(dir,true);yield return new WaitForSeconds(.2f);
            float until=Time.time+2;
            while(Time.time<until)
            {
                if(dir*(landing+regionOrigin.x-player.Body.position.x)<.03f)player.SetInputOverride(0);
                if(player.IsGrounded)break;
                yield return new WaitForFixedUpdate();
            }
            player.SetInputOverride(0);Assert.IsTrue(player.IsGrounded);
            Assert.That(player.Body.position.y,Is.EqualTo(regionOrigin.y+top+.7f).Within(.14f),"Landing height "+landing);
            yield return Walk(landing);
        }
        IEnumerator Phase()
        {
            var phase=player.GetComponent<DreamPhaseAbility>();float until=Time.time+3;
            while(!phase.Activate()&&Time.time<until)yield return null;Assert.IsTrue(phase.IsPhasing);
        }
        IEnumerator Push(string name,float x)
        {
            x+=regionOrigin.x;
            var body=GameObject.Find(name).GetComponent<Rigidbody2D>();float dir=Mathf.Sign(x-body.position.x),until=Time.time+12;
            player.SetInputOverride(dir);
            while(dir*(x-body.position.x)>.02f&&Time.time<until)yield return new WaitForFixedUpdate();
            player.SetInputOverride(0);yield return new WaitForFixedUpdate();
            Assert.That(body.position.x,Is.EqualTo(x).Within(.10f),"Push "+name);
        }
        IEnumerator FinishRewind(GrayboxRewind rewind)
        {
            float until=Time.realtimeSinceStartup+4;
            while(rewind.IsRewinding&&Time.realtimeSinceStartup<until)yield return null;
            Assert.IsFalse(rewind.IsRewinding);Assert.AreEqual(0,rewind.FogCoverage);
        }
        [UnityTest]
        public IEnumerator EveryPlayableSceneHasRewindAndRestoresAllBoxes()
        {
            foreach(string scene in new[]{"MovementTest","Dream01_Graybox","Dream02_MagicBox","Dream02_MagicStage"})
            {
                yield return Load(scene);var rewind=run.Rewind;Assert.IsNotNull(rewind,scene);
                var boxes=Object.FindObjectsOfType<GrayboxPushCrate>();var positions=new Vector2[boxes.Length];
                for(int i=0;i<boxes.Length;i++){positions[i]=boxes[i].CheckpointPosition;boxes[i].RestoreCheckpoint(positions[i]+Vector2.right*.2f);}
                player.Teleport(rewind.SavedPlayerPosition+Vector2.up*.5f);
                Assert.IsTrue(rewind.TryRewind(),scene);Assert.IsFalse(rewind.TryRewind(),"No double activation.");
                yield return FinishRewind(rewind);player.SetInputOverride(0);
                Assert.Less(Vector2.Distance(player.Body.position,rewind.SavedPlayerPosition),.1f,scene);
                for(int i=0;i<boxes.Length;i++)Assert.Less(Vector2.Distance(boxes[i].GetComponent<Rigidbody2D>().position,positions[i]),.01f,scene);
            }
        }
        [UnityTest]
        public IEnumerator LatestCheckpointRestoresBoxesPlateSawAndPhaseUnderOpaqueFog()
        {
            yield return Load("Double");
            var a=GameObject.Find("Double_CrateA").GetComponent<GrayboxPushCrate>();var b=GameObject.Find("Double_CrateB").GetComponent<GrayboxPushCrate>();
            a.RestoreCheckpoint(Position(3,.6f));b.RestoreCheckpoint(Position(14,.6f));
            var earlier=GameObject.Find("SawLesson_CrateA").GetComponent<GrayboxPushCrate>();
            earlier.RestoreCheckpoint(layout.SawOrigin+new Vector2(6,.6f));
            run.SetCheckpoint(Position(10,.76f),"test checkpoint");
            earlier.RestoreCheckpoint(layout.SawOrigin+new Vector2(3,.6f));
            a.RestoreCheckpoint(Position(6,.6f));b.RestoreCheckpoint(Position(18,.6f));
            run.Collect("test_base",GrayboxPickup.Kind.Base);yield return Phase();
            player.Teleport(Position(19,.73f));var rewind=run.Rewind;int before=rewind.RestoreCount;
            Assert.IsTrue(rewind.TryRewind());Assert.IsFalse(player.GetComponent<DreamPhaseAbility>().Activate());
            float until=Time.realtimeSinceStartup+3;
            while(rewind.RestoreCount==before&&Time.realtimeSinceStartup<until)yield return null;
            Assert.AreEqual(1,rewind.FogCoverage,"Restore occurs only while completely covered.");
            Assert.That(a.GetComponent<Rigidbody2D>().position.x,Is.EqualTo(regionOrigin.x+3).Within(.01f));
            Assert.That(b.GetComponent<Rigidbody2D>().position.x,Is.EqualTo(regionOrigin.x+14).Within(.01f));
            Assert.IsTrue(Plate.IsPressed);
            Assert.IsTrue(Gate.IsOpen);
            Assert.IsTrue(GameObject.Find("PressurePlate_P1").GetComponent<DreamPressurePlate>().IsPressed);
            Assert.IsTrue(GameObject.Find("SawGate_P1").GetComponent<DreamSawGate>().IsOpen);
            Assert.IsFalse(player.GetComponent<DreamPhaseAbility>().IsPhasing);
            ScreenCapture.CaptureScreenshot("Logs/DollwrightGraybox/Rewind_FullFog.png");
            yield return FinishRewind(rewind);Assert.AreEqual(1,run.Bases,"Collected materials survive retries.");
            Assert.That(player.Body.position.x,Is.EqualTo(regionOrigin.x+10).Within(.01f));
        }
        [UnityTest]
        public IEnumerator FallingCircusCrateIsRecoveredAndReturnsToItsSavedRail()
        {
            yield return Load("Dream01_Graybox");
            var box=GameObject.Find("PushCrate_Circus").GetComponent<GrayboxPushCrate>();var saved=box.CheckpointPosition;
            box.RestoreCheckpoint(Position(52,.6f));yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            Assert.IsTrue(box.IsFalling);Assert.IsTrue(run.Rewind.TryRewind());yield return FinishRewind(run.Rewind);
            Assert.IsFalse(box.IsFalling);Assert.AreEqual(RigidbodyType2D.Kinematic,box.GetComponent<Rigidbody2D>().bodyType);
            Assert.Less(Vector2.Distance(saved,box.GetComponent<Rigidbody2D>().position),.01f);
        }
        [UnityTest]
        public IEnumerator SawCannotBePhasedThroughAndContactRewindsSafely()
        {
            yield return Load("Saw");
            player.Teleport(Position(10,.73f));yield return Phase();player.SetInputOverride(1);
            float until=Time.time+3;
            while(!run.Rewind.IsRewinding&&Time.time<until)yield return null;
            Assert.IsTrue(run.Rewind.IsRewinding,"Solid saw contact invokes rewind even while phasing.");
            player.SetInputOverride(0);yield return FinishRewind(run.Rewind);Assert.Less(player.Body.position.x,regionOrigin.x+1);
            Assert.IsNotNull(GameObject.Find("SawLesson_CrateA"));
        }
        [UnityTest]
        public IEnumerator TwoCratesCannotOverlapWhenPushedTogether()
        {
            yield return Load("Double");
            var a=GameObject.Find("Double_CrateA").GetComponent<GrayboxPushCrate>();var b=GameObject.Find("Double_CrateB").GetComponent<GrayboxPushCrate>();
            a.RestoreCheckpoint(Position(7,.6f));b.RestoreCheckpoint(Position(9,.6f));player.Teleport(Position(10.1f,.73f));
            yield return new WaitForSeconds(.15f);player.SetInputOverride(-1);yield return new WaitForSeconds(1.2f);player.SetInputOverride(0);
            Assert.GreaterOrEqual(b.GetComponent<Rigidbody2D>().position.x-a.GetComponent<Rigidbody2D>().position.x,1.19f);
        }
        [UnityTest]
        public IEnumerator PlayerCanDemonstratePlateAndRemovingWeightClosesSaw()
        {
            yield return Load("Saw");
            var plate=Plate;var gate=Gate;
            player.Teleport(Position(6,.73f));yield return new WaitForSeconds(.15f);
            Assert.IsTrue(plate.IsPressed);Assert.IsTrue(gate.IsOpen);
            player.Teleport(Position(7.5f,.73f));yield return new WaitForSeconds(.15f);
            Assert.IsFalse(plate.IsPressed);Assert.IsFalse(gate.IsOpen);
            Assert.IsFalse(run.Rewind.IsRewinding);
        }
        [UnityTest]
        public IEnumerator FogClosesBeforeTeleportThenOpensAfterCheckpointRestore()
        {
            yield return Load("Saw");player.Teleport(Position(16,.73f));
            var rewind=run.Rewind;Assert.IsTrue(rewind.TryRewind());
            float until=Time.realtimeSinceStartup+3;
            while(rewind.FogCoverage<.45f&&Time.realtimeSinceStartup<until)yield return null;
            Assert.That(player.Body.position.x,Is.EqualTo(regionOrigin.x+16).Within(.01f));
            ScreenCapture.CaptureScreenshot("Logs/DollwrightGraybox/Rewind_Closing.png");
            while(rewind.RestoreCount==0&&Time.realtimeSinceStartup<until)yield return null;
            Assert.AreEqual(1,rewind.FogCoverage);
            while(rewind.FogCoverage>.55f&&Time.realtimeSinceStartup<until)yield return null;
            Assert.That(player.Body.position.x,Is.EqualTo(regionOrigin.x).Within(.01f));
            ScreenCapture.CaptureScreenshot("Logs/DollwrightGraybox/Rewind_Opening.png");
            yield return FinishRewind(rewind);
        }
        [UnityTest]
        public IEnumerator PlateBoxAndRoofCannotBypassSecondCrateEvenWithCoyoteJump()
        {
            yield return Load("Double");
            var plate=Plate;
            var start=GameObject.Find("Double_StartPlatform").GetComponent<BoxCollider2D>().bounds;
            var roof=GameObject.Find("Double_LowRoof").GetComponent<BoxCollider2D>().bounds;
            var upper=GameObject.Find("Double_UpperRoute").GetComponent<BoxCollider2D>().bounds;
            float gravity=Mathf.Abs(Physics2D.gravity.y)*player.Body.gravityScale;
            float grace=player.coyoteTime,fall=.5f*gravity*grace*grace;
            float rise=start.max.y-regionOrigin.y-1.2f+fall-.07f;
            float flight=(player.jumpSpeed+Mathf.Sqrt(player.jumpSpeed*player.jumpSpeed-2*gravity*rise))/gravity;
            float furthest=plate.transform.position.x+plate.halfWidth+.15f+.6f+.325f+player.moveSpeed*(grace+flight);
            Assert.Less(furthest,start.min.x-.325f-.15f,"A must leave its plate before it can serve as the exit step.");
            rise=start.max.y-roof.max.y+fall-.07f;
            flight=(player.jumpSpeed+Mathf.Sqrt(player.jumpSpeed*player.jumpSpeed-2*gravity*rise))/gravity;
            float returnRange=player.moveSpeed*(grace+flight)+.65f;
            Assert.Greater(roof.min.x-start.max.x,returnRange+.15f,"Cannot jump backward from low roof to starting platform.");
            Assert.Greater(upper.max.y-roof.max.y,player.jumpSpeed*player.jumpSpeed/(2*gravity)+.14f);
        }
        [UnityTest]
        public IEnumerator FullSawLessonAndDoubleCrateRouteCarryMaterialsAndWake()
        {
            yield return Load("Saw");run.Collect("R1",GrayboxPickup.Kind.Base);run.Collect("F1",GrayboxPickup.Kind.Face);
            yield return Walk(1.8f);yield return Push("SawLesson_CrateA",6);yield return new WaitForSeconds(.15f);
            Assert.IsTrue(Plate.IsPressed);Assert.IsTrue(Gate.IsOpen);
            ScreenCapture.CaptureScreenshot("Logs/DollwrightGraybox/SawLesson_Solved.png");
            yield return Jump(player.Body.position.x-regionOrigin.x,7.7f,0);yield return Walk(8.2f);yield return Phase();yield return Walk(10.1f);
            yield return Walk(15);Assert.AreEqual(2,run.Bases);
            var sameRun=run;var samePlayer=player;int sameScene=SceneManager.GetActiveScene().handle;
            var fog=Object.FindObjectOfType<GrayboxFog2D>();float revealed=fog.FurthestRevealed;
            yield return Walk(19.7f);
            regionOrigin=layout.DoubleOrigin;doubleRegion=true;
            yield return Walk(-.5f);yield return new WaitForSeconds(.2f);
            Assert.AreSame(sameRun,Object.FindObjectOfType<GrayboxRun>());
            Assert.AreSame(samePlayer,Object.FindObjectOfType<GrayboxPlayer2D>());
            Assert.AreEqual(sameScene,SceneManager.GetActiveScene().handle,"Saw to double-crate is a continuous walk.");
            Assert.GreaterOrEqual(fog.FurthestRevealed,revealed);
            Assert.That(run.Rewind.SavedPlayerPosition.x,Is.EqualTo(regionOrigin.x-1).Within(.01f));
            Assert.AreEqual(2,run.Bases);Assert.AreEqual(1,run.Faces);
            yield return Push("Double_CrateA",3);
            yield return Jump(player.Body.position.x-regionOrigin.x,4.6f,0);
            yield return Walk(15.2f);yield return Push("Double_CrateB",18.2f);
            yield return Walk(13.8f);yield return Jump(14,15.6f,2.15f);yield return Walk(18.1f);yield return Phase();yield return Walk(20.5f);
            yield return new WaitForSeconds(.6f);Assert.Less(player.Body.position.y,regionOrigin.y+1);
            yield return Phase();yield return Push("Double_CrateB",7);
            Assert.IsTrue(Gate.IsOpen,"A remains on the plate while B is moved.");
            yield return Jump(player.Body.position.x-regionOrigin.x,7,1.2f);yield return Jump(7,8.9f,3.2f);yield return Jump(9.8f,12.3f,5);
            yield return Walk(18.1f);yield return Phase();yield return Walk(20.5f);yield return Walk(26);
            Assert.IsTrue(run.HasMagicianHead,"H2 is the new completion reward.");
            ScreenCapture.CaptureScreenshot("Logs/DollwrightGraybox/DoubleCrate_Solved.png");
            yield return Walk(28.5f);yield return new WaitForSeconds(.1f);Assert.IsTrue(run.ExitAvailable);Assert.IsTrue(run.ConfirmExit());
            float until=Time.realtimeSinceStartup+10;
            while(SceneManager.GetActiveScene().path!=DreamTravel.WorkbenchScene&&Time.realtimeSinceStartup<until)yield return null;
            Assert.AreEqual(DreamTravel.WorkbenchScene,SceneManager.GetActiveScene().path);
            Assert.AreEqual(2,DreamTravel.LastBases);Assert.AreEqual(1,DreamTravel.LastFaces);Assert.IsTrue(DreamTravel.LastHead);
        }
    }
}

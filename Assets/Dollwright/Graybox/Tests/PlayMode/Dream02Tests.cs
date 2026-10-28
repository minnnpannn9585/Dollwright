using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
namespace Dollwright.Graybox.Tests
{
    public sealed class Dream02Tests
    {
        const string Entry="Assets/Dollwright/Graybox/Scenes/Dream02_MagicBox.unity";
        const string Stage="Assets/Dollwright/Graybox/Scenes/Dream02_MagicStage.unity";
        GrayboxPlayer2D player;
        DreamPhaseAbility phase;
        IEnumerator Load(string path)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));
            Application.runInBackground=true;
            player=Object.FindObjectOfType<GrayboxPlayer2D>();
            if(player!=null){phase=player.GetComponent<DreamPhaseAbility>();player.SetInputOverride(0);}
            yield return new WaitForSeconds(.25f);
        }
        IEnumerator Walk(float x)
        {
            float dir=Mathf.Sign(x-player.Body.position.x),until=Time.time+Mathf.Abs(x-player.Body.position.x)/4.2f+2;
            player.SetInputOverride(dir);
            while(dir*(x-player.Body.position.x)>.04f && Time.time<until)yield return new WaitForFixedUpdate();
            player.SetInputOverride(0);
            Assert.That(player.Body.position.x,Is.EqualTo(x).Within(.16f),"Walk blocked: "+x);
        }
        IEnumerator Jump(float launch,float landing,float top,bool usePhase=false)
        {
            yield return Walk(launch);yield return new WaitForSeconds(.12f);
            Assert.IsTrue(player.IsGrounded,"Launch grounded at "+launch);
            if(usePhase)yield return Activate();
            float dir=Mathf.Sign(landing-launch);player.SetInputOverride(dir,true);
            yield return new WaitForSeconds(.2f);
            float until=Time.time+2;
            while(Time.time<until)
            {
                if(dir*(landing-player.Body.position.x)<.03f)player.SetInputOverride(0);
                if(player.IsGrounded)break;
                yield return new WaitForFixedUpdate();
            }
            player.SetInputOverride(0);
            Assert.IsTrue(player.IsGrounded,"Landing grounded at "+landing);
            Assert.That(player.Body.position.y,Is.EqualTo(top+.7f).Within(.14f),"Landing height at "+landing);
            // A valid edge landing can occur before the desired center; walk along the verified platform.
            yield return Walk(landing);
        }
        IEnumerator Activate()
        {
            float until=Time.time+3;
            while(!phase.Activate()&&Time.time<until)yield return null;
            Assert.IsTrue(phase.IsPhasing);
        }
        IEnumerator Push(float target)
        {
            var crate=GameObject.Find("Puzzle_PushCrate").GetComponent<Rigidbody2D>();
            float dir=Mathf.Sign(target-crate.position.x),until=Time.time+10;
            player.SetInputOverride(dir);
            while(dir*(target-crate.position.x)>.02f&&Time.time<until)yield return new WaitForFixedUpdate();
            player.SetInputOverride(0);yield return new WaitForFixedUpdate();
            Assert.That(crate.position.x,Is.EqualTo(target).Within(.10f),"Push target");
        }
        [UnityTest]
        public IEnumerator ShelfMagicianEntersSeparateDreamWhileWhiteDollRemainsAvailable()
        {
            yield return Load(DreamTravel.WorkbenchScene);
            var ui=Object.FindObjectOfType<WorkbenchDreamUI>();
            Assert.AreEqual(DreamTravel.DreamScene,ui.initialDoll.dreamScenePath);
            Assert.AreEqual(Entry,ui.magicianDoll.dreamScenePath);
            var stock=DollCraftingStore.Current;
            Assert.IsTrue(stock.AddMaterials(new[]{"R1","F01","H1"}));
            Assert.IsTrue(stock.TryCraft(new DollRecipe(DollBody.Cotton,DollFace.HappyPaint,DollHead.Magician),out var made));
            Assert.IsNotNull(ui.magicianCollider,"Magician becomes selectable after crafting.");
            ui.OpenSelection();yield return new WaitForSeconds(1);
            Assert.IsTrue(ui.TrySelectAtScreenPoint(ui.viewCamera.WorldToScreenPoint(ui.magicianCollider.bounds.center)));
            float until=Time.time+10;
            while(SceneManager.GetActiveScene().path!=Entry&&Time.time<until)yield return null;
            Assert.AreEqual(Entry,SceneManager.GetActiveScene().path);
            Assert.AreEqual("02",DreamTravel.SelectedDollId);
        }
        [UnityTest]
        public IEnumerator RoofFallsThroughOnlyWithAbilityAndRightExitLoadsStage()
        {
            yield return Load(Entry);
            Assert.IsTrue(player.IsGrounded);
            Assert.Greater(player.Body.position.y,6.8f);
            yield return Activate();yield return new WaitForSeconds(1.3f);
            Assert.Less(player.Body.position.y,1);
            Assert.IsTrue(player.IsGrounded);
            yield return Walk(5.35f);
            player.SetInputOverride(1);yield return new WaitForSeconds(.35f);player.SetInputOverride(0);
            Assert.Less(player.Body.position.x,5.6f,"Yellow wall blocks before E.");
            yield return Activate();player.SetInputOverride(1);
            float until=Time.time+5;
            while(SceneManager.GetActiveScene().path!=Stage&&Time.time<until)yield return null;
            Assert.AreEqual(Stage,SceneManager.GetActiveScene().path);
        }
        [UnityTest]
        public IEnumerator GeometryPreventsGroundAndRoofShortcutsAndBoxCannotPhase()
        {
            yield return Load(Stage);
            var a=GameObject.Find("Platform_A_Start").GetComponent<BoxCollider2D>().bounds;
            var middle=GameObject.Find("Platform_B_Middle").GetComponent<BoxCollider2D>().bounds;
            var roof=GameObject.Find("CrateRoof_ReturnRoute").GetComponent<BoxCollider2D>().bounds;
            var curtain=GameObject.Find("Stage_PhaseCurtain").GetComponent<BoxCollider2D>();
            Assert.LessOrEqual(roof.max.x,curtain.bounds.max.x+.01f);
            Assert.GreaterOrEqual(roof.max.x,curtain.bounds.min.x);
            Assert.Less(roof.min.y,1.2f+1.4f,"Cannot stand on box beneath the low roof.");
            Assert.Greater(middle.max.y-roof.max.y,2.6f,"Cannot skip A via roof.");
            player.Teleport(new Vector2(10.8f,.73f));yield return new WaitForSeconds(.15f);
            player.SetInputOverride(1,true);yield return new WaitForSeconds(.9f);player.SetInputOverride(0);
            Assert.Less(player.Body.position.y,a.max.y,"Cannot mount A from ground.");
            var crate=GameObject.Find("Puzzle_PushCrate").GetComponent<GrayboxPushCrate>();
            var body=crate.GetComponent<Rigidbody2D>();body.position=new Vector2(crate.maxX,.6f);crate.transform.position=body.position;
            player.Teleport(new Vector2(22.2f,.73f));yield return new WaitForSeconds(.15f);
            yield return Activate();player.SetInputOverride(1);yield return new WaitForSeconds(.7f);player.SetInputOverride(0);
            Assert.LessOrEqual(crate.GetComponent<BoxCollider2D>().bounds.max.x,curtain.bounds.min.x+.01f);
            Assert.IsFalse(Physics2D.GetIgnoreCollision(crate.GetComponent<Collider2D>(),curtain));
            player.Teleport(new Vector2(6.8f,1.73f));yield return Activate();player.SetInputOverride(1);yield return new WaitForSeconds(.5f);player.SetInputOverride(0);
            Assert.Less(player.Body.position.x,7.1f,"Blue wall stays solid during E.");
        }
        [UnityTest]
        public IEnumerator ContinuousStagePuzzleRequiresBackstageReturnAndReachesBothRewards()
        {
            yield return Load(Stage);
            yield return Jump(2.3f,4.6f,1,true);
            yield return Jump(4.6f,5.7f,2.2f);
            yield return Jump(6.0f,8.9f,0,true);
            var fog=Object.FindObjectOfType<GrayboxFog2D>();
            Assert.Less(fog.FurthestRevealed,20,"Backstage should remain concealed on entry.");
            Assert.Less(player.Body.position.y,1,"Entering stage drops to the floor.");
            yield return Walk(19.9f);yield return Push(23.2f);
            yield return Walk(18.9f);yield return Jump(19.1f,20.6f,2.15f);
            yield return Walk(23.1f);yield return Activate();yield return Walk(25.6f);
            yield return new WaitForSeconds(.6f);
            Assert.Less(player.Body.position.y,1,"Drop in backstage.");
            yield return Activate();yield return Push(10.3f);
            yield return new WaitForSeconds(.35f);
            ScreenCapture.CaptureScreenshot("Logs/DollwrightGraybox/Dream02_BoxAtA.png");
            Assert.Greater(fog.FurthestRevealed,30,"Explored backstage stays revealed after return.");
            yield return Jump(player.Body.position.x,10.3f,1.2f);
            yield return Jump(10.3f,12.1f,3.2f);
            yield return Jump(13.4f,15.8f,4.9f);
            yield return Jump(15.4f,12.8f,6.6f);yield return Walk(12.1f);
            Assert.AreEqual(1,player.run.Bases);
            yield return Jump(12.9f,15.8f,4.9f);
            yield return Jump(17.1f,19.5f,6.6f);yield return Walk(20.6f);
            Assert.AreEqual(1,player.run.Faces);
            yield return Walk(23.1f);yield return Activate();yield return Walk(25.6f);yield return Walk(29.4f);
            var sameRun=player.run;int sameScene=SceneManager.GetActiveScene().handle;
            var layout=Object.FindObjectOfType<Dream02ContinuousLayout>();Assert.IsNotNull(layout);
            Assert.IsNull(GameObject.Find("Prototype_End"));
            yield return Walk(layout.SawOrigin.x+1.8f);
            Assert.AreEqual(sameScene,SceneManager.GetActiveScene().handle,"Stage to saw must not reload a scene.");
            Assert.AreSame(sameRun,player.run);
            Assert.That(player.Body.position.y,Is.EqualTo(layout.SawOrigin.y+.7f).Within(.12f),"No gap or height discontinuity at the join.");
            Assert.That(player.run.Rewind.SavedPlayerPosition.x,Is.EqualTo(layout.SawOrigin.x).Within(.01f));
            Assert.AreEqual(1,player.run.Bases);Assert.AreEqual(1,player.run.Faces);
            Assert.AreEqual(0,player.run.Respawns);
        }
        [UnityTest]
        public IEnumerator PhaseNeverReenablesCollisionInsideCurtainAndRestoresAfterExit()
        {
            yield return Load(Stage);
            var curtain=GameObject.Find("Stage_PhaseCurtain").GetComponent<Collider2D>();
            var shape=player.GetComponent<Collider2D>();
            yield return Activate();player.Teleport(new Vector2(24,.73f));
            yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(Physics2D.GetIgnoreCollision(shape,curtain));
            player.Teleport(new Vector2(25,.73f));yield return null;
            Assert.IsFalse(Physics2D.GetIgnoreCollision(shape,curtain));
        }
    }
}

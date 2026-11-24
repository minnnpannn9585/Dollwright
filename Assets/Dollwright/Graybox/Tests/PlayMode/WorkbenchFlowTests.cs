using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

namespace Dollwright.Graybox.Tests
{
    public sealed class WorkbenchFlowTests
    {
        IEnumerator Load(string path)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            Application.runInBackground = true;
            yield return new WaitForSeconds(.2f);
        }
        IEnumerator WaitScene(string path)
        {
            float end = Time.realtimeSinceStartup + 15;
            while ((SceneManager.GetActiveScene().path != path || DreamTravel.IsLoading) && Time.realtimeSinceStartup < end) yield return null;
            Assert.AreEqual(path, SceneManager.GetActiveScene().path);
            Assert.IsFalse(DreamTravel.IsLoading);
            yield return new WaitForSeconds(.2f);
        }
        void Click(Button button)
        {
            Assert.IsTrue(button.interactable);
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        }
        [UnityTest]
        public IEnumerator AssembledDollsMoveInWindmillDuringNormalPhysics()
        {
            foreach (bool wheel in new[] { true, false })
            {
                yield return Load("Assets/Workbenches/Workbench_02_DollAssembly.unity");
                var bench = Object.FindObjectOfType<RetrievalDemo.RetrievalDemoController>();
                bench.OpenAssembly();
                var assembly = bench.Free;
                assembly.Place(0, new Vector3(0,.562f,0), Quaternion.identity);
                assembly.Place(2, new Vector3(.437f,.15f,0), Quaternion.identity);
                if (wheel) assembly.Place(5, new Vector3(0,-.602f,0), Quaternion.identity);
                else
                {
                    assembly.Place(3, new Vector3(-.17f,-.472f,0), Quaternion.identity);
                    assembly.Place(4, new Vector3(.17f,-.472f,0), Quaternion.identity);
                }
                Assert.IsTrue(assembly.Complete);
                yield return null;
                GameObject.Find("试演 · 纸风车").GetComponent<Button>().onClick.Invoke();
                yield return new WaitForSeconds(.2f);
                var trial = Object.FindObjectOfType<RetrievalDemo.WindmillCommission>();
                Assert.IsNotNull(trial);
                var body = trial.doll.GetComponent<Rigidbody2D>();
                float start = body.position.x;
                float floor = body.position.y;
                float peak = floor;
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var direction = trial.GetType().GetField("direction", flags);
                var jump = trial.GetType().GetField("jump", flags);
                float end = Time.time + .55f;
                while (Time.time < end)
                {
                    direction.SetValue(trial, 1f);
                    jump.SetValue(trial, true);
                    yield return null;
                    peak = Mathf.Max(peak, body.position.y);
                }
                Assert.Greater(body.position.x - start, .6f, "Normal Play Mode must advance horizontal motion");
                Assert.Greater(peak - floor, .35f, "Normal Play Mode must advance jumps");
            }
        }

        [UnityTest]
        public IEnumerator BedSelectionDreamAndWakeCompleteRoundTrip()
        {
            yield return Load(DreamTravel.WorkbenchScene);
            var ui = Object.FindObjectOfType<WorkbenchDreamUI>();
            Assert.IsNotNull(ui);
            Assert.IsFalse(ui.IsSelecting);
            Assert.IsNull(GameObject.Find("SessionSummary"));
            Assert.IsNull(ui.transform.Find("DollSelection"));
            Assert.IsNotNull(ui.dollCollider);
            Click(ui.bedButton);
            Assert.IsTrue(ui.IsSelecting);
            Assert.IsFalse(ui.FocusReady, "No click during camera transition.");
            yield return new WaitForSeconds(1);
            Assert.IsTrue(ui.FocusReady);
            var point = ui.viewCamera.WorldToScreenPoint(ui.dollCollider.bounds.center);
            Assert.Greater(point.z, 0);
            Assert.IsTrue(ui.TrySelectAtScreenPoint(point), "Raycast must hit the visible shelf doll.");
            yield return WaitScene(DreamTravel.DreamScene);
            Assert.AreEqual("01", DreamTravel.SelectedDollId);
            var run = Object.FindObjectOfType<GrayboxRun>();
            Assert.IsTrue(run.returnToWorkbench);
            run.player.SetInputOverride(0);
            run.Collect("B1", GrayboxPickup.Kind.Base);
            run.Collect("F1", GrayboxPickup.Kind.Face);
            run.Collect("H1", GrayboxPickup.Kind.Head);
            run.player.Teleport(new Vector2(138.5f, 3.95f));
            yield return new WaitForSeconds(.15f);
            Assert.IsTrue(run.ExitAvailable);
            Assert.IsTrue(run.ConfirmExit());
            yield return WaitScene(DreamTravel.WorkbenchScene);
            Assert.AreEqual(1, DreamTravel.LastBases);
            Assert.AreEqual(1, DreamTravel.LastFaces);
            Assert.IsTrue(DreamTravel.LastHead);
            ui = Object.FindObjectOfType<WorkbenchDreamUI>();
            Assert.IsFalse(ui.IsSelecting);
            Assert.IsNull(GameObject.Find("SessionSummary"));
            Click(ui.bedButton);
            yield return new WaitForSeconds(1);
            Assert.IsTrue(ui.TrySelectAtScreenPoint(ui.viewCamera.WorldToScreenPoint(ui.dollCollider.bounds.center)));
            yield return WaitScene(DreamTravel.DreamScene);
            run = Object.FindObjectOfType<GrayboxRun>();
            Assert.AreEqual(0, run.Bases);
            Assert.AreEqual(0, run.Faces);
            Assert.IsFalse(run.HasMagicianHead, "A new visit starts a fresh run.");
        }
        [UnityTest]
        public IEnumerator SelectionCanBeClosedWithoutChangingScene()
        {
            yield return Load(DreamTravel.WorkbenchScene);
            var ui = Object.FindObjectOfType<WorkbenchDreamUI>();
            var original = ui.viewCamera.transform.position;
            var rotation = ui.viewCamera.transform.rotation;
            var fov = ui.viewCamera.fieldOfView;
            Click(ui.bedButton);
            yield return new WaitForSeconds(1);
            Assert.Greater(Vector3.Distance(original, ui.viewCamera.transform.position), 1);
            Assert.IsFalse(ui.TrySelectAtScreenPoint(Vector2.zero), "Empty screen cannot enter a dream.");
            Click(ui.backButton.GetComponent<Button>());
            yield return new WaitForSeconds(1);
            Assert.Less(Vector3.Distance(original, ui.viewCamera.transform.position), .001f);
            Assert.Less(Quaternion.Angle(rotation, ui.viewCamera.transform.rotation), .01f);
            Assert.That(ui.viewCamera.fieldOfView, Is.EqualTo(fov).Within(.001f));
            Assert.IsFalse(ui.IsSelecting);
            Assert.IsTrue(ui.bedButton.interactable);
            Assert.AreEqual(DreamTravel.WorkbenchScene, SceneManager.GetActiveScene().path);
        }
        [UnityTest]
        public IEnumerator CircusCrateIsIndependentAndRaisedLedgeRequiresIt()
        {
            yield return Load(DreamTravel.DreamScene);
            var crate = GameObject.Find("PushCrate_Circus");
            Assert.IsNotNull(crate.GetComponent<SpriteRenderer>());
            Assert.IsNotNull(crate.GetComponent<BoxCollider2D>());
            Assert.IsNotNull(crate.GetComponent<Rigidbody2D>());
            Assert.IsNotNull(crate.GetComponent<GrayboxPushCrate>());
            Assert.IsNull(GameObject.Find("PushMark (1)"));
            Assert.AreEqual("01_CollisionGeometry", crate.transform.parent.name);
            var run = Object.FindObjectOfType<GrayboxRun>();
            var player = run.player;
            player.Teleport(new Vector2(50.8f, 2.93f));
            player.SetInputOverride(0);
            yield return new WaitForSeconds(.15f);
            player.SetInputOverride(1, true);
            float maxFeet = 0, until = Time.time + .9f;
            while (Time.time < until)
            {
                maxFeet = Mathf.Max(maxFeet, player.Body.position.y - .7f);
                yield return new WaitForFixedUpdate();
            }
            player.SetInputOverride(0);
            var ledge = GameObject.Find("CircusClimb_01").GetComponent<BoxCollider2D>();
            Assert.Less(maxFeet, ledge.bounds.max.y - .1f);
            Assert.That(ledge.bounds.max.y, Is.EqualTo(4.915f).Within(.01f), "Preserve the user's raised platform.");
            Assert.That(GameObject.Find("FloatingFlags_03").transform.position.y, Is.EqualTo(6.09f).Within(.01f));
        }
        [UnityTest]
        public IEnumerator CircusCrateFallsAndReturnsToSpawnOnBothEdges()
        {
            foreach (float direction in new[] { 1f, -1f })
            {
                yield return Load(DreamTravel.DreamScene);
                var crate = GameObject.Find("PushCrate_Circus").GetComponent<GrayboxPushCrate>();
                var body = crate.GetComponent<Rigidbody2D>();
                var origin = body.position;
                var player = Object.FindObjectOfType<GrayboxPlayer2D>();
                player.Teleport(origin + new Vector2(-direction * 1.1f, .14f));
                player.SetInputOverride(0);
                yield return new WaitForSeconds(.2f);
                player.SetInputOverride(direction);
                float deadline = Time.time + 8;
                while (!crate.IsFalling && Time.time < deadline) yield return new WaitForFixedUpdate();
                player.SetInputOverride(0);
                Assert.IsTrue(crate.IsFalling, "Pushing past the edge must release the crate.");
                deadline = Time.time + 1;
                while (body.position.y > origin.y - .4f && Time.time < deadline) yield return new WaitForFixedUpdate();
                Assert.Less(body.position.y, origin.y - .4f, "Gravity must actually lower the crate.");
                deadline = Time.time + 4;
                while (crate.ResetCount == 0 && Time.time < deadline) yield return new WaitForFixedUpdate();
                Assert.AreEqual(1, crate.ResetCount);
                Assert.Less(Vector2.Distance(body.position, origin), .01f);
                Assert.IsFalse(crate.IsFalling);
                Assert.AreEqual(RigidbodyType2D.Kinematic, body.bodyType);
            }
        }
        [UnityTest]
        public IEnumerator ExitIsAfterCounterWithContinuousFloorAndNoTrap()
        {
            yield return Load(DreamTravel.DreamScene);
            var counter = GameObject.Find("BubbleCounter_Solid").GetComponent<BoxCollider2D>();
            var exit = GameObject.Find("WakeUpExit");
            Assert.Greater(exit.transform.position.x, counter.bounds.max.x);
            var run = Object.FindObjectOfType<GrayboxRun>();
            run.player.SetInputOverride(0);
            run.player.Teleport(new Vector2(133.5f, counter.bounds.max.y + .73f));
            yield return new WaitForSeconds(.15f);
            run.player.SetInputOverride(1);
            while (run.player.Body.position.x < 140)
            {
                Assert.Greater(run.player.Body.position.y, counter.bounds.max.y + .5f, "No pit after the stall.");
                yield return new WaitForFixedUpdate();
            }
            run.player.SetInputOverride(0);
            Assert.AreEqual(0, run.Respawns);
            Assert.IsFalse(run.Completed);
        }
    }
}

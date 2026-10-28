using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Dollwright.Graybox.Tests
{
    public sealed class Dream02ContinuousTests
    {
        const string Stage="Assets/Dollwright/Graybox/Scenes/Dream02_MagicStage.unity";
        IEnumerator Load()
        {
            DreamTravel.ClearJourney();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Stage,new LoadSceneParameters(LoadSceneMode.Single));
            Application.runInBackground=true;
            Object.FindObjectOfType<GrayboxPlayer2D>().SetInputOverride(0);
            yield return new WaitForSeconds(.25f);
        }
        [UnityTest]
        public IEnumerator OnlyTwoPlayableDream02ScenesAndOneContinuousRuntime()
        {
            var scenes=EditorBuildSettings.scenes.Where(x=>x.enabled&&x.path.Contains("/Dream02_")).ToArray();
            Assert.AreEqual(2,scenes.Length);
            Assert.IsTrue(scenes.Any(x=>x.path.EndsWith("Dream02_MagicBox.unity")));
            Assert.IsTrue(scenes.Any(x=>x.path==Stage));
            yield return Load();
            Assert.AreEqual(1,Object.FindObjectsOfType<GrayboxPlayer2D>().Length);
            Assert.AreEqual(1,Object.FindObjectsOfType<GrayboxRun>().Length);
            Assert.AreEqual(1,Object.FindObjectsOfType<Camera>().Length);
            Assert.AreEqual(1,Object.FindObjectsOfType<GrayboxFog2D>().Length);
            Assert.AreEqual(5,Object.FindObjectsOfType<GrayboxPushCrate>().Length);
            Assert.AreEqual(2,Object.FindObjectsOfType<DreamSawGate>().Length);
            Assert.AreEqual(0,Object.FindObjectsOfType<Dream02Portal>().Length,"Only the separate entry scene owns a scene portal.");
            Assert.AreEqual(3,Object.FindObjectsOfType<GrayboxTrigger>().Count(x=>x.action==GrayboxTrigger.Action.Checkpoint));
            var pickups=Object.FindObjectsOfType<GrayboxPickup>();
            Assert.AreEqual(pickups.Length,pickups.Select(x=>x.materialId).Distinct().Count());
            Assert.AreEqual("H2",Object.FindObjectOfType<GrayboxRun>().requiredHeadId);
        }
        [UnityTest]
        public IEnumerator JoinedFloorsRailsWiresAndRevealedFogStayInOneCoordinateSpace()
        {
            yield return Load();
            var layout=Object.FindObjectOfType<Dream02ContinuousLayout>();
            var player=Object.FindObjectOfType<GrayboxPlayer2D>();
            var fog=Object.FindObjectOfType<GrayboxFog2D>();var guide=Object.FindObjectOfType<Dream02Guide>();
            Bounds end=GameObject.Find("Continuation_Preview").GetComponent<BoxCollider2D>().bounds;
            Bounds saw=layout.sawSection.GetComponentsInChildren<BoxCollider2D>().Single(x=>x.name=="RoomFloor").bounds;
            Bounds doubles=layout.doubleSection.GetComponentsInChildren<BoxCollider2D>().Single(x=>x.name=="RoomFloor").bounds;
            Assert.That(end.max.x,Is.EqualTo(saw.min.x).Within(.002f));Assert.That(end.max.y,Is.EqualTo(saw.max.y).Within(.002f));
            Assert.That(saw.max.x,Is.EqualTo(doubles.min.x).Within(.002f));Assert.That(saw.max.y,Is.EqualTo(doubles.max.y).Within(.002f));
            foreach(var box in Object.FindObjectsOfType<GrayboxPushCrate>())
            {
                Assert.AreSame(player,box.player);
                Assert.That(box.transform.position.x,Is.InRange(box.minX,box.maxX));
            }
            foreach(string id in new[]{"P1","P2"})
            {
                var plate=GameObject.Find("PressurePlate_"+id).GetComponent<DreamPressurePlate>();
                var gate=GameObject.Find("SawGate_"+id).GetComponent<DreamSawGate>();
                var wire=GameObject.Find("PlateToSaw_"+id).GetComponent<LineRenderer>();
                Assert.AreSame(plate,gate.plate);Assert.AreSame(gate,plate.saw);
                Assert.That(wire.GetPosition(0).x,Is.EqualTo(plate.transform.position.x).Within(.002f));
                Assert.That(wire.GetPosition(0).y,Is.EqualTo(plate.transform.position.y+.08f).Within(.002f));
                Assert.That(wire.GetPosition(3).x,Is.EqualTo(gate.transform.position.x).Within(.002f));
                Assert.That(wire.GetPosition(3).y,Is.EqualTo(gate.GetComponent<BoxCollider2D>().bounds.max.y).Within(.002f));
            }
            player.Teleport(layout.DoubleOrigin+new Vector2(5,.73f));yield return null;yield return null;
            Assert.AreEqual(4,guide.section);float explored=fog.FurthestRevealed;
            player.Teleport(new Vector2(10,.73f));yield return null;yield return null;
            Assert.AreEqual(2,guide.section);Assert.GreaterOrEqual(fog.FurthestRevealed,explored);
        }
    }
}


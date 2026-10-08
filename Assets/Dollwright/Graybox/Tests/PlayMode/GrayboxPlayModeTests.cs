using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Dollwright.Graybox.Tests
{
    public sealed class GrayboxPlayModeTests
    {
        const string Movement = "Assets/Dollwright/Graybox/Scenes/MovementTest.unity";
        const string Dream = "Assets/Dollwright/Graybox/Scenes/Dream01_Graybox.unity";
        GrayboxPlayer2D player;
        GrayboxRun run;

        IEnumerator Load(string path)
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync(path);
#endif
            yield return null;
            run = Object.FindObjectOfType<GrayboxRun>();
            player = run.player;
            player.SetInputOverride(0);
            yield return new WaitForSeconds(0.3f);
        }

        [UnityTest]
        public IEnumerator WalkingAndJumpingUsePhysicsAndDisallowDoubleJump()
        {
            yield return Load(Movement);
            Assert.IsTrue(player.IsGrounded, "Player must start on the floor.");
            float startX = player.Body.position.x;
            player.SetInputOverride(1);
            yield return new WaitForSeconds(0.4f);
            Assert.Greater(player.Body.position.x - startX, 1.3f);
            player.SetInputOverride(0);
            float floorY = player.Body.position.y;
            player.SetInputOverride(0, true);
            yield return new WaitForSeconds(0.2f);
            Assert.Greater(player.Body.position.y - floorY, 1.3f);
            Assert.AreEqual(1, player.JumpCount);
            player.SetInputOverride(0, true);
            yield return new WaitForSeconds(0.22f);
            Assert.AreEqual(1, player.JumpCount, "Airborne input must not grant a second jump.");
            yield return new WaitForSeconds(0.65f);
            Assert.IsTrue(player.IsGrounded);
            Assert.That(player.Body.position.y, Is.EqualTo(floorY).Within(0.08f));
        }

        [UnityTest]
        public IEnumerator FirstStepIsReachableWithAFullJump()
        {
            yield return Load(Movement);
            player.Teleport(new Vector2(4.8f, 0.72f));
            yield return new WaitForSeconds(0.15f);
            player.SetInputOverride(1, true);
            yield return new WaitForSeconds(0.65f);
            player.SetInputOverride(0);
            yield return new WaitForSeconds(0.35f);
            Assert.IsTrue(player.IsGrounded);
            Assert.Greater(player.Body.position.y, 1.7f, "The real collider must land on Step_01.");
        }

        [UnityTest]
        public IEnumerator CameraRemainsOrthographicAndTracksHorizontally()
        {
            yield return Load(Dream);
            var camera = Camera.main;
            player.Teleport(new Vector2(30, 0.72f));
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(camera.orthographic);
            Assert.Greater(camera.transform.position.x, 29);
            Assert.That(camera.transform.position.z, Is.EqualTo(-10).Within(0.001f));
            Assert.That(camera.transform.position.y, Is.EqualTo(3.25f).Within(0.001f));
            Assert.That(Quaternion.Angle(camera.transform.rotation, Quaternion.identity), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator MagicBoxBlocksHappinessDollAndCircusStepsReachHead()
        {
            yield return Load(Dream);
            player.Teleport(new Vector2(50.8f, 0.72f));
            player.SetInputOverride(1);
            yield return new WaitForSeconds(0.5f);
            Assert.Less(player.Body.position.x, 51.3f, "Happiness doll must not walk inside the solid magic box.");
            player.SetInputOverride(0);
            // Traverse the actual rising platform colliders; no teleport to the head.
            player.Teleport(new Vector2(38.7f, 0.72f));
            yield return new WaitForSeconds(0.15f);
            yield return JumpTo(41.3f, 1.1f);
            yield return JumpTo(45.2f, 2.2f);
            yield return JumpTo(49.2f, 3.3f);
            yield return JumpTo(53.0f, 4.4f);
            player.SetInputOverride(1);
            float deadline = Time.time + 2;
            while (!run.HasMagicianHead && Time.time < deadline) yield return new WaitForFixedUpdate();
            player.SetInputOverride(0);
            Assert.IsTrue(run.HasMagicianHead, "The head must be collectible after climbing the box.");
        }

        IEnumerator JumpTo(float x, float platformTop)
        {
            Assert.IsTrue(player.IsGrounded, "Each stair jump must start grounded.");
            // Walk to an edge before jumping across a larger platform interval.
            float start = player.Body.position.x;
            float desiredLaunch = x - 2.55f;
            if (desiredLaunch > start + 0.1f)
            {
                player.SetInputOverride(1);
                float walkDeadline = Time.time + 1.3f;
                while (player.Body.position.x < desiredLaunch && Time.time < walkDeadline)
                    yield return new WaitForFixedUpdate();
            }
            player.SetInputOverride(1, true);
            float deadline = Time.time + 1.5f;
            yield return new WaitForSeconds(0.16f);
            while (Time.time < deadline)
            {
                if (player.Body.position.x >= x) player.SetInputOverride(0);
                if (player.IsGrounded) break;
                yield return new WaitForFixedUpdate();
            }
            player.SetInputOverride(0);
            Assert.IsTrue(player.IsGrounded, "Stair jump must reach a solid landing.");
            Assert.That(player.Body.position.y, Is.EqualTo(platformTop + 0.70f).Within(0.10f));
        }

        [UnityTest]
        public IEnumerator ThreeParkGapsAreJumpableWithoutFalling()
        {
            yield return Load(Dream);
            // Test exact final approach positions against the actual platforms.
            float[] starts = { 76.65f, 99.65f, 123.65f };
            float[] tops = { 1.2f, 0.8f, 0.65f };
            float[] destinations = { 80.5f, 103.5f, 127.0f };
            for (int i = 0; i < starts.Length; i++)
            {
                player.Teleport(new Vector2(starts[i], tops[i] + 0.72f));
                yield return new WaitForSeconds(0.15f);
                Assert.IsTrue(player.IsGrounded);
                player.SetInputOverride(1, true);
                float deadline = Time.time + 1.4f;
                yield return new WaitForSeconds(0.2f);
                while (Time.time < deadline)
                {
                    if (player.Body.position.x > destinations[i]) player.SetInputOverride(0);
                    if (player.IsGrounded) break;
                    yield return new WaitForFixedUpdate();
                }
                player.SetInputOverride(0);
                Assert.AreEqual(0, run.Respawns);
                Assert.IsTrue(player.IsGrounded);
                Assert.Greater(player.Body.position.x, destinations[i] - 0.9f);
            }
        }

        [UnityTest]
        public IEnumerator PickupTriggersCannotCountTwiceAndExitRequiresHead()
        {
            yield return Load(Dream);
            Assert.IsFalse(run.TryComplete());
            var pickups = Object.FindObjectsOfType<GrayboxPickup>();
            Assert.AreEqual(6, pickups.Length);
            foreach (var pickup in pickups)
            {
                player.Teleport(pickup.transform.position);
                yield return new WaitForSeconds(0.10f);
                Assert.IsTrue(run.Collected.Contains(pickup.materialId));
                Assert.IsFalse(pickup.gameObject.activeSelf);
                Assert.IsFalse(run.Collect(pickup.materialId, pickup.kind));
            }
            Assert.AreEqual(3, run.Bases);
            Assert.AreEqual(2, run.Faces);
            Assert.IsTrue(run.HasMagicianHead);
            player.Teleport(new Vector2(154, 0.75f));
            yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(run.Completed, "The exit trigger must complete after obtaining the head.");
        }

        [UnityTest]
        public IEnumerator FallingRestoresCheckpointWithoutLosingMaterials()
        {
            yield return Load(Dream);
            run.Collect("B1", GrayboxPickup.Kind.Base);
            run.SetCheckpoint(new Vector2(34, 0.76f), "test");
            player.Teleport(new Vector2(78, -6));
            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(1, run.Respawns);
            Assert.That(player.Body.position.x, Is.EqualTo(34).Within(0.05f));
            Assert.AreEqual(1, run.Bases);
        }

        [UnityTest]
        public IEnumerator RestartLoadsStandaloneGrayboxAndClearsRun()
        {
            yield return Load(Dream);
            int oldInstance = run.GetInstanceID();
            run.Collect("B1", GrayboxPickup.Kind.Base);
            run.Collect("H1", GrayboxPickup.Kind.Head);
            player.Teleport(new Vector2(34, 0.76f));
            run.Restart();
            float deadline = Time.time + 3;
            GrayboxRun restarted = null;
            while (Time.time < deadline)
            {
                restarted = Object.FindObjectOfType<GrayboxRun>();
                if (restarted != null && restarted.GetInstanceID() != oldInstance) break;
                yield return null;
            }
            Assert.IsNotNull(restarted);
            Assert.AreNotEqual(oldInstance, restarted.GetInstanceID());
            Assert.AreEqual(0, restarted.Bases);
            Assert.AreEqual(0, restarted.Faces);
            Assert.IsFalse(restarted.HasMagicianHead);
            Assert.That(restarted.player.transform.position.x, Is.EqualTo(1).Within(0.05f));
            Assert.AreEqual(Dream, SceneManager.GetActiveScene().path);
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(restarted.player.IsGrounded);
        }

        [UnityTest]
        public IEnumerator FogRevealsAheadAndDoesNotHideVisitedSpace()
        {
            yield return Load(Dream);
            var fog = Object.FindObjectOfType<GrayboxFog2D>();
            player.Teleport(new Vector2(30, 0.72f));
            yield return null;
            yield return null;
            float revealed = fog.FurthestRevealed;
            Assert.GreaterOrEqual(revealed, 37.9f);
            player.Teleport(new Vector2(12, 0.72f));
            yield return null;
            Assert.That(fog.FurthestRevealed, Is.EqualTo(revealed).Within(0.01f));
        }
    }
}

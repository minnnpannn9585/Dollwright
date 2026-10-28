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
            run.returnToWorkbench = false;
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


        IEnumerator WalkTo(float x)
        {
            float direction = Mathf.Sign(x - player.Body.position.x);
            float deadline = Time.time + Mathf.Abs(x - player.Body.position.x) / player.moveSpeed + 1.5f;
            player.SetInputOverride(direction);
            while (direction * (x - player.Body.position.x) > 0.04f && Time.time < deadline)
                yield return new WaitForFixedUpdate();
            player.SetInputOverride(0);
            Assert.That(player.Body.position.x, Is.EqualTo(x).Within(0.16f), "Walk blocked before " + x);
        }

        IEnumerator Jump(float launchX, float landingX, float top)
        {
            yield return WalkTo(launchX);
            yield return new WaitForSeconds(0.10f);
            Assert.IsTrue(player.IsGrounded, "Not grounded at launch " + launchX);
            int before = player.JumpCount;
            float direction = Mathf.Sign(landingX - launchX);
            player.SetInputOverride(direction, true);
            yield return new WaitForSeconds(0.2f);
            float deadline = Time.time + 1.4f;
            while (Time.time < deadline)
            {
                if (direction * (landingX - player.Body.position.x) < 0.03f) player.SetInputOverride(0);
                if (player.IsGrounded) break;
                yield return new WaitForFixedUpdate();
            }
            player.SetInputOverride(0);
            Assert.AreEqual(before + 1, player.JumpCount, "Jump did not trigger at " + launchX);
            Assert.IsTrue(player.IsGrounded, "Failed landing " + landingX);
            Assert.That(player.Body.position.y, Is.EqualTo(top + 0.70f).Within(0.14f), "Wrong landing height near " + landingX);
            Assert.That(player.Body.position.x, Is.EqualTo(landingX).Within(0.22f), "Wrong landing X");
        }

        IEnumerator PushToStop(string name)
        {
            var crate = GameObject.Find(name).GetComponent<GrayboxPushCrate>();
            var body = crate.GetComponent<Rigidbody2D>();
            float start = body.position.x;
            float startY = body.position.y;
            player.SetInputOverride(1);
            float deadline = Time.time + 6;
            while (body.position.x < crate.maxX - 0.01f && Time.time < deadline)
                yield return new WaitForFixedUpdate();
            if (!crate.fallAndRespawn) yield return new WaitForSeconds(0.2f);
            player.SetInputOverride(0);
            yield return new WaitForFixedUpdate();
            Assert.Greater(body.position.x - start, 3, "Contact walking must move the crate.");
            Assert.That(body.position.x, Is.EqualTo(crate.maxX).Within(crate.fallAndRespawn ? 0.10f : 0.02f), "Release the falling crate at the ledge; other crates use fixed rails.");
            Assert.That(body.position.y, Is.EqualTo(startY).Within(0.01f), "Crate must stay on its own platform height.");
        }

        [UnityTest]
        public IEnumerator EntireEditedDreamIsTraversableWithThreePushSections()
        {
            yield return Load(Dream);
            // One continuous run from spawn: all route jumps and both crates use real physics.
            // The current edited entry is a flat walk; the former small obstacle was removed.
            yield return WalkTo(11.6f);
            yield return PushToStop("PushCrate_Required");
            yield return Jump(player.Body.position.x, 19.4f, 1.2f);
            yield return Jump(19.4f, 21.1f, 3.2f);
            yield return WalkTo(24);
            Assert.AreEqual(1, run.Bases);
            yield return Jump(25.1f, 28.1f, 3.7f);
            yield return Jump(30.2f, 33.2f, 4.6f);
            yield return WalkTo(34.4f);
            yield return Jump(35.65f, 38.15f, 6.265f);
            yield return Jump(41, 44, 2.2f);
            yield return PushToStop("PushCrate_Circus");
            Assert.AreEqual(2, run.Faces);
            yield return Jump(player.Body.position.x, 50.9f, 3.4f);
            yield return Jump(51.1f, 53.6f, 4.915f);
            yield return Jump(55.3f, 58, 5);
            yield return Jump(59.65f, 61.75f, 6.5f);
            yield return Jump(62.4f, 65, 8);
            yield return WalkTo(70);
            Assert.IsTrue(run.HasMagicianHead, "Head must be gained on the box roof.");
            yield return Jump(71.6f, 74.5f, 6.6f);
            yield return Jump(77.7f, 80.5f, 4.9f);
            yield return WalkTo(82.5f);
            yield return Jump(84, 86.7f, 3.2f);
            yield return Jump(90.75f, 94.05f, 3.2f);
            yield return WalkTo(110);
            yield return Jump(111.75f, 114.8f, 0);
            Assert.AreEqual(2, run.Bases);
            yield return WalkTo(122.5f);
            Assert.IsFalse(run.ExitAvailable);
            yield return PushToStop("PushCrate_Bonus");
            yield return Jump(player.Body.position.x, 128.4f, 1.2f);
            yield return Jump(128.4f, 130.1f, 3.2f);
            yield return WalkTo(132);
            Assert.AreEqual(3, run.Bases);
            yield return WalkTo(138.5f);
            Assert.IsTrue(run.ExitAvailable);
            Assert.IsFalse(run.Completed, "Wake requires explicit confirmation.");
            Assert.IsTrue(run.ConfirmExit());
            Assert.AreEqual(0, run.Respawns, "All gaps must be traversable without falling.");
            Debug.Log("[Dollwright] Continuous automated run: " + run.Elapsed.ToString("F1") + " seconds, " + player.JumpCount + " jumps.");
        }

        [UnityTest]
        public IEnumerator HighLedgeAndBonusCounterCannotBeJumpedFromGround()
        {
            yield return Load(Dream);
            float[] walls = { 20, 129 };
            for (int i = 0; i < walls.Length; i++)
            {
                player.Teleport(new Vector2(walls[i] - 1.3f, 0.73f));
                yield return new WaitForSeconds(0.15f);
                player.SetInputOverride(1, true);
                float maxFeet = 0, until = Time.time + 1.15f;
                while (Time.time < until)
                {
                    maxFeet = Mathf.Max(maxFeet, player.Body.position.y - 0.7f);
                    yield return new WaitForFixedUpdate();
                }
                player.SetInputOverride(0);
                Assert.Less(maxFeet, 2.6f);
                Assert.Less(player.Body.position.x, walls[i] - 0.2f);
                Assert.IsFalse(run.Collected.Contains(i == 0 ? "B1" : "B3"));
            }
            var required = GameObject.Find("PushCrate_Required").GetComponent<GrayboxPushCrate>();
            float maxJumpReach = player.moveSpeed * 2 * player.jumpSpeed / (Mathf.Abs(Physics2D.gravity.y) * player.Body.gravityScale);
            Assert.Less(required.transform.position.x + 0.6f + maxJumpReach + 0.325f, 20,
                "Initial crate position must be too far from the high ledge.");
            Assert.IsNotNull(GameObject.Find("BubbleCounter_Solid").GetComponent<BoxCollider2D>());
            Assert.AreEqual(0, Object.FindObjectsOfType<GrayboxOccluder>().Length, "Counter must never fade into a phasing pickup.");
        }

        [UnityTest]
        public IEnumerator StandingOnCrateDoesNotPushItOrTrapPlayer()
        {
            yield return Load(Dream);
            var crate = GameObject.Find("PushCrate_Required").GetComponent<Rigidbody2D>();
            player.Teleport(new Vector2(13.2f, 1.92f));
            yield return new WaitForSeconds(0.15f);
            Assert.IsTrue(player.IsGrounded);
            player.SetInputOverride(1);
            yield return new WaitForSeconds(0.16f);
            player.SetInputOverride(0);
            Assert.That(crate.position.x, Is.EqualTo(13.2f).Within(0.01f));
            // Either side may push on the safe horizontal rail.
            player.Teleport(new Vector2(14.2f, 0.73f));
            yield return new WaitForSeconds(0.15f);
            player.SetInputOverride(-1);
            yield return new WaitForSeconds(1);
            player.SetInputOverride(0);
            Assert.That(crate.position.x, Is.EqualTo(12.4f).Within(0.04f));
            Assert.IsTrue(player.IsGrounded);
        }

        [UnityTest]
        public IEnumerator FlagFallUsesActualCheckpointAndPreservesInventoryWithRecovery()
        {
            yield return Load(Dream);
            player.Teleport(new Vector2(24, 3.93f));
            yield return new WaitForSeconds(0.2f);
            run.Collect("B1", GrayboxPickup.Kind.Base);
            var hits = Physics2D.RaycastAll(new Vector2(26.3f, 2.5f), Vector2.down, 10);
            Assert.IsFalse(hits.Any(h => !h.collider.isTrigger), "There must be no rescue floor below the flag gap.");
            player.Teleport(new Vector2(26.3f, -6));
            player.SetInputOverride(1);
            yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(1, run.Respawns);
            Assert.IsTrue(run.IsRecovering);
            Assert.That(player.Body.position.x, Is.EqualTo(24).Within(0.05f));
            Assert.AreEqual(1, run.Bases);
            yield return new WaitForSeconds(0.5f);
            Assert.That(player.Body.position.x, Is.EqualTo(24).Within(0.05f));
            yield return new WaitForSeconds(0.45f);
            Assert.IsFalse(run.IsRecovering);
            Assert.Greater(player.Body.position.x, 24.3f);
            player.SetInputOverride(0);
        }

        [UnityTest]
        public IEnumerator CameraKeepsSideViewAndRevealsSummitWithoutTilting()
        {
            yield return Load(Dream);
            player.Teleport(new Vector2(68, 8.73f));
            yield return new WaitForSeconds(1.2f);
            var camera = Camera.main;
            Assert.IsTrue(camera.orthographic);
            Assert.Greater(camera.transform.position.x, 68);
            Assert.Greater(camera.transform.position.y, 9);
            Assert.That(camera.transform.position.z, Is.EqualTo(-10).Within(0.001f));
            Assert.That(Quaternion.Angle(camera.transform.rotation, Quaternion.identity), Is.LessThan(0.01f));
            Vector3 screen = camera.WorldToViewportPoint(new Vector3(68.5f, 9, 0));
            Assert.That(screen.y, Is.InRange(0.2f, 0.8f));
            yield return Load(Movement);
            Assert.IsFalse(Camera.main.GetComponent<GrayboxCamera2D>().followHeight, "Movement test must keep its original fixed camera.");
        }

        [UnityTest]
        public IEnumerator PickupTriggersCannotCountTwiceAndWakeRequiresHead()
        {
            yield return Load(Dream);
            player.Teleport(new Vector2(138.5f, 3.95f));
            yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(run.ExitAvailable);
            Assert.IsFalse(run.ConfirmExit());
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
            player.Teleport(new Vector2(138.5f, 3.95f));
            yield return new WaitForSeconds(0.1f);
            Assert.IsFalse(run.Completed);
            Assert.IsTrue(run.ConfirmExit());
        }

        [UnityTest]
        public IEnumerator MagicBoxStaysSolidAndHeadIsHighestWithNoRevisitEntrance()
        {
            yield return Load(Dream);
            var box = GameObject.Find("MagicBox_SolidForHappiness").GetComponent<BoxCollider2D>();
            Assert.IsNull(GameObject.Find("MagicianRevisitEntrance_Reserved"), "Dream 02 now starts in its own scene; the revisit secret was cancelled.");
            Assert.IsFalse(box.isTrigger);
            Assert.That(box.bounds.max.y, Is.EqualTo(8).Within(0.01f));
            Assert.IsTrue(Object.FindObjectsOfType<GrayboxPickup>().All(p => p.kind == GrayboxPickup.Kind.Head || p.transform.position.y < 8));
            player.Teleport(new Vector2(63.1f, 4));
            player.SetInputOverride(1);
            yield return new WaitForSeconds(0.3f);
            player.SetInputOverride(0);
            Assert.Less(player.Body.position.x, 63.8f, "White doll cannot enter the solid box.");
        }

        [UnityTest]
        public IEnumerator RestartLoadsStandaloneGrayboxAndClearsRun()
        {
            yield return Load(Dream);
            int oldInstance = run.GetInstanceID();
            run.Collect("B1", GrayboxPickup.Kind.Base);
            run.Collect("H1", GrayboxPickup.Kind.Head);
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
        public IEnumerator FogRevealsCircusLandmarkAndKeepsVisitedSpace()
        {
            yield return Load(Dream);
            var fog = Object.FindObjectOfType<GrayboxFog2D>();
            player.Teleport(new Vector2(11.5f, 0.73f));
            yield return null;
            yield return null;
            Assert.GreaterOrEqual(fog.FurthestRevealed, 25.5f, "Show the push target before the crate interaction.");
            player.Teleport(new Vector2(49, 2.93f));
            yield return null;
            yield return null;
            float revealed = fog.FurthestRevealed;
            Assert.GreaterOrEqual(revealed, 73, "Circus approach must expose the upcoming landmark.");
            player.Teleport(new Vector2(24, 3.93f));
            yield return null;
            Assert.That(fog.FurthestRevealed, Is.EqualTo(revealed).Within(0.01f));
            player.Teleport(new Vector2(121, 0.73f));
            yield return null;
            yield return null;
            Assert.GreaterOrEqual(fog.FurthestRevealed, 136, "Show the bonus reward before the player chooses to push.");
        }
    }
}

using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace TemporalEcho.Tests
{
    public class MovementTrailTests
    {
        private GameInputScope inputScope;
        private Keyboard keyboard;
        private Touchscreen touchscreen;

        [SetUp]
        public void SetUpInput()
        {
            inputScope = new GameInputScope();
        }

        [TearDown]
        public void RestoreInput()
        {
            Time.timeScale = 1f;
            if (keyboard != null)
            {
                InputSystem.RemoveDevice(keyboard);
            }

            if (touchscreen != null)
            {
                InputSystem.RemoveDevice(touchscreen);
            }

            inputScope.Dispose();
        }

        [UnityTest]
        public IEnumerator KeyboardTrailFreezesDuringPauseAndClearsOnTeleport()
        {
            yield return SceneManager.LoadSceneAsync("Level_1");
            yield return null;
            GameObject player = GameObject.FindWithTag("Player");
            MeshRenderer[] trails = MotionTrails(player);
            Assert.That(trails.Length, Is.InRange(1, 4));
            Assert.That(trails.All(trail => trail.GetComponent<Collider>() == null), Is.True);
            yield return new WaitForSeconds(0.4f);
            Assert.That(trails.Any(trail => trail.enabled), Is.False, "Idle head bob must not emit a trail.");
            Assert.That(trails.Any(IsVisible), Is.False);

            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return Press(Key.W);
            yield return WaitFor(() => trails.Any(IsVisible));
            Mesh mesh = trails.First(IsVisible).GetComponent<MeshFilter>().sharedMesh;
            Assert.That(Vector3.Distance(mesh.vertices[0], mesh.vertices[1]), Is.GreaterThan(0.001f),
                "The ribbon must have visible width, not just path points.");
            Time.timeScale = 0f;
            yield return null;
            int[] pausedCounts = trails.Select(trail => trail.GetComponent<MeshFilter>().sharedMesh.vertexCount).ToArray();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(trails.Select(trail => trail.GetComponent<MeshFilter>().sharedMesh.vertexCount).ToArray(), Is.EqualTo(pausedCounts),
                "A paused trail must retain its shape until scaled time resumes.");

            Component movement = player.GetComponent("PlayerMovementController");
            movement.GetType().GetMethod("TeleportTo").Invoke(movement,
                new object[] { player.transform.position + Vector3.right * 2f });
            Assert.That(trails.All(trail => trail.GetComponent<MeshFilter>().sharedMesh.vertexCount == 0), Is.True,
                "Teleport must clear the old trail immediately.");
            Time.timeScale = 1f;
            yield return new WaitForSeconds(0.4f);
            Assert.That(trails.Any(IsVisible), Is.False);
            Assert.That(MotionTrails(player).Length, Is.EqualTo(trails.Length),
                "Movement must reuse the existing trail renderers.");
        }

        [UnityTest]
        public IEnumerator TouchAndBothCloneColorsProduceShortLivedTrails()
        {
            yield return SceneManager.LoadSceneAsync("Level_3");
            yield return null;
            GameObject player = GameObject.FindWithTag("Player");
            MeshRenderer[] playerTrails = MotionTrails(player);
            touchscreen = InputSystem.AddDevice<Touchscreen>();
            Vector2 start = new Vector2(Screen.width * 0.4f, Screen.height * 0.3f);
            InputSystem.QueueStateEvent(touchscreen, new TouchState
            {
                touchId = 1,
                phase = TouchPhase.Began,
                position = start,
                pressure = 1f
            });
            yield return null;
            InputSystem.QueueStateEvent(touchscreen, new TouchState
            {
                touchId = 1,
                phase = TouchPhase.Ended,
                position = start + Vector2.up * 100f
            });
            yield return WaitFor(() => playerTrails.Any(IsVisible));
            yield return new WaitForSeconds(0.6f);
            Assert.That(playerTrails.Any(IsVisible), Is.False, "The trail must fade shortly after a completed step.");

            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return Press(Key.R);
            yield return WaitFor(() => ActorHasTrail("Clone_1"));
            MeshRenderer[] firstTrails = MotionTrails(GameObject.Find("Clone_1"));
            Assert.That(firstTrails.Length, Is.InRange(1, 4));
            AssertTint(firstTrails, Color.cyan);

            yield return new WaitForSeconds(0.3f);
            yield return Press(Key.A);
            yield return new WaitForSeconds(0.2f);
            yield return Press(Key.R);
            yield return WaitFor(() => ActorHasTrail("Clone_2"));
            MeshRenderer[] secondTrails = MotionTrails(GameObject.Find("Clone_2"));
            Assert.That(secondTrails.Length, Is.EqualTo(firstTrails.Length));
            AssertTint(secondTrails, Color.magenta);
            yield return new WaitForSeconds(0.6f);
            Assert.That(secondTrails.Any(IsVisible), Is.False);
            Assert.That(MotionTrails(GameObject.Find("Clone_1")).Any(IsVisible), Is.False);
        }

        private IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        private static MeshRenderer[] MotionTrails(GameObject actor)
        {
            Assert.That(actor, Is.Not.Null);
            Assert.That(actor.GetComponent("PlayerMovementTrailController"), Is.Not.Null);
            return actor.GetComponentsInChildren<MeshRenderer>(true)
                .Where(trail => trail.sharedMaterial != null
                    && trail.sharedMaterial.shader.name == "TimeClone/MovementTrail")
                .ToArray();
        }

        private static bool ActorHasTrail(string actorName)
        {
            GameObject actor = GameObject.Find(actorName);
            return actor != null && MotionTrails(actor).Any(IsVisible);
        }

        private static bool IsVisible(MeshRenderer trail)
        {
            return trail.enabled && trail.GetComponent<MeshFilter>().sharedMesh.vertexCount > 1;
        }

        private static void AssertTint(MeshRenderer[] trails, Color expected)
        {
            Assert.That(trails.Any(trail =>
            {
                Color color = trail.GetComponent<MeshFilter>().sharedMesh.colors.LastOrDefault();
                return Mathf.Abs(color.r - expected.r) < 0.01f
                    && Mathf.Abs(color.g - expected.g) < 0.01f
                    && Mathf.Abs(color.b - expected.b) < 0.01f;
            }), Is.True, "At least the aura must use the clone's body color.");
        }

        private static IEnumerator WaitFor(Func<bool> condition)
        {
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(condition(), Is.True, "Expected a visible ribbon from actual movement.");
        }
    }
}

using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TemporalEcho.Tests
{
    public class LevelTenTests : CampaignInputTestFixture
    {
        private GameObject player;
        private GameObject box;

        [UnityTest]
        public IEnumerator KeyboardTwoClonesPassTheSameBoxAroundTheCorner()
        {
            yield return Load(false);
            yield return Solve();
        }

        [UnityTest]
        public IEnumerator TouchTwoClonesPassTheSameBoxAroundTheCorner()
        {
            yield return Load(true);
            yield return Solve();
        }

        [UnityTest]
        public IEnumerator WorkstationsAreOneWayAndAnEmptyCrossingDropsThePlayer()
        {
            yield return Load(false);
            yield return Move(Key.W, 2);
            AssertCell(box, 0, 0);
            yield return Move(Key.W);
            yield return Move(Key.A);
            yield return Move(Key.D);
            AssertCell(player, 0, -1);
            AssertCell(box, 0, 0);
            yield return Move(Key.S, 2);
            AssertCell(player, 0, -2);
            Assert.That(player.transform.position.y, Is.LessThan(0.7f),
                "The first operator cannot climb back to the starting balcony.");

            yield return Reload();
            yield return Move(Key.D);
            yield return Move(Key.W, 3);
            AssertCell(player, 1, 0);
            Assert.That(player.transform.position.y, Is.LessThan(0.7f));
            yield return Move(Key.W);
            yield return Move(Key.S);
            yield return Move(Key.D);
            AssertCell(player, 1, 0);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Level_10"));
        }

        [UnityTest]
        public IEnumerator MenuLoadsTheNewLevelWithoutDoorsPlatesOrRamps()
        {
            yield return SelectFromMenu(10);
            Assert.That(GameObject.FindWithTag("Player").transform.position.y, Is.GreaterThan(1.5f));
            Assert.That(Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Any(b => b != null && new[] { "PressurePlate", "SlidingDoor", "RampTile" }
                    .Contains(b.GetType().Name)), Is.False);
        }

        private IEnumerator Load(bool useTouch)
        {
            yield return Reload();
            CreateInputDevice(useTouch);
        }

        private IEnumerator Reload()
        {
            yield return SceneManager.LoadSceneAsync("Level_10");
            yield return null;
            player = GameObject.FindWithTag("Player");
            box = GameObject.Find("SharedBox");
            AssertCell(player, 0, -3);
            Assert.That(player.transform.position.y, Is.GreaterThan(1.5f));
            AssertCell(box, 0, -1);
        }

        private IEnumerator Solve()
        {
            int sharedBoxId = box.GetInstanceID();
            yield return Move(Key.W, 2);
            AssertCell(player, 0, -1);
            Assert.That(player.transform.position.y, Is.LessThan(0.7f));
            AssertCell(box, 0, 0);
            yield return CloneTurn();

            yield return Move(Key.A, 2);
            yield return Move(Key.W, 3);
            AssertCell(player, -2, 0);
            Assert.That(player.transform.position.y, Is.LessThan(0.7f));
            yield return Move(Key.D, 2);
            AssertCell(box, 1, 0);
            Assert.That(box.GetInstanceID(), Is.EqualTo(sharedBoxId));
            yield return Move(Key.D);
            AssertCell(box, 1, 0);
            AssertCell(player, 0, 0);
            yield return CloneTurn();

            yield return Move(Key.D);
            yield return Move(Key.W, 2);
            AssertCell(player, 1, -1);
            Assert.That(player.transform.position.y, Is.GreaterThan(1.5f));
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!At(box, 1, 0) && Time.realtimeSinceStartup < deadline) yield return null;
            AssertCell(box, 1, 0);
            Assert.That(box.GetInstanceID(), Is.EqualTo(sharedBoxId));
            AssertCell(GameObject.Find("Clone_1"), 0, -1);
            AssertCell(GameObject.Find("Clone_2"), 0, 0);
            yield return Move(Key.W);
            AssertCell(player, 1, 0);
            Assert.That(player.transform.position.y, Is.GreaterThan(1.4f),
                "The upper player must cross the box placed jointly by both echoes.");
            yield return Move(Key.W, 2);
            deadline = Time.realtimeSinceStartup + 3f;
            while (SceneManager.GetActiveScene().name != "Level_11" &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Level_11"));
        }

        private IEnumerator CloneTurn()
        {
            yield return StartCloneTurn();
            AssertCell(player, 0, -3);
            Assert.That(player.transform.position.y, Is.GreaterThan(1.5f));
        }
    }
}

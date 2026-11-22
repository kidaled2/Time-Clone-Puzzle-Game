using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TemporalEcho.Tests
{
    public class LevelTwelveTests : CampaignInputTestFixture
    {
        private GameObject player;
        private GameObject cornerBox;
        private GameObject corridorBox;
        private Collider signalGate;
        private Collider exitGate;

        [UnityTest]
        public IEnumerator MenuSelectionLoadsLevelTwelve()
        {
            yield return SelectFromMenu(12);
            AssertCell(GameObject.FindWithTag("Player"), -2, -2);
        }

        [UnityTest]
        public IEnumerator SignalGateBlocksPassageAndClosesWhenThePlayerLeavesItsPlate()
        {
            yield return Load(false);
            yield return Move(Key.W);
            yield return Move(Key.A);
            yield return Move(Key.W);
            AssertCell(player, -3, -1);

            yield return Move(Key.S, 2);
            AssertCell(player, -3, -3);
            Assert.That(signalGate.enabled, Is.False, "The first plate opens the signal gate.");
            Assert.That(exitGate.enabled, Is.True, "The first plate leaves the exit gate closed.");

            yield return Move(Key.W);
            AssertCell(player, -3, -2);
            Assert.That(signalGate.enabled, Is.True, "Leaving the first plate closes the signal gate.");
        }

        [UnityTest]
        public IEnumerator KeyboardTwoClonesHoldTheGatesAndClearTheBoxes()
        {
            yield return Solve(false);
        }

        [UnityTest]
        public IEnumerator TouchTwoClonesHoldTheGatesAndClearTheBoxes()
        {
            yield return Solve(true);
        }

        private IEnumerator Load(bool useTouch)
        {
            yield return SceneManager.LoadSceneAsync("Level_12");
            yield return null;
            CreateInputDevice(useTouch);
            player = GameObject.FindWithTag("Player");
            cornerBox = GameObject.Find("CornerBox");
            corridorBox = GameObject.Find("CorridorBox");
            signalGate = GameObject.Find("SignalGate").GetComponent<Collider>();
            exitGate = GameObject.Find("ExitGate").GetComponent<Collider>();

            AssertCell(player, -2, -2);
            AssertCell(cornerBox, -2, 1);
            AssertCell(corridorBox, 1, 1);
            Assert.That(signalGate.enabled, Is.True);
            Assert.That(exitGate.enabled, Is.True);
        }

        private IEnumerator Solve(bool useTouch)
        {
            yield return Load(useTouch);
            yield return Move(Key.A);
            yield return Move(Key.S);
            AssertCell(player, -3, -3);
            yield return StartCloneTurn();
            AssertCell(player, -2, -2);

            var firstClone = GameObject.Find("Clone_1");
            float deadline = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < deadline && (signalGate.enabled || !At(firstClone, -3, -3)))
                yield return null;
            AssertCell(firstClone, -3, -3);
            Assert.That(signalGate.enabled, Is.False, "The first clone keeps the signal gate open.");

            yield return EnterBoxCorridor();
            yield return Move(Key.D, 2);
            yield return Move(Key.S);
            yield return Move(Key.D);
            yield return Move(Key.W);
            yield return Move(Key.D);
            AssertCell(player, 1, 1);
            AssertCell(cornerBox, 0, 2);
            AssertCell(corridorBox, 2, 1);
            Assert.That(exitGate.enabled, Is.True, "Moving both boxes leaves the exit gate closed.");

            yield return Move(Key.W, 2);
            AssertCell(player, 1, 3);
            Assert.That(exitGate.enabled, Is.False, "The second plate opens the exit gate.");
            yield return StartCloneTurn();
            AssertCell(player, -2, -2);

            firstClone = GameObject.Find("Clone_1");
            var secondClone = GameObject.Find("Clone_2");
            deadline = Time.realtimeSinceStartup + 6f;
            while (Time.realtimeSinceStartup < deadline &&
                   (exitGate.enabled || !At(secondClone, 1, 3) || !At(cornerBox, 0, 2) || !At(corridorBox, 2, 1)))
                yield return null;
            AssertCell(firstClone, -3, -3);
            AssertCell(secondClone, 1, 3);
            AssertCell(cornerBox, 0, 2);
            AssertCell(corridorBox, 2, 1);
            Assert.That(signalGate.enabled, Is.False);
            Assert.That(exitGate.enabled, Is.False, "The second clone keeps the exit gate open.");

            yield return EnterBoxCorridor();
            yield return Move(Key.D, 4);
            yield return Move(Key.W);
            AssertCell(player, 1, 2);
            yield return Move(Key.D, 2);
            deadline = Time.realtimeSinceStartup + 4f;
            while (SceneManager.GetActiveScene().name == "Level_12" && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        }

        private IEnumerator EnterBoxCorridor()
        {
            yield return Move(Key.W);
            yield return Move(Key.A);
            yield return Move(Key.W, 2);
            AssertCell(player, -3, 1);
        }
    }
}

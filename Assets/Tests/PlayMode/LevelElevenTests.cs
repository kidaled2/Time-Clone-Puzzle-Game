using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TemporalEcho.Tests
{
    public class LevelElevenTests : CampaignInputTestFixture
    {
        private const float PadHold = 5f;
        private Component resonanceLock;
        private PropertyInfo completedChords;
        private GameObject player;
        private Collider gate;

        [UnityTest]
        public IEnumerator MenuSelectionLoadsLevelEleven()
        {
            yield return SelectFromMenu(11);
            AssertCell(GameObject.FindWithTag("Player"), 0, 0);
        }

        [UnityTest]
        public IEnumerator KeyboardReplaysThreeChordsAndUnlocksTheExit()
        {
            yield return Solve(false);
        }

        [UnityTest]
        public IEnumerator TouchReplaysThreeChordsAndUnlocksTheExit()
        {
            yield return Solve(true);
        }

        private IEnumerator Solve(bool useTouch)
        {
            yield return SceneManager.LoadSceneAsync("Level_11");
            yield return null;
            CreateInputDevice(useTouch);
            player = GameObject.FindWithTag("Player");
            resonanceLock = GameObject.Find("ResonanceLock").GetComponent("ResonanceLock");
            completedChords = resonanceLock.GetType().GetProperty("CompletedChords");
            Assert.That(completedChords, Is.Not.Null);
            gate = GameObject.Find("ResonanceGate").GetComponent<Collider>();
            AssertProgress(0);
            yield return Move(Key.S);
            AssertCell(player, 0, 0);

            // Record the west route, leaving time for the other voices to reach each chord.
            yield return Move(Key.A, 2);
            AssertCell(player, -2, 0);
            yield return new WaitForSeconds(PadHold);
            AssertProgress(0);
            yield return Move(Key.A);
            yield return Move(Key.W, 2);
            yield return Move(Key.D);
            AssertCell(player, -2, 2);
            yield return new WaitForSeconds(PadHold);
            yield return Move(Key.A);
            yield return Move(Key.S);
            AssertCell(player, -3, 1);
            yield return StartCloneTurn();
            AssertCell(player, 0, 0);

            // Record the east route while the first clone plays the west route.
            yield return Move(Key.D, 3);
            yield return Move(Key.S);
            AssertCell(player, 3, -1);
            yield return new WaitForSeconds(PadHold);
            AssertProgress(0);
            yield return Move(Key.W);
            yield return Move(Key.A);
            AssertCell(player, 2, 0);
            yield return new WaitForSeconds(PadHold);
            yield return Move(Key.D);
            yield return Move(Key.S, 2);
            yield return Move(Key.A);
            AssertCell(player, 2, -2);
            AssertProgress(0);
            yield return StartCloneTurn();
            AssertCell(player, 0, 0);

            // Play the north route, checking both moving echoes at each chord.
            yield return Move(Key.W, 3);
            yield return Move(Key.D, 2);
            yield return Move(Key.S);
            AssertCell(player, 2, 2);
            yield return ExpectChord(1, -2, 0, 3, -1);
            yield return Move(Key.W);
            yield return Move(Key.A);
            AssertCell(player, 1, 3);
            yield return ExpectChord(2, -2, 2, 2, 0);
            yield return Move(Key.A);
            yield return Move(Key.S);
            AssertCell(player, 0, 2);
            yield return ExpectChord(3, -3, 1, 2, -2);

            yield return Move(Key.S, 4);
            float deadline = Time.realtimeSinceStartup + 4f;
            while (SceneManager.GetActiveScene().name == "Level_11" && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        }

        private IEnumerator ExpectChord(int chord, int westX, int westZ, int eastX, int eastZ)
        {
            var west = GameObject.Find("Clone_1");
            var east = GameObject.Find("Clone_2");
            float deadline = Time.time + PadHold + 4f;
            while (Time.time < deadline &&
                   (Progress < chord || !At(west, westX, westZ) || !At(east, eastX, eastZ)))
                yield return null;
            AssertProgress(chord);
            AssertCell(west, westX, westZ);
            AssertCell(east, eastX, eastZ);
        }

        private int Progress => (int)completedChords.GetValue(resonanceLock);

        private void AssertProgress(int expected)
        {
            Assert.That(Progress, Is.EqualTo(expected));
            Assert.That(gate.enabled, Is.EqualTo(expected < 3), "Only the third chord should unlock the exit.");
        }
    }
}

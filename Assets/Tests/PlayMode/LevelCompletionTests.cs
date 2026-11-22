using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TemporalEcho.Tests
{
    public class LevelCompletionTests
    {
        private const float StepInterval = 0.22f;
        private GameInputScope inputScope;
        private Keyboard keyboard;

        [SetUp]
        public void RouteInputToGame()
        {
            inputScope = new GameInputScope();
        }

        [TearDown]
        public void RemoveKeyboard()
        {
            if (keyboard != null)
            {
                InputSystem.RemoveDevice(keyboard);
            }

            inputScope.Dispose();
        }

        [UnityTest]
        public IEnumerator LevelOneHoldTheExitPlate()
        {
            yield return LoadLevel(1);
            yield return Move(Key.W, 3);
            yield return CloneTurn();
            yield return Move(Key.W, 5);
            yield return new WaitForSeconds(0.6f);
            yield return Move(Key.W, 2);
            yield return ExpectNextScene(2);
        }

        [UnityTest]
        public IEnumerator LevelTwoOpenTwoDoorsInOrder()
        {
            yield return LoadLevel(2);
            yield return Move(Key.D, 2);
            yield return Move(Key.W, 2);
            yield return Move(Key.D);
            yield return new WaitForSeconds(3f);
            yield return Move(Key.A, 2);
            yield return CloneTurn();

            yield return Move(Key.W, 3);
            yield return new WaitForSeconds(0.7f);
            yield return Move(Key.W, 2);
            yield return new WaitForSeconds(3.2f);
            yield return Move(Key.W, 2);
            yield return ExpectNextScene(3);
        }

        [UnityTest]
        public IEnumerator LevelThreeUseTwoClones()
        {
            yield return LoadLevel(3);
            yield return Move(Key.W, 3);
            yield return Move(Key.A, 3);
            yield return CloneTurn();

            yield return Move(Key.W, 2);
            yield return Move(Key.D, 3);
            yield return new WaitForSeconds(0.9f);
            yield return Move(Key.S, 2);
            yield return CloneTurn();

            yield return Move(Key.W, 5);
            yield return new WaitForSeconds(2f);
            yield return Move(Key.W, 2);
            yield return ExpectNextScene(4);
        }

        [UnityTest]
        public IEnumerator LevelFourTimeThreeElevatedDoors()
        {
            yield return LoadLevel(4);
            yield return Move(Key.D, 2);
            yield return Move(Key.W, 2);
            yield return new WaitForSeconds(3f);
            yield return Move(Key.W, 2);
            yield return new WaitForSeconds(2f);
            yield return Move(Key.A, 2);
            yield return CloneTurn();

            yield return Move(Key.A, 3);
            yield return Move(Key.W, 4);
            yield return new WaitForSeconds(3.15f);
            yield return Move(Key.W, 2);
            yield return new WaitForSeconds(2f);
            yield return Move(Key.D, 3);
            yield return ExpectNextScene(5);
        }

        [UnityTest]
        public IEnumerator LevelFiveUseTheBoxAndHoldThePlate()
        {
            yield return LoadLevel(5);
            yield return Move(Key.A, 5);
            yield return Move(Key.W, 2);
            yield return Move(Key.D, 4);
            yield return Move(Key.A, 4);
            yield return Move(Key.W, 4);
            yield return CloneTurn();

            yield return Move(Key.W);
            yield return new WaitForSeconds(3.3f);
            yield return Move(Key.W, 2);
            yield return new WaitForSeconds(1.3f);
            yield return Move(Key.W, 3);
            yield return Move(Key.A, 3);
            yield return ExpectNextScene(6);
        }

        [UnityTest]
        public IEnumerator LevelSixCrossTwoGapsWithOneBox()
        {
            yield return LoadLevel(6);
            yield return Move(Key.D, 3);
            yield return Move(Key.W);
            yield return Move(Key.A, 2);
            yield return Move(Key.S);
            yield return Move(Key.A);
            yield return Move(Key.W, 3);
            yield return new WaitForSeconds(4f);
            yield return Move(Key.W, 2);
            yield return CloneTurn();

            yield return Move(Key.A, 3);
            yield return Move(Key.W, 4);
            yield return new WaitForSeconds(1.2f);
            yield return Move(Key.D, 6);
            yield return Move(Key.W, 2);
            yield return Move(Key.A, 2);
            yield return new WaitForSeconds(2.5f);
            yield return Move(Key.A, 4);
            yield return ExpectNextScene(7);
        }

        private IEnumerator LoadLevel(int buildIndex)
        {
            yield return SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
            yield return null;
            keyboard = InputSystem.AddDevice<Keyboard>();
            Assert.That(SceneManager.GetActiveScene().buildIndex, Is.EqualTo(buildIndex));
        }

        private IEnumerator Move(Key key, int steps = 1)
        {
            for (int step = 0; step < steps; step++)
            {
                yield return Press(key);
            }
        }

        private IEnumerator CloneTurn()
        {
            yield return Press(Key.R);
            yield return new WaitForSeconds(0.4f);
        }

        private IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(StepInterval);
        }

        private static IEnumerator ExpectNextScene(int buildIndex)
        {
            float deadline = Time.realtimeSinceStartup + 3f;
            while (SceneManager.GetActiveScene().buildIndex != buildIndex && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Scene active = SceneManager.GetActiveScene();
            GameObject player = GameObject.FindWithTag("Player");
            Assert.That(active.buildIndex, Is.EqualTo(buildIndex),
                $"Expected completion from {active.name}. Player position: {player?.transform.position}.");
        }
    }
}

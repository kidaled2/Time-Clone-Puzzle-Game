using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace TemporalEcho.Tests
{
    public class CloneTurnTests
    {
        private GameInputScope inputScope;
        private Keyboard keyboard;
        private Touchscreen touchscreen;
        private Button cloneButton;
        private GameObject player;

        [SetUp]
        public void RouteInputToGame()
        {
            inputScope = new GameInputScope();
        }

        [TearDown]
        public void RestoreInputAndTime()
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
        public IEnumerator KeyboardSpendsOneCloneOnlyAfterMovementAndWhileUnpaused()
        {
            yield return LoadLevel("Level_1");
            keyboard = InputSystem.AddDevice<Keyboard>();

            Assert.That(cloneButton.interactable, Is.False);
            cloneButton.onClick.Invoke();
            yield return PressKey(Key.R);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(GameObject.Find("Clone_1"), Is.Null);

            yield return PressKey(Key.W);
            yield return WaitForCell("Player", new Vector2(0f, -2f));
            yield return WaitFor(() => cloneButton.interactable, "The first move should enable CLONE.");

            ButtonNamed("PauseButton").onClick.Invoke();
            yield return PressKey(Key.R);
            Assert.That(cloneButton.interactable, Is.False);
            ButtonNamed("ResumeButton").onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(GameObject.Find("Clone_1"), Is.Null);
            Assert.That(cloneButton.interactable, Is.True);

            yield return PressKey(Key.R);
            yield return PressKey(Key.R);
            Assert.That(cloneButton.interactable, Is.False);
            yield return WaitForCell("Clone_1", new Vector2(0f, -2f));
            Assert.That(GameObject.Find("Clone_2"), Is.Null);
            Assert.That(Cell(player), Is.EqualTo(new Vector2(0f, -3f)));

            yield return PressKey(Key.W);
            yield return WaitForCell("Player", new Vector2(0f, -2f));
            yield return PressKey(Key.R);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(cloneButton.interactable, Is.False);
            Assert.That(Cell(player), Is.EqualTo(new Vector2(0f, -2f)));
            Assert.That(GameObject.Find("Clone_2"), Is.Null);
        }

        [UnityTest]
        public IEnumerator TouchSwipeEnablesCloneAndTouchTapReplaysFirstMove()
        {
            yield return LoadLevel("Level_1");
            touchscreen = InputSystem.AddDevice<Touchscreen>();

            Canvas.ForceUpdateCanvases();
            var buttonRect = (RectTransform)cloneButton.transform;
            Vector2 buttonCenter = RectTransformUtility.WorldToScreenPoint(
                null, buttonRect.TransformPoint(buttonRect.rect.center));

            yield return Touch(1, buttonCenter, buttonCenter);
            Assert.That(cloneButton.interactable, Is.False);
            Assert.That(GameObject.Find("Clone_1"), Is.Null);

            Vector2 swipeStart = new Vector2(Screen.width * 0.4f, Screen.height * 0.3f);
            yield return Touch(2, swipeStart, swipeStart + Vector2.up * 80f);
            yield return WaitFor(() => cloneButton.interactable, "A valid swipe should enable CLONE.");
            yield return WaitForCell("Player", new Vector2(0f, -2f));

            yield return Touch(3, buttonCenter, buttonCenter);
            yield return WaitForCell("Clone_1", new Vector2(0f, -2f));
            Assert.That(cloneButton.interactable, Is.False);
            Assert.That(Cell(player), Is.EqualTo(new Vector2(0f, -3f)));
        }

        [UnityTest]
        public IEnumerator LevelThreeRecordsTwoTurnsAndKeepsPlayerControlOnLastTurn()
        {
            yield return LoadLevel("Level_3");
            keyboard = InputSystem.AddDevice<Keyboard>();

            yield return PressKey(Key.W);
            yield return WaitForCell("Player", new Vector2(0f, -2f));
            yield return WaitFor(() => cloneButton.interactable, "The first turn should allow creating a clone.");
            cloneButton.onClick.Invoke();
            cloneButton.onClick.Invoke();
            yield return WaitForCell("Clone_1", new Vector2(0f, -2f));
            Assert.That(GameObject.Find("Clone_2"), Is.Null);
            Assert.That(cloneButton.interactable, Is.False);

            yield return PressKey(Key.A);
            yield return WaitForCell("Player", new Vector2(-1f, -3f));
            yield return WaitFor(() => cloneButton.interactable, "The second turn should allow creating a clone.");
            yield return PressKey(Key.R);
            yield return WaitForCell("Clone_2", new Vector2(-1f, -3f));
            Assert.That(GameObject.Find("Clone_1"), Is.Not.Null);

            yield return PressKey(Key.D);
            yield return WaitForCell("Player", new Vector2(1f, -3f));
            Assert.That(cloneButton.interactable, Is.False);
            cloneButton.onClick.Invoke();
            yield return PressKey(Key.R);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(GameObject.Find("Clone_3"), Is.Null);
            Assert.That(Cell(player), Is.EqualTo(new Vector2(1f, -3f)));
        }

        private IEnumerator LoadLevel(string sceneName)
        {
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
            cloneButton = ButtonNamed("CloneButton");
            player = GameObject.FindWithTag("Player");
            Assert.That(player, Is.Not.Null);
        }

        private IEnumerator PressKey(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        private IEnumerator Touch(int id, Vector2 start, Vector2 end)
        {
            InputSystem.QueueStateEvent(touchscreen, new TouchState
            {
                touchId = id,
                phase = TouchPhase.Began,
                position = start,
                pressure = 1f
            });
            yield return null;

            InputSystem.QueueStateEvent(touchscreen, new TouchState
            {
                touchId = id,
                phase = TouchPhase.Ended,
                position = end,
                delta = end - start
            });
            yield return null;
        }

        private static Button ButtonNamed(string name)
        {
            GameObject buttonObject = GameObject.Find(name);
            Assert.That(buttonObject, Is.Not.Null, $"Expected the scene button {name}.");
            Button button = buttonObject.GetComponent<Button>();
            Assert.That(button, Is.Not.Null);
            return button;
        }

        private static IEnumerator WaitForCell(string name, Vector2 expectedCell)
        {
            yield return WaitFor(() =>
            {
                GameObject actor = GameObject.Find(name);
                return actor != null && Cell(actor) == expectedCell;
            }, $"Expected {name} to reach {expectedCell}.");
        }

        private static Vector2 Cell(GameObject actor)
        {
            Vector3 position = actor.transform.position;
            return new Vector2(position.x, position.z);
        }

        private static IEnumerator WaitFor(Func<bool> condition, string failureMessage)
        {
            float timeout = Time.realtimeSinceStartup + 5f;
            while (!condition() && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(condition(), Is.True, failureMessage);
        }
    }
}

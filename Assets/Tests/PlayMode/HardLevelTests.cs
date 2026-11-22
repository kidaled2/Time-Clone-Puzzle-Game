using System.Collections;
using System.Linq;
using System.Reflection;
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
    public class HardLevelTests
    {
        private GameInputScope inputScope;
        private Keyboard keyboard;
        private Touchscreen touchscreen;
        private FieldInfo levelSelectMode;
        private bool originalLevelSelectMode;
        private bool touch;
        private int touchId;
        private GameObject player;
        private Button cloneButton;

        [SetUp]
        public void SetUp()
        {
            inputScope = new GameInputScope();
            var menuType = System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("TimeClone.UI.MainMenuManager")).First(t => t != null);
            levelSelectMode = menuType.GetField("enteredViaLevelSelect");
            originalLevelSelectMode = (bool)levelSelectMode.GetValue(null);
            levelSelectMode.SetValue(null, false);
        }

        [TearDown]
        public void TearDown()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (touchscreen != null) InputSystem.RemoveDevice(touchscreen);
            if (levelSelectMode != null) levelSelectMode.SetValue(null, originalLevelSelectMode);
            inputScope?.Dispose();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator LevelEightKeyboardTransfersTheFirstClonesDuty()
        {
            yield return Load(8, false);
            yield return SolveHandoff();
        }

        [UnityTest]
        public IEnumerator LevelEightTouchTransfersTheFirstClonesDuty()
        {
            yield return Load(8, true);
            yield return SolveHandoff();
        }

        [UnityTest]
        public IEnumerator LevelNineKeyboardReusesOneBoxAndDelaysTheSecondClone()
        {
            yield return Load(9, false);
            yield return SolveDoubleBridge();
        }

        [UnityTest]
        public IEnumerator LevelNineTouchReusesOneBoxAndDelaysTheSecondClone()
        {
            yield return Load(9, true);
            yield return SolveDoubleBridge();
        }

        [UnityTest]
        public IEnumerator MenuLoadsBothHardLevels()
        {
            foreach (int level in new[] { 8, 9 })
            {
                yield return SceneManager.LoadSceneAsync("MainMenu");
                yield return null;
                var menu = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                    .First(b => b.GetType().Name == "MainMenuManager");
                menu.SendMessage("OnLevelSelectPressed");
                yield return null;
                var buttons = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
                    .Where(b => b != null && b.gameObject.scene.name == "MainMenu" &&
                                b.GetType().Name == "LevelSelectButton").ToArray();
                Assert.That(buttons.Length, Is.EqualTo(10));
                var button = GameObject.Find("LevelButton_" + level).GetComponent<Button>();
                Assert.That(button.interactable, Is.True);
                button.onClick.Invoke();
                yield return null;
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Level_" + level));
                Assert.That((bool)levelSelectMode.GetValue(null), Is.True);
            }
        }

        private IEnumerator Load(int level, bool useTouch)
        {
            touch = useTouch;
            yield return SceneManager.LoadSceneAsync("Level_" + level);
            yield return null;
            if (touch) touchscreen = InputSystem.AddDevice<Touchscreen>();
            else keyboard = InputSystem.AddDevice<Keyboard>();
            player = GameObject.FindWithTag("Player");
            cloneButton = GameObject.Find("CloneButton").GetComponent<Button>();
        }

        private IEnumerator SolveHandoff()
        {
            yield return Move(Key.A, 2);
            yield return Move(Key.W, 2);
            Assert.That(Plate(8, 1), Is.True);
            yield return new WaitForSeconds(4f);
            yield return Move(Key.D);
            Assert.That(Plate(8, 1), Is.False);
            Assert.That(Plate(8, 3), Is.True);
            Assert.That(GameObject.Find("Door8_1").GetComponent<BoxCollider>().enabled, Is.True);
            yield return CloneTurn();

            yield return Move(Key.W, 2);
            yield return Move(Key.D, 3);
            yield return Move(Key.S, 2);
            AssertCell(player, 3, -3);
            Assert.That(Plate(8, 2), Is.True);
            yield return CloneTurn();

            yield return Move(Key.A, 3);
            yield return Move(Key.W, 4);
            yield return Move(Key.D, 4);
            AssertCell(player, 1, 1);
            Assert.That(player.transform.position.y, Is.GreaterThan(1.4f));
            yield return Move(Key.D, 2);
            yield return WaitFor(() => Plate(8, 3) && !Plate(8, 1), 8f,
                "Clone 1 must finish transferring from plate 1 to plate 3.");
            Assert.That(Plate(8, 1), Is.False);
            Assert.That(Plate(8, 2), Is.True);
            yield return Move(Key.W, 2);
            yield return ExpectScene("Level_9");
        }

        private IEnumerator SolveDoubleBridge()
        {
            yield return Move(Key.W, 3);
            AssertCell(GameObject.Find("Box"), 0, 1);
            yield return new WaitForSeconds(7f);
            yield return Move(Key.W, 2);
            AssertCell(GameObject.Find("Box"), 0, 3);
            yield return Move(Key.S, 2);
            yield return Move(Key.A, 2);
            yield return Move(Key.S);
            Assert.That(Plate(9, 1), Is.True);
            yield return CloneTurn();

            yield return Move(Key.W);
            yield return new WaitForSeconds(11f);
            yield return Move(Key.W);
            yield return Move(Key.D, 3);
            yield return Move(Key.S, 2);
            AssertCell(player, 3, -3);
            Assert.That(Plate(9, 2), Is.True);
            yield return CloneTurn();

            yield return Move(Key.A, 3);
            yield return Move(Key.W, 4);
            yield return Move(Key.D, 3);
            AssertCell(player, 0, 1);
            AssertCell(GameObject.Find("Box"), 0, 1);
            Assert.That(player.transform.position.y, Is.GreaterThan(1.4f),
                "The first crossing must happen while the box still spans the first gap.");
            yield return Move(Key.D, 3);
            yield return Move(Key.W, 2);
            yield return Move(Key.A, 2);
            AssertCell(player, 1, 3);
            yield return WaitFor(() => At(GameObject.Find("Box"), 0, 3), 8f,
                "The same recorded box must reach the second gap.");
            yield return Move(Key.A);
            AssertCell(player, 0, 3);
            Assert.That(player.transform.position.y, Is.GreaterThan(1.4f));
            yield return Move(Key.A, 2);
            AssertCell(player, -1, 3);
            Assert.That(Plate(9, 2), Is.False,
                "The delayed second clone should not have opened the final gate yet.");
            yield return WaitFor(() => Plate(9, 2), 10f, "The delayed clone must enter the service pocket.");
            Assert.That(Plate(9, 1), Is.True);
            yield return Move(Key.A, 2);
            yield return ExpectScene("Level_10");
        }

        private IEnumerator CloneTurn()
        {
            Assert.That(cloneButton.interactable, Is.True);
            if (touch)
            {
                Canvas.ForceUpdateCanvases();
                var rect = (RectTransform)cloneButton.transform;
                Vector2 center = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
                yield return Touch(center, center);
            }
            else yield return PressKey(Key.R);
            yield return new WaitForSeconds(0.45f);
            AssertCell(player, 0, -3);
        }

        private IEnumerator Move(Key key, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                if (touch)
                {
                    Vector2 dir = key == Key.W ? Vector2.up : key == Key.S ? Vector2.down :
                        key == Key.A ? Vector2.left : Vector2.right;
                    Vector2 start = new Vector2(Screen.width * 0.4f, Screen.height * 0.3f);
                    yield return Touch(start, start + dir * 100f);
                }
                else yield return PressKey(key);
                yield return new WaitForSeconds(0.24f);
            }
        }

        private IEnumerator PressKey(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        private IEnumerator Touch(Vector2 start, Vector2 end)
        {
            int id = ++touchId;
            InputSystem.QueueStateEvent(touchscreen, new TouchState
            {
                touchId = id, phase = TouchPhase.Began, position = start, pressure = 1f
            });
            yield return null;
            InputSystem.QueueStateEvent(touchscreen, new TouchState
            {
                touchId = id, phase = TouchPhase.Ended, position = end, delta = end - start
            });
            yield return null;
        }

        private static IEnumerator WaitFor(System.Func<bool> condition, float timeout, string message)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, message);
        }

        private static IEnumerator ExpectScene(string scene)
        {
            yield return WaitFor(() => SceneManager.GetActiveScene().name == scene, 3f,
                "Expected completion into " + scene);
        }

        private static bool Plate(int level, int index)
        {
            var component = GameObject.Find("PressurePlate" + level + "_" + index).GetComponent("PressurePlate");
            return (bool)component.GetType().GetProperty("IsActivated").GetValue(component);
        }

        private static bool At(GameObject actor, int x, int z)
        {
            return actor != null && Vector2.Distance(
                new Vector2(actor.transform.position.x, actor.transform.position.z), new Vector2(x, z)) < 0.12f;
        }

        private static void AssertCell(GameObject actor, int x, int z)
        {
            Assert.That(actor, Is.Not.Null);
            Assert.That(At(actor, x, z), Is.True, actor.name + " should be at (" + x + "," + z + ").");
        }
    }
}

using System.Collections;
using System.Linq;
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
    public class LevelSevenTests
    {
        private GameInputScope inputScope;
        private Keyboard keyboard;
        private Touchscreen touchscreen;
        private bool useTouch;
        private int touchId;
        private GameObject player;
        private Button cloneButton;
        private System.Reflection.FieldInfo levelSelectMode;
        private bool originalLevelSelectMode;

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
            Time.timeScale = 1f;
            levelSelectMode.SetValue(null, originalLevelSelectMode);
            inputScope.Dispose();
        }

        [UnityTest]
        public IEnumerator MenuSelectionLoadsLevelSeven()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            var menu = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .First(b => b.GetType().Name == "MainMenuManager");
            menu.SendMessage("OnLevelSelectPressed");
            yield return null;
            var button = GameObject.Find("LevelButton_7").GetComponent<Button>();
            Assert.That(button.interactable, Is.True);
            button.onClick.Invoke();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Level_7"));
            Assert.That((bool)levelSelectMode.GetValue(null), Is.True);
            Assert.That(GameObject.FindWithTag("Player"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator KeyboardTwoClonesAndPlayerBuiltBridgeReachLevelEight()
        {
            yield return Load(false);
            yield return PrepareClones();
            yield return BuildBridgeAndExit();
        }

        [UnityTest]
        public IEnumerator TouchSwipesAndCloneButtonCompleteTheSamePuzzle()
        {
            yield return Load(true);
            yield return PrepareClones();
            yield return BuildBridgeAndExit();
        }

        [UnityTest]
        public IEnumerator ServicePocketAndMissingBridgeBlockShortcuts()
        {
            yield return Load(false);
            yield return Move(Key.D, 2);
            AssertCell(player, 1, -3); // The pocket's west wall cannot be bypassed.
            yield return Move(Key.A);
            yield return Move(Key.W, 2);
            yield return Move(Key.D, 3);
            yield return Move(Key.S);
            AssertCell(player, 3, -1); // Service door stays shut without plate A.

            yield return SceneManager.LoadSceneAsync("Level_7");
            yield return null;
            BindScene();
            yield return PrepareClones();
            yield return Move(Key.A, 3);
            yield return Move(Key.W, 4);
            Assert.That(player.transform.position.y, Is.GreaterThan(1.4f));
            yield return Move(Key.D, 5);
            AssertCell(player, 2, 1);
            Assert.That(player.transform.position.y, Is.LessThan(0.7f),
                "An empty bridge gap must drop the player to the lower floor.");
            yield return Move(Key.D);
            AssertCell(player, 2, 1);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Level_7"));
        }

        private IEnumerator Load(bool touch)
        {
            useTouch = touch;
            yield return SceneManager.LoadSceneAsync("Level_7");
            yield return null;
            if (touch) touchscreen = InputSystem.AddDevice<Touchscreen>();
            else keyboard = InputSystem.AddDevice<Keyboard>();
            BindScene();
        }

        private void BindScene()
        {
            player = GameObject.FindWithTag("Player");
            cloneButton = GameObject.Find("CloneButton").GetComponent<Button>();
            Assert.That(player, Is.Not.Null);
            AssertCell(GameObject.Find("Box"), 2, 0);
        }

        private IEnumerator PrepareClones()
        {
            yield return Move(Key.A, 2);
            yield return Move(Key.W, 2);
            AssertCell(player, -2, -1);
            Assert.That(PlateActive("PressurePlate7_1"), Is.True);
            yield return CloneTurn();

            yield return Move(Key.W, 2);
            yield return Move(Key.D, 3);
            yield return Move(Key.S, 2);
            AssertCell(player, 3, -3);
            Assert.That(PlateActive("PressurePlate7_2"), Is.True);
            yield return CloneTurn();
            yield return new WaitForSeconds(1.6f);

            AssertCell(GameObject.Find("Clone_1"), -2, -1);
            AssertCell(GameObject.Find("Clone_2"), 3, -3);
            Assert.That(PlateActive("PressurePlate7_1"), Is.True);
            Assert.That(PlateActive("PressurePlate7_2"), Is.True);
            AssertCell(GameObject.Find("Box"), 2, 0);
        }

        private IEnumerator BuildBridgeAndExit()
        {
            yield return Move(Key.W, 2);
            yield return Move(Key.D, 2);
            yield return Move(Key.W);
            AssertCell(GameObject.Find("Box"), 2, 1);
            yield return Move(Key.S);
            yield return Move(Key.A, 2);
            yield return Move(Key.S, 2);
            yield return Move(Key.A, 3);
            yield return Move(Key.W, 4);
            Assert.That(player.transform.position.y, Is.GreaterThan(1.4f));
            yield return Move(Key.D, 5);
            AssertCell(player, 2, 1);
            Assert.That(player.transform.position.y, Is.GreaterThan(1.4f),
                "The final-turn player's box must support the upper crossing.");
            yield return Move(Key.D);
            yield return Move(Key.W, 2);

            float deadline = Time.realtimeSinceStartup + 3f;
            while (SceneManager.GetActiveScene().name != "Level_8" &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Level_8"));
        }

        private IEnumerator CloneTurn()
        {
            Assert.That(cloneButton.interactable, Is.True);
            if (useTouch)
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
                if (useTouch)
                {
                    Vector2 direction = key == Key.W ? Vector2.up : key == Key.S ? Vector2.down :
                        key == Key.A ? Vector2.left : Vector2.right;
                    Vector2 start = new Vector2(Screen.width * 0.4f, Screen.height * 0.3f);
                    yield return Touch(start, start + direction * 100f);
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

        private static bool PlateActive(string name)
        {
            var plate = GameObject.Find(name).GetComponent("PressurePlate");
            return (bool)plate.GetType().GetProperty("IsActivated").GetValue(plate);
        }

        private static void AssertCell(GameObject actor, int x, int z)
        {
            Assert.That(actor, Is.Not.Null);
            Assert.That(Vector2.Distance(new Vector2(actor.transform.position.x, actor.transform.position.z),
                new Vector2(x, z)), Is.LessThan(0.12f), actor.name + " should occupy the expected cell.");
        }
    }
}

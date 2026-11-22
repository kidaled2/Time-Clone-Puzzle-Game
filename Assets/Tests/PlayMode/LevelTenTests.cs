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
    public class LevelTenTests
    {
        private GameInputScope inputScope;
        private Keyboard keyboard;
        private Touchscreen touchscreen;
        private FieldInfo levelSelectMode;
        private bool originalLevelSelectMode;
        private bool touch;
        private int touchId;
        private GameObject player;
        private GameObject box;

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
            levelSelectMode?.SetValue(null, originalLevelSelectMode);
            inputScope?.Dispose();
            Time.timeScale = 1f;
        }

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
            GameObject.Find("LevelButton_10").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Level_10"));
            Assert.That((bool)levelSelectMode.GetValue(null), Is.True);
            Assert.That(GameObject.FindWithTag("Player").transform.position.y, Is.GreaterThan(1.5f));
            Assert.That(Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Any(b => b != null && new[] { "PressurePlate", "SlidingDoor", "RampTile" }
                    .Contains(b.GetType().Name)), Is.False);
        }

        private IEnumerator Load(bool useTouch)
        {
            touch = useTouch;
            yield return Reload();
            if (touch) touchscreen = InputSystem.AddDevice<Touchscreen>();
            else keyboard = InputSystem.AddDevice<Keyboard>();
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
            while (SceneManager.GetActiveScene().name != "MainMenu" &&
                   Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        }

        private IEnumerator CloneTurn()
        {
            var button = GameObject.Find("CloneButton").GetComponent<Button>();
            if (touch)
            {
                Canvas.ForceUpdateCanvases();
                var rect = (RectTransform)button.transform;
                Vector2 center = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
                yield return Touch(center, center);
            }
            else yield return Press(Key.R);
            yield return new WaitForSeconds(0.45f);
            AssertCell(player, 0, -3);
            Assert.That(player.transform.position.y, Is.GreaterThan(1.5f));
        }

        private IEnumerator Move(Key key, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                if (touch)
                {
                    Vector2 direction = key == Key.W ? Vector2.up : key == Key.S ? Vector2.down :
                        key == Key.A ? Vector2.left : Vector2.right;
                    Vector2 start = new Vector2(Screen.width * 0.4f, Screen.height * 0.3f);
                    yield return Touch(start, start + direction * 100f);
                }
                else yield return Press(key);
                yield return new WaitForSeconds(0.24f);
            }
        }

        private IEnumerator Press(Key key)
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

        private static bool At(GameObject actor, int x, int z)
        {
            return actor != null && Vector2.Distance(new Vector2(actor.transform.position.x,
                actor.transform.position.z), new Vector2(x, z)) < 0.12f;
        }

        private static void AssertCell(GameObject actor, int x, int z)
        {
            Assert.That(actor, Is.Not.Null);
            Assert.That(At(actor, x, z), Is.True, actor.name + " should be at (" + x + "," + z + ").");
        }
    }
}

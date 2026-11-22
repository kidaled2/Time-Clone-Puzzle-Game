using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace TemporalEcho.Tests
{
    public abstract class CampaignInputTestFixture
    {
        private GameInputScope inputScope;
        private Keyboard keyboard;
        private Touchscreen touchscreen;
        private FieldInfo levelSelectMode;
        private bool originalLevelSelectMode;
        private bool touch;
        private int touchId;

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

        protected void CreateInputDevice(bool useTouch)
        {
            touch = useTouch;
            if (touch) touchscreen = InputSystem.AddDevice<Touchscreen>();
            else keyboard = InputSystem.AddDevice<Keyboard>();
        }

        protected IEnumerator SelectFromMenu(int level)
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            var menu = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .First(b => b.GetType().Name == "MainMenuManager");
            menu.SendMessage("OnLevelSelectPressed");
            yield return null;
            var button = GameObject.Find("LevelButton_" + level).GetComponent<Button>();
            Assert.That(button.interactable, Is.True);
            button.onClick.Invoke();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Level_" + level));
            Assert.That((bool)levelSelectMode.GetValue(null), Is.True);
        }

        protected IEnumerator StartCloneTurn()
        {
            var button = GameObject.Find("CloneButton").GetComponent<Button>();
            Assert.That(button.interactable, Is.True);
            if (touch)
            {
                Canvas.ForceUpdateCanvases();
                var rect = (RectTransform)button.transform;
                Vector2 center = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
                yield return Touch(center, center);
            }
            else yield return Press(Key.R);
            yield return new WaitForSeconds(0.45f);
        }

        protected IEnumerator Move(Key key, int count = 1)
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

        protected static bool At(GameObject actor, int x, int z)
        {
            return actor != null && Vector2.Distance(new Vector2(actor.transform.position.x,
                actor.transform.position.z), new Vector2(x, z)) < 0.12f;
        }

        protected static void AssertCell(GameObject actor, int x, int z)
        {
            Assert.That(actor, Is.Not.Null);
            Assert.That(At(actor, x, z), Is.True, actor.name + " should be at (" + x + "," + z + ").");
        }
    }
}

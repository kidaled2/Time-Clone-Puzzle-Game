using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TimeClone.Player
{
    public sealed class PlayerInputHandler : MonoBehaviour
    {
        [Header("Input Sources")]
        [SerializeField] private bool allowKeyboardInput = true;
        [SerializeField] private bool allowTouchInput = true;

        [Header("Touch")]
        [SerializeField, Min(1f)] private float minSwipeDistancePixels = 50f;
        [SerializeField, Min(0f)] private float swipeAxisDeadZonePixels = 18f;

        [Header("State")]
        [SerializeField] private bool inputEnabled = true;

        private Vector2 touchStartPosition;
        private bool isTrackingSwipe;

        public event Action<Vector2> OnMoveInput;

        private void Update()
        {
            if (!inputEnabled)
            {
                return;
            }

            if (allowKeyboardInput && TryGetKeyboardDirection(out Vector2 keyboardDirection))
            {
                EmitMoveInput(keyboardDirection);
                return;
            }

            if (allowTouchInput)
            {
                ProcessTouchInput();
            }
        }

        /// <summary>
        /// Enables or disables move input dispatching.
        /// </summary>
        /// <param name="enabled">True to allow input, false to ignore input.</param>
        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!enabled)
            {
                isTrackingSwipe = false;
            }
        }

        private bool TryGetKeyboardDirection(out Vector2 direction)
        {
#if ENABLE_INPUT_SYSTEM
            if (TryGetInputSystemKeyboardDirection(out direction))
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (TryGetLegacyKeyboardDirection(out direction))
            {
                return true;
            }
#endif

            direction = Vector2.zero;
            return false;
        }

#if ENABLE_INPUT_SYSTEM
        private static bool TryGetInputSystemKeyboardDirection(out Vector2 direction)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                direction = Vector2.zero;
                return false;
            }

            if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
            {
                direction = Vector2.up;
                return true;
            }

            if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            {
                direction = Vector2.down;
                return true;
            }

            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                direction = Vector2.left;
                return true;
            }

            if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                direction = Vector2.right;
                return true;
            }

            direction = Vector2.zero;
            return false;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        private static bool TryGetLegacyKeyboardDirection(out Vector2 direction)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                direction = Vector2.up;
                return true;
            }

            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                direction = Vector2.down;
                return true;
            }

            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                direction = Vector2.left;
                return true;
            }

            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                direction = Vector2.right;
                return true;
            }

            direction = Vector2.zero;
            return false;
        }
#endif

        private void ProcessTouchInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (TryProcessInputSystemTouch())
            {
                return;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            ProcessLegacyTouchInput();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private bool TryProcessInputSystemTouch()
        {
            Touchscreen touchScreen = Touchscreen.current;
            if (touchScreen == null)
            {
                return false;
            }

            UnityEngine.InputSystem.Controls.TouchControl touch = touchScreen.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                touchStartPosition = touch.position.ReadValue();
                isTrackingSwipe = true;
            }

            if (touch.press.wasReleasedThisFrame)
            {
                if (!isTrackingSwipe)
                {
                    return true;
                }

                isTrackingSwipe = false;
                Vector2 delta = touch.position.ReadValue() - touchStartPosition;
                if (TryGetSwipeDirection(delta, out Vector2 swipeDirection))
                {
                    EmitMoveInput(swipeDirection);
                }
            }

            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        private void ProcessLegacyTouchInput()
        {
            if (Input.touchCount == 0)
            {
                return;
            }

            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case UnityEngine.TouchPhase.Began:
                    touchStartPosition = touch.position;
                    isTrackingSwipe = true;
                    break;
                case UnityEngine.TouchPhase.Ended:
                case UnityEngine.TouchPhase.Canceled:
                    if (!isTrackingSwipe)
                    {
                        break;
                    }

                    isTrackingSwipe = false;
                    Vector2 delta = touch.position - touchStartPosition;
                    if (!TryGetSwipeDirection(delta, out Vector2 swipeDirection))
                    {
                        break;
                    }

                    EmitMoveInput(swipeDirection);
                    break;
            }
        }
#endif

        private bool TryGetSwipeDirection(Vector2 swipeDelta, out Vector2 direction)
        {
            float absX = Mathf.Abs(swipeDelta.x);
            float absY = Mathf.Abs(swipeDelta.y);
            float dominantAxisMagnitude = Mathf.Max(absX, absY);
            if (dominantAxisMagnitude < minSwipeDistancePixels)
            {
                direction = Vector2.zero;
                return false;
            }

            float axisDifference = Mathf.Abs(absX - absY);
            if (axisDifference < swipeAxisDeadZonePixels)
            {
                direction = Vector2.zero;
                return false;
            }

            direction = absX > absY
                ? new Vector2(Mathf.Sign(swipeDelta.x), 0f)
                : new Vector2(0f, Mathf.Sign(swipeDelta.y));

            return true;
        }

        private void EmitMoveInput(Vector2 direction)
        {
            if (direction.sqrMagnitude < Mathf.Epsilon)
            {
                return;
            }

            OnMoveInput?.Invoke(direction);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FracturedChorus.Narrative
{
    public static class PrologueInput
    {
        private static readonly List<RaycastResult> RaycastResults = new List<RaycastResult>(16);

        public static bool WasAdvancePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.enterKey.wasPressedThisFrame ||
                    keyboard.numpadEnterKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame)
                {
                    return true;
                }
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !IsPointerOverUi())
            {
                return true;
            }

            var touch = Touchscreen.current;
            if (touch != null &&
                touch.primaryTouch.press.wasPressedThisFrame &&
                !IsPointerOverUi(touch.primaryTouch.touchId.ReadValue()))
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.Space))
            {
                return true;
            }

            if (Input.GetMouseButtonDown(0) && !IsPointerOverUi())
            {
                return true;
            }
#endif

            return false;
        }

        public static bool IsPointerOverUi(int pointerId = -1)
        {
            return IsPointerOverInteractiveUi(pointerId);
        }

        private static bool IsPointerOverInteractiveUi(int pointerId)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null || !TryGetPointerPosition(pointerId, out var position))
            {
                return false;
            }

            var pointerData = new PointerEventData(eventSystem)
            {
                position = position,
                pointerId = pointerId
            };

            RaycastResults.Clear();
            eventSystem.RaycastAll(pointerData, RaycastResults);
            for (var i = 0; i < RaycastResults.Count; i++)
            {
                if (IsInteractiveUiHit(RaycastResults[i].gameObject))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsInteractiveUiHit(GameObject go)
        {
            var current = go != null ? go.transform : null;
            while (current != null)
            {
                var selectable = current.GetComponent<Selectable>();
                if (selectable != null && selectable.isActiveAndEnabled && selectable.IsInteractable())
                {
                    return true;
                }

                var behaviours = current.GetComponents<MonoBehaviour>();
                for (var i = 0; i < behaviours.Length; i++)
                {
                    var behaviour = behaviours[i];
                    if (behaviour == null || !behaviour.isActiveAndEnabled)
                    {
                        continue;
                    }

                    if (behaviour is IPointerClickHandler || behaviour is IPointerDownHandler)
                    {
                        return true;
                    }
                }

                current = current.parent;
            }

            return false;
        }

        private static bool TryGetPointerPosition(int pointerId, out Vector2 position)
        {
#if ENABLE_INPUT_SYSTEM
            if (pointerId >= 0)
            {
                var touch = Touchscreen.current;
                if (touch != null)
                {
                    position = touch.primaryTouch.position.ReadValue();
                    return true;
                }
            }
            else
            {
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    position = mouse.position.ReadValue();
                    return true;
                }
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            position = Input.mousePosition;
            return true;
#else
            position = default;
            return false;
#endif
        }

        public static bool WasUpPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null &&
                (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame))
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                return true;
            }
#endif

            return false;
        }

        public static bool WasKeyboardAdvancePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.enterKey.wasPressedThisFrame ||
                    keyboard.numpadEnterKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame)
                {
                    return true;
                }
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.Space))
            {
                return true;
            }
#endif

            return false;
        }

        public static bool WasCancelPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                return true;
            }
#endif

            return false;
        }

        public static bool WasSkipHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null &&
                (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed))
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                return true;
            }
#endif

            return false;
        }

        public static bool WasDownPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null &&
                (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame))
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                return true;
            }
#endif

            return false;
        }
    }
}

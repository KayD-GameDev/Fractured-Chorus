using UnityEngine;
using UnityEngine.InputSystem;

namespace FracturedChorus.Narrative
{
    public static class PrologueInput
    {
        public static bool WasAdvancePressedThisFrame()
        {
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
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }

            return false;
        }

        public static bool WasUpPressedThisFrame()
        {
            var keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame);
        }

        public static bool WasDownPressedThisFrame()
        {
            var keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame);
        }
    }
}

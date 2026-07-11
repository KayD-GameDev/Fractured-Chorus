using UnityEngine;
using UnityEngine.InputSystem;

namespace FracturedChorus.Hub
{
    public enum TownMapPromptScheme
    {
        Keyboard,
        Gamepad
    }

    public static class TownMapInput
    {
        public static TownMapPromptScheme CurrentScheme { get; private set; } = TownMapPromptScheme.Keyboard;

        public static void RefreshScheme()
        {
            if (Gamepad.current != null && WasGamepadUsedRecently())
            {
                CurrentScheme = TownMapPromptScheme.Gamepad;
                return;
            }

            CurrentScheme = TownMapPromptScheme.Keyboard;
        }

        public static bool ConfirmPressed()
        {
            RefreshScheme();
            if (Keyboard.current != null &&
                (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
            {
                CurrentScheme = TownMapPromptScheme.Keyboard;
                return true;
            }

            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                CurrentScheme = TownMapPromptScheme.Gamepad;
                return true;
            }

            return false;
        }

        public static bool CancelPressed()
        {
            RefreshScheme();
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CurrentScheme = TownMapPromptScheme.Keyboard;
                return true;
            }

            if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
            {
                CurrentScheme = TownMapPromptScheme.Gamepad;
                return true;
            }

            return false;
        }

        public static bool MenuPressed()
        {
            RefreshScheme();
            if (Keyboard.current != null &&
                (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.mKey.wasPressedThisFrame))
            {
                CurrentScheme = TownMapPromptScheme.Keyboard;
                return true;
            }

            if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            {
                CurrentScheme = TownMapPromptScheme.Gamepad;
                return true;
            }

            return false;
        }

        private static bool WasGamepadUsedRecently()
        {
            var pad = Gamepad.current;
            if (pad == null)
            {
                return false;
            }

            return pad.leftStick.ReadValue().sqrMagnitude > 0.25f
                   || pad.buttonSouth.isPressed
                   || pad.buttonEast.isPressed
                   || pad.dpad.ReadValue().sqrMagnitude > 0.25f;
        }
    }
}

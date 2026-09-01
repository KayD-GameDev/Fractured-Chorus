using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FracturedChorus.UI
{
    /// <summary>
    /// Một chỗ duy nhất đọc phím "thoát" cho UI dùng chung, để mọi panel hiểu ESC giống nhau
    /// thay vì mỗi file chép lại một khối #if riêng.
    /// </summary>
    public static class UiCancelInput
    {
        public static bool WasPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return true;
            }

            if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
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
    }
}

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FracturedChorus.Menu
{
    public sealed class MainMenuLayoutSandboxMenu : MonoBehaviour
    {
        [SerializeField] private MainMenuLayoutSandboxRow[] rows;
        [SerializeField] private int selectedIndex;

        public void Bind(MainMenuLayoutSandboxRow[] boundRows)
        {
            rows = boundRows;
            selectedIndex = FirstInteractableIndex();
            Refresh(instant: true);
        }

        public void SelectIndex(int index, bool fromPointer)
        {
            if (!isActiveAndEnabled || rows == null || index < 0 || index >= rows.Length)
            {
                return;
            }

            if (rows[index] == null || !rows[index].Interactable)
            {
                return;
            }

            if (selectedIndex == index)
            {
                return;
            }

            selectedIndex = index;
            Refresh(instant: fromPointer);
        }

        public void ConfirmIndex(int index)
        {
            SelectIndex(index, fromPointer: true);
            if (rows == null || index < 0 || index >= rows.Length || rows[index] == null)
            {
                return;
            }

            rows[index].PlayPress();
        }

        private void OnEnable()
        {
            Refresh(instant: true);
        }

        private void Update()
        {
            if (!Application.isPlaying || rows == null || rows.Length == 0)
            {
                return;
            }

            if (WasMoveUpPressed())
            {
                Move(-1);
            }
            else if (WasMoveDownPressed())
            {
                Move(1);
            }
            else if (WasConfirmPressed())
            {
                ConfirmIndex(selectedIndex);
            }
        }

        private void Move(int delta)
        {
            var start = selectedIndex;
            var index = selectedIndex;
            for (var i = 0; i < rows.Length; i++)
            {
                index = (index + delta + rows.Length) % rows.Length;
                if (rows[index] != null && rows[index].Interactable)
                {
                    SelectIndex(index, fromPointer: false);
                    return;
                }
            }

            selectedIndex = start;
        }

        private void Refresh(bool instant)
        {
            if (rows == null)
            {
                return;
            }

            for (var i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null)
                {
                    continue;
                }

                rows[i].ApplyHighlight(i == selectedIndex, instant);
            }
        }

        private int FirstInteractableIndex()
        {
            if (rows == null)
            {
                return 0;
            }

            for (var i = 0; i < rows.Length; i++)
            {
                if (rows[i] != null && rows[i].Interactable)
                {
                    return i;
                }
            }

            return 0;
        }

        private static bool WasMoveUpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null &&
                (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame))
            {
                return true;
            }

            return Gamepad.current != null && Gamepad.current.dpad.up.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
#endif
        }

        private static bool WasMoveDownPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null &&
                (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame))
            {
                return true;
            }

            return Gamepad.current != null && Gamepad.current.dpad.down.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
#endif
        }

        private static bool WasConfirmPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null &&
                (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
            {
                return true;
            }

            return Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
#endif
        }
    }
}

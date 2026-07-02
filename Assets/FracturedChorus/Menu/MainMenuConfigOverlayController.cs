using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FracturedChorus.Menu
{
    public class MainMenuConfigOverlayController : MonoBehaviour
    {
        [SerializeField] private MainMenuStartGameController screenController;
        [SerializeField] private RectTransform highlightBar;
        [SerializeField] private Text infoText;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Slider brightnessSlider;
        [SerializeField] private Text difficultyValueText;
        [SerializeField] private Button difficultyPrevButton;
        [SerializeField] private Button difficultyNextButton;
        [SerializeField] private ConfigRow[] rows;

        private int _selectedIndex;
        private bool _active;

        [System.Serializable]
        private class ConfigRow
        {
            public RectTransform row;
            public Text label;
        }

        private void Awake()
        {
            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.AddListener(value => MainMenuGameSettings.SetMasterVolume(value));
            }

            if (brightnessSlider != null)
            {
                brightnessSlider.onValueChanged.AddListener(value =>
                {
                    MainMenuGameSettings.SetBackgroundBrightness(value);
                    screenController?.ApplyBackgroundBrightness(value);
                });
            }

            if (difficultyPrevButton != null)
            {
                difficultyPrevButton.onClick.AddListener(() => ChangeDifficulty(-1));
            }

            if (difficultyNextButton != null)
            {
                difficultyNextButton.onClick.AddListener(() => ChangeDifficulty(1));
            }

            RemoveStrayPointerSfx();
        }

        private void RemoveStrayPointerSfx()
        {
            var stray = GetComponentsInChildren<MainMenuUiPointerSfx>(true);
            for (var i = 0; i < stray.Length; i++)
            {
                Destroy(stray[i]);
            }
        }

        public void SetActive(bool active)
        {
            _active = active;
            if (active)
            {
                RefreshFromSettings();
                ScheduleSelectRow(0, fromInput: false);
            }
        }

        private void ScheduleSelectRow(int index, bool fromInput)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += () =>
                {
                    if (this == null)
                    {
                        return;
                    }

                    SelectRow(index, fromInput);
                };
                return;
            }
#endif
            SelectRow(index, fromInput);
        }

        public void RefreshFromSettings()
        {
            if (volumeSlider != null)
            {
                volumeSlider.SetValueWithoutNotify(MainMenuGameSettings.MasterVolume);
            }

            if (brightnessSlider != null)
            {
                brightnessSlider.SetValueWithoutNotify(MainMenuGameSettings.BackgroundBrightness);
            }

            UpdateDifficultyLabel();
        }

        public void HandleInput()
        {
            if (!_active)
            {
                return;
            }

            if (WasMoveUpPressed())
            {
                SelectRow(_selectedIndex - 1, fromInput: true);
            }
            else if (WasMoveDownPressed())
            {
                SelectRow(_selectedIndex + 1, fromInput: true);
            }
            else if (_selectedIndex == 2)
            {
                if (WasMoveLeftPressed())
                {
                    ChangeDifficulty(-1);
                }
                else if (WasMoveRightPressed())
                {
                    ChangeDifficulty(1);
                }
            }
        }

        private void ChangeDifficulty(int direction)
        {
            MainMenuGameSettings.CycleDifficulty(direction);
            UpdateDifficultyLabel();
            RefreshInfoText();
        }

        private void UpdateDifficultyLabel()
        {
            if (difficultyValueText != null)
            {
                difficultyValueText.text = MainMenuGameSettings.GetDifficultyLabel(MainMenuGameSettings.Difficulty);
            }
        }

        private void SelectRow(int index, bool fromInput)
        {
            if (rows == null || rows.Length == 0)
            {
                return;
            }

            _selectedIndex = (index % rows.Length + rows.Length) % rows.Length;
            RefreshHighlight();
            RefreshInfoText();

            if (fromInput && _selectedIndex == 0 && volumeSlider != null)
            {
                volumeSlider.Select();
            }
            else if (fromInput && _selectedIndex == 1 && brightnessSlider != null)
            {
                brightnessSlider.Select();
            }
        }

        private void RefreshHighlight()
        {
            if (highlightBar == null || rows == null || _selectedIndex < 0 || _selectedIndex >= rows.Length)
            {
                return;
            }

            var row = rows[_selectedIndex].row;
            if (row == null)
            {
                return;
            }

            highlightBar.SetParent(row, false);
            var barRect = highlightBar;
            barRect.anchorMin = Vector2.zero;
            barRect.anchorMax = Vector2.one;
            barRect.offsetMin = new Vector2(-8f, -3f);
            barRect.offsetMax = new Vector2(8f, 3f);
            barRect.SetAsFirstSibling();
        }

        private void RefreshInfoText()
        {
            if (infoText == null || rows == null || _selectedIndex < 0 || _selectedIndex >= rows.Length)
            {
                return;
            }

            switch (_selectedIndex)
            {
                case 0:
                    infoText.text = "Adjust master volume for menu and game audio.";
                    break;
                case 1:
                    infoText.text = "Adjust attract and main menu background brightness.";
                    break;
                case 2:
                    infoText.text = MainMenuGameSettings.GetDifficultyDescription(MainMenuGameSettings.Difficulty);
                    break;
            }
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

        private static bool WasMoveLeftPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null &&
                (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame))
            {
                return true;
            }

            return Gamepad.current != null && Gamepad.current.dpad.left.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
#endif
        }

        private static bool WasMoveRightPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null &&
                (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame))
            {
                return true;
            }

            return Gamepad.current != null && Gamepad.current.dpad.right.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
#endif
        }
    }
}

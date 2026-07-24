using UnityEngine;
using UnityEngine.UI;
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
        [SerializeField] private MainMenuConfigToggleSwitch skipUnreadToggle;
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
            ResolveSkipUnreadToggle();

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

            if (skipUnreadToggle != null)
            {
                skipUnreadToggle.ValueChanged += OnSkipUnreadToggleChanged;
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
            if (!active)
            {
                return;
            }

            RefreshFromSettings();
            screenController?.ApplyBackgroundBrightness(MainMenuGameSettings.BackgroundBrightness);
            SelectRow(0, fromInput: false);
        }

#if UNITY_EDITOR
        public void SetEditorPreviewActive(bool active)
        {
            _active = false;
            if (!active)
            {
                return;
            }

            RefreshFromSettings();
            _selectedIndex = 0;
            RefreshInfoText();
        }
#endif

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

            if (skipUnreadToggle != null)
            {
                skipUnreadToggle.SetValue(MainMenuGameSettings.SkipUnreadText, notify: false);
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
            else if (_selectedIndex == 2 && WasConfirmPressed())
            {
                skipUnreadToggle?.Toggle();
            }
            else if (_selectedIndex == 3)
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

            if (!fromInput)
            {
                return;
            }

            switch (_selectedIndex)
            {
                case 0:
                    volumeSlider?.Select();
                    break;
                case 1:
                    brightnessSlider?.Select();
                    break;
                case 2:
                    break;
            }
        }

        private void ResolveSkipUnreadToggle()
        {
            if (skipUnreadToggle != null)
            {
                return;
            }

            skipUnreadToggle = GetComponentInChildren<MainMenuConfigToggleSwitch>(true);
        }

        private static bool WasConfirmPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null &&
                (Keyboard.current.enterKey.wasPressedThisFrame ||
                 Keyboard.current.spaceKey.wasPressedThisFrame))
            {
                return true;
            }

            return Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
#endif
        }

        private void OnSkipUnreadToggleChanged(bool enabled)
        {
            MainMenuGameSettings.SetSkipUnreadText(enabled);
            RefreshInfoText();
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

            if (highlightBar.parent != row)
            {
                highlightBar.SetParent(row, false);
            }

            var barRect = highlightBar;
            barRect.anchorMin = Vector2.zero;
            barRect.anchorMax = Vector2.one;
            barRect.offsetMin = new Vector2(-8f, -3f);
            barRect.offsetMax = new Vector2(8f, 3f);
            if (barRect.GetSiblingIndex() != 0)
            {
                barRect.SetAsFirstSibling();
            }
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
                    infoText.text = "Adjust master volume across all scenes.";
                    break;
                case 1:
                    infoText.text = "Adjust screen brightness across all scenes.";
                    break;
                case 2:
                    infoText.text = MainMenuGameSettings.SkipUnreadText
                        ? "Allow skipping dialogue you have not read yet."
                        : "Only skip dialogue you have already read.";
                    break;
                case 3:
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

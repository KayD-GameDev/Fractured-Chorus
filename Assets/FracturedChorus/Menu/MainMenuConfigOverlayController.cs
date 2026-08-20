using UnityEngine;
using UnityEngine.EventSystems;
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
        [SerializeField] private Button[] difficultyChipButtons;
        [SerializeField] private Image[] difficultyChipGraphics;
        [SerializeField] private Sprite chipNormalSprite;
        [SerializeField] private Sprite chipSelectedSprite;
        [SerializeField] private Button volumeMinusButton;
        [SerializeField] private Button volumePlusButton;
        [SerializeField] private Button brightnessMinusButton;
        [SerializeField] private Button brightnessPlusButton;
        [SerializeField] private float sliderStep = 0.05f;
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
            BindRowPointers();

            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.AddListener(value =>
                {
                    SelectRow(0, fromInput: false);
                    MainMenuGameSettings.SetMasterVolume(value);
                });
            }

            if (brightnessSlider != null)
            {
                brightnessSlider.onValueChanged.AddListener(value =>
                {
                    SelectRow(1, fromInput: false);
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
                difficultyPrevButton.onClick.AddListener(() =>
                {
                    SelectRow(3, fromInput: false);
                    ChangeDifficulty(-1);
                });
            }

            if (difficultyNextButton != null)
            {
                difficultyNextButton.onClick.AddListener(() =>
                {
                    SelectRow(3, fromInput: false);
                    ChangeDifficulty(1);
                });
            }

            BindChipButtons();
            BindStepper(volumeMinusButton, 0, () => NudgeSlider(volumeSlider, -sliderStep));
            BindStepper(volumePlusButton, 0, () => NudgeSlider(volumeSlider, sliderStep));
            BindStepper(brightnessMinusButton, 1, () => NudgeSlider(brightnessSlider, -sliderStep));
            BindStepper(brightnessPlusButton, 1, () => NudgeSlider(brightnessSlider, sliderStep));

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
            SelectRow(1, fromInput: false);
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
            _selectedIndex = 1;
            RefreshHighlight();
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
            RefreshDifficultyChips();
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
            RefreshDifficultyChips();
            RefreshInfoText();
        }

        private void SetDifficulty(GameDifficulty value)
        {
            MainMenuGameSettings.SetDifficulty(value);
            UpdateDifficultyLabel();
            RefreshDifficultyChips();
            RefreshInfoText();
        }

        private void UpdateDifficultyLabel()
        {
            if (difficultyValueText != null)
            {
                difficultyValueText.text = MainMenuGameSettings.GetDifficultyLabel(MainMenuGameSettings.Difficulty);
            }
        }

        private void BindChipButtons()
        {
            if (difficultyChipButtons == null)
            {
                return;
            }

            for (var i = 0; i < difficultyChipButtons.Length; i++)
            {
                var button = difficultyChipButtons[i];
                if (button == null)
                {
                    continue;
                }

                var difficulty = (GameDifficulty)i;
                button.onClick.AddListener(() =>
                {
                    SelectRow(3, fromInput: false);
                    SetDifficulty(difficulty);
                });
            }
        }

        private void BindStepper(Button button, int rowIndex, UnityEngine.Events.UnityAction handler)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.AddListener(() =>
            {
                SelectRow(rowIndex, fromInput: false);
                handler();
            });
        }

        private static void NudgeSlider(Slider slider, float delta)
        {
            if (slider == null)
            {
                return;
            }

            slider.value = Mathf.Clamp(slider.value + delta, slider.minValue, slider.maxValue);
        }

        private void RefreshDifficultyChips()
        {
            if (difficultyChipGraphics == null)
            {
                return;
            }

            var selected = (int)MainMenuGameSettings.Difficulty;
            for (var i = 0; i < difficultyChipGraphics.Length; i++)
            {
                var graphic = difficultyChipGraphics[i];
                if (graphic == null)
                {
                    continue;
                }

                var sprite = i == selected ? chipSelectedSprite : chipNormalSprite;
                if (sprite != null)
                {
                    graphic.sprite = sprite;
                    graphic.color = Color.white;
                }
            }
        }

        private void SelectRow(int index, bool fromInput)
        {
            if (rows == null || rows.Length == 0)
            {
                return;
            }

            var next = (index % rows.Length + rows.Length) % rows.Length;
            var changed = next != _selectedIndex;
            _selectedIndex = next;
            RefreshHighlight();
            if (changed)
            {
                RefreshInfoText();
            }

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
            SelectRow(2, fromInput: false);
            MainMenuGameSettings.SetSkipUnreadText(enabled);
            RefreshInfoText();
        }

        private void BindRowPointers()
        {
            if (rows == null)
            {
                return;
            }

            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i].row;
                if (row == null)
                {
                    continue;
                }

                var graphic = row.GetComponent<Image>();
                if (graphic == null)
                {
                    graphic = row.gameObject.AddComponent<Image>();
                    graphic.color = new Color(1f, 1f, 1f, 0.001f);
                }

                graphic.raycastTarget = true;
                var index = i;
                var trigger = row.gameObject.GetComponent<EventTrigger>() ?? row.gameObject.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                entry.callback.AddListener(_ => SelectRow(index, fromInput: false));
                trigger.triggers.Add(entry);
            }
        }

        private void LateUpdate()
        {
            if (!_active || rows == null || EventSystem.current == null)
            {
                return;
            }

            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null)
            {
                return;
            }

            var selectedTransform = selected.transform;
            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i].row;
                if (row == null)
                {
                    continue;
                }

                if (selectedTransform == row || selectedTransform.IsChildOf(row))
                {
                    if (i != _selectedIndex)
                    {
                        SelectRow(i, fromInput: false);
                    }

                    return;
                }
            }
        }

        private void RefreshHighlight()
        {
            if (highlightBar != null)
            {
                highlightBar.gameObject.SetActive(false);
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

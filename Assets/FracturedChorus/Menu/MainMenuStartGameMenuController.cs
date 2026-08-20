using FracturedChorus.Meta;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FracturedChorus.Menu
{
    public class MainMenuStartGameMenuController : MonoBehaviour
    {
        [SerializeField] private MainMenuStartGameController screenController;
        [SerializeField] private RectTransform highlightBar;
        [SerializeField] private MenuOption[] options;
        [SerializeField] private Text statusText;

        private int _selectedIndex;
        private bool _enabled;
        private SaveLoadSlotListView _saveLoadView;

        [System.Serializable]
        private class MenuOption
        {
            public RectTransform row;
            public Button button;
            public Text label;
            public MainMenuButtonRowView rowView;
            public MenuAction action;
            public bool interactable;
        }

        private enum MenuAction
        {
            NewGame,
            LoadGame,
            OffBeatArchive,
            Config,
            Quit
        }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            if (enabled)
            {
                RefreshLoadGameInteractable();
                if (_selectedIndex < 0)
                {
                    _selectedIndex = FindFirstInteractableIndex();
                }

                RefreshHighlight();
            }
        }

        private void RefreshLoadGameInteractable()
        {
            if (options == null)
            {
                return;
            }

            var hasSave = GameMetaSaveLoad.HasAnySave();
            for (var i = 0; i < options.Length; i++)
            {
                if (ResolveAction(options[i]) == MenuAction.LoadGame)
                {
                    options[i].interactable = hasSave;
                }
            }
        }

        public void NotifyHover(int index)
        {
            if (!_enabled)
            {
                return;
            }

            SelectIndex(index, fromHover: true, playSfx: true);
        }

        public void NotifyHoverExit(int index)
        {
        }

        public void HandleInput()
        {
            if (!_enabled || options == null || options.Length == 0)
            {
                return;
            }

            if (WasMoveUpPressed())
            {
                SelectPrevious();
            }
            else if (WasMoveDownPressed())
            {
                SelectNext();
            }
            else if (WasConfirmPressed())
            {
                ActivateSelected(playConfirmSfx: true);
            }
        }

        private void Awake()
        {
            EnsureButtonHitAreas();

            if (options == null)
            {
                return;
            }

            for (var i = 0; i < options.Length; i++)
            {
                var index = i;
                if (options[i].button == null)
                {
                    continue;
                }

                options[i].button.onClick.AddListener(() => OnOptionClicked(index));
            }
        }

        private void EnsureButtonHitAreas()
        {
            if (options == null)
            {
                return;
            }

            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                if (option.row == null || option.button == null)
                {
                    continue;
                }

                var hitTransform = option.row.Find("HitArea");
                Image hitArea;
                if (hitTransform == null)
                {
                    var hitGo = new GameObject("HitArea", typeof(RectTransform));
                    hitGo.transform.SetParent(option.row, false);
                    hitGo.transform.SetAsFirstSibling();
                    var hitRect = hitGo.GetComponent<RectTransform>();
                    hitRect.anchorMin = Vector2.zero;
                    hitRect.anchorMax = Vector2.one;
                    hitRect.offsetMin = Vector2.zero;
                    hitRect.offsetMax = Vector2.zero;
                    hitArea = hitGo.AddComponent<Image>();
                    hitArea.color = new Color(1f, 1f, 1f, 0.001f);
                    hitArea.raycastTarget = true;
                }
                else
                {
                    hitArea = hitTransform.GetComponent<Image>();
                    if (hitArea == null)
                    {
                        hitArea = hitTransform.gameObject.AddComponent<Image>();
                        hitArea.color = new Color(1f, 1f, 1f, 0.001f);
                        hitArea.raycastTarget = true;
                    }
                }

                option.button.targetGraphic = hitArea;
                option.button.transition = Selectable.Transition.None;

                if (option.label != null)
                {
                    option.label.raycastTarget = false;
                }

                if (option.rowView == null)
                {
                    option.rowView = option.row.GetComponent<MainMenuButtonRowView>();
                    if (option.rowView == null)
                    {
                        option.rowView = option.row.gameObject.AddComponent<MainMenuButtonRowView>();
                    }
                }

                option.rowView.Configure(this, i, option.label, hitArea, option.interactable);
                EnsurePointerSfx(hitArea != null ? hitArea.gameObject : option.row.gameObject, option.rowView);
                EnsurePointerSfx(option.row.gameObject, option.rowView);
            }
        }

        private void EnsurePointerSfx(GameObject eventTarget, MainMenuButtonRowView rowView)
        {
            if (eventTarget == null || screenController == null)
            {
                return;
            }

            var pointerSfx = eventTarget.GetComponent<MainMenuUiPointerSfx>();
            if (pointerSfx == null)
            {
                pointerSfx = eventTarget.AddComponent<MainMenuUiPointerSfx>();
            }

            pointerSfx.Bind(screenController, rowView);
        }

        private void OnOptionClicked(int index)
        {
            if (!_enabled)
            {
                return;
            }

            _selectedIndex = index;
            RefreshHighlight();
            ActivateSelected(playConfirmSfx: false);
        }

        private void ActivateSelected(bool playConfirmSfx)
        {
            if (!_enabled || _selectedIndex < 0 || _selectedIndex >= options.Length)
            {
                return;
            }

            var option = options[_selectedIndex];
            if (!option.interactable)
            {
                if (playConfirmSfx)
                {
                    screenController?.PlayButtonPressSfx();
                }

                SetStatus("No save data found.");
                return;
            }

            var action = ResolveAction(option);
            if (playConfirmSfx && action != MenuAction.NewGame)
            {
                screenController?.PlayButtonPressSfx();
            }

            switch (action)
            {
                case MenuAction.NewGame:
                    if (screenController == null)
                    {
                        return;
                    }

                    screenController.PlayButtonPressSfx();
                    if (screenController.BeginNewGame())
                    {
                        SetStatus("Starting new run…");
                    }
                    break;
                case MenuAction.LoadGame:
                    OpenLoadGameSlots();
                    break;
                case MenuAction.OffBeatArchive:
                    screenController?.ShowOffBeatArchive();
                    break;
                case MenuAction.Config:
                    screenController?.ShowSettings();
                    break;
                case MenuAction.Quit:
                    QuitGame();
                    break;
            }
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SelectNext()
        {
            if (options.Length == 0)
            {
                return;
            }

            if (_selectedIndex < 0)
            {
                SelectIndex(FindFirstInteractableIndex(), fromHover: false, playSfx: true);
                return;
            }

            var next = _selectedIndex;
            for (var step = 0; step < options.Length; step++)
            {
                next = (next + 1) % options.Length;
                if (options[next].interactable || options[next].action == MenuAction.LoadGame)
                {
                    SelectIndex(next, fromHover: false, playSfx: true);
                    return;
                }
            }
        }

        private void SelectPrevious()
        {
            if (options.Length == 0)
            {
                return;
            }

            if (_selectedIndex < 0)
            {
                SelectIndex(FindLastInteractableIndex(), fromHover: false, playSfx: true);
                return;
            }

            var prev = _selectedIndex;
            for (var step = 0; step < options.Length; step++)
            {
                prev = (prev - 1 + options.Length) % options.Length;
                if (options[prev].interactable || options[prev].action == MenuAction.LoadGame)
                {
                    SelectIndex(prev, fromHover: false, playSfx: true);
                    return;
                }
            }
        }

        private void SelectIndex(int index, bool fromHover, bool playSfx = true)
        {
            var previousIndex = _selectedIndex;
            var nextIndex = Mathf.Clamp(index, 0, options.Length - 1);
            if (playSfx && previousIndex != nextIndex)
            {
                screenController?.PlayButtonPressSfx();
            }

            _selectedIndex = nextIndex;
            RefreshHighlight();

            if (!fromHover && options[_selectedIndex].button != null)
            {
                options[_selectedIndex].button.Select();
            }
        }

        private int FindFirstInteractableIndex()
        {
            for (var i = 0; i < options.Length; i++)
            {
                if (options[i].interactable)
                {
                    return i;
                }
            }

            return 0;
        }

        private int FindLastInteractableIndex()
        {
            for (var i = options.Length - 1; i >= 0; i--)
            {
                if (options[i].interactable)
                {
                    return i;
                }
            }

            return options.Length - 1;
        }

        private void RefreshHighlight()
        {
            if (options == null)
            {
                return;
            }

            var hasSelection = _selectedIndex >= 0 && _selectedIndex < options.Length;
            if (highlightBar != null)
            {
                if (!hasSelection)
                {
                    highlightBar.gameObject.SetActive(false);
                }
                else
                {
                    var row = options[_selectedIndex].row;
                    if (row != null)
                    {
                        highlightBar.gameObject.SetActive(true);
                        highlightBar.SetParent(row, false);
                        highlightBar.anchorMin = Vector2.zero;
                        highlightBar.anchorMax = Vector2.one;
                        highlightBar.offsetMin = new Vector2(-12f, -4f);
                        highlightBar.offsetMax = new Vector2(12f, 4f);
                        highlightBar.SetAsFirstSibling();
                    }
                }
            }

            for (var i = 0; i < options.Length; i++)
            {
                options[i].rowView?.SetInteractable(options[i].interactable);
                options[i].rowView?.ApplySelectionVisual(hasSelection && i == _selectedIndex);
            }
        }

        private MenuAction ResolveAction(MenuOption option)
        {
            var labelText = option.label != null ? option.label.text : string.Empty;
            switch (labelText)
            {
                case "NEW GAME":
                    return MenuAction.NewGame;
                case "LOAD GAME":
                    return MenuAction.LoadGame;
                case "OFF-BEAT ARCHIVE":
                    return MenuAction.OffBeatArchive;
                case "CONFIG":
                    return MenuAction.Config;
                case "QUIT":
                    return MenuAction.Quit;
                default:
                    return option.action;
            }
        }

        private void OpenLoadGameSlots()
        {
            if (!GameMetaSaveLoad.HasAnySave())
            {
                SetStatus("No save data found.");
                return;
            }

            var host = transform.root;
            _saveLoadView = SaveLoadSlotListView.Show(
                host,
                SaveLoadSlotListView.Mode.Load,
                onLoad: slot =>
                {
                    if (screenController != null && screenController.LoadGame(slot))
                    {
                        SetStatus($"Loading slot {slot + 1:00}…");
                    }
                });
        }

        private void SetStatus(string message)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = message;
        }

        private static bool WasMoveUpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null &&
                (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame))
            {
                return true;
            }

            if (Gamepad.current != null && Gamepad.current.dpad.up.wasPressedThisFrame)
            {
                return true;
            }

            return false;
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

            if (Gamepad.current != null && Gamepad.current.dpad.down.wasPressedThisFrame)
            {
                return true;
            }

            return false;
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

            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                return true;
            }

            return false;
#else
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
#endif
        }
    }
}

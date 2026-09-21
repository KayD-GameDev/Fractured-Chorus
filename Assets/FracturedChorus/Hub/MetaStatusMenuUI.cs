using System;
using System.Collections.Generic;
using FracturedChorus.Hub.CharacterBuild;
using FracturedChorus.Menu;
using FracturedChorus.Meta;
using FracturedChorus.RunMap;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FracturedChorus.Hub
{
    public sealed class MetaStatusMenuUI : MonoBehaviour
    {
        public enum Tab
        {
            Stats = 0,
            Bonds = 1,
            Calendar = 2,
            System = 3
        }

        public readonly struct BuildResult
        {
            public BuildResult(Button menuButton, MetaStatusMenuUI menu)
            {
                MenuButton = menuButton;
                Menu = menu;
            }

            public Button MenuButton { get; }
            public MetaStatusMenuUI Menu { get; }
        }

        [SerializeField] private GameObject root;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text dateChipLabel;
        [SerializeField] private HubCornerInfoHud cornerInfoHud;
        [SerializeField] private Text tooltipLabel;
        [SerializeField] private ResonanceDiveButton diveButton;
        [SerializeField] private Image confirmPromptIcon;
        [SerializeField] private Image closePromptIcon;
        [SerializeField] private Button statsButton;
        [SerializeField] private Button bondsButton;
        [SerializeField] private Button calendarButton;
        [SerializeField] private Button systemButton;
        [SerializeField] private Image statsImage;
        [SerializeField] private Image bondsImage;
        [SerializeField] private Image calendarImage;
        [SerializeField] private Image systemImage;
        [SerializeField] private GameObject menuListRoot;
        [SerializeField] private GameObject systemMenuListRoot;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button configButton;
        [SerializeField] private Button returnToTitleButton;
        [SerializeField] private Image saveImage;
        [SerializeField] private Image loadImage;
        [SerializeField] private Image configImage;
        [SerializeField] private Image returnToTitleImage;
        [SerializeField] private HubConfigOverlayUI hubConfigOverlay;
        [SerializeField] private Sprite statsNormal;
        [SerializeField] private Sprite statsSelected;
        [SerializeField] private Sprite bondsNormal;
        [SerializeField] private Sprite bondsSelected;
        [SerializeField] private Sprite calendarNormal;
        [SerializeField] private Sprite calendarSelected;
        [SerializeField] private Sprite systemNormal;
        [SerializeField] private Sprite systemSelected;
        [SerializeField] private CalendarOverlayUI calendarOverlay;
        [SerializeField] private SocialStatsOverlayUI socialStatsOverlay;
        [SerializeField] private TownMapSfxController sfx;

        private Tab _tab = Tab.Stats;
        private GameMetaState _state;
        private bool _wired;
        private bool _systemSubmenuOpen;

        private void Awake()
        {
            EnsureSystemSubmenuBindings();
            EnsureCornerInfoHud();
            Wire();
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void EnsureCornerInfoHud()
        {
            if (cornerInfoHud == null)
            {
                cornerInfoHud = GetComponentInChildren<HubCornerInfoHud>(true);
            }

            cornerInfoHud?.WireReferences();

            if (cornerInfoHud != null && dateChipLabel != null)
            {
                dateChipLabel.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            if (calendarOverlay != null && calendarOverlay.IsOpen)
            {
                return;
            }

            if (socialStatsOverlay != null && socialStatsOverlay.IsOpen)
            {
                return;
            }

            // ESC do StatusMenuRuntime làm chủ để mọi scene đóng/mở menu theo cùng một luật.
            if ((_systemSubmenuOpen || _tab == Tab.System) && WasHealHotkeyPressed())
            {
                var hub = UnityEngine.Object.FindAnyObjectByType<CampusHubController>();
                hub?.TryHubHealService();
                Refresh();
            }
        }

        private static bool WasHealHotkeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.H);
#else
            return false;
#endif
        }

        public bool IsOpen => root != null && root.activeSelf;

        public bool IsCalendarOpen => calendarOverlay != null && calendarOverlay.IsOpen;

        public bool IsSocialStatsOpen => socialStatsOverlay != null && socialStatsOverlay.IsOpen;

        public bool IsSystemSubmenuOpen => _systemSubmenuOpen;

        public bool TryExitSystemSubmenu()
        {
            if (!_systemSubmenuOpen)
            {
                return false;
            }

            ExitSystemSubmenu();
            return true;
        }

        public bool TryNavigateBack()
        {
            if (TryExitSystemSubmenu())
            {
                return true;
            }

            if (_tab != Tab.Stats)
            {
                Show(_state ?? GameMetaSession.Current, Tab.Stats);
                return true;
            }

            return false;
        }

        public void BindSfx(TownMapSfxController controller)
        {
            sfx = controller;
            calendarOverlay?.BindSfx(controller);
            socialStatsOverlay?.BindSfx(controller);
        }

        public void Show(GameMetaState state, Tab tab = Tab.Stats)
        {
            EnsureSpritesAssigned();
            EnsureSystemSubmenuBindings();
            Wire();
            EnsureDiveButton();
            _state = state;
            if (_systemSubmenuOpen)
            {
                ExitSystemSubmenu();
            }

            _tab = tab == Tab.System ? Tab.Stats : tab;
            if (root != null)
            {
                root.SetActive(true);
            }

            diveButton?.SetListening(true);

            sfx?.PlayOpenPanel();
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            Refresh();
        }

        public void ShowSystemSubmenu(GameMetaState state)
        {
            Show(state);
            EnterSystemSubmenu();
        }

        public void Hide()
        {
            if (calendarOverlay != null && calendarOverlay.IsOpen)
            {
                calendarOverlay.Hide();
            }

            if (socialStatsOverlay != null && socialStatsOverlay.IsOpen)
            {
                socialStatsOverlay.Hide();
            }

            if (_systemSubmenuOpen)
            {
                ExitSystemSubmenu();
            }

            if (IsOpen)
            {
                sfx?.PlayClosePanel();
            }

            diveButton?.SetListening(false);

            if (root != null)
            {
                root.SetActive(false);
            }
        }

        public void Toggle(GameMetaState state)
        {
            if (IsOpen)
            {
                Hide();
            }
            else
            {
                Show(state);
            }
        }

        public static BuildResult Build(Transform parent)
        {
            var existingMenuTf = parent.Find("StatusMenu");
            var existingButtonTf = parent.Find("MenuButton");

            if (existingMenuTf != null && existingMenuTf.Find("MenuList") == null)
            {
                existingMenuTf = null;
            }

            MetaStatusMenuUI menu = null;
            Button menuButton = null;

            if (existingMenuTf != null)
            {
                menu = existingMenuTf.GetComponent<MetaStatusMenuUI>()
                       ?? existingMenuTf.gameObject.AddComponent<MetaStatusMenuUI>();
            }

            if (existingButtonTf != null)
            {
                menuButton = existingButtonTf.GetComponent<Button>();
            }

            if (menu != null && menuButton != null && existingMenuTf != null && existingMenuTf.Find("MenuList") != null)
            {
                menu.EnsureSpritesAssigned();
                menu.EnsureCalendarOverlay(parent);
                menu.EnsureSocialStatsOverlay(parent);
                menu.Rewire();
                return new BuildResult(menuButton, menu);
            }

            if (menuButton == null)
            {
                menuButton = CreateHudMenuButton(parent);
            }

            if (menu == null)
            {
                menu = CreateMenuHierarchy(parent);
            }

            menu.EnsureCalendarOverlay(parent);
            menu.EnsureSocialStatsOverlay(parent);
            return new BuildResult(menuButton, menu);
        }

        public void EnsureCalendarOverlay(Transform townMapRoot)
        {
            if (calendarOverlay != null)
            {
                calendarOverlay.BindSfx(sfx);
                return;
            }

            calendarOverlay = CalendarOverlayUI.Build(townMapRoot).Overlay;
            calendarOverlay.BindSfx(sfx);
        }

        public void EnsureSocialStatsOverlay(Transform townMapRoot)
        {
            if (socialStatsOverlay != null)
            {
                socialStatsOverlay.BindSfx(sfx);
                return;
            }

            socialStatsOverlay = SocialStatsOverlayUI.Build(townMapRoot).Overlay;
            socialStatsOverlay.BindSfx(sfx);
        }

        private static Button CreateHudMenuButton(Transform parent)
        {
            var menuButtonGo = new GameObject("MenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
            menuButtonGo.transform.SetParent(parent, false);
            var menuButtonRect = menuButtonGo.GetComponent<RectTransform>();
            menuButtonRect.anchorMin = new Vector2(1f, 1f);
            menuButtonRect.anchorMax = new Vector2(1f, 1f);
            menuButtonRect.pivot = new Vector2(1f, 1f);
            menuButtonRect.anchoredPosition = new Vector2(-24f, -24f);
            menuButtonRect.sizeDelta = new Vector2(120f, 48f);
            var menuButtonImage = menuButtonGo.GetComponent<Image>();
            menuButtonImage.color = new Color(0.039f, 0.039f, 0.18f, 0.95f);
            var menuButton = menuButtonGo.GetComponent<Button>();
            menuButton.targetGraphic = menuButtonImage;
            var menuButtonLabel = CreateText(menuButtonGo.transform, "Label", "MENU", 22, TextAnchor.MiddleCenter);
            Stretch(menuButtonLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            menuButtonLabel.color = FcColorTokens.Brand.Cyan;
            menuButtonLabel.fontStyle = FontStyle.Bold | FontStyle.Italic;
            return menuButton;
        }

        private static MetaStatusMenuUI CreateMenuHierarchy(Transform parent)
        {
            var sprites = LoadSpritePack();

            var rootGo = new GameObject("StatusMenu", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            Stretch(rootGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(rootGo.transform, false);
            Stretch(bgGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bg = bgGo.GetComponent<Image>();
            bg.sprite = sprites.Background;
            bg.preserveAspect = false;
            bg.raycastTarget = true;
            bg.color = Color.white;

            var dateChip = CreateText(rootGo.transform, "DateChip", "01/09", 22, TextAnchor.MiddleLeft);
            Stretch(dateChip.rectTransform, new Vector2(0.02f, 0.9f), new Vector2(0.28f, 0.98f), Vector2.zero, Vector2.zero);
            dateChip.fontStyle = FontStyle.Bold;
            dateChip.color = FcColorTokens.Brand.Cyan;

            var listRoot = new GameObject("MenuList", typeof(RectTransform));
            listRoot.transform.SetParent(rootGo.transform, false);
            Stretch(listRoot.GetComponent<RectTransform>(), new Vector2(0.52f, 0.18f), new Vector2(0.96f, 0.88f), Vector2.zero, Vector2.zero);

            var stats = CreateMenuRow(listRoot.transform, "BtnStats", sprites.StatsNormal, "STATS", 0);
            var bonds = CreateMenuRow(listRoot.transform, "BtnBonds", sprites.BondsNormal, "BONDS", 1);
            var calendar = CreateMenuRow(listRoot.transform, "BtnCalendar", sprites.CalendarNormal, "CALENDAR", 2);
            var system = CreateMenuRow(listRoot.transform, "BtnSystem", sprites.SystemNormal, "SYSTEM", 3);

            var systemListRoot = new GameObject("SystemMenuList", typeof(RectTransform));
            systemListRoot.transform.SetParent(rootGo.transform, false);
            Stretch(systemListRoot.GetComponent<RectTransform>(), new Vector2(0.52f, 0.18f), new Vector2(0.96f, 0.88f), Vector2.zero, Vector2.zero);
            var save = CreateMenuRow(systemListRoot.transform, "BtnSave", sprites.StatsNormal, "SAVE", 0);
            var load = CreateMenuRow(systemListRoot.transform, "BtnLoad", sprites.BondsNormal, "LOAD", 1);
            var config = CreateMenuRow(systemListRoot.transform, "BtnConfig", sprites.CalendarNormal, "CONFIG", 2);
            var returnTitle = CreateMenuRow(systemListRoot.transform, "BtnReturnToTitle", sprites.SystemNormal, "TO TITLE", 3);
            systemListRoot.SetActive(false);

            var detail = new GameObject("DetailPanel", typeof(RectTransform), typeof(Image));
            detail.transform.SetParent(rootGo.transform, false);
            Stretch(detail.GetComponent<RectTransform>(), new Vector2(0.08f, 0.08f), new Vector2(0.48f, 0.42f), Vector2.zero, Vector2.zero);
            var detailBg = detail.GetComponent<Image>();
            detailBg.color = new Color(1f, 1f, 1f, 0f);
            detailBg.raycastTarget = false;

            var detailBody = CreateText(detail.transform, "DetailBody", string.Empty, 20, TextAnchor.UpperLeft);
            Stretch(detailBody.rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f), Vector2.zero, Vector2.zero);
            detailBody.gameObject.SetActive(false);

            var dive = ResonanceDiveButton.Ensure(detail.transform, null, fillParent: true);

            var tooltip = CreateText(rootGo.transform, "Tooltip", "View Social Stats", 18, TextAnchor.MiddleRight);
            Stretch(tooltip.rectTransform, new Vector2(0.55f, 0.08f), new Vector2(0.92f, 0.14f), Vector2.zero, Vector2.zero);
            tooltip.color = FcColorTokens.Brand.Cyan;
            tooltip.fontStyle = FontStyle.Italic;

            var prompts = new GameObject("Prompts", typeof(RectTransform));
            prompts.transform.SetParent(rootGo.transform, false);
            Stretch(prompts.GetComponent<RectTransform>(), new Vector2(0.72f, 0.02f), new Vector2(0.98f, 0.08f), Vector2.zero, Vector2.zero);

            var confirmIcon = CreateImage(prompts.transform, "ConfirmIcon", sprites.ConfirmPrompt);
            var confirmText = CreateText(prompts.transform, "ConfirmText", "Confirm", 16, TextAnchor.MiddleLeft);
            confirmText.color = Color.white;

            var confirmIconRt = confirmIcon.rectTransform;
            Stretch(confirmIconRt, new Vector2(0f, 0.1f), new Vector2(0.14f, 0.9f), Vector2.zero, Vector2.zero);
            Stretch(confirmText.rectTransform, new Vector2(0.14f, 0f), new Vector2(0.38f, 1f), Vector2.zero, Vector2.zero);

            var separator = CreateText(prompts.transform, "PromptSeparator", "|", 16, TextAnchor.MiddleCenter);
            Stretch(separator.rectTransform, new Vector2(0.41f, 0f), new Vector2(0.45f, 1f), Vector2.zero, Vector2.zero);
            separator.color = Color.white;

            var closeIcon = CreateImage(prompts.transform, "BackIcon", sprites.ClosePrompt);
            Stretch(closeIcon.rectTransform, new Vector2(0.48f, 0.1f), new Vector2(0.62f, 0.9f), Vector2.zero, Vector2.zero);
            var closeText = CreateText(prompts.transform, "BackText", "Back", 16, TextAnchor.MiddleLeft);
            Stretch(closeText.rectTransform, new Vector2(0.62f, 0f), new Vector2(0.86f, 1f), Vector2.zero, Vector2.zero);
            closeText.color = Color.white;

            var menu = rootGo.AddComponent<MetaStatusMenuUI>();
            menu.root = rootGo;
            menu.backgroundImage = bg;
            menu.dateChipLabel = dateChip;
            menu.tooltipLabel = tooltip;
            menu.diveButton = dive;
            menu.confirmPromptIcon = confirmIcon;
            menu.closePromptIcon = closeIcon;
            menu.statsButton = stats.Button;
            menu.bondsButton = bonds.Button;
            menu.calendarButton = calendar.Button;
            menu.systemButton = system.Button;
            menu.statsImage = stats.Image;
            menu.bondsImage = bonds.Image;
            menu.calendarImage = calendar.Image;
            menu.systemImage = system.Image;
            menu.menuListRoot = listRoot;
            menu.systemMenuListRoot = systemListRoot;
            menu.saveButton = save.Button;
            menu.loadButton = load.Button;
            menu.configButton = config.Button;
            menu.returnToTitleButton = returnTitle.Button;
            menu.saveImage = save.Image;
            menu.loadImage = load.Image;
            menu.configImage = config.Image;
            menu.returnToTitleImage = returnTitle.Image;
            menu.statsNormal = sprites.StatsNormal;
            menu.statsSelected = sprites.StatsSelected;
            menu.bondsNormal = sprites.BondsNormal;
            menu.bondsSelected = sprites.BondsSelected;
            menu.calendarNormal = sprites.CalendarNormal;
            menu.calendarSelected = sprites.CalendarSelected;
            menu.systemNormal = sprites.SystemNormal;
            menu.systemSelected = sprites.SystemSelected;
            menu.Rewire();
            rootGo.SetActive(false);
            return menu;
        }

        public void Rewire()
        {
            _wired = false;
            Wire();
            EnsureDiveButton();
        }

        public void EnsureSpritesAssigned()
        {
            var sprites = LoadSpritePack();
            if (backgroundImage != null && sprites.Background != null)
            {
                backgroundImage.sprite = sprites.Background;
            }

            ApplyHubButtonPlates(sprites);

            if (statsSelected != null && backgroundImage != null && backgroundImage.sprite != null
                && statsNormal != null)
            {
                if (confirmPromptIcon != null && confirmPromptIcon.sprite == null)
                {
                    confirmPromptIcon.sprite = sprites.ConfirmPrompt;
                }

                if (closePromptIcon != null && closePromptIcon.sprite == null)
                {
                    closePromptIcon.sprite = sprites.ClosePrompt;
                }

                return;
            }

            statsNormal ??= sprites.StatsNormal;
            statsSelected ??= sprites.StatsSelected;
            bondsNormal ??= sprites.BondsNormal;
            bondsSelected ??= sprites.BondsSelected;
            calendarNormal ??= sprites.CalendarNormal;
            calendarSelected ??= sprites.CalendarSelected;
            systemNormal ??= sprites.SystemNormal;
            systemSelected ??= sprites.SystemSelected;
            if (confirmPromptIcon != null && confirmPromptIcon.sprite == null)
            {
                confirmPromptIcon.sprite = sprites.ConfirmPrompt;
            }

            if (closePromptIcon != null && closePromptIcon.sprite == null)
            {
                closePromptIcon.sprite = sprites.ClosePrompt;
            }
        }

        private void Wire()
        {
            if (_wired)
            {
                return;
            }

            if (root == null)
            {
                root = gameObject;
            }

            if (statsButton == null && bondsButton == null && calendarButton == null && systemButton == null)
            {
                return;
            }

            BindTab(statsButton, Tab.Stats, openCharacterBuild: true);
            BindTab(bondsButton, Tab.Bonds, openBondsMenu: true);
            BindTab(calendarButton, Tab.Calendar, openCalendar: true);
            BindTab(systemButton, Tab.System, openSystemSubmenu: true);
            WireSystemSubmenuActions();

            _wired = true;
        }

        private void EnsureDiveButton()
        {
            var detail = transform.Find("DetailPanel");
            var parent = detail != null ? detail : transform;
            var body = parent.Find("DetailBody");
            if (body != null)
            {
                body.gameObject.SetActive(false);
            }

            diveButton = ResonanceDiveButton.Ensure(parent, OnResonanceDiveClicked, fillParent: true);
        }

        private void OnResonanceDiveClicked()
        {
            if (UiCancelInput.WasPressed())
            {
                return;
            }

            sfx?.PlaySelect();
            diveButton?.SetListening(false);
            HubNavigationEscContext.SetReturnToStatusMenu(Tab.Stats);
            Hide();
            if (!RunMapSceneLoader.LoadRunMapPrototype())
            {
                HubNavigationEscContext.Clear();
                Debug.LogError("[Fractured Chorus] Không load được RunMapPrototype từ Status Menu.");
                Show(_state ?? GameMetaSession.Current);
            }
        }

        private void BindTab(
            Button button,
            Tab tab,
            bool openCalendar = false,
            bool openSocialStats = false,
            bool openBondsMenu = false,
            bool openSystemSubmenu = false,
            bool openCharacterBuild = false)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (UiCancelInput.WasPressed())
                {
                    return;
                }

                _tab = tab;
                sfx?.PlaySelect();
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }

                Refresh();
                if (openCalendar)
                {
                    OpenCalendarOverlay();
                }
                else if (openCharacterBuild)
                {
                    OpenCharacterBuild();
                }
                else if (openBondsMenu)
                {
                    OpenBonds();
                }
                else if (openSocialStats)
                {
                    OpenSocialStatsOverlay();
                }
                else if (openSystemSubmenu)
                {
                    EnterSystemSubmenu();
                }
            });
        }

        private void EnterSystemSubmenu()
        {
            EnsureSystemSubmenuBindings();
            _systemSubmenuOpen = true;
            _tab = Tab.System;
            SetMenuListsVisible(mainVisible: false, systemVisible: true);
            WireSystemSubmenuActions();
            Refresh();
        }

        private void ExitSystemSubmenu()
        {
            if (!_systemSubmenuOpen)
            {
                return;
            }

            _systemSubmenuOpen = false;
            SetMenuListsVisible(mainVisible: true, systemVisible: false);

            _wired = false;
            Wire();
            if (_tab == Tab.System)
            {
                _tab = Tab.Stats;
            }

            Refresh();
        }

        private void SetMenuListsVisible(bool mainVisible, bool systemVisible)
        {
            if (menuListRoot != null)
            {
                menuListRoot.SetActive(mainVisible);
            }

            if (systemMenuListRoot != null)
            {
                systemMenuListRoot.SetActive(systemVisible);
            }
        }

        private void WireSystemSubmenuActions()
        {
            BindAction(saveButton, OpenSaveSlots);
            BindAction(loadButton, OpenLoadSlots);
            BindAction(configButton, OpenConfig);
            BindAction(returnToTitleButton, AskReturnToTitle);
        }

        private void BindAction(Button button, Action action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (UiCancelInput.WasPressed())
                {
                    return;
                }

                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }

                sfx?.PlaySelect();
                action?.Invoke();
            });
        }

        private void ReturnToSystemSubmenu(GameMetaState state)
        {
            _state = state;
            EnsureSpritesAssigned();
            EnsureSystemSubmenuBindings();

            if (root != null)
            {
                root.SetActive(true);
            }

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            if (!_systemSubmenuOpen)
            {
                EnterSystemSubmenu();
                return;
            }

            SetMenuListsVisible(mainVisible: false, systemVisible: true);
            WireSystemSubmenuActions();
            Refresh();
        }

        private void EnsureSystemSubmenuBindings()
        {
            if (menuListRoot == null && root != null)
            {
                menuListRoot = root.transform.Find("MenuList")?.gameObject;
            }

            if (systemMenuListRoot == null && root != null)
            {
                systemMenuListRoot = root.transform.Find("SystemMenuList")?.gameObject;
            }

            if (systemMenuListRoot != null)
            {
                if (saveButton == null)
                {
                    saveButton = systemMenuListRoot.transform.Find("BtnSave")?.GetComponent<Button>();
                    saveImage = saveButton != null ? saveButton.GetComponent<Image>() : null;
                }

                if (loadButton == null)
                {
                    loadButton = systemMenuListRoot.transform.Find("BtnLoad")?.GetComponent<Button>();
                    loadImage = loadButton != null ? loadButton.GetComponent<Image>() : null;
                }

                if (configButton == null)
                {
                    configButton = systemMenuListRoot.transform.Find("BtnConfig")?.GetComponent<Button>();
                    configImage = configButton != null ? configButton.GetComponent<Image>() : null;
                }

                if (returnToTitleButton == null)
                {
                    returnToTitleButton = systemMenuListRoot.transform.Find("BtnReturnToTitle")?.GetComponent<Button>();
                    returnToTitleImage = returnToTitleButton != null ? returnToTitleButton.GetComponent<Image>() : null;
                }
            }

            if (hubConfigOverlay == null)
            {
                var host = transform.parent != null ? transform.parent : transform;
                hubConfigOverlay = host.GetComponentInChildren<HubConfigOverlayUI>(true);
            }

            if (systemMenuListRoot != null && !_systemSubmenuOpen)
            {
                systemMenuListRoot.SetActive(false);
            }
        }

        private void OpenCharacterBuild()
        {
            try
            {
                GameMetaSession.Save();
            }
            catch (System.Exception error)
            {
                Debug.LogError($"[StatusMenu] Failed to save before Character Build: {error}");
            }

            diveButton?.SetListening(false);
            HubNavigationEscContext.SetReturnToStatusMenu(Tab.Stats);
            Hide();
            CharacterBuildMenuUI.SetReturnScene(RunMapSceneCatalog.CampusHub);
            if (!RunMapSceneLoader.LoadByName(RunMapSceneCatalog.CharacterBuild))
            {
                Debug.LogError("[StatusMenu] Failed to open Character Build.");
                Show(_state ?? GameMetaSession.Current);
            }
        }

        private void OpenBonds()
        {
            try
            {
                GameMetaSession.Save();
            }
            catch (System.Exception error)
            {
                Debug.LogError($"[StatusMenu] Failed to save before Bonds: {error}");
            }

            diveButton?.SetListening(false);
            HubNavigationEscContext.SetReturnToStatusMenu(Tab.Bonds);
            Hide();
            BondsMenuUI.SetReturnScene(RunMapSceneCatalog.CampusHub);
            if (!RunMapSceneLoader.LoadByName(RunMapSceneCatalog.Bonds))
            {
                Debug.LogError("[StatusMenu] Failed to open Bonds.");
                Show(_state ?? GameMetaSession.Current);
            }
        }

        private void OpenSaveSlots()
        {
            var canvas = GetComponentInParent<Canvas>();
            var host = canvas != null
                ? canvas.transform
                : transform.parent != null ? transform.parent : transform;
            var state = _state ?? GameMetaSession.Current;

            SaveLoadSlotListView.Show(
                host,
                SaveLoadSlotListView.Mode.Save,
                onSave: GameMetaSession.SaveToSlot,
                onClosed: () => ReturnToSystemSubmenu(state),
                sessionActive: true);
            HideWithoutClosingSubmenu();
        }

        private void OpenLoadSlots()
        {
            var canvas = GetComponentInParent<Canvas>();
            var host = canvas != null
                ? canvas.transform
                : transform.parent != null ? transform.parent : transform;
            var state = _state ?? GameMetaSession.Current;

            SaveLoadSlotListView.Show(
                host,
                SaveLoadSlotListView.Mode.Load,
                onLoad: LoadGameFromSlot,
                onClosed: () => ReturnToSystemSubmenu(state),
                sessionActive: true);
            HideWithoutClosingSubmenu();
        }

        private void OpenConfig()
        {
            HubNavigationEscContext.SetReturnToSystemSubmenu();
            MainMenuConfigLaunch.OpenFromCampusHub();
        }

        private void AskReturnToTitle()
        {
            var canvas = GetComponentInParent<Canvas>();
            var host = canvas != null
                ? canvas.transform
                : transform.parent != null ? transform.parent : transform;
            var dialog = ConfirmDialogView.Ensure(host);
            if (dialog == null)
            {
                ReturnToTitle();
                return;
            }

            dialog.Ask(
                "RETURN TO TITLE?",
                "Any unsaved progress will be lost.",
                ReturnToTitle,
                null,
                "YES",
                "NO");
        }

        private void ReturnToTitle()
        {
            try
            {
                GameMetaSession.Replace(null);
            }
            catch (Exception error)
            {
                Debug.LogError($"[StatusMenu] Failed to clear session before title: {error}");
            }

            MainMenuConfigLaunch.Clear();
            TownMapView.OpenStatusMenuOnNextShow = false;
            TownMapView.OpenSystemSubmenuOnNextShow = false;

            if (!RunMapSceneLoader.LoadByName(RunMapSceneCatalog.MainMenuStartGame))
            {
                Debug.LogError("[StatusMenu] Failed to return to Main Menu.");
            }
        }

        private void HideWithoutClosingSubmenu()
        {
            if (calendarOverlay != null && calendarOverlay.IsOpen)
            {
                calendarOverlay.Hide();
            }

            if (socialStatsOverlay != null && socialStatsOverlay.IsOpen)
            {
                socialStatsOverlay.Hide();
            }

            if (IsOpen)
            {
                sfx?.PlayClosePanel();
            }

            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private static void LoadGameFromSlot(int slot)
        {
            try
            {
                GameMetaSession.LoadSlot(slot);
                var sceneName = ResolveLoadScene(GameMetaSession.Current);
                if (!RunMapSceneLoader.LoadByName(sceneName))
                {
                    Debug.LogError($"[StatusMenu] Failed to load scene after slot {slot + 1:00}.");
                }
            }
            catch (System.Exception error)
            {
                Debug.LogError($"[StatusMenu] Failed to load slot {slot + 1:00}: {error}");
            }
        }

        private static string ResolveLoadScene(GameMetaState state)
        {
            if (state == null)
            {
                return RunMapSceneCatalog.CampusHub;
            }

            if (state.RunSnapshot.HasActiveRun)
            {
                return RunMapSceneCatalog.RunMapPrototype;
            }

            if (state.HasFlag(StoryFlagIds.OpeningInvestigationDone) || state.HasFlag(StoryFlagIds.RenArrivedHima))
            {
                return RunMapSceneCatalog.CampusHub;
            }

            return RunMapSceneCatalog.PrologueVN;
        }

        private void OpenCalendarOverlay()
        {
            var host = transform.parent != null ? transform.parent : transform;
            EnsureCalendarOverlay(host);
            if (calendarOverlay == null)
            {
                return;
            }

            calendarOverlay.transform.SetAsLastSibling();
            calendarOverlay.BindSfx(sfx);
            calendarOverlay.Show(_state ?? GameMetaSession.Current);
        }

        private void OpenSocialStatsOverlay()
        {
            var host = transform.parent != null ? transform.parent : transform;
            EnsureSocialStatsOverlay(host);
            if (socialStatsOverlay == null)
            {
                return;
            }

            socialStatsOverlay.transform.SetAsLastSibling();
            socialStatsOverlay.BindSfx(sfx);
            socialStatsOverlay.Show(_state ?? GameMetaSession.Current);
        }

        private void Refresh()
        {
            if (cornerInfoHud != null && _state != null)
            {
                cornerInfoHud.Refresh(_state);
            }
            else if (dateChipLabel != null && _state != null)
            {
                dateChipLabel.text = $"{_state.Calendar.CurrentDate.ToDisplayString()}  ·  {_state.Calendar.CurrentPhase}";
            }

            if (tooltipLabel != null)
            {
                if (_systemSubmenuOpen)
                {
                    tooltipLabel.text = "Save · Load · Config · Title";
                }
                else
                {
                    tooltipLabel.text = _tab switch
                    {
                        Tab.Stats => "Open Character Build",
                        Tab.Bonds => "Open Bonds",
                        Tab.Calendar => "Open Calendar",
                        Tab.System => "Save · Load · Config · Title",
                        _ => string.Empty
                    };
                }
            }

            if (_systemSubmenuOpen)
            {
                ApplyRow(saveImage, statsNormal, statsSelected, false);
                ApplyRow(loadImage, bondsNormal, bondsSelected, false);
                ApplyRow(configImage, calendarNormal, calendarSelected, false);
                ApplyRow(returnToTitleImage, systemNormal, systemSelected, false);
                ApplyHubButtonPlates(null);
            }
            else
            {
                ApplyRow(statsImage, statsNormal, statsSelected, false);
                ApplyRow(bondsImage, bondsNormal, bondsSelected, false);
                ApplyRow(calendarImage, calendarNormal, calendarSelected, false);
                ApplyRow(systemImage, systemNormal, systemSelected, false);
                ApplyHubButtonPlates(null);
            }
        }

        private void ApplyHubButtonPlates(SpritePack sprites)
        {
            var normal = LoadHubPlate("ui_hub_menu_btn_normal");
            var hover = LoadHubPlate("ui_hub_menu_btn_hover");
            if (normal == null && sprites != null)
            {
                normal = sprites.StatsNormal;
            }

            if (hover == null && sprites != null)
            {
                hover = sprites.StatsSelected;
            }

            if (normal == null || hover == null)
            {
                return;
            }

            statsNormal = normal;
            statsSelected = hover;
            bondsNormal = normal;
            bondsSelected = hover;
            calendarNormal = normal;
            calendarSelected = hover;
            systemNormal = normal;
            systemSelected = hover;

            BindRow(statsButton, statsImage, normal, hover);
            BindRow(bondsButton, bondsImage, normal, hover);
            BindRow(calendarButton, calendarImage, normal, hover);
            BindRow(systemButton, systemImage, normal, hover);
            BindRow(saveButton, saveImage, normal, hover);
            BindRow(loadButton, loadImage, normal, hover);
            BindRow(configButton, configImage, normal, hover);
            BindRow(returnToTitleButton, returnToTitleImage, normal, hover);

            EnsureRowIcon(statsButton, "ui_hub_menu_icon_stats");
            EnsureRowIcon(bondsButton, "ui_hub_menu_icon_bonds");
            EnsureRowIcon(calendarButton, "ui_hub_menu_icon_calendar");
            EnsureRowIcon(systemButton, "ui_hub_menu_icon_system");
            EnsureRowIcon(saveButton, "ui_hub_menu_icon_save");
            EnsureRowIcon(loadButton, "ui_hub_menu_icon_load");
            EnsureRowIcon(configButton, "ui_hub_menu_icon_config");
            EnsureRowIcon(returnToTitleButton, "ui_hub_menu_icon_title");

            var edgeFx = LoadHubPlate("ui_hub_menu_btn_edge_fx");
            EnsureTailFx(statsButton, edgeFx);
            EnsureTailFx(bondsButton, edgeFx);
            EnsureTailFx(calendarButton, edgeFx);
            EnsureTailFx(systemButton, edgeFx);
            EnsureTailFx(saveButton, edgeFx);
            EnsureTailFx(loadButton, edgeFx);
            EnsureTailFx(configButton, edgeFx);
            EnsureTailFx(returnToTitleButton, edgeFx);
        }

        private static void EnsureTailFx(Button button, Sprite edgeFx)
        {
            if (button == null || edgeFx == null)
            {
                return;
            }

            HubMenuButtonTailFx.Ensure(button, edgeFx);
        }

        private static void BindRow(Button button, Image image, Sprite normal, Sprite hover)
        {
            if (image != null)
            {
                image.sprite = normal;
                image.color = Color.white;
                image.preserveAspect = true;
                image.type = Image.Type.Simple;
            }

            if (button != null)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                var state = button.spriteState;
                state.highlightedSprite = hover;
                state.pressedSprite = hover;
                state.selectedSprite = normal;
                button.spriteState = state;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
            }
        }

        private static void EnsureRowLabel(Transform row, string caption)
        {
            var existing = row.Find("Label");
            if (existing != null)
            {
                var current = existing.GetComponent<Text>();
                if (current != null)
                {
                    current.text = caption;
                }

                return;
            }

            var label = CreateText(row, "Label", caption, 28, TextAnchor.MiddleLeft);
            label.fontStyle = FontStyle.Bold | FontStyle.Italic;
            label.color = new Color(0.08f, 0.1f, 0.22f, 1f);
            label.raycastTarget = false;
            Stretch(label.rectTransform, new Vector2(0.22f, 0.16f), new Vector2(0.86f, 0.84f), Vector2.zero, Vector2.zero);
        }

        private static void ApplyRow(Image image, Sprite normal, Sprite selected, bool isSelected)
        {
            if (image == null)
            {
                return;
            }

            var sprite = isSelected ? selected : normal;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
                image.preserveAspect = true;
            }
        }

        private static (Button Button, Image Image) CreateMenuRow(Transform parent, string name, Sprite sprite, string caption, int index)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            var yMax = 1f - (index * 0.22f);
            var yMin = yMax - 0.2f;
            var xShift = index * 0.04f;
            Stretch(rect, new Vector2(0.05f + xShift, yMin), new Vector2(0.98f, yMax), Vector2.zero, Vector2.zero);
            rect.localEulerAngles = new Vector3(0f, 0f, -4f - (index * 0.5f));

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            button.colors = colors;
            if (sprite != null)
            {
                var hover = LoadHubPlate("ui_hub_menu_btn_hover");
                if (hover != null)
                {
                    button.transition = Selectable.Transition.SpriteSwap;
                    var state = button.spriteState;
                    state.highlightedSprite = hover;
                    state.pressedSprite = hover;
                    state.selectedSprite = sprite;
                    button.spriteState = state;
                }
            }

            button.navigation = new Navigation { mode = Navigation.Mode.None };
            EnsureRowLabel(go.transform, caption);
            EnsureRowIcon(button, IconResourceForRow(name));
            var edgeFx = LoadHubPlate("ui_hub_menu_btn_edge_fx");
            HubMenuButtonTailFx.Ensure(button, edgeFx);
            return (button, image);
        }

        private static string IconResourceForRow(string rowName)
        {
            switch (rowName)
            {
                case "BtnStats":
                    return "ui_hub_menu_icon_stats";
                case "BtnBonds":
                    return "ui_hub_menu_icon_bonds";
                case "BtnCalendar":
                    return "ui_hub_menu_icon_calendar";
                case "BtnSystem":
                    return "ui_hub_menu_icon_system";
                case "BtnSave":
                    return "ui_hub_menu_icon_save";
                case "BtnLoad":
                    return "ui_hub_menu_icon_load";
                case "BtnConfig":
                    return "ui_hub_menu_icon_config";
                case "BtnReturnToTitle":
                    return "ui_hub_menu_icon_title";
                default:
                    return null;
            }
        }

        private static void EnsureRowIcon(Button button, string resourceName)
        {
            if (button == null || string.IsNullOrEmpty(resourceName))
            {
                return;
            }

            var sprite = LoadHubPlate(resourceName);
            var existing = button.transform.Find("Icon");
            if (existing != null)
            {
                var image = existing.GetComponent<Image>();
                if (image != null && sprite != null)
                {
                    image.sprite = sprite;
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                    image.color = Color.white;
                }

                return;
            }

            var go = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(button.transform, false);
            var rect = go.GetComponent<RectTransform>();
            Stretch(rect, new Vector2(0.02f, 0.1f), new Vector2(0.24f, 0.9f), Vector2.zero, Vector2.zero);
            var icon = go.GetComponent<Image>();
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = Color.white;
            icon.type = Image.Type.Simple;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            return image;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            UiFontCatalog.ApplyAutomatic(text);
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private sealed class SpritePack
        {
            public Sprite Background;
            public Sprite StatsNormal;
            public Sprite StatsSelected;
            public Sprite BondsNormal;
            public Sprite BondsSelected;
            public Sprite CalendarNormal;
            public Sprite CalendarSelected;
            public Sprite SystemNormal;
            public Sprite SystemSelected;
            public Sprite ConfirmPrompt;
            public Sprite ClosePrompt;
        }

        private static SpritePack LoadSpritePack()
        {
            var plateNormal = LoadHubPlate("ui_hub_menu_btn_normal");
            var plateHover = LoadHubPlate("ui_hub_menu_btn_hover");
            return new SpritePack
            {
                Background = LoadSprite("statusmenu_hima_city_bg_v1"),
                StatsNormal = plateNormal ?? LoadSprite("statusmenu_btn_stats_normal"),
                StatsSelected = plateHover ?? LoadSprite("statusmenu_btn_stats_selected"),
                BondsNormal = plateNormal ?? LoadSprite("statusmenu_btn_bonds_normal"),
                BondsSelected = plateHover ?? LoadSprite("statusmenu_btn_bonds_selected"),
                CalendarNormal = plateNormal ?? LoadSprite("statusmenu_btn_calendar_normal"),
                CalendarSelected = plateHover ?? LoadSprite("statusmenu_btn_calendar_selected"),
                SystemNormal = plateNormal ?? LoadSprite("statusmenu_btn_system_normal"),
                SystemSelected = plateHover ?? LoadSprite("statusmenu_btn_system_selected"),
                ConfirmPrompt = LoadSprite("statusmenu_prompt_confirm"),
                ClosePrompt = LoadSprite("statusmenu_prompt_close")
            };
        }

        private static Sprite LoadHubPlate(string fileNameNoExt)
        {
            var fromResources = Resources.Load<Sprite>($"UI/HubMenu/{fileNameNoExt}");
            if (fromResources != null)
            {
                return fromResources;
            }

            var all = Resources.LoadAll<Sprite>($"UI/HubMenu/{fileNameNoExt}");
            if (all != null && all.Length > 0)
            {
                return all[0];
            }

#if UNITY_EDITOR
            var artPath = $"Assets/FracturedChorus/Art/UI/HubMenu/{fileNameNoExt}.png";
            EnsureSpriteImporter(artPath);
            var editorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
            if (editorSprite != null)
            {
                return editorSprite;
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(artPath);
            foreach (var asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    return sprite;
                }
            }
#endif
            return null;
        }

        private static Sprite LoadSprite(string fileNameNoExt)
        {
            var fromResources = Resources.Load<Sprite>($"UI/StatusMenu/{fileNameNoExt}");
            if (fromResources != null)
            {
                return fromResources;
            }

            var all = Resources.LoadAll<Sprite>($"UI/StatusMenu/{fileNameNoExt}");
            if (all != null && all.Length > 0)
            {
                return all[0];
            }

#if UNITY_EDITOR
            var artPath = $"Assets/FracturedChorus/Art/UI/StatusMenu/{fileNameNoExt}.png";
            var jpgPath = $"Assets/FracturedChorus/Art/UI/StatusMenu/{fileNameNoExt}.jpg";
            if (!System.IO.File.Exists(artPath) && System.IO.File.Exists(jpgPath))
            {
                artPath = jpgPath;
            }

            EnsureSpriteImporter(artPath);
            var editorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
            if (editorSprite != null)
            {
                return editorSprite;
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(artPath);
            foreach (var asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    return sprite;
                }
            }
#endif
            return null;
        }

#if UNITY_EDITOR
        private static void EnsureSpriteImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            var dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }
#endif
    }
}

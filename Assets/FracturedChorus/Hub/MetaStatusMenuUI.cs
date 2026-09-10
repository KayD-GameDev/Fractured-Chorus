using System.Collections.Generic;
using System.Text;
using FracturedChorus.Hub.CharacterBuild;
using FracturedChorus.Menu;
using FracturedChorus.Meta;
using FracturedChorus.RunMap;
using FracturedChorus.Meta.Economy;
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
        [SerializeField] private Text tooltipLabel;
        [SerializeField] private Text detailBodyLabel;
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

        private void Awake()
        {
            Wire();
            if (root != null)
            {
                root.SetActive(false);
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
            if (_tab == Tab.System && WasHealHotkeyPressed())
            {
                var hub = Object.FindAnyObjectByType<CampusHubController>();
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

        public void BindSfx(TownMapSfxController controller)
        {
            sfx = controller;
            calendarOverlay?.BindSfx(controller);
            socialStatsOverlay?.BindSfx(controller);
        }

        public void Show(GameMetaState state, Tab tab = Tab.Stats)
        {
            EnsureSpritesAssigned();
            Wire();
            _state = state;
            _tab = tab == Tab.System ? Tab.Stats : tab;
            if (root != null)
            {
                root.SetActive(true);
            }

            sfx?.PlayOpenPanel();
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            Refresh();
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

            if (IsOpen)
            {
                sfx?.PlayClosePanel();
            }

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

            var detail = new GameObject("DetailPanel", typeof(RectTransform), typeof(Image));
            detail.transform.SetParent(rootGo.transform, false);
            Stretch(detail.GetComponent<RectTransform>(), new Vector2(0.08f, 0.08f), new Vector2(0.48f, 0.42f), Vector2.zero, Vector2.zero);
            var detailBg = detail.GetComponent<Image>();
            detailBg.color = FcColorTokens.Surface.Detail;
            detailBg.raycastTarget = false;

            var detailBody = CreateText(detail.transform, "DetailBody", string.Empty, 20, TextAnchor.UpperLeft);
            Stretch(detailBody.rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f), Vector2.zero, Vector2.zero);
            detailBody.color = new Color(0.85f, 0.95f, 1f);
            detailBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            detailBody.verticalOverflow = VerticalWrapMode.Overflow;

            var tooltip = CreateText(rootGo.transform, "Tooltip", "View Social Stats", 18, TextAnchor.MiddleRight);
            Stretch(tooltip.rectTransform, new Vector2(0.55f, 0.08f), new Vector2(0.92f, 0.14f), Vector2.zero, Vector2.zero);
            tooltip.color = FcColorTokens.Brand.Cyan;
            tooltip.fontStyle = FontStyle.Italic;

            var prompts = new GameObject("Prompts", typeof(RectTransform));
            prompts.transform.SetParent(rootGo.transform, false);
            Stretch(prompts.GetComponent<RectTransform>(), new Vector2(0.72f, 0.02f), new Vector2(0.98f, 0.08f), Vector2.zero, Vector2.zero);

            var confirmIcon = CreateImage(prompts.transform, "ConfirmIcon", sprites.ConfirmPrompt);
            Stretch(confirmIcon.rectTransform, new Vector2(0f, 0.1f), new Vector2(0.22f, 0.9f), Vector2.zero, Vector2.zero);
            var confirmText = CreateText(prompts.transform, "ConfirmText", "Confirm", 16, TextAnchor.MiddleLeft);
            Stretch(confirmText.rectTransform, new Vector2(0.22f, 0f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            confirmText.color = Color.white;

            var closeIcon = CreateImage(prompts.transform, "CloseIcon", sprites.ClosePrompt);
            Stretch(closeIcon.rectTransform, new Vector2(0.52f, 0.1f), new Vector2(0.74f, 0.9f), Vector2.zero, Vector2.zero);
            var closeText = CreateText(prompts.transform, "CloseText", "Close", 16, TextAnchor.MiddleLeft);
            Stretch(closeText.rectTransform, new Vector2(0.74f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            closeText.color = Color.white;

            var menu = rootGo.AddComponent<MetaStatusMenuUI>();
            menu.root = rootGo;
            menu.backgroundImage = bg;
            menu.dateChipLabel = dateChip;
            menu.tooltipLabel = tooltip;
            menu.detailBodyLabel = detailBody;
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
            BindTab(bondsButton, Tab.Bonds, openSocialStats: true);
            BindTab(calendarButton, Tab.Calendar, openCalendar: true);
            BindTab(systemButton, Tab.System, openSaveSlots: true);

            _wired = true;
        }

        private void BindTab(
            Button button,
            Tab tab,
            bool openCalendar = false,
            bool openSocialStats = false,
            bool openSaveSlots = false,
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
                Refresh();
                if (openCalendar)
                {
                    OpenCalendarOverlay();
                }
                else if (openCharacterBuild)
                {
                    OpenCharacterBuild();
                }
                else if (openSocialStats)
                {
                    OpenSocialStatsOverlay();
                }
                else if (openSaveSlots)
                {
                    OpenSaveSlots();
                }
            });
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

            CharacterBuildMenuUI.SetReturnScene(RunMapSceneCatalog.CampusHub);
            if (!RunMapSceneLoader.LoadByName(RunMapSceneCatalog.CharacterBuild))
            {
                Debug.LogError("[StatusMenu] Failed to open Character Build.");
            }
        }

        private void OpenSaveSlots()
        {
            var canvas = GetComponentInParent<Canvas>();
            var host = canvas != null
                ? canvas.transform
                : transform.parent != null ? transform.parent : transform;
            var state = _state ?? GameMetaSession.Current;

            // Mở panel save trước rồi mới ẩn status menu — một lần ESC đóng slot list,
            // lần sau đóng status. Session đang chơi nên tab SAVE luôn bật.
            SaveLoadSlotListView.Show(
                host,
                SaveLoadSlotListView.Mode.Save,
                onSave: GameMetaSession.SaveToSlot,
                onClosed: () => Show(state),
                sessionActive: true);
            Hide();
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
            if (dateChipLabel != null && _state != null)
            {
                dateChipLabel.text = $"{_state.Calendar.CurrentDate.ToDisplayString()}  ·  {_state.Calendar.CurrentPhase}";
            }

            if (tooltipLabel != null)
            {
                tooltipLabel.text = _tab switch
                {
                    Tab.Stats => "Open Character Build",
                    Tab.Bonds => "View Social Stats",
                    Tab.Calendar => "Open Calendar",
                    Tab.System => "Save Game",
                    _ => string.Empty
                };
            }

            if (detailBodyLabel != null)
            {
                if (_tab == Tab.Calendar)
                {
                    detailBodyLabel.text = "Opening calendar…";
                }
                else if (_tab == Tab.Bonds && socialStatsOverlay != null && socialStatsOverlay.IsOpen)
                {
                    detailBodyLabel.text = "Opening Resonance Field…";
                }
                else
                {
                    detailBodyLabel.text = _state == null ? "No save loaded." : BuildBody(_state, _tab);
                }
            }

            ApplyRow(statsImage, statsNormal, statsSelected, _tab == Tab.Stats);
            ApplyRow(bondsImage, bondsNormal, bondsSelected, _tab == Tab.Bonds);
            ApplyRow(calendarImage, calendarNormal, calendarSelected, _tab == Tab.Calendar);
            ApplyRow(systemImage, systemNormal, systemSelected, _tab == Tab.System);
            ApplyHubButtonPlates(null);
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

            BindRow(statsButton, statsImage, normal, hover, _tab == Tab.Stats);
            BindRow(bondsButton, bondsImage, normal, hover, _tab == Tab.Bonds);
            BindRow(calendarButton, calendarImage, normal, hover, _tab == Tab.Calendar);
            BindRow(systemButton, systemImage, normal, hover, _tab == Tab.System);
        }

        private static void BindRow(Button button, Image image, Sprite normal, Sprite hover, bool isSelected)
        {
            if (image != null)
            {
                image.sprite = isSelected ? hover : normal;
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
                state.selectedSprite = hover;
                button.spriteState = state;
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

        private static string BuildBody(GameMetaState state, Tab tab)
        {
            return tab switch
            {
                Tab.Calendar => BuildCalendar(state),
                Tab.Stats => BuildStats(state),
                Tab.Bonds => BuildBonds(state),
                Tab.System => BuildSystem(state),
                _ => string.Empty
            };
        }

        private static string BuildSystem(GameMetaState state)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Slot {GameMetaSession.ActiveSlotIndex + 1:00}");
            sb.AppendLine($"Notes: {state.Wallet.Notes}");
            sb.AppendLine($"Difficulty: {state.Difficulty}");
            sb.AppendLine("Select System to open save slots.");
            sb.AppendLine($"Press H — clinic heal (−{EconomyTable.HubHealCost} Notes).");
            return sb.ToString();
        }

        private static string BuildCalendar(GameMetaState state)
        {
            var c = state.Calendar;
            var sb = new StringBuilder();
            sb.AppendLine($"Date: {c.CurrentDate.ToDisplayString()}");
            sb.AppendLine($"Phase: {c.CurrentPhase}");
            sb.AppendLine($"Slots: {c.SlotsUsedToday}/{CalendarState.MaxSlotsPerDay}");
            sb.AppendLine($"Morning quiz: {(c.MorningQuizDone ? "Done" : "Pending")}");
            sb.AppendLine($"Days to vault deadline: {c.DaysUntilVaultDeadline}");
            sb.AppendLine($"Vault quest: {(state.HasFlag(StoryFlagIds.VaultQuestActive) ? "Active" : "Inactive")}");
            if (state.HasFlag(StoryFlagIds.VaultClearedOnTime))
            {
                sb.AppendLine("Vault: Cleared on time");
            }
            else if (state.HasFlag(StoryFlagIds.VaultMissedDeadline))
            {
                sb.AppendLine("Vault: Missed deadline");
            }

            return sb.ToString();
        }

        private static string BuildStats(GameMetaState state)
        {
            var sb = new StringBuilder();
            foreach (SocialStatType stat in System.Enum.GetValues(typeof(SocialStatType)))
            {
                var rank = state.SocialStats.GetRank(stat);
                var exp = state.SocialStats.GetExp(stat);
                var need = state.SocialStats.GetThresholdForRank(rank);
                sb.AppendLine($"{stat}: Rank {rank}  ·  EXP {exp}/{need}");
            }

            return sb.ToString();
        }

        private static string BuildBonds(GameMetaState state)
        {
            var sb = new StringBuilder();
            var order = new[]
            {
                BondNpcIds.Ren,
                BondNpcIds.Charlotte,
                BondNpcIds.Coda,
                BondNpcIds.Astra,
                BondNpcIds.Ryo,
                BondNpcIds.MeiLin
            };

            foreach (var npcId in order)
            {
                var bond = state.GetBond(npcId);
                var lockText = bond.IsLocked ? " [LOCKED]" : string.Empty;
                sb.AppendLine(
                    $"{DisplayNpc(npcId)} · {bond.EchoKey}  R{bond.Rank}/{bond.ArcCap}  EXP {bond.Exp}{lockText}");
            }

            return sb.ToString();
        }

        private static string DisplayNpc(string npcId) => npcId switch
        {
            BondNpcIds.MeiLin => "Mei Lin",
            BondNpcIds.Ren => "Ren",
            BondNpcIds.Charlotte => "Charlotte",
            BondNpcIds.Coda => "Coda",
            BondNpcIds.Ryo => "Ryo",
            BondNpcIds.Astra => "Astra",
            _ => npcId
        };

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
                    state.selectedSprite = hover;
                    button.spriteState = state;
                }
            }

            EnsureRowLabel(go.transform, caption);
            return (button, image);
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

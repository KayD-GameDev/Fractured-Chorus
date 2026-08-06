using System.Collections.Generic;
using System.Text;
using FracturedChorus.Menu;
using FracturedChorus.Meta;
using FracturedChorus.Meta.Economy;
using FracturedChorus.UI;
using UnityEngine;
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
        [SerializeField] private PartyStatusMenuUI partyStatusMenu;
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

            if (partyStatusMenu != null && partyStatusMenu.IsOpen)
            {
                return;
            }

            if (TownMapInput.CancelPressed())
            {
                Hide();
            }

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

        public bool IsPartyStatusOpen => partyStatusMenu != null && partyStatusMenu.IsOpen;

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

            if (partyStatusMenu != null && partyStatusMenu.IsOpen)
            {
                partyStatusMenu.Hide();
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

            if (existingMenuTf != null)
            {
                var needsRebuild = existingMenuTf.Find("MenuList") == null;
                if (!needsRebuild)
                {
                    var bg = existingMenuTf.Find("Background")?.GetComponent<Image>();
                    needsRebuild = bg == null || bg.sprite == null || !bg.sprite.name.Contains("v6");
                }

                if (needsRebuild)
                {
                    existingMenuTf.gameObject.name = "StatusMenu_OLD";
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(existingMenuTf.gameObject);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(existingMenuTf.gameObject);
                    }

                    existingMenuTf = null;
                }
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
                menu.EnsurePartyStatusMenu(parent);
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
            menu.EnsurePartyStatusMenu(parent);
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

        public void EnsurePartyStatusMenu(Transform townMapRoot)
        {
            if (partyStatusMenu != null)
            {
                return;
            }

            partyStatusMenu = PartyStatusMenuUI.Ensure(townMapRoot);
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

            var stats = CreateMenuRow(listRoot.transform, "BtnStats", sprites.StatsNormal, 0);
            var bonds = CreateMenuRow(listRoot.transform, "BtnBonds", sprites.BondsNormal, 1);
            var calendar = CreateMenuRow(listRoot.transform, "BtnCalendar", sprites.CalendarNormal, 2);
            var system = CreateMenuRow(listRoot.transform, "BtnSystem", sprites.SystemNormal, 3);

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
            if (statsSelected != null && backgroundImage != null && backgroundImage.sprite != null)
            {
                return;
            }

            var sprites = LoadSpritePack();
            if (backgroundImage != null && backgroundImage.sprite == null)
            {
                backgroundImage.sprite = sprites.Background;
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

            BindTab(statsButton, Tab.Stats, openPartyStatus: true);
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
            bool openPartyStatus = false,
            bool openSaveSlots = false)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                _tab = tab;
                sfx?.PlaySelect();
                Refresh();
                if (openCalendar)
                {
                    OpenCalendarOverlay();
                }
                else if (openPartyStatus)
                {
                    OpenPartyStatusMenu();
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

        private void OpenPartyStatusMenu()
        {
            var host = transform.parent != null ? transform.parent : transform;
            EnsurePartyStatusMenu(host);
            if (partyStatusMenu == null)
            {
                return;
            }

            partyStatusMenu.transform.SetAsLastSibling();
            partyStatusMenu.Show(_state ?? GameMetaSession.Current);
        }

        private void OpenSaveSlots()
        {
            var host = transform.parent != null ? transform.parent : transform;
            SaveLoadSlotListView.Show(
                host,
                SaveLoadSlotListView.Mode.Save,
                onSave: slot => GameMetaSession.SaveToSlot(slot));
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
                    Tab.Stats => "View Party Status",
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
                else if (_tab == Tab.Stats && partyStatusMenu != null && partyStatusMenu.IsOpen)
                {
                    detailBodyLabel.text = "Opening party status…";
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
                image.color = isSelected ? FcColorTokens.Selection.TabIconTint : Color.white;
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

        private static (Button Button, Image Image) CreateMenuRow(Transform parent, string name, Sprite sprite, int index)
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
            return new SpritePack
            {
                Background = LoadSprite("statusmenu_ren_bg_v6"),
                StatsNormal = LoadSprite("statusmenu_btn_stats_normal"),
                StatsSelected = LoadSprite("statusmenu_btn_stats_selected"),
                BondsNormal = LoadSprite("statusmenu_btn_bonds_normal"),
                BondsSelected = LoadSprite("statusmenu_btn_bonds_selected"),
                CalendarNormal = LoadSprite("statusmenu_btn_calendar_normal"),
                CalendarSelected = LoadSprite("statusmenu_btn_calendar_selected"),
                SystemNormal = LoadSprite("statusmenu_btn_system_normal"),
                SystemSelected = LoadSprite("statusmenu_btn_system_selected"),
                ConfirmPrompt = LoadSprite("statusmenu_prompt_confirm"),
                ClosePrompt = LoadSprite("statusmenu_prompt_close")
            };
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

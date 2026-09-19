using System;
using FracturedChorus.Meta;
using FracturedChorus.RunMap;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class BondsMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private HubCornerInfoHud cornerHud;
        [SerializeField] private SocialStatsRadarGraphic radar;
        [SerializeField] private SocialStatsNodeView[] nodes = new SocialStatsNodeView[5];
        [SerializeField] private Sprite[] statIcons = new Sprite[5];
        [SerializeField] private BondRosterChipView[] chips = new BondRosterChipView[0];
        [SerializeField] private Button rosterChevron;
        [SerializeField] private Button rosterChevronPrev;
        [SerializeField] private Sprite chipFrameNormal;
        [SerializeField] private Sprite chipFrameSelected;
        [SerializeField] private Sprite chipFrameLocked;
        [SerializeField] private Sprite lockIcon;
        [SerializeField] private Sprite[] portraitSprites = new Sprite[6];
        [SerializeField] private Sprite reservedPortrait;
        [SerializeField] private Sprite storyHiddenPortrait;
        [SerializeField] private BondDetailCardView detail;
        [SerializeField] private BondEpisodeRowView[] episodeRows = new BondEpisodeRowView[5];
        [SerializeField] private BondNavRowView[] navRows = new BondNavRowView[5];
        [SerializeField] private Sprite navPlateNormal;
        [SerializeField] private Sprite navPlateSelected;
        [SerializeField] private Text socialStatsTitle;
        [SerializeField] private Text socialStatsJp;
        [SerializeField] private Text linkTitle;
        [SerializeField] private Text linkJp;
        [SerializeField] private Text episodeHint;
        [SerializeField] private Text headerLabel;
        [SerializeField] private Text headerJp;
        [SerializeField] private Text confirmLabel;
        [SerializeField] private Text backLabel;
        [SerializeField] private Image promoImage;

        private GameMetaState _state;
        private int _rosterIndex = 1;
        private int _pageOffset;
        private int _episodeIndex;
        private int _navIndex = 1;
        private bool _buttonsBound;
        private bool _navButtonsBound;
        private bool _chevronBound;

        private static string _returnScene = RunMapSceneCatalog.CampusHub;

        public string SelectedNpcId => NpcIdAt(_rosterIndex);

        public static int WrapRosterIndex(int index, int delta, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            var wrapped = (index + delta) % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EnsurePromoImage();
            RefreshPromoPreviewInEditMode();
        }

        private void RefreshPromoPreviewInEditMode()
        {
            if (promoImage == null)
            {
                return;
            }

            var sprite = BondPromoSprites.Episode(BondNpcIds.Charlotte, 0)
                ?? BondPromoSprites.DefaultPromo
                ?? BondPromoSprites.CharlotteEpisode(0);
            promoImage.sprite = sprite;
            promoImage.enabled = sprite != null;
            promoImage.preserveAspect = true;
            promoImage.color = Color.white;
        }
#endif

        private void Start()
        {
            if (_state == null)
            {
                if (Application.isPlaying)
                {
                    _navIndex = -1;
                }

                Show(GameMetaSession.Current ?? CreateSandboxState());
            }
        }

        public static void SetReturnScene(string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                _returnScene = sceneName;
            }
        }

        public static void ReturnToCampusHub(bool openStatusMenu = true)
        {
            try
            {
                GameMetaSession.Save();
            }
            catch (Exception error)
            {
                Debug.LogError($"[BondsMenu] Failed to save before close: {error}");
            }

            var sceneName = string.IsNullOrWhiteSpace(_returnScene)
                ? RunMapSceneCatalog.CampusHub
                : _returnScene;
            var toCampus = sceneName == RunMapSceneCatalog.CampusHub;
            if (openStatusMenu && toCampus && !HubNavigationEscContext.HasPending)
            {
                HubNavigationEscContext.SetReturnToStatusMenu(MetaStatusMenuUI.Tab.Bonds);
            }

            if (toCampus)
            {
                HubNavigationEscContext.ApplyBeforeLoadCampusHub(forceTownMap: !openStatusMenu);
            }

            if (!RunMapSceneLoader.LoadByName(sceneName))
            {
                TownMapView.OpenStatusMenuOnNextShow = false;
                TownMapView.OpenStatusMenuTabOnNextShow = null;
                HubNavigationEscContext.Clear();
                Debug.LogError($"[BondsMenu] Failed to return to '{sceneName}'.");
            }
        }

        public bool TryHandleCancelInput()
        {
            if (_state == null || root == null || !root.activeInHierarchy)
            {
                return false;
            }

            if (_navIndex == -1)
            {
                SelectNav(BondNavIds.Link);
                return true;
            }

            if (_navIndex != BondNavIds.Link)
            {
                SelectNav(BondNavIds.Link);
                return true;
            }

            return false;
        }

        public void Show(GameMetaState state)
        {
            if (root == null)
            {
                root = gameObject;
            }

            _state = state ?? CreateSandboxState();
            root?.SetActive(true);
            EnsureNavRows();
            EnsureNavSprites();
            EnsureStatNodes();
            EnsureChipSlots();
            EnsureChevron();
            BindButtons();
            EnsureValidRosterSelection();
            EnsurePromoImage();
            EnsureStoryHiddenPortrait();
            _episodeIndex = Mathf.Clamp(_episodeIndex, 0, Mathf.Max(0, SelectedEpisodes().Length - 1));
            ApplyStaticCopy();
            cornerHud?.Refresh(_state);
            Refresh();
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void Update()
        {
            if (_state == null || root == null || !root.activeInHierarchy)
            {
                return;
            }

            if (!Application.isPlaying || !BondNavIds.IsImplemented(_navIndex) || _navIndex != BondNavIds.Link)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (TownMapInput.MonthNextPressed() || (keyboard != null && keyboard.rightArrowKey.wasPressedThisFrame))
            {
                MoveRoster(1);
            }
            else if (TownMapInput.MonthPrevPressed() || (keyboard != null && keyboard.leftArrowKey.wasPressedThisFrame))
            {
                MoveRoster(-1);
            }

            if (keyboard != null && keyboard.upArrowKey.wasPressedThisFrame)
            {
                MoveEpisode(-1);
            }
            else if (keyboard != null && keyboard.downArrowKey.wasPressedThisFrame)
            {
                MoveEpisode(1);
            }

            if (TownMapInput.ConfirmPressed())
            {
                ConfirmEpisode();
            }
        }

        private void BindButtons()
        {
            if (_buttonsBound)
            {
                return;
            }

            for (var i = 0; i < chips.Length; i++)
            {
                var chip = chips[i];
                if (chip?.Button == null)
                {
                    continue;
                }

                var slot = i;
                chip.Button.onClick.AddListener(() => SelectSlot(slot));
            }

            for (var i = 0; i < episodeRows.Length; i++)
            {
                var row = episodeRows[i];
                if (row?.Button == null)
                {
                    continue;
                }

                var episodeIndex = i;
                row.Button.onClick.AddListener(() => SelectEpisode(episodeIndex));
            }

            BindNavButtons();
            BindChevron();
            _buttonsBound = true;
        }

        private void BindChevron()
        {
            if (_chevronBound)
            {
                return;
            }

            if (rosterChevron != null)
            {
                rosterChevron.onClick.AddListener(() => PageRoster(1));
            }

            if (rosterChevronPrev != null)
            {
                rosterChevronPrev.onClick.AddListener(() => PageRoster(-1));
            }

            _chevronBound = rosterChevron != null || rosterChevronPrev != null;
        }

        private void BindNavButtons()
        {
            if (_navButtonsBound)
            {
                return;
            }

            EnsureNavRows();
            EnsureNavSprites();

            for (var i = 0; i < navRows.Length; i++)
            {
                var row = navRows[i];
                if (row?.Button == null)
                {
                    continue;
                }

                var navIndex = i;
                row.Button.onClick.AddListener(() => SelectNav(navIndex));
            }

            _navButtonsBound = navRows.Length > 0 && navRows[0] != null;
        }

        private void EnsureNavRows()
        {
            if (navRows != null && navRows.Length > 0 && navRows[0] != null)
            {
                return;
            }

            var leftNav = transform.Find("LeftNav");
            if (leftNav == null)
            {
                return;
            }

            var rowNames = new[]
            {
                "Row_SocialStats",
                "Row_Link",
                "Row_Conversations",
                "Row_Memories",
                "Row_Gallery"
            };

            navRows = new BondNavRowView[rowNames.Length];
            for (var i = 0; i < rowNames.Length; i++)
            {
                var rowTransform = leftNav.Find(rowNames[i]);
                if (rowTransform == null)
                {
                    continue;
                }

                var view = rowTransform.GetComponent<BondNavRowView>();
                if (view == null)
                {
                    view = rowTransform.gameObject.AddComponent<BondNavRowView>();
                }

                navRows[i] = view;
            }
        }

        private void EnsureNavSprites()
        {
            if (navPlateNormal == null)
            {
                navPlateNormal = BondsPackSprites.MenuNormal;
            }

            if (navPlateSelected == null)
            {
                navPlateSelected = BondsPackSprites.MenuSelected;
            }
        }

        private void EnsureStatNodes()
        {
            if (nodes != null && nodes.Length > 0 && nodes[0] != null)
            {
                return;
            }

            var center = transform.Find("CenterStats");
            if (center == null)
            {
                return;
            }

            var nodeNames = new[]
            {
                "Node_Resonance",
                "Node_Cadence",
                "Node_Pulse",
                "Node_Harmony",
                "Node_Rhythm"
            };

            nodes = new SocialStatsNodeView[nodeNames.Length];
            for (var i = 0; i < nodeNames.Length; i++)
            {
                var row = center.Find(nodeNames[i]);
                nodes[i] = row != null ? row.GetComponent<SocialStatsNodeView>() : null;
            }
        }

        private void SelectSlot(int slot)
        {
            SelectRoster(_pageOffset + slot);
        }

        private void SelectRoster(int index)
        {
            _rosterIndex = Mathf.Clamp(index, 0, Mathf.Max(0, BondPresentation.RosterEntryCount - 1));
            EnsureValidRosterSelection();
            Refresh();
        }

        private void PageRoster(int delta)
        {
            EnsureChipSlots();
            var slotCount = SlotCount();
            _pageOffset = BondPresentation.NextPageOffset(_pageOffset, slotCount, delta);
            if (_rosterIndex < _pageOffset || _rosterIndex >= _pageOffset + slotCount)
            {
                _rosterIndex = _pageOffset;
            }

            EnsureValidRosterSelection();
            Refresh();
        }

        private void SelectEpisode(int index)
        {
            _episodeIndex = Mathf.Clamp(index, 0, Mathf.Max(0, SelectedEpisodes().Length - 1));
            Refresh();
        }

        private void SelectNav(int index)
        {
            index = Mathf.Clamp(index, 0, Mathf.Max(0, navRows.Length - 1));
            if (Application.isPlaying && !BondNavIds.IsImplemented(index))
            {
                return;
            }

            if (Application.isPlaying && _navIndex == index)
            {
                DismissPanels();
                return;
            }

            _navIndex = index;
            Refresh();
        }

        private void DismissPanels()
        {
            _navIndex = -1;
            Refresh();
        }

        private void MoveRoster(int delta)
        {
            _rosterIndex = WrapRosterIndex(_rosterIndex, delta, BondPresentation.RosterEntryCount);
            Refresh();
        }

        private void MoveEpisode(int delta)
        {
            var count = SelectedEpisodes().Length;
            _episodeIndex = WrapRosterIndex(_episodeIndex, delta, count);
            Refresh();
        }

        private void ConfirmEpisode()
        {
            var episodes = SelectedEpisodes();
            if (_state == null || episodes.Length == 0)
            {
                return;
            }

            var episode = episodes[_episodeIndex];
            var bond = _state.GetBond(SelectedNpcId);
            if (!BondPresentation.IsBondRosterUnlocked(SelectedNpcId, bond)
                || !BondLinkEpisodeCatalog.IsUnlocked(bond.Rank, episode.RequiredRank))
            {
                return;
            }

            Debug.Log($"[Bonds sandbox] play {SelectedNpcId} episode {episode.Index} {episode.Title}");
        }

        private void EnsureValidRosterSelection()
        {
            _rosterIndex = Mathf.Clamp(_rosterIndex, 0, Mathf.Max(0, BondPresentation.RosterEntryCount - 1));
        }

        private int SlotCount()
        {
            return chips != null ? chips.Length : 0;
        }

        private void EnsureChipSlots()
        {
            var roster = transform.Find("CenterStats/Roster");
            if (roster == null)
            {
                return;
            }

            var found = new BondRosterChipView[BondPresentation.VisibleChipSlots];
            var count = 0;
            for (var i = 0; i < BondPresentation.VisibleChipSlots; i++)
            {
                var root = roster.Find("Chip_" + i);
                if (root == null)
                {
                    continue;
                }

                var view = root.GetComponent<BondRosterChipView>();
                if (view == null)
                {
                    view = root.gameObject.AddComponent<BondRosterChipView>();
                }

                found[count++] = view;
            }

            for (var i = BondPresentation.VisibleChipSlots; ; i++)
            {
                var extra = roster.Find("Chip_" + i);
                if (extra == null)
                {
                    break;
                }

                extra.gameObject.SetActive(false);
            }

            if (count == 0)
            {
                return;
            }

            if (chips == null || chips.Length != count)
            {
                var trimmed = new BondRosterChipView[count];
                System.Array.Copy(found, trimmed, count);
                chips = trimmed;
                return;
            }

            for (var i = 0; i < count; i++)
            {
                if (chips[i] == null)
                {
                    chips[i] = found[i];
                }
            }
        }

        private void EnsureChevron()
        {
            rosterChevron = EnsureChevronButton(rosterChevron, "CenterStats/Roster/Chevron");
            rosterChevronPrev = EnsureChevronButton(rosterChevronPrev, "CenterStats/Roster/ChevronPrev");
        }

        private Button EnsureChevronButton(Button current, string path)
        {
            var button = current;
            if (button == null)
            {
                var found = transform.Find(path);
                if (found != null)
                {
                    button = found.GetComponent<Button>();
                    if (button == null)
                    {
                        button = found.gameObject.AddComponent<Button>();
                    }
                }
            }

            if (button == null)
            {
                return null;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                if (button.targetGraphic == null)
                {
                    button.targetGraphic = image;
                }
            }

            return button;
        }

        private void RefreshChevronVisibility(int slotCount)
        {
            var pages = BondPresentation.PageCount(slotCount);
            var page = slotCount > 0 ? _pageOffset / slotCount : 0;
            if (rosterChevronPrev != null)
            {
                rosterChevronPrev.gameObject.SetActive(page > 0);
            }

            if (rosterChevron != null)
            {
                rosterChevron.gameObject.SetActive(page < pages - 1);
            }
        }

        private void ApplyStaticCopy()
        {
            SetText(headerLabel, BondPresentation.Title);
            SetText(headerJp, BondPresentation.TitleJp);
            SetText(socialStatsTitle, BondPresentation.SocialStatsTitle);
            SetText(socialStatsJp, BondPresentation.SocialStatsJp);
            SetText(linkTitle, BondPresentation.LinkEpisodesTitle);
            SetText(linkJp, BondPresentation.LinkEpisodesJp);
            SetText(confirmLabel, "Confirm");
            SetText(backLabel, "Back");
        }

        private void Refresh()
        {
            if (_state == null)
            {
                return;
            }

            var ranks = new int[SocialStatPresentation.OrderedStats.Length];
            for (var i = 0; i < ranks.Length; i++)
            {
                var stat = SocialStatPresentation.OrderedStats[i];
                ranks[i] = _state.SocialStats.GetRank(stat);
            }

            radar?.SetRanks(ranks);
            for (var i = 0; i < nodes.Length && i < SocialStatPresentation.OrderedStats.Length; i++)
            {
                var stat = SocialStatPresentation.OrderedStats[i];
                var icon = i < statIcons.Length ? statIcons[i] : null;
                nodes[i]?.Bind(stat, _state.SocialStats.GetRank(stat), icon);
            }

            EnsureChipSlots();
            var slotCount = SlotCount();
            _pageOffset = BondPresentation.PageOffsetFor(_rosterIndex, slotCount);
            RefreshChevronVisibility(slotCount);

            for (var i = 0; i < slotCount; i++)
            {
                var rosterIndex = _pageOffset + i;
                var chip = chips[i];
                if (chip == null)
                {
                    continue;
                }

                if (rosterIndex >= BondPresentation.RosterEntryCount)
                {
                    chip.gameObject.SetActive(false);
                    continue;
                }

                chip.gameObject.SetActive(true);
                var npcId = NpcIdAt(rosterIndex);
                var bond = _state.GetBond(npcId);
                var rosterLocked = BondPresentation.ShouldShowRosterLock(npcId, bond);
                var selected = rosterIndex == _rosterIndex;
                var frame = rosterLocked
                    ? chipFrameLocked
                    : selected
                        ? chipFrameSelected
                        : chipFrameNormal;
                chip.Bind(npcId, selected, FaceSpriteFor(rosterIndex), frame, lockIcon, bond);
            }

            detail?.Bind(_state, SelectedNpcId, FaceSpriteFor(_rosterIndex));
            SetText(episodeHint, BondPresentation.GetEpisodeHint(SelectedNpcId));

            var selectedBond = _state.GetBond(SelectedNpcId);
            var rosterUnlocked = BondPresentation.IsBondRosterUnlocked(SelectedNpcId, selectedBond);
            var episodes = SelectedEpisodes();
            for (var i = 0; i < episodeRows.Length && i < episodes.Length; i++)
            {
                var episode = episodes[i];
                var unlocked = rosterUnlocked
                    && BondLinkEpisodeCatalog.IsUnlocked(selectedBond.Rank, episode.RequiredRank);
                episodeRows[i]?.Bind(episode, unlocked, i == _episodeIndex);
            }

            RefreshNav();
            RefreshPromo();
            ApplyPlayModeLayout();
            RefreshPanelHeader();
        }

        private void RefreshPanelHeader()
        {
            if (!Application.isPlaying)
            {
                ApplyStaticCopy();
                return;
            }

            var social = _navIndex == BondNavIds.SocialStats;
            var link = _navIndex == BondNavIds.Link;
            SetPanelActive("CenterStats/Title", social);
            SetPanelActive("LinkEpisodes/Title", link);

            if (social)
            {
                SetText(socialStatsTitle, BondPresentation.SocialStatsTitle);
                SetText(socialStatsJp, BondPresentation.SocialStatsJp);
            }

            if (link)
            {
                SetText(linkTitle, BondPresentation.LinkEpisodesTitle);
                SetText(linkJp, BondPresentation.LinkEpisodesJp);
            }
        }

        private void ApplyPlayModeLayout()
        {
            if (!Application.isPlaying)
            {
                SetPanelActive("Background", true);
                SetPanelActive("CornerHud", true);
                SetPanelActive("HeaderBonds", true);
                SetPanelActive("LeftNav", true);
                SetPanelActive("CenterStats", true);
                SetPanelActive("LinkEpisodes", true);
                SetPanelActive("Footer", true);
                SetPanelActive("Divider", true);
                SetSocialStatsSections(true);
                SetLinkSections(true);
                SetNavInteractable(true);
                SetPanelActive("DismissBackdrop", false);
                return;
            }

            SetPanelActive("Background", true);
            SetPanelActive("LeftNav", true);
            SetPanelActive("CornerHud", true);
            SetPanelActive("HeaderBonds", true);
            SetPanelActive("Footer", false);
            SetPanelActive("Divider", false);
            SetPanelActive("MockGuide", false);

            var social = _navIndex == BondNavIds.SocialStats;
            var link = _navIndex == BondNavIds.Link;
            var showContent = social || link;
            SetPanelActive("CenterStats", showContent);
            SetPanelActive("LinkEpisodes", link);
            SetSocialStatsSections(social);
            SetLinkSections(link);
            SetNavInteractable(false);
            SetPanelActive("DismissBackdrop", false);
        }

        private void SetNavInteractable(bool allEnabled)
        {
            for (var i = 0; i < navRows.Length; i++)
            {
                var row = navRows[i];
                if (row?.Button == null)
                {
                    continue;
                }

                row.Button.interactable = allEnabled || BondNavIds.IsImplemented(i);
            }
        }

        private void SetSocialStatsSections(bool visible)
        {
            SetPanelActive("CenterStats/ChartRoot", visible);
            SetPanelActive("CenterStats/Node_Resonance", visible);
            SetPanelActive("CenterStats/Node_Cadence", visible);
            SetPanelActive("CenterStats/Node_Pulse", visible);
            SetPanelActive("CenterStats/Node_Harmony", visible);
            SetPanelActive("CenterStats/Node_Rhythm", visible);
        }

        private void SetLinkSections(bool visible)
        {
            SetPanelActive("CenterStats/Roster", visible);
        }

        private void SetPanelActive(string path, bool active)
        {
            var panel = transform.Find(path);
            if (panel != null)
            {
                panel.gameObject.SetActive(active);
            }
        }

        private void EnsurePromoImage()
        {
            if (promoImage != null)
            {
                return;
            }

            var path = "LinkEpisodes/PromoFrame/PromoImage";
            var imageTransform = transform.Find(path);
            promoImage = imageTransform != null ? imageTransform.GetComponent<Image>() : null;
        }

        private void RefreshPromo()
        {
            EnsurePromoImage();
            if (promoImage == null)
            {
                return;
            }

            var sprite = BondPromoSprites.DefaultPromo;
            if (BondPromoCatalog.UsesEpisodePromos(SelectedNpcId))
            {
                var episodeSprite = BondPromoSprites.Episode(SelectedNpcId, _episodeIndex);
                if (episodeSprite != null)
                {
                    sprite = episodeSprite;
                }
            }

            promoImage.sprite = sprite;
            promoImage.enabled = sprite != null;
            promoImage.preserveAspect = true;
            promoImage.color = IsSelectedEpisodeUnlocked() ? Color.white : BondPresentation.PromoLockedTint;
        }

        private bool IsSelectedEpisodeUnlocked()
        {
            if (_state == null)
            {
                return false;
            }

            var episodes = SelectedEpisodes();
            if (_episodeIndex < 0 || _episodeIndex >= episodes.Length)
            {
                return false;
            }

            var bond = _state.GetBond(SelectedNpcId);
            return BondPresentation.IsBondRosterUnlocked(SelectedNpcId, bond)
                && BondLinkEpisodeCatalog.IsUnlocked(bond.Rank, episodes[_episodeIndex].RequiredRank);
        }

        private void RefreshNav()
        {
            for (var i = 0; i < navRows.Length; i++)
            {
                navRows[i]?.Bind(_navIndex >= 0 && i == _navIndex, navPlateNormal, navPlateSelected);
            }
        }

        private BondLinkEpisode[] SelectedEpisodes() =>
            BondLinkEpisodeCatalog.GetEpisodes(SelectedNpcId);

        private string NpcIdAt(int index)
        {
            if (index < 0 || index >= BondPresentation.RosterOrder.Length)
            {
                return string.Empty;
            }

            return BondPresentation.RosterOrder[index];
        }

        private Sprite PortraitAt(int index)
        {
            if (index < 0 || index >= BondPresentation.RosterOrder.Length)
            {
                return reservedPortrait;
            }

            return index < portraitSprites.Length ? portraitSprites[index] : reservedPortrait;
        }

        private Sprite FaceSpriteFor(int rosterIndex)
        {
            var npcId = NpcIdAt(rosterIndex);
            if (BondPresentation.UsesStoryHiddenFace(npcId))
            {
                EnsureStoryHiddenPortrait();
                return storyHiddenPortrait != null ? storyHiddenPortrait : reservedPortrait;
            }

            return PortraitAt(rosterIndex);
        }

        private void EnsureStoryHiddenPortrait()
        {
            if (storyHiddenPortrait != null)
            {
                return;
            }

#if UNITY_EDITOR
            storyHiddenPortrait = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                BondPresentation.StoryHiddenCardPath);
#endif
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }

        public static GameMetaState CreateSandboxState()
        {
            var state = GameMetaState.CreateHubStart();
            state.GetBond(BondNpcIds.Ryo).IsLocked = true;
            state.GetBond(BondNpcIds.MeiLin).IsLocked = true;
            return state;
        }
    }
}

using FracturedChorus.Meta;
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
        [SerializeField] private BondRosterChipView[] chips = new BondRosterChipView[7];
        [SerializeField] private Sprite chipFrameNormal;
        [SerializeField] private Sprite chipFrameSelected;
        [SerializeField] private Sprite chipFrameLocked;
        [SerializeField] private Sprite lockIcon;
        [SerializeField] private Sprite[] portraitSprites = new Sprite[6];
        [SerializeField] private Sprite reservedPortrait;
        [SerializeField] private BondDetailCardView detail;
        [SerializeField] private BondEpisodeRowView[] episodeRows = new BondEpisodeRowView[5];
        [SerializeField] private Text socialStatsTitle;
        [SerializeField] private Text socialStatsJp;
        [SerializeField] private Text linkTitle;
        [SerializeField] private Text linkJp;
        [SerializeField] private Text episodeHint;
        [SerializeField] private Text headerLabel;
        [SerializeField] private Text headerJp;
        [SerializeField] private Text confirmLabel;
        [SerializeField] private Text backLabel;

        private GameMetaState _state;
        private int _rosterIndex = 1;
        private int _episodeIndex;
        private bool _buttonsBound;

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

        private void Start()
        {
            if (_state == null)
            {
                Show(CreateSandboxState());
            }
        }

        public void Show(GameMetaState state)
        {
            if (root == null)
            {
                root = gameObject;
            }

            _state = state ?? CreateSandboxState();
            root?.SetActive(true);
            BindButtons();
            EnsureValidRosterSelection();
            _episodeIndex = Mathf.Clamp(_episodeIndex, 0, Mathf.Max(0, BondLinkEpisodeCatalog.Episodes.Length - 1));
            UiFontCatalog.ApplyHierarchy(transform);
            cornerHud?.Refresh(_state);
            ApplyStaticCopy();
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

                var chipIndex = i;
                chip.Button.onClick.AddListener(() => SelectRoster(chipIndex));
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

            _buttonsBound = true;
        }

        private void SelectRoster(int index)
        {
            _rosterIndex = Mathf.Clamp(index, 0, Mathf.Max(0, BondPresentation.VisibleChipCount - 1));
            EnsureValidRosterSelection();
            Refresh();
        }

        private void SelectEpisode(int index)
        {
            _episodeIndex = Mathf.Clamp(index, 0, Mathf.Max(0, BondLinkEpisodeCatalog.Episodes.Length - 1));
            Refresh();
        }

        private void MoveRoster(int delta)
        {
            for (var i = 0; i < BondPresentation.VisibleChipCount; i++)
            {
                _rosterIndex = WrapRosterIndex(_rosterIndex, delta, BondPresentation.VisibleChipCount);
                if (BondPresentation.IsPortraitUnlocked(SelectedNpcId))
                {
                    Refresh();
                    return;
                }
            }
        }

        private void MoveEpisode(int delta)
        {
            var count = BondLinkEpisodeCatalog.Episodes.Length;
            _episodeIndex = WrapRosterIndex(_episodeIndex, delta, count);
            Refresh();
        }

        private void ConfirmEpisode()
        {
            if (_state == null || BondLinkEpisodeCatalog.Episodes.Length == 0)
            {
                return;
            }

            var episode = BondLinkEpisodeCatalog.Episodes[_episodeIndex];
            var bond = _state.GetBond(SelectedNpcId);
            if (!BondLinkEpisodeCatalog.IsUnlocked(bond.Rank, episode.RequiredRank))
            {
                return;
            }

            Debug.Log($"[Bonds sandbox] play {SelectedNpcId} episode {episode.Index} {episode.Title}");
        }

        private void EnsureValidRosterSelection()
        {
            _rosterIndex = Mathf.Clamp(_rosterIndex, 0, Mathf.Max(0, BondPresentation.VisibleChipCount - 1));
            if (BondPresentation.IsPortraitUnlocked(SelectedNpcId))
            {
                return;
            }

            for (var i = 0; i < BondPresentation.VisibleChipCount; i++)
            {
                var candidate = WrapRosterIndex(_rosterIndex, i, BondPresentation.VisibleChipCount);
                if (BondPresentation.IsPortraitUnlocked(NpcIdAt(candidate)))
                {
                    _rosterIndex = candidate;
                    return;
                }
            }

            _rosterIndex = 0;
        }

        private void ApplyStaticCopy()
        {
            SetText(headerLabel, BondPresentation.Title);
            SetText(headerJp, BondPresentation.TitleJp);
            SetText(socialStatsTitle, BondPresentation.SocialStatsTitle);
            SetText(socialStatsJp, BondPresentation.SocialStatsJp);
            SetText(linkTitle, BondPresentation.LinkEpisodesTitle);
            SetText(linkJp, BondPresentation.LinkEpisodesJp);
            SetText(episodeHint, BondPresentation.EpisodeLockHint);
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

            for (var i = 0; i < chips.Length; i++)
            {
                var npcId = NpcIdAt(i);
                var unlocked = BondPresentation.IsPortraitUnlocked(npcId);
                var frame = !unlocked
                    ? chipFrameLocked
                    : i == _rosterIndex
                        ? chipFrameSelected
                        : chipFrameNormal;
                chips[i]?.Bind(npcId, i == _rosterIndex, PortraitAt(i), frame, lockIcon);
            }

            detail?.Bind(_state, SelectedNpcId, PortraitAt(_rosterIndex));

            var selectedBond = _state.GetBond(SelectedNpcId);
            for (var i = 0; i < episodeRows.Length && i < BondLinkEpisodeCatalog.Episodes.Length; i++)
            {
                var episode = BondLinkEpisodeCatalog.Episodes[i];
                var unlocked = BondLinkEpisodeCatalog.IsUnlocked(selectedBond.Rank, episode.RequiredRank);
                episodeRows[i]?.Bind(episode, unlocked, i == _episodeIndex);
            }
        }

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

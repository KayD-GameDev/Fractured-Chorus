### Task 4: View components + `BondsMenuUI` bind

**Files:**
- Create: `Assets/FracturedChorus/Hub/BondRosterChipView.cs`
- Create: `Assets/FracturedChorus/Hub/BondDetailCardView.cs`
- Create: `Assets/FracturedChorus/Hub/BondEpisodeRowView.cs`
- Modify: `Assets/FracturedChorus/Hub/BondsMenuUI.cs` (replace Task 3 stub in the same file)
- Modify: `Assets/FracturedChorus/Editor/BondsSceneSetupEditor.cs` — `AttachMissing` wires SerializeField refs by **name**, never Rects
- Modify: `Assets/FracturedChorus/Editor/BondPresentationTests.cs` — add selection wrap test against a static helper on `BondsMenuUI`

**Interfaces:**
- Consumes: `BondPresentation.*`, `BondLinkEpisodeCatalog.*`, `SocialStatPresentation`, `SocialStatsRadarGraphic.SetRanks(IReadOnlyList<int>)`, `SocialStatsNodeView.Bind(SocialStatType, int, Sprite)`, `GameMetaState`
- Produces:
  - `BondsMenuUI.Show(GameMetaState state)`
  - `BondsMenuUI.Hide()`
  - `BondsMenuUI.SelectedNpcId` → `string`
  - `BondsMenuUI.WrapRosterIndex(int index, int delta, int count)` → `int` (pure, testable)
  - `BondRosterChipView.Bind(string npcId, bool selected, Sprite face, Sprite frame, Sprite lockIcon)`
  - `BondDetailCardView.Bind(GameMetaState state, string npcId, Sprite portrait)`
  - `BondEpisodeRowView.Bind(BondLinkEpisode episode, bool unlocked, bool selected)`

- [ ] **Step 1: Failing wrap test** (append to `BondPresentationTests`)

```csharp
[Test]
public void WrapRosterIndex_Cycles()
{
    Assert.AreEqual(1, BondsMenuUI.WrapRosterIndex(0, 1, 7));
    Assert.AreEqual(0, BondsMenuUI.WrapRosterIndex(6, 1, 7));
    Assert.AreEqual(6, BondsMenuUI.WrapRosterIndex(0, -1, 7));
}
```

Run: FAIL until `WrapRosterIndex` exists.

- [ ] **Step 2: Views**

`BondRosterChipView.cs`:

```csharp
using FracturedChorus.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class BondRosterChipView : MonoBehaviour
    {
        [SerializeField] private Image frame;
        [SerializeField] private Image face;
        [SerializeField] private Image lockIcon;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text roleLabel;
        [SerializeField] private Button button;

        public string NpcId { get; private set; }
        public Button Button => button;

        public void Bind(string npcId, bool selected, Sprite faceSprite, Sprite frameSprite, Sprite lockSprite)
        {
            NpcId = npcId;
            var unlocked = BondPresentation.IsPortraitUnlocked(npcId);
            if (nameLabel != null)
            {
                nameLabel.text = unlocked ? BondPresentation.GetDisplayName(npcId) : string.Empty;
            }

            if (roleLabel != null)
            {
                roleLabel.text = BondPresentation.GetRoleLabel(npcId);
            }

            if (face != null)
            {
                face.sprite = faceSprite;
                face.enabled = faceSprite != null;
                face.color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                face.preserveAspect = true;
            }

            if (frame != null)
            {
                frame.sprite = frameSprite;
                frame.enabled = frameSprite != null;
            }

            if (lockIcon != null)
            {
                lockIcon.sprite = lockSprite;
                lockIcon.enabled = !unlocked && lockSprite != null;
            }

            if (button != null)
            {
                button.interactable = unlocked;
            }
        }
    }
}
```

`BondDetailCardView.cs`:

```csharp
using FracturedChorus.Meta;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class BondDetailCardView : MonoBehaviour
    {
        [SerializeField] private Image portrait;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text rankLabel;
        [SerializeField] private Text bioLabel;
        [SerializeField] private Text quoteLabel;
        [SerializeField] private Image expFill;
        [SerializeField] private Text expLabel;
        [SerializeField] private Text nextRankLabel;
        [SerializeField] private Text nextHintLabel;

        public void Bind(GameMetaState state, string npcId, Sprite portraitSprite)
        {
            var unlocked = BondPresentation.IsPortraitUnlocked(npcId);
            var bond = state != null ? state.GetBond(npcId) : null;
            if (portrait != null)
            {
                portrait.sprite = portraitSprite;
                portrait.enabled = portraitSprite != null;
                portrait.preserveAspect = true;
            }

            if (nameLabel != null)
            {
                nameLabel.text = BondPresentation.GetDisplayName(npcId);
            }

            if (rankLabel != null)
            {
                rankLabel.text = bond != null && unlocked ? $"Rank {bond.Rank}" : "Locked";
            }

            if (bioLabel != null)
            {
                bioLabel.text = BondPresentation.GetBio(npcId);
            }

            if (quoteLabel != null)
            {
                var quote = BondPresentation.GetQuote(npcId);
                quoteLabel.text = string.IsNullOrEmpty(quote) ? string.Empty : $"\"{quote}\"";
            }

            var threshold = bond != null ? bond.GetThresholdForRank(bond.Rank) : 0;
            var exp = bond != null ? bond.Exp : 0;
            if (expLabel != null)
            {
                expLabel.text = unlocked ? $"{exp} / {threshold}" : "—";
            }

            if (expFill != null)
            {
                expFill.type = Image.Type.Filled;
                expFill.fillMethod = Image.FillMethod.Horizontal;
                expFill.fillAmount = unlocked && threshold > 0 ? Mathf.Clamp01(exp / (float)threshold) : 0f;
            }

            if (nextRankLabel != null)
            {
                nextRankLabel.text = BondPresentation.NextRankLabel;
            }

            if (nextHintLabel != null)
            {
                nextHintLabel.text = BondPresentation.NextRankHint;
            }
        }
    }
}
```

`BondEpisodeRowView.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class BondEpisodeRowView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text indexLabel;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Button button;
        [SerializeField] private Sprite playSprite;
        [SerializeField] private Sprite lockSprite;

        public int EpisodeIndex { get; private set; }
        public bool Unlocked { get; private set; }
        public Button Button => button;

        public void Bind(BondLinkEpisode episode, bool unlocked, bool selected)
        {
            EpisodeIndex = episode.Index;
            Unlocked = unlocked;
            if (indexLabel != null)
            {
                indexLabel.text = episode.Index.ToString("00");
            }

            if (titleLabel != null)
            {
                titleLabel.text = episode.Title;
                titleLabel.color = unlocked
                    ? FracturedChorus.UI.FcColorTokens.Brand.TextPrimary
                    : FracturedChorus.UI.FcColorTokens.Brand.TextMuted;
            }

            if (icon != null)
            {
                icon.sprite = unlocked ? playSprite : lockSprite;
                icon.enabled = icon.sprite != null;
            }

            if (button != null)
            {
                button.interactable = unlocked;
            }
        }
    }
}
```

`BondEpisodeRowView` uses `FracturedChorus.UI.FcColorTokens` fully qualified — no extra using required.

- [ ] **Step 3: `BondsMenuUI`**

```csharp
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
            _state = state ?? CreateSandboxState();
            UiFontCatalog.ApplyHierarchy(transform);
            if (cornerHud != null)
            {
                cornerHud.Refresh(_state);
            }

            ApplyStaticCopy();
            Refresh();
        }

        public void Hide()
        {
        }

        private void Update()
        {
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
            var episode = BondLinkEpisodeCatalog.Episodes[_episodeIndex];
            var bond = _state.GetBond(SelectedNpcId);
            if (!BondLinkEpisodeCatalog.IsUnlocked(bond.Rank, episode.RequiredRank))
            {
                return;
            }

            Debug.Log($"[Bonds sandbox] play {SelectedNpcId} episode {episode.Index} {episode.Title}");
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
                ranks[i] = _state.SocialStats.GetRank(SocialStatPresentation.OrderedStats[i]);
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
```

Q/E already maps through `TownMapInput.MonthPrevPressed` / `MonthNextPressed`. Reusing them for roster in **sandbox only** is acceptable. Left/Right arrows are extra.

Do **not** call `Hide` on Esc in this plan.

Default `_rosterIndex = 1` (Charlotte). Skip locked chips in `MoveRoster`.

- [ ] **Step 4: Wire refs by name in AttachMissing** (`SerializedObject` find `DateLabel` etc.). Never write Rects.

- [ ] **Step 5: Run EditMode tests** — `BondPresentationTests` including wrap: PASS.

- [ ] **Step 6: Play Mode** on `BondsLayoutSandbox` (not CampusHub).

Expected:
- Charlotte selected, Rank 1, EXP `0 / 10`
- Radar pentagon at rank 1
- Episode 01 playable, 02–05 locked
- Left/Right skips Ryo/MeiLin/reserved
- Enter logs episode 01
- Esc does not unload the scene
- MockGuide off

- [ ] **Step 7: Commit**

```bash
git add Assets/FracturedChorus/Hub/BondsMenuUI.cs Assets/FracturedChorus/Hub/BondRosterChipView.cs Assets/FracturedChorus/Hub/BondDetailCardView.cs Assets/FracturedChorus/Hub/BondEpisodeRowView.cs Assets/FracturedChorus/Editor/BondsSceneSetupEditor.cs Assets/FracturedChorus/Editor/BondPresentationTests.cs Assets/FracturedChorus/Scenes/BondsLayoutSandbox.unity
git commit -m "$(cat <<'EOF'
Bind Bonds sandbox HUD to social stats and bond ranks.

EOF
)"
```

---


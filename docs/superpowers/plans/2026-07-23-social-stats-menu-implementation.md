# Social Stats Menu (Resonance Field) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship Screen C — Social Stats (Resonance Field): 5-axis radar + nodes + Ren bust + Esc Back, bound to `SocialStatsState`, opened from Status Menu Stats tab.

**Architecture:** Overlay sibling của `CalendarOverlayUI` — code-built uGUI hierarchy (`SocialStatsOverlayUI.Build`), custom `MaskableGraphic` cho radar polygon/axes, data từ `GameMetaState.SocialStats`. Không dùng PNG mock làm fullscreen production texture.

**Tech Stack:** Unity 6 · uGUI · `FracturedChorus.Hub` · `FracturedChorus.Meta` · Editor menu wire/preview

**Design SoT:**
- UI lock §3b: [`docs/superpowers/specs/2026-07-19-status-and-echo-keys-ui-lock.md`](../specs/2026-07-19-status-and-echo-keys-ui-lock.md)
- Stat names / rank 1–10: [`docs/superpowers/specs/2026-07-11-persona-calendar-design.md`](../specs/2026-07-11-persona-calendar-design.md) §5
- Mock: `docs/combat/illustrations/social_stats_menu_mock.png` · `Art/UI/SocialStats/social_stats_menu_mock.png`
- Runtime data đã có: `Meta/SocialStatType.cs`, `Meta/SocialStatsState.cs`

## Global Constraints

- Palette: navy / cobalt / cyan / white (shared với Status + Echo Keys lock §1)
- Aspect: 16:9 reference
- EN copy MVP; JP subtitle `共鳴フィールド` optional dưới title
- Rank scale runtime: **1–10** (`SocialStatsState.MaxRank`); radar normalize `rank / 10`
- Axis order L→R (clock): **Resonance · Cadence · Pulse · Harmony · Rhythm** (= enum order 0→4)
- Input MVP: **Esc** Back only (không Q/E, không selection list)
- Mock PNG = composition reference only — rebuild zones bằng uGUI
- Namespace: `FracturedChorus.Hub` (UI) · reuse `FracturedChorus.Meta` (state)
- Pattern: `Build(Transform parent)` + `Show(GameMetaState)` / `Hide()` + SFX bind như Calendar
- Không generate comments trong source trừ khi cần XML cho public API Editor

### Out of scope (this plan)

- Party Status Screen A (combat stats / Q/E)
- Echo Keys Screen B (Bond list)
- Gamepad remapping
- Stat EXP bar trên node (mock chỉ Rank + flavor)
- Icon art production pack (placeholder circle / simple sprite OK MVP)
- Dedicated Ren “cyan grade head” art — dùng bust có sẵn hoặc crop placeholder

---

## File map

| File | Responsibility |
|------|----------------|
| `Hub/SocialStatPresentation.cs` | Static: display name, flavor EN, axis angle order |
| `Hub/SocialStatsRadarGraphic.cs` | `MaskableGraphic`: axes + ticks + fill polygon từ ranks |
| `Hub/SocialStatsNodeView.cs` | 1 node: icon + name + Rank + flavor |
| `Hub/SocialStatsOverlayUI.cs` | Root overlay: Build hierarchy, Show/Hide, Esc, bind state |
| `Hub/MetaStatusMenuUI.cs` | Stats tab → open overlay (như Calendar) |
| `Hub/UiEditPreviewRoot.cs` | PreviewMode + host Social Stats |
| `Editor/UiEditPreviewSetupEditor.cs` | Build preview host nếu thiếu |
| `Editor/CampusHubSceneSetupEditor.cs` | Optional wire menu entry |
| `Art/UI/SocialStats/README.md` | Asset slot notes (mock vs runtime) |
| Spec lock §6 | Thêm acceptance checklist Social Stats |

```
TownMap / StatusMenu parent
└── SocialStatsOverlay          // sibling CalendarOverlay
    ├── DimBackdrop
    ├── Watermark ("RESONANCE FIELD")
    ├── TitleBlock (SOCIAL STATS + JP)
    ├── ChartRoot
    │   ├── RadarGraphic        // SocialStatsRadarGraphic
    │   └── CenterGlyph         // optional note icon Image
    ├── NodesRoot
    │   ├── Node_Resonance … Node_Rhythm
    ├── HeroBust                // Ren head/bust Image
    └── FooterEsc               // [Esc] Back prompt
```

---

### Task 1: Presentation constants + radar graphic

**Files:**
- Create: `Assets/FracturedChorus/Hub/SocialStatPresentation.cs`
- Create: `Assets/FracturedChorus/Hub/SocialStatsRadarGraphic.cs`
- Test: Editor Play / Scene view — component on empty Image-sized Rect; set sample ranks; visual check

**Interfaces:**
- Produces:
  - `SocialStatPresentation.OrderedStats` → `SocialStatType[5]` L→R
  - `SocialStatPresentation.GetDisplayName(SocialStatType)` → `string`
  - `SocialStatPresentation.GetFlavor(SocialStatType)` → `string`
  - `SocialStatsRadarGraphic.SetRanks(IReadOnlyList<int> ranks)` — length 5, each clamped 1–10
  - `SocialStatsRadarGraphic.MaxRank` default 10

- [ ] **Step 1: Add presentation helper**

```csharp
using FracturedChorus.Meta;

namespace FracturedChorus.Hub
{
    public static class SocialStatPresentation
    {
        public static readonly SocialStatType[] OrderedStats =
        {
            SocialStatType.Resonance,
            SocialStatType.Cadence,
            SocialStatType.Pulse,
            SocialStatType.Harmony,
            SocialStatType.Rhythm
        };

        public static string GetDisplayName(SocialStatType stat) => stat.ToString();

        public static string GetFlavor(SocialStatType stat) => stat switch
        {
            SocialStatType.Resonance => "Build deeper bonds through empathy.",
            SocialStatType.Cadence => "Understand timing and flow.",
            SocialStatType.Pulse => "Find strength in commitment.",
            SocialStatType.Harmony => "Unite through shared purpose.",
            SocialStatType.Rhythm => "Keep steady. Drive forward.",
            _ => string.Empty
        };
    }
}
```

- [ ] **Step 2: Add radar `MaskableGraphic`**

Pattern: `VaultTerritoryGraphic` (`RunMap/UI/VaultTerritoryGraphic.cs`) — override `OnPopulateMesh(VertexHelper vh)`.

Required mesh parts:
1. **Axes** — 5 rays from center to rim (cyan, thin)
2. **Tick rings** — concentric pentagons at 0.2 / 0.4 / 0.6 / 0.8 / 1.0 (mock shows 1–5 ticks; keep 5 rings even when max rank = 10)
3. **Fill polygon** — vertices at `t = rank / MaxRank` along each axis; translucent cyan fill + brighter stroke

Angle layout (degrees, 0° = up, clockwise-friendly):

```text
Pulse     = -90°   (12 o'clock)
Cadence   = -90° - 72°
Resonance = -90° - 144°
Rhythm    = -90° + 144°
Harmony   = -90° + 72°
```

Map `OrderedStats` index → angle so visual L→R matches lock (Resonance left … Rhythm right).

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SocialStatsRadarGraphic : MaskableGraphic
    {
        [SerializeField] private int maxRank = 10;
        [SerializeField] private Color axisColor = new Color(0f, 0.83f, 1f, 0.45f);
        [SerializeField] private Color ringColor = new Color(0f, 0.83f, 1f, 0.22f);
        [SerializeField] private Color fillColor = new Color(0f, 0.75f, 1f, 0.28f);
        [SerializeField] private Color strokeColor = new Color(0.4f, 0.95f, 1f, 0.9f);
        [SerializeField] private float strokeWidth = 2.5f;

        private readonly int[] _ranks = { 1, 1, 1, 1, 1 };

        public void SetRanks(IReadOnlyList<int> ranks)
        {
            for (var i = 0; i < 5; i++)
            {
                var v = ranks != null && i < ranks.Count ? ranks[i] : 1;
                _ranks[i] = Mathf.Clamp(v, 1, maxRank);
            }

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            // 1) draw 5 ring pentagons (ringColor)
            // 2) draw 5 axis lines (axisColor)
            // 3) draw fill fan + stroke for data polygon (fillColor / strokeColor)
            // Use rectTransform.rect; center = rect.center; radius = 0.48 * min(w,h)
        }
    }
}
```

Implement line/quad helpers locally (2 triangles per segment). Keep file self-contained; do not pull RunMap types.

- [ ] **Step 3: Smoke-check in empty Canvas**

Editor: temporary GameObject + `SocialStatsRadarGraphic` + `SetRanks(new[]{4,5,3,4,2})` → polygon matches mock shape (Cadence high, Rhythm low).

- [ ] **Step 4: Commit**

```bash
git add Assets/FracturedChorus/Hub/SocialStatPresentation.cs Assets/FracturedChorus/Hub/SocialStatsRadarGraphic.cs Assets/FracturedChorus/Hub/SocialStatPresentation.cs.meta Assets/FracturedChorus/Hub/SocialStatsRadarGraphic.cs.meta
git commit -m "$(cat <<'EOF'
Add Social Stats radar graphic and presentation copy.

EOF
)"
```

---

### Task 2: Node view + overlay shell Build/Show/Hide

**Files:**
- Create: `Assets/FracturedChorus/Hub/SocialStatsNodeView.cs`
- Create: `Assets/FracturedChorus/Hub/SocialStatsOverlayUI.cs`
- Create: `Assets/FracturedChorus/Art/UI/SocialStats/README.md`

**Interfaces:**
- Consumes: `SocialStatPresentation.*`, `SocialStatsRadarGraphic.SetRanks`, `GameMetaState.SocialStats`
- Produces:
  - `SocialStatsNodeView.Bind(SocialStatType, int rank, Sprite iconOrNull)`
  - `SocialStatsOverlayUI.Build(Transform parent) -> BuildResult`
  - `SocialStatsOverlayUI.Show(GameMetaState state)` / `Hide()` / `bool IsOpen`
  - `SocialStatsOverlayUI.BindSfx(TownMapSfxController)`

- [ ] **Step 1: Node view**

```csharp
using FracturedChorus.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class SocialStatsNodeView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text rankLabel;
        [SerializeField] private Text flavorLabel;

        public void Bind(SocialStatType stat, int rank, Sprite icon)
        {
            if (nameLabel != null)
            {
                nameLabel.text = SocialStatPresentation.GetDisplayName(stat);
            }

            if (rankLabel != null)
            {
                rankLabel.text = $"Rank {Mathf.Clamp(rank, 1, SocialStatsState.MaxRank)}";
            }

            if (flavorLabel != null)
            {
                flavorLabel.text = SocialStatPresentation.GetFlavor(stat);
            }

            if (iconImage != null)
            {
                iconImage.enabled = icon != null;
                if (icon != null)
                {
                    iconImage.sprite = icon;
                    iconImage.preserveAspect = true;
                }
            }
        }
    }
}
```

- [ ] **Step 2: Overlay UI — API mirror Calendar**

Copy structural habits from `CalendarOverlayUI`:
- `BuildResult` struct
- `Build(parent)` finds `SocialStatsOverlay` or creates hierarchy
- `EnsureRuntimeBindings` / rewire Esc + close button
- `Update`: if open && `TownMapInput.CancelPressed()` → `Hide()`
- Colors: reuse cyan `new Color(0f, 0.831f, 1f, 1f)` + navy dim backdrop

Hierarchy create (code):

| Child | Notes |
|-------|-------|
| `DimBackdrop` | full stretch Image alpha ~0.75 navy |
| `Watermark` | Text rotated ~-28°, `RESONANCE FIELD`, low alpha |
| `TitleBlock/Title` | `SOCIAL STATS` bold italic-ish (font style Bold) |
| `TitleBlock/Subtitle` | `共鳴フィールド` smaller cyan |
| `ChartRoot/Radar` | `SocialStatsRadarGraphic` ~720×720 centered upper mid |
| `NodesRoot/Node_*` | 5 nodes; anchor positions around chart (serialized offsets) |
| `HeroBust` | bottom-center Image; load Ren bust if available |
| `FooterEsc` | Text `[Esc] Back` bottom-right |

Show path:

```csharp
public void Show(GameMetaState state)
{
    _state = state;
    if (root != null)
    {
        root.SetActive(true);
    }

    sfx?.PlayOpenPanel();
    Refresh();
}

private void Refresh()
{
    if (_state == null)
    {
        return;
    }

    var ranks = new int[5];
    for (var i = 0; i < 5; i++)
    {
        var stat = SocialStatPresentation.OrderedStats[i];
        ranks[i] = _state.SocialStats.GetRank(stat);
        _nodes[i]?.Bind(stat, ranks[i], _icons != null && i < _icons.Length ? _icons[i] : null);
    }

    radar?.SetRanks(ranks);
}
```

Hero sprite load order (Editor + runtime):
1. `Resources.Load<Sprite>("UI/SocialStats/ren_resonance_bust_v1")` if present
2. Else `Resources.Load<Sprite>` / Art path fallback to `Art/Characters/Ren/VnBust/ren_bust_neutral_v1`
3. Else leave empty (no soft-lock)

- [ ] **Step 3: README asset slots**

Write `Art/UI/SocialStats/README.md`:
- Mock = reference only
- Optional future: 5 stat icons `stat_icon_{resonance,cadence,pulse,harmony,rhythm}_v1.png`
- Optional dedicated bust `ren_resonance_bust_v1.png`

- [ ] **Step 4: Compile + open overlay from temporary button in Play Mode**

Verify Esc closes; ranks from `GameMetaSession.Current` or `NewGame()` defaults (all Rank 1 → small pentagon).

- [ ] **Step 5: Commit**

```bash
git add Assets/FracturedChorus/Hub/SocialStatsNodeView.cs Assets/FracturedChorus/Hub/SocialStatsOverlayUI.cs Assets/FracturedChorus/Art/UI/SocialStats/README.md
git commit -m "$(cat <<'EOF'
Add Social Stats Resonance Field overlay shell.

EOF
)"
```

---

### Task 3: Wire Status Menu Stats tab + preview

**Files:**
- Modify: `Assets/FracturedChorus/Hub/MetaStatusMenuUI.cs`
- Modify: `Assets/FracturedChorus/Hub/UiEditPreviewRoot.cs`
- Modify: `Assets/FracturedChorus/Editor/UiEditPreviewSetupEditor.cs`
- Modify (optional): `Assets/FracturedChorus/Editor/CampusHubSceneSetupEditor.cs`
- Modify: `Assets/FracturedChorus/Hub/CampusHubController.cs` (preview enum if used)

**Interfaces:**
- Consumes: `SocialStatsOverlayUI.Build`, `Show`, `BindSfx`
- Produces: Stats tab click opens overlay; Esc on overlay returns to Status Menu (Status stays open underneath, same Calendar behavior)

- [ ] **Step 1: MetaStatusMenuUI — field + open**

Add:

```csharp
[SerializeField] private SocialStatsOverlayUI socialStatsOverlay;
```

Mirror Calendar:

```csharp
private void BindTab(Button button, Tab tab, bool openCalendar = false, bool openSocialStats = false)
{
    // ...
    button.onClick.AddListener(() =>
    {
        _tab = tab;
        sfx?.PlaySelect();
        Refresh();
        if (openCalendar)
        {
            OpenCalendarOverlay();
        }
        else if (openSocialStats)
        {
            OpenSocialStatsOverlay();
        }
    });
}

// Wire:
BindTab(statsButton, Tab.Stats, openSocialStats: true);
BindTab(bondsButton, Tab.Bonds);
BindTab(calendarButton, Tab.Calendar, openCalendar: true);
```

`OpenSocialStatsOverlay()`:
- parent = `transform.parent ?? transform`
- `EnsureSocialStatsOverlay(host)` → `SocialStatsOverlayUI.Build(host)`
- `SetAsLastSibling()`, `BindSfx`, `Show(_state ?? GameMetaSession.Current)`

`Update` / cancel: if `socialStatsOverlay != null && socialStatsOverlay.IsOpen` → return early (Calendar already does this) so Esc closes overlay first, not whole Status Menu.

Detail body when Stats selected (optional polish):

```csharp
Tab.Stats => "Opening Resonance Field…",
```

Keep `BuildStats` for debug fallback if overlay missing — or call only when overlay null.

- [ ] **Step 2: UiEditPreviewRoot**

Add `PreviewMode.SocialStats` (or nested under StatusMenu with auto-open). Prefer explicit mode:

```csharp
public enum PreviewMode
{
    StatusMenu = 0,
    Calendar = 1,
    SocialStats = 2
}
```

On SocialStats: show Status host (or dedicated host) + `socialStatsOverlay.Show(state)`.

- [ ] **Step 3: Editor setup**

`UiEditPreviewSetupEditor`: build `SocialStatsOverlay` under preview root if missing (call `Build`).

Optional menu: `Fractured Chorus → Wire Social Stats Overlay` next to Status Menu wire.

- [ ] **Step 4: Manual test matrix**

| Step | Expected |
|------|----------|
| CampusHub → Status → Stats | Overlay opens; radar + 5 names |
| Esc | Overlay closes; Status remains |
| Esc again | Status closes |
| MetaDebug add Cadence EXP → rank up → reopen | Cadence vertex moves out |
| Save/load ranks | Overlay matches `SocialStatsState` |

- [ ] **Step 5: Commit**

```bash
git add Assets/FracturedChorus/Hub/MetaStatusMenuUI.cs Assets/FracturedChorus/Hub/UiEditPreviewRoot.cs Assets/FracturedChorus/Editor/UiEditPreviewSetupEditor.cs
git commit -m "$(cat <<'EOF'
Wire Social Stats overlay from Status Menu Stats tab.

EOF
)"
```

---

### Task 4: Layout lock pass + acceptance + spec sync

**Files:**
- Modify: `Assets/FracturedChorus/Hub/SocialStatsOverlayUI.cs` (anchors/fonts/sizes)
- Modify: `docs/superpowers/specs/2026-07-19-status-and-echo-keys-ui-lock.md` §6
- Optional icons under `Art/UI/SocialStats/` (only if art ready; else skip)

**Layout targets (16:9, match mock zones — not pixel-perfect):**

| Zone | Approx |
|------|--------|
| Title | top-left safe |
| Chart | center-upper; origin above Ren head |
| Nodes | arc around chart; left pair / top / right pair |
| Hero | bottom-center, bust crop, cyan-ish Image color tint OK |
| Esc | bottom-right sharp prompt |

- [x] **Step 1: Tune RectTransforms** so nodes không đè title; radar không cắt hero

- [x] **Step 2: Update lock acceptance**

Append to §6:

```markdown
**Social Stats**

- [ ] Title `SOCIAL STATS` (+ optional JP)
- [ ] 5-axis radar bind ranks 1–10 từ `SocialStatsState`
- [ ] Nodes L→R Resonance·Cadence·Pulse·Harmony·Rhythm + Rank + flavor
- [ ] Ren bust slot bottom-center
- [ ] Esc Back; open from Status Stats tab
- [ ] Không dùng mock PNG fullscreen làm production texture
```

- [ ] **Step 3: Final playtest vs mock** — hierarchy + affordance pass

- [ ] **Step 4: Commit**

```bash
git add Assets/FracturedChorus/Hub/SocialStatsOverlayUI.cs docs/superpowers/specs/2026-07-19-status-and-echo-keys-ui-lock.md
git commit -m "$(cat <<'EOF'
Polish Social Stats layout and sync UI lock acceptance.

EOF
)"
```

---

## Acceptance (ship gate)

- [ ] Open from Status → Stats shows Resonance Field overlay
- [ ] 5 nodes correct names + flavors from presentation helper
- [ ] Radar polygon tracks `GetRank` for all 5 stats (MaxRank 10)
- [ ] Esc closes overlay before Status Menu
- [ ] Rank change (activity / MetaDebug) reflected on reopen
- [ ] Compiles; Calendar + Status paths unchanged when Stats not pressed
- [ ] Mock PNG not assigned as fullscreen BG in production hierarchy

---

## Spec coverage (self-review)

| Spec item | Task |
|-----------|------|
| Title SOCIAL STATS + JP optional | Task 2 |
| Hero Ren head/bust | Task 2 |
| 5-axis radar ranks | Task 1–2 |
| Nodes L→R + icon/name/Rank/flavor | Task 2 (icon optional) |
| Watermark RESONANCE FIELD | Task 2 |
| Esc Back | Task 2–3 |
| Data `SocialStatsState` | Task 2–3 |
| No mock fullscreen production | Global + Task 2 README |
| Wire from hub Status | Task 3 |
| Acceptance checklist in lock | Task 4 |

## Placeholder scan

No TBD steps; icon art explicitly optional with null-safe bind.

---

## Estimate

| Task | Effort |
|------|--------|
| 1 Radar + presentation | 0.5–1 ngày |
| 2 Overlay shell | 1 ngày |
| 3 Wire + preview | 0.5 ngày |
| 4 Layout polish + spec | 0.5 ngày |
| **Total** | **~2.5–3 ngày** (1 dev) |

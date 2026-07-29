# Fractured Chorus — UI Color Tokens

Runtime SoT: `Assets/FracturedChorus/UI/FcColorTokens.cs`

## DO / DON'T

| DO | DON'T |
|----|-------|
| Dùng `FcColorTokens.Brand.*` cho accent UI hub/menu | Hardcode `new Color(0f, 0.831f, 1f)` |
| Dùng `FcColorTokens.Surface.*` cho panel/backdrop | Mỗi overlay tự định navy riêng |
| Dùng `FcColorTokens.Semantic.*` cho gameplay (element, damage, event) | Trộn semantic và brand cùng một màu hồng |
| `WithAlpha(token, a)` khi cần opacity khác | Copy RGB rồi sửa alpha tay |

## Brand

| Token | Hex | Usage |
|-------|-----|-------|
| `Brand.Cyan` | `#00D4FF` | Hub accent chính |
| `Brand.CyanDim` | `#008CB3` | Label phụ, archive inactive |
| `Brand.CyanHover` | — | Hover text, button highlight |
| `Brand.CyanSoft` | — | Stat bar fill, radar fill base |
| `Brand.CyanNeonBody` | `#22D3EE` | Combat bridge (Epic 4) |
| `Brand.CyanNeonCore` | `#8CF3FF` | Combat glow core (Epic 4) |
| `Brand.MagentaAccent` | `#FF3DA6` | Neon accent, deploy back column |
| `Brand.RedSelection` | `#FF4757` | Selection accent (Epic 2) |
| `Brand.TextPrimary` | `#EAFBFF` | Bright copy on dark |

## Surface

| Token | Usage |
|-------|-------|
| `Surface.Dim` | Backdrop, watermark, deploy hint |
| `Surface.Panel` | Calendar, tutorial, party status |
| `Surface.Modal` | Save slots, level-up, skill equip |
| `Surface.Track` | Stat bar track |
| `Surface.Row` / `RowSelected` | Save slot rows |
| `Surface.Detail` | Meta status detail pane |
| `Surface.Chip` | Calendar date chip |
| `Surface.DimmerBlack` | Full-screen dimmer (tutorial) |

## Semantic

Element colors align with `HarmonyElementPalette` → delegates to `Semantic.Element*` (Epic 5). Calendar event ring uses `Semantic.EventGold` (merged from 3 legacy golds).

| Token | Usage |
|-------|-------|
| `Semantic.ElementRhythm/Melody/Harmony` | Badge ring, party card border, element disc fallback |
| `Semantic.EventGold` | Calendar event dot ring |
| `Semantic.Damage/Heal/Crit` | Combat popups, counter (not brand magenta) |

## Scene bridge

```
CampusHub / Menu ── Brand.Cyan + Surface.*
        │
        ▼
RunMap ── RunMap.* strokes + Surface fill (Epic 3)
        │
        ▼
Combat ── Brand.CyanNeon* + Semantic.* (Epic 4)
```

## Migration table

| Old constant / value | New token |
|----------------------|-----------|
| `(0, 0.831, 1)` Cyan | `Brand.Cyan` |
| `(0, 0.55, 0.7)` CyanDim | `Brand.CyanDim` |
| `(0.55, 0.85, 1)` hover | `Brand.CyanHover` |
| `(0.2, 0.75, 1)` bar fill | `Brand.CyanSoft` |
| `(0.02, 0.04, 0.12, 0.75)` NavyDim | `Surface.Dim` |
| `(0.03, 0.05, 0.14, …)` PanelBlue | `Surface.Panel` / `WithAlpha` |
| `(0.039, 0.059, 0.18, 0.94)` NavyPanel | `Surface.Modal` |
| `(0.06, 0.08, 0.24)` NavyRow | `Surface.Row` |
| `(0.1, 0.14, 0.34)` NavyRowSelected | `Surface.RowSelected` |
| `(0.039, 0.039, 0.18, 0.72)` DetailPanel | `Surface.Detail` |
| `(0.08, 0.12, 0.22)` BarBg | `Surface.Track` |
| `(0, 0, 0, 0.72)` DimColor | `Surface.DimmerBlack` |
| Deploy BackColor pink | `Brand.MagentaAccent` |
| Deploy Front/Mid cyan | `Brand.CyanHover` / `Brand.TextPrimary` |

## Selection (Epic 2)

| Role | Token |
|------|-------|
| Selected accent | `Selection.Accent` (= `Brand.RedSelection` @ 0.95α) |
| Hover / focus | `Brand.CyanHover` |
| Idle text | `Brand.TextIdle` / `Brand.CyanDim` |
| Tab icon selected wash | `Selection.TabIconTint` |
| Save slot row selected | `Selection.RowBackground` |
| Off-Beat track selected label | `Selection.Accent` (catalog list, not hover) |
| VN agree/disagree highlight | `Selection.VnChoiceHighlight` (cyan — warm BG) |

## Run Map (Epic 3)

Node strokes: `RunMap.*` · fill `LerpSurface(stroke)` · current node ring `Brand.CyanNeonBody` · path `CyanNeonCore` @ 0.85α · boss gate `Surface.Modal` + `RedSelection` CTA.

## Combat tint (Epic 4)

| UI | Token |
|----|-------|
| Skill radial highlight | `Brand.CyanNeonBody` |
| Skill drag ghost | `Brand.CyanNeonBody` @ 0.75α |
| Skill frame active | `Brand.CyanHover` |
| Damage pop | `Semantic.Damage` |
| Heal pop | `Semantic.Heal` |
| Crit pop | `Semantic.Crit` |
| Boss note accent | `Brand.MagentaAccent` |
| Boss track borders | `CyanNeonCore` top · `MagentaAccent` bottom |
| Party HP bar (editor setup) | `Brand.CyanSoft` |

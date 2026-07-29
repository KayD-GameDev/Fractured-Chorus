# Post-Defense Polish — Regression Checklist

Run after implementing polish epics. Mark each scene pair.

## Scenes

| # | Scene | Check |
|---|-------|-------|
| 1 | MainMenuStartGame | Fonts unified; Load Game opens 10 slots; Difficulty copy matches multipliers |
| 2 | CampusHub | Notes HUD ♪; Tutorial hub once; Status→Party Status; System→Save slots; hover on tabs |
| 3 | RunMap / Cadence | Tutorial map once; Battle/Elite enter combat; Camp spends Notes; Treasure grants; Relay shop; Notes HUD |
| 4 | CombatPrototype | No world HP numbers; Deploy column badges + boss pressure; hover Deploy/Execute/Continue; result +Notes |
| 5 | Overlay stack | Open Status + Settings; Tutorial coach above panels; combat chips not under result |

## Systems

- [ ] Save slot 0–9 independent; legacy `fc_meta_save.json` migrates to slot 0
- [ ] Skill equip in hub → combat kit matches
- [ ] Difficulty OnBeat vs OffBeat enemy HP/dmg differs
- [ ] Boss Despair pressure text visible in Deploy; weighted front targeting
- [ ] Tutorial flags persist after complete

## Overlap fixes

- CombatResult sort = Popup (400)
- Damage numbers = 520
- Tutorial = 1100
- Settings = 32000

## Palette (FcColorTokens)

Spot-check after Epic 0–5. SoT: `Assets/FracturedChorus/UI/FcColorTokens.cs` · doc: `docs/ui/COLOR_TOKENS.md`

| # | Scene | Check |
|---|-------|-------|
| 1 | MainMenuStartGame | Menu hover cyan (`Brand.CyanHover`); no legacy cyan selection on choices |
| 2 | CampusHub | Overlays share navy (`Surface.*`); tabs/icons selection red tint; calendar event ring gold = `Semantic.EventGold` |
| 3 | RunMap / Cadence | Legend strokes match `RunMap.*`; current node ring cyan neon (not StS orange); boss gate modal navy + red CTA |
| 4 | CombatPrototype | Skill radial highlight cyan neon (not orange); damage pink ≠ UI magenta accent same frame; party HP bar cyan soft |
| 5 | OpeningInvestigation / FlowerShop / Prologue VN | Choice selected = cyan (`Selection.VnChoiceHighlight`); readable on warm BG |

### Global palette rules

- [ ] Hub cyan consistent — no stray `(0, 0.831, 1)` outside `FcColorTokens.cs`
- [ ] Run Map not StS-green dominant — no `#82B366` / `#AA4E49` hex in code
- [ ] Combat skill highlight not orange `(0.95, 0.62, 0.25)`
- [ ] Selection red visible on VN choice, Status tab, Save slot row
- [ ] Element badges (party card) match semantic rhythm/melody/harmony hues
- [ ] Damage pop (`Semantic.Damage`) visually distinct from brand magenta accent

### Dev aid

Editor menu **Fractured Chorus → UI → Apply Color Tokens To Active Scene** — remaps legacy baked `Graphic` colors + name heuristics; save scene after run.

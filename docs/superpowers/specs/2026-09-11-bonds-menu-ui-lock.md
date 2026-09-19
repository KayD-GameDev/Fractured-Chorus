# Bonds Menu UI Lock

> Date: 2026-09-11
> Scope: Task 2 P0 art and composition lock for the Bonds sandbox.
> Priority: sandbox before `CampusHub.unity`

## Visual Lock

- Standalone `BondsLayoutSandbox` ships before any CampusHub integration.
- `_ref/_ref_bonds_menu_v1.jpg` is composition-only and must never be assigned as the runtime background.
- Canvas target is `1920x1080` with title-safe focus inside roughly 90% of the frame.
- No baked letters, numbers, Latin glyphs, or JP glyphs inside generated PNGs.
- Do not generate city background, character busts, radar polygon, or Confirm/Back keycaps.
- Charlotte in the mock is composition-only; runtime character art reuses existing bust/reference assets.

### Zones

| Zone | Runtime lock |
|------|--------------|
| Corner date | `HubCornerInfoHud` shows date/day/phase from `Calendar`. Scene text carries `HIMA CITY` and `Music Lives in You`. |
| Bonds header | Scene chrome with `BondPresentation.Title` and `BondPresentation.TitleJp`. |
| Left nav | `Social Stats` selected and enabled. `Link`, `Conversations`, `Memories`, `Gallery` exist and are muted/disabled. |
| Radar | `SocialStatsRadarGraphic.SetRanks` reads from `SocialStatsState`. |
| Nodes | `SocialStatsNodeView.Bind` with Task 2 stat icons. |
| Roster | Order is `Ren, Charlotte, Coda, Astra, Ryo, MeiLin, reserved`. Default selection is `Charlotte`. Unlocked portraits: Ren, Charlotte, Coda. Astra, Ryo, Mei Lin show locked until story. |
| Detail | Selected NPC bust plus bio, quote, `Rank n`, `{exp}/{threshold}`, and `NEXT RANK` hint. |
| Link Episodes | Five rows. Unlock rule is `bond.Rank >= requiredRank`. Confirm on unlocked rows logs only in sandbox. |
| Background | Reuse `Assets/FracturedChorus/Art/UI/StatusMenu/statusmenu_hima_city_bg_v1.jpg`. |
| MockGuide | Full mock at alpha `0.4`, default off. |

### Episode Titles

1. `A Usual Day` - required rank `1`
2. `After Class` - required rank `2`
3. `A Different Melody` - required rank `3`
4. `Unspoken Words` - required rank `4`
5. `Toward Tomorrow` - required rank `5`

## Global Constraints

- Scene hierarchy and RectTransform layout live only on the `.unity` scene.
- Never hardcode `anchoredPosition`, `sizeDelta`, anchors, or sibling order for existing UI objects in runtime/setup code.
- Seed layout only when creating a new object.
- Do not open, heal, wire, or otherwise modify `Assets/FracturedChorus/Scenes/CampusHub.unity` in this task.
- Palette and copy lock stay consistent with `BondPresentation` and existing UI tokens.
- Generated chrome uses transparent PNGs except the promo interior image.
- QA gate per art file: readable on `#030914`, silhouette reads cleanly, zero baked glyphs.

## File Map

| Path | Responsibility |
|------|----------------|
| `docs/superpowers/specs/2026-09-11-bonds-menu-ui-lock.md` | Visual/data lock for the Bonds sandbox |
| `Assets/FracturedChorus/Art/UI/Bonds/_ref/_ref_bonds_menu_v1.jpg` | Composition mock only |
| `Assets/FracturedChorus/Art/UI/Bonds/Icons/*.png` | Production line icons |
| `Assets/FracturedChorus/Art/UI/Bonds/Kit/*.png` | Production glass chrome and 9-slice kit |
| `Assets/FracturedChorus/Art/UI/Bonds/Decor/*.png` | Promo piano art and locked silhouette |
| `Assets/FracturedChorus/Art/UI/Bonds/README.md` | Reuse and slot notes |
| `Assets/FracturedChorus/Editor/BondsArtImportEditor.cs` | Importer configuration and 9-slice borders |
| `Assets/FracturedChorus/Editor/BondsArtImportTests.cs` | Asset presence/import coverage |

## Art Reuse Rules

| Asset/type | Reuse path |
|------------|------------|
| City BG | `Assets/FracturedChorus/Art/UI/StatusMenu/statusmenu_hima_city_bg_v1.jpg` |
| Ren bust | `Assets/FracturedChorus/Art/Characters/Ren/VnBust/ren_bust_neutral_v1.png` |
| Charlotte bust | `Assets/FracturedChorus/Art/Characters/Charlotte/VnBust/charlotte_bust_neutral_v1.png` |
| Coda bust | `Assets/FracturedChorus/Art/Characters/Coda/VnBust/coda_bust_neutral_v1.png` |
| Astra reference | `Assets/FracturedChorus/Art/Characters/_Reference/LuxeConcert/astra_ref.png` |


# Task 2 Report — Spec lock + generate / import P0 art

**Date:** 2026-09-11  
**Branch:** `branch2`  
**Status:** DONE_WITH_CONCERNS

## Summary

Completed Task 2 scope for Bonds art/spec import preparation:
- kept the spec lock at `docs/superpowers/specs/2026-09-11-bonds-menu-ui-lock.md`
- kept `Assets/FracturedChorus/Art/UI/Bonds/README.md`
- kept the mock reference at `Assets/FracturedChorus/Art/UI/Bonds/_ref/_ref_bonds_menu_v1.jpg`
- kept `Assets/FracturedChorus/Editor/BondsArtImportEditor.cs`
- kept `Assets/FracturedChorus/Editor/BondsArtImportTests.cs`
- authored Unity `.meta` files for all 26 P0 PNGs plus the mock JPG, using HubMenu single-sprite import settings with unique GUIDs and brief-matched `spriteBorder` values on the 9-slice kit assets
- cleaned this report so it contains Task 2 only

PNG generation itself was completed outside this subagent by the controller using Cursor image generation after `belt` auth was unavailable and `cursor.GenerateImage` was unavailable to this subagent model.

## File Coverage

| Area | Result |
|------|--------|
| `Icons/` PNG set | 15 present |
| `Kit/` PNG set | 9 present |
| `Decor/` PNG set | 2 present |
| PNG `.meta` files | 26 authored |
| Mock JPG `.meta` | authored |

## Import Settings Applied

For each P0 PNG `.meta`:
- `textureType: 8`
- `spriteMode: 1`
- `alphaIsTransparency: 1`
- `wrapU/V/W: 1` (Clamp)
- `enableMipMap: 0`
- `filterMode: 1` (Bilinear)

9-slice borders from the brief were applied to:
- `ui_bonds_panel_glass_v1.png` → `48,48,48,48`
- `ui_bonds_header_plate_v1.png` → `80,40,80,40`
- `ui_bonds_nav_selected_v1.png` → `40,24,40,24`
- `ui_bonds_chip_frame_normal_v1.png` → `24,24,24,24`
- `ui_bonds_chip_frame_selected_v1.png` → `24,24,24,24`
- `ui_bonds_chip_locked_v1.png` → `24,24,24,24`
- `ui_bonds_episode_row_v1.png` → `32,20,32,20`
- `ui_bonds_bar_track_v1.png` → `8,6,8,6`
- `ui_bonds_bar_fill_v1.png` → `8,6,8,6`

## Test Summary

| Check | Result |
|------|--------|
| Existence check for spec / README / import editor / tests | PASS |
| C# lints for `BondsArtImportEditor.cs` and `BondsArtImportTests.cs` | PASS |
| Unity importer execution | NOT RUN — Unity Editor unavailable |
| Unity EditMode `BondsArtImportTests` | NOT RUN — Unity Editor unavailable |

## Concerns

1. Some generated kit panels read more filled / 3D than ideal for a hollow 9-slice chrome kit.
2. `ui_bonds_silhouette_locked_v1.png` may still include white ground and should be visually checked in Unity on dark background.
3. Promo piano art has no baked English caption, which is correct; caption remains expected in uGUI text.
4. The mock JPG `.meta` exists, but `BondsArtImportEditor.ConfigureAll()` will still convert that asset to `TextureImporterType.Default` once run in Unity.

## Commit

- Subject: `Add Bonds HUD chrome kit and composition lock for sandbox.`
- SHA: `c66f7d5`

## Follow-up fix (2026-09-11)

- Replaced hollow 9-slice kit PNGs: `ui_bonds_header_plate_v1`, `ui_bonds_chip_frame_selected_v1`, `ui_bonds_chip_locked_v1`.
- Set `_ref_bonds_menu_v1.jpg.meta` `textureType` to `0` (Default) so mock ref matches expected importer without waiting for `ConfigureAll()`.

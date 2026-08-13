# Task 5 Report - Slice tam 2 to PNG layers

**Status:** DONE_WITH_CONCERNS
**Branch:** branch2

## Done

- Added `Assets/FracturedChorus/Editor/LoadingScreenArtImportTests.cs` first, matching the brief's `AssetDatabase.LoadAssetAtPath<Sprite>` existence check for the six loading art PNGs.
- Added `Assets/FracturedChorus/Editor/LoadingScreenArtImportEditor.cs` with menu `Fractured Chorus/Import Loading Screen Art`, source copy paths, black-to-alpha thresholding, connected-component extraction, sprite importer setup, and PNG writing to `Assets/FracturedChorus/Art/UI/LoadingScreen/`.
- Copied the source sheet to `Assets/FracturedChorus/Art/UI/LoadingScreen/_source/loading_screen_part.jpg` and the QA reference to `Assets/FracturedChorus/Art/UI/LoadingScreen/_source/loading_screen_wish.jpg`.
- Generated these runtime layers plus Unity `.meta` files: `loading_clouds.png`, `loading_notes_stars.png`, `loading_skyline.png`, `loading_buildings_signs.png`, `loading_clef.png`, `loading_floor.png`.
- Generated `.meta` files for the new Editor scripts and the new `LoadingScreen` / `_source` folders so Unity can resolve the assets later without recreating import metadata.

## Not Done

- Did not run the Unity EditMode test because `Unity.exe` is unavailable in this environment.
- Did not bake `tam 1` into `loading_background`; the task explicitly forbids that.
- Did not use the wish image as a runtime layer; it was copied only for QA reference under `_source`.

## Verification

- `ReadLints` returned no diagnostics for `Assets/FracturedChorus/Editor/LoadingScreenArtImportEditor.cs` and `Assets/FracturedChorus/Editor/LoadingScreenArtImportTests.cs`.
- External slice run summary: `BLOBS kept=47 clouds=2 notes=14 buildings=29 clefSource=3 skylineSource=3 floor=44`.
- PNG verification script confirmed file existence, RGBA output, and transparent pixels for every layer:
  - `loading_clouds.png` -> `Format32bppArgb`, `365x149`, `transparent=25194`
  - `loading_notes_stars.png` -> `Format32bppArgb`, `608x289`, `transparent=136446`
  - `loading_skyline.png` -> `Format32bppArgb`, `644x199`, `transparent=41668`
  - `loading_buildings_signs.png` -> `Format32bppArgb`, `1024x671`, `transparent=526956`
  - `loading_clef.png` -> `Format32bppArgb`, `428x235`, `transparent=61742`
  - `loading_floor.png` -> `Format32bppArgb`, `609x111`, `transparent=11251`

## Concerns

- The component sheet does not separate perfectly under the brief's connected-component heuristic: one central blob (`id=3`) merged the clef swirl and skyline mass, so both `loading_clef.png` and `loading_skyline.png` required spatial masking on top of connected-component classification.
- `loading_notes_stars.png` and `loading_buildings_signs.png` still reflect heuristic spillover compared with the music-city wish: some signage remains in `notes_stars`, and a few decorative sky fragments remain in `buildings_signs`.

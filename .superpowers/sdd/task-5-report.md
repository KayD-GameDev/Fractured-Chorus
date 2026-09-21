# Task 5 Report - Layout snapshot tool + acceptance

**Status:** DONE_WITH_CONCERNS
**Branch:** branch2

## Done

- Added `Tools/save-bonds-sandbox-layout-snapshot.mjs`, cloned from the CampusHub snapshot tool for `Assets/FracturedChorus/Scenes/BondsLayoutSandbox.unity` and outputting `Assets/FracturedChorus/Art/UI/Bonds/bonds_sandbox_layout_snapshot.json`.
- Added menu `Fractured Chorus/Bonds/Save Layout Snapshot` to `Assets/FracturedChorus/Editor/BondsSceneSetupEditor.cs` and copied the `RunNodeTool` process pattern so the snapshot is saved only from the active sandbox scene.
- Extended `Tools/seed-bonds-layout-sandbox.mjs` so every `Node_*` now contains `Icon`, `Name`, `Rank`, and `Flavor` child objects with Unity-default RectTransforms only; no mock pixel layout was authored in code.
- Re-ran `Tools/seed-bonds-layout-sandbox.mjs`, so `Assets/FracturedChorus/Scenes/BondsLayoutSandbox.unity` now contains those children and each `SocialStatsNodeView` serializes refs to them in YAML.
- Added `Assets/FracturedChorus/Art/UI/Bonds/bonds_sandbox_layout_snapshot.json.meta` so the generated snapshot can be tracked as a Unity asset.

## Not Done

- Did not modify `Assets/FracturedChorus/Scenes/CampusHub.unity`.
- Did not add any sandbox scene to `EditorBuildSettings`.
- Did not serialize the full `BondsMenuUI` reference graph directly into YAML; that remains the job of Unity-side `Attach Missing Layout Objects`, which is allowed by the task note.
- Did not run Play Mode or claim visual acceptance because `Unity.exe` is unavailable in this environment.

## Verification

- Ran `node Tools/seed-bonds-layout-sandbox.mjs` successfully.
- Ran `node Tools/save-bonds-sandbox-layout-snapshot.mjs` successfully; generated snapshot contains `149` nodes and the expected `BondsCanvas` root children.
- Verified `BondsLayoutSandbox.unity` now contains repeated `Icon` / `Name` / `Rank` / `Flavor` children under all five `Node_*` entries and `SocialStatsNodeView` YAML refs are no longer `0`.
- `ReadLints` returned no diagnostics for `Assets/FracturedChorus/Editor/BondsSceneSetupEditor.cs`.
- Repository search shows the new snapshot JSON is produced only by the tool/editor path and is not consumed by runtime C#.

## Human Acceptance Checklist

- Open `Assets/FracturedChorus/Scenes/BondsLayoutSandbox.unity` in Unity.
- Run `Fractured Chorus/Bonds/Attach Missing Layout Objects` if `BondsMenuUI` refs are still empty in the Inspector.
- Save the scene, then run `Fractured Chorus/Bonds/Save Layout Snapshot`.
- Enter Play Mode at `1920x1080` and manually verify every acceptance item from `D:\Fractured-Chorus1\.superpowers\sdd\task-5-brief.md`.
- Confirm `EditorBuildSettings` still excludes the sandbox scene.
- Confirm this task did not add changes to `CampusHub.unity`; the repository already had a pre-existing diff on that file before this task.

## Concerns

- `Assets/FracturedChorus/Scenes/CampusHub.unity` is still dirty in git, but that diff pre-existed and was not touched here.
- Final visual sign-off for safe-zone, selected nav state, promo caption behavior, and font application still requires a human Unity pass.

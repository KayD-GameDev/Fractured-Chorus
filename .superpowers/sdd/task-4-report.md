# Task 4 Report - View components + BondsMenuUI bind

**Status:** DONE
**Branch:** branch2

## Done

- Replaced the stub in `Assets/FracturedChorus/Hub/BondsMenuUI.cs` with sandbox-only bind logic for social stats, roster selection wrap/skip, detail card refresh, episode list refresh, `Show`, `Hide`, `SelectedNpcId`, and pure `WrapRosterIndex`.
- Added `Assets/FracturedChorus/Hub/BondRosterChipView.cs`, `Assets/FracturedChorus/Hub/BondDetailCardView.cs`, and `Assets/FracturedChorus/Hub/BondEpisodeRowView.cs` as bind-only view components.
- Updated `Assets/FracturedChorus/Editor/BondsSceneSetupEditor.cs` so `AttachMissing` now wires `SerializeField` references by hierarchy name and adds missing components without writing any RectTransform layout values.
- Added the wrap regression test to `Assets/FracturedChorus/Editor/BondPresentationTests.cs`.

## Not Done

- Did not run EditMode tests because `Unity.exe` is unavailable in this environment.
- Did not run the manual Play Mode walkthrough on `BondsLayoutSandbox.unity` for the same reason.
- Did not modify `CampusHub.unity`.

## Verification

- `ReadLints` returned no diagnostics for the edited C# files.
- `git diff --check` passed on the Task 4 code changes.
- Static search confirmed the new `BondsMenuUI` runtime code contains zero `anchoredPosition`, `sizeDelta`, `Stretch`, or `SetSiblingIndex` usage.

## Concerns

- `AttachMissing` can only bind scene children that already exist by the expected names; the current sandbox scene snapshot still shows the five stat node roots without named child labels/images, so those subfields stay null until the scene objects are added manually.

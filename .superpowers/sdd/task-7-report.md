# Task 7 Report

## Scope
- Added a grep-based guard test at `Assets/FracturedChorus/Editor/LoadingScreenNoSyncLoadTests.cs`.
- Updated `Assets/FracturedChorus/Combat/Core/CombatController.cs` so `OnResultRetry()` reloads through `RunMapSceneLoader.LoadByName(SceneManager.GetActiveScene().name)`.
- Updated `Assets/FracturedChorus/Menu/MainMenuStartGameController.cs` so NEW/LOAD skip the black fade overlay, still play transition SFX, duck BGM, call `GameMetaSession.LoadSlot(slot)` before load for LOAD, and re-enable the menu if `LoadByName` fails.

## TDD Evidence
### Fail before fix
- Regex used: `SceneManager\.LoadScene\s*\(`.
- `rg` before the code change matched:
  - `Assets/FracturedChorus/Combat/Core/CombatController.cs:1219`

### Pass after fix
- `rg` after the code change returned no matches under `Assets/FracturedChorus` for `SceneManager\.LoadScene\s*\(`.
- PowerShell validation excluding `/Editor/` and `LoadingScreenController.cs` returned:
  - `SYNC_LOAD_MATCHES: none`

## Files Changed
- `Assets/FracturedChorus/Editor/LoadingScreenNoSyncLoadTests.cs`
- `Assets/FracturedChorus/Combat/Core/CombatController.cs`
- `Assets/FracturedChorus/Menu/MainMenuStartGameController.cs`

## Done
- Guard test written first.
- Combat retry routed through the loading overlay path.
- Main menu NEW/LOAD now hand off directly to the loading overlay path without the black fade overlay.
- Failure handling added so menu interaction is restored when scene loading fails.

## Not Done
- Unity EditMode test execution was not run because `Unity.exe` is unavailable in this environment.
- No unrelated pre-existing workspace changes were modified or reverted.

## Concerns
- `CombatController.cs` already contained unrelated unstaged edits before this task, so only the `OnResultRetry()` hunk should be staged for the task commit.

## Review follow-up
- `MainMenuStartGameController.cs`: when `LoadByName` returns false in `BeginNewGameRoutine` and `LoadGameRoutine`, call `_bgmController?.RestoreVolume()` alongside menu re-enable so ducked BGM is not left muted after a failed load handoff.

# Task 4 Report — LoadingScreenController + async load

**Status:** DONE
**Branch:** branch2

## Done

- Added `Assets/FracturedChorus/UI/Loading/LoadingScreenController.cs` with `Ensure()`, singleton busy state, async `BeginLoad`, fade timing via `LoadingProgress`, and `Resources.Load("UI/LoadingScreen")` fallback to a root `GameObject`.
- Updated `Assets/FracturedChorus/RunMap/RunMapSceneLoader.cs` so `LoadByName` now checks whitespace, busy state, `CanLoad`, then delegates to `LoadingScreenController.Ensure().BeginLoad(sceneName, mode)`.
- Added `Assets/FracturedChorus/Editor/LoadingScreenControllerBusyTests.cs` with the required EditMode coverage for empty and unknown scene names plus `TearDown` cleanup via `DestroyImmediate(Instance.gameObject)`.
- Kept `EnsureView()` scoped to root `CanvasGroup` + `LoadingScreenView` only; no random child `Image` binding and no prefab/art builder work in this task.
- Confirmed `Assets/FracturedChorus/RunMap/RunMapSceneLoader.cs` contains zero `SceneManager.LoadScene(` calls after the change.

## Not Done

- Did not execute EditMode tests because `Unity.exe` is unavailable in this environment and the task explicitly forbids searching for it.
- Did not create prefab/art slice work reserved for Task 5-6.

## Verification

- `ReadLints` returned no diagnostics for the edited C# files.
- Static check confirmed no sync load call remains in `RunMapSceneLoader.cs`.

## Concerns

- The fallback instance supports async scene loading flow and busy checks, but without the future prefab/resources from Task 6 it does not provide a fully wired visible bar hierarchy.

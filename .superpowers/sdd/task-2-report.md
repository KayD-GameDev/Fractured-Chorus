# Task 2 Report — RunMapSceneLoader.CanLoad

## Status

**DONE_WITH_CONCERNS** — `CanLoad` and public `ResolveScenePath` implemented per brief; EditMode tests not executed from CLI (no Unity Editor).

## Deliverables

| File | Action |
|------|--------|
| `Assets/FracturedChorus/RunMap/RunMapSceneLoader.cs` | Modified — added `CanLoad`, `ResolveScenePath` → public |
| `Assets/FracturedChorus/Editor/RunMapSceneLoaderCanLoadTests.cs` | Created |
| `Assets/FracturedChorus/Editor/RunMapSceneLoaderCanLoadTests.cs.meta` | Created |

## Implementation summary

`RunMapSceneLoader.CanLoad(string sceneName)`:

- Returns `false` for null/whitespace.
- Resolves path via `ResolveScenePath`, checks `SceneUtility.GetBuildIndexByScenePath` — returns `true` if build index ≥ 0.
- Otherwise falls back to `Application.CanStreamedLevelBeLoaded(sceneName)`.
- Does not call `SceneManager.LoadScene`.

`ResolveScenePath` changed from `private static` to `public static` (same body as before).

`LoadByName` unchanged — still synchronous `SceneManager.LoadScene` (Task 4 scope).

## Tests (TDD)

Written first in `RunMapSceneLoaderCanLoadTests` (`FracturedChorus.Tests`):

1. `CanLoad_Empty_IsFalse` — `""`, `"   "`, `null`
2. `CanLoad_KnownScenes_IsTrue` — MainMenuStartGame, PrologueVN, CombatPrototype
3. `CanLoad_Unknown_IsFalse` — `DefinitelyMissingScene_XYZ`

Expected TDD flow: Step 2 FAIL (missing `CanLoad`) → Step 3 implement → Step 4 PASS.

## Test execution

- Unity Hub Editor folder has no installed `Unity.exe` (6000.2.6f1 metadata stub only).
- **EditMode not run** in this environment.

**Recommended verification:** Unity → Test Runner → EditMode → run `RunMapSceneLoaderCanLoadTests`.

Build Settings (`ProjectSettings/EditorBuildSettings.asset`) already includes MainMenuStartGame, PrologueVN, and CombatPrototype — `CanLoad_KnownScenes_IsTrue` should pass in Editor.

## Self-review

- Only task-scoped files committed; `LoadingProgress` untouched.
- No new comments in source.
- `LoadByName` not converted to async.
- Commit message matches brief (≤72 chars, why-focused).

## Commit

- **SHA:** `3bbf28a`
- **Subject:** Expose scene load checks without starting a load.
- **Files:** 3 changed, 58 insertions, 1 deletion

## Concerns

1. EditMode tests unverified in Unity (no Editor install).
2. `CanLoad_KnownScenes_IsTrue` depends on Build Settings — scenes are present today; regression if scenes removed from build list.

## Not done

- Async `LoadByName` (Task 4).
- Push to remote (explicitly excluded).
- Modifications to `LoadingProgressTests.cs` (brief allowed either file; used separate file per preference).

# Task 1 Report — LoadingProgress math

## Status

**DONE_WITH_CONCERNS** — Implementation and tests match the brief; EditMode tests were not executed from CLI.

## Deliverables

| File | Action |
|------|--------|
| `Assets/FracturedChorus/UI/Loading/LoadingProgress.cs` | Created |
| `Assets/FracturedChorus/UI/Loading/LoadingProgress.cs.meta` | Created |
| `Assets/FracturedChorus/Editor/LoadingProgressTests.cs` | Created |
| `Assets/FracturedChorus/Editor/LoadingProgressTests.cs.meta` | Created |

## Implementation summary

`LoadingProgress` is a static helper in namespace `FracturedChorus.UI.Loading`:

- **Constants** (verbatim from brief): `UnityActivationCap=0.9f`, `FadeInSec=0.20f`, `FadeOutSec=0.25f`, `MinHoldSec=0.80f`, `SmoothTime=0.12f`, `ActivateFill=0.99f`, `PercentVisibleMin=0.02f`.
- **`MapAsyncProgress(float)`** — returns `0` for non-positive input; otherwise `Clamp01(unityProgress / UnityActivationCap)`.
- **`CanActivate(float displayedFill, float holdElapsedSec)`** — true when `displayedFill >= ActivateFill` and `holdElapsedSec >= MinHoldSec`.

## Tests (TDD)

Written first per brief (`LoadingProgressTests` in `FracturedChorus.Tests`):

1. `MapAsyncProgress_Zero_IsZero`
2. `MapAsyncProgress_Cap_IsOne`
3. `MapAsyncProgress_HalfCap_IsHalf`
4. `MapAsyncProgress_AboveCap_ClampsToOne`
5. `CanActivate_RequiresFillAndHold`

Manual trace against expected values: all five cases pass by inspection.

## Test execution

- Checked `C:\Program Files\Unity\Hub\Editor` — directory exists but contains no installed Editor versions (no `Unity.exe` found).
- `where Unity` — not on PATH.
- **EditMode not run** — no Unity Editor available for batchmode from this environment.

**Recommended verification:** Unity → Window → General → Test Runner → EditMode → run `LoadingProgressTests`.

## Self-review

- Signatures and constants match the brief exactly.
- `FadeOutSec` is `0.25f` (float literal), not a string or seconds suffix.
- No explanatory comments in new source.
- Commit scoped to the four task files only (no `git add -A`).
- Folder `Assets/FracturedChorus/UI/Loading/` has no `Loading.meta` in the commit; Unity may generate it on first import — add in a follow-up if the project requires folder meta for version control.

## Commit

- **SHA:** `0a1065a`
- **Subject:** Add loading progress mapping for async scene activation.
- **Files:** 4 changed, 84 insertions

## Concerns

1. EditMode tests unverified in Unity (no Editor install detected).
2. `Loading.meta` for the new folder was not committed (not listed in brief); confirm after opening project in Unity.

## Not done

- UI, scene load, art (out of scope for Task 1).
- Push to remote (explicitly excluded).

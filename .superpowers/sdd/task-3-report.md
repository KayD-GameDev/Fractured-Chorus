# Task 3 Report — LoadingScreenView (bar live)

## Status

**DONE_WITH_CONCERNS** — Implementation and tests match the brief; EditMode tests were not executed from CLI (Unity.exe not installed).

## Deliverables

| File | Action |
|------|--------|
| `Assets/FracturedChorus/UI/Loading/LoadingScreenView.cs` | Created |
| `Assets/FracturedChorus/UI/Loading/LoadingScreenView.cs.meta` | Created |
| `Assets/FracturedChorus/Editor/LoadingScreenViewTests.cs` | Created (preferred over appending `LoadingProgressTests.cs`) |
| `Assets/FracturedChorus/Editor/LoadingScreenViewTests.cs.meta` | Created |

## Implementation summary

`LoadingScreenView` (`FracturedChorus.UI.Loading`):

- **Constants:** `BarWidth=720`, `BarHeight=36`, `NeonPink=(1, 0.306, 0.784)`, `FillWhite=(1, 0.92, 0.96)`.
- **API:** `SetProgress`, `SetVisible(visible, instant)`, `TickMotion`, `Group`, `Bind` / `BindLayers`, `BuildForTests`.
- **`SetProgress`:** clamps `p`; sets fill; percent text `{RoundToInt(p*100)}%`; hide when `p < LoadingProgress.PercentVisibleMin`; `percentRect.x = Lerp(24, BarWidth-40, p)`.
- **`SetVisible`:** uses the unambiguous body only — `blocksRaycasts=visible`, `interactable=false`, set alpha only when `instant`.
- **`BuildForTests`:** CanvasGroup + filled Image + Text; no full canvas hierarchy.
- **`TickMotion`:** clef scale pulse + notes vertical bob (unchanged from brief).

Did **not** change `LoadingProgress` math. Did **not** create Controller. Did **not** slice art.

## Tests (TDD)

Written first in `LoadingScreenViewTests`:

1. `SetProgress_SetsFillAndPercent` — 0.75 → fill 0.75, `"75%"`, visible
2. `SetProgress_HidesPercentNearZero` — 0 → percent hidden

Manual trace: both cases match `PercentVisibleMin=0.02` and percent formatting.

## Test execution

- Unity.exe not installed (per task instruction: do not search).
- **EditMode not run** — not executable in this environment.

**Recommended verification:** Unity → Test Runner → EditMode → `LoadingScreenViewTests`.

## Self-review

- Ambiguous first `SetVisible` ternary body was **not** used; second body only.
- Tests in new file (cleaner than stuffing `LoadingProgressTests.cs`).
- No explanatory comments in source.
- Commit scoped to four task files only (no `git add -A`).
- `NeonPink` / `FillWhite` present for later wire-up; unused in current methods (same as brief skeleton).
- Brief lists `UiFontCatalog.Body` as consumer; `BuildForTests` uses builtin `LegacyRuntime.ttf` per provided snippet (catalog reserved for real UI build later).

## Commit

- **SHA:** `c9be144`
- **Subject:** Add loading bar view with live fill and percent.
- **Files:** 4 changed, 182 insertions

## Concerns

1. EditMode unverified (no Unity Editor).
2. `NeonPink` / `FillWhite` unused until real bar/art bind (may warn CS0414 depending on project warning settings).
3. `UiFontCatalog.Body` not applied in `BuildForTests` (matches brief code; defer to scene/builder task).

## Not done

- Controller (Task 4).
- Art slice / full canvas hierarchy.
- Push to remote (excluded).

## Review fix (UiFontCatalog.Body)

- **Change:** `LoadingScreenView` now assigns `UiFontCatalog.Body` in `BuildForTests()` and `Bind()`; `loadingLabel` also gets `FontStyle.Bold` when present.
- **Tests:** Unity.exe not installed — EditMode not re-run (per task instruction: do not search).
- **Commit:** `1e72957` — Use catalog body font on the loading bar labels.
- **Files:** `Assets/FracturedChorus/UI/Loading/LoadingScreenView.cs` only.

# Task 1 Report — Pure gesture helper + EditMode tests

**Status:** DONE  
**Branch:** branch2  
**Commit:** `f9fcb49` — Add click-vs-drag gesture helper with EditMode tests.

## Summary

Added `BoardPointerGesture` static helper in `Assets/FracturedChorus/UI/BoardPointerGesture.cs` with two public methods:

- `ShouldCommitDrag(pointerDownScreen, currentScreen, thresholdPx)` — returns `true` when screen-space distance is **strictly greater than** `thresholdPx`.
- `IsClick(pointerDownScreen, releaseScreen, thresholdPx)` — returns `!ShouldCommitDrag(...)`.

Added EditMode NUnit tests in `Assets/FracturedChorus/Editor/BoardPointerGestureTests.cs` (Editor fallback; no asmdef in project).

`BoardDragController` was **not** modified (Task 2 scope).

## TDD Evidence

### RED (Step 3)

Tests written first. `Tools/check-compile.ps1` failed on `Assembly-CSharp-Editor`:

```
Assets\FracturedChorus\Editor\BoardPointerGestureTests.cs(13,28): error CS0103: The name 'BoardPointerGesture' does not exist in the current context
(... 4 more CS0103 errors ...)
COMPILE FAILED (1 assembly/assemblies)
```

### GREEN (Step 5)

After implementing `BoardPointerGesture.cs`:

```
== Assembly-CSharp ==
== Assembly-CSharp-Editor ==
COMPILE OK
```

Unity Test Runner CLI / batchmode test script not available in repo; EditMode tests compile and are discoverable via Test Runner window (EditMode → `BoardPointerGestureTests`).

## Files Created

| File | Purpose |
|------|---------|
| `Assets/FracturedChorus/UI/BoardPointerGesture.cs` | Pure gesture threshold helper |
| `Assets/FracturedChorus/Editor/BoardPointerGestureTests.cs` | 3 NUnit EditMode tests |

Not created (by design):

- `Assets/FracturedChorus/Tests/EditMode/*` — skipped; zero asmdefs in project; brief prefers Editor fallback.
- ~~`BoardPointerGesture.cs.meta`~~ — added in review fix commit `b5f59fb` (see below).

## Test Coverage

| Test | Asserts |
|------|---------|
| `ShouldCommitDrag_False_WhenDistanceAtOrBelowThreshold` | Distance exactly 8px and 0px with threshold 8 → `false` |
| `ShouldCommitDrag_True_WhenDistanceAboveThreshold` | Distance 8.1px with threshold 8 → `true` |
| `IsClick_True_OnlyWhenNotCommitted` | 5px diagonal (3,4) → click; 10px horizontal → not click |

## Self-Review

- **Threshold semantics:** Uses `Vector2.Distance(...) > thresholdPx` (strict `>`), matching brief and `BoardDragController.clickDragThresholdPx = 8f` intent.
- **No side effects:** Static pure functions; no Unity lifecycle or input dependencies.
- **Namespace:** `FracturedChorus.UI` for helper; `FracturedChorus.Tests` for tests — consistent with project layout.
- **No comments** in new source files per global constraint.
- **Scope:** Only Task 1 files committed; unrelated working-tree changes left unstaged.
- **Risk:** Low. Logic is trivial; boundary behavior locked by tests.

## Verification

- [x] `Tools/check-compile.ps1` — COMPILE OK (initial GREEN + post-review-fix `b5f59fb`)
- [ ] Unity Test Runner EditMode run — not executed (no CLI); compile-only verification
- [x] `BoardDragController` unchanged

## Review Fix (2026-08-01)

**Finding:** `BoardPointerGesture.cs.meta` and `BoardPointerGestureTests.cs.meta` were not committed with Task 1.

**Fix commit:** `b5f59fb` — Add Unity .meta for BoardPointerGesture assets.

| File | GUID |
|------|------|
| `Assets/FracturedChorus/UI/BoardPointerGesture.cs.meta` | `2f376d7e697c40bc9e1a220d89acc8a5` |
| `Assets/FracturedChorus/Editor/BoardPointerGestureTests.cs.meta` | `0a96f1173d4c4056b8588041c6f2bd1d` |

**Compile check (post-fix):** see Verification below.

## Next (Task 2)

Wire `BoardPointerGesture` into `BoardDragController` so pointer-down does not immediately start drag; defer drag commit until movement exceeds threshold.

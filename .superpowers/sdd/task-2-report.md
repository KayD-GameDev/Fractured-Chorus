# Task 2 Report — Deferred drag in BoardDragController

**Status:** DONE  
**Branch:** branch2  
**Commit:** `ae9c929` — Defer unit drag so short clicks open the skill panel.

## Summary

Wired `BoardDragController` to consume `BoardPointerGesture` from Task 1. Pointer-down records pick state only; drag commits after 8px threshold in `Update`; pointer-up ends drag or fires `_onUnitClicked` on short click.

## Changes (`BoardDragController.cs`)

| Area | Before | After |
|------|--------|-------|
| Class summary | Press-and-hold drag; click after Execute | Planning window: click → skill panel; drag → reposition |
| `HandlePointerDown` | Eager `BeginDrag` when `CanDragUnit` | Store `_pointerDownUnit` / screen; set `_dragPointerActive`; reset `_draggingUnit` |
| `Update` (held) | `UpdateDragAtScreen` only if already dragging | `ShouldCommitDrag` → `BeginDrag`; then `UpdateDragAtScreen` |
| `HandlePointerUp` | Manual distance check + `_dragPointerActive &&` drag guard | `EndDrag` if dragging; else `IsClick` + `CanOpenSkillPanelFor` → click handler |
| `CancelActiveDrag` | Early return when no drag | Always clear pointer state; cancel snap if dragging |

## Gates (unchanged)

- **Drag:** `CanDragUnit` → `IsPlanningWindowOpen` + alive player unit
- **Skill click:** `CanOpenSkillPanelFor` → same session gate + optional `_canOpenSkillPanel` predicate
- **CombatSession / SkillPanelUIView:** not modified

## Verification

```
== Assembly-CSharp ==
== Assembly-CSharp-Editor ==
COMPILE OK
```

- [x] `Tools/check-compile.ps1` — COMPILE OK
- [ ] Play Mode QA — not run (Task 3+ scope)
- [x] Only `BoardDragController.cs` committed

## Self-Review

- **Deferred drag:** No `BeginDrag` on pointer-down; threshold uses shared helper with strict `>` (8.1px commits, 8px does not).
- **Click path:** `IsClick` delegates to `ShouldCommitDrag` negation — consistent with Task 1 tests.
- **Pointer-down reset:** `_draggingUnit = null` on new press clears stale drag state before pick; safe for single-pointer input.
- **`CancelActiveDrag`:** Now clears held-pointer state even when not mid-drag (e.g. pointer-down without threshold crossed) — avoids stuck `_dragPointerActive`.
- **Behavior change:** Skill panel opens on short click during **Planning window** (was documented as post-Execute only); matches design spec for this task.
- **Risk:** Low-medium until Play Mode confirms no double-fire with skill panel UI; compile-only verification here.

## Files Modified

| File | Action |
|------|--------|
| `Assets/FracturedChorus/UI/BoardDragController.cs` | Modified |

## Not Done

- Play Mode / EditMode integration tests for full click-vs-drag flow on board
- Tasks 3+ (timeline lock, CombatSession gates if any)

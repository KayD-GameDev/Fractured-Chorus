# Design: Planning Window Skill Click + Timeline Beat Lock

> **Date:** 2026-08-01  
> **Status:** Approved for planning  
> **Related:** [`2026-08-01-uniform-beat-continuous-music-design.md`](./2026-08-01-uniform-beat-continuous-music-design.md) · `TimelineLayoutLock` · `BoardDragController`

## Problem

After merging Deploy into Planning (`IsPlanningWindowOpen`), the player can drag units but **cannot open the skill panel** by clicking a unit.

Root cause in `BoardDragController`:

1. `HandlePointerDown` calls `BeginDrag` whenever `CanDragUnit` is true.
2. `CanDragUnit` is now true for the entire planning window (reposition ‖ skill assign).
3. `HandlePointerUp` always takes the `EndDrag` branch when a drag was started, so `_onUnitClicked` (skill panel) never runs.

Previously, after Deploy locked reposition, `CanDragUnit` became false and clicks opened the panel. Concurrent agency broke that split.

Secondary need: lock beat slot / ScanBar / TrackLine sizes from `CombatTutorial` so runtime rebuilds cannot shrink them again (`slotWidth` 52 overwrote `Beat_0` 73.85).

## Goals

1. In every planning window: **short click = open skill panel**, **drag past threshold = reposition unit**.
2. Persist canonical sizes for beat slots, ScanBar, and TrackLine (scope A only).
3. No change to `IsPlanningWindowOpen` semantics or combat phase model.

## Non-goals

- Sync Prototype outer `BeatTimelineUI` frame to Tutorial.
- Lock LeftRail, note band, lane avatar gutter, boss note number layout.
- Tutorial copy rewrite.
- Player-facing drag-threshold setting.
- Re-splitting Deploy and Planning into separate phases.

## Interaction model (deferred drag)

| Gesture | Result |
|---------|--------|
| Pointer-down on player unit | Record `_pointerDownUnit` only — **do not** `BeginDrag` |
| Pointer held + move > `clickDragThresholdPx` (default 8) + `CanDragUnit` | Commit drag: `BeginDrag` + follow feet / highlight |
| Pointer-up, drag never committed | If not moved past threshold and `CanOpenSkillPanelFor` → `_onUnitClicked` → skill panel |
| Pointer-up after drag committed | `EndDrag` (move / swap / snap) as today |

Session gates unchanged:

- Reposition and skill panel both require `CombatSession.IsPlanningWindowOpen`.
- While timeline is running (`IsTimelineRunning` / `IsPlaybackActive`), neither opens nor drags.

## Architecture

### Unit: `BoardDragController` (primary fix)

**Does:** Resolve click vs drag with a commit threshold.  
**Depends on:** `CombatSession.IsPlanningWindowOpen`, existing click handler / skill-panel predicate.  
**Interface:** No public API change required. Internal state:

- Keep `_pointerDownUnit`, `_pointerDownScreen`, `clickDragThresholdPx`.
- Add explicit `_dragCommitted` (or equivalent: only set `_draggingUnit` after threshold).
- `Update` while held: if not yet committed and distance > threshold → `BeginDrag`.
- `HandlePointerUp`: committed → `EndDrag`; else click path → `_onUnitClicked`.
- `CancelActiveDrag` / UI-blocked pointer: clear pointer + drag state.

### Unit: `TimelineLayoutLock` (layout persistence)

**Does:** Canonical constants for beat/ScanBar/TrackLine.  
**Source of truth:** `CombatTutorial.unity` (`Beat_0`, `ScanBar`, `TrackLine`).

| Constant | Value | Source |
|----------|-------|--------|
| `SlotWidth` | 73.85 | `Beat_0.sizeDelta.x` / `preferredWidth` |
| `MinSlotWidth` | 14 | Inspector floor |
| `ScanBarWidth` | 6 | ScanBar `sizeDelta.x` |
| `ScanBarVerticalInset` | -4 | ScanBar `sizeDelta.y` |
| `TrackLineY` | 6 | TrackLine `anchoredPosition.y` |
| `TrackLineHeight` | 2 | TrackLine `sizeDelta.y` |

`ResolveSlotWidth` / `ClampSlotWidth` already prevent shrink below `SlotWidth` when `preserveSceneLayout` is true. Extend so `AlignScanBar` and `ApplyTrackLineLayout` read ScanBar/TrackLine constants instead of magic numbers.

### Untouched

- `CombatSession`, `SkillPanelUIView` gates, `BeatTimelineEngine.CanAssignAction`.
- Music / Beat Offset Anchor / Execute button flow.

## Data flow

```
PointerDown(unit)
  → store candidate
PointerMove(held)
  → if dist > threshold && CanDragUnit → BeginDrag (committed)
PointerUp
  → if committed → EndDrag → formation change (optional)
  → else if CanOpenSkillPanelFor → FocusPlayerUnit / ToggleForUnit → drag skill to lane → TryAssignPlayerAction
```

## Error handling

- Invalid / NaN pointer positions: keep existing early-return (no raycast).
- Pointer-up over blocked UI: clear state; do not open panel if press was blocked.
- Unit dies mid-drag: existing `CanDragUnit` / snap-home paths remain.
- If skill panel predicate fails while click path runs: no-op (same as today).

## Testing / acceptance

1. Planning: short click unit → skill panel opens; drag skill to lane → assign succeeds.  
2. Drag unit > ~8px → move/swap; drop off-grid → snap home.  
3. Short click leaves unit on its cell (no positional drift).  
4. During Execute: no panel, no unit drag.  
5. First planning window and mid-fight planning windows both support click + drag.  
6. After Play / ForceRefit: slot width ≥ 73.85; ScanBar 6×(-4); TrackLine y=6 h=2.

## Implementation sketch (for plan)

1. Refactor `BoardDragController` pointer down/move/up to deferred drag.  
2. Expand `TimelineLayoutLock` with TrackLine Y/height; wire `BeatTimelineUIView`.  
3. Confirm scene `slotWidth` / `Beat_0.preferredWidth` stay 73.85 on Tutorial + Prototype.  
4. Update `SCENE_SETUP.md` one paragraph on click vs drag.  
5. Play Mode smoke on acceptance list above.

## Decisions log

| Topic | Choice |
|-------|--------|
| Skill failure mode | A — panel does not open; drag works |
| Interaction | A — click = panel, drag past threshold = reposition |
| Layout lock scope | A — beat slot + ScanBar + TrackLine only |
| Approach | Deferred drag in `BoardDragController` |

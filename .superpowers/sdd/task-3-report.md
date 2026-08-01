# Task 3 Report — TimelineLayoutLock + ScanBar / TrackLine

**Status:** DONE
**Branch:** branch2
**Commit:** `c29083f` — Lock TrackLine and ScanBar sizes to CombatTutorial constants.

## Summary

Extended `TimelineLayoutLock` with `TrackLineY` (6) and `TrackLineHeight` (2). Wired `BeatTimelineUIView.ApplyTrackLineLayout`, `GetScanLineX`, and verified `AlignScanBar` already uses `ScanBarWidth` / `ScanBarVerticalInset` constants.

## Changes

| File | Change |
|------|--------|
| `TimelineLayoutLock.cs` | New static class: `SlotWidth`, `ScanBar*`, `TrackLineY`, `TrackLineHeight`, `ClampSlotWidth`, `ResolveSlotWidth` |
| `BeatTimelineUIView.cs` | `ApplyTrackLineLayout` → lock constants; `GetScanLineX` → `ClampSlotWidth(slotWidth) * 0.5f`; `AlignScanBar` → lock constants (prior drift fixed) |

## Scene Lock Verification

```powershell
Select-String -Path CombatTutorial.unity,CombatPrototype.unity -Pattern "slotWidth:|m_PreferredWidth: 73|m_SizeDelta: \{x: 73.85"
```

| Scene | slotWidth | Beat_0 preferredWidth | Beat_0 sizeDelta.x |
|-------|-----------|----------------------|-------------------|
| CombatTutorial | 73.85 | 73.85 | 73.85 |
| CombatPrototype | 73.85 | 73.85 | 73.85 |

No YAML rewrite required — scenes already match canonical lock.

## Verification

```
== Assembly-CSharp ==
== Assembly-CSharp-Editor ==
COMPILE OK
```

- [x] `Tools/check-compile.ps1` — COMPILE OK
- [x] Scene Select-String — both scenes 73.85
- [x] Only `TimelineLayoutLock.cs` + `BeatTimelineUIView.cs` committed
- [ ] Play Mode QA — not run (layout scope A only)

## Self-Review

- **TrackLine:** Magic `6f` / `2f` replaced with `TimelineLayoutLock.TrackLineY` / `TrackLineHeight`.
- **ScanBar X:** `GetScanLineX` now clamps slot width before half-beat center — consistent with locked beat strip.
- **AlignScanBar:** Already wired to `ScanBarWidth` (6) and `ScanBarVerticalInset` (-4); no remaining magic numbers in scope A.
- **Risk:** Low until Play Mode confirms scroll/scan alignment at 73.85 beat width.

## Not Done

- Play Mode visual QA for TrackLine / ScanBar alignment during scroll
- Layout scope B+ (Header, LeftRail, BossTrackFrame runtime wiring beyond serialized defaults)

## Important/Critical Review Fixes

**Status:** DONE

### Changes

- `TimelineLayoutLock.ClampSlotWidth` now returns `SlotWidth` for non-positive input and clamps all other values to at least `73.85`.
- `ResolveSlotWidth` now clamps serialized widths to at least `SlotWidth`.
- Removed the XML summary block from `TimelineLayoutLock`.
- Added `TimelineLayoutLock.cs.meta` with GUID `8c4e2a91b7d64f0e9a3f5c1d6e8b2a47`.
- Committed only `m_PreferredWidth` and `slotWidth` scene lock fields; `m_SizeDelta.x` was already `73.85` in HEAD.
- Preserved `TrackLineY` and `TrackLineHeight` wiring. Did not touch `BoardDragController` or `BoardPointerGesture`.

### Commits

- `153d34a` — Lock timeline scene widths to canonical spacing.
- `149ced0` — Prevent timeline slots from shrinking below canonical width.

### Verification

```powershell
Select-String -Path "Assets/FracturedChorus/Scenes/CombatTutorial.unity","Assets/FracturedChorus/Scenes/CombatPrototype.unity" -Pattern "slotWidth: 73.85","m_PreferredWidth: 73.85","m_SizeDelta: \{x: 73.85"
```

Result: both scenes contain all three canonical `73.85` values.

```powershell
Tools/check-compile.ps1
```

Result:

```text
== Assembly-CSharp ==
== Assembly-CSharp-Editor ==
COMPILE OK
```

# Task 1 Report — Presentation + episode catalog (TDD)

**Date:** 2026-09-11  
**Branch:** `branch2`  
**Status:** DONE_WITH_CONCERNS

## Summary

Implemented pure C# Bonds HUD presentation layer and link-episode catalog per task brief, with 8 NUnit EditMode tests. Committed only the 6 task files (3 `.cs` + 3 `.meta`).

## Files Created

| File | Purpose |
|------|---------|
| `Assets/FracturedChorus/Editor/BondPresentationTests.cs` | 8 EditMode tests (exact copy from brief) |
| `Assets/FracturedChorus/Hub/BondPresentation.cs` | Static copy/constants, roster order, display names, bios, quotes, portrait unlock |
| `Assets/FracturedChorus/Hub/BondLinkEpisodeCatalog.cs` | `BondLinkEpisode` struct + 5-episode catalog + rank gate helper |

## TDD Steps

### Step 1 — Failing tests written

`BondPresentationTests.cs` added with all 8 tests from brief:
- `RosterOrder_MatchesHubBondList`
- `VisibleChipCount_IsSixNpcsPlusReserved`
- `DisplayNames_MatchLock`
- `RoleLabel_OnlyRenIsPlayer`
- `PortraitUnlock_Arc1VisibleFour`
- `CharlotteCopy_MatchesMock`
- `LinkEpisodes_FiveTitles_RankGates`
- `EpisodeUnlock_UsesBondRank`

### Step 2 — Verify fail (type missing)

**Not executed in CI.** Unity Editor 6000.4.0f1 (`ProjectVersion.txt`) not found on this machine (`Unity.exe` absent under `Program Files\Unity\Hub\Editor` and common alternate paths). Expected compile failure before implementation was not observed locally.

### Step 3 — Implementation

Added `BondPresentation.cs` and `BondLinkEpisodeCatalog.cs` verbatim from brief. Reuses existing `FracturedChorus.Meta.BondNpcIds` from `BondState.cs` — no duplication.

Key contracts:
- `RosterOrder`: ren, charlotte, coda, astra, ryo, mei_lin
- `VisibleChipCount`: 7
- Portrait unlock: ren/charlotte/coda/astra only
- 5 link episodes with ranks 1–5
- `IsUnlocked(bondRank, requiredRank)`: `bondRank >= requiredRank && requiredRank >= 1`

### Step 4 — Verify pass

**Not executed.** Unity Test Runner unavailable. Implementation matches brief exactly; all 8 assertions are satisfied by static data/logic review.

### Step 5 — Commit

```
9da4f4c Add Bonds presentation constants for sandbox HUD copy.
```

Staged/committed only:
- `Assets/FracturedChorus/Hub/BondPresentation.cs` (+ `.meta`)
- `Assets/FracturedChorus/Hub/BondLinkEpisodeCatalog.cs` (+ `.meta`)
- `Assets/FracturedChorus/Editor/BondPresentationTests.cs` (+ `.meta`)

Unrelated Hub/StatusMenu WIP left unstaged.

## Test Summary

| Expected | Actual |
|----------|--------|
| 8 EditMode tests PASS | Not run — Unity unavailable |
| Pre-impl FAIL (missing types) | Not run |

Manual verification: all test expectations align with implementation.

## Concerns

1. **Unity Test Runner not run** — user should run EditMode → `BondPresentationTests` once in Unity 6000.4.0f1 to confirm green.
2. **`.meta` GUIDs hand-authored** — Unity may regenerate on first import; if GUIDs change, re-commit only if Unity rewrites them.

## Not Done (out of scope for Task 1)

- uGUI sandbox scene wiring (later tasks)
- RectTransform layout (epic constraint; no Rects in this task)

## Next Steps (Task 2+)

Run tests in Unity, then proceed to sandbox scene / HUD binding per epic plan.

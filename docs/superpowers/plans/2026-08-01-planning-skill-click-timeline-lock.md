# Planning Skill Click + Timeline Beat Lock Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restore skill-panel open on short unit click while keeping formation drag in the same Planning window, and lock beat / ScanBar / TrackLine sizes from CombatTutorial.

**Architecture:** Extract a pure gesture helper (`BoardPointerGesture`) that decides click vs drag from pointer distance; `BoardDragController` defers `BeginDrag` until past threshold. Canonical sizes live in `TimelineLayoutLock`; `BeatTimelineUIView` reads them for ScanBar and TrackLine.

**Tech Stack:** Unity 6 · C# · uGUI · Unity Test Framework (EditMode) · `Tools/check-compile.ps1`

**Spec:** [`docs/superpowers/specs/2026-08-01-planning-skill-click-timeline-lock-design.md`](../specs/2026-08-01-planning-skill-click-timeline-lock-design.md)

## Global Constraints

- Short click = open skill panel; drag past `clickDragThresholdPx` (default **8**) = reposition unit.
- Both actions only when `CombatSession.IsPlanningWindowOpen`.
- Do **not** change `IsPlanningWindowOpen` semantics or re-split Deploy/Planning phases.
- Layout lock scope A only: beat slot ≥ **73.85**, ScanBar **6 / -4**, TrackLine **y=6 h=2**.
- No explanatory comments in source code.
- Commit after each task with Summary-first message.

## File map

| File | Responsibility |
|------|----------------|
| `Assets/FracturedChorus/UI/BoardPointerGesture.cs` | Pure click-vs-drag decision (testable) |
| `Assets/FracturedChorus/UI/BoardDragController.cs` | Deferred drag wiring |
| `Assets/FracturedChorus/UI/TimelineLayoutLock.cs` | Add TrackLine constants |
| `Assets/FracturedChorus/UI/BeatTimelineUIView.cs` | Apply lock to ScanBar / TrackLine |
| `Assets/FracturedChorus/Tests/EditMode/BoardPointerGestureTests.cs` | EditMode unit tests |
| `Assets/FracturedChorus/Scenes/SCENE_SETUP.md` | Document click vs drag |
| Scenes Tutorial + Prototype | Confirm `slotWidth` / `Beat_0` stay 73.85 (already set; verify only) |

---

### Task 1: Pure gesture helper + EditMode tests

**Files:**
- Create: `Assets/FracturedChorus/UI/BoardPointerGesture.cs`
- Create: `Assets/FracturedChorus/UI/BoardPointerGesture.cs.meta` (Unity generates on import if missing)
- Create: `Assets/FracturedChorus/Tests/EditMode/BoardPointerGestureTests.cs`
- Create: `Assets/FracturedChorus/Tests/EditMode/FracturedChorus.EditModeTests.asmdef` (if no EditMode asmdef exists yet)

**Interfaces:**
- Consumes: none
- Produces:
  - `BoardPointerGesture.ShouldCommitDrag(Vector2 pointerDownScreen, Vector2 currentScreen, float thresholdPx) -> bool`
  - `BoardPointerGesture.IsClick(Vector2 pointerDownScreen, Vector2 releaseScreen, float thresholdPx) -> bool` (= `!ShouldCommitDrag`)

- [ ] **Step 1: Check for existing EditMode asmdef**

Run:
```powershell
Get-ChildItem -Recurse Assets -Filter *.asmdef | Select-Object FullName
```

If none under `Assets/FracturedChorus/Tests`, create:

`Assets/FracturedChorus/Tests/EditMode/FracturedChorus.EditModeTests.asmdef`
```json
{
  "name": "FracturedChorus.EditModeTests",
  "rootNamespace": "FracturedChorus.Tests",
  "references": [
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner",
    "GUID:PLACEHOLDER_ASSEMBLY_CSHARP"
  ],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "optionalUnityReferences": ["TestAssemblies"]
}
```

Resolve `Assembly-CSharp` reference: open `Assets/FracturedChorus` scripts — they compile into `Assembly-CSharp` by default (no runtime asmdef). Prefer referencing the assembly by name `"Assembly-CSharp"` in the asmdef `references` array (Unity accepts it for default assemblies). If Unity rejects it, put the test file under `Assets/FracturedChorus/Editor/` as a simple Editor test without a separate asmdef, using `NUnit.Framework` already available to Editor scripts — **prefer that fallback** to avoid asmdef fights:

Fallback path if asmdef fails:
- Create: `Assets/FracturedChorus/Editor/BoardPointerGestureTests.cs` with `[Test]` and menu/Test Runner discovery under EditMode.

- [ ] **Step 2: Write failing tests first**

`Assets/FracturedChorus/Tests/EditMode/BoardPointerGestureTests.cs` (or Editor fallback):
```csharp
using FracturedChorus.UI;
using NUnit.Framework;
using UnityEngine;

namespace FracturedChorus.Tests
{
    public class BoardPointerGestureTests
    {
        [Test]
        public void ShouldCommitDrag_False_WhenDistanceAtOrBelowThreshold()
        {
            var down = new Vector2(100f, 100f);
            Assert.IsFalse(BoardPointerGesture.ShouldCommitDrag(down, down + new Vector2(8f, 0f), 8f));
            Assert.IsFalse(BoardPointerGesture.ShouldCommitDrag(down, down, 8f));
        }

        [Test]
        public void ShouldCommitDrag_True_WhenDistanceAboveThreshold()
        {
            var down = new Vector2(100f, 100f);
            Assert.IsTrue(BoardPointerGesture.ShouldCommitDrag(down, down + new Vector2(8.1f, 0f), 8f));
        }

        [Test]
        public void IsClick_True_OnlyWhenNotCommitted()
        {
            var down = new Vector2(50f, 50f);
            Assert.IsTrue(BoardPointerGesture.IsClick(down, down + new Vector2(3f, 4f), 8f));
            Assert.IsFalse(BoardPointerGesture.IsClick(down, down + new Vector2(10f, 0f), 8f));
        }
    }
}
```

- [ ] **Step 3: Run tests — expect FAIL (type missing)**

Unity Test Runner → EditMode → run `BoardPointerGestureTests`.  
Or CLI if available:
```powershell
# Only if project has batchmode test script; otherwise use Test Runner window
```
Expected: compile error / missing `BoardPointerGesture`.

- [ ] **Step 4: Implement helper**

`Assets/FracturedChorus/UI/BoardPointerGesture.cs`:
```csharp
using UnityEngine;

namespace FracturedChorus.UI
{
    public static class BoardPointerGesture
    {
        public static bool ShouldCommitDrag(Vector2 pointerDownScreen, Vector2 currentScreen, float thresholdPx)
        {
            return Vector2.Distance(pointerDownScreen, currentScreen) > thresholdPx;
        }

        public static bool IsClick(Vector2 pointerDownScreen, Vector2 releaseScreen, float thresholdPx)
        {
            return !ShouldCommitDrag(pointerDownScreen, releaseScreen, thresholdPx);
        }
    }
}
```

- [ ] **Step 5: Re-run tests — expect PASS**

- [ ] **Step 6: Commit**

```powershell
git add Assets/FracturedChorus/UI/BoardPointerGesture.cs Assets/FracturedChorus/UI/BoardPointerGesture.cs.meta Assets/FracturedChorus/Tests Assets/FracturedChorus/Editor/BoardPointerGestureTests.cs
git commit -m @"
Add click-vs-drag gesture helper with EditMode tests.

Keeps planning click/drag rules testable before BoardDragController wiring.
"@
```

---

### Task 2: Deferred drag in `BoardDragController`

**Files:**
- Modify: `Assets/FracturedChorus/UI/BoardDragController.cs`

**Interfaces:**
- Consumes: `BoardPointerGesture.ShouldCommitDrag`, `BoardPointerGesture.IsClick`
- Produces: same public API (`CanDragUnit`, `BeginDrag`, `EndDrag`, `CancelActiveDrag`, click handler)

- [ ] **Step 1: Update class summary + remove eager BeginDrag on pointer down**

Replace the class XML summary with:
```csharp
/// <summary>
/// Planning window: short click opens skill panel; drag past threshold repositions unit.
/// Uses Physics2D pick — reliable with Screen Space Overlay UI + Input System.
/// </summary>
```

Replace `HandlePointerDown` body so it **never** calls `BeginDrag`:
```csharp
private void HandlePointerDown(Vector2 screenPos)
{
    _pointerDownUnit = null;
    _dragPointerActive = false;
    _draggingUnit = null;

    if (IsScreenPointBlockedByUi(screenPos))
    {
        return;
    }

    var view = PickUnitAtScreen(screenPos);
    if (view == null)
    {
        return;
    }

    _pointerDownUnit = view;
    _pointerDownScreen = screenPos;
    _dragPointerActive = true;
}
```

- [ ] **Step 2: Commit drag only after threshold in `Update`**

Replace the held-pointer block in `Update` with:
```csharp
if (IsPointerHeld() && _dragPointerActive && _pointerDownUnit != null)
{
    if (_draggingUnit == null
        && CanDragUnit(_pointerDownUnit)
        && BoardPointerGesture.ShouldCommitDrag(_pointerDownScreen, screenPos, clickDragThresholdPx))
    {
        BeginDrag(_pointerDownUnit);
    }

    if (_draggingUnit != null)
    {
        UpdateDragAtScreen(screenPos);
    }
}
```

- [ ] **Step 3: Fix `HandlePointerUp` for deferred drag**

```csharp
private void HandlePointerUp(Vector2 screenPos)
{
    if (_draggingUnit != null)
    {
        EndDrag(_draggingUnit);
    }
    else if (_pointerDownUnit != null
             && BoardPointerGesture.IsClick(_pointerDownScreen, screenPos, clickDragThresholdPx)
             && CanOpenSkillPanelFor(_pointerDownUnit))
    {
        _onUnitClicked?.Invoke(_pointerDownUnit.Unit, _pointerDownUnit);
    }

    _pointerDownUnit = null;
    _dragPointerActive = false;
}
```

- [ ] **Step 4: Clear pointer state in `CancelActiveDrag`**

Ensure:
```csharp
public void CancelActiveDrag()
{
    if (_draggingUnit != null)
    {
        CancelDrag(_draggingUnit);
    }

    _dragPointerActive = false;
    _pointerDownUnit = null;
    _draggingUnit = null;
}
```

- [ ] **Step 5: Compile check**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/check-compile.ps1
```
Expected: `COMPILE OK`

- [ ] **Step 6: Commit**

```powershell
git add Assets/FracturedChorus/UI/BoardDragController.cs
git commit -m @"
Defer unit drag so short clicks open the skill panel.

Fixes Planning-window regression after Deploy merged with skill assign.
"@
```

---

### Task 3: Extend `TimelineLayoutLock` + wire ScanBar / TrackLine

**Files:**
- Modify: `Assets/FracturedChorus/UI/TimelineLayoutLock.cs`
- Modify: `Assets/FracturedChorus/UI/BeatTimelineUIView.cs` (`ApplyTrackLineLayout`, `AlignScanBar`, `GetScanLineX`)

**Interfaces:**
- Consumes: existing `SlotWidth`, `ScanBarWidth`, `ScanBarVerticalInset`
- Produces: `TrackLineY`, `TrackLineHeight` constants

- [ ] **Step 1: Add TrackLine constants**

In `TimelineLayoutLock.cs`, after `ScanBarVerticalInset`:
```csharp
public const float TrackLineY = 6f;
public const float TrackLineHeight = 2f;
```

- [ ] **Step 2: Wire `ApplyTrackLineLayout`**

```csharp
trackLine.anchorMin = new Vector2(0f, 0f);
trackLine.anchorMax = new Vector2(1f, 0f);
trackLine.pivot = new Vector2(0.5f, 0f);
trackLine.anchoredPosition = new Vector2(0f, TimelineLayoutLock.TrackLineY);
trackLine.sizeDelta = new Vector2(0f, TimelineLayoutLock.TrackLineHeight);
```

- [ ] **Step 3: Wire `AlignScanBar` + idle ScanBar X**

`AlignScanBar` already uses `TimelineLayoutLock.ScanBarWidth` / `ScanBarVerticalInset` — verify; if magic numbers remain, replace them.

`GetScanLineX`:
```csharp
private float GetScanLineX()
{
    return TimelineLayoutLock.ClampSlotWidth(slotWidth) * 0.5f;
}
```

- [ ] **Step 4: Verify scene locks (no YAML rewrite unless drifted)**

```powershell
Select-String -Path Assets/FracturedChorus/Scenes/CombatTutorial.unity,Assets/FracturedChorus/Scenes/CombatPrototype.unity -Pattern "slotWidth:|m_PreferredWidth: 73|m_SizeDelta: \{x: 73.85"
```
Expected: both scenes show `slotWidth: 73.85` and Beat_0 width/preferredWidth 73.85.

- [ ] **Step 5: Compile + commit**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/check-compile.ps1
git add Assets/FracturedChorus/UI/TimelineLayoutLock.cs Assets/FracturedChorus/UI/BeatTimelineUIView.cs
git commit -m @"
Lock TrackLine and ScanBar sizes to CombatTutorial constants.

Prevents runtime layout from drifting off the authored beat strip.
"@
```

---

### Task 4: Docs + Play Mode acceptance

**Files:**
- Modify: `Assets/FracturedChorus/Scenes/SCENE_SETUP.md` (short paragraph under timeline / input)
- Optional note in `docs/combat/UNIFORM_BEAT_QA.md` or leave Play Mode list in this plan only

- [ ] **Step 1: Document interaction in SCENE_SETUP**

Add under Beat Timeline / input section:
```markdown
### Planning window — unit click vs drag

- **Short click** (move ≤ `clickDragThresholdPx`, default 8px) → open skill panel.
- **Drag** past threshold → reposition / swap on player grid.
- Both only while `CombatSession.IsPlanningWindowOpen`.
- Gesture math: `BoardPointerGesture` · wiring: `BoardDragController`.
```

- [ ] **Step 2: Play Mode checklist (manual)**

On `CombatPrototype` or `CombatTutorial`:

1. Planning: short click unit → skill panel opens.  
2. Drag skill onto lane → assign succeeds.  
3. Drag unit > 8px → move/swap; drop off-grid → snap home.  
4. Short click leaves unit on cell (no drift).  
5. During Execute: no panel, no unit drag.  
6. After segment hold (mid-fight planning): click + drag both still work.  
7. Slot width ≥ 73.85; ScanBar sizeDelta `(6, -4)`; TrackLine `(y=6, h=2)`.

- [ ] **Step 3: Commit docs**

```powershell
git add Assets/FracturedChorus/Scenes/SCENE_SETUP.md
git commit -m @"
Document planning click-vs-drag on the timeline setup guide.
"@
```

---

## Spec coverage check

| Spec requirement | Task |
|------------------|------|
| Deferred drag / click opens panel | Task 1–2 |
| Drag past threshold repositions | Task 2 |
| Gates stay `IsPlanningWindowOpen` | Task 2 (no session changes) |
| Lock SlotWidth 73.85 | Task 3 (verify) + existing lock |
| Lock ScanBar 6 / -4 | Task 3 |
| Lock TrackLine y=6 h=2 | Task 3 |
| Acceptance tests | Task 1 (unit) + Task 4 (Play Mode) |
| Non-goals (outer frame, LeftRail, etc.) | Not in plan |

## Placeholder / consistency review

- No TBD steps.
- Helper method names match across Task 1–2: `ShouldCommitDrag`, `IsClick`.
- Threshold comparison is **strict greater-than** (`>`), matching existing `BoardDragController` distance check and the unit tests in Task 1.

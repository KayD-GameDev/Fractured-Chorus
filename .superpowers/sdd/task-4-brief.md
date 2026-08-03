### Task 4: Docs + Play Mode acceptance

**Files:**
- Modify: `Assets/FracturedChorus/Scenes/SCENE_SETUP.md` (short paragraph under timeline / input)
- Optional note in `docs/combat/UNIFORM_BEAT_QA.md` or leave Play Mode list in this plan only

- [ ] **Step 1: Document interaction in SCENE_SETUP**

Add under Beat Timeline / input section:
```markdown
### Planning window â€” unit click vs drag

- **Short click** (move â‰¤ `clickDragThresholdPx`, default 8px) â†’ open skill panel.
- **Drag** past threshold â†’ reposition / swap on player grid.
- Both only while `CombatSession.IsPlanningWindowOpen`.
- Gesture math: `BoardPointerGesture` Â· wiring: `BoardDragController`.
```

- [ ] **Step 2: Play Mode checklist (manual)**

On `CombatPrototype` or `CombatTutorial`:

1. Planning: short click unit â†’ skill panel opens.  
2. Drag skill onto lane â†’ assign succeeds.  
3. Drag unit > 8px â†’ move/swap; drop off-grid â†’ snap home.  
4. Short click leaves unit on cell (no drift).  
5. During Execute: no panel, no unit drag.  
6. After segment hold (mid-fight planning): click + drag both still work.  
7. Slot width â‰¥ 73.85; ScanBar sizeDelta `(6, -4)`; TrackLine `(y=6, h=2)`.

- [ ] **Step 3: Commit docs**

```powershell
git add Assets/FracturedChorus/Scenes/SCENE_SETUP.md
git commit -m @"
Document planning click-vs-drag on the timeline setup guide.
"@
```

---

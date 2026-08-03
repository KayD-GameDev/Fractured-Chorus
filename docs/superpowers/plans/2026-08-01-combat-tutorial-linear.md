# Combat Tutorial Linear Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** One-way gated CombatTutorial with Coda coach (VI), scripted boss notes, hand pointer, exit to RunMap.

**Status:** Superseded — runtime = `TutorialDirector` text + optional `Art/UI/Tutorial/Steps/{stepId}_v1.png`.  
**Architecture (legacy plan):** `CombatTutorialDirector` FSM + bridge (đã gỡ).

**Tech Stack:** Unity 6 C#, existing TutorialCoachView, CombatController music-scan Execute path, BossNoteTier.

## Global Constraints

- Runtime copy VI; docs VI+EN (`docs/tutorial/TUTORIAL_COPY.md`)
- No Skip; death retries CombatTutorial; exit RunMap + `tutorial_cadence_intro_done`
- Scripted telegraphs: Seg0 Red@12 Hits=1; Seg1 Blue Hits=2
- `tutorialSceneMode == false` → null bridge, zero behavior
- Execute via `StartRound` / `ResumeFromPlanningPause` / `StartExecuteSegment` — not `ConfirmPlanningAndExecute`

---

### Task 1: Docs SoT

**Files:**
- Create: `docs/superpowers/specs/2026-08-01-combat-tutorial-linear-design.md`
- Create: `docs/superpowers/plans/2026-08-01-combat-tutorial-linear.md`
- Rewrite: `docs/tutorial/TUTORIAL_COPY.md`
- Modify: `Assets/FracturedChorus/Scenes/SCENE_SETUP.md`

- [x] Write design + copy + this plan
- [x] Touch SCENE_SETUP CombatTutorial section for linear director

### Task 2: Art pointer

- [x] Gen `Assets/FracturedChorus/Art/UI/Tutorial/tutorial_point_hand_v1.png`
- [x] Import as UI Sprite (TextureType Sprite)

### Task 3: Bridge + masks

- [x] `TutorialInputMask` flags + `ITutorialCombatBridge` + `TutorialCombatBridge`
- [x] Hook Deploy / unit click / skill assign / Execute / damage / counter
- [x] `PauseScanAtBeat` on BeatTimelineUIView

### Task 4: Scripted telegraphs

- [x] `TutorialScriptedTelegraphs` inject at PrepareTelegraphs when tutorial

### Task 5: Overlay + coach VI

- [x] `TutorialHighlightOverlay` dim/spotlight/hand
- [x] Director shows VI via coach

### Task 6: Director FSM

- [x] `CombatTutorialStepDef` + 20-step catalog
- [x] `CombatTutorialDirector` gates
- [x] Replace `StartCadenceIntroTrack` handoff
- [x] Editor Prepare wires components

### Task 7: Exit / fail

- [x] Flag + RunMap on complete
- [x] Death → retry scene
- [x] No Skip UI

### Task 8: QA

- [ ] Manual Play Mode checklist (open CombatTutorial, Prepare menu, walk gates)

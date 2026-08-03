# Combat Tutorial Linear — Design

**Date:** 2026-08-01  
**Status:** Superseded (2026-08-01) — runtime simplified to `TutorialDirector` text + optional step images; gates/bridge/layers removed. Step table SoT: `docs/tutorial/TUTORIAL_COPY.md`.  
**Scene:** `Assets/FracturedChorus/Scenes/CombatTutorial.unity`  
**Approach:** Isolated `CombatTutorialDirector` + thin `ITutorialCombatBridge` (tutorialSceneMode only)

## Goal

One-way gated combat tutorial with Coda as coach. Separate from normal fights. Pattern documented for later scenes via bridge — do not embed the FSM into `CombatPrototype`.

## Constraints

| Rule | Value |
|------|-------|
| Runtime copy | Vietnamese |
| Docs copy | VI + EN |
| Skip | None |
| Back-step | None |
| Death | Retry same scene from step 1 |
| Exit | RunMap next node + `tutorial_cadence_intro_done` |
| Party | Ren (guided) + Coda (free plan); basics only |
| Enemy | Kiki Ueda visual; telegraphs scripted (no Elite random) |
| Deploy | One guided step (Ren MID, Coda BACK) |
| Non-tutorial | `tutorialSceneMode == false` → zero tutorial behavior |

## Architecture

```
CombatTutorialDirector (FSM)
  → TutorialCoachView (VI copy)
  → TutorialHighlightOverlay (dim / spotlight / hand pointer)
  → ITutorialCombatBridge
       → CombatController / BeatTimeline / Deploy / Skill drag
       → TutorialScriptedTelegraphs (override planner)
```

Hub/map tracks on `TutorialDirector` stay confirm-through. `StartCadenceIntroTrack` hands off to `CombatTutorialDirector` when present.

### Bridge API

- `SetInputMask(TutorialInputMask)` — Deploy / ClickUnit / DragSkill / Execute / None
- `PauseScanAtBeat(int)` / `ResumeScan()`
- `InjectTelegraphs(segment, notes[])` or scripted provider consulted at prepare
- Events: DeployConfirmed, UnitClicked, SkillPlaced, InvalidStandingDrop, Execute, CounterResolved, DamageTaken
- `LockUnitPlanning(unitId)` / unlock
- `ExitToRunMapNext()` after completion flag

## Linear steps (20)

| # | stepId | Gate | Highlight |
|---|--------|------|-----------|
| 1 | meet_danger | Confirm | Coda coach |
| 2 | deploy_place | Deploy + Ren MID + Coda BACK | Grid columns |
| 3 | intro_timeline_beat | Confirm | Dim + one beat cell |
| 4 | intro_boss_note | Confirm | Boss note + hits number + hand |
| 5 | intro_skill_bar | Confirm | Skill panel / radial |
| 6 | click_ren | Click Ren | Ren |
| 7 | skill_parts_s1 | Confirm | StandingBefore (grey) |
| 8 | skill_parts_active | Confirm | Active (colored) |
| 9 | skill_parts_s2 | Confirm | StandingAfter (grey) |
| 10 | place_counter_ok | Place Ren basic Active on boss note beat | Valid drop slot |
| 11 | place_standing_fail | Attempt Standing drop (reject) + Confirm | Grey dots |
| 12 | note_check_v | Confirm after V / cover_perfect preview | Boss note |
| 13 | free_coda_plan | Coda ≥1 valid skill on timeline | Coda lane |
| 14 | press_execute_1 | Execute pressed | Execute button |
| 15 | freeze_counter | Confirm while scan paused at counter | Counter window |
| 16 | freeze_player_hit | Confirm at player skill resolve window | Player Active beat |
| 17 | exec2_hard_note | Confirm after hard note spawn | HitsRequired ≥ 2 note |
| 18 | teach_take_damage | Confirm after intentional damage teach | HP / exposed |
| 19 | free_place_boss | Player plans counters (or Confirm) | Boss notes |
| 20 | press_execute_2 | Execute → resolve → RunMap | Execute |

## Scripted telegraphs

- **Segment 0:** 1× Red (`HitsRequired=1`) at fixed planning-horizon beat (default absolute beat 12).
- **Segment 1:** 1× Blue (`HitsRequired=2`) at fixed beat in segment (default segment-local mapping). Step 18 forces insufficient counter / standing expose so player takes damage once with coach.

## Art

- `Assets/FracturedChorus/Art/UI/Tutorial/tutorial_point_hand_v1.png` — one-finger pointing hand, transparent UI sprite.

## Error handling

| Case | Behavior |
|------|----------|
| Invalid drop outside step 11 | Reject; no advance |
| Early Execute | Button disabled until gate |
| Party wipe | Fail overlay → reload CombatTutorial |
| Missing highlight target | LogError; Confirm-only fallback for that step |

## Reuse for later scenes

1. Keep combat free of FSM logic beyond bridge null-checks.
2. New linear tutorials = new director + step table + optional scripted telegraphs.
3. Copy SoT: `docs/tutorial/TUTORIAL_COPY.md`.

## Out of scope

CORE/MICRO/EYE, async per-char planning, VN Narrative graph, hub/map track rewrite, applying FSM to CombatPrototype.

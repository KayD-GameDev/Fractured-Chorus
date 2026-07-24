# Flower Shop Work Event — Design Spec

**Date:** 2026-07-25  
**Status:** Implemented (slice)  
**Game:** Fractured Chorus (Unity 6)

---

## 1. Summary

Part-time flower shop activity (`flower_job`) plays a short VN event instead of granting EXP instantly. First visit includes shop-manager intro; every visit rolls a random customer flower quiz. Correct choice grants **Resonance +1 EXP** on top of base rewards.

---

## 2. Social stats mapping (P5 → FC)

| Persona-style | Fractured Chorus | Flower shop role |
|---|---|---|
| Charm | Resonance | Primary (+10 base, +1 if correct) |
| Proficiency | Harmony | Secondary (+4 base) |
| Knowledge / Guts / Kindness | Cadence / Pulse / Rhythm | Not used by this job |

Rewards are **stat EXP** (rank thresholds unchanged). Wrong answers are not punished.

---

## 3. Player flow

1. Town Map → Flower Shop (Day, **Wed / Sat** only) → Arrange Flowers  
2. Load `FlowerShopWork`  
3. First time only: manager reminds Ren to listen carefully (`flower_job_intro_done`)  
4. Random customer request (5 scenarios)  
5. Think CG + 3 choices  
6. Feedback + happy CG  
7. Apply EXP, consume Day slot, return `CampusHub`

---

## 4. Data

- `FlowerWorkScenarioSO` in `Resources/FlowerWork/`  
- Fields: `id`, `customerLine`, `thinkPrompt`, `choices[3]`, `correctIndex`, `correctReply`, `wrongReply`  
- CGs: `Art/Narrative/Events/FlowerShop/flower_event_0{1-4}_*_v1.png`  
- BG cue ids: `flower_arrive`, `flower_customer`, `flower_think`, `flower_happy`

---

## 5. Integration points

| System | Change |
|---|---|
| `HubPhaseDriver` | `flower_job` → pending + load scene (no early EXP) |
| `HubPendingActivity` | Bridge while VN runs |
| `VnRuntimeController` | Real `Choice` beats + `Finished` / `LoadNextSceneOnEnd` |
| `TownMapPinView` | `AllowedWeekdays` filter |
| `FlowerWorkEventController` | Orchestrate script, reward, return hub |

---

## 6. Playtest checklist

- [ ] Wed or Sat Day: flower pin visible; other weekdays hidden  
- [ ] First visit: manager intro lines play  
- [ ] Second visit: intro skipped  
- [ ] Choice correct → Resonance +11, Harmony +4  
- [ ] Choice wrong → Resonance +10, Harmony +4  
- [ ] Slot consumed; hub resumes Evening (or next day if evening slot already used)  
- [ ] Scenario avoids immediate repeat when pool > 1  

---

## 7. Scene setup (Editor)

Menu: **Fractured Chorus → Hub → Create FlowerShopWork Scene**  
(or reopen project with `Library/fc_create_flower_shop_work_scene.flag` present)

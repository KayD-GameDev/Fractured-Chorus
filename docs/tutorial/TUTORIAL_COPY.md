# Tutorial Copy (EN)

Track ids: `hub` · `map` · `combat` · `cadence_intro`

---

## Hub track (`hub`)

| stepId | bodyCopy | requiresConfirm |
|--------|----------|-----------------|
| `hub_menu` | Open **MENU** (top-right) to view party stats, bonds, calendar, and save slots. | yes |
| `hub_town` | Click map pins to spend activity slots. Morning quiz and calendar phases gate what you can do each day. | yes |
| `hub_done` | Hub basics covered. Explore campus, then enter a Cadence run when ready. | yes |

---

## Map track (`map`)

| stepId | bodyCopy | requiresConfirm |
|--------|----------|-----------------|
| `map_nodes` | Select reachable nodes to advance. Battle and Elite nodes lead to combat; the boss gate ends the sector. | yes |
| `map_camp` | After defeat you return to the nearest camp node. HP persists between fights on the run. | yes |
| `map_done` | Map navigation ready. Clear the path to the boss when your party is set. | yes |

---

## Combat track (`combat`)

| stepId | bodyCopy | requiresConfirm |
|--------|----------|-----------------|
| `combat_deploy` | **Deploy phase:** drag units on the front / mid / back columns. FRONT takes less damage; BACK deals more. | yes |
| `combat_plan` | After Deploy, drag skills onto the timeline beats. Standing phases (grey dots) leave you exposed to boss telegraphs. | yes |
| `combat_execute` | Press **Execute** to resolve the round. Counter boss notes on beat, then finish with your skill windows. | yes |
| `combat_done` | Combat tutorial complete. Good luck — keep the rhythm. | yes |

---

## Cadence intro track (`cadence_intro`)

Coda voice. Coach portrait: `coda_chibi_fullbody_v1`. Panel art per-step optional (placeholder uses chibi on first step).

Flag: `tutorial_cadence_intro_done`

| stepId | bodyCopy | coach | panel | requiresConfirm |
|--------|----------|-------|-------|-----------------|
| `cadence_meet` | Hey — I'm Coda. That beast is Kiki Ueda. Stick with me — it's you, me, and her. | chibi | chibi | yes |
| `cadence_deploy` | Deploy: drag us on FRONT / MID / BACK. FRONT soaks hits; BACK hits harder. Park Ren in MID opposite Kiki. | chibi | — | yes |
| `cadence_plan` | Plan: drag your Basic onto the timeline beats. Skill and Ult aren't unlocked yet — keep it simple. | chibi | — | yes |
| `cadence_execute` | Execute: press Execute to resolve the round. Counter her notes on beat, then land your skill windows. | chibi | — | yes |
| `cadence_done` | You've got this. Drop Kiki — I'll keep coaching from the sideline. | chibi | — | yes |

### Encounter wiring

- Id: `Encounter_Tutorial`
- **Scene:** `Assets/FracturedChorus/Scenes/CombatTutorial.unity`
- Party: Ren + Coda only; skills = basic only
- Enemy: **Kiki Ueda** (Lv1 Elite) — see [`docs/combat/KIKI_UEDA_LV1.md`](../combat/KIKI_UEDA_LV1.md)
- BG: `Background canvas/Image` → `cadence_smoke_war_front_bg_v1`
- Entry (test): CampusHub hotkey **Tutorial Fight** → `CombatTutorial`
- Editor: **Fractured Chorus → Open / Prepare Combat Tutorial Scene**

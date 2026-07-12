# Opening Investigation VN — Design Spec

**Date:** 2026-07-12  
**Status:** Approved for planning  
**Scope:** Linear VN cold-open after Prologue, before CampusHub  
**Canon source:** `Fractured_Chorus_Story (3).docx` (Prologue SCENE 01–04) + Ren arrival beat (this spec)

---

## 1. Goal

Ship a playable **OpeningInvestigation** scene that:

1. Shows the SyncPod hijack incident (Haruto) and the Mei Lin / Ryo crime-scene beat.
2. Cuts to **Ren arriving in Lumina at the same hour**, forced Top-1 broadcast of *Eternal Spark* (clean mix), no mind control.
3. Loads **CampusHub** (01/09) with story flags set.

Language for all dialogue in this scene: **English**.

---

## 2. Scene flow

```
MainMenu → PrologueVN → OpeningInvestigation → CampusHub (01/09)
```

| Change | Detail |
|--------|--------|
| Prologue exit | `nextSceneName` → `OpeningInvestigation` (no longer direct CampusHub) |
| Opening end | Load `CampusHub`; run hub bootstrap + flags below |

**Calendar note:** Persona-calendar design previously placed `OpeningInvestigation` on 17/08 *before* Prologue. This spec **supersedes** that order for MVP: cold open plays **after** PrologueVN, then hub starts 01/09. Date label on Ren text card remains September 1 (hub start). Incident time-of-day is night → “four hours later” dawn investigation; Ren beat is concurrent with the investigation hour.

---

## 3. Architecture (Hybrid C)

### 3.1 Layers

| Layer | Responsibility | Must not |
|-------|----------------|----------|
| **VnRuntime** | Typewriter, advance, speaker/sprite show-hide, BG/BGM/SFX cues, fade, text card, end → load scene | Hardcode story lines |
| **VnScript data** | Ordered beats for Opening (EN text + cue IDs) | Embed presentation logic |

### 3.2 Reuse

- Extract or adapt from Prologue: typewriter, advance input, audio play helpers.
- Keep existing PrologueVN working (adapter or shared modules under `Narrative/Vn/`).
- **ChoiceNode** reserved in the data model for later social/story scenes; **not used** in OpeningInvestigation.

### 3.3 Script model (minimal)

```
VnScript
  id
  nextScene
  beats[]:
    kind: Line | Narration | Cue | TextCard | Fade | End
    speakerId?          // null = narration
    text?
    expression?
    bgId? / bgmId? / sfxId?
    duration?           // TextCard / Fade
    setFlags[]?         // typically on End
```

Missing cue/sprite IDs: log error, skip visual/audio, **never soft-lock** text advance. End failure: log + fallback load CampusHub.

---

## 4. Cast & art binds

| ID | Role | Art |
|----|------|-----|
| `haruto` | Victim (office worker) | Placeholder / silhouette until asset exists |
| `ryo` | Rookie officer | Placeholder until asset exists |
| `mei_lin` | Inspector | Placeholder until asset exists |
| `ren` | Protagonist arrival | **School / HIMA uniform only** — `Art/Characters/Ren/School/ren_hima_uniform_menu_fullbody_v1.png`. **Do not** use combat (`N_Corp_*`) sprites. |

### 4.1 Org canon (from story doc)

| Org | Role |
|-----|------|
| **StellaWorks** | Root entertainment conglomerate behind The Pentad |
| **LUXE** | Pinky-branch Prime Unit; hit song *Eternal Spark* |

SyncPod **mandatory broadcast windows** play current Chorus Board **#1**. While LUXE holds #1, that slot is *Eternal Spark*. After LUXE falls (later arc), another unit’s hit occupies the slot.

**Haruto** = malicious hijack (red LED, forced movement, drain).  
**Ren** = same forced #1 track, **clean** mix, no undertone, no control, no complaint.

---

## 5. Flags on End

| Flag | Purpose |
|------|---------|
| `lumina_case_open` | Already used by meta; keep true |
| `opening_investigation_done` | Scene completed |
| `ren_arrived_hima` | Existing hub flag; set on End (Ren reached Lumina / HIMA enrollment path) |

Optional alias `ren_arrived_lumina` only if code needs a distinct id; prefer single flag to avoid drift.

---

## 6. Beat map

| # | Beat | Content |
|---|------|---------|
| 1 | Street | SCENE 01 — Haruto, *Bring Me Home*, rejects billboard bleed |
| 2 | Hijack | SCENE 02 — SyncPod skip, red LED, undertone, loss of control |
| 3 | Alley | SCENE 03 — Lotus Service Lane / 452, collapse |
| 4 | TextCard | "Four hours later" |
| 5 | Crime scene | SCENE 04 — Ryo + Mei Lin; husk body; cracked SyncPod; log **SW-ES-040**; do not file StellaWorks |
| 6 | Fade cut | To Lumina street / station, camera behind crowd |
| 7 | Ren arrival | SCENE 05 — forced clean *Eternal Spark*; accepts; walks on |
| 8 | End | Text card Sept 1 → CampusHub |

---

## 7. Full script (English, linear)

### SCENE 01 — lumina_street_night_01

**Time:** 23:47 · Neon Crossing, Lumina  
**BGM:** `bgm_lumina_night_ambient` (+ distant *Eternal Spark* instrumental ~15%)  
**BG:** Main street — LUXE billboards, crowd, cyan/magenta neon  

**[NARRATION]**  
Late night in Lumina.  
A city that never sleeps.

**[SFX: crowd_hum + distant_chorus_humming]**

**[NARRATION]**  
On the giant screens, Prime Unit MVs loop again and again — no one remembers how many times.  
People pass by humming along, almost unconscious.

**[CHAR: haruto_walk — torso / silhouette from behind]**  
**[EXP: neutral]**

**Haruto (inner):**  
…Another late shift. I’m wiped.

**[SFX: syncpod_idle_pulse]**

**[NARRATION]**  
SyncPod on his right ear. Blue LED. Slow pulse.  
Personal playlist — track three: *Bring Me Home*.

**Haruto (inner):**  
*Bring Me Home*… Yeah. That fits.

**[SFX: billboard_spark_hook — 2s *Eternal Spark* hook from a nearer billboard]**

**[NARRATION]**  
A line of the hit bleeds in through his left ear.  
Haruto shakes his head — his SyncPod stays on his own track.

**Haruto:**  
Not now.

### SCENE 02 — syncpod_hijack

**BG:** Same street — camera tight on Haruto  
**BGM:** Crossfade ambient → `bgm_resonance_undertone` (metallic layer in ~3s)  
**FX:** `fx_led_blue_to_red` · subtle chromatic edge  

**[SFX: syncpod_static_crackle]**

**Haruto:**  
—!

**[NARRATION]**  
The LED on his ear flickers. Once. Twice.

**[SFX: syncpod_track_skip]**

**Haruto (inner):**  
What the…? Why did it change tracks on its own?

**[FX: LED blue → red snap 0.4s]**  
**[SFX: undertone_metal_scrape]**

**Haruto:**  
It hurts. What is this?

**[NARRATION]**  
Not the song he was listening to.  
Not the song on the billboard.  
Something else — like a thousand needles driven into the heart.

**Haruto:**  
Stop…! I have to shut it off!

**[SFX: syncpod_no_response]**

**[NARRATION]**  
The SyncPod doesn’t answer. His feet keep moving.  
Not toward home.

**Haruto (whisper, losing control):**  
No… don’t…

**[FX: pupil_dilate + slight RGB split]**

**[NARRATION]**  
Vision blurs. His body feels heavy — pulled forward on an invisible wire.  
He slips through the crowd. No one notices.

### SCENE 03 — alley_void

**BG:** `lumina_alley_narrow` — Lane 452, neon in puddles  
**BGM:** `bgm_resonance_undertone` solo — half-beat off from *Eternal Spark*  
**FX:** `fx_cadence_micro_warp`

**[SFX: footsteps_echo_alley]**

**[NARRATION]**  
**Lotus Service Lane** — no billboards here.  
Only emergency lights and the damp breath of shop ACs.

**Haruto (distant, like someone else’s voice):**  
…Someone… help me…

**[FX: syncpod_red_pulse_strong]**  
**[SFX: resonance_hum_crescendo]**

**[NARRATION]**  
The SyncPod burns red. The waveform spins — not music.  
Like **suction**.

**[CG: cg_haruto_kneel — optional]**

**Haruto:**  
ARRGHH…!

**[FX: alley_neon_melt_2s]**

**[NARRATION]**  
The neon on the wall **glitches** for one beat — then settles.  
Haruto drops to his knees. One hand clawing at the SyncPod. It won’t come off.

**Haruto:**  
Help… —

**[SFX: syncpod_overload_whine]**  
**[FX: screen_white_flash → cut black]**

**[NARRATION]**  
The whine cuts dead.  
Silence.

**[FADE TO BLACK — 2s]**  
**[TEXT CARD: "Four hours later"]**

### SCENE 04 — crime_scene_dawn

**Time:** 03:52 · Lane 452  
**BG:** `lumina_alley_crime_scene` — tape, flash, thin mist  
**BGM:** `bgm_investigation_low`  
**CHAR:** Ryo (left), Mei Lin (right)

**[SFX: police_radio_chatter + camera_shutter]**

**[NARRATION]**  
Yellow tape across the mouth of the lane.  
A patrol car. Two people.

**[CHAR: ryo — uniform, slightly pale]**  
**[EXP: uneasy]**

**Ryo:**  
Inspector Lin… how many is this now?

**[CHAR: mei_lin]**  
**[EXP: calm, tired eyes]**

**Mei Lin:**  
Don’t count. Counting just makes it worse.

**[FX: camera pan to body — no gore]**

**[NARRATION]**  
Male victim. Mid-to-late twenties.  
Face-down. One hand reaching toward his ear.

**[EXP: ryo — shock]**

**Ryo:**  
His skin…

**[EXP: mei_lin — grim]**

**Mei Lin:**  
Like a husk. Like something drank him dry from the inside.

**Ryo:**  
Overdose? Or the same as the others?

**Mei Lin:**  
…

**[SFX: syncpod_dead_pulse]**  
**[CG: cg_syncpod_cracked_red]**

**[NARRATION]**  
SyncPod SP-01. Red LED — a **crack** down the glass.

**Ryo:**  
What was he listening to before he died?

**Mei Lin:**  
Chorus Board logged the device online at 23:51.  
Personal playlist: indie.  
But the last entry…

**[NARRATION]**  
Mei Lin opens her tablet. The screen stays angled away from the player.

**Mei Lin:**  
…a track that isn’t in the public catalog.  
ID: **SW-ES-040**

**Ryo:**  
SW is…

**Mei Lin:**  
StellaWorks. Guesswork only. Don’t put it in the report.

**[EXP: ryo — confused]**

**Ryo:**  
Why not, Inspector?

**Mei Lin:**  
Because a report with that name **vanishes** before it can ever be filed.  
Same as the three before this.

**Ryo:**  
Damn it… so what do we do?

**Mei Lin:**  
….

**[SFX: distant_billboard_spark]**

**[NARRATION]**  
From the mouth of the lane, the *Eternal Spark* hook still carries — thin, far, innocent.

**Ryo:**  
Inspector…

**Mei Lin:**  
Do your job.  
And hope this time it doesn’t happen to you.

**[EXP: mei_lin — glance toward camera/player]**

**Mei Lin:**  
…If you ever hear something strange in your ears…  
Save yourself first.

**[FADE OUT]**

### SCENE 05 — ren_arrival_lumina

**[NARRATION]**  
At the same hour — across the city.  
The station. The crowd. Neon still singing.

**[CHAR: ren — School HIMA uniform fullbody]**  
**[SFX: syncpod_idle_pulse]**

**[NARRATION]**  
A SyncPod on Ren’s ear. Blue LED.  
One pulse — then the track cuts out.

**[SFX: syncpod_track_skip]**  
**[BGM: Eternal Spark — clean Top-1 / radio mix, no undertone]**

**[NARRATION]**  
Mandatory broadcast window.  
Current Chorus Board #1: *Eternal Spark* — LUXE.

**Ren (inner):**  
…This one again.

**[NARRATION]**  
He doesn’t switch it off. Doesn’t flinch.  
Just tightens his backpack strap and steps into the flow.

**Ren (inner):**  
If it’s number one, you listen. That’s how this city works.

**[NARRATION]**  
Around him, mouths hum the hook without looking at the billboards.  
Ren hears it with them — clear, clean, on the beat.

**[NARRATION]**  
No pain. No tug on his feet.  
Just a hit song… and a newcomer to Lumina.

**[SFX: distant_crowd_hum + eternal_spark_hook_soft]**

**[NARRATION]**  
Bag on his shoulder. Early-enrollment papers for HIMA.  
Light rain still falling.

**[TEXT CARD]**  
*September 1 — Ren Takahashi arrives in Lumina.*

**[END]** → flags → **CampusHub**

---

## 8. Assets (MVP)

| Asset | Status | MVP fallback |
|-------|--------|--------------|
| BG street / alley / crime | Missing | Solid tint / crop from existing Lumina town map |
| Haruto / Ryo / Mei Lin sprites | Missing | Nameplate only or silhouette |
| Ren school uniform | Present | `Ren/School/ren_hima_uniform_menu_fullbody_v1.png` |
| *Eternal Spark* | Present (combat/menu) | Use clean bed; no Cadence remix undertone for Ren beat |
| *Bring Me Home* | Missing | Mute + optional ♪ cue, or short placeholder bed |
| Listed SFX | Mostly missing | Reuse prologue click/pulse where plausible; else silent |

Ship criterion: text + advance + fades + correct scene load. Art/audio bind by ID incrementally.

---

## 9. Out of scope

- Choice UI / branching dialogue (later scenes; Ren choices will alter other NPCs’ lines then)
- Second-layer / Desync audio tutorial
- Full save-anywhere mid-VN
- OpeningInvestigation before Prologue (old calendar order)
- Combat Ren art in this scene

---

## 10. Success criteria

1. New Game → Prologue completes → OpeningInvestigation plays EN script linear end-to-end.
2. Ren uses **school uniform** art only.
3. Ren *Eternal Spark* is clean Top-1 (no undertone); Haruto path still uses hijack/undertone cues.
4. End loads CampusHub on 01/09 with `lumina_case_open` + `opening_investigation_done` + `ren_arrived_hima`.
5. Missing art/SFX does not block advance.

---

## 11. Follow-ups (planning)

- Implementation plan under `docs/superpowers/plans/`.
- Patch `2026-07-11-persona-calendar-design.md` §16.1 flow order to match this spec when implementing.
- Author `OpeningInvestigation` scene setup editor batch (mirror PrologueVN setup).

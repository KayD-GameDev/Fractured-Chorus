# Combat Level + XP Progression (Arc 1)

> **Status:** Design lock · SoT tables → [`CHARACTER_LEVEL_PROGRESS.md`](../../combat/CHARACTER_LEVEL_PROGRESS.md)  
> **Boss entry:** Party Lv15 optimal · [`BOSS_ENCOUNTER_DESIGN.md`](../../combat/BOSS_ENCOUNTER_DESIGN.md)  
> **Skills:** Unlock by level · [`SKILL_KIT.md`](../../combat/SKILL_KIT.md)  
> **Out of scope:** Unity `CombatXp` runtime, reward SO, HUD XP bar

---

## 1. Goals

| Constraint | Lock |
|------------|------|
| Soft target | Dungeon F1→F15 → **Party Lv15** before boss F16 |
| Boss scene | Tuned for **Lv15**, 14 stat pts spent (optimal) |
| Soft-cap | Lv16–18 grindable via dungeon but **intentionally slow** |
| Boss first clear | Grant **12600** Combat XP (= Σ Lv15→18) |
| Hard cap Arc 1 | **Lv18** |
| Combat numbers @ Lv15 | STR/Ma/HB/HP/W unchanged from prior tune; EN optimal = **+5 pts** (see §4) |

---

## 2. Model

```
Dungeon F1–F15 ──XP──► Party Lv15 ──Boss F16──► +12600 XP ──► Party Lv18
                         │
                         └── grind post-15 (×0.12 XP) ──► slow Lv16–18
```

- **1 Party Combat Level** — shared XP bar.
- On level-up: **each** of Ren / Charlotte / Coda gains **+1 stat point** (spent independently).
- Points: Lv1 = 0 · Lv15 = **14** · Lv18 = **17**.
- No separate skill-point currency — skills unlock at level thresholds.
- Auto-growth + HB conversion unchanged (`1 pt HB → +5`).
- Social/calendar EXP (Persona hub) is a **different** system — not Combat XP.

---

## 3. Stat formulas (unchanged)

```
Stat(Lv) = Base + Growth×(Lv−1) + ManualPts×Conversion
W        = clamp(7 + ⌊(HB − 120) / 26⌋, 7, 10)
Latency  = max(0, 2 − ⌊HB / 85⌋)

Ren HP       = STR × 2.0 + 30
Charlotte HP = STR × 6.0 + 50
Coda HP      = STR × 2.0 + Ma × 0.35 + 15
```

### Base Lv1

| | STR | Ma | HB | EN | HP | W |
|--|-----|----|----|----|----|---|
| Ren | 22 | 6 | 145 | 4 | 74 | 7 |
| Charlotte | 15 | 5 | 105 | 10 | 140 | 7 |
| Coda | 6 | 30 | 125 | 3 | 38 | 7 |

### Auto-growth / level

| | Ren | Charlotte | Coda |
|--|-----|-----------|------|
| STR | +1.0 | +1.0 | +1.0 |
| Ma | +0.2 | +0.1 | +1.0 |
| HB | +0.5 | +0.5 | +0.5 |
| EN | +0.2 | +0.3 | +0.2 |

---

## 4. Optimal point spend

| Char | Ratio (14 pts → Lv15) | Ratio (+3 → Lv18) |
|------|----------------------|-------------------|
| Ren | 6 STR / 3 HB / 5 EN | +1 EN → +1 STR → +1 HB |
| Charlotte | 6 STR / 3 HB / 5 EN | +1 EN → +1 STR → +1 HB |
| Coda | 6 Ma / 3 HB / 5 EN | +1 EN → +1 Ma → +1 HB |

Lv18 Coda Ma = **54** (30 + 17 growth + 7 manual), không phải 55.

### Spend order (level reached → point)

Same cadence for Ren/Charlotte (STR) and Coda (Ma):

| →Lv | Point | →Lv | Point | →Lv | Point |
|-----|-------|-----|-------|-----|-------|
| 2 | EN | 7 | HB | 12 | STR/Ma |
| 3 | STR/Ma | 8 | EN | 13 | STR/Ma |
| 4 | STR/Ma | 9 | STR/Ma | 14 | EN |
| 5 | EN | 10 | HB | 15 | HB |
| 6 | STR/Ma | 11 | EN | 16 | EN |
| | | | | 17 | STR/Ma |
| | | | | 18 | HB |

### Lv15 reference (optimal) — boss tune

| | STR | Ma | HB | EN | HP | W | Lat |
|--|-----|----|----|----|----|----|-----|
| Ren | 42 | 8.8 | 167 | **11.8** | 114 | 8 | 1 |
| Charlotte | 35 | 6.4 | 127 | **19.2** | 260 | 7 | 1 |
| Coda | 20 | 50 | 147 | **10.8** | 73 | 8 | 1 |

Party HP = **447** (EN-only delta vs older 10.8/18.2/9.8 snapshot; HP/DPS unchanged).

Per-level full tables: [`CHARACTER_LEVEL_PROGRESS.md`](../../combat/CHARACTER_LEVEL_PROGRESS.md) (generated).

---

## 5. Skill unlock (no SP)

| Lv | Ren | Charlotte | Coda |
|----|-----|-----------|------|
| 1 | Strike | Ram | Pulse |
| 3 | — | Anchor | — |
| 4 | Crosscut | — | — |
| 5 | — | — | Mend |
| 9 | — | Bulwark | — |
| 10 | Finale | — | — |
| 11 | — | — | Encore |

| Range | Feel |
|-------|------|
| 1–2 | Basic + Space guard |
| 3–5 | Setup/Counter online |
| 9–11 | Full 3-skill kit |
| 12–15 | Stat polish → boss ready |
| 15 | Soft target / boss entry |
| 16–18 | Post-boss (XP dump) or slow grind |

---

## 6. Combat XP curve

### XP to next level

| From→To | XP | Cum. to reach To |
|---------|-----|------------------|
| 1→2 | 60 | 60 |
| 2→3 | 90 | 150 |
| 3→4 | 130 | 280 |
| 4→5 | 180 | 460 |
| 5→6 | 240 | 700 |
| 6→7 | 310 | 1010 |
| 7→8 | 390 | 1400 |
| 8→9 | 480 | 1880 |
| 9→10 | 580 | 2460 |
| 10→11 | 690 | 3150 |
| 11→12 | 810 | 3960 |
| 12→13 | 940 | 4900 |
| 13→14 | 1080 | 5980 |
| 14→15 | 1230 | **7210** |
| 15→16 | **3600** | 10810 |
| 16→17 | **4200** | 15010 |
| 17→18 | **4800** | **19810** |

- Soft target budget: **Σ 1→15 = 7210**
- Soft-cap band: **Σ 15→18 = 12600** = boss first-clear grant

### Dungeon node XP (F1–F15)

Event / Camp / Relay = **0**. Path assumption ~**10** Battle+Elite nodes.

| Floor band | Battle | Elite | Recommended Lv |
|------------|--------|-------|----------------|
| 1–3 | 120 | 200 | 1–4 |
| 4–6 | 220 | 380 | 4–7 |
| 7–9 | 350 | 600 | 7–10 |
| 10–12 | 500 | 850 | 10–13 |
| 13–15 | 700 | 1200 | 13–15 |

Competent path total ≈ **7000–7800** → land **Lv15 ±1** pre-boss.

### Soft-cap grant (dungeon only)

```
granted = baseNodeXP
if partyLevel >= 15:
  granted = floor(granted * 0.12)
if partyLevel > recommendedLv + 2:
  granted = floor(granted * 0.5)
granted = max(granted, 1)
```

Example: Elite F15 @ Lv15 → `floor(1200 × 0.12) = 144` → **~25 elites** for 15→16 alone.

Boss grant **ignores** soft-cap and overlevel penalty.

### Boss F16 first clear

| Rule | Value |
|------|-------|
| Grant | **+12600** Combat XP |
| Expected entry | Lv15 @ 0% into next |
| Expected exit | **Lv18** |
| Underleveled entry | Same grant (may stop &lt;18) |
| Already &gt;15 | Fill toward 18; overflow discarded |
| Repeat clear (MVP) | **0** Combat XP |

---

## 7. Calendar / run map bridge

- Dungeon = Evening activity ([persona calendar](./2026-07-11-persona-calendar-design.md) §9).
- One run can span F1→F16; XP applies per combat node clear.
- Boss defeat also sets story flags (`sector_cleared` / vault) — XP package is combat reward, separate from social EXP.

---

## 8. Out of scope (implement later)

- `CombatXp` / party level runtime
- Grunt/Elite `rewardXp` on encounter assets
- Level-up UI / stat spend screen
- Persist party level in `GameMetaState` / `RunSnapshot`

---

## 9. Source of truth map

| Topic | File |
|-------|------|
| Per-level stats + unlock | `docs/combat/CHARACTER_LEVEL_PROGRESS.md` (+ `.xlsx`) |
| Generator | `Tools/generate-stat-excel.js` |
| Boss entry + XP grant | `docs/combat/BOSS_ENCOUNTER_DESIGN.md` |
| Skill names / footprint | `docs/combat/SKILL_KIT.md` |
| This design | this file |

---

## Changelog

| Date | Note |
|------|------|
| 2026-07-19 | Initial lock: party XP, soft-cap, boss 12600, optimal 6/3/5, SKILL_KIT unlock names |

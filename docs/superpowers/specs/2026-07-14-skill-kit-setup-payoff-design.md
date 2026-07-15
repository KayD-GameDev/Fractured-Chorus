# Skill Kit Redesign — Setup → Payoff (Prep Channel)

> **Status:** Design approved (2026-07-14)  
> **Scope:** Approach 1+ — graft Prep onto existing kit; light Skill/Ult effect rewrite; Basic unchanged  
> **Spine:** Setup → Payoff · Channel Prep on empty Active (S) beats  
> **Out of scope:** Basic rewrite · CORE/MICRO/EYE tag UI · W formula change · AI spawn floor · intro-pause beat 6 · full empty-beat skill catalog (#4 beyond Prep)  
> **Related:** `docs/combat/SKILL_KIT.md` · `docs/combat/COMBAT_MECHANICS.md` §3 · `SkillDefinitionSO` · `SkillFootprintUtil`

---

## 1. Goals

- Combat kit has a readable rhythm: **setup (empty) → clash (notes) → dump (Prep amplify)**.
- Per character: **1 Basic = pure DPS**; **Skill + Ult** = timeline interaction + Prep empower + distinct role effects.
- Early dense notes stay playable: **Prep never gates cast** — amplify only.

### Problem (current)

| Surface | Today | Gap |
|---------|--------|-----|
| Skill/Ult | Mostly Damage + footprint length | Little identity beyond numbers |
| Empty beats | Often “dead” placement | No incentive to plan gaps |
| Utility seeds | Anchor / Mend / Encore / Cycle Shift in docs | Weak or missing runtime + Prep loop |

---

## 2. Locked decisions

| ID | Decision |
|----|----------|
| Spine | **B** Setup → Payoff |
| Scope | **1** Keep Basic; redesign Skill + Ult |
| Channel | **A** +1 Prep per **S** beat with **no** boss note impact |
| Cast vs Prep | **X** Skill/Ult always counter when overlapping notes; Prep amplifies; Prep 0 = base cast |
| Economy | **E1** Cap **3** / unit; Skill empower @ **≥1** (spend 1); Ult @ **≥2** (spend 2) |
| Ownership | Prep **per unit** (not shared party pool) |
| Delivery | **1+** Keep skill names + footprints as spine; light effect rewrite + Prep layer |
| Anchor target | **D1** Delay every **CORE** note whose impact beat lies in Anchor’s **S** beats |

### Skill vs Ult roles (Farm → Dump)

| Slot | Role |
|------|------|
| Basic | DPS only — no Prep read/write |
| Skill (T2) | Clash tool + light empower @1+; strong empty channel (long S where applicable) |
| Ult (T3) | Clash tool + strong empower @2+ |

---

## 3. Prep economy

```
Empty beat ∩ S active  →  +1 Prep (per unit), clamp 0..3
Note beat ∩ S active   →  Counter / Perfect as today; Prep unchanged
Empower Skill          →  if Prep ≥ 1: apply Skill empower, Prep -= 1
Empower Ult            →  if Prep ≥ 2: apply Ult empower, Prep -= 2
Cast with Prep = 0     →  base effect only (always legal)
```

- Leftover Prep after spend is kept (e.g. 3 − 2 = 1).
- S1 / S2 never farm Prep.
- Basic never farms or spends Prep.

---

## 4. Kit tables (Basic unchanged)

### Ren — DPS · Cycle Shift

| # | Name | S1-S-S2 | Base (Prep 0) | Empower |
|---|------|---------|---------------|---------|
| 1 | **Strike** | 1-1-1 | Damage + Cycle Shift | — |
| 2 | **Crosscut** | 2-2-2 | Damage · 2 counter hits (1 per S beat) | **≥1:** +1 extra counter hit on the **first S beat that has a note** (same beat can receive 2 hits); if no note in S, empower is damage ×1.15 only; does **not** advance Cycle Shift |
| 3 | **Finale** | 2-3-3 | Damage burst · 3 counter hits | **≥2:** each hit in this cast uses **Harmony** vs CORE (ignore current Active); soft MULTI cue if ≥3 Perfect in cast |

### Charlotte — Tank · Tempo

| # | Name | S1-S-S2 | Base | Empower |
|---|------|---------|------|---------|
| 1 | **Ram** | 1-1-1 | Damage | — |
| 2 | **Anchor** | 2-2-2 | **DelayBossNote +2** on CORE notes with impact in **S** (D1); 0 damage | **≥1:** Delay **+3**; delayed notes **keep tier** (`DelayKeepTier`) |
| 3 | **Bulwark** | 2-2-3 | Shield **65** + counter damage per S beat | **≥2:** Shield **100**; **1** Perfect in this S → **Guard charge +1** (if Guard system present; else defer Guard charge to stub flag) |

### Coda — Support

| # | Name | S1-S-S2 | Base | Empower |
|---|------|---------|------|---------|
| 1 | **Pulse** | 1-1-1 | Damage (Ma) | — |
| 2 | **Mend** | 2-1-2 | Heal 25 + Ma×0.5 (ally); resolves on empty | **≥1:** Heal **+15**; overheal → Shield equal to overflow (**cap 30**) |
| 3 | **Encore** | 1-1-1 | **ReduceS2 −1** on next skill of target ally | **≥2:** ReduceS2 **−1 for whole party** (next skill each); targeted ally also **+1 Prep** (still cap 3) |

---

## 5. Effect semantics & timeline presentation

### DelayBossNote (Anchor) — D1

- On resolve of Anchor’s Active window: every **CORE** telegraph whose **impact beat ∈ Anchor S beats** shifts **+N** beats later (base N=2, empower N=3).
- Visual: note slides to new beat cell; short ghost on old cell + optional `+2` / `+3` badge.
- Empower: tier (hits remaining) unchanged by the delay (`DelayKeepTier`).

### ReduceS2 (Encore)

- Affects the **next skill placement** footprint of the buffed unit(s): `standingBeatsAfter` reduced by 1 (min 0) for that one placement.
- Visual: buff pip/chip `S2−1` on ally portrait; drag preview shows one fewer S2 standing dot; clears after that skill is placed/resolved.

### Prep UI

| Element | Spec |
|---------|------|
| Location | Per-unit near portrait / skill radial |
| Display | 0–3 pips |
| Motion | Pulse on gain; flash on spend |

### Channel vs clash (lane)

| Situation | Unit lane | Boss row |
|-----------|-----------|----------|
| S on empty | Footprint + Prep +1 at beat resolve | — |
| S on note | Counter / Perfect chip (existing) | Note degrade / cancel (existing) |
| Anchor delay | Anchor footprint | Notes in S slide +N |
| Encore | `S2−1` chip on ally | — |

---

## 6. New / extended effect kinds

| Kind / rule | Purpose |
|-------------|---------|
| Prep channel / spend | Shared economy |
| `DelayKeepTier` | Anchor empower |
| Overheal → Shield (cap 30) | Mend empower |
| ForceHarmonyHits | Finale empower |
| PartyReduceS2 + GiftPrep | Encore empower |
| GuardChargeOnPerfect | Bulwark empower (stub if Guard not ready) |

Existing kinds remain: `Damage`, `MiniDamage`, `Heal`, `Shield`, `ReduceS2`, `DelayBossNote`, `CycleShift`, …

---

## 7. Acceptance (Play)

1. Basic never changes Prep.
2. Empty S on Skill/Ult → +1 Prep / beat, cap 3 per unit.
3. S overlapping note → counter; Prep does not increase.
4. Prep 0 → Skill/Ult still cast **base**.
5. Prep ≥1 Skill / ≥2 Ult → empower applied and stacks spent (1 / 2).
6. Anchor D1: CORE impacts in S shift +2 (+3 empowered); empower keeps tier.
7. Encore: next ally skill preview has S2 −1; empower = party S2 −1 + gift 1 Prep to target.
8. Intro-pause after beat 6 unchanged.

---

## 8. Implementation notes (for plan)

1. Extend `SkillDefinitionSO` (or parallel Prep tuning SO) with empower thresholds / spend / effect params — avoid hardcoding per name in UI.
2. Prep state lives on combat unit runtime (session), not Scene UI alone.
3. Channel check: at S beat fire, if no boss telegraph impact on that beat (boss row) → +Prep. Party footprints do not block channel.
4. Wire Delay / ReduceS2 if still doc-only; then layer empower variants.
5. Update `docs/combat/SKILL_KIT.md` to match this spec after implementation lands (or in same PR as data).
6. Edit Preview / Inspector: expose Prep pips + delay ghost toggles if CombatRoot preview pattern already exists.

---

## 9. Out of scope (later)

- Dedicated empty-only “buff skills” beyond Prep (#4 expansion)
- CORE / MICRO / EYE presentation pass
- Async HB planning latency redesign
- Ren Cycle Shift VFX pass (logic may stub ForceHarmony first)
- Hard AI wall-clock spawn floor

# Docs Runtime SoT Sync

> **Status:** Design approved (2026-07-16)  
> **Spine:** Code runtime = source of truth · docs chỉnh khớp · không đổi C#  
> **Scope:** Approach **3** (Hybrid) · File scope **S2**  
> **Out of scope:** Code/balance changes · empty-beat catalog (#4) design · rewrite historical `PROJECT_LOG` entries · `LOGGING.md` process doc  

**Related:** `docs/combat/SKILL_KIT.md` · `docs/combat/COMBAT_MECHANICS.md` · `docs/combat/BOSS_ENCOUNTER_DESIGN.md` · `docs/setup/UNITY_WORKFLOW.md` · `docs/PROJECT_STATUS.md` · specs `2026-07-14-*`

---

## 1. Goals

- Teammate trên `main` đọc SoT combat **khớp game đang chạy**.
- Xóa / supersede claim gameplay lệch (Delay, intro-pause, Guard %, Phase AV, Encore UI, enemy spawn).
- Giữ specs 07-14 làm lịch sử quyết định, nhưng **không** để luật đã đổi còn đứng như current.

### Locked decisions

| ID | Decision |
|----|----------|
| SoT | **A** — code runtime thắng khi lệch doc |
| File scope | **S2** — SoT combat + design specs 07-14 |
| Method | **3** Hybrid — SoT sửa đầy đủ; specs = banner + sửa inline dòng luật lệch |

---

## 2. Source of truth hierarchy

```
Code runtime
  → SKILL_KIT.md + COMBAT_MECHANICS.md (current sections)
  → Specs 2026-07-14 (history; superseded lines marked / edited inline)
```

Constants to mirror (do not invent numbers in docs):

| Topic | Code |
|-------|------|
| Intro-pause | `TimelineConstants.IntroPlanningPauseAfterBeatIndex = 6` · Execute from `7` |
| Enemy spawn (segment 0) | `IntroEnemySpawnZoneStartBeat = 10` via `GetMinEnemyImpactBeat` |
| Block reduction | `BlockTiming.GetDamageReduction`: OnBeat 0.68 · Early 0.25 · Late 0.10 · OffBeat 0 |
| Phase AV | `PhaseAvTracker` Phase1Budget 150 / Later 100 — still gates assign |
| Anchor Delay | `DelayImpactTelegraphsAfterBeat` — notes **after** S end; notes in S stay |
| Encore UI | `PartyMemberCardView` icon `Resources/UI/Combat/Buffs/buff_reduce_s2_v1` |

---

## 3. Claim → fix map

| Stale claim | Correct (runtime) | Primary files |
|-------------|-------------------|---------------|
| Delay D1 = CORE impacts **in** Anchor S | Delay notes **after** S; notes in S unchanged | skill-kit **spec**; `SKILL_KIT` DelayBossNote row + Charlotte |
| Intro-pause after beat 0 / localBeat 0.5; Continue; auto-resume | Pause after beat **6**; label **Execute**; no auto-resume | `COMBAT_MECHANICS` §1; `UNITY_WORKFLOW` |
| Space Guard Early/Late −15%, Perfect −50% | Barrier reduce OnBeat **68%** / Early **25%** / Late **10%** / OffBeat **0%** | `COMBAT_MECHANICS` §9; `BOSS_ENCOUNTER_DESIGN` |
| “Phase AV removed” | **Still gates** spend; AvLabel UI hidden — document as *legacy retained* | `COMBAT_MECHANICS`; `PROJECT_STATUS` if needed |
| Enemy attacks from beat 3 only | Segment 0 min impact ≥ **beat 10** | `COMBAT_MECHANICS` |
| Encore visual = text chip `S2−1` | **Buff icon** on party card | skill-kit **spec** |
| Delay / ReduceS2 “doc-only” | **Implemented** at Planning | skill-kit **spec** §8 |
| Spec acceptance still open | Note runtime shipped; SoT = SKILL_KIT / MECHANICS | timeline-note + counter-feel **specs** |

---

## 4. Per-file work

### SoT combat

1. **`SKILL_KIT.md`** — Align `DelayBossNote` kind row with “after S”; keep Charlotte table; clarify Encore icon path if needed.
2. **`COMBAT_MECHANICS.md`** — Rewrite §1 intro/enemy lines to beat 6 / Execute; §9 Guard % from `BlockTiming`; Phase AV = legacy retained; §14 rows if stale; changelog entry.
3. **`BOSS_ENCOUNTER_DESIGN.md`** — Align Reactive Guard with barrier model; kit 3 skill / no Guard skill.
4. **`UNITY_WORKFLOW.md`** — Planning flow: Deploy → intro-pause @ beat 6 → Execute (not Continue / auto-resume).
5. **`PROJECT_STATUS.md`** — Note Phase AV legacy gate if still implied removed.

### Specs 07-14 (hybrid)

For each of:
- `2026-07-14-skill-kit-setup-payoff-design.md`
- `2026-07-14-timeline-note-readability-design.md`
- `2026-07-14-counter-presentation-feel-design.md`

Apply:
1. Header status: `Implemented — runtime SoT supersedes where noted (2026-07-16)`.
2. Inline edit only rows that contradict §3 map (especially skill-kit D1, S2−1 chip, doc-only note, acceptance).
3. Do **not** full rewrite kit tables if Prep economy already matches.

### Log

- **`PROJECT_LOG.md`** — one new entry for this sync pass (do not rewrite old dated entries).

---

## 5. Spec banner template

```markdown
> **Status:** Implemented — runtime SoT supersedes where noted (2026-07-16)  
> **SoT:** `docs/combat/SKILL_KIT.md` · `docs/combat/COMBAT_MECHANICS.md`
```

---

## 6. Acceptance

1. No remaining “Delay impacts in S” / “Delay-ReduceS2 doc-only” as current law in SoT or skill-kit spec.
2. SoT + UNITY_WORKFLOW: intro-pause beat **6**, button **Execute**, no Continue/auto-resume as current.
3. Guard reduction documented as **68 / 25 / 10 / 0**.
4. Phase AV documented as **still gating** (legacy retained).
5. Three 07-14 specs carry Implemented banner; HIGH/MED law lines edited.
6. `#4` empty-beat remains backlog / out of scope.

---

## 7. Out of scope

- Any C# / asset / scene change
- New design for empty-beat catalog (#4)
- Full rewrite of historical `PROJECT_LOG` / `LOGGING.md`
- Re-opening closed implementation plan checklists except fixing stale *law* wording if still wrong

---

## Changelog

| Date | Note |
|------|------|
| 2026-07-16 | Design approved: A + S2 + Approach 3 |

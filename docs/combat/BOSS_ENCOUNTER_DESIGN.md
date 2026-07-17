# Boss Encounter — Stat, Skill & Combat Pacing

> **Trạng thái:** Stat/boss lock · **cơ chế gameplay → xem [COMBAT_MECHANICS.md](./COMBAT_MECHANICS.md)**  
> **Ngữ cảnh:** Party Lv 15 vs Boss Lv 18 · scene boss đầu · nhạc **Eternal Spark (Cadence Remix)** · **619 beat** (`EternalSpark_CadenceRemix_beats.csv` + pad t=0 → `MusicBeatMapSO.BeatCount`)  
> **Illustrations:** `docs/combat/illustrations/`  
> **Code hiện tại:** DamageCalculator, PhaseAvTracker *(deprecated)*, EnemyTelegraph, UnitStatBlockSO

---

## 1. Hệ stat


| Stat               | Vai trò                                                                                                                                             |
| ------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| **STR**            | Máu (party) · sát thương đánh tay và skill vật lý                                                                                                   |
| **Ma**             | Sát thương skill phép                                                                                                                               |
| **Heartbeat**      | Thứ tự Planning · beat bar W · telegraph intel · planning latency — xem [COMBAT_MECHANICS.md §10](./COMBAT_MECHANICS.md#10-heartbeat-hb--4-vai-trò) |
| **Endurance (EN)** | Giảm sát thương nhận vào · scale reactive Guard                                                                                                     |


### Chỉ số phụ


| Field          | Ý nghĩa                                                |
| -------------- | ------------------------------------------------------ |
| baseLuck       | % crit mỗi lần skill gây dmg (0–100)                   |
| critMultiplier | Hệ số dmg khi crit                                     |
| maxHp          | Máu tối đa                                             |
| Element        | Melody / Rhythm / Harmony — tam giác pre-condition dmg |


### Tam giác hệ

```
Rhythm → Melody → Harmony → Rhythm
Advantage ×1.5    Disadvantage ×0.5
```

---

## 2. Chỉ số party Lv 15 (optimal build)


|                  | Ren (DPS)                       | Charlotte (Tank) | Coda (Support) |
| ---------------- | ------------------------------- | ---------------- | -------------- |
| **STR**          | 42                              | 35               | 20             |
| **Ma**           | 8.8                             | 6.4              | 50             |
| **HB**           | 167                             | 127              | 147            |
| **EN**           | 10.8                            | 18.2             | 9.8            |
| Element          | Melody (**Active** Cycle Shift) | Rhythm           | Harmony        |
| Dmg type         | Physical                        | Physical         | Magical        |
| Base Luck        | 18%                             | 8%               | 16%            |
| Crit Mult        | ×1.35                           | ×1.15            | ×1.30          |
| **HP**           | 114                             | 260              | 73             |
| Beat bar W       | 8                               | 7                | 8              |
| Planning latency | 1                               | 1                | 1              |


Tổng HP party = 447 · Chi tiết Lv1→18: [CHARACTER_LEVEL_PROGRESS.md](./CHARACTER_LEVEL_PROGRESS.md)

> **§13 auto Lv15 = 536 HP** (pure growth, no manual). §2 = **optimal manual build** (−17%) — dùng cho boss tune & sim.

### Công thức HP

```
Ren:       HP = STR × 2.0 + 30
Charlotte: HP = STR × 6.0 + 50
Coda:      HP = STR × 2.0 + Ma × 0.35 + 15
```

---

## 3. Boss Lv 18 — The Pulse (3 target)

> Boss **không có HB**. **Thân** spawn nốt CORE (dmg HP) · **Micro** / **Mắt** spawn nốt pressure (buff/debuff, không dmg HP). Chi tiết: [COMBAT_MECHANICS.md §5–§6](./COMBAT_MECHANICS.md#5-boss-anatomy--thân--micro--mắt).

### Thân (CORE) — win condition


| Stat      | Giá trị  | Vai trò                     |
| --------- | -------- | --------------------------- |
| STR       | **58**   | Raw dmg nốt CORE leak       |
| Ma        | 24       | Skill phép (future)         |
| EN        | 20       | EnduranceFactor khi bị đánh |
| HP        | **1680** | Pool chính — **0 = thắng**  |
| **Pulse** | **130**  | Spawn gap nốt CORE          |
| Element   | Rhythm   | Harmony vs party            |


### Micro · Mắt (optional kill)


| Target    | HP      | EN  | Element | Leak effect                                     |
| --------- | ------- | --- | ------- | ----------------------------------------------- |
| **Micro** | **280** | 12  | Harmony | Boss `Resonance` +1 (+6% STR/stack, max 3)      |
| **Mắt**   | **200** | 10  | Melody  | Party `Dissonance` +1 (−12% guard/stack, max 3) |


- Counter Perfect nốt mini → **MiniDmg** + gỡ 1 stack · **không leak = không dmg HP**
- Chết Micro → hết spawn Micro + clear `Resonance` · chết Mắt → hết Eye + clear `Dissonance`

Boss 3 target timeline

### Pulse → spawn gap

```
minGap = clamp(5 − ⌊(effectivePulse − 80) / 25⌋, 3, 5)
effectivePulse = pulse × phaseScale
```


| Phase   | Pulse scale | Min gap (Pulse 130) |
| ------- | ----------- | ------------------- |
| Opening | ×0.75       | **5 beat**          |
| Mid     | ×1.0        | **3 beat**          |
| Enrage  | ×1.15       | **3 beat**          |


### Note color weights (normalized)


| Phase   | Tím     | Xanh | Đỏ      |
| ------- | ------- | ---- | ------- |
| Opening | **0%**  | 34%  | **66%** |
| Mid     | 10%     | 40%  | 50%     |
| Enrage  | **38%** | 33%  | 33%     |


### Leak damage (nốt CORE không counter)


| Màu  | Mult trên boss hit |
| ---- | ------------------ |
| Tím  | ×1.35              |
| Xanh | ×1.15              |
| Đỏ   | ×1.0               |


`Resonance` stack nhân thêm vào STR effective khi tính CORE leak.

**Ren vs CORE:** dùng **Active element** (Cycle Shift) — không còn cố định ×0.5. Coda ×1.5 · Charlotte ×1.0.

**Asset:** `StatBlock_Boss_Pulse` (Core) · `StatBlock_Boss_Micro` · `StatBlock_Boss_Eye` · `UnitPreset_Boss_Pulse`

### Balance targets (Monte Carlo · `Tools/simulate-boss-run.js` · **619 beat**)

> ⚠ Sim hiện tại **chỉ model CORE mono HP 2160** (proxy difficulty ≈ CORE 1680 + áp lực mini). Mini + Cycle Shift **chưa** trong sim.


| Skill tier                         | Win rate | Win beat p50 | % bài    | Ghi chú                    |
| ---------------------------------- | -------- | ------------ | -------- | -------------------------- |
| Learning (55% counter · 35% guard) | **~5%**  | ~382         | ~62%     | First clear không kỳ vọng  |
| Competent (70% · 50%)              | **~41%** | ~371         | ~60%     | Cần học kit + guard        |
| Good (82% · 65%)                   | **~85%** | ~367         | **~59%** | Target mastery — leak ~62% |


> Tune 2026-06-30 v2: CORE **1680** / STR **58** (sim mono ≈ Good ~85%). v3 thêm Micro/Mắt — rebalance sim pending.

---

## 4. Công thức sát thương

```
Raw        = Random(tier) × AttackPower × 10
CoreFinal  = Raw × 1/(4×√EN_core) × BeatTiming × Harmony × CritMult
MiniDmg    = Raw × 1/(4×√EN_mini) × BeatTiming × 0.85 × CritMult
```


| Tier        | Random    |
| ----------- | --------- |
| 1 Basic     | 0.80–1.05 |
| 2 Signature | 0.90–1.10 |
| 3 Burst     | 1.10–1.50 |



| Target          | Harmony                                          | BeatTiming                     |
| --------------- | ------------------------------------------------ | ------------------------------ |
| **CORE**        | Ren **Active** · Charlotte Rhythm · Coda Harmony | §8 COMBAT_MECHANICS            |
| **Micro / Mắt** | **Bỏ qua**                                       | Perfect full · Early/Late ×0.5 |


Dmg skill theo target: [SKILL_KIT.md](./SKILL_KIT.md)

### Reactive Guard (Space)

Không còn skill Guard. Space đặt **barrier 1 beat**. SoT: [COMBAT_MECHANICS.md §1 Block / §9](./COMBAT_MECHANICS.md#9-reactive-guard-space).


| Timing vs impact `E` | Giảm dmg (`BlockTiming.GetDamageReduction`) |
| -------------------- | ------------------------------------------- |
| OnBeat (cùng ô)      | **68%**                                     |
| Early (`E−1`)        | **25%**                                     |
| Late (`E+1`)         | **10%**                                     |
| OffBeat              | **0%**                                      |


---

## 5–12. Cơ chế gameplay

> **Deprecated (2026-06-30):** Cycle, Guard skill, telegraph 2-beat/cycle.  
> **Phase AV:** deprecated as UX cycle — **budget gate may still exist in code** (*legacy retained*; xem [COMBAT_MECHANICS.md](./COMBAT_MECHANICS.md)).  
> **Thay bằng:** [COMBAT_MECHANICS.md](./COMBAT_MECHANICS.md) — Planning/Execute, boss notes, HB roles, kit 3 skill.

### Kit skill (tóm tắt — 3 skill / nhân vật)


| Char      | Basic        | Counter        | Burst/Support |
| --------- | ------------ | -------------- | ------------- |
| Ren       | Strike 1-1-1 | Crosscut 2-2-2 | Finale 2-3-3  |
| Charlotte | Ram 1-1-1    | Anchor 2-2-2   | Bulwark 2-2-3 |
| Coda      | Pulse 1-1-1  | Mend 2-1-2     | Encore 1-1-1  |


Chi tiết effect: [SKILL_KIT.md](./SKILL_KIT.md)

### Map sang code


| Thiết kế mới        | Code hiện tại                                  |
| ------------------- | ---------------------------------------------- |
| COMBAT_MECHANICS.md | PhaseAvTracker, batch planning, 1-row timeline |
| STR / Ma            | strength + ma + strengthType                   |
| HP từ STR/Ma        | maxHp nhập tay                                 |
| Boss Pulse          | BossStatBlockSO.pulse + color weights          |
| 3 skill, no Guard   | Kit 3 skill / nhân vật · Space barrier (`BlockBarrierTracker`) |



| Asset     | Preset                                       |
| --------- | -------------------------------------------- |
| Ren       | UnitPreset_Ren · StatBlock_Ren               |
| Charlotte | UnitPreset_Tank · StatBlock_Tank             |
| Coda      | UnitPreset_Mage · StatBlock_Mage             |
| The Pulse | UnitPreset_Boss_Pulse · StatBlock_Boss_Pulse |


---

## 13. Level Progression — Stat Allocation & HB Conversion

> **Mục tiêu:** Player bắt đầu yếu (~25-30% stat Lv15), cảm nhận power curve rõ ràng qua mỗi level. Cap arc 1 = Lv18.
> **Mỗi level = 1 stat point** để cộng vào STR, Ma, EN (+1) hoặc HB (+5).
> **HB quy đổi:** 1 point = +5 HB → tăng beat bar W, giảm planning latency, cải thiện telegraph intel

### Công thức HP (giữ nguyên)

```
Ren:       HP = STR × 2.0 + 30
Charlotte: HP = STR × 6.0 + 50
Coda:      HP = STR × 2.0 + Ma × 0.35 + 15
```

### Auto-Growth Per Level (giảm HB)


| Stat    | Ren  | Charlotte | Coda | Ghi chú                                   |
| ------- | ---- | --------- | ---- | ----------------------------------------- |
| **STR** | +1.0 | +1.0      | +1.0 | Tăng chậm, phụ thuộc manual points        |
| **Ma**  | +0.2 | +0.1      | +1.0 | Ren/Charlotte ít Ma growth                |
| **HB**  | +0.5 | +0.5      | +0.5 | **Giảm mạnh** từ old values (1.9/1.6/2.2) |
| **EN**  | +0.2 | +0.3      | +0.2 | Tăng chậm                                 |


### Manual Allocation — Conversion Table


| Points into | Stat gain | Ý nghĩa                                        |
| ----------- | --------- | ---------------------------------------------- |
| **STR**     | +1        | +2 HP (Ren/Coda) · +6 HP (Charlotte)           |
| **Ma**      | +1        | +Skill dmg                                     |
| **EN**      | +1        | Giảm dmg qua EnduranceFactor + reactive Guard  |
| **HB**      | **+5**    | +W beat bar · intel tốt hơn · latency thấp hơn |


### HB — tác dụng (cơ chế mới)


| Char      | HB Lv15    | Beat bar W | Planning latency |
| --------- | ---------- | ---------- | ---------------- |
| Ren       | 152+manual | 9          | 0                |
| Charlotte | 112+manual | 7          | 0–1              |
| Coda      | 132+manual | 8          | 0                |


+5 HB ≈ +0–1 W (tùy ngưỡng), xem [COMBAT_MECHANICS.md §3](./COMBAT_MECHANICS.md#3-skill-footprint--s1--s--s2)

### Stat tables (Lv15 target — manual build)

```
Stat(Lv) = Base_Lv1 + Auto_Growth × (Lv - 1) + Manual_Points × Conversion
HB Conversion: Manual_Points × 5
Max manual points per stat: 10
```

**Total manual points available:** 17 (Lv 2–18, Lv1 = start, no point)

---

### Ren — DPS · Melody


| Lv  | STR | Ma  | HB    | EN  | HP    | W   |
| --- | --- | --- | ----- | --- | ----- | --- |
| 1   | 22  | 6   | 145   | 4   | 74    | 8   |
| 15  | 37  | 8.8 | 152   | 6.8 | 103.6 | 9   |
| 18  | 40  | 9.4 | 153.5 | 7.4 | 109.4 | 9   |


**Optimal Lv15 build:** 6 STR → 3 HB → 5 EN

### Charlotte — Tank · Rhythm


| Lv  | STR | Ma  | HB    | EN   | HP  | W   |
| --- | --- | --- | ----- | ---- | --- | --- |
| 1   | 15  | 5   | 105   | 10   | 140 | 7   |
| 15  | 30  | 6.4 | 112   | 14.2 | 230 | 7   |
| 18  | 33  | 6.7 | 113.5 | 15.1 | 248 | 7   |


**Optimal Lv15 build:** 6 STR → 3 HB → 5 EN

### Coda — Support · Harmony


| Lv  | STR | Ma  | HB    | EN  | HP   | W   |
| --- | --- | --- | ----- | --- | ---- | --- |
| 1   | 6   | 30  | 125   | 3   | 38   | 7   |
| 15  | 20  | 43  | 132   | 5.8 | 82.4 | 8   |
| 18  | 23  | 46  | 133.5 | 6.4 | 92   | 8   |


**Optimal Lv15 build:** 6 Ma → 3 HB → 5 EN

---

### What 1 Point Actually Does

**HB (+5):** có thể +1 beat bar W khi vượt ngưỡng; cải thiện telegraph intel; giảm planning latency.

**STR (+1) — HP gain depends on class:**


| Character | STR before→after | HP before→after | HP gain |
| --------- | ---------------- | --------------- | ------- |
| Ren       | 37 → 38          | 103.6 → 106     | +2.4    |
| Charlotte | 30 → 31          | 230 → 236       | **+6**  |
| Coda      | 20 → 21          | 82.4 → 84.4     | +2      |


**EN (+1)** — reactive Guard + EnduranceFactor khi nốt chạm impact line.

### Skill Unlock (3 skill — không Guard)


| Lv  | Ren      | Charlotte | Coda   |
| --- | -------- | --------- | ------ |
| 1   | Strike   | Ram       | Pulse  |
| 3   | —        | Anchor    | —      |
| 4   | Crosscut | —         | —      |
| 5   | —        | —         | Mend   |
| 9   | —        | Bulwark   | —      |
| 10  | Finale   | —         | —      |
| 11  | —        | —         | Encore |


> Charlotte unlock sớm nhất (tank cần counter tool early) → Ren → Coda muộn nhất (support complexity ramp slowly).

### Power Milestones


| Level Range | Milestone                                         |
| ----------- | ------------------------------------------------- |
| **1–3**     | Basic only. Learning beat timing + Space guard.   |
| **3–5**     | Counter skill unlock.                             |
| **5–11**    | Full 3-skill kit. Multi-row counter vs Tím notes. |


### Tổng HP Party theo Level (pure auto-growth, no manual)


| Lv  | Ren HP | Charlotte HP | Coda HP | **Total** |
| --- | ------ | ------------ | ------- | --------- |
| 1   | 74     | 140          | 38      | **252**   |
| 5   | 94     | 200          | 51      | **345**   |
| 10  | 114    | 260          | 67      | **441**   |
| 15  | 134    | 320          | 82      | **536**   |
| 18  | 146    | 356          | 92      | **594**   |


---

## 14. Việc cần làm

> Chi tiết implementation: [COMBAT_MECHANICS.md §14](./COMBAT_MECHANICS.md#14-map-sang-code-delta)

### P0

- Boss 3 target + note tag CORE/MICRO/EYE
- Ren Cycle Shift + Active element dmg
- Mini pressure (`Resonance` / `Dissonance`)
- COMBAT_MECHANICS.md — Planning pause + dual BGM
- 3-row timeline UI + boss note row
- Skill S1/S/S2 footprint
- Boss note HP (Tím/Xanh/Đỏ) CORE + degrade
- Bỏ PhaseAvTracker
- Reactive Space guard

### P1

- HB → W, intel, latency
- Async per-char planning
- 9 skill asset (3 × 3 char)
- Boss spawn random + min gap

---

## Changelog


| Ngày       | Nội dung                                                                                                    |
| ---------- | ----------------------------------------------------------------------------------------------------------- |
| 2026-06-29 | Tạo doc — stat Lv15, boss Lv18, kit CE2, tune dài+nhanh, mô phỏng, skill 12                                 |
| 2026-06-29 | Thêm Section 13 — Level Progression Lv1→18, growth curves, skill unlock, power milestones                   |
| 2026-06-29 | Overhaul Section 13 — Stat allocation system, HB conversion (+5/point), auto-growth reduced, example builds |
| 2026-06-30 | **COMBAT_MECHANICS.md** — Planning/Execute loop, boss notes, HB roles, kit 3 skill                          |
| 2026-06-30 | BOSS_ENCOUNTER_DESIGN.md — deprecate cycle/AV/Guard, link mechanics doc, update stats/todos                 |
| 2026-06-30 | Boss 3 target · Ren Cycle Shift · CoreFinal/MiniDmg · skill dmg tables                                      |
| 2026-06-30 | Retune Good ~85% · sync 619 beat                                                                            |

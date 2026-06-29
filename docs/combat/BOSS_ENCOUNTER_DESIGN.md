# Boss Encounter — Stat, Skill & Combat Pacing

> **Trạng thái:** Thiết kế (design lock tạm) · chưa implement vào Unity  
> **Ngữ cảnh:** Party Lv 15 vs Boss Lv 18 · scene boss đầu · nhạc Eternal Spark (Cadence Remix)  
> **Tham chiếu gameplay:** Caligula Effect 2 (timeline 3 giai đoạn, counter khi trùng beat)  
> **Code hiện tại:** DamageCalculator, PhaseAvTracker, EnemyTelegraph, UnitStatBlockSO

---

## 1. Hệ stat

| Stat | Vai trò |
|------|---------|
| **STR** | Máu (party) · sát thương đánh tay và skill vật lý |
| **Ma** | Sát thương skill phép |
| **Heartbeat** | Tốc độ / ưu tiên trên cùng beat. Base AV = 12000 / Heartbeat — số thấp = nhanh hơn |
| **Endurance (EN)** | Giảm sát thương nhận vào · scale Guard |

### Chỉ số phụ

| Field | Ý nghĩa |
|-------|---------|
| baseLuck | % crit mỗi lần skill gây dmg (0–100) |
| critMultiplier | Hệ số dmg khi crit |
| maxHp | Máu tối đa |
| Element | Melody / Rhythm / Harmony — tam giác pre-condition dmg |

### Tam giác hệ

```
Rhythm → Melody → Harmony → Rhythm
Advantage ×1.5    Disadvantage ×0.5
```

---

## 2. Chỉ số party Lv 15

| | Ren (DPS) | Charlotte (Tank) | Coda (Support) |
|--|-----------|------------------|----------------|
| **STR** | 74 | 55 | 22 |
| **Ma** | 16 | 12 | 82 |
| **HB** | 172 | 128 | 156 |
| **EN** | 12 | 26 | 10 |
| Element | Melody | Rhythm | Harmony |
| Dmg type | Physical | Physical | Magical |
| Base Luck | 18% | 8% | 16% |
| Crit Mult | ×1.35 | ×1.15 | ×1.30 |
| **HP** | 178 | 380 | 111 |
| Base AV | ~69.8 | ~93.8 | ~76.9 |

Tổng HP party = 669

### Công thức HP

```
Ren:       HP = STR × 2.0 + 30
Charlotte: HP = STR × 6.0 + 50
Coda:      HP = STR × 2.0 + Ma × 0.35 + 15
```

---

## 3. Boss Lv 18 — The Pulse

| Stat | Giá trị |
|------|---------|
| STR | 88 |
| Ma | 24 |
| HB | 138 |
| EN | 20 |
| HP | 3000 |
| Element | Rhythm |

Boss Rhythm → Coda Advantage ×1.5 · Ren Disadvantage ×0.5

### Pattern

| Giai đoạn | Cycle | Hành vi |
|-----------|-------|---------|
| Mở đầu | 1–5 | Đơn mục tiêu front |
| Phase 2 | 6+ | Tempo tăng · 2 telegraph/cycle |
| AOE | 3·6·9… | Toàn party |
| Phase 3 | 12+ | Enrage +20% dmg |

---

## 4. Công thức sát thương

```
Raw   = Random(tier) × AttackPower × 10
Final = Raw × 1/(4×√EN) × BeatTiming × Harmony × CritMult
```

| Tier | Random |
|------|--------|
| 1 Basic | 0.80–1.05 |
| 2 Signature | 0.90–1.10 |
| 3 Burst | 1.10–1.50 |

### Guard

```
Guard (Ren):      min(EN × 2.5, 65%)
Parry (Charlotte): min(EN × 3.0, 75%)
Ward (Coda, ally): min(EN × 2 + Ma × 0.5, 55%)
Perfect Guard = 100% block + stagger boss 1 beat
```

---

## 5. Cycle and AV

| | Cycle 1 | Cycle 2+ |
|--|---------|----------|
| AV | 180 | 130 |
| Beat/cycle | 12 | 12 |
| Sec/cycle | ~4.2 | ~4.2 |

| Loại | AV |
|------|-----|
| Basic | 0 |
| Signature | 25 |
| Guard | 0 |

Tổng mục tiêu: 16–17 cycle ≈ 70 giây nhạc

---

## 6. Telegraph

Telegraph = dấu hiệu boss sẽ đánh ở beat nào (EnemyTelegraph trong code).

| Thuật ngữ | Ý nghĩa |
|-----------|---------|
| Telegraph | Ô đỏ / icon boss |
| Tung | Beat nhân vật thực hiện skill |
| Counter | Tung trùng beat telegraph |
| Perfect | Counter + On-Beat |

2 telegraph/cycle: Parry/Hold cover cả cycle.

---

## 7. Skill 3 giai đoạn (CE2)

```
Chọn skill → CHỜ 1 → TUNG → CHỜ 2 → lượt tiếp
```

| Giai đoạn | Ý nghĩa |
|-----------|---------|
| Chờ 1 | Wind-up · prepare |
| Tung | Active · 1–2 beat · counter window |
| Chờ 2 | Recovery · xong mới skill mới |

### Footprint

| Loại | Tổng |
|------|------|
| Basic | 3 (1–1–1) |
| Signature | 3–4 |
| Guard | 3 |
| Burst | 5 |

---

## 8. Counter and Perfect

| Loại skill | Counter | Perfect |
|------------|---------|---------|
| Tấn công | Hủy đòn · phản 50% dmg | Phản 100% · Ren −1 beat Chờ 2 |
| Guard | Giảm dmg EN | 0 dmg + stagger boss |

---

## 9. Bộ skill 12 skill (4 × 3 nhân vật)

### Ren — DPS · Melody

| # | Tên | Loại | AV | 1–T–2 | Scale | Mô tả |
|---|-----|------|-----|-------|-------|-------|
| 1 | Strike | Basic | 0 | 1–1–1 | STR | Đánh 1 mục tiêu |
| 2 | Riposte | Sig | 25 | 1–1–2 | STR | Counter ×1.5 · Perfect ×2 |
| 3 | Finale | Sig | 25 | 1–1–2 | STR | Tier 3 · Perfect bỏ Chờ 2 |
| 4 | Guard | Guard | 0 | 1–1–2 | EN | EN×2.5% cap 65% |

### Charlotte — Tank · Rhythm

| # | Tên | Loại | AV | 1–T–2 | Scale | Mô tả |
|---|-----|------|-----|-------|-------|-------|
| 1 | Ram | Basic | 0 | 1–1–2 | STR | +threat boss ưu tiên |
| 2 | Bulwark | Sig | 25 | 2–1–2 | EN | Shield EN×8 · counter giữ + phản |
| 3 | Hold the Line | Sig | 25 | 1–2–2 | EN | 2 beat · giảm dmg ally EN×3% cap 45% |
| 4 | Parry | Guard | 0 | 1–1–1 | EN | EN×3% cap 75% · Perfect stagger |

### Coda — Support · Harmony

| # | Tên | Loại | AV | 1–T–2 | Scale | Mô tả |
|---|-----|------|-----|-------|-------|-------|
| 1 | Pulse | Basic | 0 | 1–1–1 | Ma | +Resonance stack |
| 2 | Arc | Sig | 25 | 1–1–2 | Ma | Adv vs Rhythm · Perfect strip buff |
| 3 | Cadence | Sig | 25 | 1–1–1 | Ma | Heal ally Ma×1.2 · Perfect cleanse +−1 beat |
| 4 | Ward | Guard | 0 | 1–1–2 | EN+Ma | Guard 1 ally · Perfect reflect phép |

### Phân vai Signature

| Class | Sig A (counter) | Sig B (burst/support) |
|-------|-----------------|----------------------|
| DPS | Riposte | Finale |
| Tank | Bulwark | Hold the Line |
| Support | Arc | Cadence |

---

## 10. Nhịp trận

| | Cũ | Mới |
|--|-----|-----|
| Cycle thắng | 10 | 16–17 |
| Giây nhạc | 62s | 70–74s |
| Beat/cycle | 16 | 12 |
| Telegraph | 1/cycle | 2/cycle |
| Boss HP | 1850 | 3000 |
| AV | 150/100 | 180/130 |

### Phase timeline

| Mốc | Cycle | ~Giây |
|-----|-------|-------|
| Mở trận | 1 | 0s |
| Phase 2 | 6 | ~25s |
| Phase 3 Enrage | 12 | ~50s |
| Victory | 16–17 | ~67–71s |

---

## 11. Mô phỏng

| Kết quả | Cycle | ~Giây |
|---------|-------|-------|
| HP 1850 · 1 telegraph | 10 | 62s |
| HP 3000 · 2 telegraph · 4.2s/cycle | 16–17 | ~70s |

Script: tools/simulate-12cycle.mjs · tools/simulate-fast-pace-long.mjs  
Ảnh: assets/boss-fight-simulation-infographic.png

---

## 12. Map sang code

| Thiết kế | Code hiện tại |
|----------|--------------|
| STR / Ma | Chỉ strength + strengthType |
| HP từ STR | maxHp nhập tay |
| 4 skill | Kit cũ Basic/Skill/Ult/Guard |
| 3 giai đoạn | Chưa có |
| Telegraph | EnemyTelegraph, SimpleEnemyAI |
| AV | PhaseAvTracker (150/100) |
| Counter CE2 | Chưa implement |

| Thiết kế | Asset |
|----------|-------|
| Ren | UnitPreset_Ren |
| Charlotte | UnitPreset_Tank |
| Coda | UnitPreset_Mage |

---

## 13. Level Progression — Stat Allocation & HB Conversion

> **Mục tiêu:** Player bắt đầu yếu (~25-30% stat Lv15), cảm nhận power curve rõ ràng qua mỗi level. Cap arc 1 = Lv18.
> **Mỗi level = 1 stat point** để cộng vào STR, Ma, EN (+1) hoặc HB (+5).
> **HB quy đổi:** 1 point +5 HB ≈ 3-4 AV reduction (bằng 0.5 beat advantage/cycle). Nếu +1 HB thì barely noticeable.

### Công thức HP (giữ nguyên)

```
Ren:       HP = STR × 2.0 + 30
Charlotte: HP = STR × 6.0 + 50
Coda:      HP = STR × 2.0 + Ma × 0.35 + 15
```

### Auto-Growth Per Level (giảm HB)

| Stat | Ren | Charlotte | Coda | Ghi chú |
|------|-----|-----------|------|---------|
| **STR** | +1.0 | +1.0 | +1.0 | Tăng chậm, phụ thuộc manual points |
| **Ma** | +0.2 | +0.1 | +1.0 | Ren/Charlotte ít Ma growth |
| **HB** | +0.5 | +0.5 | +0.5 | **Giảm mạnh** từ old values (1.9/1.6/2.2) |
| **EN** | +0.2 | +0.3 | +0.2 | Tăng chậm |

### Manual Allocation — Conversion Table

| Points into | Stat gain | Ý nghĩa |
|-------------|-----------|---------|
| **STR** | +1 | +2 HP (Ren/Coda) · +6 HP (Charlotte) |
| **Ma** | +1 | +Skill dmg |
| **EN** | +1 | +Guard % · +0.3-0.6 AV reduction |
| **HB** | **+5** | +2-3 AV reduction (~0.5 beat faster/cycle) |

### HB Conversion Rationale

```
Base AV = 12000 / HB
```

| HB trước | +1 HB | +5 HB (1 point) | AV shift |
|-----------|-------|-----------------|----------|
| 100 | 101 (+0.08 AV) | 105 | **-3.4** |
| 125 | 126 (+0.06 AV) | 130 | **-2.8** |
| 145 | 146 (+0.05 AV) | 150 | **-2.7** |
| 170 | 171 (+0.04 AV) | 175 | **-2.4** |

> 1 HB ≈ 0.06-0.08 AV → barely noticeable. 5 HB ≈ 3-4 AV → meaningful.

### Stat Formula

```
Stat(Lv) = Base_Lv1 + Auto_Growth × (Lv - 1) + Manual_Points × Conversion
HB Conversion: Manual_Points × 5
Max manual points per stat: 10
```

**Total manual points available:** 17 (Lv 2–18, Lv1 = start, no point)

---

### Ren — DPS · Melody

| Lv | STR | Ma | HB | EN | HP | AV | Manual |
|----|-----|----|----|----|----|-----|--------|
| 1 | 22 | 6 | 145 | 4 | 74 | 82.8 | — |
| 5 | 27 | 6.8 | 147 | 4.8 | 83.6 | 81.4 | 4 (2S/1H/1E) |
| 10 | 32 | 7.8 | 149.5 | 5.8 | 93.6 | 80.3 | 9 (4S/2H/3E) |
| 15 | 37 | 8.8 | 152 | 6.8 | 103.6 | 78.9 | 14 (6S/3H/5E) |
| 18 | 40 | 9.4 | 153.5 | 7.4 | 109.4 | 78.2 | 17 (8S/4H/5E) |

**Optimal Lv15 build:** 6 STR → 3 HB → 5 EN

### Charlotte — Tank · Rhythm

| Lv | STR | Ma | HB | EN | HP | AV | Manual |
|----|-----|----|----|----|----|-----|--------|
| 1 | 15 | 5 | 105 | 10 | 140 | 114.3 | — |
| 5 | 20 | 5.4 | 107 | 11.2 | 170 | 112.1 | 4 (2S/1H/1E) |
| 10 | 25 | 5.9 | 109.5 | 12.7 | 200 | 109.6 | 9 (4S/2H/3E) |
| 15 | 30 | 6.4 | 112 | 14.2 | 230 | 107.1 | 14 (6S/3H/5E) |
| 18 | 33 | 6.7 | 113.5 | 15.1 | 248 | 105.7 | 17 (8S/4H/5E) |

**Optimal Lv15 build:** 6 STR → 3 HB → 5 EN

### Coda — Support · Harmony

| Lv | STR | Ma | HB | EN | HP | AV | Manual |
|----|-----|----|----|----|----|-----|--------|
| 1 | 6 | 30 | 125 | 3 | 38 | 96.0 | — |
| 5 | 10 | 34 | 127 | 3.8 | 50.8 | 94.5 | 4 (2M/1H/1E) |
| 10 | 15 | 38.5 | 129.5 | 4.8 | 66.6 | 92.7 | 9 (4M/2H/3E) |
| 15 | 20 | 43 | 132 | 5.8 | 82.4 | 90.9 | 14 (6M/3H/5E) |
| 18 | 23 | 46 | 133.5 | 6.4 | 92 | 89.9 | 17 (8M/4H/5E) |

**Optimal Lv15 build:** 6 Ma → 3 HB → 5 EN

---

### What 1 Point Actually Does

**HB (+5) — Most impactful for AV:**

| Character | HB before→after | AV before→after | Beat shift |
|-----------|----------------|-----------------|------------|
| Ren Lv15 | 152 → 157 | 78.9 → 76.4 | +2.5 (0.59s faster) |
| Charlotte Lv15 | 112 → 117 | 107.1 → 102.6 | +4.5 (1.05s faster) |
| Coda Lv15 | 132 → 137 | 90.9 → 87.6 | +3.3 (0.78s faster) |

**STR (+1) — HP gain depends on class:**

| Character | STR before→after | HP before→after | HP gain |
|-----------|-----------------|-----------------|---------|
| Ren | 37 → 38 | 103.6 → 106 | +2.4 |
| Charlotte | 30 → 31 | 230 → 236 | **+6** |
| Coda | 20 → 21 | 82.4 → 84.4 | +2 |

**EN (+1) — Guard & AV:**

| Character | EN before→after | Guard % shift | AV shift |
|-----------|-----------------|---------------|----------|
| Ren | 6.8 → 7.8 | 17% → 19.5% | -1.6 |
| Charlotte | 14.2 → 15.2 | 42.6% → 45.6% | -1.2 |
| Coda | 5.8 → 6.8 | 14.1% → 16.4% | -1.5 |

**Ma (+1) — Skill damage:**

| Character | Ma before→after | Heal gain (Coda) | Dmg gain (Ren) |
|-----------|-----------------|------------------|----------------|
| Ren | 8.8 → 9.8 | — | +1% |
| Coda | 43 → 44 | +1.2 heal | +1% |

---

### Skill Unlock

| Lv | Ren | Charlotte | Coda |
|----|-----|-----------|------|
| 1 | Strike (Basic) + Guard | Ram (Basic) + Parry (Guard) | Pulse (Basic) + Ward (Guard) |
| 3 | — | **Bulwark** (Sig A: counter) | — |
| 4 | **Riposte** (Sig A: counter) | — | — |
| 5 | — | — | **Arc** (Sig A: counter) |
| 9 | — | **Hold the Line** (Sig B: support) | — |
| 10 | **Finale** (Sig B: burst) | — | — |
| 11 | — | — | **Cadence** (Sig B: heal) |

> Charlotte unlock sớm nhất (tank cần counter tool early) → Ren → Coda muộn nhất (support complexity ramp slowly).

### Power Milestones

| Level Range | Milestone |
|-------------|-----------|
| **1–3** | Basic attacks + Guard. Learning beat timing. |
| **3–5** | Sig A unlock. Counter mechanic introduced. |
| **5–9** | Full counter toolkit. Player practices Perfect timing. |
| **9–11** | Sig B unlock. Full kit available before boss. |
| **12–15** | Stats approach design target. Pre-boss power spike. |
| **15–18** | Grind zone. Diminishing returns. Optional for challenge seekers. |

### Tổng HP Party theo Level (pure auto-growth, no manual)

| Lv | Ren HP | Charlotte HP | Coda HP | **Total** |
|----|--------|-------------|---------|-----------|
| 1 | 74 | 140 | 38 | **252** |
| 5 | 94 | 200 | 51 | **345** |
| 10 | 114 | 260 | 67 | **441** |
| 15 | 134 | 320 | 82 | **536** |
| 18 | 146 | 356 | 92 | **594** |

---

## 14. Việc cần làm

### P0

- [ ] Field Ma riêng hoặc quy ước rõ
- [ ] maxHp derive từ STR
- [ ] Skill phase: windUpBeats, activeBeats, recoveryBeats
- [ ] Resolve counter khi activeBeat == telegraph.beat
- [ ] Parry/Hold cover cả cycle
- [ ] Tune PhaseAvTracker: 180/130

### P1

- [ ] Đổi tên preset Charlotte / Coda
- [ ] 12 skill asset
- [ ] Boss preset + telegraph pattern
- [ ] Stat block Lv15 + Lv18

### P2

- [ ] UI timeline màu Chờ/Tung/Chờ 2
- [ ] Flash vàng Perfect window
- [ ] Phase 2 @ cycle 6, Phase 3 @ cycle 12

---

## Changelog

| Ngày | Nội dung |
|------|----------|
| 2026-06-29 | Tạo doc — stat Lv15, boss Lv18, kit CE2, tune dài+nhanh, mô phỏng, skill 12 |
| 2026-06-29 | Thêm Section 13 — Level Progression Lv1→18, growth curves, skill unlock, power milestones |
| 2026-06-29 | Overhaul Section 13 — Stat allocation system, HB conversion (+5/point), auto-growth reduced, example builds |

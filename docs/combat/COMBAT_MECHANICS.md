# Combat Mechanics — Planning / Execute Loop

> **Trạng thái:** Design lock (2026-06-30) · thay thế cơ chế Phase AV + cycle cũ  
> **Tham chiếu:** Caligula Effect 2 · nhạc Eternal Spark (Cadence Remix)  
> **Illustrations:** `docs/combat/illustrations/`

---

## 1. Vòng lặp combat

```
Intro 3s
  → Planning (pause timeline · nhạc 1 loop)
  → [Execute] confirm
  → Execute (timeline + nhạc 2 chạy)
  → S2 skill xong (+ latency) → Planning char đó
  → lặp đến hết trận
```

| Giai đoạn | Timeline | Nhạc |
|-----------|----------|------|
| **Planning** | Pause | Nhạc 1 — soft/slow BGM, loop |
| **Execute** | Chạy sync beat | Nhạc 1 dừng · Nhạc 2 — BGM gốc |

- **Bỏ:** Phase AV budget chung party · cycle cố định · skill Guard trên kit
- **Giữ:** Nút **Execute** mỗi lần plan (player confirm)

---

## 2. Timeline UI

### Layout

| Row | Nội dung |
|-----|----------|
| 1 | Ren |
| 2 | Charlotte |
| 3 | Coda |
| 4 (chung) | Boss notes |

- **Cùng beat, khác row** → hợp lệ (chồng counter multi-hit)
- **Cùng row** → skill mới bắt đầu sau S2 skill trước; S1 không được nằm trong vùng S2 cũ

### Impact Line (thanh đỏ)

- Nốt boss spawn từ phía impact line, di chuyển về impact line
- **Dmg lên player** chỉ khi nốt **chạm impact line**
- Không counter kịp → **Space guard** (reactive)

---

## 3. Skill footprint — S1 · S · S2

```
[S1 wind-up] → [S active] → [S2 recovery]
```

| Loại | S | Ghi chú |
|------|---|---------|
| **Basic** | **1 beat** cố định | S2 ngắn, hồi nhanh |
| **Counter** | **1–3 beat** | S dài để mài nhiều nốt trên nhiều beat |
| **Burst/Support** | theo skill | Tier cao, S2 dài hơn |

### Thanh giới hạn beat (Planning Window W)

- Player đặt skill trong thanh **W beat**
- **Toàn bộ S** phải nằm trong W
- **S1** có thể tràn ra trái · **S2** có thể tràn ra phải

```
W = clamp(7 + ⌊(HB − 120) / 26⌋, 7, 10)
```

| Char | HB (Lv15) | W |
|------|-----------|---|
| Ren | 172 | **9** |
| Coda | 156 | **8** |
| Charlotte | 128 | **7** |

### Planning flow

**Lần đầu (trước Execute 1):** thứ tự HB cao → thấp, mỗi char **1 skill** → Execute.

**Sau Execute:** async — hết S2 char nào → pause → Planning char đó (không chờ party).

**Planning latency** (beat chờ trước khi mở UI sau S2):

```
Latency = max(0, 2 − ⌊HB / 85⌋)
```

| Char | HB | Latency |
|------|-----|---------|
| Ren / Coda | 172 / 156 | 0 beat |
| Charlotte | 128 | 1 beat |

---

## 4. Boss notes — màu = hit còn lại (cùng 1 beat)

> **Không phải** nhiều beat liên tiếp cùng màu.

| Màu | Hit còn lại để triệt tiêu |
|-----|---------------------------|
| **Tím** | 3 |
| **Xanh** | 2 |
| **Đỏ** | 1 |
| *(mất)* | 0 — triệt tiêu |

### Degrade

```
Tím(3) ──1 hit──► Xanh(2) ──1 hit──► Đỏ(1) ──1 hit──► CANCELLED
```

- Mỗi **frame S active** trên beat = **1 counter hit** lên nốt tại beat đó (Perfect timing)
- Triệt tiêu nốt Tím @ 1 beat → cần **3 hit cùng beat** (3 row chồng hoặc nhiều lượt plan)
- S dài 3 beat (9–10–11) = 1 hit / beat cho nốt **trên từng beat**, không gom 3 hit vào 1 beat

### Spawn

- Random từ impact line trở đi
- Khoảng cách tối thiểu giữa đợt: **3–4 beat** (tunable)
- Không theo cycle — pattern có trọng số theo boss phase

---

## 5. Counter timing (skill)

So sánh beat **S active** vs beat **nốt boss**:

| Δ beat | Tên | Skill dmg | Triệt tiêu hit nốt |
|--------|-----|-----------|---------------------|
| −2, −1 | Early | ×0.5 | ✗ |
| **0** | **Perfect** | Full | **✓ −1 hit** |
| +1, +2 | Late | ×0.25 | ✗ |
| ngoài ±2 | Off-beat | ×0.01 | ✗ |

Công thức dmg giữ nguyên:

```
Raw   = Random(tier) × AttackPower × 10
Final = Raw × 1/(4×√EN) × BeatTiming × Harmony × CritMult
```

---

## 6. Reactive Guard (Space)

**Không còn skill Guard.** Player guard chủ động khi nốt còn sót chạy về impact line.

| Timing (±1 beat vs nốt) | Giảm dmg |
|---------------------------|----------|
| Early / Late | **−15%** |
| Perfect (±0) | **−50%** |
| Off-beat | **0%** |

```
DmgTaken = BossRaw × (1 − GuardReduction) × EnduranceFactor
```

EN vẫn scale reduction qua `EnduranceFactor`.

---

## 7. Heartbeat (HB) — 4 vai trò

| # | Vai trò | Cơ chế |
|---|---------|--------|
| 1 | Thứ tự Planning | HB cao → trước |
| 2 | Beat bar width **W** | Công thức §3 |
| 3 | **Telegraph intel** (lúc Planning, timeline chung) | Ren: màu + hit + beat chính xác · Coda: màu + hit, beat ±1 · Charlotte: cảnh báo mờ |
| 4 | **Planning latency** | Công thức §3 |
| — | UI assist khi kéo skill | Ren: highlight Perfect · Coda: ±1 · Charlotte: presence only |

**Bỏ:** Phase AV · Base AV priority · skill Guard · HB giảm S2 beat

---

## 8. Kit skill (3 skill / nhân vật)

### Ren — DPS · Melody

| # | Tên | Loại | S1–S–S2 | Scale | Mô tả |
|---|-----|------|---------|-------|-------|
| 1 | Strike | Basic | 1–1–1 | STR | Đánh nhanh |
| 2 | Riposte | Counter | 2–3–1 | STR | S rộng, mài nhiều nốt |
| 3 | Finale | Burst | 1–2–2 | STR | Tier 3 burst |

### Charlotte — Tank · Rhythm

| # | Tên | Loại | S1–S–S2 | Scale | Mô tả |
|---|-----|------|---------|-------|-------|
| 1 | Ram | Basic | 1–1–1 | STR | +threat |
| 2 | Bulwark | Counter | 2–2–2 | EN | Counter + shield |
| 3 | Hold the Line | Support | 1–3–2 | EN | S=3, cover ally |

### Coda — Support · Harmony

| # | Tên | Loại | S1–S–S2 | Scale | Mô tả |
|---|-----|------|---------|-------|-------|
| 1 | Pulse | Basic | 1–1–1 | Ma | Resonance stack |
| 2 | Arc | Counter | 1–3–1 | Ma | S=3, strip buff |
| 3 | Cadence | Support | 2–1–2 | Ma | Heal · cleanse |

---

## 9. Walkthrough — beat 9–11

**Spawn:** beat 9 Tím(3) · beat 10 Đỏ(1) · beat 11 Đỏ(1)

**Ren** S1:1 S:3 S2:2 @ 8–13:

| Beat | Kết quả |
|------|---------|
| 9 | Tím → **Xanh** (2 hit còn) |
| 10 | Đỏ → **Cancel** |
| 11 | Đỏ → **Cancel** |

**Coda Basic** S1:1 S:1 S2:1 @ 8–10:

| Beat | Kết quả |
|------|---------|
| 9 | Xanh +1 hit → **Đỏ** (1 hit còn) |

**Hết skill** → Space guard nốt Đỏ @ beat 9 khi chạm impact line.

→ Xem `illustrations/combat-note-walkthrough-example.png`

---

## 10. Illustrations

| File | Nội dung |
|------|----------|
| `combat-timeline-3-rows.png` | Timeline 3 row + boss notes |
| `combat-planning-execute-music.png` | Planning vs Execute + dual BGM |
| `combat-hb-roles-comparison.png` | HB 4 vai trò |
| `combat-counter-guard-timing.png` | Counter ±2 · Guard ±1 · degrade |
| `combat-note-walkthrough-example.png` | Ví dụ beat 9–11 |
| `combat-same-beat-stacking.png` | Chồng 3 hit cùng beat |

---

## 11. Map sang code (delta)

| Thiết kế mới | Code hiện tại | Action |
|--------------|---------------|--------|
| Planning pause + dual music | Single BGM | P0 |
| 3-row timeline | 1 agenda list | P0 |
| S1/S/S2 footprint | Chưa có | P0 |
| Note HP (tím/xanh/đỏ) | EnemyTelegraph 1-hit | P0 |
| Bỏ PhaseAvTracker | PhaseAvTracker 150/100 | P0 remove |
| Reactive Space guard | GuardHeldSinceQuery | P1 refactor |
| HB → W, intel, latency | HB → BaseAv only | P1 |
| Async per-char planning | Batch planning | P1 |

---

## Changelog

| Ngày | Nội dung |
|------|----------|
| 2026-06-30 | Tạo doc — Planning/Execute loop, boss notes, HB roles, kit 3 skill, illustrations |

# Combat Mechanics — Planning / Execute Loop

> **Trạng thái:** Design lock (2026-06-30) · thay thế cơ chế Phase AV + cycle cũ  
> **Tham chiếu:** Caligula Effect 2 · nhạc Eternal Spark (Cadence Remix)  
> **Illustrations:** `docs/combat/illustrations/`

---

## 1. Vòng lặp combat

```
Dàn trận (kéo unit vào ô)
  → [Deploy] → nhạc chạy
  → scan cho beat 0 kêu + lướt qua vạch → INTRO-PAUSE tại localBeat 0.5 (chỉ lần Deploy đầu)
  → Player đặt skill lên lane (kéo-thả; footprint S1 xám · S màu · S2 xám)
  → auto tiếp khi cả đội đã xếp skill · hoặc bấm [Execute]
  → Timeline chạy **2 phase** (beat 0–31) rồi dừng
  → [Execute] — đặt skill vòng mới (không intro-pause) → chạy 2 phase → lặp
  → quái telegraph **2 pha** S1 (wind-up) + S (impact) từ beat thứ 3 trở đi
  → lặp đến hết trận
```

| Giai đoạn | Timeline | Nhạc |
|-----------|----------|------|
| **Deploy** | Pause (chờ Deploy) | Chưa phát |
| **Intro-pause** | Pause tại localBeat 0.5 (beat 0 đã kêu, trước khi beat 1 chạm vạch) | **Pause tại chỗ** (giữ vị trí bài) |
| **Execute** | Chạy sync beat | Phát tiếp từ chỗ pause |

- **Bỏ:** Phase AV budget chung party · cycle cố định · skill Guard trên kit
- **Giữ:** Nút **Deploy** (dàn trận) → **Execute** (sau intro-pause và mỗi round segment). Nhãn do `CombatController` ép runtime.

### Intro-pause (set up skill sau khi qua beat đầu tiên)

- Player **dàn trận** xong → bấm **Deploy** → `CombatController.StartRound` khoá reposition + `PlayBossMusic` + `BeatTimelineUIView.BeginRoundPlayback`.
- Pause theo **vị trí phân số của vạch quét**: `BeatTimelineUIView.PlanningPauseLocalBeat` (hằng số code = `0.5`, đơn vị beat; không bị scene serialize ghi đè; `-1` = tắt). `0.5` = vạch quét nằm giữa beat 0 và beat 1 → beat 0 đã kêu + lướt qua vạch, **dừng NGAY TRƯỚC khi beat 1 chạm vạch** (nhạc dừng sớm hơn so với dừng tại beat 1). `TryEnterPlanningPauseByLocalBeat()` kiểm tra `_localBeat >= ngưỡng` trong scan loop → `EnterPlanningPause`:
  - `CombatMusicController.PausePlayback()` — pause AudioSource tại chỗ, beat nhạc đóng băng theo `source.time`.
  - `RefreshLaneMarkers()` — refresh markers + footprint ngay lúc pause để điểm tròn hiện đúng thời điểm nút Continue xuất hiện.
  - `Debug.Log("[BeatTimeline] Intro-pause tại localBeat=…")` — xác nhận code mới đã chạy.
  - Timeline scan dừng, **không** gọi `FinishPlayback` (chưa Execute).
  - `CombatController.OnTimelinePlanningPause` hiện nút **Execute**.
- Session vẫn ở phase **Planning** suốt lúc pause → player **kéo-thả** skill lên lane (click/W/A/D chỉ highlight, không gán).
- **Resume tự động:** mỗi lần gán skill (`HandleActionAssigned`) kiểm tra `AllPartyUnitsHaveActions()` — khi mọi unit còn sống đã có action trong agenda → `ResumeFromPlanningPause`.
- **Resume tay:** bấm **Execute** → `ResumeFromPlanningPause` → nhạc + scan tiếp tục.

### Round segment (2 timeline phase)

- `TimelineConstants.RoundPhaseCount = 2` → scan dừng sau beat **31** (`GetRoundEndBeatExclusive()` = 32).
- `BeatTimelineUIView.FinishRoundSegment` → `CombatController.OnRoundSegmentComplete` → clear agenda player, hiện nút **Execute** cho vòng planning tiếp theo (không intro-pause).

### Luật ra đòn của quái

- Quái **chỉ được đặt telegraph từ beat thứ 3** trở đi — `TimelineConstants.EnemyFirstAttackBeat = 2`.
- Mỗi đòn = **2 pha:** S1 wind-up (ô đỏ nhạt, `IsWindupOnly`) + S impact (ô đỏ đậm); damage resolve chỉ tại **impact beat**.
- `SimpleEnemyAI.PlanTelegraphsForPhase` chọn beat impact sao cho S1 nằm trong phase.

---

## 2. Timeline UI

### Layout (character lanes)

Timeline giữ **một hàng cột beat duy nhất**. Trên đó overlay **N dòng kẻ (lane) ngang** — mỗi lane cho **một thành viên party còn sống** (lấy từ `Grid.PlayerUnits`, tối đa 4). Lane 0 ở trên cùng, cách đều theo chiều cao viewport, tô màu + nhãn theo `PlaceholderColor`/`DisplayName` của unit.

| Lớp | Nội dung |
|-----|----------|
| Cột beat (chung) | Boss telegraph notes — tag nguồn: **CORE** · **MICRO** · **EYE** |
| Lane 0..N-1 | 1 dòng kẻ / party member; skill của unit hiển thị bằng **marker** đặt tại `(beat x, lane y)` |

- **Player action** không còn vẽ trong ô beat nữa → render thành **`TimelineLaneMarkerView`** trên lane của unit (chip tròn: nền = màu unit, viền glow theo `ActionGlowType`, nhãn tên skill). Marker mới có animation "bay vào lane" (scale + trượt lên ~0.18s).
- **Boss notes** vẫn nằm trên hàng beat chung như cũ (không có lane riêng).
- **Cùng beat, khác lane** → hợp lệ (nhiều unit hành động cùng beat).
- Nốt **CORE** vs **MICRO** / **EYE** dùng chung hàng beat nhưng **icon + viền màu** khác nhau (xem §5–§6).

### Đặt skill — kéo-thả + highlight phím

1. **Kéo-thả (gán duy nhất):** kéo từ `SkillSlot_{Top,Left,Right}` → preview footprint S1/S/S2 trên lane → thả → `TryAssignPlayerAction` (chặn overlap qua `SkillFootprintUtil`).
2. **Click / W·A·D:** highlight ô radial — **không** gán timeline, **không** slow-mo.

**Skill panel Hierarchy:** `SkillPanelUI/Radial/SkillSlot_*` — scene-first (`Setup Skill Panel in Hierarchy`).

**Bỏ:** token giữa · slow-mo panel · arm skill khi scan bar chạy.

Lane số lượng đồng bộ động theo party sống (rebuild khi có unit chết/đổi đội hình); marker chỉ animate cho entry mới, refresh/scroll không animate lại.

### Impact Line (thanh đỏ)

- Nốt **CORE** chạm impact line → dmg HP (Space guard giảm)
- Nốt **MICRO** / **EYE** chạm impact line → **pressure only** (§6), không guard được effect

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

| Char | HB (Lv15 optimal) | W |
|------|-------------------|---|
| Ren | 167 | **8** |
| Coda | 147 | **8** |
| Charlotte | 127 | **7** |

### Planning flow

**Lần đầu (trước Execute 1):** thứ tự HB cao → thấp, mỗi char **1 skill** → Execute.

**Sau Execute:** async — hết S2 char nào → pause → Planning char đó (không chờ party).

**Planning latency** (beat chờ trước khi mở UI sau S2):

```
Latency = max(0, 2 − ⌊HB / 85⌋)
```

| Char | HB | Latency |
|------|-----|---------|
| Ren | 167 | 1 beat |
| Coda | 147 | 1 beat |
| Charlotte | 127 | 1 beat |

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
- Chỉ áp dụng cho nốt **CORE** (Thân). Nốt **MICRO** / **EYE** spawn theo lịch riêng (§6)

---

## 5. Boss anatomy — Thân · Micro · Mắt

The Pulse = **3 target** trên cùng encounter (3 unit logic, 1 boss row telegraph).

| Target | Tên | Vai trò combat | HP (Lv18) |
|--------|-----|----------------|-----------|
| **CORE** | **Thân** | Nốt gây **dmg HP party** khi leak · pool HP chính | **1680** |
| **MICRO** | **Micro** | Nốt **không dmg HP** · leak → **buff boss** hoặc bị counter → **dmg Micro + gỡ buff** | **280** |
| **EYE** | **Mắt** | Nốt **không dmg HP** · leak → **debuff party** hoặc bị counter → **dmg Mắt + gỡ debuff** | **200** |

```
Tổng pool tuỳ chọn = 2160 (core-only sim) + 480 mini = 2640 nếu hạ cả 3
Win condition mặc định: CORE HP = 0 (Micro/Mắt chết = áp lực giảm, không bắt buộc)
```

| Target | Element | EN | Ghi chú |
|--------|---------|-----|---------|
| CORE | Rhythm | 20 | Harmony ×1.5 Coda · ×0.5 Ren @ Melody |
| MICRO | Harmony | 12 | Counter dmg dùng `MiniDmg` (§7) |
| EYE | Melody | 10 | Counter dmg dùng `MiniDmg` (§7) |

**Telegraph:** mỗi nốt row 4 có badge `CORE` (đỏ đậm) · `◈ MICRO` (tím nhạt) · `◇ EYE` (xanh lạnh). HB intel: Ren thấy tag nguồn · Coda thấy tag + effect icon · Charlotte chỉ “có mini hay không”.

→ Xem `illustrations/combat-boss-3-target-timeline.svg`

---

## 6. Mini pressure notes (Micro · Mắt)

> **Khác CORE:** leak mini **không trừ HP party** · **không** dùng Space guard cho effect mini (guard vẫn chỉ giảm dmg nốt CORE).

### Resolve @ impact line

| Kết quả | CORE note | MICRO / EYE note |
|---------|-----------|------------------|
| **Perfect counter** | −1 hit + dmg CORE (§7) | **MiniDmg** lên pool Micro/Mắt + **xóa 1 stack** buff/debuff tương ứng |
| **Leak (không counter)** | Dmg HP party + leak mult | **Không dmg HP** · áp dụng **pressure effect** (bảng dưới) |
| Late / Early counter | Không −hit · dmg giảm | Không MiniDmg · **vẫn leak pressure** như fail |

### Pressure effects (The Pulse Lv18)

| Nguồn | Leak (fail) | Counter (Perfect) |
|-------|-------------|-------------------|
| **◈ MICRO** | Boss **`Resonance`** +1 stack (max 3): STR boss +6%/stack cho nốt CORE kế | −1 `Resonance` stack (nếu có) · MiniDmg |
| **◇ EYE** | Party **`Dissonance`** +1 stack (max 3): reactive Guard −12%/stack (chỉ nốt CORE) | −1 `Dissonance` trên target bị aim · MiniDmg |

- Spawn mini: **không** chiếm slot màu Tím/Xanh/Đỏ của CORE — mỗi mini có **hit = 1** (chỉ Đỏ logic), không degrade
- Min gap riêng: Micro mỗi **8–12 beat** · Mắt mỗi **10–14 beat** (phase Mid+ tăng tần suất)
- Micro chết → hết spawn Micro + clear `Resonance` · Mắt chết → hết spawn Eye + clear `Dissonance` party

---

## 7. Element triangle & Ren Cycle Shift

```
Rhythm → Melody → Harmony → Rhythm
Advantage ×1.5    Disadvantage ×0.5    Neutral ×1.0
```

**Harmony chỉ áp dmg lên CORE** (Thân Rhythm). MiniDmg **bỏ qua** Harmony.

### Ren — Cycle Shift (chỉ **Strike** basic)

| | |
|--|--|
| **Base element** | Melody (Ren cố định identity) |
| **Active element** | Hệ **đang dùng** cho mọi skill Ren — bắt đầu Melody |
| **Trigger** | Mỗi lần **Strike** resolve xong (hết S2) → xoay **Melody → Rhythm → Harmony → Melody** |

```
Melody ──Strike──► Rhythm ──Strike──► Harmony ──Strike──► Melody …
         Crosscut / Finale dùng Active element lúc bắt đầu S1 — không xoay
```

**Arc 1 vs The Pulse (Rhythm CORE):**

| Active | vs CORE | Gợi ý |
|--------|---------|-------|
| Melody | ×0.5 | Mở trận / setup |
| Rhythm | ×1.0 | 1 basic để lên neutral |
| Harmony | ×1.5 | Dump Crosscut / Finale |

UI: icon hệ nhỏ cạnh portrait Ren + pulse khi Strike xoay.

Charlotte (Rhythm) vs CORE luôn ×1.0 · Coda (Harmony) vs CORE ×1.5 · không xoay.

---

## 8. Counter timing (skill)

So sánh beat **S active** vs beat **nốt boss**:

| Δ beat | Tên | Skill dmg | Triệt tiêu hit nốt |
|--------|-----|-----------|---------------------|
| −2, −1 | Early | ×0.5 | ✗ |
| **0** | **Perfect** | Full | **✓ −1 hit** |
| +1, +2 | Late | ×0.25 | ✗ |
| ngoài ±2 | Off-beat | ×0.01 | ✗ |

Công thức dmg giữ nguyên:

```
Raw        = Random(tier) × AttackPower × 10
CoreFinal  = Raw × 1/(4×√EN_core) × BeatTiming × Harmony(active, CORE) × CritMult
MiniDmg    = Raw × 1/(4×√EN_mini) × BeatTiming × 0.85 × CritMult   // không Harmony
```

| Target | BeatTiming | Harmony |
|--------|------------|---------|
| CORE | §8 bảng timing | Ren Active element · Charlotte Rhythm · Coda Harmony |
| MICRO / EYE | Perfect = full · Late/Early = ×0.5 · Off = ×0.01 | **Bỏ qua** |

---

## 9. Reactive Guard (Space)

**Không còn skill Guard.** Player guard chủ động khi nốt còn sót chạy về impact line.

| Timing (±1 beat vs nốt) | Giảm dmg |
|---------------------------|----------|
| Early / Late | **−15%** |
| Perfect (±0) | **−50%** |
| Off-beat | **0%** |

```
DmgTaken = BossRaw × (1 − GuardReduction − DissonancePenalty) × EnduranceFactor
DissonancePenalty = 0.12 × DissonanceStacks   // Eye leak, max 3
```

EN vẫn scale reduction qua `EnduranceFactor`.

---

## 10. Heartbeat (HB) — 4 vai trò

| # | Vai trò | Cơ chế |
|---|---------|--------|
| 1 | Thứ tự Planning | HB cao → trước |
| 2 | Beat bar width **W** | Công thức §3 |
| 3 | **Telegraph intel** | Ren: màu + hit + beat + **note tag CORE/MICRO/EYE** · Coda: tag + effect icon · Charlotte: mini presence |
| 4 | **Planning latency** | Công thức §3 |
| — | UI assist khi kéo skill | Ren: highlight Perfect · Coda: ±1 · Charlotte: presence only |

**Bỏ:** Phase AV · Base AV priority · skill Guard · HB giảm S2 beat

---

## 11. Kit skill (3 skill / nhân vật)

> Chi tiết dmg số: [SKILL_KIT.md](./SKILL_KIT.md)

### Ren — DPS · Cycle Shift

| # | Tên | S1–S–S2 | Effect |
|---|-----|---------|--------|
| 1 | Strike | 1–1–1 | Basic dmg · **+1 bước Cycle Shift** |
| 2 | Crosscut | 2–2–2 | 2 beat counter · dmg theo **Active element** |
| 3 | Finale | 2–3–3 | 3 beat counter · dmg theo **Active element** |

### Charlotte — Tank · Rhythm

| # | Tên | S1–S–S2 | Effect |
|---|-----|---------|--------|
| 1 | Ram | 1–1–1 | Basic dmg |
| 2 | Anchor | 2–2–2 | Delay boss note +2 beat |
| 3 | Bulwark | 2–2–3 | Shield 65 + counter |

### Coda — Support · Harmony

| # | Tên | S1–S–S2 | Effect |
|---|-----|---------|--------|
| 1 | Pulse | 1–1–1 | Ma dmg |
| 2 | Mend | 2–1–2 | Heal ally |
| 3 | Encore | 1–1–1 | Ally S2 −1 beat |

---

## 12. Walkthrough — beat 9–11 (+ mini @ 12)

**Spawn:** beat 9 Tím CORE(3) · beat 10 Đỏ CORE(1) · beat 11 Đỏ CORE(1) · beat 12 **◈ MICRO**(1)

**Ren** mở trận Active = Melody (×0.5 vs CORE). **Strike** @ 7–9 → dmg thấp · sau S2 Active = **Rhythm**.

**Ren Crosscut** @ 8–13 (Active Rhythm ×1.0):

| Beat | Kết quả |
|------|---------|
| 9 | Tím → **Xanh** (2 hit còn) |
| 10 | Đỏ → **Cancel** |
| 11 | Đỏ → **Cancel** |

**Coda Basic** S1:1 S:1 S2:1 @ 8–10:

| Beat | Kết quả |
|------|---------|
| 9 | Xanh +1 hit → **Đỏ** (1 hit còn) |

**Beat 12 MICRO:** Perfect Crosscut hit → MiniDmg + **`Resonance` không lên**. Nếu miss → **`Resonance` +1** (boss STR +6%), **0 dmg HP**.

**Hết skill** → Space guard nốt Đỏ CORE @ beat 9 khi chạm impact line.

→ Xem `illustrations/combat-note-walkthrough-example.png`

---

## 13. Illustrations

| File | Nội dung |
|------|----------|
| `combat-boss-3-target-timeline.svg` | CORE / MICRO / EYE trên row 4 · resolve @ impact |
| `combat-timeline-3-rows.png` | Timeline 3 row + boss notes |
| `combat-planning-execute-music.png` | Planning vs Execute + dual BGM |
| `combat-hb-roles-comparison.png` | HB 4 vai trò |
| `combat-counter-guard-timing.png` | Counter ±2 · Guard ±1 · degrade |
| `combat-note-walkthrough-example.png` | Ví dụ beat 9–11 |
| `combat-same-beat-stacking.png` | Chồng 3 hit cùng beat |

---

## 14. Map sang code (delta)

| Thiết kế | Trạng thái code | Ghi chú |
|----------|-----------------|---------|
| Intro-pause + Deploy/Continue | ✅ MVP | `PlanningPauseLocalBeat`, `CombatController`, `PausePlayback` |
| Character lanes + drag skill | ✅ MVP | `BeatTimelineUIView`, `TimelineLaneMarkerView` |
| Footprint S1/S/S2 UI | ✅ MVP | `RefreshFootprintDots`, `SkillDefinitionSO` fields |
| Beat map sync nhạc | ✅ MVP | Scene wired + `CombatMusicSceneSetup` |
| Enemy attack từ beat 3 | ✅ MVP | `EnemyFirstAttackBeat = 2` |
| Scene-first UI sizing | ✅ MVP | `RectSizeUtil` |
| Skill tên đúng kit | ✅ MVP | `Resources/Skills/*`, `SkillUiNames` |
| Boss 3 target (Core/Micro/Eye) | 🔲 P0 | Single boss HP |
| Note tag CORE / MICRO / EYE | 🔲 P0 | Single telegraph |
| Ren Cycle Shift | 🔲 P0 | Fixed element |
| Mini pressure (no HP leak) | 🔲 P0 | N/A |
| Note HP degrade (tím/xanh/đỏ) | 🔲 P0 | 1-hit telegraph |
| Enforce footprint overlap | ✅ MVP | `SkillFootprintUtil`, `CanAssignAction(unit, skill, beat)` |
| Round segment 2 phase | ✅ MVP | `FinishRoundSegment`, `RoundPhaseCount = 2` |
| Skill panel scene-first | ✅ MVP | `SkillPanelUI/Radial/SkillSlot_*` |
| Enemy 2-phase telegraph | ✅ MVP | `SimpleEnemyAI`, `EnemyTelegraph.IsWindupOnly` |
| Bỏ PhaseAvTracker | 🟡 legacy | Code còn stub |
| `Resonance` / `Dissonance` stacks | 🔲 P1 | N/A |
| Async per-char planning | 🔲 P1 | Batch planning |

---

## Changelog

| Ngày | Nội dung |
|------|----------|
| 2026-07-05 | Audit + fix: scene wire beat map/Deploy/prune null unitViews; bỏ overlay Start re-bind; callback pause thay FindAnyObjectByType; `RefreshLaneMarkers` thay RefreshAll khi gán skill; verify script cập nhật |
| 2026-07-03 | Fix: intro-pause `PlanningPauseLocalBeat=0.5`; footprint refresh lúc pause; nhãn Deploy/Continue ép runtime |
| 2026-07-03 | Fix: pause kiểm tra TRƯỚC `FireScanBeat` → dừng ngay trước beat 1 (beat 1 chưa fire, trước cả beat player sắp set); footprint refresh ngay lúc `EnterPlanningPause` để điểm tròn hiện đúng khi Continue xuất hiện |
| 2026-07-03 | Fix: pause-beat chuyển thành hằng số `PlanningPauseAfterBeat=1` (scan qua beat 0 rồi mới dừng, không dừng tức thì); nhãn nút do `CombatController` ép runtime (Deploy/Continue) chống scene serialize cũ |
| 2026-07-03 | Intro-pause: scan dừng sau khi qua beat đầu tiên (pause tại beat 1) cho player set up skill; nhạc `PausePlayback/ResumePlayback`; auto-resume khi cả đội đã xếp skill hoặc bấm Continue |
| 2026-07-03 | Nút dàn trận đổi chữ thành **Deploy** → sau intro-pause đổi thành **Continue** |
| 2026-07-03 | Footprint skill: mỗi skill đặt lên lane vẽ **S1 tròn xám · S tròn/chip màu · S2 tròn xám** (`RefreshFootprintDots`); bỏ điểm tròn trống |
| 2026-07-03 | Đổi tên kỹ năng theo SKILL_KIT (Crosscut/Finale · Anchor/Bulwark · Mend/Encore) + set footprint S1-S-S2 vào asset; UI hiện tên thật |
| 2026-07-03 | Luật: quái chỉ ra đòn từ **beat thứ 3** (`TimelineConstants.EnemyFirstAttackBeat = 2`) |
| 2026-06-30 | Illustration `combat-boss-3-target-timeline.svg` |
| 2026-06-30 | Boss 3 target · Mini pressure · Ren Cycle Shift · CoreFinal vs MiniDmg |
| 2026-06-30 | Tạo doc — Planning/Execute loop, boss notes, HB roles, kit 3 skill, illustrations |

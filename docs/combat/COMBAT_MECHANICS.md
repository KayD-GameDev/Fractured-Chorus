# Combat Mechanics — Planning / Execute Loop

> **Trạng thái:** Runtime SoT sync (2026-07-16) · kit Prep Setup→Payoff · Phase AV legacy retained in code  
> **Kit detail:** [SKILL_KIT.md](./SKILL_KIT.md)  
> **Tham chiếu:** Caligula Effect 2 · nhạc Eternal Spark (Cadence Remix)  
> **Illustrations:** `docs/combat/illustrations/`

---

## 1. Vòng lặp combat

```
Dàn trận (kéo unit vào ô) — [Deploy] hiện ngay, chưa có nhạc
  → bấm Deploy → nhạc + timeline sync → intro-pause sau beat 6
  → [Execute] hiện → gán skill lên lane (planning từ beat 7)
  → bấm Execute → chạy 2 phase rồi dừng tại vạch trắng
  → [Execute] block kế — divider giữ tại scan bar, không nhảy timeline
  → lặp đến hết trận
```

| Giai đoạn | Timeline | Nhạc |
|-----------|----------|------|
| **Dàn trận** | Pause tại beat 0 | Chưa phát |
| **Deploy → intro-pause** | Sync nhạc, pause @ beat **6** | Phát → pause tại chỗ |
| **Execute (skill planning)** | Pause · horizon từ beat **7** | Pause tại chỗ |
| **Execute (chạy segment)** | Chạy sync beat | Phát tiếp |

- **Bỏ:** cycle cố định · skill Guard trên kit
- **Phase AV:** *legacy retained* — `PhaseAvTracker` vẫn gate budget **150 / 100** khi gán skill; UI `AvLabel` có thể ẩn
- **Giữ:** Nút **Deploy** (dàn trận) → **Execute** (sau intro-pause và mỗi round segment). Nhãn do `CombatController` ép runtime.

### Intro-pause (sau Deploy — gán skill)

- Vào scene: **Deploy** hiện ngay, player dàn trận, **không** phát nhạc.
- Bấm **Deploy** → `PlayBossMusic` + timeline sync từ beat 0.
- Pause sau beat index **`IntroPlanningPauseAfterBeatIndex = 6`** → planning horizon / Execute từ beat **7** (`IntroExecuteStartBeatIndex`).
- Nhãn nút: **Execute** (`CombatController` ép runtime). **Không** auto-resume khi đủ skill.
- Bấm **Execute** → `ResumePlayback` + scan tiếp.

### ScanBar anchor (segment / phase)

- ScanBar **cố định** trong viewport; content scroll (`ApplyScrollVisual` không sweep scan bar).
- Hết 2 phase: **phase divider** (+2px) căn ScanBar (`GetPhaseDividerContentPx`); viewport lộ ~1 beat phase trước.
- Block kế: `continueFromHold` — divider = điểm bắt đầu segment mới, không nhảy timeline.
- Nhạc chuyển segment: `PlaySegmentTransitionMusic` (stub, asset sau).

### Round segment (2 timeline phase liên tục)

- `TimelineConstants.RoundPhaseCount = 2` → mỗi block chạy **32 beat** (phase 1 + phase 2), rồi block kế (phase 3–4, …) **không** reset scroll về đầu.
- `CombatSession.RoundSegmentIndex`: 0 = beat 0–31, 1 = beat 32–63, …
- Scan bar fire **absolute beat** (`_segmentStartBeat + local offset`); dừng khi chạm **vạch trắng thứ 2** của block (sau beat 31, 63, …).
- `BeatTimelineUIView.FinishRoundSegment` → `HoldAtRoundEnd` (divider tại scan bar) → `RefreshTelegraphsAndSlots` (không rebuild layout) → **Execute** block kế bắt đầu tại cùng px (`continueFromHold`).
- Hết timeline (`segmentStart >= TotalBeats`) → không hiện Execute nữa.

### Luật ra đòn của quái

- Segment 0 (intro): min impact ≥ **`IntroEnemySpawnZoneStartBeat = 10`** (`GetMinEnemyImpactBeat`). Các phase sau: phase start + buffer (`EnemySpawnBufferBeatsAfterHorizon`).
- **Mỗi timeline phase (16 beat):** mỗi quái còn sống đặt `telegraphAttacksPerPhase` impact (preset, mặc định 1; Boss/Elite chỉnh trên asset).
- Plan **một lần** khi Deploy / khi vào planning sau block (`PrepareTelegraphsForCurrentSegment`, `EndRoundSegment` pre-plan) — **không** random lại khi scan qua beat đầu phase.
- Quái chết → xóa ngay telegraph của unit đó từ phase hiện tại tới hết block (không re-roll đòn quái còn sống).
- Damage resolve tại **impact beat**; player vẫn thấy footprint S1/S/S2 khi kéo skill của mình.
- `SimpleEnemyAI.PlanTelegraphsForPhase` chọn beat impact trong 16 beat của phase.

### Block (Space — thanh chắn)

- **Không** còn skill Guard trên kit / asset (`*_guard` đã xóa). Kit player = **3 skill** (Basic / Skill / Ult) trên radial W/A/D.
- Trong lúc scan: **Space (edge)** đặt **thanh chắn** snap **integer beat** (1 beat = tối đa 1 barrier).
- Timing barrier vs beat đòn quái `E`: **OnBeat** (cùng ô) giảm 68% dmg · **Early** (E−1) 25% · **Late** (E+1) 10% · khác 0%.
- Chỉ giảm dmg khi: không có counter trên beat `E`, có standing footprint chạm `E`, và phase chưa vượt **7** block hiệu lực.
- **Không đỡ / block không hợp lệ:** đòn quái đánh **1** nhân vật — ưu tiên standing overlap @ `E` có **BaseAv cao nhất**; không có standing → **BaseAv cao nhất** toàn party còn sống (nhận dmg thay cho đội).

### Counter player

- Active beat (S) trùng beat telegraph quái → counter **đúng quái đó** (không pick cột trước).
- Boss note **Tím/Xanh/Đỏ** (`HitsRequired` 3/2/1): đủ skill counter active cùng beat → **hủy đòn** (không dmg).
- **Portrait** trên beat slot hiển thị màu tier: Grunt luôn **Đỏ**; Elite **70% Đỏ / 30% Xanh**; Boss roll đủ 3 màu theo phase.

### Resolve đòn quái

- Counter đủ → cancel · không counter → block (nếu hợp lệ) → dmg.
- Target: standing overlap trên beat `E` → **BaseAv cao nhất** trong nhóm; không có standing → **BaseAv cao nhất** party.
- Cùng beat nhiều skill player: **không** sort theo ActionPriority — resolve theo thứ tự agenda.

### Planning UX

- Gán hết skill **không** auto-resume — bắt buộc bấm **Execute**.
- Kéo marker skill → **xóa ngay** khi bắt đầu kéo (dots footprint biến mất cùng lúc) + refund AV.
- **Skill radial (W/A/D):** chỉ hiện phím + tên skill — **không** hiện cost AV trên nút.
- **Bỏ Cycle header** trên timeline (`Cycle remaining/budget` per phase) — budget skill giữ theo round segment, không reset khi scan qua phase divider.
- **Hex floor (ô vị trí):** chỉ hiện hex **Player** lúc `AllowPlayerReposition` (Deploy / dàn trận). Hex **Enemy** luôn ẩn. Sau Deploy (`LockPlayerReposition`) ẩn cả hai — `CombatController.ApplySlotFloorVisibilityForCurrentPhase` → `BoardDragController.SetSlotFloorsVisible` / `GridCellMarker.SetFloorVisible`.
- **Nút Deploy / Execute:** `CombatExecuteOverlayUIView.ApplyAlphaHitTest` — `alphaHitTestMinimumThreshold = 0.1` **chỉ khi** `texture.isReadable` (tránh Console error); nếu chưa Readable thì tạm full-rect. Sprites `combat_btn_deploy_v1` / `combat_btn_execute_v1` cần Read/Write + Uncompressed — menu **Fractured Chorus → Ensure Combat Button Sprites Readable** (`CombatButtonSpriteImportSettings`).

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

1. **Kéo-thả:** kéo từ `SkillSlot_{Top,Left,Right}` → preview footprint S1/S/S2 trên lane → thả → `TryAssignPlayerAction` (chặn overlap qua `SkillFootprintUtil`).
2. **Click:** highlight ô radial.
3. **W / A / D:** gắn skill ô tương ứng vào chuột (ghost bám con trỏ); có thể **đổi W/A/D** khi đang kéo → thả lên lane để gán.

**Skill panel Hierarchy:** `SkillPanelUI` tròn (220×220, `UiCircleSpriteUtil.Circle`) + `Radial/SkillSlot_*` tròn, label **20px đen** — scene-first (`Setup Skill Panel in Hierarchy`).

**Bỏ:** token giữa · slow-mo panel · arm skill khi scan bar chạy.

Lane số lượng đồng bộ động theo party sống (rebuild khi có unit chết/đổi đội hình); marker chỉ animate cho entry mới, refresh/scroll không animate lại.

### Impact Line (thanh đỏ)

- Nốt quái chạm impact line → dmg HP (counter hủy, hoặc block barrier giảm dmg)

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

### Beat placement tối thiểu (S1)

- `placementBeat` = beat bắt đầu phase **S (Active)**.
- Beat sớm nhất: `GetMinimumPlacementBeat(skill) = standingBeatsBefore` — ví dụ `standingBeatsBefore = 1` → không đặt tại beat 0, sớm nhất beat index **1** (beat thứ 2).
- `SkillFootprintUtil.CanPlace` + `CanAssignAction` từ chối beat sớm hơn; preview drag hiện invalid.

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

**Không còn skill Guard.** Space đặt **barrier 1 beat** (`BlockBarrierTracker`).

| Timing vs impact `E` | Giảm dmg (`BlockTiming.GetDamageReduction`) |
|----------------------|---------------------------------------------|
| OnBeat (cùng ô) | **68%** |
| Early (`E−1`) | **25%** |
| Late (`E+1`) | **10%** |
| OffBeat | **0%** |

Chỉ giảm dmg khi: không counter trên `E`, có standing footprint chạm `E`, và chưa vượt cap block hiệu lực trong phase (xem §1 Block).

**Không đỡ / không hợp lệ:** target theo `CombatTargetPicker` (BaseAv cao nhất trong standing / party).

---

## 10. Heartbeat (HB) — 4 vai trò

| # | Vai trò | Cơ chế |
|---|---------|--------|
| 1 | Thứ tự Planning | HB cao → trước |
| 2 | Beat bar width **W** | Công thức §3 |
| 3 | **Telegraph intel** | Ren: màu + hit + beat + **note tag CORE/MICRO/EYE** · Coda: tag + effect icon · Charlotte: mini presence |
| 4 | **Planning latency** | Công thức §3 |
| — | UI assist khi kéo skill | Ren: highlight Perfect · Coda: ±1 · Charlotte: presence only |

**Bỏ:** Base AV priority (sort) · skill Guard · HB giảm S2 beat  
**Giữ (legacy):** Phase AV budget gate khi gán skill (150/100)

---

## 11. Kit skill (3 skill / nhân vật)

> Chi tiết dmg số + Prep laws: [SKILL_KIT.md](./SKILL_KIT.md)

### Prep — Setup → Payoff (runtime)

```
Empty S (Skill/Ult) → +1 Prep / beat (cap 3)
S ∩ note            → Counter; không farm Prep
Basic               → không đụng Prep
Empower Skill @ ≥1 / Ult @ ≥2 → tiêu Prep; Prep 0 vẫn cast base
```

- Anchor Delay / Encore ReduceS2 apply **lúc đặt (Planning)** — xem `CombatSession.ApplyPlanningUtilityEffects`.
- UI: `PrepPipsView` · Encore buff icon · note sprites qua `TimelineNoteVisualCatalog` (`Resources/UI/Combat/**`).

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
| Intro-pause + Deploy/Execute | ✅ MVP | `TryEnterIntroPauseAfterBeat0`, `SnapScrollToAnchor` |
| ScanBar anchor scroll | ✅ MVP | `GetBeatEndContentPx`, `GetPhaseDividerContentPx`, fixed ScanBar |
| Segment transition music | 🔲 stub | `PlaySegmentTransitionMusic` (asset TBD) |
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
| Round segment 2 phase liên tục | ✅ MVP | `FinishRoundSegment`, `RoundSegmentIndex`, absolute beat scroll |
| Skill panel circular scene-only | ✅ MVP | `ApplyCircularPanelStyle`, `UpgradeRadialSlotStyle` |
| W/A/D swap while keyboard drag | ✅ MVP | `TryGetDirectionKeyPressedThisFrame` |
| Hold scroll at round end | ✅ MVP | `GetSegmentDividerScrollPx`, `HoldAtRoundEnd` |
| Enemy telegraph per phase | ✅ MVP | `PlanTelegraphsForPhase`, `telegraphAttacksPerPhase` |
| Block barriers (Space) | ✅ MVP | `BlockBarrierTracker`, `BlockInputController` |
| Counter targeting | ✅ MVP | `CombatCounterResolver` |
| Boss note tiers Tím/Xanh/Đỏ | ✅ MVP | `BossNoteTier`, `BossTelegraphPlanner`, Knight 3/phase |
| Portrait tier color on beat slot | ✅ MVP | Fallback tint; sprites ưu tiên catalog |
| Note tier / ghost / cover sprites | ✅ MVP | `TimelineNoteVisualCatalog` ← `Resources/UI/Combat/Timeline/` |
| Prep channel + pips + empower | ✅ MVP | `CombatUnit.Prep`, `PrepPipsView`, `TryResolveEmpowerAtBeat` |
| Shield absorb | ✅ MVP | `CombatUnit.Shield` · Bulwark / Mend overheal |
| DelayBossNote @ Planning | ✅ MVP | `DelayImpactTelegraphsAfterBeat` · slide VFX |
| ReduceS2 + buff icon | ✅ MVP | `PendingReduceS2` · `PartyMemberCardView` BuffReduceS2 |
| Counter presentation feel | ✅ MVP | `CounterPresentationDriver` · Perfect chip · MULTI |
| Elite note roll 70/30 | ✅ MVP | `BossTelegraphPlanner.RollEliteNoteTier` |
| Pre-deploy intro scroll | (removed) | Intro on Deploy; anchor end beat 0 at ScanBar |
| Segment handoff no jump | ✅ MVP | `continueFromHold`, `RefreshTelegraphsAndSlots` |
| Enemy target highest BaseAv | ✅ MVP | `PickHighestBaseAvAlive` |
| Bỏ PhaseAvTracker cycle UI | ✅ MVP | Ẩn `AvLabel`; bỏ `SyncToTimelinePhase` |
| `Resonance` / `Dissonance` stacks | 🔲 P1 | N/A |
| Async per-char planning | 🔲 P1 | Batch planning |
| Empty-beat skill catalog (#4) | 🔲 backlog | Beyond Prep channel |

---

## Changelog

| Ngày | Nội dung |
|------|----------|
| 2026-07-16 | Runtime SoT sync: intro beat 6 · Guard 68/25/10 · Phase AV legacy · enemy zone beat 10 |
| 2026-07-16 | Map Prep/Shield/Delay/Encore/note catalog/counter feel; restore `Resources/UI` load path |
| 2026-07-16 | Xóa `*_guard` khỏi preset Ren/Tank/Mage; block = Space; target dmg = BaseAv cao nhất |
| 2026-07-16 | Fix alpha hit-test: guard `isReadable`; editor ép Read/Write + Uncompressed cho button sprites; docs sync |
| 2026-07-15 | Ẩn hex floor Enemy luôn; Player floor chỉ lúc Deploy; nút Deploy/Execute click theo alpha sprite (`alphaHitTestMinimumThreshold`) |
| 2026-07-06 | Combat UX: enemy target BaseAv **cao nhất**; bỏ sort player theo AV; pre-deploy scroll trước Deploy; drag-remove skill ngay OnBeginDrag; Portrait tier màu (Grunt đỏ, Elite 70/30, Boss full); segment handoff `continueFromHold` |
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

# Persona-like Calendar System — Design Spec

**Date:** 2026-07-11 (rev. story calendar 2026-07-11)  
**Status:** Approved — story timeline integrated  
**Game:** Fractured Chorus (Unity 6)

---

## 1. Summary

Hub-calendar meta layer tại campus (Persona-style): 30 ngày/arc, buổi sáng quiz auto, 2 slot hoạt động (Day + Evening), social stat themed, Bond qua **Echo Keys**, dungeon run là evening activity. Arc 1 cap Bond rank; part-time jobs và classroom quiz bổ sung stat.

---

## 2. Design Decisions (locked)

| # | Decision | Choice |
|---|----------|--------|
| 1 | Calendar vs run map | **A — Hub calendar**; run map = dungeon activity |
| 2 | Social stats | **C — Hybrid** themed names, Persona rank/threshold |
| 3 | Day structure | **2 slots** (Day + Evening) + **Morning auto** (không tiêu slot) |
| 4 | Arc length | **Tháng 9** (01/09 → 30/09); prologue 17/08 tách riêng |
| 5 | NPC scope MVP | Party + Ryo + Mei Lin + **Astra** (Pulse) |
| 6 | Bond framework | **Echo Keys** (12 Khóa Hồi âm) |

---

## 3. Scene Flow

```
MainMenu → PrologueVN → OpeningInvestigation → CampusHub (01/09)
                           │
         ┌─────────────────┼─────────────────┐
         ▼                 ▼                 ▼
   SocialEventScene   RunMapPrototype   Rest/Shop UI
   (VN segment)       (dungeon run)     (hub overlay)
         │                 │
         └────────→ advance slot/day ←──┘
```

> **Flow supersede (2026-07-12):** OpeningInvestigation plays **after** PrologueVN (not before). See `docs/superpowers/specs/2026-07-12-opening-investigation-vn-design.md`.

- **CampusHub** = meta hub scene dùng BG **Lumina City town map** (`lumina-city-town-map-bg_v1.png`); HIMA là 1 địa điểm trên map, không phải toàn cảnh hub
- Run map/combat không thay calendar — chỉ là activity tiêu tốn slot
- Quay hub sau mỗi activity; hết 2 slot → `AdvanceDay()`

**Scene catalog bổ sung:** `CampusHub` trong `RunMapSceneCatalog`.

---

## 4. Daily Loop

```
AdvanceDay()
  → MorningBeat (auto classroom quiz, không tiêu slot)
  → Day slot     (player chọn 1 activity)
  → Evening slot (player chọn 1 activity)
  → AdvanceDay() nếu cả 2 slot đã dùng
```

| Beat | Tiêu slot? | Mô tả |
|------|------------|-------|
| Morning — Lớp học | Không | Thầy hỏi, đúng → +stat EXP |
| Day | Có (1) | Activity tự chọn |
| Evening | Có (1) | Activity tự chọn |

---

## 5. Social Stats (Hybrid)

| Stat | Persona tương đương | Mở Bond / activity |
|------|---------------------|-------------------|
| **Resonance** | Charm | Melody, Pulse; flower shop |
| **Cadence** | Knowledge | Harmony, Measure; study, quiz |
| **Pulse** | Guts | Bass, Crescendo; practice |
| **Harmony** | Proficiency | Harmony, Rest; part-time |
| **Rhythm** | Courage | Dissonance, Fermata; rest |

- Rank **1–10** per stat; activity cho **stat EXP**, không cộng thẳng rank
- Rank threshold mặc định: 15 → 25 → … → 120 (tune in data)

### Stat EXP từ activity (MVP)

| Activity | Stat EXP |
|----------|----------|
| Study (thư viện) | Cadence +8 |
| Practice / tiệm nhạc | Harmony +8 hoặc Pulse +8 |
| Cafe / giao lưu | Resonance +8 |
| Rest | Rhythm +5 |
| CV tiện lợi | Harmony +10, Cadence +4 |
| Shop hoa | Resonance +10, Harmony +4 |
| Classroom quiz (đúng) | Theo chủ đề câu +8 |

---

## 6. Echo Keys — Bond System

12 Echo Keys (voice archetypes trong dàn hợp xướng bị fracture). MVP dùng **6**; 6 còn lại reserved.

| NPC | Echo Key | Stat gate chính |
|-----|----------|-----------------|
| Ren | **Melody** | Resonance, Pulse |
| Charlotte | **Bass** | Pulse, Rhythm |
| Coda | **Harmony** | Cadence, Harmony |
| Ryo | **Measure** | Cadence, Rhythm |
| Mei Lin | **Dissonance** | Cadence, Resonance |
| **Astra** | **Pulse** | Resonance, Harmony |

**Reserved keys (Arc 2+):** Rest, Overtone, Static, Cadence, Crescendo, Fermata.

### Bond mechanics

- Rank **1–10** per NPC
- Hangout → +Bond EXP (base 10; bonus choice đúng trong VN)
- Rank up cần: Bond EXP threshold + `minStats` + optional `StoryFlag`
- Rank up → `SocialEventSO` scene + optional `BondPassiveSO` (combat buff)

### Bond lock — Arc 1 cap

Bond bị **khóa ở mốc rank** trong Arc 1; mở dần qua story flag / Arc 2.

| Bond | Cap Arc 1 | Mở khóa tiếp |
|------|-----------|--------------|
| Ren (Melody) | Rank 4 | `arc1_midpoint_cleared` (~ngày 15) |
| Charlotte (Bass) | Rank 3 | `dungeon_sector1_clear` |
| Coda (Harmony) | Rank 4 | `arc1_midpoint_cleared` |
| Ryo (Measure) | Rank 2 | Rank 3+ trong Arc 2 |
| Mei Lin (Dissonance) | Rank 2 | `investigation_started`; rank 3+ Arc 2 |
| Astra (Pulse) | Rank 5 | Tự nhiên trong Arc 1; hangout sau 02/09 |

Khi chạm cap: hangout vẫn +EXP nhưng không rank up; UI hiển thị khóa + hint *"Cần tiến thêm câu chuyện"*.

`BondManager.CanRankUp()` check `bondRank < GetArcCap(npcId, currentArc)`.

---

## 7. Activities

### Availability filter

```
Available = phase match
          ∧ slot chưa dùng (trừ Morning)
          ∧ stat/Bond/flag đủ
          ∧ NPC không busy (hangout)
          ∧ weekly schedule (part-time)
          ∧ không bị CalendarEvent khóa
```

### Activity table (MVP)

| Activity | Phase | Slot | Output |
|----------|-------|------|--------|
| Morning Quiz | Morning | 0 | +stat EXP (đúng) |
| Study | Day | 1 | +Cadence |
| Practice / tiệm nhạc | Day | 1 | +Harmony / Pulse |
| Hangout | Day / Evening | 1 | +Bond EXP |
| CV tiện lợi | Day / Evening | 1 | Harmony + Cadence; schedule 3 ngày/tuần |
| Shop hoa | Day | 1 | Resonance + Harmony; Thứ 4 + Thứ 7 |
| Shop | Day | 1 | Mua item (stub) |
| Dungeon Run | Evening | 1 | Load RunMapPrototype |
| Rest | Evening | 1 | +Rhythm nhỏ |
| Fixed Event | auto | 1 | Forced VN scene |

**Part-time:** `ActivityDefinitionSO.weeklySchedule` — không tăng Bond trực tiếp; có thể set flag (vd `part_time_ren_met` ngày 8).

### Morning Classroom Quiz

`ClassroomQuizSO`: 1 câu/ngày, pool 30 câu không lặp, 4 đáp án.

| Chủ đề | Stat thưởng (đúng) |
|--------|-------------------|
| Âm nhạc / lịch sử | Cadence +8 |
| Giao tiếp / văn học | Resonance +8 |
| Thể chất / biểu diễn | Pulse +8 |
| Logic / điều tra | Cadence +8 |

- Sai: không phạt (MVP)
- Flow: vào Day phase → quiz overlay → xong → Activity Picker

---

## 8. Event Scheduler

`CalendarEventScheduler` chạy on `AdvanceDay()` và khi vào phase.

| Priority | Behavior | Ví dụ |
|----------|----------|-------|
| **Forced** | Chiếm slot kế tiếp | Mei Lin summon, Ryo checkpoint |
| **Blocking** | Khóa slot / auto resolve | (optional) lớp chiếm Day — MVP dùng Morning thay |
| **Optional** | Notification, player chọn | Festival ngày 20 |

### Story-fixed events — xem **§16 Story Calendar** (ngày thực 08–09/2026)

- Ryo / Mei Lin chain gắn điều tra Lumina (17/08) và incident Cadence (05/09)
- Missed event → flag `missed_ryo_12_09`; branch dialogue khác ở rank 4+

---

## 9. Dungeon Run ↔ Calendar

| Rule | Detail |
|------|--------|
| Entry | Evening activity, tiêu 1 slot |
| During run | `CalendarState` paused; `RunState` active |
| Exit | Save `RunSnapshot` (seed, floor, sector) |
| Boss defeat | Flag `sector_cleared`, rewards; không refund slot |
| MVP limit | 1 run tối/ngày; **07/09–20/09** mở Cadence Remediation (flag `vault_quest_active`) |
| Deadline | **20/09** — phải clear Vault; fail → bad branch (§16.4) |

---

## 10. Arc End (30/09)

```
if date > 30/09 OR final_event_complete:
  → ArcSummary (stats, Bond ranks, flags)
  → Save meta, lock calendar
  → Ending scene theo flag priority
```

Ending priority (stub): `vault_cleared_on_time` + `mei_investigation_deep` > `ryo_trust_high` > `party_bond_avg` > `vault_missed_deadline` > default.

---

## 11. Architecture

### `GameMetaState` (unified save)

```
GameMetaState
├── CalendarState
│     GameDate CurrentDate       // month + day (Arc 1: 09/01–09/30)
│     DayPhase CurrentPhase      // Morning | Day | Evening
│     int SlotsUsedToday         // 0–2
│     bool MorningQuizDone
├── SocialStats                  // 5 stats × rank + exp
├── BondState                    // npcId → rank, exp, arcCap
├── StoryFlags                   // Dictionary<string, bool/int>
└── RunSnapshot                  // seed, floor, sector (bridge RunState)
```

- MVP persist: JSON file (path TBD, e.g. `Application.persistentDataPath`)
- `RunProfile` giữ player name (`PlayerPrefs`); meta save tách biệt
- `RunState` in-memory trong dungeon; flush vào `RunSnapshot` on exit

### Enums

```csharp
DayPhase   { Morning, Day, Evening }
EchoKey    { Melody, Bass, Harmony, Measure, Dissonance, Pulse,
             Rest, Overtone, Static, Cadence, Crescendo, Fermata }
SocialStat { Resonance, Cadence, Pulse, Harmony, Rhythm }
```

### ScriptableObjects

| SO | Role |
|----|------|
| `ActivityDefinitionSO` | id, phase, schedule, stat rewards, scene target |
| `CalendarEventSO` | fixed day, conditions, priority, forced/optional |
| `BondDefinitionSO` | npcId, echoKey, rank thresholds, availability, arcCap |
| `SocialEventSO` | VN content, choices, stat/Bond rewards |
| `ClassroomQuizSO` | question, 4 choices, correct index, stat reward |
| `BondPassiveSO` | combat buff unlock by rank |

### Module layout

```
Assets/FracturedChorus/
├── Meta/              GameMetaState, CalendarState, SocialStats, BondState, SaveLoad
├── Hub/               CampusHubController, ActivityPickerUI, CalendarUI, MorningQuizUI
├── Social/            BondManager, ActivityResolver, CalendarEventScheduler
└── Data/ScriptableObjects/Social/
```

**Reuse:** `RunMapSceneLoader`, `PrologueVNController` patterns (typewriter, choice), `RunProfile.PlayerName`, `RunState` / `CadenceRunProgress`.

---

## 12. MVP Asset Count (estimate)

| Asset | Count |
|-------|-------|
| `BondDefinitionSO` | 6 |
| `SocialEventSO` | ~20 |
| `CalendarEventSO` | ~12 |
| `ActivityDefinitionSO` | ~8 |
| `ClassroomQuizSO` | 30 |
| `BondPassiveSO` | 6 |

---

## 13. Implementation Order

1. `Meta/` — state types, save/load JSON
2. `CampusHub.unity` + CalendarUI + ActivityPicker
3. Morning Quiz overlay + 30 `ClassroomQuizSO`
4. Hangout pipeline — Ren end-to-end (`SocialEventSO`)
5. Bond lock + arc cap logic
6. Part-time jobs + weekly schedule filter
7. `CalendarEventScheduler` — Ryo / Mei Lin chain
8. Dungeon Run as Evening activity + `RunSnapshot`
9. Day 30 arc summary + ending stub

---

## 14. Out of Scope (MVP)

- Arc 2+ content và 6 Echo Keys reserved
- Full shop economy
- Reversed Key (Mei Lin investigation branch)
- Weather / random event pool
- Lore chi tiết ngoài §16 (dialogue canon vẫn từ Google Doc / Decision Log)

---

## 15. Open Items (post-MVP tune)

- Exact Bond EXP thresholds per rank
- Stat EXP rank curve balance
- JSON save encryption / slot system
- Classroom quiz wrong-answer penalty (optional Rhythm +2)

---

## 16. Story Calendar — Arc 1 (Tháng 9)

### 16.1 Pre-hub: Opening Investigation

**Scene:** `OpeningInvestigation` — plays **after** `PrologueVN`, before `CampusHub` (supersedes older “17/08 before Prologue” order; see `2026-07-12-opening-investigation-vn-design.md`).

| Beat | Sự kiện | Flag |
|------|---------|------|
| Haruto / SyncPod | Night hijack in Lumina | — |
| Crime scene | Mei Lin + Ryo; log `SW-ES-040` / StellaWorks off-record | `lumina_case_open` |
| Ren arrival | Same hour; forced Top-1 *Eternal Spark* (clean) | `ren_arrived_hima`, `opening_investigation_done` |

- Không tiêu hub slot; linear VN
- Thiết lập thread điều tra → nối vụ 05/09 và deadline vault

### 16.2 Hub start — Tháng 9

`CalendarState` bắt đầu **01/09**. `GameDate` = `{ month: 9, day: N }`.

| Ngày | Loại | Sự kiện | Flag / ghi chú |
|------|------|---------|----------------|
| **01/09** | Forced | Ren đến HIMA; tutorial hub | `ren_arrived_hima` |
| **02/09** | Forced | Nhập học sớm; gặp **Astra**, tham quan campus | `astra_met`, `hima_tour_done` |
| 03–04/09 | Free | Player activities; morning quiz bắt đầu | — |
| **05/09** | Forced (full day) | Khai giảng → Ren **Resonance Dive lần đầu** → **Coda** xuất hiện hỗ trợ → Boss **Mimi** → thoát | `opening_ceremony`, `first_resonance_dive`, `coda_met`, `coda_rescue`, `cadence_breach`, `mimi_encountered` |
| **06/09** | Forced | Gặp lại **Charlotte**; bàn incident; quyết định điều tra **LUXE**; nhận deadline vault | `charlotte_reunited`, `luxe_investigation_start`, `vault_quest_active` |
| 07–19/09 | Free + scheduled | Hangout, part-time, Ryo/Mei Lin events, **Dungeon Run** (Cadence) | Countdown UI → 20/09 |
| **12/09** | Optional | Ryo checkpoint (Measure) | Cần `lumina_case_open` |
| **14/09** | Optional | Mei Lin hỏi han (Dissonance) | Cadence ≥ 2 |
| **20/09** | **Deadline** | Phải hoàn thành Vault Remediation | `vault_cleared` hoặc `vault_missed_deadline` |
| 21–30/09 | Free / epilogue | Hậu vault; arc wrap | Bond cap mở thêm nếu `vault_cleared` |

#### 05/09 — Khai giảng & Resonance Dive (chi tiết)

Thứ tự scene bắt buộc trong ngày:

```
Opening Ceremony (HIMA)
  → Ren kích hoạt Resonance Dive lần đầu (uncontrolled)
  → Rơi vào Cadence
  → Coda xuất hiện — hỗ trợ Ren điều hướng / ổn định echo
  → Encounter Boss Mimi
  → Thoát ra ngoài (breach kết thúc)
```

| Beat | Nội dung | Flag |
|------|----------|------|
| Ceremony | Lễ khai giảng; trigger Dive | `opening_ceremony` |
| First Dive | Ren mất kiểm soát, rơi Cadence | `first_resonance_dive`, `cadence_breach` |
| Coda rescue | Coda can thiệp lần đầu; giới thiệu ngắn | `coda_met`, `coda_rescue` |
| Mimi | Boss fight; chip LUXE drop (setup 06/09) | `mimi_encountered` |
| Escape | Quay campus; full day consumed | — |

- **Resonance Dive** = mechanic narrative (lần đầu = forced tutorial breach); không phải hub activity
- **Coda** chưa hangout được trong 05/09; mở từ **06/09** (sau khi `coda_met`)
- Bond Coda (Harmony): rank 1 scene có thể nhúng trong `coda_rescue` hoặc hangout đầu 07/09

### 16.3 Lý do quay lại Cadence — deadline 20/09

Sau incident **05/09**, Ren để lại **Fracture Signature** trong Cadence Vault. Ren và Charlotte phải clear Vault trước **20/09** (14 ngày kể từ 06/09) vì **ba áp lực chồng lên nhau**:

1. **Quy chế HIMA (Astra)**  
   Học sinh nhập học sớm gây **Cadence Breach** trong lễ khai giảng → bắt buộc **Vault Remediation** trong 14 ngày. Không hoàn thành → đình chỉ tư cách nhập học sớm và báo cáo bắt buộc lên **LUXE** (đối tác tài trợ HIMA).

2. **Trace TTL 14 ngày (LUXE)**  
   Chip nhận dạng rơi từ Mimi mang logo LUXE. Access log trong vault chỉ giữ **14 ngày** trước khi LUXE remote-wipe — đây là manh mối duy nhất nối vụ Lumina **17/08** với HIMA. Charlotte nhận ra pattern vì từng thấy signature tương tự.

3. **Fracture lan (cảnh báo từ Charlotte)**  
   Signature không được xử lý → echo leak ảnh hưởng campus (debuff **Rhythm** nhẹ, một số khu hub khóa). Charlotte đã từng vào Cadence trước đó nên cảm nhận được mối nguy — thúc đẩy hợp tác với Ren.

**Gameplay:** Từ 07/09, activity **Dungeon Run** chỉ mở buổi tối và gắn label *Cadence Remediation*. UI countdown *"Còn X ngày đến 20/09"*. Clear boss F16 trước deadline → `vault_cleared_on_time`.

### 16.4 Deadline fail / success

| Kết quả | Flag | Hậu quả Arc 1 |
|---------|------|----------------|
| Clear vault trước 20/09 | `vault_cleared_on_time` | Mở Bond cap party; Mei Lin/Ryo rank 3+ stub; ending tốt hơn |
| Không clear đến 20/09 | `vault_missed_deadline` | Đình chỉ nhập học sớm; LUXE lock điều tra; Rhythm debuff kéo dài; branch ép |
| Clear sau 20/09 | `vault_cleared_late` | Vẫn chơi tiếp nhưng mất trace LUXE; Mei Lin disappointed |

### 16.5 Bond lock gắn story

| Bond | Cap đến | Mở khi |
|------|---------|--------|
| Charlotte | 06/09 | Hangout sau reunion; cap rank 3 đến `vault_cleared` |
| Astra | 02/09 | Hangout sau tour; cap rank 5 |
| Coda | 05/09 | Gặp trong Resonance Dive; hangout từ **06/09**; cap rank 4 đến `vault_cleared` |
| Ren (self) | — | Cap rank 4 đến `arc1_midpoint` (~15/09) |
| Ryo / Mei Lin | Rank 2 | Rank 3+ sau `vault_cleared` hoặc Arc 2 |

### 16.6 Scene catalog bổ sung

| Scene | Vai trò |
|-------|---------|
| `OpeningInvestigation` | 17/08 Ryo + Mei Lin |
| `CampusHub` | Meta hub 01/09+ |
| `SocialEventScene` | VN beats (02/09 Astra, 05/09 Coda rescue, 06/09 Charlotte, …) |
| `OpeningCeremony` | 05/09 — Resonance Dive + Mimi (combat/VN hybrid) |
| `RunMapPrototype` | Cadence Remediation 07/09–20/09 |

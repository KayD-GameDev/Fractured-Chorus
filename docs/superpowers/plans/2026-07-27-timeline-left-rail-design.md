# Timeline Left Rail (Clef Column) — Design & Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thiết kế lại cột trái timeline thành Clef Column (nền + khóa sol + PHASE/AV + 3 ô avatar) — **không** mất sợi dây `LaneLines`.

**Architecture:** LeftRail = `Header` (scene) + `LaneAvatarGutter` (runtime). Art chỉ trong LeftRail. Viewport staff / LaneLines / TrackLine / BossTrackFrame / ScanBar **out of scope** redesign; chỉ regression-check.

**Tech Stack:** Unity 6 · UGUI · `BeatTimelineUIView` · `TimelineLaneAvatarSlotView` · `Resources/UI/Combat/Timeline/LeftRail/`

## Global Constraints

- **Không mất sợi dây:** `viewport/LaneLines` (Image ngang 5px) còn sau mọi thay đổi
- LeftRail art **không bleed** vào Viewport (Header W ≈ 211px SoT)
- Không đổi logic select lane / AV / phase index / counter
- Commit chỉ khi user yêu cầu
- Play Mode checklist bắt buộc

### File map

| Action | Path |
|--------|------|
| Survey SoT | `CombatPrototype.unity` Header / Viewport / Clef / Budget |
| Brief | `Art/UI/Combat/Timeline/LeftRail/ASSET_BRIEF.md` |
| Ref | `Art/UI/Combat/Timeline/Refs/left_rail_clef_column_ref_v1.png` |
| Create art | `Art/.../LeftRail/left_rail_bg_v1.png`, `treble_clef_v1.png`, `lane_avatar_ring_v1.png` |
| Mirror | `Resources/UI/Combat/Timeline/LeftRail/*` |
| Modify | `BeatTimelineUIView.cs`, `TimelineLaneAvatarSlotView.cs` |
| Scene | `CombatPrototype.unity` — fix `PHARSE`→`PHASE`; Text Clef → Image |
| Doc | `docs/combat/COMBAT_MECHANICS.md` 1 đoạn |

---

## Size survey (CombatPrototype @ Canvas 1920×1080) — LOCKED

Công thức Rect: `size = parentSize × (anchorMax−anchorMin) + sizeDelta`.

| Node | Anchors / layout | Size / pos (px) |
|------|------------------|-----------------|
| **BeatTimelineUI** | amin (0.02,0.02) amax (0.98,0.2228) sd (0,138.8) | **W 1843 · H 358** |
| **Header** (= LeftRail) | left stretch Y, sd.x **210.82** | **W 211 · H 358** |
| **Viewport** | stretch + pos.x 101.41, sd (−218.82,−16) | H ≈ **342**; inset trái ≈ Header |
| **PhaseLabel** | top-left Header | **154×51**, font **30**, text **`PHARSE`** (bug) |
| **Budget** | Header local ~(105, 61) | **81×37**, BudgetText font **20** `0/10` |
| **Clef** (scene) | Header ~(105, −48) | **120×160**, Text font 120 = `\u266A` (♪) — **chưa phải khóa sol** |
| **LaneAvatarGutter** | runtime, `laneAvatarGutter: {fileID: 0}` | W **44**, slot **40×40** |
| **laneBand** (serialized) | min **0.16** · max **0.48** | Y từ đáy VP: **164 / 109 / 55** (3 lane) |
| **noteBand** | **0.72** | Y ≈ **246** (boss notes — trên avatar) |
| **LaneLines** | full width Viewport | thickness **5** — SoT sợi dây |
| **TrackLine** | y=6, h=2 | baseline — out of scope |
| **ScanBar** | x=26, w=6 | out of scope |
| **BossTrackFrame** | h=56 | out of scope |
| **preserveSceneLayout** | **1** | giữ frame Header/outer |

### Layout implication

```
Header 211px          Viewport (~1624×342)
┌────────────────┐┌─────────────────────────────┐
│ PHASE 154×51   ││ noteBand Y≈246              │
│ Budget 81×37   ││ LaneLines h=5 @ Y 164/109/55│
│ Clef 120×160   ││ (KHÔNG đụng LeftRail art)   │
│ [avatars 40]   ││                             │
│ gutter runtime ││                             │
│ W=44 overlays  ││                             │
└────────────────┘└─────────────────────────────┘
H ≈ 358
```

**Conflict hiện tại:** gutter 44px + slot 40 **quá hẹp** so với ref (ô ~48–52 trong cột 211). Plan đề xuất nâng slot → **48–52**, căn **center X = Header/2 ≈ 105.4** (trùng Budget/Clef X scene), gutter widen tới **Header width** hoặc parent slots vào Header — **không** mở rộng Header sang phải (tránh đẩy Viewport / mất cảm giác dây).

### Art export targets (@2×)

| Asset | PNG canvas | Display |
|-------|------------|---------|
| `left_rail_bg_v1` | **422×716** | 211×358 |
| `treble_clef_v1` | **256×320** | ~120×160 |
| `lane_avatar_ring_v1` | **96×96** | 48–52 |

Chi tiết alpha/import: `Art/.../LeftRail/ASSET_BRIEF.md`.

---

## Resource prep status

| Item | Status |
|------|--------|
| Size survey scene | ✅ locked |
| UI ref mockup | ✅ `Refs/left_rail_clef_column_ref_v1.png` |
| Folders Art + Resources `LeftRail/` | ✅ |
| `ASSET_BRIEF.md` | ✅ |
| Runtime PNGs bg / clef / ring | ✅ sized + mirrored Resources |
| Gate L-Q1…L-Q8 | ✅ user ok 2026-07-27 |
| Code / scene wire | ✅ `EnsureLeftRailVisuals` + avatar ring; scene `PHASE` |

---

## Quyết định (gate)

| ID | Câu hỏi | Đề xuất |
|----|---------|---------|
| L-Q1 | Phạm vi | **A)** Header + Gutter = 1 Clef Column |
| L-Q2 | Khóa sol | **A)** Sprite thay Text ♪; watermark alpha ~0.4–0.55; giữ rect ~120×160 |
| L-Q3 | PHASE / AV | **A)** Giữ; sửa `PHARSE`→`PHASE` |
| L-Q4 | 3 ô | **A)** Portrait + ring; click select; display **48–52** (↑40) |
| L-Q5 | Avatar ↔ dây | **A)** Chỉ sync Y với LaneLines; không vẽ dây trong LeftRail |
| L-Q6 | Nền | **A)** `left_rail_bg_v1` riêng — **không** crop staff |
| L-Q7 | Dead | **A)** desat alpha 0.35 |
| L-Q8 | Gutter width | **A)** Widen gutter/slots trong Header 211; **không** tăng Header W |

**Gate lock (2026-07-27):** User **ok** → L-Q1…L-Q8 theo cột Đề xuất.

---

## Design (tóm tắt)

- Neon Cadence: navy fill, cyan top / magenta bottom edge
- Clef sprite thay `\u266A`; PHASE + Budget trên/overlay
- 3 ring căn Y = `GetLaneYFromBottom` (cùng LaneLines)
- Ref: `Refs/left_rail_clef_column_ref_v1.png` (composition only)

### Non-goals

- Không redesign `timeline_staff_holo_bg_v1` / notes / Perfect
- Không bake LaneLines vào bg
- Không tăng Header width > ~211 (trừ khi đo lại Viewport inset)

---

## Implementation tasks

### Task 0: Gate lock

- [ ] User chốt L-Q1…L-Q8 (hoặc “ok đề xuất”)

### Task 1: Regression baseline (trước art/code)

- [ ] Play Mode note: `LaneLines` count = alive players; h≈5; Y khớp lane
- [ ] Screenshot baseline dây (optional)

### Task 2: Gen runtime art (theo ASSET_BRIEF sizes)

- [ ] `left_rail_bg_v1` 422×716 — **cấm** staff wires
- [ ] `treble_clef_v1` 256×320
- [ ] `lane_avatar_ring_v1` 96×96 (+ selected opt)
- [ ] Mirror Resources; Sprite UI import

### Task 3: Wire bg + clef trên Header

- [ ] Image bg stretch Header; clef Image thay Text `Clef`
- [ ] Fix PhaseLabel `PHASE`
- [ ] Verify Viewport/LaneLines untouched

### Task 4: Avatar rings + size

- [ ] Slot 48–52; center X ≈ 105; Y sync lane band
- [ ] Ring sprite + portrait fallback tint
- [ ] Click select giữ nguyên

### Task 5: Docs + Play Mode sign-off

- [ ] `COMBAT_MECHANICS.md` 1 đoạn LeftRail vs LaneLines
- [ ] Checklist dưới — all pass

---

## Play Mode checklist

| # | Check |
|---|--------|
| 1 | Đúng 3 sợi dây ngang màu lane trong track |
| 2 | Deploy/Execute/scroll: dây không mất |
| 3 | LeftRail bg+clef trong **211×358**; không che notes |
| 4 | 3 ô thẳng hàng Y với 3 dây |
| 5 | Click ô → select ring |
| 6 | Staff / TrackLine / BossTrackFrame / ScanBar OK |
| 7 | Resolve chip không lệch (`GetHeaderRightEdge…`) |

---

## Order

```
Gate lock → Task1 baseline → Task2 art @sizes → Task3 Header wire → Task4 avatars → Task5 sign-off
```

Không done nếu checklist #1 hoặc #2 fail.

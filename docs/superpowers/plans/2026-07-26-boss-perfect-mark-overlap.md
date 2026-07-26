# Boss Perfect Mark Size & Neighbor Overlap — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** ✓ Cover Perfect đọc rõ trong đầu nốt, hold-preview không phình quá tay, và **không chồng** lên nốt sống kề (đặc biệt nốt `2`/`3` hoặc đầu beamed còn hit).

**Architecture:** Giữ Perfect là child của `NoteNum_*` (cùng neo số). Size không còn “min px cố định + preview ×1.55” thuần — chuyển sang **budget theo khoảng cách beat** (neighbor-aware) + **tight sprite** (ít glow ngoài vòng). Preview = settled size × hệ số nhẹ (≤1.15), không nhân chồng scale.

**Tech Stack:** Unity 6 · UGUI · `BossNoteClusterView` · `BossNoteNumberLayout` · `cover_perfect_v1` (Art + Resources)

## Global Constraints

- Không đổi logic counter (`GetRemainingHits` / cancel / Space)
- Perfect vẫn chỉ hiện khi remaining ≤ 0 (hoặc drop preview remainingAfter ≤ 0)
- Parent Perfect = `NoteNum_*` slot; không neo lại `noteBand` / điểm quét cột
- `RefreshBeatsAndBossNotes` sau place skill **giữ nguyên** (đã fix degrade)
- Commit chỉ khi user yêu cầu
- Play Mode checklist bắt buộc trước đóng task

### File map

| Action | Path | Trách nhiệm |
|--------|------|-------------|
| Modify | `Assets/FracturedChorus/UI/BossNoteNumberLayout.cs` | Tunables size / preview / cap neighbor |
| Modify | `Assets/FracturedChorus/UI/BossNoteClusterView.cs` | Resolve size neighbor-aware; preview nhẹ |
| Modify | `Assets/FracturedChorus/UI/BeatTimelineUIView.cs` | DropCover fallback dùng cùng resolve |
| Replace art | `Art/.../cover_perfect_v1.png` + Resources mirror | Tight ✓, glow trong vòng, ít bleed |
| Optional util | `BossNotePerfectSizeUtil.cs` (nếu resolve >40 dòng) | Pure function size từ layout + spacing |
| Doc | `docs/combat/COMBAT_MECHANICS.md` | 1 đoạn visual Perfect vs neighbor |

---

## Hiện trạng (repro từ Play Mode)

| State | Quan sát |
|-------|----------|
| Hold / drop preview | ✓ + aura **quá to** (scaleVsNumber 1.9 × preview 1.55 × min 56) |
| Đặt xong (cleared) | Size “ổn” trong đầu nốt đơn lẻ |
| Cleared rồi kề nốt `2` / nốt còn hit | Vòng glow Perfect **chồng** lên đầu nốt kế — xấu |
| Beamed một đầu ✓ + đầu kia còn `1` | Cùng sprite beam; ✓ to → đè đầu còn sống |

### Công thức size hiện tại (bug nguồn)

```
side = max(slotSide × perfectMarkScaleVsNumber, perfectMarkMinPx)
if preview: side *= perfectPreviewScale
```

Default: `1.9 × slot` rồi × `1.55` lúc hold → footprint lớn hơn cả đầu nốt nhạc; glow sprite còn padding đen/cyan → overlap hàng xóm.

---

## Quyết định cần chốt (gate) — đề xuất mặc định

| ID | Câu hỏi | Đề xuất | Lý do |
|----|---------|---------|--------|
| P-Q1 | Preview scale? | **A)** `preview = settled × 1.1` (max 1.15) · B) bằng settled · C) giữ 1.55 | Hold đang to quá |
| P-Q2 | Settled size SoT? | **A)** `min(slotSide × 1.35, neighborCap)` · B) cố định 40px · C) = slotSide | Đọc rõ nhưng không to hơn đầu số |
| P-Q3 | Chống overlap? | **A)** Cap theo **½ khoảng cách contentX tới beat kề** · B) chỉ giảm alpha · C) chỉ sửa art | A giải quyết case nốt `2` kế |
| P-Q4 | Beamed một đầu clear? | **A)** Cap thêm theo khoảng 2 đầu trong sprite · B) ẩn beam khi 1 clear · C) như single | Giữ N-Q7 (beam + ✓) |
| P-Q5 | Art Perfect? | **A)** Gen/edit **tight** (✓ + vòng mỏng, glow ≤ 8% canvas) · B) giữ art, chỉ scale code | Art hiện bleed mạnh |
| P-Q6 | Nốt cleared cạnh nốt sống: ai thắng z-order? | **A)** Perfect dưới glyph sống (sibling order) · B) Perfect trên · C) thu nhỏ Perfect thêm 10% nếu neighbor sống | A + P-Q3 đủ |

**Nếu user không trả lời từng dòng → implement theo cột Đề xuất.**

> **Gate lock (2026-07-26):** User **ok** → implement P-Q1…P-Q6 theo đề xuất.

---

## Design — Neighbor-aware size

### Input

- `slotSide` = cạnh ô `NoteNum_*` (đã có)
- `beatIndex` của Perfect
- `ContentXForBeat(beat)` (đã inject vào cluster)
- Neighbor trái/phải = beat ± 1 (nếu có impact hoặc có NoteNum slot / telegraph)

### Cap

```
gapL = |x(beat) - x(beat-1)|   // nếu không có neighbor trái → +∞
gapR = |x(beat+1) - x(beat)|
maxDiameter = min(gapL, gapR) × perfectNeighborFill   // default 0.72
side = min(desiredSettled, maxDiameter)
side = max(side, perfectMarkMinPx)   // min đọc được, default 36 — nhưng nếu maxDiameter < min → dùng maxDiameter (ưu tiên không chồng)
```

**Rule ưu tiên:** không chồng > floor size. Nếu beat rất hẹp: Perfect nhỏ hơn, vẫn căn giữa slot.

### Preview

```
previewSide = settledSide × perfectPreviewScale   // default 1.1, Range 1.0–1.2
```

**Cấm** nhân thêm `perfectMarkScaleVsNumber` lần hai ở `AddDropCoverOverlay` (hiện fallback nhân vs × preview → double inflate).

### Beamed head

Khi Perfect là child của note beamed (local head pos):

```
headGap = |localLeft - localRight|   // hoặc FittedLocal khoảng cách 2 đầu
maxDiameter = min(maxDiameter, headGap × 0.72)
```

---

## Art brief — `cover_perfect_v1` tight

| Yêu cầu | Giá trị |
|---------|---------|
| Canvas | Vuông 1024 (hoặc 512), **transparent** BG |
| ✓ | Trắng, **căn geometric center**, stroke đủ dày đọc @ 36–48px |
| Vòng | 1 vòng neon mỏng cyan/magenta; **không** outer bloom lớn |
| Bleed | Glow/soft edge ≤ ~8% bán kính ngoài vòng |
| Không | Chữ, digit, watermark, khung kép dày như hiện tại |

Pipeline: edit/gen → matte → ghi đè:

- `Assets/FracturedChorus/Art/UI/Combat/Timeline/Feedback/cover_perfect_v1.png`
- `Assets/FracturedChorus/Resources/UI/Combat/Timeline/cover_perfect_v1.png`

Giữ GUID `.meta` (không tạo file mới tên khác trừ khi import force).

---

## API / code shape

### `BossNoteNumberLayout` — thay tunables

| Field | Default đề xuất | Ghi chú |
|-------|-----------------|--------|
| `perfectMarkScaleVsNumber` | **1.35** (↓ từ 1.9) | So với ô số |
| `perfectPreviewScale` | **1.1** (↓ từ 1.55) | Hold nhẹ hơn settled |
| `perfectMarkMinPx` | **36** (↓ từ 56) | Floor mềm; neighbor cap thắng nếu hẹp |
| `perfectNeighborFill` | **0.72** (mới) | Max đường kính = fill × min gap |
| Xóa / deprecate | double-apply trong DropCover | Một đường resolve duy nhất |

### `BossNoteClusterView`

```csharp
Vector2 ResolvePerfectMarkSize(
    RectTransform slot,
    int beatIndex,
    bool preview,
    bool beamedHead,
    float beamedHeadGapPx /* 0 nếu single */)
```

- Gọi từ `SpawnPerfectOnSlot` + `TryAttachPerfectPreview`
- Expose `public Vector2 GetPerfectMarkSizeForBeat(int beat, bool preview)` cho timeline fallback

### `BeatTimelineUIView.AddDropCoverOverlay`

- Ưu tiên `TryAttachPerfectPreview` (đã có)
- Fallback: gọi `GetPerfectMarkSizeForBeat` — **không** tự nhân scale lại

### Z-order (P-Q6 A)

Khi rebuild cluster: spawn toàn bộ Perfect cleared **trước**, spawn nốt sống / số **sau** (hoặc `SetAsLastSibling` trên note glyph sống). Perfect không đè số/glyph hàng xóm.

---

## Tasks

### Task 1 — Chốt gate + lock defaults trong plan

- [ ] **Step 1:** Xác nhận P-Q1…P-Q6 (hoặc “theo đề xuất”).
- [ ] **Step 2:** Ghi defaults vào bảng tunables phía trên (đã điền đề xuất).

### Task 2 — Pure size resolve + unit-style checks (Editor / manual table)

- [ ] **Step 1:** Thêm `perfectNeighborFill`; hạ default scale/preview/min như bảng.
- [ ] **Step 2:** Implement `ResolvePerfectMarkSize(...)` neighbor-aware trong `BossNoteClusterView` (hoặc util tách).
- [ ] **Step 3:** Bảng verify tay (log tạm `[PerfectSize]`):

| Case | Expect |
|------|--------|
| Single cleared, 2 bên trống | side ≈ slot×1.35 |
| Cleared @ N, nốt 2 @ N+1 | side ≤ 0.72 × gap(N,N+1) |
| Hold preview | ≤ settled×1.15 |
| Beamed L clear, R còn 1 | side ≤ 0.72 × khoảng 2 đầu |

- [ ] **Step 4:** Xóa double-scale trong `AddDropCoverOverlay` fallback.

### Task 3 — Art tight Perfect

- [ ] **Step 1:** Gen/edit `cover_perfect_v1` theo brief (transparent, centered ✓).
- [ ] **Step 2:** Matte + ghi Art + Resources; giữ `.meta` GUID.
- [ ] **Step 3:** Play Mode: settled ✓ không còn “quầng” đè nốt kế khi gap ~1 beat.

### Task 4 — Preview hold polish

- [ ] **Step 1:** `perfectPreviewScale = 1.1`; outline preview mỏng hơn settled (hoặc cùng độ dày).
- [ ] **Step 2:** Giữ ẩn Text số lúc preview; `EndPerfectPreview` restore (đã có — regression check).
- [ ] **Step 3:** Hold skill phủ đủ hit trên nốt 1 kề nốt 2: preview không phình đè số `2`.

### Task 5 — Z-order + beamed edge cases

- [ ] **Step 1:** Rebuild order: cleared Perfect không `SetAsLastSibling` toàn layer trên nốt sống.
- [ ] **Step 2:** Beamed một đầu clear: ✓ trong đầu clear; đầu còn hit giữ số; không overlap visually theo Task 2 cap.
- [ ] **Step 3:** Cả hai đầu clear: hai ✓, mỗi cái cap theo headGap/2 logic.

### Task 6 — Docs + Play checklist

- [ ] **Step 1:** `COMBAT_MECHANICS.md` — Perfect size neighbor-capped; preview ≤1.15×.
- [ ] **Step 2:** Play checklist (bắt buộc):

1. Place 1 Active lên nốt 1 đơn → ✓ settled, không to bất thường  
2. Hold (chưa thả) lên nốt sẽ clear → preview hơi lớn hơn settled, **không** gấp đôi  
3. Clear nốt 1, bên phải còn nốt 2 → ✓ không chồng vòng lên đầu nốt 2  
4. Beamed 1+1: clear trái → ✓ trái + số phải; không đè  
5. Clear cả hai beamed → hai ✓ gọn trong 2 đầu  
6. Exit Play + re-enter: layout serialize ổn  

---

## Ngoài scope (không làm trong plan này)

- Đổi grouping beamed / HitsRequired rules (đã chốt plan note visuals)
- Đổi Cover Miss art
- Perfect chip bay lên đầu unit (`CounterNoteResolveChipView`) — hệ khác
- Auto-layout đổi `slotWidth` timeline

---

## Risk register

| Risk | Mitigation |
|------|------------|
| Neighbor cap làm ✓ quá nhỏ trên timeline dày | `perfectNeighborFill` 0.72 + art tight; minPx chỉ khi gap cho phép |
| Scene serialize giữ scale cũ 1.9/1.55 | OnValidate / defaults mới; ghi chú “re-select BeatTimelineUI hoặc reset layout section” |
| Art tight vẫn glow URP bloom | Giảm bloom trên UI camera / sprite đã cắt glow |
| Preview gắn slot rồi Destroy làm mất Perfect settled | Chỉ destroy `NotePerfectPreview_*`; settled tên `NotePerfect_*` — verify HideDropGhost |

---

## Success criteria

1. Hold preview: rõ nhưng **không** phình đè hàng xóm.  
2. Settled ✓: đọc được trong đầu nốt, **không** chồng nốt `2`/`3` kề.  
3. Beamed partial/full clear: đẹp, không đè số còn lại.  
4. Degrade + rebuild cluster sau place vẫn hoạt động (không regress Task refresh).

---

## Execution note

Sau khi user chốt gate (hoặc “làm theo đề xuất”), agent chạy Task 2→6 theo thứ tự; không gen art trước khi size formula xong (tránh tune art hai lần).

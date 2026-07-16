# Timeline Note Readability — Implementation Plan

**Date:** 2026-07-14  
**Design spec:** [`../specs/2026-07-14-timeline-note-readability-design.md`](../specs/2026-07-14-timeline-note-readability-design.md)  
**Target:** Unity 6 · `Assets/FracturedChorus/` · scene `CombatPrototype`  
**Estimate:** 3 phases · ~0.5 ngày

---

## 0. Principles

- Resolve logic (`CanAssignAction`, counter HitsRequired, Perfect chip) **không đổi**
- Presentation-only: catalog + sprites trên note / drop / drag cover
- Art đã giữ tại `Art/UI/Combat/Timeline/` — không gen art mới trong plan này
- Standing footprint dots, W window, empty-beat: **out of scope**
- Không comment thừa; mỗi phase có acceptance trước khi sang phase sau
- Thêm UI field → cập nhật `CombatPrototypeBootstrapEditor`

### Hook hiện tại

| Surface | Today | Replace with |
|---------|--------|--------------|
| `BeatSegmentView.SetTelegraphSlot` | `portrait.color = GetNotePortraitColor(tier)` | `portrait.sprite` + white/tint from catalog |
| `ShowDropGhost` Active beats | tinted `AddDropPreviewDot` | ghost sprite Image |
| Drag over impact note | (none) | cover Perfect/Miss overlay |
| `HideDropGhost` | clear dots + lane ghost | also clear cover overlays |

### Out of scope

- Footprint standing redesign · W window · empty-beat · CORE/MICRO/EYE · resolve Perfect chip art swap

---

## Phase 1 — Catalog + note tier sprites (~1–1.5h)

### Task 1.1 — `TimelineNoteVisualCatalog`

**Objective:** Một chỗ giữ 7 sprite + size; không hardcode path trong view.

**Files:**
- Create: `Assets/FracturedChorus/Combat/Presentation/TimelineNoteVisualCatalog.cs`

```csharp
namespace FracturedChorus.Combat.Presentation
{
    [System.Serializable]
    public class TimelineNoteVisualCatalog
    {
        public Sprite NoteRed;
        public Sprite NoteBlue;
        public Sprite NotePurple;
        public Sprite DropGhostValid;
        public Sprite DropGhostInvalid;
        public Sprite CoverPerfect;
        public Sprite CoverMiss;

        public float NoteDisplaySize = 26f;
        public float GhostDisplaySize = 28f;
        public float CoverDisplaySize = 32f;

        public Sprite NoteForTier(BossNoteTier tier) => tier switch
        {
            BossNoteTier.Purple => NotePurple != null ? NotePurple : NoteRed,
            BossNoteTier.Blue => NoteBlue != null ? NoteBlue : NoteRed,
            _ => NoteRed
        };

        public Sprite DropGhost(bool valid) =>
            valid ? DropGhostValid : DropGhostInvalid;

        public Sprite Cover(bool valid) =>
            valid ? CoverPerfect : CoverMiss;
    }
}
```

*(Import `BossNoteTier` from `FracturedChorus.Combat.Timeline`.)*

**Verify:** compiles; null-safe fallbacks không NRE khi thiếu 1 sprite.

### Task 1.2 — Wire catalog on `BeatTimelineUIView` + pass into segments

**Files:**
- Modify: `Assets/FracturedChorus/UI/BeatTimelineUIView.cs`
- Modify: `Assets/FracturedChorus/UI/BeatSegmentView.cs`
- Modify: scene / bootstrap assign sprites (Inspector hoặc Editor setup nếu có Ensure path)

**Changes:**
1. `[SerializeField] TimelineNoteVisualCatalog noteVisuals;` trên `BeatTimelineUIView` (+ public getter nếu bootstrap cần).
2. Khi bind/refresh segment có telegraph: gọi API mới trên `BeatSegmentView`, ví dụ `SetNoteVisualCatalog(noteVisuals)` một lần, hoặc truyền sprite vào `SetTelegraphSlot`.
3. `SetTelegraphSlot` (impact):
   - `portrait.sprite = catalog.NoteForTier(telegraph.NoteTier)` khi sprite ≠ null
   - `portrait.color = Color.white` (hoặc alpha giữ readability)
   - `portrait.rectTransform.sizeDelta = Vector2.one * catalog.NoteDisplaySize`
4. Windup: **không** gán note-tier sprite (giữ treatment hiện tại).
5. Nếu catalog/sprite null → fallback tint màu cũ (`GetNotePortraitColor`) để không vỡ scene chưa assign.

**Acceptance Phase 1:**
- [x] Compile
- [x] Play CombatPrototype: Red/Blue/Purple impact notes hiện đúng sprite; windup không đổi thành số 1/2/3
- [x] Scene chưa gán sprite vẫn chạy (fallback tint)

*(Runtime done 2026-07-14/16. Sprites load từ `Resources/UI/Combat/Timeline/`.)*

**Commit (khi user yêu cầu):** `Add timeline note tier sprites via visual catalog.`

---

## Phase 2 — Drop ghost + drag cover (~1.5–2h)

### Task 2.1 — Active footprint → ghost sprites

**Files:**
- Modify: `Assets/FracturedChorus/UI/BeatTimelineUIView.cs` (`ShowDropGhost`, `AddDropPreviewDot` / helpers, `HideDropGhost`)

**Changes:**
1. Active beat preview: tạo Image dùng `noteVisuals.DropGhost(valid)` thay vì solid color dot (size = `GhostDisplaySize`).
2. Standing / non-Active: giữ `AddDropPreviewDot` màu như cũ.
3. Null sprite → fallback tint hiện tại.

### Task 2.2 — Cover overlays on impact notes under Active

**Files:**
- Modify: `Assets/FracturedChorus/UI/BeatTimelineUIView.cs`

**Changes:**
1. Private list `List<Image> _dropCoverOverlays` (hoặc tương đương).
2. Trong `ShowDropGhost`, sau khi biết `valid` + Active beats:
   - Query telegraphs từ `_timeline` tại `info.BeatIndex`
   - Nếu có telegraph `!IsWindupOnly` → spawn overlay Image với `noteVisuals.Cover(valid)`, size `CoverDisplaySize`, anchor tại note column (cùng X beat; Y = note row / segment note portrait nếu có helper sẵn, else beat X + enemy lane / content Y đã dùng cho note UI)
3. `HideDropGhost`: destroy/disable + clear `_dropCoverOverlays`.
4. Layer: overlays trên note, dưới hoặc cạnh Perfect chip layer — không che ScanBar; `SetAsLastSibling` trong lane/note layer phù hợp (tránh lỗi Perfect chip bị LaneMarkers đè — giữ pattern ResolveChipLayer nếu cần layer riêng `DropCoverLayer`).

**Y placement rule (chọn 1, ghi trong code rõ ràng):**
- **Preferred:** cùng Y với note portrait trên `BeatSegmentView` nếu timeline expose beat→segment rect;  
- **Fallback:** center of beat column in enemy telegraph band (document which constant used).

**Acceptance Phase 2:**
- [x] Drag skill valid → Active = ghost valid
- [x] Drag invalid → Active = ghost invalid
- [x] Active đè impact note + valid → cover Perfect; + invalid → cover Miss
- [x] Windup-only dưới Active → **không** cover Perfect/Miss
- [x] Thả / hide → overlays biến mất
- [x] Perfect resolve chip + MULTI vẫn như #1

**Commit (khi user yêu cầu):** `Show drop ghosts and drag cover overlays on timeline notes.`

---

## Phase 3 — Edit Preview + alpha polish (~0.5–1h)

### Task 3.1 — Inspector foldout

**Files:**
- Modify: `Assets/FracturedChorus/Editor/CombatPrototypeBootstrapEditor.cs` (`DrawTimeline`)

**Changes:**
- Foldout Timeline: PropertyField / ping cho 7 sprite + 3 size trên catalog (qua `BeatTimelineUIView` serialized object).
- Nút Ping từng sprite asset nếu đã gán.

### Task 3.2 — Alpha clean (nếu còn checkerboard)

**Files:**
- Process in place under `Art/UI/Combat/Timeline/**` (border-only flood-fill; preserve letter faces)

**Acceptance Phase 3:**
- [x] Edit Preview chọn/ping đủ 7 sprite + size
- [ ] PNG không còn checkerboard bake rõ trên nền transparent trong Game view *(playtest residual)*
- [x] Checklist success criteria trong design spec §6 đều pass *(runtime)*

**Closed 2026-07-16 (runtime).** Residual: alpha polish nếu còn bake. Sprites: `Resources/UI/Combat/Timeline/` + source `Art/UI/Combat/Timeline/`.

---

## Play Mode checklist (end-to-end)

1. CombatPrototype → Deploy → thấy note Red/Blue/Purple icons.
2. Drag skill lên lane: Active ghost đổi theo valid/invalid.
3. Kéo Active qua boss impact note: cover ✓ hoặc ✗ đúng validity.
4. Kéo qua windup: không cover.
5. Execute: Perfect chip / counter feel không regress.
6. Intro-pause beat 6 + Deploy/Execute buttons OK.

---

## Dependencies

| Item | Status |
|------|--------|
| Design spec approved | ✅ |
| 7 sprites under `Art/UI/Combat/Timeline/` | ✅ kept |
| `#1` Counter feel / Perfect chip | ✅ must not regress |

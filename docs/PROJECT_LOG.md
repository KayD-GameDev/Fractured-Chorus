# Project Log — Fractured Chorus

Nhật ký phiên làm việc (cách làm + lý do). Tài liệu scene/timeline chi tiết: `Assets/FracturedChorus/Scenes/SCENE_SETUP.md`.

---

## 2026-06-25 — Beat timeline: độ rộng theo giây + hiệu ứng quét + 30 phase

### Mục tiêu
- Timeline chạy **mượt** dù khoảng cách (giây) giữa các beat chênh nhau nhiều.
- Hiệu ứng chiếu sáng ô đẹp, đồng nhất cho mọi nốt.
- Chạy hết bài nhạc không bị gián đoạn; đủ máu để test.
- Giải pháp **tái sử dụng cho mọi bài hát** (data-driven), không hardcode.

### Vấn đề (root cause)
Trước đây mỗi ô beat rộng **bằng nhau**, nhưng vị trí cuộn lấy theo `musical beat × bước_pixel_cố_định`. Vì `MusicBeatMapSO.TimeToMusicalBeat` nội suy tuyến tính **trong từng** khoảng beat, tốc độ `px/giây` **nhảy bậc** tại mỗi ranh giới beat ⇒ timeline lúc nhanh lúc chậm (giật).

### Cách làm — timeline mượt bằng độ rộng theo giây
Hai thay đổi đi đôi (cần cả hai để vừa mượt vừa khớp):

1. **Độ rộng ô tỉ lệ số giây**: `width[i] = max(minSlotWidth, span_giây[i] × pixelsPerSecond)`.
2. **Cuộn tốc độ không đổi**: vị trí cuộn `S = PxOfLocalBeat(localBeat)`, với
   `PxOfLocalBeat(lb) = offset[k] + frac × width[k]` (k = phần nguyên, frac = phần lẻ của `localBeat`).
   Vì `frac` tăng tuyến tính theo giây trong mỗi beat và `width[k] = span[k] × pps`, nên
   `d(px)/d(giây) = width[k]/span[k] = pps` → **hằng số** ⇒ mượt tuyệt đối, vạch quét vẫn cắt đúng từng beat đúng thời điểm, đúng cả khi nhạc loop.

`pixelsPerSecond = slotWidth / span_trung_bình_của_bài` (giữ mật độ hiển thị ~ như cũ).

**Kiến trúc render**: bỏ "carousel ảo", chuyển sang **render đủ `TimelineConstants.TotalBeats` ô** trong một hàng dài, cuộn cả hàng, `RectMask2D` cắt viền. `HorizontalLayoutGroup.childControlWidth = true` để `LayoutElement.preferredWidth` (độ rộng theo giây) thực sự được áp.

**Tái sử dụng cho bài khác**: toàn bộ độ rộng suy ra tại runtime từ beat map của bài đang chơi. Thêm bài mới = gán CSV/`...BeatMap.asset` cho `CombatMusicController` (cơ chế `TryLoadBeatMapFromCsv` sẵn có). Không sửa code timeline. Không có beat map → tự về độ rộng đều theo `Auto Beat Interval`.

> Lưu ý nguồn dữ liệu: `CombatMusicController` nạp beat map **một lần lúc Awake**, ưu tiên `...BeatMap.asset` (nếu gán) → bỏ qua CSV. Sửa beat bằng tay phải sửa đúng nguồn đang dùng, rồi vào Play (hoặc sang round mới) để `RebuildLayout` tính lại độ rộng.

### Cách làm — hiệu ứng quét (mọi nốt)
- Cường độ sáng `0..1` theo vị trí thanh đỏ trong ô: `p = vị_trí_trong_ô / width`.
  - `p ≤ 0.5`: `intensity = SmoothStep(0,1, p/0.5)` → rìa sáng nhẹ, **tâm chớp mạnh nhất**.
  - `p > 0.5`: mục tiêu = 0 → **tắt dần**.
- Làm mượt theo thời gian trong `BeatSegmentView.Update()` bằng `MoveTowards`:
  - Sáng lên giới hạn bởi `scanFadeInDuration` (mặc định 0.08s) → nốt ngắn không "pop".
  - Tắt đi theo `scanFadeOutDuration` (mặc định 0.35s) → tắt chậm rãi, vẫn mờ dần kể cả khi thanh đỏ đã rời ô.
- Nốt dài không đổi cảm giác: mục tiêu thay đổi chậm hơn giới hạn thời gian nên độ sáng bám theo vị trí (tăng dần đẹp).

### Cách làm — 30 phase & máu test
- `TimelineConstants`: `PhaseCount = 30`, `TotalBeats = Phase1SlotCount + (PhaseCount-1) × LaterPhaseSlotCount = 480` (16 beat/phase). `PhaseAvTracker` tính ngân sách bằng công thức nên tăng phase an toàn.
- Charlotte = unit **Tank**: máu 3000 ở `StatBlock_Tank.asset` (nguồn thực tế vì preset có gán statBlock) và `UnitStats.CreateTankPreset()` (đường fallback).

### File đã chạm
- `Assets/FracturedChorus/Audio/MusicBeatMapSO.cs` — thêm `GetBeatSpanSec`, `AverageBeatSpanSec`.
- `Assets/FracturedChorus/Audio/CombatMusicController.cs` — expose `BeatMap`.
- `Assets/FracturedChorus/UI/BeatTimelineUIView.cs` — render-all + độ rộng theo giây + cuộn theo offset tích lũy + chiếu sáng theo vị trí.
- `Assets/FracturedChorus/UI/BeatSegmentView.cs` — cường độ sáng liên tục + fade-in/out theo thời gian.
- `Assets/FracturedChorus/Combat/Timeline/TimelineConstants.cs` — 30 phase / 480 beat.
- `Assets/FracturedChorus/Resources/StatBlocks/StatBlock_Tank.asset`, `Assets/FracturedChorus/Combat/Units/UnitStats.cs` — Tank 3000 HP.

### Tham số tinh chỉnh (Inspector)
- `BeatTimelineUIView`: `Slot Width` (độ rộng trung bình mục tiêu), `Min Slot Width`, `Auto Beat Interval`, `Skill Panel Open Speed Multiplier`.
- `BeatSegmentView`: `Scan Scale Boost`, `Scan Fade In Duration`, `Scan Fade Out Duration`.

### Còn cần kiểm (chưa chạy Unity trong phiên này)
- Vào Play scene `CombatPrototype`: xác nhận timeline mượt, hiệu ứng quét, 480 beat trong first-pass nhạc (`firstPassEndSec = 244.8s`) để tránh musical beat nhảy lùi lúc loop, Tank = 3000 HP.

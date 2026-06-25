# CombatPrototype — Scene setup

Logic nằm trong **MonoBehaviour `.cs`**. Layout chỉnh trực tiếp trong **Hierarchy** (kéo Transform, RectTransform, anchor UI).

> **Ghi chú cập nhật (2026-06):** Beat timeline dùng **quét liên tục trên track ngang** (conveyor + sweep), không còn nhảy từng ô / dừng–trượt rời. Chi tiết mục [Beat Timeline UI](#beat-timeline-ui-quét-liên-tục).

---

## Cách tạo hierarchy (khuyến nghị)

1. Mở scene combat (hoặc scene mới).
2. Menu **Fractured Chorus → Setup Combat Scene Hierarchy**.
3. Unity tạo `CombatRoot` với cấu trúc:

```
CombatRoot          ← CombatPrototypeBootstrap + CombatController
├── World
│   ├── Grid
│   │   ├── PlayerGrid/Cell_Player_R0_C0 …
│   │   └── EnemyGrid/Cell_Enemy_R0_C0 …
│   └── Units
│       ├── Unit_Ren
│       ├── Unit_Tank
│       ├── Unit_Mage
│       └── Unit_Grunt …
└── CombatCanvas
    ├── BeatTimelineUI    ← kéo anchor, resize bar
    └── SkillPanelUI      ← vị trí mặc định; runtime follow unit
EventSystem
Main Camera
```

4. **Save scene** → `Assets/FracturedChorus/Scenes/CombatPrototype.unity`
5. Add scene vào **Build Settings**
6. **Play**

---

## Chỉnh layout trong Editor

| Object | Chỉnh gì |
|--------|----------|
| `World/Grid/.../Cell_*` | Vị trí ô lưới 3×3 (kéo Transform) |
| `World/Units/Unit_*` | Vị trí nhân vật trên sân |
| `BeatTimelineUI` | Anchor, chiều cao bar, vị trí trên màn hình |
| `BeatTimelineUI/Viewport/ScrollContent` | Spacing ô beat (`HorizontalLayoutGroup`) |
| `BeatTimelineUI/Viewport/ScrollContent/Beat_0` | Template ô beat (clone thêm lúc runtime theo chiều rộng viewport) |
| `SkillPanelUI` | Size, pivot; panel follow unit khi Play |

**UnitView Inspector:** `Side`, `Row`, `Column` = logic combat (0–2). Transform = vị trí hiển thị — có thể tách riêng khi chỉnh layout.

**GridCellMarker:** honeycomb hex (`Board margin.drawio`). Hàng **1–3** (vàng), cột **1–3** (đỏ): player C1 = phải/front; enemy C1 = trái/front (board mirror). Drop → **neon xanh**.

**UnitView:** row/column = **-1** trong scene; runtime gán khi đặt lên ô. Mặc định demo: **H2** — Tank C1, Ren C2, Mage C3.

---

## Beat Timeline UI — quét liên tục

### Hierarchy trong Viewport

```
BeatTimelineUI
├── Header          (PhaseLabel, Budget, AvLabel — optional)
└── Viewport        (RectMask2D)
    ├── TrackLine       ← đường track ngang (mờ); tạo sẵn khi Rebuild, hoặc runtime tự tạo
    ├── ScrollContent   ← hàng ô beat; scroll ngang liên tục khi Play
    │   └── Beat_0      ← template; thêm BeatSlot_1…N lúc runtime
    └── ScanBar         ← vạch quét đỏ; trượt dọc track, không nhảy từng ô
```

### Độ rộng ô theo thời gian thực (smooth scroll)

> **Cập nhật (2026-06):** Độ rộng mỗi ô beat **tỉ lệ với số giây** của khoảng beat đó (`width = span_giây × pixelsPerSecond`), không còn các ô đều nhau. Nhờ vậy:
>
> - Vùng beat **ngắn liên tục** → ô **hẹp, sát nhau**; vùng beat **dài** → ô **rộng ra** ⇒ timeline phản ánh đúng nhịp bài.
> - Scroll được lái bằng `musical beat → offset tích lũy`, nên tốc độ **px/giây không đổi** ⇒ chạy **mượt**, vẫn khớp nhạc và đúng cả khi nhạc loop.
> - **Data-driven**: độ rộng suy ra từ `MusicBeatMapSO` của bài đang chơi (`GetBeatSpanSec`, `AverageBeatSpanSec`). Đổi bài khác chỉ cần gán beat map/CSV cho `CombatMusicController` — không sửa code timeline. Không có beat map → tự về độ rộng đều theo `Auto Beat Interval`.

### Hành vi khi Play (prototype hiện tại)

| Giai đoạn | Visual | Logic combat |
|-----------|--------|----------------|
| **Đầu timeline** | `TrackLine` + `ScanBar` cố định ở mốc `slotWidth/2`; content trôi trái | Beat resolve khi `localBeat` chạm chỉ số nguyên (rìa trái ô) |
| **Cuối timeline** | Content dừng; `ScanBar` trượt phải | Cùng crossing detection |
| **Mở skill panel** | Tốc độ quét **0.25×** (scroll + nhạc nếu `useMusicSync`) | Không đổi |

**Hiệu ứng chiếu sáng ô (mọi nốt giống nhau):** thanh đỏ vừa chạm **rìa** ô → ô sáng nhẹ → tới **tâm** chớp mạnh nhất (`SmoothStep`) → qua tâm thì **tắt dần** theo thời gian. Cả lúc sáng lên và tắt đi đều được làm mượt theo thời lượng (tránh "pop" ở nốt ngắn). Việc tắt dần vẫn tiếp tục kể cả khi thanh đỏ đã rời ô.

**Inspector `BeatTimelineUIView` (tuning):**

| Field | Ý nghĩa |
|-------|---------|
| `Slot Width` | **Độ rộng ô trung bình mục tiêu** → suy ra `pixelsPerSecond = slotWidth / span_trung_bình`. Tăng = cả timeline giãn rộng, chênh lệch ngắn/dài rõ hơn |
| `Min Slot Width` | Độ rộng tối thiểu của ô (beat quá ngắn không bị mảnh quá; mặc định 14) |
| `Auto Beat Interval` | Fallback khi **không có** beat map → scroll đều theo interval này |
| `Skill Panel Open Speed Multiplier` | Hệ số chậm khi panel skill mở (mặc định 0.25) |

**Inspector `BeatSegmentView` (hiệu ứng quét):**

| Field | Ý nghĩa |
|-------|---------|
| `Scan Scale Boost` | Hệ số phóng to ô lúc chớp sáng (mặc định 1.14) |
| `Scan Fade In Duration` | Thời lượng sáng lên tối thiểu (mặc định 0.08s; tăng nếu nốt ngắn còn giật) |
| `Scan Fade Out Duration` | Thời lượng tắt dần (mặc định 0.35s; tăng = tắt chậm rãi hơn) |

**Runtime tạo đủ `TimelineConstants.TotalBeats` ô** (render-all, không còn carousel ảo): hàng `ScrollContent` dài hơn viewport và được `RectMask2D` cắt viền. `HorizontalLayoutGroup` để `childControlWidth = true` để áp `preferredWidth` (độ rộng theo giây) cho từng ô.

**Scene cũ thiếu `TrackLine`:** vẫn chạy; `BeatTimelineUIView` tự tạo `TrackLine` lúc Play. Rebuild UI để có sẵn trong scene.

### Phase divider

Vạch trắng **PhaseDivider** trên từng ô sau beat 15, 31, 47… (mỗi **16 beat** một phase). Hiện cấu hình **30 phase × 16 = 480 beat** (`TimelineConstants.PhaseCount` / `TotalBeats`). Trôi qua vạch quét cùng content — không cần setup thêm.

---

## Fallback

Nếu xóa hết `Units` khỏi scene, bootstrap spawn từ `EncounterDefinitionSO` (hoặc demo runtime) — vị trí tính bằng code, không chỉnh Hierarchy được.

---

## Không làm

- Gắn logic combat qua UnityEvent trên scene.
- Sửa `.unity` YAML tay — dùng Editor menu + Inspector.
- Gắn logic scroll/resolve beat bằng Animation clip trên scene — do `BeatTimelineUIView` + `CombatSession` xử lý.

**Input System:** Project dùng **Input System Package** — EventSystem phải có `Input System UI Input Module` (không dùng Standalone Input Module). Nếu lỗi 999+ `InvalidOperationException` về Input: menu **Fractured Chorus → Fix Input System (EventSystem)** hoặc Play (bootstrap tự sửa).

---

## Rebuild UI (timeline + skill panel)

Menu **Fractured Chorus → Rebuild Hex Board Grid (scene)** — cập nhật vị trí honeycomb + viền hex (từ `Board margin.drawio`).

Menu **Fractured Chorus → Rebuild Timeline + Skill Panel (Hierarchy)** — tạo lại:

- `BeatTimelineUI/Viewport/ScrollContent/Beat_0` (template ô beat)
- `BeatTimelineUI/Viewport/TrackLine` (đường track ngang)
- `BeatTimelineUI/Viewport/ScanBar` (vạch quét đỏ trên track)
- `SkillPanelUI` (ẩn mặc định, hiện khi click unit)

Save scene sau khi rebuild.

> **Lưu ý:** Rebuild (editor) chỉ tạo template `Beat_0`. Lúc Play, runtime mới clone đủ `TotalBeats` ô (`BeatSlot_1…N`) với độ rộng theo giây — không cần các object beat trong Hierarchy.

---

## Regenerate

Menu **Fractured Chorus → Setup Combat Scene Hierarchy** → chọn **Tạo lại** (xóa CombatRoot cũ). **Save scene trước** nếu đã chỉnh layout quan trọng.

---

## Changelog (scene / timeline UI)

| Ngày | Thay đổi |
|------|----------|
| 2026-06-25 | **Độ rộng ô theo giây**: `width = span × pixelsPerSecond` (data-driven từ `MusicBeatMapSO`); scroll lái bằng musical beat → px/giây không đổi (mượt, khớp nhạc). Render-all `TotalBeats` ô + `RectMask2D`; `childControlWidth = true`. |
| 2026-06-25 | **Hiệu ứng quét nâng cấp** (mọi nốt): rìa → tâm chớp (`SmoothStep`) → tắt dần theo thời gian; làm mượt cả fade-in/out (`scanFadeInDuration` / `scanFadeOutDuration`). Bỏ `scanAlignThreshold`. |
| 2026-06-25 | **30 phase** (`PhaseCount = 30`, `TotalBeats = 480`) để chạy hết bài không gián đoạn. Charlotte (Tank) máu = 3000 để test (`StatBlock_Tank` + `UnitStats.CreateTankPreset`). |
| 2026-06-23 | Skill panel mở: `SetSkillPanelOpen` → scroll 0.25× + `CombatMusicController` pitch 0.25× (music sync). |
| 2026-06 | Timeline: scroll liên tục trên `TrackLine`; **chỉ ô đang nằm dưới vạch đỏ** nổi lên (`BeatSegmentView.SetScanHighlighted`), các ô khác giữ nguyên. |
| — | Combat flow vẫn prototype (Planning + auto-resolve trong một lần quét) — chưa tách UC-04 Planning dừng / Resolution riêng. |

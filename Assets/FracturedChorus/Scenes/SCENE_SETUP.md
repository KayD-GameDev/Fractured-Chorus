# CombatPrototype — Scene setup

Logic nằm trong **MonoBehaviour `.cs`**. Layout chỉnh trực tiếp trong **Hierarchy** (kéo Transform, RectTransform, anchor UI).

## Quy tắc Scene = Source of Truth

**Mọi GameObject trong scene** (unit, ô lưới, UI, EXECUTE button, camera…) — nếu bạn đổi vị trí, màu, scale, active/inactive trong Hierarchy rồi **Save scene**, thì **Play phải hiển thị y hệt**.

| Được phép lúc Play | Không được (trừ khi tắt Respect Scene Authoring) |
|--------------------|---------------------------------------------------|
| Gắn logic combat (HP, grid index, timeline data) | Snap lại Transform unit/ô lưới theo công thức |
| Clone beat slot từ template `Beat_0` (carousel) | Rebuild màu hex / bật lại child đã tắt |
| Ẩn/hiện UI theo phase gameplay (EXECUTE, skill panel) | Ép màu placeholder lên sprite đã chỉnh |

**Inspector flags (mặc định bật):**
- `CombatPrototypeBootstrap` → **Respect Scene Authoring**
- `GridCellMarker` → **Preserve Scene Visuals**
- `UnitView` → **Preserve Scene Visuals**
- `BeatTimelineUIView` / `SkillPanelUIView` → **Preserve Scene Layout** (chỉ khung ngoài / Header; carousel vẫn auto-layout TrackLine + ScrollContent)

**Rebuild layout:** chỉ dùng menu **Fractured Chorus → Setup / Rebuild…**, không tự chạy khi Play.

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

**UnitView Inspector:** `Side`, `Row`, `Column` = logic combat (index **0–2** = hàng/cột hiển thị **1–3**). **Scene là nguồn sự thật:** giá trị Inspector + Transform trong Hierarchy phải khớp Game khi Play — bootstrap không ghi đè formation mặc định nếu đã gán `Row`/`Column` hợp lệ.

**GridCellMarker:** honeycomb hex (`Board margin.drawio`). Hàng **1–3** (vàng), cột **1–3** (đỏ): player C1 = phải/front; enemy C1 = trái/front (board mirror). Drop → **neon xanh**. Tắt/đổi màu child `Hexagon Flat Top` trong scene → giữ nguyên khi Play.

**UnitView:** `Body Collider` = `BoxCollider2D` (click/drag). Con `FeetAnchor` (child) + `BoxCollider2D` nhỏ = điểm chân snap vào tâm ô — kéo `FeetAnchor` trong scene để chỉnh. Menu **Migrate Unit Colliders (2D + Feet)** nếu scene còn `BoxCollider` 3D.

**Input:** Main Camera cần `Physics2DRaycaster` (menu **Fix Input System**).

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

### Hành vi khi Play (prototype hiện tại)

| Giai đoạn | Visual | Logic combat |
|-----------|--------|----------------|
| **Đầu timeline** | `TrackLine` + `ScanBar` cố định; content trôi trái; ô dưới vạch quét **nổi lên** | Beat resolve khi vạch quét cắt qua tâm ô |
| **Cuối timeline** | Content dừng; `ScanBar` trượt phải; ô dưới vạch quét **nổi lên** | Cùng crossing detection |
| **Mở skill panel** | Tốc độ quét **0.25×** (scroll + nhạc nếu `useMusicSync`) | Không đổi |

**Inspector `BeatTimelineUIView` (tuning):**

| Field | Ý nghĩa |
|-------|---------|
| `Auto Beat Interval` | Thời gian 1 beat → tốc độ scroll = `slotStep / interval` |
| `Skill Panel Open Speed Multiplier` | Hệ số chậm khi panel skill mở (mặc định 0.25) |
| `Slot Width` / spacing | Fallback nếu chưa refit viewport |
| `Scan Align Threshold` | Ngưỡng khớp tâm ô với vạch đỏ (0.28 × slot step); nhỏ hơn = chỉ nổi khi quét trúng |

**Không cần** 105 GameObject beat trong Hierarchy — runtime chỉ giữ **N ô vừa viewport** (carousel ảo), populate nội dung beat 0…104.

**Scene cũ thiếu `TrackLine`:** vẫn chạy; `BeatTimelineUIView` tự tạo `TrackLine` lúc Play. Rebuild UI để có sẵn trong scene.

### Phase divider

Vạch trắng **PhaseDivider** trên từng ô sau beat 14, 24, 34… (giữa phase timeline). Trôi qua vạch quét cùng content — không cần setup thêm.

---

## Fallback

Nếu xóa hết `Units` khỏi scene, bootstrap spawn từ `EncounterDefinitionSO` (hoặc demo runtime) — vị trí tính bằng code, không chỉnh Hierarchy được.

---

## Không làm

- Gắn logic combat qua UnityEvent trên scene.
- Sửa `.unity` YAML tay — dùng Editor menu + Inspector.
- Gắn logic scroll/resolve beat bằng Animation clip trên scene — do `BeatTimelineUIView` + `CombatSession` xử lý.

**Input System:** Project dùng **Input System Package** — EventSystem phải có `Input System UI Input Module` (không dùng Standalone Input Module). Nếu lỗi 999+ `InvalidOperationException` về Input: menu **Fractured Chorus → Fix Input System (EventSystem)** hoặc Play (bootstrap tự sửa).

**Một lần áp dụng mọi cập nhật play-ready:** **Fractured Chorus → Apply All Play-Ready Updates** — sửa EventSystem + Physics2DRaycaster, migrate BoxCollider2D + FeetAnchor, restore sprite từ preset, bật `respectSceneAuthoring` / `preserveSceneVisuals`, refit timeline viewport, **Save scene**. Chạy sau khi pull code mới hoặc khi Play không khớp Hierarchy.

---

## Rebuild UI (timeline + skill panel)

Menu **Fractured Chorus → Rebuild Hex Board Grid (scene)** — cập nhật vị trí honeycomb + viền hex (từ `Board margin.drawio`).

Menu **Fractured Chorus → Rebuild Timeline + Skill Panel (Hierarchy)** — tạo lại:

- `BeatTimelineUI/Viewport/ScrollContent/Beat_0` (template ô beat)
- `BeatTimelineUI/Viewport/TrackLine` (đường track ngang)
- `BeatTimelineUI/Viewport/ScanBar` (vạch quét đỏ trên track)
- `SkillPanelUI` (ẩn mặc định, hiện khi click unit)

Save scene sau khi rebuild.

> **Lưu ý:** Rebuild **không** tạo 105 object `Beat_1…Beat_104` — số ô hiển thị do code fit theo chiều rộng viewport.

---

## Regenerate

Menu **Fractured Chorus → Setup Combat Scene Hierarchy** → chọn **Tạo lại** (xóa CombatRoot cũ). **Save scene trước** nếu đã chỉnh layout quan trọng.

---

## Changelog (scene / timeline UI)

| Ngày | Thay đổi |
|------|----------|
| 2026-06-23 | Skill panel mở: `SetSkillPanelOpen` → scroll 0.25× + `CombatMusicController` pitch 0.25× (music sync). |
| 2026-06 | Timeline: scroll liên tục trên `TrackLine`; **chỉ ô đang nằm dưới vạch đỏ** nổi lên (`BeatSegmentView.SetScanHighlighted`), các ô khác giữ nguyên. |
| — | Combat flow vẫn prototype (Planning + auto-resolve trong một lần quét) — chưa tách UC-04 Planning dừng / Resolution riêng. |

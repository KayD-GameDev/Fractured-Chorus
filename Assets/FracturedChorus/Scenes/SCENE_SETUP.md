# CombatPrototype — Scene setup

Logic nằm trong **MonoBehaviour `.cs`**. Layout chỉnh trực tiếp trong **Hierarchy** (kéo Transform, RectTransform, anchor UI).

## Quy tắc Scene = Source of Truth

**Mọi GameObject trong scene** (unit, ô lưới, UI, EXECUTE button, camera…) — nếu bạn đổi vị trí, màu, scale, active/inactive trong Hierarchy rồi **Save scene**, thì **Play phải hiển thị y hệt**.

**Quy tắc bổ sung (2026-06-27):**
- **Không code trên scene** — logic combat/UI chỉ trong `.cs`; không UnityEvent wiring phức tạp, không Visual Scripting gameplay.
- **GameObject phải hiện trong Hierarchy** — UI/combat object phải thấy được trong Editor (ví dụ `Card_Mage`, `Card_Ren`, `Card_Tank` dưới `CardsRow`); runtime **không** spawn ẩn rồi dịch layout khi `preserveSceneLayout` bật.

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
- `PartyStatusBarUIView` → anchor bar góc trái trên trên **CombatCanvas**; thẻ `Card_*` nằm sẵn trong **CardsRow** (Hierarchy-first); `preserveSceneLayout` — Play chỉ bind, không dịch thẻ

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
    ├── PartyStatusBarUI   ← góc trái trên; CardsRow chứa Card_Mage / Card_Ren / Card_Tank + CardTemplate (inactive)
    ├── BeatTimelineUI    ← kéo anchor, resize bar
    └── SkillPanelUI      ← vị trí mặc định; runtime follow unit
Background canvas         ← sibling của CombatCanvas; chỉ chứa ảnh nền (không đặt combat UI)
EventSystem
Main Camera
```

4. **Save scene** → `Assets/FracturedChorus/Scenes/CombatPrototype.unity`
5. Add scene vào **Build Settings**
6. **Play**

---

## CombatTutorial — scene riêng (BG + enemy authoring)

Clone của CombatPrototype để chỉnh tutorial Cadence **không đụng** boss scene.

| Mục | Cách chỉnh |
|-----|------------|
| Scene | `Assets/FracturedChorus/Scenes/CombatTutorial.unity` |
| Mở | Menu **Fractured Chorus → Open Combat Tutorial Scene** |
| BG | `Background canvas/Image` — smoke-war front (`cadence_smoke_war_front_bg_v1`) |
| Quái | `World/Units/Unit_Kiki_Ueda` — Kiki Lv1 Elite; grunts inactive |
| Party | Ren + Mage (Coda) active; Tank + Boss inactive mặc định |
| Runtime | `tutorialSceneMode` = true → **không** overwrite BG/enemy từ handoff; vẫn ép basic skills + coach Coda |
| Test | CampusHub → **Tutorial Fight**, hoặc Play trực tiếp scene |

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
| `PartyStatusBarUI` | Anchor góc trái trên; Mage tại **(0,0)**, mỗi thẻ +**100px** X; badge offset **(-4,-4)** |
| `PartyStatusBarUI/CardsRow` | Chỉ `CardTemplate` (inactive) trong Edit mode — lúc Play runtime **clone** theo số unit |
| `PartyStatusBarUI/CardTemplate` | Model thẻ — **inactive**; con: `Border`, `Avatar`, `HealthBarBg`, `ElementBadge/ElementIcon` |

**Party status bar:** Phải nằm dưới **CombatCanvas**. Thứ tự thẻ **theo formation** (hàng 2 ưu tiên → cột front → PartyBarOrder); refresh sau kéo đổi ô. Spacing **100px** giữa thẻ (Mage index 0 tại x=0).

**UnitView Inspector:** `Side`, `Row`, `Column` = logic combat (index **0–2** = hàng/cột hiển thị **1–3**). **Scene là nguồn sự thật:** giá trị Inspector + Transform trong Hierarchy phải khớp Game khi Play — bootstrap không ghi đè formation mặc định nếu đã gán `Row`/`Column` hợp lệ.

**GridCellMarker:** honeycomb hex (`Board margin.drawio`). Hàng **1–3** (vàng), cột **1–3** (đỏ): player C1 = phải/front; enemy C1 = trái/front (board mirror). Drop → **neon xanh**. Tắt/đổi màu child `Hexagon Flat Top` trong scene → giữ nguyên khi Play.

**UnitView:** `Body Collider` = `BoxCollider2D` trên **unit root** (click/drag). Con `FeetAnchor` = Transform snap grid — **không collider** (tránh cướp raycast). Menu **Migrate Unit Colliders (2D + Feet)** hoặc **Apply All Play-Ready Updates** nếu scene còn collider cũ.

**EXECUTE:** `CombatCanvas/ExecuteOverlay` — chỉ hiện trước round; bấm để bắt đầu quét timeline.

**Input:** Main Camera cần `Physics2DRaycaster` (`maxRayIntersections = 32`); menu **Fix Input System** / **Apply All**.

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

Vạch trắng **PhaseDivider** trên từng ô sau beat 15, 31, 47… (mỗi **16 beat** một phase). **`TimelineConstants.TotalBeats = 619`** — khớp `MusicBeatMapSO` Eternal Spark (618 CSV marker + pad t=0). **39 phase** (phase cuối 11 beat).

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

> **Lưu ý:** Rebuild (editor) chỉ tạo template `Beat_0`. Lúc Play, runtime mới clone đủ `TotalBeats` ô (`BeatSlot_1…N`) với độ rộng theo giây — không cần các object beat trong Hierarchy.

---

## Regenerate

Menu **Fractured Chorus → Setup Combat Scene Hierarchy** → chọn **Tạo lại** (xóa CombatRoot cũ). **Save scene trước** nếu đã chỉnh layout quan trọng.

---

## Changelog (scene / timeline UI)

| Ngày | Thay đổi |
|------|----------|
| 2026-06-27 | **Party bar hierarchy-first:** `Card_Mage/Ren/Tank` trong `CardsRow`; spacing **1.25px**; Tank ngoài cùng phải; `preserveSceneLayout` — không dịch thẻ khi di chuyển unit; icon hệ art (`icon_he_*`). Rule: không code scene + object phải hiện Hierarchy. |
| 2026-06-26 | **Party status bar:** clone từ `CardTemplate` (max 5); spacing 1px; thứ tự Mage→Ren→Tank; badge hệ tròn (bỏ `RoleBadge`); swap formation refresh bar; menu Find/Remove Missing Scripts. |
| 2026-06-25 | **Độ rộng ô theo giây**: `width = span × pixelsPerSecond` (data-driven từ `MusicBeatMapSO`); scroll lái bằng musical beat → px/giây không đổi (mượt, khớp nhạc). Render-all `TotalBeats` ô + `RectMask2D`; `childControlWidth = true`. |
| 2026-06-25 | **Hiệu ứng quét nâng cấp** (mọi nốt): rìa → tâm chớp (`SmoothStep`) → tắt dần theo thời gian; làm mượt cả fade-in/out (`scanFadeInDuration` / `scanFadeOutDuration`). Bỏ `scanAlignThreshold`. |
| 2026-06-25 | **39 phase** · `TotalBeats = 619` (Eternal Spark beat map) — chạy hết bài không cắt sớm @ 480. |
| 2026-06-25 | **Scene-first:** `respectSceneAuthoring`, `preserveSceneCollider`; `UnitFeetAnchor` (Transform only); input `Physics2DRaycaster`. Drag pre-EXECUTE / click post-EXECUTE qua `BoardDragController`. Import sprite Ren/Tank/Mage. |
| 2026-06-24 | **EXECUTE** overlay — khóa timeline/skill trước round; `AllowPlayerReposition` gate. Stat blocks + Resources presets. UI skill rename Skill 1/2. |
| 2026-06-23 | Skill panel mở: `SetSkillPanelOpen` → scroll 0.25× + `CombatMusicController` pitch 0.25× (music sync). |
| 2026-06 | Timeline: scroll liên tục trên `TrackLine`; **chỉ ô đang nằm dưới vạch đỏ** nổi lên (`BeatSegmentView.SetScanHighlighted`), các ô khác giữ nguyên. |
| — | Combat flow vẫn prototype — chưa Morale/Affliction, Interrupt đầy đủ. |

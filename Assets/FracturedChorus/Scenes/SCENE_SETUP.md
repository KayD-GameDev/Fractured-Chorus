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

## CombatTutorial — scene riêng (text + ảnh step)

Clone của CombatPrototype để chỉnh tutorial Cadence **không đụng** boss scene.  
Copy/step SoT: `docs/tutorial/TUTORIAL_COPY.md`.

| Mục | Cách chỉnh |
|-----|------------|
| Scene | `Assets/FracturedChorus/Scenes/CombatTutorial.unity` |
| Mở / Prepare | **Fractured Chorus → Open / Prepare Combat Tutorial Scene** (Prepare xóa legacy TutorialEditCanvas) |
| BG | `Background canvas/Image` — `cadence_smoke_war_front_bg_v1` |
| Quái | `World/Units/Unit_Kiki_Ueda` — Kiki Lv1 Elite; grunts inactive |
| Party | Ren + Mage (Coda) active; Tank + Boss inactive mặc định |
| Runtime | `tutorialSceneMode` = true; `TutorialDirector` chạy text VI + Coda chibi; bấm Next |
| Ảnh step (optional) | `Art/UI/Tutorial/Steps/{stepId}_v1.png` — thiếu file = chỉ text |
| Exit | Victory → `tutorial_cadence_intro_done` + RunMap; chết → reload scene |
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

### Planning window — unit click vs drag

- **Short click** (move ≤ `clickDragThresholdPx`, default 8px) → open skill panel.
- **Drag** past threshold → reposition / swap on player grid.
- Both only while `CombatSession.IsPlanningWindowOpen`.
- Gesture math: `BoardPointerGesture` · wiring: `BoardDragController`.

---

## Beat Timeline UI — quét liên tục

### Hierarchy trong Viewport

```
BeatTimelineUI
├── Header          (PhaseLabel, Budget, AvLabel — optional)
├── LaneAvatarGutter
│   ├── AvatarColumnBackground
│   └── LaneAvatar_0..3   ← size/X Hierarchy; Play sync Y qua world→gutter (không copy Y local LaneLines)
└── Viewport        (RectMask2D)
    ├── TrackLine
    ├── ScrollContent
    │   ├── Beat_0 / Beat_1  ← template/seed; Play SetActive(false)
    │   └── BeatSlot_0…N     ← clone runtime từ Beat_0
    ├── ScanBar         ← rect + sibling order từ Hierarchy (preserveSceneLayout; không SetAsLastSibling)
    ├── LaneLines       ← Top=15, Bottom=-15; vẽ trên ScanBar
    │   └── Lane_0..3
    ├── BossTrackFrame
    │   └── BorderTop   ← note rail @ Y=215
    ├── BossNoteClusterLayer
    │   └── NoteSingle_* seed: Edit preview only; Play destroy + chỉ spawn từ telegraph
    └── LaneMarkers / LaneFootprint  ← Y neo theo Lane_* (world→layer), không dùng công thức riêng
```

`LaneAvatar_*` → child `FrameRing` (Image): Play gán `laneAvatarRingSprite` vào object này — không tạo ring cứng trong code.

Menu **Fractured Chorus → Seed Timeline Lane Preview (Hierarchy)** seed từ `Resources/UnitPresets` (Ren/Tank/Mage). Play **bind** preset lên shell scene; ScanBar/LaneAvatarGutter giữ Hierarchy. `Lane_0` = Character Line 1. Chọn `NoteSingle_1` → Inspector **Remaining Hits**.

### Độ rộng ô — khóa theo CombatTutorial

> **Cập nhật (2026-08-01):** Beat **chia đều**. Độ rộng ô **không** còn suy từ span giây biến thiên.
>
> **Canonical lock:** `TimelineLayoutLock` (đọc từ `CombatTutorial.unity` → `Beat_0`):
>
> | Hằng | Giá trị | Nguồn |
> |------|---------|--------|
> | `SlotWidth` | **73.85** | `Beat_0.sizeDelta.x` + `LayoutElement.preferredWidth` |
> | `MinSlotWidth` | 14 | Inspector |
> | `ScanBarWidth` | 6 | ScanBar |
> | `LaneMarkerSize` | 26 | Inspector |
> | Khung `BeatTimelineUI` | Anchor `(0.02,0.02)–(0.98,0.22277778)` · posY `69.4` · sizeDeltaY `138.8` | `CombatTutorial` — **Prototype phải khớp** |
>
> Runtime (`preserveSceneLayout = true`): `ResolveLockedSlotWidth()` = `max(template, TimelineLayoutLock.SlotWidth)` — **không bao giờ co nhỏ hơn 73.85**, kể cả khi field `slotWidth` scene bị lệch (ví dụ còn 52).
>
> Đổi kích thước ô / khung: sửa trong **CombatTutorial**, cập nhật `TimelineLayoutLock`, sync sang `CombatPrototype`. Không thu khung ad-hoc.

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
| `Slot Width` | Độ rộng mỗi ô beat (uniform). **Khóa ≥ 73.85** qua `TimelineLayoutLock` |
| `Min Slot Width` | Sàn tuyệt đối (mặc định 14) — không dùng để co ô dưới lock |
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

Vạch trắng **PhaseDivider** sau beat 21, 43, 65… (mỗi **22 beat** một phase). **`TimelineConstants.TotalBeats = 677`** — Boss Remix 152 BPM. **30 phase**. Mỗi Execute đủ **22 beat** mới Planning. Intro **12 beat** không nốt; hết intro mới spawn phase 1–3.

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

Menu **Fractured Chorus → Seed Timeline Lane Preview (Hierarchy)** — seed edit-mode từ UnitPresetSO:

- Note rail `BossTrackFrame/BorderTop` @ **Y=215**
- `NoteSingle_0` size **52.95 × 67.24** (bụng neo BorderTop)
- `Lane_0..3` + `LaneAvatar_0..3` (max 4; Play **dàn đều** Y theo số party sống; footprint/avatar neo theo `Lane_*`)
- Avatar slot **42×42** (`leftRailLayout.avatarSlotSize`)

Save scene sau khi rebuild/seed.

> **Lưu ý:** Rebuild (editor) chỉ tạo template `Beat_0`/`Beat_1`. Lúc Play: ẩn template; clone `BeatSlot_0…N`; ẩn note seed trên `BossNoteClusterLayer`; ScanBar giữ draw order Scene (dưới `LaneLines`).

---

## Regenerate

Menu **Fractured Chorus → Setup Combat Scene Hierarchy** → chọn **Tạo lại** (xóa CombatRoot cũ). **Save scene trước** nếu đã chỉnh layout quan trọng.

---

## Changelog (scene / timeline UI)

| Ngày | Thay đổi |
|------|----------|
| 2026-08-06 | **Timeline rail layout:** BorderTop Y=215; note single 52.95×67.24; max 4 party lanes aesthetic stretch; avatar 42×42. |
| 2026-08-05 | **Timeline lanes preset-bind:** note belly → `BorderTop`; bỏ Fill; BorderBottom → `Lane_0`; Play bind `UnitPresetSO.timelineLaneColor` + `timelineAvatarSprite`; seed từ Resources presets. |
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

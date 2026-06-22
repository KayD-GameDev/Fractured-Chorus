# CombatPrototype — Scene setup

Logic nằm trong **MonoBehaviour `.cs`**. Layout chỉnh trực tiếp trong **Hierarchy** (kéo Transform, RectTransform, anchor UI).

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
    └── SkillPanelUI      ← vị trí mặc định; runtime follow unit nếu bật
EventSystem
Main Camera
```

4. **Save scene** → `Assets/FracturedChorus/Scenes/CombatPrototype.unity`
5. Add scene vào **Build Settings**
6. **Play**

## Chỉnh layout trong Editor

| Object | Chỉnh gì |
|--------|----------|
| `World/Grid/.../Cell_*` | Vị trí ô lưới 3×3 (kéo Transform) |
| `World/Units/Unit_*` | Vị trí nhân vật trên sân |
| `BeatTimelineUI` | Anchor, chiều cao bar, vị trí trên màn hình |
| `BeatTimelineUI/Segments/Beat_*` | Layout Group spacing (parent `Segments`) |
| `SkillPanelUI` | Size, pivot; tắt **Follow Selected Unit** nếu muốn cố định vị trí |

**UnitView Inspector:** `Side`, `Row`, `Column` = logic combat (0–2). Transform = vị trí hiển thị — có thể tách riêng khi chỉnh layout.

**GridCellMarker:** chỉ visual; không bắt buộc sync với unit.

## Fallback

Nếu xóa hết `Units` khỏi scene, bootstrap spawn từ `EncounterDefinitionSO` (hoặc demo runtime) — vị trí tính bằng code, không chỉnh Hierarchy được.

## Không làm

- Gắn logic combat qua UnityEvent trên scene.
- Sửa `.unity` YAML tay — dùng Editor menu + Inspector.

**Input System:** Project dùng **Input System Package** — EventSystem phải có `Input System UI Input Module` (không dùng Standalone Input Module). Nếu lỗi 999+ `InvalidOperationException` về Input: menu **Fractured Chorus → Fix Input System (EventSystem)** hoặc Play (bootstrap tự sửa).

## Rebuild UI (105 beat timeline + skill panel)

Menu **Fractured Chorus → Rebuild Timeline + Skill Panel (Hierarchy)** — tạo lại:
- `BeatTimelineUI/Viewport/ScrollContent/Beat_0…Beat_104` (105 ô)
- Thanh đỏ **ScanBar** trên ô quét đầu viewport
- Vạch trắng **PhaseDivider** sau ô 15, 25, 35…
- `SkillPanelUI` (ẩn mặc định, hiện khi click unit)

Save scene sau khi rebuild.

## Regenerate

Menu **Fractured Chorus → Setup Combat Scene Hierarchy** → chọn **Tạo lại** (xóa CombatRoot cũ). **Save scene trước** nếu đã chỉnh layout quan trọng.

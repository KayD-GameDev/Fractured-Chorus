# Unity workflow — Fractured Chorus

Canonical Unity project: `F:\Unity_Project\Fractured Chorus`  
Design mirror (docs only): GitHub repo `fractured-chorus`

---

## Core rules

1. **Logic chỉ trong MonoBehaviour / plain C# `.cs`** — không nhúng gameplay vào file `.unity` YAML, Visual Scripting, hoặc UnityEvent wiring phức tạp trên scene.
2. **Layout chỉnh trong Hierarchy** — menu **Fractured Chorus → Setup Combat Scene Hierarchy** tạo objects; kéo Transform / RectTransform trong Editor. Bootstrap **đọc** scene refs, không spawn UI/grid nếu đã có trong scene.
3. **Không code trên scene · GameObject phải hiện Hierarchy** — mọi UI/combat object (thẻ party, timeline template, unit…) phải thấy trong Hierarchy; Play chỉ **bind dữ liệu**, không spawn ẩn / không dịch layout khi `preserveSceneLayout` bật.
4. **Không sửa serialized logic trên scene** — thêm hành vi bằng script mới hoặc mở rộng class hiện có; tránh duplicate component logic trên nhiều prefab/scene.
5. **Namespace** — `FracturedChorus.Combat`, `FracturedChorus.UI`, `FracturedChorus.Data`.
6. **Ghi log mỗi session** — prepend entry vào [`docs/PROJECT_LOG.md`](PROJECT_LOG.md); quy trình team: [`docs/LOGGING.md`](../LOGGING.md).

---

## Folder convention

```
Assets/FracturedChorus/
├── Combat/                 # Beat Timeline, grid, units, damage, AI
│   ├── Bootstrap/ Core/ Timeline/ Grid/ Units/ Actions/ Damage/ AI/
├── UI/                     # Timeline bar, skill panel, unit views
├── Data/ScriptableObjects/
│   └── Presets/            # .asset: unit, skill, encounter
├── Scenes/                 # CombatPrototype + SCENE_SETUP.md
├── RunMap/                 # Node graph — MapGenerator, RunMapUIView (P2 MVP)
├── Narrative/              # Dialogue / story scenes (P2)
├── Audio/ Music/ SFX/
├── Art/
│   ├── Characters/
│   │   ├── Coda/Animation/       ← Mage (UnitPreset_Mage)
│   │   ├── Ren/Animation/        ← DPS (UnitPreset_Ren)
│   │   ├── Charlotte/Animation/  ← Tank (UnitPreset_Tank)
│   │   └── _Reference/           ← temp ref sprites (not canon)
│   ├── Backgrounds/
│   └── UI/
└── Prefabs/ Combat/ UI/
```

Index: `Assets/FracturedChorus/README.md` in Unity project.

Patterns (OOAD): **Command** (`ICombatAction`), **Observer** (timeline/UI events), **MVC** (`CombatController` + views), **Factory** (encounter runtime / enemy spawn sau này).

---

## ScriptableObject workflow

| Asset | Menu | Dùng cho |
|-------|------|----------|
| `SkillDefinitionSO` | Fractured Chorus / Skill Definition | delay, tier, glow, target |
| `UnitPresetSO` | Fractured Chorus / Unit Preset | stat block ref, skill list, `battleSprite`, placeholder color |
| `UnitStatBlockSO` | Fractured Chorus / Unit Stat Block | element, Physical/Magical strength, HP, crit, speed |
| `EncounterDefinitionSO` | Fractured Chorus / Encounter Definition | spawn list (side, row, col) |

**Resources (runtime load):** `Assets/FracturedChorus/Resources/StatBlocks/`, `Skills/`, `UnitPresets/`.  
Tạo bộ mặc định: menu **Fractured Chorus → Create Default Stat Blocks & Presets**. Chi tiết formula: `Assets/FracturedChorus/Data/ScriptableObjects/Presets/README.md`.

Nếu chưa có asset: `CombatPrototypeBootstrap` tạo **demo encounter runtime** (`EncounterRuntimeFactory`).

---

## Combat prototype spec (vertical slice — cập nhật 2026-06-26)

| Hạng mục | Giá trị |
|----------|---------|
| Player grid | **2×3** honeycomb (2 hàng × 3 cột), **max 4** units |
| Enemy grid | **2×3** mirror, **max 6** units |
| Timeline | **105 beats / 10 phases** (phase 1 = 15 ô, sau = 10 ô/phase) |
| **Planning flow** | (1) **EXECUTE** overlay — kéo formation / **swap** ally; (2) sau EXECUTE — click unit → skill panel |
| UI MVP | Carousel timeline + skill panel + EXECUTE + **party status bar** (góc trái trên) |
| AV | Phase budget 150/100; Base AV = priority only |
| Stats | `UnitStatBlockSO` → `UnitStats`; `DamageCalculator` (Physical/Magical, Harmony, crit) |
| Targeting | Cột **C1→C2→C3**; trong cột Tank → DPS → Mage |
| Input | `Physics2DRaycaster` + `BoardDragController` (OverlapPoint); `UnitFeetAnchor` không collider |
| Party UI | `PartyStatusBarUIView` — thẻ `Card_*` trong Hierarchy; spacing **1.25px**; Tank ngoài cùng phải; icon hệ art |

Log chi tiết session: [`docs/PROJECT_LOG.md`](../../PROJECT_LOG.md) (entries 2026-06-24 … 2026-06-26).

### Combat flow (prototype hiện tại)

```
Vào scene → UI/timeline khóa
    ↓
Planning (pre-EXECUTE): kéo unit player giữa các ô (neon xanh DropGlow); kéo lên ô ally → swap vị trí
    ↓
Bấm EXECUTE → LockPlayerReposition → timeline quét + nhạc
    ↓
Planning (post-EXECUTE): click unit → skill panel → gán skill tại beat scan
    ↓
Resolve khi scan bar cắt beat → dmg theo formula → Victory/Defeat
```

**Scene authoring:** `CombatPrototypeBootstrap.respectSceneAuthoring = true` — layout Hierarchy = Play. Sau pull code: **Fractured Chorus → Apply All Play-Ready Updates**.

### Party status bar (Hierarchy-first)

```
CombatCanvas/PartyStatusBarUI     ← PartyStatusBarUIView (preserveSceneLayout)
├── CardsRow
│   ├── Card_Mage                 ← active; bind lúc Play
│   ├── Card_Ren
│   └── Card_Tank                 ← ngoài cùng bên phải
└── CardTemplate (inactive)       ← model thẻ
    ├── Border
    ├── Avatar
    ├── HealthBarBg/HealthBarFill
    └── ElementBadge/ElementIcon  ← icon hệ (Nhịp / Giai điệu / Hòa âm)
```

| Menu Editor | Khi nào dùng |
|-------------|----------------|
| **Setup Party Cards in Hierarchy** | Tạo/căn `Card_Mage/Ren/Tank` + spacing 1.25px |
| **Apply Element Badge Icons (Stat Blocks)** | Gán sprite `icon_he_*` vào StatBlock Tank/Ren/Mage |
| **Add Party Status Bar (Hierarchy)** | Scene chưa có bar |
| **Fix Party Status Bar (Move to CombatCanvas)** | Bar nằm nhầm `Background canvas` |
| **Upgrade Party Card Template (Hierarchy)** | Scene cũ còn `RoleBadge` / thiếu `ElementIcon` tròn |
| **Find Missing Scripts (Active Scene)** | Console báo *The referenced script … is missing* |
| **Remove Missing Scripts (Active Scene)** | Xóa component Missing Script → Save scene |

**Runtime:** `RefreshPartyStatusBar()` sau init và sau formation swap — **chỉ bind HP/avatar**, không dịch RectTransform thẻ. Thứ tự cố định trái→phải: Mage · Ren · Tank (không phụ thuộc cột lưới).

**Chỉnh layout:** kéo `Card_*` / `CardTemplate` trong Hierarchy → Save. Spacing mặc định **1.25px** (`PartyCardDisplayOrder.BarSlotSpacing`).

---

## Art & placeholders

- Chưa import art approved → dùng **placeholder** (sprite màu / quad) trong code bootstrap.
- Art production → `docs/ASSET_INVENTORY.md` (GitHub) trước khi thay placeholder trong build.
- **Character animation folders** (tên canon, không tên role):
  - Mage → `Art/Characters/Coda/Animation/`
  - DPS → `Art/Characters/Ren/Animation/`
  - Tank → `Art/Characters/Charlotte/Animation/`
  - Mỗi state: `Idle`, `Move`, `Attack`, `Hit`, `Death`; skill thêm subfolder (vd. `Attack/Skill1_*`).
  - Chi tiết: `Assets/FracturedChorus/Art/Characters/README.md`.

---

## UC / FR traceability (combat slice)

| ID | Implemented (slice 1) |
|----|------------------------|
| UC-03 Position Unit | Grid placement + **pre-EXECUTE drag** (`BoardDragController`) |
| UC-04 Execute Skill | EXECUTE → scan resolve; skill assign at beat |
| FR-02 Beat Timeline | 105-beat engine + carousel UI + EXECUTE gate |
| FR-07 Damage | `UnitStatBlockSO` + `DamageCalculator` (Harmony, crit) |
| FR-03 Dual Grid | Honeycomb **2×3** + front-column targeting |

Deferred: UC-05 Interrupt, Morale/Affliction, Posture/Clash, UC-09 Boss.

---

## Git & commit (Unity project)

**Không commit:** `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `.vs/`  
**Nên commit:** `Assets/FracturedChorus/`, `Packages/manifest.json`, `ProjectSettings/` (khi ổn định)

GitHub repo `fractured-chorus` giữ **docs + design**; code Unity canonical tại `F:\Unity_Project\Fractured Chorus`.

---

## Scene setup

Xem [`F:\Unity_Project\Fractured Chorus\Assets\FracturedChorus\Scenes\SCENE_SETUP.md`](file:///F:/Unity_Project/Fractured%20Chorus/Assets/FracturedChorus/Scenes/SCENE_SETUP.md)

### Play-ready sync checklist

Code mới chỉ có hiệu lực khi **Unity compile xong** và **scene đã lưu** sau menu Editor.

| Bước | Hành động |
|------|-----------|
| 1 | Mở `F:\Unity_Project\Fractured Chorus`, scene `CombatPrototype` |
| 2 | Chờ compile (Console không lỗi đỏ) |
| 3 | Menu **Fractured Chorus → Apply All Play-Ready Updates** — tự Save scene |
| 4 | Play — so sánh Hierarchy với Edit mode (unit row, hex màu, timeline) |

Kiểm tra nhanh từ repo mirror (không cần Unity):

```powershell
python scripts/verify_combat_scene_sync.py
```

| Triệu chứng Play ≠ Scene | Nguyên nhân thường gặp |
|--------------------------|-------------------------|
| Unit nhảy row / hex đỏ bật lại | Scene chưa save sau chỉnh tay; hoặc `respectSceneAuthoring` chưa persist |
| Click unit không mở skill panel | Scene vẫn `PhysicsRaycaster` + `BoxCollider` 3D — chạy Apply All |
| Timeline gap bên phải | `BeatTimelineUIView` cũ chưa compile — refit qua Apply All |
| Sprite mất sau migrate | Chạy **Restore Unit Sprites from Presets** (đã gộp trong Apply All) |
| Console *missing script* (×2) | **Find Missing Scripts** → **Remove Missing Scripts** → Save; tránh sửa `.unity` YAML tay (UTF-8 BOM) |
| Thẻ party dính nhau / lệch khi kéo unit | Chạy **Setup Party Cards in Hierarchy** + **Apply All**; `preserveSceneLayout` phải bật |
| Party bar không hiện | Kiểm tra parent = **CombatCanvas**; `CardTemplate` inactive; bootstrap `partyStatusBarView` wired |

**Lưu ý:** Repo GitHub `fractured-chorus` giữ docs/scripts; **không** mirror `.cs` Unity. Canonical code = `F:\Unity_Project\Fractured Chorus\Assets\FracturedChorus\`.

---

## Phase gate

Combat Unity = **P2**. Run map = **P2 MVP** — scene `RunMapPrototype.unity`, doc [`RUNMAP_SCENE_SETUP.md`](../../Assets/FracturedChorus/Scenes/RUNMAP_SCENE_SETUP.md) (Unity) / mirror [`RUNMAP_SCENE_SETUP.md`](RUNMAP_SCENE_SETUP.md).

### Run map quick ref

| Menu | Mục đích |
|------|----------|
| **Create Run Map Prototype Scene** | Tạo + save scene |
| **Setup Run Map Scene Hierarchy** | Rebuild hierarchy |

Play: procedural map, random seed, F1 ở đáy scroll. Template: `MapTemplate_Default` — tắt **Use Reference Demo On Play** cho map ngẫu nhiên.

Mọi thay đổi canon gameplay/story → Notion Decision Log trước khi implement.

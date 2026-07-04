# RunMapPrototype — Scene setup (StS clone)

Logic trong **MonoBehaviour `.cs`**. Layout layer chỉnh trong **Hierarchy**; lúc Play `RunMapController` boot map → `RunMapUIView` rebuild node + edge theo seed và **fit viewport**.

**Tham khảo:** Slay the Spire map (7 cột × 15 tầng + boss F16) · [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2830078257) · [YouTube](https://www.youtube.com/watch?v=7HYu7QXBuCY) · `docs/diagrams/Fractured-Chorus-Run-Map-Node.drawio`

**Scene:** `Assets/FracturedChorus/Scenes/RunMapPrototype.unity`

---

## Tạo / mở scene

1. Mở Unity `F:\Unity_Project\Fractured Chorus`
2. Mở scene `RunMapPrototype.unity` **hoặc** menu **Fractured Chorus → Create Run Map Prototype Scene**
3. **File → Build Settings** — phải có:
   - `RunMapPrototype.unity`
   - `CombatPrototype.unity`
4. Chờ compile → **Play**

Menu bổ sung: **Fractured Chorus → Run Map → …** (xem bảng dưới).

---

## Hierarchy

```
RunMapRoot                    ← RunMapBootstrap (settings) + RunMapController (boot + click)
└── RunMapCanvas              ← scale (1,1,1); Screen Space Camera
    ├── TopBar                ← title, seed, status
    ├── MapScrollView         ← ScrollRect + RunMapScrollDriver (scroll 50%)
    │   └── Viewport
    │       └── MapContent    ← RunMapUIView (fitToViewport)
    │           ├── ConnectionsLayer   ← edge clones (template inactive ở đây)
    │           ├── NodesLayer         ← NodeTemplate + runtime nodes
    │           └── FloorLabelsLayer   ← F1…F16 labels
    └── LegendPanel           ← RunMapLegendPanelView (font/màu/spacing runtime)
EventSystem
Main Camera
```

**Quy tắc layout (2026-06-28+):**
- `MapContent` + mọi layer con: **anchor/pivot đáy** `(0.5, 0)` — Y từ đáy (F1 thấp, F16 cao).
- **Connection lines** spawn vào **`ConnectionsLayer`** (layer order: connections → labels → nodes).
- `MapConnectionLineView`: anchor đáy `(0.5, 0)`; `Image` dùng `UiCircleSpriteUtil.White` (Unity 6 không vẽ line nếu sprite null).
- Map **chỉ build lúc Play** — `RunMapController.Start()` → coroutine `BootRunMap()` (không phụ thuộc `RunMapBootstrap.Start()`).

---

## Gameplay prototype (Play)

| Bước | Hành vi |
|------|---------|
| Vào scene | `MapGenerator.GenerateFromTemplate()` — **procedural** mặc định; seed random mỗi Play |
| Scroll ban đầu | Content ở **đáy** — **F1** trong viewport; scroll chậm **50%** (`RunMapScrollDriver`) |
| Scroll lên | F2…F15 → **Boss F16** (node **58px**, icon ♪) |
| Click F1 | Bắt đầu run; edge visited **cam đậm**; preview **cam nhạt** |
| Click tiếp | Chỉ node **reachable** (outgoing edge); auto-scroll theo node |
| F15 → F16 | Mọi Camp F15 nối về **một** boss |
| **Click boss F16** | Status *"Vào trận Oni F16…"* → ~0.35s → load **`CombatPrototype`** (`RunMapSceneLoader`) |

**Lưu ý boss:** Phải đi path tới F16 (reachable từ F15 Camp). Click lại node boss khi đang đứng tại đó vẫn trigger load.

Console log mẫu:
```
[Fractured Chorus] Run map generated — seed XXXXX, procedural=True
[Fractured Chorus] Map elite density — N/M (25–35%), target 25–35%.
[Fractured Chorus] RunMapUIView built — nodes N, edges N.
[Fractured Chorus] Boss node selected — loading combat scene.
[Fractured Chorus] Load scene index … (CombatPrototype).
```

---

## MapTemplate_Default

Path: `Assets/FracturedChorus/Data/ScriptableObjects/Presets/MapTemplate_Default.asset`

| Flag / weight | Mặc định | Ý nghĩa |
|---------------|----------|---------|
| **Use Reference Demo On Play** | `off` | Bật → map cố định `STS_PATHS` (debug) |
| **Randomize Seed On Play** | `on` | Seed mới mỗi Play |
| **Default Seed** | `42` | Khi tắt randomize |
| battleWeight | 0.26 | Roll loại node (floors ngẫu nhiên) |
| **eliteWeight** | **0.32** | + validate **25–35%** elite trên toàn map (trừ boss) |
| eventWeight | 0.17 | |
| relay / camp / treasure | 0.05 / 0.06 / 0.14 | |

Fixed floors: **F1 Battle · F9 Treasure · F15 Camp** — không roll.

---

## Path generation (procedural)

1. Lưới 7×15 + boss F16  
2. **6 path** unique (signature hash) — random walk ±1 cột, mutate nếu trùng  
3. Prune node không thuộc path  
4. Gán cố định F1 / F9 / F15  
5. Roll loại + rule StS (`NodeTypeAssigner.ValidateRules` + **elite density 25–35%**)  
6. Boss F16 — 1 node giữa; mọi F15 → boss  

Demo reference: `MapGenerator.GenerateDemoReference(seed)` — chỉ khi bật flag trên SO.

Chi tiết: `scripts/build_fc_diagrams_drawio.py` (repo `fractured-chorus`).

---

## Scripts (OOAD)

| Class | Vai trò |
|-------|---------|
| `MapTemplateSO` | Grid, path count, seed flags, type weights |
| `MapGenerator` | Path gen ×6, prune, boss, `GenerateFromTemplate()` |
| `MapGraph` / `MapNodeData` | Runtime graph; lookup `(floor, column)` O(1) |
| `RunState` | Current node, visited; `CanSelectNode` (re-click boss) |
| `NodeTypeAssigner` | Roll loại, rules, **elite 25–35%** |
| `PathValidator` | Connectivity F1 → boss |
| `RunMapLayoutMetrics` | Spacing bottom-origin, content size, `fitToViewport` |
| `RunMapUIView` | Build node/edge, path highlight, layer order |
| `MapNodeView` / `MapConnectionLineView` | Node UI; line sprite + bottom anchor |
| `RunMapLegendPanelView` | Legend font 20px, dot stroke+fill, spacing compact |
| `RunMapScrollDriver` | Scroll 50%, smooth follow node |
| `RunMapController` | Boot map, click travel, **boss → combat scene** |
| `RunMapBootstrap` | Seed + `MapTemplateSO` (settings only) |
| `RunMapSceneCatalog` / `RunMapSceneLoader` | Tên scene + load theo build index |

**UC:** UC-01 Start Run · UC-02 Select Contract · UC-12 Navigate Map · UC-09 Boss Oni (entry từ map)

---

## Node types (FC ↔ StS)

| FC | StS | Ghi chú |
|----|-----|---------|
| Battle | Monster (M) | F1 cố định |
| Event | ? | |
| Elite | Elite (E) | **25–35%** nodes (non-boss) |
| Camp | Rest | F15 cố định; icon **+25%** vs node thường |
| Relay | Shop | |
| Treasure | Treasure (T) | F9 cố định |
| Boss Oni | Boss | F16 — **58px**, load `CombatPrototype` |

Icon scale: base × **1.75**; Camp thêm × **1.25**; boss glyph lớn hơn (`MapLayoutConstants`).

---

## Path visuals

| Trạng thái | Edge |
|------------|------|
| Mặc định | Xám ~4px |
| Visited path | Cam đậm ~7px |
| Preview (từ node hiện tại) | Cam nhạt ~5.5px |

---

## Legend panel

| Constant | Giá trị |
|----------|---------|
| Desc font | 20px |
| VLG spacing | 13.5px |
| HLG dot↔text | 17.5px |
| Dot | 34px, màu `MapNodePalette` (stroke + fill giống node map) |

Menu **Fractured Chorus → Upgrade Run Map Legend Panel** — rebuild + Save scene.

---

## Chỉnh layout (Editor)

| Object | Chỉnh gì |
|--------|----------|
| `MapScrollView` | Vùng map (~2%–78% màn hình) |
| `LegendPanel` | ~74%–96% bên phải |
| `RunMapRoot` → `bossCombatSceneName` | Mặc định `CombatPrototype` |
| `RunMapUIView.fitToViewport` | Bật = spacing scale theo viewport lúc Play |

Sau sửa Hierarchy → **Save scene**. Play rebuild map — không giữ clone runtime cũ.

---

## Troubleshooting

| Triệu chứng | Nguyên nhân / fix |
|-------------|-------------------|
| **Không thấy connection lines** | Template `Image` thiếu sprite → code gán `UiCircleSpriteUtil.White`; edges trên `ConnectionsLayer` |
| **Boss click không vào combat** | Build Settings thiếu `CombatPrototype`; boss chưa reachable; Console lỗi `RunMapSceneLoader` |
| F1 ở **trên** thay vì đáy | Layer anchor `(0.5, 0)` — **Run Map → Setup Scene Hierarchy** |
| Map trống khi Play | `RunMapController` disabled; Console `NodeTemplate chưa gán` → Setup hierarchy |
| Scene view chỉ template | **Bình thường Edit mode** — map full khi **Play** |
| Seed luôn 42 | Bật **Randomize Seed On Play** trên template / bootstrap |
| Legend giãn quá rộng | Chạy **Upgrade Run Map Legend Panel**; `RunMapLegendPanelView.Apply()` lúc Play |
| `RunMapCanvas` scale 0 | Set scale **(1,1,1)** — scene đã patch; reload scene |
| ADTM thread timeout | Cảnh báo Unity khi recompile — chờ compile xong |

---

## Editor menus

**Fractured Chorus → Run Map**

| Menu | Khi nào |
|------|---------|
| **Create Prototype Scene** | Scene mới + save |
| **Setup Scene Hierarchy** | Rebuild hierarchy + macro layer |
| **Setup Cadence Macro Layer** | Wire macro map, mask, inner layer |
| **Open Layout Editor** | Macro map — mask vault, background |
| **Open Pinky Vault Map Editor** | Inner map node — 3 Part, floor/boss/weights |
| **Upgrade Legend Panel** | Font/spacing/màu legend |
| **Save Scene Upgrades** | Legend + scroll + macro layer → save scene |

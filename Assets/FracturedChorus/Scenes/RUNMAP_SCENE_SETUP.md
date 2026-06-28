# RunMapPrototype — Scene setup (StS clone)

Logic trong **MonoBehaviour `.cs`**. Layout layer chỉnh trong **Hierarchy**; lúc Play `RunMapUIView` rebuild map (node + edge) theo seed và **fit viewport**.

**Tham khảo:** Slay the Spire map (7 cột × 15 tầng + boss F16) · [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2830078257) · [YouTube](https://www.youtube.com/watch?v=7HYu7QXBuCY) · `docs/diagrams/Fractured-Chorus-Run-Map-Node.drawio`

**Scene:** `Assets/FracturedChorus/Scenes/RunMapPrototype.unity`

---

## Tạo / mở scene

1. Mở Unity `F:\Unity_Project\Fractured Chorus`
2. Mở scene `RunMapPrototype.unity` **hoặc** menu **Fractured Chorus → Create Run Map Prototype Scene**
3. **File → Build Settings → Add Open Scenes**
4. Chờ compile → **Play**

Menu bổ sung: **Fractured Chorus → Setup Run Map Scene Hierarchy** (tạo lại hierarchy trên scene hiện tại).

---

## Hierarchy

```
RunMapRoot                    ← RunMapBootstrap + RunMapController
└── RunMapCanvas
    ├── TopBar                ← title, seed, status
    ├── MapScrollView         ← ScrollRect dọc (StS: F1 đáy → scroll lên boss)
    │   └── Viewport
    │       └── MapContent    ← RunMapUIView (fitToViewport)
    │           ├── ConnectionsLayer   ← template inactive (edge runtime spawn vào NodesLayer)
    │           ├── NodesLayer         ← NodeTemplate + runtime nodes + connection lines
    │           └── FloorLabelsLayer   ← F1…F16 labels
    └── LegendPanel
EventSystem
Main Camera
```

**Quy tắc layout (2026-06-28):**
- `MapContent` + mọi layer con: **anchor/pivot đáy** `(0.5, 0)` — tọa độ Y tính từ **đáy** content (F1 thấp, F16 cao).
- **Connection lines** spawn trong `NodesLayer` (cùng parent với node) — scroll đồng bộ.
- `MapConnectionLineView` dùng anchor đáy `(0.5, 0)` — **không** dùng center anchor `(0.5, 0.5)` (gây lệch ~nửa chiều cao map).

---

## Gameplay prototype (Play)

| Bước | Hành vi |
|------|---------|
| Vào scene | `MapGenerator.Generate(seed)` — **procedural** mặc định; seed random mỗi Play |
| Scroll ban đầu | Camera/content ở **đáy** — **F1** hiện trong viewport |
| Scroll lên | Xem F2…F15 → **Boss F16** (node to, icon ♪) |
| Click F1 | Bắt đầu run; edge đã đi **cam đậm**; edge kế tiếp **cam nhạt** |
| Click tiếp | Chỉ node **reachable** (outgoing edge); auto-scroll theo node |
| F15 → F16 | Mọi Camp F15 nối về **một** boss duy nhất |

### MapTemplate_Default (`Assets/FracturedChorus/Data/ScriptableObjects/Presets/`)

| Flag | Mặc định | Ý nghĩa |
|------|----------|---------|
| **Use Reference Demo On Play** | `off` | Bật → map cố định khớp `STS_PATHS` draw.io (debug) |
| **Randomize Seed On Play** | `on` | Mỗi Play seed mới → layout path khác |
| **Default Seed** | `42` | Dùng khi tắt randomize |

Console log: `[Fractured Chorus] Run map generated — seed XXXXX, procedural=True`

---

## Path generation (procedural)

1. Lưới 7×15 + boss F16  
2. **6 path** unique (signature hash) — random walk ±1 cột, mutate nếu trùng  
3. Prune node không thuộc path  
4. Gán cố định: **F1 Battle · F9 Treasure · F15 Camp**  
5. Gán ngẫu nhiên + rule override (StS)  
6. **Boss F16** — 1 node giữa; mọi F15 → boss  

Demo reference: `MapGenerator.GenerateDemoReference(seed)` — chỉ khi bật flag trên SO.

Chi tiết design: `scripts/build_fc_diagrams_drawio.py` (GitHub `fractured-chorus`).

---

## Scripts (OOAD)

| Class | Vai trò |
|-------|---------|
| `MapTemplateSO` | Grid, path count, seed flags, type weights |
| `MapGenerator` | Path gen ×6, prune, boss, type assign |
| `MapGraph` / `MapNodeData` | Runtime graph |
| `RunState` | Current node, visited path |
| `NodeTypeAssigner` | Roll loại + rule re-roll |
| `PathValidator` | Connectivity F1 → boss |
| `RunMapUIView` | Layout bottom-origin, fit viewport, vẽ node/edge |
| `MapNodeView` / `MapConnectionLineView` | Node button + UI line |
| `RunMapController` | Click → travel, scroll follow |
| `RunMapBootstrap` | Resolve seed, generate, init |

**UC:** UC-01 Start Run · UC-02 Select Contract · UC-12 Navigate Map

---

## Node types (FC ↔ StS)

| FC | StS | Ghi chú |
|----|-----|---------|
| Battle | Monster (M) | F1 cố định |
| Event | ? | |
| Elite | Elite (E) | |
| Camp | Rest | F15 cố định |
| Relay | Shop | |
| Treasure | Treasure (T) | F9 cố định |
| Boss Oni | Boss | F16 — **58px**, 1 node |

---

## Path visuals

| Trạng thái | Edge |
|------------|------|
| Mặc định | Xám, ~4px |
| Đã đi (visited path) | Cam đậm, ~7px |
| Preview (từ node hiện tại) | Cam nhạt, ~5.5px |

---

## Chỉnh layout (Editor)

| Object | Chỉnh gì |
|--------|----------|
| `MapScrollView` | Vùng map (~2%–78% màn hình) |
| `LegendPanel` | ~80%–98% bên phải |
| `NodeTemplate` | Model node (inactive) |
| `RunMapUIView.fitToViewport` | Bật = spacing scale theo viewport lúc Play |

Sau sửa Hierarchy → **Save scene**. Play rebuild map runtime — không cần giữ node clone cũ.

---

## Troubleshooting

| Triệu chứng | Nguyên nhân / fix |
|-------------|-------------------|
| F1 ở **trên** thay vì đáy | Layer stretch + pivot giữa → chạy **Setup Run Map Scene Hierarchy** hoặc đảm bảo layer anchor `(0.5, 0)` |
| Edge chỉ hiện từ ~F10 | Line dùng center anchor → đã fix `MapConnectionLineView` anchor đáy; lines trong `NodesLayer` |
| Map F1/F2 y chang mỗi Play | `MapTemplate_Default` vẫn bật **Use Reference Demo On Play** → tắt |
| Scroll không hết map | `ComputeContentSize()` — content height phải ≥ boss Y + padding |
| Seed luôn 42 | Tắt **Use Override Seed** trên `RunMapBootstrap`; bật **Randomize Seed On Play** |

---

## Editor menus

| Menu | Khi nào |
|------|---------|
| **Create Run Map Prototype Scene** | Scene mới + save `RunMapPrototype.unity` |
| **Setup Run Map Scene Hierarchy** | Rebuild hierarchy trên scene active |

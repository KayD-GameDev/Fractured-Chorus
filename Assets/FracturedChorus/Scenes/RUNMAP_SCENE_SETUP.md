# RunMapPrototype — Scene setup (StS clone)

Logic trong **MonoBehaviour `.cs`**. Layout chỉnh trong **Hierarchy** — Play chỉ bind/generate node, không spawn ẩn layout khi `preserveSceneLayout` bật.

**Tham khảo:** Slay the Spire map (7 cột × 15 tầng + boss F16) · [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2830078257) · [YouTube](https://www.youtube.com/watch?v=7HYu7QXBuCY) · `docs/diagrams/Fractured-Chorus-Run-Map-Node.drawio`

---

## Tạo scene nhanh

1. Mở Unity project `F:\Unity_Project\Fractured Chorus`
2. Menu **Fractured Chorus → Create Run Map Prototype Scene**
3. Scene lưu tại `Assets/FracturedChorus/Scenes/RunMapPrototype.unity`
4. **File → Build Settings → Add Open Scenes**
5. **Play** — scroll map, click node F1, đi theo path lên boss F16

Hoặc trên scene có sẵn: **Fractured Chorus → Setup Run Map Scene Hierarchy**

---

## Hierarchy

```
RunMapRoot                    ← RunMapBootstrap + RunMapController
└── RunMapCanvas
    ├── TopBar                ← title, seed, status
    ├── MapScrollView         ← ScrollRect (dọc, StS-style)
    │   └── Viewport
    │       └── MapContent    ← RunMapUIView
    │           ├── ConnectionsLayer / ConnectionTemplate (inactive)
    │           ├── NodesLayer / NodeTemplate (inactive)
    │           └── FloorLabelsLayer
    └── LegendPanel           ← chú thích màu node FC
EventSystem
Main Camera
```

---

## Gameplay prototype (Play)

| Bước | Hành vi |
|------|---------|
| Vào scene | `MapGenerator.GenerateDemoReference(seed)` — map cố định khớp `STS_PATHS` trong repo |
| Scroll | Bắt đầu ở **F1** (đáy map) |
| Click node F1 | Bắt đầu run — path cam highlight |
| Click node kế | Chỉ node **reachable** (outgoing edge từ node hiện tại) |
| F16 Boss | Node Oni — nối từ mọi Camp F15 |

Toggle procedural: `MapTemplate_Default` → tắt **Use Reference Demo On Play** → dùng `MapGenerator.Generate(seed)`.

---

## Scripts (OOAD)

| Class | Vai trò |
|-------|---------|
| `MapTemplateSO` | Cấu hình grid, seed, weights |
| `MapGenerator` | Template → path ×6 → prune → fixed F1/F9/F15 → random types → boss |
| `MapGraph` / `MapNodeData` | Runtime graph |
| `RunState` | Vị trí player, visited path |
| `NodeTypeAssigner` | Roll loại node + rule override (StS) |
| `PathValidator` | Connectivity F1 → boss |
| `RunMapUIView` | Vẽ node + edge trên UI |
| `RunMapController` | Click → travel |
| `RunMapBootstrap` | Start run |

**UC:** UC-01 Start Run · UC-12 Navigate Map

---

## Node types (FC ↔ StS)

| FC | StS | Màu |
|----|-----|-----|
| Battle | Monster (M) | đỏ nhạt |
| Event | ? | xanh lá |
| Elite | Elite (E) | tím |
| Camp | Rest | vàng |
| Relay | Shop | cam |
| Treasure | Treasure (T) | xanh dương |
| Boss Oni | Boss F16 | tối |

Gán cố định: **F1 Battle · F9 Treasure · F15 Camp · F16 Boss**

---

## Chỉnh layout

| Object | Chỉnh gì |
|--------|----------|
| `MapScrollView` | Vùng map trên màn hình |
| `MapContent` | Kích thước content (RunMapUIView cũng set runtime) |
| `NodeTemplate` | Kích thước / font icon node |
| `LegendPanel` | Vị trí legend bên phải |

Save scene sau khi kéo — Play phải khớp Hierarchy.

---

## Quy trình generate (design)

1. Map Template — lưới 7×15  
2. Path Generation ×6 — random walk ±1 cột  
3. Prune — chỉ giữ node có path  
4. Gán cố định F1/F9/F15  
5. Gán ngẫu nhiên + rule re-roll  
6. Boss F16 + nối từ F15  

Chi tiết: `scripts/build_fc_diagrams_drawio.py` trên GitHub `fractured-chorus`.

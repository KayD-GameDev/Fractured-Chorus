# RunMap — Node icon brief

> Style ref: cyber-musical dark fantasy · neon bloom · gold frame/pedestal  
> Palette lock: **deep purple / magenta / cyan / polished gold** on `#000000`

## Ship set (v1)

| Node | File | Status |
|------|------|--------|
| Battle | `runmap_node_battle_v1.png` | ✅ |
| Elite | `runmap_node_elite_v1.png` | ✅ anime 2D |
| Treasure | `runmap_node_treasure_v1.png` | ✅ |
| Event | `runmap_node_event_v1.png` | ✅ |
| Camp | `runmap_node_camp_v1.png` | ✅ |
| Relay (Shop) | `runmap_node_relay_v1.png` | ✅ |
| Start | `runmap_node_start_v1.png` | ✅ departure · nhỏ hơn node thường |
| Boss Final | `runmap_node_boss_final_v1.png` | ✅ anime 2D |
| Boss Floor I | `runmap_node_boss_floor_i_v1.png` | ✅ anime 2D |
| Boss Floor II | `runmap_node_boss_floor_ii_v1.png` | ✅ anime 2D |

## Style note — v1 ship (simple)

- Hướng: **anime 2D tối giản** — 1 motif trung tâm, khung gold mỏng, ≤2 nốt nhạc
- Bỏ: equalizer, speakers, debris, particle dày, ribbon/banner thừa
- Palette: purple / magenta / cyan / gold trên đen
- Bản dày chi tiết lưu `*_busy_v1.png` (không ship)

## Reference only (không ship)

| File | Ghi chú |
|------|---------|
| `runmap_node_elite_pre_anime_v1.png` | Elite trước pass anime |
| `runmap_node_boss_*_pre_anime_v1.png` | Boss trước pass anime |
| `runmap_node_boss_final_ref_red_v1.png` | Bản đỏ — palette lệch set |
| `runmap_node_boss_floor_ref_v1.png` | Bản gốc trước regen |

## Composition families

| Family | Nodes | Layout |
|--------|-------|--------|
| Pedestal | Battle, Elite, Treasure | Tiered gold platform + central motif |
| Frame | Event, Boss Final, Boss Floor | Gold circular frame + bottom plaque/banner |

## Unity import (đề xuất)

| Setting | Value |
|---------|-------|
| Texture Type | Sprite (2D and UI) |
| Pixels Per Unit | 100 |
| Filter Mode | Bilinear |
| Max Size | 512 (node thường) / 768 (boss) |
| Compression | High Quality |

Display target trên map: node thường **~48px**, boss **~58px** (`MapLayoutConstants`).

## Matte pipeline (trước khi gắn UI)

Nền đen tuyệt đối → chạy `Tools/neon-matte.mjs` để alpha từ luminance:

```bash
node Tools/neon-matte.mjs Assets/FracturedChorus/Art/UI/RunMap/Nodes/runmap_node_battle_v1.png Assets/FracturedChorus/Art/UI/RunMap/Nodes/runmap_node_battle_v1_matte.png --canvas 512x512
```

Lặp cho từng file ship. Boss có thể dùng canvas 768×768.

## Code hook

| Piece | Path |
|-------|------|
| Icon set SO | `Data/ScriptableObjects/Presets/MapNodeIconSet_Default.asset` |
| Resolve API | `MapNodeIconSetSO.Resolve(type, isBoss, sector)` |
| Node view | `MapNodeView` — `Image iconImage`, fallback emoji nếu thiếu sprite |
| Wire menu | **Fractured Chorus → Run Map → Wire Node Icons** |

Boss variant theo `PinkySectorId`: Pulse→Floor I · Echo→Floor II · Canticle→Final.

Display size: node **~80–104px**, **Start ~72% (~58–75px)**, boss **~112–140px** (fit viewport). Editor preview strip: **Map Nodes** trên `CadenceMapController`.

## Palette lock (regen boss)

| Token | Hex (approx) |
|-------|--------------|
| Deep purple bg | `#1a0a2e` – `#2d1458` |
| Magenta glow | `#e040a0` – `#ff44cc` |
| Cyan glow | `#00d4ff` – `#44eeff` |
| Gold frame | `#c9a030` – `#ffd700` |
| **Cấm** | đỏ cam `#ff4400` (Final Boss ref cũ) |

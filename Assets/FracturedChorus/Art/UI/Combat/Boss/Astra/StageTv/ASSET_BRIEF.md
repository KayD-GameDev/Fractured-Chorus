# Stage TV — Asset brief (Astra boss intro jumbotron)

> Skill: `game-2d-asset-creation` + alpha key `game-2d-asset-image-edit`  
> Character lock: `Art/Characters/Astra/CHARACTER_LOCK.md` (Cadence **bust with outfit**)

## Display (CombatPrototype @1920×1080)

| Surface | Size | Notes |
|---------|------|--------|
| TV rest | scene RectTransform | authored on `AstraStageTv` under Background canvas; config `RestNormalizedPos` / `RestSizePx` only seed an unauthored rect |
| Screen hole | scene `Screen` rect | config `ScreenInsetNormalized` only when unauthored; face reel spacing = screen height |
| Drop | from above the canvas top edge back to the authored rest | 1.0s then reel |

## Assets (one PNG each — never atlas)

| File | Canvas | Role |
|------|--------|------|
| `astra_stage_tv_frame_v1.png` | **1024 × 1024** | Neon magenta/cyan bezel; **true alpha hole** in the screen |
| `astra_tv_face_joy_v1.png` | **1024 × 1024** | Hỷ — smile |
| `astra_tv_face_anger_v1.png` | **1024 × 1024** | Nộ — scowl |
| `astra_tv_face_love_v1.png` | **1024 × 1024** | Ái — affection |
| `astra_tv_face_hate_v1.png` | **1024 × 1024** | Ố — contempt |
| `astra_tv_face_sorrow_v1.png` | **1024 × 1024** | Bi — tears |

All faces: same 1:1 crop (crown → hair → upper shoulders), **Cadence Boss outfit** (black/white vest, gold stars, choker) visible at neck/shoulders, platinum hair, golden amber eyes, true-alpha background.

## Pipeline

1. Gen **separate** files. Do not composite faces into the TV frame.
2. Faces: solid `#000000` background → flood-key black from **top + sides** (keep platinum hair and dark costume).
3. Frame: punch checkerboard / near-white interior to **alpha 0**. Outer corners also alpha 0.
4. Mirror Art → `Resources/UI/Combat/Boss/Astra/StageTv/`.

## Import (Unity)

- Texture Type: **Sprite (2D and UI)** · Sprite Mode **Single** · Mesh Type **Full Rect**
- Alpha Is Transparency: on · Mip Maps: off · Filter Bilinear · Wrap Clamp
- Max Size **1024** · Compression **None**

## Cấm

- Không gộp 5 mặt thành spritesheet.
- Không bake mặt / khoá sol vào `astra_stage_tv_frame_v1`.
- Không crop vai trần / “no outfit” — Stage TV = Cadence bust có đồ.
- Không dùng checkerboard bake làm “trong suốt”.

# LeftRail — Asset brief (size SoT từ CombatPrototype @1920×1080)

> Ref composition: `../Refs/left_rail_clef_column_ref_v1.png`  
> Plan v1: `docs/superpowers/plans/2026-07-27-timeline-left-rail-design.md`  
> Plan v2 (regen PHASE / chip / clef): `docs/superpowers/plans/2026-07-27-leftrail-phase-clef-regen.md`

## Measured (scene + code, 2026-07-27 sau regen)

| Surface | Size (px) | Notes |
|---------|-----------|--------|
| BeatTimelineUI | **1843 × 358** | anchors y 0.02–0.223 + sizeDelta.y 138.8 |
| Header (LeftRail) | **211 × 358** | stretch Y; **không** phủ Viewport |
| Viewport | ~1624 × **342** | sizeDelta (−218.82, −16); left inset ≈ Header |
| PhaseLabel | **170 × 62** | anchor/pivot top-left, pos (20, −6); Text tắt, art qua child `PhaseArt` |
| Budget (phase chip) | **118 × 44** | center (105.5, **87**) — nằm dưới PhaseLabel; `BudgetText` font **22** hiện `1/39` |
| Clef | **136.78 × 152.45** | center (102.3, **−21**); `clefAlpha` **0.62** |
| LaneAvatarGutter | **72** wide @ X = Viewport.left − 72 | giữa LeftRail và beat track; **không** đè ScanBar/notes |
| Lane band Y | 0.16–0.48 × ViewportH | Y≈ **164 / 109 / 55** từ đáy VP (3 lane) |
| LaneLines | height **5** | **cấm** vẽ vào asset LeftRail |

## Assets hiện hành

| File | Canvas PNG | Ink bbox | Display | Ghi chú |
|------|-----------|----------|---------|---------|
| `left_rail_bg_v1.png` | 422 × 716 | — | 211 × 358 | không đổi |
| `phase_label_v4.png` | **340 × 124** | 320 × 68 | **170 × 62** (chữ ~34px) | canvas = 2× rect, aspect khớp tuyệt đối |
| `phase_chip_v3.png` | **236 × 88** | 232 × 84 | **118 × 44** | **khung rỗng**, số do `BudgetText` vẽ runtime |
| `treble_clef_v4.png` | **112 × 300** | 104 × 292 | **57 × 152** | watermark alpha 0.62 |
| `avatar_column_bg_v1.png` | **144 × 716** | 136 × 698 | **72 × 358** | glass strip cyan→magenta, hazard ticks mép phải; cột giữa LeftRail và track |
| `lane_avatar_frame_pc_v1.png` | **96 × 96** | 92 × 92 | **40–52** | PC: magenta rim + cyan hairline + side notches |
| `lane_avatar_frame_boss_v1.png` | **96 × 96** | 90 × 90 | **40–52** | Boss: magenta dominant + hazard ticks 2 bên |
| `lane_avatar_ring_v1.png` | 96 × 96 | — | legacy | vòng tròn cũ — giữ backup, code ưu tiên `frame_pc_v1` |
| `Avatars/ren_chibi_avatar_v1.png` | **256 × 256** | circle Ø254 | **40–52** | chibi bust crop tròn, nền disc navy `#061435` |
| `Avatars/coda_chibi_avatar_v1.png` | **256 × 256** | circle Ø254 | **40–52** | như trên |
| `Avatars/charlotte_chibi_avatar_v1.png` | **256 × 256** | circle Ø254 | **40–52** | như trên |

## Pipeline avatar chibi (không dùng neon-matte)

Portrait có màu tối/trắng nên **cấm** matte theo luminance. Quy trình:

1. Gen chibi full-body 2.5 heads trên **nền navy phẳng** kín khung 1:1, theo `CHARACTER_LOCK.md` của nhân vật.
2. Lưu bản gen vào `Art/Characters/<Name>/Chibi/<name>_chibi_fullbody_v1.png` (source, không ship trực tiếp).
3. Crop tròn:

```bash
node Tools/circle-avatar.mjs Tools/out_avatars ren=<gen.png> coda=<gen.png> charlotte=<gen.png>
node Tools/preview-avatars.mjs   # so sánh bust vs full @128 và @48
```

4. Ship biến thể **bust** (`*_chibi_avatar_bust_v1.png`) → `LeftRail/Avatars/<name>_chibi_avatar_v1.png`. Biến thể `full` chỉ để đối chiếu — mặt không đọc được ở 48px.

## Pipeline bắt buộc cho asset neon

1. Gen trên **nền đen tuyệt đối** (`#000000`), không gradient/vignette/khung.
2. Matte bằng `Tools/neon-matte.mjs` — alpha = luminance/knee, unpremultiply, trim, re-canvas:

```bash
node Tools/neon-matte.mjs <gen.png> <out.png> --canvas 340x124 --ink 320x68
node Tools/neon-matte.mjs <gen.png> <out.png> --canvas 236x88 --ink 232x84 --fit fill
node Tools/neon-matte.mjs <out.png> --report-only   # audit lại file đã có
```

3. QA gate (script tự in): semi-alpha ≥15%, border alpha = 0, near-white ≤5%, center offset ≤2px.
4. Preview layout: `node Tools/preview-left-rail.mjs proposal`.

## Import (Unity)

- Texture Type: **Sprite (2D and UI)** · Sprite Mode **Single**
- Alpha Is Transparency: on · Mip Maps: off · Filter Bilinear · Wrap Clamp
- Max Size **512** · Compression **None**
- Mirror copy → `Resources/UI/Combat/Timeline/LeftRail/` (fallback khi scene mất ref)

## Cấm

- **Không dùng `Tools/SpriteBgClear.exe`** cho asset neon — flood-fill từ biên để lại glow trắng opaque bên trong và alpha nhị phân (răng cưa).
- Không gen nền trắng.
- Không bake chữ/số vào `phase_chip` — số phase là `phaseIndex+1 / TimelineConstants.PhaseCount` (tối đa `39/39`).
- Không vẽ 3 sợi dây ngang / staff full-width vào `left_rail_bg`.
- Không gen art đè vùng Viewport.
- Portrait nhân vật: bake sẵn circle PNG vào `Avatars/` → gán `UnitPresetSO.timelineAvatarSprite`; runtime không mask thêm.

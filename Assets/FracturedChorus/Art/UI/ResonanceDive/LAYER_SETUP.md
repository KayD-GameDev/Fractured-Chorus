# Resonance Dive — gán layer

## Alpha

Cả 11 file đính kèm là **JPEG 1024×576, không có kênh alpha**. Nền caro đã bị vẽ vào pixel. Đã key caro + bỏ nhiễu JPEG, xuất PNG vào:

- `Assets/FracturedChorus/Art/UI/ResonanceDive/`
- `Assets/FracturedChorus/Resources/UI/ResonanceDive/`

Báo cáo: `layer_import_report.json`. Nếu còn PNG gốc (RGBA), thay file rồi chạy lại `Tools/matte-resonance-dive-user-layers.mjs`.

Không có `07_Text`. Chữ = `TextRoot/TitleLabel` + `SubtitleLabel` (`UiFontCatalog`).

## Hierarchy

```
ResonanceDiveButton
├── ShadowRoot / Layer_Shadow          ← 12_Shadow
└── Visual                             ← scale chung Hover/Pressed
    ├── Layer_Base                     ← 01_Base
    ├── Layer_Glass                    ← 03_Glass
    ├── Layer_Gradient                 ← 04_Gradient
    ├── Layer_Border                   ← 02_Border
    ├── Layer_Glow                     ← 08_Glow
    ├── Layer_Wave                     ← 10_Wave
    ├── FaceMask (Mask = 01_Base)
    │   └── Layer_Scanline             ← 11_Scanline
    ├── Layer_DecoLeft                 ← 05_Deco_Left (pivot ngôi sao)
    ├── Layer_DecoRight                ← 06_Deco_Right
    └── Layer_Particles                ← 09_Particles
└── TextRoot / TitleLabel / SubtitleLabel
```

Cùng canvas 1024×576 nên Image stretch trong `Visual` khớp nhau. Chỉnh pos/size/alpha trên từng Image. Hierarchy không ghi đè RectTransform object đã có.

## Gán sprite

`AssignMissingSprites()` chỉ gán khi `Image.sprite == null`.

| Image | Resources |
|---|---|
| Layer_Shadow | `UI/ResonanceDive/12_Shadow` |
| Layer_Base | `01_Base` |
| Layer_Glass | `03_Glass` |
| Layer_Gradient | `04_Gradient` |
| Layer_Border | `02_Border` |
| Layer_Glow | `08_Glow` |
| Layer_Wave | `10_Wave` |
| Layer_Scanline | `11_Scanline` |
| Layer_DecoLeft | `05_Deco_Left` |
| Layer_DecoRight | `06_Deco_Right` |
| Layer_Particles | `09_Particles` |

Motion: `Resources/UI/ResonanceDive/ResonanceDiveMotion.asset`.

## Menu Unity

1. `Fractured Chorus / Resonance Dive / Rebuild In Open Scene`
2. `Fractured Chorus / Resonance Dive / Create Prefab` → `UI/Prefabs/ResonanceDiveButton.prefab`
3. Inspector: Normal / Hover / Pressed (preview, unscaled)

Play cũng `Ensure` hierarchy nếu thiếu `Visual`. **Ctrl+S** sau Rebuild.

## Motion (SO, không hardcode trong Update)

| Layer | Hành vi |
|---|---|
| Base, Glass, Text | ổn định |
| Gradient, Border, Glow | pulse 2.4s, sáng hơn khi hover |
| DecoLeft | lắc ±3° quanh pivot sao |
| DecoRight | dịch X khi hover |
| Particles | trôi + fade (một atlas) |
| Wave | nhấp alpha |
| Scanline | quét dưới→trên 0.45s trong FaceMask |
| Shadow | phồng hover, thu pressed |

Hover: Visual 103% / 0.15s. Pressed: 97% + nudge Y / 0.08s. Release: hover nếu còn pointer, không thì normal. Scale lerp về target, không cộng dồn.

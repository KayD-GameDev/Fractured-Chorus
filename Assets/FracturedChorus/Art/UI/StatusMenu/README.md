# Status Menu UI — Art Pack

**Palette:** Cyan `#00D4FF` · Pink `#FF0066` · Dark Blue `#0A0A2E`  
**Path:** `Assets/FracturedChorus/Art/UI/StatusMenu/`

## Background

| File | Notes |
|------|--------|
| `statusmenu_ren_bg_v6.png` | 1920×1080 plate (inverted Ren + MAIN MENU) |
| `statusmenu_ren_bg_v5.png` … `v1` | Iterations / backup |

## Buttons (list)

| File | Use |
|------|-----|
| `statusmenu_btn_*_selected.png` | Active row: white parallelogram + pink slash + label |
| `statusmenu_btn_*_normal.png` | Idle row: cyan italic label |
| `*_stats` / `*_bonds` / `*_calendar` / `*_system` | Four menu entries |
| `statusmenu_btn_selected_plate.png` | Blank selected chrome (TMP overlay) |
| `statusmenu_btn_normal_plate.png` | Blank idle chrome |
| `statusmenu_slash_accent.png` | Pink slash overlay |

## Prompts

| File | Use |
|------|-----|
| `statusmenu_prompt_confirm.png` | Confirm |
| `statusmenu_prompt_close.png` | Close |

## Wire vào scene

1. Exit Play Mode  
2. `Fractured Chorus → Wire Town Map Status Menu`  
3. Save scene  

Play Mode cũng auto-rebuild StatusMenu nếu thiếu layout v6 (Resources path).

## Runtime

- MENU / Tab / M → mở overlay full-screen (`statusmenu_ren_bg_v6`)  
- List phải: STATS · BONDS · CALENDAR · SYSTEM  
- Selected/normal sprites swap  
- SYSTEM / Esc → đóng  
- Detail panel trái-dưới hiện data meta  

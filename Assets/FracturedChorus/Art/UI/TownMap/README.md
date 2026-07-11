# Town Map UI — Art Pack

**Palette:** Cyan `#2EC4B6` · Navy `#0B1A33` · White · Accent sun `#FFD246`  
**Path:** `Assets/FracturedChorus/Art/UI/TownMap/`

## P0 / P1 assets

Xem bảng trong lịch sử commit — pin, panel, slash, sun/moon/dawn, wordmark, prompts.

## P2 (runtime)

| Feature | Implementation |
|---------|----------------|
| SFX | `TownMapSfxController` — reuse `MainMenu_ChangeMenu_Ting` + `MainMenu_ButtonPress` |
| Prompt KB/Pad | `TownMapPromptBar` + `TownMapInput` — Enter/Esc ↔ South/East |
| District keys | Enter confirm · Esc close |
| Return Hub | `RunMapHubBridge` · Esc trên Cadence map · consume Evening slot |

## Setup

```
Fractured Chorus → Setup CampusHub Scene Hierarchy
```

## Playtest

1. Hub Morning → Continue → Town Map  
2. Click HIMA → panel → Confirm (or Enter)  
3. Evening + set `vault_quest_active` → Cadence Gate → RunMap → Esc → Hub  

## Còn lại (sau Phase 2)

Forced story days · fine-tune pin anchors · dedicated TownMap SFX · font TTF

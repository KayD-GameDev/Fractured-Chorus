# Run Map (P2 MVP)

Hai tầng: **Cadence Macro Map** (chọn Vault) → **Inner Node Map** (StS-style path trong Vault).

## Luồng Pinky (Arc 1)

```
Macro Map → click Pinky (mask highlight)
  → Part 1 Pulse Lane  (F1–10 → F11 Mimi)
  → Part 2 Echo Lane   (F1–10 → F11 Kiki)
  → Part 3 Canticle    (F1–12 → F13 Chart Lord)
```

5 Vault Ngu Chi Am trên macro map; chỉ **Pinky** unlock. 3 Part nằm **bên trong** inner map.

## Scene & docs

| | Path |
|--|------|
| Scene | `Assets/FracturedChorus/Scenes/RunMapPrototype.unity` |
| Macro setup | Menu **Fractured Chorus → Run Map → Setup Cadence Macro Layer** |
| Layout editor | **Fractured Chorus → Run Map → Open Layout Editor** (macro mask) |
| Inner map editor | **Run Map → Open Pinky Vault Map Editor** (3 Part node graph) |
| Mask preview | Layout Editor → **Scene View Mask Edit** |
| Inner preview | Pinky Vault Map Editor → **Preview Part 1/2/3** |
| Background | `Art/Backgrounds/cadence_macro_map_bg_v2_5fingers.png` |
| Layout SO | `Data/ScriptableObjects/Presets/CadenceMapLayout_Default.asset` |
| Setup | [`../Scenes/RUNMAP_SCENE_SETUP.md`](../Scenes/RUNMAP_SCENE_SETUP.md) |
| Template SO | `Data/ScriptableObjects/Presets/MapTemplate_Default.asset` |
| Combat entry | `Scenes/CombatPrototype.unity` (boss click) |

## Scripts

| Path | Class |
|------|-------|
| `RunMap/` | `CadenceMapController` — macro↔inner, 3-part progression |
| `RunMap/UI/` | `CadenceMacroMapView`, `VaultTerritoryGraphic` — mask + highlight |
| `RunMap/Core/` | `CadenceRunProgress`, `MapGenerationProfile`, `PinkySectorId` |
| `RunMap/Core/` | `MapGenerator`, `MapGraph`, `RunState`, `NodeTypeAssigner`, … |
| `RunMap/UI/` | `RunMapUIView`, `MapNodeView`, `MapConnectionLineView`, … |
| `Data/ScriptableObjects/` | `CadenceMapLayoutSO`, `MapTemplateSO` |
| `Editor/` | `RunMapSceneSetupEditor` |

## Play flow

1. **Macro:** hover/click territory (polygon mask) — Pinky highlight → Dive
2. **Inner Part 1:** `MapGenerator.GenerateSector(Pulse)` — F1→F11 Mimi
3. **Boss win** → auto Part 2 Echo → Part 3 Canticle (prototype: `simulateBossVictoryOnReturn`)
4. Combat win hook: `CadenceMapController.NotifyBossVictory()`

## Defaults

- F1 **đáy** scroll; boss F16 **58px** giữa map
- Edges trên **`ConnectionsLayer`**; nodes trên **`NodesLayer`**
- Legend: font 20px, spacing 13.5px, màu khớp `MapNodePalette`
- Camp icon **+25%** vs node thường
- Demo map: bật `useReferenceDemoOnPlay` trên `MapTemplate_Default`

**UC:** UC-01 Start Run · UC-02 Select Contract · UC-12 Navigate Map · UC-09 Boss Oni

Design: `scripts/build_fc_diagrams_drawio.py`, GDD §3.

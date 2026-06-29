# Run Map (P2 MVP)

Slay the Spire–style node graph: **7×15 floors + boss floor 16**.

## Scene & docs

| | Path |
|--|------|
| Scene | `Assets/FracturedChorus/Scenes/RunMapPrototype.unity` |
| Setup | [`../Scenes/RUNMAP_SCENE_SETUP.md`](../Scenes/RUNMAP_SCENE_SETUP.md) |
| Template SO | `Data/ScriptableObjects/Presets/MapTemplate_Default.asset` |
| Combat entry | `Scenes/CombatPrototype.unity` (boss F16 click) |

## Scripts

| Path | Class |
|------|-------|
| `RunMap/Core/` | `MapGenerator`, `MapGraph`, `RunState`, `NodeTypeAssigner`, `PathValidator`, `MapLayoutConstants`, `RunMapLayoutMetrics` |
| `RunMap/UI/` | `RunMapUIView`, `MapNodeView`, `MapConnectionLineView`, `MapNodePalette`, `RunMapScrollDriver`, `RunMapLegendPanelView` |
| `RunMap/` | `RunMapController`, `RunMapBootstrap`, `RunMapSceneCatalog`, `RunMapSceneLoader` |
| `Data/ScriptableObjects/` | `MapTemplateSO` |
| `Editor/` | `RunMapSceneSetupEditor` |

## Play flow

1. **`RunMapController.Start()`** → `BootRunMap()` → `MapGenerator.GenerateFromTemplate` + `RunMapUIView.BuildMap`
2. Player path F1 → … → F15 Camp → **F16 Boss**
3. Click boss (reachable) → **`RunMapSceneLoader.LoadByName("CombatPrototype")`**
4. Procedural seed mỗi Play; elite density **25–35%**; scroll **50%** speed

## Defaults

- F1 **đáy** scroll; boss F16 **58px** giữa map
- Edges trên **`ConnectionsLayer`**; nodes trên **`NodesLayer`**
- Legend: font 20px, spacing 13.5px, màu khớp `MapNodePalette`
- Camp icon **+25%** vs node thường
- Demo map: bật `useReferenceDemoOnPlay` trên `MapTemplate_Default`

**UC:** UC-01 Start Run · UC-02 Select Contract · UC-12 Navigate Map · UC-09 Boss Oni

Design: `scripts/build_fc_diagrams_drawio.py`, GDD §3.

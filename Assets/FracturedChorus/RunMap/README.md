# Run Map (P2)

Slay the Spire–style node graph: **7×15 floors + boss floor 16**.

## Scene

- `Assets/FracturedChorus/Scenes/RunMapPrototype.unity`
- Setup doc: [`RUNMAP_SCENE_SETUP.md`](../Scenes/RUNMAP_SCENE_SETUP.md)

## Scripts

| Path | Class |
|------|-------|
| `RunMap/Core/` | `MapGenerator`, `MapGraph`, `RunState`, `NodeTypeAssigner`, `PathValidator` |
| `RunMap/UI/` | `RunMapUIView`, `MapNodeView`, `MapConnectionLineView`, `MapNodePalette` |
| `RunMap/` | `RunMapController`, `RunMapBootstrap` |
| `Data/ScriptableObjects/` | `MapTemplateSO` |
| `Editor/` | `RunMapSceneSetupEditor` — menu **Create Run Map Prototype Scene** |

**UC:** UC-01 Start Run · UC-02 Select Contract · UC-12 Navigate Map

Design ref: GitHub `scripts/build_fc_diagrams_drawio.py`, GDD §3, StS workshop + YouTube links in scene setup doc.

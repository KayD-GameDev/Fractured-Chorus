# Run Map (P2 MVP)



Slay the Spire–style node graph: **7×15 floors + boss floor 16**.



## Scene & docs



| | Path |

|--|------|

| Scene | `Assets/FracturedChorus/Scenes/RunMapPrototype.unity` |

| Setup | [`../Scenes/RUNMAP_SCENE_SETUP.md`](../Scenes/RUNMAP_SCENE_SETUP.md) |

| Template SO | `Data/ScriptableObjects/Presets/MapTemplate_Default.asset` |



## Scripts



| Path | Class |

|------|-------|

| `RunMap/Core/` | `MapGenerator`, `MapGraph`, `RunState`, `NodeTypeAssigner`, `PathValidator`, `MapLayoutConstants`, `RunMapLayoutMetrics` |

| `RunMap/UI/` | `RunMapUIView`, `MapNodeView`, `MapConnectionLineView`, `MapNodePalette`, `RunMapScrollDriver`, `RunMapLegendPanelView` |

| `RunMap/` | `RunMapController`, `RunMapBootstrap` |

| `Data/ScriptableObjects/` | `MapTemplateSO` (grid + seed flags + **type weights**) |

| `Editor/` | `RunMapSceneSetupEditor` |



## Play defaults



- **Procedural** map via `MapGenerator.GenerateFromTemplate`, **random seed** mỗi Play

- F1 ở **đáy** scroll; boss F16 **một node to** ở trên

- Path click + edge highlight (visited / preview); edge metadata on `MapConnectionLineView`

- Demo reference map: bật `useReferenceDemoOnPlay` trên `MapTemplate_Default`



**UC:** UC-01 Start Run · UC-02 Select Contract · UC-12 Navigate Map



Design: `scripts/build_fc_diagrams_drawio.py`, GDD §3, StS workshop/YouTube links trong scene setup doc.


# Fractured Chorus — Unity assets root

Canonical code & content live under `Assets/FracturedChorus/`.  
Design docs: GitHub repo `fractured-chorus` (`docs/`).

## Folder map

```
FracturedChorus/
├── Combat/                 # Beat Timeline, Dual Grid, damage, AI (active)
│   ├── Bootstrap/
│   ├── Core/
│   ├── Timeline/
│   ├── Grid/
│   ├── Units/
│   ├── Actions/
│   ├── Damage/
│   └── AI/
├── UI/                     # Timeline bar, skill panel, unit views (active)
├── Data/
│   └── ScriptableObjects/
│       └── Presets/        # Unit / skill / encounter .asset files
├── Scenes/                 # CombatPrototype + setup guide
├── RunMap/                 # StS-style node graph (P2)
├── Narrative/              # VN-style scenes, dialogue data (P2)
├── Audio/
│   ├── Music/
│   └── SFX/
├── Art/
│   ├── Characters/         # Import after ASSET_INVENTORY approval
│   ├── Backgrounds/
│   └── UI/
└── Prefabs/
    ├── Combat/
    └── UI/
```

## Namespaces

| Folder | Namespace |
|--------|-----------|
| Combat/* | `FracturedChorus.Combat.*` |
| UI/* | `FracturedChorus.UI` |
| Data/* | `FracturedChorus.Data` |
| RunMap/* | `FracturedChorus.RunMap` (future) |
| Narrative/* | `FracturedChorus.Narrative` (future) |

## Rules

- Gameplay logic → **`.cs` scripts only**; scene holds bootstrap + SO refs.
- New art → check `docs/ASSET_INVENTORY.md` before import.
- Session log → `docs/PROJECT_LOG.md` on GitHub repo.

See also: `Scenes/SCENE_SETUP.md`, GitHub `docs/setup/UNITY_WORKFLOW.md`.

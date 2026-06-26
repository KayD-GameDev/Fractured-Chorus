# Fractured Chorus — Unity assets root

Canonical code & content live under `Assets/FracturedChorus/`.  
Design docs: GitHub repo `fractured-chorus` (`docs/`).

## Combat prototype (cập nhật 2026-06-25)

| Tính năng | Script chính |
|-----------|--------------|
| EXECUTE → bắt đầu round | `CombatExecuteOverlayUIView`, `CombatController.StartRound` |
| Kéo formation (trước EXECUTE) | `BoardDragController`, `GridCellMarker` DropGlow |
| Click unit → skill panel (sau EXECUTE) | `BoardDragController` + `SkillPanelUIView` |
| Thẻ party (avatar + HP + hệ) góc trái trên | `PartyStatusBarUIView`, `PartyMemberCardView` |
| Stat / dmg / crit | `UnitStatBlockSO`, `DamageCalculator`, `HarmonyElementResolver` |
| Target cột front | `CombatTargetPicker` |
| Scene layout giữ nguyên khi Play | `SceneAuthoringPolicy`, `respectSceneAuthoring` |

**Playtest:** mở `Scenes/CombatPrototype.unity` → menu **Apply All Play-Ready Updates** → Save → Play.

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
│       └── Presets/        # README + Unit / skill / stat block .asset
├── Resources/              # StatBlocks, Skills, UnitPresets (runtime load)
├── Scenes/                 # CombatPrototype + SCENE_SETUP.md
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
- **Hierarchy = source of truth** cho layout; rebuild chỉ qua menu Editor.
- New art → check `docs/ASSET_INVENTORY.md` before import.
- Session log → `docs/PROJECT_LOG.md` on GitHub repo.

See also: `Scenes/SCENE_SETUP.md`, GitHub `docs/setup/UNITY_WORKFLOW.md`.

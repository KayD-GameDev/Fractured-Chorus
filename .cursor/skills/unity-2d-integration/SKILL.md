---
name: fc-unity-2d-integration
description: >-
  Imports Fractured Chorus 2D assets into Unity 6 URP — sprites, animators, presets,
  meta GUID hygiene — without overwriting scene-authored layout.
disable-model-invocation: true
---

# Fractured Chorus — Unity 2D Integration

## When to use

- New PNG/PSD landed in `Assets/FracturedChorus/Art/` and must wire to gameplay or UI.

## Steps

1. Drop file under correct tree (`Art/Characters/`, `Art/UI/`, etc.) — `2d-asset-standards.mdc`.
2. Let Unity generate or preserve `.meta` — **commit** meta with asset; never orphan GUID in scene.
3. Texture importer: Sprite mode, appropriate PPU, mesh type for small sprites.
4. Assign to **Prefab/Scene in Editor** — respect rule: no C# that reapplies RectTransform layout for existing UI (`unity-2d-pipeline.mdc`).
5. `UnitPresetSO` / ScriptableObjects: reference new sprite fields as designed in codebase — do not hardcode layout.

## URP / packages

- URP 17.x; 2D packages listed in [Documentation/Art_Pipeline/FRACTURE_CHORUS_ART_PIPELINE_AUDIT.md](Documentation/Art_Pipeline/FRACTURE_CHORUS_ART_PIPELINE_AUDIT.md).

## Rules

- `unity-2d-pipeline.mdc`, `animation-standards.mdc` when hooking clips.

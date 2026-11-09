---
name: fc-ui-asset-generation
description: >-
  Generates or specifies Fractured Chorus UI frames, icons, skill illustrations,
  and HUD elements — holo/neon, readable at game resolution.
disable-model-invocation: true
---

# Fractured Chorus — UI Asset Generation

## When to use

- Stat menu, combat chrome, Resonance Dive, tutorial glyphs, skill icons.

## Steps

1. Read existing kit under `Assets/FracturedChorus/Art/UI/` for tone (CrystalKit, PartyCard, etc.).
2. Apply `fracture-chorus-art-style.mdc` — cool cyan glass, not magenta-dominant.
3. Pixel alignment: prefer even dimensions; sharp edges for neon lines (`2d-asset-standards.mdc`).
4. If UI shows a character portrait: use assets from CHARACTER_LOCK paths or generate with `fc-character-consistency` skill.
5. **Unity:** layout stays in scene — do not script RectTransform overrides for authored menus (`unity-2d-pipeline.mdc`).

## Deliverables

- PNG with alpha; slice names matching hierarchy in relevant `LAYER_SETUP.md` if present (e.g. Resonance Dive).

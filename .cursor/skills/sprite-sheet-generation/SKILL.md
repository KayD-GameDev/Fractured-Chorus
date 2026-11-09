---
name: fc-sprite-sheet-generation
description: >-
  Defines and exports sprite sheets for Fractured Chorus — grid, pivot, padding,
  and naming for Animator-ready imports.
disable-model-invocation: true
---

# Fractured Chorus — Sprite Sheet Generation

## When to use

- New combat idle loop, walk, attack strip, or VFX-aligned character strip.

## Steps

1. Confirm target: frame size, FPS, loop points with animation lead (`animation-standards.mdc`).
2. Build sheet: uniform grid; **padding** ≥ 2 px between cells unless shader compensates; consistent pivot (usually feet for full-body).
3. Naming: `2d-asset-standards.mdc`; one sheet per state or clearly separated rows in doc.
4. Unity: single texture → Sprite Mode **Multiple**; slice in editor; preserve `.meta` after first import.
5. Replace temporary `N_Corp_*` / `Lobotomy_*` / `LCB_*` strips when swapping to FC-owned art (see `Documentation/Art_Pipeline/FRACTURE_CHORUS_ART_PIPELINE_AUDIT.md`).

## Tools

- Aseprite or PSD export; Unity `2d.aseprite` importer or PNG + manual slice.

## Rules

- `animation-standards.mdc`, `unity-2d-pipeline.mdc`.

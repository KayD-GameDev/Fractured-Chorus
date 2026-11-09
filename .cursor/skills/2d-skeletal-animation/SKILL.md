---
name: fc-2d-skeletal-animation
description: >-
  Plans or implements skeletal 2D for Fractured Chorus using Unity 2D Animation
  or future Spine — mesh, weights, constraints, export discipline.
disable-model-invocation: true
---

# Fractured Chorus — 2D Skeletal Animation

## When to use

- Decision or implementation of bone-driven 2D instead of pure frame strips.

## Current project note

- `com.unity.2d.animation` is in `Packages/manifest.json`; **Spine is not** in manifest — confirm with lead before depending on Spine runtime.
- Repo audit did not show `SpriteSkin` in `.meta` — verify in Editor if rigging is active.

## Unity 2D Animation path

1. Prepare mesh-friendly art (limbs separable or soft deforms acceptable by style).
2. Rig in Unity **Skinning Editor**; limit influences per vertex per performance target.
3. Export animations as clips; keep rig source under version control with PSD/png source.

## Spine path (if adopted later)

1. Add official Spine Unity package per runbook; separate `Spine/` or `Art/Spine/<Name>/`.
2. Runtime: `SkeletonAnimation`; naming convention documented in `Documentation/Art_Pipeline/`.

## Rules

- `animation-standards.mdc`, `unity-2d-pipeline.mdc`, `character-consistency.mdc`.

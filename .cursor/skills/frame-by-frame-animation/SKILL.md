---
name: fc-frame-by-frame-animation
description: >-
  Produces frame-by-frame 2D animation for Fractured Chorus using PSD layers, sheets,
  or sequential PNGs with Unity Animator clips.
disable-model-invocation: true
---

# Fractured Chorus — Frame-by-Frame Animation

## When to use

- Character state loop, hit reaction, skill windup, ambient idle polish.

## Pipeline

1. Storyboard 3–5 keys → tie down timing (default **12 fps** gameplay unless specified in `animation-standards.mdc`).
2. Source: PSD layers (`2d.psdimporter`) or Aseprite timeline → export sheet (`fc-sprite-sheet-generation` skill).
3. **Lock:** character silhouette must match `CHARACTER_LOCK.md` across frames.
4. Animator: reuse pattern under `Art/Characters/<Name>/Animation/`; clip naming consistent with existing `*.anim`.

## QA

- No sliding feet on idle unless intentional; loop clean.
- Replace third-party ref frames before ship.

## Rules

- `animation-standards.mdc`, `character-consistency.mdc` when character visible.

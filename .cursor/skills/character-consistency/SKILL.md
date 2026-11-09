---
name: fc-character-consistency
description: >-
  Enforces CHARACTER_LOCK when generating or editing Fractured Chorus character art.
  Use for any portrait, bust, sprite, or BG that shows a locked character identity.
disable-model-invocation: true
---

# Fractured Chorus — Character Consistency

## When to use

- Image gen, edit, or review for Ren, Charlotte, Coda, or any folder with `CHARACTER_LOCK.md`.
- User reports drift (face, hair, outfit, proportions).

## Hard prerequisites

1. Open `Assets/FracturedChorus/Art/Characters/<Name>/CHARACTER_LOCK.md`.
2. Load canonical reference images at paths listed in the lock (if files exist).
3. Apply `.cursor/rules/character-consistency.mdc` and `character-art-lock.mdc`.

## Checklist before sign-off

- Face: hair shape, bangs, eye color, skin — match lock.
- Outfit: every layer named in lock; props (SyncPod, badges, weapon silhouette) match.
- Forbidden lines in lock: not violated (e.g. wrong headphones, wrong eye color).
- Canvas: bust **1024×1536** when lock specifies; padding left/right per README.
- After new file: update lock paths; preserve `.meta` GUID when replacing.

## Storage (consistency system)

| Data | Where |
|------|--------|
| Written lock | `CHARACTER_LOCK.md` |
| Optional ref dumps | `Art/Characters/<Name>/_Refs/` or `_Consistency/` — add paths into lock when introduced |

## Optional tools

- Image gen: user skill `ai-image-generation` or `gpt-image-2`; always attach lock excerpt + ref paths in the prompt.

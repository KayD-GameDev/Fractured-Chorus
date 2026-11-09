---
name: fc-background-art
description: >-
  Creates environment and background art for Fractured Chorus — Lumina urban, depth,
  controlled neon. Use for BG, CG stills, or RunMap/VN backdrops.
disable-model-invocation: true
---

# Fractured Chorus — Background Art

## When to use

- City exteriors, interiors, Cadence-flavored spaces, narrative CG.

## Steps

1. Apply `fracture-chorus-art-style.mdc`: `#0B1220` depth, `#00D4FF` / `#8CD9FF` accents, sparse `#C9A84E`.
2. Establish **read hierarchy:** foreground silhouettes → mid urban read → sky/fog.
3. If characters appear in BG art: full **character-consistency** pass (lock + refs).
4. Export/target: see `Assets/FracturedChorus/Art/Backgrounds/` naming; match existing samples’ mood without copying third-party locations.

## Rules

- `2d-asset-standards.mdc` (resolution per screen brief; avoid magenta wash).

## Notes

- Separate mock comp from ship layers when iterating (per project convention: mock outside `Art` or clearly tagged).

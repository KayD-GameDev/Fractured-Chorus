---
name: fc-character-design
description: >-
  Defines character brief and visual identity for Fractured Chorus before asset production.
  Use when designing a new cast member, alternate outfit, or updating CHARACTER_LOCK
  with user approval.
disable-model-invocation: true
---

# Fractured Chorus — Character Design

## When to use

- New playable or major NPC.
- Canon outfit change approved by design lead.
- Need a structured brief before concept art or AI generation.

## Prerequisites

- Read `Assets/FracturedChorus/Art/Characters/README_CHARACTER_LOCK.md`.
- If character exists, read `Assets/FracturedChorus/Art/Characters/<Name>/CHARACTER_LOCK.md` — do not create a second lock.

## Steps

1. **Brief:** role, age read, silhouette keywords, personality read for acting (VN + combat).
2. **Identity table:** face, hair, eyes (exact hue), skin, distinctive props — align with global style in `.cursor/rules/fracture-chorus-art-style.mdc`.
3. **Outfit layers:** list outer → inner; hex approximations for cloth and trim; forbidden items (e.g. wrong headphone type).
4. **Output:** one `CHARACTER_LOCK.md` (new) or a **draft section** for user to merge into existing lock — include canonical path placeholders for future ref PNGs.

## Rules to apply

- `character-consistency.mdc`, `fracture-chorus-art-style.mdc`, `character-art-lock.mdc`.

## Optional

- Personal skills: `drawing-2d-characters-reference-style`, `persona-5-character-art-style` (reference only — FC palette overrides).

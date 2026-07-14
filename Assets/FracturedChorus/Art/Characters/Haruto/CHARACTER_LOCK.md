# CHARACTER LOCK — Haruto

> **Do not create a second lock.** Update this file only when identity/outfit/props change by design.

## Identity
| Field | Value |
|-------|--------|
| ID | `Haruto` |
| Display | Haruto |
| Role | Office worker / Resonance Dive victim (Opening) |
| Age look | Mid–late 20s |
| Build | Slim, tired professional |

## Face / Hair
- Short dark brown–black hair, slightly messy bangs, soft center part
- Brown eyes, often tired / under-eye shadow
- Pale fair skin
- Clean anime VN face (Persona-adjacent technique, original character)

## Outfit (default Opening / VN bust)
- Charcoal / dark grey suit jacket + matching trousers
- White dress shirt (subtle light geometric pattern OK)
- Navy blue necktie (slightly loose OK)
- Black belt, silver rectangular buckle
- No school uniform

## SyncPod SP-01 (left ear, viewer’s right)
| Mode | When | Look |
|------|------|------|
| **Blue / Normal** | `neutral`, `startled` | Circular metal pod, **blue** waveform screen, thin black cable down collar |
| **Red / Resonance Dive** | `pain`, `agony`, `desperate`, `fear` | Same pod shape; screen **red** glow + red waveform / Resonance Dive energy (match `SyncPod_SP01_ResonanceDive_Red.png`) — intact on busts unless scene asks broken |

Prop refs:
- `Art/Props/SyncPod/SyncPod_SP01_Normal_Blue.png`
- `Art/Props/SyncPod/SyncPod_SP01_ResonanceDive_Red.png`
- `Art/Props/SyncPod/SyncPod_SP01_DesignSheet_v4.png`

## Bust framing
- Canvas: **1024×1536** PNG, transparent BG
- Waist-up bust; **do not** clip shoulders/arms on canvas edges (padding left/right)
- No white fringe / checkerboard leftovers
- Paths:
  - `Art/Characters/Haruto/VnBust/haruto_bust_<expr>_v1.png`
  - Portrait fallback neutral: `Art/UI/Narrative/Portraits/haruto_bust_neutral_v1.png` (keep in sync with VnBust neutral)

## Expressions (Opening)
| ID | Mood | SyncPod |
|----|------|---------|
| `neutral` | Tired, downcast | Blue |
| `startled` | Shock | Blue |
| `pain` | Eyes shut / grit teeth | **Red** |
| `agony` | Screaming / extreme pain | **Red** |
| `desperate` | Wide watery eyes, reaching | **Red** |
| `fear` | Panic sweat | **Red** |

## Other refs
- Fullbody: `Art/Characters/Haruto/haruto_office_fullbody_v1.png`
- Sheet: `Art/Characters/Haruto/haruto_vn_bust_expressions_sheet_v1.png`
- Crime BG close: `Art/Backgrounds/lumina_alley_haruto_body_close_v1.png` (broken red SyncPod OK on BG only)

## Speaker asset
- `Data/ScriptableObjects/Narrative/Speakers/Speaker_Haruto.asset`
- Speaker ID: `haruto`

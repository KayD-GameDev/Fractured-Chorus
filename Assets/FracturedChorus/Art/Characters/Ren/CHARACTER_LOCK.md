# CHARACTER LOCK — Ren

> **Do not create a second lock.** Update only when identity/outfit changes by design.

## Identity
| Field | Value |
|-------|--------|
| ID | `Ren` |
| Display | Ren |
| Role | Protagonist / HIMA newcomer |
| Age look | Late teens |
| Build | Slim student |

## Face / Hair
- Messy layered black hair, bangs over forehead
- Cool **grey** irises (cool gaze) — not red, not bright blue, not glowing
- Pale skin

## Outfit (school / Opening)
- Dark navy–black blazer with light-blue collar trim
- White collared shirt
- Dark blue tie with light-blue waveform graphic near tip
- Gold musical-note pin on left lapel
- SyncPod on ear: small circular ear device, **blue** waveform for standard VN / school lines (unless a future lock update says otherwise)
- **No** over-ear headphones around the neck

## Full body (school / menu)
- Path: `Art/Characters/Ren/School/ren_hima_uniform_menu_fullbody_v1.png`
- Must match Face / Outfit lock (grey eyes + SyncPod on ear)

## Title screen pose
- Path: `Art/UI/TitleScreen/SheetV1/char_ren_title_pose_v1_alpha.png`
- Same school identity; reach pose (left palm toward camera, right hand in pocket)
- Fitted **black leather gloves** on both hands (title keyart only)
- Cool smirk; grey irises

## Config pose
- Clean: `Art/Characters/Ren/School/ren_config_pose_v1.png`
- Overlay: `Art/Characters/Ren/School/ren_config_pose_fx_v1.png` (cùng pose; hào quang + viền hologram; **không** pha lê; tóc sót phông nhuộm hồng/holo; mỏm tay trái rè/glitch)
- Same school identity (grey irises, SyncPod, gold note pin, waveform tie, light-blue collar trim)
- Floating lean-back; right arm reach with **fingerless glove on right hand only** (config keyart)
- Left arm tucked behind torso

## Bust framing
- Prefer 1024×1536 PNG transparent; clean alpha
- Default portrait: `Art/UI/Narrative/Portraits/ren_school_bust_neutral_v1.png`
- Expression set: `Art/Characters/Ren/VnBust/`

## Expressions
| Id | Use |
|----|-----|
| `neutral` | default cool gaze |
| `startled` | bất ngờ — subtle widen, still composed |
| `smile` | cười — cool smirk, not soft grin |
| `curious` | thắc mắc — raised brow |
| `annoyed` | khó chịu — cold glare |

Tone: keep **ngầu** — restrained face acting, no cartoon exaggeration.

## Combat party card
- Path: `Art/UI/Combat/Characters/Ren_Clear_charCard.png`
- Grammar: tilted cyan diamond + name **Ren** + two bar slots (top = HP, bottom = Prep 3-segment rectangles)
- Wired via `UnitPreset_Ren.combatCardSprite` → EmbeddedBars skin on party status bar
- Display only via `CardArt` (no Border / Avatar chrome)
- Element circle: top-right of card (Melody badge)
- Card size follows scene CardTemplate height × art aspect (no squash)
- Bar stack rotation: −18° (matches art slots slanting down left→right)

## Speaker
- `Speaker_Ren.asset` — id `ren` — `IsProtagonist` / right-slot preference in dual portrait

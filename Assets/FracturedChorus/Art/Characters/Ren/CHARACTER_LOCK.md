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
- Portrait: `Art/UI/Combat/Characters/Avatars/ren_party_avatar_v1.png` (bust only; cut from Clear card)
- Chrome (shared): `Art/UI/Combat/PartyCard/` — CardBg jagged, AccentShard diamond, bar track
- Hierarchy: CardBg / AccentShard / Avatar / NameLabel / BarStack (HP number+bar, PREP number+pips) / ElementBadge
- Wired via `UnitPreset_Ren.combatCardSprite` → Avatar Image on party status bar
- Element circle: top-right of card (Melody badge)
- Card size follows scene CardTemplate (~240×120); BarStack rotation authored in Hierarchy

## Combat weapon — Violin Rifle (Súng Trường Violin)

> Sheet: `Art/Characters/Ren/Combat/ren_weapon_violin_rifle_concept_v1.jpg`  
> Tagline: *Music breaks the silence* · *Every shot is a note*

**Do not** draw a concert violin, a pistol, or a generic black fiddle. Combat art uses this hybrid only.

| Part | Lock |
|------|------|
| Silhouette | Long **sniper / marksman rifle** — stock, receiver, long barrel, muzzle |
| Body | Violin waist as receiver; circular glowing **sound-hole core** with a **treble clef** |
| Barrel | Violin neck as barrel; **4 glowing strings** run stock→muzzle |
| Bridge | Mechanical violin-bridge at the trigger / projectile gate |
| Stock | Lower violin bout, segmented futuristic shoulder stock |
| Scope | Slim high-tech optic on the top rail |
| Finish (Basic) | Matte **white plates** + charcoal/black frame |
| Energy | **Violet / purple** glow (not cyan, not orange) |

**Modes (icons / VFX)**
- **Normal:** sharp triangular violet note-darts from the muzzle
- **Skill:** weapon wrapped in swirling purple aura + translucent **music-stave rings** + particles

**Forbidden:** over-ear headphones on combat Ren (school lock = SyncPod only). Sheet extra colorways (Black / Pastel) are variants — default combat = **Basic**.

## Combat skill icons
| Skill | Id | Icon |
|-------|-----|------|
| Strike | `ren_basic` | `Art/UI/Skills/Ren/ren_skill_icon_strike_v2.png` |
| Crosscut | `ren_skill` | `Art/UI/Skills/Ren/ren_skill_icon_crosscut_v2.png` |
| Finale | `ren_ult` | `Art/UI/Skills/Ren/ren_skill_icon_finale_v2.png` |

Style: circular crystal-glass emblem (cyan–lavender rim), matte cel, bright interior, **heavy purple aura framing**; weapon = Violin Rifle lock; Ren = faint silhouette only (face not required).

## Speaker
- `Speaker_Ren.asset` — id `ren` — `IsProtagonist` / right-slot preference in dual portrait

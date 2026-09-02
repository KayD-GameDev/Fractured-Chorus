# CHARACTER LOCK — Astra Vale

> **Do not create a second lock.** Update only when identity/outfit changes by design.
> Three outfits share the same face identity: Stage (LUXE), School (HIMA), Cadence Boss (dark).

## Identity
| Field | Value |
|-------|--------|
| ID | `Astra` |
| Display | Astra Vale |
| Role | Pulse bond · LUXE idol / HIMA campus guide · Arc 1 Cadence boss (Chart Lord) |
| Age look | Late teens · polished idol |
| Build | Slim elegant |

## Face / Hair (shared)
- Very long straight platinum-blonde / silvery-white hair (mid-thigh length OK)
- Soft bangs; optional soft pink underlights (Stage/School) or colder highlights (Cadence Boss)
- Large **golden amber** eyes
- Stage/School: confident friendly idol expression — warm
- Cadence Boss: cold arrogant idol-tyrant glare / smirk — not warm

## Outfit A — Stage / LUXE
- White sleeveless vest/corset with gold trim, gold buttons, gold star pin on left chest
- Gold/white choker with large gold star pendant
- Left: long white detached sleeve with gold bands; right: white wrist cuff
- Short white pleated skirt (gold hem, pink inner pleats OK) + gold star chains
- White thigh-high stockings + gold star garter chains
- White platform boots with gold soles/heels and star buckles
- **No** SyncPod, **no** school blazer
- Path: `Art/Characters/Astra/Stage/astra_luxe_stage_menu_fullbody_v1.png`
- Ref: `Art/Characters/Astra/astra_fullbody_ref_v1.png`

## Outfit B — Real world / HIMA school
- Dark navy–black blazer with gold or soft pink accent piping OK
- White collared shirt
- Gold or soft pink necktie
- Circular blue **HIMA** badge with white anchor on left lapel
- Dark / black **pleated skirt**
- **White** sheer or opaque stockings (school — not black)
- Black or white loafers / low heels OK
- SyncPod on ear: circular pod, **blue** waveform + **thin black cable** down to collar
- Path: `Art/Characters/Astra/School/astra_hima_uniform_menu_fullbody_v1.png`

### Title screen pose
- Path: `Art/UI/TitleScreen/SheetV1/char_astra_title_pose_v1_alpha.png`
- Natural contrapposto; tablet in one hand, free hand relaxed
- School identity (pink tie, white stockings, HIMA badge, SyncPod)

## Outfit C — Cadence Boss (luminous dark / Arc 1)
- High-contrast **bright + dark** idol-boss suit (not pure all-black): black structured panels mixed with luminous **white** panels
- Pleated skirt mixes black and white; gold hem; crimson inner flash OK
- Asymmetric sleeves OK (one black, one white)
- Stockings: black/white contrast (mismatched or white with black bands) + bright gold star garters
- Boots: high-contrast black/white with gold or crimson star buckles; strong specular on gold/stars
- Choker: black band + bright gold fractured star
- Lighting: strong key + rim so whites/gold read luminous against deep black
- Expression: cold idol-tyrant (boss), not warm Stage smile
- **No** school blazer, **no** SyncPod required; **no** Persona logos/masks/UI
- Path: `Art/Characters/Astra/Cadence/astra_cadence_boss_menu_fullbody_v1.png`
- Art note: Persona 5–inspired *technique* (Atlus illustration grammar) — original character only

## Art / proportion
- Same polish family as Ren school fullbody (~8 heads, cel + rim)
- Full body must be one continuous figure — hips and legs correctly joined

## Combat icon (boss) — legacy baked composite
- Path: `Art/UI/Combat/Characters/astra_character_icon_bars_boss_v1.png`
- Layout: tilted diamond + name stack + two bars; diamond uses boss crimson/magenta (not party cyan-blue)
- Facing: toward **left** (boss side; opposite party icons that face right)
- Name stack: small **"Chart Lord"** above large **"Astra"**
- Expression: cunning / sly smirk (gian xảo)
- Face/outfit: Cadence Boss luminous-dark identity

## Combat enemy card
- Portrait: `Art/UI/Combat/Characters/Avatars/astra_enemy_avatar_v1.png` (bust only; faces left)
- Chrome (shared): `Art/UI/Combat/PartyCard/` — CardBg jagged (mirrored), AccentShard crimson, bar track
- Hierarchy: CardBg / AccentShard / Avatar / NameLabel / BarStack (HP number+bar, PREP number+pips) / ElementBadge
- Wired via `UnitPreset_Boss_Despair.combatCardSprite` → Avatar Image on enemy status bar
- Element circle: top-right of card; HP fill red; tilt +6°
- Card size follows scene CardTemplate (~240×118)

## Mini-boss target cards
| Target | Name on card | Portrait (bust) |
|--------|--------------|-----------------|
| **Mắt** (EYE) | AstraEye | `Art/UI/Combat/Characters/Avatars/astra_eye_enemy_avatar_v1.png` |
| **Micro** | AstraMic | `Art/UI/Combat/Characters/Avatars/astra_mic_enemy_avatar_v1.png` |
- Same modular chrome as Astra enemy card. Legacy composites: `astra_miniboss_eye_icon_v1.png`, `astra_miniboss_mic_icon_v1.png`

## Tone
- Stage/School: bright, welcoming idol — Pulse key energy
- Cadence Boss: commanding, cold Chart Lord presence

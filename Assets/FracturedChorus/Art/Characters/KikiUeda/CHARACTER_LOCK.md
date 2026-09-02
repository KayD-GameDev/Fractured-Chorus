# CHARACTER_LOCK — Kiki Ueda

## Identity
- Name: Kiki Ueda
- Role: Elite Cadence mini-boss (Floor 1 gate)
- Form: Four-legged smoke beast (Cadence corruption)

## Visual
- Silhouette: quadruped, heavy forelimbs, low center of gravity
- Palette: ash charcoal, dried-blood red, pale bone highlights
- Atmosphere: smoke / war-front haze

## Combat art paths
- Idle: `Art/Characters/KikiUeda/kiki_ueda_idle_v1.png`
- Hurt: `Art/Characters/KikiUeda/kiki_ueda_hurt_v1.png`
- Dead: `Art/Characters/KikiUeda/kiki_ueda_dead_v1.png`
- Evade: `Art/Characters/KikiUeda/kiki_ueda_evade_v1.png`
- Move: `Art/Characters/KikiUeda/kiki_ueda_move_v1.png`

## Combat icon (Cadence elite card) — legacy baked composite
- Path: `Art/UI/Combat/Characters/kiki_ueda_character_icon_bars_elite_v1.png`
- Layout: same grammar as Astra boss icon — tilted crimson diamond + name stack + two bars
- Title / Name: **"Smoke Beast"** / **"Kiki"**
- Facing: toward **left** (enemy side)
- Subject: beast bust (armor + red smoke fur + wax seal), not human

## Combat enemy card
- Portrait: `Art/UI/Combat/Characters/Avatars/kiki_enemy_avatar_v1.png` (bust only; faces left)
- Chrome (shared): `Art/UI/Combat/PartyCard/` — CardBg jagged (mirrored), AccentShard crimson, bar track
- Hierarchy: CardBg / AccentShard / Avatar / NameLabel / BarStack (HP number+bar, PREP number+pips) / ElementBadge
- Wired via `UnitPreset_Kiki_Ueda.combatCardSprite` → Avatar Image on enemy status bar
- Element circle: top-right of card; HP fill red; tilt +6°

## Notes
- Full-body combat sprite; keep feet readable on muddy BG.
- Do not redesign silhouette without updating this lock.

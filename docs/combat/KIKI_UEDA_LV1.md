# Kiki Ueda — Lv1 Elite (Floor 1 mini-boss)

> First Cadence gate / tutorial foe. Scales later as Floor 1 mini-boss.

## Design intent

- Teach Deploy / Plan / Execute against **one** readable threat.
- Party: Ren + Coda (basics only).
- Feel: heavier than a grunt, softer than Knight of Despair.

## Lv1 stats

| Stat | Value | Rationale |
|------|-------|-----------|
| Role | Elite | Floor gate, not sector boss |
| Element | Rhythm | Smoke-war / Cadence beast |
| STR | 28 | Above Ren Lv1 (22), well below grunt prototype (60) |
| EN | 6 | Slightly tougher than Ren (4) |
| HB | 128 | Slower than Ren (145) → readable telegraphs |
| Luck | 7% | Light crit spice |
| Crit | ×1.18 | |
| HP | 260 | ~1.7× grunt (150); duo Ren+Coda (~112 HP pool) can clear in a few rounds |
| Speed | 10 | |
| Telegraphs / phase | 2 | Elite pressure without boss slam |

## Skills

| Id | Name | Slot | Delay | Note |
|----|------|------|-------|------|
| `kiki_claw` | Claw | Basic | 2 | Main teaching hit |
| `kiki_smoke_rend` | Smoke Rend | Skill | 3 | Identity lunge / heavier window |

Ult reserved for later floor scaling.

## Assets

- Preset: `Resources/UnitPresets/UnitPreset_Kiki_Ueda.asset`
- Stat block: `Resources/StatBlocks/StatBlock_Kiki_Ueda.asset`
- Art: `Art/Characters/KikiUeda/`
- Combat icon: `Art/UI/Combat/Characters/kiki_ueda_character_icon_bars_elite_v1.png` (Smoke Beast / Kiki — Astra card grammar)
- Tutorial scene: `Scenes/CombatTutorial.unity`
- BG: `Art/Backgrounds/cadence_smoke_war_front_bg_v1.png`

## Later (Floor 1 mini-boss)

- Raise HP / STR with floor curve; unlock Ult.
- Keep silhouette + Rhythm identity from CHARACTER_LOCK.

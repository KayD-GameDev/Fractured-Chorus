# ScriptableObject presets

## Stat blocks (chỉnh tay)

**Create → Fractured Chorus → Unit Stat Block** — hoặc menu **Fractured Chorus → Create Default Stat Blocks & Presets**.

Baseline người chơi = **Lv15 optimal** (xem `docs/combat/CHARACTER_LEVEL_PROGRESS.md`).

| Asset | Element (pre-condition) | Strength (attack power) | HB | EN | Luck% | Crit | HP |
|-------|-------------------------|-------------------------|----|----|-------|------|----|
| `StatBlock_Ren` | Melody | **Physical** · 42 | 167 | 10.8 | 18 | ×1.35 | 114 |
| `StatBlock_Tank` (Charlotte) | Rhythm | **Physical** · 35 | 127 | 18.2 | 8 | ×1.15 | 260 |
| `StatBlock_Mage` (Coda) | Harmony | **Magical** · 50 | 147 | 9.8 | 16 | ×1.3 | 73 |
| `StatBlock_Grunt` | Rhythm | **Physical** · 60 | 120 | 8 | 5 | ×1.1 | 150 |

Trong **Unit Stat Block**: chọn **Damage Type** (Physical/Magical) trước, rồi nhập **Strength**. Combat dùng loại dmg từ stat block — không còn field Magic riêng.

Nhiều **Unit Preset** có thể trỏ cùng một **Stat Block**.

## Unit & skill presets

| Type | Path |
|------|------|
| **Unit Preset** | `Resources/UnitPresets/UnitPreset_*.asset` |
| **Stat Block** | `Resources/StatBlocks/StatBlock_*.asset` |
| **Skill Definition** | `Resources/Skills/*.asset` |
| **Encounter** | `Create → Fractured Chorus → Encounter Definition` |

## Damage formula (doc)

- Random tier 1: 0.80–1.05 · tier 2: 0.90–1.10 · tier 3: 1.10–1.50
- Raw = random × Strength × **10**
- Final = raw × **100/(100×4×√EN)** × beat × pre-condition × critMult
- **Base Luck** = % crit chance (0–100) mỗi lần skill gây dmg

Nếu chưa có asset, `EncounterRuntimeFactory` tạo preset runtime khi Play.

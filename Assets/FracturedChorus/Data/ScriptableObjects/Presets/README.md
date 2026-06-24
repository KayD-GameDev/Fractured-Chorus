# ScriptableObject presets

## Stat blocks (chỉnh tay)

**Create → Fractured Chorus → Unit Stat Block** — hoặc menu **Fractured Chorus → Create Default Stat Blocks & Presets**.

| Asset | Element (pre-condition) | Ghi chú |
|-------|-------------------------|---------|
| `StatBlock_Ren` | Melody | **Physical** · Strength 100 |
| `StatBlock_Tank` | Rhythm | **Physical** · Strength 80 |
| `StatBlock_Mage` | Harmony | **Magical** · Strength 100 |
| `StatBlock_Grunt` | Rhythm | **Physical** · Strength 60 |

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

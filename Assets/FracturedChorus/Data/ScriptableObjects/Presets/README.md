# ScriptableObject presets

Create assets via **Create → Fractured Chorus → …**

| Type | Example assets |
|------|----------------|
| **Unit Preset** | `Ren`, `Tank`, `Mage`, `Grunt` |
| **Skill Definition** | `ren_attack`, `ren_rush`, `grunt_strike` |
| **Encounter Definition** | `demo_encounter_01` |

Wire skills on each Unit Preset, then list spawns (side, row 0–2, col 0–2) on Encounter.

If no assets assigned, `CombatPrototypeBootstrap` builds a demo encounter at runtime (`EncounterRuntimeFactory`).

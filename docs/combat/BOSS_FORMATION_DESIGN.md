# Boss Formation Design

Player grid uses three columns (display C1–C3). Positional combat modifiers:

| Column | Role | Modifier |
|--------|------|----------|
| **C1 front** | Tank lane | Incoming damage ×0.85 |
| **C2 mid** | Neutral | ×1.0 |
| **C3 back** | DPS / support lane | Outgoing damage ×1.15 · heal potency ×1.15 |

These stack with boss target bias and difficulty pierce scaling.

---

## BossFormationProfile

ScriptableObject fields (`BossFormationProfileSO`):

| Field | Type | Notes |
|-------|------|-------|
| `frontTargetWeight` | float | Enemy target pick bias toward column 0 (≥1 = front preference) |
| `backPierceChance` | float 0–1 | Roll to ignore front bias and strike back column only |
| `columnSlamColumn` | int | Column index for slam telegraph focus; **−1** = none |
| `formationDisrupt` | enum | `None` · `ForceSwapAdjacent` · `PinColumn` |
| `pressureSummary` | string | Deploy-phase strip copy (boss pressure hint) |

Runtime holder: `BossFormationRuntime` — active profile per encounter; `ApplyDifficultyScale(float pierceFrontBiasMult)` scales pierce chance and front bias from `DifficultyRuntime`.

Target resolution: `CombatTargetPicker.PickEnemyAttackTarget(grid, profile)` — weighted front bias with back-pierce override; standing-on-beat logic unchanged in `PickEnemyAttackTargetForBeat`, formation pick used when no standing units.

---

## Encounter defaults

### The Pulse / Boss Despair (`Encounter_Boss_Despair`)

- `frontTargetWeight`: **2.4** (high front bias)
- `backPierceChance`: **0.28**
- `columnSlamColumn`: **1** (mid column slam)
- `formationDisrupt`: **ForceSwapAdjacent**
- `pressureSummary`: *"The Pulse anchors the front — mid-column slams punish clustered lines."*

### Elite (`Encounter_Elite_Grunts`)

- `frontTargetWeight`: **1.35** (light front bias)
- `backPierceChance`: **0.12**
- `columnSlamColumn`: **−1**
- `formationDisrupt`: **None**
- `pressureSummary`: *"Elite squad favors the front row — spread before Execute."*

### Battle grunts

Neutral profile: `frontTargetWeight` 1.0 · `backPierceChance` 0.08 · no slam · `formationDisrupt` None.

---

## Deploy UI

`DeployFormationHintView` shows during player reposition (Deploy):

- Column badges: **FRONT −15% dmg taken** · **MID** · **BACK +15% dmg dealt**
- Boss pressure strip from `profile.pressureSummary`

Hidden after Deploy lock (`LockPlayerReposition`).

---

## Difficulty coupling

`DifficultyRuntime.PierceFrontBias` multiplies both `frontTargetWeight` and `backPierceChance` on the active profile at encounter start.

| Difficulty | Pierce / front bias mult |
|------------|--------------------------|
| On Beat | ×0.80 |
| Cadence | ×1.00 |
| Off Beat | ×1.15 |

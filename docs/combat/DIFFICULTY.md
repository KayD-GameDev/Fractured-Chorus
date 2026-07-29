# Difficulty

Difficulty is stored per save slot as `GameMetaState.Difficulty` (`GameDifficulty` int: 0 On Beat, 1 Cadence, 2 Off Beat).

Runtime multipliers: `Assets/FracturedChorus/Combat/Difficulty/DifficultyRuntime.cs`

## On Beat

| Stat | Multiplier |
|------|------------|
| Enemy HP | ×0.85 |
| Enemy damage | ×0.85 |
| Pierce / front bias | ×0.80 |
| Notes earned | ×1.10 |
| Planning window bonus | +1 beat |

## Cadence (default)

All combat multipliers ×1.0. No planning bonus. No Early/Late block penalty.

## Off Beat

| Stat | Multiplier |
|------|------------|
| Enemy HP | ×1.15 |
| Enemy damage | ×1.20 |
| Pierce / front bias | ×1.15 |
| Notes earned | ×1.00 |
| Early / Late block | −0.10 abs window |

Main-menu difficulty selection seeds new games; loaded saves use the value stored in the slot.

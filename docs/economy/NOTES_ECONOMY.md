# Notes Economy

Fractured Chorus uses **Notes** as the run/hub currency stored in `WalletState.Notes`.

## Earn formulas

| Source | Formula |
|--------|---------|
| Battle | `40 + floor × 5` |
| Elite | `80 + floor × 8` |
| Boss | `200 + floor × 15` |
| Treasure | `50–120` (deterministic roll from floor seed) |

Difficulty multipliers apply at payout time via `DifficultyRuntime.NotesEarn` (On Beat: ×1.1).

## Spend costs

| Service | Cost |
|---------|------|
| Camp heal | 30 |
| Relay | 50 |
| Hub heal | 40 |

## Runtime

- Wallet: `Assets/FracturedChorus/Meta/WalletState.cs`
- Tables: `Assets/FracturedChorus/Meta/Economy/EconomyTable.cs`
- Saved per slot in `GameMetaSaveData.notes`

# Difficulty — On Beat / Cadence / Off Beat

> **Lock:** Ba bậc. Cadence = số cân bằng gốc. On Beat / Off Beat **chỉ** đổi tỷ lệ combat + lệch level kẻ địch. Không thêm quái, không đổi kit, không đổi XP curve.  
> Runtime: `Assets/FracturedChorus/Combat/Difficulty/DifficultyRuntime.cs`  
> Lưu theo slot: `GameMetaState.Difficulty` (`0` On Beat · `1` Cadence · `2` Off Beat)

Chọn lúc New Game (Config). Save đã tạo giữ bậc đã khóa.

## Công thức (Cadence = gốc)

```
HP địch     = HPCadence × EnemyHp × (1 + 0.06 × LevelOffset)
Dmg địch    = DmgCadence × EnemyDamage × (1 + 0.05 × LevelOffset)
Lv boss     = 18 + LevelOffset
Lv party gợi ý = bảng dưới (cap Arc 1 vẫn 18)
```

| | On Beat | Cadence (mặc định) | Off Beat |
|--|---------|--------------------|----------|
| **LevelOffset** | −2 | 0 | +2 |
| **Party gợi ý trước boss** | Lv13 | Lv15 | Lv17 |
| **Boss hiệu lực** | Lv16 | Lv18 | Lv20 (scale; cap party vẫn 18) |
| Enemy HP ratio | ×0.85 | ×1.00 | ×1.15 |
| Enemy dmg ratio | ×0.85 | ×1.00 | ×1.20 |
| Pierce / front bias | ×0.80 | ×1.00 | ×1.15 |
| Notes thắng trận | ×1.10 | ×1.00 | ×1.00 |
| Early / Late block | 0 | 0 | −0.10 abs |
| **HP địch sau gộp** | ×0.748 | ×1.00 | ×1.288 |
| **Dmg địch sau gộp** | ×0.765 | ×1.00 | ×1.320 |

On Beat khoảng **¾** số Cadence. Off Beat khoảng **+30%**. Đủ tách Easy / Normal / Hard mà không thiết kế lại encounter.

## Cadence — số gốc

Mọi bảng stat, boss, XP trong `CHARACTER_LEVEL_PROGRESS.md` và `BOSS_ENCOUNTER_DESIGN.md` viết cho Cadence: party soft target **Lv15**, boss **Lv18**, cap **Lv18**.

## On Beat

Kẻ địch yếu hơn 2 level và nhân ×0.85. Người mới, người muốn cốt truyện, người lần đầu vào nhịp. Notes ×1.10 để bù nếu họ farm ít hơn.

## Off Beat

Kẻ địch mạnh hơn 2 level, máu ×1.15, đòn ×1.20, chặn lệch nhịp phạt thêm 10%. Party nên **Lv17** trước boss; vào Lv15 vẫn được nhưng phải chặn và counter chặt. XP dungeon không tăng — độ khó đến từ số, không từ grind bắt buộc.

## Không đổi theo bậc

- Kit 3 skill, Space chặn, Cover, Prep, QTE
- Lịch ngày, Bond, Social Stat
- Combat XP curve và soft-cap Lv15–18
- Số node / loại quái trên run map

## Runtime hook

| Chỗ | Dùng |
|-----|------|
| `PartyLoadoutApplicator.ApplyDifficultyToEnemy` | `ResolvedEnemyHp` |
| `CombatSession` (đòn địch) | `ResolvedEnemyDamage` |
| `BossFormationRuntime.ApplyDifficultyScale` | `PierceFrontBias` |
| `CombatRewardService` | `NotesEarn` |
| Config copy | `MainMenuGameSettings.GetDifficultyDescription` |

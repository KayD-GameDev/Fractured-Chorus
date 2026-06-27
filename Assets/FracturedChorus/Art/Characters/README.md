# Characters — art import

Mỗi nhân vật party = một folder tên **canon**; combat role map qua `UnitPresetSO` / `StatBlock_*`.

| Role (combat) | Tên canon | Folder | Preset / stat block |
|---------------|-----------|--------|---------------------|
| **Mage** (Staff · Hòa âm) | **Coda** | `Coda/` | `UnitPreset_Mage`, `StatBlock_Mage` |
| **DPS** (Bow · Giai điệu) | **Ren** | `Ren/` | `UnitPreset_Ren`, `StatBlock_Ren` |
| **Tank** (Shield · Nhịp) | **Charlotte** | `Charlotte/` | `UnitPreset_Tank`, `StatBlock_Tank` |

## Layout

```
Characters/
├── Coda/Animation/Idle|Move|Attack|Hit|Death/
├── Ren/Animation/Idle|Move|Attack|Hit|Death/
├── Charlotte/Animation/Idle|Move|Attack|Hit|Death/
└── _Reference/          ← sprite ref tạm (LoR idle), không dùng ship
```

- **Sprite tĩnh combat:** `{Name}/Battle/` hoặc root `{Name}/` — gán `UnitPresetSO.battleSprite`.
- **Animation clip:** subfolder trong `Animation/`; sheet PNG + Animator hoặc frame-by-frame (xem repo GitHub `assets/characters/*/animation/`).
- **Trước import:** duyệt GitHub `docs/ASSET_INVENTORY.md` + LOCK (`Coda_LOCK`, `Charlott_LOCK`, `Ren_LOCK`).

Sprite ref LoR ở root (`LCB_*`, `N_Corp_*`) — chuyển dần sang `_Reference/`; không gán preset canon.

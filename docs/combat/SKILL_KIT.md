# Skill Kit — 3 skill / nhân vật

> **Trạng thái:** Design + data lock · footprint S1-S-S2 trên timeline  
> **Code:** `SkillDefinitionSO` · `Resources/Skills/*` · `UnitPreset_*`

---

## Footprint timeline

```
[S1 wind-up] → [S active × N] → [S2 recovery]
```

- **Placement beat** = beat bắt đầu S1
- **Counter hit** = mỗi beat thuộc S active (Perfect vs nốt boss)
- **Toàn bộ S** phải nằm trong planning window W
- Cùng row: footprint không overlap

| Ký hiệu | Ý nghĩa |
|---------|---------|
| S1 trung bình | **2 beat** |
| S1 ngắn | **1 beat** |
| S2 trung bình | **2 beat** |
| S2 dài | **3 beat** |

---

## Ren — DPS · Melody · Physical

| # | Tên | S1-S-S2 | Tier | Effect | Timeline |
|---|-----|---------|------|--------|----------|
| 1 | **Strike** | 1-1-1 | 1 | Damage | 1 nốt / lần dùng |
| 2 | **Crosscut** | 2-2-2 | 2 | Damage | 2 beat S → counter tối đa 2 nốt (1 hit/beat) |
| 3 | **Finale** | 2-3-3 | 3 | Damage burst | 3 beat S → 3 nốt, S2 dài (lock row) |

**Asset:** `ren_basic` · `ren_skill` · `ren_ult`

---

## Charlotte — Tank · Rhythm · Physical

| # | Tên | S1-S-S2 | Tier | Effect | Timeline |
|---|-----|---------|------|--------|----------|
| 1 | **Ram** | 1-1-1 | 1 | Damage | Basic threat |
| 2 | **Anchor** | 2-2-2 | 2 | **DelayBossNote +2** | Beat S đầu: đẩy nốt boss @ beat đó +2 beat |
| 3 | **Bulwark** | 2-2-3 | 2 | **Shield 65 + counter** | Shield lúc S bắt đầu · dmg mỗi beat S · S2 dài |

**Asset:** `tank_basic` · `tank_skill` · `tank_ult`

---

## Coda — Support · Harmony · Magical

| # | Tên | S1-S-S2 | Tier | Effect | Timeline |
|---|-----|---------|------|--------|----------|
| 1 | **Pulse** | 1-1-1 | 1 | Damage (Ma) | 1 nốt |
| 2 | **Mend** | 2-1-2 | 2 | **Heal** 25 + Ma×0.5 | Hồi 1 ally · cast giữa S ngắn |
| 3 | **Encore** | 1-1-1 | 2 | **ReduceS2 −1** | Skill kế tiếp của 1 ally: S2 −1 beat |

**Asset:** `mage_basic` · `mage_skill` · `mage_ult`

---

## Effect kinds (`SkillEffectKind`)

| Kind | Mô tả |
|------|-------|
| `Damage` | Counter + dmg theo tier / AttackPower |
| `Heal` | `effectValue + Ma×0.5` |
| `Shield` | `effectValue` HP buffer (Bulwark: 65) |
| `ReduceS2` | Giảm S2 skill placement kế tiếp |
| `DelayBossNote` | Đẩy telegraph boss +N beat |

---

## Unlock (giữ progression doc)

| Lv | Ren | Charlotte | Coda |
|----|-----|-----------|------|
| 1 | Strike | Ram | Pulse |
| 4 | Crosscut | — | — |
| 3 | — | Anchor | — |
| 5 | — | — | Mend |
| 9 | — | Bulwark | — |
| 10 | Finale | — | — |
| 11 | — | — | Encore |

---

## Ví dụ timeline — Ren Crosscut 2-2-2 @ beat 8

| Beat | 8 | 9 | 10 | 11 | 12 | 13 |
|------|---|---|----|----|----|----|
| Phase | S1 | S1 | **S** | **S** | S2 | S2 |
| Counter | — | — | hit | hit | — | — |

Đặt @ beat 8 → footprint chiếm 8–13 (6 beat). Boss nốt @ beat 9–10 có thể bị counter 2 lần nếu timing Perfect.

---

## Chưa làm (runtime P0)

- [ ] UI hiển thị S1/S/S2 khác màu trên segment
- [ ] Counter degrade boss note HP (Tím/Xanh/Đỏ)
- [ ] Pick ally target cho Mend / Encore (hiện auto first ally)

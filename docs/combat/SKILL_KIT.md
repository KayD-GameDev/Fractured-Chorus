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

## Ren — DPS · Cycle Shift · Physical

> **Cycle Shift:** mỗi **Strike** xong → Active element xoay Melody → Rhythm → Harmony. Crosscut / Finale dùng Active @ S1, không xoay. Xem [COMBAT_MECHANICS.md §7](./COMBAT_MECHANICS.md#7-element-triangle--ren-cycle-shift).

| # | Tên | S1-S-S2 | Tier | Effect | Target |
|---|-----|---------|------|--------|--------|
| 1 | **Strike** | 1-1-1 | 1 | Damage + **Cycle Shift** | CORE · Mini |
| 2 | **Crosscut** | 2-2-2 | 2 | Damage · 2 counter hit | CORE · Mini |
| 3 | **Finale** | 2-3-3 | 3 | Damage burst · 3 counter hit | CORE · Mini |

**Asset:** `ren_basic` · `ren_skill` · `ren_ult`

### Ren — dmg tham chiếu Lv15 (avg roll · Perfect · vs The Pulse)

| Skill | Active | vs CORE | vs MICRO/EYE |
|-------|--------|---------|--------------|
| Strike | Melody | **~11** | **~14** MiniDmg |
| Strike | Rhythm | **~22** | **~14** |
| Strike | Harmony | **~33** | **~14** |
| Crosscut / beat | Melody | **~12** | **~15** |
| Crosscut / beat | Rhythm | **~23** | **~15** |
| Crosscut / beat | Harmony | **~35** | **~15** |
| Finale / beat | Harmony | **~46** | **~20** |

*Coda Harmony ×1.5 vs CORE ≈ **~39** Pulse · Charlotte Rhythm ≈ **~18** Ram.*

---

## Charlotte — Tank · Rhythm · Physical

| # | Tên | S1-S-S2 | Tier | Effect | Target |
|---|-----|---------|------|--------|--------|
| 1 | **Ram** | 1-1-1 | 1 | Damage | CORE · Mini |
| 2 | **Anchor** | 2-2-2 | 2 | **DelayBossNote +2** (CORE only) | CORE telegraph |
| 3 | **Bulwark** | 2-2-3 | 2 | **Shield 65** + counter dmg | CORE · Mini |

| Skill | vs CORE | vs MICRO/EYE |
|-------|---------|--------------|
| Ram | **~18** | **~12** MiniDmg |
| Anchor | — | — |
| Bulwark / beat | **~20** | **~13** |

**Asset:** `tank_basic` · `tank_skill` · `tank_ult`

---

## Coda — Support · Harmony · Magical

| # | Tên | S1-S-S2 | Tier | Effect | Target |
|---|-----|---------|------|--------|--------|
| 1 | **Pulse** | 1-1-1 | 1 | Damage (Ma) | CORE · Mini |
| 2 | **Mend** | 2-1-2 | 2 | **Heal** 25 + Ma×0.5 | Ally |
| 3 | **Encore** | 1-1-1 | 2 | **ReduceS2 −1** | Ally |

| Skill | vs CORE | vs MICRO/EYE |
|-------|---------|--------------|
| Pulse | **~39** (×1.5 Harmony) | **~26** MiniDmg |
| Mend | — (~50 HP heal) | — |
| Encore | — | — |

**Asset:** `mage_basic` · `mage_skill` · `mage_ult`

---

## Effect kinds (`SkillEffectKind`)

| Kind | Mô tả |
|------|-------|
| `Damage` | Counter + **CoreFinal** vs CORE |
| `MiniDamage` | Counter Perfect vs MICRO/EYE note → **MiniDmg** pool |
| `Heal` | `effectValue + Ma×0.5` |
| `Shield` | `effectValue` HP buffer (Bulwark: 65) |
| `ReduceS2` | Giảm S2 skill placement kế tiếp |
| `DelayBossNote` | Đẩy telegraph **CORE** +N beat |
| `CycleShift` | Ren Strike: xoay Active element (§7 mechanics) |
| `PurgeResonance` | Counter Micro: −1 boss `Resonance` stack |
| `PurgeDissonance` | Counter Eye: −1 party `Dissonance` stack |

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
- [ ] Counter degrade boss note HP (Tím/Xanh/Đỏ) — **CORE only**
- [ ] Note tag CORE / MICRO / EYE trên row 4
- [ ] Ren Active element + Cycle Shift animation
- [ ] Mini pressure resolve (`Resonance` / `Dissonance`)
- [ ] Pick ally target cho Mend / Encore (hiện auto first ally)

## Changelog

| Ngày | Nội dung |
|------|----------|
| 2026-06-30 | Cycle Shift · CoreFinal vs MiniDmg · dmg table · effect kinds mới |

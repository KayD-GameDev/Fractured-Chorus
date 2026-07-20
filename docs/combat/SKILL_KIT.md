# Skill Kit — 3 skill / nhân vật

> **Trạng thái:** Setup → Payoff (Prep) · Cover gauge Phase 4  
> **Spec:** Prep [`2026-07-14-skill-kit-setup-payoff-design.md`](../superpowers/specs/2026-07-14-skill-kit-setup-payoff-design.md) · Cover [`2026-07-16-cover-gauge-empty-beat-design.md`](../superpowers/specs/2026-07-16-cover-gauge-empty-beat-design.md)  
> **Code:** `SkillDefinitionSO` · `CombatUnit.Prep` · `CoverRuntime` · `CoverHudView`

---

## Prep — Setup → Payoff

```
Empty beat ∩ S (Skill/Ult)  →  +1 Prep (cap 3 / unit)
Note beat ∩ S               →  Counter; Prep không tăng
Basic                       →  Không đụng Prep
Empower Skill @ Prep ≥1     →  tiêu 1 · amplify nhẹ
Empower Ult @ Prep ≥2       →  tiêu 2 · amplify mạnh
Prep = 0                    →  vẫn cast base
```

- UI: pip cyan trên **party card** (`PrepPipsView`)
- Spend lúc **beat S đầu** của placement; channel cùng beat xảy ra **sau** spend
- **Anchor Delay / Encore ReduceS2** resolve **ngay khi đặt (Planning)** — VFX đọc được trước Execute
  - Delay: chỉ note **sau cửa S** của Anchor (+N); note nằm trong S giữ nguyên; slide trên timeline
  - Encore: `PendingReduceS2` trên ally + **icon buff** (`Resources/UI/Combat/Buffs/buff_reduce_s2_v1`) góc dưới-trái card (trên HP bar); skill đặt sau snapshot S2 ngắn hơn
  - Empower Encore: party ReduceS2 + **gift +1 Prep** ally (planning)

## Cover — Empty Beat Gauge (Ren)

```
Empty beat ∩ S (Skill/Ult, any ally)  →  +1 Cover gauge (cap 10, party)
Note beat ∩ S                         →  no Cover charge
Basic                                 →  no Cover charge
Planning stop · gauge ≥ 8 · Ren alive →  COVER button (−8, pending)
Scan / Execute after activate         →  12 beat window: party dmg ×1.25
                                        Early/Late → OnBeat (dmg + Guard)
```

- UI: `CoverHudView` trên party bar · `CoverRuntime` trên `CombatSession`
- Audio: `EternalSpark_RenCover.mp3` overlay từ **1:36.5** khi cửa mở; duck boss (không đụng beat sync)
- UI btn: scene `CoverHud` / `CoverButton` — edit RectTransform + `CoverHudView.buttonSprite` (menu **Fractured Chorus → Setup Cover HUD**) · Resources fallback `combat_btn_cover_v1`
- Tách Prep (per-unit amplify); Cover = party burst (Muse-style)
- Gate nút: `AllowCoverActivate` (Deploy reposition / planning pause / giữa segment) — không bấm lúc scan
- Playtest start: Inspector bootstrap `startCoverGauge` / `startPrepAll` (không chỉnh trên màn hình)
- Cover energy UI: `CoverEnergyGauge` — khung + 10 nấc hologram (fill dưới→trên); art `Resources/UI/Combat/Cover/`

---

## Footprint timeline — 3 pha (Standing 1 · Using · Standing 2)

```
[Standing Phase 1] → [Using Skill Phase × N] → [Standing Phase 2]
      (S1)                    (S)                     (S2)
```

- Mỗi skill **luôn 3 pha**; hai **standing phase** giúp **chống spam skill liên tục giữa các beat** — xem [COMBAT_MECHANICS.md §3](./COMBAT_MECHANICS.md#3-skill-footprint--3-pha-standing-1--using--standing-2).
- **Placement beat** = beat bắt đầu **Using (S)**; S1 nằm trước, S2 nằm sau.
- **Counter hit** = mỗi beat thuộc S active (Perfect vs nốt boss)
- **Toàn bộ S** phải nằm trong planning window W
- Cùng row: footprint không overlap
- **Số beat mỗi pha tùy skill** (cột `S1-S-S2`).
- **Data:** `standingBeatsBefore` · `activeBeats` · `standingBeatsAfter` · `effectKind` · Prep empower fields.
- **UI:** S = chip màu unit · **S1/S2 = nút tròn xám** · Encore pending → **buff icon** trên party card (không text badge).

---

## Ren — DPS · Cycle Shift · Physical

| # | Tên | S1-S-S2 | Tier | Base | Empower |
|---|-----|---------|------|------|---------|
| 1 | **Strike** | 1-1-1 | 1 | Damage + Cycle Shift | — |
| 2 | **Crosscut** | 2-2-2 | 2 | Damage · 2 counter hit | ≥1: +1 hit @ beat note đầu trong S; empty S → ×1.15 dmg |
| 3 | **Finale** | 2-3-3 | 3 | Damage burst · 3 counter hit | ≥2: Force Harmony hits |

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

---

## Charlotte — Tank · Rhythm · Physical

| # | Tên | S1-S-S2 | Tier | Base | Empower |
|---|-----|---------|------|------|---------|
| 1 | **Ram** | 1-1-1 | 1 | Damage | — |
| 2 | **Anchor** | 2-2-2 | 2 | **DelayBossNote +2** — chỉ note **sau S**; note trong S không đẩy | ≥1: Delay **+3** · giữ tier |
| 3 | **Bulwark** | 2-2-3 | 2 | **Shield 65** + counter dmg | ≥2: Shield **100** · GuardCharge stub |

**Asset:** `tank_basic` · `tank_skill` · `tank_ult`

| Skill | vs CORE | vs MICRO/EYE |
|-------|---------|--------------|
| Ram | **~18** | **~12** MiniDmg |
| Anchor | — | — |
| Bulwark / beat | **~20** | **~13** |

---

## Coda — Support · Harmony · Magical

| # | Tên | S1-S-S2 | Tier | Base | Empower |
|---|-----|---------|------|------|---------|
| 1 | **Pulse** | 1-1-1 | 1 | Damage (Ma) | — |
| 2 | **Mend** | 2-1-2 | 2 | Heal 25 + Ma×0.5 | ≥1: +15 · overheal→Shield cap 30 |
| 3 | **Encore** | 1-1-1 | 2 | **ReduceS2 −1** (ally skill kế) | ≥2: party S2−1 + gift 1 Prep ally |

**Asset:** `mage_basic` · `mage_skill` · `mage_ult`

| Skill | vs CORE | vs MICRO/EYE |
|-------|---------|--------------|
| Pulse | **~39** (×1.5 Harmony) | **~26** MiniDmg |
| Mend | — (~50 HP heal) | — |
| Encore | — | — |

---

## Effect kinds (`SkillEffectKind`)

| Kind | Mô tả |
|------|-------|
| `Damage` | Counter + dmg vs enemy |
| `Heal` | `effectValue + Ma×0.5` (+ empower) |
| `Shield` | `effectValue` / empower value HP buffer |
| `ReduceS2` | `PendingReduceS2` → footprint S2 ngắn hơn 1 lần đặt kế |
| `DelayBossNote` | Đẩy impact telegraph **sau cửa S** của skill +N beat (note trong S giữ nguyên) |
| `CycleShift` | Flag Strike (runtime VFX còn mở) |

---

## Unlock (SoT — đồng bộ progression)

> Level / XP / soft-cap: [combat-level-xp-progression-design](../superpowers/specs/2026-07-19-combat-level-xp-progression-design.md) · tables [CHARACTER_LEVEL_PROGRESS.md](./CHARACTER_LEVEL_PROGRESS.md)

| Lv | Ren | Charlotte | Coda |
|----|-----|-----------|------|
| 1 | Strike | Ram | Pulse |
| 3 | — | Anchor | — |
| 4 | Crosscut | — | — |
| 5 | — | — | Mend |
| 9 | — | Bulwark | — |
| 10 | Finale | — | — |
| 11 | — | — | Encore |

Không có skill-point riêng — unlock theo mốc Party Combat Level.

---

## Ví dụ — Ren Crosscut 2-2-2 @ beat 8

| Beat | 8 | 9 | 10 | 11 | 12 | 13 |
|------|---|---|----|----|----|----|
| Phase | S1 | S1 | **S** | **S** | S2 | S2 |
| Counter | — | — | hit | hit | — | — |

Empty cả 2 S → +2 Prep. Có note @ 10–11 → counter, Prep không tăng. Prep≥1 lúc vào S → empower (+1 hit @ beat note đầu).

---

## Runtime checklist

- [x] UI S1/S2 + S trên lane
- [x] Footprint fields + asset S1-S-S2 / tên skill
- [x] Prep channel / cap / pips
- [x] Empower spend + Crosscut/Finale/Bulwark/Mend/Encore amplify
- [x] Shield absorb
- [x] DelayBossNote D1 + slide VFX trên timeline
- [x] ReduceS2 pending + footprint preview + buff icon
- [x] Runtime sprites dưới `Resources/UI/Combat/**` (không Prefabs — `Resources.Load`)
- [x] Cover gauge + HUD + 12-beat window (Phase 4)
- [ ] Enforce footprint overlap (đã có `SkillFootprintUtil.CanPlace` — verify anti-spam standing)
- [ ] Counter degrade Tím/Xanh/Đỏ CORE
- [ ] Note tag CORE / MICRO / EYE
- [ ] Ren Cycle Shift animation
- [ ] Mini pressure Resonance / Dissonance
- [ ] Pick ally target Mend / Encore (hiện auto first ally)
- [ ] Bulwark GuardCharge thật (đang stub)

## Changelog

| Ngày | Nội dung |
|------|----------|
| 2026-07-17 | Cover gauge Phase 4: empty S → party gauge · Planning COVER · 12 beat ×1.25 + W1′ |
| 2026-07-16 | Sync DelayBossNote kind = after S (runtime SoT) |
| 2026-07-16 | Restore `Resources/UI` sau merge (Prefabs rename làm vỡ Load); Encore gift Prep @ planning; docs handoff |
| 2026-07-16 | Xóa skill Guard khỏi kit asset (`ren/tank/mage_guard`); block = Space |
| 2026-07-15 | Prep Setup→Payoff · empower tables · Delay D1 · ReduceS2 UI · sync spec |
| 2026-07-05 | Audit project: scene sync; fix overlay binding |
| 2026-07-03 | Footprint S1-S-S2 · tên skill · UI nút xám |
| 2026-06-30 | Cycle Shift · CoreFinal vs MiniDmg · effect kinds |

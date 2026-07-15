# Skill Kit Setup → Payoff — Implementation Plan

**Date:** 2026-07-14  
**Design spec:** [`../specs/2026-07-14-skill-kit-setup-payoff-design.md`](../specs/2026-07-14-skill-kit-setup-payoff-design.md)  
**Target:** Unity 6 · `Assets/FracturedChorus/` · scene `CombatPrototype`  
**Estimate:** 3 phases · ~1–1.5 ngày

---

## 0. Principles

- Basic (Strike / Ram / Pulse) **không** đọc/ghi Prep
- Prep **không** khóa cast — chỉ amplify khi đủ ngưỡng
- Channel: +1 Prep / beat **S** khi **không** có boss telegraph impact @ beat đó
- Cap **3** / unit; Skill spend **1** @ ≥1; Ult spend **2** @ ≥2
- Delay / ReduceS2 hiện **doc-only** — implement base rồi mới empower
- Không comment thừa; mỗi phase có acceptance trước khi sang phase sau
- Đồng bộ `docs/combat/SKILL_KIT.md` khi data/runtime ổn (cuối Phase 3)

### Hook hiện tại

| Surface | Today | Plan |
|---------|--------|------|
| `SkillDefinitionSO` | footprint + `baseDamage` + `glowType` | + effect kind / Prep empower params |
| `CombatSession.ResolveBeatAtScan` | counter + `SkillActionCommand` dmg/heal | + Prep channel; Delay resolve; empower context |
| `SkillActionCommand` | Attack dmg · Support heal | + Shield / ReduceS2 / Delay hooks · empower multipliers |
| `CombatUnit` | HP only | + Prep · Shield buffer · ReduceS2 pending · Prep gift |
| `SkillFootprintUtil.GetStandingAfter` | raw SO field | honor pending ReduceS2 when preview/assign |
| `BeatTimelineUIView` | footprint dots · Perfect chip | + Prep pips · Delay slide/ghost · `S2−1` chip |
| `BeatTimelineEngine` telegraphs | fixed beat | API move CORE impact +N (D1) |

### Out of scope

- CORE/MICRO/EYE tags · W formula · AI spawn floor · Basic rewrite · intro-pause beat 6 · empty-only skill catalog (#4 beyond Prep)

---

## Phase 1 — Prep economy + pips UI (~3–4h)

### Task 1.1 — Runtime Prep on unit

**Objective:** State Prep 0–3 sống theo encounter/segment, API gain/spend.

**Files:**
- Modify: `Assets/FracturedChorus/Combat/Units/CombatUnit.cs`
- Create (optional): `Assets/FracturedChorus/Combat/Skills/PrepResource.cs` nếu muốn tách khỏi `CombatUnit`

```csharp
public int Prep { get; private set; }
public const int PrepCap = 3;

public int GainPrep(int amount = 1)
{
    Prep = Mathf.Min(PrepCap, Prep + Mathf.Max(0, amount));
    return Prep;
}

public bool TrySpendPrep(int amount)
{
    if (amount <= 0 || Prep < amount) return false;
    Prep -= amount;
    return true;
}

public void ResetPrep() => Prep = 0;
```

**Reset:** `CombatSession.EndRoundSegment` / `BeginPlanningRound` — quyết định: **giữ Prep qua segment** (khuyến nghị) hoặc clear mỗi Deploy; default **giữ** trong 1 encounter, clear khi `BeginPlanningRound` encounter mới.

**Verify:** unit test manual via debug log; Prep clamp 0–3.

---

### Task 1.2 — Channel on empty S

**Objective:** Mỗi beat S fire: không boss impact → +1 Prep (Skill/Ult only; không Basic).

**Files:**
- Modify: `Assets/FracturedChorus/Combat/Core/CombatSession.cs`
- Helper: `CombatCounterResolver` hoặc static `PrepChannelUtil`

**Logic (trong `ResolveBeatAtScan` sau khi biết telegraphs @ beat):**

```
foreach player entry active at beat:
  if skill.slotKind == BasicAttack → skip
  if GetImpactTelegraphsAtBeat(beat).Count == 0 → unit.GainPrep(1)
  else → no prep (clash)
```

Party footprints **không** chặn channel.

**Verify:** Play — đặt Crosscut toàn empty S → Prep +2; đè note → Prep không tăng.

---

### Task 1.3 — Prep pips UI

**Objective:** 0–3 pip gần portrait / skill radial từng player unit.

**Files:**
- Create: `Assets/FracturedChorus/UI/PrepPipsView.cs` (hoặc gắn vào party card hiện có)
- Modify: `BeatTimelineUIView` / party bar binder — refresh khi Prep đổi

| Spec | Value |
|------|--------|
| Pips | 3 Image; fill 0..Prep |
| Pulse | scale/alpha ngắn khi Gain |
| Flash | khi Spend |

Event: `CombatUnit.OnPrepChanged` (thêm event cạnh `OnHpChanged`).

**Verify:** pip cập nhật đúng khi channel; không hiện trên enemy.

**Phase 1 acceptance**

1. Basic không đổi Prep  
2. Empty S Skill/Ult → +1 / beat, cap 3  
3. S + note → counter path cũ, Prep không tăng  
4. Pips đọc đúng 0–3  

---

## Phase 2 — Effect data + amplify damage/heal/shield (~4–5h)

### Task 2.1 — `SkillEffectKind` + SO fields

**Objective:** Data-driven base + empower; tránh hardcode tên skill trong resolver.

**Files:**
- Modify: `Assets/FracturedChorus/Data/ScriptableObjects/SkillDefinitionSO.cs`

```csharp
public enum SkillEffectKind
{
    Damage = 0,
    Heal = 1,
    Shield = 2,
    DelayBossNote = 3,
    ReduceS2 = 4,
    CycleShift = 5,
    // empower-only helpers can stay as flags below
}

[Header("Effect")]
public SkillEffectKind effectKind = SkillEffectKind.Damage;
public int effectValue;          // heal/shield/delay beats/reduce amount
public bool grantsCycleShift;    // Strike

[Header("Prep empower")]
public bool usesPrepEmpower;
public int prepEmpowerThreshold = 1;  // 1 Skill / 2 Ult
public int prepEmpowerCost = 1;
public int empowerEffectValue;        // e.g. shield 100, delay 3, heal bonus 15
public float empowerDamageMultiplier = 1f;
public int empowerExtraHits;          // Crosscut +1
public bool empowerForceHarmony;      // Finale
public bool empowerKeepDelayTier;     // Anchor
public bool empowerOverhealToShield;  // Mend
public int empowerOverhealShieldCap = 30;
public bool empowerPartyReduceS2;     // Encore
public bool empowerGiftPrepToTarget;  // Encore
public bool empowerGuardChargeOnPerfect; // Bulwark (stub OK)
```

**Assets cập nhật** (`Resources/Skills/`):

| Asset | effectKind | Notes |
|-------|------------|-------|
| `ren_basic` | Damage + CycleShift flag | usesPrep=false |
| `ren_skill` | Damage | thresh 1, extraHits 1, no cycle on empower |
| `ren_ult` | Damage | thresh 2, forceHarmony |
| `tank_basic` | Damage | — |
| `tank_skill` | DelayBossNote value 2 | thresh 1, empower value 3, keepTier |
| `tank_ult` | Shield 65 | thresh 2, empower 100, guardCharge stub |
| `mage_basic` | Damage | — |
| `mage_skill` | Heal | thresh 1, +15, overheal shield |
| `mage_ult` | ReduceS2 1 | thresh 2, party + gift Prep |

---

### Task 2.2 — Empower context at cast start

**Objective:** Khi skill **bắt đầu cửa S** (beat Active đầu), thử spend Prep nếu đủ threshold → flag `AgendaEntry.Empowered` (hoặc session map).

**Files:**
- Modify: `AgendaEntry` / timeline entry struct
- Modify: `CombatSession` — on first Active beat of entry:

```
if skill.usesPrepEmpower && unit.Prep >= threshold:
  if unit.TrySpendPrep(cost):
    entry.IsEmpowered = true
```

Spend **một lần / placement**, không mỗi beat S.

**Verify:** Prep 2 + Ult → sau cast Prep 0, `IsEmpowered` true; Prep 1 + Ult → base only, Prep còn 1.

---

### Task 2.3 — Apply amplify in `SkillActionCommand` + counter hits

**Objective:** Damage/Heal/Shield theo kind + empower.

**Files:**
- Modify: `SkillActionCommand.cs`
- Modify: `CombatUnit.cs` — `Shield` buffer (absorb trước HP)
- Modify: `CombatCounterResolver` / beat resolve — Crosscut `empowerExtraHits`: beat S **đầu tiên có note** nhận **thêm 1** counter contribution (cùng unit đếm 2 hits @ beat đó nếu HitsRequired cho phép)

Finale `empowerForceHarmony`: khi `IsEmpowered`, `DamageCalculator` dùng Harmony relation vs CORE (map Active→Harmony cho cast).

Mend: heal `effectValue + Ma*0.5` (+15 nếu empowered); overheal → Shield cap 30.

Bulwark: apply Shield 65/100; counter dmg giữ Damage path trên S có note; GuardCharge stub = log/flag.

**Verify:** Prep 0 Mend heal base; Prep 1 Mend +15 + overheal shield; Crosscut empower double-hit first note beat.

**Phase 2 acceptance**

1. Prep 0 → base effects  
2. Threshold đúng · spend 1/2  
3. Crosscut / Finale / Bulwark / Mend empower số liệu khớp spec  
4. Shield absorb dmg telegraph  

---

## Phase 3 — Delay D1 + ReduceS2 + timeline VFX + docs (~4–5h)

### Task 3.1 — `DelayBossNote` (D1)

**Objective:** Anchor resolve: mọi CORE telegraph có `BeatIndex ∈ Anchor S beats` → `BeatIndex += N` (2 base / 3 empower).

**Files:**
- Modify: `BeatTimelineEngine.cs` — `TryMoveTelegraph(EnemyTelegraph t, int newBeat)` (clamp range, merge rules nếu ô đích đã có note — **khuyến nghị:** cho phép nhiều telegraph / beat hoặc đẩy tiếp +1 đến slot trống; document trong code path đơn giản: set beat, refresh UI)
- Modify: `CombatSession` — khi entry Delay kind: on **last Active beat** hoặc **first Active beat** (chọn **first Active beat** để player thấy sớm): apply delay N

Empower: `empowerKeepDelayTier` → không đổi `NoteTier` / `HitsRequired`.

**UI:**
- Modify: `BeatTimelineUIView` — khi telegraph move: tween note X; ghost cũ 0.25–0.4s; badge `+2`/`+3`

**Verify:** Note @ beat trong S Anchor dịch đúng +2; empower +3 giữ tier.

---

### Task 3.2 — `ReduceS2`

**Objective:** Encore base: target ally `PendingReduceS2 = 1` cho **1** skill kế.

**Files:**
- Modify: `CombatUnit` — `int PendingReduceS2`
- Modify: `SkillFootprintUtil.GetStandingAfter(skill, unit)` overload — `max(0, so.standingBeatsAfter - unit.PendingReduceS2)`
- Modify: `CanAssignAction` / preview / `RefreshFootprintDots` dùng overload
- Clear `PendingReduceS2` sau `TryAssignPlayerAction` thành công cho unit đó

Empower: `PendingReduceS2 = 1` **mọi** player ally; target `GainPrep(1)`.

**UI:** chip `S2−1` trên portrait ally khi pending > 0.

**Verify:** Encore → Crosscut preview 2-2-**1**; sau đặt 1 skill, S2 về bình thường.

---

### Task 3.3 — Docs + Edit Preview polish

**Files:**
- Update: `docs/combat/SKILL_KIT.md` — bảng + Prep laws + bỏ/sửa checklist runtime
- Optional: CombatRoot Edit Preview — mock Prep pips / delay ghost (nếu pattern preview đã có)

**Phase 3 acceptance**

1. Spec acceptance §7 đủ (Delay D1, Encore S2, Prep laws)  
2. `SKILL_KIT.md` khớp spec  
3. Intro-pause beat 6 không đổi  

---

## 4. Suggested order / PR split

| PR | Content |
|----|---------|
| A | Phase 1 Prep + pips |
| B | Phase 2 SO + empower dmg/heal/shield |
| C | Phase 3 Delay + ReduceS2 + docs |

Có thể gộp A+B nếu anh muốn 1 PR economy+amplify trước utility timeline.

---

## 5. Risk notes

| Risk | Mitigation |
|------|------------|
| Telegraph move collision | Simple push-to-empty; log nếu fail |
| Double-hit Crosscut vs HitsRequired | Count 2 contributions same unit same beat only when empowered |
| GuardCharge chưa có hệ | Stub flag + log; không block ship Bulwark shield |
| Ally target Mend/Encore vẫn first-ally | Giữ auto-first; pick UI = later |
| Prep giữ qua segment | Playtest; nếu OP → clear mỗi Deploy |

---

## 6. Done when

- [ ] Phase 1 acceptance  
- [ ] Phase 2 acceptance  
- [ ] Phase 3 acceptance  
- [ ] Spec file vẫn là source of truth; `SKILL_KIT.md` synced  

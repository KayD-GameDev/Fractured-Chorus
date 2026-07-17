# Cover Gauge from Empty Beats — Design Spec

> **Status:** Implemented — runtime landed (2026-07-17); Play Mode verify pending  
> **Scope:** Phase 4 combat A1 — empty-beat → party Cover gauge → Ren Cover buff window  
> **Approach:** CoverRuntime service (separate from Prep)  
> **SoT (after ship):** [`docs/combat/SKILL_KIT.md`](../../combat/SKILL_KIT.md) · [`docs/combat/COMBAT_MECHANICS.md`](../../combat/COMBAT_MECHANICS.md)  
> **Out of scope:** Ally pick Mend/Encore · GuardCharge · Cycle Shift VFX · CORE/MICRO/EYE tags · Cover activate during Execute · full Cover song VN/audio · W formula / AI spawn / intro-pause beat 6  
> **Related:** Prep Setup→Payoff (`2026-07-14-skill-kit-setup-payoff-design.md`) · Muse Cover (Caligula) fantasy

---

## 1. Goals

- Empty Active (S) beats on Skill/Ult feed a **party Cover gauge** — gap planning stays valuable alongside Prep.
- Cover is a **strong, low-spam** party buff (Muse-style “open cover”), activated by Ren from Planning HUD.
- Prep economy unchanged: per-unit Prep channel/spend remains the amplify loop.

### Problem

| Surface | Today | Gap |
|---------|--------|-----|
| Empty S | +Prep only | No party-level payoff for gap farming |
| Burst identity | Finale / MULTI presentation | No dedicated Ren Cover window |
| Spam risk | — | Strong buff needs high cost |

### Success criteria

1. Empty S (Skill/Ult, any ally) → CoverGauge +1 (cap 10); note ∩ S → no charge.
2. Basic never charges Cover.
3. Gauge &lt; 8 or Ren dead/absent or window active/pending → Cover button disabled.
4. Planning activate → −8; next Execute starts 12-beat window.
5. During window: party outgoing damage ×1.25; Early/Late use OnBeat multipliers (W1′).
6. Prep channel/spend and intro-pause after beat 6 unchanged.

---

## 2. Locked decisions

| ID | Decision |
|----|----------|
| Spine | Empty S → Cover gauge → Planning activate → Execute buff window |
| Charge | **E** +1 per empty S beat (Skill/Ult any ally); Basic excluded |
| Cap / Cost | **H2** Cap **10** · Cost **8** |
| Effect | **B** Buff window: damage ×**1.25** + timing forgive |
| Duration | **D12** **12** beats |
| Activate | **K1** Cover HUD button · **Planning only** |
| Timing forgive | **W1′** Early/Late → OnBeat multiplier (discrete; no DSP window) |
| Architecture | **1** `CoverRuntime` service (not grafted into Prep types) |
| Ren gate | Ren must be alive and in party to activate; gauge still charges without Ren |
| Combat reset | Gauge + window reset at end of each combat (MVP) |
| Stack | No second activate while Pending or Active |

### Constants

| Key | Value |
|-----|-------|
| `CoverGaugeCap` | 10 |
| `CoverActivateCost` | 8 |
| `CoverDurationBeats` | 12 |
| `CoverDamageMultiplier` | 1.25f |
| Charge filter | Same as Prep channel: Skill/Ult S, no boss impact on beat |

---

## 3. Runtime flow

```
Execute beat resolve (Skill/Ult on S, not Basic)
  no boss impact on beat  →  Prep +1 (existing)  AND  CoverGauge +1 (cap 10)
  boss impact on beat     →  Counter / Perfect (existing); Prep & Cover unchanged

Planning (Deploy / mid-block planning)
  CoverGauge ≥ 8 AND Ren alive AND !Pending AND !Active
    → Cover button enabled
  Press Cover → Gauge −8; CoverPending = true

Execute start (segment after pending)
  CoverPending → CoverActiveBeatsRemaining = 12; CoverPending = false

Each Execute beat while remaining > 0
  Party outgoing damage × 1.25
  BeatTiming Early/Late → OnBeat damage mult
  BlockTiming Early/Late → OnBeat Guard reduction (same forgive)
  Tick: remaining−−; at 0 → clear active buff
```

### State

| Field | Type | Notes |
|-------|------|-------|
| `CoverGauge` | int 0..10 | Party / session |
| `CoverPending` | bool | Armed in Planning |
| `CoverActiveBeatsRemaining` | int 0..12 | Counts down on Execute beat advance |

---

## 4. Components & data flow

```
TryChannelPrepAtBeat (empty S)
        ├─► CombatUnit.GainPrep(1)           [existing]
        └─► CoverRuntime.TryCharge(1)        [new]

CoverHudView (Planning)
        └─► CoverRuntime.TryActivate()       −8, Pending

CombatSession / timeline Execute start
        └─► CoverRuntime.BeginWindowIfPending()

SkillActionCommand damage path
        └─► × CoverRuntime.OutgoingDamageMultiplier

BeatTiming / BlockTiming consumers
        └─► CoverRuntime.RemapTimingForForgive (Early|Late → OnBeat values)

Beat advance (Execute)
        └─► CoverRuntime.TickBeat()
```

| Unit | Path | Responsibility |
|------|------|----------------|
| `CoverConstants` | `Assets/FracturedChorus/Combat/Cover/CoverConstants.cs` | Cap, cost, duration, damage mult |
| `CoverRuntime` | `Assets/FracturedChorus/Combat/Cover/CoverRuntime.cs` | Gauge / pending / window; Charge, Activate, Begin, Tick, queries |
| `CoverHudView` | `Assets/FracturedChorus/UI/CoverHudView.cs` | Bar 0–10 + button; Planning-only enable; Ren gate |
| Wire | `CombatSession.cs` · `CombatPrototypeBootstrap.cs` | Charge next to Prep; Begin @ Execute; Tick @ beat |
| Consume | `SkillActionCommand.cs` · timing apply sites | ×1.25 + W1′ |
| Art | `Resources/UI/Combat/` | Bar + button stubs OK for Phase 4 |
| Docs | `SKILL_KIT.md` · `COMBAT_MECHANICS.md` · `PROJECT_STATUS.md` | Sync after runtime |

### Public API (target)

```csharp
public sealed class CoverRuntime
{
    public int Gauge { get; }
    public bool IsPending { get; }
    public int ActiveBeatsRemaining { get; }
    public bool IsActive => ActiveBeatsRemaining > 0;
    public float OutgoingDamageMultiplier { get; } // 1.25 or 1

    public bool TryCharge(int amount);
    public bool CanActivate(bool renAlive);
    public bool TryActivate(bool renAlive);
    public void BeginWindowIfPending();
    public void TickBeat();
    public void Reset();

    public BeatTiming RemapPlayerTiming(BeatTiming timing);
    public BlockTiming RemapGuardTiming(BlockTiming timing);
}
```

---

## 5. Edge cases & errors

| Case | Behavior |
|------|----------|
| Gauge &lt; 8 / Ren dead / Active / Pending | Button disabled; `TryActivate` returns false + `Debug.Log` |
| Double activate | Blocked by Pending/Active |
| Charge at cap | Clamp to 10 |
| Encounter end | `Reset()` — gauge and window cleared |
| Intro-pause beat 6 | Unchanged |
| Charge during active window | Still allowed up to cap |
| Missing UI sprites | Fallback text label; `Debug.LogError` on load failure |

Error handling: try/catch around HUD Resources load and bootstrap wire; log with `Debug.LogError`; friendly disabled state for player.

---

## 6. Acceptance (Play Mode)

| # | Test | Pass |
|---|------|------|
| 1 | 8× empty S (Skill/Ult) | Gauge 8; button enables in Planning |
| 2 | S ∩ note | Gauge does not increase |
| 3 | Basic on empty | No Cover charge |
| 4 | Activate in Planning | Gauge −8; Execute starts 12-beat ×1.25 |
| 5 | Early/Late during window | Player dmg mult = OnBeat; Guard Early/Late = OnBeat reduction |
| 6 | After 12 beats | Multiplier back to 1.0 |
| 7 | Prep + intro-pause | No regression |

---

## 7. Implementation notes (for plan)

1. Call `TryCharge` from the same empty-S path as `TryChannelPrepAtBeat` (after Prep gain, same filters).
2. Do not store Cover gauge on `CombatUnit` — party resource on session/`CoverRuntime`.
3. Apply damage mult in one place on the outgoing damage path (`SkillActionCommand` or shared helper) to avoid double-apply.
4. W1′: remap at consumption time; do not change `BeatTimingResolver.Resolve` global results permanently.
5. UI under `Resources/UI/**` only (not Prefabs).
6. Update combat docs in the same delivery as runtime.

---

## 8. Out of scope (later)

- Cover activate mid-Execute / hotkey
- Cover song VN, full SFX/VFX pass
- Ally targeting for Mend/Encore
- GuardCharge real system · Cycle Shift VFX
- CORE/MICRO/EYE tag presentation
- Persist Cover gauge across run map nodes


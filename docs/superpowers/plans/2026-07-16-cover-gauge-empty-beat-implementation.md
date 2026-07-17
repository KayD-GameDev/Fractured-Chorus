# Cover Gauge Empty-Beat — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Empty Skill/Ult S beats charge a party Cover gauge; Planning HUD activates Ren Cover for a 12-beat party damage/timing buff window.

**Architecture:** `CoverRuntime` on `CombatSession` (party resource, separate from Prep). Charge beside `TryChannelPrepAtBeat`. Activate via `CoverHudView` in Planning. `ConfirmPlanningAndExecute` starts the window; `ResolveBeatAtScan` ticks duration. Damage ×1.25 and W1′ timing remap applied at consumption sites.

**Tech Stack:** Unity 6 · C# · `Assets/FracturedChorus/` · scene `CombatPrototype` · UI under `Resources/UI/Combat/**`

**Spec:** [`../specs/2026-07-16-cover-gauge-empty-beat-design.md`](../specs/2026-07-16-cover-gauge-empty-beat-design.md)

## Global Constraints

- Cap **10** · Cost **8** · Duration **12** · Damage **×1.25**
- Charge: +1 empty S Skill/Ult (any ally); Basic never; note ∩ S never
- Activate: Planning HUD only; Ren alive (`DisplayName == "Ren"`)
- W1′: Early/Late → OnBeat multipliers while active (player dmg + Guard block reduction)
- Prep / intro-pause beat 6 unchanged
- No NUnit assembly in repo — verify with `Debug.Log` + Play Mode checklist (same as skill-kit / counter plans)
- Runtime UI sprites under `Resources/UI/**` only

### File map

| Action | Path |
|--------|------|
| Create | `Assets/FracturedChorus/Combat/Cover/CoverConstants.cs` |
| Create | `Assets/FracturedChorus/Combat/Cover/CoverRuntime.cs` |
| Create | `Assets/FracturedChorus/UI/CoverHudView.cs` |
| Modify | `Assets/FracturedChorus/Combat/Core/CombatSession.cs` |
| Modify | `Assets/FracturedChorus/Combat/Actions/CombatContext.cs` |
| Modify | `Assets/FracturedChorus/Combat/Actions/SkillActionCommand.cs` |
| Modify | `Assets/FracturedChorus/Combat/Bootstrap/CombatPrototypeBootstrap.cs` |
| Modify | `Assets/FracturedChorus/Combat/Core/CombatController.cs` (phase → HUD refresh if needed) |
| Modify | `docs/combat/SKILL_KIT.md` |
| Modify | `docs/combat/COMBAT_MECHANICS.md` |
| Modify | `docs/PROJECT_STATUS.md` |
| Untouched | Prep economy · TimelineConstants intro · CORE tags · ally pick |

---

### Task 1: CoverConstants + CoverRuntime

**Files:**
- Create: `Assets/FracturedChorus/Combat/Cover/CoverConstants.cs`
- Create: `Assets/FracturedChorus/Combat/Cover/CoverRuntime.cs`

**Interfaces:**
- Consumes: `BeatTiming` (`FracturedChorus.Combat.Damage`), `BlockTiming` (`FracturedChorus.Combat.Block`)
- Produces: `CoverRuntime` API used by Session / HUD / damage path

- [ ] **Step 1: Add `CoverConstants.cs`**

```csharp
namespace FracturedChorus.Combat.Cover
{
    public static class CoverConstants
    {
        public const int GaugeCap = 10;
        public const int ActivateCost = 8;
        public const int DurationBeats = 12;
        public const float DamageMultiplier = 1.25f;
        public const string RenDisplayName = "Ren";
    }
}
```

- [ ] **Step 2: Add `CoverRuntime.cs`**

```csharp
using FracturedChorus.Combat.Block;
using FracturedChorus.Combat.Damage;
using UnityEngine;

namespace FracturedChorus.Combat.Cover
{
    public sealed class CoverRuntime
    {
        public int Gauge { get; private set; }
        public bool IsPending { get; private set; }
        public int ActiveBeatsRemaining { get; private set; }
        public bool IsActive => ActiveBeatsRemaining > 0;
        public float OutgoingDamageMultiplier =>
            IsActive ? CoverConstants.DamageMultiplier : 1f;

        public bool TryCharge(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            var before = Gauge;
            Gauge = Mathf.Min(CoverConstants.GaugeCap, Gauge + amount);
            return Gauge != before;
        }

        public bool CanActivate(bool renAlive) =>
            renAlive &&
            !IsPending &&
            !IsActive &&
            Gauge >= CoverConstants.ActivateCost;

        public bool TryActivate(bool renAlive)
        {
            if (!CanActivate(renAlive))
            {
                return false;
            }

            Gauge -= CoverConstants.ActivateCost;
            IsPending = true;
            Debug.Log($"[Cover] Activated (−{CoverConstants.ActivateCost}) → gauge {Gauge}/{CoverConstants.GaugeCap} pending");
            return true;
        }

        public void BeginWindowIfPending()
        {
            if (!IsPending)
            {
                return;
            }

            IsPending = false;
            ActiveBeatsRemaining = CoverConstants.DurationBeats;
            Debug.Log($"[Cover] Window start {ActiveBeatsRemaining} beats ×{CoverConstants.DamageMultiplier}");
        }

        public void TickBeat()
        {
            if (ActiveBeatsRemaining <= 0)
            {
                return;
            }

            ActiveBeatsRemaining--;
            if (ActiveBeatsRemaining <= 0)
            {
                Debug.Log("[Cover] Window ended");
            }
        }

        public void Reset()
        {
            Gauge = 0;
            IsPending = false;
            ActiveBeatsRemaining = 0;
        }

        public BeatTiming RemapPlayerTiming(BeatTiming timing)
        {
            if (!IsActive)
            {
                return timing;
            }

            return timing is BeatTiming.Early or BeatTiming.Late
                ? BeatTiming.OnBeat
                : timing;
        }

        public BlockTiming RemapGuardTiming(BlockTiming timing)
        {
            if (!IsActive)
            {
                return timing;
            }

            return timing is BlockTiming.Early or BlockTiming.Late
                ? BlockTiming.OnBeat
                : timing;
        }
    }
}
```

- [ ] **Step 3: Compile smoke**

Open Unity / wait domain reload. Expected: no CS errors on new files.

- [ ] **Step 4: Commit** (when user requests)

```bash
git add Assets/FracturedChorus/Combat/Cover/CoverConstants.cs Assets/FracturedChorus/Combat/Cover/CoverRuntime.cs
git commit -m "$(cat <<'EOF'
Add CoverRuntime party gauge for empty-beat Cover.

EOF
)"
```

---

### Task 2: Wire CombatSession (charge / begin / tick / reset)

**Files:**
- Modify: `Assets/FracturedChorus/Combat/Core/CombatSession.cs`
- Modify: `Assets/FracturedChorus/Combat/Actions/CombatContext.cs`

**Interfaces:**
- Consumes: `CoverRuntime` from Task 1
- Produces: `session.Cover`; charge on empty S; begin on Execute; tick per resolved beat; `CombatContext.CoverOutgoingMultiplier`

- [ ] **Step 1: Expose Cover on session**

Near `PhaseAv` / `BlockBarriers`:

```csharp
public CoverRuntime Cover { get; } = new();
```

Add `using FracturedChorus.Combat.Cover;`

- [ ] **Step 2: Charge beside Prep**

Change `TryChannelPrepAtBeat` from `static` to instance method (or pass `CoverRuntime`). After each successful `GainPrep(1)`:

```csharp
Cover.TryCharge(1);
Debug.Log($"[Cover] +1 @ beat {beatIndex} → {Cover.Gauge}/{CoverConstants.GaugeCap}");
```

Keep Basic skip and telegraph-block identical to Prep.

Update call site: `TryChannelPrepAtBeat(...)` already on instance — remove `static` keyword.

- [ ] **Step 3: Begin window on Execute**

In `ConfirmPlanningAndExecute`, immediately after `Timeline.SetPhase(CombatPhase.Executing);`:

```csharp
Cover.BeginWindowIfPending();
```

- [ ] **Step 4: Tick after beat resolve**

End of `ResolveBeatAtScan`, after enemy resolves / before or after `TryEndEncounterIfDecided`:

```csharp
if (Phase == CombatPhase.Executing)
{
    Cover.TickBeat();
}
```

- [ ] **Step 5: Reset**

In `BeginPlanningRound` (start of combat planning):

```csharp
Cover.Reset();
```

In `TryEndEncounterIfDecided` when outcome Victory/Defeat, before/after `OnEncounterEnded`:

```csharp
Cover.Reset();
```

Do **not** reset Cover on `EndRoundSegment` (mid-fight planning keeps gauge; Pending already consumed at Execute).

- [ ] **Step 6: CombatContext multiplier field**

```csharp
public float CoverOutgoingMultiplier { get; set; } = 1f;
```

In `ResolvePlayerAttacksAtBeat`, after `BeatTimingResolver.Resolve`:

```csharp
var timing = Cover.RemapPlayerTiming(BeatTimingResolver.Resolve(entry.BeatIndex, enemyBeat));
ResolvePlayerAttack(entry, timing);
```

In `ResolvePlayerAttack` when building `CombatContext`:

```csharp
BeatTiming = timing,
CoverOutgoingMultiplier = Cover.OutgoingDamageMultiplier,
```

- [ ] **Step 7: Guard forgive**

In `ResolveEnemyTelegraphAtBeat`, after `TryGetBlockTiming`:

```csharp
if (blockTiming.HasValue)
{
    var timing = Cover.RemapGuardTiming(blockTiming.Value);
    var reduction = timing.GetDamageReduction();
    // ... rest using timing in log
}
```

- [ ] **Step 8: Play Mode log check**

Deploy → place Skill on empty S → Execute. Console: `[Prep] +1` and `[Cover] +1`. Expected both.

- [ ] **Step 9: Commit** (when user requests)

```bash
git commit -m "$(cat <<'EOF'
Wire Cover gauge charge and window into CombatSession.

EOF
)"
```

---

### Task 3: Apply Cover damage multiplier in SkillActionCommand

**Files:**
- Modify: `Assets/FracturedChorus/Combat/Actions/SkillActionCommand.cs`

**Interfaces:**
- Consumes: `ctx.CoverOutgoingMultiplier`
- Produces: outgoing player skill damage ×1.25 while Cover active

- [ ] **Step 1: Multiply after empower adjustments**

In `ApplyDamageToTarget`, after empower multiplier blocks, before `TakeDamage`:

```csharp
if (ctx.CoverOutgoingMultiplier > 0f && !Mathf.Approximately(ctx.CoverOutgoingMultiplier, 1f))
{
    finalDamage *= ctx.CoverOutgoingMultiplier;
}
```

- [ ] **Step 2: Verify**

Activate Cover (Task 4) or temporarily force `Cover.TryActivate` + `BeginWindowIfPending` via temporary debug — damage log `final=` should rise ~25% vs baseline same skill.

- [ ] **Step 3: Commit** (when user requests)

```bash
git commit -m "$(cat <<'EOF'
Apply Cover outgoing damage multiplier on skill hits.

EOF
)"
```

---

### Task 4: CoverHudView + bootstrap wire

**Files:**
- Create: `Assets/FracturedChorus/UI/CoverHudView.cs`
- Modify: `Assets/FracturedChorus/Combat/Bootstrap/CombatPrototypeBootstrap.cs`
- Modify: `Assets/FracturedChorus/Combat/Core/CombatController.cs` (refresh on phase / optional)

**Interfaces:**
- Consumes: `CombatSession.Cover`, `session.Phase`, player units for Ren alive
- Produces: Planning-only Cover button + gauge fill 0–10

- [ ] **Step 1: Implement `CoverHudView`**

Pattern: code-built UI like `PrepPipsView.EnsureOn` — parent under party status canvas or CombatCanvas.

Requirements:
- Root name `CoverHud`
- Fill bar (Image type Filled) or 10 segment pips showing `Gauge / 10`
- Button label `COVER` (sprite optional: `Resources.Load<Sprite>("UI/Combat/combat_btn_cover_v1")` — null → text only)
- `Bind(CombatSession session)`
- `Refresh()`:
  - visible always in combat
  - `button.interactable = session.Phase == CombatPhase.Planning && session.Cover.CanActivate(IsRenAlive(session))`
  - show Pending/Active state text optional (`PENDING` / `ACTIVE N`)
- OnClick → `session.Cover.TryActivate(IsRenAlive(session)); Refresh();`
- `IsRenAlive`: any player unit with `DisplayName == CoverConstants.RenDisplayName` && `IsAlive`

```csharp
private static bool IsRenAlive(CombatSession session)
{
    if (session?.Grid == null)
    {
        return false;
    }

    foreach (var u in session.Grid.PlayerUnits)
    {
        if (u != null &&
            u.IsAlive &&
            string.Equals(u.DisplayName, CoverConstants.RenDisplayName, System.StringComparison.Ordinal))
        {
            return true;
        }
    }

    return false;
}
```

- [ ] **Step 2: Ensure in bootstrap**

After `_session.Initialize` / party bar refresh:

```csharp
EnsureCoverHud();
```

```csharp
private CoverHudView _coverHud;

private void EnsureCoverHud()
{
    try
    {
        if (_coverHud == null)
        {
            _coverHud = FindAnyObjectByType<CoverHudView>();
        }

        if (_coverHud == null && partyStatusBarView != null)
        {
            _coverHud = CoverHudView.EnsureOn(partyStatusBarView.transform as RectTransform);
        }

        _coverHud?.Bind(_session);
        _coverHud?.Refresh();
    }
    catch (System.Exception e)
    {
        Debug.LogError("[Bootstrap] Failed to setup CoverHud: " + e);
    }
}
```

- [ ] **Step 3: Refresh on phase / scan**

Subscribe in `CoverHudView.Bind`:
- `session.OnPhaseChanged` → Refresh
- optional: refresh after charge — hook `OnScanBeat` → Refresh so bar updates during Execute

In `CombatController`, if phase callbacks already exist for overlay, call `_coverHud.Refresh()` only if bootstrap holds ref — prefer self-subscribe inside `CoverHudView` to avoid controller bloat.

- [ ] **Step 4: Play Mode acceptance (spec §6)**

| # | Test | Pass |
|---|------|------|
| 1 | 8× empty S Skill/Ult | Gauge 8; COVER enables in Planning |
| 2 | S ∩ note | Gauge unchanged |
| 3 | Basic empty | No Cover + |
| 4 | Press COVER → Execute | −8; 12 beats ×1.25 logs |
| 5 | Early/Late in window | Remapped to OnBeat mult |
| 6 | After 12 beats | Mult 1.0; ACTIVE clear |
| 7 | Prep + intro-pause beat 6 | Unchanged |

- [ ] **Step 5: Commit** (when user requests)

```bash
git commit -m "$(cat <<'EOF'
Add Cover HUD and wire Planning activate for Ren Cover.

EOF
)"
```

---

### Task 5: Docs SoT sync

**Files:**
- Modify: `docs/combat/SKILL_KIT.md`
- Modify: `docs/combat/COMBAT_MECHANICS.md`
- Modify: `docs/PROJECT_STATUS.md`
- Modify: `docs/superpowers/specs/2026-07-16-cover-gauge-empty-beat-design.md` (Status → Implemented after Play pass)

- [ ] **Step 1: SKILL_KIT — new Cover section after Prep**

```markdown
## Cover — Empty Beat Gauge (Ren)

```
Empty beat ∩ S (Skill/Ult, any ally)  →  +1 Cover gauge (cap 10, party)
Note beat ∩ S                         →  no Cover charge
Basic                                 →  no Cover charge
Planning · gauge ≥ 8 · Ren alive      →  COVER button (−8)
Execute after activate                →  12 beat window: party dmg ×1.25
                                        Early/Late → OnBeat (dmg + Guard)
```

- UI: `CoverHudView` · `CoverRuntime` on `CombatSession`
- Separate from Prep (per-unit amplify)
```

Update header status line to mention Cover Phase 4.

- [ ] **Step 2: COMBAT_MECHANICS**

Add subsection under Prep (or after) mirroring Cover laws; backlog row `#4 empty-beat` → ✅ Cover gauge.

- [ ] **Step 3: PROJECT_STATUS**

| Cover gauge (empty S → Ren Cover window) | ✅ |
| Empty-beat skill catalog (#4) | ✅ Cover gauge (not dedicated buff skills) |

Update “Việc tiếp theo”: remove empty-beat #4; keep ally pick / GuardCharge / CORE tags.

- [ ] **Step 4: Spec status banner**

Set `Status: Implemented — runtime SoT (YYYY-MM-DD)` after Play acceptance.

- [ ] **Step 5: Commit** (when user requests)

```bash
git commit -m "$(cat <<'EOF'
Document Cover gauge empty-beat runtime in combat SoT.

EOF
)"
```

---

## Suggested PR split

| PR | Content |
|----|---------|
| A | Tasks 1–3 runtime (CoverRuntime + session + damage) |
| B | Task 4 HUD |
| C | Task 5 docs |

A+B OK as one PR if preferred.

---

## Risks

| Risk | Mitigation |
|------|------------|
| TickBeat double-count | Only tick inside `ResolveBeatAtScan` once per beat (`_resolvedBeats` already gates) |
| `ResolveAnyRemainingBeats` at Deploy→Execute | Window starts then ticks through remaining — intended for segment resolve |
| unitId `DPS` for Ren | Gate on `DisplayName == "Ren"` only |
| Grid cover mod name clash | Keep method name `GetCoverModifier` (grid); party system is `CoverRuntime` / `[Cover]` logs |
| Pending across EndRoundSegment | Begin only in `ConfirmPlanningAndExecute`; if player activates then somehow skips Execute, Pending holds until next Execute |

---

## Done when

- [ ] Spec acceptance §6 all pass in Play Mode
- [ ] Prep + intro-pause unchanged
- [ ] Docs synced (`SKILL_KIT` / `COMBAT_MECHANICS` / `PROJECT_STATUS`)

---

## Self-review (plan vs spec)

| Spec requirement | Task |
|------------------|------|
| Charge E empty S | Task 2 |
| Cap 10 / Cost 8 | Task 1 constants |
| K1 Planning button | Task 4 |
| D12 ×1.25 + W1′ | Tasks 1–3 |
| Ren alive gate | Task 4 |
| Reset per combat | Task 2 |
| Prep unchanged | charge parallel only |
| Docs | Task 5 |
| No placeholders / TBD | OK |

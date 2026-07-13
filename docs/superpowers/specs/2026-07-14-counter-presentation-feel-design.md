# Counter Presentation Feel — Dense Notes

> **Status:** Design approved (2026-07-14)  
> **Scope:** Approach 2 — combo body layer + note resolve chip / MULTI  
> **Out of scope:** Skill kit redesign, empty-beat buffs, timeline info density overhaul, hard AI spawn floor  
> **Related:** `docs/combat/COMBAT_MECHANICS.md` · `UnitView` · `BeatTimelineUIView` · Eternal Spark beat map

---

## 1. Goals

- Perfect counter resolve logic stays unchanged (cancel / HitsRequired / damage).
- When boss notes are dense on uneven wall-clock beats: no idle jitter between counters; player can still read **which note** was countered.
- Keep intro-pause after beat index 6; Deploy / Execute loop unchanged.

### Problem evidence

| Source | Value |
|--------|-------|
| Counter clips (Ren/Coda/Boss Be Countered) | ~0.35s |
| Beat gap median / min | ~0.39s / ~0.15s |
| Gaps &lt; 0.35s | 126 |
| Max consecutive gaps &lt; 0.30s | 10 |
| Current code | Each Perfect → `Play` from 0 + `ReturnToIdleAfter(clip.length)` → next Perfect cuts anim |

---

## 2. Feel rules (wall-clock)

| Situation | Body (`UnitView`) | Note feedback (timeline) |
|-----------|-------------------|--------------------------|
| Gap to next Perfect **≥ 0.28s** | Restart Counter from start | 1 chip/VFX on beat cell |
| Gap **&lt; 0.28s** (dense chain) | Do **not** full restart; hold attack pose or retrigger from **hit-frame** (~35% normalized time) | Still 1 chip/VFX per note |
| **≥ 3** Perfect within **0.9s** (party window) | At most one burst pose when threshold hit; further notes in window use HitRetrigger | `MULTI ×N` banner near ScanBar |

### Tunable constants (Inspector)

| Id | Default |
|----|---------|
| `RestartGapSec` | 0.28 |
| `BurstWindowSec` | 0.9 |
| `BurstCount` | 3 |
| `HitRetriggerNormalizedTime` | ~0.35 |

Clock source: same clock as music-driven scan (`AudioSettings.dspTime` or music controller musical clock). Prefer music-aligned clock so gaps match what the player hears.

---

## 3. Architecture

### Components

| Piece | Role |
|-------|------|
| **`CounterPresentationDriver`** (new) | On Perfect: decide Restart vs HitRetrigger vs Burst; drive body + note VFX + SFX |
| **`UnitView`** (extend) | `PlayCounterRestart()` · `PlayCounterHitRetrigger()` · `PlayCounterBurst()` — stop blind restart-on-every-Perfect |
| **`BeatTimelineUIView` / beat slot** (extend) | Spawn note-resolve chip at beat that crossed ScanBar; optional MULTI banner near ScanBar |
| **`CombatSfxController`** (light) | Keep Perfect SFX; burst may reuse clip or a short accent (new asset optional) |

Resolve path (`CombatSession` / `CombatCounterResolver`) unchanged. Presentation moves out of inline `PlayCounterAnimations` in `BeatTimelineUIView` into the driver.

### Data flow

```
Scan hits beat E
  → CombatSession.ResolveBeatAtScan(E)           // existing logic
  → if Perfect: CounterPresentationDriver.OnPerfect(E, playerUnits, enemies)
       ├─ now, gap = now - lastPerfectTime[unit]
       ├─ Body: gap ≥ RestartGapSec → Restart | else HitRetrigger
       │         party Perfect count in BurstWindowSec ≥ BurstCount → Burst once
       ├─ Timeline: NoteResolveChip @ beat E (tier color, −1 / ×N)
       └─ SFX: Perfect (burst: stronger accent or same clip)
```

**Burst scope:** count Perfects across **party** in the 0.9s window (readable MULTI). Body burst plays on the unit that scored the Perfect that crossed the threshold.

### File boundaries

- Do not grow feel logic inside `BeatTimelineUIView` (~2175 LOC); driver owns decisions; timeline exposes spawn-chip / banner hooks.
- No AI min-impact-gap hard floor in this phase.

---

## 4. UI — note resolve chip

| Property | Spec |
|----------|------|
| Anchor | Telegraph beat cell for `E` (at or just past ScanBar) |
| Look | Short pulse 0.2–0.35s: tier-colored rim (Red/Blue/Purple) + `−1` (or `×N` if multi-hit same beat) |
| Motion | Scale 0.7→1.1→1.0 or fade-up 12–20px; non-blocking |
| Dense stack | New chip does not kill previous; cap ~6 visible; older fade sooner |
| Non-Perfect | Do not use this chip (leak/damage keep separate feedback) |

---

## 5. UI — MULTI banner

| Property | Spec |
|----------|------|
| Trigger | ≥3 Perfect within 0.9s (party) |
| Content | `MULTI` + count (e.g. `×4`) |
| Position | Near ScanBar, above timeline track |
| Lifetime | 0.6s; further Perfects in window refresh count, do not spawn a second banner |

---

## 6. Body anim API

| API | Behavior |
|-----|----------|
| `PlayCounterRestart()` | Play Counter 0→end (gap ≥ `RestartGapSec`) |
| `PlayCounterHitRetrigger()` | Play from ~0.35 normalized time; do not return to Idle between chain hits |
| `PlayCounterBurst()` | Once when burst threshold reached; subsequent notes in window use HitRetrigger |

Enemy `PlayBeCountered` follows the same restart vs retrigger gap rule when multiple enemies are hit in a dense chain (optional same driver path).

---

## 7. Acceptance (Play mode)

1. Gap ≥ 0.28s: each Perfect = full Counter restart + one chip on the correct beat.
2. Chain with gaps &lt; 0.28s: no Idle between hits; each note still gets a chip.
3. ≥3 Perfect / 0.9s: `MULTI ×N` appears; not N full restarts.
4. Telegraph cancel / HitsRequired / damage identical to pre-change behavior.
5. Intro-pause still after beat 6; Deploy / Execute unchanged.

---

## 8. Out of scope (later)

- Timeline information density (CORE/MICRO/EYE tags, W window, hover cover)
- Skill kit redesign
- Empty-beat setup / buff incentives
- Hard AI wall-clock spawn floor (soft floor may be revisited after feel lands)

---

## 9. Implementation notes (for plan)

1. Add `CounterPresentationDriver`; wire from existing Perfect path in `BeatTimelineUIView` (replace direct `PlayCounterAnimations` body).
2. Extend `UnitView` APIs; keep keyword clip resolve.
3. Chip + MULTI as lightweight UI under timeline (pool preferred).
4. Tune defaults in Inspector on CombatPrototype; verify on dense sections of Eternal Spark (early gaps ~0.15–0.30s).

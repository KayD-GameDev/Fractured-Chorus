# Counter Presentation Feel — Implementation Plan

**Date:** 2026-07-14  
**Design spec:** [`../specs/2026-07-14-counter-presentation-feel-design.md`](../specs/2026-07-14-counter-presentation-feel-design.md)  
**Target:** Unity 6 · `Assets/FracturedChorus/` · scene `CombatPrototype`  
**Estimate:** 3 phases · ~0.5–1 ngày

---

## 0. Principles

- Resolve logic (`CombatSession` / `CombatCounterResolver`) **không đổi**
- Feel decision sống ngoài `BeatTimelineUIView` — driver riêng
- Clock: `AudioSettings.dspTime` (khớp `CombatSfxController.PlayPerfectCounter`)
- Intro-pause @ beat 6, Deploy/Execute: không đụng
- Không comment thừa trong code; không asset art mới bắt buộc (UI Text + Image đủ)
- Mỗi phase có acceptance trước khi sang phase sau

### Hook hiện tại (thay thế)

`BeatTimelineUIView.TryPlayCounterEnterSfx` → `PlayPerfectCounter` + `PlayCounterAnimations`  
→ gọi `CounterPresentationDriver.NotifyPerfect(beatIndex)` thay cho body anim trực tiếp; SFX có thể chuyển vào driver hoặc giữ 1 chỗ gọi.

### Out of scope

- Skill kit / empty-beat buff / timeline info density / AI min-gap

---

## Phase 1 — Policy + UnitView APIs (~1–2h)

### Task 1.1 — Pure policy class

**Objective:** Quyết định Restart / HitRetrigger / Burst không phụ thuộc MonoBehaviour.

**Files:**
- Create: `Assets/FracturedChorus/Combat/Presentation/CounterPresentationPolicy.cs`
- Create: `Assets/FracturedChorus/Combat/Presentation/CounterPresentationPolicy.cs.meta` (Unity auto)

```csharp
namespace FracturedChorus.Combat.Presentation
{
    public enum CounterBodyMode
    {
        Restart,
        HitRetrigger,
        Burst
    }

    public sealed class CounterPresentationPolicy
    {
        public float RestartGapSec = 0.28f;
        public float BurstWindowSec = 0.9f;
        public int BurstCount = 3;

        // Party Perfect timestamps in window (dspTime)
        readonly System.Collections.Generic.List<double> _partyHits = new();
        readonly System.Collections.Generic.Dictionary<int, double> _lastUnitHit =
            new(); // unitInstanceId → dspTime
        bool _burstFiredThisWindow;

        public void Reset()
        {
            _partyHits.Clear();
            _lastUnitHit.Clear();
            _burstFiredThisWindow = false;
        }

        /// <summary>Call once per Perfect beat presentation.</summary>
        public CounterBodyMode Decide(int unitInstanceId, double dspNow)
        {
            Prune(dspNow);

            var gapOk = true;
            if (_lastUnitHit.TryGetValue(unitInstanceId, out var last))
            {
                gapOk = (dspNow - last) >= RestartGapSec;
            }

            _partyHits.Add(dspNow);
            _lastUnitHit[unitInstanceId] = dspNow;

            var inWindow = CountInWindow(dspNow);
            if (inWindow >= BurstCount && !_burstFiredThisWindow)
            {
                _burstFiredThisWindow = true;
                return CounterBodyMode.Burst;
            }

            return gapOk ? CounterBodyMode.Restart : CounterBodyMode.HitRetrigger;
        }

        public int PartyHitCountInWindow(double dspNow)
        {
            Prune(dspNow);
            return CountInWindow(dspNow);
        }

        void Prune(double dspNow)
        {
            var cutoff = dspNow - BurstWindowSec;
            _partyHits.RemoveAll(t => t < cutoff);
            if (_partyHits.Count == 0)
            {
                _burstFiredThisWindow = false;
            }
        }

        int CountInWindow(double dspNow) => _partyHits.Count;
    }
}
```

**Verify (Editor):** compile; optional Edit Mode smoke — gọi `Decide` với timestamps giả:
- gap 0.40 → Restart  
- gap 0.15 → HitRetrigger  
- 3 hits trong 0.5s → hit thứ 3 = Burst, hit thứ 4 = HitRetrigger  

*(Repo chưa có NUnit test assembly — verify bằng Debug.Log tạm hoặc Play Mode checklist Phase 3.)*

### Task 1.2 — UnitView body APIs

**Objective:** Tách restart / hit-retrigger / burst; không Idle giữa chuỗi HitRetrigger.

**Files:**
- Modify: `Assets/FracturedChorus/UI/UnitView.cs`

**Changes:**
1. Thêm `[SerializeField] float hitRetriggerNormalizedTime = 0.35f;`
2. Thêm public:
   - `PlayCounterRestart()` → current Counter clip from `normalizedTime = 0`, schedule idle after **remaining** length
   - `PlayCounterHitRetrigger()` → same state, `Play(state, 0, hitRetriggerNormalizedTime)`; **cancel** idle coroutine hoặc reschedule idle từ phần còn lại; **không** force idle nếu Perfect kế tới sớm (driver sẽ gọi lại)
   - `PlayCounterBurst()` → Restart lần đầu hoặc variant cùng clip từ 0 (YAGNI: = Restart visual đủ)
3. Giữ `PlayCounterAnimation()` gọi `PlayCounterRestart()` để tương thích tạm
4. Enemy: `PlayBeCounteredRestart()` / `PlayBeCounteredHitRetrigger()` mirror cùng pattern (hoặc overload mode trên `PlayBeCounteredAnimation(CounterBodyMode)`)

**Idle rule:** Chỉ `ReturnToIdleAfter` khi mode = Restart/Burst; HitRetrigger reschedule idle = `clip.length * (1 - hitRetriggerNormalizedTime)` nhưng nếu API được gọi lại trước đó thì stop coroutine (đã có pattern `_combatAnimRoutine`).

**Acceptance Phase 1:**
- [ ] Project compiles
- [ ] Manual: gọi Restart 2 lần cách 0.4s → 2 full plays; gọi HitRetrigger liên tục → không thấy Idle xen kẽ

**Commit:** `Add counter presentation policy and UnitView retrigger APIs.`

---

## Phase 2 — Driver + wire timeline (~2–3h)

### Task 2.1 — CounterPresentationDriver

**Files:**
- Create: `Assets/FracturedChorus/Combat/Presentation/CounterPresentationDriver.cs`

```csharp
// MonoBehaviour on CombatRoot
// SerializeField: CounterPresentationPolicy fields OR embed policy instance
// SerializeField: CombatSfxController sfx
// SerializeField: BeatTimelineUIView timeline (for chips) — or interface ICounterNoteFeedback
//
// public void NotifyPerfect(int beatIndex, BeatTimelineEngine timeline)
// {
//   var dsp = AudioSettings.dspTime;
//   CollectCounteringPlayerUnits → foreach unit Decide(id, dsp) → UnitView API
//   CollectCounteredEnemyUnits → same gap rule per enemy id
//   sfx.PlayPerfectCounter()  // move from TryPlayCounterEnterSfx OR call once here
//   timeline.SpawnNoteResolveChip(beatIndex, tier, hitsDelta)
//   var count = policy.PartyHitCountInWindow(dsp);
//   if (count >= BurstCount) timeline.ShowOrRefreshMultiBanner(count);
// }
```

Reset policy khi `BeginRoundPlayback` / segment start / encounter end (timeline hoặc controller gọi `driver.Reset()`).

### Task 2.2 — Wire BeatTimelineUIView

**Files:**
- Modify: `Assets/FracturedChorus/UI/BeatTimelineUIView.cs` (~1066–1107)

**Changes:**
1. `[SerializeField] CounterPresentationDriver counterPresentation;` + setter từ Bind/bootstrap
2. `TryPlayCounterEnterSfx`: giữ gate `_precomputedCounterBeats` / `_lastCounterSfxBeat`; thay `PlayPerfectCounter` + `PlayCounterAnimations` bằng:

```csharp
if (counterPresentation != null)
{
    counterPresentation.NotifyPerfect(beatIndex, _timeline);
}
else
{
    // fallback cũ
    combatSfxController?.PlayPerfectCounter();
    PlayCounterAnimations(beatIndex);
}
```

3. Xóa hoặc deprecate private `PlayCounterAnimations` sau khi fallback không cần
4. Public stubs (implement Phase 3 nếu chưa): `SpawnNoteResolveChip`, `ShowOrRefreshMultiBanner` — no-op OK trong Task 2.2

### Task 2.3 — Bootstrap

**Files:**
- Modify: `Assets/FracturedChorus/Combat/Bootstrap/CombatPrototypeBootstrap.cs`
- Optional Editor: `Assets/FracturedChorus/Editor/CombatSceneSetupEditor.cs` — Ensure component on CombatRoot

Awake: `GetComponent` / `AddComponent<CounterPresentationDriver>()`, assign sfx + timeline refs, pass into `timelineView` bind path.

**Acceptance Phase 2:**
- [ ] Play CombatPrototype: Perfect vẫn cancel telegraph như cũ
- [ ] Gap rộng: full Counter restart
- [ ] Gap hẹp (early song dens): không Idle giật; SFX vẫn nghe
- [ ] Intro-pause beat 6 + Deploy/Execute OK

**Commit:** `Wire CounterPresentationDriver into Perfect counter path.`

---

## Phase 3 — Note chip + MULTI banner (~2–3h)

### Task 3.1 — Chip view + pool

**Files:**
- Create: `Assets/FracturedChorus/UI/CounterNoteResolveChipView.cs`
- Modify: `Assets/FracturedChorus/UI/BeatTimelineUIView.cs` (spawn helper)
- Optional: child under `BeatTimelineUI/Viewport` — `ResolveChipLayer` (RectTransform full stretch, raycast off)

**Chip behavior:**
- Parent to chip layer; position = world/anchored pos of beat slot `E` (reuse slot offset API đã có: `_slotOffsetPx` / `FindSlotAtContentPos` inverse)
- Color từ tier (reuse palette `BeatSegmentView` — extract static `BossNoteTierColors` nhỏ vào `Combat/Presentation` hoặc shared UI util để DRY)
- Text `-1`; lifetime 0.25–0.35s scale pulse; pool size 6; dequeue oldest khi full

### Task 3.2 — MULTI banner

**Files:**
- Create: `Assets/FracturedChorus/UI/CounterMultiBannerView.cs`  
  hoặc nested under timeline: single `Text` + CanvasGroup gần ScanBar

**Behavior:**
- `ShowOrRefresh(count)`: set `MULTI ×{count}`, alpha 1, restart 0.6s fade timer
- Không spawn instance thứ 2

### Task 3.3 — Driver → UI

Hoàn thiện `NotifyPerfect`: đọc telegraph tier tại beat (nếu nhiều telegraph, chip theo telegraph được counter / primary); `hitsDelta = 1` mặc định.

### Task 3.4 — Play Mode acceptance (spec §7)

| # | Test | Pass |
|---|------|------|
| 1 | Gap ≥ 0.28s | Full restart + 1 chip đúng beat |
| 2 | Chuỗi gap &lt; 0.28s | Không Idle giữa; mỗi nốt 1 chip |
| 3 | ≥3 Perfect / 0.9s | `MULTI ×N`; không N full restart |
| 4 | Cancel / HitsRequired / dmg | Identical pre-change |
| 5 | Intro beat 6 · Deploy/Execute | Unchanged |

Dense check: Eternal Spark early beats (gaps ~0.15–0.30s) sau Deploy → Execute.

**Commit:** `Add note resolve chips and MULTI banner for dense counters.`

---

## File map (summary)

| Action | Path |
|--------|------|
| Create | `Combat/Presentation/CounterPresentationPolicy.cs` |
| Create | `Combat/Presentation/CounterPresentationDriver.cs` |
| Create | `UI/CounterNoteResolveChipView.cs` |
| Create | `UI/CounterMultiBannerView.cs` (or fold into timeline) |
| Modify | `UI/UnitView.cs` |
| Modify | `UI/BeatTimelineUIView.cs` |
| Modify | `Bootstrap/CombatPrototypeBootstrap.cs` |
| Optional | `Editor/CombatSceneSetupEditor.cs` |
| Untouched | `CombatSession`, `CombatCounterResolver`, `TimelineConstants`, skill data |

---

## Risks

| Risk | Mitigation |
|------|------------|
| Chip position lệch khi scroll | Anchor theo content pos của beat, không screen-fixed |
| Burst fire mỗi Perfect sau ngưỡng | Flag `_burstFiredThisWindow` reset khi window empty |
| SFX double-play | Chỉ gọi `PlayPerfectCounter` **một** nơi (driver) |
| HitRetrigger không có hit-frame thật | Tunable `0.35`; art pass sau |

---

## Handoff

Sau khi plan này OK: implement theo Phase 1 → 2 → 3, commit từng phase.

**Không** làm song song skill kit / empty-beat / UI intel trong cùng PR.

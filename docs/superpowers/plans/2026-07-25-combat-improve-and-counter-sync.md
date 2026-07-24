# Combat Improve + Counter Beat Sync — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Khép meta-loop combat (win/lose → RunMap), mở Battle/Elite encounter, tăng game-feel, rồi **khóa SFX counter đúng beat nhạc** (không sớm/trễ cảm nhận được).

**Architecture:** Giữ `CombatSession` / `CombatCounterResolver` làm logic; meta exit qua `RunMapSceneLoader` + run state; encounter inject qua static/session payload vào `CombatPrototypeBootstrap`; counter SFX schedule theo **DSP time của beat map**, không theo frame UI.

**Tech Stack:** Unity 6 · C# · `CombatPrototype.unity` · `RunMapPrototype` · `MusicBeatMapSO` · `AudioSettings.dspTime`

## Global Constraints

- Không đổi công thức damage / counter resolve logic trừ khi task nêu rõ
- Intro-pause @ beat 6 giữ nguyên
- Scene Hierarchy vẫn SoT cho layout; không spawn UI ẩn phá `preserveSceneLayout`
- Không NUnit trong repo — verify bằng Play Mode checklist + log DSP offset
- Mỗi task ship được / test được độc lập trước khi sang task sau
- Commit chỉ khi user yêu cầu

### File map (toàn plan)

| Action | Path |
|--------|------|
| Modify | `Assets/FracturedChorus/Combat/Core/CombatController.cs` |
| Modify | `Assets/FracturedChorus/Combat/Bootstrap/CombatPrototypeBootstrap.cs` |
| Modify | `Assets/FracturedChorus/UI/BeatTimelineUIView.cs` |
| Modify | `Assets/FracturedChorus/Audio/CombatSfxController.cs` |
| Modify | `Assets/FracturedChorus/Audio/CombatMusicController.cs` |
| Modify | `Assets/FracturedChorus/RunMap/RunMapController.cs` |
| Modify | `Assets/FracturedChorus/RunMap/CadenceMapController.cs` |
| Modify | `Assets/FracturedChorus/RunMap/RunMapSceneLoader.cs` |
| Create | `Assets/FracturedChorus/Combat/Bootstrap/CombatEncounterHandoff.cs` |
| Create | `Assets/FracturedChorus/UI/CombatResultOverlayUIView.cs` (hoặc mở rộng overlay hiện có) |
| Create/Author | `Assets/FracturedChorus/Resources/Encounters/*.asset` |
| Untouched (P0) | `CombatCounterResolver` hit math · Cover charge rules · VN/Hub |

---

## Bối cảnh — vì sao thứ tự này

1. **P0 meta exit** trước: không exit thì mọi polish combat bị kẹt trong scene chết.
2. **P0 encounter mapping** trước content mới: một scene, nhiều node type.
3. **P1 counter sync** ngay sau: fantasy “đánh đúng beat” là USP; hiện có lệch có hệ thống (xem Task 4).
4. **P1 floating feedback** sau sync: số bay phải cùng lúc với SFX đã khóa beat.
5. **P2 stubs / prefab** cuối: cắt nợ hoặc ship — không mở rộng hệ mới.

---

### Task 1: Win/Lose overlay + return RunMap

**Why:** `CombatController` chỉ `Debug.Log` Victory/Defeat; `HandleEncounterEnded` dừng UI/nhạc, **không** `LoadScene`. Boss = soft-lock.

**Files:**
- Create: `Assets/FracturedChorus/UI/CombatResultOverlayUIView.cs`
- Modify: `Assets/FracturedChorus/Combat/Core/CombatController.cs` (`HandleEncounterEnded`, phase Victory/Defeat)
- Modify: `Assets/FracturedChorus/RunMap/RunMapSceneLoader.cs` (thêm `LoadRunMapPrototype` nếu chưa có API rõ)
- Modify: scene `CombatPrototype.unity` — gắn overlay dưới `CombatCanvas`

**Interfaces:**
- Consumes: `CombatPhase`, `OnEncounterEnded`
- Produces: `CombatResultOverlayUIView.Show(won)`, button → load RunMap; handoff flag boss cleared / failed

- [ ] **Step 1: Thêm overlay tối giản**

`CombatResultOverlayUIView`: panel + title (`VICTORY` / `DEFEAT`) + button `Continue`. `Show(bool victory)` bật panel, chặn input timeline.

- [ ] **Step 2: Wire `HandleEncounterEnded`**

Trong `CombatController.HandleEncounterEnded`: sau stop music/timeline → `resultOverlay.Show(_session.Phase == CombatPhase.Victory)`.

- [ ] **Step 3: Continue → RunMap**

Button gọi `CombatEncounterHandoff.SetResult(...)` rồi `RunMapSceneLoader.LoadRunMapPrototype()` (Single).

- [ ] **Step 4: Play Mode checklist**

1. RunMap → Boss → combat → giết hết enemy → overlay Victory → Continue → về RunMap.
2. Defeat (hạ hết party) → overlay Defeat → Continue → về RunMap (node fail theo rule hiện có hoặc stub log).

**Done when:** Không còn kẹt trong `CombatPrototype` sau win/lose.

---

### Task 2: Encounter handoff + Battle/Elite vào combat

**Why:** Chỉ Boss load `CombatPrototype`. Battle/Elite là type trên map nhưng không combat → run map “giả”.

**Files:**
- Create: `Assets/FracturedChorus/Combat/Bootstrap/CombatEncounterHandoff.cs`
- Modify: `Assets/FracturedChorus/Combat/Bootstrap/CombatPrototypeBootstrap.cs`
- Modify: `Assets/FracturedChorus/RunMap/RunMapController.cs` / `CadenceMapController.cs`
- Create: `Resources/Encounters/Encounter_Battle_Grunt.asset`, `Encounter_Elite_*.asset`, `Encounter_Boss_Despair.asset` (hoặc reuse factory data → SO)

**Interfaces:**
- `CombatEncounterHandoff.SetPending(encounterId, nodeType, returnScene)`
- Bootstrap: nếu handoff có id → load SO; else scene units / demo factory (giữ fallback)

- [ ] **Step 1: `CombatEncounterHandoff` static**

```csharp
namespace FracturedChorus.Combat.Bootstrap
{
    public static class CombatEncounterHandoff
    {
        public static string EncounterId { get; private set; }
        public static string ReturnSceneName { get; private set; } = "RunMapPrototype";
        public static bool HasPending => !string.IsNullOrEmpty(EncounterId);

        public static void SetPending(string encounterId, string returnScene = "RunMapPrototype")
        {
            EncounterId = encounterId;
            ReturnSceneName = returnScene;
        }

        public static void Clear()
        {
            EncounterId = null;
        }
    }
}
```

- [ ] **Step 2: Bootstrap đọc handoff**

`Awake`: nếu `HasPending` → `Resources.Load<EncounterDefinitionSO>("Encounters/" + id)` → spawn; `Clear()` sau khi consume.

- [ ] **Step 3: Map nodes**

`Battle` / `Elite` / `Boss` đều delay → `LoadCombatPrototype` với encounter id khác nhau (table nhỏ trong controller hoặc catalog).

- [ ] **Step 4: Play Mode**

Battle node → combat grunt pack; Boss → despair; Continue về đúng `ReturnSceneName`.

**Done when:** 3 node types vào cùng scene với roster khác nhau.

---

### Task 3: Kết quả run state (boss clear / fail)

**Why:** Về map mà không cập nhật graph → player vào lại boss vô hạn hoặc mất progress.

**Files:**
- Modify: `RunState` / `CadenceRunProgress` (file hiện đang dùng cho clear node)
- Modify: `CombatEncounterHandoff` thêm `LastVictory`, `LastNodeId`
- Modify: RunMap OnEnable/Start apply handoff result

- [ ] **Step 1:** Handoff ghi `LastVictory` + node id trước khi load combat.
- [ ] **Step 2:** Khi return, RunMap mark node cleared / apply fail rule (đúng convention map hiện tại).
- [ ] **Step 3:** Play Mode: thắng boss → node cleared; thua → không clear (hoặc rule đã chốt).

**Done when:** Win/lose ảnh hưởng map state.

---

### Task 4: Counter SFX khớp beat nhạc (P0 feel)

**Why / audit hiện tại (đã đọc code — chưa Play Mode):**

| Cơ chế | Hành vi | Hệ quả |
|--------|---------|--------|
| Music sync | `_localBeat = music.TotalMusicalBeat - roundStart` → scroll | Scan **bám** beat map ✓ |
| Hit fire | `GetBeatHitContentX` dùng `beatHitAnchorT` **default 0.5** | Fire khi scan ở **giữa slot** = ~**nửa beat sau** downbeat map ✗ |
| SFX trigger | `TryPlayCounterEnterSfx` → `NotifyPerfect` → `PlayPerfectCounter` | Cùng lúc resolve logic (nhất quán UI, lệch nhạc) |
| `PlayScheduled(dspTime)` với `dspTime = now` | Không schedule tới thời điểm beat; gần như “play ASAP” | Thêm jitter buffer ~1 frame / audio quantum |
| Cache | `BeginRoundPlayback(false)` gọi `ResetCounterSfxState()` **xóa cache**; `PrepareSegmentScanStart` chỉ `RebuildCounterBeatCache` khi `continueFromHold` | Intro path phụ thuộc `ResumeRoundPlayback` rebuild — dễ quên khi đổi flow |

**Kết luận audit:** Counter **chưa khớp downbeat nhạc**. Nó khớp **tâm ô timeline** (`beatHitAnchorT = 0.5`). Với beat map onset-style, nghe = **trễ ~½ beat**. Đây là lệch có hệ thống, không phải random drift nhỏ.

**Files:**
- Modify: `Assets/FracturedChorus/UI/BeatTimelineUIView.cs`
- Modify: `Assets/FracturedChorus/Audio/CombatSfxController.cs`
- Modify: `Assets/FracturedChorus/Audio/CombatMusicController.cs` (API DSP beat time)
- Modify: `Assets/FracturedChorus/Combat/Presentation/CounterPresentationDriver.cs` (optional: nhận scheduled time)

**Target feel:** Onset counter SFX trong **±30ms** so với `beatTimesSec[beat]` của map (sau khi bù clip padding nếu có).

- [ ] **Step 1: Chốt semantic hit = đầu beat**

Đổi default `beatHitAnchorT` → `0f` (đầu nốt = musical beat index nguyên).  
Nếu art/timeline đang “căn giữa” có chủ đích: giữ visual riêng, nhưng **SFX + resolve** dùng anchor 0 — tách `logicHitAnchorT` vs visual nếu cần.

- [ ] **Step 2: API thời điểm DSP của beat**

`CombatMusicController`:

```csharp
public bool TryGetDspTimeForMusicalBeat(float musicalBeat, out double dspTime)
{
    // musicalBeat absolute trên track đang play
    // dspTime = AudioSettings.dspTime + (beatMap.MusicalBeatToTime(musicalBeat) - source.time) / pitch
}
```

Thêm inverse `MusicalBeatToTime` trên `MusicBeatMapSO` nếu chưa có.

- [ ] **Step 3: Schedule SFX đúng beat, không `PlayScheduled(now)`**

`CombatSfxController.PlayPerfectCounter(double dspTime)`:
- nếu `dspTime > AudioSettings.dspTime` → `PlayScheduled(dspTime)`
- nếu đã trễ < 30ms → play immediate
- nếu trễ > 30ms → skip hoặc play immediate + log (tránh “đuổi theo” muộn)

`NotifyPerfect` / `TryPlayCounterEnterSfx`: tính `targetDsp` từ beat index + music controller; truyền vào SFX.

- [ ] **Step 4: Rebuild counter cache mọi path playback**

Trong `PrepareSegmentScanStart` (cả nhánh không `continueFromHold`) gọi `RebuildCounterBeatCache()` sau layout.  
`BeginRoundPlayback`: reset state rồi rebuild trong `PrepareSegmentScanStart` (không để cache rỗng).

- [ ] **Step 5: Đo offset Play Mode**

Log khi counter fire:

```text
[CounterSync] beat=N musicBeat=X.xxx deltaMs=Y
```

`deltaMs = (source.time - beatMap.TimeOfBeat(N)) * 1000`.

Acceptance: 10 counter liên tiếp, `|deltaMs| <= 30` (trừ frame hitch >50ms có log riêng).

- [ ] **Step 6: Kiểm tra clip**

Mở `Combat_PerfectCounter.wav` — nếu có silence đầu >10ms, trim import hoặc bù `sfxLeadSec` âm trong schedule.

**Done when:** Counter nghe dính kick/snare của Eternal Spark; log delta trong budget.

---

### Task 5: Floating damage + HP bar punch

**Why:** `HandleUnitHpChanged` trống; damage chủ yếu log → depth combat không “đọc” được trên board.

**Files:**
- Modify: `CombatController.HandleUnitHpChanged`
- Create/Modify: floating text helper trên `UnitView` hoặc canvas world/UI
- Wire crit / Perfect tint nếu đã có sprite feedback

- [ ] **Step 1:** Subscribe HP → spawn số + punch bar.
- [ ] **Step 2:** Counter beat: số/crit không che SFX (spawn cùng frame hoặc +1 frame sau SFX đã schedule).
- [ ] **Step 3:** Play Mode — hit thường / crit / heal đọc rõ.

**Done when:** Mọi `TakeDamage` / heal có feedback nhìn thấy trên unit hoặc bar.

---

### Task 6: Stubs — ship hoặc cắt

**Why:** Nợ nửa vời làm design/docs lệch runtime.

| Stub | Quyết định đề xuất |
|------|-------------------|
| `MoveActionCommand` (UC-06) | Cắt khỏi UI nếu chưa design; hoặc ship swap 1 ô |
| `DualGrid.GetCoverModifier` luôn `1f` | Cắt claim “positional cover” khỏi docs hoặc implement 1 modifier đơn giản |
| `CycleShift` → damage | Rename effect hoặc implement thật |
| `GuardCharge` perfect log | Wire vào `BlockBarrier` hoặc remove flag |

- [ ] **Step 1:** Chốt bảng Cut vs Ship với designer (1 quyết định/stub).
- [ ] **Step 2:** Implement hoặc xóa claim + dead code path.
- [ ] **Step 3:** Cập nhật `docs/combat/*.md` cho khớp.

**Done when:** Không còn stub “giả có” trong skill tooltips / docs.

---

### Task 7: Prefab hóa combat unit/UI (P2)

**Why:** `Prefabs/Combat/` trống; scene khổng lồ khó clone encounter.

- [ ] Extract `Unit_*` + party card template → prefab.
- [ ] Bootstrap instantiate từ prefab khi spawn encounter SO.
- [ ] Scene prototype vẫn author được layout mẫu.

**Done when:** Encounter mới không cần copy-paste Hierarchy thủ công toàn bộ.

---

## Thứ tự thực thi khuyến nghị

```text
Task 1 (exit) → Task 2 (encounter) → Task 3 (run state)
       → Task 4 (counter sync) → Task 5 (floating FX)
       → Task 6 (stubs) → Task 7 (prefabs)
```

Không làm Task 5 trước Task 4: số bay lệch beat sẽ “khóa sai” cảm giác rhythm.

---

## Counter sync — ý kiến (tóm tắt cho review)

**Chưa khớp downbeat nhạc.** Pipeline music→scroll đúng hướng; điểm fire cố ý đặt giữa slot (`beatHitAnchorT = 0.5`) nên SFX/resolve trễ ~½ beat so với marker trong `EternalSpark_*_BeatMap`. `PlayScheduled(now)` không bù latency tới beat. Ưu tiên sửa Task 4 trước khi polish VFX khác.

---

## Self-check

| Yêu cầu | Task |
|---------|------|
| Win/lose UI + về map | 1 |
| Battle/Elite combat | 2 |
| Persist clear/fail | 3 |
| Counter đúng beat nhạc | 4 |
| Feedback HP/dmg | 5 |
| Stub debt | 6 |
| Prefab scale | 7 |
| Không placeholder TBD | ✓ paths/API cụ thể |

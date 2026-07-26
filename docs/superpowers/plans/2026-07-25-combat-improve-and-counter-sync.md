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

- [x] **Step 1: Overlay** — `CombatResultOverlayUIView` + art Result/ (Victory/Defeat/Continue/Retry)
- [x] **Step 2: Wire `HandleEncounterEnded`** — stop music + show overlay
- [x] **Step 3: Continue / Retry** — handoff + Cadence victory pending / camp return; Retry reload scene
- [ ] **Step 4: Play Mode checklist** (user)

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

- [x] **Step 1: `CombatEncounterHandoff` + `EncounterCatalog`**
- [x] **Step 2: Bootstrap load handoff encounter (party scene + enemy SO)**
- [x] **Step 3: Map Battle/Elite/Boss → combat; Treasure/Event/Relay toast**
- [ ] **Step 4: Menu Create Encounter Assets + Play Mode**

~~legacy steps below~~

- [x] **Step 1b: `CombatEncounterHandoff` static**

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

- [x] **Step 1:** Handoff result + reward stub + `LastFoughtEncounterId`
- [x] **Step 2:** Boss không clear trước combat; victory → Cadence + clear source node; defeat → camp + full heal HP store
- [x] **Step 3:** `PartyRunHpStore` persist HP; Prep reset mỗi fight
- [ ] **Step 4:** Play Mode verify

**Done when:** Win/lose ảnh hưởng map state + HP persist.

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

- [x] **Step 1: Chốt semantic hit = đầu beat** — `beatHitAnchorT = 0` (code + scene)
- [x] **Step 2: API DSP** — `MusicalBeatToTime` + `TryGetDspTimeForMusicalBeat` / `TryGetMusicDeltaMs`
- [x] **Step 3: Schedule SFX** — `PlayPerfectCounter(targetDsp)`; late ≤30ms immediate; >30ms warn + immediate
- [x] **Step 4: Rebuild counter cache** — mọi nhánh `PrepareSegmentScanStart`
- [ ] **Step 5: Play Mode** — log `[CounterSync]`; accept `|deltaMs|≤30`
- [x] **Step 6: Clip** — trim ~46ms silence `Perfect sound Game.wav` (onset≈0ms)

**Done when:** Counter nghe dính kick/snare của Eternal Spark; log delta trong budget.

---

### Task 5: Floating damage + HP bar punch

**Why:** `HandleUnitHpChanged` trống; damage chủ yếu log → depth combat không “đọc” được trên board.

**Files:**
- Modify: `CombatController.HandleUnitHpChanged`
- Create/Modify: floating text helper trên `UnitView` hoặc canvas world/UI
- Wire crit / Perfect tint nếu đã có sprite feedback

- [x] **Step 1:** `LastHpChange` + float digits + unit/bar punch
- [x] **Step 2:** Spawn cùng frame HP event (sau SFX schedule path; không chặn audio)
- [ ] **Step 3:** Play Mode — hit thường / crit / heal đọc rõ
- [ ] **Art note:** CRIT badge gen lệch chữ (HIT) — tạm ẩn; crit = scale lớn hơn

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

- [x] **Step 1:** Chốt bảng Cut vs Ship với designer (1 quyết định/stub).
- [x] **Step 2:** Implement hoặc xóa claim + dead code path.
- [x] **Step 3:** Cập nhật `docs/combat/*.md` cho khớp.

**Done when:** Không còn stub “giả có” trong skill tooltips / docs. ✅

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

---

## Decision gates — câu hỏi duyệt (chờ user)

Trả lời từng câu với Diana. Chỉ implement khi gate đã chốt.

**Quyết định đã chốt — ĐỦ GATE:** `G-Q1: C` · `G-Q2: A` · xem bảng tổng kết bên dưới.

**T6-Q4 rule (user):**
- Space trên timeline (thanh/beat đỏ boss) tại beat **chưa** bị block
- Đặt barrier → giảm damage nhận
- Block đúng timing (OnBeat) → phát **perfect/clash sound** (cùng clip counter)
- Hệ `BlockInputController` / `BlockBarrier` đã có nền — Task 6 polish + SFX; flag Bulwark stub bỏ pretend hoặc gộp vào flow này

**Art số bay (T5):**
- `Art/UI/Combat/DamageNumbers/combat_dmg_digits_holo_v1.png` (+ v2 nếu cleaner)
- `Art/UI/Combat/DamageNumbers/combat_heal_digits_holo_v1.png`
- `Art/UI/Combat/DamageNumbers/combat_crit_badge_holo_v1.png`
- Mirror: `Resources/UI/Combat/DamageNumbers/`

**Art đã gen (T1):**
- `Assets/FracturedChorus/Art/UI/Combat/Result/combat_result_victory_v1.png`
- `Assets/FracturedChorus/Art/UI/Combat/Result/combat_result_defeat_v1.png`
- `Assets/FracturedChorus/Art/UI/Combat/Result/combat_btn_continue_v1.png`
- `Assets/FracturedChorus/Art/UI/Combat/Result/combat_btn_retry_v1.png`
- Mirror: `Resources/UI/Combat/Result/`

### Task 1 — Win/Lose + return RunMap

| ID | Câu hỏi | Options | Đề xuất | Chốt |
|----|---------|---------|---------|------|
| T1-Q1 | Overlay copy ngôn ngữ? | A) EN (`VICTORY`/`DEFEAT`) · B) VI · C) EN + VI phụ | A | **A** |
| T1-Q2 | Sau Continue luôn về đâu? | A) `RunMapPrototype` · B) scene đã lưu trong handoff · C) Hub nếu fail | B | **B** |
| T1-Q3 | Defeat có cho Retry trong combat không? | A) Chỉ Continue về map · B) thêm Retry (reload encounter) · C) Retry + Continue | A | **C** — Continue = về **camp gần nhất** (set up), không phải node vừa thua |
| T1-Q4 | Nhạc khi hiện overlay? | A) Stop hẳn · B) duck volume · C) stinger riêng (chưa có asset thì A) | A | **A** |
| T1-Q5 | Scope Task 1 có cần art overlay mới không? | A) Panel Text · B) chờ art · C) reuse Execute · Gen = Diana tạo art | A | **Gen** — art hologram đã tạo (`Result/`) |

### Task 2 — Encounter handoff + Battle/Elite

| ID | Câu hỏi | Options | Đề xuất | Chốt |
|----|---------|---------|---------|------|
| T2-Q1 | Node nào vào combat ở sprint này? | A) Battle+Elite+Boss · B) chỉ Battle+Boss · C) chỉ Boss (Task 2 hoãn) | A | **A** |
| T2-Q2 | Roster tạm khi chưa có SO đủ? | A) Author 3 Encounter SO ngay · B) map type → factory keys (Grunt/Elite/Boss) · C) mọi node dùng demo hiện tại | B | **A** |
| T2-Q3 | Party vào combat lấy từ đâu? | A) Scene/default formation như nay · B) persist HP từ run · C) full heal mỗi fight | A (B để Task 3+) | **B** |
| T2-Q4 | Treasure/Camp/Event có đụng Task 2 không? | A) Không — giữ behavior cũ · B) stub toast “coming soon” | A | **B** |

### Task 3 — Run state win/lose

| ID | Câu hỏi | Options | Đề xuất | Chốt |
|----|---------|---------|---------|------|
| T3-Q1 | Thắng Boss →? | A) Clear boss + dùng `NotifyBossVictory` / flow Cadence sẵn có · B) chỉ mark node visited · C) về Hub luôn | A | **A** |
| T3-Q2 | Thắng Battle/Elite →? | A) Clear node, mở path tiếp · B) clear + reward screen (hoãn reward) · C) clear + heal party | A | **B** |
| T3-Q3 | Thua combat →? | A) Về map, **không** clear node (vào lại được) · B) Game Over / về Hub · C) mất run | A | **A** — Continue → camp gần nhất |
| T3-Q4 | Có persist HP/Prep giữa node không (sprint này)? | A) Không — mỗi fight full · B) Có persist · C) chỉ persist HP, Prep reset | A | **C** |

### Task 4 — Counter sync beat

| ID | Câu hỏi | Options | Đề xuất | Chốt |
|----|---------|---------|---------|------|
| T4-Q1 | Hit semantic muốn khớp cái gì? | A) **Downbeat / đầu slot** (`beatHitAnchorT=0`) · B) giữ giữa slot (0.5) · C) tách: resolve@0, SFX có offset ms chỉnh Inspector | A | **A** |
| T4-Q2 | Đổi hit có kéo **damage resolve** cùng lúc không? | A) Có — SFX + resolve cùng anchor · B) Chỉ SFX; resolve giữ 0.5 | A | **A** |
| T4-Q3 | Budget sync chấp nhận? | A) ±30ms · B) ±50ms · C) “nghe ổn” không cần log | A | **A** |
| T4-Q4 | Clip counter hiện là Clash Hit — có trim silence đầu không? | A) Đo rồi trim nếu >10ms · B) chỉ bù `sfxLeadSec` · C) để nguyên | A | **A** — đo được ~46ms silence |
| T4-Q5 | Thứ tự so với Task 1–3? | A) Làm Task 4 **sau** meta loop (như plan) · B) Ưu tiên Task 4 **trước** Task 1 | A | **A** |

### Task 5 — Floating damage / bar punch

| ID | Câu hỏi | Options | Đề xuất | Chốt |
|----|---------|---------|---------|------|
| T5-Q1 | Số bay hiện ở đâu? | A) Trên đầu unit world/UI · B) Chỉ punch party/enemy bar · C) Cả hai | C | **C** |
| T5-Q2 | Style số? | A) Text TMP · B) chờ sprite · C) màu harmony · **Gen sprite** | A | **Gen sprite** — digits dmg/heal + CRIT badge |
| T5-Q3 | Làm Task 5 khi nào? | A) Sau Task 4 · B) Song song Task 4 · C) Hoãn P2 | A | **A** |

### Task 6 — Stubs cut/ship

| ID | Câu hỏi | Options | Đề xuất | Chốt |
|----|---------|---------|---------|------|
| T6-Q1 | `MoveActionCommand` (UC-06)? | A) **Cut** khỏi UI/docs sprint này · B) Ship swap 1 ô · C) Giữ stub im lặng | A | **A** |
| T6-Q2 | Positional cover (`GetCoverModifier=1`)? | A) **Cut claim** docs · B) Ship modifier đơn giản front-column · C) Giữ | A | **B** — front: −dmg nhận; back: +dmg gây & +heal potency (số % chốt lúc implement) |
| T6-Q3 | `CycleShift`? | A) Cut/rename thành damage rõ ràng · B) Implement thật · C) Giữ | A | **A** — chưa design |
| T6-Q4 | `GuardCharge` perfect? | A) Cut flag · B) Wire vào BlockBarrier · C) Giữ log | A | **B** — Space block beat đỏ chưa chặn; đúng → perfect SFX |

### Task 7 — Prefabs (P2)

| ID | Câu hỏi | Options | Đề xuất | Chốt |
|----|---------|---------|---------|------|
| T7-Q1 | Làm trong sprint này? | A) Hoãn sau Task 1–5 · B) Làm ngay sau Task 2 · C) Bỏ khỏi plan | A | **A** |
| T7-Q2 | Prefab tối thiểu? (khi làm sau) | A) Unit + PartyCard only · B) + Beat slot template · C) Full combat UI | A | **A** |

### Gate toàn plan

| ID | Câu hỏi | Options | Đề xuất | Chốt |
|----|---------|---------|---------|------|
| G-Q1 | Scope sprint đầu? | A) Task 1–4 only · B) Task 1–5 · C) Full 1–7 | A | **C** = Task 1–6 (T7 hoãn) |
| G-Q2 | Commit sau mỗi task? | A) User yêu cầu từng lần · B) Auto commit mỗi task done | A | **A** |

### Tổng kết quyết định (locked)

| Gate | Chốt |
|------|------|
| T1 | EN · Continue theo handoff · Defeat Retry+Continue→camp · Stop nhạc · Art `Result/` |
| T2 | **Boss only → combat** (boss+mini hiện có). Battle/Elite chưa design → toast coming soon. Catalog/SO giữ cho sau. |
| T3 | Boss Cadence victory · Battle/Elite reward screen · Thua→camp không clear · HP persist, Prep reset |
| T4 | Hit đầu beat · SFX+resolve cùng · ±30ms · Trim ~46ms silence · sau Task 1–3 |
| T5 | Số bay + bar punch · sprite digits/CRIT · sau Task 4 |
| T6 | Cut Move · Ship positional front/back · Cut CycleShift pretend · Guard Space+perfect SFX |
| T7 | Hoãn · sau này Unit+PartyCard |
| G | Sprint **Task 1–6** · Commit khi user yêu cầu |

**Thứ tự implement:** `1 → 2 → 3 → 4 → 5 → 6`

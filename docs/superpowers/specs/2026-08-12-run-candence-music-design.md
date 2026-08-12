# Run Candence Music — Design Spec

> **Ngày:** 2026-08-12  
> **Trạng thái:** Approved  
> **Track nguồn:** `Eternal Spark - Candence.mp3` (user Downloads → import project)  
> **Liên quan:** [`2026-08-01-uniform-beat-continuous-music-design.md`](2026-08-01-uniform-beat-continuous-music-design.md) (boss path — không thay đổi)

---

## 1. Vấn đề

Run map (Pinky Vault → inner nodes → Battle/Elite) cần nhạc riêng, liền mạch qua scene load, beat sync combat — **tách biệt** boss track (`EternalSpark_BossRemix`).

| # | Hiện trạng | Vấn đề |
|---|-----------|--------|
| 1 | `RunMapBgmController` phát `The_Locked_Vault.mp3`, destroy khi load combat | Nhạc đứt khi vào trận |
| 2 | `CombatMusicController` chỉ boss remix + intro 12 beat | Pooled combat không có run music |
| 3 | `TimelineConstants` hardcoded boss (677 beat, intro 12) | Không áp dụng cho Candence |
| 4 | Scene load `Single` | AudioSource scene-local không persist |

---

## 2. Mục tiêu

1. **Candence** chạy từ lúc chọn **Pinky Vault** — beat 0 ngay (`firstBeatOffsetSec = 0`, không intro gameplay).
2. **Inner map:** volume thấp (40%), nhịp vẫn chạy.
3. **Battle/Elite combat:** volume full (100%), timeline sync liền mạch từ beat hiện tại — **không intro scan**.
4. **Planning duck** giống boss: 70% volume + low-pass 900 Hz, fade 0.25s.
5. **Boss node:** pause Candence → BossRemix + intro 12 beat → thắng boss, resume Candence đúng beat đã pause.
6. **Return Hub:** stop Candence session; macro map quay `The_Locked_Vault`.
7. **Boss scene/playtest riêng:** không regression path boss hiện tại.

---

## 3. Quyết định thiết kế

### 3.1 Hướng triển khai: `RunMusicSession` DontDestroyOnLoad

Session riêng sống qua scene load. `CombatMusicController` **giữ nguyên** — chỉ phục vụ boss track.

**Không** mở rộng `CombatMusicController` thêm profile Run (tránh regression boss, file phình).

### 3.2 Asset & beat map

| Field | Giá trị |
|-------|---------|
| Clip path | `Assets/FracturedChorus/Audio/Music/EternalSpark_Candence.mp3` |
| Beat map | `Assets/FracturedChorus/Audio/Music/EternalSpark_Candence_BeatMap.asset` |
| BPM | **152.0000** (đo `Tools/beat-analyzer`, trùng boss tempo) |
| `firstBeatOffsetSec` | **0** — beat 0 @ `t=0` khi `Begin()` (user: không intro gameplay) |
| `BEAT_SPAN_SEC` | 0.394737 |
| `DURATION_SEC` | 271.8935 |
| `TOTAL_BEATS` | **689** |
| Loop | Bar-aligned (fade 50ms), `_loopBeatAccum` giữ `TotalMusicalBeat` monotonic |

Ghi chú: analyzer đo downbeat acoustic ~1.489s; gameplay grid bắt đầu @ 0 theo yêu cầu product.

### 3.3 Volume modes

| Mode | Volume | Low-pass | Fade |
|------|--------|----------|------|
| Map | 0.40 | 22000 Hz (open) | 0.25s |
| Combat | 1.00 | 22000 Hz | 0.25s |
| Planning (duck) | 0.70 | 900 Hz | 0.25s (duckFadeSec) |
| BossPaused | 0.00 (pause source) | — | instant |

Map ↔ Combat transition: fade 0.25s.

### 3.4 Session lifecycle

```
Pinky Vault selected
  → RunMusicSession.Ensure().Begin(clip, beatMap)   // beat 0, play

Inner map active
  → SetMode(Map)                                     // 40%

Load CombatPrototype (Battle/Elite)
  → session persists; RunCombatMusicBridge attaches

Combat Start (pooled)
  → SetMode(Combat)                                  // 100%
  → skip StartCombatIntro (intro beat count = 0)

Planning window
  → EnterPlanningDuck()                              // 70% + LP

Execute
  → ExitPlanningDuck()                               // 100%

Victory → inner map
  → SetMode(Map)                                     // 40%, beat continues

Boss node
  → Pause(saveBeat) + CombatMusicController.PlayBossMusic()

Boss victory → inner map
  → CombatMusicController.StopMusic() + Resume(saveBeat)

Return Hub / Escape
  → Stop() + RunMapBgmController.StartLoop()
```

Macro map (trước Vault): vẫn `The_Locked_Vault.mp3` — không đụng Candence.

### 3.5 Timeline profile (pooled combat)

`RunTimelineProfile` (static hoặc SO nhẹ) thay `TimelineConstants` cho pooled path:

| Constant | Boss (giữ) | Run Candence |
|----------|------------|--------------|
| `TotalBeats` | 677 | **689** |
| `CombatIntroBeatCount` | 12 | **0** |
| `CombatIntroDurationSec` | ~5.90s | **0** |
| `BossRemixFirstBeatOffsetSec` | 1.161 | N/A |
| BPM | 152 | 152 |

Boss encounter (`Encounter_Boss_Despair`): vẫn dùng `TimelineConstants` + intro 12 beat.

### 3.6 Components

**Mới**

- `RunMusicSession.cs` — DDOL singleton, AudioSource, beat clock, loop, volume modes, Pause/Resume
- `RunCombatMusicBridge.cs` — facade `IMusicSync` cho `CombatController` / `BeatTimelineUIView`
- `RunTimelineProfile.cs` — constants pooled combat
- `EternalSpark_Candence_BeatMap.asset` — ScriptableObject `MusicBeatMapSO`

**Sửa**

- `CadenceMapController.HandleVaultSelected` — gọi `RunMusicSession.Begin()`
- `CadenceMapController.ReturnToHub` — gọi `RunMusicSession.Stop()`
- `RunMapController` — `SetMode(Map)` khi inner map active
- `CombatController` — nhánh pooled: skip intro, dùng bridge; boss: giữ flow cũ
- `CombatPrototypeBootstrap` — detect pooled vs boss; wire bridge hoặc `CombatMusicController`
- `RunMapBgmController` — không start loop khi `RunMusicSession.IsActive`
- Editor menu — import Candence mp3 + generate beat map asset

**Không sửa (boss isolation)**

- `CombatMusicController.PlayBossMusic()` logic
- `TimelineConstants` boss values
- Boss beat map asset

---

## 4. Data flow & sync

**Beat clock (single source of truth trong session):**

```
TimeToMusicalBeat(t) = (t - firstBeatOffsetSec) / beatSpanSec + loopBeatAccum
TotalMusicalBeat     = beatMap.TimeToMusicalBeat(source.time) + loopBeatAccum
```

**Scene load handoff:**

`RunMusicSession` DontDestroyOnLoad — `AudioSource` không destroy. Combat scene:

```csharp
var bridge = RunCombatMusicBridge.Attach(RunMusicSession.Instance);
combatController.Initialize(..., musicSync: bridge, ...);
```

`RunCombatMusicBridge` expose: `TotalMusicalBeat`, `BeatDuration`, `IsPlaying`, `EnterPlanningDuck`, `ExitPlanningDuck`, `TryGetDspTimeForMusicalBeat`, `TryGetMusicDeltaMs`, `SetPlaybackSpeedMultiplier`.

**Pooled combat start:**

- Không gọi `PlayBossMusic()`
- Không gọi `BeginIntroPlayback(CombatIntroDurationSec)`
- `_session.EndCombatIntro()` ngay (hoặc skip `IsCombatIntroActive`)
- Timeline dùng `RunTimelineProfile.TotalBeats`

**Boss resume accuracy:**

On `Pause()`: lưu `_pausedMusicalBeat = TotalMusicalBeat`.  
On `Resume()`: `source.time = beatMap.MusicalBeatToTime(_pausedMusicalBeat - loopBeatAccum)` rồi `Play()`.

---

## 5. Error handling

| Tình huống | Xử lý |
|------------|--------|
| Clip / beat map null | `Debug.LogError`; combat fallback auto-beat interval |
| `Begin()` khi session active | No-op, giữ beat hiện tại |
| Boss bootstrap không pause session | Guard: `CombatPoolRoll.IsPooledEncounterId` vs `BossDespair` |
| Resume sau boss lệch beat | Restore từ `_pausedMusicalBeat` |
| Hub return, session null | `RunMapBgmController.StartLoop()` bình thường |
| Loop jump cuối bài | Fade 50ms; `_loopBeatAccum += endBeat - startBeat` |

---

## 6. Test plan (manual QA)

| # | Kiểm tra | Kỳ vọng |
|---|----------|---------|
| 1 | Chọn Pinky Vault | Candence play ngay, beat 0 |
| 2 | Đi node map ~30s | Vol ~40%, nhịp đều |
| 3 | Battle node | Vol 100%, timeline sync ngay, **không intro scan** |
| 4 | Planning window | Duck 70% + trầm |
| 5 | Execute | Full volume |
| 6 | Thắng → map | Vol 40%, beat liên tục (không reset) |
| 7 | Boss node | BossRemix + intro 12 beat; Candence im |
| 8 | Thắng boss → map | Candence resume đúng beat pre-boss |
| 9 | Escape → Hub | Candence stop; `The_Locked_Vault` chạy |
| 10 | Run >272s | Loop mượt, beat không nhảy lùi |
| 11 | Boss scene playtest riêng | Không regression 677 beat / intro |

---

## 7. Decisions log (brainstorming)

| Câu hỏi | Lựa chọn |
|---------|----------|
| Boss node Candence | **A** — pause → BossRemix → resume beat |
| Map vs combat volume | **B** — 40% / 100%, fade 0.25s |
| Planning duck pooled | **A** — giống boss (70% + LP) |
| Kết thúc session | **A** — stop khi Return Hub; macro BGM riêng |
| Kiến trúc | **RunMusicSession DDOL** (không mở rộng CombatMusicController) |

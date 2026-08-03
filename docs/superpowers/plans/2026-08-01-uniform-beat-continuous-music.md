# Uniform Beat + Continuous Music Implementation Plan

> **For agentic workers:** Steps dùng checkbox (`- [ ]`) để theo dõi. Spec: [`docs/superpowers/specs/2026-08-01-uniform-beat-continuous-music-design.md`](../specs/2026-08-01-uniform-beat-continuous-music-design.md)

**Goal:** Beat chia đều theo BPM cố định, boss track mới, Deploy song song Planning, nhạc chạy liên tục từ lúc vào trận đến hết trận.

**Architecture:** Nhạc là đồng hồ tuyệt đối chạy không ngừng; timeline là hệ quy chiếu tương đối trượt theo qua **Beat Offset Anchor** — `_localBeat = TotalMusicalBeat - _roundStartMusicalBeat`, và `_roundStartMusicalBeat` được snap lại vào mốc bar (bội số 4 beat) mỗi lần bấm EXECUTE.

**Tech Stack:** Unity 6 · C# · uGUI · `ffmpeg` + dotnet 9 console cho phân tích BPM offline.

## Global Constraints

- Beat span cố định `60 / bpm` cho mọi beat. Không còn `beatTimesSec[]`.
- `BeatsPerBar = 4`. Resume snap vào bội số 4 beat kế tiếp.
- Nhạc boss **không bao giờ** `Pause()` / `Stop()` trong encounter (chỉ stop khi Victory/Defeat/thoát scene).
- Planning audio = duck volume `1.0 → 0.7` + lowpass `22000 → 900 Hz`, fade 0.25s.
- Boss track: `Eternal Spark - Boss Remix.mp3` (268.2935s · 48kHz · stereo). Import: `loadType: 0`, `compressionFormat: 1`, `quality: 1`, `preloadAudioData: 0`, `forceToMono: 0`, `sampleRateOverride: 48000`.
- Một nút duy nhất: **EXECUTE**.
- Không viết comment giải thích trong source code.

---

## Task 0: Ghi spec + plan

- [x] Spec `docs/superpowers/specs/2026-08-01-uniform-beat-continuous-music-design.md`
- [x] Plan `docs/superpowers/plans/2026-08-01-uniform-beat-continuous-music.md`

## Task 1: Đo BPM + offset

- [ ] `tools/beat-analyzer/` — dotnet console: spectral flux (FFT 1024 / hop 256 @ 22050Hz) → comb-filter tempo scan 70–200 BPM → phase fit → downbeat fit
- [ ] Sinh `clicktrack.wav` để nghe kiểm chứng
- [ ] Ghi `BPM` / `FIRST_BEAT_SEC` / `BEAT_SPAN_SEC` / `TOTAL_BEATS` / `TOTAL_BARS` vào spec

## Task 2: Import track mới, xóa track cũ

- [ ] Copy mp3 → `Assets/FracturedChorus/Audio/Music/EternalSpark_BossRemix.mp3` + import settings
- [ ] Xóa `EternalSpark_CadenceRemix.mp3` / `_BeatMap.asset` / 3 file CSV / `EternalSpark_PlanningSilent.mp3` (+meta)
- [ ] Sửa path trong `CombatMusicSceneSetup.cs` và `CombatMusicController.TryAssignDefaultClip()`

## Task 3: `MusicBeatMapSO` sang model uniform

- [ ] Thay thân class sang `bpm + firstBeatOffsetSec`, thêm `BeatsPerBar` / `SnapUpToBar` / `TotalBeatsForClip`
- [ ] Tạo `EternalSpark_BossRemix_BeatMap.asset`
- [ ] `TimelineConstants.TotalBeats` = giá trị Task 1; `PhaseCount` derived
- [ ] Xóa `MusicBeatMapImporter.cs`, `BeatMapTapEditorWindow.cs`

## Task 4: `CombatMusicController` chạy liên tục

- [ ] Xóa planning BGM / transition SFX / Ren Cover / `PausePlayback` / `ResumePlayback` / `EnterPlanningPhase` / `PlaySegmentTransitionMusic`
- [ ] Thêm `EnterPlanningDuck()` / `ExitPlanningDuck()` (volume + `AudioLowPassFilter`)
- [ ] Loop theo bar + `_loopBeatAccum` giữ `TotalMusicalBeat` đơn điệu tăng
- [ ] `TryGetDspTimeForMusicalBeat` / `TryGetMusicDeltaMs` trừ `_loopBeatAccum`

## Task 5: Beat Offset Anchor

- [ ] `AnchorTimelineToNextBar()` + `ResumeLeadBeats = 0.5f`
- [ ] Scan routine clamp đơn điệu `_localBeat = Max(_localBeat, ...)`
- [ ] `PrepareSegmentScanStart` / `ResumeRoundPlayback` gọi anchor
- [ ] `FinishRoundSegment` → `EnterPlanningDuck`; bỏ `PausePlayback` trong `ResetForNextPlanningSegment`
- [ ] Xóa intro-pause (`EnterPlanningPause`, `TryEnterIntroPlanningPause`, `SetPlanningPauseEnabled`, hằng `Intro*BeatIndex`)
- [ ] `RebuildLayout` bỏ `_roundStartBeatIndex` khỏi `GetSpanSec`

## Task 6: Gộp Deploy vào Planning

- [ ] `CombatSession`: `IsPlanningWindowOpen` + `SetTimelineRunning`, xóa `AllowPlayerReposition` / `LockPlayerReposition`
- [ ] `BeginPlanningRound()` pre-plan telegraph cho segment 0
- [ ] `CombatController`: xóa `StartDeployRound`, `Initialize` phát nhạc + duck ngay
- [ ] Overlay chỉ còn EXECUTE
- [ ] Xóa lớp nhạc Cover khỏi `HandleCoverChanged`

## Task 7: Mở khoá skill UI trong cửa sổ planning

- [ ] `BoardDragController` / `SkillPanelUIView` / `BeatTimelineUIView` / `CombatPrototypeBootstrap` dùng `IsPlanningWindowOpen`
- [ ] Hex floor + formation hint hiện suốt mọi cửa sổ planning

## Task 8: Wire lại 2 scene

- [ ] `CombatMusicSceneSetup.WireCurrentScene()` gán clip / beatmap / loop bar / `autoBeatInterval`
- [ ] Chạy menu trên `CombatPrototype.unity` + `CombatTutorial.unity`

## Task 9: Tutorial

- [ ] `TutorialDirector` copy Deploy → Execute, gộp `combat_deploy` + `combat_plan`
- [ ] Sync `docs/tutorial/TUTORIAL_COPY.md`

## Task 10: Docs + QA

- [ ] `docs/combat/COMBAT_MECHANICS.md` §1 + §14 + changelog
- [ ] `docs/PROJECT_STATUS.md`
- [ ] `docs/combat/UNIFORM_BEAT_QA.md` checklist Play Mode

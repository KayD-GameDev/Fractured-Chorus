# Run Candence Music Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Candence track chạy liền mạch từ Pinky Vault → inner map (40%) → Battle/Elite combat (100%, beat sync, no intro), tách biệt boss BossRemix.

**Architecture:** `RunMusicSession` DontDestroyOnLoad giữ AudioSource + beat clock; pooled combat dùng `RunCombatMusicBridge` implement `ICombatMusicSync`; boss giữ `CombatMusicController`. Timeline runtime profile switch sang 689 beat / intro 0 cho pooled.

**Tech Stack:** Unity 6 · C# · uGUI · `MusicBeatMapSO` · `Tools/beat-analyzer` (dotnet 9) · NUnit EditMode tests.

## Global Constraints

- Clip: `Assets/FracturedChorus/Audio/Music/EternalSpark_Candence.mp3` (copy từ `C:\Users\Asus\Downloads\Eternal Spark - Candence.mp3`).
- Beat map: BPM **152.0000**, `firstBeatOffsetSec = **0**`, `TOTAL_BEATS = **689**`, `DURATION_SEC = 271.8935`.
- Map volume **0.40** / Combat **1.00** / Planning duck **0.70** + low-pass **900 Hz**; fade **0.25s**.
- Pooled combat: **no intro** (`CombatIntroBeatCount = 0`).
- Boss node: pause Candence → BossRemix + intro 12 beat → resume Candence beat đã lưu.
- Return Hub: stop Candence; macro map dùng `The_Locked_Vault.mp3`.
- Boss path (`Encounter_Boss_Despair`): không regression — vẫn 677 beat, intro 12, `CombatMusicController` only.
- Không viết comment giải thích trong source code mới.

## File Structure

| File | Trách nhiệm |
|------|-------------|
| `Assets/FracturedChorus/Audio/RunMusicSession.cs` | DDOL singleton, playback, loop, volume modes, pause/resume |
| `Assets/FracturedChorus/Audio/RunCombatMusicBridge.cs` | MonoBehaviour `ICombatMusicSync` → delegate session |
| `Assets/FracturedChorus/Audio/ICombatMusicSync.cs` | Interface chung cho timeline + combat controller |
| `Assets/FracturedChorus/Combat/Timeline/CombatTimelineProfile.cs` | Runtime TotalBeats / intro cho boss vs run |
| `Assets/FracturedChorus/Audio/Music/EternalSpark_Candence_BeatMap.asset` | Beat map SO |
| `Assets/FracturedChorus/Editor/RunMusicSceneSetupEditor.cs` | Import mp3 + tạo beat map asset |
| `Assets/FracturedChorus/Editor/RunMusicSessionTests.cs` | NUnit: beat math + pause/resume time |
| Sửa: `CadenceMapController`, `RunMapBgmController`, `RunMapHubBridge`, `CombatPrototypeBootstrap`, `CombatController`, `BeatTimelineUIView`, `CombatMusicController`, `TimelineConstants`/`BeatTimelineEngine` | Wiring |

---

### Task 1: Import Candence audio + beat map asset

**Files:**
- Create: `Assets/FracturedChorus/Audio/Music/EternalSpark_Candence.mp3` (+ `.meta`)
- Create: `Assets/FracturedChorus/Audio/Music/EternalSpark_Candence_BeatMap.asset` (+ `.meta`)
- Create: `Assets/FracturedChorus/Editor/RunMusicSceneSetupEditor.cs`

**Interfaces:**
- Produces: `MusicBeatMapSO` asset wired với clip Candence, BPM 152, offset 0.

- [ ] **Step 1: Copy source mp3**

```powershell
Copy-Item "C:\Users\Asus\Downloads\Eternal Spark - Candence.mp3" `
  "D:\Fractured-Chorus1\Assets\FracturedChorus\Audio\Music\EternalSpark_Candence.mp3" -Force
```

- [ ] **Step 2: Tạo editor menu import + beat map**

```csharp
#if UNITY_EDITOR
using FracturedChorus.Audio;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class RunMusicSceneSetupEditor
    {
        private const string SourcePath = @"C:\Users\Asus\Downloads\Eternal Spark - Candence.mp3";
        private const string ClipPath = "Assets/FracturedChorus/Audio/Music/EternalSpark_Candence.mp3";
        private const string BeatMapPath = "Assets/FracturedChorus/Audio/Music/EternalSpark_Candence_BeatMap.asset";
        private const float CandenceBpm = 152f;

        [MenuItem("Fractured Chorus/Import Run Candence Music")]
        public static void ImportRunCandenceMusic()
        {
            if (System.IO.File.Exists(SourcePath))
            {
                System.IO.File.Copy(SourcePath, System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Application.dataPath)!,
                    ClipPath.Replace('/', System.IO.Path.DirectorySeparatorChar)), true);
            }

            AssetDatabase.Refresh();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
            if (clip == null)
            {
                Debug.LogError($"[RunMusic] Missing clip at {ClipPath}");
                return;
            }

            var beatMap = AssetDatabase.LoadAssetAtPath<MusicBeatMapSO>(BeatMapPath);
            if (beatMap == null)
            {
                beatMap = ScriptableObject.CreateInstance<MusicBeatMapSO>();
                AssetDatabase.CreateAsset(beatMap, BeatMapPath);
            }

            beatMap.EditorSetData(clip, CandenceBpm, 0f);
            EditorUtility.SetDirty(beatMap);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RunMusic] Candence imported: {clip.length:F1}s, {beatMap.TotalBeatsForClip()} beats @ {CandenceBpm} BPM.");
        }
    }
}
#endif
```

- [ ] **Step 3: Chạy menu trong Unity**

Menu: `Fractured Chorus → Import Run Candence Music`

Expected: Console log `689 beats`; asset `EternalSpark_Candence_BeatMap.asset` có `bpm=152`, `firstBeatOffsetSec=0`.

- [ ] **Step 4: Commit**

```bash
git add Assets/FracturedChorus/Audio/Music/EternalSpark_Candence* Assets/FracturedChorus/Editor/RunMusicSceneSetupEditor.cs
git commit -m "Import Candence run music and beat map asset."
```

---

### Task 2: Combat timeline profile (boss vs run)

**Files:**
- Create: `Assets/FracturedChorus/Combat/Timeline/CombatTimelineProfile.cs`
- Modify: `Assets/FracturedChorus/Combat/Timeline/TimelineConstants.cs`
- Modify: `Assets/FracturedChorus/Combat/Timeline/BeatTimelineEngine.cs`
- Modify: `Assets/FracturedChorus/UI/BeatTimelineUIView.cs` (property `TotalBeats`)
- Test: `Assets/FracturedChorus/Editor/RunMusicSessionTests.cs` (profile section)

**Interfaces:**
- Produces: `CombatTimelineProfile.ApplyBoss()`, `ApplyRun()`, `TotalBeats`, `CombatIntroDurationSec`

- [ ] **Step 1: Write failing test**

```csharp
using FracturedChorus.Combat.Timeline;
using NUnit.Framework;

namespace FracturedChorus.Tests
{
    public class CombatTimelineProfileTests
    {
        [Test]
        public void ApplyRun_Sets689BeatsAndZeroIntro()
        {
            CombatTimelineProfile.ApplyRun();
            Assert.AreEqual(689, CombatTimelineProfile.TotalBeats);
            Assert.AreEqual(0, CombatTimelineProfile.CombatIntroBeatCount);
            Assert.AreEqual(0f, CombatTimelineProfile.CombatIntroDurationSec);
        }

        [Test]
        public void ApplyBoss_Restores677BeatsAndIntro()
        {
            CombatTimelineProfile.ApplyRun();
            CombatTimelineProfile.ApplyBoss();
            Assert.AreEqual(677, CombatTimelineProfile.TotalBeats);
            Assert.AreEqual(12, CombatTimelineProfile.CombatIntroBeatCount);
            Assert.Greater(CombatTimelineProfile.CombatIntroDurationSec, 5f);
        }
    }
}
```

- [ ] **Step 2: Run test — expect FAIL**

Unity: `Window → General → Test Runner → EditMode → Run All`

Expected: `CombatTimelineProfile` type not found.

- [ ] **Step 3: Implement profile**

```csharp
namespace FracturedChorus.Combat.Timeline
{
    public static class CombatTimelineProfile
    {
        public const int BossTotalBeats = 677;
        public const int RunTotalBeats = 689;
        public const int BossIntroBeatCount = 12;
        public const int RunIntroBeatCount = 0;

        public static int TotalBeats { get; private set; } = BossTotalBeats;
        public static int CombatIntroBeatCount { get; private set; } = BossIntroBeatCount;
        public static float CombatIntroDurationSec { get; private set; } =
            TimelineConstants.BossRemixFirstBeatOffsetSec
            + BossIntroBeatCount * (60f / TimelineConstants.BossRemixBpm);

        public static void ApplyBoss()
        {
            TotalBeats = BossTotalBeats;
            CombatIntroBeatCount = BossIntroBeatCount;
            CombatIntroDurationSec = TimelineConstants.BossRemixFirstBeatOffsetSec
                + BossIntroBeatCount * (60f / TimelineConstants.BossRemixBpm);
        }

        public static void ApplyRun()
        {
            TotalBeats = RunTotalBeats;
            CombatIntroBeatCount = RunIntroBeatCount;
            CombatIntroDurationSec = 0f;
        }
    }
}
```

- [ ] **Step 4: Point runtime reads at profile**

Trong `BeatTimelineUIView.cs`, thay:

```csharp
private static int TotalBeats => TimelineConstants.TotalBeats;
```

bằng:

```csharp
private static int TotalBeats => CombatTimelineProfile.TotalBeats;
```

Trong `BeatTimelineEngine.cs`, thay `public const int BeatCount = TimelineConstants.TotalBeats` bằng:

```csharp
public static int BeatCount => CombatTimelineProfile.TotalBeats;
```

Trong `CombatController.cs`, thay mọi `TimelineConstants.CombatIntroDurationSec` → `CombatTimelineProfile.CombatIntroDurationSec`, và `TimelineConstants.TotalBeats` → `CombatTimelineProfile.TotalBeats`.

Grep project: `TimelineConstants.TotalBeats` trong runtime combat paths → `CombatTimelineProfile.TotalBeats` (giữ const gốc 677 trong `TimelineConstants` cho boss reference / docs).

- [ ] **Step 5: Run tests — expect PASS**

- [ ] **Step 6: Commit**

```bash
git add Assets/FracturedChorus/Combat/Timeline/CombatTimelineProfile.cs Assets/FracturedChorus/Editor/RunMusicSessionTests.cs
git add Assets/FracturedChorus/Combat/Timeline/BeatTimelineEngine.cs Assets/FracturedChorus/UI/BeatTimelineUIView.cs Assets/FracturedChorus/Combat/Core/CombatController.cs
git commit -m "Add combat timeline profile for run vs boss beat counts."
```

---

### Task 3: RunMusicSession core (DDOL)

**Files:**
- Create: `Assets/FracturedChorus/Audio/RunMusicSession.cs`
- Test: `Assets/FracturedChorus/Editor/RunMusicSessionTests.cs` (beat time section)

**Interfaces:**
- Produces: `RunMusicSession.Instance`, `Begin()`, `Stop()`, `SetMode(RunMusicMode)`, `Pause()`, `Resume()`, `TotalMusicalBeat`, `IsActive`

- [ ] **Step 1: Write failing test for beat time conversion**

```csharp
[Test]
public void MusicalBeatToTime_ZeroOffset_BeatZeroAtZero()
{
    var map = ScriptableObject.CreateInstance<MusicBeatMapSO>();
    map.EditorSetData(null, 152f, 0f);
    Assert.AreEqual(0f, map.MusicalBeatToTime(0f), 0.0001f);
    Assert.AreEqual(0.394737f, map.BeatSpanSec, 0.001f);
    Object.DestroyImmediate(map);
}
```

- [ ] **Step 2: Run test — PASS (uses existing MusicBeatMapSO) or extend session test after Step 3**

- [ ] **Step 3: Implement RunMusicSession**

```csharp
using System.Collections;
using UnityEngine;

namespace FracturedChorus.Audio
{
    public enum RunMusicMode
    {
        Map,
        Combat,
        Planning,
        BossPaused
    }

    public sealed class RunMusicSession : MonoBehaviour
    {
        public static RunMusicSession Instance { get; private set; }

        [SerializeField] private AudioClip candenceClip;
        [SerializeField] private MusicBeatMapSO beatMap;
        [SerializeField] private float mapVolume = 0.4f;
        [SerializeField] private float combatVolume = 1f;
        [SerializeField] private float duckVolume = 0.7f;
        [SerializeField] private float duckCutoffHz = 900f;
        [SerializeField] private float fadeSec = 0.25f;
        [SerializeField] private float loopFadeSec = 0.05f;

        private AudioSource _source;
        private AudioLowPassFilter _lowPass;
        private float _totalMusicalBeat;
        private float _loopBeatAccum;
        private float _pausedMusicalBeat;
        private bool _playing;
        private bool _pausedForBoss;
        private RunMusicMode _mode = RunMusicMode.Map;
        private Coroutine _fadeRoutine;
        private Coroutine _loopRoutine;

        public bool IsActive => _playing;
        public MusicBeatMapSO BeatMap => beatMap;
        public float TotalMusicalBeat => _totalMusicalBeat;
        public float BeatDuration => beatMap != null && beatMap.HasData ? beatMap.BeatSpanSec : 60f / 152f;
        public bool IsPlaying => _playing && _source != null && _source.isPlaying;
        public AudioSource Source => _source;

        public static RunMusicSession Ensure()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var go = new GameObject(nameof(RunMusicSession));
            DontDestroyOnLoad(go);
            return go.AddComponent<RunMusicSession>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureAudio();
        }

        public void Begin(AudioClip clip = null, MusicBeatMapSO map = null)
        {
            if (_playing && !_pausedForBoss)
            {
                return;
            }

            if (clip != null)
            {
                candenceClip = clip;
            }

            if (map != null)
            {
                beatMap = map;
            }

            TryLoadDefaults();
            if (candenceClip == null || beatMap == null)
            {
                Debug.LogError("[RunMusic] Candence clip or beat map missing.");
                return;
            }

            EnsureAudio();
            _playing = true;
            _pausedForBoss = false;
            _loopBeatAccum = 0f;
            _totalMusicalBeat = 0f;
            _source.clip = candenceClip;
            _source.time = 0f;
            _source.loop = false;
            _source.Play();
            SetMode(RunMusicMode.Map, immediate: true);
        }

        public void Stop()
        {
            _playing = false;
            _pausedForBoss = false;
            StopAllCoroutines();
            if (_source != null)
            {
                _source.Stop();
            }
        }

        public void SetMode(RunMusicMode mode, bool immediate = false)
        {
            _mode = mode;
            if (!_playing || _source == null)
            {
                return;
            }

            if (mode == RunMusicMode.BossPaused)
            {
                _source.Pause();
                return;
            }

            if (_source.time == 0f && !_source.isPlaying && _pausedForBoss)
            {
                _source.UnPause();
            }
            else if (!_source.isPlaying)
            {
                _source.UnPause();
            }

            var targetVol = mode switch
            {
                RunMusicMode.Map => mapVolume,
                RunMusicMode.Combat => combatVolume,
                RunMusicMode.Planning => duckVolume,
                _ => mapVolume
            };
            var targetCutoff = mode == RunMusicMode.Planning ? duckCutoffHz : 22000f;
            StartFade(targetVol, targetCutoff, immediate);
        }

        public void PauseForBoss()
        {
            if (!_playing)
            {
                return;
            }

            SyncBeat();
            _pausedMusicalBeat = _totalMusicalBeat;
            _pausedForBoss = true;
            SetMode(RunMusicMode.BossPaused, immediate: true);
        }

        public void ResumeFromBoss()
        {
            if (!_playing || beatMap == null)
            {
                return;
            }

            _pausedForBoss = false;
            var audioTime = beatMap.MusicalBeatToTime(_pausedMusicalBeat - _loopBeatAccum);
            _source.time = Mathf.Clamp(audioTime, 0f, Mathf.Max(0f, candenceClip.length - 0.01f));
            _source.UnPause();
            SetMode(RunMusicMode.Map, immediate: false);
        }

        public void EnterPlanningDuck() => SetMode(RunMusicMode.Planning);
        public void ExitPlanningDuck() => SetMode(RunMusicMode.Combat);

        public bool TryGetDspTimeForMusicalBeat(float musicalBeat, out double dspTime)
        {
            dspTime = AudioSettings.dspTime;
            if (_source == null || beatMap == null)
            {
                return false;
            }

            var targetAudioTime = beatMap.MusicalBeatToTime(musicalBeat - _loopBeatAccum);
            dspTime = AudioSettings.dspTime + (targetAudioTime - _source.time);
            return true;
        }

        public bool TryGetMusicDeltaMs(float musicalBeat, out float deltaMs)
        {
            deltaMs = 0f;
            if (_source == null || beatMap == null)
            {
                return false;
            }

            var targetAudioTime = beatMap.MusicalBeatToTime(musicalBeat - _loopBeatAccum);
            deltaMs = (_source.time - targetAudioTime) * 1000f;
            return true;
        }

        private void Update()
        {
            if (!_playing || _source == null || !_source.isPlaying || _pausedForBoss)
            {
                return;
            }

            SyncBeat();
            TryLoop();
        }

        private void SyncBeat()
        {
            if (beatMap == null)
            {
                return;
            }

            _totalMusicalBeat = beatMap.TimeToMusicalBeat(_source.time) + _loopBeatAccum;
        }

        private void TryLoadDefaults()
        {
#if UNITY_EDITOR
            candenceClip ??= UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/FracturedChorus/Audio/Music/EternalSpark_Candence.mp3");
            beatMap ??= UnityEditor.AssetDatabase.LoadAssetAtPath<MusicBeatMapSO>(
                "Assets/FracturedChorus/Audio/Music/EternalSpark_Candence_BeatMap.asset");
#endif
            beatMap ??= Resources.Load<MusicBeatMapSO>("Music/EternalSpark_Candence_BeatMap");
        }

        private void EnsureAudio()
        {
            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
                _source.playOnAwake = false;
                _source.loop = false;
                _source.spatialBlend = 0f;
            }

            _lowPass ??= gameObject.GetComponent<AudioLowPassFilter>()
                         ?? gameObject.AddComponent<AudioLowPassFilter>();
            _lowPass.cutoffFrequency = 22000f;
        }

        private void StartFade(float targetVolume, float targetCutoff, bool immediate)
        {
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
            }

            _fadeRoutine = StartCoroutine(FadeRoutine(targetVolume, targetCutoff, immediate));
        }

        private IEnumerator FadeRoutine(float targetVolume, float targetCutoff, bool immediate)
        {
            var startVol = _source.volume;
            var startCutoff = _lowPass.cutoffFrequency;
            if (immediate)
            {
                _source.volume = targetVolume;
                _lowPass.cutoffFrequency = targetCutoff;
                yield break;
            }

            for (var t = 0f; t < fadeSec; t += Time.unscaledDeltaTime)
            {
                var a = Mathf.Clamp01(t / fadeSec);
                _source.volume = Mathf.Lerp(startVol, targetVolume, a);
                _lowPass.cutoffFrequency = Mathf.Lerp(startCutoff, targetCutoff, a);
                yield return null;
            }

            _source.volume = targetVolume;
            _lowPass.cutoffFrequency = targetCutoff;
        }

        private void TryLoop()
        {
            if (beatMap == null || candenceClip == null || _loopRoutine != null)
            {
                return;
            }

            var endBar = (beatMap.TotalBeatsForClip() - 1) / MusicBeatMapSO.BeatsPerBar;
            if (endBar <= 0)
            {
                return;
            }

            var endBeat = endBar * MusicBeatMapSO.BeatsPerBar;
            if (_source.time < beatMap.MusicalBeatToTime(endBeat) - 0.05f)
            {
                return;
            }

            var startBeat = 0;
            _loopBeatAccum += endBeat - startBeat;
            _loopRoutine = StartCoroutine(LoopJumpRoutine(beatMap.MusicalBeatToTime(startBeat)));
        }

        private IEnumerator LoopJumpRoutine(float targetTime)
        {
            var half = Mathf.Max(0.005f, loopFadeSec * 0.5f);
            for (var t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                _source.volume *= 1f - Mathf.Clamp01(t / half);
                yield return null;
            }

            _source.time = targetTime;
            for (var t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                _source.volume = Mathf.Lerp(0f, _mode == RunMusicMode.Map ? mapVolume : combatVolume, t / half);
                yield return null;
            }

            _loopRoutine = null;
        }
    }
}
```

- [ ] **Step 4: Play Mode smoke test**

Enter Play on empty scene, gọi `RunMusicSession.Ensure().Begin()` qua temporary debug script hoặc Inspector test.

Expected: Audio plays, `TotalMusicalBeat` increases.

- [ ] **Step 5: Commit**

```bash
git add Assets/FracturedChorus/Audio/RunMusicSession.cs
git commit -m "Add DontDestroyOnLoad RunMusicSession for Candence playback."
```

---

### Task 4: ICombatMusicSync + RunCombatMusicBridge

**Files:**
- Create: `Assets/FracturedChorus/Audio/ICombatMusicSync.cs`
- Create: `Assets/FracturedChorus/Audio/RunCombatMusicBridge.cs`
- Modify: `Assets/FracturedChorus/Audio/CombatMusicController.cs` — implement interface

**Interfaces:**
- Produces: `ICombatMusicSync` implemented by `CombatMusicController` and `RunCombatMusicBridge`
- Consumes: `RunMusicSession.Instance`

- [ ] **Step 1: Define interface**

```csharp
using UnityEngine;

namespace FracturedChorus.Audio
{
    public interface ICombatMusicSync
    {
        MusicBeatMapSO BeatMap { get; }
        float TotalMusicalBeat { get; }
        float BeatDuration { get; }
        bool IsPlaying { get; }
        float SourceTimeSec { get; }
        AudioSource Source { get; }
        bool UsesRunSession { get; }
        void PlayBossMusic();
        void StopMusic();
        void EnterPlanningDuck();
        void ExitPlanningDuck();
        void SetPlaybackSpeedMultiplier(float multiplier);
        bool TryGetDspTimeForMusicalBeat(float musicalBeat, out double dspTime);
        bool TryGetMusicDeltaMs(float musicalBeat, out float deltaMs);
    }
}
```

- [ ] **Step 2: CombatMusicController implements interface**

Thêm `: ICombatMusicSync` và property:

```csharp
public bool UsesRunSession => false;
```

Các method public hiện có giữ nguyên signature.

- [ ] **Step 3: Implement bridge**

```csharp
using UnityEngine;

namespace FracturedChorus.Audio
{
    public sealed class RunCombatMusicBridge : MonoBehaviour, ICombatMusicSync
    {
        public bool UsesRunSession => true;
        public MusicBeatMapSO BeatMap => Session?.BeatMap;
        public float TotalMusicalBeat => Session != null ? Session.TotalMusicalBeat : 0f;
        public float BeatDuration => Session != null ? Session.BeatDuration : 60f / 152f;
        public bool IsPlaying => Session != null && Session.IsPlaying;
        public float SourceTimeSec => Session?.Source != null ? Session.Source.time : 0f;
        public AudioSource Source => Session?.Source;

        private RunMusicSession Session => RunMusicSession.Instance;

        public static RunCombatMusicBridge Attach(Transform parent)
        {
            var existing = parent.GetComponentInChildren<RunCombatMusicBridge>(true);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(nameof(RunCombatMusicBridge));
            go.transform.SetParent(parent, false);
            return go.AddComponent<RunCombatMusicBridge>();
        }

        public void PlayBossMusic() { }

        public void StopMusic() { }

        public void EnterPlanningDuck() => Session?.EnterPlanningDuck();

        public void ExitPlanningDuck() => Session?.ExitPlanningDuck();

        public void SetPlaybackSpeedMultiplier(float multiplier)
        {
            if (Session?.Source != null)
            {
                Session.Source.pitch = Mathf.Max(0.001f, multiplier);
            }
        }

        public bool TryGetDspTimeForMusicalBeat(float musicalBeat, out double dspTime)
        {
            if (Session != null)
            {
                return Session.TryGetDspTimeForMusicalBeat(musicalBeat, out dspTime);
            }

            dspTime = AudioSettings.dspTime;
            return false;
        }

        public bool TryGetMusicDeltaMs(float musicalBeat, out float deltaMs)
        {
            if (Session != null)
            {
                return Session.TryGetMusicDeltaMs(musicalBeat, out deltaMs);
            }

            deltaMs = 0f;
            return false;
        }
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add Assets/FracturedChorus/Audio/ICombatMusicSync.cs Assets/FracturedChorus/Audio/RunCombatMusicBridge.cs Assets/FracturedChorus/Audio/CombatMusicController.cs
git commit -m "Add run combat music bridge implementing ICombatMusicSync."
```

---

### Task 5: Refactor timeline + CombatController to ICombatMusicSync

**Files:**
- Modify: `Assets/FracturedChorus/UI/BeatTimelineUIView.cs`
- Modify: `Assets/FracturedChorus/Combat/Core/CombatController.cs`

**Interfaces:**
- Consumes: `ICombatMusicSync`
- Produces: pooled + boss paths compile; timeline binds either controller type

- [ ] **Step 1: BeatTimelineUIView — đổi field + Bind signature**

Thay `CombatMusicController musicController` runtime reference bằng `ICombatMusicSync _musicSync` (giữ SerializeField boss reference cho scene wiring fallback):

```csharp
[SerializeField] private CombatMusicController musicController;
private ICombatMusicSync _musicSync;

public void Bind(..., CombatMusicController music, ...)
{
    musicController = music;
    _musicSync = music;
    ...
}
```

Thêm overload hoặc parameter:

```csharp
public void BindMusicSync(ICombatMusicSync sync)
{
    _musicSync = sync;
    if (sync is CombatMusicController controller)
    {
        musicController = controller;
    }
}
```

Replace internal `musicController?.` calls với `_musicSync ?? musicController` helper:

```csharp
private ICombatMusicSync Music => _musicSync ?? musicController;
```

- [ ] **Step 2: CombatController — đổi field + Initialize**

```csharp
private ICombatMusicSync _musicSync;
private bool _usesRunMusic;

public void Initialize(..., CombatMusicController music = null, ...)
{
    _musicSync = music;
    _usesRunMusic = music != null && music.UsesRunSession;
    ...
    timelineView?.BindMusicSync(_musicSync);
    StartCombatIntro();
}
```

Thêm overload Initialize nhận `ICombatMusicSync` trực tiếp (bootstrap gọi overload này).

- [ ] **Step 3: StartCombatIntro — skip intro cho run**

```csharp
private void StartCombatIntro()
{
    if (_usesRunMusic)
    {
        RunMusicSession.Instance?.SetMode(RunMusicMode.Combat);
        OnCombatIntroComplete();
        return;
    }

    if (_musicSync != null && !_musicSync.IsPlaying)
    {
        _musicSync.PlayBossMusic();
    }
    ...
}
```

- [ ] **Step 4: HandleEncounterEnded — không stop run session**

```csharp
private void HandleEncounterEnded()
{
    ...
    if (_usesRunMusic)
    {
        RunMusicSession.Instance?.SetMode(RunMusicMode.Map);
    }
    else
    {
        _musicSync?.StopMusic();
    }
    ...
}
```

- [ ] **Step 5: Playtest boss scene**

Open `CombatPrototype.unity`, Play without handoff.

Expected: BossRemix + intro 12 beat unchanged.

- [ ] **Step 6: Commit**

```bash
git add Assets/FracturedChorus/UI/BeatTimelineUIView.cs Assets/FracturedChorus/Combat/Core/CombatController.cs
git commit -m "Route combat music through ICombatMusicSync with run intro skip."
```

---

### Task 6: Wire vault entry, map mode, hub stop

**Files:**
- Modify: `Assets/FracturedChorus/RunMap/CadenceMapController.cs`
- Modify: `Assets/FracturedChorus/RunMap/RunMapBgmController.cs`
- Modify: `Assets/FracturedChorus/RunMap/RunMapHubBridge.cs`
- Modify: `Assets/FracturedChorus/RunMap/RunMapController.cs`

- [ ] **Step 1: Begin session on Pinky Vault**

Trong `HandleVaultSelected` sau `Progress.BeginPinkyRun(seed)`:

```csharp
var clip = Resources.Load<AudioClip>("Music/EternalSpark_Candence");
var map = Resources.Load<MusicBeatMapSO>("Music/EternalSpark_Candence_BeatMap");
RunMusicSession.Ensure().Begin(clip, map);
```

Copy beat map + clip vào `Assets/FracturedChorus/Resources/Music/` HOẶC load qua AssetDatabase path trong editor và serialized refs trên session prefab. Plan: duplicate asset reference vào Resources folder:

```
Assets/FracturedChorus/Resources/Music/EternalSpark_Candence_BeatMap.asset → symlink or copy
```

Editor step in Task 1: also copy beat map to Resources.

- [ ] **Step 2: Stop session on hub return**

Trong `RunMapHubBridge.ReturnToCampusHub()` trước `LoadByName`:

```csharp
if (RunMusicSession.Instance != null)
{
    RunMusicSession.Instance.Stop();
}
```

- [ ] **Step 3: RunMapBgmController — defer khi session active**

```csharp
private void Start()
{
    if (RunMusicSession.Instance != null && RunMusicSession.Instance.IsActive)
    {
        return;
    }

    StartLoop();
}
```

- [ ] **Step 4: Inner map SetMode Map**

Trong `CadenceMapController.EnterInnerSectorDeferred` sau `innerController.Initialize`:

```csharp
RunMusicSession.Instance?.SetMode(RunMusicMode.Map);
```

Trong `RunMapController.ApplyCombatReturnHandoff` sau victory handoff:

```csharp
RunMusicSession.Instance?.SetMode(RunMusicMode.Map);
```

- [ ] **Step 5: Manual test vault → map**

Play `RunMapPrototype`, chọn Pinky Vault.

Expected: Candence plays ~40% volume; `The_Locked_Vault` không play.

- [ ] **Step 6: Commit**

```bash
git add Assets/FracturedChorus/RunMap/
git add Assets/FracturedChorus/Resources/Music/
git commit -m "Wire Candence session to vault entry and map hub lifecycle."
```

---

### Task 7: Combat bootstrap pooled vs boss music

**Files:**
- Modify: `Assets/FracturedChorus/Combat/Bootstrap/CombatPrototypeBootstrap.cs`

**Interfaces:**
- Consumes: `CombatEncounterHandoff.EncounterId`, `CombatPoolRoll.IsPooledEncounterId`
- Produces: bootstrap wires `RunCombatMusicBridge` + `CombatTimelineProfile.ApplyRun()` for pooled; boss path unchanged

- [ ] **Step 1: Detect encounter profile early in Awake**

Sau khi resolve `encounterId`:

```csharp
var isPooledEncounter = handoffEncounter != null
    && CombatPoolRoll.IsPooledEncounterId(encounterId);
var isBossEncounter = encounterId == EncounterCatalog.BossDespair;

if (isPooledEncounter)
{
    CombatTimelineProfile.ApplyRun();
}
else
{
    CombatTimelineProfile.ApplyBoss();
}
```

- [ ] **Step 2: Wire music sync**

```csharp
ICombatMusicSync musicSync;
if (isPooledEncounter && RunMusicSession.Instance != null && RunMusicSession.Instance.IsActive)
{
    RunMusicSession.Instance.SetMode(RunMusicMode.Combat);
    var bridge = RunCombatMusicBridge.Attach(transform);
    musicSync = bridge;
}
else
{
    EnsureMusicController();
    if (isBossEncounter && RunMusicSession.Instance != null && RunMusicSession.Instance.IsActive)
    {
        RunMusicSession.Instance.PauseForBoss();
    }

    musicSync = musicController;
}

combatController.InitializeWithMusic(_session, _timeline, timelineView, skillPanelView, musicSync, ...);
```

Thêm method `InitializeWithMusic` trên `CombatController` (wrapper gọi logic Initialize).

- [ ] **Step 3: Boss resume on return**

Trong `CombatController.OnResultContinue` trước scene load:

```csharp
if (_usesRunMusic)
{
    RunMusicSession.Instance?.SetMode(RunMusicMode.Map);
}
else if (RunMusicSession.Instance != null && RunMusicSession.Instance.IsActive)
{
    RunMusicSession.Instance.ResumeFromBoss();
}
```

- [ ] **Step 4: WarnIfBeatMap — chỉ boss**

Skip warning khi `CombatTimelineProfile.TotalBeats == 689`.

- [ ] **Step 5: Playtest pooled battle**

RunMap → Battle node.

Expected: No intro scan; timeline 689 beats; music continuous from map beat.

- [ ] **Step 6: Playtest boss node mid-run**

Expected: Candence pauses; BossRemix intro; victory → Candence resumes.

- [ ] **Step 7: Commit**

```bash
git add Assets/FracturedChorus/Combat/Bootstrap/CombatPrototypeBootstrap.cs Assets/FracturedChorus/Combat/Core/CombatController.cs
git commit -m "Bootstrap pooled combat on run music session and boss pause-resume."
```

---

### Task 8: QA + docs sync

**Files:**
- Create: `docs/combat/RUN_CANDENCE_MUSIC_QA.md`
- Modify: `docs/superpowers/specs/2026-08-12-run-candence-music-design.md` — link plan

- [ ] **Step 1: Write QA checklist** (copy §6 spec thành bảng ✅/❌)

- [ ] **Step 2: Run full manual QA** theo spec §6

- [ ] **Step 3: Commit**

```bash
git add docs/combat/RUN_CANDENCE_MUSIC_QA.md docs/superpowers/specs/2026-08-12-run-candence-music-design.md
git commit -m "Add QA checklist for run Candence music flow."
```

---

## Self-Review

| Spec requirement | Task |
|------------------|------|
| DDOL session Vault → map → combat | Task 3, 6, 7 |
| Map 40% / Combat 100% fade 0.25s | Task 3 SetMode |
| No intro pooled | Task 5 StartCombatIntro |
| Planning duck 70% | Task 3, 4 bridge |
| Boss pause/resume | Task 7 |
| Hub stop + macro BGM | Task 6 |
| 689 beats @ 152 offset 0 | Task 1, 2 |
| Boss isolation | Task 7 else branch |
| Loop >272s | Task 3 TryLoop |

No TBD placeholders. Types consistent: `ICombatMusicSync`, `RunMusicMode`, `CombatTimelineProfile`.

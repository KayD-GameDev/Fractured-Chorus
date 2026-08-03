# Uniform Beat + Continuous Music — Design Spec

> **Ngày:** 2026-08-01
> **Trạng thái:** Approved
> **Plan:** [`docs/superpowers/plans/2026-08-01-uniform-beat-continuous-music.md`](../plans/2026-08-01-uniform-beat-continuous-music.md)
> **SoT combat:** [`docs/combat/COMBAT_MECHANICS.md`](../../combat/COMBAT_MECHANICS.md)

---

## 1. Vấn đề

Combat hiện tại có 4 điểm gây khó chịu:

| # | Hiện trạng | Vấn đề |
|---|-----------|--------|
| 1 | Beat dài ngắn khác nhau (`MusicBeatMapSO.beatTimesSec[]` 619 mốc từ CSV) | Ô beat trên timeline rộng hẹp lộn xộn, khó đọc, khó cân bằng footprint skill |
| 2 | Boss track = `EternalSpark_CadenceRemix.mp3` | Cần đổi sang `Eternal Spark - Boss Remix` |
| 3 | Deploy là phase riêng trước Planning | Nhân vật bị bó buộc một vị trí suốt cả round |
| 4 | Nhạc `Pause()` mỗi lần vào planning, thay bằng planning BGM + transition SFX | Nhạc đứt liên tục, mất hứng |

---

## 2. Quyết định thiết kế

### 2.1 Beat chia đều

Beat span cố định `60 / bpm` cho mọi beat. `MusicBeatMapSO` bỏ mảng timestamp, chuyển sang model `bpm + firstBeatOffsetSec`:

```
TimeToMusicalBeat(t) = (t - firstBeatOffsetSec) / (60 / bpm)
MusicalBeatToTime(b) = firstBeatOffsetSec + b * (60 / bpm)
```

Lợi ích: O(1) arithmetic thay vì binary search, mọi ô beat rộng bằng nhau, không cần pipeline CSV. Biến thiên duy nhất còn lại là **tốc độ chạy** (`CombatMusicController.SetPlaybackSpeedMultiplier` → `AudioSource.pitch`), đã có sẵn.

Hệ quả: xóa `MusicBeatMapImporter.cs`, `BeatMapTapEditorWindow.cs`, toàn bộ file CSV.

### 2.2 Beat Offset Anchor — nhạc chạy liên tục

**Nguyên lý:** nhạc là đồng hồ tuyệt đối chạy không ngừng; timeline là hệ quy chiếu **tương đối** trượt theo.

Cơ chế đã có sẵn một nửa trong code — `BeatTimelineUIView.cs:1616`:

```csharp
_localBeat = musicController.TotalMusicalBeat - _roundStartMusicalBeat;
```

Thay đổi:

1. Nhạc **không bao giờ** `Pause()` trong encounter.
2. Vào planning: scan đóng băng tại `_localBeat`, nhạc vẫn chạy → `TotalMusicalBeat` tiếp tục tăng.
3. Bấm EXECUTE: `_roundStartMusicalBeat = SnapUpToBar(TotalMusicalBeat + 0.5) - _localBeat`.
4. Scan giữ nguyên tới khi chạm mốc bar nhờ clamp đơn điệu: `_localBeat = Max(_localBeat, TotalMusicalBeat - _roundStartMusicalBeat)`.

Kết quả: không beat nào bị bỏ qua, không telegraph nào bị mất, nhạc không đứt một giây nào. Vì beat đều nhau, cộng offset bằng số nguyên beat là lossless về nhịp.

**Snap:** bội số 4 beat (`BeatsPerBar = 4`, khớp 4/4 và phase 16 beat = 4 bar). Trễ tối đa ~1 bar, vào đúng phách mạnh.

**Loop nhạc:** thay `introEndSec / loopEndSec / firstPassEndSec` (giây) bằng `loopStartBar / loopEndBar` (số bar). Mỗi vòng loop cộng `_loopBeatAccum += (loopEndBar - loopStartBar) * 4` để `TotalMusicalBeat` đơn điệu tăng, không nhảy lùi.

### 2.3 Audio khi planning

Duck-only: `source.volume 1.0 → 0.7` + `AudioLowPassFilter.cutoffFrequency 22000 → 900 Hz`, fade 0.25s.

Bỏ hoàn toàn: planning BGM (`EternalSpark_PlanningSilent.mp3`), transition SFX (`Combat_PlanningTransition.wav` khỏi music controller), lớp nhạc Ren Cover (giữ file `EternalSpark_RenCover.mp3` trên đĩa, chờ track cover mới hợp với Boss Remix).

### 2.4 Deploy song song Planning

Xoá khái niệm Deploy như một giai đoạn riêng. `CombatSession.AllowPlayerReposition` → `IsPlanningWindowOpen`:

```csharp
public bool IsPlanningWindowOpen => Phase == CombatPhase.Planning && !IsTimelineRunning && !IsEncounterOver;
```

Một điều kiện duy nhất mở cả kéo unit lẫn gán skill. Bỏ intro-pause @ beat 6 (thừa, vì player đã plan được từ cửa sổ đầu). Một nút **EXECUTE** duy nhất.

### 2.5 Vòng lặp mới

```
Vào scene boss
  → PlayBossMusic + EnterPlanningDuck, scan đóng băng @ beat 0
  → Planning Window: kéo unit + gán skill CÙNG LÚC
  → EXECUTE → anchor vào bar kế → ExitPlanningDuck → chạy 32 beat
  → chạm phase divider → EnterPlanningDuck → Planning Window
  → lặp đến Victory / Defeat
```

Nhạc không dừng ở bất kỳ mũi tên nào.

---

## 3. Ràng buộc

- Boss track: `Eternal Spark - Boss Remix.mp3` — 268.2935s · 48kHz · stereo.
- Import settings khớp convention: `loadType: 0` (Decompress On Load), `compressionFormat: 1` (Vorbis), `quality: 1`, `preloadAudioData: 0`, `forceToMono: 0`, `sampleRateOverride: 48000`.
- `BeatsPerBar = 4`.
- Duck: volume 0.7 · cutoff 900 Hz · fade 0.25s.
- Không viết comment giải thích trong source code.

---

## 4. Kết quả đo BPM (Task 1)

Đo bằng `tools/beat-analyzer` (spectral flux FFT-1024/hop-256 → comb-filter tempo scan → phase fit → downbeat fit).

| Tham số | Giá trị |
|---------|---------|
| `BPM` | **152.0000** |
| `FIRST_BEAT_SEC` | **1.1610** |
| `BEAT_SPAN_SEC` | **0.394737** |
| `TOTAL_BEATS` | **677** |
| `TOTAL_BARS` | **169** |
| `DOWNBEAT_PHASE` | 2 |

`TimelineConstants.PhaseCount` derived = `1 + ceil((677 - 16) / 16)` = **43**.

**Xác nhận không trôi nhịp:** phân tích độc lập 60s cuối (`-ss 208`) cho ra đúng `BPM=152.0000` và `FIRST_BEAT_SEC=0.0000`. Lưới beat toàn bài dự đoán mốc beat tại `t = 208.003s`, tức lệch 3ms sau khoảng cách 208 giây — nếu BPM sai dù chỉ 0.05 thì sai số tích luỹ đã là 68ms (0.17 beat) và sẽ lộ ra ngay. Tempo cố định, model uniform hợp lệ.

Click-track để nghe kiểm chứng: `C:\Users\Asus\Downloads\BossRemix_clicktrack_152bpm.mp3` (click 1600Hz ở phách mạnh, 1000Hz ở phách nhẹ).

---

## 5. Rủi ro

| Rủi ro | Xử lý |
|--------|-------|
| BPM sai octave (75 / 300 thay vì 150) | Comb-filter loại nghiệm ngoài dải 110–190; click-track để nghe kiểm chứng |
| DSP counter SFX lệch sau khi loop nhạc | `TryGetDspTimeForMusicalBeat` trừ `_loopBeatAccum` trước khi đổi sang thời gian audio |
| Click unit vừa mở skill panel vừa bắt đầu drag | `BoardDragController` đã tách click/drag bằng drag threshold sẵn có |
| Tutorial `AwaitDeploy` kẹt vì không còn nút Deploy | `PlayerDeployed` vẫn bắn ở lần bấm EXECUTE đầu tiên |

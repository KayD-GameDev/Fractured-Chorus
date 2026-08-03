# QA — Uniform Beat + Continuous Music

> **Ngày:** 2026-08-01 · **Scope:** Boss Remix 152 BPM · Beat Offset Anchor · Planning Window
> **SoT:** [`COMBAT_MECHANICS.md`](./COMBAT_MECHANICS.md) §1 · spec [`2026-08-01-uniform-beat-continuous-music-design.md`](../superpowers/specs/2026-08-01-uniform-beat-continuous-music-design.md)

Chạy trên `CombatPrototype.unity` trừ khi ghi rõ. Đánh dấu ✅ / ❌ + note.

---

## A. Import & wiring

| # | Kiểm tra | Kỳ vọng |
|---|----------|---------|
| A1 | Mở scene, chọn `CombatMusic` | `bossTrack = EternalSpark_BossRemix`, `beatMap = EternalSpark_BossRemix_BeatMap` |
| A2 | Chọn beat map asset | `bpm = 152`, `firstBeatOffsetSec = 1.161` |
| A3 | Console lúc Play | **Không** có warning `[CombatBootstrap] Beat map yields … TotalBeats` |
| A4 | Console lúc Play | Log `[CombatMusic] Playing 'EternalSpark_BossRemix' … 152 BPM … 677 beats` |
| A5 | Project search | Không còn `EternalSpark_CadenceRemix*`, `EternalSpark_PlanningSilent`, file `*_beats*.csv` |
| A6 | Menu Fractured Chorus | Không còn `Import Ren Cover Audio`; có `Import Combat Audio From Downloads` |

## B. Beat chia đều

| # | Kiểm tra | Kỳ vọng |
|---|----------|---------|
| B1 | Nhìn timeline | Mọi ô beat **rộng bằng nhau**, không còn chỗ hẹp/rộng lộn xộn |
| B2 | Header phase | Tối đa `30/30` |
| B3 | Chạy tới cuối bài | Timeline hết đúng beat 676, không cắt sớm / không thừa ô rỗng |
| B4 | Phase divider | Sau beat 21, 43, 65… (mỗi **22** beat) |

## C. Nhạc không dừng ⭐

| # | Kiểm tra | Kỳ vọng |
|---|----------|---------|
| C1 | Vừa vào scene | Nhạc boss **full** intro 12 beat (~5.90s) → rồi duck vào Planning |
| C2 | Bấm Execute | Nhạc **sáng lên** mượt trong ~0.25s, **không** khựng, không có tiếng stinger |
| C3 | Hết segment (22 beat) | Nhạc **tiếp tục chạy**, chỉ trầm lại; scan dừng ở vạch trắng |
| C4 | Nghe suốt 3 round liên tiếp | Không có một khoảng lặng / giật / nhảy nhạc nào |
| C5 | Ngồi yên ở planning 30s | Nhạc chạy đều, không lặp lỗi, không cắt |
| C6 | Chạy tới cuối bài (~268s) | Loop về đầu **êm** (fade 50ms), không click/pop, scan không nhảy lùi |

## D. Beat Offset Anchor ⭐

| # | Kiểm tra | Kỳ vọng |
|---|----------|---------|
| D1 | Bấm Execute | Scan **đứng yên** một nhịp ngắn rồi mới chạy — bắt vào phách mạnh |
| D2 | Bấm Execute 5 lần ở 5 thời điểm ngẫu nhiên trong cửa sổ planning | Vào đúng beat kế; độ trễ ≤ ~0.39s (1 beat @ 152 BPM) |
| D3 | Đếm beat qua từng segment | **Không** beat nào bị bỏ qua; beat đầu segment luôn là beat kế tiếp của segment trước |
| D4 | Telegraph boss | Không telegraph nào biến mất hoặc resolve hụt khi qua ranh giới segment |
| D5 | Counter SFX | Vẫn khớp nhạc sau khi anchor lại, và **sau khi nhạc đã loop một vòng** |

## E. Planning Window (Deploy ‖ Skill) ⭐

| # | Kiểm tra | Kỳ vọng |
|---|----------|---------|
| E1 | Cửa sổ planning đầu tiên | Kéo được unit sang ô khác **và** mở được skill panel — cùng lúc |
| E2 | Cửa sổ planning **giữa trận** (sau segment 1) | Vẫn kéo được unit sang ô khác (trước đây bị khoá) |
| E3 | Nút overlay | Luôn ghi **Execute**, không bao giờ hiện "Deploy" |
| E4 | Hex floor Player | Hiện trong mọi cửa sổ planning, ẩn khi timeline chạy |
| E5 | Hex floor Enemy | Luôn ẩn |
| E6 | Trong lúc timeline chạy | Không kéo được unit, không mở được skill panel, không kéo lại lane marker |
| E7 | Click nhanh vào unit (không kéo) | Mở skill panel, **không** bị nuốt thành drag |
| E8 | Kéo unit rồi thả ngoài grid | Về chỗ cũ, không mở skill panel |
| E9 | Formation hint | Hiện mỗi cửa sổ planning (trừ khi tutorial suppress) |

## F. Intro + phase 22 + lookahead 3 ⭐

| # | Kiểm tra | Kỳ vọng |
|---|----------|---------|
| F1 | Vào `CombatPrototype` | Intro **12 beat** (~5.90s); không agency |
| F2 | Trong intro | Timeline **không** có nốt |
| F2b | Hết intro → Planning | Spawn + hiện nốt phase **1–3** |
| F3 | Nốt quái phase 1 | Impact ≥ beat **3**; Boss ~5 / phase |
| F4 | Execute | Đủ **22** beat → Planning; spawn thêm phase **4** |
| F5 | Charlotte Anchor | Đẩy nốt sau S (kể cả qua phase); nốt phía sau đi theo; không mất khi replan |
| F6 | Planning giữa trận | Không intro lại |

## G. Tutorial (`CombatTutorial.unity`)

| # | Kiểm tra | Kỳ vọng |
|---|----------|---------|
| G1 | Chạy track Cadence Intro | Slide cuối ghi "nhấn **Execute**" |
| G2 | Bấm Execute lần đầu | `AwaitDeploy` hoàn tất → flag `tutorial_cadence_intro_done`, vào free play |
| G3 | Nhạc suốt tutorial | Không pause khi coach hiện/ẩn |
| G4 | Coach view đang mở | Nút Execute ẩn (không click xuyên) |

## H. Kết thúc trận

| # | Kiểm tra | Kỳ vọng |
|---|----------|---------|
| H1 | Victory | Nhạc dừng hẳn, lowpass reset (không kẹt trạng thái duck) |
| H2 | Defeat | Như trên |
| H3 | Thoát scene giữa planning | Không còn AudioSource nào phát rớt lại |

---

## Điều chỉnh nhanh

| Muốn | Sửa |
|------|-----|
| Planning nghe rõ/nhỏ hơn | `CombatMusic.duckVolume` (0.7) |
| Planning trầm nhiều/ít hơn | `CombatMusic.duckCutoffHz` (900) |
| Fade duck nhanh/chậm | `CombatMusic.duckFadeSec` (0.25) |
| Execute chậm hơn / vào đầu bar | Đổi `SnapUpToBeat` → `SnapUpToBar` trong `AnchorTimelineToNextBar` |
| Loop một đoạn thay vì cả bài | `CombatMusic.loopStartBar` / `loopEndBar` (bar, `-1` = bar đầy cuối) |
| Đổi bài boss | Đo bằng `Tools/beat-analyzer` → sửa `bpm`/`firstBeatOffsetSec` trên beat map + `TimelineConstants.TotalBeats` |

## Đo lại BPM

```powershell
cd Tools/beat-analyzer
dotnet build -c Release
ffmpeg -v error -i "<track>.mp3" -ac 1 -ar 22050 -f f32le pcm.f32
dotnet bin/Release/net9.0/BeatAnalyzer.dll --input pcm.f32 --click "<track>.mp3"
```

In ra `BPM` / `FIRST_BEAT_SEC` / `TOTAL_BEATS` / `TOTAL_BARS` và ghi `clicktrack.wav` để nghe kiểm chứng.
Xác nhận tempo không trôi: chạy lại trên 60s cuối (`ffmpeg -ss <len-60>`), BPM phải trùng và `FIRST_BEAT_SEC` phải khớp lưới toàn bài.

# Boss Musical Note Visuals — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Đổi portrait nốt boss từ vòng tròn tier sang glyph nốt nhạc (đơn/đôi), số = remaining hits trong đầu nốt, màu cả glyph theo tier, đủ hit → Cover Perfect.

**Architecture:** Logic counter (`HitsRequired` / `GetRemainingHits`) **không đổi**. Thêm layer presentation `BossNoteClusterView` span cột beat; `BeatSegmentView` thôi tự vẽ portrait impact khi cluster layer active. Grouping chạy trên danh sách impact telegraphs mỗi lần refresh Planning / Delay slide.

**Tech Stack:** Unity 6 · UGUI Image + Text · `TimelineNoteVisualCatalog` · Resources `UI/Combat/Timeline/`

## Global Constraints

- Không đổi `CountCountersAtBeat` / cancel resolve / Space-block rules
- `HitsRequired` spawn vẫn SoT; UI chỉ đọc remaining
- Runtime sprites dưới `Resources/UI/Combat/Timeline/`
- Commit chỉ khi user yêu cầu
- Play Mode checklist bắt buộc trước khi đóng task art+UI

### File map

| Action | Path |
|--------|------|
| Create | `Assets/FracturedChorus/Combat/Presentation/BossNoteClusterBuilder.cs` |
| Create | `Assets/FracturedChorus/UI/BossNoteClusterView.cs` |
| Modify | `Assets/FracturedChorus/UI/BeatTimelineUIView.cs` |
| Modify | `Assets/FracturedChorus/UI/BeatSegmentView.cs` |
| Modify | `Assets/FracturedChorus/Combat/Presentation/TimelineNoteVisualCatalog.cs` |
| Create | `Assets/FracturedChorus/Art/UI/Combat/Timeline/note_music_*.png` (+ Resources mirror) |
| Modify | `docs/combat/COMBAT_MECHANICS.md` § note visuals |

---

## Quyết định đã chốt (gate)

| ID | Chốt |
|----|------|
| N-Q1 | **Nốt đôi chỉ khi hai nốt `1` kề nhau** (cả hai remaining = 1). Chuỗi 3 nốt-1 → đôi + đơn. Nốt `2`/`3` **luôn đơn**. |
| N-Q1b | Nốt đơn **random glyph variant** (nhiều shape đơn) để đa dạng — không random nốt `2`/`3` thành đôi. |
| N-Q2 | Số = **remaining hits** |
| N-Q3 | Degrade → đổi màu **cả glyph** theo tier (3 Tím / 2 Xanh / 1 Đỏ) |
| N-Q4 | Remaining 0 → **Cover Perfect ✓**, nốt biến mất |
| N-Q5 | Nốt đôi **span 2 cột beat** thật (đầu trái @ N, phải @ N+1) |
| N-Q6 | Beamed chỉ `HitsRequired==1` spawn | **B** ✅ |
| N-Q7 | Một đầu clear → giữ beam + ✓ | **B** ✅ |
| N-Q8 | Art pre-color | **B** ✅ |
| N-Q9 | Số = Text outline | **A** ✅ |
| N-Palette | **Neon Cadence** ✅ |
| N-Q10 | Variant = hash(beat) | **B** ✅ |
| N-Q11 | 5 shape đơn × 3 màu + beamed đỏ | **B** ✅ |

---

## Điểm bất hợp lý / xung đột — đã xử lý một phần + còn mở

### 1. ✅ Làm rõ: nốt đôi = hai nốt **số 1** kề nhau

| Khái niệm | Nghĩa |
|-----------|--------|
| **HitsRequired / remaining** | Logic counter trên **một** beat |
| **Nốt đôi (beamed)** | Hai impact ở `N`,`N+1` **và** mỗi bên `remaining == 1` |
| **Nốt 2 / 3** | Luôn glyph **đơn** (số 2 hoặc 3 trong đầu), dù có hàng xóm |

Không còn nhầm “HitsRequired=2 ⇒ nốt đôi”.

### 2. ✅ N-Q3 vs beamed — bớt xung đột

Hai đầu beamed đều remaining=1 → cùng tier Đỏ → tint cả beam một màu ổn.  
Khi một đầu → 0: xem N-Q7.

### 3. Random glyph — chốt N-Q10 = B

`variantIndex = Hash(beatIndex) % VariantCount` — deterministic theo cột beat.  
DelayBossNote đổi beat → shape có thể đổi (chấp nhận). Không persist field trên telegraph.

### 4. ⚠ “Nốt 1” theo remaining hay spawn?

Nếu dùng **remaining**: nốt spawn Xanh (2) bị counter 1 hit → remaining=1; nếu hàng xóm cũng 1 → **đột nhiên beamed** mid-planning.

Nếu dùng **spawn HitsRequired==1**: chỉ nốt Đỏ gốc mới được đôi; degrade 2→1 vẫn đơn.

**Mở N-Q6 (revised):**
- **A)** Beamed khi `remaining==1` cả hai (động)  
- **B)** Beamed khi `HitsRequired==1` cả hai lúc spawn (ổn định hơn)

**Đề xuất: B** — tránh beam “nhảy” khi counter.

### 5. Một đầu của cặp về 0 trước đầu kia

**Chốt N-Q7 = B:** Giữ beamed glyph; đầu đã clear hiện Cover Perfect ✓ trên cột đó; đầu còn lại vẫn số 1.

### 6. Art — chốt N-Q8 = B (pre-color)

Pre-render sprite sẵn màu × shape (không tint mask runtime).  
Số vẫn Text đè đầu nốt.  
Số file ≈ `(variant đơn × 3 tier) + (beamed × 3 tier)` — beamed chỉ Đỏ trong practice (HitsRequired=1), vẫn ship 3 màu beamed nếu degrade edge / consistency, hoặc chỉ Đỏ cho beamed (tiết kiệm).  

**Đề xuất asset beamed:** chỉ **Đỏ** (vì N-Q6=B chỉ pair nốt 1). Đơn: 3 variant × 3 màu.

### 7. `BeatSegmentView` 1 slot ≠ span 2 cột

Beamed = layer riêng viewport (như BlockBarrierLayer).

### 8. DelayBossNote / slide

Rebuild cluster; variant theo N-Q10.

### 9. Drop-preview / Perfect

Slot ẩn note khi cluster on; Perfect/Miss vẫn trên slot.

### 10. Multi-telegraph cùng beat

Primary = HitsRequired cao nhất.

### 11. Micro / EYE

P2 — ngoài scope.

---

## Palette đề xuất — Neon Cadence

Khớp `bossTrackFrame` (cyan `#73FAFF` / magenta `#D973FF`) + prep pip cyan — đọc được trên nền track đỏ sẫm.

| Tier | Remaining | Note fill (pre-color) | Glow / edge | Số (Text) |
|------|-----------|----------------------|-------------|-----------|
| **Purple** | 3 | `#B44CFF` | `#E0A0FF` soft | `#FFFFFF` + outline `#2A0A40` |
| **Blue** | 2 | `#3D9CFF` | `#7EC8FF` | `#FFFFFF` + outline `#0A1A40` |
| **Red** | 1 (+ beamed) | `#FF4A5A` | `#FF8A96` | `#FFFFFF` + outline `#401018` |
| Perfect ✓ | 0 | giữ `cover_perfect` | — | — |

Số: trắng + outline tối (đọc trên mọi màu đầu nốt). Không dùng vàng/cream (lệch hologram).

## Gate — ĐỦ CHỐT

Không còn câu hỏi mở. Asset count sprint: **5×3 = 15** sprite đơn + **1** beamed đỏ (+ optional beamed blue/purple nếu sau cần).

---

## Thuật toán grouping (SoT)

Input: sorted unique beat indices có impact (`!IsWindupOnly`), mỗi beat 1 primary telegraph.

`IsPairable(t)` = (N-Q6=B ? `t.HitsRequired==1` : `GetRemainingHits(t)==1`) AND remaining > 0.

```
i = 0
while i < beats.Count:
  t = primary[beats[i]]
  if remaining(t) <= 0: i++; continue   // Perfect handled by slot

  if i+1 < n
     AND beats[i+1] == beats[i]+1
     AND IsPairable(t) AND IsPairable(primary[beats[i+1]]):
    emit Beamed(beats[i], beats[i+1])  // cả hai số 1
    i += 2
  else:
    emit Single(beats[i], variant=t.VisualVariant)  // 2/3 hoặc 1 lẻ
    i += 1
```

| Tình huống | Output |
|------------|--------|
| `1` lẻ | Single (random variant) số 1 |
| `1`,`1` kề | Beamed |
| `1`,`1`,`1` | Beamed + Single |
| `2` hoặc `3` (kể cả kề nhau) | Single mỗi cột (variant random), **không** beam |
| `1`,`2` kề | Single + Single |

`VisualVariant = Hash(beatIndex) % VariantCount` (N-Q10=B).

---

## Tasks

### Task 1: Art pre-color (5 đơn × 3 màu + beamed đỏ)

**Files:**
- Create: `Assets/FracturedChorus/Art/UI/Combat/Timeline/note_music_single_v{0-4}_{purple|blue|red}_v1.png` (15)
- Create: `Assets/FracturedChorus/Art/UI/Combat/Timeline/note_music_beamed_red_v1.png` (1)
- Mirror: `Resources/UI/Combat/Timeline/`

**Done when:** 16 sprites Neon Cadence; đầu nốt đủ chỗ số Text; beamed span 2 cột.

- [x] **Step 1:** Gen 5 shape đơn × purple/blue/red + 1 beamed red.
- [x] **Step 2:** Import + Resources mirror.
- [x] **Step 3:** Wire `TimelineNoteVisualCatalog` arrays.

### Task 2: `BossNoteClusterBuilder` (pure logic)

**Files:**
- Create: `Assets/FracturedChorus/Combat/Presentation/BossNoteClusterBuilder.cs`
- Test: Play Mode log / manual assert table dưới

**Produces:**
```csharp
public enum BossNoteGlyphKind { Single, Beamed }
public readonly struct BossNoteHead {
  public int BeatIndex;
  public EnemyTelegraph Telegraph;
  public int RemainingHits;
  public BossNoteTier DisplayTier;
}
public readonly struct BossNoteCluster {
  public BossNoteGlyphKind Kind;
  public BossNoteHead Left;   // Single: only Left used
  public BossNoteHead Right;  // Beamed only
}
public static List<BossNoteCluster> Build(BeatTimelineEngine timeline);
```

- [ ] **Step 1:** Implement grouping + remaining/tier per head.
- [ ] **Step 2:** Verify table: beats `{8}` → 1 single; `{8,9}` → 1 beamed; `{8,9,10}` → beamed(8,9)+single(10); `{8,10}` → 2 singles.
- [ ] **Step 3:** Skip windup-only; primary telegraph nếu multi cùng beat.

### Task 3: `BossNoteClusterView` + timeline layer

**Files:**
- Create: `Assets/FracturedChorus/UI/BossNoteClusterView.cs`
- Modify: `BeatTimelineUIView.cs` (layer + rebuild on telegraph/agenda change)
- Modify: `BeatSegmentView.cs` (ẩn impact portrait khi cluster layer on; giữ Perfect/Miss)

**Behavior:**
- Single: Image tint tier + Text số @ beat X  
- Beamed: Image span X→X+1; hai Text số @ hai đầu; tint theo **N-Q6**  
- Remaining 0: không vẽ head đó; slot hiện Cover Perfect (N-Q4)  
- N-Q7=A: rebuild → đầu còn lại thành Single  

- [ ] **Step 1:** Layer `BossNoteClusterLayer` under viewport, scroll sync như barrier.
- [ ] **Step 2:** Rebuild on `OnTelegraphsChanged`, agenda assign/remove, Delay batch.
- [ ] **Step 3:** BeatSegment impact path: `showClusterNotes=true` → clear note sprite trừ Perfect/Miss.

### Task 4: Catalog + drop preview sync

**Files:**
- Modify: `TimelineNoteVisualCatalog.cs`
- Modify: drop ghost path in `BeatTimelineUIView.ShowDropGhost`

- [ ] **Step 1:** Fields `NoteMusicSingle`, `NoteMusicBeamed`; fallback tint circle nếu null.
- [ ] **Step 2:** Drag preview: remainingAfter cập nhật số/tint trên cluster (hoặc temporary overlay).
- [ ] **Step 3:** Fully-countered place lock vẫn per-beat (đã có).

### Task 5: Docs + Play Mode checklist

**Files:**
- Modify: `docs/combat/COMBAT_MECHANICS.md` (visual section)

**Checklist:**
1. 1 nốt lẻ — glyph đơn, số đúng remaining  
2. 2 kề — beamed span 2 cột  
3. 3 kề — đôi + đơn  
4. Counter 1 hit trên Tím — số/màu 3→2  
5. Đủ hit — Perfect ✓, nốt đi  
6. Beamed: clear trái trước — phải thành đơn (N-Q7)  
7. DelayBossNote slide — cluster không lệch cột  
8. Space / counter lock không regress  

---

## Thứ tự

```
Gates đủ → Task 1 (art 16 sprites) → Task 2 (builder) → Task 3 (view) → Task 4 (preview) → Task 5 (docs/QA)
```

Logic damage/counter **không** nằm trong plan này.

---

## Self-check

| Yêu cầu user | Task |
|--------------|------|
| Nốt nhạc thay vòng tròn | 1, 3 |
| Số trong vòng / đầu nốt | 3 |
| Màu theo hit / degrade | 2, 3 |
| Nốt đôi khi 2 kề | 2, 3 |
| 3 kề = đôi + đơn | 2 |
| Span 2 cột | 3 |
| Perfect khi đủ | 3 |
| Bắt bất hợp lý trước code | § Điểm bất hợp lý + N-Q6..Q9 |

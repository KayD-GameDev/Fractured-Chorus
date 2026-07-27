# LeftRail v2 — Regen PHASE wordmark · Phase chip · Treble clef

> Scope: **chỉ 3 asset** trong `Header` (LeftRail). `left_rail_bg_v1`, `lane_avatar_ring_v1`, LaneLines, Viewport = **out of scope**.
> Parent plan: `2026-07-27-timeline-left-rail-design.md`

**Goal:** gen lại 3 asset cho **đẹp**, **alpha sạch (xoá hết phông/halo trắng)**, **kích thước hiển thị đúng tỉ lệ cột 211px**.

---

## 1. Đo hiện trạng (SoT — scene + PNG, 2026-07-27)

### Rect trong scene (`CombatPrototype.unity`, Canvas 1920×1080)

| Node | sizeDelta | anchoredPos | Ghi chú |
|------|-----------|-------------|---------|
| `Header` | 210.82 × stretchY | (0,0) | H ≈ 358 |
| `PhaseLabel` | **170 × 62** | (20, −6) anchor top-left | child `PhaseArt` stretch, y +7.5 |
| `Budget` | **118 × 44** | (105.5, 127.3) | `BudgetText` stretch full |
| `Clef` | **136.78 × 152.45** | (102.3, 56.8) | child `ClefIcon` stretch, y −2.6 |

### PNG hiện tại + kích thước render thực (Image.preserveAspect = true)

| Asset | Canvas PNG | Ink bbox | Ink / canvas | Scale vào rect | **Ink hiển thị** |
|-------|-----------|----------|--------------|----------------|------------------|
| `phase_label_v3` | 280×100 | **253×22** | 90% W / **22% H** | ×0.607 | **154 × 13 px** |
| `av_budget_chip_v2` | 240×80 | 203×37 | 85% / 46% | ×0.492 | 100 × 18 px |
| `treble_clef_v3` | 256×320 | **104×233** | **41% W** / 73% H | ×0.476 | **50 × 111 px** |

### Alpha audit (System.Drawing sampling)

| Asset | Pixel A=0 | 0<A<255 | A=255 | Near-white opaque (max>200, sat<30) |
|-------|-----------|---------|-------|--------------------------------------|
| `phase_label_v3` | có | **0** | 3773 | **1498 = 40%** |
| `treble_clef_v3` | có | **0** | 9349 | **2313 = 25%** |
| `av_budget_chip_v2` | có | **0** | 6721 | 948 = 14% |

**Kết luận đo được (nguyên nhân "xấu"):**

1. **Alpha nhị phân (0 hoặc 255), 0 pixel bán trong suốt** → mọi cạnh **răng cưa**, glow bị cắt cứng.
   Thủ phạm: `Tools/SpriteBgClear.exe` — flood-fill từ biên, xoá pixel near-white (`Tools/SpriteBgClear.cs:143-156`). Nó **chỉ xoá phông chạm biên**; vùng glow trắng đục **bên trong** bị giữ lại **opaque 100%** → 25–40% pixel là "phông trắng sữa" bám vào chữ và khoá sol.
2. **Chữ PHASE render chỉ 13px cao** trong cột 211px (ink chiếm 22% chiều cao canvas) → yếu, mảnh, đọc không ra.
3. **Khoá sol render 50px rộng** (ink chỉ 41% canvas) → nhỏ và loãng so với ref.
4. **Bug số phase:** `av_budget_chip_v2` **bake sẵn chữ "0/100"**. Code phát hiện tên chứa `chip` → `budgetLabel.enabled = false` (`BeatTimelineUIView.cs:571-574`) → **số phase runtime bị tắt**. Giá trị đúng phải là `phaseIndex+1 / TimelineConstants.PhaseCount` = **`1/39` … `39/39`**, không phải `0/100`.
5. Style hiện tại: chữ **outline rỗng** + khoá sol **lõi trắng** → mất bản sắc Neon Cadence (cyan core / magenta accent) và bị "wash out" trên nền navy.

---

## 2. Design direction (Neon Cadence, lock chung cho 3 asset)

| Layer | Lock |
|-------|------|
| Palette | core cyan `#8CF3FF` → body `#22D3EE`, accent magenta `#FF3DA6`, không dùng trắng thuần làm fill |
| Light | phát sáng tự thân (emissive), **1 nguồn**, không đổ bóng ngoài |
| Glow | bloom mềm ≤ 10% bbox, alpha tail giảm dần (bắt buộc bán trong suốt) |
| Nền gen | **đen tuyệt đối `#000000`**, không gradient, không vignette, không viền khung |
| Cấm | text phụ, watermark, khung viền, nền trắng, drop shadow, 3D bevel, sợi dây staff |

Lý do nền đen: pipeline matte mới lấy **alpha = luminance**, nền đen → alpha 0 tuyệt đối, glow giữ nguyên độ mềm. Nền trắng (như lần trước) buộc phải threshold → alpha nhị phân + halo.

---

## 3. Target size mới (tính ngược từ rect)

Quy tắc: **canvas PNG = 2× rect**, ink lấp gần kín canvas → không còn "khoảng trắng chết" ăn mất scale.

| Asset mới | Canvas PNG | Ink bbox target | Ink/canvas | Ink hiển thị | So với hiện tại |
|-----------|-----------|-----------------|------------|--------------|-----------------|
| `phase_label_v4.png` | **340 × 124** | 320 × 68 (cap-height 60) | 94% / 55% | **160 × 34 px** | 13 → **34 px** (+161%) |
| `phase_chip_v3.png` | **236 × 88** | 232 × 84 | 98% / 95% | **116 × 42 px** | khung kín rect |
| `treble_clef_v4.png` | **140 × 306** | 132 × 292 | 94% / 95% | **68 × 152 px** | 50×111 → **68×152** (+38%) |

Ghi chú clef: rect cao 152.45 là trần cứng → muốn to hơn phải sửa rect (xem gate Q2). Canvas 140×306 (aspect 0.457) khớp aspect thật của khoá sol nên không phí pixel.

---

## 4. Prompt gen (mỗi asset 3 biến thể, đổi **1** biến/vòng)

### A. `phase_label_v4` — wordmark "PHASE"

```text
Neon sign wordmark, the single word "PHASE" in uppercase, on a pure solid black background.
Wide geometric techno sans-serif, heavy uniform stroke, generous letter-spacing, perfectly
horizontal baseline, the word fills 94% of the image width and is vertically centered.
Cyan neon tube: bright #8CF3FF core, #22D3EE body, soft cyan bloom halo under 10% of the
letter height. One thin magenta #FF3DA6 hairline accent under the word.
Sharp anti-aliased edges, crisp, high contrast against black.
Negative: white background, white fill, extra text, numbers, frame, border, box, drop shadow,
3D bevel, blur, watermark, reflection, gradient background.
```

Variant B: chữ **đặc** (solid fill) thay vì tube. Variant C: cyan core + magenta rim-light lệch 1px.

### B. `phase_chip_v3` — khung số phase (**không** chữ số)

```text
Empty HUD badge frame, horizontal elongated hexagon capsule, on a pure solid black background.
Magenta #FF3DA6 neon outer rim, thin cyan #22D3EE inner hairline, small notch cuts on the left
and right ends, dark translucent navy interior panel. The frame fills 98% of the image.
Completely EMPTY interior — no text, no numbers, no glyphs, no icons inside.
Sharp anti-aliased edges, symmetric, high contrast against black.
Negative: any text, any digits, "0/100", letters, white background, white fill, drop shadow,
3D bevel, blur, watermark.
```

> **Bắt buộc rỗng** — số `1/39` do `budgetLabel` (UGUI Text) vẽ runtime.

### C. `treble_clef_v4` — khoá sol

```text
A single treble clef music symbol, centered, on a pure solid black background.
Elegant classical treble clef proportions, drawn as one continuous neon glass tube with a
uniform stroke weight, cyan #22D3EE body with a brighter #8CF3FF inner core line, soft cyan
bloom under 8% of the stroke width. The clef fills 95% of the image height, tall narrow
composition, no tilt.
Sharp anti-aliased edges, high contrast against black.
Negative: white fill, milky white haze, staff lines, musical notes, extra symbols, text,
frame, border, background pattern, drop shadow, blur, watermark.
```

Variant B: stroke mảnh hơn 30% (watermark feel). Variant C: gradient cyan→magenta từ trên xuống.

---

## 5. Pipeline hậu kỳ — **thay `SpriteBgClear`**

Script mới: `Tools/neon-matte.mjs` (Node + `sharp` đã có trong `node_modules`). `SpriteBgClear.exe` **không dùng** cho 3 asset này.

Các bước, chạy 1 lệnh/asset:

| # | Bước | Công thức |
|---|------|-----------|
| 1 | Luminance matte | `L = max(R,G,B)/255`; `A = clamp(L / kKnee, 0, 1)` với `kKnee = 0.90` → nền đen A=0, glow giữ alpha mềm |
| 2 | Unpremultiply | `RGB' = RGB / max(A, 0.02)` → chống viền tối quanh chữ khi UI blend |
| 3 | Denoise nền | A < `2/255` → set A=0 (dọn nhiễu JPEG/gen) |
| 4 | Trim | crop về ink bbox theo ngưỡng A ≥ 8 |
| 5 | Re-canvas | resize ink về target bbox (§3), pad trong suốt ra đúng canvas PNG, căn giữa |
| 6 | QA report | in bảng: size, bbox, %ink, %alpha biên (0<A<255), %near-white opaque |

CLI dự kiến:

```bash
node Tools/neon-matte.mjs <in.png> <out.png> --canvas 340x124 --ink 320x68 --knee 0.90
```

### QA gate (fail = gen lại, không "tạm chấp nhận")

| # | Chỉ tiêu | Ngưỡng |
|---|----------|--------|
| G1 | Pixel bán trong suốt `0<A<255` / tổng pixel non-zero | **≥ 15%** (chứng minh hết alpha nhị phân) |
| G2 | Alpha vành biên 2px | **= 0** toàn bộ |
| G3 | Near-white opaque (max>200, sat<30, A>200) | **≤ 5%** (clef ≤ 3%) |
| G4 | Ink bbox / canvas | đúng §3 ±3% |
| G5 | Chip có ký tự bake | **0** (kiểm tra mắt + histogram vùng lõi) |
| G6 | Không lệch tâm | ink center lệch canvas center ≤ 2px |

---

## 6. Thay đổi code (kèm dọn rác)

| File | Thay đổi |
|------|----------|
| `BeatTimelineUIView.cs:571-583` | **Xoá** nhánh `bakedBudgetText` — chip luôn là khung rỗng, `budgetLabel` luôn bật; set `fontSize = 22`, bold, color `#EAFBFF` |
| `BeatTimelineUIView.cs:613-636` | Fallback chain: `phase_label_v4` → (bỏ v3/v2/v1); `phase_chip_v3` → (bỏ `av_budget_chip_v2`/`av_budget_frame_v1`); `treble_clef_v4` → (bỏ v3/v2/v1) |
| `LeftRailLayout.cs` | `clefAlpha` mặc định 0.5 → **0.62** (art mới không còn lõi trắng nên cần đậm hơn để giữ cảm giác watermark) |
| Scene | Không đổi rect (trừ khi chốt gate Q2) |
| Xoá file | `phase_label_v1/v2/v3`, `treble_clef_v1/v2/v3`, `av_budget_chip_v2`, `av_budget_frame_v1` (+ `.meta`, cả bản `Resources/`) sau khi v4 pass QA |

---

## 7. Import Unity (cả `Art/` và mirror `Resources/UI/Combat/Timeline/LeftRail/`)

- Texture Type **Sprite (2D and UI)** · Alpha Is Transparency **on** · Mip Maps **off**
- Filter **Bilinear** · Compression **None** (asset UI nhỏ, tránh block artifact trên glow)
- Max Size **512** · Wrap **Clamp**
- `phase_chip_v3`: cân nhắc Sprite Border `28,20,28,20` + `Image.Type = Sliced` nếu muốn chip co giãn theo chuỗi số dài (`39/39`)

---

## 8. Tasks

### Task 0 — Gate
- [x] Q1 = A (sprite) · Q2 = A (giữ rect clef) · Q3 = A (hexagon capsule) · Q4 = A (xoá asset cũ)

### Task 1 — Tooling
- [x] `Tools/neon-matte.mjs` (sharp; matte → unpremultiply → trim → re-canvas → QA)
- [x] Smoke test asset cũ: `treble_clef_v3` semi-alpha **0%**, near-white **24.7%**; `phase_label_v3` **0%** / **39.7%** → script đo đúng

### Task 2 — Gen art
- [x] `phase_label` 3 biến thể (tube / solid / rim-light) → chọn **solid**
- [x] `treble_clef` 3 biến thể (tube dày / hairline / gradient) → chọn **tube dày**
- [x] `phase_chip` 2 biến thể → chọn **hexagon capsule magenta**, ruột rỗng

### Task 3 — Matte + resize
- [x] `phase_label_v4` 340×124 · ink 320×68 · semi-alpha 58.7% · near-white 0.0%
- [x] `treble_clef_v4` 112×300 · ink 104×292 · semi-alpha 70.9% · near-white 0.0%
- [x] `phase_chip_v3` 236×88 · ink 232×84 (`--fit fill`) · semi-alpha 94.8% · near-white 0.0%
- [x] Mirror `Resources/` + `.meta` Sprite Single, alphaIsTransparency, maxSize 512, compression none

### Task 4 — Code + scene
- [x] Bỏ nhánh `bakedBudgetText`; `budgetLabel` luôn bật, font 22, color `#EAFBFF`
- [x] Fallback chain gọn còn 1 dòng/asset (`phase_label_v4`, `treble_clef_v4`, `phase_chip_v3`)
- [x] `LeftRailLayout.clefAlpha` default 0.5 → **0.62**; scene 0.85 → 0.62
- [x] Scene: cập nhật 3 `Sprite` field + 3 `m_Sprite` của Image sang GUID mới
- [x] Scene: `BudgetText` m_Enabled 0 → 1, font 18 → 22, text `0/100` → `1/39`
- [x] Xoá 32 file asset cũ (8 PNG + 8 meta × Art/Resources)

### Task 5 — Layout fix (phát sinh từ preview)
- [x] Dựng `Tools/preview-left-rail.mjs` → phát hiện chip (y 30..74) bị PhaseLabel (y 6..68) đè
- [x] Budget y 127.3 → **87** (chip xuống dưới label, y 70..114)
- [x] Clef y 56.8 → **−21** (y 124..276, giữa cột)
- [x] Xác nhận `laneAvatarGutter` parent = root, W 44 → avatar ở x 2..42, **không** giao clef (x 74..131)

### Task 6 — Verify
- [ ] Play Mode checklist §9 (cần Unity Editor)
- [x] Cập nhật `ASSET_BRIEF.md` (size mới + cấm `SpriteBgClear`)

---

## 9. Play Mode checklist

| # | Check |
|---|-------|
| 1 | "PHASE" cao ~34px, nét liền, không răng cưa khi zoom 200% |
| 2 | Chip hiện số **runtime** và **đổi theo beat** (`1/39` → `2/39` …) |
| 3 | Không còn mảng trắng đục quanh chữ / khoá sol trên nền navy |
| 4 | Khoá sol ~68×152, không đè lên ô avatar trên cùng |
| 5 | 3 sợi `LaneLines` còn nguyên (regression bắt buộc) |
| 6 | LeftRail art không tràn khỏi 211px sang Viewport |
| 7 | Resolve chip / `GetHeaderRightEdge…` không lệch |

---

## 10. Gate questions

| ID | Câu hỏi | Chốt (2026-07-27) |
|----|---------|-------------------|
| **Q1** | PHASE sprite art hay UGUI Text + font? | **A) Sprite** |
| **Q2** | Khoá sol giữ rect hay nới rect? | **A) Giữ rect** (chỉ đổi vị trí Y, xem Task 5) |
| **Q3** | Chip: frame rỗng + Text runtime hay 9-slice? | **A) Frame rỗng, Type Simple** (meta vẫn có border 28/20 để bật Sliced sau) |
| **Q4** | Asset cũ sau khi v4 pass QA? | **A) Xoá hết** PNG + meta ở Art/ và Resources/ |
| **Q5** | Vị trí sau preview (chip đè label) | **Áp đề xuất**: Budget y=87, Clef y=−21, clefAlpha 0.62 |

### Kết quả đo sau regen

| Asset | Ink hiển thị trước | Ink hiển thị sau | Semi-alpha | Near-white |
|-------|-------------------|------------------|------------|------------|
| PHASE | 154 × **13** px | 160 × **34** px | 0% → **58.7%** | 39.7% → **0.0%** |
| Chip | 100 × 18 (bake `0/100`) | 116 × 42 (rỗng, số runtime) | 0% → **94.8%** | 14% → **0.0%** |
| Clef | 50 × 111 | 53 × **148** | 0% → **70.9%** | 24.7% → **0.0%** |

---

## Non-goals

- Không đụng `left_rail_bg_v1`, `lane_avatar_ring_v1`, staff, notes, ScanBar
- Không đổi logic phase index / AV / select lane
- Không commit khi chưa được yêu cầu

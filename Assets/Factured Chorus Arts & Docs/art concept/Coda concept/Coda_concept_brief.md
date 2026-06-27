# Coda — Concept brief v11 · **LOCKED CANON**

**Status:** LOCKED · xem `Coda_LOCK.txt`  
**Canon art:** `Coda_concept_CANON_alpha.png` (= v11 hairfix)  
**Deliverable:** `Coda_concept_v11_hairfix.png` · `Coda_concept_v11_hairfix_alpha.png`

---

## Pillars (cập nhật)

1. **Human-first** — da và silhouette người bình thường; glitch chỉ ở vài vùng (không full waveform)
2. **Audio scavenger (light)** — đồ hậu tận thế, **không** bandolier / cable / chân máy
3. **Guide vui, hơi mỉa** — pose năng động, smirk tự tin; spectrum eyes = “luôn đang monitor”

---

## Body & glitch rules (v3)

| Vùng | Treatment |
|------|-----------|
| Toàn thân (mặc định) | Da người solid, cel-shade bình thường |
| **Tay trái** (đang vẫy) | Waveform trong suốt cyan–tím + sine oscilloscope + nốt nhạc bay (giống ref v1) |
| **Bắp tay phải** (forearm) | Cùng style waveform, **chỉ** vùng cẳng tay dưới khuỷu |
| **Bắp chân trái** (v10+) | **Một** patch waveform cyan–tím trên **bắp chân trên chân trái** (đối diện chân phải v9) — **không** star, **không** chân phải, **không** bụng |
| Chân | **Human legs**, combat boots đôi bình thường |

**Forbidden:** full transparent body, chân máy/prosthetic, bandolier chéo ngực, jack cable trên tay, carabiner.

**Glitch visual lock:** translucent glow + grid/sine bên trong + floating eighth notes — **không** pixel RGB split kiểu v2.

---

## Hair (2 variants)

| Variant | Kiểu | Màu |
|---------|------|-----|
| **v6** | Undercut bob spiky, một side cạo | Trắng + highlight tím nhạt (ref v2) |
| **v6b** | Bob + bangs + ponytail nhỏ **đỉnh đầu** | **Trắng** `#F5F0FF` + highlight tím nhạt `#C4B5FD` — **không đen** |

## Skin (v7 / v7b)

- **Fair light peach-beige** — `#F0D5BC` / `#FFDFC4` (sáng hơn ~2 tông so với tan v6)

---

## Eyes (canon v9)

- **Rainbow gradient iris** (ref v7b) — gradient dọc teal → xanh lá → vàng → cam/hồng; highlight trắng + star sparkle nhỏ
- **Không** EQ bar dọc/ngang, **không** heterochromia
- Biểu cảm: smirk tự tin

## Outfit v8 (cyber-pop scavenger)

| Slot | Item |
|------|------|
| Inner | Áo crop **đen** + graphic soundwave cyan/hồng/tím; **viền fracture/glitch** nhẹ (pixel tear hem, seam) |
| Mid | Denim vest **xanh nhạt** mở, tay ragged |
| Bottom | Short denim **đen** distressed, belt đen + chain bạc |
| Leg | Thigh strap đen; bandage đầu gối (optional) |
| Feet | Combat boots **đen**, dây **tím** |
| Gloves | Fingerless đen (tay hông) |

Legacy v7 denim-nâu vẫn trong archive.

---

## Outfit concept — “post-apoc active girl” + audio loot

Phối đồ theo **silhouette reference** (vest + bandolier + shorts + mech chân) nhưng **manifest prop audio**:

| Slot | Item | Story read |
|------|------|------------|
| Top | Crop tank trắng, viền rách | Sống sót / DIY |
| Mid | Denim vest không tay, spike vai | Patch **waveform tím** ngực trái |
| Bottom | Denim shorts | Năng động |
| Belt | Utility belt + **một** pouch | Tối giản; không dây chéo |
| Hands | Găng fingerless (tay phải); tay trái glitch | Tay trống, **không** cầm cable |
| Chân | Bandage đùi (optional) + **boots nâu đôi** | Human, không cơ khí |

---

## Color script v8 (60 / 30 / 10)

| Role | Hex (gợi ý) | % | Cảm xúc / UX |
|------|-------------|---|--------------|
| **Black cloth** | `#1A1A1A` | ~25% | Crop + shorts anchor |
| **Light denim** | `#7EB8DA` / `#4A90B8` | ~20% | Vest, tách khỏi bg tối |
| **Skin** | `#F0D5BC` / `#FFDFC4` | ~15% | Fair peach-beige, cel 2–3 bước |
| **Hair** | `#E8E0F0` + `#B794F6` | ~10% | Trắng tím nhạt |
| **Soundwave accent** | cyan `#22D3EE` · magenta · purple | ~10% | Áo trong + glitch |
| **Purple identity** | `#8B5CF6` | ~8% | Coda / glitch / boot laces |
| **Metal** | `#C0C0C0` | ~5% | Belt, chain |

---

## Expression

Smirk tự tin, mắt EQ sáng — vui tích cực + chút mỉa (“tôi đo được nhịp của bạn đấy”).

---

## Engine import

- Concept canon: `Coda_concept_v11_hairfix_alpha.png`
- PPU 100–150; Filter Bilinear; Pivot feet nếu in-world
- Glitch notes: tách layer VFX sau nếu animate idle pulse

---

## Changelog

| Ver | Thay đổi |
|-----|----------|
| v1 | Full waveform body, idol outfit |
| v2 | Human + pixel glitch; post-apoc + bandolier + chân máy |
| v3 | Bỏ bandolier/cable/chân máy; glitch waveform ref v1 **chỉ** tay trái + forearm phải + bụng |
| **v4** | Giữ nguyên v3; đổi tóc → bob + bangs + ponytail đỉnh (ref concept gốc) |
| **v5** | Giữ v4; da sáng fair peach |
| **v6** | Da + tóc v2 (tan + undercut spiky); outfit/glitch v5 |
| **v6b** | Da v2 + tóc v4 (tan + bob/ponytail đỉnh) |
| **v7** | v6 undercut + da sáng + **glitch waveform** (tay vẫy + forearm + bụng) |
| **v7b** | v6b + da sáng 2 tông |
| **v8** | Outfit ref cyber-pop + spectrum eyes + áo trong fracture nhẹ |
| **v9** | v8 outfit + rainbow gradient eyes + bỏ mark bụng + star calf (sai chân phải) |
| **v10** | v9 fix: waveform mark · chân trái (v10b tóc đen — lỗi) |
| **v11** | v10 + **tóc trắng–tím** (fix drift đen) |

---

## Prompt template v9

```text
Full-body 2D anime character, transparent background.
Coda: mostly human fair peach skin. Waveform glitch on left waving hand/arm, right forearm only
(translucent cyan-purple, oscilloscope sine, floating musical notes).
EYES: smooth vertical rainbow gradient iris (teal→green→yellow→orange-pink), white highlight + star sparkle.
NO equalizer bars, NO heterochromia.
White-lavender hair bob + bangs + crown ponytail (base WHITE #F5F0FF, purple tips only — NO black hair).
OUTFIT v8: black soundwave crop (fractured hem), light blue denim vest, black shorts, belt+chain,
thigh strap, black boots purple laces, fingerless glove hip hand.
Clean midriff — NO belly marks. ONE cyan-purple WAVEFORM sine patch on character LEFT leg UPPER CALF (opposite leg from v9 wrong placement). NOT star. NOT right leg.
NO bandolier, NO cables, NO mechanical legs. Cel-shaded, key top-left. No text/watermark.
```

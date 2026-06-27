# Ren — Human world concept v4 · **LOCKED CANON**

**Status:** LOCKED v4 · xem `Ren_LOCK.txt`  
**Canon art:** `Ren_human_CANON_full_alpha.png` · `Ren_human_uniform_only_alpha.png` · `Ren_human_CANON_chibi_alpha.png`  
**Approved gen:** `Ren_human_full_v4_logo_gen_checker.png` · `Ren_human_uniform_v4_logo_gen_1024_b.png` · `Ren_human_chibi_v4_gen.png`  
**Realm:** Thế giới thực — Lumina / HIMA  
**F drive:** `F:\Factured Chorus\art concept\Ren concept\human_world\`  
**Pipeline:** `scripts/ren_v4_logo_gen_finalize.py` (Charlotte v6 pattern — gen baked HIMA + matte only)  
**Palette:** `Ren_human_palette_CANON.png`

---

## Pillars

1. **Quiet rebel prodigy** — nghe được lớp “nứt vỡ”; không phá nhạc, **cover lại** bài bị chiếm đoạt
2. **Melancholic discipline** — mệt mỏi kiểu Yi Sang, bình tĩnh kiểu Persona 5 Ren; không cười rạng rỡ
3. **Off-Beat student** — học sinh HIMA, indie Second Listen; violin là nhạc cụ chính, **không** vũ khí hóa

---

## Anatomy pass (full body)

| Check | Value |
|-------|-------|
| Line of action | Thẳng đứng, vai thả nhẹ — posture học sinh trường nhạc |
| Ribcage/pelvis | Contrapposto nhẹ, cân bằng hai chân |
| Head-count | ~7.5 heads (full) · ~2.4 heads (chibi MapleStory) |
| Joints | Tay cầm violin/bow hợp lý; không noodle limbs |

---

## Face lock

| Feature | Spec |
|---------|------|
| Shape | Slim anime male, pale complexion |
| Eyes | Heavy-lidded tired gaze; grey-blue iris `#6887C7` / `#4B69A3`; highlight `#D7E7FF` |
| Expression | Calm, suppressed melancholy — small neutral mouth |
| Hair | Messy jet-black; bowl bangs che phủ trán + volume sóng hai bên (Yi Sang × P5 Ren) |
| Ahoge | Optional nhỏ |

---

## Outfit manifest — HIMA futuristic school uniform

| Slot | Item | Story read |
|------|------|------------|
| Blazer | Đen, viền trắng piping | Đồng phục Nhật hiện đại |
| Crest | **HIMA logo baked in gen** ngực trái — lapel che cạnh trong (~15–20%) | Same rule as Charlotte v6 |
| Shirt | Trắng, hơi nhăn | Formal nhưng không cứng |
| Tie | Đỏ maroon đậm | Accent ấm duy nhất trên upper body |
| Vest | Xám charcoal, khuy cài | Layer Yi Sang |
| Outer | Áo khoác charcoal **khoác vai** (cape), viền cyan + **glitch pixel hem** | Fractured motif — full only |
| Bottom | Quần tây đen slim | School formal |
| Feet | Giày tây đen bóng | HIMA dress code |
| Hands | Găng fingerless đen (manifest: **trái**; gen v4 cả hai — accepted drift) |

**Uniform-only variant:** same blazer/vest/tie — **no cape, no violin**.

**Forbidden:** idol costume, full tactical Cadence armor, súng trên violin, Pillow re-composite logo.

---

## Prop manifest — Off-Beat Violin (full only)

| Part | Spec |
|------|------|
| Body | Matte black, silver filigree trim |
| Neck/scroll | **Traditional violin** — không barrel, không grip |
| Strings + f-holes | Cyan glow `#22D3EE` / `#67E8F9` |
| Glitch | Pixel-cube fracture cạnh dưới thân |
| Text | **"True chord"** serif trắng trên thân (upper bout) |
| Bow | Thân tối, hair bow = một line cyan glow |

---

## Color script (60 / 30 / 10)

| Role | Hex | % | Emotion |
|------|-----|---|---------|
| Black cloth | `#161A22` / `#1A1A1A` | ~45% | Discipline, indie underdog |
| Grey vest/coat | `#363C48` | ~15% | Yi Sang layer |
| Skin | `#F0D5BC` pale | ~12% | Human world grounded |
| Maroon tie | `#6B2030` | ~5% | Warm accent |
| Cyan fracture | `#22D3EE` | ~8% | Off-Beat / hears cracks |
| HIMA crest | blue-purple + gold border | ~5% | Academy identity |

---

## Chibi lock

- MapleStory ratio: head ~42%, compact limbs
- **Same HIMA uniform manifest as full** — crest on left breast by design (Charlotte v6 rule)
- **Canon pose (violin):** violin + tay che ngực trái → crest **không hiện**; không Pillow composite logo
- **Tư thế đứng khác:** khi ngực đọc được → gen/bake crest + lapel occlude ~15–20%
- Matte: `matte_chibi` trong `ren_v4_logo_gen_finalize.py` (checker blob cleanup)

---

## Engine import

- Canvas: **1024×682** RGBA
- PPU 100–128; pivot feet; Filter Bilinear (cel painted)

---

## QA v4 notes

| Check | Full | Uniform | Chibi |
|-------|------|---------|-------|
| HIMA crest baked in gen | ✓ | ✓ lapel occlude | manifest ✓ · occluded by pose |
| Face intact after matte | ✓ charlotte matte | ✓ | ✓ matte_chibi |
| Violin "True chord" | ✓ | n/a | ✓ |
| Cape glitch hem | ✓ | n/a | ✓ |
| Transparent alpha | ✓ | ✓ | ✓ |
| No Pillow logo composite | ✓ | ✓ | ✓ |

---

## Prompt template (v4 logo gen)

```text
Same Ren as v3 anatomy reference. Add HIMA circular crest left breast like Charlotte:
dark blue-purple gradient, white treble clef, lapel partially covers inner 15-20%.
Baked on blazer fabric — NOT floating sticker. 1024×682, checkerboard or clean export BG.
Cel-shade Persona 5 × Yi Sang. Full: cape cyan glitch hem, violin "True chord", cyan strings.
Uniform-only: no cape, no violin.
Negative: Pillow composite, holographic sticker logo, face corruption, gun violin parts.
```

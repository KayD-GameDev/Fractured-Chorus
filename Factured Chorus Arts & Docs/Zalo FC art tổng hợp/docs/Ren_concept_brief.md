# Ren — Human world concept v2 · **LOCKED CANON**

**Status:** LOCKED · xem `Ren_LOCK.txt`  
**Canon art:** `Ren_human_CANON_full_alpha.png` · `Ren_human_CANON_chibi_alpha.png`  
**Realm:** Thế giới thực — Lumina / HIMA  
**F drive:** `F:\Factured Chorus\art concept\Ren concept\human_world\`  
**Archive:** v1 · logo ref: `HIMA_logo_source.png`

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
| Crest | Patch **HIMA logo chính thức** (composite từ `HIMA_logo_source.png`) ngực trái | Harmonia International Music Academy |
| Shirt | Trắng, hơi nhăn | Formal nhưng không cứng |
| Tie | Đỏ maroon đậm | Accent ấm duy nhất trên upper body |
| Vest | Xám charcoal, khuy cài, D-ring bạc nhỏ | Layer Yi Sang |
| Outer | Áo khoác charcoal **khoác vai** (cape), viền cyan LED + **glitch pixel hem** | Fractured motif — thế giới thực |
| Bottom | Quần tây đen slim | School formal |
| Feet | Giày tây đen bóng | HIMA dress code |
| Hands | Găng fingerless đen (manifest: **trái**; gen v1 có thể cả hai — sửa v2 nếu cần) |

**Forbidden:** idol costume, full tactical Cadence armor, súng trên violin, rifle stock/barrel/grip.

---

## Prop manifest — Off-Beat Violin

Theo `Ren_Weapon_Notes.txt` + concept FINAL (không súng):

| Part | Spec |
|------|------|
| Body | Matte black, silver filigree trim |
| Neck/scroll | **Traditional violin** — không barrel, không grip |
| Strings + f-holes | Cyan glow `#22D3EE` / `#67E8F9` |
| Glitch | Pixel-cube fracture cạnh dưới thân |
| Emblem | Silver cross nhỏ tailpiece |
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
| HIMA crest | teal `#14B8A6` · purple `#8B5CF6` | ~5% | Academy identity |
| Silver metal | `#8F98AA` | ~5% | Trim violin + hardware |

---

## Chibi lock

- MapleStory ratio: head ~42%, compact limbs
- **Same manifest** as full — blazer crest, cape glitch hem, Off-Beat violin + bow
- Expression: calm cute, không smirk Coda

---

## Engine import

- Full: 1536×1024 source → scale PPU 100–128; pivot feet
- Chibi battle token: normalize 512×768 nếu vào combat UI (see `normalize_chibi_battle.py`)
- Filter: Bilinear (cel painted) hoặc Point nếu downscale pixel

---

## QA v2 notes

| Check | Full | Chibi |
|-------|------|-------|
| HIMA crest = logo gốc | ✓ composite | ✓ composite |
| Violin text "True chord" | ✓ | ✓ |
| School blazer + tie | ✓ | ✓ |
| Cape glitch hem | ✓ | ✓ |
| Violin no gun parts | ✓ | ✓ |
| Cyan strings/f-holes | ✓ | ✓ |
| Transparent alpha export | ✓ `_alpha` | ✓ `_alpha` |
| Fingerless glove left only | ⚠ both hands | ⚠ both hands |

---

## Prompt template (copy-paste)

```text
Full-body 2D anime, transparent background, Fractured Chorus Ren human world.
Young slim male HIMA music student, melancholic calm, heavy-lidded grey-blue eyes,
messy black bowl bangs + wavy sides. Black school blazer white piping, HIMA teal-purple
crest patch left breast, white shirt maroon tie, charcoal vest, charcoal cape on shoulders
cyan LED glitch pixel hem. Black trousers dress shoes. Off-Beat violin matte black silver
trim traditional neck scroll NO gun, cyan glowing strings f-holes glitch edge, bow cyan line.
Cel-shade key top-left. NO rifle stock grip barrel. NO idol costume.
```

Chibi: MapleStory 2.4-head, same manifest.

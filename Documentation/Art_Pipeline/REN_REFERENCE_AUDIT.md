# Ren — Character Reference Audit

**Phạm vi:** chỉ đọc project; không sửa `CHARACTER_LOCK.md`, Rules, Skills, asset, scene, prefab, script, `.meta`.  
**Nguồn:** `Assets/FracturedChorus/Art/Characters/Ren/CHARACTER_LOCK.md` (khai báo path), kiểm tra `Test-Path` + kích thước pixel qua `System.Drawing` (Windows).  
**Ngày:** 2026-11-06  

**Đọc kèm:** `README_CHARACTER_LOCK.md`, `.cursor/rules/character-art-lock.mdc`, `.cursor/rules/character-consistency.mdc`, `Documentation/Art_Pipeline/CHARACTER_ART_DIRECTION.md` (mục tiêu consistency; không lặp lại toàn văn).

---

## 1. Cách gắn path → workspace

Mọi path trong lock (trừ `.asset`) được hiểu là dưới:

`Assets/FracturedChorus/<path trong lock>`

Kết quả kiểm tra: **tất cả file bitmap/jpg/png** khai báo tồn tại.  
Khai báo **`Speaker_Ren.asset`** không có folder — file thực tế: `Assets/FracturedChorus/Data/ScriptableObjects/Narrative/Speakers/Speaker_Ren.asset` (xem bảng dưới).

---

## 2. Bảng trạng thái từng reference (theo CHARACTER_LOCK)

**Chú thích:** `READY` = file tồn tại, định dạng phù hợp role; `NEEDS_REVIEW` = cần art/AD xác nhận nội dung visual hoặc chỗ path không khớp lock; không suy đoán nội dung từ tên file.

| Nhóm | Path trong lock | Đường dẫn đầy đủ (workspace) | Tồn tại | Định dạng | Kích thước (px) | Trạng thái |
|------|-----------------|------------------------------|---------|-----------|-----------------|------------|
| School — full body | `Art/Characters/Ren/School/ren_hima_uniform_menu_fullbody_v1.png` | `Assets/FracturedChorus/Art/Characters/Ren/School/ren_hima_uniform_menu_fullbody_v1.png` | Có | PNG | 1024×1536 | READY |
| Past — full body | `Art/Characters/Ren/Past/ren_aoto_hs_fullbody_v1.png` | `Assets/FracturedChorus/Art/Characters/Ren/Past/ren_aoto_hs_fullbody_v1.png` | Có | PNG | 864×1152 | READY |
| Pair (Touya) | `Art/Characters/Touya/Past/touya_aoto_hs_fullbody_v1.png` | `Assets/FracturedChorus/Art/Characters/Touya/Past/touya_aoto_hs_fullbody_v1.png` | Có | PNG | 864×1152 | READY |
| Title | `Art/UI/TitleScreen/SheetV1/char_ren_title_pose_v1_alpha.png` | `Assets/FracturedChorus/Art/UI/TitleScreen/SheetV1/char_ren_title_pose_v1_alpha.png` | Có | PNG | 511×1533 | READY |
| Config | `Art/Characters/Ren/School/ren_config_pose_v1.png` | `Assets/FracturedChorus/Art/Characters/Ren/School/ren_config_pose_v1.png` | Có | PNG | 1024×1536 | READY |
| Config FX | `Art/Characters/Ren/School/ren_config_pose_fx_v1.png` | `Assets/FracturedChorus/Art/Characters/Ren/School/ren_config_pose_fx_v1.png` | Có | PNG | 1024×1536 | READY |
| VN default portrait | `Art/UI/Narrative/Portraits/ren_school_bust_neutral_v1.png` | `Assets/FracturedChorus/Art/UI/Narrative/Portraits/ren_school_bust_neutral_v1.png` | Có | PNG | 1024×1536 | READY |
| Expression set | `Art/Characters/Ren/VnBust/` (folder) | Cùng dưới `.../Ren/VnBust/` | Có (5 PNG) | PNG | mỗi file 1024×1536 | READY |
| Combat semichibi **source** | `Art/Characters/Ren/Battle/ren_combat_battle_idle_semichibi_v2_full.png` | `Assets/FracturedChorus/Art/Characters/Ren/Battle/ren_combat_battle_idle_semichibi_v2_full.png` | Có | PNG | **864×1152** (khớp lock / SoT lead) | READY |
| Combat semichibi *(history)* | `.../ren_combat_battle_idle_semichibi_v3.png` | `Assets/FracturedChorus/Art/Characters/Ren/Battle/ren_combat_battle_idle_semichibi_v3.png` | Có | PNG | 470×1109 | READY — chỉ reference |
| Combat HD alt | `Art/Characters/Ren/Battle/ren_combat_battle_idle_v2.png` | `Assets/FracturedChorus/Art/Characters/Ren/Battle/ren_combat_battle_idle_v2.png` | Có | PNG | 864×1152 | READY |
| Party card portrait | `Art/UI/Combat/Characters/Avatars/ren_party_avatar_v1.png` | `Assets/FracturedChorus/Art/UI/Combat/Characters/Avatars/ren_party_avatar_v1.png` | Có | PNG | 1005×1513 | READY |
| Weapon concept | `Art/Characters/Ren/Combat/ren_weapon_violin_rifle_concept_v1.jpg` | `Assets/FracturedChorus/Art/Characters/Ren/Combat/ren_weapon_violin_rifle_concept_v1.jpg` | Có | JPG | 1024×682 | READY |
| Skill icons | `Art/UI/Skills/Ren/ren_skill_icon_strike_v2.png` | `Assets/FracturedChorus/Art/UI/Skills/Ren/ren_skill_icon_strike_v2.png` | Có | PNG | 1024×1024 | READY |
| Skill icons | `.../ren_skill_icon_crosscut_v2.png` | `Assets/FracturedChorus/Art/UI/Skills/Ren/ren_skill_icon_crosscut_v2.png` | Có | PNG | 1024×1024 | READY |
| Skill icons | `.../ren_skill_icon_finale_v2.png` | `Assets/FracturedChorus/Art/UI/Skills/Ren/ren_skill_icon_finale_v2.png` | Có | PNG | 1024×1024 | READY |
| Speaker | `Speaker_Ren.asset` (lock chỉ tên file) | Thực tế: `Assets/FracturedChorus/Data/ScriptableObjects/Narrative/Speakers/Speaker_Ren.asset` | Có | `.asset` | N/A (YAML) | NEEDS_REVIEW — path trong lock không phải path Unity đầy đủ; không phải ảnh reference |

### 2.1 Chi tiết folder Expression (`Art/Characters/Ren/VnBust/`)

File tìm thấy (khớp bảng `Expressions` trong lock):

| File | Kích thước |
|------|------------|
| `ren_bust_neutral_v1.png` | 1024×1536 |
| `ren_bust_startled_v1.png` | 1024×1536 |
| `ren_bust_smile_v1.png` | 1024×1536 |
| `ren_bust_curious_v1.png` | 1024×1536 |
| `ren_bust_annoyed_v1.png` | 1024×1536 |

**MISSING:** không — đủ 5 Id (`neutral`, `startled`, `smile`, `curious`, `annoyed`).

### 2.2 Thư mục chỉ nêu nội dung chứ không liệt kê file đơn

- **`Art/UI/Combat/PartyCard/`** (chrome chung): thư mục **tồn tại** — không đánh giá từng asset Ren trong audit path-by-path (lock không ghi file cụ thể ngoài avatar ở trên).

---

## 3. Đánh giá theo nhóm (READY / PARTIAL / MISSING / NEEDS_REVIEW)

| Nhóm | Kết luận | Ghi chú |
|------|----------|--------|
| **Face / Portrait** | **READY** | Default portrait + 5 bust VN + party avatar đều có; canvas bust 1024×1536 thống nhất (trừ party avatar crop khác — đúng vai trò card). |
| **Full Body** | **READY** | School + Past có file; Touya pair có (so khớp scene flashback). |
| **School Outfit** | **READY** | Full body school + config + VN + combat HD path khớp school lock (xác minh visual: NEEDS_REVIEW cho AD). |
| **Past Outfit** | **READY** | `ren_aoto_hs_fullbody_v1.png` tồn tại. |
| **Title / Config** | **READY** | Title + config clean + FX đều có. |
| **Combat Semichibi** | **READY** | **Source:** `semichibi_v2_full` **864×1152** (lock + lead). **History/ref:** v3 (470×1109), `semichibi_v2.png` (crop). |
| **Weapon Violin Rifle** | **READY** (concept) | Một sheet JPG concept — đủ cho silhouette/material lock trong CHARACTER_LOCK; **PARTIAL** nếu pipeline cần thêm shot riêng in-game ortho (chưa khai báo trong lock). |
| **Expressions** | **READY** | Đủ 5 expression theo bảng lock. |

**Tổng thể cho AI consistency:** hệ path trong lock **đầy đủ file trên đĩa**; độ tin cậy “nhất quán khi gen” phụ thuộc **việc prompt gắn đúng path + AD review** — không đánh giá pixel trong audit này. Toàn nhóm **NEEDS_REVIEW** ở mức “nội dung ảnh có đúng identity” là trách nhiệm người (không đọc ảnh).

---

## 4. Đề xuất

### 4.1 Dùng ngay (lam reference cho AI / artist)

- Full body school: `ren_hima_uniform_menu_fullbody_v1.png`
- Bust default + set VnBust (5) + `ren_school_bust_neutral_v1.png`
- Past: `ren_aoto_hs_fullbody_v1.png`
- Title / Config: ba file title + config đã liệt kê
- Combat: **source** `ren_combat_battle_idle_semichibi_v2_full.png` + HD alt `ren_combat_battle_idle_v2.png`; history: `semichibi_v2.png`, `semichibi_v3.png`
- Weapon concept: `ren_weapon_violin_rifle_concept_v1.jpg`
- Skill icons (silhouette + aura cues)

### 4.2 Bổ sung (ưu tiên nếu muốn giảm drift)

| Hạng mục | Gợi ý | Lý do |
|----------|--------|--------|
| Character sheet thống nhất | Một trang turnaround (front/side/back) + sheet expression — xem `CHARACTER_ART_DIRECTION.md` §4 | Lock có nhiều shot rời; sheet một trang giúp AI anchor tỉ lệ. |
| Weapon | Thêm ortho/in-game idle weapon held (PNG) nếu combat cần khớp 3D-space | Lock hiện chỉ có concept JPG. |
| Lock path | Chuẩn hóa dòng `Speaker_Ren.asset` → full path hoặc “bắt buộc không phải texture ref” | Tránh hiểu nhầm là ảnh. |

### 4.3 Xác nhận thủ công (NEEDS_REVIEW)

- **Mọi** file trên: AD xác nhận “khớp grey eyes / SyncPod / cấm headphone school / v.v.”
- **Speaker asset:** chỉ là data narrative — không dùng làm visual ref.
- **`Battle/`:** một số PNG có thể thiếu `.meta` trong repo (đã ghi trong audit pipeline trước) — trước khi ship cần Unity refresh + commit meta để GUID ổn định (không sửa trong task này).

### 4.4 Ưu tiên Character Sheet (nếu chỉ làm một batch)

1. **School full body + bust neutral + 1 expression** — nền cho mọi shot.  
2. **Turnaround school** (chưa có sheet đơn; ghép từ nhiều file hoặc vẽ mới).  
3. **Violin Rifle** — sheet ortho cạnh concept.  
4. Past / Title / Config — sau khi school đã khóa.

---

## 5. Kết luận ngắn

- **Path coverage:** không có file **MISSING** trong `CHARACTER_LOCK.md` cho ảnh (PNG/JPG) đã kiểm tra.  
- **Speaker:** khai báo path **không** trỏ tới file ảnh — ghi **NEEDS_REVIEW** về documentation, không phải thiếu asset.  
- **Đủ reference để AI có “điểm neo” nhất quán:** **có** — số lượng file và đa góc (school, past, title, config, VN, combat, weapon) **READY** về mặt tồn tại; chất lượng consistency cần **review** thủ công và optional **character sheet** tổng hợp.

---

*Báo cáo hoàn tất. Chờ xác nhận của anh trước khi chỉnh lock hoặc thêm asset.*

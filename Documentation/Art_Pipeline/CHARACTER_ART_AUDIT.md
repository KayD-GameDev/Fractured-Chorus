# Fractured Chorus — Character Art Pipeline Audit

**Loại báo cáo:** chỉ đọc repo.  
**Ngày:** 2026-11-06  
**Trọng tâm:** character assets, animation liên quan party, hệ **Character Consistency**.

---

## 1. Hiện trạng project (character scope)

### 1.1 Unity & pipeline

| Mục | Giá trị |
|-----|---------|
| Unity | **6000.4.0f1** (`ProjectSettings/ProjectVersion.txt`) |
| SRP | **URP 17.4.0** (`Packages/manifest.json`) |
| 2D packages | `2d.animation`, `2d.psdimporter`, `2d.aseprite`, `2d.sprite`, tilemap, spriteshape (`Packages/manifest.json`) |

### 1.2 Cấu trúc `Art/Characters`

**Folder con cấp một đã liệt kê (PowerShell `Get-ChildItem -Directory`):**

`_Reference`, `Astra`, `Charlotte`, `Coda`, `Elite 1`, `Elite 2`, `Elite 3`, `Enemy 1`, `Enemy 2`, `Enemy 3`, `Haruto`, `Kiki`, `KikiUeda`, `KnightOfDespair`, `MeiLin`, `Mimi`, `Mini Enemy`, `Ren`, `Ryo`, `Touya`.

### 1.3 Party combat (canonical)

Theo `Art/Characters/README.md`:

| Role | Canon ID | Folder | Preset (ghi trong README) |
|------|----------|--------|---------------------------|
| Mage | Coda | `Coda/` | `UnitPreset_Mage` |
| DPS | Ren | `Ren/` | `UnitPreset_Ren` |
| Tank | Charlotte | `Charlotte/` | `UnitPreset_Tank` |

### 1.4 CHARACTER_LOCK

**Có lock:** Ren, Charlotte, Coda, Mimi, Kiki, Astra, Touya, KikiUeda, MeiLin, Ryo, Haruto (paths: `*/CHARACTER_LOCK.md`).

**Template:** `Assets/FracturedChorus/Art/Characters/README_CHARACTER_LOCK.md`.

**Cursor:** `.cursor/rules/character-art-lock.mdc` — bust **1024×1536**, PNG alpha sạch; không tạo lock trùng.

---

## 2. Danh sách character assets (tóm tắt có bằng chứng)

### 2.1 Nhân vật có CHARACTER_LOCK — gói asset chính (từ lock)

| ID | Định dạng / gói chính | Kích thước / ghi chú đã xác minh |
|----|------------------------|-----------------------------------|
| **Ren** | PNG bust, PNG battle idle, JPG weapon concept, paths UI/combat | Semi-chibi combat SoT **`ren_combat_battle_idle_semichibi_v2_full.png` — 864×1152** (`Ren/CHARACTER_LOCK.md`; lead 2026-11); subfolder `Animation/`, `Battle/`, `School/`, `Past/`, `VnBust/`, `Combat/` |
| **Charlotte** | PNG school fullbody, VN busts (neutral, startled, curious, grim) | Bust framing **1024×1536** (lock); cấm badge/styling Limbus trong lock |
| **Coda** | PNG school + Cadence fullbody, Chibi, VN bust, combat icon | Hai outfit (school vs Cadence) cùng face lock; chibi canvas **1024×1536** cho một asset (lock) |

**Các ID còn lại (Mimi, Kiki, …):** có `CHARACTER_LOCK.md` — báo cáo không trích full từng dòng; khi sản xuất cần mở từng file lock.

### 2.2 File ref / temporary (bằng chứng tên file)

| Pattern | Số lượng (trong `Art/Characters`) | Ví dụ path |
|---------|-----------------------------------|------------|
| `LCB_*` | 24 | `Charlotte/LCB_Sinner_Ishmael_*.png`, `Ren/LCB_Sinner_Yi_Sang_Dead_Sprite.png` |
| `N_Corp*` | 172 | `Ren/Animation/Ren - Idle/N_Corp_*.psd`, `Ren/N_Corp._E.G.O_*_Sprite.png` |
| `Lobotomy_*` | 188 | `Coda/Animation/Coda - Idle/Lobotomy_E_*.psd`, `Lobotomy_E.G.O_..._Idle_Sprite.png` |

README gọi `LCB_*` / `N_Corp_*` là **sprite ref LoR**, không gán preset canon — dùng làm bằng chứng **cần thay**.

### 2.3 `_Reference`

`Assets/FracturedChorus/Art/Characters/_Reference/LuxeConcert/` — `kiki_ref.png`, `mimi_ref.png`, `astra_ref.png`.

### 2.4 Animation artifacts

- **Animator:** `Ren/Animation/Ren - Idle/Unit_Ren.controller`, `Coda/Animation/Coda - Idle/Unit_Mage.controller`, `Charlotte/Animation/_CharlottController.controller` (và nhiều controller enemy/boss — xem glob `**/*.controller`).
- **Clip:** 61 file `.anim` dưới `Art/Characters`.
- **Rig 2D (SpriteSkin / Sprite Library):** không tìm thấy chuỗi tương ứng trong `Assets/**/*.meta` (grep). Kết luận: **chưa xác minh** skeletal rig đang active; clip hiện gắn với workflow sprite/PSD layer.

### 2.5 `.meta` và PNG Battle (Ren)

Một số file PNG dưới `Ren/Battle/` **không** có `.meta` đi kèm trong workspace — có thể do môi trường chưa import hoặc meta chưa commit. Kích thường pixel **không** đọc được từ meta cho các file đó trong audit này; SoT idle semi-chibi combat theo **`CHARACTER_LOCK.md`:** **864×1152** (`v2_full`). *(Báo cáo gốc trước đó dùng 470×1109 — superseded.)*

---

## 3. Asset cần thay thế & ưu tiên (character)

| Ưu tiên | Mục | Lý do |
|---------|-----|--------|
| **P0** | Toàn bộ **`N_Corp_*`** trong `Ren/Animation/` và PNG `N_Corp._E.G.O_*` | README + Ren lock: ref LoR / placeholder cho combat anim |
| **P0** | **`Lobotomy_*`** PSD/sprite **Coda** | Cùng họ ref tạm; trùng naming convention ngoài IP |
| **P1** | **`LCB_*`** Charlotte + Ren dead | Tên LCB; README xếp vào ref LoR |
| **P1** | Enemy / Elite / Mini / Knight — sprite idle + controller | Tên asset trỏ tới IP khác → rủi ro nhận diện/bản quyền khi ship |
| **P2** | `_Reference/LuxeConcert/*` | Concept ref — xác định in-game vs moodboard |

---

## 4. Công cụ — character production

| Công cụ | Vai trò character | Ghi chú |
|---------|-------------------|---------|
| **CHARACTER_LOCK.md** | Source of truth nhận diện | Đã dùng; bắt buộc trước mọi gen |
| **PSD Importer** | Layer animation (hiện trạng Ren/Coda folders) | Package có |
| **2d.animation** | Skinning 2D nếu chuyển skeletal | Chưa thấy SpriteSkin trong meta |
| **Aseprite** (app) + importer | Sheet + timing | App trả phí |
| **Krita / PS** | Clean alpha, edit ref | PS trả phí; Krita miễn phí |
| **Spine** | Skeletal premium | **Không** trong `manifest.json` |
| **Cursor GenerateImage** | Concept / bust iteration | Luôn đính kèm lock + ref path |

---

## 5. Quy trình sản xuất nhân vật đề xuất

**Chuỗi chuẩn:**

`Character Brief` → `Character Concept` → `Character Reference Sheet` → `Character Portrait` → `Layer Separation (PSD / tags)` → `Animation Production` → `Unity Integration`.

- **AI:** concept, portrait đầu, variation nhỏ (khi đã có ref sheet).
- **Thủ công:** chốt silhouette, palette, expression keys, clean alpha, in-between.
- **Script:** chỉ hỗ trợ batch rename / import presets — không thay layout UI trong scene runtime (theo quy ước dự án).

---

## 6. Đề xuất Cursor Rules & Skills (đã / sẽ gắn project)

**Đã có:** `.cursor/rules/character-art-lock.mdc`.

**Đề xuất thêm (đã implement trong repo cùng phase này):**

- `fracture-chorus-art-style.mdc` — palette Lumina, hologram/neon, hạn chế magenta.
- `character-consistency.mdc` — workflow đọc lock + `_Refs` + checklist.
- `2d-asset-standards.mdc` — định dạng, naming, độ phân giải.
- `animation-standards.mdc` — frame vs skeletal, naming clip.
- `unity-2d-pipeline.mdc` — import URP, Sprite, giữ `.meta`.

**Skills (project):** `.cursor/skills/<workflow>/SKILL.md` — `character-design`, `character-consistency`, `concept-art`, `background-art`, `ui-asset-generation`, `sprite-sheet-generation`, `frame-by-frame-animation`, `2d-skeletal-animation`, `unity-2d-integration`.

---

## 7. Vấn đề kỹ thuật trước khi sản xuất mass

1. **Đồng bộ `.meta`:** đảm bảo mọi PNG/PSD mới có `.meta` commit để GUID ổn định khi thay sprite trong prefab.
2. **Loại bỏ ref tạm:** giảm rủi ro bản quyền và drift style — ưu tiên `N_Corp_*`, `Lobotomy_*`, `LCB_*` party.
3. **Quyết định skeletal:** Unity 2D Animation vs Spine — ảnh hưởng export và animator.
4. **`docs/ASSET_INVENTORY.md`:** README nhắc nhưng **không có** trong workspace — nên khôi phục hoặc trỏ sang `Documentation/Art_Pipeline/`.
5. **Kiểm chứng trong Unity:** mapping Scene → SpriteRenderer/Animator cho từng hero — chưa exhaustive trong audit chỉ đọc.

---

**Kết thúc báo cáo character.** Chờ xác nhận trước khi thay asset hoặc chỉnh scene.

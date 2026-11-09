# Fractured Chorus — Character Art Direction System

**Vai trò tài liệu:** quy chuẩn Art Direction cho **toàn bộ nhân vật**, bổ sung cho `CHARACTER_LOCK.md` từng người và hệ Rules/Skills trong `.cursor/`.  
**Source of truth cho identity từng nhân vật:** `Assets/FracturedChorus/Art/Characters/<Name>/CHARACTER_LOCK.md` — **ưu tiên lock** khi palette / phụ kiện / tỷ lệ mâu thuẫn palette thế giới.  
**Ngày:** 2026-11-06  

---

## 1. Art Direction tổng thể

### 1.1 Phong cách

| Trục | Định hướng |
|------|------------|
| Thể loại | Anime 2D / JRPG |
| Tham khảo tâm lý (không sao chép asset) | Caligula Effect 2, Persona — mood UI/đô thị, không nhân bản nhận diện có bản quyền |
| Thế giới | Lumina: đô thị hiện đại, chân thực; hologram và neon **có chừng mực** |
| Giới hạn | Tránh **quá cyberpunk / sci-fi**; tránh bão hòa màu toàn khung |

### 1.2 Nhân vật

- Thiết kế **hiện đại, cá tính, nhận diện rõ** (silhouette đọc được ở scale UI/combat).
- Tone diễn mặt theo **CHARACTER_LOCK** (ví dụ Ren: restrained, “ngầu”, không cartoon exaggeration).

### 1.3 Palette thế giới (directional — không áp máy móc lên từng nhân vật)

| Màu | Hex | Vai trò |
|-----|-----|--------|
| Nền / độ sâu | `#0B1220` | Shadow, không gian, BG tone |
| Neon chính | `#00D4FF` | Accent, hologram, rim hợp cảnh |
| Neon phụ | `#8CD9FF` | Highlight, glow nhẹ |
| Vàng nhấn | `#C9A84E` | Badge, pin, chi tiết kim loại chọn lọc |

**Quy tắc ưu tiên:**

1. **CHARACTER_LOCK** (màu áo, mắt, prop, hex ghi trong lock) quyết định nhận diện nhân vật.  
2. Palette thế giới dùng để **hòa cảnh** (BG, FX holo, UI chrome), không thay màu mắt / tóc / đồng phục đã khóa.  
3. Hạn chế **hồng / magenta** như màu chủ đạo khung — trừ khi lock hoặc brief (ví dụ weapon/VFX của Ren có violet/purple theo lock) chỉ định.

### 1.4 Đối chiếu Rules hiện có

Nội dung §1.1–1.3 đã được **codify** một phần trong [`.cursor/rules/fracture-chorus-art-style.mdc`](../../.cursor/rules/fracture-chorus-art-style.mdc) (globs `Assets/FracturedChorus/Art/**`).

---

## 2. Character Art Standard

Chuẩn dưới đây là **framework** áp dụng mọi nhân vật; **số đo cụ thể** (độ tuổi đọc, head count, màu mắt) lấy từ CHARACTER_LOCK.

### A. Face Design

| Hạng mục | Quy chuẩn |
|----------|-----------|
| **Face proportions** | Anime JRPG hiện đại: trục mắt–mũi–miệng ổn định theo nhân vật; một nhân vật giữ **cùng tỉ lệ** qua bust / full body / semi-chibi (semi-chibi chỉ được phép **đầu lớn hơn có kiểm soát** nếu lock ghi, ví dụ Ren combat semichibi). |
| **Eye design** | Màu và highlight: **theo lock** (Ren: grey irises, không đỏ/không xanh sáng/không glow mắt trừ khi lock đổi). Hai mắt cùng kích thước cơ bản; glare tối giản nếu tone là cool/restrained. |
| **Nose and mouth** | Mũi gợi nhẹ (cel anime); miệng: line rõ khi nói/expression; không caricature trừ khi lock cho phép. |
| **Hairline and hairstyle** | Chu vi tóc, độ dài, lớp layer tóc — **đúng lock** (Ren: messy layered black, bangs). Không đổi kiểu giữa cùng outfit canon trừ brief thay đổi design đã chốt và cập nhật lock. |
| **Facial expression** | Chỉ dùng expression Id có trong lock (Ren: `neutral`, `startled`, `smile`, `curious`, `annoyed` + tone ngầu). Thay đổi SyncPod (xanh/đỏ) theo rule trong [character-art-lock.mdc](../../.cursor/rules/character-art-lock.mdc) và từng lock nhân vật. |

### B. Body Design

| Hạng mục | Quy chuẩn |
|----------|-----------|
| **Body proportions** | Full body school/menu: họ “JRPG polish” — một số lock ghi **~7.5–8.5 heads** (tham chiếu cross-party trong lock Coda/Charlotte); **luôn đối chiếu CHARACTER_LOCK** của nhân vật đang vẽ. |
| **Silhouette** | Đọc được vai/cánh tay/chân và prop chính (SyncPod, vũ khí silhouette) ở scale nhỏ. |
| **Pose language** | Phù hợp vai trò (tank planted, DPS linh hoạt…) trong biên lock; pose title/config của Ren **đã khóa path + prop** — không tự đổi găng/tay/pose đã mô tả. |
| **Anatomy consistency** | Cùng một nhân vật: chiều dài tay/chân và vai không drift giữa các shot; semi-chibi chỉ là biến thể đã được lock phê duyệt. |

### C. Costume Design

| Hạng mục | Quy chuẩn |
|----------|-----------|
| **Main costume** | Lớp ngoài → trong theo bullet trong lock (Ren school: blazer, trim cyan, sơ mi, cà vạt waveform, pin nốt nhạc, SyncPod). |
| **Secondary costume** | Outfit phụ / flashback / thế giới khác: section riêng trong lock (Ren Past Aoto: `#3A3D45` blazer, cà vạt đen plain — **cấm** pin, SyncPod, trim cyan). |
| **Accessories** | Mỗi món (pin, SyncPod, ribbon…) có mô tả lock; **Forbidden** trong Ren lock (không headphone over-ear school combat; title gloves chỉ title keyart). |
| **Material and palette** | Vải vs da vs kim loại: matte/blazer, sheer stockings (nhân vật khác), glow chỉ trên prop phù hợp lock. Hex áo/tóc/mắt lấy từ lock, không gán palette thế giới lên áo đã khóa. |

### D. Illustration

| Hạng mục | Quy chuẩn |
|----------|-----------|
| **Line art** | Cel clean, có thể rim nhẹ; không jitter random — phù hợp JRPG ship art. |
| **Shading** | Cell shade + core shadow rõ; tránh noise quá đặc. |
| **Lighting** | Key + rim cyan/ambient theo cảnh; không wash magenta toàn da. |
| **Rendering** | Đọc được ở kích thước thực tế (bust slot ~PreserveAspect 440×600 class); không over-render vụn không thấy in game. |
| **Edge quality** | Alpha sạch cho PNG ship; không fringe sáng quanh silhouette. |
| **Transparent background** | Bust/sprite gameplay: **PNG alpha** (theo [README_CHARACTER_LOCK.md](../../Assets/FracturedChorus/Art/Characters/README_CHARACTER_LOCK.md), [character-art-lock.mdc](../../.cursor/rules/character-art-lock.mdc)). |

### E. Character Sheet (deliverable nghệ thuật)

| Sheet | Mục đích |
|-------|-----------|
| **Front / Side / Back** | Khóa silhouette, độ dài tóc, góc vai, chi tiết lưng (nơ, túi, SyncPod). |
| **Expression sheet** | Toàn bộ Id trong lock + khớp tone diễn. |
| **Pose sheet** | Vài pose đại diện vai trò (idle, combat idle, social) không mâu thuẫn lock. |
| **Costume sheet** | Callout layer + swatch màu hex từ lock + material notes. |

Đường dẫn file cụ thể do lock hoặc AD chỉ định; **Ren không tạo lock mới** — chỉ thêm ref vào sheet nếu được giao việc và cập nhật path trong lock **chỉ khi user/design chỉ định** (không thuộc scope tài liệu này).

---

## 3. Character Consistency Rules

**Hệ hiện có:** một `CHARACTER_LOCK.md` / nhân vật; [README_CHARACTER_LOCK.md](../../Assets/FracturedChorus/Art/Characters/README_CHARACTER_LOCK.md); [.cursor/rules/character-art-lock.mdc](../../.cursor/rules/character-art-lock.mdc); [.cursor/rules/character-consistency.mdc](../../.cursor/rules/character-consistency.mdc).

### 3.1 Quy tắc bắt buộc cho AI / nghệ sĩ

| # | Quy tắc |
|---|--------|
| 1 | Trước khi gen hoặc vẽ: **đọc đầy đủ** `CHARACTER_LOCK.md` của nhân vật đó. |
| 2 | **Tham chiếu ảnh chuẩn** tại các path trong lock (ví dụ Ren: full body school, bust neutral, expression folder). |
| 3 | **Không tự ý** thay đổi: khuôn mặt cấu trúc, **màu mắt**, kiểu tóc, **màu tóc**, tỷ lệ cơ thể (trong cùng tier art: bust vs full), trang phục canon, phụ kiện nhận diện. |
| 4 | Nếu brief mới **mâu thuẫn** lock (ví dụ “mắt xanh” cho Ren): **dừng và hỏi lại** người chỉ đạo — không chỉnh lock hay design trong phiên làm việc không có quyết định chính thức. |
| 5 | Sau khi có asset path mới: **cập nhật lock** theo quy trình dự án (do user/design); **giữ `.meta` GUID** khi thay file đã có reference trong Unity ([character-art-lock.mdc](../../.cursor/rules/character-art-lock.mdc)). |
| 6 | Nhân vật mới: **một lock duy nhất** từ đầu; không nhân bản file lock ([README_CHARACTER_LOCK.md](../../Assets/FracturedChorus/Art/Characters/README_CHARACTER_LOCK.md)). |

### 3.2 Trường hợp Ren (đồng bộ lock hiện tại)

- **Không** tạo thêm CHARACTER_LOCK cho Ren.  
- Mọi output phải thỏa: grey eyes, messy black hair, pale skin, HIMA uniform school, SyncPod (blue waveform school), cấm over-ear headphones trong school/combat semichibi như lock ghi; title/config/title gloves theo đúng section trong [Ren CHARACTER_LOCK.md](../../Assets/FracturedChorus/Art/Characters/Ren/CHARACTER_LOCK.md).

---

## 4. Quy chuẩn Character Sheet

**Mục tiêu:** một trang (hoặc file PSD) có thể dùng làm ref nội bộ và cho AI lần sau, **không** thay thế text lock.

| Thành phần | Yêu cầu tối thiểu |
|------------|-------------------|
| **Turnaround** | Front, ¾ (nếu là góc chính game), side, back — thống nhất scale (vd cùng chiều cao đầu). |
| **Expressions** | Ít nhất full set Id trong lock; đồng nhất kích thước đầu/cổ. |
| **Poses** | Ít nhất neutral stance + 1 combat-read hoặc social theo brief. |
| **Costume** | Callout: blazer, cổ, cà vạt, pin, SyncPod, giày; màu hex từ lock. |
| **Notes** | Forbidden list rút từ lock (copy ngắn gọn). |

Layout cụ thể (grid A4 vs canvas 4K) do pipeline chọn; chưa có rule riêng **grid expression bắt buộc 3×3** trong repo — xem §6.

---

## 5. Quy chuẩn ảnh đầu ra cho Unity

Tổng hợp từ [README_CHARACTER_LOCK.md](../../Assets/FracturedChorus/Art/Characters/README_CHARACTER_LOCK.md), [`.cursor/rules/2d-asset-standards.mdc`](../../.cursor/rules/2d-asset-standards.mdc), [`.cursor/rules/unity-2d-pipeline.mdc`](../../.cursor/rules/unity-2d-pipeline.mdc).

| Loại | Đặc tả |
|------|--------|
| **VN bust** | Ưu tiên **1024×1536** PNG transparent; padding L/R ~8–12%; không opaque silhouette chạm mép ngang; alpha sạch. |
| **Combat static** | Kích thước theo lock (Ren semichibi source **864×1152**, `ren_combat_battle_idle_semichibi_v2_full.png`). |
| **Định dạng ship** | PNG (UI/sprite); PSD cho layer anim (PSD Importer). |
| **Naming** | `{scope}_{subject}_{variant}_v{major}.png` (2d-asset-standards). |
| **Import** | Giữ `.meta`/GUID khi replace; không hardcode RectTransform layout runtime cho UI đã author trong scene (unity-2d-pipeline). |
| **Sprite sheet** | Grid + pivot + padding thống nhất; ghi trong brief/lock. |

---

## 6. Nội dung Rules / Skills còn thiếu (chỉ đề xuất — không tạo/sửa file trong task này)

Đối chiếu với: [`.cursor/rules/`](../../.cursor/rules/) (`fracture-chorus-art-style`, `character-consistency`, `character-art-lock`, `2d-asset-standards`, `animation-standards`, `unity-2d-pipeline`) và [`.cursor/skills/`](../../.cursor/skills/) (`character-design`, `character-consistency`, `concept-art`, `background-art`, `ui-asset-generation`, `sprite-sheet-generation`, `frame-by-frame-animation`, `2d-skeletal-animation`, `unity-2d-integration`).

| Khoảng trống | Gợi ý bổ sung (tài liệu hoặc rule sau này) |
|--------------|-------------------------------------------|
| **Face anatomy số hóa mặc định** | Bảng tham chiếu anime (khoảng cách mắt, chiều cao tai) *optional* — hiện chỉ có head-count chéo trong vài lock, chưa có global default trong rule. |
| **Expression sheet grid** | Số hàng/cột chuẩn (vd 2×3 vs 3×3) và margin — chưa có trong rule. |
| **Turnaround góc cố định** | Side = pure profile hay 45° — chưa có một dòng chuẩn world (chỉ mô tả trong báo cáo này). |
| **Line weight / brush** | Độ đậm line theo resolution export — chưa có trong rule. |
| **Hair layer convention** | Thứ tự layer (sau tai / trước tai) trong PSD — có trong practice nhưng chưa codify. |
| **Chibi vs full-body cross-check** | Checklist so khớp đặc điểm nhận diện khi cùng nhân vật có nhiều tier (semichibi vs full) — skill `character-consistency` có checklist chung; có thể bổ sung bullet “tier alignment” cụ thể. |
| **Prompt library path** | `Documentation/Art_Pipeline/prompts/` được audit gợi ý trước đây — **chưa** thấy folder trong repo tại thời điểm báo cáo. |

**Không phát hiện file rule/skill trùng tên** với báo cáo này; hệ **Character Consistency** đã có lớp rule + skill `character-consistency`.

---

## 7. Đề xuất áp dụng thử nghiệm cho Ren

**Mục tiêu:** củng cố nhận diện Ren trong mọi pipeline (VN → combat static → key art) mà **không** đổi thiết kế đã lock.

| Bước | Việc | Đầu ra / ghi chú |
|------|------|-----------------|
| 1 | Rà soát ref paths trong [Ren/CHARACTER_LOCK.md](../../Assets/FracturedChorus/Art/Characters/Ren/CHARACTER_LOCK.md) — đảm bảo mỗi block (School, Past, Title, Config, Battle, VN bust) có ít nhất **một** file ref có thể mở trong repo. | Checklist path (không đổi design). |
| 2 | Thử nghiệm **một** Character Sheet nội bộ (front + ¾ + expression subset) chỉ dùng làm ref AI lần sau — không thay ship asset cho đến khi approv. | Theo §4. |
| 3 | Mọi prompt gen bust/expression: paste **Forbidden** + “grey eyes / black hair / SyncPod blue” từ lock. | Giảm drift màu mắt / tai nghe. |
| 4 | Combat semichibi: mọi bản chỉnh sửa bám **`ren_combat_battle_idle_semichibi_v2_full.png`** (v2/v3 khác chỉ history/ref) và bảng weapon Violin Rifle (purple energy) nếu có vũ khí trong khung. | Không pistol/generic violin. |
| 5 | Trước khi merge art: chạy checklist §3.1 + đối chiếu ảnh `ren_school_bust_neutral_v1.png` path default portrait. | Gate QA nội bộ. |

**Ren — không tạo lock mới;** mọi thay đổi identity chỉ sau khi lead/design cập nhật [CHARACTER_LOCK.md](../../Assets/FracturedChorus/Art/Characters/Ren/CHARACTER_LOCK.md) có chủ đích.

---

## Tài liệu tham chiếu nội bộ (đã đọc cho báo cáo này)

- [Assets/FracturedChorus/Art/Characters/README.md](../../Assets/FracturedChorus/Art/Characters/README.md)
- [Assets/FracturedChorus/Art/Characters/README_CHARACTER_LOCK.md](../../Assets/FracturedChorus/Art/Characters/README_CHARACTER_LOCK.md)
- [Assets/FracturedChorus/Art/Characters/Ren/CHARACTER_LOCK.md](../../Assets/FracturedChorus/Art/Characters/Ren/CHARACTER_LOCK.md)
- [.cursor/rules/character-art-lock.mdc](../../.cursor/rules/character-art-lock.mdc)
- [.cursor/rules/fracture-chorus-art-style.mdc](../../.cursor/rules/fracture-chorus-art-style.mdc)
- [.cursor/rules/character-consistency.mdc](../../.cursor/rules/character-consistency.mdc)
- [.cursor/rules/2d-asset-standards.mdc](../../.cursor/rules/2d-asset-standards.mdc)

---

*Báo cáo chỉ gồm tài liệu Markdown; không chỉnh sửa lock, rules, skills, asset, scene, prefab hoặc script. Chờ xác nhận của anh trước khi chỉnh sửa hoặc mở rộng hệ thống.*

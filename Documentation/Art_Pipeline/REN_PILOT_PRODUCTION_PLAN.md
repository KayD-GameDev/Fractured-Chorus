# Ren — Pilot Production Plan (Art & Animation)

**Loại:** kế hoạch pilot — chưa bắt buộc sản xuất asset; mọi thay đổi Unity/asset sau khi duyệt.  
**Cập nhật:** 2026-11-06  

**Tham chiếu:** [CHARACTER_LOCK.md](../../Assets/FracturedChorus/Art/Characters/Ren/CHARACTER_LOCK.md) · [CHARACTER_ART_DIRECTION.md](CHARACTER_ART_DIRECTION.md) · Rules `character-art-lock`, `unity-2d-pipeline` (`.cursor/rules/`)

---

## 1. Phân loại artwork

### A. Visual Novel / Story Artwork

Bao gồm:

- School outfit (HIMA)
- Past outfit (Aoto)
- VN bust và biểu cảm
- Fullbody (menu / school)
- Title pose
- Config pose (+ overlay FX nếu có)
- Narrative CG và hình phục vụ cốt truyện

**Đặc điểm**

- Artwork **tĩnh**; **không** thuộc Combat Animation Pipeline.
- Ưu tiên **chất lượng hình**, nhận diện nhân vật và **consistency** theo Character Lock.
- **Không bắt buộc** tách layer để rig — chỉ tách khi brief/đạo diễn yêu cầu.

### B. Combat Artwork

- **`ren_combat_battle_idle_semichibi_v2_full.png` (864×1152)** — theo lead + CHARACTER_LOCK — là **đối tượng sản xuất animation** của pilot này (idle và clip combat sau bám cùng SoT). **`semichibi_v2.png`** (crop) và **`semichibi_v3.png`** chỉ history/reference — không dùng làm base chỉnh sửa mới.
- Sprite phục vụ **hệ thống chiến đấu** (battle unit trên board / sandbox).
- Toàn bộ quy trình animation Ren trong pilot **tập trung vào nhóm này** — không dùng School fullbody làm nguồn animation pilot.

**Lưu ý**

- **v2_full** là nguồn cần kiểm tra trước khi sản xuất clip. **Không** lấy v3 hay bản crop `semichibi_v2.png` làm SoT; luôn đối chiếu file trên disk, (khi được phép) **UnitPreset_Ren**, và **CHARACTER_LOCK**.

---

## 2. Phân chia pipeline

### Visual Novel Pipeline

```
Character Lock
  → Artwork Brief
  → Concept / Artwork Generation
  → Character Consistency Review
  → User Approval
  → Export PNG
  → Unity Visual Novel Integration
```

- **Không** thêm bước rig hoặc animation combat trong luồng này.

### Combat Animation Pipeline

```
Character Lock
  → Combat Semichibi Source (**v2_full** — đã kiểm tra file)
  → Layer Separation / Animation Preparation
  → User Approval
  → Frame-by-Frame hoặc Skeletal Rig (Unity 2D Animation)
  → Animation Clips
  → Animator
  → Unity Combat Sandbox
  → User Approval
```

- FBF ưu tiên **trước** để chứng minh pipeline; skeletal chỉ khi pilot FBF cho thấy lợi ích rõ.

---

## 3. Pilot mới

Chỉ có **một** pilot animation:

### Ren Combat Semichibi (v2_full — SoT)

**Mục tiêu**

- Kiểm tra khả năng xử lý **artwork combat** (alpha, kích thước canvas, readability trên hex).
- Kiểm tra **chuẩn bị sprite** cho animation (layer / slice / pivot chân nếu có).
- Thử **FBF** trước (ít frame, loop idle).
- **Đánh giá** Unity 2D Animation / skeletal chỉ khi FBF và nguồn art cho phép lợi ích rõ ràng.

**Không** dùng School outfit (fullbody menu) làm animation pilot.

**Gợi ý sandbox (khi triển khai thật):** folder `Assets/FracturedChorus/Art/_Pilot/Ren/Combat/` hoặc scene sandbox duplicate — **không** wire `UnitPreset_Ren` production cho đến khi nghiệm thu.

---

## 4. Vấn đề cần xác minh

Dưới đây là trạng thái **đã đọc trong repo** (cần re-verify khi làm việc trong Unity):

| Hạng mục | Trạng thái |
|----------|------------|
| **SoT combat semichibi (lead + lock)** | **`ren_combat_battle_idle_semichibi_v2_full.png`** — **864×1152**. **`semichibi_v2.png`:** crop pipeline **554×1247** (script `export_battle_canvas`) — chỉ export/history. **`semichibi_v3.png`:** **470×1109** — history/ref. Pipeline tái xuất crop: `Tools/Art/ren_semichibi_v2_syncpod_wire.py` (input JPG green trong `.cursor/projects/d-Fractured-Chorus1/assets/`). |
| **v2 crop / v3** | Không tương đương SoT. Mọi chỉnh art mới chỉ trên **v2_full**. |
| **`UnitPreset_Ren.battleSprite`** | `Assets/FracturedChorus/Resources/UnitPresets/UnitPreset_Ren.asset` trỏ GUID `9a437a3dfde73224686f2d2a81ee930e` → **`N_Corp._E.G.O_Fell_Bullet_Yi_Sang_Idle_Sprite.png`** (placeholder ref ngoài), **chưa** trỏ FC semichibi — cập nhật sau QA (ngoài scope task đồng bộ doc này nếu user cấm sửa preset). |
| **CHARACTER_LOCK vs preset** | Lock đã chốt path/kích thước **v2_full** cho nguồn art; **preset Unity vẫn placeholder** → lệch vận hành in-game cho đến khi gán sprite (task sau). |
| **Animator / clip hiện tại** | `Unit_Ren.controller`, `Ren Idle.anim` vẫn gắn chuỗi **N_Corp / PSD** — thay khi pilot combat **v2_full** (SoT) được triển khai trong Unity. |

**Checklist trước khi bắt tay animation**

- [x] Chốt **một** file semichibi combat làm **single source** — **`v2_full`** (lead; lock đã đồng bộ).
- [ ] Duyệt consistency với **CHARACTER_LOCK** (SyncPod, không headphone trùm đầu, Violin Rifle, v.v.).
- [ ] Quyết định có **đổi** `UnitPreset_Ren.battleSprite` sang sprite FC hay không — nếu có, giữ chiến lược `.meta`/GUID.
- [ ] Trong Unity: References tới `UnitPreset_Ren`, scene combat sandbox, Import Settings sprite (pivot, compression).

---

## 5. Ghi chú triển khai Cursor / skill

- Đọc lock trước: `fc-character-consistency`, `fc-sprite-sheet-generation`, `fc-frame-by-frame-animation`, `fc-unity-2d-integration` (khi áp dụng).
- Pilot này **không** mặc định gen School PSD idle; mọi deliverable animation bám **`ren_combat_battle_idle_semichibi_v2_full.png`** (SoT).

---

*Tài liệu kế hoạch; mọi import asset hoặc chỉnh `UnitPreset_Ren` chỉ sau User Approval.*

# Ren Combat Semichibi — Báo cáo đồng bộ Source of Truth

**Ngày:** 2026-11-06  
**Quyết định lead (SoT):** `Assets/FracturedChorus/Art/Characters/Ren/Battle/ren_combat_battle_idle_semichibi_v2_full.png` — **864×1152** (xem [`REN_COMBAT_SOURCE_OF_TRUTH.md`](REN_COMBAT_SOURCE_OF_TRUTH.md)).

**Phạm vi task:** chỉnh **CHARACTER_LOCK**, tài liệu Art Pipeline liên quan, **một** rule Cursor khi có ví dụ gây nhầm; **ghi nhận** script cần xem lại — **không** sửa ảnh, `.meta`, GUID, `UnitPreset_Ren`, animator, clip, scene, prefab.

---

## 1. Tóm tắt điều chỉnh

| Loại | Nội dung |
|------|----------|
| SoT chỉnh sửa | `ren_combat_battle_idle_semichibi_v2_full.png` (864×1152) |
| Reference / history | `ren_combat_battle_idle_semichibi_v2.png` (crop ~554×1247 — pipeline), `ren_combat_battle_idle_semichibi_v3.png` (470×1109) — **không** dùng làm base chỉnh sửa mới |
| Preset Unity | `UnitPreset_Ren.battleSprite` vẫn placeholder `N_Corp_*` — **chưa** đồng bộ (theo giới hạn task) |

---

## 2. File đã sửa

| File | Thay đổi chính |
|------|----------------|
| [`CHARACTER_LOCK.md`](../../../Assets/FracturedChorus/Art/Characters/Ren/CHARACTER_LOCK.md) | SoT combat semichibi → **v2_full** 864×1152; v2 crop + v3 → reference/history; không đụng mô tả nhận diện khác. |
| [`REN_REFERENCE_AUDIT.md`](../REN_REFERENCE_AUDIT.md) | Bảng path: dòng **source** v2_full; dòng history v3; tóm tắt READY. |
| [`CHARACTER_ART_DIRECTION.md`](../CHARACTER_ART_DIRECTION.md) | Kích thước combat static + bước QA → **v2_full**. |
| [`Ren_Visual_Reference_Index.md`](Ren_Visual_Reference_Index.md) | Ghi chú SoT; bảng Combat liệt kê v2_full + history. |
| [`REN_PILOT_PRODUCTION_PLAN.md`](../REN_PILOT_PRODUCTION_PLAN.md) | Pilot và bảng “cần xác minh” align **v2_full**; checklist chốt SoT [x]. |
| [`.cursor/rules/2d-asset-standards.mdc`](../../../.cursor/rules/2d-asset-standards.mdc) | Ví dụ filename + kích thước Ren combat → **v2_full** / 864×1152 (tránh nhầm v3). |
| [`CHARACTER_ART_AUDIT.md`](../CHARACTER_ART_AUDIT.md) | Dòng Ren + đoạn `.meta` — SoT Ren combat **864×1152**; ghi *superseded* 470×1109. |
| [`FRACTURE_CHORUS_ART_PIPELINE_AUDIT.md`](../FRACTURE_CHORUS_ART_PIPELINE_AUDIT.md) | Đoạn Ren Battle / lock — cập nhật SoT + *superseded*. |
| [`REN_COMBAT_SOURCE_OF_TRUTH.md`](REN_COMBAT_SOURCE_OF_TRUTH.md) | Refresh §3/§6 cho khớp lock hiện tại + pointer sang báo cáo này. |

**Rule / skill khác:** đã grep `.cursor/**/*.mdc` — không còn ví dụ **v3** hay **470×1109** cho Ren combat; **không** thêm rule trùng.

---

## 3. File không sửa (theo yêu cầu hoặc không cần)

| File / nhóm | Lý do |
|---------------|--------|
| Mọi `*.png` / texture | Cấm chỉnh pixel. |
| `*.meta`, GUID | Cấm. |
| [`UnitPreset_Ren.asset`](../../../Assets/FracturedChorus/Resources/UnitPresets/UnitPreset_Ren.asset) | Cấm trong task; vẫn trỏ `N_Corp_*`. |
| Animator, `.anim`, scene, prefab | Cấm. |
| [`Tools/Art/ren_semichibi_v2_syncpod_wire.py`](../../../Tools/Art/ren_semichibi_v2_syncpod_wire.py), [`ren_skin_match_idle.py`](../../../Tools/Art/ren_skin_match_idle.py) | Chỉ **ghi nhận** (mục 5) — chưa refactor path/policy. |
| [`Ren_Visual_Reference_Sheet.png`](Ren_Visual_Reference_Sheet.png) (nếu có) | Không rebuild ảnh trong task; index đã note sheet có thể cũ. |

---

## 4. Quét repo sau cập nhật (markdown + code text)

**Pattern đã quét:** `semichibi_v3`, `470×1109`, `battle_idle_semichibi_v2` (không `_full`), SoT/canon cho Ren combat.

**Kết quả:**

- **Không** còn tài liệu nào gọi **v3** hoặc **470×1109** là *nguồn chỉnh sửa chính* ngoài bảng *history/ref* và note *superseded*.
- **Script** vẫn **ghi path** tới `semichibi_v2.png` / `v2_full` — mong đợi; cần policy rõ khi sửa script (mục 5).

---

## 5. Script — ghi nhận (chưa sửa code)

| Script | Ghi chú cho task sau |
|--------|----------------------|
| `Tools/Art/ren_semichibi_v2_syncpod_wire.py` | Xuất `full_out` → **v2_full** và `battle_out` → crop **v2**; đảm bảo mọi *edit* artist bắt nguồn từ **v2_full**, crop chỉ là bước export / scale game nếu lead duyệt; path input JPG cứng trong `.cursor/projects/...` — có thể chuẩn hóa relative sau. |
| `Tools/Art/ren_skin_match_idle.py` | Đọc/ghi `semichibi_v2.png` và `v2_full`; một path `.cursor/projects/d-Question/...` — kiểm tra khi hợp nhất máy dev. |

---

## 6. Mâu thuẫn / việc còn tồn tại

1. **Gameplay vs lock:** `UnitPreset_Ren.battleSprite` vẫn **N_Corp** placeholder — cần task Unity riêng (import `v2_full`, `.meta`, gán sprite) khi lead cho phép.
2. **Contact sheet PNG:** có thể vẫn composite cũ — rebuild hoặc chấp nhận lệch visual đến khi rebuild.
3. **Script dual-output:** pipeline vẫn tạo **v2 crop** song song **v2_full** — thống nhất tài liệu “crop không phải SoT” đã có; code có thể thêm comment/README sau.
4. **Animator hiện tại:** vẫn gắn chuỗi ref cũ (đã nêu trong pilot plan) — ngoài scope.

---

## 7. Trạng thái — chờ xác nhận

Đồng bộ văn bản theo lead đã áp dụng trong các file mục 2. **Diana dừng tại đây**; xin anh xác nhận hoặc chỉ định bước tiếp (Unity preset, rebuild sheet, chỉnh script).

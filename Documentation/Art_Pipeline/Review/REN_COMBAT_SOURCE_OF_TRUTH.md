# Ren — Combat Semichibi Source of Truth (Lead decision + audit)

**Quyết định Lead (đưa vào báo cáo này):** Nguồn artwork Combat Semichibi Ren **duy nhất** được phép tiếp tục phát triển là:

`Assets/FracturedChorus/Art/Characters/Ren/Battle/ren_combat_battle_idle_semichibi_v2_full.png`

**Kích thước:** **864 × 1152** (đã xác minh bằng đọc file PNG qua Pillow trong phiên audit này).

**Phạm vi bản gốc (audit):** chỉ đọc repo và lập kế hoạch — không sửa ảnh / preset / meta. **Sau khi lead duyệt:** team đồng bộ lock + docs + rule mẫu (task `REN_SOURCE_SYNC_*`) — báo cáo tổng kết: [`REN_SOURCE_SYNC_REPORT.md`](REN_SOURCE_SYNC_REPORT.md).

---

## 1. Nguồn chính thức (theo Lead)

| Thuộc tính | Giá trị |
|------------|---------|
| Path | `Assets/FracturedChorus/Art/Characters/Ren/Battle/ren_combat_battle_idle_semichibi_v2_full.png` |
| Kích thước | **864 × 1152** |
| Vai trò | **Single source of truth** cho mọi chỉnh sửa / sản xuất mới liên quan combat semichibi Ren (idle và phát triển tiếp theo trên cùng tầng art). |

**Ghi chú Unity:** Trong workspace hiện tại, glob `*.meta` trong `.../Ren/Battle/` trả về **0 file** — các PNG battle (gồm `v2_full`) có thể chưa được Unity import/commit meta trong repo, hoặc meta nằm ngoài thư mục đã quét. **Cần kiểm tra thủ công trong Unity** trước khi gắn `battleSprite` (import + commit `.meta`).

---

## 2. Các phiên bản tham khảo / lịch sử (không dùng làm nguồn chỉnh sửa mới)

Theo yêu cầu Lead, các file sau chỉ coi là **reference / history** — không tự ý dùng làm base cho pipeline sản xuất mới:

| File | Ghi chú (từ audit repo) |
|------|-------------------------|
| `ren_combat_battle_idle_semichibi_v2.png` | Bản crop canvas pipeline (script xuất `battle_out`; kích thước khác `v2_full` — xem `Tools/Art/ren_semichibi_v2_syncpod_wire.py` `export_battle_canvas` 554×1247). |
| `ren_combat_battle_idle_semichibi_v3.png` | **History/ref**; 470×1109 — không dùng làm SoT (CHARACTER_LOCK đã cập nhật). |
| `ren_combat_battle_idle_v2.png`, `ren_combat_battle_idle_v1.png` | HD / variant combat static — không thay thế vai trò “semichibi source” theo Lead. |

Các file trên **vẫn giữ trong repo** (không xóa).

---

## 3. Các file / vị trí đang mâu thuẫn với quyết định mới

### 3.1 CHARACTER_LOCK — **đã đồng bộ với lead**

[`CHARACTER_LOCK.md`](../../Assets/FracturedChorus/Art/Characters/Ren/CHARACTER_LOCK.md) ghi **SoT:** `ren_combat_battle_idle_semichibi_v2_full.png` (**864×1152**); **v2 crop** và **v3** là reference/history. Ghi chú `battleSprite`/preset: sau nghiệm thu trỏ preset FC — **không** nhét v3 làm nguồn chỉnh sửa.

### 3.2 UnitPreset_Ren — **không trỏ semichibi**

[`Assets/FracturedChorus/Resources/UnitPresets/UnitPreset_Ren.asset`](../../Assets/FracturedChorus/Resources/UnitPresets/UnitPreset_Ren.asset): `battleSprite` → guid `9a437a3dfde73224686f2d2a81ee930e` → **`N_Corp._E.G.O_Fell_Bullet_Yi_Sang_Idle_Sprite.png`** (placeholder ref), **không** phải v2_full hay v3.

**Mâu thuẫn:** Lead chốt nguồn art FC là `v2_full`, preset vẫn ref ngoài — **chưa** đồng bộ (task này không sửa preset).

### 3.3 Documentation

| File | Mâu thuẫn |
|------|------------|
| [REN_REFERENCE_AUDIT.md](../REN_REFERENCE_AUDIT.md) / [CHARACTER_ART_DIRECTION.md](../CHARACTER_ART_DIRECTION.md) / [Ren_Visual_Reference_Index.md](Ren_Visual_Reference_Index.md) / [REN_PILOT_PRODUCTION_PLAN.md](../REN_PILOT_PRODUCTION_PLAN.md) | Đã align **v2_full** trong đợt `REN_SOURCE_SYNC_*` (xem `REN_SOURCE_SYNC_REPORT.md`). |
| [`.cursor/rules/2d-asset-standards.mdc`](../../../.cursor/rules/2d-asset-standards.mdc) | Ví dụ filename đã trỏ **v2_full** + 864×1152. |
| Báo cáo audit tĩnh ([CHARACTER_ART_AUDIT.md](../CHARACTER_ART_AUDIT.md), [FRACTURE_CHORUS_ART_PIPELINE_AUDIT.md](../FRACTURE_CHORUS_ART_PIPELINE_AUDIT.md)) | Có thể vẫn mang ngày/số cũ — nên thêm note addendum hoặc cập nhật bảng Ren khi maintenance. |

### 3.4 Rules / Skills — **gián tiếp**

- Rules/skills đọc **CHARACTER_LOCK**; sau khi lock align lead, agent nên bám **v2_full**. Rule `2d-asset-standards` có ví dụ tên file + kích thước khớp SoT.

### 3.5 Script toolchain

| Script | Hành vi liên quan |
|--------|-------------------|
| [`Tools/Art/ren_semichibi_v2_syncpod_wire.py`](../../../Tools/Art/ren_semichibi_v2_syncpod_wire.py) | Ghi **`v2_full`** và **`semichibi_v2.png`**; input cứng `..._v2_green.jpg` dưới `.cursor/projects/.../assets/`. |
| [`Tools/Art/ren_skin_match_idle.py`](../../../Tools/Art/ren_skin_match_idle.py) | Xuất **`semichibi_v2.png`** và **`semichibi_v2_full.png`** từ path cố định. |

**Kế hoạch:** sau khi Lead duyệt, script nên **ưu tiên xuất/đồng bộ từ cùng một nguồn truth** (`v2_full`) và tránh tái tạo `v2` cropped như “source” mới nếu Lead cấm dùng v2 làm base — có thể chỉ giữ script crop cho **export game** tách từ `v2_full` (design decision).

### 3.6 Animation clips

[`Ren Idle.anim`](../../../Assets/FracturedChorus/Art/Characters/Ren/Animation/Ren%20-%20Idle/Ren%20Idle.anim) (và PSD `N_Corp_*`) — **không** dùng `v2_full` làm nguồn frame hiện tại. Đồng bộ animation là bước **sau** nghiệm thu art (ngoài scope chỉnh file trong task này).

---

## 4. Những nơi cần cập nhật (đề xuất — thực hiện sau duyệt)

| Thứ tự đề xuất | Nơi | Nội dung cập nhật |
|----------------|-----|-------------------|
| 1 | **`CHARACTER_LOCK.md`** | Path SoT: `..._semichibi_v2_full.png`; kích thước **864×1152**; ghi v2/v3 là reference/history; mô tả SyncPod khớp art trên **v2_full**. |
| 2 | **`REN_REFERENCE_AUDIT.md`** / **`CHARACTER_ART_DIRECTION.md`** / **`Ren_Visual_Reference_Index.md`** / **`REN_PILOT_PRODUCTION_PLAN.md`** | Thay “canonical semichibi” thành **v2_full**; phân biệt **source vs reference**. |
| 3 | **Contact sheet** `Ren_Visual_Reference_Sheet.png` (nếu rebuild) | Ô combat dùng **v2_full** thay v2 hoặc v3. |
| 4 | **`.cursor/rules/2d-asset-standards.mdc`** | Đổi ví dụ filename sang `..._v2_full.png` hoặc giữ generic. |
| 5 | **`Tools/Art/*.py`** | Document/chỉnh input/output: **bản sửa đổi phải part từ `v2_full`**; giảm hardcode path ngoài repo nếu có thể. |
| 6 | **Unity** | Import `v2_full`, có `.meta`, slice/pivot; **sau đó** cân nhắc `UnitPreset_Ren.battleSprite` (và có thể crop variant cho UI scale) — **không** làm trong task audit. |
| 7 | **Animator** | Clip idle mới hoặc retarget từ sheet — sau khi art/đội animation chốt. |

---

## 5. Thứ tự cập nhật an toàn (không gãy GUID / không ghi đè nhầm)

1. **Freeze** file `ren_combat_battle_idle_semichibi_v2_full.png` trên branch làm baseline (không ghi đè nội dung pixel trong bước đồng bộ doc).
2. **Cập nhật văn bản:** `CHARACTER_LOCK.md` trước (source of truth nội bộ team), rồi docs `Documentation/Art_Pipeline/`.
3. **Unity:** Mở Editor → reimport / tạo `.meta` cho `Battle/*.png` nếu thiếu → commit **cả** PNG + `.meta` (tránh GUID orphan).
4. **QA in-game:** Gán tạm `battleSprite` trên **preset nhân bản** hoặc object sandbox — **không** đổi `UnitPreset_Ren` production cho đến khi pass (trừ khi Lead chỉ định).
5. **Preset chính:** Cập nhật `UnitPreset_Ren.battleSprite` guid → sprite từ `v2_full` (hoặc sub-sprite slice) — **một** commit sau QA.
6. **Animation:** Thay dầu vào `Ren Idle` / sheet mới; giữ bản `N_Corp` trong history branch nếu cần rollback.
7. **Script:** Chỉnh pipeline để không coi `v2.png`/`v3.png` là input chỉnh sửa — document rõ.

---

## 6. Tóm tắt mâu thuẫn cốt lõi (trạng thái sau đồng bộ doc)

| Nguồn | Trạng thái |
|--------|------------|
| Lead | SoT = **`v2_full` 864×1152** |
| CHARACTER_LOCK | Khớp lead (**v2_full**; v2 crop / v3 = history). |
| UnitPreset_Ren | Vẫn **`N_Corp_*`** — **chưa** gán FC (task Unity sau). |
| Scripts | Vẫn xuất **`v2_full`** + **`v2`** crop — cần policy rõ: crop chỉ export phụ, không SoT (xem `REN_SOURCE_SYNC_REPORT.md`). |
| Review sheet PNG / các báo cáo audit snapshot | Có thể cần rebuild hoặc note “superseded” nếu vẫn hiển thị frame cũ. |

**Hành động tiếp theo:** Unity import + `.meta`, QA sandbox, cập nhật preset/anim khi lead cho phép (không gộp vào task chỉ sửa văn bản).

---

*Báo cáo lead + audit gốc; chi tiết đợt sync: [`REN_SOURCE_SYNC_REPORT.md`](REN_SOURCE_SYNC_REPORT.md).*

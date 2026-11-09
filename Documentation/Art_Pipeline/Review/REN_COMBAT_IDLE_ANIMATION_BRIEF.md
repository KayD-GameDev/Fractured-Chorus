# Ren — Combat Idle Animation Brief

**Ngày:** 2026-11-06 · **Vai trò:** animation intent đã duyệt (full FBF).

**SoT:** `Assets/FracturedChorus/Art/Characters/Ren/Battle/ren_combat_battle_idle_semichibi_v2_full.png` — **864 × 1152**.

---

## 1. Mục tiêu

Thiết kế **Combat Idle** FBF đầu tiên cho Ren combat semichibi: loop nhịp thở nhẹ, giữ stance và silhouette chiến đấu, tuân **CHARACTER_LOCK**. Brief mô tả ý định; thực thi và QC theo Production Spec.

---

## 2. Thông số

| Hạng mục | Giá trị |
|----------|---------|
| Tên | Combat Idle |
| Vòng lặp | **1,5–2,0 s** |
| Keyframe | **4** — F1, F2, F3, F4 |
| Phương pháp | FBF từ SoT |
| Loop | **F4 → F1** liền mạch (không nhảy silhouette) |

---

## 3. Biên độ

Quy tắc duy nhất (**full production**):

- Trần chu kỳ: **1–2 px** tại **864 × 1152**.
- **F3** = **đỉnh** chu kỳ (đạt trần).
- **F2** **dưới F3**.
- **F4** gần **F1** hơn F3.
- **Không** hiểu F3 = F2 + thêm 1–2 px.

---

## 4. Chuyển động (full production)

| Vùng | Cho phép |
|------|----------|
| Torso / vai | Nhẹ, trong giới hạn §3 |
| Tay + Violin Rifle | **Một khối rigid** với torso; không trượt grip |
| Tóc | Dao động phụ cực nhỏ; không đổi kiểu |
| SyncPod | Tuỳ chọn; giữ gắn tai–dây–cổ |

Mặt, chân, stance đứng; không bước; không crop chân. Kỹ thuật canvas: [`REN_COMBAT_V2_FULL_READINESS_AUDIT.md`](REN_COMBAT_V2_FULL_READINESS_AUDIT.md).

---

## 5. Character Lock

[`CHARACTER_LOCK.md`](../../../Assets/FracturedChorus/Art/Characters/Ren/CHARACTER_LOCK.md) là **authority** nhận diện: tóc/mắt xám/biểu cảm, đồng phục, Violin Rifle Basic, SyncPod (không headphone combat). Brief không thay lock.

---

## 6. Keyframes

| Key | Vai trò |
|-----|---------|
| **F1** | **Byte-identical** SoT |
| **F2** | Early inhale; dưới đỉnh §3 |
| **F3** | Inhale peak; **max** §3 |
| **F4** | Exhale; **key riêng** — gần F1 (silhouette/rest), **không** duplicate/hash F1 |

F2–F4 bám **F1** làm nguồn hình khi sản xuất full.

---

## 7. Loop và Playback

Thứ tự: **F1 → F2 → F3 → F4 → F1**.

Hold một chu kỳ: F1 **30%**, F2 **25%**, F3 **15%**, F4 **30%**.

**SpriteRenderer:** đổi sprite **rời** (hold từng PNG); **không** morph/blend pixel giữa các sprite.

Ví dụ mẫu 20 Hz / T = 1,6 s: chỉ tại Production Spec **§3**.

---

## 8. DCC / Consistency

1. F1 = copy SoT; overlay ref cho F2–F4.  
2. Transform nhóm torso + rigid tay–súng trước khi sửa line.  
3. Diff QC: vùng §4; alpha **0/255**.  
4. Naming: Production Spec **§7**.  
5. Pivot thống nhất; không crop chân.

Under-draw cục bộ khi cần (nách/áo sau súng); không phá silhouette lock.

---

## 9. Rủi ro

Biên độ lớn phá stance; mép dưới canvas sát chân; lệch glow súng nếu không khóa layer F1; pilot AI ≠ full §4.

---

## 10. Phạm vi Full Production và Pilot

| Thứ bậc | Tài liệu |
|---------|----------|
| Identity | CHARACTER_LOCK |
| Intent | **Brief này** |
| Full production | [`REN_COMBAT_IDLE_FBF_PRODUCTION_SPEC.md`](REN_COMBAT_IDLE_FBF_PRODUCTION_SPEC.md) |
| AI pilot hẹp | [`REN_COMBAT_IDLE_FBF_AI_PROMPT_PACK_v1.1.md`](REN_COMBAT_IDLE_FBF_AI_PROMPT_PACK_v1.1.md) |

| Đường | Nội dung |
|-------|----------|
| **Full FBF** | Brief §3–8 + Spec **§5–7**, **§9**, **§12.1** |
| **AI pilot** | Prompt Pack; Spec **§8**, **§10**, **§12.2** |

**Pass pilot AI không duyệt pipeline full** (Spec **§8**, **§12.2**).

---

*Chờ lead trước sản xuất.*

# Ren — Combat Semichibi v2_full: Animation Readiness Audit

**Mục tiêu:** đánh giá mức độ “sẵn sàng” cho animation (FBF / skeletal) của nguồn chính thức duy nhất phân tích trong báo cáo này.

**File phân tích (SoT):** `Assets/FracturedChorus/Art/Characters/Ren/Battle/ren_combat_battle_idle_semichibi_v2_full.png`

**Tham chiếu nhận diện (không mở file khác làm nguồn hình):** `Assets/FracturedChorus/Art/Characters/Ren/CHARACTER_LOCK.md` — mô tả đồng phục, Violin Rifle Basic, SyncPod (đĩa + sóng + dây), không headphone combat.

**Phạm vi:** chỉ đọc / phân tích. **Không** chỉnh PNG, không tách layer, không Unity/Blender trong báo cáo này.

**Phương pháp hình học:** Python 3 + Pillow + NumPy trên file SoT (2026-11-06). Thị giác: mô tả ảnh từ viewer (độ phân giải đầy đủ).

---

## Legend

| Nhãn | Ý nghĩa |
|------|---------|
| **Verified (code)** | Đo trực tiếp trên pixel trong file SoT. |
| **Judgment (image)** | Nhận định từ quan sát hình (pose, overlap, đồ họa). |
| **Manual (DCC/Unity)** | Cần mở trong Blender/Unity hoặc thao tác tay để chốt. |

---

## 1. Canvas size, transparency, alpha bounds

| Hạng mục | Kết quả | Nguồn |
|----------|---------|--------|
| Kích thước canvas | **864 × 1152** px | **Verified (code)** — `Image.size` |
| Định dạng / kênh | PNG, **RGBA** | **Verified (code)** |
| Alpha min / max | **0** / **255** | **Verified (code)** |
| Trung bình alpha toàn canvas | **~58.13** (≈ 22.8% pixel “đục” theo ngưỡng dưới) | **Verified (code)** |
| Phân phối alpha | **2 giá trị duy nhất (0 và 255)**; vùng `0 < α ≤ 240`: **0** pixel | **Verified (code)** → **alpha nhị phân (cạnh cứng), không viền feather trong kênh alpha** |
| “Opaque” cho bbox (ngưỡng α > 16) | **226 913** pixel (**~22.80%** diện tích canvas) | **Verified (code)** |

**Judgment (image):** Bản vẽ kiểu cel/outline; cạnh rõ, phù hợp sprite 2D nhưng khi scale nhỏ trên hex/board cần kiểm tra alias ở Unity (bilinear + premultiply).

---

## 2. Character bounding box và tỷ lệ chiếm canvas

**Định nghĩa bbox (code):** hình chữ nhật nhỏ nhất bao toàn bộ pixel có **α > 16** (gần tương đương mask “có nhân vật” khi alpha nhị phân).

| Hạng mục | Giá trị | Nguồn |
|----------|---------|--------|
| Góc trên-trái (x, y) | **(235, 42)** | **Verified (code)** |
| Kích thước bbox (w × h) | **552 × 1070** px | **Verified (code)** |
| Bbox / canvas (chiều ngang) | **552 / 864 ≈ 63.9%** | **Verified (code)** |
| Bbox / canvas (chiều dọc) | **1070 / 1152 ≈ 92.9%** | **Verified (code)** |
| Diện tích bbox / diện tích canvas | **~59.3%** | **Verified (code)** *(552×1070 / 864×1152)* |
| Diện tích pixel α>16 / canvas | **~22.8%** | **Verified (code)** *(mật độ nội dung trong bbox thấp hơn bbox chữ nhật — có nhiều “lỗ hổng” trong khung súng/tóc/kẽ chân)* |
| Trọng tâm phân bố alpha (norm) | **x ≈ 0.489, y ≈ 0.487** (gần giữa canvas) | **Verified (code)** |

**Judgment (image):** Tóc và nòng súng kéo rộng bbox ngang; chân sát đáy frame — **ít dư địa dưới chân** cho nhảy / squat nếu không mở rộng canvas hoặc cho phép chân tràn frame trong clip.

---

## 3. Silhouette readability

**Judgment (image):**

- **Đọc tốt ở idle:** Tách tốt giữa đầu — thân — chân; súng dài tạo **đường chéo** rõ, tăng nhận diện silhouette so với stance đứng thẳng tay sườn.
- **Rủi ro khi scale nhỏ:** Chi tiết phụ (scope, dây SyncPod, nốt nhạc trên receiver) có thể **nhiễu** khi mip hoặc distant read — **Manual (Unity):** kiểm tra kích thước hiển thị thực trên board/combat camera.

---

## 4. Các bộ phận có thể gây khó khi animation

| Khu vực | Quan sát | Ý nghĩa pipeline |
|---------|----------|------------------|
| **Súng + hai tay** | Tay ôm cò / foregrip; cánh tay và khối súng **chồng nhau nhiều** | **Judgment (image)** — tách layer hoặc rig cần quyết định súng **con child** của tay vs **object riêng + IK**; overlap làm “ẩn” một phần áo/tay trái. |
| **Violin Rifle (chi tiết lock)** | Stock, barrel, scope, glow tím, ký hiệu nốt | **Judgment (image)** — animation bắn/giật có thể cần **VFX riêng** (flash, đạn nốt) hơn là chỉ xoay sprite một lớp. |
| **SyncPod + dây** | Chi tiết nhỏ trên tai → cổ (theo lock) | **Judgment (image)** — chuyển động đầu mạnh: dễ **cắt** hoặc cần **bone / mesh con**; dây có thể là mask cứng hoặc simulated sau. |
| **Tóc** | Khối tóc gọn, outline dày | **Judgment (image)** — idle ít vấn đề; **sway** cần sub-mesh hoặc FBF. |
| **Áo / quần** | Gấp nếp tương đối ít | **Judgment (image)** — skeletal ít artifact đứt line nếu pose gần idle; pose lớn có thể cần corrective hoặc FBF hybrid. |
| **Chân / giày** | Tách chân rõ trong pose hiện tại | **Judgment (image)** — đủ cho idle; bbox sát đáy — bước dài có thể chạm mép. |

---

## 5. Khả năng chuẩn bị layer cho skeletal (2D Animation)

**Điều kiện hiện tại:** một **PNG đơn, alpha nhị phân** — **Verified (code)** không có layer nội bộ.

| Tiêu chí | Đánh giá |
|----------|-----------|
| Sẵn sàng import skeletal **nguyên khối** | **Manual (Unity)** — dùng mesh từ sprite làm được nhưng vùng tay–súng–ngực **overlap phức tạp** → weight painting khó sạch nếu không tách. |
| Sẵn sàng sau **PSD / tách layer** (ngoài scope báo cáo) | **Judgment (image)** — nên tách **đại thể:** thân, đầu, **súng**, cánh tay trái/phải (hoặc súng + forearm chain), chân trái/phải, SyncPod+dây; phù hợp Unity 2D Animation / Skinning. |
| Giả định “dùng nguyên PNG” | Không phải technical blocker — nhưng **khuyến nghị tách hoặc ít nhất meshRegions** cho tay–súng. |

---

## 6. FBF so với Skeletal

| Hướng | Ưu / phù hợp với artwork này | Rủi ro |
|--------|------------------------------|--------|
| **FBF** | **Judgment:** phù hợp pilot **ngắn** (idle loop ít frame), giữ outline cứng, ít cần đoán geometry ẩn. | Chi phí frame tăng nhanh nếu combat set đầy đủ; súng chi tiết lặp lại nhiều frame. |
| **Skeletal / mesh** | **Judgment:** phù hợp **mở rộng** clip sau pilot; tái sử dụng pose base v2_full làm key ref. | Cần tách layer hoặc mesh sạch ở vùng **tay–súng**; glow có thể cần shader hoặc add-on sprite. |

**Kết luận trung tính (Judgment):** v2_full **đủ chất lượng** làm **key / texture source** cho cả hai hướng; lựa chọn FBF vs skeletal phụ thuộc scope pilot và chấp nhận cost tách layer.

---

## 7. Đề xuất pivot và cấu trúc chuẩn bị animation

**Lưu ý:** pivot Unity nên thiết lập **trong Editor** trên Sprite — bảng dưới là **gợi ý** từ bbox + pose (pixels, gốc trên-trái canvas).

| Gợi ý pivot | Giá trị gần đúng | Lý do |
|-------------|------------------|--------|
| **Root / hips (grounding)** | Đáy bbox (pixel opaque max **y**): **1111**; giữa ngang bbox **~511** — tức **~(511, 1111)** làm điểm xuất phát; tinh chỉnh **chạm gót** trong Unity | **Verified (bbox)** + **Manual (Unity)** — đặt gần đáy silhouette để ổn định idle. |
| **Thân / spine** | Trên đường giữa bbox, ~35–45% chiều cao bbox từ đáy | **Judgment (image)** — vùng ngực/bụng bớt che bởi súng hơn đầu. |
| **Đầu** | Trên 15–20% bbox từ top | **Judgment (image)** |
| **Súng** | Pivot gần **receiver / grip** (không ở nòng) | **Judgment (image)** — khớp quay hậu tọa / aim; **Manual** khi có layer súng riêng. |

**Cấu trúc preparation đề xuất (conceptual — không tạo asset ở đây):**

1. **Import SoT** → Sprite (Single) — **Manual (Unity)** pivot + compression.
2. **Idle:** ưu tiên **FBF 2–4 frame** hoặc **skeletal nhẹ** (breathing) trên torso/head; súng child hoặc constrained.
3. **Khi mở rộng:** PSD / layer split theo mục 5; rig 2D với bones: root, spine, neck, head, arms, legs, optional `hair`, **`weapon`**, **`syncpod_wire`**.
4. **Sort order:** súng vs forearm — **Manual** test Z-order theo góc cam gameplay.

---

## Tóm tắt nhanh

| Tiêu chí | Kết luận ngắn |
|----------|----------------|
| Kỹ thuật file | **864×1152**, **RGBA**, **alpha 0/255**, bbox **552×1070**, centroid gần giữa canvas. |
| Không gian canvas | **Ít margin dưới chân**; bbox chiếm **~93%** chiều cao — cần lưu ý clip có biên độ chân/lưỡi súng. |
| Readiness tổng thể | **Đủ** làm nguồn idle và pilot; **tách layer hoặc mesh cẩn thận** cho skeletal nếu có cử động tay–súng lớn. |

---

*Báo cáo kết thúc. Chờ xác nhận trước khi chỉnh art hoặc pipeline.*

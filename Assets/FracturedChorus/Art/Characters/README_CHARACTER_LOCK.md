# Character Art Lock — Fractured Chorus

## Mục đích
File `CHARACTER_LOCK.md` khóa nhận diện visual của **một** nhân vật để mọi sprite / bust / BG có nhân vật sau này khớp nhau.

## Quy tắc bắt buộc
1. **Nhân vật mới** → tạo `Assets/FracturedChorus/Art/Characters/<Name>/CHARACTER_LOCK.md` **trước hoặc cùng lúc** với sprite đầu tiên.
2. **Đã có lock** → **đọc và tuân thủ**; **không** tạo file lock mới / không đổi identity trừ khi user yêu cầu cập nhật lock.
3. Mọi generate/edit sprite phải reference: lock + SyncPod prop sheet (nếu có) + bust/expression đã approved gần nhất.
4. Tên thư mục = PascalCase ID nhân vật (`Haruto`, `MeiLin`, `Ryo`, `Ren`).

## Nội dung tối thiểu trong lock
- Identity (tên, tuổi ước lượng, vai trò)
- Face / hair / eyes / skin
- Outfit layers + palette hex gần đúng
- Props gắn người (vd SyncPod: mode blue vs red)
- Bust framing chuẩn (canvas, padding, crop)
- Expression list + SyncPod mode theo expression
- Reference paths trong repo

## Bust framing chuẩn (VN)
- Canvas khuyến nghị: **1024×1536** (hoặc tỉ lệ tương đương)
- Opaque silhouette **không** chạm mép L/R (padding ≥ ~8–12% chiều ngang)
- Transparent PNG, không fringe trắng
- PreserveAspect UI slot ~440×600

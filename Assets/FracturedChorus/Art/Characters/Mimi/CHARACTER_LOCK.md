# CHARACTER_LOCK — Mimi

## Identity
- Name: Mimi
- Title: **The Pulse** (`PinkySectorId.Pulse` boss — "Mimi — The Pulse")
- Role: Cadence elite / sector boss; enemy trong `CombatTutorial`
- Archetype: idol bị Cadence ăn mòn — vui vẻ nhưng hiếu chiến

## Reference
- **Canonical:** `Art/Characters/Mimi/mimi_fullbody_ref_v2.png`
- **Deprecated:** `mimi_fullbody_ref_v1.png` và `Art/Characters/_Reference/LuxeConcert/mimi_ref.png`
  (thiết kế idol trắng/hồng/vàng cũ — **không** dùng cho art mới)

## Face / hair / eyes
- Tóc: nâu hạt dẻ dài, gợn sóng, vài lọn highlight **hồng magenta**; mái lệch phải
- Mắt: xanh lục vàng (yellow-green), mắt trái nheo/nháy, mắt phải mở
- Biểu cảm mặc định: **cười toe (grin) hở răng**, hiếu chiến
- Da: sáng, ửng hồng nhẹ ở má
- Phụ kiện đầu: kẹp/hoa tai ngôi sao vàng bên phải

## Outfit layers
1. **Corset đen** không tay, cổ vuông, viền + hoa văn **ngôi sao vàng** ở ngực, nẹp dọc màu mận
2. **Choker** đen mảnh
3. **Găng/tay áo rời đen** dài tới khuỷu (tay trái dài hơn), bo vàng
4. **Băng tay vàng** ở bắp tay phải
5. **Chân váy 2 lớp**: lớp ngoài đen/tím rách tua kiểu lông vũ pixel; lớp trong xếp ly **hồng magenta**
6. **Xích vàng** rủ ở hông phải
7. **Tất đùi đen** bóng; garter có hoa văn ngôi sao vàng
8. **Ủ chân (leg warmer) hồng đậm** ở cổ chân
9. **Boots đế dày đen**, khóa + đế viền vàng

## Props — Twin Pulse Axes
- Hai rìu lưỡi kép, cán **đỏ mận** bọc kim loại xám
- Lưỡi rìu: đen, khắc **ngôi sao vàng** ở tâm, lưỡi dát **magenta/hồng dạng pixel glitch** (mosaic block)
- Pixel glitch = dấu hiệu Cadence corruption; giữ lại ở mọi sprite/VFX

## Palette (gần đúng)
| Vùng | Hex |
|---|---|
| Tóc nâu | `#8A5A3C` |
| Highlight hồng | `#E8578F` |
| Đen vải | `#151318` |
| Magenta pixel | `#FF3FA4` / `#C81E78` |
| Tím glitch | `#7A2A8C` |
| Vàng gold | `#E0B33A` |
| Mắt | `#A8C93A` |

## Combat art paths
- Full-body ref: `Art/Characters/Mimi/mimi_fullbody_ref_v2.png`
- Battle sprite: **chưa có** — scene `CombatTutorial` tạm dùng rig `kiki_ueda_*`
  (`Art/Characters/KikiUeda/kiki_ueda_idle_v1.png` + `Unit_Kiki_Ueda.controller`)

## Combat icon (Cadence elite card) — legacy baked composite
- Path: `Art/UI/Combat/Characters/mimi_character_icon_bars_elite_v1.png`
- Layout: cùng grammar Astra/Kiki — diamond nghiêng + name stack + hai thanh trắng
- Title / Name: **"The Pulse"** / **"Mimi"**
- Diamond: magenta/crimson (elite tier)
- Facing: quay về **trái** (enemy side)

## Combat enemy card
- Portrait: `Art/UI/Combat/Characters/Avatars/mimi_enemy_avatar_v1.png` (bust only; quay trái)
- Chrome (shared): `Art/UI/Combat/PartyCard/` — CardBg jagged (mirrored), AccentShard, side-tube track
- Hierarchy: CardBg / AccentShard / Avatar / NameLabel / BarStack (HP + PREP side tube) / ElementBadge
- Wire: `UnitPreset_Kiki_Ueda.combatCardSprite` → Avatar Image trên enemy status bar
- Element circle: góc trên-phải; HP fill đỏ; tilt +6°

## Bust framing chuẩn (VN)
- Canvas 1024×1536, transparent PNG, không fringe trắng
- Silhouette không chạm mép L/R (padding ≥ ~10%)

## Notes
- **Không** đổi silhouette / palette mà không cập nhật file lock này.
- Card art đã đổi sang Mimi nhưng `unitId` trong preset vẫn là `kiki_ueda` (rig/animator chưa đổi tên).

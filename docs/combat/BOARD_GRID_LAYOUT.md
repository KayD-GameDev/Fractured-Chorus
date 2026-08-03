# Board Grid — Toạ độ ô cố định (2 hàng × 3 cột)

> Nguồn chuẩn (single source of truth): `Assets/FracturedChorus/Combat/Grid/HexBoardLayout.cs`.
> Scene `CombatPrototype.unity` **đã khớp 100%** với các toạ độ world dưới đây.
> Menu **Fractured Chorus → Rebuild Hex Board Grid (scene)** snap ô về đúng các giá trị này ⇒ **không đổi sau mỗi lần gen**.

Quét từ scene ngày 2026-07-01. Board: 2 hàng (`Row 0` = hàng trên/empty, `Row 1` = hàng đơn vị y=0) × 3 cột, cho cả Player (side 0) và Enemy (side 1).

## Toạ độ WORLD (canonical — dùng để kiểm tra & tái tạo)

`HexBoardLayout.GetWorldPosition(side, row, col, sideGap=3.5)`; z (depth) = `row*0.1 + col*0.05` (ô đặt z=0, chỉ unit dùng depth).

### Player (side 0), anchorX = −3.5
| Ô | Col 0 | Col 1 | Col 2 |
|---|-------|-------|-------|
| Row 0 (trên) | (−2.80, 1.35) | (−4.57, 1.35) | (−6.33, 1.35) |
| Row 1 (đơn vị) | (−2.10, 0.00) | (−3.87, 0.00) | (−5.63, 0.00) |

### Enemy (side 1), anchorX = +3.5 (lật trục X so với player)
| Ô | Col 0 | Col 1 | Col 2 |
|---|-------|-------|-------|
| Row 0 (trên) | (2.80, 1.35) | (4.57, 1.35) | (6.33, 1.35) |
| Row 1 (đơn vị) | (2.10, 0.00) | (3.87, 0.00) | (5.63, 0.00) |

## Hằng số HexBoardLayout
- `DefaultSideGap = 3.5`, `RowVerticalPitch = 1.35`, `HexRadius = 0.55`.
- `RowY = { 1.35, 0 }` (index 0 = trên, index 1 = hàng đơn vị).
- `PlayerLocalOffsets` (x, y), enemy = lật dấu x:
  - Row 0: C0 (0.70, 1.35) · C1 (−1.07, 1.35) · C2 (−2.83, 1.35)
  - Row 1: C0 (1.40, 0.00) · C1 (−0.37, 0.00) · C2 (−2.13, 0.00)

## Giá trị LOCAL trong scene (để đối chiếu Hierarchy)

Chuỗi cha: `CombatRoot (0,0,0)` → `World (0, −0.26, 0)` → `Grid (0,0,0)` → `PlayerGrid (0,0,0)` / `EnemyGrid (0.62, −0.12, 0)`.
World = local ô + offset các cha ⇒ khớp bảng WORLD ở trên.

### Player cells — `m_LocalPosition` (cha PlayerGrid)
| Ô | Col 0 | Col 1 | Col 2 |
|---|-------|-------|-------|
| Row 0 | (−2.80, 1.61) | (−4.57, 1.61) | (−6.33, 1.61) |
| Row 1 | (−2.10, 0.26) | (−3.87, 0.26) | (−5.63, 0.26) |

### Enemy cells — `m_LocalPosition` (cha EnemyGrid, offset 0.62, −0.12)
| Ô | Col 0 | Col 1 | Col 2 |
|---|-------|-------|-------|
| Row 0 | (2.18, 1.73) | (3.95, 1.73) | (5.71, 1.73) |
| Row 1 | (1.48, 0.38) | (3.25, 0.38) | (5.01, 0.38) |

## Visibility hex floor (runtime)

- **Player:** hex hiện trong mọi planning window (`CombatSession.IsPlanningWindowOpen`); ẩn khi timeline đang quét.
- **Enemy:** hex **luôn ẩn** (không dùng cho dàn trận).
- API: `CombatController.ApplySlotFloorVisibilityForCurrentPhase` → `GridCellMarker.SetFloorVisible` (tắt child `Hexagon Flat Top`, giữ collider/transform).

## Quy tắc chống trôi toạ độ
1. Sửa layout **chỉ trong scene** hoặc qua hằng số `HexBoardLayout` — cả hai đang khớp nhau.
2. `CombatPrototypeBootstrap` khi Play **không** reposition ô (tôn trọng scene).
3. Nếu chỉnh `HexBoardLayout`, chạy lại menu **Rebuild Hex Board Grid (scene)** rồi lưu scene để đồng bộ, và cập nhật bảng này.

## Menu "Rebuild Hex Board Grid (scene)" — hành vi
- Nhóm ô theo Y, **giữ 2 hàng TRÊN** (top + hàng units), **xoá hàng DƯỚI cùng** khỏi scene.
- Re-index hàng còn lại về 0-based: **Row 0 = hàng trên**, **Row 1 = hàng units** (đổi tên ô cho khớp, vd hàng top cũ `R2` → `R0`).
- Snap ô + unit về đúng bảng WORLD ở trên; kích hoạt (SetActive) mọi ô giữ lại.

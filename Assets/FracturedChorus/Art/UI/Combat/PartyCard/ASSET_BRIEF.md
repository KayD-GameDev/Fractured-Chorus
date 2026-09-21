# Party / enemy card chrome

> Skill: `game-2d-asset-creation`

Compact HUD card: circular portrait on the left, two curved side tubes on the right.

| File | Role |
|------|------|
| `party_card_bg_jagged_v1.png` | Jagged P5 silhouette wrapping portrait + tube lobes |
| `party_card_side_tube_track_v1.png` | Empty curved tube (HP + gauge track) |
| `party_card_side_tube_fill_v1.png` | Inset fill shape; tint green (HP) or magenta (prep) |
| `party_card_accent_diamond_v1.png` | Accent shard behind portrait |
| `party_card_bar_track_v1.png` | Legacy horizontal track (unused on side-tube skin) |

- Tube canvas **80 × 320**, true alpha, **Full Rect** mesh (Unity `Image.Filled` Vertical).
- HP empties top → bottom; remaining fill sits at the bottom.
- Background canvas **640 × 472**, true alpha outside the shard. Do not flip for enemy cards.

## Cấm

- Không bake caro / nền đen đặc thành “trong suốt”.
- Không spritesheet gộp track + fill.

# Combat buff icons

> Skill: `game-2d-asset-creation` + alpha key `game-2d-asset-image-edit`

## Reduce S2

| File | Role |
|------|------|
| `buff_reduce_s2_v1.png` | Encore / PendingReduceS2 chip on party cards |

## Astra Stage TV moods (Plan A)

Circular **Cadence face close-up** tokens (forehead/bangs → chin only; no choker/shoulders). Shown on **party and enemy** cards when the Stage TV locks a face.

| File | Mood | Expression |
|------|------|------------|
| `astra_tv_mood_joy_v1.png` | Joy | Smile |
| `astra_tv_mood_anger_v1.png` | Anger | Scowl |
| `astra_tv_mood_love_v1.png` | Love | Soft affection |
| `astra_tv_mood_hate_v1.png` | Hate | Contempt |
| `astra_tv_mood_sorrow_v1.png` | Sorrow | Tears |

- Canvas **512 × 512**, true circle, **true alpha** outside the disc.
- Identity: `astra_cadence_boss_menu_fullbody_v1` face (platinum hair, amber eyes) + TV mood expression.
- Style: magenta/cyan TV bezel. Runtime: `PartyMemberCardView` child `BuffAstraTv` trên **CardTemplate** (Hierarchy) — chỉnh Rect bằng tay; code không ghi đè khi đã author.

## Cấm

- Không gộp 5 mood thành spritesheet.
- Không bake caro / nền đen đặc thành “trong suốt”.
- Không crop dưới cằm (không choker / vai / outfit).

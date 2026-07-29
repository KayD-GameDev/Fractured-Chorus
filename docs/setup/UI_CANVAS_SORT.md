# UI Canvas Sort Policy

Shared sort orders for Fractured Chorus overlays. Runtime constants: `FracturedChorus.UI.UiCanvasLayers`.

| Layer | Order | Examples |
|-------|------:|----------|
| World/Board | 0 | Combat board, unit sprites |
| HUD | 100 | Party/Enemy bars, Cover HUD |
| Panel | 200 | Skill radial, Deploy overlays |
| Popup | 400 | Timeline resolve |
| PopupDamage | 520 | Damage number popups |
| PopupChip | 530 | Counter note chips |
| Modal | 1000 | Status, Save/Load, Calendar |
| Tutorial | 1100 | Tutorial coach marks |
| System | 32000 | Global settings |

Rules:
- Modal and Tutorial canvases use `overrideSorting = true`.
- Opening a modal calls `SetAsLastSibling` within its layer.
- Settings always wins (System).

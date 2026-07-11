# Calendar Overlay — Design

**Date:** 2026-07-11  
**Trigger:** Status Menu → CALENDAR → full-screen overlay (Esc/Close → Status Menu)

## Layout
- Left: title, year `2026`, month `9`, weekday header, 6×7 day grid
- Right: `calendar_ren_panel_v1.png` (Ren mood art)
- TODAY = `Calendar.CurrentDate` (pink)
- Event ring/dot: story beats `1,2,5,6` + vault `20`

## Scope now
- September only (Arc 1); next month ghosted
- STATS/BONDS overlays: later (same pattern)

## Code
- `Hub/CalendarOverlayUI.cs`
- Hook: `MetaStatusMenuUI.OpenCalendarOverlay`

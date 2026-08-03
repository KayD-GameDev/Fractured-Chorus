# Task 4 Report — Docs + Play Mode acceptance

**Status:** DONE (docs) · Play Mode **PENDING HUMAN**
**Branch:** branch2
**Commit:** `b617fad` — Document planning click-vs-drag on the timeline setup guide.

## Summary

Documented planning-window click-vs-drag interaction in `SCENE_SETUP.md` under the Input / Beat Timeline setup section. Play Mode acceptance checklist recorded below for manual QA — not executed by agent.

## Changes

| File | Change |
|------|--------|
| `Assets/FracturedChorus/Scenes/SCENE_SETUP.md` | Added `### Planning window — unit click vs drag` paragraph after Input block |

## SCENE_SETUP addition

```markdown
### Planning window — unit click vs drag

- **Short click** (move ≤ `clickDragThresholdPx`, default 8px) → open skill panel.
- **Drag** past threshold → reposition / swap on player grid.
- Both only while `CombatSession.IsPlanningWindowOpen`.
- Gesture math: `BoardPointerGesture` · wiring: `BoardDragController`.
```

Placement: after **Input** paragraph, before **Beat Timeline UI — quét liên tục** (canonical SoT for scene authoring).

## Play Mode checklist (manual — PENDING HUMAN)

Run on `CombatPrototype.unity` or `CombatTutorial.unity`. Mark ✅ / ❌ + note.

| # | Check | Expected |
|---|-------|----------|
| 1 | Planning: short click unit | Skill panel opens |
| 2 | Drag skill onto lane | Assign succeeds |
| 3 | Drag unit > 8px | Move/swap; drop off-grid → snap home |
| 4 | Short click | Unit stays on cell (no drift) |
| 5 | During Execute | No panel, no unit drag |
| 6 | After segment hold (mid-fight planning) | Click + drag both still work |
| 7 | Timeline layout lock | Slot width ≥ 73.85; ScanBar `sizeDelta` `(6, -4)`; TrackLine `(y=6, h=2)` |

**Agent result:** Not run — Unity Play Mode unavailable to agent.

## Related QA docs

- `docs/combat/UNIFORM_BEAT_QA.md` §E (Planning Window) — E7/E8 already cover click-vs-drag; no duplicate edit required.

## Verification

- [x] Paragraph added per task brief
- [x] Docs committed with Summary-first message
- [ ] Play Mode checklist — **pending human**

## Not Done

- Play Mode manual QA (items 1–7 above)

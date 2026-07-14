# Timeline Note Readability — Note Tier + Drag Cover

> **Status:** Design approved (2026-07-14)  
> **Scope:** Approach A — wire kept sprites only (note tier + drop ghost + drag cover)  
> **Approach:** Timeline visual catalog (serialized refs) + `BeatSegmentView` / `ShowDropGhost` consumers  
> **Out of scope:** Footprint standing redesign, W window, empty-beat incentives, CORE/MICRO/EYE, resolve Perfect-chip / MULTI changes  
> **Related:** `docs/combat/COMBAT_MECHANICS.md` · `BeatSegmentView` · `BeatTimelineUIView.ShowDropGhost` · `BossNoteTier`  
> **Art:** `Assets/FracturedChorus/Art/UI/Combat/Timeline/`

---

## 1. Goals

- Player reads **boss note hits** at a glance (1 / 2 / 3) without parsing text-only labels.
- During skill drag, player sees **valid vs invalid placement** and whether Active footprint **covers an impact note** (Perfect cover preview vs Miss).
- Combat resolve logic unchanged (`CanAssignAction`, counter HitsRequired, Perfect chip path from #1 feel).

### Problem (current)

| Surface | Today | Gap |
|---------|--------|-----|
| Enemy note cell | Portrait tint by `BossNoteTier` + text `HitsRequired` | Tier/hits not icon-primary |
| Drop preview | Colored footprint dots (unit / red) | No dedicated valid/invalid glyph |
| Note under drag | No overlay | Cannot tell cover Perfect vs Miss before drop |

---

## 2. Assets (kept)

| Role | Path |
|------|------|
| Note 1 hit | `Timeline/Notes/note_tier_red_v1.png` |
| Note 2 hits | `Timeline/Notes/note_tier_blue_v1.png` |
| Note 3 hits | `Timeline/Notes/note_tier_purple_v1.png` |
| Drop valid | `Timeline/Feedback/drop_ghost_valid_v1.png` |
| Drop invalid | `Timeline/Feedback/drop_ghost_invalid_v1.png` |
| Cover Perfect | `Timeline/Feedback/cover_perfect_v1.png` |
| Cover Miss | `Timeline/Feedback/cover_miss_v1.png` |

Alpha: border-only clean if checkerboard remains; do not punch holes in hologram faces.

---

## 3. Mapping

| Runtime signal | Sprite |
|----------------|--------|
| `BossNoteTier.Red` (= 1) | `note_tier_red_v1` |
| `BossNoteTier.Blue` (= 2) | `note_tier_blue_v1` |
| `BossNoteTier.Purple` (= 3) | `note_tier_purple_v1` |
| `CanAssignAction(...) == true` | `drop_ghost_valid_v1` |
| `CanAssignAction(...) == false` | `drop_ghost_invalid_v1` |
| Drag Active beat ∩ impact telegraph + place **valid** | `cover_perfect_v1` |
| Drag Active beat ∩ impact telegraph + place **invalid** | `cover_miss_v1` |

`BossNoteTier` enum values already equal hits (`HitsRequiredForTier`). Sprite digit matches tier; no separate hits→sprite table.

---

## 4. Behavior

### 4.1 Note cell (`BeatSegmentView`)

- Impact telegraph: `portrait` uses tier sprite from catalog (replace color-tint-only presentation).
- Windup-only: keep existing `◆ ↑` treatment; **do not** apply note-tier sprite.
- Label text may stay as secondary (`HitsRequired` / skill name); icon is primary readable signal.
- Suggested display size: Inspector-tunable (default ~22–28px to match current portrait slot; grow only if readable at timeline density).

### 4.2 Drop ghost (`BeatTimelineUIView.ShowDropGhost`)

- For each `FootprintBeatRole.Active` under preview: show ghost sprite (valid/invalid) instead of solid unit-color / red tint dots.
- Standing / non-Active footprint dots: **unchanged** (out of scope A).
- Lane marker ghost (`SetGhost` / invalid preview flag) may remain; Active beat glyphs are the catalog ghosts.

### 4.3 Drag cover overlay

- While `ShowDropGhost` is active, for each Active beat that has an enemy telegraph with `IsWindupOnly == false` at that beat index:
  - place valid → overlay `cover_perfect` on that note cell (or beat column note anchor)
  - place invalid → overlay `cover_miss`
- Clear overlays in `HideDropGhost`.
- Multiple notes under one footprint: one cover overlay per overlapping impact beat.
- Resolve-time Perfect chip / MULTI banner: **unchanged**.

### 4.4 Edit Preview

- CombatRoot custom inspector Timeline foldout: assign or ping the 7 sprites + sizes (note / ghost / cover).
- New UI fields → update `CombatPrototypeBootstrapEditor` (existing convention).

---

## 5. Architecture

### Catalog

| Piece | Role |
|-------|------|
| **`TimelineNoteVisualCatalog`** | Holds 7 `Sprite` refs (+ optional sizes). Serializable class or small SO; default: serialized on `BeatTimelineUIView` or `CombatPrototypeBootstrap` |
| **`BeatSegmentView`** | Consumes sprite for current telegraph; no AssetDatabase paths |
| **`BeatTimelineUIView`** | Owns drop ghost + cover overlay lifecycle; reads catalog |

### Data flow

```
Telegraph bind
  → BeatSegmentView.SetTelegraph(...)
  → catalog.NoteSprite(NoteTier) → portrait.sprite

ShowDropGhost(unit, skill, screen)
  → valid = timeline.CanAssignAction(...)
  → foreach Active beat:
       ghost sprite = valid ? Valid : Invalid
       if impact telegraph @ beat:
         cover = valid ? Perfect : Miss
  → HideDropGhost clears ghosts + covers
```

### File boundaries

- Do not push catalog path strings into `BeatSegmentView`.
- Prefer not growing feel/resolve logic inside timeline; this change is presentation-only.
- No changes to `CombatCounterResolver` / `CanAssignAction` rules.

---

## 6. Success criteria

- [ ] Red / Blue / Purple impact notes show distinct tier sprites in CombatPrototype.
- [ ] Drag valid → Active beats show valid ghost; invalid → invalid ghost.
- [ ] Drag over impact note → Perfect or Miss cover matches place validity; clears on release / hide.
- [ ] Windup beats never show note-tier or cover-perfect as if they were impact notes.
- [ ] Perfect resolve chip + counter feel (#1) still behave as before.
- [ ] Edit Preview can select/ping the seven sprites.

---

## 7. Non-goals (phase later)

- Footprint standing contrast / layer order redesign.
- W-window strip or keyboard-W affordance art.
- Empty-beat incentives / rewards UI.
- CORE / MICRO / EYE note types.
- Replacing resolve Perfect popup chip with `cover_perfect`.

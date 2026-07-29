# Color Palette Unification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or execute task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Gom 3 palette rời (Hub Persona · Combat Neon Cadence · Run Map StS) thành một hệ token có bridge rõ, giảm cảm giác “đổi game” giữa scene và lệch design lock.

**Architecture:** Tạo `FcColorTokens` (runtime SoT) + `FcColorRole` enum phân **brand / surface / semantic / scene-bridge**. Hub giữ navy+cyan; Combat map token Neon spec; Run Map remap stroke/fill về navy base + semantic hue giữ node type. Không đổi art PNG lần 1 — chỉ code tint + editor scene bake.

**Tech Stack:** Unity 6 · UGUI `Color` · `ColorUtility.TryParseHtmlString` · existing `HarmonyElementPalette` · `MapNodePalette`

## Global Constraints

- **Design lock (Status/Echo Keys §1):** Persona-like · navy / cobalt / cyan / white · accent red on selection (chọn một hệ selection, áp nhất quán).
- **Combat Neon Cadence lock:** core `#8CF3FF` · body `#22D3EE` · accent `#FF3DA6` · text `#EAFBFF` · không fill trắng thuần trên neon chrome.
- **Không** regen character art / background PNG trong epic này.
- **Không** đổi gameplay semantics (Rhythm=đỏ, Melody=tím, Harmony=vàng giữ nguyên hue family).
- Mọi hardcoded `new Color(...)` mới phải qua token hoặc semantic alias — PR reviewer reject inline brand colors.
- Regression: 5 scene pairs trong `docs/qa/POST_DEFENSE_POLISH_REGRESSION.md` + Run Map legend readability.

---

## File map (tạo / sửa)

| File | Trách nhiệm |
|------|-------------|
| `Assets/FracturedChorus/UI/FcColorTokens.cs` | **NEW** — brand, surface, text, semantic, bridge |
| `Assets/FracturedChorus/UI/FcColorTokens.cs.meta` | Unity meta |
| `docs/ui/COLOR_TOKENS.md` | **NEW** — human SoT + usage rules + migration table |
| `Assets/FracturedChorus/UI/HarmonyElementPalette.cs` | Delegate border/badge → semantic element tokens |
| `Assets/FracturedChorus/RunMap/UI/MapNodePalette.cs` | Remap StS hex → bridge palette |
| `Assets/FracturedChorus/RunMap/UI/MapNodeView.cs` | Selection orange → `Brand.Accent` |
| `Assets/FracturedChorus/RunMap/UI/RunMapBossGateView.cs` | Gray/beige → surface + brand text |
| Hub overlays (~12 files) | Replace local `Cyan`/`PanelBg` constants |
| Combat UI (~8 files) | Skill radial, damage, counter, deploy, timeline tints |
| `Assets/FracturedChorus/Narrative/Vn/VnChoiceView.cs` | Selection color theo lock |
| `Assets/FracturedChorus/UI/UiButtonHoverFeedback.cs` | Default hover → token |
| `docs/qa/POST_DEFENSE_POLISH_REGRESSION.md` | Thêm mục palette spot-check |

---

## Token schema (lock trước khi code)

### Brand (meta + bridge)

| Token | Hex | RGB (0–1) | Dùng cho |
|-------|-----|-----------|----------|
| `Brand.Cyan` | `#00D4FF` | (0, 0.831, 1) | Hub accent chính — **giữ giá trị hiện tại** |
| `Brand.CyanDim` | `#008CB3` | (0, 0.55, 0.7) | Secondary labels, archive dim |
| `Brand.CyanHover` | `#8CD9FF` | (0.55, 0.85, 1) | Hover text / button highlight |
| `Brand.CyanNeonBody` | `#22D3EE` | combat body — bridge Hub↔Combat |
| `Brand.CyanNeonCore` | `#8CF3FF` | combat glow core |
| `Brand.MagentaAccent` | `#FF3DA6` | Neon accent duy nhất (combat + deploy back) |
| `Brand.RedSelection` | `#FF4757` | Selection accent (Persona lock) — **mới, thay cyan selection** |
| `Brand.TextPrimary` | `#EAFBFF` | Combat neon text / bright UI on dark |

### Surface (navy panels — 3 cấp)

| Token | RGBA | Dùng cho |
|-------|------|----------|
| `Surface.Dim` | (0.02, 0.04, 0.12, 0.75) | Backdrop dim, watermark BG |
| `Surface.Panel` | (0.03, 0.05, 0.14, 0.92) | Overlay chính (Calendar, Status, Tutorial) |
| `Surface.Modal` | (0.039, 0.059, 0.18, 0.94) | Save slots, detail panel |
| `Surface.Track` | (0.08, 0.12, 0.22, 0.95) | Stat bar track, row bg |

### Semantic (gameplay — không đổi hue family)

| Token | Maps to | Ghi chú |
|-------|---------|---------|
| `Semantic.ElementRhythm` | HarmonyElement Rhythm red | |
| `Semantic.ElementMelody` | HarmonyElement Melody purple | |
| `Semantic.ElementHarmony` | HarmonyElement Harmony gold | |
| `Semantic.Damage` | pink `#FF61C7` | gần `#FF3DA6` family, tách khỏi brand magenta |
| `Semantic.Heal` | `#40FF8C` | |
| `Semantic.Crit` | `#FFE033` | dùng chung EventGold |
| `Semantic.Warning` | orange `#F27D22` | invalid target, map selection ring (thay cam StS) |
| `Semantic.EventGold` | `#FFD633` | calendar event dot — merge 3 gold cũ |

### Run Map node strokes (remap — giữ phân biệt type)

| Node | Old (StS) | New direction |
|------|-----------|---------------|
| Battle | `#AA4E49` | `#C04A55` — đỏ navy-shift |
| Event | `#82B366` | `#5BA88A` — teal-muted (gần brand) |
| Elite | `#795F86` | `#7A5E9E` — tím, lệch Melody |
| Camp | `#D6B657` | `#C9A84E` → `Semantic.EventGold` family |
| Relay | `#D79B00` | `#E8A830` |
| Treasure | `#7091C0` | `#4A9FD4` — cyan-shift |
| Boss | `#C0463E` | `#D43840` + fill `Surface.Panel` |

Fill = `Color.Lerp(stroke, Surface.Panel, 0.55f)` thay white mix 0.52.

---

## Epic 0 — Token foundation (P0)

### Task 0.1: Create `FcColorTokens`

- [ ] Tạo `Assets/FracturedChorus/UI/FcColorTokens.cs` static class
- [ ] Nested static classes: `Brand`, `Surface`, `Semantic`, `RunMap`
- [ ] Helper: `FromHex(string)`, `WithAlpha(Color, float)`, `LerpSurface(Color stroke)`
- [ ] XML doc tối thiểu trên class — link `docs/ui/COLOR_TOKENS.md`

**Acceptance:** Project compile; không consumer bắt buộc yet.

### Task 0.2: Write `docs/ui/COLOR_TOKENS.md`

- [ ] Bảng token + “DO / DON’T” (vd: không dùng `Color.white` fill trên neon chrome)
- [ ] Migration table: old constant → new token (15+ rows)
- [ ] Scene bridge diagram Hub → Map → Combat

**Acceptance:** Doc review — mọi token trong code có dòng tương ứng.

---

## Epic 1 — Hub / Meta migration (P0)

**Phạm vi:** Thay local `Cyan`/`PanelBg`/`NavyDim` bằng `FcColorTokens`.

| File | Thay thế |
|------|----------|
| `MetaStatusMenuUI.cs` | Cyan, DetailPanel |
| `CalendarOverlayUI.cs` | Cyan, PanelBlue, ChipBg, Pink→calendar only, EventGold→Semantic |
| `SocialStatsOverlayUI.cs` | Cyan, NavyDim, Watermark |
| `PartyStatusMenuUI.cs` | PanelBg, Cyan, BarBg, BarFill |
| `SkillEquipPanelUI.cs` | PanelBg, Cyan |
| `LevelUpAllocUI.cs` | PanelBg, Cyan |
| `SaveLoadSlotListView.cs` | NavyPanel, NavyRow, Cyan |
| `TutorialCoachView.cs` | PanelColor, Cyan, DimColor |
| `DeployFormationHintView.cs` | PanelBg, Cyan; Front/Mid→brand; Back→Brand.MagentaAccent |
| `OffBeatArchiveController.cs`, `OffBeatTrackRowView.cs` | Cyan family |
| `UiButtonHoverFeedback.cs` | hoverTint default → Brand.CyanHover |

### Task 1.1: Hub batch replace

- [ ] Replace constants file-by-file (12 files)
- [ ] `SocialStatsRadarGraphic.cs` serialized defaults → token values in `Reset()` or `[SerializeField]` comment
- [ ] Compile + play CampusHub: menu, calendar, social stats, party status

**Acceptance:** Không còn `0.831f, 1f` ngoài `FcColorTokens.cs` (grep verify).

---

## Epic 2 — Selection language (P1)

Design lock: **accent red on selection**. Hiện cyan selection ở VN + hub.

### Task 2.1: Lock decision (30 min review)

- [ ] Confirm: **Selected = Brand.RedSelection**, **Hover/Focus = Brand.CyanHover**, **Brand idle = white/cyan dim**
- [ ] Ghi vào `COLOR_TOKENS.md` §Selection

### Task 2.2: Apply selection

- [ ] `VnChoiceView.cs` — `selectedColor` → `Brand.RedSelection` (alpha 0.95)
- [ ] `MetaStatusMenuUI.cs` — tab selected tint (sprite tint hoặc text) → red accent edge, không fill cả row đỏ
- [ ] `OffBeatTrackRowView.cs` — Selected text giữ Cyan **hoặc** chuyển Red — pick one, document
- [ ] `SaveLoadSlotListView.cs` — row selected border/highlight → RedSelection bg shift nhẹ

**Acceptance:** Screenshot 3 màn (VN choice, Status tab, Save slot) — selection đọc rõ, không lẫn hover.

---

## Epic 3 — Run Map bridge (P0)

### Task 3.1: Remap `MapNodePalette`

- [ ] Replace hex constants theo bảng Epic 0
- [ ] `LightenFill` → `FcColorTokens.LerpSurface(stroke, 0.55f)`
- [ ] Update `RunMapLegendPanelView` comment nếu có

### Task 3.2: `MapNodeView` selection

- [ ] Orange `(0.9, 0.49, 0.13)` → `Semantic.Warning` hoặc `Brand.CyanNeonBody` ring (pick: **cyan ring** cho brand, cam chỉ unavailable)
- [ ] Label dark gray giữ; Boss white giữ

### Task 3.3: `RunMapBossGateView`

- [ ] `PanelColor` → `Surface.Modal`
- [ ] `DimColor` → `Surface.Dim` alpha 0.82
- [ ] Text beige → `Brand.TextPrimary` / `Brand.CyanDim`
- [ ] `AccentColor` fight button → `Semantic.ElementRhythm` hoặc `Brand.RedSelection`

**Acceptance:** Run Map playthrough — legend + nodes + boss gate cảm giác cùng universe với CampusHub; node types vẫn phân biệt được.

---

## Epic 4 — Combat Neon alignment (P1)

**Không regen PNG v4/v3/v4 lần này** — chỉ code tint + label color.

| File | Thay đổi |
|------|----------|
| `SkillRadialSlotView.cs` | HighlightColor → `Brand.CyanNeonBody`; frame gold → `Brand.CyanHover` hoặc giữ gold → `Semantic.EventGold` |
| `DamageNumberPopupView.cs` | DamageColor → `Semantic.Damage`; Crit → `Semantic.Crit` |
| `CounterMultiBannerView.cs` | label gold → `Semantic.Crit` hoặc `Brand.TextPrimary` |
| `BossNoteClusterView.cs` | hot pink → `Brand.MagentaAccent` / outline tokens |
| `BeatTimelineUIView.cs` | boss frame borders → cyan+magenta pair; giảm tricolor đỏ tím nếu không semantic |
| `CombatUiHierarchy.cs` | HP fill green → `Semantic.Heal` dim hoặc cyan bar (pick: **cyan fill** cho party, green heal only) |
| `UiButtonHoverFeedback` on combat buttons | already epic 1 |

### Task 4.1: Combat tint pass

- [ ] Implement table above
- [ ] Play CombatPrototype: skill select, damage pop, counter, boss track, deploy hint

**Acceptance:** Combat HUD không còn cam `(0.95, 0.62, 0.25)` trên skill chrome; damage/heal/crit đi qua Semantic.*

---

## Epic 5 — Harmony + gold dedup (P2)

### Task 5.1: `HarmonyElementPalette` → tokens

- [ ] `GetBorderColor` return `FcColorTokens.Semantic.Element*`
- [ ] Icon disc colors match

### Task 5.2: Calendar gold merge

- [ ] `CalendarOverlayUI.EventGold` → `Semantic.EventGold`
- [ ] Verify contrast on `PanelBlue`

**Acceptance:** Element badges match stat radar; calendar event dots match crit/skill gold family.

---

## Epic 6 — Scene bake & regression (P0)

### Task 6.1: Editor menu (optional helper)

- [ ] `Fractured Chorus/UI/Apply Color Tokens To Active Scene` — walk `Graphic` + `Text`, map by component name rules (chỉ dev aid, không bắt buộc MVP)

### Task 6.2: Manual scene check

- [ ] MainMenuStartGame — menu hover/selection
- [ ] CampusHub — full overlay stack
- [ ] RunMapPrototype — legend + gate
- [ ] CombatPrototype — skill + result + deploy
- [ ] OpeningInvestigation / FlowerShop — VN choice readability on BG

### Task 6.3: Update regression doc

- [ ] Add section **Palette** to `POST_DEFENSE_POLISH_REGRESSION.md`:
  - Hub cyan consistent
  - Map not StS-green dominant
  - Combat skill highlight not orange
  - Selection red visible
  - Damage pink ≠ UI magenta accent same frame

**Acceptance:** Checklist signed off 5/5 scenes.

---

## Epic 7 — Art follow-up (out of scope MVP, track separately)

| Item | Khi nào |
|------|---------|
| Regen left-rail PNG theo Neon spec | Sau code tint stable |
| Run Map node ring sprite (optional) | Nếu stroke-only chưa đủ |
| Status menu mock parity (red shard panels) | Art pass Persona |
| Flower shop warm BG grade + UI overlay tint | Color grading pass |

---

## Execution order (sprint-sized)

```
Week A (P0): Epic 0 → Epic 1 → Epic 3 → Epic 6 partial
Week B (P1): Epic 2 → Epic 4 → Epic 6 complete
Week C (P2): Epic 5 + art follow-up triage
```

**Commit strategy:** 1 commit per epic (Summary focus why):

1. `Introduce FcColorTokens as UI color source of truth`
2. `Align hub overlays to shared color tokens`
3. `Remap run map palette to brand bridge colors`
4. `Unify selection accent to Persona red lock`
5. `Align combat UI tints to Neon Cadence semantics`

---

## Risk & rollback

| Risk | Mitigation |
|------|------------|
| Selection red clash VN BG | Tune alpha; outline giữ `VnDialoguePanelLayout.TextOutlineColor` |
| Map nodes khó phân biệt sau remap | A/B screenshot old vs new; giữ Δhue ≥30° giữa types |
| Serialized scene colors stale | Scene bake menu; Play mode bootstrap không đủ — cần mở scene save |
| Too many tokens | YAGNI: chỉ 4 nested classes, không factory pattern |

Rollback: revert epic commit; tokens file isolated.

---

## Definition of Done

- [ ] `grep "0\.831f, 1f" Assets/FracturedChorus` → chỉ `FcColorTokens.cs`
- [ ] `grep "#AA4E49\|#82B366" Assets` → 0 (StS hex removed)
- [ ] `docs/ui/COLOR_TOKENS.md` exists and linked from plan
- [ ] Regression palette section pass 5 scenes
- [ ] Design lock selection rule documented and implemented in ≥3 UI surfaces

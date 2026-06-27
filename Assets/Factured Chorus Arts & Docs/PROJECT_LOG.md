# Project log — Fractured Chorus (design workspace)

**Canonical design assets:** `F:\Factured Chorus` (draw.io, art concept, briefs)  
**Canonical Unity code:** `F:\Unity_Project\Fractured Chorus`  
**Work log / status (local):** `Assets/Factured Chorus Arts & Docs/` (file này + `PROJECT_STATUS.md`)  
**GitHub mirror (docs only):** `C:\Users\admin\Projects\fractured-chorus\docs\`

Newest first.

---

## 2026-06-27 — Party bar: formation sync, số thẻ phải→trái, sort cùng cột

**Focus:** code (Unity)

**Owner:** Khoa

**Done**
- **`PartyCardDisplayOrder`:** thứ tự logic thẻ theo lưới — **C1 → C2 → C3** (Tank = thẻ 1, Ren = thẻ 2, Mage = thẻ 3); refresh sau kéo formation (`BindFromSession` / `ApplyFormationEntries`).
- **Cùng cột:** H2 (hàng đỏ **2**) → hàng **1** (trên) → hàng **3** (dưới); map index lưới `0=H3/dưới`, `1=H2`, `2=H1/trên`; hòa → `PartyBarOrder`.
- **`PartyCardLayout`:** spacing **100px**; **thẻ 1 neo ngoài cùng phải**, thẻ 2/3… tăng dần sang trái (`GetCardAnchoredPosition(index, total)`).
- **`PartyStatusBarUIView`:** clone runtime từ `CardTemplate` (tối đa 5); `CardsRow` + HLG tắt lúc Play; `SetSiblingIndex` + reorder không destroy khi cùng tập unit.
- **`CombatPrototypeBootstrap`:** `RefreshPartyStatusBar()` sau init + mỗi lần `BoardDragController` đổi formation — nguồn `DualGrid.PlayerUnits`.
- **Badge hệ:** góc phải trên card offset `(-4,-4)`; icon art `icon_he_nhip|giai_dieu|hoa_am.png` (Rhythm/Melody/Harmony).
- **Editor:** `EnsurePartyCardsInHierarchy` dọn `Card_*` cố định, giữ `CardTemplate` inactive; menu **Apply All Play-Ready Updates**.

**Decisions**
- Thứ tự **logic** thẻ (1→N) = C1→C3; **layout UI** đếm từ phải (thẻ 1 bên phải) — không trùng hướng đọc trái→phải trên màn hình.
- Sort cùng cột theo **số hàng đỏ** trên board, không theo Y Unity thuần (tránh Mage front đè Ren back).

**Next**
- Playtest 4–5 unit, nhiều cột chồng hàng; dead unit trên lưới.
- Sync `docs/PROJECT_LOG.md` (GitHub mirror) nếu lệch entry 2026-06-27 cũ (1.25px / `preserveSceneLayout`).

**Refs:** `PartyCardDisplayOrder.cs`, `PartyCardLayout.cs`, `PartyStatusBarUIView.cs`, `CombatPrototypeBootstrap.cs`, `BoardDragController.cs`, `CombatUiHierarchy.cs`, `SCENE_SETUP.md`

---

## 2026-06-21 — Scene-first cho mọi GameObject (không chỉ Ren)

**Focus:** code

**Done**
- **Rule mở rộng:** mọi object trong Hierarchy (grid, unit, UI, hex…) — chỉnh scene + Save → Play phải khớp.
- `CombatPrototypeBootstrap.respectSceneAuthoring` (default **true**): không snap ô lưới, không di chuyển unit lúc Play.
- `GridCellMarker.preserveSceneVisuals`: không rebuild hex khi chọn object / Play.
- `UnitView.preserveSceneVisuals`: giữ sprite/màu scene.
- `BeatTimelineUIView` / `SkillPanelUIView.preserveSceneLayout`: giữ anchor TrackLine, ScanBar, panel.
- `SceneAuthoringPolicy.cs` + `SCENE_SETUP.md` cập nhật bảng quy tắc.

**Decisions**
- Rebuild layout chỉ qua menu Editor; runtime chỉ wire logic combat.

**Refs:** `CombatPrototypeBootstrap.cs`, `GridCellMarker.cs`, `UnitView.cs`, `BeatTimelineUIView.cs`, `SkillPanelUIView.cs`

---

## 2026-06-21 — Scene = source of truth (bootstrap không ghi đè Hierarchy)

**Focus:** code

**Done**
- **Rule:** object cần thiết phải nằm trong Hierarchy; chỉnh scene (vị trí unit, màu/tắt hex ô, Row/Column) phải khớp Game khi Play.
- **`RegisterSceneUnits`:** bỏ `ConfigureDemo()` (xóa Row/Column); ưu tiên `UnitView.Row`/`Column` từ Inspector → suy ô từ Transform → fallback `DefaultPartyFormation`.
- **`GridCellMarker.PrepareForPlay`:** không rebuild màu hex đã chỉnh; chỉ tạo mesh thiếu; giữ child `Hexagon Flat Top` inactive nếu user tắt trong scene.
- Docs: `SCENE_SETUP.md` cập nhật quy ước index 0–2 = hàng/cột hiển thị 1–3.

**Decisions**
- Bootstrap chỉ snap layout honeycomb (`SnapToLayoutPosition`); không ép formation demo khi scene đã gán ô.

**Next**
- Save scene sau chỉnh; playtest Ren H1 (Row=**0**) vs H2 (Row=**1**).

**Refs:** `CombatPrototypeBootstrap.cs`, `GridCellMarker.cs`, `SCENE_SETUP.md`

---

## 2026-06-23 — Hex board margin + drag-drop formation

**Focus:** code

**Done**
- Honeycomb 3×3 từ `Board margin.drawio` (player); enemy mirror X.
- Drag-drop unit snap ô; highlight neon blue khi hover ô hợp lệ (Planning).
- Files: `HexBoardLayout.cs`, `BoardDragController.cs`, `GridCellMarker` hex mesh.

**Next**
- Playtest formation + combat target columns sau khi đổi vị trí.

**Refs:** `F:\Factured Chorus Arts & Docs\Board margin.drawio`

---

## 2026-06-23 — Enemy telegraph, guard resolve, AV model tách

**Focus:** code

**Done**
- **Timeline đỏ (enemy telegraph):** beat có đòn quái = nền đỏ; trùng beat player vẫn thấy đỏ + label party (`| EN`).
- **Execute scan — Guard:** scan qua beat có Guard → chặn/giảm dmg quái theo beat condition (Early/On/Late/Off); không Guard → dmg thẳng.
- **Execute scan — Priority:** player attack và enemy telegraph **cùng beat** → so **Base AV** (thấp hơn đi trước).
- **AV model tách:** Base AV = **priority only** (Ren ≈ 75, Mage ≈ 80, Tank ≈ 86, Grunt ≈ 100) — **không trừ** khi dùng skill; **Phase AV** (150 phase 1 / 100 sau) = budget số lần ra đòn party.
- **Enemy targeting:** `CombatTargetPicker` — cột có Tank trước → cột giữa (col 1) → cột Mage; Tank chết thì bỏ cột Tank.

**Decisions**
- Guard chỉ active trên **beat được scan** (không tìm guard toàn timeline như trước).
- Telegraph là nguồn hiển thị đỏ UI; agenda enemy phụ trợ resolve.

**Blockers / risks**
- Cần playtest: guard + attack cùng beat với thứ tự Base AV khác nhau.
- Formation scene (Tank C2, Mage C0) ảnh hưởng cột target — chỉnh scene nếu muốn Tank front col 0.

**Next**
- Playtest Execute loop; portrait quái trên slot đỏ; drag skill → beat.

**Refs:** `CombatSession.cs`, `CombatTargetPicker.cs`, `AvResourceSystem.cs`, `BeatSegmentView.cs`, `PROJECT_STATUS.md`

---

## 2026-06-22 — Combat UI polish, skill panel bugfix, formation

**Focus:** code

**Done**
- **Timeline tốc độ khi mở skill panel = 0.25×** — field `skillPanelOpenSpeedMultiplier` wired qua `SetSkillPanelOpen()`; hết warning CS0414.
- **Carousel timeline:** số slot visible **tính động** theo viewport + width `Beat_0` (không cố định 20/25); scan bar gán skill khi tới beat; **bỏ EXECUTE** — auto-play 105 beat rồi `ConfirmPlanning()`.
- **Header timeline:** phase budget `N/10`; AV còn lại / budget phase; phase 1 budget AV **150** (phase sau **100**).
- **Skill panel:** padding **1.5px** phải unit; nhãn Skill 1 / Skill 2; toggle click unit; đóng khi assign thành công; ẩn lúc bootstrap.
- **Bug hai bảng skill nhấp nháy:** gỡ nested `Canvas`/`GraphicRaycaster` trên `SkillPanelUI`; `DestroyImmediate` + panel `inactive` trong scene.
- **Đội hình:** hoán đổi **Tank ↔ Mage** (Tank R2C2, Mage R1C0); **quái căn ô** — `AlignUnitViewToGridCell()` snap theo `GridCellMarker` khi Play.
- **AV label** không còn clip chữ "AV" (widen + overflow).

**Decisions**
- Timeline chậm khi chọn skill: **0.25×** (cân bằng đọc UI vs nhịp combat), chỉnh được trên `BeatTimelineUI` Inspector.
- Không dùng nested Canvas cho overlay panel — chỉ `SetAsLastSibling()` trên root `CombatCanvas`.
- Vị trí unit runtime ưu tiên **marker ô trong scene** hơn công thức `GetWorldPosition` thuần.

**Blockers / risks**
- Scene cũ lưu lúc Play Mode có thể còn nested Canvas — chạy **Fractured Chorus → Rebuild Timeline + Skill Panel** + Save.
- Art/audio vẫn placeholder; Execute phase chưa playtest kỹ.

**Next**
- Playtest Planning → Execute → Upkeep end-to-end.
- Drag skill lên beat; telegraph portrait trên slot.
- Import sprite từ `art assets/`.

**Refs:** `SkillPanelUIView.cs`, `BeatTimelineUIView.cs`, `CombatController.cs`, `CombatPrototypeBootstrap.cs`, `CombatPrototype.unity`, `PROJECT_STATUS.md`

---

## 2026-06-21 — Combat UI: skill panel + timeline carousel

**Focus:** code

**Done**
- **Skill panel:** hiện sát bên phải unit (~10px screen space); click cùng unit lần nữa → toggle tắt; chọn skill → đóng panel **chỉ khi** `TryAssignPlayerAction` thành công (thiếu AV / beat không hợp lệ → panel vẫn mở, cập nhật AV trên title).
- **Beat timeline carousel:** Hierarchy chỉ giữ **`Beat_0` template**; runtime clone **20 slot** visible; logic 105 beat (`TimelineConstants`) vẫn trong `BeatTimelineEngine` — không tạo 105 GameObject trong scene.
- Khi EXECUTE scan: mỗi beat lướt **trái → phải** (carousel): ô trái biến mất, ô mới sinh bên phải với beat tiếp theo; animation ~0.12s.
- Editor `TimelineHierarchyBuilder`: viewport width = 20 slot; wire `segmentTemplate` + `slotsRow` thay array 105 segments.
- Bootstrap: `ToggleForUnit` thay `ShowForUnit`.

**Decisions**
- Carousel slide **mỗi beat scan** (không chờ `ScrollOffset` engine) — khớp UX “thanh trượt” trong mock UI.
- Phase divider luôn có trên template, bật/tắt theo `DisplayBeatIndex` runtime.

**Blockers / risks**
- Scene `CombatPrototype.unity` cũ vẫn có 105 `Beat_X` — cần menu **Fractured Chorus → Rebuild Timeline + Skill Panel (Hierarchy)** rồi Save scene.

**Next**
- Drag skill lên beat cụ thể (thay auto `FindNextEmptyBeat`).
- Telegraph portrait / posture bar trên beat slot.
- Import sprite approved từ `art assets/`.

**Refs:** `BeatTimelineUIView.cs`, `SkillPanelUIView.cs`, `TimelineHierarchyBuilder.cs`, GDD Beat Timeline

---

## 2026-06-21 — Combat prototype Unity (vertical slice 2)

**Focus:** code

**Done**
- Mở rộng timeline **15 → 105 beat** (phase 1 = 15 slot, phase 2–10 = 10 slot/phase); `BeatTimingResolver` Early/On/Late/Off vs enemy telegraph.
- Model **AV**: Ren base 75; 4 skill slot (Basic 0 / Skill 25 / Ultimate 50 / Guard 0); log Console `[AV]`.
- **Hierarchy-first UI:** menu Editor `Setup Combat Scene Hierarchy` — grid 3×3 có viền ô, party cột 0 = phải (front), timeline + skill panel trên Canvas.
- Fix **Input System**: `CombatInputSetup` + `InputSystemUIInputModule`; `UnitView` `IPointerClickHandler`; menu Fix EventSystem.
- `GridCellMarker`, `SimpleEnemyAI` telegraph, Guard giảm dmg theo beat condition.
- Folder structure đầy đủ: RunMap, Narrative, Audio, Art, Prefabs + README từng module.

**Decisions**
- Logic chỉ trong `.cs`; scene YAML chỉ reference + layout (rule từ vertical slice 1).
- Timeline UI ban đầu tạo 105 segment trong Hierarchy — **đã thay** bằng template + carousel (entry trên).

**Blockers / risks**
- UnityConnect token errors — bỏ qua khi dev local.
- Art placeholder; chưa link sprite từ `F:\Factured Chorus\art assets\`.

**Next**
- (Đã làm một phần) carousel 20 beat + skill panel toggle — xem entry cùng ngày phía trên.

**Refs:** `docs/setup/UNITY_WORKFLOW.md`, `Assets/FracturedChorus/Scenes/SCENE_SETUP.md`

---

## 2026-06-21 — Combat prototype Unity (vertical slice 1)

**Focus:** code

**Done**
- Unity project `F:\Unity_Project\Fractured Chorus`: cấu trúc `Assets/FracturedChorus/` (Combat, UI, Data).
- Model stats chung `UnitStats` + preset Ren/Tank/Mage/Grunt; `DamageCalculator` theo doc damage.
- `DualGrid` 3×3 — player max **5**, enemy max **9**; placeholder `UnitView`.
- `BeatTimelineEngine` 15 beat; `CombatSession` Planning → Execute → Upkeep.
- UI: timeline bar + EXECUTE + skill panel click unit.
- Bootstrap runtime; `EncounterRuntimeFactory` demo 3 player vs 2 grunt.

**Decisions**
- Code canonical chỉ Unity project; GitHub repo giữ docs/log.
- Enemy max 9 (full 3×3) — ghi nhận lệch GDD max 5/bên.

**Refs:** GDD v2.2, OOAD UC-03/04

---

## 2026-06-03 — Phân công Phase 1 (cập nhật)

**Focus:** production

**Done**
- Cập nhật TEAM, MILESTONES, Linear theo phân công mới.

**Decisions**
- Thiên: story Google Doc; Kiên + Tính: art + SFX; Khoa: story flow draw.io, OOAD Notion, work log.

**Refs:** `docs/TEAM.md` (GitHub repo)

---

## 2026-06-02 — Bootstrap hệ thống làm việc

**Focus:** production

**Done**
- Stack: Notion · Linear · Google Doc + draw.io · GitHub.
- Repo `fractured-chorus`, template log, milestone phase 1, asset inventory.
- Index: GDD v2.2, OOAD, Story, draw.io trên `F:\OOAD&UML\`.

**Decisions**
- Một nguồn sự thật mỗi loại nội dung; assets phase 1 web-sourced + ghi license.

**Refs:** `README.md`, `docs/setup/`

---

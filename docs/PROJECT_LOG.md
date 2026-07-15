# Project log — Fractured Chorus

**Canonical (text):** file này trên **GitHub** `fractured-chorus/docs/PROJECT_LOG.md`  
**Notion (team log):** https://app.notion.com/p/37441bb3f2a281768901eb58a16bc252  
**Snapshot:** [`PROJECT_STATUS.md`](PROJECT_STATUS.md) · **Quy trình ghi log:** [`LOGGING.md`](LOGGING.md)  
**Unity code:** `F:\Unity_Project\Fractured Chorus` (log combat tóm tắt ở đây cho tới khi Unity vào Git)

Newest first.

---

## 2026-07-15 — Combat UX: ẩn hex Enemy + alpha hit-test Deploy/Execute

**Focus:** code (Unity) · docs

**Done**
- **Hex floor:** Enemy luôn ẩn; Player chỉ hiện lúc Deploy reposition; sau Deploy ẩn cả hai (`ApplySlotFloorVisibilityForCurrentPhase`).
- **Nút Deploy/Execute:** `alphaHitTestMinimumThreshold = 0.1` — click chỉ trên vùng opaque của sprite; bật Read/Write trên `combat_btn_deploy_v1` / `combat_btn_execute_v1`.

**Refs:** `CombatController.cs`, `CombatExecuteOverlayUIView.cs`, `BoardDragController.cs`, `GridCellMarker.cs`

---

## 2026-07-05 — Combat: skill UI scene-first, 2-phase round, footprint overlap

**Focus:** code (Unity) · editor · docs · verify

**Owner:** Khoa

**Done**
- **Skill panel Hierarchy-first:** `SkillPanelUI/Radial/SkillSlot_{Top,Left,Right}`; menu **Setup Skill Panel in Hierarchy** + Apply All.
- **Bỏ slow-mo panel, center token, arm-at-scan;** click/W/A/D highlight; gán chỉ kéo-thả.
- **`SkillFootprintUtil`** — overlap enforce + drag preview S1/S/S2.
- **Quái 2 pha** S1 wind-up + S impact; **round 2 timeline phase** → nút Execute.

**Refs:** `SkillFootprintUtil.cs`, `SkillPanelUIView.cs`, `BeatTimelineUIView.cs`, `CombatController.cs`, `SimpleEnemyAI.cs`

---

## 2026-07-05 — Combat: intro-pause, Deploy/Continue, scene sync + audit fixes

**Focus:** code (Unity) · scene · docs · verify script

**Owner:** Khoa

**Done**
- **Intro-pause theo vị trí vạch quét:** `BeatTimelineUIView.PlanningPauseLocalBeat = 0.5` (hằng số code) — beat 0 kêu + lướt qua vạch, dừng **trước** beat 1 chạm vạch. `TryEnterPlanningPauseByLocalBeat()` trong scan loop; `CombatMusicController.PausePlayback()` / `ResumePlayback()`.
- **Nút Deploy → Continue:** `CombatController` ép nhãn runtime (`DeployLabel`/`ResumeLabel`); bỏ `CombatExecuteOverlayUIView.Start()` re-bind (tránh ghi đè pause flow). Sau intro-pause hiện **Continue**; auto-resume khi cả đội đã xếp skill hoặc bấm tay.
- **Footprint 3 pha trên lane:** `RefreshFootprintDots` — S1/S2 tròn xám · S chip màu unit; refresh qua `RefreshLaneMarkers()` (không rebuild 619 slot mỗi lần gán skill).
- **Luật quái:** `TimelineConstants.EnemyFirstAttackBeat = 2` — telegraph chỉ từ beat thứ 3.
- **Scene `CombatPrototype.unity`:** wire `beatMap` + `beatMapCsv`; nhãn ExecuteButton **Deploy**; xóa `unitViews` null; xóa `HealthBarFill` mồ côi ở scene root.
- **Editor Apply All:** gộp wire combat music, prune null unitViews, Deploy label, orphan cleanup; bỏ reference `respectSceneAuthoring` (field đã xóa).
- **Timeline ↔ Controller:** callback `onPlanningPause` / `onConfirmPlanning` trong `BeatTimelineUIView.Bind()` — bỏ `FindAnyObjectByType<CombatController>()`.
- **Scene-first UI sizing:** `RectSizeUtil` — party/enemy card, badge, skill panel đọc kích thước từ scene; fallback constants chỉ khi chưa authored.
- **Verify script:** `scripts/verify_combat_scene_sync.py` cập nhật checklist (beat map, Deploy, null unitViews, overlay Start bind).

**Decisions**
- Pause timing = **phân số localBeat**, không dùng serialized field trên scene (tránh Unity ghi đè giá trị cũ).
- Không hiện điểm tròn trống trên lane — chỉ footprint khi player **đặt skill**.
- Nhãn nút do `CombatController` làm chủ duy nhất; scene YAML + Apply All chỉ để đồng bộ Inspector.

**Refs:** `UI/BeatTimelineUIView.cs`, `Combat/Core/CombatController.cs`, `UI/CombatExecuteOverlayUIView.cs`, `Audio/CombatMusicController.cs`, `Editor/CombatSceneSetupEditor.cs`, `Editor/CombatMusicSceneSetup.cs`, `UI/RectSizeUtil.cs`, `Scenes/CombatPrototype.unity`, `docs/combat/COMBAT_MECHANICS.md`, `scripts/verify_combat_scene_sync.py`

---

## 2026-07-03 — Combat: intro-pause design lock + footprint UI

**Focus:** code (Unity) · docs

**Owner:** Khoa

**Done**
- Thiết kế intro-pause: Deploy → nhạc chạy → pause cho player set up skill → Continue / auto-resume.
- Footprint S1/S/S2 trên lane; đổi tên skill asset theo `SKILL_KIT.md` (Crosscut, Anchor, Bulwark, Mend, Encore).
- `SkillUiNames` hiện `displayName` thật thay vì placeholder Skill 1/2.

**Refs:** `docs/combat/SKILL_KIT.md`, `Resources/Skills/*.asset`

---

## 2026-07-01 — Combat: reset stat units (Lv15) + canh Y thẻ quái theo players

**Focus:** code (Unity) · data

**Owner:** Khoa

**Done**
- **Reset stat 3 nhân vật về baseline Lv15 optimal** (theo `docs/combat/CHARACTER_LEVEL_PROGRESS.md`):
  - **Ren** (Melody · Physical): STR 42 · EN 10.8 · HB 167 · Luck 18% · Crit ×1.35 · HP 114.
  - **Charlotte/Tank** (Rhythm · Physical): STR 35 · EN 18.2 · HB 127 · Luck 8% · Crit ×1.15 · HP 260.
  - **Coda/Mage** (Harmony · **Magical**): STR(attack power)=Ma 50 · EN 9.8 · HB 147 · Luck 16% · Crit ×1.3 · HP 73.
  - Sửa ở cả 3 nguồn: `Resources/StatBlocks/StatBlock_{Ren,Tank,Mage}.asset`, fallback `UnitStats.CreateRen/Tank/MagePreset`, và editor `CombatDataAssetGenerator`. Mage đổi `strengthType` → **Magical**. HB khớp W (8/8/7) & latency (1/1/1) trong COMBAT_MECHANICS §3.
- **Thẻ quái canh cùng Y với thẻ players:** `EnemyStatusBarUIView` dùng **kích thước thẻ = template party (115×167)** + gap `PartyCardLayout.CardGap`, thẻ top-aligned (pivot 1,1). Bootstrap `AlignEnemyBarToPartyY()` copy trục Y (anchor/pivot/height/anchoredPosition.y) từ thanh party sang thanh quái, giữ cạnh phải cho quái ⇒ 2 hàng thẻ cùng đỉnh. `MaxEnemyCards` 9 → **6** (khớp `DualGrid.MaxEnemyUnits`).
- **Rà soát cơ chế timeline beats (docs §2–§3):** xác nhận timeline 1 hàng beat + lane theo nhân vật (đã dựng) khớp thiết kế; boss notes ở hàng beat chung. Planning Window **W** + footprint **S1/S/S2** vẫn là P0 chưa implement (theo mục "Chưa làm" trong SKILL_KIT) — không đổi lần này.

**Refs:** `Resources/StatBlocks/StatBlock_*.asset`, `Combat/Units/UnitStats.cs`, `Editor/CombatDataAssetGenerator.cs`, `Data/ScriptableObjects/Presets/README.md`, `UI/EnemyStatusBarUIView.cs`, `Combat/Bootstrap/CombatPrototypeBootstrap.cs`

---

## 2026-07-01 — Combat: timeline lanes theo nhân vật + kéo skill vào lane

**Focus:** code (Unity)

**Owner:** Khoa

**Done**
- **Dòng kẻ (lane) cho từng nhân vật:** `BeatTimelineUIView` giữ 1 hàng cột beat, overlay `LaneLines` + `LaneMarkers` dưới viewport. Mỗi party member còn sống (từ `Grid.PlayerUnits`, tối đa 4) = 1 lane ngang, cách đều theo chiều cao, tô màu/nhãn theo unit. Lane rebuild động khi đội hình đổi.
- **Player action → marker trên lane:** action người chơi không vẽ trong ô beat nữa. `TimelineLaneMarkerView` mới (chip tròn: nền màu unit, glow theo `ActionGlowType`, nhãn skill) đặt tại `(beat x, lane y)`, có animation bay vào lane (~0.18s). `RefreshLaneMarkers` reuse marker theo key `(unit,beat)` — chỉ animate entry mới, scroll/refresh không animate lại. Markers layer sync x với `slotsRow` khi cuộn.
- **Ô beat chỉ còn boss telegraph:** `BeatSegmentView.SetSlot` bỏ vẽ player entry, chỉ render telegraph quái + trạng thái scan/rỗng.
- **Kéo skill từ radial → thả vào lane:** `SkillRadialSlotView` thành drag source (`IBeginDrag/IDrag/IEndDrag`) + ghost bám con trỏ. `SkillPanelUIView` thêm callback `onSkillDroppedAtScreen` / preview / drag-end; giữ nguyên đường click + phím W/A/D.
- **Controller wiring:** `CombatController.AssignSkillAtScreenPoint` (check Phase AV → `TryGetBeatAtScreenPoint` → `TryAssignPlayerAction`), preview qua `ShowDropGhost`/`HideDropGhost`. Click/phím vẫn auto-gán tại scan beat rồi animate vào lane.

**Decisions**
- Lane count = số party member **đang sống** (không lane cho quái — quái vẫn nằm hàng beat chung).
- Hỗ trợ **cả hai** cách đặt skill: kéo-thả tay + click/phím auto tại beat hiện tại.
- Layer lane tạo runtime dưới `Viewport`, không bắt buộc sửa scene.

**Refs:** `UI/BeatTimelineUIView.cs`, `UI/TimelineLaneMarkerView.cs` (mới), `UI/BeatSegmentView.cs`, `UI/SkillRadialSlotView.cs`, `UI/SkillPanelUIView.cs`, `Combat/Core/CombatController.cs`, `docs/combat/COMBAT_MECHANICS.md`

---

## 2026-07-01 — Combat: grid 2×3, party 4, party bar resize, fix kéo tank

**Focus:** code (Unity)

**Owner:** Khoa

**Done**
- **Grid 2 hàng × 3 cột:** `DualGrid.Rows = 2` (cột giữ 3), bỏ hàng dưới cũ. `GridPosition.IsValid` theo `DualGrid.Rows/Columns`; `UnitView.IsPlacedOnGrid` dùng `GridPosition.IsValid`. `HexBoardLayout` còn 2 hàng (hàng đơn vị index 1 giữ y=0, hàng còn lại phía trên). **Scene = nguồn chuẩn:** xoá hẳn ô hàng dưới cũ + đặt lại vị trí ô/unit ngay trong scene qua menu **Rebuild Hex Board Grid (scene)** (`Undo.DestroyObjectImmediate` ô ngoài phạm vi). Bootstrap chỉ `PrepareForPlay` ô hợp lệ, không snap/không ghi đè Transform (ẩn an toàn nếu còn sót ô thừa).
- **Party tối đa 4:** `DualGrid.MaxPlayerUnits = 4`, `MaxEnemyUnits = 6`, `PartyStatusBarUIView.MaxPartyCards = 4`.
- **Party status bar 713×167, thẻ 115×167, khoảng cách 2.0px:** `PartyCardLayout.CardWidth=115`, `CardGap=2`, `CardStepX=117`; scene `PartyStatusBarUI`/`CardTemplate` sizeDelta + LayoutElement; `cardSpacing=2`. Editor `TimelineHierarchyBuilder` cập nhật hằng số. *(2026-07-01: gap 0.75→1.5→2.0)*
- **Toạ độ board cố định:** quét scene → chốt toạ độ 12 ô (world + local) trong `docs/combat/BOARD_GRID_LAYOUT.md`; scene khớp 100% `HexBoardLayout` ⇒ không trôi sau mỗi lần gen.
- **Menu Rebuild Hex Board đổi hành vi:** nhóm ô theo Y, **giữ 2 hàng trên (top+units), xoá hàng dưới cùng**, re-index về R0(top)/R1(units) + snap toạ độ đã lưu + SetActive ô giữ lại. *(2026-07-01: theo yêu cầu xoá R0 dưới, hiện R2 top)*
- **Fix kéo tank (Charlotte):** sprite lớn nhưng BoxCollider2D thân hẹp → bấm dễ trượt. `BoardDragController.PickUnitAtScreen` thêm fallback `PickNearestUnit` (chọn unit gần con trỏ nhất trong `cellPickRadius` khi OverlapPoint trượt).

**Decisions**
- **Sửa trong scene, không phải runtime:** ô hàng dưới bị xoá hẳn khỏi scene (không `SetActive(false)` lúc Play). Menu **Rebuild Hex Board Grid (scene)** dọn ô thừa + snap ô/unit về margin 2×3, sau đó lưu scene. Play tôn trọng layout scene.
- Idle clip của tank chỉ key `m_Sprite` (không key vị trí) → Animator không phải nguyên nhân lỗi kéo.

**Refs:** `Combat/Grid/DualGrid.cs`, `GridPosition.cs`, `HexBoardLayout.cs`, `Combat/Bootstrap/CombatPrototypeBootstrap.cs`, `UI/UnitView.cs`, `BoardDragController.cs`, `PartyCardLayout.cs`, `PartyStatusBarUIView.cs`, `Editor/TimelineHierarchyBuilder.cs`, `Scenes/CombatPrototype.unity`

---

## 2026-06-29 — Combat: skill panel radial, thẻ quái, guard Spacebar

**Focus:** code (Unity)

**Owner:** Khoa

**Done**
- **Skill panel radial:** bỏ list nút dọc → 3 ô tròn **Top=W / Left=A / Right=D** quanh tâm. Bảng nổi **trên đầu nhân vật** đang chọn (pivot đáy-giữa, clamp trong canvas). Giữ tính năng cũ: title, dismiss backdrop, slow-mo 0.25× khi mở, toggle, kiểm tra Phase AV. (`SkillPanelUIView`, `SkillRadialSlotView`, `SkillCenterTokenView`)
- **Kéo token chọn skill:** object tròn ở tâm — kéo-thả vào ô để arm skill (ngoài click + phím W/A/D).
- **Thẻ quái góc phải trên:** `EnemyStatusBarUIView` xếp ngang (mọc phải→trái), tái dùng `PartyMemberCardView` (avatar, bar máu, badge hệ) — bind từ `Grid.EnemyUnits`. Bootstrap tự tạo runtime, mượn CardTemplate của party bar.
- **Guard = giữ Spacebar:** bỏ ô Guard khỏi bảng skill. Đòn quái nay **defer tới cuối beat đỏ** mới resolve; block nếu người chơi giữ Space **liên tục từ đầu beat** (`GuardInputController.HeldThroughBeatSince`). `CombatSession` dùng pending-hit + `GuardHeldSinceQuery`. Mặc định chặn 100% damage (chỉnh được).

**Decisions**
- Guard không còn là skill/beat assignment; lọc `IsGuard` khỏi panel nên unit còn đúng 3 skill → khớp 3 ô radial.
- Đòn quái resolve ở **cuối beat** (không phải đầu beat) để "giữ Space trọn beat đỏ" mới chặn được — fix lỗi guard không ăn.
- Block full (remaining 0) theo yêu cầu "giữ hết beat = block damage"; `blockedDamageRemaining` để tinh chỉnh.

**Refs:** `UI/SkillPanelUIView.cs`, `UI/SkillRadialSlotView.cs`, `UI/SkillCenterTokenView.cs`, `UI/EnemyStatusBarUIView.cs`, `Combat/Core/GuardInputController.cs`, `Combat/Core/CombatSession.cs`, `Combat/Core/CombatController.cs`, `Combat/Bootstrap/CombatPrototypeBootstrap.cs`

---

## 2026-06-28 — Run Map: refactor + tối ưu code

**Focus:** code (Unity) + docs

**Owner:** Khoa

**Done**
- **`RunMapLayoutMetrics`:** tách layout/spacing/content size khỏi `RunMapUIView`.
- **`MapGraph`:** lookup `(floor, column)` O(1); bỏ LINQ `FindNode`.
- **`MapGenerator`:** gom `BuildGraphFromPaths`; `GenerateFromTemplate()` + weights từ `MapTemplateSO`.
- **`RunState`:** `HashSet` visited + `IsVisited()` — không rebuild HashSet mỗi refresh.
- **`MapConnectionLineView`:** `BindEdge(from, to)` — bỏ parse tên GameObject.
- **`RunMapUIView`:** cache màu edge, font label; bỏ field dead (`connectionsLayer`, `ApplyAuthoringPolicy`).
- **`RunMapBootstrap`:** gọn seed + generation qua template API.

**Refs:** `RunMap/`, `RUNMAP_SCENE_SETUP.md`

---

## 2026-06-28 — Run Map: layout scroll, procedural seed, edge sync

**Focus:** code (Unity) + docs

**Owner:** Khoa

**Done**
- **Layout bottom-origin:** `MapContent` + layers anchor/pivot `(0.5, 0)` — F1 đáy, F16 trên; `fitToViewport` scale spacing theo scroll viewport.
- **Edge ↔ node sync:** `MapConnectionLineView` anchor đáy (fix lệch ~F10); lines spawn trong `NodesLayer` cùng node — scroll đồng bộ.
- **Procedural mặc định:** `MapTemplate_Default` — `useReferenceDemoOnPlay=0`, `randomizeSeedOnPlay=1`; path unique hash + mutate; log seed Console.
- **Path UX:** visited edge cam đậm, preview cam nhạt; auto-scroll theo node; boss **58px** hội tụ F15.
- **Docs:** `RUNMAP_SCENE_SETUP.md`, `RunMap/README.md`, `PROJECT_STATUS`, mirror repo.

**Decisions**
- Demo reference (`STS_PATHS`) chỉ khi bật flag Inspector — không phải Play default.
- Không dùng center anchor cho connection lines trên bottom-pivot layer.

**Refs:** `RUNMAP_SCENE_SETUP.md`, `RunMapPrototype.unity`, `MapTemplate_Default.asset`

---

## 2026-06-28 — Unity: Run Map prototype scene (StS clone 7×15 + F16)

**Focus:** code (Unity) + docs

**Owner:** Khoa

**Done**
- **`RunMap/` module:** `MapGenerator`, `MapGraph`, `RunState`, `NodeTypeAssigner`, `PathValidator`.
- **UI:** `RunMapUIView`, `MapNodeView`, `MapConnectionLineView` — scroll map dọc, click path.
- **Scene:** menu **Create Run Map Prototype Scene** → `RunMapPrototype.unity`.

**Decisions**
- Node naming FC: Battle / Event / Elite / Camp / Relay / Treasure / Boss Oni.

**Refs:** `RunMap/README.md`, `RUNMAP_SCENE_SETUP.md`

---

## 2026-06-27 — Unity: folder animation theo tên canon (Coda · Ren · Charlotte)

**Focus:** art pipeline + docs

**Owner:** Khoa

**Done**
- Tạo `Assets/FracturedChorus/Art/Characters/{Coda,Ren,Charlotte}/Animation/` với subfolder `Idle|Move|Attack|Hit|Death`.
- Thêm `Art/Characters/README.md` — map role combat ↔ tên canon ↔ `UnitPreset_*` / `StatBlock_*`.
- Thêm `_Reference/` cho sprite LoR tạm (không ship).
- Cập nhật `Art/README.md`, `Assets/FracturedChorus/README.md`, `docs/setup/UNITY_WORKFLOW.md`.

**Decisions**
- Folder Unity đặt theo **tên nhân vật** (Coda = Mage, Charlotte = Tank); preset SO vẫn giữ suffix role (`UnitPreset_Mage`, `Card_Mage`, …).

**Refs:** `Art/Characters/README.md`, `UNITY_WORKFLOW.md`

---

## 2026-06-27 — Party bar: hierarchy-first, spacing 1.25px, icon hệ

**Focus:** code (Unity) + docs

**Owner:** Khoa

**Done**
- **Rule docs:** không code trên scene; GameObject UI/combat phải hiện trong Hierarchy — cập nhật `SCENE_SETUP.md`, `UNITY_WORKFLOW.md`, `SceneAuthoringPolicy`.
- **`PartyStatusBarUIView`:** `preserveSceneLayout` (mặc định bật) — bind thẻ `Card_*` có sẵn trong `CardsRow`, **không** dịch layout khi di chuyển unit; spacing **1.25px** (`PartyCardDisplayOrder.BarSlotSpacing`).
- **`PartyCardDisplayOrder`:** thứ tự cố định trái→phải Mage · Ren · **Tank** (Tank ngoài cùng phải) — bỏ sort theo cột lưới (fix thẻ nhảy sang phải khi swap ô).
- **Icon hệ:** import `Art/UI/icon_he_nhip|giai_dieu|hoa_am.png`; menu **Apply Element Badge Icons** → `StatBlock_Tank/Ren/Mage`.
- **Editor:** **Setup Party Cards in Hierarchy**; gộp vào **Apply All Play-Ready Updates**.
- **Docs mirror:** sync `docs/` sang `F:\Unity_Project\Fractured Chorus\docs\`.

**Decisions**
- Thẻ party = scene objects (`Card_Mage`, `Card_Ren`, `Card_Tank`), không runtime Instantiate trừ fallback scene trống.
- Icon badge art thay vòng tròn procedural khi `elementBadgeIcon` đã gán.

**Refs:** `PartyStatusBarUIView.cs`, `PartyCardDisplayOrder.cs`, `ElementBadgeIconSetup.cs`, `CombatUiHierarchy.cs`

---

## 2026-06-26 — Party status bar UI, formation swap, scene hygiene

**Focus:** code (Unity) + docs

**Owner:** Khoa

**Done**
- **Party status bar** góc trái trên (`CombatCanvas/PartyStatusBarUI`): clone tối đa **5** thẻ lúc Play từ `CardTemplate` (scene-first, không prefab).
- **`PartyStatusBarUIView`:** layout thẻ **thủ công** — khoảng cách `cardSpacing = 1px` giữa khung; anchor clone **góc trái trên**; refresh sau đổi formation.
- **`PartyMemberCardView`:** avatar (`UnitPresetSO.portraitSprite` / `battleSprite`), bar HP (anchor scale), badge hệ tròn (`ElementBadge/ElementIcon`); màu khung theo `HarmonyElement`.
- **`PartyCardDisplayOrder`:** cùng hàng → cột giảm dần (Mage → Ren → Tank); cùng cột → `PartyBarOrder` (thứ tự đặt lên lưới).
- **`BoardDragController` + `DualGrid.TrySwapUnits`:** kéo lên ô đồng đội → **hoán đổi** vị trí; refresh party bar sau swap.
- **`HarmonyElementPalette`**, **`UiCircleSpriteUtil`:** màu/icon hệ + sprite tròn procedural (Unity 6 không dùng `Knob.psd`).
- **Editor menus:** Add / Fix Party Status Bar, Upgrade Party Card Template, **Find / Remove Missing Scripts (Active Scene)**.
- **Scene fix:** `CardTemplate` inactive; `CardsRow` spacing 1; gỡ UTF-8 BOM khỏi `CombatPrototype.unity` (tránh lỗi import).

**Decisions**
- Bar party **chỉ** dưới `CombatCanvas` — không đặt trên `Background canvas`.
- Spacing 1px — layout code, không phụ thuộc `HorizontalLayoutGroup` lúc Play (HLG tắt trên `CardsRow`).
- `RoleBadge` (hình thoi) **bỏ** — chỉ badge hệ tròn góc phải trên.

**Refs:** `PartyStatusBarUIView.cs`, `PartyMemberCardView.cs`, `CombatUiHierarchy.cs`, `TimelineHierarchyBuilder.cs`, `SCENE_SETUP.md`

---

## 2026-06-25 — Scene-first layout, sprite import, input drag/click

**Focus:** code (Unity) + merge

**Owner:** Khoa

**Done**
- **Scene = source of truth:** `SceneAuthoringPolicy`, `CombatPrototypeBootstrap.respectSceneAuthoring`, `UnitView.preserveSceneCollider` — Play không ghi đè Transform/collider đã chỉnh Hierarchy.
- **Input unit:** `BoardDragController` dùng `Physics2D.OverlapPoint` + Input System (không EventSystem trên world unit) — **trước EXECUTE** giữ-kéo đổi ô; **sau EXECUTE** click mở skill panel.
- **`UnitFeetAnchor`:** child Transform snap grid; **không** collider (tránh cướp raycast).
- **`CombatInputSetup`:** `Physics2DRaycaster` trên Main Camera, `maxRayIntersections = 32`.
- **Import art:** 3 sprite combat (`UnitPreset_Ren/Tank/Mage` + `battleSprite` trên `UnitPresetSO`).
- **UI pre-battle:** timeline/skill panel khóa cho tới khi bấm EXECUTE (`2cb196d`).
- **`SkillPanelDismissBackdrop`:** click nền đóng panel.
- Editor **Apply All Play-Ready Updates** + scene backup `Assets/SceneBackup/`.
- Merge `origin/main` (PR #5 Fixing_layout) — resolve conflict 5 file.

**Decisions**
- BoxCollider2D trên **unit root** = hit target click/drag; kích thước/offset giữ theo scene (`preserveSceneCollider`).
- Skill panel anchor = `bodyCollider.bounds.max.x`.

**Refs:** `BoardDragController.cs`, `UnitFeetAnchor.cs`, `CombatSceneSetupEditor.cs`, `CombatPrototype.unity`

---

## 2026-06-24 — EXECUTE phase, stat blocks, damage & targeting

**Focus:** code (Unity)

**Owner:** Khoa

**Done**
- **Nút EXECUTE:** `CombatExecuteOverlayUIView` — chỉ hiện lúc Planning + `AllowPlayerReposition`; bấm → `LockPlayerReposition()` → timeline quét, nhạc boss.
- **Hai giai đoạn Planning:** (1) sắp xếp formation kéo-thả; (2) sau EXECUTE gán skill + click unit.
- **`UnitStatBlockSO`:** element (Harmony pre-condition), Physical/Magical strength, endurance, heartBeat, baseLuck (% crit), critMultiplier, maxHp, baseSpeed.
- **Resources pipeline:** `Resources/StatBlocks/`, `Skills/`, `UnitPresets/` + menu **Create Default Stat Blocks & Presets** (`CombatDataAssetGenerator`).
- **`DamageCalculator`:** tier random × strength × 10 × endurance factor × beat × harmony × crit.
- **`CombatTargetPicker`:** quái đánh cột **C1 → C2 → C3**; trong cột ưu tiên Tank → DPS → Mage.
- **Board layout:** margin honeycomb + chỉnh row/column hiển thị unit (`HexBoardLayout`, `GridCellMarker`).
- **UI rename:** `SkillUiNames` — slot Skill → "Skill 1", Ultimate → "Skill 2".

**Decisions**
- Stats tách **Stat Block** (dùng chung) khỏi **Unit Preset** (sprite + skill list).
- Không mở skill panel / không quét timeline trước EXECUTE.

**Refs:** `CombatController.cs`, `CombatSession.cs`, `UnitStatBlockSO.cs`, `Presets/README.md`, PR #4 KayDBranch

---

## 2026-06-23 — Hex board margin + drag-drop formation

**Focus:** code (Unity)

**Done**
- Layout honeycomb 3×3 từ `Board margin.drawio` — **player**; **enemy mirror X**.
- `GridCellMarker`: viền hex + fill; **DropGlow neon blue** khi kéo unit tới ô hợp lệ (Planning).
- `BoardDragController` + `UnitView` drag: snap unit vào ô; `DualGrid.TryMoveUnit`.
- Editor menu **Rebuild Hex Board Grid (scene)**.

**Refs:** `HexBoardLayout.cs`, `BoardDragController.cs`, `docs/diagrams/Board-margin.drawio`

---

## 2026-06-23 — Team 2 người + fix slow timeline khi skill panel

**Focus:** production + code (Unity)

**Owner:** Khoa

**Done**
- **Team:** Kiên, Tính rời dự án — cập nhật `TEAM.md`, `WORKFLOW`, `LOGGING`, broadcast, milestones → **Thiên + Khoa**.
- **Unity:** Xác nhận `SetSkillPanelOpen(0.25×)` còn trong code; sửa **music sync** — `CombatMusicController.SetPlaybackSpeedMultiplier` + `BeatTimelineUIView` đồng bộ pitch nhạc khi panel mở.

**Decisions**
- Art/SFX Phase 1 tạm **Khoa** maintain (pipeline scripts trong repo).

**Refs:** `BeatTimelineUIView.cs`, `CombatMusicController.cs`, `CombatController.cs`

---

## 2026-06-23 — Audit repo + logging sync (collab Thiên / GitHub)

**Focus:** production

**Owner:** Khoa (scribe) · team review

**Done**
- **Scan repo GitHub:** `docs/`, `assets/characters/` (Ren/Charlotte/Coda LOCK + motion specs), `scripts/` (~40 pipeline Python), `docs/diagrams/` (story flow, combat layout, run map), `.github/` PR template.
- **Chuẩn hóa logging:** tạo [`LOGGING.md`](LOGGING.md) — Linear (task) · Notion (nhật ký) · Google Doc (story **Thiên**) · GitHub `PROJECT_LOG` (mirror quyết định) · Unity local (code).
- **Cập nhật [`PROJECT_STATUS.md`](PROJECT_STATUS.md)** — bảng Phase 1, art pipeline, combat MVP, blocker first push GitHub.
- **Xác nhận stack team:** [`TEAM.md`](TEAM.md), [`TEAM_MESSENGER_BROADCAST.md`](TEAM_MESSENGER_BROADCAST.md), Linear FAC-13…19, Notion + Doc links [`LINKS.md`](LINKS.md).

**Decisions**
- Bỏ canonical log tại `F:\Factured Chorus` (path không còn); **GitHub repo = mirror log chính** cho docs/art scripts.
- **Thiên** ghi story trên Google Doc; Khoa sync tóm tắt `STORY_SUMMARY.md` khi canon đổi (PR nhỏ).
- Unity combat vẫn local; mọi session code → bullet + entry `PROJECT_LOG` + Linear issue.

**Blockers**
- Repo **chưa commit / chưa remote GitHub** — cần first push + invite **Thiên**.
- `LINKS.md` GitHub URL trống cho tới khi push.

**Next**
- Push `fractured-chorus` lên GitHub; Thiên clone + bookmark Doc/Linear/Notion.
- Thiên: tiếp P1-1 story tab; Khoa: art pipeline + Unity playtest.

**Refs:** [`LOGGING.md`](LOGGING.md), [`PROJECT_STATUS.md`](PROJECT_STATUS.md)

---

## 2026-06-23 — Combat: telegraph phase, resolve scan, quái 20/150 HP

**Focus:** code (Unity local)

**Owner:** Khoa

**Done**
- Timeline đỏ; resolve khi **scan qua beat** (Tank nhận dmg đúng lúc ô đỏ).
- **AV:** Base AV = priority; Phase AV = budget party.
- Guard / priority / target cột Tank → giữa → Mage.
- **Mỗi beat đầu phase** (0, 15, 25…): random N ô đỏ (N = quái sống) trong 10/15 ô phase.
- Quái **20 dmg**, **150 HP**; **Victory** khi 2 quái về 0 → dừng timeline.

**Refs:** `CombatSession.cs`, `SimpleEnemyAI.cs`, `TimelineConstants.cs`

---

## 2026-06-22 — Combat UI polish, skill panel bugfix, formation

**Focus:** code

**Done**
- Timeline **0.25×** khi skill panel mở; carousel slot động; auto 105 beat; phase AV header.
- Fix skill panel nhấp nháy (nested Canvas); Tank/Mage swap; enemy snap ô lưới.

**Next**
- Playtest; import art; drag skill → beat.

---

## 2026-06-21 — Combat UI: skill panel + timeline carousel

**Focus:** code

**Done**
- Skill panel toggle; `Beat_0` template + carousel; slide mỗi beat scan.

**Refs:** `BeatTimelineUIView.cs`, `SkillPanelUIView.cs`

---

## 2026-06-21 — Combat prototype Unity (vertical slice 2)

**Focus:** code

**Done**
- Timeline 105 beat / 10 phase; AV 4 skill; hierarchy Editor; Input System; grid R→L.

**Refs:** [`setup/UNITY_WORKFLOW.md`](setup/UNITY_WORKFLOW.md)

---

## 2026-06-21 — Combat prototype Unity (vertical slice 1)

**Focus:** code

**Done**
- Unity `F:\Unity_Project\Fractured Chorus` — Dual Grid, `CombatSession`, bootstrap, demo encounter.

**Decisions**
- Code canonical Unity; repo GitHub giữ docs/log/art meta.

---

## 2026-06-03 — Phân công Phase 1

**Focus:** production

**Done**
- **Thiên:** story Google Doc · **Kiên + Tính:** art/SFX · **Khoa:** story flow, Notion, work log.

**Refs:** [`TEAM.md`](TEAM.md), Linear FAC-13…19

---

## 2026-06-02 — Bootstrap hệ thống làm việc

**Focus:** production

**Done**
- Stack Notion · Linear · Google Doc · draw.io · GitHub repo template.

**Refs:** [`WORKFLOW.md`](WORKFLOW.md)

---

<!-- Template — xem LOGGING.md

## YYYY-MM-DD — [tiêu đề]

**Focus:** … | **Owner:** …
**Done** / **Decisions** / **Blockers** / **Next** / **Refs**

-->

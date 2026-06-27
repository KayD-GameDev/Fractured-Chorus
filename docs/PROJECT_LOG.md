# Project log — Fractured Chorus

**Canonical (text):** file này trên **GitHub** `fractured-chorus/docs/PROJECT_LOG.md`  
**Notion (team log):** https://app.notion.com/p/37441bb3f2a281768901eb58a16bc252  
**Snapshot:** [`PROJECT_STATUS.md`](PROJECT_STATUS.md) · **Quy trình ghi log:** [`LOGGING.md`](LOGGING.md)  
**Unity code:** `F:\Unity_Project\Fractured Chorus` (log combat tóm tắt ở đây cho tới khi Unity vào Git)

Newest first.

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

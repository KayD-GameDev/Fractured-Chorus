# Fractured Chorus — Trạng thái dự án (snapshot)

**Cập nhật:** 2026-06-21  
**Unity build:** `F:\Unity_Project\Fractured Chorus` · Scene `CombatPrototype.unity`  
**Design / art:** `F:\Factured Chorus`  
**Docs mirror:** `C:\Users\admin\Projects\fractured-chorus`

---

## Tổng quan Phase 1

| Hạng mục | Owner (Phase 1) | Trạng thái | Ghi chú |
|----------|-----------------|------------|---------|
| Story bible (Google Doc) | Thiên | 🔲 Chưa sync log | `Fractured_Chorus_Story.docx` |
| Art characters / BG | Kiên + Tính | 🔲 Placeholder | Unity dùng sprite vuông màu |
| SFX phase 1 | Kiên + Tính | 🔲 Chưa | Folder `Assets/FracturedChorus/Audio/` có README |
| Story flow draw.io | Khoa | 🔲 Tham chiếu | `Fractured-Chorus-Story-Flow.drawio` |
| OOAD → Notion wiki | Khoa | 🔲 Docx xong | Chưa đẩy Notion |
| Notion work log | Khoa | 🔲 Template repo | `docs/setup/NOTION_WORK_LOG.md` |
| **Combat prototype (Unity)** | Khoa (code) | 🟡 Vertical slice | Planning + Execute scan (guard, enemy telegraph đỏ, priority Base AV) |

**Chú thích:** 🔲 Chưa / chưa ghi log · 🟡 Đang làm · ✅ Xong cho scope hiện tại

---

## Combat prototype — đã làm tới đâu

| Module | Trạng thái | Chi tiết |
|--------|------------|----------|
| **Dual Grid 3×3** | ✅ Core | Player max 5, enemy max 9; `GridCellMarker` viền ô; party cột 0 = phải (front) |
| **Unit stats & damage** | ✅ MVP | Ren/Tank/Mage/Grunt preset; `DamageCalculator` (beat timing, harmony); Guard giảm dmg |
| **AV model** | ✅ MVP | **Phase AV** (150/100) = budget số lần ra đòn party; **Base AV** = priority cùng beat (không trừ) |
| **Enemy telegraph UI** | ✅ MVP | Beat quái = **nền đỏ** trên timeline; trùng beat player vẫn hiện đỏ |
| **Guard resolve** | ✅ MVP | Scan qua beat có Guard → chặn dmg quái theo beat timing |
| **Action priority** | ✅ MVP | Cùng beat: player attack vs enemy — **Base AV thấp hơn trước** |
| **Enemy targeting** | ✅ MVP | Cột Tank → giữa (col 1) → cột Mage; Tank chết → bỏ cột Tank |
| **Beat timeline engine** | ✅ MVP | 105 beat / 10 phase; telegraph scan; `BeatTimingResolver` |
| **Timeline UI** | ✅ MVP | Carousel slot động theo viewport; chỉ `Beat_0` template trong Hierarchy; scan bar đỏ; **không** nút EXECUTE — auto-play 105 beat rồi confirm |
| **Skill assign flow** | 🟡 MVP | Click unit → panel; chọn skill → arm; gán khi scan bar tới beat (nếu đủ AV); panel đóng khi assign OK |
| **Skill panel UI** | ✅ MVP | Sát unit (~1.5px); toggle click lại; Skill 1 / Skill 2 / Guard; timeline **0.25×** khi panel mở |
| **Formation / layout** | ✅ Scene-first | Row/Column + Transform trong Hierarchy = Play; bootstrap không ép formation demo; hex ô giữ màu/tắt như scene |
| **Input** | ✅ | Input System + `IPointerClickHandler` trên unit |
| **Bootstrap / Editor** | ✅ Scene-first | `respectSceneAuthoring` — Play không snap/rebuild visual; menu Editor để rebuild |
| **Execute phase / AI** | 🟡 MVP | `SimpleEnemyAI` telegraph; resolve scan + guard + priority; cần playtest/VFX |
| **Drag skill → beat** | 🔲 | Chưa — assign theo scan beat |
| **Telegraph portrait / posture** | 🔲 | Chưa portrait riêng quái; có màu đỏ + label |
| **Morale / Interrupt / Cover** | 🔲 Stub | GDD deferred |
| **Run map / roguelite run** | 🔲 | Folder placeholder |
| **Art / audio thật** | 🔲 | Chưa import từ `F:\Factured Chorus\art assets\` |

---

## Bug đã xử lý (2026-06-22)

| Vấn đề | Cách xử lý |
|--------|------------|
| Hai bảng skill nhấp nháy | Gỡ nested `Canvas` trên `SkillPanelUI`; panel ẩn lúc bootstrap |
| Cảnh báo `skillPanelOpenSpeedMultiplier` không dùng | `SetSkillPanelOpen()` + Inspector `0.25` |
| AV label chỉ hiện "AV" | Widen label + overflow visible |
| Quái đứng lệch ô | `AlignUnitViewToGridCell()` đọc `GridCellMarker` |
| Play ghi đè scene (Ren nhảy hàng, hex đỏ bật lại) | Bootstrap đọc Row/Column scene; `PrepareForPlay` không reset hex visual |

---

## Logic dmg quái (2026-06-23)

| Bước | Hành vi |
|------|---------|
| Planning | `SimpleEnemyAI` gán telegraph đỏ trên timeline |
| Scan + Guard | Beat có Guard party → giảm/block dmg quái (`BeatTimingResolver`) |
| Scan không Guard | Quái dmg thẳng target |
| Cùng beat | So **Base AV** — thấp hơn resolve trước (Ren 75 trước Grunt 100) |
| Target | `CombatTargetPicker`: cột Tank → col 1 → cột Mage |

---

## Việc tiếp theo (ưu tiên)

1. Playtest full loop Planning → Execute → Upkeep; log bug combat resolve.
2. Drag / drop skill lên beat cụ thể (UX mock UI).
3. Telegraph + portrait trên beat slot; posture bar.
4. Import sprite approved; thay placeholder `UnitView`.
5. Rebuild scene cũ nếu còn 105 `Beat_X` hoặc nested Canvas — menu Editor rồi Save.

---

## Đường dẫn nhanh

| Nội dung | Path |
|----------|------|
| Work log (canonical) | `F:\Factured Chorus\PROJECT_LOG.md` |
| Work log (GitHub mirror) | `docs/PROJECT_LOG.md` |
| Unity workflow | `docs/setup/UNITY_WORKFLOW.md` |
| Scene setup | `Assets/FracturedChorus/Scenes/SCENE_SETUP.md` |
| GDD | `F:\OOAD&UML\Final Asm\Fractured-Chorus-Gameplay-Core.docx` |

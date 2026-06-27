# Fractured Chorus — Trạng thái dự án (snapshot)

**Cập nhật:** 2026-06-27  
**Unity build:** `F:\Unity_Project\Fractured Chorus` · Scene `CombatPrototype.unity`  
**Design / art / log (local):** `F:\Unity_Project\Fractured Chorus\Assets\Factured Chorus Arts & Docs\`  
**Design draw.io / briefs:** `F:\Factured Chorus`  
**Docs mirror (GitHub):** `C:\Users\admin\Projects\fractured-chorus`

---

## Tổng quan Phase 1

| Hạng mục | Owner (Phase 1) | Trạng thái | Ghi chú |
|----------|-----------------|------------|---------|
| Story bible (Google Doc) | Thiên | 🔲 Chưa sync log | `Fractured_Chorus_Story.docx` |
| Art characters / BG | Kiên + Tính | 🟡 Một phần | Sprite Ren/Tank/Mage + icon hệ UI |
| SFX phase 1 | Kiên + Tính | 🔲 Chưa | Folder `Assets/FracturedChorus/Audio/` có README |
| Story flow draw.io | Khoa | 🔲 Tham chiếu | `Fractured-Chorus-Story-Flow.drawio` |
| OOAD → Notion wiki | Khoa | 🔲 Docx xong | Chưa đẩy Notion |
| Notion work log | Khoa | 🔲 Template repo | `docs/setup/NOTION_WORK_LOG.md` |
| **Combat prototype (Unity)** | Khoa (code) | 🟡 Vertical slice | Planning + Execute scan; formation drag-drop; party bar sync |

**Chú thích:** 🔲 Chưa / chưa ghi log · 🟡 Đang làm · ✅ Xong cho scope hiện tại

---

## Combat prototype — đã làm tới đâu

| Module | Trạng thái | Chi tiết |
|--------|------------|----------|
| **Dual Grid 3×3** | ✅ Core | Player max 5, enemy max 9; honeycomb `HexBoardLayout`; drag-drop + swap đồng đội |
| **Unit stats & damage** | ✅ MVP | Ren/Tank/Mage/Grunt preset; `DamageCalculator`; Guard giảm dmg |
| **AV model** | ✅ MVP | Phase AV (150/100); Base AV = priority cùng beat |
| **Enemy telegraph UI** | ✅ MVP | Beat quái = nền đỏ; trùng beat player vẫn hiện đỏ |
| **Guard resolve** | ✅ MVP | Scan qua beat có Guard → chặn dmg quái theo beat timing |
| **Action priority** | ✅ MVP | Cùng beat: Base AV thấp hơn trước |
| **Enemy targeting** | ✅ MVP | Cột C1 (front) → C2 → C3; Tank chết → bỏ cột Tank |
| **Beat timeline engine** | ✅ MVP | 105 beat / 10 phase; telegraph scan |
| **Timeline UI** | ✅ MVP | Carousel slot động; `Beat_0` template; auto-play 105 beat |
| **Skill assign flow** | 🟡 MVP | Click unit → panel; gán khi scan bar tới beat |
| **Skill panel UI** | ✅ MVP | Sát unit; toggle; timeline 0.25× khi panel mở |
| **Party status bar** | ✅ MVP | Clone `CardTemplate` lúc Play; HP + badge hệ; **formation sync** |
| **Party card order** | ✅ MVP | C1→C2→C3 logic; cùng cột H2→H1→H3; thẻ 1 neo **phải**; spacing 100px |
| **Formation / layout** | ✅ Scene-first | Row/Column Hierarchy = Play; bootstrap `respectSceneAuthoring` |
| **Input** | ✅ | Input System; drag unit + click skill panel |
| **Bootstrap / Editor** | ✅ | Menu Apply All / Setup Party Cards; `CombatUiHierarchy` |
| **Execute phase / AI** | 🟡 MVP | Resolve scan + guard; cần playtest/VFX |
| **Drag skill → beat** | 🔲 | Chưa — assign theo scan beat |
| **Telegraph portrait** | 🔲 | Chưa portrait riêng quái |
| **Morale / Interrupt / Cover** | 🔲 Stub | GDD deferred |
| **Run map / roguelite run** | 🔲 | Folder placeholder |

---

## Party bar — quy tắc hiện tại (2026-06-27)

| Khía cạnh | Quy tắc |
|-----------|---------|
| **Số thẻ (1→N)** | C1 (Tank/front) = **thẻ 1** → C2 (Ren) = thẻ 2 → C3 (Mage) = thẻ 3 |
| **Vị trí UI** | Đếm **phải → trái**: thẻ 1 ngoài cùng phải, +100px sang trái mỗi thẻ |
| **Cùng cột** | Hàng đỏ **2** (H2) trước → hàng **1** (trên) → hàng **3** (dưới) |
| **Refresh** | Sau kéo ô: `CombatPrototypeBootstrap.RefreshPartyStatusBar()` ← `DualGrid` |
| **Hierarchy** | `CardsRow` + `CardTemplate` (inactive); không giữ `Card_Tank/Ren/Mage` cố định lúc Play |

**Ví dụ:** cả 3 ở cột 1 — Tank H2, Ren H1, Mage H3 → đọc phải→trái: **Tank · Ren · Mage**.

---

## Bug đã xử lý (2026-06-22 → 2026-06-27)

| Vấn đề | Cách xử lý |
|--------|------------|
| Hai bảng skill nhấp nháy | Gỡ nested `Canvas` trên `SkillPanelUI` |
| Play ghi đè scene (Ren nhảy hàng, hex đỏ bật lại) | Bootstrap đọc Row/Column scene; `PrepareForPlay` giữ hex visual |
| Thẻ party không đổi khi kéo formation | `BindFromSession` mỗi lần formation change; sort từ `DualGrid` |
| Thứ tự cùng cột sai (Mage trước Ren) | `GetWithinColumnRowRank`: H2 → hàng 1 → hàng 3 (số đỏ board) |
| Thẻ 1 không nằm bên phải | `PartyCardLayout` mirror X theo `totalCards` |
| `UnitView.UnitId` compile error | `ResolveUnitId()` qua `CombatUnit` / preset |

---

## Logic dmg quái (2026-06-23)

| Bước | Hành vi |
|------|---------|
| Planning | `SimpleEnemyAI` gán telegraph đỏ trên timeline |
| Scan + Guard | Beat có Guard party → giảm/block dmg quái |
| Scan không Guard | Quái dmg thẳng target |
| Cùng beat | So Base AV — thấp hơn resolve trước |
| Target | `CombatTargetPicker`: cột C1 → C2 → C3 |

---

## Việc tiếp theo (ưu tiên)

1. Playtest party bar: 4–5 unit, nhiều người cùng cột/hàng; unit chết trên lưới.
2. Playtest full loop Planning → Execute → Upkeep.
3. Drag skill lên beat cụ thể.
4. Import sprite approved còn thiếu; thay placeholder enemy.
5. Rebuild scene cũ nếu còn 105 `Beat_X` — menu Editor rồi Save.

---

## Đường dẫn nhanh

| Nội dung | Path |
|----------|------|
| Work log (Arts & Docs) | `Assets/Factured Chorus Arts & Docs/PROJECT_LOG.md` |
| Trạng thái (file này) | `Assets/Factured Chorus Arts & Docs/PROJECT_STATUS.md` |
| Work log (Unity `docs/`) | `docs/PROJECT_LOG.md` |
| Work log (GitHub mirror) | `C:\Users\admin\Projects\fractured-chorus\docs\PROJECT_LOG.md` |
| Unity workflow | `docs/setup/UNITY_WORKFLOW.md` |
| Scene setup | `Assets/FracturedChorus/Scenes/SCENE_SETUP.md` |
| Board margin | `Assets/Factured Chorus Arts & Docs/Board margin.drawio` |
| GDD | `F:\OOAD&UML\Final Asm\Fractured-Chorus-Gameplay-Core.docx` |

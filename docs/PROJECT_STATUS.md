# Fractured Chorus — Trạng thái dự án

**Cập nhật:** 2026-06-28 (Run Map layout + procedural + docs sync)  
**Repo:** `C:\Users\admin\Projects\fractured-chorus` (docs mirror)  
**Unity:** `F:\Unity_Project\Fractured Chorus` · scenes `CombatPrototype.unity` · `RunMapPrototype.unity` · GitHub remote **Fractured-Chorus1**  
**Log:** [`PROJECT_LOG.md`](PROJECT_LOG.md) · [`LOGGING.md`](LOGGING.md)

---

## Tổng quan Phase 1 (team 2 dev)

| ID | Hạng mục | Owner | Trạng thái | Ghi chú |
|----|----------|-------|------------|---------|
| P1-1 | Story / tuyến truyện (Google Doc) | **Thiên** | 🟡 Đang làm | Canon trên Doc; tóm tắt [`STORY_SUMMARY.md`](design/STORY_SUMMARY.md) |
| P1-2 | Art characters / BG | **Khoa** (tạm) | 🟡 | Repo LOCK/brief Ren/Charlotte/Coda; Kiên/Tính đã rời team |
| P1-3 | SFX phase 1 | **Khoa** (tạm) | 🔲 | `ASSET_INVENTORY.md` A-AU-* placeholder |
| P1-4 | Story flow draw.io | Khoa | 🟡 | [`diagrams/Fractured-Chorus-Story-Flow.drawio`](diagrams/Fractured-Chorus-Story-Flow.drawio) |
| P1-5 | OOAD → Notion wiki | Khoa | 🟡 | Doc + Drive links trong [`FINAL_DELIVERABLES_LINKS.md`](design/FINAL_DELIVERABLES_LINKS.md) |
| P1-6 | Notion work log | Khoa | ✅ Setup | [Notion Work Log](https://app.notion.com/p/37441bb3f2a281768901eb58a16bc252) |
| — | **Combat prototype (Unity)** | Khoa | 🟡 MVP | Remote GitHub; PR #4–#5 merged |
| — | **GitHub repo + PR workflow** | Team | 🟡 | Unity trên GitHub; docs mirror `fractured-chorus` chưa push |

🔲 Chưa · 🟡 Đang · ✅ Xong cho scope hiện tại

---

## GitHub repo — có gì (scan 2026-06-23)

| Thư mục | Nội dung |
|---------|----------|
| `docs/` | GDD/OOAD index, TEAM, MILESTONES, WORKFLOW, **PROJECT_LOG**, **PROJECT_STATUS**, **LOGGING**, diagrams |
| `assets/characters/` | Ren / Charlotte / Coda — `*_LOCK.txt`, concept brief, animation motion specs (JSON) |
| `scripts/` | ~40 pipeline Python (Ren/Charlotte chibi, palette, GIF motion QA, Linear/Notion helpers) |
| `.github/` | PR template, issue template Phase 1 |
| `.cursor/skills/` | `fractured-chorus-ai` project skill |

**Chưa có trong repo:** Unity `.cs`, binary PNG art (PNG ở `Downloads\art` + drive F:).

---

## Art & animation (repo metadata)

| Nhân vật | Lock | Pipeline scripts (ví dụ) |
|----------|------|---------------------------|
| **Ren** | `assets/characters/ren/Ren_LOCK.txt` v4 | `ren_v4_logo_gen_finalize.py`, chibi rifle idle, skill1 fell bullet |
| **Charlotte** | `Charlott_LOCK.txt` v13 | `charlotte_v8_finalize.py`, crest sync từ chibi |
| **Coda** | `Coda_LOCK.txt`, cadence brief | brief + cadence lock |

Inventory tổng: [`ASSET_INVENTORY.md`](ASSET_INVENTORY.md).

**Unity import layout** (`Assets/FracturedChorus/Art/Characters/`):

| Role | Tên canon | Folder animation |
|------|-----------|------------------|
| Mage | **Coda** | `Coda/Animation/Idle|Move|Attack|Hit|Death/` |
| DPS | **Ren** | `Ren/Animation/…` |
| Tank | **Charlotte** | `Charlotte/Animation/…` |

Preset SO vẫn `UnitPreset_Mage|Ren|Tank` — xem `Art/Characters/README.md`.

---

## Unity combat prototype (local)

| Module | Trạng thái |
|--------|------------|
| Dual Grid 3×3 honeycomb, margin `Board-margin.drawio` | ✅ MVP |
| **EXECUTE** overlay — khóa UI/timeline trước khi bắt đầu round | ✅ |
| Pre-EXECUTE **drag formation** · post-EXECUTE **click → skill panel** | ✅ |
| `UnitStatBlockSO` + Resources presets (Ren/Tank/Mage/Grunt) | ✅ |
| `DamageCalculator` Physical/Magical + Harmony + Base Luck crit | ✅ |
| Target cột **C1→C2→C3**, trong cột Tank→DPS→Mage | ✅ |
| Timeline **105 beat / 10 phase**, carousel + scan bar | ✅ MVP |
| Phase AV budget (150/100) · Base AV = priority only | ✅ |
| Skill panel + dismiss backdrop, slow **0.25×** khi panel mở | ✅ |
| Enemy telegraph **đỏ** · random mỗi **beat đầu phase** | ✅ |
| Scene-first authoring (`respectSceneAuthoring`, collider tay) | ✅ |
| **Party status bar** — thẻ Hierarchy-first, spacing 1.25px, icon hệ art, Tank ngoài cùng phải | ✅ MVP |
| Pre-EXECUTE **swap formation** (kéo lên ô ally) + refresh thẻ party | ✅ |
| Battle sprite Ren/Tank/Mage (`UnitPresetSO.battleSprite`) | 🟡 3/3 demo |
| Audio beat map (`MusicBeatMapSO`, `CombatMusicController`) | 🟡 Stub/editor |
| Run map / Morale / drag skill → beat slot | 🔲 |
| **Run map prototype** (`RunMapPrototype.unity`) | 🟡 MVP | Procedural seed, F1 đáy scroll, boss F16, path click + edge highlight |

### Run map prototype (Unity — 2026-06-28)

| Thành phần | Trạng thái |
|------------|------------|
| Scene `RunMapPrototype.unity` | ✅ |
| Procedural `MapGenerator` (6 path unique, boss hội tụ) | ✅ |
| Random seed mỗi Play (`MapTemplate_Default`) | ✅ |
| Scroll F1 đáy → F16; `fitToViewport` | ✅ |
| Edge + node cùng layer, bottom anchor | ✅ |
| Path visited / preview highlight | ✅ |
| Demo reference map (flag Inspector) | ✅ debug only |
| Nối node → Combat scene | 🔲 |

Doc: [`RUNMAP_SCENE_SETUP.md`](setup/RUNMAP_SCENE_SETUP.md) · Unity `Assets/FracturedChorus/Scenes/RUNMAP_SCENE_SETUP.md`

Chi tiết combat: [`setup/UNITY_WORKFLOW.md`](setup/UNITY_WORKFLOW.md) · log: [`PROJECT_LOG.md`](PROJECT_LOG.md).

---

## Party status bar (Unity — 2026-06-27)

| Thành phần | Mô tả |
|------------|--------|
| Hierarchy | `CardsRow/Card_Mage`, `Card_Ren`, `Card_Tank` + `CardTemplate` (inactive) — **phải hiện trong Hierarchy** |
| Runtime | `preserveSceneLayout` — bind HP/avatar; **không dịch thẻ** khi di chuyển unit; spacing **1.25px** |
| Thứ tự | Trái → phải: Mage · Ren · **Tank** (Tank ngoài cùng phải); cố định theo role, không theo cột lưới |
| Icon hệ | `Art/UI/icon_he_nhip` (Nhịp/Tank), `icon_he_giai_dieu` (Giai điệu/Ren), `icon_he_hoa_am` (Hòa âm/Mage) |
| Editor | **Setup Party Cards in Hierarchy** · **Apply Element Badge Icons** · **Apply All Play-Ready Updates** |

Rule: **không code trên scene** — logic chỉ `.cs`. Chi tiết: Unity `SCENE_SETUP.md`, `docs/setup/UNITY_WORKFLOW.md`.

---

## Công cụ team (đã setup)

| Tool | URL / path |
|------|------------|
| Linear FAC | https://linear.app/factured-chorus-taskboard/team/FAC |
| Notion Work Log | https://app.notion.com/p/37441bb3f2a281768901eb58a16bc252 |
| Notion wiki GDD/OOAD | https://app.notion.com/p/37441bb3f2a281a5844fddf89e5e8e9c |
| Google Doc kit | [`LINKS.md`](LINKS.md) |
| GitHub | _(chưa gắn remote — điền sau first push)_ |

Broadcast nhóm: [`TEAM_MESSENGER_BROADCAST.md`](TEAM_MESSENGER_BROADCAST.md).

---

## Việc tiếp theo (ưu tiên)

1. **Playtest** party bar + swap formation: EXECUTE → kéo đổi chỗ ally → thẻ refresh đúng thứ tự.
2. Push docs mirror `fractured-chorus` lên GitHub; cập nhật `LINKS.md`.
3. **Thiên:** tiếp tab Story → Done Linear P1-1.
4. Import sprite canon đầy đủ (Grunt, UI polish) khi art approved.
5. **Run map:** playtest procedural seed; nối node Battle → `CombatPrototype` (Linear issue).

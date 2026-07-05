# Fractured Chorus — Trạng thái dự án

**Cập nhật:** 2026-07-05 (Combat: skill UI scene-first, 2-phase round, footprint overlap)  
**Repo mirror:** `C:\Users\admin\Projects\fractured-chorus` (docs + scripts)  
**Unity:** `F:\Unity_Project\Fractured Chorus` · scenes `CombatPrototype.unity` · `RunMapPrototype.unity`  
**Log:** [`PROJECT_LOG.md`](PROJECT_LOG.md) · [`LOGGING.md`](LOGGING.md)

---

## Tổng quan Phase 1 (team 2 dev)

| ID | Hạng mục | Owner | Trạng thái | Ghi chú |
|----|----------|-------|------------|---------|
| P1-1 | Story / tuyến truyện (Google Doc) | **Thiên** | 🟡 Đang làm | Canon trên Doc; tóm tắt [`STORY_SUMMARY.md`](design/STORY_SUMMARY.md) |
| P1-2 | Art characters / BG | **Khoa** (tạm) | 🟡 | Repo LOCK/brief Ren/Charlotte/Coda |
| P1-3 | SFX phase 1 | **Khoa** (tạm) | 🔲 | `ASSET_INVENTORY.md` A-AU-* placeholder |
| P1-4 | Story flow draw.io | Khoa | 🟡 | [`diagrams/Fractured-Chorus-Story-Flow.drawio`](diagrams/Fractured-Chorus-Story-Flow.drawio) |
| P1-5 | OOAD → Notion wiki | Khoa | 🟡 | [`FINAL_DELIVERABLES_LINKS.md`](design/FINAL_DELIVERABLES_LINKS.md) |
| P1-6 | Notion work log | Khoa | ✅ Setup | [Notion Work Log](https://app.notion.com/p/37441bb3f2a281768901eb58a16bc252) |
| — | **Combat prototype (Unity)** | Khoa | 🟡 MVP | 2-phase round + footprint overlap (2026-07-05) |
| — | **GitHub repo + PR workflow** | Team | 🟡 | Unity trên GitHub; docs mirror `fractured-chorus` |

🔲 Chưa · 🟡 Đang · ✅ Xong cho scope hiện tại

---

## Unity combat prototype (local — 2026-07-05)

| Module | Trạng thái |
|--------|------------|
| Dual Grid **2×3** honeycomb, party max **4**, enemy max **6** | ✅ |
| **Deploy** → **Execute** (intro-pause lần đầu) | ✅ |
| Intro-pause @ beat 0.5 (chỉ Deploy đầu) | ✅ |
| Pre-Deploy **drag formation** + swap ally | ✅ |
| Skill assign: **kéo → timeline**; W/A/D highlight | ✅ |
| **Footprint overlap** + drag preview | ✅ |
| **Round 2 timeline phase** → Execute lại | ✅ |
| Skill panel **Hierarchy-first** (Radial + 3 slots) | ✅ |
| Quái **2 pha** S1 + S telegraph | ✅ |
| Bỏ slow-mo · center token | ✅ |
| Timeline **619 beat** sync nhạc (`MusicBeatMapSO` + CSV wired scene) | ✅ |
| Character **lanes** + `TimelineLaneMarkerView` | ✅ |
| Skill **footprint** S1/S/S2 (xám · màu · xám) trên lane | ✅ |
| Enemy telegraph từ **beat 3** (`EnemyFirstAttackBeat = 2`) | ✅ |
| **Scene-first UI sizing** (`RectSizeUtil`, card/badge/panel) | ✅ |
| Party + enemy status bar (Hierarchy-first) | ✅ |
| Guard = giữ **Spacebar** trọn beat đỏ | ✅ |
| Phase AV budget (150/100) — **design bỏ**, code còn stub | 🟡 legacy |
| Boss 3-target / note degrade / Cycle Shift / mini pressure | 🔲 P0 design |
| Run map → Combat (boss node) | 🟡 MVP |

**Combat flow hiện tại:** xem [`combat/COMBAT_MECHANICS.md`](combat/COMBAT_MECHANICS.md) §1.

**Play-ready:** **Apply All Play-Ready Updates** + **Setup Skill Panel in Hierarchy** · verify: `python scripts/verify_combat_scene_sync.py`.

Chi tiết kỹ thuật: [`setup/UNITY_WORKFLOW.md`](setup/UNITY_WORKFLOW.md) · log: [`PROJECT_LOG.md`](PROJECT_LOG.md).

---

## Run map prototype (Unity)

| Thành phần | Trạng thái |
|------------|------------|
| Scene `RunMapPrototype.unity` | ✅ |
| Procedural seed, F1→F16 scroll, boss F16 | ✅ |
| Boss node → `CombatPrototype` (`RunMapSceneLoader`) | ✅ |
| Battle / Elite → combat | 🔲 |

Doc: [`setup/RUNMAP_SCENE_SETUP.md`](setup/RUNMAP_SCENE_SETUP.md)

---

## Việc tiếp theo (ưu tiên)

1. **Playtest** intro-pause: Deploy → nhạc → pause @ beat 0.5 → đặt skill → Continue / auto-resume.
2. Enforce footprint overlap + boss note degrade (P0 design → code).
3. Boss 3-target telegraph (CORE / MICRO / EYE).
4. Push docs mirror `fractured-chorus` lên GitHub; sync `LINKS.md`.
5. **Thiên:** tiếp tab Story → Done Linear P1-1.

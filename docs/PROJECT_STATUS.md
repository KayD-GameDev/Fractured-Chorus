# Fractured Chorus — Trạng thái dự án

**Cập nhật:** 2026-07-17 (Cover Phase 4 · bỏ Phase AV budget gate)  
**Unity scenes:** `CombatPrototype.unity` · `RunMapPrototype.unity` · hub/VN scenes  
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
| — | **Combat prototype (Unity)** | Khoa | 🟡 MVP+ | Prep Setup→Payoff + note visuals + counter feel (2026-07-16) |
| — | **GitHub repo + PR workflow** | Team | 🟡 | Unity trên GitHub |

🔲 Chưa · 🟡 Đang · ✅ Xong cho scope hiện tại

---

## Unity combat prototype (2026-07-16)

| Module | Trạng thái |
|--------|------------|
| Dual Grid **2×3** honeycomb, party max **4**, enemy max **6** | ✅ |
| **Deploy** → **Execute** (intro-pause lần đầu) | ✅ |
| Pre-Deploy **drag formation** + swap ally | ✅ |
| Skill assign: **kéo → timeline**; footprint S1/S/S2 | ✅ |
| **Prep Setup→Payoff** Phase 1–3 (channel / empower / Delay / ReduceS2) | ✅ |
| Prep pips trên party card (`PrepPipsView`) | ✅ |
| Shield absorb · Bulwark / Mend overheal | ✅ |
| Anchor Delay + Encore ReduceS2 **lúc Planning** + buff icon | ✅ |
| Timeline note sprites + drop ghost + cover (`TimelineNoteVisualCatalog`) | ✅ |
| Counter presentation (`CounterPresentationDriver` / Perfect chip / MULTI) | ✅ |
| Runtime UI load path `Resources/UI/**` (không đặt dưới Prefabs) | ✅ restored |
| Guard = **Spacebar** barrier (không skill Guard trên kit) | ✅ |
| BaseAv = speed/order + dmg target (không gate số skill) | ✅ |
| Phase AV budget 150/100 | ❌ removed 2026-07-17 |
| Boss 3-target / note degrade / Cycle Shift / mini pressure | 🔲 P0 design |
| **Cover gauge** (empty S → party gauge · Planning COVER · 12 beat ×1.25) | ✅ |
| Empty-beat catalog (**#4**) | ✅ Cover gauge (không dedicated buff skills) |
| Bulwark GuardCharge + positional front/back · CycleShift pretend cut | ✅ Task 6 |
| Ally pick Mend / Encore · Cycle Shift redesign | 🔲 backlog |
| Run map → Combat (boss node) | 🟡 MVP |

**Combat flow:** [`combat/COMBAT_MECHANICS.md`](combat/COMBAT_MECHANICS.md) §1 · **Kit:** [`combat/SKILL_KIT.md`](combat/SKILL_KIT.md)

**Plans (superpowers):**
- [`2026-07-14-skill-kit-setup-payoff-implementation.md`](superpowers/plans/2026-07-14-skill-kit-setup-payoff-implementation.md) — Phase 1–3 **done**
- [`2026-07-14-timeline-note-readability-implementation.md`](superpowers/plans/2026-07-14-timeline-note-readability-implementation.md) — runtime **done** (playtest residual OK)
- [`2026-07-14-counter-presentation-feel-implementation.md`](superpowers/plans/2026-07-14-counter-presentation-feel-implementation.md) — runtime **done** (playtest residual OK)
- [`2026-07-16-cover-gauge-empty-beat-implementation.md`](superpowers/plans/2026-07-16-cover-gauge-empty-beat-implementation.md) — Phase 4 runtime **landed** (Play Mode verify)

---

## Run map prototype (Unity)

| Thành phần | Trạng thái |
|------------|------------|
| Scene `RunMapPrototype.unity` | ✅ |
| Procedural seed, F1→F16 scroll, boss F16 | ✅ |
| Boss node → `CombatPrototype` (`RunMapSceneLoader`) | ✅ |
| Battle / Elite → combat | 🔲 |

---

## Việc tiếp theo (ưu tiên cho main)

1. **Playtest Cover:** farm 8 empty S → COVER → 12 beat ×1.25; Prep/intro-pause không regress.
2. P0 boss: CORE / MICRO / EYE tags · note degrade.
3. Ally pick Mend/Encore · Ren Cycle Shift redesign (sau).
4. Task 7 prefabs Unit + PartyCard (P2).
5. **Thiên:** Story P1-1.

# Docs Runtime SoT Sync — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Align combat SoT docs and 2026-07-14 design specs with current Unity runtime (code = SoT); no C# changes.

**Architecture:** Hybrid sync — edit SoT files fully for HIGH/MED gameplay claims; add Implemented banners on specs and patch only superseded law lines. Verify with `rg` (ripgrep) / Select-String that stale phrases are gone.

**Tech Stack:** Markdown docs under `docs/` · constants mirrored from `TimelineConstants`, `BlockTiming`, `PhaseAvTracker`

**Spec:** [`../specs/2026-07-16-docs-runtime-sot-sync-design.md`](../specs/2026-07-16-docs-runtime-sot-sync-design.md)

## Global Constraints

- SoT = **code runtime** — do not invent numbers; copy from constants below
- **Docs-only** — no C# / asset / scene edits
- Intro-pause: `IntroPlanningPauseAfterBeatIndex = 6`, Execute from `7`
- Enemy segment-0 min impact: `IntroEnemySpawnZoneStartBeat = 10`
- Block reduce: OnBeat **68%** · Early **25%** · Late **10%** · OffBeat **0%**
- Phase AV: **still gates** (150/100) — document as *legacy retained*; AvLabel may be hidden
- Anchor Delay: notes **after** S only
- Encore UI: **icon** buff, not text `S2−1`
- `#4` empty-beat remains backlog
- Do **not** rewrite historical `PROJECT_LOG` entries (add one new entry only)

### File map

| File | Role |
|------|------|
| `docs/combat/SKILL_KIT.md` | Delay kind row + consistency |
| `docs/combat/COMBAT_MECHANICS.md` | Intro, enemy, Guard §9, Phase AV wording |
| `docs/combat/BOSS_ENCOUNTER_DESIGN.md` | Guard table · kit map |
| `docs/setup/UNITY_WORKFLOW.md` | Planning flow row |
| `docs/PROJECT_STATUS.md` | Phase AV legacy note |
| `docs/superpowers/specs/2026-07-14-skill-kit-setup-payoff-design.md` | Banner + D1 / chip / doc-only |
| `docs/superpowers/specs/2026-07-14-timeline-note-readability-design.md` | Banner + acceptance note |
| `docs/superpowers/specs/2026-07-14-counter-presentation-feel-design.md` | Banner + status |
| `docs/PROJECT_LOG.md` | One sync entry |

---

### Task 1: SKILL_KIT Delay kind row

**Files:**
- Modify: `docs/combat/SKILL_KIT.md`

**Interfaces:**
- Consumes: Charlotte table already says “sau S”
- Produces: `DelayBossNote` kind description aligned with after-S

- [ ] **Step 1: Confirm stale line**

Run (PowerShell):

```powershell
Select-String -Path "docs/combat/SKILL_KIT.md" -Pattern "trong cửa S"
```

Expected: at least the `DelayBossNote` kind row.

- [ ] **Step 2: Replace kind description**

Change:

```markdown
| `DelayBossNote` | Đẩy impact telegraph trong cửa S +N beat (D1) |
```

To:

```markdown
| `DelayBossNote` | Đẩy impact telegraph **sau cửa S** của skill +N beat (note trong S giữ nguyên) |
```

- [ ] **Step 3: Changelog line**

Under `## Changelog`, add:

```markdown
| 2026-07-16 | Sync DelayBossNote kind = after S (runtime SoT) |
```

- [ ] **Step 4: Verify**

```powershell
Select-String -Path "docs/combat/SKILL_KIT.md" -Pattern "trong cửa S"
Select-String -Path "docs/combat/SKILL_KIT.md" -Pattern "sau cửa S"
```

Expected: first command empty (or only historical if any); second finds kind row + Charlotte.

- [ ] **Step 5: Commit**

```bash
git add docs/combat/SKILL_KIT.md
git commit -m "Sync SKILL_KIT DelayBossNote with after-S runtime."
```

---

### Task 2: COMBAT_MECHANICS intro, enemy, Phase AV, Guard §9

**Files:**
- Modify: `docs/combat/COMBAT_MECHANICS.md`

**Interfaces:**
- Consumes: `TimelineConstants` beat 6/7/10; `BlockTiming` 68/25/10; `PhaseAvTracker` still gates
- Produces: SoT §1 / §9 / header / §10 “Bỏ Phase AV” wording consistent

- [ ] **Step 1: Update status banner**

Replace header line:

```markdown
> **Trạng thái:** Design lock (2026-06-30) · thay thế cơ chế Phase AV + cycle cũ  
```

With:

```markdown
> **Trạng thái:** Runtime SoT sync (2026-07-16) · kit Prep Setup→Payoff · Phase AV legacy retained in code  
> **Kit detail:** [SKILL_KIT.md](./SKILL_KIT.md)  
```

- [ ] **Step 2: Fix §1 flow + intro-pause**

In the opening flow diagram and tables, replace beat-0 intro wording so that after Deploy:

- Music/timeline runs until intro-pause after beat index **6**
- Then **Execute** (skill planning) — not Continue / auto-resume

Replace bullet:

```markdown
- **Bỏ:** Phase AV budget chung party · cycle cố định · skill Guard trên kit
```

With:

```markdown
- **Bỏ:** cycle cố định · skill Guard trên kit
- **Phase AV:** *legacy retained* — `PhaseAvTracker` vẫn gate budget **150 / 100** khi gán skill; UI `AvLabel` có thể ẩn
```

Replace entire `### Intro-pause` subsection body with:

```markdown
### Intro-pause (sau Deploy — gán skill)

- Vào scene: **Deploy** hiện ngay, player dàn trận, **không** phát nhạc.
- Bấm **Deploy** → `PlayBossMusic` + timeline sync từ beat 0.
- Pause sau beat index **`IntroPlanningPauseAfterBeatIndex = 6`** → planning horizon / Execute từ beat **7** (`IntroExecuteStartBeatIndex`).
- Nhãn nút: **Execute** (`CombatController` ép runtime). **Không** auto-resume khi đủ skill.
- Bấm **Execute** → `ResumePlayback` + scan tiếp.
```

Also fix the stage table rows that say “qua beat 0, dừng” for Deploy→intro-pause to mention pause @ beat **6**.

- [ ] **Step 3: Fix §1 enemy spawn**

Replace:

```markdown
- Quái **chỉ được đặt telegraph từ beat thứ 3** trở đi — `TimelineConstants.EnemyFirstAttackBeat = 2`.
```

With:

```markdown
- Segment 0 (intro): min impact ≥ **`IntroEnemySpawnZoneStartBeat = 10`** (`GetMinEnemyImpactBeat`). Các phase sau: phase start + buffer (`EnemySpawnBufferBeatsAfterHorizon`).
```

Keep other enemy bullets (per-phase count, plan once, etc.) unless they explicitly contradict.

- [ ] **Step 4: Fix §9 Guard table**

Replace the §9 timing table and keep Space / no skill Guard framing:

```markdown
## 9. Reactive Guard (Space)

**Không còn skill Guard.** Space đặt **barrier 1 beat** (`BlockBarrierTracker`).

| Timing vs impact `E` | Giảm dmg (`BlockTiming.GetDamageReduction`) |
|----------------------|---------------------------------------------|
| OnBeat (cùng ô) | **68%** |
| Early (`E−1`) | **25%** |
| Late (`E+1`) | **10%** |
| OffBeat | **0%** |

Chỉ giảm dmg khi: không counter trên `E`, có standing footprint chạm `E`, và chưa vượt cap block hiệu lực trong phase (xem §1 Block).

**Không đỡ / không hợp lệ:** target theo `CombatTargetPicker` (BaseAv cao nhất trong standing / party).
```

Remove the stale `DmgTaken = BossRaw × (1 − GuardReduction − DissonancePenalty)` block from §9 **or** mark clearly as P0 design-not-shipped if Dissonance is still unimplemented — preferred: remove from “current” §9 and leave Resonance/Dissonance as 🔲 in §14.

- [ ] **Step 5: Fix §10 “Bỏ Phase AV”**

Replace:

```markdown
**Bỏ:** Phase AV · Base AV priority · skill Guard · HB giảm S2 beat
```

With:

```markdown
**Bỏ:** Base AV priority (sort) · skill Guard · HB giảm S2 beat  
**Giữ (legacy):** Phase AV budget gate khi gán skill (150/100)
```

- [ ] **Step 6: Changelog**

Add:

```markdown
| 2026-07-16 | Runtime SoT sync: intro beat 6 · Guard 68/25/10 · Phase AV legacy · enemy zone beat 10 |
```

- [ ] **Step 7: Verify stale phrases gone from current sections**

```powershell
Select-String -Path "docs/combat/COMBAT_MECHANICS.md" -Pattern "localBeat 0\.5|Continue|auto-resume|−15%|−50%|Bỏ:\*\* Phase AV|beat thứ 3"
```

Expected: no matches in current law sections (changelog history lines mentioning Continue may remain — if they match, prefix those changelog rows is OK; do not leave §1/§9 with those values).

Also confirm:

```powershell
Select-String -Path "docs/combat/COMBAT_MECHANICS.md" -Pattern "IntroPlanningPauseAfterBeatIndex|68%|legacy retained|IntroEnemySpawnZoneStartBeat"
```

Expected: hits in §1 / §9.

- [ ] **Step 8: Commit**

```bash
git add docs/combat/COMBAT_MECHANICS.md
git commit -m "Sync COMBAT_MECHANICS with runtime intro, Guard, AV."
```

---

### Task 3: BOSS_ENCOUNTER_DESIGN Guard + kit map

**Files:**
- Modify: `docs/combat/BOSS_ENCOUNTER_DESIGN.md`

**Interfaces:**
- Consumes: Guard % from Task 2
- Produces: BOSS doc points to same barrier model

- [ ] **Step 1: Fix Reactive Guard table (~lines 186–195)**

Replace Early/Late −15% / Perfect −50% with OnBeat 68% / Early 25% / Late 10% / OffBeat 0%, and one-line pointer to `COMBAT_MECHANICS` §1 Block / §9.

- [ ] **Step 2: Fix “Map sang code” kit row**

Replace any “Kit 4 skill cũ” / ambiguous Guard kit row with: **3 skill, no Guard skill** — Space barrier.

- [ ] **Step 3: Deprecated blurb**

Keep deprecated Cycle/Guard **skill**, but clarify Phase AV is **deprecated as UX cycle** yet **budget gate may still exist in code** — or point to MECHANICS *legacy retained*.

- [ ] **Step 4: Verify + commit**

```powershell
Select-String -Path "docs/combat/BOSS_ENCOUNTER_DESIGN.md" -Pattern "−15%|−50%|4 skill"
```

Expected: empty (or only clearly historical).

```bash
git add docs/combat/BOSS_ENCOUNTER_DESIGN.md
git commit -m "Sync BOSS_ENCOUNTER Guard and kit map with runtime."
```

---

### Task 4: UNITY_WORKFLOW + PROJECT_STATUS

**Files:**
- Modify: `docs/setup/UNITY_WORKFLOW.md`
- Modify: `docs/PROJECT_STATUS.md`

**Interfaces:**
- Consumes: beat 6 / Execute from Task 2
- Produces: workflow table matches SoT

- [ ] **Step 1: UNITY_WORKFLOW planning row**

In `## Combat prototype spec` table, replace Planning flow and related UI MVP / Enemy attacks rows:

```markdown
| **Planning flow** | (1) **Deploy** — formation / swap; (2) nhạc → **intro-pause** after beat **6**; (3) đặt skill → bấm **Execute** (không auto-resume) |
| UI MVP | Carousel timeline + lanes + skill panel + Deploy/**Execute** overlay + party/enemy status bar |
| Enemy attacks | Segment 0 min impact ≥ beat **10** (`IntroEnemySpawnZoneStartBeat`); later phases use phase buffer |
```

Update date in section title to `2026-07-16`.

- [ ] **Step 2: Fix combat flow ASCII if it still says Continue**

Search file for `Continue` / `auto-resume` / `0.5` and align with Execute / beat 6.

- [ ] **Step 3: PROJECT_STATUS Phase AV**

Ensure Unity table notes Phase AV **legacy gate** (150/100) still in code — not “design bỏ / code stub” as if unused. Example row:

```markdown
| Phase AV budget (150/100) — **legacy retained** (vẫn gate assign; UI có thể ẩn) | 🟡 legacy |
```

- [ ] **Step 4: Verify + commit**

```powershell
Select-String -Path "docs/setup/UNITY_WORKFLOW.md" -Pattern "Continue|auto-resume|localBeat 0\.5"
Select-String -Path "docs/PROJECT_STATUS.md" -Pattern "legacy|150"
```

```bash
git add docs/setup/UNITY_WORKFLOW.md docs/PROJECT_STATUS.md
git commit -m "Sync UNITY_WORKFLOW and PROJECT_STATUS with runtime SoT."
```

---

### Task 5: Specs 07-14 hybrid banners + inline law fixes

**Files:**
- Modify: `docs/superpowers/specs/2026-07-14-skill-kit-setup-payoff-design.md`
- Modify: `docs/superpowers/specs/2026-07-14-timeline-note-readability-design.md`
- Modify: `docs/superpowers/specs/2026-07-14-counter-presentation-feel-design.md`

**Interfaces:**
- Consumes: claim map from design spec §3
- Produces: specs no longer assert superseded laws as current

- [ ] **Step 1: Banner on all three specs**

Replace or extend the opening `> **Status:**` block with:

```markdown
> **Status:** Implemented — runtime SoT supersedes where noted (2026-07-16)  
> **SoT:** [`docs/combat/SKILL_KIT.md`](../../combat/SKILL_KIT.md) · [`docs/combat/COMBAT_MECHANICS.md`](../../combat/COMBAT_MECHANICS.md)
```

Keep original scope/out-of-scope lines below if still accurate.

- [ ] **Step 2: skill-kit spec — D1 decision**

Change locked decision:

```markdown
| Anchor target | **D1** Delay every **CORE** note whose impact beat lies in Anchor’s **S** beats |
```

To:

```markdown
| Anchor target | **D1′ (runtime)** Delay notes with impact **after** Anchor’s S window (+N); notes **in** S stay put — see `SKILL_KIT` / `DelayImpactTelegraphsAfterBeat` |
```

Also update §4 Charlotte Delay wording and §5 / §7 acceptance item 6 if they still say “impacts in S”.

- [ ] **Step 3: skill-kit spec — Encore chip + doc-only**

Replace visual `S2−1` chip language with buff **icon** on party card (`buff_reduce_s2_v1`).

Replace §8 note 4:

```markdown
4. Wire Delay / ReduceS2 if still doc-only; then layer empower variants.
```

With:

```markdown
4. Delay / ReduceS2 **implemented at Planning** (`ApplyPlanningUtilityEffects`); empower variants shipped — see SoT.
```

- [ ] **Step 4: timeline-note + counter specs — acceptance status**

Add under acceptance / top:

```markdown
**Runtime (2026-07-16):** Shipped. Treat unchecked boxes below as historical checklist; verify against SoT + Play Mode residual only.
```

- [ ] **Step 5: Verify + commit**

```powershell
Select-String -Path "docs/superpowers/specs/2026-07-14-skill-kit-setup-payoff-design.md" -Pattern "doc-only|lies in Anchor|S2−1"
Select-String -Path "docs/superpowers/specs/2026-07-14-*.md" -Pattern "runtime SoT supersedes"
```

Expected: skill-kit no longer asserts doc-only / in-S delay as current; all three have supersedes banner.

```bash
git add docs/superpowers/specs/2026-07-14-skill-kit-setup-payoff-design.md `
  docs/superpowers/specs/2026-07-14-timeline-note-readability-design.md `
  docs/superpowers/specs/2026-07-14-counter-presentation-feel-design.md
git commit -m "Mark 07-14 specs implemented; align superseded laws."
```

---

### Task 6: PROJECT_LOG entry + final grep gate

**Files:**
- Modify: `docs/PROJECT_LOG.md`

**Interfaces:**
- Consumes: all prior tasks
- Produces: handoff note for main

- [ ] **Step 1: Prepend log entry** (newest first, after the header block)

```markdown
## 2026-07-16 — Docs runtime SoT sync (code = SoT)

**Focus:** docs

**Done**
- Sync SoT: Delay after-S · intro-pause beat 6 · Guard 68/25/10 · Phase AV legacy gate · enemy zone beat 10 · Encore icon.
- Specs 07-14: Implemented banner + inline superseded laws (Hybrid Approach 3).
- Spec: `docs/superpowers/specs/2026-07-16-docs-runtime-sot-sync-design.md`.

**Handoff:** `#4` empty-beat vẫn backlog; không đổi C# trong pass này.

**Refs:** `SKILL_KIT.md`, `COMBAT_MECHANICS.md`, `BOSS_ENCOUNTER_DESIGN.md`, `UNITY_WORKFLOW.md`, specs `2026-07-14-*`
```

- [ ] **Step 2: Final stale-phrase gate across SoT + specs**

```powershell
$paths = @(
  "docs/combat/SKILL_KIT.md",
  "docs/combat/COMBAT_MECHANICS.md",
  "docs/combat/BOSS_ENCOUNTER_DESIGN.md",
  "docs/setup/UNITY_WORKFLOW.md",
  "docs/PROJECT_STATUS.md",
  "docs/superpowers/specs/2026-07-14-skill-kit-setup-payoff-design.md",
  "docs/superpowers/specs/2026-07-14-timeline-note-readability-design.md",
  "docs/superpowers/specs/2026-07-14-counter-presentation-feel-design.md"
)
Select-String -Path $paths -Pattern "doc-only|localBeat 0\.5|auto-resume|−15%|−50%|trong cửa S \+N"
```

Expected: **no matches** in those files (changelog historical lines inside MECHANICS that mention Continue are acceptable only inside `## Changelog` dated rows — prefer zero hits; if changelog must keep history, leave them and re-run excluding Changelog by reading file).

- [ ] **Step 3: Commit**

```bash
git add docs/PROJECT_LOG.md
git commit -m "Log docs runtime SoT sync pass for main handoff."
```

---

## Spec coverage (self-review)

| Spec requirement | Task |
|------------------|------|
| SKILL_KIT Delay after-S | Task 1 |
| MECHANICS intro 6 / Execute / enemy 10 / Guard % / Phase AV | Task 2 |
| BOSS Guard + kit | Task 3 |
| UNITY_WORKFLOW + PROJECT_STATUS | Task 4 |
| Specs banners + D1 / chip / doc-only / acceptance | Task 5 |
| PROJECT_LOG entry + final gate | Task 6 |
| No code changes | Global Constraints |
| #4 stays backlog | Global Constraints · log handoff |

**Placeholder scan:** none intentional.  
**Type consistency:** N/A (docs-only); numbers locked in Global Constraints.

# Status Menu + Echo Keys UI — Design Lock

> **Status:** Visual / interaction lock cho implement Unity UI  
> **Date:** 2026-07-19  
> **Refs (SoT art):**
> - Status: [`docs/combat/illustrations/ren_status_menu_lv15_mock.png`](../../combat/illustrations/ren_status_menu_lv15_mock.png) · `Art/UI/StatusMenu/`
> - Echo Keys: [`docs/combat/illustrations/echo_keys_social_link_menu_mock.png`](../../combat/illustrations/echo_keys_social_link_menu_mock.png) · `Art/UI/SocialLink/` · `docs/superpowers/specs/illustrations/`
> - Social Stats: [`docs/combat/illustrations/social_stats_menu_mock.png`](../../combat/illustrations/social_stats_menu_mock.png) · `Art/UI/SocialStats/`
> **Related:** [persona-calendar](./2026-07-11-persona-calendar-design.md) · [combat XP progression](./2026-07-19-combat-level-xp-progression-design.md) · [SKILL_KIT](../../combat/SKILL_KIT.md) · [CHARACTER_LEVEL_PROGRESS](../../combat/CHARACTER_LEVEL_PROGRESS.md)
>
> **Art note (Echo Keys — Hampton lock):** Pose = note-hand raised + **other hand in pocket**. Construction: hand length ≈ face height; wrist thinner than palm; pocket arm only cuff/tension readable. Do not re-inflate hand when regenerating.

---

## 1. Shared visual language

| Token | Lock |
|-------|------|
| Style | Persona-like · slanted panels · geometric shards |
| Palette | Navy / cobalt / cyan / white · accent red on selection |
| Typography | Bold italic display names · clean sans UI labels · EN text MVP |
| Aspect | **16:9** reference; safe margins for 16:10 / 21:9 letterbox later |
| Input language | **Keyboard** prompts in mock (`Esc`, `V`, `Q`, `E`, `Enter`) — map to gamepad in implement |

Mocks = **composition + hierarchy + copy**. Không bắt pixel-perfect shader/glitch; giữ layout zones và affordance.

---

## 2. Screen A — Party Status Menu

### 2.1 Purpose

Xem 1 party member: identity · element · level/XP · skill kit · combat stats. Swap member không rời màn.

### 2.2 Zones (lock)

```
┌────────────────────────────────────────────────────────────┐
│  [Q]«                                              [E]»    │
│  Name (full)                                               │
│  Element label · Lv · NEXT EXP                             │
│  Element icon row (3 unlocked + ? locked)                  │
│                         ┌──────────────┐                   │
│  ┌──────────┐           │  Character   │                   │
│  │ SKILLS   │           │  fullbody /  │                   │
│  │ 3+empty  │           │  bust art    │                   │
│  └──────────┘           └──────────────┘                   │
│              ┌─────────────────────┐                       │
│              │ STATS (bars)        │                       │
│              └─────────────────────┘                       │
│                              [Esc] Back  [V] View Skills   │
└────────────────────────────────────────────────────────────┘
```

| Zone | Content lock |
|------|----------------|
| **Identity** | Full legal name (Ren = **Ren Takahashi**). Subtitle = active element name (Melody / Rhythm / Harmony). |
| **Level / XP** | `Lv {n}` + `NEXT EXP {xpToNext}` — Combat XP party level ([progression spec](./2026-07-19-combat-level-xp-progression-design.md)). Lv15 sample: **NEXT EXP 3600**. |
| **Element row** | **3** badge icons SoT art: `icon-he-nhip` (Rhythm) · `icon-he-giai-dieu` (Melody) · `icon-he-hoa-am` (Harmony). Active member element = highlight ring. Remaining slots = dark circle + **?** (future keys/elements — not invent icons). |
| **Skills** | Kit unlocked at current level ([SKILL_KIT](../../combat/SKILL_KIT.md)). Ren Lv15: **Strike · Crosscut · Finale**. Empty slots = `—`. Grid 2-col OK. |
| **Stats** | Labels: **St · Ma · En · Hb · Lu**. Mapping: St←STR, Ma←Ma, En←EN, Hb←Heartbeat, Lu←Luck. |
| **Stat bars** | Visual max = **300** (full width). **Không** hiện chữ MAX / `/300` / cap trên UI. Chỉ số hiện tại cạnh bar. |
| **Portrait** | Character art bên phải (slot “quái/Persona” kiểu P3). Ren mock dùng school fullbody look-over-shoulder. |
| **Swap** | **[Q]** left edge · **[E]** right edge — cycle party order (Ren → Charlotte → Coda → …). Wrap. |
| **Footer** | **[Esc]** Back · **[V]** View Skills (mở skill detail — stub OK MVP). |

### 2.3 Sample data (Ren Lv15 optimal — mock)

| Field | Value |
|-------|-------|
| St | 42 |
| Ma | 9 (8.8 rounded) |
| En | 12 (11.8 rounded) |
| Hb | 167 |
| Lu | 18 |
| Bar fill | value / 300 |

Charlotte / Coda: cùng layout; đổi name, element highlight, skills, stat numbers từ SoT.

### 2.4 Non-goals (Status)

- Không edit stats trên màn này (allocation = flow level-up riêng).
- Không hiện social Bond rank ở đây.
- Cover gauge / Prep pips = combat HUD, không bắt buộc trên Status.

---

## 3. Screen B — Echo Keys (Social Link list)

### 3.1 Purpose

Danh sách Bond theo **Echo Key** (không dùng Arcana Persona). Chọn 1 key → (sau này) detail / hangout / rank story.

### 3.2 Zones (lock)

```
┌────────────────────────────────────────────────────────────┐
│  LIST                                                      │
│  ┌─────────────────────────┐     Ren bust + glowing note   │
│  │ I   Melody              │     (cyan/white eighth-note)│
│  │ II  Bass                │                             │
│  │ III Harmony             │     watermark: ECHO KEYS      │
│  │ IV  Measure             │                             │
│  │ V   Dissonance          │                             │
│  │     ▼ Scroll  (peek)    │                             │
│  └─────────────────────────┘                             │
│         Whose Echo Key…     [Enter] Confirm  [Esc] Back    │
└────────────────────────────────────────────────────────────┘
```

| Zone | Content lock |
|------|----------------|
| **List title** | `LIST` (hoặc localized sau). |
| **Row** | Index (I–V visible) · **Echo Key name** (primary) · NPC subtitle · optional Rank badge. |
| **Visible rows** | **5** simultaneous. |
| **Scroll** | Affordances rõ (▼ Scroll). Peek hàng kế (**Pulse** / Astra). Scroll tiếp: reserved keys Arc2+. |
| **Selection** | Selected = white panel + dark text + red accent edge (như mock). |
| **Hero art** | Ren (hoặc current player face) · tay tỉ lệ giải phẫu bình thường · đỡ/nắm **nốt nhạc xanh–trắng** phát sáng (không tarot card). |
| **Watermark** | **ECHO KEYS** diagonal, translucent. |
| **BG** | Cyan→navy dream/plaza geometric — bám mock (Persona SL ref). |
| **Footer** | Prompt: `Whose Echo Key do you want to view?` · **[Enter]** Confirm · **[Esc]** Back. |

### 3.3 Echo Key order (lock list order)

**Viewport (5):**

| # | Echo Key | Subtitle (NPC) |
|---|----------|----------------|
| I | Melody | Ren Takahashi |
| II | Bass | Charlotte |
| III | Harmony | Coda |
| IV | Measure | Ryo |
| V | Dissonance | Mei Lin |

**Scroll (tiếp):**

| # | Echo Key | Subtitle |
|---|----------|----------|
| VI | Pulse | Astra |
| VII+ | Rest, Overtone, Static, Cadence, Crescendo, Fermata | Locked / TBD — UI: key name + lock state, không bịa NPC |

Rank badge trên mock = **illustrative**. Runtime: `BondProgress.Rank` + ArcCap ([calendar design §6](./2026-07-11-persona-calendar-design.md)).

### 3.4 Non-goals (Echo Keys)

- Không Q/E swap character trên list này (khác Status).
- Không hiện full social-stat bars trên list (detail screen sau).
- Confirm → detail panel / rank timeline = **phase sau**; mock chỉ lock list shell.

---

## 3b. Screen C — Social Stats (Resonance Field)

### Purpose

Xem 5 Social Stats (calendar / Bond gates). Layout bám Metaphor radial; **tông FC** navy/cyan như Status + Echo Keys.

### Zones (lock)

| Zone | Content |
|------|---------|
| Title | `SOCIAL STATS` (+ optional JP subtitle) |
| Hero | Ren **head/bust** bottom-center · cyan–white grade · earpiece |
| Chart | 5-axis radar · rays from head · polygon = current ranks (1–10 scale; mock may show 1–5 ticks) |
| Nodes L→R | **Resonance · Cadence · Pulse · Harmony · Rhythm** — icon + name + Rank + short flavor |
| Watermark | `RESONANCE FIELD` / `SOCIAL STATS` diagonal |
| Footer | **[Esc] Back** (sharp keycap) |

Stat names SoT: [persona-calendar §5](./2026-07-11-persona-calendar-design.md). Rank numbers on mock = illustrative; runtime từ `SocialStatsState`.

**Art:** [`social_stats_menu_mock.png`](../../combat/illustrations/social_stats_menu_mock.png)

---

## 4. Input map (implement)

| Action | Keyboard | Notes |
|--------|----------|-------|
| Back | Esc | Cả 2 màn |
| Confirm / open | Enter | Echo Keys list |
| View Skills | V | Status only |
| Prev member | Q | Status only |
| Next member | E | Status only |
| List up/down | ↑↓ / W S | Echo Keys |
| Scroll page | Mouse wheel / PgUp PgDn | Echo Keys |

Gamepad: map sau; giữ cùng semantic.

---

## 5. Art / asset dependencies

| Asset | Path |
|-------|------|
| Status mock | `docs/combat/illustrations/ren_status_menu_lv15_mock.png` |
| Echo Keys mock | `docs/combat/illustrations/echo_keys_social_link_menu_mock.png` |
| Element badges | `Assets/.../Prefabs/UI/badge Icon/icon-he-{nhip,giai-dieu,hoa-am}.png` |
| Ren menu fullbody | `Art/Characters/Ren/School/ren_hima_uniform_menu_fullbody_v1.png` |
| Skill icons (optional) | `Art/UI/Skills/Ren/*` |

Runtime UI: rebuild bằng uGUI/UI Toolkit theo zone — **không** dùng PNG mock làm texture fullscreen production (trừ tạm prototype).

---

## 6. Acceptance checklist (UI build)

**Status**

- [ ] Full name + element + Lv + NEXT EXP bind từ meta/combat state
- [ ] 3 element badges + ? slots; highlight đúng hệ unit
- [ ] Skills bind kit theo level; empty = —
- [ ] Stats St/Ma/En/Hb/Lu; bar fill = value/300; no cap label
- [ ] Q/E cycle party; Esc back; V skills stub
- [ ] Portrait slot per member

**Echo Keys**

- [ ] 5 rows + scroll; order Melody→…→Dissonance then Pulse→reserved
- [ ] Key name primary; NPC subtitle; rank from BondState
- [ ] Selection style + Confirm/Esc
- [ ] Hero art + note prop (hoặc spine/static) tỉ lệ tay ổn
- [ ] Locked reserved keys không chọn / grey

---

## 7. Changelog

| Date | Note |
|------|------|
| 2026-07-19 | Lock Status + Echo Keys mocks; bar max 300 hidden; Hb; Esc/V/Q/E; Echo list 5+scroll |
| 2026-07-19 | Echo Keys art: Hampton proportion — note-hand ≈ face; other hand in pocket |
| 2026-07-19 | Add Social Stats mock (Metaphor radial · FC cyan tone · Ren head) |

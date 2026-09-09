# STAT Menu (Crystal Kit) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thay `PartyStatusMenuUI` (placeholder code-built) bằng màn STAT 3 cột theo mock crystal-glass, vẫn mở từ `MetaStatusMenuUI` tab STATS.

**Architecture:** Layout sống trên scene/prefab (SerializeField), giống `CharacterBuildMenuUI` + Config — không `BuildHierarchy` runtime. Chrome cắt từ kit mock; data bind từ SoT hiện có (`UnitStatBlockSO` / loadout / skill kit). Tab BONDS / CALENDAR / SYSTEM giữ overlay cũ.

**Tech Stack:** Unity 6 · UGUI · `UiFontCatalog` · `FcColorTokens` (token glass mới) · Canvas 1920×1080

## Global Constraints

- Canvas reference **1920×1080**, Scale With Screen Size, Match 0.5, title-safe 90%.
- Input: Keyboard Q/E cycle member, Esc back (giữ `TownMapInput`); map gamepad sau.
- Copy MVP **EN**; JP subtitle mock là optional.
- Character art: fullbody school hiện có (Ren / Charlotte / Coda). Không gen nhân vật mới.
- Mock = composition. Không bake số/tên vào PNG.
- Layout Rect **chỉ** trên scene/prefab. Apply kit chỉ bind sprite.
- Không invent Skill Point economy, 6 elemental resist, Memory Fragment, Battle Style trừ khi Task 0 lock chúng là data thật.

---

## Hiện trạng (SoT code + art)

| Lớp | File | Việc đang làm | Gap vs mock |
|-----|------|----------------|-------------|
| Shell 4 tab | `Hub/MetaStatusMenuUI.cs` | STATS / BONDS / CALENDAR / SYSTEM; Persona navy; BG `statusmenu_ren_bg_v6` | Visual kit mới; nav mock = vertical crystal buttons |
| Party STAT | `Hub/PartyStatusMenuUI.cs` | Runtime-build: name, Lv hardcode 15, 5 bar St/Ma/En/Hb/Lu, text 3 skill slots, Q/E, V equip | Không portrait, không 3 cột, không header 8 chip, không resist/style/memory |
| Build (richer) | `Hub/CharacterBuild/CharacterBuildMenuUI.cs` | Scene refs: element icons, 5 skill rows, 5 stats + allocate, portrait Ren | Scene riêng, chưa wire CampusHub; layout cũ |
| Data | `UnitStatBlockSO` · `SKILL_KIT.md` · roster 3 | STR/Ma/EN/HB/Luck; 3 skill/nhân vật; unlock theo level; **không SP** | Mock: 4 stat, 10 slot + SP, 6 resist, Memory, Style, 8 portrait, LV/99 |
| Art cũ | `Art/UI/StatusMenu/` | Button parallelogram, BG plate, prompts | Không dùng cho kit mới |
| Art sẵn (reuse) | Fullbody school ×3 · combat face icons · Montserrat | — | Cần chrome/icon kit mới |

Luồng mở: Town Map MENU → `MetaStatusMenuUI.Show(Tab.Stats)` → `PartyStatusMenuUI.Show`.

---

## File map

| Path | Role |
|------|------|
| `Art/UI/StatMenu/Kit/` | Chrome 9-slice (panel, slot, bar, nav, close) |
| `Art/UI/StatMenu/Icons/` | Stat / lock / plus / (resist nếu lock) |
| `Art/UI/StatMenu/Decor/` | HUD ring, crystal, waveform |
| `Resources/UI/StatMenu/` | Runtime load nếu cần (mirror Config) |
| `Hub/StatMenu/StatMenuUI.cs` | Controller bind + input (thay PartyStatus) |
| `Hub/StatMenu/StatMenuSkillSlotView.cs` | 1 card × 10 instance |
| `Hub/StatMenu/StatMenuPortraitChipView.cs` | Header chip × 8 |
| `Hub/StatMenu/StatMenuStatRowView.cs` | 1 hàng STR/MA/EN/HB (+ Luck nếu lock) |
| Prefab `Resources` hoặc scene CampusHub child `StatMenuRoot` | Layout SoT |
| `Hub/MetaStatusMenuUI.cs` | Swap `partyStatusMenu` → `StatMenuUI` |
| `docs/superpowers/specs/2026-07-19-status-and-echo-keys-ui-lock.md` | Ghi visual lock mới (sau khi Task 0) |

Không sửa `CharacterBuildMenuUI` trong epic này (allocate vẫn scene Build).

---

## Data lock (chốt trước Task 2)

| Mock | SoT | Quyết định kế hoạch (đổi được ở Task 0) |
|------|-----|------------------------------------------|
| STR MA EN HB | + **Luck** | **Hiện 4 hàng mock.** Luck không lên STAT (vẫn trong CharacterBuild). |
| 10 skill + SP + `+` | 3 skill, no SP | **10 ô:** 01–03 = kit unlock; 04–10 locked. Nút `+` không spend. |
| 6 resist | 3 Harmony element | **Hàng resist = stub 0%** (icon cắt từ kit). Không hệ combat mới. |
| Memory Fragment | không có | **Gauge visual 0%.** |
| Battle Style BALANCE | không có | **Label cố định BALANCE.** |
| 8 portrait | roster 3 | **3 face + 5 lock.** Click chip = swap member (thay Q/E cạnh màn). Q/E vẫn cycle. |
| LV 01/99 | cap 18 | **`LV. {n} / 18`.** |
| KAI | Ren | Fullbody Ren school. |

---

### Task 0: Lock data + visual token

**Files:**
- Modify: `docs/superpowers/specs/2026-07-19-status-and-echo-keys-ui-lock.md` (section Screen A — append crystal kit; không xóa SoT data)
- Create: `docs/ui/STAT_MENU_TOKENS.md` (hex glass / border / fill)

**Produces:** Bảng lock ở trên đã confirm (hoặc diff 1 dòng).

- [ ] Confirm 4 vs 5 stat, SP, resist stub, LV/18 với user nếu khác bảng
- [ ] Ghi token màu (lavender glass, violet glow, cyan fill) — **không** trộn navy Persona trên màn STAT
- [ ] Play Mode: không đổi code

---

### Task 1: Slice kit chrome

**Files:**
- Create: `Art/UI/StatMenu/Kit/*.png` + `.meta` (9-slice borders)
- Source: mock no-object + kit user đã gửi (workspace assets Stats_Mock*)

**Produces:** Sprite list P0 — `ui_stat_panel_{portrait,status,skill}_v1`, `ui_stat_header_bar_v1`, `ui_stat_btn_nav_{normal,selected}_v1`, `ui_stat_btn_close_v1`, `ui_stat_slot_skill_{empty,locked}_v1`, `ui_stat_slot_portrait_{empty,locked,active}_v1`, `ui_stat_bar_{track,fill}_v1`, `ui_stat_badge_sp_v1`, `ui_stat_divider_glow_v1`, `ui_stat_hud_ring_v1`, `ui_stat_crystal_shard_v1`, `ui_stat_waveform_v1`

Import: Sprite, Clamp, alphaIsTransparency, 9-slice panel/slot/bar. Wrap Clamp. Filter Bilinear.

- [ ] Cắt PNG, set border, không bake text
- [ ] Import Unity, kiểm tra 9-slice trên Image Sliced ở 1920×1080

---

### Task 2: Prefab 3 cột (dummy data)

**Files:**
- Create: prefab `Assets/FracturedChorus/Hub/StatMenu/StatMenu.prefab` (hoặc child trên CampusHub, inactive)
- Create: `StatMenuSkillSlotView.cs`, `StatMenuPortraitChipView.cs`, `StatMenuStatRowView.cs`, `StatMenuUI.cs` (Show/Hide + serialized refs only)

**Produces:** `StatMenuUI.Show` hiện dummy Ren Lv1, 4 stat = 10, 10 skill empty, 8 chip (1 active + 7 lock).

Layout zones (anchor, không pixel tuyệt đối ngoài seed lần đầu):

```
Header: STAT title | 8 chips | Close
Left:   index+name | LV/EXP | fullbody + ring | Memory gauge
Center: 4 stat rows | resist 6 | Battle Style
Right:  SKILL POINT stub | 10 skill cards
Nav:    STAT selected · BONDS · CALENDAR · SYSTEM (wire tab sau)
```

- [ ] Dựng hierarchy trên scene, Save
- [ ] CanvasScaler 1920×1080
- [ ] Play: overlay full-screen, Esc ẩn (input stub OK)

---

### Task 3: Bind SoT roster + stats + skills

**Files:**
- Modify: `StatMenuUI.cs`
- Consumes: `PartyCharacterIds`, `GameMetaState.Loadout`, `PartyStatBases` (move shared helper khỏi `PartyStatusMenuUI` nếu duplicate), `SkillUnlockCatalog`, `UnitStatBlockSO` / `PartyStatBases`
- Modify: `PartyStatusMenuUI.cs` — không xóa; Meta sẽ ngừng gọi

**Produces:**
- Roster cycle Q/E + click chip 0–2
- Name `01 REN` (display), LV from stub/combat XP nếu đã có trên meta
- Bars: value / 300 (giữ lock cũ, không in `/300`)
- Slots 01–03: tên/icon skill unlock; 04–10 locked sprite
- Fullbody swap Ren/Charlotte/Coda sprites

- [ ] Bind 3 nhân vật, so số với CharacterBuild Lv15 sample
- [ ] Play: Q/E đổi portrait + stats + 3 skill

---

### Task 4: Wire MetaStatusMenuUI

**Files:**
- Modify: `Hub/MetaStatusMenuUI.cs` (`partyStatusMenu` → `StatMenuUI` hoặc adapter)
- Modify: `Editor/CampusHubSceneSetupEditor.cs` Wire Town Map Status Menu — Ensure prefab instance

**Produces:** MENU → STATS mở kit mới. BONDS/CALENDAR/SYSTEM không đổi. SYSTEM/Esc đóng shell.

Nav 4 nút crystal trên STAT overlay: STAT = ở lại; BONDS/CALENDAR/SYSTEM = Hide StatMenu + `SetTab` shell cũ **hoặc** hide crystal nav và giữ list Persona bên phải. **Chọn A:** crystal nav thay 4 button parallelogram khi STAT overlay mở (đúng mock). Shell Persona ẩn khi StatMenu open.

- [ ] Wire tab + close
- [ ] Play: Town Map MENU → STAT mock; Esc về map; BONDS vẫn Echo/Social overlay cũ

---

### Task 5: Focus + juice tối thiểu

**Files:**
- Modify: `StatMenuUI.cs`

**Produces:** EventSystem focus Close hoặc chip 0 lúc open. Focus indicator = selected chip sprite (không chỉ đổi màu). Không poll stats trong Update — refresh on Show/cycle.

- [ ] Gamepad/keyboard: chips → stat rows (non-interactive) → skill grid (no spend) → nav → close
- [ ] Reduced motion: skip waveform loop nếu có

---

## Non-goals (epic này)

- CharacterBuild allocate / remaining points
- Skill Point spend, 6-element combat resist, Memory/Style systems
- BONDS / CALENDAR / SYSTEM visual kit
- JP font, shader frosted blur (PNG glass đủ)

## Test plan (Play Mode)

- [ ] 16:9 1920×1080: 3 cột không chồng, close trong safe zone
- [ ] Ren/Charlotte/Coda: fullbody + 4 stat + 3 skill
- [ ] Chip 4–8: lock, click không swap
- [ ] Esc / SYSTEM đóng; Q/E wrap roster
- [ ] BONDS/CALENDAR overlay cũ vẫn mở từ nav

## Open

- Skill icon: dùng placeholder Limbus hiện có hay note generic?
- Shell Persona BG v6: tắt hẳn khi StatMenu mở, hay dim dưới glass?

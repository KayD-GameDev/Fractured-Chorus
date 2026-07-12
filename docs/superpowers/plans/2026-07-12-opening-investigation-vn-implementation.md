# Opening Investigation VN — Implementation Plan

**Date:** 2026-07-12  
**Design spec:** [`../specs/2026-07-12-opening-investigation-vn-design.md`](../specs/2026-07-12-opening-investigation-vn-design.md)  
**Target:** Unity 6 · `Assets/FracturedChorus/`  
**Estimate:** 4 phases · ~3–5 ngày (1 dev)

---

## 0. Principles

- Logic in `.cs`; scene = layout + serialized refs (project Unity workflow)
- Hybrid C: shared **VnRuntime** + data-driven **VnScript**; Opening = linear only
- Reuse Prologue typewriter / input / audio patterns without breaking PrologueVN
- Dialogue language: **English** (from design §7)
- Ren portrait/sprite: **school uniform only** — `Art/Characters/Ren/School/ren_hima_uniform_menu_fullbody_v1.png`
- Missing art/SFX must not soft-lock advance
- Mock/content text lives in script asset under `Narrative/Scripts/`; keep controllers thin
- Each phase has **acceptance criteria** before the next

### Target flow

```
MainMenu → PrologueVN → OpeningInvestigation → CampusHub (01/09)
```

### Out of scope (this plan)

- Choice UI / branching
- Undertone Desync tutorial for Ren
- Full save-anywhere mid-VN
- Final BG/SFX production packs (placeholders OK)

---

## Phase 1 — Shared VN runtime skeleton (1–1.5 ngày)

### 1.1 Folder & types

| File | Nội dung |
|------|----------|
| `Narrative/Vn/VnBeatKind.cs` | `Line, Narration, Cue, TextCard, Fade, End` (+ reserved `Choice` enum value unused) |
| `Narrative/Vn/VnBeat.cs` | Serializable beat: kind, speakerId, text, expression, bgId, bgmId, sfxId, duration, setFlags[] |
| `Narrative/Vn/VnScriptSO.cs` | ScriptableObject: `id`, `nextScene`, `beats[]` |
| `Narrative/Vn/VnSpeakerCatalog.cs` | Map speakerId → display name + optional sprite (Ren school bind here) |
| `Narrative/Vn/VnCueResolver.cs` | Resolve bg/bgm/sfx IDs; log + no-op on miss |

### 1.2 Runtime

| File | Nội dung |
|------|----------|
| `Narrative/Vn/VnTypewriterView.cs` | Thin wrapper or shared extract from `PrologueTypewriterView` (prefer wrap/reuse first; extract only if duplication hurts) |
| `Narrative/Vn/VnInput.cs` | Advance input (reuse `PrologueInput` or alias) |
| `Narrative/Vn/VnAudioPlayer.cs` | Play BGM/SFX by clip or id; silence on miss |
| `Narrative/Vn/VnRuntimeController.cs` | Walk beats: show text, wait advance, apply cues/fades, on End set flags + load scene |

**Prologue:** leave `PrologueVNController` intact for this phase. Optional follow-up: point typewriter bind at shared helper — not required for Opening ship.

### 1.3 Flag id

| File | Change |
|------|--------|
| `Meta/StoryFlagIds.cs` | Add `OpeningInvestigationDone = "opening_investigation_done"` |

**Acceptance Phase 1:**
- [ ] Create empty `VnScriptSO` with 2–3 test beats; Play Mode stub scene (or Editor play helper) advances through End
- [ ] Missing sfxId logs error, text still advances
- [ ] Compiles; PrologueVN still playable unchanged

---

## Phase 2 — Opening script data + scene (1.5–2 ngày)

### 2.1 Catalog & Build Settings

| File | Change |
|------|--------|
| `RunMap/RunMapSceneCatalog.cs` | Add `OpeningInvestigation = "OpeningInvestigation"` |
| `RunMap/RunMapSceneLoader.cs` | Path resolve if catalog-driven |
| Build Settings | Add `Scenes/OpeningInvestigation.unity` after PrologueVN, before CampusHub |

### 2.2 Script asset

| Asset | Nội dung |
|-------|----------|
| `Narrative/Scripts/OpeningInvestigation_EN.asset` (or JSON + importer if team prefers; **default SO**) | Full design §7 beats SCENE 01–05 |
| Speaker ids | `haruto`, `ryo`, `mei_lin`, `ren` |
| Ren sprite | Bind school uniform path above |
| `nextScene` | `CampusHub` |
| End `setFlags` | `lumina_case_open`, `opening_investigation_done`, `ren_arrived_hima` |

Authoring: Editor menu **Fractured Chorus → Narrative → Populate Opening Investigation Script** that fills SO from a static EN table in `OpeningInvestigationScriptBuilder.cs` (keeps long text out of scene YAML; rebuildable).

### 2.3 Scene setup

| File | Nội dung |
|------|----------|
| `Scenes/OpeningInvestigation.unity` | Canvas: dialogue panel, nameplate, body, portrait Image, fade overlay, text-card, BG Image |
| `Editor/OpeningInvestigationSceneSetupEditor.cs` | Mirror Prologue setup: create hierarchy, wire `VnRuntimeController`, assign script SO + Ren sprite |

Placeholder BGs: solid color or crop from `lumina-city-town-map-bg_v1.png` until dedicated night/alley art exists.

### 2.4 Meta bootstrap timing

| File | Change |
|------|--------|
| `PrologueVNController.cs` | `nextSceneName` default → `OpeningInvestigation`; do **not** call hub bootstrap here (already only ResetSession at Start) |
| `VnRuntimeController` End (Opening) | Call `GameMetaSession.BeginHubAfterPrologue()` **or** new `BeginHubAfterOpening()` that = CreateHubStart + `opening_investigation_done` |
| `GameMetaState.CreateHubStart()` | Ensure `LuminaCaseOpen` + `RenArrivedHima`; Opening End also sets `OpeningInvestigationDone` |
| `CampusHubController` | Keep fallback `BeginHubAfterPrologue()` if session missing (dev enter hub direct) |

Prefer rename clarity:

```csharp
// GameMetaSession
BeginHubAfterOpening() // CreateHubStart + OpeningInvestigationDone
```

Keep `BeginHubAfterPrologue()` as obsolete alias → same method for one release to avoid breaking Editor previews.

**Acceptance Phase 2:**
- [ ] Editor menu builds Opening scene hierarchy
- [ ] Play OpeningInvestigation: full EN script advances Haruto → crime → Ren arrival → CampusHub
- [ ] Hub date 01/09; flags `lumina_case_open`, `ren_arrived_hima`, `opening_investigation_done`
- [ ] Ren shows **school** sprite, not combat

---

## Phase 3 — Wire New Game flow (0.5 ngày)

| Task | Detail |
|------|--------|
| Prologue exit | Confirm serialized + default `nextSceneName = OpeningInvestigation` |
| Main Menu | Unchanged (still loads PrologueVN) |
| Build Settings order | MainMenu → PrologueVN → OpeningInvestigation → CampusHub → … |
| Docs patch | Update `PROLOGUE_VN_SETUP.md` flow line; note in persona-calendar design §16.1 “superseded by 2026-07-12 opening spec” (short pointer, not full rewrite) |

**Acceptance Phase 3:**
- [ ] New Game from Main Menu → Prologue → Opening → Hub without manual scene load
- [ ] Skip/disagree paths in Prologue still return Main Menu (unchanged)

---

## Phase 4 — Audio/visual polish stubs (0.5–1 ngày)

| Cue ID (from design) | MVP behavior |
|----------------------|--------------|
| `bgm_lumina_night_ambient` | Placeholder loop or silence |
| `bgm_resonance_undertone` | Optional metallic bed if file exists; else silence |
| `Eternal Spark` clean (Ren) | Existing track, **not** Cadence Remix if a separate clean clip exists; else use available bed at lower “radio” feel |
| `Bring Me Home` | Silence OK |
| SFX skips / pulse / shutter | Reuse Prologue typing/click where plausible |

| Visual | MVP |
|--------|-----|
| Haruto / Ryo / Mei Lin | Nameplate only or gray silhouette quad |
| Crime / alley BG | Dark tint panels |
| Text cards | Full-screen fade + centered TMP/Text |
| Light rain | Optional particle later; narration mentions rain is enough for MVP |

**Acceptance Phase 4:**
- [ ] No null-ref on missing clips
- [ ] Ren beat plays clean Spark (or documented fallback) without undertone cue used on Haruto path
- [ ] Full New Game path still green

---

## File checklist (create / touch)

### Create

- `Narrative/Vn/VnBeatKind.cs`
- `Narrative/Vn/VnBeat.cs`
- `Narrative/Vn/VnScriptSO.cs`
- `Narrative/Vn/VnSpeakerCatalog.cs`
- `Narrative/Vn/VnCueResolver.cs`
- `Narrative/Vn/VnAudioPlayer.cs`
- `Narrative/Vn/VnRuntimeController.cs`
- `Narrative/Vn/OpeningInvestigationScriptBuilder.cs` (Editor or runtime static table)
- `Narrative/Scripts/OpeningInvestigation_EN.asset`
- `Scenes/OpeningInvestigation.unity`
- `Editor/OpeningInvestigationSceneSetupEditor.cs`

### Modify

- `Meta/StoryFlagIds.cs` — new flag
- `Meta/GameMetaSession.cs` / `GameMetaState.cs` — hub-after-opening
- `RunMap/RunMapSceneCatalog.cs` (+ loader if needed)
- `Narrative/PrologueVNController.cs` — next scene default
- Build Settings / Editor build list helpers if project uses them
- `Scenes/PROLOGUE_VN_SETUP.md` — flow note
- `docs/superpowers/specs/2026-07-11-persona-calendar-design.md` — §16.1 pointer to new order

### Do not modify for this plan

- Combat Ren animation folders
- Choice UI
- CampusHub morning stubs beyond flag compatibility

---

## Test plan (manual)

1. Enter Play from OpeningInvestigation alone → Hub + flags.
2. New Game → Prologue (agree + contract) → Opening full read → Hub 01/09.
3. Prologue disagree → Main Menu.
4. Force-delete Ren sprite ref → nameplate still works, advance OK.
5. Force-missing BGM id → console error once, advance OK.
6. Confirm Ren visual = `ren_hima_uniform_menu_fullbody_v1` (Inspector).

---

## Risk & mitigations

| Risk | Mitigation |
|------|------------|
| Prologue typewriter tightly coupled | Wrap first; don’t force extract in Phase 1 |
| Long EN script in SO hard to edit | Builder menu regenerates SO from C# const table |
| Hub bootstrap called twice | Only Opening End (or Hub fallback) creates hub state; Prologue only `ResetSession` at start |
| Calendar doc drift | One-line supersede note in §16.1 |

---

## Done definition

Playable OpeningInvestigation matches design success criteria §10: linear EN script, school Ren, clean Top-1 for Ren, correct flags, New Game chain wired, missing assets non-blocking.

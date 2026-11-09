# Ren — Combat Idle FBF Production Specification

**Ngày:** 2026-11-06 · **Vai trò:** full production requirements, QC, Unity, acceptance.

**SoT:** `Assets/FracturedChorus/Art/Characters/Ren/Battle/ren_combat_battle_idle_semichibi_v2_full.png` — **864 × 1152**.

**Intent:** [`REN_COMBAT_IDLE_ANIMATION_BRIEF.md`](REN_COMBAT_IDLE_ANIMATION_BRIEF.md) · **Identity:** [`CHARACTER_LOCK.md`](../../../Assets/FracturedChorus/Art/Characters/Ren/CHARACTER_LOCK.md) · **AI pilot:** [`REN_COMBAT_IDLE_FBF_AI_PROMPT_PACK_v1.1.md`](REN_COMBAT_IDLE_FBF_AI_PROMPT_PACK_v1.1.md) · **Canvas:** [`REN_COMBAT_V2_FULL_READINESS_AUDIT.md`](REN_COMBAT_V2_FULL_READINESS_AUDIT.md).

---

## 1. Production Goal

Sản xuất **bốn PNG** F1–F4 cho Combat Idle FBF, loop **1,5–2,0 s**, khớp Brief và CHARACTER_LOCK. Skeletal ngoài scope trừ khi lead mở rộng.

---

## 2. Frame Definitions

Biên độ: Brief **§3** (F3 = peak **1–2 px** @ 864×1152; F2 &lt; F3; F4 gần F1 hơn F3).

| Key | Definition |
|-----|------------|
| **F1** | Byte-identical SoT (hash) |
| **F2** | Early inhale; dưới peak; rigid tay–súng với torso |
| **F3** | Inhale peak; **maximum** cycle displacement |
| **F4** | **Distinct** key — silhouette/rest **gần F1**, **không** duplicate F1; **F4→F1** mượt |

---

## 3. Timing

Loop: **F1 → F2 → F3 → F4 → F1**. Hold %: Brief **§7** (30 / 25 / 15 / 30).

**SpriteRenderer:** discrete sprite swap + hold; **no** pixel morph between sprites.

**Ví dụ minh hoạ** (không bắt buộc Unity FPS): **T = 1,6 s**, **20 Hz**, **32** samples (0…31).

| Key | Holds (samples) | Start index |
|-----|-----------------|-------------|
| F1 | 10 | **0** |
| F2 | 8 | **10** |
| F3 | 5 | **18** |
| F4 | 9 | **23** |

---

## 4. Canvas / Export

| Field | Rule |
|-------|------|
| Size | **864 × 1152** all keys |
| Format | PNG RGBA |
| Alpha | **0 / 255 only** — no feather |
| Transform | No crop, resize, rotate |
| Color | No global exposure shift vs F1 |
| Layers | Flatten one layer per export |

---

## 5. Permitted Regions — Full Production

| Region | vs F1 |
|--------|-------|
| Torso | Chest, shoulders, front blazer; breath-phased folds |
| Upper arm (on torso) | With rigid hand–weapon group |
| Hair | Minimal edge motion |
| SyncPod + wire | Optional if attached; else F1 |

Không mở rộng sang mặt/chân redesign ngoài under-draw kiểm soát (§7).

---

## 6. Locked Regions — Full Production

Face/expression; hair mass; uniform identity; stance/feet; Violin Rifle design; hand–weapon **rigid** with torso; SyncPod rules; binary alpha. Head ≤ ~1 px with torso if needed; no independent neck acting.

---

## 7. Production Workflow

**F1:** byte copy SoT; SHA-256 QC; F2–F4 separate files.

**Naming:**

```
ren_combat_battle_idle_fbf_f01_sot.png
ren_combat_battle_idle_fbf_f02_inhale.png
ren_combat_battle_idle_fbf_f03_inhale_peak.png
ren_combat_battle_idle_fbf_f04_exhale.png
```

**Occlusion:** at Brief amplitudes, group transform reduces need; local under-draw if gaps; lock weapon glow from F1.

---

## 8. Pilot Exception — AI Masked Editing

Restricted feasibility test — **not** full Combat Idle (prompts: Prompt Pack v1.1).

| Rule | Pilot |
|------|-------|
| Edit | Interior upper-torso / shoulder **fabric** (mask) |
| Hand–weapon | **Pixel-lock F1** (≠ full rigid-with-torso) |
| Lock | Face, hair, SyncPod, lower body, legs, feet |
| Forbid | Silhouette shift; whole-character transform |
| In-mask | Folds, tension, shading only |
| Post-gen | Prompt Pack **§5** → **§7**; QC Spec **§10** |

**Passing pilot does not approve full production** (**§12.2** vs **§12.1**).

---

## 9. Difference Map QC — Full Production

1. F1 vs Fn: same dimensions; binary alpha.  
2. Absolute RGB difference; review Δ ≠ 0.  
3. Diff allowed: §5 (+ justified under-draw).  
4. Blink: face/weapon stable.  
5. **F4 vs F1:** diff ≠ 0; diff &lt; F3 vs F1.

**Reject:** significant face/weapon drift outside rigid rules; non-binary alpha.

---

## 10. Pilot QC — AI Masked Editing

After Prompt Pack **§5** (pixel restoration):

1. RGB outside mask = F1.  
2. Alpha = F1 (0/255).  
3. Canvas 864×1152, alignment kept.  
4. Seam visual at mask boundary.  
5. No diff on locked regions.

**Does not replace §9.**

---

## 11. Unity Import

| Item | Rule |
|------|------|
| Sprite | Single mode; shared PPU |
| Pivot | Shared F1–F4 (footing per readiness) |
| Mesh | Prefer Full Rect if bounds jitter |
| Compression | Preserve binary alpha |
| Clip | Four sprites on §3 schedule; seamless loop |

No `UnitPreset_Ren` production wire until **§12.1** unless sandbox per lead.

---

## 12. Acceptance and Rollback

### 12.1 Full production acceptance

Pass all: F1 hash (§7); §4 export; CHARACTER_LOCK; §5–6 regions; **§9**; F4 distinct near F1 (§2); naming §7.

### 12.2 Pilot acceptance

Pass **§10** only. **Not** §12.1 or ship-ready idle.

### 12.3 Rollback

| Condition | Action |
|-----------|--------|
| F1 ≠ SoT | Replace F1; stop |
| Bad alpha | Reject F2–F4 |
| Loop pop | Tune F3/F4; no blur cheat |
| Minimum ship | Static F1 only |

### 12.4 Production risks

Low vertical margin; F4 must stay distinct; tight mesh bounds jitter.

---

*Chờ lead trước sản xuất.*

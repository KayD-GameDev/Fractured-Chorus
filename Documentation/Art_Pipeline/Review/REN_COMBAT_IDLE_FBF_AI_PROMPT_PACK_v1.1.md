# Ren — Combat Idle FBF AI Prompt Pack v1.1

**Ngày:** 2026-11-09 · **Restricted AI feasibility pilot only** — not full Combat Idle FBF.

**Supersedes:** [`REN_COMBAT_IDLE_FBF_AI_PRODUCTION_PROMPTS.md`](REN_COMBAT_IDLE_FBF_AI_PRODUCTION_PROMPTS.md) (v1.0, historical).

**Hierarchy:** CHARACTER_LOCK → [Brief](REN_COMBAT_IDLE_ANIMATION_BRIEF.md) → [Production Spec](REN_COMBAT_IDLE_FBF_PRODUCTION_SPEC.md) → **this pack**.

---

## 1. Pilot Rules

| Rule | Value |
|------|-------|
| F1 | Byte copy SoT `.../ren_combat_battle_idle_semichibi_v2_full.png` |
| Source for F2–F4 | **F1 only** — never generated F2/F3 |
| Mask | Interior upper-torso + permitted shoulder fabric |
| Pixel-lock | Hands, rifle, face, hair, SyncPod, lower body, legs, feet |
| Motion | In-mask fabric only — no silhouette / whole-body shift |
| Pass pilot | **Does not** approve full pipeline (Spec **§8**, **§12.2**) |

Full-production motion (Brief §4) is **out of scope** for §2–§3 prompts.

---

## 2. Global Prompt

Attach with every F2–F4 call:

```
Use only the supplied F1 image as reference. Preserve CHARACTER LOCK identity, style, colors, uniform, silhouette, and equipment. Do not redesign the character.

Pixel-lock hands and Violin Rifle to F1. Edit only inside the provided mask: interior upper-torso and permitted shoulder fabric. Do not cross mask boundaries.

Output one 864×1152 RGBA frame, transparent background, binary alpha intent, no multi-pose sheet, no labels, no headphones, no BG/FX/blur.
```

---

## 3. Frame Prompts

Append one line after §2 (F1 + mask attached):

| Frame | Line |
|-------|------|
| **F2** | Early inhale: subtle in-mask fabric folds and tension — weaker than F3. |
| **F3** | Inhale peak: strongest in-mask fabric change in the cycle — no silhouette change. |
| **F4** | Exhale: in-mask fabric nearer F1 than F3 — distinct frame, not a duplicate of F1. |

---

## 4. Output

**864 × 1152**, transparent **RGBA**, aligned with F1. Do not fill transparency with solids.

---

## 5. Pixel Restoration

1. Restore F1 RGB outside approved mask.  
2. Restore F1 alpha (0/255).  
3. Keep canvas size and alignment.  
4. Verify outside-mask equality (e.g. Python).  
5. Reject locked-region drift.  
6. Inspect mask seams.

---

## 6. Assembly Order

1. Global Prompt (§2)  
2. One Frame Prompt (§3)  
3. Attachments: image = F1; mask = interior torso fabric  
4. Run §5  
5. Pilot QC (§7)

---

## 7. Pilot QC

Apply Production Spec **§10** after §5.

Full production acceptance is Spec **§12.1** — not met by this pilot alone.

---

*Chờ lead trước sản xuất.*

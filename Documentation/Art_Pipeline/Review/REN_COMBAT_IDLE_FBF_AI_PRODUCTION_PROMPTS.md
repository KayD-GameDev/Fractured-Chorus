# Ren — Combat Idle FBF AI Production Prompts

**Loại:** prompt pack cho chỉnh frame bằng AI — bám Production Spec.  
**Trạng thái:** **SUPERSEDED** — không dùng cho sản xuất. **Active:** [`REN_COMBAT_IDLE_FBF_AI_PROMPT_PACK_v1.1.md`](REN_COMBAT_IDLE_FBF_AI_PROMPT_PACK_v1.1.md) (restricted AI pilot; masked edit + pixel lock).  
**Ngày:** 2026-11-09 (v1.0) · v1.1 = 2026-11-09  

**Lịch sử:** v1.0 thiếu mask boundaries, pilot scope và post-gen restoration; mâu thuẫn rigid-move vs pixel-lock đã xử lý trong v1.1.  

**F1 (không dùng AI):** sao chép bytes từ  
`Assets/FracturedChorus/Art/Characters/Ren/Battle/ren_combat_battle_idle_semichibi_v2_full.png`  
→ đặt tên theo spec; hash phải trùng SoT.

**Tham chiếu:** [`REN_COMBAT_IDLE_FBF_PRODUCTION_SPEC.md`](REN_COMBAT_IDLE_FBF_PRODUCTION_SPEC.md) · [`CHARACTER_LOCK.md`](../../../Assets/FracturedChorus/Art/Characters/Ren/CHARACTER_LOCK.md)

**Input ảnh khi gọi model:** luôn đính kèm **F1 = SoT** cho F2 / F3 / F4. **F3 và F4 không dùng ảnh F2/F3 đã gen làm nguồn** (chỉ F1).

**Sau gen:** Difference Map QC + alpha binary (0/255); sửa tay DCC nếu model phá rigid tay–súng.

---

## Global Character Lock — apply to F2, F3, F4

Use the supplied original image as the absolute and exclusive character reference.

Preserve Ren's exact identity, semichibi proportions, original anime art style, linework, colors, shading, clothing details, silhouette and equipment.

Character details that must remain unchanged:

- Messy layered black hair.
- Grey eyes and original facial expression.
- Original uniform and all clothing details.
- Violin Rifle Basic sniper weapon, including its original sound-hole/clef design, four purple strings and glow.
- Original hand positions and weapon grip.
- SyncPod disc, blue wave details and thin connecting wire.
- No combat headphones.

Preserve the original transparent background, canvas composition, character scale and grounding position.

Do not redesign, reinterpret, beautify, simplify or replace any character detail.

This is a subtle animation-frame edit, not a new character illustration.

---

## F2 — Early Inhale

Create one animation frame: Ren Combat Idle — Early Inhale (F2).

Use the supplied original F1 artwork as the visual source.

Introduce a very subtle breathing movement:

- Slight upward relaxation and lift of both shoulders.
- Very subtle expansion of the upper chest and blazer.
- Keep the movement natural and almost unnoticeable.
- Target approximately 1–2 pixels of displacement at the final 864 × 1152 resolution.
- Preserve the original head position, expression, hands, weapon, legs and feet.

Do not move the character as a whole.

The hands and Violin Rifle must remain a rigid, unchanged group.

Do not alter the weapon design, hair silhouette, face, eyes, SyncPod or uniform identity.

Output exactly one full-frame image. No contact sheet, no multiple poses, no text or labels.

---

## F3 — Inhale Peak

Create one animation frame: Ren Combat Idle — Inhale Peak (F3).

Use the original F1 artwork as the visual source, not the generated F2.

Show the peak of a very subtle breathing cycle:

- The upper chest reaches its maximum natural expansion.
- Both shoulders lift slightly more than in F2.
- The blazer and upper torso show a minimal breathing adjustment.
- Keep all changes restrained and consistent with the original artwork.
- Do not exaggerate chest expansion.
- Do not scale or move the entire character.

Preserve the exact face, eyes, hairstyle, clothing identity, hands, weapon, SyncPod, leg position and feet.

The hands and Violin Rifle must remain unchanged and rigid.

Output exactly one full-frame image. No additional poses, effects, text or labels.

---

## F4 — Exhale

Create one animation frame: Ren Combat Idle — Exhale (F4).

Use the original F1 artwork as the visual source, not generated F2 or F3.

Show Ren naturally relaxing after the inhale:

- The shoulders gently settle.
- The chest returns close to its original resting position.
- The upper torso becomes visually closer to F1 than F3.
- Maintain a subtle but genuine hand-drawn pose difference from F1.
- The difference must look natural, not artificially introduced.

Preserve the exact face, eyes, hairstyle, outfit, hands, weapon, SyncPod, legs and feet.

Do not blend or blur frames to simulate movement.

The hands and Violin Rifle remain rigid and unchanged.

Output exactly one full-frame image. No contact sheet, extra pose, text or label.

---

## Negative instructions — apply to all frames

Do not:

- Redesign the character or weapon.
- Change face, eye shape, expression or hairstyle.
- Change clothing colors, accessories or proportions.
- Change the number or shape of weapon strings.
- Alter hand positions or weapon grip.
- Add combat headphones.
- Crop, rotate, zoom or reposition the character.
- Add background, shadows, particles or new visual effects.
- Add motion blur, glow blur or artificial interpolation.
- Create a character sheet or multiple frames in one image.

**Final target:** 864 × 1152 pixels, transparent RGBA PNG, consistent with the original F1 artwork.

---

## Assembly order (for operators)

When submitting to an image model, concatenate in this order unless the tool has a dedicated “negative” field:

1. **Global Character Lock** (block above)  
2. **Frame-specific block** (F2 / F3 / F4)  
3. **Negative instructions** (block above)

**Attachment:** single image input = **SoT / F1** only.

---

*Prompt pack — chờ lead xác nhận trước khi chạy sản xuất hàng loạt.*

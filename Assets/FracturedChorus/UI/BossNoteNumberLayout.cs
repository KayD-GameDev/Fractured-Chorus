using System;
using UnityEngine;

namespace FracturedChorus.UI
{
    [Serializable]
    public class BossNoteNumberLayout
    {
        [Tooltip("Scale glyph nốt đơn (nhân NoteDisplaySize).")]
        [Range(1.2f, 3.5f)] public float singleScale = 2.15f;

        [Tooltip("Scale chiều cao nốt đôi.")]
        [Range(1.2f, 3.5f)] public float beamedHeightScale = 2f;

        [Tooltip("Cỡ số = headSize × hệ số này.")]
        [Range(0.2f, 0.55f)] public float numberSizeFactor = 0.36f;

        [Tooltip("✓ so với ô số (1 = bằng ô NoteNum).")]
        [Range(1f, 2f)] public float perfectMarkScaleVsNumber = 1.35f;

        [Tooltip("Nhân thêm khi hold / drop preview (≤1.15).")]
        [Range(1f, 1.2f)] public float perfectPreviewScale = 1.1f;

        [Tooltip("Floor size ✓ (px). Neighbor cap thắng nếu gap hẹp.")]
        public float perfectMarkMinPx = 36f;

        [Tooltip("Max đường kính ✓ = fill × min khoảng beat kề.")]
        [Range(0.45f, 0.9f)] public float perfectNeighborFill = 0.72f;

        [Header("Số — offset px (cộng thêm sau neo đầu nốt)")]
        [Tooltip("Nudge tất cả nốt đơn (px). +X phải, +Y lên.")]
        public Vector2 numberNudgeSingle = Vector2.zero;

        [Tooltip("Nudge cả hai đầu nốt đôi (px).")]
        public Vector2 numberNudgeBeamed = new Vector2(-4f, 6f);

        [Tooltip("Nudge thêm đầu trái nốt đôi (px).")]
        public Vector2 numberNudgeBeamedLeft = Vector2.zero;

        [Tooltip("Nudge thêm đầu phải nốt đôi (px).")]
        public Vector2 numberNudgeBeamedRight = Vector2.zero;

        [Tooltip("Offset thêm theo variant 0..4 (px).")]
        public Vector2[] variantNudges = new Vector2[5];

        [Header("Neo đầu nốt trong sprite (0–1, gốc giữa ảnh)")]
        [Tooltip("Stem-up (v0,v1,v2,v4): tâm vòng đầu nốt (−Y = xuống).")]
        public Vector2 singleHeadNormStemUp = new Vector2(-0.08f, -0.30f);

        [Tooltip("Stem-down (v3).")]
        public Vector2 singleHeadNormStemDown = new Vector2(0.06f, 0.28f);

        [Tooltip("Beamed: đầu trái trong sprite.")]
        public Vector2 beamedHeadNormLeft = new Vector2(-0.32f, -0.10f);

        [Tooltip("Beamed: đầu phải trong sprite.")]
        public Vector2 beamedHeadNormRight = new Vector2(0.28f, -0.10f);

        public Vector2 ResolveSingleHeadNorm(int variantIndex) =>
            variantIndex == 3 ? singleHeadNormStemDown : singleHeadNormStemUp;

        public Vector2 ResolveVariantNudge(int variantIndex)
        {
            if (variantNudges == null || variantIndex < 0 || variantIndex >= variantNudges.Length)
            {
                return Vector2.zero;
            }

            return variantNudges[variantIndex];
        }
    }
}

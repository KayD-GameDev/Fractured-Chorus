using System;
using UnityEngine;

namespace FracturedChorus.UI
{
    [Serializable]
    public class LeftRailLayout
    {
        [Tooltip("Khi bật: không ghi đè RectTransform đã chỉnh tay trên Scene.")]
        public bool preserveSceneRects = true;

        [Tooltip("Alpha khóa sol (Image).")]
        [Range(0.15f, 1f)] public float clefAlpha = 0.62f;

        [Tooltip("Alpha nền LeftRail.")]
        [Range(0.35f, 1f)] public float backgroundAlpha = 1f;

        [Tooltip("Size khóa sol (px) — chỉ áp khi preserveSceneRects = false.")]
        public Vector2 clefSize = new Vector2(120f, 160f);

        [Tooltip("Vị trí khóa sol trong Header (local) — chỉ khi preserveSceneRects = false.")]
        public Vector2 clefAnchoredPosition = new Vector2(105.41f, -47.6f);

        [Tooltip("Độ rộng cột avatar gutter (px). Display target ≈ 72.")]
        public float avatarGutterWidth = 72f;

        [Tooltip("Offset X fallback khi forceAvatarLayout = false. Khi force = true: tự = Viewport.left − gutterW (cột nằm trong gap, không đè beat track).")]
        public float avatarGutterOffsetX = 139f;

        [Tooltip("Size ô avatar (px). Gốc scene = 40; đề xuất 48–56.")]
        public float avatarSlotSize = 48f;

        [Tooltip("Alpha nền cột avatar.")]
        [Range(0.35f, 1f)] public float avatarColumnBackgroundAlpha = 1f;

        [Tooltip("Khi false: giữ RectTransform scene (kéo tay trong Editor). Khi true: ép X = Viewport.left − gutterW.")]
        public bool forceAvatarLayout = false;
    }
}

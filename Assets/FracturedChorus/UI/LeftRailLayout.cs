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

        [Tooltip("Độ rộng cột avatar gutter (px) — chỉ khi không preserve scene.")]
        public float avatarGutterWidth = 72f;

        [Tooltip("Offset X gutter — chỉ khi không preserve scene / forceAvatarLayout = false.")]
        public float avatarGutterOffsetX = 139f;

        [Tooltip("Size ô avatar (px) — fallback khi scene chưa có LaneAvatar_*. Ưu tiên size Hierarchy.")]
        public float avatarSlotSize = 42f;

        [Tooltip("Alpha nền cột avatar.")]
        [Range(0.35f, 1f)] public float avatarColumnBackgroundAlpha = 1f;

        [Tooltip("Khi true và không preserve: ép gutter flush Viewport. Mặc định false — lấy Rect trên Scene.")]
        public bool forceAvatarLayout = false;
    }
}

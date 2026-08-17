using FracturedChorus.RunMap.Core;
using UnityEngine;

namespace FracturedChorus.Data
{
    [CreateAssetMenu(fileName = "RunMapLayoutConfig", menuName = "Fractured Chorus/Run Map Layout")]
    public sealed class RunMapLayoutConfigSO : ScriptableObject
    {
        [Header("Grid spacing")]
        [SerializeField] private float nodeSpacingX = MapLayoutConstants.NodeSpacingX;
        [SerializeField] private float nodeSpacingY = MapLayoutConstants.NodeSpacingY;
        [SerializeField] private float nodeDiameter = MapLayoutConstants.NodeDiameter;
        [SerializeField] private float bossNodeDiameter = MapLayoutConstants.BossNodeDiameter;
        [SerializeField] private float bossYOffset = MapLayoutConstants.BossYOffset;

        [Header("Start / content padding")]
        [SerializeField] private float contentPaddingBottom = MapLayoutConstants.ContentPaddingBottom;
        [SerializeField] private float contentPaddingTop = MapLayoutConstants.ContentPaddingTop;
        [SerializeField] private float startBottomInset = MapLayoutConstants.StartBottomInset;
        [SerializeField] private float startToF1GapScale = MapLayoutConstants.StartToF1GapScale;
        [SerializeField] private float startNodeScale = MapLayoutConstants.StartNodeScale;
        [SerializeField] private float viewportBottomGutter = MapLayoutConstants.ViewportBottomGutter;

        [Header("Viewport auto-fit")]
        [SerializeField] private bool allowViewportFit = true;
        [SerializeField] private float fitWidthScale = 0.94f;
        [SerializeField] private float labelGutter = 56f;
        [SerializeField] private float minSpacingX = 104f;
        [SerializeField] private float maxSpacingX = 168f;
        [SerializeField] private float minSpacingY = 88f;
        [SerializeField] private float maxSpacingY = 128f;
        [SerializeField] private float minNodeDiameter = 72f;
        [SerializeField] private float maxNodeDiameter = 104f;
        [SerializeField] private float spacingYViewportDivisor = 4.6f;
        [SerializeField] private float nodeDiameterSpacingRatio = 0.62f;
        [SerializeField] private float bossDiameterScale = 1.38f;
        [SerializeField] private float minBossDiameter = 96f;
        [SerializeField] private float maxBossDiameter = 140f;

        [Header("Scene preview")]
        [SerializeField] private bool showLayoutPreviewInScene = true;
        [SerializeField] private int previewFloorCount = 4;
        [SerializeField] [Range(0.25f, 1f)] private float previewAlpha = 0.55f;

        public float NodeSpacingX => nodeSpacingX;
        public float NodeSpacingY => nodeSpacingY;
        public float NodeDiameter => nodeDiameter;
        public float BossNodeDiameter => bossNodeDiameter;
        public float BossYOffset => bossYOffset;
        public float ContentPaddingBottom => contentPaddingBottom;
        public float ContentPaddingTop => contentPaddingTop;
        public float StartBottomInset => startBottomInset;
        public float StartToF1GapScale => startToF1GapScale;
        public float StartNodeScale => startNodeScale;
        public float ViewportBottomGutter => viewportBottomGutter;
        public bool AllowViewportFit => allowViewportFit;
        public float FitWidthScale => fitWidthScale;
        public float LabelGutter => labelGutter;
        public float MinSpacingX => minSpacingX;
        public float MaxSpacingX => maxSpacingX;
        public float MinSpacingY => minSpacingY;
        public float MaxSpacingY => maxSpacingY;
        public float MinNodeDiameter => minNodeDiameter;
        public float MaxNodeDiameter => maxNodeDiameter;
        public float SpacingYViewportDivisor => spacingYViewportDivisor;
        public float NodeDiameterSpacingRatio => nodeDiameterSpacingRatio;
        public float BossDiameterScale => bossDiameterScale;
        public float MinBossDiameter => minBossDiameter;
        public float MaxBossDiameter => maxBossDiameter;
        public bool ShowLayoutPreviewInScene => showLayoutPreviewInScene;
        public int PreviewFloorCount => Mathf.Max(1, previewFloorCount);
        public float PreviewAlpha => previewAlpha;

        public void ResetToDefaults()
        {
            nodeSpacingX = MapLayoutConstants.NodeSpacingX;
            nodeSpacingY = MapLayoutConstants.NodeSpacingY;
            nodeDiameter = MapLayoutConstants.NodeDiameter;
            bossNodeDiameter = MapLayoutConstants.BossNodeDiameter;
            bossYOffset = MapLayoutConstants.BossYOffset;
            contentPaddingBottom = MapLayoutConstants.ContentPaddingBottom;
            contentPaddingTop = MapLayoutConstants.ContentPaddingTop;
            startBottomInset = MapLayoutConstants.StartBottomInset;
            startToF1GapScale = MapLayoutConstants.StartToF1GapScale;
            startNodeScale = MapLayoutConstants.StartNodeScale;
            viewportBottomGutter = MapLayoutConstants.ViewportBottomGutter;
            showLayoutPreviewInScene = true;
            previewFloorCount = 4;
            previewAlpha = 0.55f;
        }
    }
}

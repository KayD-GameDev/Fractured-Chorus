using UnityEngine;

namespace FracturedChorus.UI
{
    [CreateAssetMenu(fileName = "ResonanceDiveMotion", menuName = "Fractured Chorus/Resonance Dive Motion")]
    public sealed class ResonanceDiveMotion : ScriptableObject
    {
        [SerializeField] private float glowCycleSeconds = 2.4f;
        [SerializeField] private float hoverBlendSeconds = 0.15f;
        [SerializeField] private float pressedBlendSeconds = 0.08f;
        [SerializeField] private float scanlineSeconds = 0.45f;
        [SerializeField] private float visualHoverScale = 1.03f;
        [SerializeField] private float visualPressedScale = 0.97f;
        [SerializeField] private float pressedNudgeY = -6f;
        [SerializeField] private float decoLeftSwayDegrees = 3f;
        [SerializeField] private float decoRightHoverX = 8f;
        [SerializeField] private float shadowHoverScale = 1.04f;
        [SerializeField] private float shadowPressedScale = 0.9f;
        [SerializeField] private float particleDrift = 6f;
        [SerializeField] private float wavePulseMin = 0.35f;
        [SerializeField] private float wavePulseMax = 0.85f;
        [SerializeField] private float glowIdleMin = 0.45f;
        [SerializeField] private float glowIdleMax = 0.75f;
        [SerializeField] private float glowHoverMin = 0.75f;
        [SerializeField] private float glowHoverMax = 1f;
        [SerializeField] private float borderIdleMin = 0.7f;
        [SerializeField] private float borderIdleMax = 0.9f;
        [SerializeField] private float borderHoverMin = 0.9f;
        [SerializeField] private float borderHoverMax = 1f;
        [SerializeField] private float gradientIdleMin = 0.35f;
        [SerializeField] private float gradientIdleMax = 0.55f;
        [SerializeField] private float gradientHoverMin = 0.55f;
        [SerializeField] private float gradientHoverMax = 0.8f;
        [SerializeField] private Vector2 decoLeftPivot = new Vector2(0.281f, 0.499f);

        public float GlowCycleSeconds => Mathf.Max(0.01f, glowCycleSeconds);
        public float HoverBlendSeconds => Mathf.Max(0.01f, hoverBlendSeconds);
        public float PressedBlendSeconds => Mathf.Max(0.01f, pressedBlendSeconds);
        public float ScanlineSeconds => Mathf.Max(0.01f, scanlineSeconds);
        public float VisualHoverScale => visualHoverScale;
        public float VisualPressedScale => visualPressedScale;
        public float PressedNudgeY => pressedNudgeY;
        public float DecoLeftSwayDegrees => decoLeftSwayDegrees;
        public float DecoRightHoverX => decoRightHoverX;
        public float ShadowHoverScale => shadowHoverScale;
        public float ShadowPressedScale => shadowPressedScale;
        public float ParticleDrift => particleDrift;
        public float WavePulseMin => wavePulseMin;
        public float WavePulseMax => wavePulseMax;
        public float GlowIdleMin => glowIdleMin;
        public float GlowIdleMax => glowIdleMax;
        public float GlowHoverMin => glowHoverMin;
        public float GlowHoverMax => glowHoverMax;
        public float BorderIdleMin => borderIdleMin;
        public float BorderIdleMax => borderIdleMax;
        public float BorderHoverMin => borderHoverMin;
        public float BorderHoverMax => borderHoverMax;
        public float GradientIdleMin => gradientIdleMin;
        public float GradientIdleMax => gradientIdleMax;
        public float GradientHoverMin => gradientHoverMin;
        public float GradientHoverMax => gradientHoverMax;
        public Vector2 DecoLeftPivot => decoLeftPivot;
    }
}

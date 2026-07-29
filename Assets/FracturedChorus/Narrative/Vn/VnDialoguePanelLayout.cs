using UnityEngine;

namespace FracturedChorus.Narrative.Vn
{
    public static class VnDialoguePanelLayout
    {
        public static readonly Vector2 DialoguePanelAnchorMin = new Vector2(0.04f, 0.03f);
        public static readonly Vector2 DialoguePanelAnchorMax = new Vector2(0.96f, 0.36f);

        public static readonly Vector2 NameplateAnchorMin = new Vector2(0.018f, 0.90f);
        public static readonly Vector2 NameplateAnchorMax = new Vector2(0.195f, 1.06f);
        public const int NameplateFontSize = 26;

        public static readonly Vector2 BodyAnchorMin = new Vector2(0.07f, 0.12f);
        public static readonly Vector2 BodyAnchorMax = new Vector2(0.93f, 0.74f);
        public const int BodyFontSize = 30;

        public static readonly Vector2 BodyBackingAnchorMin = new Vector2(0.04f, 0.08f);
        public static readonly Vector2 BodyBackingAnchorMax = new Vector2(0.96f, 0.78f);
        public static readonly Color BodyBackingColor = new Color(0.02f, 0.05f, 0.12f, 0.62f);

        public static readonly Vector2 TextCardBodyAnchorMin = new Vector2(0.15f, 0.35f);
        public static readonly Vector2 TextCardBodyAnchorMax = new Vector2(0.85f, 0.65f);
        public const int TextCardFontSize = 40;
        public static readonly Color TextCardDimColor = new Color(0f, 0f, 0f, 0.78f);

        public static readonly Vector2 ChoicePanelAnchorMin = new Vector2(0.52f, 0.22f);
        public static readonly Vector2 ChoicePanelAnchorMax = new Vector2(0.96f, 0.78f);

        public static readonly Vector2 DateHudAnchorMin = new Vector2(0.62f, 1f);
        public static readonly Vector2 DateHudAnchorMax = new Vector2(1f, 1f);
        public static readonly Vector2 DateHudPivot = new Vector2(1f, 1f);
        public static readonly Vector2 DateHudOffsetMin = new Vector2(0f, -150f);
        public static readonly Vector2 DateHudOffsetMax = Vector2.zero;

        public static readonly Vector2 DateLabelAnchorMin = new Vector2(0.08f, 0.45f);
        public static readonly Vector2 DateLabelAnchorMax = new Vector2(0.78f, 0.95f);
        public static readonly Vector2 DateLabelOffsetMin = new Vector2(-13f, -16f);
        public static readonly Vector2 DateLabelOffsetMax = new Vector2(-297.6024f, 45f);
        public const int DateLabelFontSize = 70;

        public static readonly Vector2 PhaseIconAnchorMin = new Vector2(0.8f, 0.35f);
        public static readonly Vector2 PhaseIconAnchorMax = new Vector2(0.96f, 0.9f);
        public static readonly Vector2 PhaseIconOffsetMin = new Vector2(-104.06128f, -22.5f);
        public static readonly Vector2 PhaseIconOffsetMax = new Vector2(91.8775f, 60f);

        public static readonly Vector2 PhaseLabelAnchorMin = new Vector2(0.08f, 0.15f);
        public static readonly Vector2 PhaseLabelAnchorMax = new Vector2(0.78f, 0.5f);
        public static readonly Vector2 PhaseLabelOffsetMin = new Vector2(-32f, -12f);
        public static readonly Vector2 PhaseLabelOffsetMax = new Vector2(-251.4571f, 0f);
        public const int PhaseLabelFontSize = 35;

        public static readonly Color NameplateTextColor = Color.white;
        public static readonly Color BodyTextColor = new Color(0.98f, 1f, 1f, 1f);
        public static readonly Color TextCardBodyColor = new Color(0.95f, 0.98f, 1f, 1f);
        public static readonly Color PhaseLabelColor = new Color(0.55f, 0.9f, 0.95f, 1f);

        public static readonly Color TextShadowColor = new Color(0f, 0f, 0f, 0.88f);
        public static readonly Vector2 TextShadowDistance = new Vector2(1.5f, -1.5f);
        public static readonly Color TextOutlineColor = new Color(0.02f, 0.04f, 0.1f, 0.95f);
        public static readonly Vector2 TextOutlineDistance = new Vector2(2f, -2f);
    }
}

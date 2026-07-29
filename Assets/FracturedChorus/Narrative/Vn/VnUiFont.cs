using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Narrative.Vn
{
    public static class VnUiFont
    {
        private const string RegularPath = "Fonts/VnDialogue";
        private const string BoldPath = "Fonts/VnDialogueBold";
        private const string ItalicPath = "Fonts/VnDialogueItalic";

        private static Font s_regular;
        private static Font s_bold;
        private static Font s_italic;

        public static Font Resolve(FontStyle style = FontStyle.Normal)
        {
            switch (style)
            {
                case FontStyle.Bold:
                case FontStyle.BoldAndItalic:
                    return ResolveBold();
                case FontStyle.Italic:
                    return ResolveItalic();
                default:
                    return ResolveRegular();
            }
        }

        public static Font ResolveRegular()
        {
            if (s_regular != null)
            {
                return s_regular;
            }

            s_regular = Resources.Load<Font>(RegularPath);
            if (s_regular != null)
            {
                return s_regular;
            }

            s_regular = ResolveLegacyFallback();
            return s_regular;
        }

        private static Font ResolveBold()
        {
            if (s_bold != null)
            {
                return s_bold;
            }

            s_bold = Resources.Load<Font>(BoldPath);
            if (s_bold != null)
            {
                return s_bold;
            }

            s_bold = ResolveRegular();
            return s_bold;
        }

        private static Font ResolveItalic()
        {
            if (s_italic != null)
            {
                return s_italic;
            }

            s_italic = Resources.Load<Font>(ItalicPath);
            if (s_italic != null)
            {
                return s_italic;
            }

            s_italic = ResolveRegular();
            return s_italic;
        }

        private static Font ResolveLegacyFallback()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font != null)
            {
                return font;
            }

            font = Font.CreateDynamicFontFromOSFont(
                new[] { "Montserrat", "Segoe UI", "Arial", "Helvetica Neue", "Yu Gothic UI", "Meiryo UI" },
                32);

            if (font == null)
            {
                Debug.LogError("[VnUiFont] No usable UI font found.");
            }

            return font;
        }

        public static void Apply(Text text, int fontSize = -1, FontStyle? style = null)
        {
            if (text == null)
            {
                return;
            }

            var requestedStyle = style ?? FontStyle.Normal;
            var font = Resolve(requestedStyle);
            if (font != null)
            {
                text.font = font;
            }

            text.fontStyle = UsesDedicatedAsset(requestedStyle, font)
                ? FontStyle.Normal
                : requestedStyle;

            if (fontSize > 0)
            {
                text.fontSize = fontSize;
            }

            text.supportRichText = false;
            text.resizeTextForBestFit = false;
            text.alignByGeometry = false;
        }

        public static void ApplyAssetOnly(Text text, FontStyle style = FontStyle.Normal)
        {
            Apply(text, style: style);
        }

        public static void ApplyReadableEffectsOnly(Text text)
        {
            if (text == null)
            {
                return;
            }

            ApplyAssetOnly(text, text.fontStyle);
            EnsureReadableEffects(text, outline: true, shadow: true);
        }

        public static void ApplyReadableNameplate(Text text)
        {
            Apply(text, VnDialoguePanelLayout.NameplateFontSize, FontStyle.Bold);
            if (text == null)
            {
                return;
            }

            text.color = VnDialoguePanelLayout.NameplateTextColor;
            text.alignment = TextAnchor.MiddleCenter;
            EnsureReadableEffects(text, outline: true, shadow: true);
        }

        public static void ApplyReadableBody(Text text, int fontSize = -1)
        {
            var size = fontSize > 0 ? fontSize : VnDialoguePanelLayout.BodyFontSize;
            Apply(text, size, FontStyle.Normal);
            if (text == null)
            {
                return;
            }

            text.color = fontSize > 0 && fontSize >= VnDialoguePanelLayout.TextCardFontSize
                ? VnDialoguePanelLayout.TextCardBodyColor
                : VnDialoguePanelLayout.BodyTextColor;
            EnsureReadableEffects(text, outline: true, shadow: true);
        }

        private static bool UsesDedicatedAsset(FontStyle style, Font font)
        {
            if (font == null)
            {
                return false;
            }

            if (style == FontStyle.Bold || style == FontStyle.BoldAndItalic)
            {
                return font == s_bold;
            }

            if (style == FontStyle.Italic)
            {
                return font == s_italic;
            }

            return false;
        }

        private static void EnsureReadableEffects(Text text, bool outline, bool shadow)
        {
            if (outline)
            {
                var outlineFx = text.GetComponent<Outline>();
                if (outlineFx == null)
                {
                    outlineFx = text.gameObject.AddComponent<Outline>();
                }

                outlineFx.effectColor = VnDialoguePanelLayout.TextOutlineColor;
                outlineFx.effectDistance = VnDialoguePanelLayout.TextOutlineDistance;
                outlineFx.useGraphicAlpha = true;
            }

            if (shadow)
            {
                var shadowFx = text.GetComponent<Shadow>();
                if (shadowFx == null)
                {
                    shadowFx = text.gameObject.AddComponent<Shadow>();
                }

                shadowFx.effectColor = VnDialoguePanelLayout.TextShadowColor;
                shadowFx.effectDistance = VnDialoguePanelLayout.TextShadowDistance;
                shadowFx.useGraphicAlpha = true;
            }
        }
    }
}

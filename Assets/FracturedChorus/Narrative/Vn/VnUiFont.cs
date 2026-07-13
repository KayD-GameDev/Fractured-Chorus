using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Narrative.Vn
{
    public static class VnUiFont
    {
        private const string ResourcePath = "Fonts/VnDialogue";
        private static Font s_cached;

        public static Font Resolve()
        {
            if (s_cached != null)
            {
                return s_cached;
            }

            s_cached = Resources.Load<Font>(ResourcePath);
            if (s_cached != null)
            {
                return s_cached;
            }

            s_cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (s_cached != null)
            {
                return s_cached;
            }

            s_cached = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (s_cached != null)
            {
                return s_cached;
            }

            s_cached = Font.CreateDynamicFontFromOSFont(
                new[] { "Segoe UI", "Arial", "Helvetica Neue", "Yu Gothic UI", "Meiryo UI" },
                32);

            if (s_cached == null)
            {
                Debug.LogError("[VnUiFont] No usable UI font found.");
            }

            return s_cached;
        }

        public static void Apply(Text text, int fontSize = -1, FontStyle? style = null)
        {
            if (text == null)
            {
                return;
            }

            var font = Resolve();
            if (font != null)
            {
                text.font = font;
            }

            if (fontSize > 0)
            {
                text.fontSize = fontSize;
            }

            if (style.HasValue)
            {
                text.fontStyle = style.Value;
            }

            text.supportRichText = false;
            text.resizeTextForBestFit = false;
            text.alignByGeometry = false;
        }

        public static void ApplyAssetOnly(Text text)
        {
            Apply(text);
        }
    }
}

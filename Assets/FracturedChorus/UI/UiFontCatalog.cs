using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public enum UiFontRole
    {
        Body,
        Display,
        DisplaySecondary,
        Dialogue,
    }

    public static class UiFontCatalog
    {
        private const string DialogueResourcePath = "Fonts/VnDialogue";
        private const string DisplayBoldItalicPath = "Fonts/FcDisplayBoldItalic";
        private const string DisplaySemiBoldItalicPath = "Fonts/FcDisplaySemiBoldItalic";

        private static Font s_body;
        private static Font s_display;
        private static Font s_displaySecondary;
        private static Font s_dialogue;

        public static Font Body => ResolveMontserrat(ref s_body);
        public static Font Dialogue => ResolveMontserrat(ref s_dialogue);
        public static Font Display => ResolveDisplay(ref s_display, DisplayBoldItalicPath);
        public static Font DisplaySecondary => ResolveDisplay(ref s_displaySecondary, DisplaySemiBoldItalicPath);

        public static Font ForRole(UiFontRole role)
        {
            switch (role)
            {
                case UiFontRole.Display:
                    return Display;
                case UiFontRole.DisplaySecondary:
                    return DisplaySecondary;
                case UiFontRole.Dialogue:
                    return Dialogue;
                default:
                    return Body;
            }
        }

        public static void ApplyAutomatic(Text text)
        {
            if (text == null)
            {
                return;
            }

            var role = UiFontRules.Resolve(text);
            var size = text.fontSize > 0 ? text.fontSize : -1;
            var style = UiFontRules.ResolveStyle(text, role);
            Apply(text, role, size, style);
        }

        public static void ApplyHierarchy(Transform root, bool includeInactive = true)
        {
            if (root == null)
            {
                return;
            }

            var texts = root.GetComponentsInChildren<Text>(includeInactive);
            for (var i = 0; i < texts.Length; i++)
            {
                ApplyAutomatic(texts[i]);
            }

            ApplyKnownComponents(root, includeInactive);
        }

        private static void ApplyKnownComponents(Transform root, bool includeInactive)
        {
            foreach (var dateHud in root.GetComponentsInChildren<FracturedChorus.Narrative.Vn.VnStoryDateHud>(includeInactive))
            {
                dateHud.ApplyFonts();
            }

            foreach (var slashBanner in root.GetComponentsInChildren<FracturedChorus.Hub.CalendarSlashBanner>(includeInactive))
            {
                slashBanner.ApplyFonts();
            }

            foreach (var runtime in root.GetComponentsInChildren<FracturedChorus.Narrative.Vn.VnRuntimeController>(includeInactive))
            {
                FracturedChorus.Narrative.Vn.VnRuntimeUiLayoutApplier.ApplyReadability(runtime);
            }
        }

        public static void Apply(Text text, UiFontRole role = UiFontRole.Body, int fontSize = -1, FontStyle? style = null)
        {
            if (text == null)
            {
                return;
            }

            switch (role)
            {
                case UiFontRole.Display:
                case UiFontRole.DisplaySecondary:
                    ApplyDisplayAsset(text, ForRole(role), fontSize);
                    return;
                case UiFontRole.Dialogue:
                    FracturedChorus.Narrative.Vn.VnUiFont.Apply(text, fontSize, style ?? FontStyle.Normal);
                    return;
                default:
                    FracturedChorus.Narrative.Vn.VnUiFont.Apply(text, fontSize, style ?? FontStyle.Normal);
                    return;
            }
        }

        public static Font ResolveLegacyFallback()
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

            return Font.CreateDynamicFontFromOSFont(
                new[] { "Arial", "Segoe UI", "Helvetica Neue", "Yu Gothic UI", "Meiryo UI" },
                32);
        }

        private static Font ResolveMontserrat(ref Font cache)
        {
            if (cache != null)
            {
                return cache;
            }

            cache = Resources.Load<Font>(DialogueResourcePath);
            if (cache != null)
            {
                return cache;
            }

            cache = ResolveLegacyFallback();
            if (cache == null)
            {
                Debug.LogError("[UiFontCatalog] No usable Montserrat/body font found.");
            }

            return cache;
        }

        private static Font ResolveDisplay(ref Font cache, string resourcePath)
        {
            if (cache != null)
            {
                return cache;
            }

            cache = Resources.Load<Font>(resourcePath);
            if (cache != null)
            {
                return cache;
            }

            cache = ResolveLegacyFallback();
            if (cache == null)
            {
                Debug.LogError($"[UiFontCatalog] No usable display font for '{resourcePath}'.");
            }

            return cache;
        }

        private static void ApplyDisplayAsset(Text text, Font font, int fontSize)
        {
            if (font != null)
            {
                text.font = font;
            }

            text.fontStyle = FontStyle.Normal;
            if (fontSize > 0)
            {
                text.fontSize = fontSize;
            }

            text.supportRichText = false;
            text.resizeTextForBestFit = false;
            text.alignByGeometry = false;
        }
    }
}

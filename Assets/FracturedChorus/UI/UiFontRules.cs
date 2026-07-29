using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public static class UiFontRules
    {
        public static UiFontRole Resolve(Text text)
        {
            if (text == null)
            {
                return UiFontRole.Body;
            }

            var name = text.gameObject.name;
            if (IsDialogue(name, text.transform))
            {
                return UiFontRole.Dialogue;
            }

            if (IsDisplaySecondary(name, text.transform))
            {
                return UiFontRole.DisplaySecondary;
            }

            if (IsDisplay(name, text.transform))
            {
                return UiFontRole.Display;
            }

            return UiFontRole.Body;
        }

        public static FontStyle ResolveStyle(Text text, UiFontRole role)
        {
            if (text == null)
            {
                return FontStyle.Normal;
            }

            if (role == UiFontRole.Display || role == UiFontRole.DisplaySecondary)
            {
                return FontStyle.Normal;
            }

            var name = text.gameObject.name;
            if (role == UiFontRole.Dialogue)
            {
                if (name == "Nameplate")
                {
                    return FontStyle.Bold;
                }

                if (name == "DisclaimerText" || name == "Placeholder")
                {
                    return FontStyle.Italic;
                }
            }

            var existing = text.fontStyle;
            if ((existing & FontStyle.Bold) != 0 && (existing & FontStyle.Italic) != 0)
            {
                return FontStyle.BoldAndItalic;
            }

            if ((existing & FontStyle.Bold) != 0)
            {
                return FontStyle.Bold;
            }

            if ((existing & FontStyle.Italic) != 0)
            {
                return FontStyle.Italic;
            }

            return FontStyle.Normal;
        }

        private static bool IsDialogue(string name, Transform transform)
        {
            if (name == "Nameplate"
                || name == "DialogueBody"
                || name == "TextCardBody"
                || name == "DisclaimerText")
            {
                return true;
            }

            if (name == "Body" && IsUnder(transform, "LogPanel"))
            {
                return true;
            }

            if (name == "LogLine" || name == "LogDisplayText")
            {
                return true;
            }

            return false;
        }

        private static bool IsDisplaySecondary(string name, Transform transform)
        {
            if (name == "PhaseLabel"
                || name == "SlotLabel"
                || name == "DeadlineLabel"
                || name == "Phase")
            {
                return true;
            }

            if (name == "Label" && IsUnder(transform, "Option_"))
            {
                return true;
            }

            return false;
        }

        private static bool IsDisplay(string name, Transform transform)
        {
            if (name == "DateLabel"
                || name == "LogTitle"
                || name == "AgreeLabel"
                || name == "DisagreeLabel"
                || name == "ChoicePrompt"
                || name == "Wordmark"
                || name == "Watermark"
                || name == "MonthBig"
                || name == "TodayLabel")
            {
                return true;
            }

            if (name.EndsWith("Title")
                || name.EndsWith("Header")
                || name.EndsWith("Chip")
                || name.EndsWith("Prompt"))
            {
                return true;
            }

            if (name == "Label" && IsUnder(transform, "ConvenienceBar"))
            {
                return true;
            }

            if (name == "Label" && (IsUnder(transform, "LogButton")
                                    || IsUnder(transform, "AutoButton")
                                    || IsUnder(transform, "SkipButton")
                                    || IsUnder(transform, "CloseButton")))
            {
                return true;
            }

            if (name == "Label" && IsUnder(transform, "PromptBar"))
            {
                return true;
            }

            if (name == "Label" && HasAncestorPrefix(transform, "Row_"))
            {
                return true;
            }

            if (name == "Label" && IsUnder(transform, "BtnCalendar"))
            {
                return true;
            }

            if (name == "dateChipLabel" || name == "menuButtonLabel")
            {
                return true;
            }

            return false;
        }

        private static bool IsUnder(Transform transform, string ancestorNameContains)
        {
            var current = transform.parent;
            while (current != null)
            {
                if (current.name.Contains(ancestorNameContains))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool HasAncestorPrefix(Transform transform, string prefix)
        {
            var current = transform.parent;
            while (current != null)
            {
                if (current.name.StartsWith(prefix))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}

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
            if (name == "Bio"
                || name == "Quote"
                || name == "Hint"
                || (name == "Label" && IsUnder(transform, "LinkEpisodes")))
            {
                return false;
            }

            if (name == "HpValue"
                || name == "PrepValue"
                || name == "NameLabel"
                || name == "HpLabel"
                || name == "PrepLabel"
                || name == "LabelJp")
            {
                return true;
            }

            if (name == "DateLabel"
                || name == "DayLabel"
                || name == "LocationLabel"
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
                if (IsBondsRowText(transform))
                {
                    return false;
                }

                return true;
            }

            if (name == "Label" && IsHubMenuButtonLabel(transform))
            {
                return true;
            }

            if (IsStatNodeLabel(name, transform))
            {
                return false;
            }

            if (IsUnder(transform, "HeaderBonds")
                || IsUnder(transform, "CenterStats")
                || IsUnder(transform, "DetailCard"))
            {
                return true;
            }

            if (name == "dateChipLabel"
                || name == "menuButtonLabel"
                || name == "TitleLabel")
            {
                return true;
            }

            return false;
        }

        private static bool IsStatNodeLabel(string name, Transform transform)
        {
            if (name != "Name" && name != "Rank" && name != "Flavor")
            {
                return false;
            }

            var parent = transform.parent;
            return parent != null && parent.name.StartsWith("Node_");
        }

        private static bool IsBondsRowText(Transform transform)
        {
            return IsUnder(transform, "LeftNav") || IsUnder(transform, "LinkEpisodes");
        }

        private static bool IsHubMenuButtonLabel(Transform transform)
        {
            return IsUnder(transform, "BtnStats")
                   || IsUnder(transform, "BtnBonds")
                   || IsUnder(transform, "BtnCalendar")
                   || IsUnder(transform, "BtnSystem")
                   || IsUnder(transform, "BtnSave")
                   || IsUnder(transform, "BtnLoad")
                   || IsUnder(transform, "BtnConfig")
                   || IsUnder(transform, "BtnReturnToTitle");
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

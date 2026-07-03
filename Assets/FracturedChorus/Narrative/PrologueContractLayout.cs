using UnityEngine;

namespace FracturedChorus.Narrative
{
    public static class PrologueContractLayout
    {
        public const string ContractSpritePath =
            "Assets/FracturedChorus/Art/UI/Narrative/Contract_Document_Scribble_v1.png";

        public static readonly Vector2 NameLineMin = new Vector2(0.29f, 0.235f);
        public static readonly Vector2 NameLineMax = new Vector2(0.83f, 0.285f);
        public static readonly Vector2 SignatureLineMin = new Vector2(0.29f, 0.125f);
        public static readonly Vector2 SignatureLineMax = new Vector2(0.83f, 0.175f);

        public static void ApplyFieldRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        public static void ApplyFieldRect(RectTransform rect, PrologueVNLayoutConfig config, bool nameField)
        {
            if (config == null)
            {
                ApplyFieldRect(
                    rect,
                    nameField ? NameLineMin : SignatureLineMin,
                    nameField ? NameLineMax : SignatureLineMax);
                return;
            }

            ApplyFieldRect(
                rect,
                nameField ? config.nameLineMin : config.signatureLineMin,
                nameField ? config.nameLineMax : config.signatureLineMax);
        }
    }
}

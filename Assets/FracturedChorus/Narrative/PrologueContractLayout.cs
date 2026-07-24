using UnityEngine;

namespace FracturedChorus.Narrative
{
    public static class PrologueContractLayout
    {
        public const string ContractSpritePath =
            "Assets/FracturedChorus/Art/UI/Narrative/Contract_Document_Realistic_v2.png";

        public static readonly Vector2 NameLineMin = new Vector2(0.355f, 0.205f);
        public static readonly Vector2 NameLineMax = new Vector2(0.70f, 0.25f);
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

        public static void CaptureFieldAnchors(
            RectTransform paper,
            RectTransform field,
            out Vector2 anchorMin,
            out Vector2 anchorMax)
        {
            anchorMin = NameLineMin;
            anchorMax = NameLineMax;

            if (paper == null || field == null)
            {
                return;
            }

            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(paper, field);
            var paperRect = paper.rect;
            if (paperRect.width <= 0f || paperRect.height <= 0f)
            {
                return;
            }

            var xMin = (bounds.min.x - paperRect.xMin) / paperRect.width;
            var xMax = (bounds.max.x - paperRect.xMin) / paperRect.width;
            var yMin = (bounds.min.y - paperRect.yMin) / paperRect.height;
            var yMax = (bounds.max.y - paperRect.yMin) / paperRect.height;

            anchorMin = new Vector2(Mathf.Clamp01(xMin), Mathf.Clamp01(yMin));
            anchorMax = new Vector2(Mathf.Clamp01(xMax), Mathf.Clamp01(yMax));

            if (anchorMax.x < anchorMin.x)
            {
                (anchorMin.x, anchorMax.x) = (anchorMax.x, anchorMin.x);
            }

            if (anchorMax.y < anchorMin.y)
            {
                (anchorMin.y, anchorMax.y) = (anchorMax.y, anchorMin.y);
            }
        }
    }
}

using UnityEngine;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Layout thẻ party — thẻ 1 (C1/Tank) neo ngoài cùng phải; thẻ 2,3… tăng dần sang trái (−100px X mỗi bước).
    /// </summary>
    public static class PartyCardLayout
    {
        public const float CardStepX = 100f;
        public const float BadgeSize = 22f;
        public const float BadgeIconInset = 4f;
        public const float BadgeAnchorX = -4f;
        public const float BadgeAnchorY = -4f;

        /// <param name="cardIndex">0 = thẻ 1 (C1/front), 1 = thẻ 2, …</param>
        /// <param name="totalCards">Số thẻ đang hiển thị trên bar.</param>
        public static Vector2 GetCardAnchoredPosition(int cardIndex, int totalCards)
        {
            if (totalCards <= 0)
            {
                return Vector2.zero;
            }

            var clampedIndex = Mathf.Clamp(cardIndex, 0, totalCards - 1);
            var xFromLeft = (totalCards - 1 - clampedIndex) * CardStepX;
            return new Vector2(xFromLeft, 0f);
        }

        /// <summary>Số thẻ hiển thị 1-based (thẻ 1 = Tank / C1).</summary>
        public static int GetCardDisplayNumber(int cardIndex) => cardIndex + 1;

        public static void ApplyElementBadgeRect(RectTransform badgeRect)
        {
            if (badgeRect == null)
            {
                return;
            }

            badgeRect.anchorMin = new Vector2(1f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(BadgeAnchorX, BadgeAnchorY);
            badgeRect.sizeDelta = new Vector2(BadgeSize, BadgeSize);
            badgeRect.localScale = Vector3.one;
        }

        public static void ApplyElementIconRect(RectTransform iconRect)
        {
            if (iconRect == null)
            {
                return;
            }

            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            var inset = BadgeIconInset;
            iconRect.offsetMin = new Vector2(inset, inset);
            iconRect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}

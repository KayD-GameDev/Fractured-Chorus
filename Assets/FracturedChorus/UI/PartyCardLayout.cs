using UnityEngine;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Layout thẻ party — vị trí/size object trên thẻ lấy từ CardTemplate Hierarchy.
    /// Hằng số dưới đây chỉ là FALLBACK khi node chưa được dựng trong scene.
    /// </summary>
    public static class PartyCardLayout
    {
        /// <summary>Fallback — modular P5 party card. Scene CardTemplate size wins.</summary>
        public const float CardWidth = 240f;
        public const float CardHeight = 118f;
        public const float CardGap = 2.75f;
        public const float CardStepX = CardWidth + CardGap;

        public static float ComputeCardStepX(float effectiveCardWidth, float cardGap) =>
            effectiveCardWidth + cardGap;

        public const float BadgeSize = 22f;
        public const float BadgeIconInset = 4f;
        public const float BadgeAnchorX = 16f;
        public const float BadgeAnchorY = -12f;

        // Fallback khi Hierarchy chưa có BarStack / badge (không dùng để ghi đè scene đã author).
        public const float EmbeddedCardWidth = 180.7f;
        public const float EmbeddedCardHeight = 180f;
        public const float EmbeddedBarStackRotationZ = -18f;
        public const float EmbeddedBarStackWidth = 82f;
        public const float EmbeddedBarStackHeight = 36f;
        public const float EmbeddedBarStackPosX = 18f;
        public const float EmbeddedBarStackPosY = 36f;
        public const float EmbeddedSlotGap = 3f;
        public const float EmbeddedBadgeSize = 35f;
        /// <summary>Enemy badge inset từ góc trên-phải (khớp editor EnsureElementBadge).</summary>
        public const float EmbeddedBadgeAnchorX = -18f;
        public const float EmbeddedBadgeAnchorY = -18f;

        public const float ModularCardRotationZ = -6f;
        public const float ModularEnemyCardRotationZ = 6f;
        public const float ModularAvatarWidth = 80f;
        public const float ModularAvatarHeight = 102f;

        public static void ApplyEmbeddedBarStackRect(RectTransform barStack)
        {
            if (barStack == null)
            {
                return;
            }

            barStack.anchorMin = new Vector2(0f, 0f);
            barStack.anchorMax = new Vector2(0f, 0f);
            barStack.pivot = new Vector2(0.5f, 0.5f);
            barStack.anchoredPosition = new Vector2(
                EmbeddedBarStackPosX + EmbeddedBarStackWidth * 0.5f,
                EmbeddedBarStackPosY + EmbeddedBarStackHeight * 0.5f);
            barStack.sizeDelta = new Vector2(EmbeddedBarStackWidth, EmbeddedBarStackHeight);
            barStack.localRotation = Quaternion.Euler(0f, 0f, EmbeddedBarStackRotationZ);
            barStack.localScale = Vector3.one;
        }

        public static void ApplyEmbeddedHealthSlotRect(RectTransform healthSlot, RectTransform gaugeSlot)
        {
            var gap = EmbeddedSlotGap * 0.5f;

            if (healthSlot != null)
            {
                healthSlot.anchorMin = new Vector2(0f, 0.5f);
                healthSlot.anchorMax = new Vector2(1f, 1f);
                healthSlot.pivot = new Vector2(0.5f, 0.5f);
                healthSlot.offsetMin = new Vector2(0f, gap);
                healthSlot.offsetMax = Vector2.zero;
                healthSlot.localRotation = Quaternion.identity;
                healthSlot.localScale = Vector3.one;
            }

            if (gaugeSlot != null)
            {
                gaugeSlot.anchorMin = new Vector2(0f, 0f);
                gaugeSlot.anchorMax = new Vector2(1f, 0.5f);
                gaugeSlot.pivot = new Vector2(0.5f, 0.5f);
                gaugeSlot.offsetMin = Vector2.zero;
                gaugeSlot.offsetMax = new Vector2(0f, -gap);
                gaugeSlot.localRotation = Quaternion.identity;
                gaugeSlot.localScale = Vector3.one;
            }
        }

        /// <summary>FALLBACK-ONLY khi Hierarchy chưa author badge.</summary>
        public static void ApplyElementBadgeRect(RectTransform badgeRect)
        {
            ApplyElementBadgeRect(badgeRect, enemySide: false);
        }

        /// <summary>
        /// Badge hệ: party góc trên-trái; enemy góc trên-phải (mép ngoài bar).
        /// </summary>
        public static void ApplyElementBadgeRect(RectTransform badgeRect, bool enemySide)
        {
            if (badgeRect == null)
            {
                return;
            }

            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.localScale = Vector3.one;
            if (enemySide)
            {
                badgeRect.anchorMin = new Vector2(1f, 1f);
                badgeRect.anchorMax = new Vector2(1f, 1f);
                badgeRect.anchoredPosition = new Vector2(-16f, -12f);
                badgeRect.sizeDelta = new Vector2(BadgeSize, BadgeSize);
                return;
            }

            badgeRect.anchorMin = new Vector2(0f, 1f);
            badgeRect.anchorMax = new Vector2(0f, 1f);
            badgeRect.anchoredPosition = new Vector2(BadgeAnchorX, BadgeAnchorY);
            badgeRect.sizeDelta = new Vector2(BadgeSize, BadgeSize);
        }

        public static void ApplyElementIconRect(RectTransform iconRect)
        {
            if (iconRect == null)
            {
                return;
            }

            // Hierarchy đã inset icon → giữ nguyên.
            if (RectSizeUtil.IsAuthored(iconRect) &&
                (iconRect.offsetMin.sqrMagnitude > 0.01f || iconRect.offsetMax.sqrMagnitude > 0.01f))
            {
                return;
            }

            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            var inset = BadgeIconInset;
            iconRect.offsetMin = new Vector2(inset, inset);
            iconRect.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>FALLBACK-ONLY khi Hierarchy chưa author node modular.</summary>
        public static void ApplyModularCardBgRect(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        public static void ApplyModularAccentRect(RectTransform rect, bool enemySide = false)
        {
            if (rect == null)
            {
                return;
            }

            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(44f, 44f);
            rect.localScale = Vector3.one;
            if (enemySide)
            {
                rect.anchorMin = new Vector2(1f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.anchoredPosition = new Vector2(-42f, 12f);
                rect.localRotation = Quaternion.Euler(0f, 0f, -12f);
                return;
            }

            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(42f, 12f);
            rect.localRotation = Quaternion.Euler(0f, 0f, 12f);
        }

        public static void ApplyModularAvatarRect(RectTransform rect, bool enemySide = false)
        {
            if (rect == null)
            {
                return;
            }

            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(ModularAvatarWidth, ModularAvatarHeight);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            if (enemySide)
            {
                rect.anchorMin = new Vector2(1f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.anchoredPosition = new Vector2(-46f, 6f);
                return;
            }

            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(46f, 6f);
        }

        public static void ApplyModularNameRect(RectTransform rect, bool enemySide = false)
        {
            if (rect == null)
            {
                return;
            }

            rect.sizeDelta = new Vector2(88f, 18f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            if (enemySide)
            {
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-12f, 10f);
                return;
            }

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(12f, 10f);
        }

        public static void ApplyModularBarStackRect(RectTransform barStack, bool enemySide = false)
        {
            if (barStack == null)
            {
                return;
            }

            barStack.sizeDelta = new Vector2(128f, 84f);
            barStack.localRotation = Quaternion.identity;
            barStack.localScale = Vector3.one;
            if (enemySide)
            {
                barStack.anchorMin = new Vector2(0f, 0.5f);
                barStack.anchorMax = new Vector2(0f, 0.5f);
                barStack.pivot = new Vector2(0f, 0.5f);
                barStack.anchoredPosition = new Vector2(14f, 2f);
                return;
            }

            barStack.anchorMin = new Vector2(1f, 0.5f);
            barStack.anchorMax = new Vector2(1f, 0.5f);
            barStack.pivot = new Vector2(1f, 0.5f);
            barStack.anchoredPosition = new Vector2(-14f, 2f);
        }

        public static void ApplyModularHealthGaugeSlots(RectTransform healthSlot, RectTransform gaugeSlot)
        {
            const float split = 0.46f;
            const float gap = 1.25f;

            if (healthSlot != null)
            {
                healthSlot.anchorMin = new Vector2(0f, split);
                healthSlot.anchorMax = new Vector2(1f, 1f);
                healthSlot.pivot = new Vector2(0.5f, 0.5f);
                healthSlot.offsetMin = new Vector2(0f, gap);
                healthSlot.offsetMax = Vector2.zero;
                healthSlot.localRotation = Quaternion.identity;
                healthSlot.localScale = Vector3.one;
            }

            if (gaugeSlot != null)
            {
                gaugeSlot.anchorMin = new Vector2(0f, 0f);
                gaugeSlot.anchorMax = new Vector2(1f, split);
                gaugeSlot.pivot = new Vector2(0.5f, 0.5f);
                gaugeSlot.offsetMin = Vector2.zero;
                gaugeSlot.offsetMax = new Vector2(0f, -gap);
                gaugeSlot.localRotation = Quaternion.identity;
                gaugeSlot.localScale = Vector3.one;
            }
        }

        public static void ApplyModularHpLabelRect(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(2f, -1f);
            rect.sizeDelta = new Vector2(24f, 14f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        public static void ApplyModularHpValueRect(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(26f, -1f);
            rect.sizeDelta = new Vector2(98f, 22f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        public static void ApplyModularHealthBarRect(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 2f);
            rect.sizeDelta = new Vector2(-4f, 8f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        public static void ApplyModularPrepLabelRect(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(2f, -1f);
            rect.sizeDelta = new Vector2(36f, 12f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        public static void ApplyModularPrepValueRect(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(38f, -1f);
            rect.sizeDelta = new Vector2(86f, 16f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        public static void ApplyModularPrepPipsRect(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 2f);
            rect.sizeDelta = new Vector2(-4f, 11f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        public static Vector2 GetCardAnchoredPosition(int cardIndex, int totalCards) =>
            GetCardAnchoredPosition(cardIndex, totalCards, CardStepX);

        public static Vector2 GetCardAnchoredPosition(int cardIndex, int totalCards, float cardStepX)
        {
            if (totalCards <= 0)
            {
                return Vector2.zero;
            }

            var clampedIndex = Mathf.Clamp(cardIndex, 0, totalCards - 1);
            var xFromLeft = (totalCards - 1 - clampedIndex) * cardStepX;
            return new Vector2(xFromLeft, 0f);
        }

        public static int GetCardDisplayNumber(int cardIndex) => cardIndex + 1;
    }
}

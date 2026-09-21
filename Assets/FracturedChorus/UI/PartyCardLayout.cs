using UnityEngine;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Layout thẻ party — vị trí/size object trên thẻ lấy từ CardTemplate Hierarchy.
    /// Hằng số dưới đây chỉ là FALLBACK khi node chưa được dựng trong scene.
    /// </summary>
    public static class PartyCardLayout
    {
        /// <summary>Fallback — compact portrait + side tubes. Scene CardTemplate size wins.</summary>
        public const float CardWidth = 160f;
        public const float CardHeight = 118f;
        public const float CardGap = 2.75f;
        public const float CardStepX = CardWidth + CardGap;
        public const float DefaultStatusBarWidth = 798f;

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

        /// <summary>Fallback BuffAstraTv — scene CardTemplate Rect thắng khi đã author.</summary>
        public const float AstraTvMoodIconSize = 28f;
        public const float AstraTvMoodIconPosX = -32f;
        public const float AstraTvMoodIconPosY = -34f;

        public const float ModularCardRotationZ = -6f;
        public const float ModularEnemyCardRotationZ = 6f;
        public const float ModularAvatarWidth = 96f;
        public const float ModularAvatarHeight = 96f;
        public const float AvatarLeftPad = 8f;
        public const float AvatarTubeGap = 4f;
        public const float SideTubeWidth = 14f;
        public const float SideTubeGap = 3f;
        public const float SideTubeHeight = 100f;

        public static float SideTubeStackWidth => SideTubeWidth * 2f + SideTubeGap;

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
            badgeRect.anchorMin = new Vector2(0f, 1f);
            badgeRect.anchorMax = new Vector2(0f, 1f);
            badgeRect.anchoredPosition = new Vector2(BadgeAnchorX, BadgeAnchorY);
            badgeRect.sizeDelta = new Vector2(BadgeSize, BadgeSize);
        }

        /// <summary>FALLBACK-ONLY khi Hierarchy chưa author BuffAstraTv.</summary>
        public static void ApplyAstraTvMoodIconRect(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(AstraTvMoodIconPosX, AstraTvMoodIconPosY);
            rect.sizeDelta = new Vector2(AstraTvMoodIconSize, AstraTvMoodIconSize);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
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
            rect.sizeDelta = new Vector2(36f, 36f);
            rect.localScale = Vector3.one;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(AvatarLeftPad + 22f, 14f);
            rect.localRotation = Quaternion.Euler(0f, 0f, enemySide ? -12f : 12f);
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
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(
                AvatarLeftPad + ModularAvatarWidth * 0.5f,
                8f);
        }

        public static void ApplyModularNameRect(RectTransform rect, bool enemySide = false)
        {
            if (rect == null)
            {
                return;
            }

            rect.sizeDelta = new Vector2(ModularAvatarWidth, 16f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(AvatarLeftPad, 4f);
        }

        public static void ApplyModularBarStackRect(RectTransform barStack, bool enemySide = false)
        {
            if (barStack == null)
            {
                return;
            }

            barStack.sizeDelta = new Vector2(SideTubeStackWidth, SideTubeHeight);
            barStack.localRotation = Quaternion.identity;
            barStack.localScale = Vector3.one;
            barStack.anchorMin = new Vector2(0f, 0.5f);
            barStack.anchorMax = new Vector2(0f, 0.5f);
            barStack.pivot = new Vector2(0f, 0.5f);
            barStack.anchoredPosition = new Vector2(
                AvatarLeftPad + ModularAvatarWidth + AvatarTubeGap,
                8f);
        }

        public static void ApplyModularHealthGaugeSlots(RectTransform healthSlot, RectTransform gaugeSlot)
        {
            ApplySideTubeColumn(healthSlot, 0f);
            ApplySideTubeColumn(gaugeSlot, SideTubeWidth + SideTubeGap);
        }

        private static void ApplySideTubeColumn(RectTransform slot, float x)
        {
            if (slot == null)
            {
                return;
            }

            slot.anchorMin = new Vector2(0f, 0f);
            slot.anchorMax = new Vector2(0f, 1f);
            slot.pivot = new Vector2(0f, 0.5f);
            slot.offsetMin = new Vector2(x, 0f);
            slot.offsetMax = new Vector2(x + SideTubeWidth, 0f);
            slot.localRotation = Quaternion.identity;
            slot.localScale = Vector3.one;
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

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
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

        public static float ComputeRowFitScale(int cardCount, float cardWidth, float cardGap, float barWidth)
        {
            if (cardCount <= 0 || cardWidth <= 0f || barWidth <= 0f)
            {
                return 1f;
            }

            var needed = cardCount * cardWidth + Mathf.Max(0, cardCount - 1) * cardGap;
            if (needed <= barWidth)
            {
                return 1f;
            }

            return barWidth / needed;
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

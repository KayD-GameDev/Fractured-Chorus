using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public static class DamageNumberDigitStrip
    {
        public const float NativeHeight = 200f;
        public const float NativeGutter = 5f;
        public const float DefaultHeight = 72f;
        public const float CritHeight = 92f;
        public const int MaxDigits = 6;

        public static Color ResolveTint(bool heal, bool isCritical)
        {
            if (heal)
            {
                return FcColorTokens.Semantic.Heal;
            }

            return isCritical ? Color.white : Color.white;
        }

        public static float ResolveHeight(bool isCritical)
        {
            return isCritical ? CritHeight : DefaultHeight;
        }

        public static Image CreateImage(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.type = Image.Type.Simple;
            return image;
        }

        public static Vector2 Apply(
            RectTransform row,
            List<Image> digits,
            Image critBadge,
            int amount,
            bool heal,
            bool isCritical)
        {
            if (row == null || digits == null)
            {
                return Vector2.zero;
            }

            amount = Mathf.Max(0, amount);
            var height = ResolveHeight(isCritical);
            var tint = ResolveTint(heal, isCritical);
            var spacing = NativeGutter * (height / NativeHeight);
            var text = amount.ToString();
            if (text.Length > MaxDigits)
            {
                text = text.Substring(text.Length - MaxDigits);
            }

            var showBadge = isCritical && !heal && DamageNumberDigitAtlas.CritBadge != null;
            var cursor = 0f;
            if (showBadge && critBadge != null)
            {
                var badge = DamageNumberDigitAtlas.CritBadge;
                var badgeSize = SizeForSprite(badge, height * 0.72f);
                critBadge.gameObject.SetActive(true);
                critBadge.sprite = badge;
                critBadge.color = Color.white;
                critBadge.rectTransform.sizeDelta = badgeSize;
                critBadge.rectTransform.anchoredPosition = new Vector2(cursor + badgeSize.x * 0.5f, 0f);
                cursor += badgeSize.x + spacing;
            }
            else if (critBadge != null)
            {
                critBadge.gameObject.SetActive(false);
            }

            for (var i = 0; i < digits.Count; i++)
            {
                var image = digits[i];
                if (image == null)
                {
                    continue;
                }

                if (i >= text.Length)
                {
                    image.gameObject.SetActive(false);
                    continue;
                }

                var value = text[i] - '0';
                var sprite = DamageNumberDigitAtlas.GetDigit(value, heal);
                if (sprite == null)
                {
                    image.gameObject.SetActive(false);
                    continue;
                }

                var size = SizeForSprite(sprite, height);
                image.gameObject.SetActive(true);
                image.sprite = sprite;
                image.color = tint;
                image.rectTransform.sizeDelta = size;
                image.rectTransform.anchoredPosition = new Vector2(cursor + size.x * 0.5f, 0f);
                cursor += size.x + spacing;
            }

            var total = Mathf.Max(0f, cursor - spacing);
            var shift = -total * 0.5f;
            if (showBadge && critBadge != null)
            {
                var badgePos = critBadge.rectTransform.anchoredPosition;
                critBadge.rectTransform.anchoredPosition = new Vector2(badgePos.x + shift, badgePos.y);
            }

            for (var i = 0; i < digits.Count && i < text.Length; i++)
            {
                var image = digits[i];
                if (image == null || !image.gameObject.activeSelf)
                {
                    continue;
                }

                var pos = image.rectTransform.anchoredPosition;
                image.rectTransform.anchoredPosition = new Vector2(pos.x + shift, pos.y);
            }

            row.sizeDelta = new Vector2(total, height);
            return row.sizeDelta;
        }

        private static Vector2 SizeForSprite(Sprite sprite, float height)
        {
            if (sprite == null)
            {
                return new Vector2(height * 0.5f, height);
            }

            var rect = sprite.rect;
            if (rect.height <= 0.01f)
            {
                return new Vector2(height * 0.5f, height);
            }

            return new Vector2(rect.width * (height / rect.height), height);
        }
    }
}

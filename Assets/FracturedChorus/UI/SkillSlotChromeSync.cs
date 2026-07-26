using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Đồng bộ chrome ô skill (size + Frame/Ring/Label/Icon/Art) từ SkillSlot_Template.
    /// Giữ nguyên <see cref="RectTransform.anchoredPosition"/> của từng ô Top/Left/Right.
    /// </summary>
    public static class SkillSlotChromeSync
    {
        public const string TemplateName = "SkillSlot_Template";

        /// <summary>Ưu tiên SkillSlot_Template; không có thì dùng SkillSlot_Top làm template.</summary>
        public static RectTransform ResolveTemplate(RectTransform radialRoot, RectTransform fallbackTop)
        {
            if (radialRoot != null)
            {
                var named = radialRoot.Find(TemplateName) as RectTransform;
                if (named != null)
                {
                    return named;
                }
            }

            return fallbackTop;
        }

        /// <summary>
        /// Copy layout chrome từ template → slot. Không đổi vị trí radial (anchoredPosition).
        /// Sibling order: Ring → Icon → Frame → Label (Frame vẽ trên art, Label trên cùng).
        /// </summary>
        public static void ApplyFromTemplate(RectTransform templateRoot, RectTransform slotRoot)
        {
            if (templateRoot == null || slotRoot == null)
            {
                return;
            }

            if (templateRoot == slotRoot)
            {
                ApplySiblingOrder(slotRoot);
                return;
            }

            var savedPos = slotRoot.anchoredPosition;
            CopyRectLayout(templateRoot, slotRoot);
            slotRoot.anchoredPosition = savedPos;

            CopyRootImageStyle(templateRoot.GetComponent<Image>(), slotRoot.GetComponent<Image>());

            SyncNamedChild(templateRoot, slotRoot, "Ring", copyImageFully: true);
            SyncNamedChild(templateRoot, slotRoot, "Icon", copyImageFully: true);

            var templateIcon = templateRoot.Find("Icon") as RectTransform;
            var slotIcon = slotRoot.Find("Icon") as RectTransform;
            if (templateIcon != null && slotIcon != null)
            {
                // Art: layout + Mask setup only — skill sprite vẫn từ Bind.
                SyncNamedChild(templateIcon, slotIcon, "Art", copyImageFully: false, copySprite: false);
            }

            // Frame layout/fallback từ template; sprite có thể bị SkillDefinitionSO.frame ghi đè sau Bind.
            SyncNamedChild(templateRoot, slotRoot, "Frame", copyImageFully: true);
            SyncNamedChild(templateRoot, slotRoot, "Label", copyImageFully: false, copySprite: false, copyTextStyle: true);

            ApplySiblingOrder(slotRoot);
        }

        /// <summary>Ring → Icon → Frame → Label.</summary>
        public static void ApplySiblingOrder(RectTransform slotRoot)
        {
            if (slotRoot == null)
            {
                return;
            }

            var index = 0;
            PlaceSibling(slotRoot, "Ring", ref index);
            PlaceSibling(slotRoot, "Icon", ref index);
            PlaceSibling(slotRoot, "Frame", ref index);
            PlaceSibling(slotRoot, "Label", ref index);

            var frame = slotRoot.Find("Frame");
            if (frame != null)
            {
                frame.gameObject.SetActive(true);
                var frameImage = frame.GetComponent<Image>();
                if (frameImage != null)
                {
                    frameImage.enabled = true;
                    frameImage.raycastTarget = false;
                }
            }
        }

        private static void PlaceSibling(Transform slotRoot, string childName, ref int index)
        {
            var child = slotRoot.Find(childName);
            if (child == null)
            {
                return;
            }

            child.SetSiblingIndex(index);
            index++;
        }

        private static void SyncNamedChild(
            Transform templateParent,
            Transform slotParent,
            string childName,
            bool copyImageFully,
            bool copySprite = true,
            bool copyTextStyle = false)
        {
            var templateChild = templateParent.Find(childName) as RectTransform;
            if (templateChild == null)
            {
                return;
            }

            var slotChild = slotParent.Find(childName) as RectTransform;
            if (slotChild == null)
            {
                var go = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer));
                slotChild = go.GetComponent<RectTransform>();
                slotChild.SetParent(slotParent, false);
                CopyComponentsShallow(templateChild.gameObject, go);
            }

            CopyRectLayout(templateChild, slotChild);

            if (copyImageFully || copySprite)
            {
                CopyImageVisual(
                    templateChild.GetComponent<Image>(),
                    slotChild.GetComponent<Image>(),
                    forceFull: copyImageFully,
                    copySprite: copySprite);
            }

            if (copyTextStyle)
            {
                CopyTextStyle(templateChild.GetComponent<Text>(), slotChild.GetComponent<Text>());
            }

            var templateMask = templateChild.GetComponent<Mask>();
            var slotMask = slotChild.GetComponent<Mask>();
            if (templateMask != null)
            {
                if (slotMask == null)
                {
                    slotMask = slotChild.gameObject.AddComponent<Mask>();
                }

                slotMask.showMaskGraphic = templateMask.showMaskGraphic;
            }
        }

        private static void CopyRectLayout(RectTransform from, RectTransform to)
        {
            if (from == null || to == null)
            {
                return;
            }

            to.anchorMin = from.anchorMin;
            to.anchorMax = from.anchorMax;
            to.pivot = from.pivot;
            to.sizeDelta = from.sizeDelta;
            to.anchoredPosition = from.anchoredPosition;
            to.offsetMin = from.offsetMin;
            to.offsetMax = from.offsetMax;
            to.localRotation = from.localRotation;
            to.localScale = from.localScale;
        }

        private static void CopyRootImageStyle(Image from, Image to)
        {
            if (from == null || to == null)
            {
                return;
            }

            if (from.sprite != null)
            {
                to.sprite = from.sprite;
            }

            to.type = from.type;
            to.preserveAspect = from.preserveAspect;
            // Idle/highlight màu nền vẫn do slot runtime; chỉ sync sprite/type.
        }

        private static void CopyImageVisual(Image from, Image to, bool forceFull, bool copySprite)
        {
            if (from == null || to == null)
            {
                return;
            }

            if (copySprite && from.sprite != null && (forceFull || to.sprite == null))
            {
                to.sprite = from.sprite;
            }

            to.type = from.type;
            to.preserveAspect = from.preserveAspect;

            if (forceFull)
            {
                to.color = from.color;
                to.enabled = from.enabled;
            }
            else if (to.color.a <= 0.01f)
            {
                to.color = from.color;
            }

            to.raycastTarget = false;
        }

        private static void CopyTextStyle(Text from, Text to)
        {
            if (from == null || to == null)
            {
                return;
            }

            if (from.font != null)
            {
                to.font = from.font;
            }

            to.fontSize = from.fontSize;
            to.fontStyle = from.fontStyle;
            to.alignment = from.alignment;
            to.color = from.color;
            to.horizontalOverflow = from.horizontalOverflow;
            to.verticalOverflow = from.verticalOverflow;
            to.raycastTarget = false;
            // Không copy text — label runtime [W]/[A]/[D].
        }

        private static void CopyComponentsShallow(GameObject from, GameObject to)
        {
            var fromImage = from.GetComponent<Image>();
            if (fromImage != null && to.GetComponent<Image>() == null)
            {
                var img = to.AddComponent<Image>();
                img.sprite = fromImage.sprite;
                img.type = fromImage.type;
                img.color = fromImage.color;
                img.raycastTarget = false;
                img.preserveAspect = fromImage.preserveAspect;
            }

            var fromText = from.GetComponent<Text>();
            if (fromText != null && to.GetComponent<Text>() == null)
            {
                var text = to.AddComponent<Text>();
                text.font = fromText.font;
                text.fontSize = fromText.fontSize;
                text.alignment = fromText.alignment;
                text.color = fromText.color;
                text.raycastTarget = false;
            }

            var fromMask = from.GetComponent<Mask>();
            if (fromMask != null && to.GetComponent<Mask>() == null)
            {
                var mask = to.AddComponent<Mask>();
                mask.showMaskGraphic = fromMask.showMaskGraphic;
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public static class ResonanceDiveHierarchy
    {
        public const string ShadowRootName = "ShadowRoot";
        public const string VisualName = "Visual";
        public const string FaceMaskName = "FaceMask";
        public const string TextRootName = "TextRoot";

        private static readonly string[] Retired =
        {
            "Layer_Text",
        };

        public static void Ensure(ResonanceDiveButton button)
        {
            if (button == null)
            {
                return;
            }

            var root = button.transform;
            RetireOld(root);
            var motion = button.GetComponent<ResonanceDiveButton>();
            motion.ResolveRefs();

            var shadowRoot = GetOrCreateGroup(root, ShadowRootName, 0);
            var visual = GetOrCreateGroup(root, VisualName, 1);
            var textRoot = GetOrCreateGroup(root, TextRootName, 2);
            SeedLayer(shadowRoot, "Layer_Shadow", true);
            SeedLayer(visual, "Layer_Base", true);
            SeedLayer(visual, "Layer_Glass", true);
            SeedLayer(visual, "Layer_Gradient", true);
            SeedLayer(visual, "Layer_Border", true);
            SeedLayer(visual, "Layer_Glow", true);
            SeedLayer(visual, "Layer_Wave", true);
            var mask = GetOrCreateGroup(visual, FaceMaskName, visual.childCount);
            EnsureMask(mask);
            SeedLayer(mask, "Layer_Scanline", false);
            var decoLeft = SeedLayer(visual, "Layer_DecoLeft", true);
            SeedLayer(visual, "Layer_DecoRight", true);
            var atlas = SeedLayer(visual, "Layer_Particles", true);
            if (atlas != null)
            {
                atlas.gameObject.SetActive(false);
            }
            SeedLabel(textRoot, "TitleLabel", "RESONANCE DIVE", UiFontRole.Display);
            SeedLabel(textRoot, "SubtitleLabel", "ENTER THE OTHER SIDE", UiFontRole.Body);
            ApplyDecoPivot(decoLeft);
            HideBakedText(root);
            textRoot.SetAsLastSibling();
            button.ResolveRefs();
        }

        private static void RetireOld(Transform root)
        {
            for (var i = 0; i < root.childCount; i += 1)
            {
                var child = root.GetChild(i);
                if (child.name == "Layer_Text" || child.name == "Layer_Particles")
                {
                    child.gameObject.SetActive(false);
                }
            }

            foreach (var name in Retired)
            {
                var found = root.Find(name);
                if (found != null)
                {
                    found.gameObject.SetActive(false);
                }
            }
        }

        private static void HideBakedText(Transform root)
        {
            var baked = root.Find("Layer_Text");
            if (baked == null)
            {
                var visual = root.Find(VisualName);
                baked = visual != null ? visual.Find("Layer_Text") : null;
            }

            if (baked != null)
            {
                baked.gameObject.SetActive(false);
            }
        }

        private static RectTransform GetOrCreateGroup(Transform parent, string name, int sibling)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            Stretch(rect);
            rect.SetSiblingIndex(Mathf.Clamp(sibling, 0, parent.childCount - 1));
            return rect;
        }

        private static Image SeedLayer(Transform parent, string name, bool stretch)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing.GetComponent<Image>();
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            if (stretch)
            {
                Stretch(rect);
            }
            else
            {
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(0f, 8f);
            }

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.color = Color.white;
            return image;
        }

        private static void EnsureMask(RectTransform mask)
        {
            var image = mask.GetComponent<Image>();
            if (image == null)
            {
                image = mask.gameObject.AddComponent<Image>();
            }

            image.raycastTarget = false;
            image.color = Color.white;
            image.preserveAspect = true;
            var maskComp = mask.GetComponent<Mask>();
            if (maskComp == null)
            {
                maskComp = mask.gameObject.AddComponent<Mask>();
            }

            maskComp.showMaskGraphic = false;
        }

        private static void SeedLabel(Transform parent, string name, string copy, UiFontRole role)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return;
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            Stretch(rect);
            if (name == "TitleLabel")
            {
                rect.offsetMin = new Vector2(80f, 8f);
                rect.offsetMax = new Vector2(-40f, -4f);
            }
            else
            {
                rect.offsetMin = new Vector2(80f, -36f);
                rect.offsetMax = new Vector2(-40f, -48f);
            }

            var text = go.GetComponent<Text>();
            text.text = copy;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            UiFontCatalog.Apply(text, role, name == "TitleLabel" ? 42 : 16);
        }

        private static void ApplyDecoPivot(Image decoLeft)
        {
            if (decoLeft == null)
            {
                return;
            }

            var motion = Resources.Load<ResonanceDiveMotion>(ResonanceDiveButton.MotionResource);
            if (motion == null)
            {
                return;
            }

            if (decoLeft.rectTransform.pivot == new Vector2(0.5f, 0.5f))
            {
                decoLeft.rectTransform.pivot = motion.DecoLeftPivot;
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }
    }
}

using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.RunMap.UI
{
    public static class HologramChoiceCardChrome
    {
        public static readonly Color Frame = FcColorTokens.WithAlpha(FcColorTokens.Brand.CyanNeonBody, 0.95f);
        public static readonly Color FrameGlow = FcColorTokens.WithAlpha(FcColorTokens.Brand.CyanNeonCore, 0.4f);
        public static readonly Color InnerEdge = FcColorTokens.WithAlpha(FcColorTokens.Brand.MagentaAccent, 0.7f);
        public static readonly Color Fill = new Color(0.03f, 0.16f, 0.26f, 221f / 255f);
        public static readonly Color Scan = FcColorTokens.WithAlpha(FcColorTokens.Brand.CyanNeonCore, 0.08f);
        public static readonly Color Kind = FcColorTokens.Brand.CyanNeonBody;
        public static readonly Color Title = FcColorTokens.Brand.CyanNeonCore;
        public static readonly Color Body = FcColorTokens.Brand.TextPrimary;
        public static readonly Color TextShadow = new Color(0.01f, 0.06f, 0.12f, 0.92f);

        private static Sprite s_borderSprite;

        public static GameObject Create(string name, RectTransform parent, bool withCanvasGroup)
        {
            var types = withCanvasGroup
                ? new[]
                {
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button),
                    typeof(CanvasGroup)
                }
                : new[]
                {
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button)
                };

            var go = new GameObject(name, types);
            go.transform.SetParent(parent, false);

            var frame = go.GetComponent<Image>();
            frame.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = frame;
            button.transition = Selectable.Transition.None;

            var kind = CreateLabel("Kind", go.transform, "HP", 18, TextAnchor.MiddleCenter);
            ApplyRect(kind.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(260f, 28f));
            UiFontCatalog.ApplyAutomatic(kind);

            var title = CreateLabel("Title", go.transform, "Reward", 26, TextAnchor.MiddleCenter);
            ApplyRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(260f, 56f));
            UiFontCatalog.Apply(title, UiFontRole.DisplaySecondary, 26);

            var body = CreateLabel("Body", go.transform, "Description", 18, TextAnchor.UpperCenter);
            ApplyRect(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -24f), new Vector2(252f, 140f));
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;
            UiFontCatalog.ApplyAutomatic(body);

            Apply(go.transform);
            UiButtonHoverFeedback.Ensure(go);
            return go;
        }

        public static void Apply(Transform root)
        {
            if (root == null)
            {
                return;
            }

            var frame = root.GetComponent<Image>();
            if (frame != null)
            {
                ApplySlicedBorder(frame, Frame);
                EnsureOutline(frame.gameObject, FrameGlow, new Vector2(2.2f, -2.2f));
            }

            var glow = EnsureLayer(root, "Glow", FrameGlow, new Vector2(-6f, -6f), new Vector2(6f, 6f), 0);
            ApplySlicedBorder(glow, FrameGlow);
            var inner = EnsureLayer(root, "InnerEdge", InnerEdge, new Vector2(4f, 4f), new Vector2(-4f, -4f), 1);
            ApplySlicedBorder(inner, InnerEdge);
            var fill = EnsureLayer(root, "Fill", Fill, new Vector2(6f, 6f), new Vector2(-6f, -6f), 2);
            fill.sprite = null;
            fill.type = Image.Type.Simple;
            EnsureScanlines(fill.transform);
            EnsureCorners(root);
            StyleLabel(root.Find("Kind")?.GetComponent<Text>(), Kind);
            StyleLabel(root.Find("Title")?.GetComponent<Text>(), Title);
            StyleLabel(root.Find("Body")?.GetComponent<Text>(), Body);
            root.Find("Kind")?.SetAsLastSibling();
            root.Find("Title")?.SetAsLastSibling();
            root.Find("Body")?.SetAsLastSibling();
            root.GetComponent<UiButtonHoverFeedback>()?.RecaptureBaseFromGraphic();
        }

        private static void ApplySlicedBorder(Image image, Color color)
        {
            image.sprite = BorderSprite();
            image.type = Image.Type.Sliced;
            image.fillCenter = false;
            image.color = color;
        }

        private static Sprite BorderSprite()
        {
            if (s_borderSprite != null)
            {
                return s_borderSprite;
            }

            const int size = 16;
            const int border = 3;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var clear = Color.clear;
            var solid = Color.white;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var edge = x < border || x >= size - border || y < border || y >= size - border;
                    tex.SetPixel(x, y, edge ? solid : clear);
                }
            }

            tex.Apply(false, false);
            s_borderSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            s_borderSprite.hideFlags = HideFlags.HideAndDontSave;
            return s_borderSprite;
        }

        private static Image EnsureLayer(
            Transform root,
            string name,
            Color color,
            Vector2 offsetMin,
            Vector2 offsetMax,
            int sibling)
        {
            var child = root.Find(name);
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(root, false);
                child = go.transform;
            }

            var image = child.GetComponent<Image>();
            if (image == null)
            {
                image = child.gameObject.AddComponent<Image>();
            }

            image.color = color;
            image.raycastTarget = false;
            var rt = image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            child.SetSiblingIndex(Mathf.Min(sibling, root.childCount - 1));
            return image;
        }

        private static void EnsureScanlines(Transform fill)
        {
            var host = fill.Find("Scanlines");
            if (host == null)
            {
                var go = new GameObject("Scanlines", typeof(RectTransform));
                go.transform.SetParent(fill, false);
                host = go.transform;
                var hostRt = go.GetComponent<RectTransform>();
                hostRt.anchorMin = Vector2.zero;
                hostRt.anchorMax = Vector2.one;
                hostRt.offsetMin = Vector2.zero;
                hostRt.offsetMax = Vector2.zero;
            }

            for (var i = 0; i < 9; i++)
            {
                var lineName = $"Scan_{i}";
                var line = host.Find(lineName);
                if (line == null)
                {
                    var go = new GameObject(lineName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    go.transform.SetParent(host, false);
                    line = go.transform;
                }

                var image = line.GetComponent<Image>();
                image.color = Scan;
                image.raycastTarget = false;
                var rt = image.rectTransform;
                rt.anchorMin = new Vector2(0.04f, 1f);
                rt.anchorMax = new Vector2(0.96f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(0f, -18f - i * 30f);
            }
        }

        private static void EnsureCorners(Transform root)
        {
            PlaceCorner(root, "CornerTL", new Vector2(0f, 1f), new Vector2(1f, -1f));
            PlaceCorner(root, "CornerTR", new Vector2(1f, 1f), new Vector2(-1f, -1f));
            PlaceCorner(root, "CornerBL", new Vector2(0f, 0f), new Vector2(1f, 1f));
            PlaceCorner(root, "CornerBR", new Vector2(1f, 0f), new Vector2(-1f, 1f));
        }

        private static void PlaceCorner(Transform root, string name, Vector2 anchor, Vector2 dir)
        {
            var fromRight = dir.x < 0f;
            var fromTop = dir.y < 0f;
            var host = root.Find(name);
            if (host == null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(root, false);
                host = go.transform;
            }

            var hostRt = host.GetComponent<RectTransform>();
            hostRt.anchorMin = anchor;
            hostRt.anchorMax = anchor;
            hostRt.pivot = anchor;
            hostRt.anchoredPosition = new Vector2(fromRight ? -8f : 8f, fromTop ? -8f : 8f);
            hostRt.sizeDelta = Vector2.zero;

            PlaceBar(EnsureNamedImage(host, "H"), fromRight, fromTop, new Vector2(22f, 2f));
            PlaceBar(EnsureNamedImage(host, "V"), fromRight, fromTop, new Vector2(2f, 22f));
        }

        private static void PlaceBar(Image image, bool fromRight, bool fromTop, Vector2 size)
        {
            image.color = FcColorTokens.Brand.CyanNeonCore;
            image.raycastTarget = false;
            var rt = image.rectTransform;
            var pivot = new Vector2(fromRight ? 1f : 0f, fromTop ? 1f : 0f);
            rt.anchorMin = pivot;
            rt.anchorMax = pivot;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
        }

        private static Image EnsureNamedImage(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
                child = go.transform;
            }

            var image = child.GetComponent<Image>();
            if (image == null)
            {
                image = child.gameObject.AddComponent<Image>();
            }

            return image;
        }

        private static void StyleLabel(Text text, Color color)
        {
            if (text == null)
            {
                return;
            }

            text.color = color;
            EnsureOutline(text.gameObject, TextShadow, new Vector2(1.4f, -1.4f));
        }

        private static void EnsureOutline(GameObject host, Color color, Vector2 distance)
        {
            var outline = host.GetComponent<Outline>();
            if (outline == null)
            {
                outline = host.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static Text CreateLabel(string name, Transform parent, string content, int size, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = size;
            text.alignment = align;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.font = UiFontCatalog.Body != null
                ? UiFontCatalog.Body
                : Resources.GetBuiltinResource<Font>("Arial.ttf");
            return text;
        }

        private static void ApplyRect(RectTransform rt, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }
    }
}

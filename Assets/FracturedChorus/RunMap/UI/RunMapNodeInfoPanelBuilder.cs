using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.RunMap.UI
{
    public static class RunMapNodeInfoPanelBuilder
    {
        private static readonly Vector2 SidebarAnchorMin = new Vector2(0.50f, 0.05f);
        private static readonly Vector2 SidebarAnchorMax = new Vector2(0.93f, 0.30f);

        public static RunMapNodeInfoPanel EnsureSidebar(Transform parent, bool showEditPreview = false)
        {
            if (parent == null)
            {
                return null;
            }

            var existing = parent.Find("NodeInfoSidebar");
            if (existing != null)
            {
                var found = existing.GetComponent<RunMapNodeInfoPanel>();
                if (found != null)
                {
                    if (showEditPreview)
                    {
                        found.ShowEditPreview();
                    }
                    else
                    {
                        found.Hide();
                    }

                    return found;
                }
            }

            var legacy = parent.GetComponentInChildren<RunMapNodeInfoPanel>(true);
            if (legacy != null)
            {
                legacy.transform.SetParent(parent, false);
                legacy.gameObject.name = "NodeInfoSidebar";
                if (showEditPreview)
                {
                    legacy.ShowEditPreview();
                }
                else
                {
                    legacy.Hide();
                }

                return legacy;
            }

            var built = BuildSidebar(parent, showEditPreview);
            return built;
        }

        public static RunMapNodeInfoPanel Create(Transform parent) => EnsureSidebar(parent);

        private static RunMapNodeInfoPanel BuildSidebar(Transform parent, bool showEditPreview)
        {
            var root = new GameObject("NodeInfoSidebar", typeof(RectTransform), typeof(RunMapNodeInfoPanel));
            root.transform.SetParent(parent, false);
            StretchToSidebar(root.GetComponent<RectTransform>());
            root.transform.SetAsLastSibling();

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.06f, 0.07f, 0.09f, 0.92f);

            var titleGo = CreateText("Title", panel.transform, string.Empty, 22, FontStyle.Bold, TextAnchor.UpperLeft);
            Stretch(titleGo, 16f, -16f, -132f, -14f);

            var bodyGo = CreateText("Body", panel.transform, string.Empty, 16, FontStyle.Normal, TextAnchor.UpperLeft);
            Stretch(bodyGo, 16f, -16f, -84f, -136f);
            bodyGo.GetComponent<Text>().lineSpacing = 1.06f;

            var hintGo = CreateText("Hint", panel.transform, string.Empty, 13, FontStyle.Italic, TextAnchor.UpperLeft);
            Stretch(hintGo, 16f, -16f, -46f, -86f);
            hintGo.GetComponent<Text>().color = new Color(0.75f, 0.82f, 0.88f, 0.95f);

            var confirmGo = CreateButton("ConfirmButton", panel.transform, "Travel", new Vector2(-16f, 14f), new Vector2(118f, 32f));
            var closeGo = CreateButton("CloseButton", panel.transform, "×", new Vector2(16f, 14f), new Vector2(32f, 32f));

            var view = root.GetComponent<RunMapNodeInfoPanel>();
            view.Wire(
                panelRect,
                titleGo.GetComponent<Text>(),
                bodyGo.GetComponent<Text>(),
                hintGo.GetComponent<Text>(),
                confirmGo.GetComponent<Button>(),
                closeGo.GetComponent<Button>(),
                confirmGo.transform.Find("Label")?.GetComponent<Text>());

            if (showEditPreview)
            {
                view.ShowEditPreview();
            }
            else
            {
                root.SetActive(false);
            }

            return view;
        }

        private static void StretchToSidebar(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = SidebarAnchorMin;
            rect.anchorMax = SidebarAnchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static GameObject CreateText(
            string name,
            Transform parent,
            string content,
            int fontSize,
            FontStyle style,
            TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = UiFontCatalog.Body;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return go;
        }

        private static GameObject CreateButton(string name, Transform parent, string label, Vector2 offset, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = name == "CloseButton"
                ? new Color(0.18f, 0.2f, 0.24f, 0.95f)
                : new Color(0.12f, 0.72f, 0.82f, 1f);

            var labelGo = CreateText("Label", go.transform, label, name == "CloseButton" ? 20 : 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(labelGo, 0f, 0f, 0f, 0f);
            return go;
        }

        private static void Stretch(GameObject go, float left, float right, float bottom, float top)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
        }
    }
}

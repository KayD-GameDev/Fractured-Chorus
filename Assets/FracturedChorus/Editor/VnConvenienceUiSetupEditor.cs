#if UNITY_EDITOR
using FracturedChorus.Narrative;
using FracturedChorus.Narrative.Vn;
using FracturedChorus.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class VnConvenienceUiSetupEditor
    {
        public static VnConvenienceController EnsureConvenienceUi(Transform canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            var existing = canvas.GetComponentInChildren<VnConvenienceController>(true);
            if (existing != null)
            {
                return existing;
            }

            var root = CreateUiObject("VnConvenienceRoot", canvas);
            StretchRect(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var barRoot = CreateUiObject("ConvenienceBar", root.transform);
            var barRect = barRoot.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(1f, 0f);
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(1f, 0f);
            barRect.anchoredPosition = new Vector2(-24f, 24f);
            barRect.sizeDelta = new Vector2(320f, 44f);

            var barLayout = barRoot.AddComponent<HorizontalLayoutGroup>();
            barLayout.childAlignment = TextAnchor.MiddleRight;
            barLayout.spacing = 10f;
            barLayout.childControlWidth = true;
            barLayout.childControlHeight = true;
            barLayout.childForceExpandWidth = false;
            barLayout.childForceExpandHeight = true;

            var logButton = CreateBarButton(barRoot.transform, "LogButton", "LOG");
            var autoButton = CreateBarButton(barRoot.transform, "AutoButton", "AUTO");
            var skipButton = CreateBarButton(barRoot.transform, "SkipButton", "SKIP");

            var barView = barRoot.AddComponent<VnConvenienceBarView>();
            SetSerializedField(barView, "logButton", logButton);
            SetSerializedField(barView, "autoButton", autoButton);
            SetSerializedField(barView, "skipButton", skipButton);
            SetSerializedField(barView, "autoLabel", autoButton.GetComponentInChildren<Text>(true));
            SetSerializedField(barView, "skipLabel", skipButton.GetComponentInChildren<Text>(true));

            var logPanelRoot = CreateUiObject("LogPanel", root.transform);
            StretchRect(logPanelRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var logGroup = logPanelRoot.AddComponent<CanvasGroup>();
            logGroup.alpha = 0f;
            logGroup.blocksRaycasts = false;
            logGroup.interactable = false;
            logPanelRoot.SetActive(false);

            var backdrop = CreateImage("Backdrop", logPanelRoot.transform, null, new Color(0f, 0f, 0f, 0.72f));
            StretchRect(backdrop.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var backdropButton = backdrop.gameObject.AddComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;

            var panel = CreateImage("Panel", logPanelRoot.transform, null, new Color(0.04f, 0.08f, 0.16f, 0.94f));
            panel.raycastTarget = false;
            StretchRect(panel.gameObject, new Vector2(0.12f, 0.1f), new Vector2(0.88f, 0.9f), Vector2.zero, Vector2.zero);

            var closeButton = CreateBarButton(panel.transform, "CloseButton", "CLOSE");
            var closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-12f, -12f);
            closeRect.sizeDelta = new Vector2(120f, 36f);

            var scrollGo = CreateUiObject("Scroll", panel.transform);
            StretchRect(scrollGo, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.88f), Vector2.zero, Vector2.zero);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewport = CreateUiObject("Viewport", scrollGo.transform);
            StretchRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = Color.clear;

            var content = CreateUiObject("Content", viewport.transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            var bodyGo = CreateUiObject("Body", content.transform);
            var bodyRect = bodyGo.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.anchoredPosition = Vector2.zero;
            bodyRect.sizeDelta = new Vector2(0f, 48f);
            var bodyText = bodyGo.AddComponent<Text>();
            VnUiFont.Apply(bodyText, 26, FontStyle.Normal);
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.color = new Color(0.9f, 0.95f, 1f, 1f);
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            bodyText.raycastTarget = false;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var bodyFitter = bodyGo.AddComponent<ContentSizeFitter>();
            bodyFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRect;

            var logView = logPanelRoot.AddComponent<VnLogPanelView>();
            SetSerializedField(logView, "root", logGroup);
            SetSerializedField(logView, "bodyText", bodyText);
            SetSerializedField(logView, "scrollRect", scrollRect);
            SetSerializedField(logView, "closeButton", closeButton);
            SetSerializedField(logView, "backdropButton", backdropButton);
            SetSerializedField(logView, "panelBackground", panel);

            var controller = root.AddComponent<VnConvenienceController>();
            SetSerializedField(controller, "bar", barView);
            SetSerializedField(controller, "logPanel", logView);

            var fade = canvas.Find("FadeOverlay");
            if (fade != null)
            {
                root.transform.SetSiblingIndex(fade.GetSiblingIndex());
            }

            barRoot.transform.SetAsLastSibling();

            return controller;
        }

        [MenuItem("Fractured Chorus/Narrative/Ensure VN Convenience UI In Active Scene")]
        public static void EnsureInActiveScene()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] No Canvas in active scene.");
                return;
            }

            var controller = EnsureConvenienceUi(canvas.transform);
            var runtime = Object.FindAnyObjectByType<VnRuntimeController>();
            if (runtime != null)
            {
                SetSerializedField(runtime, "convenience", controller);
                EditorUtility.SetDirty(runtime);
            }

            var prologue = Object.FindAnyObjectByType<PrologueVNController>();
            if (prologue != null)
            {
                SetSerializedField(prologue, "convenience", controller);
                EditorUtility.SetDirty(prologue);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] VN Convenience UI ensured in active scene.");
        }

        private static Button CreateBarButton(Transform parent, string name, string label)
        {
            var go = CreateUiObject(name, parent);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.08f, 0.16f, 0.28f, 0.82f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var layout = go.AddComponent<LayoutElement>();
            layout.minWidth = 92f;
            layout.preferredWidth = 92f;
            layout.minHeight = 40f;
            layout.preferredHeight = 40f;

            var text = CreateDisplayText("Label", go.transform, label, 22, TextAnchor.MiddleCenter);
            StretchRect(text.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.color = new Color(0.82f, 0.92f, 1f, 0.92f);
            return button;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = CreateUiObject(name, parent);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        private static Text CreateDisplayText(string name, Transform parent, string content, int fontSize, TextAnchor anchor)
        {
            var go = CreateUiObject(name, parent);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.alignment = anchor;
            text.color = Color.white;
            UiFontCatalog.Apply(text, UiFontRole.Display, fontSize);
            return text;
        }

        private static Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor anchor)
        {
            var go = CreateUiObject(name, parent);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.alignment = anchor;
            text.color = Color.white;
            VnUiFont.Apply(text, fontSize, FontStyle.Normal);
            return text;
        }

        private static void StretchRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetSerializedField(Object target, string fieldName, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                return;
            }

            switch (value)
            {
                case null:
                    prop.objectReferenceValue = null;
                    break;
                case Object obj:
                    prop.objectReferenceValue = obj;
                    break;
                case string s:
                    prop.stringValue = s;
                    break;
                case bool b:
                    prop.boolValue = b;
                    break;
                case int i:
                    prop.intValue = i;
                    break;
                case float f:
                    prop.floatValue = f;
                    break;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif

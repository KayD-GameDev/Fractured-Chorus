#if UNITY_EDITOR
using FracturedChorus.Combat.Timeline;
using FracturedChorus.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class TimelineHierarchyBuilder
    {
        public const float SlotWidth = 52f;
        public const float SlotHeight = 64f;

        public static BeatTimelineUIView BuildTimeline(Transform canvasTransform)
        {
            var existing = canvasTransform.Find("BeatTimelineUI");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var timelineGo = CreateUiObject("BeatTimelineUI", canvasTransform);
            var rootRect = timelineGo.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.02f, 0.02f);
            rootRect.anchorMax = new Vector2(0.98f, 0.22f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            timelineGo.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

            CreateTimelineHeader(timelineGo.transform);
            var viewport = CreateViewport(timelineGo.transform, out var scrollContent, out var scanBar, out var segmentTemplate);

            var ui = timelineGo.AddComponent<BeatTimelineUIView>();
            SetField(ui, "viewport", viewport);
            SetField(ui, "slotsRow", scrollContent);
            SetField(ui, "segmentTemplate", segmentTemplate);
            SetField(ui, "scanBar", scanBar);
            SetField(ui, "slotWidth", SlotWidth);
            ui.WireReferences();
            return ui;
        }

        public static SkillPanelUIView BuildSkillPanel(Transform canvasTransform)
        {
            var existing = canvasTransform.Find("SkillPanelUI");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var panelGo = CreateUiObject("SkillPanelUI", canvasTransform);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(180f, 220f);
            panelGo.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

            var titleGo = CreateUiObject("Title", panelGo.transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -8f);
            titleRect.sizeDelta = new Vector2(-16f, 28f);
            var title = titleGo.AddComponent<Text>();
            ApplyText(title);
            title.fontStyle = FontStyle.Bold;
            title.text = "Skills";

            var buttonsGo = CreateUiObject("Buttons", panelGo.transform);
            var buttonsRect = buttonsGo.GetComponent<RectTransform>();
            StretchWithPadding(buttonsRect, 0f, 0f, 1f, 1f);
            buttonsRect.offsetMin = new Vector2(8f, 8f);
            buttonsRect.offsetMax = new Vector2(-8f, -40f);
            var layout = buttonsGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;

            panelGo.SetActive(false);

            var ui = panelGo.AddComponent<SkillPanelUIView>();
            SetField(ui, "panelRect", panelRect);
            SetField(ui, "buttonContainer", buttonsRect);
            SetField(ui, "titleLabel", title);
            SetField(ui, "screenPaddingPx", 1.5f);
            ui.WireReferences();
            return ui;
        }

        private static RectTransform CreateViewport(Transform parent, out RectTransform scrollContent,
            out RectTransform scanBar, out BeatSegmentView segmentTemplate)
        {
            var viewportGo = CreateUiObject("Viewport", parent);
            var viewportRect = viewportGo.GetComponent<RectTransform>();
            StretchWithPadding(viewportRect, 0f, 0f, 1f, 1f);
            viewportRect.offsetMin = new Vector2(120f, 8f);
            viewportRect.offsetMax = new Vector2(-8f, -8f);
            var viewportBg = viewportGo.AddComponent<Image>();
            viewportBg.color = new Color(0f, 0f, 0f, 0.25f);
            viewportGo.AddComponent<RectMask2D>();

            var scrollGo = CreateUiObject("ScrollContent", viewportGo.transform);
            scrollContent = scrollGo.GetComponent<RectTransform>();
            scrollContent.anchorMin = new Vector2(0f, 0f);
            scrollContent.anchorMax = new Vector2(0f, 1f);
            scrollContent.pivot = new Vector2(0f, 0.5f);
            scrollContent.anchoredPosition = Vector2.zero;
            scrollContent.offsetMin = Vector2.zero;
            scrollContent.offsetMax = Vector2.zero;

            var layout = scrollGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 2f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            segmentTemplate = CreateBeatSegment(scrollGo.transform, 0);

            var scanGo = CreateUiObject("ScanBar", viewportGo.transform);
            scanBar = scanGo.GetComponent<RectTransform>();
            scanBar.anchorMin = new Vector2(0f, 0f);
            scanBar.anchorMax = new Vector2(0f, 1f);
            scanBar.pivot = new Vector2(0.5f, 0.5f);
            scanBar.sizeDelta = new Vector2(6f, -4f);
            scanBar.anchoredPosition = new Vector2(SlotWidth * 0.5f, 0f);
            var scanImg = scanGo.AddComponent<Image>();
            scanImg.color = new Color(1f, 0.15f, 0.1f, 0.85f);

            var trackGo = CreateUiObject("TrackLine", viewportGo.transform);
            trackGo.transform.SetAsFirstSibling();
            var trackRect = trackGo.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 0f);
            trackRect.anchorMax = new Vector2(1f, 0f);
            trackRect.pivot = new Vector2(0.5f, 0f);
            trackRect.anchoredPosition = new Vector2(0f, 6f);
            trackRect.sizeDelta = new Vector2(0f, 2f);
            trackGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.14f);

            return viewportRect;
        }

        private static BeatSegmentView CreateBeatSegment(Transform parent, int index)
        {
            var segGo = CreateUiObject($"Beat_{index}", parent);
            var segRect = segGo.GetComponent<RectTransform>();
            segRect.sizeDelta = new Vector2(SlotWidth, SlotHeight);
            segGo.AddComponent<LayoutElement>().preferredWidth = SlotWidth;
            segGo.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 0.85f);

            var glowGo = CreateUiObject("Glow", segGo.transform);
            StretchWithPadding(glowGo.GetComponent<RectTransform>(), 0.05f, 0.1f, 0.95f, 0.9f);
            glowGo.AddComponent<Image>().color = new Color(1f, 0.2f, 0.2f, 0.15f);

            var portraitGo = CreateUiObject("Portrait", segGo.transform);
            var portraitRect = portraitGo.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0f, 0.5f);
            portraitRect.anchorMax = new Vector2(0f, 0.5f);
            portraitRect.pivot = new Vector2(0f, 0.5f);
            portraitRect.anchoredPosition = new Vector2(4f, 0f);
            portraitRect.sizeDelta = new Vector2(24f, 24f);
            portraitGo.AddComponent<Image>().color = new Color(0.4f, 0.4f, 0.5f, 1f);

            var labelGo = CreateUiObject("ActionLabel", segGo.transform);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(4f, 4f);
            labelRect.offsetMax = new Vector2(-4f, -4f);
            var label = labelGo.AddComponent<Text>();
            ApplyText(label);
            label.fontSize = 10;
            label.fontStyle = FontStyle.Italic;
            label.alignment = TextAnchor.LowerCenter;

            if (TimelineConstants.IsPhaseDividerAfter(index))
            {
                CreatePhaseDivider(segGo.transform);
            }
            else if (index == 0)
            {
                CreatePhaseDivider(segGo.transform);
            }

            var segment = segGo.AddComponent<BeatSegmentView>();
            segment.SetDisplayBeatIndex(index);
            segment.WireReferences();
            return segment;
        }

        private static void CreatePhaseDivider(Transform parent)
        {
            var divGo = CreateUiObject("PhaseDivider", parent);
            var divRect = divGo.GetComponent<RectTransform>();
            divRect.anchorMin = new Vector2(1f, 0f);
            divRect.anchorMax = new Vector2(1f, 1f);
            divRect.pivot = new Vector2(0.5f, 0.5f);
            divRect.sizeDelta = new Vector2(3f, 0f);
            divRect.anchoredPosition = new Vector2(2f, 0f);
            divGo.AddComponent<Image>().color = Color.white;
        }

        private static void CreateTimelineHeader(Transform parent)
        {
            var headerGo = CreateUiObject("Header", parent);
            var headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 0f);
            headerRect.anchorMax = new Vector2(0f, 1f);
            headerRect.pivot = new Vector2(0f, 0.5f);
            headerRect.sizeDelta = new Vector2(110f, 0f);

            var clefGo = CreateUiObject("Clef", headerGo.transform);
            var clefRect = clefGo.GetComponent<RectTransform>();
            clefRect.anchorMin = new Vector2(0f, 0.5f);
            clefRect.anchorMax = new Vector2(0f, 0.5f);
            clefRect.anchoredPosition = new Vector2(12f, 0f);
            clefRect.sizeDelta = new Vector2(24f, 48f);
            var clefText = clefGo.AddComponent<Text>();
            ApplyText(clefText);
            clefText.text = "\u266A";
            clefText.fontSize = 28;

            var budgetGo = CreateUiObject("Budget", headerGo.transform);
            var budgetRect = budgetGo.GetComponent<RectTransform>();
            budgetRect.anchorMin = new Vector2(0f, 0.5f);
            budgetRect.anchorMax = new Vector2(0f, 0.5f);
            budgetRect.anchoredPosition = new Vector2(58f, 8f);
            budgetRect.sizeDelta = new Vector2(36f, 28f);
            budgetGo.AddComponent<Image>().color = new Color(0.8f, 0.2f, 0.6f, 0.8f);
            var budgetTextGo = CreateUiObject("BudgetText", budgetGo.transform);
            StretchFull(budgetTextGo.GetComponent<RectTransform>());
            var budgetText = budgetTextGo.AddComponent<Text>();
            ApplyText(budgetText);
            budgetText.text = "1/10";

            var avGo = CreateUiObject("AvLabel", headerGo.transform);
            var avRect = avGo.GetComponent<RectTransform>();
            avRect.anchorMin = new Vector2(0f, 0.5f);
            avRect.anchorMax = new Vector2(0f, 0.5f);
            avRect.anchoredPosition = new Vector2(58f, -16f);
            avRect.sizeDelta = new Vector2(96f, 20f);
            var avText = avGo.AddComponent<Text>();
            ApplyText(avText);
            avText.fontSize = 11;
            avText.alignment = TextAnchor.MiddleLeft;
            avText.horizontalOverflow = HorizontalWrapMode.Overflow;
            avText.text = "AV 150/150";

            var phaseGo = CreateUiObject("PhaseLabel", headerGo.transform);
            var phaseRect = phaseGo.GetComponent<RectTransform>();
            phaseRect.anchorMin = new Vector2(0f, 1f);
            phaseRect.anchorMax = new Vector2(0f, 1f);
            phaseRect.pivot = new Vector2(0f, 1f);
            phaseRect.anchoredPosition = new Vector2(0f, 4f);
            phaseRect.sizeDelta = new Vector2(110f, 18f);
            var phaseText = phaseGo.AddComponent<Text>();
            ApplyText(phaseText);
            phaseText.fontSize = 11;
            phaseText.alignment = TextAnchor.MiddleLeft;
            phaseText.text = "PHASE";
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void StretchWithPadding(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ApplyText(Text text)
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
        }

        private static void SetField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetField(Object target, string fieldName, float value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.floatValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetField(Object target, string fieldName, bool value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.boolValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetField(Object target, string fieldName, BeatSegmentView value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif

using System;
using FracturedChorus.Data;
using FracturedChorus.RunMap.Core;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.RunMap.UI
{
    [ExecuteAlways]
    public class RunMapLegendPanelView : MonoBehaviour
    {
        private static readonly Vector2 StretchMin = Vector2.zero;
        private static readonly Vector2 StretchMax = Vector2.one;

        [SerializeField] private MapNodeIconSetSO iconSet;

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                Apply();
            }
        }
#endif

        private void Start()
        {
            if (Application.isPlaying)
            {
                Apply();
            }
        }

        public void Apply()
        {
            PruneDuplicateViews();
            PruneDuplicateLayoutGroups<VerticalLayoutGroup>();
            EnsurePanelLayout();

            ApplyTitle();
            ApplyHint();

            foreach (Transform child in transform)
            {
                if (!TryParseRowType(child.name, out var type))
                {
                    continue;
                }

                PruneRowLayoutDuplicates(child);
                EnsureRowLayout(child);
                ApplyRow(child, type);
            }

            if (transform is RectTransform panelRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
            }
        }

        private void PruneDuplicateViews()
        {
            var views = GetComponents<RunMapLegendPanelView>();
            for (var i = 1; i < views.Length; i++)
            {
                if (views[i] == null)
                {
                    continue;
                }

                DestroyLayoutComponent(views[i]);
            }
        }

        private void PruneDuplicateLayoutGroups<T>() where T : Component
        {
            var groups = GetComponents<T>();
            for (var i = 1; i < groups.Length; i++)
            {
                if (groups[i] == null)
                {
                    continue;
                }

                DestroyLayoutComponent(groups[i]);
            }
        }

        private static void PruneRowLayoutDuplicates(Transform row)
        {
            PruneDuplicates<HorizontalLayoutGroup>(row);
            PruneDuplicates<ContentSizeFitter>(row);
            PruneDuplicates<LayoutElement>(row);
        }

        private static void PruneDuplicates<T>(Transform target) where T : Component
        {
            var components = target.GetComponents<T>();
            for (var i = 1; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    continue;
                }

                DestroyLayoutComponent(components[i]);
            }
        }

        private static void DestroyLayoutComponent(Component component)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(component);
                return;
            }
#endif
            Destroy(component);
        }

        private void EnsurePanelLayout()
        {
            var vlg = GetComponent<VerticalLayoutGroup>() ?? gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(22, 22, 28, 22);
            vlg.spacing = MapLayoutConstants.LegendVerticalSpacing;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = false;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
        }

        private static void EnsureRowLayout(Transform row)
        {
            var rect = row as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
            }

            var le = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
            le.minHeight = MapLayoutConstants.LegendRowMinHeight;
            le.preferredHeight = -1f;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 0f;

            var fitter = row.GetComponent<ContentSizeFitter>() ?? row.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var hlg = row.GetComponent<HorizontalLayoutGroup>() ?? row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = MapLayoutConstants.LegendRowHorizontalSpacing;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.padding = new RectOffset(0, 0, 0, 0);
        }

        private void ApplyTitle()
        {
            var title = transform.Find("LegendTitle")?.GetComponent<Text>();
            if (title == null)
            {
                return;
            }

            title.fontSize = MapLayoutConstants.LegendTitleFontSize;
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.92f, 0.94f, 0.96f);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;

            var le = title.GetComponent<LayoutElement>() ?? title.gameObject.AddComponent<LayoutElement>();
            le.minHeight = MapLayoutConstants.LegendTitleHeight;
            le.preferredHeight = -1f;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 0f;

            var fitter = title.GetComponent<ContentSizeFitter>() ?? title.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var titleRect = title.transform as RectTransform;
            if (titleRect != null)
            {
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = new Vector2(0f, 1f);
                titleRect.pivot = new Vector2(0f, 1f);
                titleRect.anchoredPosition = Vector2.zero;
            }
        }

        private void ApplyHint()
        {
            var hint = transform.Find("Hint")?.GetComponent<Text>();
            if (hint == null)
            {
                return;
            }

            hint.text = "Bấm tên để xem thông tin.";
            hint.fontSize = MapLayoutConstants.LegendHintFontSize;
            hint.color = new Color(0.62f, 0.65f, 0.7f);
            hint.lineSpacing = MapLayoutConstants.LegendHintLineSpacing;
            hint.horizontalOverflow = HorizontalWrapMode.Wrap;
            hint.verticalOverflow = VerticalWrapMode.Overflow;

            var le = hint.GetComponent<LayoutElement>() ?? hint.gameObject.AddComponent<LayoutElement>();
            le.minHeight = MapLayoutConstants.LegendHintMinHeight;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 0f;

            var fitter = hint.GetComponent<ContentSizeFitter>() ?? hint.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var hintRect = hint.transform as RectTransform;
            if (hintRect != null)
            {
                hintRect.anchorMin = new Vector2(0f, 1f);
                hintRect.anchorMax = new Vector2(0f, 1f);
                hintRect.pivot = new Vector2(0f, 1f);
                hintRect.anchoredPosition = Vector2.zero;
            }
        }

        private void ApplyRow(Transform row, MapNodeType type)
        {
            var desc = row.Find("Desc")?.GetComponent<Text>();
            if (desc != null)
            {
                desc.text = MapNodeCatalog.Title(type);
                desc.fontSize = MapLayoutConstants.LegendDescFontSize;
                desc.color = new Color(0.88f, 0.9f, 0.93f);
                desc.horizontalOverflow = HorizontalWrapMode.Overflow;
                desc.verticalOverflow = VerticalWrapMode.Overflow;
                desc.raycastTarget = false;

                var le = desc.GetComponent<LayoutElement>() ?? desc.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 0f;
                le.preferredHeight = -1f;
                le.flexibleWidth = 0f;
                le.flexibleHeight = 0f;

                var fitter = desc.GetComponent<ContentSizeFitter>() ?? desc.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var descRect = desc.transform as RectTransform;
                if (descRect != null)
                {
                    descRect.anchorMin = new Vector2(0f, 0.5f);
                    descRect.anchorMax = new Vector2(0f, 0.5f);
                    descRect.pivot = new Vector2(0f, 0.5f);
                    descRect.anchoredPosition = Vector2.zero;
                    descRect.sizeDelta = Vector2.zero;
                }
            }

            var dot = row.Find("Dot");
            if (dot == null)
            {
                dot = CreateSwatchRoot(row);
            }

            dot.gameObject.SetActive(true);
            ConfigureSwatch(dot, type);

            WireRowClick(row, type);
        }

        private Transform CreateSwatchRoot(Transform row)
        {
            var go = new GameObject("Dot", typeof(RectTransform));
            go.transform.SetParent(row, false);
            go.transform.SetAsFirstSibling();

            var rect = go.GetComponent<RectTransform>();
            var diameter = MapLayoutConstants.LegendDotSize;
            rect.sizeDelta = new Vector2(diameter, diameter);

            var le = go.AddComponent<LayoutElement>();
            le.minWidth = diameter;
            le.minHeight = diameter;
            le.preferredWidth = diameter;
            le.preferredHeight = diameter;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;

            var fitter = go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fitter.aspectRatio = 1f;

            return go.transform;
        }

        private void ConfigureSwatch(Transform dot, MapNodeType type)
        {
            var diameter = type == MapNodeType.Start
                ? MapLayoutConstants.LegendDotSize * MapLayoutConstants.StartNodeScale
                : MapLayoutConstants.LegendDotSize;

            var rect = dot as RectTransform ?? dot.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(diameter, diameter);
            }

            var le = dot.GetComponent<LayoutElement>() ?? dot.gameObject.AddComponent<LayoutElement>();
            le.minWidth = diameter;
            le.minHeight = diameter;
            le.preferredWidth = diameter;
            le.preferredHeight = diameter;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;

            var legacyImage = dot.GetComponent<Image>();
            if (legacyImage != null)
            {
                legacyImage.enabled = false;
            }

            var stroke = EnsureImageChild(dot, "Stroke", StretchMin, StretchMax, Vector2.zero, Vector2.zero);
            var inset = diameter * (3f / MapLayoutConstants.NodeDiameter);
            var fill = EnsureImageChild(
                dot,
                "Fill",
                StretchMin,
                StretchMax,
                new Vector2(inset, inset),
                new Vector2(-inset, -inset));
            var icon = EnsureImageChild(dot, "Icon", StretchMin, StretchMax, Vector2.zero, Vector2.zero);

            var sprite = ResolveIconSet()?.Resolve(type, type == MapNodeType.Boss, PinkySectorId.Canticle);

            if (sprite != null)
            {
                stroke.enabled = false;
                fill.enabled = false;
                icon.enabled = true;
                icon.sprite = sprite;
                icon.color = Color.white;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                return;
            }

            stroke.enabled = true;
            fill.enabled = true;
            icon.enabled = false;

            stroke.sprite = UiCircleSpriteUtil.Circle;
            stroke.color = MapNodePalette.StrokeColor(type);
            stroke.raycastTarget = false;

            fill.sprite = UiCircleSpriteUtil.Circle;
            fill.color = MapNodePalette.FillColor(type);
            fill.raycastTarget = false;
        }

        private MapNodeIconSetSO ResolveIconSet()
        {
            if (iconSet != null)
            {
                return iconSet;
            }

#if UNITY_EDITOR
            iconSet = UnityEditor.AssetDatabase.LoadAssetAtPath<MapNodeIconSetSO>(
                "Assets/FracturedChorus/Data/ScriptableObjects/Presets/MapNodeIconSet_Default.asset");
#endif
            return iconSet;
        }

        private static Image EnsureImageChild(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var child = parent.Find(name);
            GameObject go;
            if (child == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = child.gameObject;
                if (go.GetComponent<Image>() == null)
                {
                    go.AddComponent<Image>();
                }
            }

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;

            return go.GetComponent<Image>();
        }

        private void WireRowClick(Transform row, MapNodeType type)
        {
            var graphic = row.GetComponent<Image>();
            if (graphic == null)
            {
                graphic = row.gameObject.AddComponent<Image>();
            }

            graphic.color = new Color(1f, 1f, 1f, 0.001f);
            graphic.raycastTarget = true;

            var button = row.GetComponent<Button>() ?? row.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.transition = Selectable.Transition.None;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ShowTypeInfo(type));
        }

        private static void ShowTypeInfo(MapNodeType type)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var panel = UnityEngine.Object.FindAnyObjectByType<RunMapNodeInfoPanel>(FindObjectsInactive.Include);
            if (panel == null)
            {
                return;
            }

            panel.ShowTypeInfo(type);
        }

        private static bool TryParseRowType(string rowName, out MapNodeType type)
        {
            type = MapNodeType.Battle;
            if (string.IsNullOrEmpty(rowName))
            {
                return false;
            }

            if (rowName == "LegendSpacer")
            {
                return false;
            }

            if (!rowName.StartsWith("Legend_", StringComparison.Ordinal))
            {
                return false;
            }

            return Enum.TryParse(rowName.Substring("Legend_".Length), out type);
        }
    }
}

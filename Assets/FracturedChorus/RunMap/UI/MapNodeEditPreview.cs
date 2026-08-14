using FracturedChorus.Data;
using FracturedChorus.RunMap.Core;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FracturedChorus.RunMap.UI
{
    [ExecuteAlways]
    public sealed class MapNodeEditPreview : MonoBehaviour
    {
        private static readonly MapNodeType[] PreviewTypes =
        {
            MapNodeType.Start,
            MapNodeType.Battle,
            MapNodeType.Elite,
            MapNodeType.Treasure,
            MapNodeType.Event,
            MapNodeType.Camp,
            MapNodeType.Relay,
            MapNodeType.Boss
        };

        [SerializeField] private MapNodeIconSetSO iconSet;
        [SerializeField] private float previewSize = 96f;
        [SerializeField] private float spacing = 18f;
        [SerializeField] private float labelFontSize = 16f;

        private RectTransform _strip;

        public void SetIconSet(MapNodeIconSetSO set)
        {
            iconSet = set;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            Rebuild();
        }

        public void Hide()
        {
            ClearStrip();
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        public void Rebuild()
        {
            if (iconSet == null)
            {
#if UNITY_EDITOR
                iconSet = AssetDatabase.LoadAssetAtPath<MapNodeIconSetSO>(
                    "Assets/FracturedChorus/Data/ScriptableObjects/Presets/MapNodeIconSet_Default.asset");
#endif
            }

            EnsureStrip();
            ClearStripChildren();

            var x = 0f;
            foreach (var type in PreviewTypes)
            {
                var diameter = type switch
                {
                    MapNodeType.Boss => previewSize * 1.35f,
                    MapNodeType.Start => previewSize,
                    _ => previewSize
                };
                CreatePreviewCell(type, x, diameter);
                x += diameter + spacing;
            }

            _strip.sizeDelta = new Vector2(x - spacing, previewSize * 1.55f);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        private void EnsureStrip()
        {
            if (_strip != null)
            {
                return;
            }

            var existing = transform.Find("PreviewStrip") as RectTransform;
            if (existing != null)
            {
                _strip = existing;
                return;
            }

            var go = new GameObject("PreviewStrip", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            _strip = go.GetComponent<RectTransform>();
            _strip.anchorMin = new Vector2(0.5f, 0.55f);
            _strip.anchorMax = new Vector2(0.5f, 0.55f);
            _strip.pivot = new Vector2(0.5f, 0.5f);
            _strip.anchoredPosition = Vector2.zero;
        }

        private void ClearStrip()
        {
            if (_strip == null)
            {
                return;
            }

            ClearStripChildren();
        }

        private void ClearStripChildren()
        {
            if (_strip == null)
            {
                return;
            }

            for (var i = _strip.childCount - 1; i >= 0; i--)
            {
                var child = _strip.GetChild(i).gameObject;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(child);
                    continue;
                }
#endif
                Destroy(child);
            }
        }

        private void CreatePreviewCell(MapNodeType type, float x, float diameter)
        {
            var cell = new GameObject(type.ToString(), typeof(RectTransform));
            cell.transform.SetParent(_strip, false);
            var cellRect = cell.GetComponent<RectTransform>();
            cellRect.anchorMin = new Vector2(0f, 0.5f);
            cellRect.anchorMax = new Vector2(0f, 0.5f);
            cellRect.pivot = new Vector2(0f, 0.5f);
            cellRect.anchoredPosition = new Vector2(x, 0f);
            cellRect.sizeDelta = new Vector2(diameter, diameter + 28f);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(cell.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(diameter, diameter);

            var icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var sprite = iconSet != null
                ? iconSet.Resolve(type, type == MapNodeType.Boss, PinkySectorId.Pulse)
                : null;
            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.color = Color.white;
            }
            else
            {
                icon.sprite = UiCircleSpriteUtil.Circle;
                icon.color = MapNodePalette.FillColor(type);
            }

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(cell.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(0f, 24f);

            var label = labelGo.GetComponent<Text>();
            label.text = MapNodePalette.DisplayName(type);
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = Mathf.RoundToInt(labelFontSize);
            label.color = Color.white;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            if (label.font == null)
            {
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (label.font == null)
                {
                    label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
            }
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (!Application.isPlaying && gameObject.activeInHierarchy)
            {
                EditorApplication.delayCall += RebuildDeferred;
            }
        }

        private void RebuildDeferred()
        {
            if (this == null || Application.isPlaying || !gameObject.activeInHierarchy)
            {
                return;
            }

            Rebuild();
        }
#endif
    }
}

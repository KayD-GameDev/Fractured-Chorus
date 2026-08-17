using System;
using FracturedChorus.Data;
using FracturedChorus.RunMap.Core;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.RunMap.UI
{
    public class MapNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const string RenAvatarPath =
            "Assets/FracturedChorus/Art/UI/Combat/Timeline/LeftRail/Avatars/ren_chibi_avatar_v1.png";

        [SerializeField] private Image fillImage;
        [SerializeField] private Image strokeImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image selectionFrameImage;
        [SerializeField] private Image playerMarkerImage;
        [SerializeField] private Text labelText;
        [SerializeField] private Button button;
        [SerializeField] private MapNodeIconSetSO iconSet;
        [SerializeField] private Sprite playerMarkerSprite;

        private const float HoverScale = 1.12f;
        private const float ClickPulseScale = 1.24f;
        private const float ClickPulseSeconds = 0.16f;
        private const float ScaleLerp = 16f;

        private Image _hitImage;
        private PinkySectorId _sector = PinkySectorId.Pulse;
        private static Sprite s_renAvatar;
        private bool _hovered;
        private float _clickPulse;
        private Color _iconBaseColor = Color.white;

        public MapNodeData BoundNode { get; private set; }
        public bool SuppressPlayerMarker { get; set; }
        public event Action<MapNodeView> Clicked;

        private void Awake()
        {
            if (GetComponent<MapNodeScrollForwarder>() == null)
            {
                gameObject.AddComponent<MapNodeScrollForwarder>();
            }

            button ??= GetComponent<Button>();
            EnsureHitTarget();
            EnsureCircleSprites();
            EnsureIconImage();
            EnsureSelectionFrame();
            EnsurePlayerMarker();
            DisableChildRaycastsExceptButtonTarget();

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDisable()
        {
            _hovered = false;
            _clickPulse = 0f;
            transform.localScale = Vector3.one;
        }

        private void LateUpdate()
        {
            var hover = _hovered && CanHover() ? HoverScale : 1f;
            var pulse = _clickPulse > 0f
                ? Mathf.Lerp(1f, ClickPulseScale, Mathf.Clamp01(_clickPulse))
                : 1f;
            var target = Mathf.Max(hover, pulse);
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                Vector3.one * target,
                1f - Mathf.Exp(-ScaleLerp * Time.unscaledDeltaTime));

            if (_clickPulse > 0f)
            {
                _clickPulse -= Time.unscaledDeltaTime / ClickPulseSeconds;
                if (iconImage != null && iconImage.enabled)
                {
                    var flash = Mathf.Clamp01(_clickPulse);
                    iconImage.color = Color.Lerp(_iconBaseColor, FcColorTokens.Brand.CyanNeonCore, flash);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = CanHover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
        }

        private void HandleClick()
        {
            if (CanHover())
            {
                _clickPulse = 1f;
            }

            Clicked?.Invoke(this);
        }

        private bool CanHover() =>
            BoundNode != null && !BoundNode.Cleared && (button == null || button.interactable);

        public void Configure(MapNodeIconSetSO set, PinkySectorId sector, Sprite renAvatar = null)
        {
            iconSet = set;
            _sector = sector;
            if (renAvatar != null)
            {
                playerMarkerSprite = renAvatar;
            }
        }

        private void EnsureHitTarget()
        {
            _hitImage = GetComponent<Image>();
            if (_hitImage == null)
            {
                _hitImage = gameObject.AddComponent<Image>();
            }

            _hitImage.color = new Color(1f, 1f, 1f, 0.001f);
            _hitImage.raycastTarget = true;
            if (button != null)
            {
                button.targetGraphic = _hitImage;
            }
        }

        private void DisableChildRaycastsExceptButtonTarget()
        {
            if (strokeImage != null)
            {
                strokeImage.raycastTarget = false;
            }

            if (iconImage != null)
            {
                iconImage.raycastTarget = false;
            }

            if (selectionFrameImage != null)
            {
                selectionFrameImage.raycastTarget = false;
            }

            if (playerMarkerImage != null)
            {
                playerMarkerImage.raycastTarget = false;
            }

            if (labelText != null)
            {
                labelText.raycastTarget = false;
            }
        }

        private void EnsureCircleSprites()
        {
            if (fillImage != null && fillImage.sprite == null)
            {
                fillImage.sprite = UiCircleSpriteUtil.Circle;
            }

            if (strokeImage != null && strokeImage.sprite == null)
            {
                strokeImage.sprite = UiCircleSpriteUtil.Circle;
            }
        }

        private void EnsureIconImage()
        {
            if (iconImage != null)
            {
                return;
            }

            var existing = transform.Find("Icon");
            if (existing != null)
            {
                iconImage = existing.GetComponent<Image>();
                if (iconImage != null)
                {
                    return;
                }
            }

            var go = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            if (labelText != null)
            {
                go.transform.SetSiblingIndex(labelText.transform.GetSiblingIndex());
            }

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            iconImage = go.GetComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.enabled = false;
        }

        private void EnsureSelectionFrame()
        {
            if (selectionFrameImage != null)
            {
                return;
            }

            var existing = transform.Find("SelectionFrame");
            if (existing != null)
            {
                selectionFrameImage = existing.GetComponent<Image>();
                if (selectionFrameImage != null)
                {
                    return;
                }
            }

            var go = new GameObject("SelectionFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-8f, -8f);
            rect.offsetMax = new Vector2(8f, 8f);
            selectionFrameImage = go.GetComponent<Image>();
            selectionFrameImage.sprite = UiCircleSpriteUtil.Circle;
            selectionFrameImage.type = Image.Type.Sliced;
            selectionFrameImage.color = new Color(1f, 1f, 1f, 0.95f);
            selectionFrameImage.raycastTarget = false;
            selectionFrameImage.enabled = false;
        }

        private void EnsurePlayerMarker()
        {
            if (playerMarkerImage != null)
            {
                return;
            }

            var existing = transform.Find("PlayerMarker");
            if (existing != null)
            {
                playerMarkerImage = existing.GetComponent<Image>();
                if (playerMarkerImage != null)
                {
                    return;
                }
            }

            var go = new GameObject("PlayerMarker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetAsLastSibling();
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 8f);
            rect.sizeDelta = new Vector2(32f, 32f);
            playerMarkerImage = go.GetComponent<Image>();
            playerMarkerImage.preserveAspect = true;
            playerMarkerImage.raycastTarget = false;
            playerMarkerImage.enabled = false;
        }

        public void Bind(MapNodeData node)
        {
            BoundNode = node;
            RefreshVisual();
        }

        public void RefreshVisual(bool reachable = false, bool onPath = false, bool current = false, bool selected = false)
        {
            if (BoundNode == null)
            {
                return;
            }

            EnsureIconImage();
            EnsureSelectionFrame();
            EnsurePlayerMarker();

            var sprite = ResolveNodeSprite();
            var useIcon = sprite != null;

            var fill = MapNodePalette.FillColor(BoundNode.Type);
            var stroke = MapNodePalette.StrokeColor(BoundNode.Type);
            var iconColor = Color.white;
            var alpha = BoundNode.Cleared ? 0.35f : 1f;

            if (BoundNode.Cleared)
            {
                iconColor = new Color(0.72f, 0.74f, 0.78f, 1f);
            }
            else if (!BoundNode.Cleared && current && !SuppressPlayerMarker)
            {
                stroke = FcColorTokens.Brand.CyanNeonBody;
                iconColor = Color.white;
            }
            else if (!BoundNode.Cleared && onPath)
            {
                stroke = FcColorTokens.WithAlpha(FcColorTokens.Brand.CyanNeonCore, 0.85f);
                if (BoundNode.Type != MapNodeType.Start)
                {
                    iconColor = new Color(0.85f, 0.95f, 1f, 1f);
                }
            }

            fill.a *= alpha;
            stroke.a *= alpha;
            iconColor.a = alpha;

            if (useIcon)
            {
                if (fillImage != null)
                {
                    fillImage.enabled = false;
                }

                if (strokeImage != null)
                {
                    strokeImage.enabled = false;
                }

                iconImage.enabled = true;
                iconImage.sprite = sprite;
                iconImage.color = iconColor;
                iconImage.preserveAspect = true;
                var iconScale = BoundNode.Type == MapNodeType.Start ? 1.08f : 1f;
                iconImage.rectTransform.localScale = Vector3.one * iconScale;

                if (labelText != null)
                {
                    labelText.enabled = false;
                }
            }
            else
            {
                if (fillImage != null)
                {
                    fillImage.enabled = true;
                    fillImage.color = fill;
                }

                if (strokeImage != null)
                {
                    strokeImage.enabled = true;
                    strokeImage.color = stroke;
                }

                if (iconImage != null)
                {
                    iconImage.enabled = false;
                }

                if (labelText != null)
                {
                    labelText.enabled = true;
                    labelText.text = MapNodePalette.Label(BoundNode.Type);
                    labelText.color = BoundNode.Type == MapNodeType.Boss
                        ? Color.white
                        : new Color(0.2f, 0.2f, 0.25f, alpha);
                    labelText.fontSize = MapLayoutConstants.NodeLabelFontSize(BoundNode.Type, BoundNode.IsBoss);
                    labelText.fontStyle = BoundNode.IsBoss ? FontStyle.Bold : FontStyle.Normal;
                }
            }

            if (selectionFrameImage != null)
            {
                selectionFrameImage.enabled = false;
            }

            _iconBaseColor = iconColor;

            if (playerMarkerImage != null)
            {
                var marker = ResolvePlayerMarkerSprite();
                var showMarker = current && marker != null && !SuppressPlayerMarker;
                playerMarkerImage.enabled = showMarker;
                if (showMarker)
                {
                    playerMarkerImage.sprite = marker;
                    playerMarkerImage.color = Color.white;
                }
            }

            if (button != null)
            {
                button.interactable = !BoundNode.Cleared;
            }
        }

        private Sprite ResolveNodeSprite()
        {
            if (BoundNode == null)
            {
                return null;
            }

            return iconSet != null
                ? iconSet.Resolve(BoundNode.Type, BoundNode.IsBoss, _sector)
                : null;
        }

        private Sprite ResolvePlayerMarkerSprite()
        {
            if (playerMarkerSprite != null)
            {
                return playerMarkerSprite;
            }

#if UNITY_EDITOR
            if (s_renAvatar == null)
            {
                s_renAvatar = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(RenAvatarPath);
            }
#endif
            return s_renAvatar;
        }

        public void WireImages(
            Image fill,
            Image stroke,
            Text label,
            Button btn,
            Image icon = null,
            Image selectionFrame = null,
            Image playerMarker = null)
        {
            fillImage = fill;
            strokeImage = stroke;
            labelText = label;
            button = btn;
            iconImage = icon;
            selectionFrameImage = selectionFrame;
            playerMarkerImage = playerMarker;
            EnsureHitTarget();
            DisableChildRaycastsExceptButtonTarget();
        }
    }
}

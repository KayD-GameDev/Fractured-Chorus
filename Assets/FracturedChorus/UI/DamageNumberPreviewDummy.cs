using System.Collections.Generic;
using FracturedChorus.Combat.Units;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Edit-mode dummy that renders the same digit strip combat will spawn, so values/size can be judged before Play.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class DamageNumberPreviewDummy : MonoBehaviour
    {
        public const string ObjectName = "DamageNumberPreviewDummy";

        [Header("Value")]
        [SerializeField] [Min(0)] private int amount = 42;
        [SerializeField] private bool heal;
        [SerializeField] private bool critical;

        [Header("Placement")]
        [SerializeField] private UnitView followUnit;
        [SerializeField] private Vector2 screenOffset = new(0f, 180f);
        [SerializeField] private bool hideWhenPlaying = true;

        [Header("Readout")]
        [SerializeField] private string displayedValue = "42";

        private Canvas _canvas;
        private RectTransform _canvasRect;
        private RectTransform _row;
        private readonly List<Image> _digits = new();
        private Image _critBadge;
        private Text _tag;
        private int _builtAmount = int.MinValue;
        private bool _builtHeal;
        private bool _builtCrit;
        private bool _builtSprites;

        public int Amount
        {
            get => amount;
            set => amount = Mathf.Max(0, value);
        }

        public bool Heal
        {
            get => heal;
            set => heal = value;
        }

        public bool Critical
        {
            get => critical;
            set => critical = value;
        }

        public string DisplayedValue => displayedValue;

        private void OnEnable()
        {
            EnsureVisuals();
            Refresh(true);
        }

        private void OnValidate()
        {
            amount = Mathf.Max(0, amount);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    Refresh(true);
                }
            };
#endif
        }

        private void LateUpdate()
        {
            if (Application.isPlaying && hideWhenPlaying)
            {
                if (_canvas != null && _canvas.gameObject.activeSelf)
                {
                    _canvas.gameObject.SetActive(false);
                }

                return;
            }

            Refresh(false);
        }

        public void Refresh(bool force)
        {
            EnsureVisuals();
            if (_canvas == null)
            {
                return;
            }

            var playingHide = Application.isPlaying && hideWhenPlaying;
            if (_canvas.gameObject.activeSelf == playingHide)
            {
                _canvas.gameObject.SetActive(!playingHide);
            }

            if (playingHide)
            {
                return;
            }

            var showAmount = ResolveAmount();
            var showHeal = heal;
            var showCrit = critical;
            if (force || showAmount != _builtAmount || showHeal != _builtHeal || showCrit != _builtCrit || !_builtSprites)
            {
                DamageNumberDigitStrip.Apply(_row, _digits, _critBadge, showAmount, showHeal, showCrit);
                displayedValue = showAmount.ToString();
                if (_tag != null)
                {
                    var kind = showHeal ? "HEAL" : showCrit ? "CRIT" : "DMG";
                    _tag.text = "PREVIEW  " + kind + "  " + displayedValue;
                }

                _builtAmount = showAmount;
                _builtHeal = showHeal;
                _builtCrit = showCrit;
                _builtSprites = DamageNumberDigitAtlas.HasDigits(showHeal);
            }

            var pos = ResolveScreenPosition();
            _row.anchoredPosition = pos;
            if (_tag != null)
            {
                _tag.rectTransform.anchoredPosition = pos + new Vector2(0f, -56f);
            }
        }

        private int ResolveAmount()
        {
            if (Application.isPlaying && followUnit != null && followUnit.Unit != null)
            {
                var change = followUnit.Unit.LastHpChange;
                if (change.ShouldShowFeedback)
                {
                    heal = change.Kind == HpChangeKind.Heal;
                    critical = change.IsCritical;
                    return change.Amount;
                }
            }

            return amount;
        }

        private Vector2 ResolveScreenPosition()
        {
            if (followUnit == null)
            {
                return screenOffset;
            }

            var cam = Camera.main;
            if (cam == null || _canvasRect == null)
            {
                return screenOffset;
            }

            var world = followUnit.GetDamageNumberAnchor();
            var screen = cam.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                screen,
                null,
                out var local);
            return local;
        }

        private void EnsureVisuals()
        {
            if (_canvas != null && _row != null && _digits.Count >= DamageNumberDigitStrip.MaxDigits)
            {
                return;
            }

            _canvas = GetComponentInChildren<Canvas>(true);
            if (_canvas == null)
            {
                var canvasGo = new GameObject(
                    "PreviewCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                canvasGo.transform.SetParent(transform, false);
                _canvas = canvasGo.GetComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = UiCanvasLayers.PopupDamage + 2;
                _canvas.overrideSorting = true;
                _canvas.pixelPerfect = true;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;
                canvasGo.GetComponent<GraphicRaycaster>().enabled = false;
            }

            _canvasRect = _canvas.GetComponent<RectTransform>();
            _row = _canvas.transform.Find("Digits") as RectTransform;
            if (_row == null)
            {
                var rowGo = new GameObject("Digits", typeof(RectTransform));
                _row = rowGo.GetComponent<RectTransform>();
                _row.SetParent(_canvasRect, false);
                _row.anchorMin = new Vector2(0.5f, 0.5f);
                _row.anchorMax = new Vector2(0.5f, 0.5f);
                _row.pivot = new Vector2(0.5f, 0.5f);
                _row.anchoredPosition = Vector2.zero;
            }

            _critBadge = _row.Find("CritBadge")?.GetComponent<Image>();
            if (_critBadge == null)
            {
                _critBadge = DamageNumberDigitStrip.CreateImage("CritBadge", _row);
            }

            _digits.Clear();
            for (var i = 0; i < DamageNumberDigitStrip.MaxDigits; i++)
            {
                var child = _row.Find("Digit_" + i);
                var image = child != null
                    ? child.GetComponent<Image>()
                    : DamageNumberDigitStrip.CreateImage("Digit_" + i, _row);
                _digits.Add(image);
            }

            _tag = _canvas.transform.Find("PreviewTag")?.GetComponent<Text>();
            if (_tag == null)
            {
                var tagGo = new GameObject(
                    "PreviewTag",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                var tagRt = tagGo.GetComponent<RectTransform>();
                tagRt.SetParent(_canvasRect, false);
                tagRt.anchorMin = new Vector2(0.5f, 0.5f);
                tagRt.anchorMax = new Vector2(0.5f, 0.5f);
                tagRt.pivot = new Vector2(0.5f, 0.5f);
                tagRt.sizeDelta = new Vector2(420f, 28f);
                tagRt.anchoredPosition = new Vector2(0f, -56f);
                _tag = tagGo.GetComponent<Text>();
                _tag.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                _tag.fontSize = 16;
                _tag.alignment = TextAnchor.MiddleCenter;
                _tag.color = new Color(0.55f, 0.9f, 1f, 0.85f);
                _tag.raycastTarget = false;
            }
        }
    }
}

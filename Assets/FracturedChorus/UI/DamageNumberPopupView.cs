using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class DamageNumberPopupView : MonoBehaviour
    {
        private const string CanvasName = "DamageNumberCanvas";
        private const float Lifetime = 0.9f;
        private const float RisePixels = 96f;
        private const int PoolCap = 16;
        private const int CanvasSortOrder = 520;

        private static readonly Color DamageColor = new Color(1f, 0.38f, 0.78f, 1f);
        private static readonly Color HealColor = new Color(0.25f, 1f, 0.55f, 1f);
        private static readonly Color CritColor = new Color(1f, 0.88f, 0.2f, 1f);
        private static readonly Color OutlineColor = new Color(0.02f, 0.01f, 0.05f, 1f);

        private static readonly Queue<DamageNumberPopupView> Pool = new();
        private static RectTransform _canvasRoot;
        private static Camera _worldCamera;
        private static Font _font;

        private RectTransform _rect;
        private CanvasGroup _group;
        private Text _label;
        private Outline _outline;
        private Shadow _shadow;
        private Coroutine _playRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Pool.Clear();
            _canvasRoot = null;
            _worldCamera = null;
            _font = null;
        }

        public static void Spawn(Vector3 worldPosition, int amount, bool heal, bool isCritical)
        {
            if (amount <= 0)
            {
                return;
            }

            var view = Rent();
            view.Play(worldPosition, amount, heal, isCritical);
        }

        private static DamageNumberPopupView Rent()
        {
            EnsureCanvas();
            while (Pool.Count > 0)
            {
                var pooled = Pool.Dequeue();
                if (pooled != null)
                {
                    return pooled;
                }
            }

            var go = new GameObject(
                "DamageNumberPopup",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(DamageNumberPopupView));
            var view = go.GetComponent<DamageNumberPopupView>();
            view.Build(_canvasRoot);
            return view;
        }

        private static void EnsureCanvas()
        {
            if (_canvasRoot != null)
            {
                return;
            }

            var existing = GameObject.Find(CanvasName);
            if (existing != null)
            {
                Object.Destroy(existing);
            }

            var go = new GameObject(
                CanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Object.DontDestroyOnLoad(go);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortOrder;
            canvas.overrideSorting = true;
            canvas.pixelPerfect = true;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            go.GetComponent<GraphicRaycaster>().enabled = false;
            _canvasRoot = go.GetComponent<RectTransform>();
            _worldCamera = Camera.main;
        }

        private static Font ResolveFont()
        {
            if (_font != null)
            {
                return _font;
            }

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return _font;
        }

        private void Build(RectTransform parent)
        {
            _rect = GetComponent<RectTransform>();
            _rect.SetParent(parent, false);
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = new Vector2(280f, 100f);

            _group = GetComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var labelGo = new GameObject(
                "Amount",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text),
                typeof(Outline),
                typeof(Shadow));
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.SetParent(_rect, false);
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            _label = labelGo.GetComponent<Text>();
            _label.font = ResolveFont();
            _label.fontStyle = FontStyle.Bold;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;
            _label.verticalOverflow = VerticalWrapMode.Overflow;
            _label.raycastTarget = false;
            _label.supportRichText = false;

            _outline = labelGo.GetComponent<Outline>();
            _outline.effectColor = OutlineColor;
            _outline.effectDistance = new Vector2(4f, -4f);
            _outline.useGraphicAlpha = true;

            _shadow = labelGo.GetComponent<Shadow>();
            _shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            _shadow.effectDistance = new Vector2(3f, -5f);
            _shadow.useGraphicAlpha = true;
        }

        private void Play(Vector3 worldPosition, int amount, bool heal, bool isCritical)
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
            }

            if (_worldCamera == null)
            {
                _worldCamera = Camera.main;
            }

            gameObject.SetActive(true);
            _rect.SetAsLastSibling();
            _group.alpha = 1f;
            _rect.anchoredPosition = WorldToCanvas(worldPosition);

            _label.text = Mathf.Abs(amount).ToString();
            _label.fontSize = isCritical ? 86 : 70;
            _label.color = isCritical && !heal ? CritColor : heal ? HealColor : DamageColor;
            _outline.effectDistance = isCritical ? new Vector2(5f, -5f) : new Vector2(4f, -4f);

            _playRoutine = StartCoroutine(AnimateRoutine(_rect.anchoredPosition, isCritical));
        }

        private Vector2 WorldToCanvas(Vector3 worldPosition)
        {
            if (_worldCamera == null)
            {
                return Vector2.zero;
            }

            var screen = _worldCamera.WorldToScreenPoint(worldPosition + Vector3.up * 0.45f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRoot,
                screen,
                null,
                out var local);
            return local;
        }

        private IEnumerator AnimateRoutine(Vector2 start, bool isCritical)
        {
            var end = start + Vector2.up * RisePixels;
            var startScale = isCritical ? 1.25f : 1.05f;
            var peakScale = startScale * 1.12f;
            _rect.localScale = Vector3.one * startScale;
            var elapsed = 0f;

            while (elapsed < Lifetime)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / Lifetime);
                var ease = 1f - (1f - t) * (1f - t);
                _rect.anchoredPosition = Vector2.Lerp(start, end, ease);
                var scaleT = t < 0.12f ? t / 0.12f : 1f;
                _rect.localScale = Vector3.one * Mathf.Lerp(startScale, peakScale, scaleT);
                _group.alpha = t < 0.75f ? 1f : 1f - ((t - 0.75f) / 0.25f);
                yield return null;
            }

            Recycle();
        }

        private void Recycle()
        {
            _playRoutine = null;
            gameObject.SetActive(false);
            if (Pool.Count < PoolCap)
            {
                Pool.Enqueue(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}

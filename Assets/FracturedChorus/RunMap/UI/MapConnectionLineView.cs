using System.Collections;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.RunMap.UI
{
    [RequireComponent(typeof(Image))]
    public class MapConnectionLineView : MonoBehaviour
    {
        private static readonly Vector2 BottomAnchor = new Vector2(0.5f, 0f);

        [SerializeField] private Image lineImage;
        [SerializeField] private float thickness = 3f;

        private Coroutine _revealRoutine;

        public int FromNodeId { get; private set; } = -1;
        public int ToNodeId { get; private set; } = -1;

        private void Awake()
        {
            EnsureLineImage();
        }

        public void BindEdge(int fromNodeId, int toNodeId)
        {
            FromNodeId = fromNodeId;
            ToNodeId = toNodeId;
        }

        public void SetEndpoints(Vector2 from, Vector2 to, Color color, float thicknessOverride = -1f)
        {
            if (!EnsureLineImage())
            {
                return;
            }

            var delta = to - from;
            var length = delta.magnitude;
            var lineThickness = thicknessOverride > 0f ? thicknessOverride : thickness;
            var rect = lineImage.rectTransform;
            rect.anchorMin = BottomAnchor;
            rect.anchorMax = BottomAnchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(Mathf.Max(length, 0.01f), lineThickness);
            rect.anchoredPosition = from + delta * 0.5f;
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            rect.localScale = Vector3.one;
            lineImage.type = Image.Type.Simple;
            lineImage.fillAmount = 1f;
            lineImage.color = color;
            lineImage.enabled = true;
            lineImage.SetAllDirty();

            var renderer = lineImage.canvasRenderer;
            if (renderer != null)
            {
                renderer.cullTransparentMesh = false;
            }
        }

        public void PlayReveal(Vector2 from, Vector2 to, Color color, float thicknessOverride, float duration, System.Action onComplete)
        {
            if (_revealRoutine != null)
            {
                StopCoroutine(_revealRoutine);
            }

            SetEndpoints(from, to, color, thicknessOverride);
            if (!EnsureLineImage() || !isActiveAndEnabled)
            {
                onComplete?.Invoke();
                return;
            }

            _revealRoutine = StartCoroutine(RevealRoutine(duration, onComplete));
        }

        public void WireImage(Image image)
        {
            lineImage = image;
            EnsureLineImage();
        }

        private IEnumerator RevealRoutine(float duration, System.Action onComplete)
        {
            lineImage.type = Image.Type.Filled;
            lineImage.fillMethod = Image.FillMethod.Horizontal;
            lineImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            lineImage.fillAmount = 0f;
            var elapsed = 0f;
            var seconds = Mathf.Max(0.08f, duration);
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                lineImage.fillAmount = Mathf.Clamp01(elapsed / seconds);
                yield return null;
            }

            lineImage.fillAmount = 1f;
            lineImage.type = Image.Type.Simple;
            _revealRoutine = null;
            onComplete?.Invoke();
        }

        private bool EnsureLineImage()
        {
            lineImage ??= GetComponent<Image>();
            if (lineImage == null)
            {
                return false;
            }

            if (lineImage.sprite == null)
            {
                lineImage.sprite = UiCircleSpriteUtil.White;
            }

            lineImage.type = Image.Type.Simple;
            lineImage.raycastTarget = false;
            lineImage.maskable = true;
            lineImage.enabled = true;

            var renderer = lineImage.canvasRenderer;
            if (renderer != null)
            {
                renderer.cullTransparentMesh = false;
            }

            return true;
        }
    }
}

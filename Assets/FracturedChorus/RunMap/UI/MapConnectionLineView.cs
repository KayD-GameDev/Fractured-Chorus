using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.RunMap.UI
{
    /// <summary>Vẽ đường nối giữa hai node trên UI Canvas.</summary>
    [RequireComponent(typeof(Image))]
    public class MapConnectionLineView : MonoBehaviour
    {
        [SerializeField] private Image lineImage;
        [SerializeField] private float thickness = 3f;

        private void Awake()
        {
            if (lineImage == null)
            {
                lineImage = GetComponent<Image>();
            }
        }

        public void SetEndpoints(Vector2 from, Vector2 to, Color color, float thicknessOverride = -1f)
        {
            if (lineImage == null)
            {
                return;
            }

            var delta = to - from;
            var length = delta.magnitude;
            var lineThickness = thicknessOverride > 0f ? thicknessOverride : thickness;
            var rect = lineImage.rectTransform;
            // Phải khớp node: anchor đáy (0.5, 0) — không dùng center anchor.
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(length, lineThickness);
            rect.anchoredPosition = from + delta * 0.5f;
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            lineImage.color = color;
        }

        public void WireImage(Image image)
        {
            lineImage = image;
        }
    }
}

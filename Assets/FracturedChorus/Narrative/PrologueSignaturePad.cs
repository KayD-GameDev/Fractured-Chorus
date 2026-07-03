using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.Narrative
{
    public class PrologueSignaturePad : MonoBehaviour
    {
        [SerializeField] private RawImage targetImage;
        [SerializeField] private int textureWidth = 512;
        [SerializeField] private int textureHeight = 128;
        [SerializeField] private Color strokeColor = new Color(0.08f, 0.12f, 0.28f, 1f);
        [SerializeField] private int strokeRadius = 3;

        private Texture2D _texture;
        private bool _drawing;
        private Vector2 _lastUv;
        private PrologueAudioController _audio;

        public bool HasStroke { get; private set; }

        public void Bind(PrologueAudioController audio)
        {
            _audio = audio;
        }

        private void Awake()
        {
            EnsureTexture();
            EnsurePointerRelay();
        }

        public void Clear()
        {
            EnsureTexture();
            var pixels = _texture.GetPixels();
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            _texture.SetPixels(pixels);
            _texture.Apply();
            HasStroke = false;
        }

        public void ForwardPointerDown(PointerEventData eventData)
        {
            _drawing = true;
            _audio?.PlayPenSign();
            PaintAt(eventData);
        }

        public void ForwardDrag(PointerEventData eventData)
        {
            if (!_drawing)
            {
                return;
            }

            PaintAt(eventData);
        }

        public void ForwardPointerUp(PointerEventData eventData)
        {
            _drawing = false;
        }

        private void PaintAt(PointerEventData eventData)
        {
            if (targetImage == null || _texture == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    targetImage.rectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var local))
            {
                return;
            }

            var rect = targetImage.rectTransform.rect;
            var uv = new Vector2(
                (local.x - rect.xMin) / rect.width,
                (local.y - rect.yMin) / rect.height);

            if (uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f)
            {
                return;
            }

            var x = Mathf.Clamp(Mathf.RoundToInt(uv.x * (_texture.width - 1)), 0, _texture.width - 1);
            var y = Mathf.Clamp(Mathf.RoundToInt(uv.y * (_texture.height - 1)), 0, _texture.height - 1);

            if (HasStroke)
            {
                DrawLine(_lastUv, uv);
            }
            else
            {
                Stamp(x, y);
                HasStroke = true;
            }

            _lastUv = uv;
            _texture.Apply();
        }

        private void DrawLine(Vector2 fromUv, Vector2 toUv)
        {
            var from = new Vector2(fromUv.x * (_texture.width - 1), fromUv.y * (_texture.height - 1));
            var to = new Vector2(toUv.x * (_texture.width - 1), toUv.y * (_texture.height - 1));
            var distance = Vector2.Distance(from, to);
            var steps = Mathf.Max(1, Mathf.CeilToInt(distance));
            for (var i = 0; i <= steps; i++)
            {
                var t = i / (float)steps;
                var p = Vector2.Lerp(from, to, t);
                Stamp(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
            }
        }

        private void Stamp(int x, int y)
        {
            for (var dy = -strokeRadius; dy <= strokeRadius; dy++)
            {
                for (var dx = -strokeRadius; dx <= strokeRadius; dx++)
                {
                    if (dx * dx + dy * dy > strokeRadius * strokeRadius)
                    {
                        continue;
                    }

                    var px = x + dx;
                    var py = y + dy;
                    if (px < 0 || py < 0 || px >= _texture.width || py >= _texture.height)
                    {
                        continue;
                    }

                    _texture.SetPixel(px, py, strokeColor);
                }
            }
        }

        private void EnsureTexture()
        {
            if (_texture != null)
            {
                return;
            }

            _texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            _texture.wrapMode = TextureWrapMode.Clamp;
            Clear();

            if (targetImage != null)
            {
                targetImage.texture = _texture;
            }
        }

        private void EnsurePointerRelay()
        {
            if (targetImage == null)
            {
                return;
            }

            targetImage.raycastTarget = true;
            var relay = targetImage.GetComponent<PrologueSignaturePointerRelay>();
            if (relay == null)
            {
                relay = targetImage.gameObject.AddComponent<PrologueSignaturePointerRelay>();
            }

            relay.Bind(this);
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                Destroy(_texture);
            }
        }
    }
}

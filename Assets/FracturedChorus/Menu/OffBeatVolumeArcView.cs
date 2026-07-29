using FracturedChorus.UI;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.Menu
{
    public sealed class OffBeatVolumeArcView : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IPointerDownHandler, ICanvasRaycastFilter
    {
        private const string PrefsKey = "fc_offbeat_volume";
        private const int RingTexSize = 128;

        [Header("Refs")]
        [SerializeField] private RectTransform arcRoot;
        [SerializeField] private Image trackImage;
        [SerializeField] private Image fillImage;

        [Header("Layout (chỉnh vị trí / size)")]
        [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, 155.7f);
        [SerializeField] private Vector2 size = new Vector2(228.89f, 208.41f);
        [SerializeField] private float localEulerZ = -368.749f;
        [SerializeField] private bool applyLayoutOnAwake = true;
        [SerializeField] [Range(0f, 0.05f)] private float hitAreaAlpha;

        [Header("Arc shape")]
        [SerializeField] [Range(0f, 360f)] private float startAngleDeg = 210f;
        [SerializeField] [Range(30f, 180f)] private float sweepAngleDeg = 140f;
        [SerializeField] [Range(0.04f, 0.28f)] private float ringThickness = 0.14f;
        [SerializeField] [Range(0f, 0.2f)] private float hitPadding = 0.06f;

        [Header("Drag / Fill")]
        [Tooltip("Bật nếu kéo sang phải mà volume giảm")]
        [SerializeField] private bool invertDrag;
        [SerializeField] private bool fillClockwise;
        [SerializeField] [Range(0f, 1f)] private float volume = 0.85f;

        private Sprite _ringSprite;
        private float _cachedThickness = -1f;

        public event Action<float> VolumeChanged;

        public float Volume
        {
            get => volume;
            set => SetVolume(value, notify: false);
        }

        private void Awake()
        {
            ResolveImages();
            if (applyLayoutOnAwake)
            {
                ApplyLayout();
            }

            EnsureRingSprites();
            AlignFillRotation();
            volume = PlayerPrefs.GetFloat(PrefsKey, volume);
            ApplyFill();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveImages();
            if (applyLayoutOnAwake)
            {
                ApplyLayout();
            }

            EnsureRingSprites();
            AlignFillRotation();
            ApplyFill();
        }

        [ContextMenu("Capture Layout From Root")]
        private void CaptureLayoutFromRootMenu()
        {
            CaptureLayoutFromRoot();
        }
#endif

        public void BindFill(Image fill)
        {
            fillImage = fill;
            ResolveImages();
            EnsureRingSprites();
            AlignFillRotation();
            ApplyFill();
        }

        public void Bind(Image track, Image fill)
        {
            trackImage = track;
            fillImage = fill;
            EnsureRingSprites();
            AlignFillRotation();
            ApplyFill();
        }

        public void CaptureLayoutFromRoot()
        {
            var root = GetArcRoot();
            if (root == null)
            {
                return;
            }

            anchoredPosition = root.anchoredPosition;
            size = root.sizeDelta;
            localEulerZ = root.localEulerAngles.z;
        }

        public void ApplyLayout()
        {
            var root = GetArcRoot();
            if (root == null)
            {
                return;
            }

            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = anchoredPosition;
            root.sizeDelta = size;
            root.localEulerAngles = new Vector3(0f, 0f, localEulerZ);
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            return TryGetLocal(screenPoint, eventCamera, out var local) && IsInVolumeZone(local);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            ApplyFromPointer(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            ApplyFromPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            ApplyFromPointer(eventData);
        }

        private RectTransform GetArcRoot()
        {
            if (arcRoot != null)
            {
                return arcRoot;
            }

            if (transform.parent != null && transform.parent.name == "VolumeArcRoot")
            {
                arcRoot = transform.parent as RectTransform;
            }
            else
            {
                arcRoot = transform as RectTransform;
            }

            return arcRoot;
        }

        private void ResolveImages()
        {
            var root = GetArcRoot();
            if (root == null)
            {
                return;
            }

            if (trackImage == null)
            {
                var trackTf = root.Find("Track");
                if (trackTf != null)
                {
                    trackImage = trackTf.GetComponent<Image>();
                }
            }

            if (fillImage == null)
            {
                var fillTf = root.Find("Fill");
                if (fillTf != null)
                {
                    fillImage = fillTf.GetComponent<Image>();
                }
            }
        }

        private void EnsureRingSprites()
        {
            var ring = GetOrCreateRingSprite(ringThickness);
            var arc = Mathf.Clamp01(sweepAngleDeg / 360f);
            ConfigureRingImage(trackImage, ring, new Color(0.3f, 0.36f, 0.44f, 0.85f), arc);
            ConfigureRingImage(fillImage, ring, FcColorTokens.Brand.Cyan, 0f);
            var hit = GetComponent<Image>();
            if (hit != null)
            {
                hit.raycastTarget = true;
                hit.color = new Color(1f, 1f, 1f, hitAreaAlpha);
            }
        }

        private void AlignFillRotation()
        {
            var z = startAngleDeg - 270f;
            if (trackImage != null)
            {
                trackImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, z);
            }

            if (fillImage != null)
            {
                fillImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, z);
            }
        }

        private void ConfigureRingImage(Image image, Sprite ring, Color color, float fillAmount)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = ring;
            image.color = color;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Bottom;
            image.fillClockwise = fillClockwise;
            image.fillAmount = fillAmount;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = true;
        }

        private Sprite GetOrCreateRingSprite(float thickness)
        {
            thickness = Mathf.Clamp(thickness, 0.04f, 0.28f);
            if (_ringSprite != null && Mathf.Abs(_cachedThickness - thickness) < 0.0001f)
            {
                return _ringSprite;
            }

            if (_ringSprite != null)
            {
                DestroySprite(_ringSprite);
                _ringSprite = null;
            }

            var tex = new Texture2D(RingTexSize, RingTexSize, TextureFormat.RGBA32, false)
            {
                name = "OffBeatVolumeRing",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var center = (RingTexSize - 1) * 0.5f;
            var outer = center - 1f;
            var inner = Mathf.Max(1f, outer * (1f - thickness));
            var pixels = new Color32[RingTexSize * RingTexSize];
            for (var y = 0; y < RingTexSize; y++)
            {
                for (var x = 0; x < RingTexSize; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var d = Mathf.Sqrt(dx * dx + dy * dy);
                    byte a = 0;
                    if (d <= outer && d >= inner)
                    {
                        var edge = Mathf.Min(outer - d, d - inner);
                        a = edge >= 1f
                            ? (byte)255
                            : (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(edge) * 255f), 0, 255);
                    }

                    pixels[y * RingTexSize + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            _ringSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, RingTexSize, RingTexSize),
                new Vector2(0.5f, 0.5f),
                100f);
            _ringSprite.name = "OffBeatVolumeRingSprite";
            _cachedThickness = thickness;
            return _ringSprite;
        }

        private static void DestroySprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            var tex = sprite.texture;
            if (Application.isPlaying)
            {
                Destroy(sprite);
                if (tex != null)
                {
                    Destroy(tex);
                }
            }
            else
            {
                DestroyImmediate(sprite);
                if (tex != null)
                {
                    DestroyImmediate(tex);
                }
            }
        }

        private void ApplyFromPointer(PointerEventData eventData)
        {
            if (!TryGetLocal(eventData.position, eventData.pressEventCamera, out var local))
            {
                return;
            }

            if (!IsInVolumeZone(local))
            {
                return;
            }

            var angle = Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;
            angle = (angle + 360f) % 360f;
            var start = startAngleDeg % 360f;
            var rel = (angle - start + 360f) % 360f;

            float t;
            if (rel > sweepAngleDeg)
            {
                t = rel > 180f + sweepAngleDeg * 0.5f ? 0f : 1f;
            }
            else
            {
                t = rel / Mathf.Max(1f, sweepAngleDeg);
            }

            if (invertDrag)
            {
                t = 1f - t;
            }

            SetVolume(t, notify: true);
        }

        private bool TryGetLocal(Vector2 screenPoint, Camera eventCamera, out Vector2 local)
        {
            var rt = GetArcRoot();
            if (rt == null)
            {
                local = default;
                return false;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rt, screenPoint, eventCamera, out local);
        }

        private bool IsInVolumeZone(Vector2 local)
        {
            var rt = GetArcRoot();
            if (rt == null)
            {
                return false;
            }

            var half = Mathf.Min(rt.rect.width, rt.rect.height) * 0.5f;
            if (half < 1f)
            {
                return false;
            }

            var radius = local.magnitude;
            var inner = half * (1f - ringThickness - hitPadding);
            var outer = half * (1f + hitPadding);
            if (radius < inner || radius > outer)
            {
                return false;
            }

            var angle = Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;
            angle = (angle + 360f) % 360f;
            var start = startAngleDeg % 360f;
            var rel = (angle - start + 360f) % 360f;
            return rel <= sweepAngleDeg + 20f;
        }

        private void SetVolume(float value, bool notify)
        {
            volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PrefsKey, volume);
            PlayerPrefs.Save();
            ApplyFill();
            if (notify)
            {
                VolumeChanged?.Invoke(volume);
            }
        }

        private void ApplyFill()
        {
            if (fillImage == null)
            {
                return;
            }

            var arc = Mathf.Clamp01(sweepAngleDeg / 360f);
            fillImage.fillAmount = Mathf.Lerp(0.04f, arc, volume);
            fillImage.fillClockwise = fillClockwise;
            fillImage.color = FcColorTokens.Brand.Cyan;
            if (trackImage != null)
            {
                trackImage.fillAmount = arc;
                trackImage.fillClockwise = fillClockwise;
            }
        }

        private void OnDestroy()
        {
            if (_ringSprite != null)
            {
                DestroySprite(_ringSprite);
                _ringSprite = null;
            }
        }
    }
}

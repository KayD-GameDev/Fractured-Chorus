using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Combat.Presentation
{
    public sealed class AstraStageTvView : MonoBehaviour
    {
        public const string ObjectName = "AstraStageTv";
        public const string PrefabResourcePath = "UI/Combat/Boss/Astra/AstraStageTv";
        public const string FrameChildName = "Frame";
        public const string ScreenChildName = "Screen";
        public const string FaceReelChildName = "FaceReel";

        private static readonly string[] FaceNames =
        {
            "Face_Joy",
            "Face_Anger",
            "Face_Love",
            "Face_Hate",
            "Face_Sorrow"
        };

        [SerializeField] private AstraStageTvConfig config;
        [SerializeField] private RectTransform frame;
        [SerializeField] private Image frameImage;
        [SerializeField] private RectTransform screen;
        [SerializeField] private RectTransform faceReel;

        private readonly AstraStageTvSequence _sequence = new AstraStageTvSequence();
        private Image[] _faceImages = System.Array.Empty<Image>();
        private bool _playing;
        private bool _restCaptured;
        private Vector2 _restAnchored;
        private float _dropFromY;
        private Vector2 _restSize;
        private float _faceHeight;

        public AstraStageTvConfig Config => config;
        public AstraStageTvSequence Sequence => _sequence;
        public RectTransform Frame => frame;
        public RectTransform Screen => screen;
        public RectTransform FaceReel => faceReel;

        public static AstraStageTvView FindActive()
        {
            return FindAnyObjectByType<AstraStageTvView>(FindObjectsInactive.Include);
        }

        /// <summary>
        /// Intro hook. Skips a TV that the pooled-combat path hid.
        /// </summary>
        public static void PlayActive()
        {
            var view = FindActive();
            if (view != null && view.gameObject.activeSelf)
            {
                view.Play();
            }
        }

        public static AstraStageTvView EnsureOnBackground(Transform backgroundCanvas)
        {
            if (backgroundCanvas == null)
            {
                return null;
            }

            var existing = backgroundCanvas.Find(ObjectName)?.GetComponent<AstraStageTvView>();
            if (existing != null)
            {
                existing.EnsureBuilt();
                existing.PlaceAfterSceneVideo();
                if (!Application.isPlaying)
                {
                    existing.ShowAtRest();
                }
                else
                {
                    existing.ParkAboveForIntro();
                }

                return existing;
            }

            AstraStageTvView view = null;
            var prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab != null)
            {
                var instance = Instantiate(prefab, backgroundCanvas, false);
                instance.name = ObjectName;
                view = instance.GetComponent<AstraStageTvView>();
            }

            if (view == null)
            {
                var go = new GameObject(ObjectName, typeof(RectTransform));
                go.transform.SetParent(backgroundCanvas, false);
                view = go.AddComponent<AstraStageTvView>();
            }

            view.EnsureBuilt();
            view.PlaceAfterSceneVideo();
            if (!Application.isPlaying)
            {
                view.ShowAtRest();
            }
            else
            {
                view.ParkAboveForIntro();
            }

            return view;
        }

        public static void HideIfPresent()
        {
            var view = FindActive();
            if (view != null)
            {
                view.Hide();
            }
        }

        public void EnsureBuilt()
        {
            if (config == null)
            {
                config = AstraStageTvConfig.Load();
            }
            else
            {
                config.EnsureSpritesLoaded();
            }

            var root = transform as RectTransform;
            if (root == null)
            {
                return;
            }

            var frameExisted = root.Find(FrameChildName) != null;
            frame = EnsureChild(root, FrameChildName, out frameImage, asLastSibling: true);
            if (!frameExisted)
            {
                StretchFill(frame);
            }

            screen = EnsureChild(root, ScreenChildName, out var screenImage, asLastSibling: false);
            if (screen.GetComponent<RectMask2D>() == null)
            {
                screen.gameObject.AddComponent<RectMask2D>();
            }

            if (screenImage != null)
            {
                screenImage.sprite = null;
                screenImage.color = new Color(0.02f, 0.02f, 0.08f, 0.55f);
                screenImage.raycastTarget = false;
            }

            var reelTf = screen.Find(FaceReelChildName) as RectTransform;
            if (reelTf == null)
            {
                var reelGo = new GameObject(FaceReelChildName, typeof(RectTransform));
                reelTf = reelGo.GetComponent<RectTransform>();
                reelTf.SetParent(screen, false);
            }

            faceReel = reelTf;
            if (!HasAuthoredRect(faceReel))
            {
                StretchFill(faceReel);
            }

            if (frameImage != null)
            {
                frameImage.sprite = config != null ? config.FrameSprite : null;
                frameImage.preserveAspect = true;
                frameImage.color = Color.white;
                frameImage.raycastTarget = false;
            }

            CaptureRestLayout(force: false);
            ApplyScreenLayout();
            BuildFaceReel();
        }

        public void Play()
        {
            EnsureBuilt();
            if (config == null)
            {
                Debug.LogWarning("[AstraStageTv] Missing config.");
                return;
            }

            gameObject.SetActive(true);
            PlaceAfterSceneVideo();
            _dropFromY = ResolveDropFromY();
            SetAnchoredY(_dropFromY);
            _sequence.Begin(
                config.DropDurationSec,
                config.SpinDurationSec,
                config.SpinSpeedFacesPerSec,
                config.DecelDurationSec);
            _playing = true;
            ApplySequenceVisual();
        }

        public void ParkAboveForIntro()
        {
            EnsureBuilt();
            PlaceAfterSceneVideo();
            _dropFromY = ResolveDropFromY();
            SetAnchoredY(_dropFromY);
            _playing = false;
        }

        public void ShowAtRest()
        {
            EnsureBuilt();
            PlaceAfterSceneVideo();
            var root = transform as RectTransform;
            if (root != null)
            {
                root.anchoredPosition = _restAnchored;
            }

            _playing = false;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _playing = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_playing)
            {
                return;
            }

            if (_sequence.Phase == AstraStageTvPhase.Dropping)
            {
                // Canvas rect is still unresolved during Awake, so keep the start above the live height.
                _dropFromY = ResolveDropFromY();
            }

            _sequence.Tick(Time.deltaTime);
            ApplySequenceVisual();
            if (_sequence.Phase == AstraStageTvPhase.Locked)
            {
                _playing = false;
            }
        }

        private void ApplySequenceVisual()
        {
            var drop = AstraStageTvSequence.Smooth01(_sequence.DropT);
            var root = transform as RectTransform;
            if (root != null)
            {
                root.anchoredPosition = new Vector2(
                    _restAnchored.x,
                    Mathf.Lerp(_dropFromY, _restAnchored.y, drop));
            }
            ApplyReelOffset(_sequence.ReelOffset);
        }

        /// <summary>
        /// Scene RectTransform is the source of truth for the rest pose. Config only seeds a rect
        /// that nobody authored yet (fresh runtime spawn).
        /// </summary>
        private void CaptureRestLayout(bool force)
        {
            var root = transform as RectTransform;
            if (root == null)
            {
                return;
            }

            if (!HasAuthoredRect(root) && config != null)
            {
                var anchor = config.RestNormalizedPos;
                root.anchorMin = anchor;
                root.anchorMax = anchor;
                root.pivot = new Vector2(0.5f, 0.5f);
                root.sizeDelta = config.RestSizePx;
                root.anchoredPosition = Vector2.zero;
                force = true;
            }

            if (force || !_restCaptured)
            {
                _restAnchored = root.anchoredPosition;
                _restCaptured = true;
            }

            _restSize = root.rect.size;
        }

        private void ApplyScreenLayout()
        {
            if (frame != null)
            {
                if (!HasAuthoredRect(frame))
                {
                    StretchFill(frame);
                }

                frame.SetAsLastSibling();
            }

            if (screen == null)
            {
                _faceHeight = _restSize.y;
                return;
            }

            if (!HasAuthoredRect(screen) && config != null)
            {
                var inset = Mathf.Clamp01(config.ScreenInsetNormalized);
                screen.anchorMin = new Vector2(0.5f, 0.5f);
                screen.anchorMax = new Vector2(0.5f, 0.5f);
                screen.pivot = new Vector2(0.5f, 0.5f);
                screen.sizeDelta = new Vector2(
                    _restSize.x * (1f - inset * 2f),
                    _restSize.y * (1f - inset * 2f));
                screen.anchoredPosition = Vector2.zero;
            }

            _faceHeight = screen.rect.height;
        }

        private static bool HasAuthoredRect(RectTransform rt)
        {
            return rt != null && rt.rect.width > 1f && rt.rect.height > 1f;
        }

        private void BuildFaceReel()
        {
            if (faceReel == null || config == null)
            {
                return;
            }

            var faces = config.ResolveFaces();
            EnsureFaceImageCount(AstraStageTvSequence.FaceCount);

            for (var i = 0; i < _faceImages.Length; i++)
            {
                var image = _faceImages[i];
                var rt = image.rectTransform;
                if (!HasAuthoredRect(rt))
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = screen != null ? screen.rect.size : new Vector2(_faceHeight, _faceHeight);
                }

                image.sprite = faces != null && i < faces.Length ? faces[i] : null;
                image.preserveAspect = true;
                image.color = Color.white;
                image.raycastTarget = false;
                image.enabled = image.sprite != null;
            }

            faceReel.anchoredPosition = Vector2.zero;
            ApplyReelOffset(_sequence.ReelOffset);
        }

        /// <summary>
        /// Wraps the five faces around the mask window so the reel loops with no extra Image objects.
        /// </summary>
        private void ApplyReelOffset(float offset)
        {
            if (faceReel == null || _faceImages.Length == 0)
            {
                return;
            }

            var spacing = _faceHeight > 1f ? _faceHeight : _restSize.y;
            var count = _faceImages.Length;
            var half = count * 0.5f;
            for (var i = 0; i < count; i++)
            {
                var delta = i - offset;
                delta -= Mathf.Floor((delta + half) / count) * count;
                _faceImages[i].rectTransform.anchoredPosition = new Vector2(0f, delta * spacing);
            }
        }

        private void EnsureFaceImageCount(int count)
        {
            var images = new System.Collections.Generic.List<Image>(count);
            for (var i = 0; i < faceReel.childCount; i++)
            {
                var image = faceReel.GetChild(i).GetComponent<Image>();
                if (image != null)
                {
                    images.Add(image);
                }
            }

            while (images.Count < count)
            {
                var index = images.Count;
                var name = index < FaceNames.Length ? FaceNames[index] : "Face_" + index;
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(faceReel, false);
                images.Add(go.GetComponent<Image>());
            }

            for (var i = images.Count - 1; i >= count; i--)
            {
                var extra = images[i].gameObject;
                images.RemoveAt(i);
                if (Application.isPlaying)
                {
                    Destroy(extra);
                }
                else
                {
                    DestroyImmediate(extra);
                }
            }

            _faceImages = images.ToArray();
        }

        public void PlaceAfterSceneVideo()
        {
            var parent = transform.parent;
            if (parent == null)
            {
                return;
            }

            var sceneVideo = parent.Find("SceneVideo");
            if (sceneVideo != null)
            {
                var target = sceneVideo.GetSiblingIndex() + 1;
                if (transform.GetSiblingIndex() != target)
                {
                    transform.SetSiblingIndex(target);
                }

                return;
            }

            transform.SetAsLastSibling();
        }

        /// <summary>
        /// Anchored Y that puts the authored rest rect fully above the canvas top edge.
        /// </summary>
        private float ResolveDropFromY()
        {
            var root = transform as RectTransform;
            var parentRt = transform.parent as RectTransform;
            var height = _restSize.y > 1f ? _restSize.y : (root != null ? root.rect.height : 0f);
            if (root == null || parentRt == null)
            {
                return _restAnchored.y + height;
            }

            var canvas = GetComponentInParent<Canvas>();
            var canvasRt = canvas != null ? canvas.transform as RectTransform : parentRt;
            var canvasTop = canvasRt.TransformPoint(new Vector3(0f, canvasRt.rect.yMax, 0f));
            var topInParent = parentRt.InverseTransformPoint(canvasTop).y;

            var parentRect = parentRt.rect;
            var anchorY = (root.anchorMin.y + root.anchorMax.y) * 0.5f;
            var restCenterY = parentRect.yMin + anchorY * parentRect.height + _restAnchored.y;

            return _restAnchored.y + Mathf.Max(height, topInParent - restCenterY + height);
        }

        private void SetAnchoredY(float y)
        {
            var root = transform as RectTransform;
            if (root == null)
            {
                return;
            }

            var pos = root.anchoredPosition;
            pos.y = y;
            root.anchoredPosition = pos;
        }

        private static RectTransform EnsureChild(
            RectTransform parent,
            string name,
            out Image image,
            bool asLastSibling)
        {
            var child = parent.Find(name) as RectTransform;
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                child = go.GetComponent<RectTransform>();
                child.SetParent(parent, false);
            }

            image = child.GetComponent<Image>();
            if (image == null)
            {
                image = child.gameObject.AddComponent<Image>();
            }

            if (asLastSibling)
            {
                child.SetAsLastSibling();
            }
            else
            {
                child.SetAsFirstSibling();
            }

            return child;
        }

        private static void StretchFill(RectTransform rt)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
    }
}

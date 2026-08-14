using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace FracturedChorus.RunMap.UI
{
    public class RunMapBackgroundView : MonoBehaviour
    {
        private const string DefaultSpritePath =
            "Assets/FracturedChorus/Art/UI/RunMap/Backgrounds/runmap_stage_background_v1.png";
        private const string DefaultVideoPath =
            "Assets/FracturedChorus/Art/UI/RunMap/Backgrounds/runmap_stage_background_anim_v1.mp4";

        [SerializeField] private Image backgroundImage;
        [SerializeField] private RawImage videoImage;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private VideoClip backgroundVideo;
        [SerializeField] private bool preferVideo = true;
        [SerializeField] private bool loopVideo = true;
        [SerializeField] [Range(0f, 1f)] private float videoAlpha = 1f;
        [SerializeField] private Vector2 videoAspect = new Vector2(9f, 16f);

        private static readonly Vector2 BottomAnchor = new Vector2(0.5f, 0f);

        private VideoPlayer _videoPlayer;
        private RenderTexture _renderTexture;

        private void Awake()
        {
            EnsureComponents();
            ApplyBackground();
        }

        private void OnDestroy()
        {
            ReleaseRenderTexture();
        }

        public void Configure(Sprite sprite, VideoClip video = null)
        {
            backgroundSprite = sprite;
            if (video != null)
            {
                backgroundVideo = video;
            }

            ApplyBackground();
        }

        public void SyncContentRect(float width, float height)
        {
            EnsureComponents();

            var rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = BottomAnchor;
                rect.anchorMax = BottomAnchor;
                rect.pivot = BottomAnchor;
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(width, height);
            }

            LayoutVideoSurface(width, height);
            ApplyBackground();
        }

        private void LayoutVideoSurface(float contentWidth, float contentHeight)
        {
            if (videoImage == null)
            {
                return;
            }

            var videoRect = videoImage.rectTransform;
            var aspect = videoAspect.y / Mathf.Max(1f, videoAspect.x);
            var fitWidth = contentWidth;
            var fitHeight = fitWidth * aspect;

            if (fitHeight < contentHeight)
            {
                fitHeight = contentHeight;
                fitWidth = fitHeight / aspect;
            }

            videoRect.anchorMin = BottomAnchor;
            videoRect.anchorMax = BottomAnchor;
            videoRect.pivot = BottomAnchor;
            videoRect.anchoredPosition = Vector2.zero;
            videoRect.sizeDelta = new Vector2(fitWidth, fitHeight);
        }

        private void EnsureComponents()
        {
            var rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = BottomAnchor;
                rect.anchorMax = BottomAnchor;
                rect.pivot = BottomAnchor;
            }

            backgroundImage ??= GetComponent<Image>();
            if (backgroundImage == null)
            {
                backgroundImage = gameObject.AddComponent<Image>();
            }

            backgroundImage.raycastTarget = false;
            backgroundImage.preserveAspect = false;
            backgroundImage.type = Image.Type.Simple;

            if (videoImage == null)
            {
                var videoGo = transform.Find("VideoSurface");
                if (videoGo != null)
                {
                    videoImage = videoGo.GetComponent<RawImage>();
                }
            }

            if (videoImage == null)
            {
                var videoGo = new GameObject("VideoSurface", typeof(RectTransform), typeof(RawImage));
                videoGo.transform.SetParent(transform, false);
                videoImage = videoGo.GetComponent<RawImage>();
            }

            videoImage.raycastTarget = false;
        }

        private void ApplyBackground()
        {
            EnsureComponents();
            ResolveDefaultAssets();

            if (preferVideo && TryApplyVideo())
            {
                return;
            }

            ApplySprite();
        }

        private void ResolveDefaultAssets()
        {
#if UNITY_EDITOR
            if (backgroundVideo == null)
            {
                backgroundVideo = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>(DefaultVideoPath);
            }

            if (backgroundSprite == null)
            {
                backgroundSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(DefaultSpritePath);
            }
#endif
        }

        private bool TryApplyVideo()
        {
            StopVideo();

            if (backgroundVideo == null || videoImage == null)
            {
                return false;
            }

            var width = Mathf.Max(64, (int)backgroundVideo.width);
            var height = Mathf.Max(64, (int)backgroundVideo.height);
            AllocateRenderTexture(width, height);

            _videoPlayer = gameObject.GetComponent<VideoPlayer>();
            if (_videoPlayer == null)
            {
                _videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }

            _videoPlayer.playOnAwake = false;
            _videoPlayer.isLooping = loopVideo;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.targetTexture = _renderTexture;
            _videoPlayer.clip = backgroundVideo;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            _videoPlayer.aspectRatio = VideoAspectRatio.FitVertically;

            videoImage.texture = _renderTexture;
            videoImage.color = new Color(1f, 1f, 1f, videoAlpha);
            videoImage.enabled = true;

            if (backgroundImage != null)
            {
                backgroundImage.enabled = false;
            }

            _videoPlayer.Prepare();
            _videoPlayer.prepareCompleted += OnVideoPrepared;
            return true;
        }

        private void OnVideoPrepared(VideoPlayer source)
        {
            source.prepareCompleted -= OnVideoPrepared;
            source.Play();
        }

        private void ApplySprite()
        {
            StopVideo();

            if (videoImage != null)
            {
                videoImage.enabled = false;
            }

            if (backgroundImage == null)
            {
                return;
            }

            if (backgroundSprite == null)
            {
                backgroundImage.enabled = false;
                return;
            }

            backgroundImage.enabled = true;
            backgroundImage.sprite = backgroundSprite;
            backgroundImage.color = Color.white;
        }

        private void StopVideo()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.prepareCompleted -= OnVideoPrepared;
                if (_videoPlayer.isPlaying)
                {
                    _videoPlayer.Stop();
                }

                _videoPlayer.clip = null;
            }
        }

        private void AllocateRenderTexture(int width, int height)
        {
            if (_renderTexture != null && _renderTexture.width == width && _renderTexture.height == height)
            {
                return;
            }

            ReleaseRenderTexture();
            _renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = "RunMapBackgroundVideo",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            _renderTexture.Create();
        }

        private void ReleaseRenderTexture()
        {
            if (_renderTexture == null)
            {
                return;
            }

            if (_videoPlayer != null)
            {
                _videoPlayer.targetTexture = null;
            }

            _renderTexture.Release();
            Destroy(_renderTexture);
            _renderTexture = null;
        }
    }
}

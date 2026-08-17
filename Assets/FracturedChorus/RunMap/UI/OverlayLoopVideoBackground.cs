using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace FracturedChorus.RunMap.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class OverlayLoopVideoBackground : MonoBehaviour
    {
        [SerializeField] private Image fallbackImage;
        [SerializeField] private RawImage videoImage;
        [SerializeField] private VideoClip clip;
        [SerializeField] private Sprite fallbackSprite;
        [SerializeField] private Color missingColor = new Color(0.22f, 0.06f, 0.34f, 1f);

        private VideoPlayer _videoPlayer;
        private RenderTexture _renderTexture;

        public void Bind(Image fallback, VideoClip video, Sprite sprite, Color missing)
        {
            fallbackImage = fallback;
            clip = video;
            fallbackSprite = sprite;
            missingColor = missing;
            EnsureVideoSurface();
            Apply();
        }

        private void OnEnable()
        {
            if (clip != null || fallbackImage != null)
            {
                Apply();
            }
        }

        private void OnDisable()
        {
            StopVideo();
        }

        private void OnDestroy()
        {
            StopVideo();
            ReleaseRenderTexture();
        }

        private void Apply()
        {
            EnsureVideoSurface();
            ApplyFallback();

            if (clip != null && TryApplyVideo())
            {
                return;
            }

            if (videoImage != null)
            {
                videoImage.enabled = false;
            }
        }

        private void EnsureVideoSurface()
        {
            if (videoImage == null)
            {
                var existing = transform.Find("VideoSurface");
                if (existing != null)
                {
                    videoImage = existing.GetComponent<RawImage>();
                }
            }

            if (videoImage == null)
            {
                var go = new GameObject("VideoSurface", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                go.transform.SetParent(transform, false);
                videoImage = go.GetComponent<RawImage>();
            }

            var rt = videoImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            videoImage.raycastTarget = false;
            videoImage.color = Color.white;

            if (fallbackImage != null)
            {
                videoImage.transform.SetSiblingIndex(fallbackImage.transform.GetSiblingIndex() + 1);
            }
        }

        private void ApplyFallback()
        {
            if (fallbackImage == null)
            {
                return;
            }

            fallbackImage.raycastTarget = true;
            fallbackImage.preserveAspect = false;
            if (fallbackSprite != null)
            {
                fallbackImage.sprite = fallbackSprite;
                fallbackImage.color = Color.white;
            }
            else
            {
                fallbackImage.sprite = null;
                fallbackImage.color = missingColor;
            }
        }

        private bool TryApplyVideo()
        {
            if (clip == null || videoImage == null)
            {
                return false;
            }

            StopVideo();

            var width = Mathf.Max(64, (int)clip.width);
            var height = Mathf.Max(64, (int)clip.height);
            AllocateRenderTexture(width, height);

            _videoPlayer = GetComponent<VideoPlayer>();
            if (_videoPlayer == null)
            {
                _videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }

            _videoPlayer.playOnAwake = false;
            _videoPlayer.isLooping = true;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.targetTexture = _renderTexture;
            _videoPlayer.clip = clip;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            _videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
            _videoPlayer.skipOnDrop = true;

            videoImage.texture = _renderTexture;
            videoImage.color = Color.white;
            videoImage.enabled = true;

            _videoPlayer.prepareCompleted -= OnVideoPrepared;
            _videoPlayer.prepareCompleted += OnVideoPrepared;
            _videoPlayer.Prepare();
            return true;
        }

        private void OnVideoPrepared(VideoPlayer source)
        {
            source.prepareCompleted -= OnVideoPrepared;
            if (source == null || !source.isPrepared)
            {
                return;
            }

            source.Play();
        }

        private void StopVideo()
        {
            if (_videoPlayer == null)
            {
                return;
            }

            _videoPlayer.prepareCompleted -= OnVideoPrepared;
            if (_videoPlayer.isPlaying)
            {
                _videoPlayer.Stop();
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
                name = "OverlayLoopVideo",
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
            if (Application.isPlaying)
            {
                Destroy(_renderTexture);
            }
            else
            {
                DestroyImmediate(_renderTexture);
            }

            _renderTexture = null;
        }
    }
}

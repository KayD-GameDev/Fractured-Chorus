using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace FracturedChorus.Combat.Presentation
{
    public sealed class LuxeArenaBackgroundDirector : MonoBehaviour
    {
        [SerializeField] private LuxeArenaBackgroundConfig config;
        [SerializeField] private RawImage sceneVideoImage;

        private VideoPlayer _videoPlayer;
        private RenderTexture _renderTexture;

        public LuxeArenaBackgroundConfig Config => config;

        private void OnEnable()
        {
            ApplyVideoBackground();
        }

        private void OnDisable()
        {
            StopVideo();
        }

        private void OnDestroy()
        {
            ReleaseRenderTexture();
        }

        private void ApplyVideoBackground()
        {
            var clip = config != null ? config.SceneBackgroundVideo : null;
            if (clip == null || sceneVideoImage == null)
            {
                StopVideo();
                if (sceneVideoImage != null)
                {
                    sceneVideoImage.enabled = false;
                }

                return;
            }

            StartVideo(clip);
        }

        private void StartVideo(VideoClip clip)
        {
            StopVideo();

            var width = Mathf.Max(64, (int)clip.width);
            var height = Mathf.Max(64, (int)clip.height);
            AllocateRenderTexture(width, height);

            _videoPlayer = gameObject.GetComponent<VideoPlayer>();
            if (_videoPlayer == null)
            {
                _videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }

            _videoPlayer.playOnAwake = false;
            _videoPlayer.isLooping = config == null || config.LoopSceneVideo;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.targetTexture = _renderTexture;
            _videoPlayer.clip = clip;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            _videoPlayer.aspectRatio = VideoAspectRatio.Stretch;

            var alpha = config != null ? config.SceneVideoAlpha : 1f;
            sceneVideoImage.texture = _renderTexture;
            sceneVideoImage.color = new Color(1f, 1f, 1f, alpha);
            sceneVideoImage.enabled = true;
            sceneVideoImage.rectTransform.SetAsFirstSibling();

            _videoPlayer.Prepare();
            _videoPlayer.prepareCompleted += OnVideoPrepared;
        }

        private void OnVideoPrepared(VideoPlayer source)
        {
            source.prepareCompleted -= OnVideoPrepared;
            source.Play();
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

            if (sceneVideoImage != null)
            {
                sceneVideoImage.enabled = false;
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
                name = "LuxeArenaSceneVideo",
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

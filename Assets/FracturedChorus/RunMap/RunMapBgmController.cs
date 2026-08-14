using FracturedChorus.Audio;
using UnityEngine;

namespace FracturedChorus.RunMap
{
    [RequireComponent(typeof(AudioSource))]
    public class RunMapBgmController : MonoBehaviour
    {
        [SerializeField] private AudioClip worldMapClip;
        [SerializeField] [Range(0f, 1f)] private float volume = 0.5f;

        private AudioSource _source;
        private float _normalVolume;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _normalVolume = volume;
            _source.volume = volume;
            _source.spatialBlend = 0f;
        }

        private void Start()
        {
            if (IsCandenceSessionActive())
            {
                StopLoop();
                return;
            }

            StartLoop();
        }

        private void Update()
        {
            if (IsCandenceSessionActive() && IsPlaying)
            {
                StopLoop();
            }
        }

        public bool IsPlaying => _source != null && _source.isPlaying;

        public void SetClip(AudioClip clip)
        {
            worldMapClip = clip;
            if (_source == null)
            {
                return;
            }

            _source.clip = worldMapClip;
        }

        public void StartLoop()
        {
            if (IsCandenceSessionActive())
            {
                StopLoop();
                return;
            }

            if (worldMapClip == null)
            {
                Debug.LogWarning("[Fractured Chorus] RunMapBgmController: worldMapClip is not assigned.");
                return;
            }

            _source.clip = worldMapClip;
            if (!_source.isPlaying)
            {
                _source.volume = _normalVolume;
                _source.Play();
            }
        }

        public void StopLoop()
        {
            if (_source != null && _source.isPlaying)
            {
                _source.Stop();
            }
        }

        public static void StopAll()
        {
            var controllers = Object.FindObjectsByType<RunMapBgmController>(FindObjectsInactive.Include);
            for (var i = 0; i < controllers.Length; i++)
            {
                controllers[i]?.StopLoop();
            }
        }

        public void SetVolume(float value)
        {
            _normalVolume = Mathf.Clamp01(value);
            if (_source != null)
            {
                _source.volume = _normalVolume;
            }
        }

        private static bool IsCandenceSessionActive() =>
            RunMusicSession.Instance != null && RunMusicSession.Instance.IsActive;
    }
}

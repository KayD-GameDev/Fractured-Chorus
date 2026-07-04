using UnityEngine;

namespace FracturedChorus.Menu
{
    [RequireComponent(typeof(AudioSource))]
    public class MainMenuBgmController : MonoBehaviour
    {
        [SerializeField] private AudioClip menuClip;
        [SerializeField] [Range(0f, 1f)] private float volume = 0.65f;

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

        public bool IsPlaying => _source != null && _source.isPlaying;

        public void SetClip(AudioClip clip)
        {
            menuClip = clip;
            if (_source == null)
            {
                return;
            }

            _source.clip = menuClip;
        }

        public void StartLoop()
        {
            if (menuClip == null)
            {
                Debug.LogWarning("[Fractured Chorus] MainMenuBgmController: menuClip is not assigned.");
                return;
            }

            _source.clip = menuClip;
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

        public void Duck(float multiplier)
        {
            if (_source == null)
            {
                return;
            }

            _source.volume = _normalVolume * Mathf.Clamp01(multiplier);
        }

        public void RestoreVolume()
        {
            SetVolume(_normalVolume);
        }

        public void ApplyMasterVolume(float masterVolume)
        {
            if (_source == null)
            {
                return;
            }

            _source.volume = _normalVolume * Mathf.Clamp01(masterVolume);
        }

        public void SetVolume(float value)
        {
            _normalVolume = Mathf.Clamp01(value);
            if (_source != null)
            {
                _source.volume = _normalVolume;
            }
        }
    }
}

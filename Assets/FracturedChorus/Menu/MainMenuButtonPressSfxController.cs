using UnityEngine;

namespace FracturedChorus.Menu
{
    [RequireComponent(typeof(AudioSource))]
    public class MainMenuButtonPressSfxController : MonoBehaviour
    {
        [SerializeField] private AudioClip buttonPressClip;
        [SerializeField] [Range(0.1f, 2f)] private float volume = 1f;

        private AudioSource _source;
        private float _baseVolume = 1f;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.volume = volume;
            _source.spatialBlend = 0f;
            _baseVolume = volume;
            PreloadClip();
        }

        public void ApplyMasterVolume(float masterVolume)
        {
            if (_source != null)
            {
                _source.volume = _baseVolume * Mathf.Clamp01(masterVolume);
            }
        }

        public void Configure(AudioClip clip, float sfxVolume = 1f)
        {
            buttonPressClip = clip;
            volume = sfxVolume;
            _baseVolume = sfxVolume;
            if (_source != null)
            {
                _source.volume = volume;
            }

            PreloadClip();
        }

        public void PlayButtonPress()
        {
            if (buttonPressClip == null || _source == null)
            {
                return;
            }

            PreloadClip();
            _source.Stop();
            _source.clip = buttonPressClip;
            _source.time = 0f;
            _source.Play();
        }

        public void StopButtonPress()
        {
            if (_source != null && _source.isPlaying)
            {
                _source.Stop();
            }
        }

        private void PreloadClip()
        {
            if (buttonPressClip == null)
            {
                return;
            }

            if (!buttonPressClip.preloadAudioData)
            {
                buttonPressClip.LoadAudioData();
            }
        }
    }
}

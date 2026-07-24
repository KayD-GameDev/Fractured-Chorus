using System;
using System.Collections;
using UnityEngine;

namespace FracturedChorus.Menu
{
    [RequireComponent(typeof(AudioSource))]
    public class MainMenuTransitionSfxController : MonoBehaviour
    {
        [SerializeField] private AudioClip changeMenuClip;
        [SerializeField] [Range(0.5f, 2f)] private float volume = 1.45f;

        private AudioSource _source;
        private float _baseVolume = 1.45f;

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
                _source.volume = _baseVolume;
            }
        }

        public void Configure(AudioClip clip, float sfxVolume = 1.45f)
        {
            changeMenuClip = clip;
            volume = sfxVolume;
            _baseVolume = sfxVolume;
            if (_source != null)
            {
                _source.volume = volume;
            }

            PreloadClip();
        }

        private void PreloadClip()
        {
            if (changeMenuClip == null)
            {
                return;
            }

            if (!changeMenuClip.preloadAudioData)
            {
                changeMenuClip.LoadAudioData();
            }
        }

        public float GetChangeMenuDuration()
        {
            if (changeMenuClip == null)
            {
                return 0.55f;
            }

            return changeMenuClip.length;
        }

        public void PlayChangeMenu()
        {
            if (changeMenuClip == null || _source == null)
            {
                return;
            }

            PreloadClip();
            _source.PlayOneShot(changeMenuClip, _source.volume);
        }

        public IEnumerator WaitUntilFinishedRoutine()
        {
            while (_source != null && _source.isPlaying)
            {
                yield return null;
            }
        }
    }
}

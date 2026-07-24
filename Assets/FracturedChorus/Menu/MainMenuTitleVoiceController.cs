using System;
using System.Collections;
using UnityEngine;

namespace FracturedChorus.Menu
{
    [RequireComponent(typeof(AudioSource))]
    public class MainMenuTitleVoiceController : MonoBehaviour
    {
        [SerializeField] private AudioClip femaleVoiceClip;
        [SerializeField] private AudioClip maleVoiceClip;
        [SerializeField] [Range(0f, 1f)] private float volume = 1f;

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
        }

        public void ApplyMasterVolume(float masterVolume)
        {
            if (_source != null)
            {
                _source.volume = _baseVolume;
            }
        }

        public void Configure(AudioClip femaleClip, AudioClip maleClip)
        {
            femaleVoiceClip = femaleClip;
            maleVoiceClip = maleClip;
        }

        public IEnumerator PlayRandomIntroRoutine(
            float bgmLeadInSeconds = 0.45f,
            float bgmLeadInProgress = 0.48f,
            Action onBgmLeadIn = null)
        {
            var clip = PickRandomClip();
            if (clip == null)
            {
                onBgmLeadIn?.Invoke();
                yield break;
            }

            _source.clip = clip;
            _source.Play();

            var leadInTriggered = false;
            var leadInByTime = clip.length - bgmLeadInSeconds;
            var leadInByProgress = clip.length * Mathf.Clamp01(bgmLeadInProgress);
            var leadInTime = Mathf.Max(0f, Mathf.Min(leadInByTime, leadInByProgress));

            while (_source.isPlaying)
            {
                if (!leadInTriggered && onBgmLeadIn != null && _source.time >= leadInTime)
                {
                    leadInTriggered = true;
                    onBgmLeadIn.Invoke();
                }

                yield return null;
            }

            if (!leadInTriggered)
            {
                onBgmLeadIn?.Invoke();
            }
        }

        public void StopIntro()
        {
            if (_source != null && _source.isPlaying)
            {
                _source.Stop();
            }
        }

        private AudioClip PickRandomClip()
        {
            if (femaleVoiceClip == null)
            {
                return maleVoiceClip;
            }

            if (maleVoiceClip == null)
            {
                return femaleVoiceClip;
            }

            return UnityEngine.Random.value < 0.5f ? femaleVoiceClip : maleVoiceClip;
        }
    }
}

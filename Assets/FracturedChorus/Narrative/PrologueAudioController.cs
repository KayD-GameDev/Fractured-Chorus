using System.Collections;
using UnityEngine;

namespace FracturedChorus.Narrative
{
    public class PrologueAudioController : MonoBehaviour
    {
        [SerializeField] private AudioClip bgmClip;
        [SerializeField] private AudioClip butterflyWingsClip;
        [SerializeField] private AudioClip typingClip;
        [SerializeField] private AudioClip penSignClip;
        [SerializeField] private AudioClip buttonPressClip;
        [SerializeField] private AudioClip menuTingClip;
        [SerializeField] private float bgmVolume = 0.46f;
        [SerializeField] private float butterflyVolume = 0.26f;
        [SerializeField] private float typingVolume = 0.55f;
        [SerializeField] private float penSignVolume = 0.7f;
        [SerializeField] private float buttonPressVolume = 0.85f;
        [SerializeField] private float menuTingVolume = 1f;

        private AudioSource _bgmSource;
        private AudioSource _butterflySource;
        private AudioSource _typingSource;
        private AudioSource _sfxSource;

        private void Awake()
        {
            _bgmSource = CreateSource("PrologueBgm", true, false);
            _butterflySource = CreateSource("PrologueButterfly", true, false);
            _typingSource = CreateSource("PrologueTyping", false, false);
            _sfxSource = CreateSource("PrologueSfx", false, false);
        }

        public void StartBgm()
        {
            if (bgmClip == null || _bgmSource == null)
            {
                return;
            }

            _bgmSource.clip = bgmClip;
            _bgmSource.volume = bgmVolume;
            _bgmSource.loop = true;
            _bgmSource.Play();
        }

        public void StartButterflyWings()
        {
            if (butterflyWingsClip == null || _butterflySource == null)
            {
                return;
            }

            _butterflySource.clip = butterflyWingsClip;
            _butterflySource.volume = butterflyVolume;
            _butterflySource.loop = true;
            _butterflySource.pitch = 1f;
            _butterflySource.Play();

            if (_bgmSource != null && _bgmSource.isPlaying)
            {
                _bgmSource.volume = bgmVolume * 0.82f;
            }
        }

        public void StopButterflyWings()
        {
            if (_butterflySource != null && _butterflySource.isPlaying)
            {
                _butterflySource.Stop();
            }

            if (_bgmSource != null && _bgmSource.isPlaying)
            {
                _bgmSource.volume = bgmVolume;
            }
        }

        public void BeginTypingLine()
        {
            if (typingClip == null || _typingSource == null)
            {
                return;
            }

            StopTypingLine();
            _typingSource.clip = typingClip;
            _typingSource.loop = true;
            _typingSource.time = 0f;
            _typingSource.pitch = 1f;
            _typingSource.volume = typingVolume;
            _typingSource.Play();
        }

        public void StopTypingLine()
        {
            if (_typingSource != null && _typingSource.isPlaying)
            {
                _typingSource.Stop();
            }
        }

        public void PlayPenSign()
        {
            if (penSignClip == null || _sfxSource == null)
            {
                return;
            }

            _sfxSource.pitch = 1f;
            _sfxSource.volume = penSignVolume;
            _sfxSource.PlayOneShot(penSignClip);
        }

        public void PlayButtonPress()
        {
            if (buttonPressClip == null || _sfxSource == null)
            {
                return;
            }

            _sfxSource.pitch = 1f;
            _sfxSource.volume = buttonPressVolume;
            _sfxSource.PlayOneShot(buttonPressClip);
        }

        public float PlayMenuTing()
        {
            if (menuTingClip == null || _sfxSource == null)
            {
                return 0f;
            }

            _sfxSource.pitch = 1f;
            _sfxSource.volume = menuTingVolume;
            _sfxSource.PlayOneShot(menuTingClip);
            return menuTingClip.length;
        }

        public void FadeOutAll(float duration)
        {
            StopTypingLine();
            StopAllCoroutines();
            StartCoroutine(FadeRoutine(duration));
        }

        private IEnumerator FadeRoutine(float duration)
        {
            var elapsed = 0f;
            var bgmStart = _bgmSource != null ? _bgmSource.volume : 0f;
            var wingStart = _butterflySource != null ? _butterflySource.volume : 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = 1f - Mathf.Clamp01(elapsed / duration);
                if (_bgmSource != null)
                {
                    _bgmSource.volume = bgmStart * t;
                }

                if (_butterflySource != null)
                {
                    _butterflySource.volume = wingStart * t;
                }

                yield return null;
            }

            if (_bgmSource != null)
            {
                _bgmSource.Stop();
            }

            StopButterflyWings();
        }

        private AudioSource CreateSource(string sourceName, bool loop, bool playOnAwake)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = playOnAwake;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }
    }
}

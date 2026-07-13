using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Narrative
{
    public class PrologueTypewriterView : MonoBehaviour
    {
        [SerializeField] private Text bodyText;
        [SerializeField] private float charsPerSecond = 32f;
        [SerializeField] private float punctuationPause = 0.14f;
        [SerializeField] private AudioClip typingClip;
        [SerializeField] private float typingVolume = 0.55f;

        private readonly StringBuilder _builder = new StringBuilder();
        private Coroutine _routine;
        private PrologueAudioController _audio;
        private AudioSource _localTypingSource;
        private Action _onComplete;
        private bool _localTypingActive;

        public bool IsTyping { get; private set; }

        public Text BodyText => bodyText;

        public void Bind(PrologueAudioController audio)
        {
            _audio = audio;
        }

        public void BindTypingClip(AudioClip clip, float volume = 0.55f)
        {
            typingClip = clip;
            typingVolume = volume;
            EnsureLocalTypingSource();
        }

        public void Clear()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            StopTypingSound();
            IsTyping = false;
            _onComplete = null;
            _builder.Clear();
            if (bodyText != null)
            {
                bodyText.text = string.Empty;
            }
        }

        public void SetInstant(string text)
        {
            Clear();
            if (bodyText != null)
            {
                bodyText.text = text ?? string.Empty;
            }
        }

        public void Type(string text, Action onComplete = null)
        {
            Clear();
            _onComplete = onComplete;
            if (bodyText == null || string.IsNullOrEmpty(text))
            {
                CompleteTyping();
                return;
            }

            _routine = StartCoroutine(TypeRoutine(text));
        }

        public void SkipToEnd(string text)
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            IsTyping = false;
            if (bodyText != null)
            {
                bodyText.text = text ?? string.Empty;
            }

            CompleteTyping();
        }

        private IEnumerator TypeRoutine(string text)
        {
            IsTyping = true;
            _builder.Clear();
            bodyText.text = string.Empty;
            BeginTypingSound();

            var delay = 1f / Mathf.Max(1f, charsPerSecond);
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                _builder.Append(c);
                bodyText.text = _builder.ToString();

                var wait = delay;
                if (c == '\n' || c == '.' || c == '?' || c == '!' || c == ',')
                {
                    wait += punctuationPause;
                }

                yield return new WaitForSecondsRealtime(wait);
            }

            IsTyping = false;
            _routine = null;
            CompleteTyping();
        }

        private void CompleteTyping()
        {
            IsTyping = false;
            StopTypingSound();
            var callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
        }

        public void BeginTypingSound()
        {
            if (_audio != null)
            {
                _audio.BeginTypingLine();
                return;
            }

            if (typingClip == null)
            {
                return;
            }

            EnsureLocalTypingSource();
            if (_localTypingSource == null)
            {
                return;
            }

            _localTypingActive = true;
            _localTypingSource.clip = typingClip;
            _localTypingSource.loop = true;
            _localTypingSource.time = 0f;
            _localTypingSource.pitch = 1f;
            _localTypingSource.volume = typingVolume;
            _localTypingSource.Play();
        }

        public void StopTypingSound()
        {
            if (_audio != null)
            {
                _audio.StopTypingLine();
            }

            _localTypingActive = false;
            if (_localTypingSource != null && _localTypingSource.isPlaying)
            {
                _localTypingSource.Stop();
            }
        }

        private void EnsureLocalTypingSource()
        {
            if (_localTypingSource != null)
            {
                return;
            }

            var go = new GameObject("VnTyping");
            go.transform.SetParent(transform, false);
            _localTypingSource = go.AddComponent<AudioSource>();
            _localTypingSource.playOnAwake = false;
            _localTypingSource.loop = true;
        }

        private void OnDisable()
        {
            if (_localTypingActive)
            {
                StopTypingSound();
            }
        }
    }
}

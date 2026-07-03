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

        private readonly StringBuilder _builder = new StringBuilder();
        private Coroutine _routine;
        private PrologueAudioController _audio;
        private Action _onComplete;

        public bool IsTyping { get; private set; }

        public void Bind(PrologueAudioController audio)
        {
            _audio = audio;
        }

        public void Clear()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            _audio?.StopTypingLine();
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
            _audio?.BeginTypingLine();

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
            _audio?.StopTypingLine();
            var callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
        }
    }
}

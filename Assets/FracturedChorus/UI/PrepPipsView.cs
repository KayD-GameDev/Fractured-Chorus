using System.Collections;
using FracturedChorus.Combat.Units;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class PrepPipsView : MonoBehaviour
    {
        private static readonly Color PipOn = new Color(0.45f, 0.85f, 1f, 1f);
        private static readonly Color PipOff = new Color(0.2f, 0.22f, 0.28f, 0.55f);
        private static readonly Color PipFlash = new Color(1f, 0.95f, 0.55f, 1f);

        private readonly Image[] _pips = new Image[CombatUnit.PrepCap];
        private RectTransform _root;
        private int _displayed;
        private Coroutine _feedbackRoutine;

        public static PrepPipsView EnsureOn(RectTransform parent)
        {
            if (parent == null)
            {
                return null;
            }

            var existing = parent.Find("PrepPips")?.GetComponent<PrepPipsView>();
            if (existing != null)
            {
                existing.EnsureBuilt();
                return existing;
            }

            var go = new GameObject("PrepPips", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(6f, -6f);
            rt.sizeDelta = new Vector2(54f, 14f);

            var view = go.AddComponent<PrepPipsView>();
            view.EnsureBuilt();
            return view;
        }

        public void EnsureBuilt()
        {
            _root = transform as RectTransform;
            if (_root == null)
            {
                return;
            }

            for (var i = 0; i < _pips.Length; i++)
            {
                if (_pips[i] != null)
                {
                    continue;
                }

                var child = _root.Find($"Pip_{i}")?.GetComponent<Image>();
                if (child == null)
                {
                    var pipGo = new GameObject($"Pip_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    var pipRt = pipGo.GetComponent<RectTransform>();
                    pipRt.SetParent(_root, false);
                    pipRt.anchorMin = new Vector2(0f, 0.5f);
                    pipRt.anchorMax = new Vector2(0f, 0.5f);
                    pipRt.pivot = new Vector2(0.5f, 0.5f);
                    pipRt.sizeDelta = new Vector2(12f, 12f);
                    pipRt.anchoredPosition = new Vector2(7f + i * 16f, 0f);
                    child = pipGo.GetComponent<Image>();
                    child.sprite = UiCircleSpriteUtil.Circle;
                    child.raycastTarget = false;
                }

                _pips[i] = child;
            }

            ApplyVisual(_displayed, animate: false);
        }

        public void SetPrep(int prep, bool animate)
        {
            EnsureBuilt();
            prep = Mathf.Clamp(prep, 0, CombatUnit.PrepCap);
            var gained = prep > _displayed;
            var spent = prep < _displayed;
            _displayed = prep;
            ApplyVisual(prep, animate);

            if (!animate || (!gained && !spent))
            {
                return;
            }

            if (_feedbackRoutine != null)
            {
                StopCoroutine(_feedbackRoutine);
            }

            _feedbackRoutine = StartCoroutine(FeedbackRoutine(gained));
        }

        private void ApplyVisual(int prep, bool animate)
        {
            for (var i = 0; i < _pips.Length; i++)
            {
                var pip = _pips[i];
                if (pip == null)
                {
                    continue;
                }

                pip.color = i < prep ? PipOn : PipOff;
                if (!animate)
                {
                    pip.rectTransform.localScale = Vector3.one;
                }
            }
        }

        private IEnumerator FeedbackRoutine(bool gained)
        {
            var index = gained ? _displayed - 1 : _displayed;
            if (index < 0 || index >= _pips.Length)
            {
                _feedbackRoutine = null;
                yield break;
            }

            var pip = _pips[index];
            if (pip == null)
            {
                _feedbackRoutine = null;
                yield break;
            }

            pip.color = gained ? Color.white : PipFlash;
            pip.rectTransform.localScale = Vector3.one * 1.3f;
            yield return new WaitForSecondsRealtime(0.12f);
            ApplyVisual(_displayed, animate: false);
            _feedbackRoutine = null;
        }
    }
}

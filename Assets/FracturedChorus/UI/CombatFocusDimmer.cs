using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Darkens the battle scene except a small focus set, by tinting sprite/background colors.
    /// Deliberately avoids sorting-order changes so authored draw order stays intact.
    /// </summary>
    public class CombatFocusDimmer : MonoBehaviour
    {
        [SerializeField] private string backgroundCanvasName = "Background canvas";
        [SerializeField] [Range(0f, 1f)] private float dimFactor = 0.35f;
        [SerializeField] private float fadeSeconds = 0.12f;

        private readonly List<UnitView> _dimmed = new();
        private readonly List<Graphic> _backgroundGraphics = new();
        private readonly List<Color> _backgroundBaseColors = new();
        private Coroutine _fadeRoutine;
        private float _currentFactor = 1f;
        private bool _focusActive;

        public bool IsFocusActive => _focusActive;

        public void Configure(float focusDimFactor, float focusFadeSeconds)
        {
            dimFactor = Mathf.Clamp01(focusDimFactor);
            fadeSeconds = Mathf.Max(0f, focusFadeSeconds);
        }

        public void Focus(IReadOnlyList<UnitView> keepBright)
        {
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            ApplyFactor(1f);
            CollectTargets(keepBright);
            _focusActive = true;
            StartFade(dimFactor);
        }

        public void Release()
        {
            if (!_focusActive)
            {
                return;
            }

            _focusActive = false;
            StartFade(1f);
        }

        /// <summary>Instantly restore full brightness — used when a sequence is aborted.</summary>
        public void ReleaseImmediate()
        {
            _focusActive = false;
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            ApplyFactor(1f);
            ClearTargets();
        }

        private void CollectTargets(IReadOnlyList<UnitView> keepBright)
        {
            ClearTargets();
            _currentFactor = 1f;

            foreach (var view in FindObjectsByType<UnitView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (view == null || Contains(keepBright, view))
                {
                    continue;
                }

                _dimmed.Add(view);
            }

            var backgroundRoot = GameObject.Find(backgroundCanvasName);
            if (backgroundRoot == null)
            {
                return;
            }

            foreach (var graphic in backgroundRoot.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic == null)
                {
                    continue;
                }

                _backgroundGraphics.Add(graphic);
                _backgroundBaseColors.Add(graphic.color);
            }
        }

        private void ClearTargets()
        {
            _dimmed.Clear();
            _backgroundGraphics.Clear();
            _backgroundBaseColors.Clear();
        }

        private static bool Contains(IReadOnlyList<UnitView> views, UnitView view)
        {
            if (views == null)
            {
                return false;
            }

            for (var i = 0; i < views.Count; i++)
            {
                if (views[i] == view)
                {
                    return true;
                }
            }

            return false;
        }

        private void StartFade(float targetFactor)
        {
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            if (!isActiveAndEnabled || fadeSeconds <= 0f)
            {
                ApplyFactor(targetFactor);
                if (targetFactor >= 1f)
                {
                    ClearTargets();
                }

                return;
            }

            _fadeRoutine = StartCoroutine(FadeRoutine(targetFactor));
        }

        private IEnumerator FadeRoutine(float targetFactor)
        {
            var from = _currentFactor;
            var t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.deltaTime;
                ApplyFactor(Mathf.Lerp(from, targetFactor, Mathf.Clamp01(t / fadeSeconds)));
                yield return null;
            }

            ApplyFactor(targetFactor);
            _fadeRoutine = null;

            if (targetFactor >= 1f)
            {
                ClearTargets();
            }
        }

        private void ApplyFactor(float factor)
        {
            _currentFactor = factor;

            for (var i = _dimmed.Count - 1; i >= 0; i--)
            {
                var view = _dimmed[i];
                if (view == null)
                {
                    _dimmed.RemoveAt(i);
                    continue;
                }

                view.SetVisualDimFactor(factor);
            }

            for (var i = 0; i < _backgroundGraphics.Count; i++)
            {
                var graphic = _backgroundGraphics[i];
                if (graphic == null)
                {
                    continue;
                }

                var baseColor = _backgroundBaseColors[i];
                graphic.color = new Color(
                    baseColor.r * factor,
                    baseColor.g * factor,
                    baseColor.b * factor,
                    baseColor.a);
            }
        }

        private void OnDisable()
        {
            ReleaseImmediate();
        }
    }
}

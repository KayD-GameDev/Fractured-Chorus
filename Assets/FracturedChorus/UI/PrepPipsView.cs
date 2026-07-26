using System.Collections;
using FracturedChorus.Combat.Units;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class PrepPipsView : MonoBehaviour
    {
        public enum LayoutMode
        {
            Circles,
            SegmentStrip
        }

        private static readonly Color PipOn = new Color(0.45f, 0.85f, 1f, 1f);
        private static readonly Color PipOff = new Color(0.12f, 0.14f, 0.18f, 0.75f);
        private static readonly Color PipFlash = new Color(1f, 0.95f, 0.55f, 1f);

        private const float SegmentGap = 1.5f;

        private readonly Image[] _pips = new Image[CombatUnit.PrepCap];
        private readonly bool[] _pipCreatedByCode = new bool[CombatUnit.PrepCap];
        private RectTransform _root;
        private LayoutMode _mode = LayoutMode.SegmentStrip;
        private bool _rootCreatedByCode;
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

            var gaugePrep = parent.Find("BarStack/GaugeSlot/PrepPips")?.GetComponent<PrepPipsView>();
            if (gaugePrep != null)
            {
                gaugePrep.EnsureBuilt();
                return gaugePrep;
            }

            var go = new GameObject("PrepPips", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            ApplyClassicRootLayout(rt);

            var view = go.AddComponent<PrepPipsView>();
            view._rootCreatedByCode = true;
            view.EnsureBuilt();
            return view;
        }

        public void SetLayoutMode(LayoutMode mode)
        {
            _mode = mode;
            EnsureBuilt();
            ApplyPipGeometry();
            ApplyVisual(_displayed, animate: false);
        }

        public void LayoutIn(RectTransform slot)
        {
            _root = transform as RectTransform;
            if (_root == null || slot == null)
            {
                return;
            }

            if (_root.parent != slot)
            {
                _root.SetParent(slot, false);
            }

            // Hierarchy đã author PrepPips → không đụng Rect. Chỉ fallback khi code tự tạo.
            if (_rootCreatedByCode && !RectSizeUtil.IsAuthored(_root))
            {
                _root.anchorMin = Vector2.zero;
                _root.anchorMax = Vector2.one;
                _root.pivot = new Vector2(0.5f, 0.5f);
                _root.offsetMin = Vector2.zero;
                _root.offsetMax = Vector2.zero;
                _root.localRotation = Quaternion.identity;
                _root.localScale = Vector3.one;
            }

            SetLayoutMode(LayoutMode.SegmentStrip);
        }

        public void LayoutClassic(RectTransform cardRoot)
        {
            _root = transform as RectTransform;
            if (_root == null || cardRoot == null)
            {
                return;
            }

            if (_root.parent != cardRoot)
            {
                _root.SetParent(cardRoot, false);
            }

            if (_rootCreatedByCode && !RectSizeUtil.IsAuthored(_root))
            {
                ApplyClassicRootLayout(_root);
            }

            SetLayoutMode(LayoutMode.SegmentStrip);
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
                    child = pipGo.GetComponent<Image>();
                    child.raycastTarget = false;
                    _pipCreatedByCode[i] = true;
                }

                _pips[i] = child;
            }

            ApplyPipGeometry();
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

        private void ApplyPipGeometry()
        {
            if (_mode == LayoutMode.SegmentStrip)
            {
                ApplySegmentStripGeometry();
            }
            else
            {
                ApplyCircleGeometry();
            }
        }

        private void ApplyCircleGeometry()
        {
            for (var i = 0; i < _pips.Length; i++)
            {
                var pip = _pips[i];
                if (pip == null || !_pipCreatedByCode[i])
                {
                    // Pip từ Hierarchy — chỉ đảm bảo có sprite circle nếu trống.
                    if (pip != null && pip.sprite == null)
                    {
                        pip.sprite = UiCircleSpriteUtil.Circle;
                    }

                    continue;
                }

                var pipRt = pip.rectTransform;
                pipRt.anchorMin = new Vector2(0f, 0.5f);
                pipRt.anchorMax = new Vector2(0f, 0.5f);
                pipRt.pivot = new Vector2(0.5f, 0.5f);
                pipRt.sizeDelta = new Vector2(12f, 12f);
                pipRt.anchoredPosition = new Vector2(7f + i * 16f, 0f);
                pip.sprite = UiCircleSpriteUtil.Circle;
                pip.type = Image.Type.Simple;
            }
        }

        private void ApplySegmentStripGeometry()
        {
            var count = _pips.Length;
            if (count <= 0)
            {
                return;
            }

            CullExtraPipChildren();

            var unit = 1f / count;
            for (var i = 0; i < count; i++)
            {
                var pip = _pips[i];
                if (pip == null)
                {
                    continue;
                }

                pip.gameObject.SetActive(true);

                // Pip từ CardTemplate Hierarchy → giữ nguyên Rect; chỉ đảm bảo sprite fill.
                if (!_pipCreatedByCode[i])
                {
                    if (pip.sprite == null)
                    {
                        pip.sprite = UiCircleSpriteUtil.White;
                    }

                    pip.type = Image.Type.Simple;
                    pip.preserveAspect = false;
                    pip.raycastTarget = false;
                    continue;
                }

                var pipRt = pip.rectTransform;
                pipRt.anchorMin = new Vector2(i * unit, 0f);
                pipRt.anchorMax = new Vector2((i + 1) * unit, 1f);
                pipRt.pivot = new Vector2(0.5f, 0.5f);
                pipRt.anchoredPosition = Vector2.zero;
                pipRt.sizeDelta = Vector2.zero;
                pipRt.offsetMin = new Vector2(i > 0 ? SegmentGap * 0.5f : 0f, 0f);
                pipRt.offsetMax = new Vector2(i < count - 1 ? -SegmentGap * 0.5f : 0f, 0f);
                pipRt.localScale = Vector3.one;
                pipRt.localRotation = Quaternion.identity;
                pip.sprite = UiCircleSpriteUtil.White;
                pip.type = Image.Type.Simple;
                pip.preserveAspect = false;
                pip.raycastTarget = false;
            }
        }

        /// <summary>Chỉ giữ Pip_0..Pip_{PrepCap-1}; tắt/xóa pip thừa.</summary>
        private void CullExtraPipChildren()
        {
            if (_root == null)
            {
                return;
            }

            for (var i = _root.childCount - 1; i >= 0; i--)
            {
                var child = _root.GetChild(i);
                if (child == null || !child.name.StartsWith("Pip_"))
                {
                    continue;
                }

                if (!int.TryParse(child.name.Substring("Pip_".Length), out var index) ||
                    index < 0 ||
                    index >= CombatUnit.PrepCap)
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(child.gameObject);
                    }
                    else
                    {
                        Object.DestroyImmediate(child.gameObject);
                    }
                }
            }
        }

        private static void ApplyClassicRootLayout(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(6f, -6f);
            rt.sizeDelta = new Vector2(54f, 14f);
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
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
            pip.rectTransform.localScale = Vector3.one * 1.15f;
            yield return new WaitForSecondsRealtime(0.12f);
            ApplyVisual(_displayed, animate: false);
            _feedbackRoutine = null;
        }
    }
}

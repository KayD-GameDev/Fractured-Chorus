using System;
using System.Collections;
using FracturedChorus.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.Menu
{
    [RequireComponent(typeof(Image))]
    public class MainMenuConfigToggleSwitch : MonoBehaviour, IPointerClickHandler
    {
        private const float AnimDuration = 0.18f;

        [SerializeField] private Slider visualSlider;
        [SerializeField] private Image graphic;
        [SerializeField] private Image trackImage;
        [SerializeField] private RectTransform knob;
        [SerializeField] private Text labelOn;
        [SerializeField] private Text labelOff;
        [SerializeField] private RectTransform knobOffAnchor;
        [SerializeField] private RectTransform knobOnAnchor;

        private Coroutine _anim;

        public event Action<bool> ValueChanged;

        public bool IsOn { get; private set; }

        private void Awake()
        {
            if (visualSlider == null)
            {
                visualSlider = GetComponent<Slider>();
            }

            if (visualSlider != null)
            {
                visualSlider.interactable = false;
                visualSlider.wholeNumbers = true;
                visualSlider.minValue = 0f;
                visualSlider.maxValue = 1f;
            }

            if (graphic == null)
            {
                graphic = GetComponent<Image>();
            }

            ResolveSceneParts();
            HideLegacy("Background");
            HideLegacy("Fill Area");
            HideLegacy("Handle Slide Area");
        }

        private void OnEnable()
        {
            GameLoc.Changed += RefreshLabels;
            RefreshLabels();
        }

        private void OnDisable()
        {
            GameLoc.Changed -= RefreshLabels;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            Toggle();
        }

        public void Toggle()
        {
            SetValue(!IsOn, notify: true);
        }

        public void SetValue(bool isOn, bool notify)
        {
            ResolveSceneParts();
            var changed = IsOn != isOn;
            IsOn = isOn;
            if (visualSlider != null)
            {
                visualSlider.SetValueWithoutNotify(isOn ? 1f : 0f);
            }

            if (!changed || !notify || !isActiveAndEnabled)
            {
                Snap(isOn);
            }
            else
            {
                if (_anim != null)
                {
                    StopCoroutine(_anim);
                }

                _anim = StartCoroutine(Animate(isOn));
            }

            if (notify)
            {
                ValueChanged?.Invoke(isOn);
            }
        }

        private void ResolveSceneParts()
        {
            if (trackImage == null)
            {
                trackImage = transform.Find("Track")?.GetComponent<Image>();
            }

            if (knob == null)
            {
                knob = transform.Find("Knob") as RectTransform;
            }

            if (labelOff == null)
            {
                labelOff = transform.Find("LabelOff")?.GetComponent<Text>();
            }

            if (labelOn == null)
            {
                labelOn = transform.Find("LabelOn")?.GetComponent<Text>();
            }

            if (knobOffAnchor == null)
            {
                knobOffAnchor = transform.Find("KnobAnchorOff") as RectTransform;
            }

            if (knobOnAnchor == null)
            {
                knobOnAnchor = transform.Find("KnobAnchorOn") as RectTransform;
            }
        }

        private void RefreshLabels()
        {
            if (labelOn != null)
            {
                labelOn.text = GameLoc.Get("settings.on");
            }

            if (labelOff != null)
            {
                labelOff.text = GameLoc.Get("settings.off");
            }

            ApplyLabelAlpha(IsOn);
        }

        private IEnumerator Animate(bool isOn)
        {
            var from = knob != null ? knob.anchoredPosition : Vector2.zero;
            var to = KnobPos(isOn);
            var elapsed = 0f;
            while (elapsed < AnimDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / AnimDuration));
                PlaceKnob(Vector2.Lerp(from, to, t));
                ApplyLabelAlpha(isOn, t);
                yield return null;
            }

            Snap(isOn);
            _anim = null;
        }

        private void Snap(bool isOn)
        {
            PlaceKnob(KnobPos(isOn));
            ApplyLabelAlpha(isOn);
        }

        private void PlaceKnob(Vector2 pos)
        {
            if (knob == null)
            {
                return;
            }

            knob.anchoredPosition = pos;
        }

        private Vector2 KnobPos(bool isOn)
        {
            var anchor = isOn ? knobOnAnchor : knobOffAnchor;
            return anchor != null ? anchor.anchoredPosition : knob != null ? knob.anchoredPosition : Vector2.zero;
        }

        private void ApplyLabelAlpha(bool isOn, float blend = 1f)
        {
            if (labelOn != null)
            {
                var color = labelOn.color;
                color.a = isOn ? blend : 1f - blend;
                labelOn.color = color;
            }

            if (labelOff != null)
            {
                var color = labelOff.color;
                color.a = isOn ? 1f - blend : blend;
                labelOff.color = color;
            }
        }

        private void HideLegacy(string childName)
        {
            var child = transform.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }

    }
}

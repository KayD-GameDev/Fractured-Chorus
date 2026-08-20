using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.Menu
{
    [RequireComponent(typeof(Image))]
    public class MainMenuConfigToggleSwitch : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Slider visualSlider;
        [SerializeField] private Image graphic;
        [SerializeField] private Sprite spriteOn;
        [SerializeField] private Sprite spriteOff;

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

            if (graphic != null)
            {
                graphic.raycastTarget = true;
                if (spriteOn == null && spriteOff == null && graphic.color.a <= 0.001f)
                {
                    graphic.color = new Color(1f, 1f, 1f, 0.001f);
                }
            }
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
            IsOn = isOn;
            ApplyVisual(isOn);
            if (notify)
            {
                ValueChanged?.Invoke(isOn);
            }
        }

        private void ApplyVisual(bool isOn)
        {
            if (visualSlider != null)
            {
                visualSlider.SetValueWithoutNotify(isOn ? 1f : 0f);
            }

            if (graphic == null)
            {
                return;
            }

            var sprite = isOn ? spriteOn : spriteOff;
            if (sprite != null)
            {
                graphic.sprite = sprite;
                graphic.color = Color.white;
            }
        }
    }
}

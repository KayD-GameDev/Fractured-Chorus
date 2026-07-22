using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.Narrative
{
    public class PrologueContractConfirmButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image target;
        [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.38f);
        [SerializeField] private Color hoverColor = Color.white;

        private bool _hovered;

        private void Awake()
        {
            if (target == null)
            {
                target = GetComponent<Image>();
            }

            ApplyVisual(false);
        }

        public void Configure(Image image)
        {
            target = image;
            ApplyVisual(_hovered);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            ApplyVisual(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            ApplyVisual(false);
        }

        private void ApplyVisual(bool hover)
        {
            if (target == null)
            {
                return;
            }

            target.color = hover ? hoverColor : idleColor;
        }
    }
}

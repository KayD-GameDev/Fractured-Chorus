using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.Narrative
{
    public class PrologueContractConfirmButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly Color LabelOnLightButton = new Color(0.10f, 0.14f, 0.28f, 1f);

        [SerializeField] private Image target;
        [SerializeField] private Text label;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite hoverSprite;

        private bool _hovered;

        private void Awake()
        {
            if (target == null)
            {
                target = GetComponent<Image>();
            }

            if (label == null)
            {
                label = transform.Find("Label")?.GetComponent<Text>();
            }

            ApplyVisual(false);
        }

        public void Configure(Image image)
        {
            Configure(image, null, null);
        }

        public void Configure(Image image, Sprite idle, Sprite hover)
        {
            target = image;
            if (idle != null)
            {
                idleSprite = idle;
            }

            if (hover != null)
            {
                hoverSprite = hover;
            }

            if (label == null)
            {
                label = transform.Find("Label")?.GetComponent<Text>();
            }

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
            if (target != null)
            {
                var sprite = hover
                    ? (hoverSprite != null ? hoverSprite : idleSprite)
                    : (idleSprite != null ? idleSprite : hoverSprite);
                if (sprite != null)
                {
                    target.sprite = sprite;
                }

                target.color = Color.white;
                target.preserveAspect = true;
            }

            if (label == null)
            {
                return;
            }

            label.gameObject.SetActive(true);
            label.raycastTarget = false;
            UiFontCatalog.Apply(label, UiFontRole.Display);
            label.color = LabelOnLightButton;
        }
    }
}

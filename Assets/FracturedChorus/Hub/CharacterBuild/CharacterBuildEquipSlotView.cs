using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub.CharacterBuild
{
    public sealed class CharacterBuildEquipSlotView : MonoBehaviour
    {
        private static readonly Color SelectedOutline = new Color(1f, 0.84f, 0.2f, 1f);

        [SerializeField] private Button button;
        [SerializeField] private Text label;
        [SerializeField] private Image frame;
        [SerializeField] private Image labelBackground;

        public Button Button => button;

        public void Bind(string text, bool visible, bool locked)
        {
            if (label != null)
            {
                label.text = text ?? string.Empty;
                var ink = CharacterBuildStatTheme.LabelColor;
                label.color = locked ? new Color(ink.r, ink.g, ink.b, 0.55f) : ink;
            }

            if (labelBackground != null)
            {
                labelBackground.enabled = false;
            }

            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }

            if (button != null)
            {
                button.interactable = visible && !locked;
            }
        }

        public void ApplyFrame(Sprite unlocked, Sprite lockedSprite, bool locked, bool selected)
        {
            var graphic = frame != null
                ? frame
                : button != null ? button.targetGraphic as Image : GetComponent<Image>();
            if (graphic == null)
            {
                return;
            }

            var sprite = unlocked != null ? unlocked : lockedSprite;
            if (sprite != null)
            {
                graphic.sprite = sprite;
                graphic.type = Image.Type.Simple;
                graphic.preserveAspect = false;
            }

            graphic.color = Color.white;
            ApplySelectedOutline(graphic.gameObject, !locked && selected);
        }

        private static void ApplySelectedOutline(GameObject target, bool selected)
        {
            if (target == null)
            {
                return;
            }

            var outline = target.GetComponent<Outline>();
            if (!selected)
            {
                if (outline != null)
                {
                    outline.enabled = false;
                }

                return;
            }

            if (outline == null)
            {
                outline = target.AddComponent<Outline>();
            }

            outline.enabled = true;
            outline.effectColor = SelectedOutline;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = true;
        }
    }
}

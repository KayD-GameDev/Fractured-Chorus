using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub.CharacterBuild
{
    public sealed class CharacterBuildEquipSlotView : MonoBehaviour
    {
        private static readonly Color SelectedOutline = new Color(1f, 0.84f, 0.2f, 1f);
        private static readonly Color UnlockedTint = Color.white;
        private static readonly Color LockedTint = new Color(0.78f, 0.82f, 0.9f, 1f);
        private static readonly Color LabelUnlocked = Color.white;
        private static readonly Color LabelLocked = new Color(0.72f, 0.82f, 0.92f, 0.85f);

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
                label.color = locked ? LabelLocked : LabelUnlocked;
            }

            if (labelBackground != null)
            {
                labelBackground.enabled = visible && !locked;
                if (!locked)
                {
                    labelBackground.color = new Color(0.04f, 0.06f, 0.16f, 0.55f);
                }
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

            var sprite = locked ? lockedSprite : unlocked;
            if (sprite != null)
            {
                graphic.sprite = sprite;
                graphic.type = Image.Type.Simple;
                graphic.preserveAspect = locked;
            }

            graphic.color = locked ? LockedTint : UnlockedTint;
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

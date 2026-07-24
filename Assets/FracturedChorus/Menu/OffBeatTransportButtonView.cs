using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Menu
{
    public sealed class OffBeatTransportButtonView : MonoBehaviour
    {
        private static readonly Color Idle = new Color(0.55f, 0.75f, 0.9f, 0.55f);
        private static readonly Color Active = new Color(0f, 0.9f, 1f, 1f);
        private static readonly Color GlowIdle = new Color(0f, 0.7f, 1f, 0f);
        private static readonly Color GlowActive = new Color(0f, 0.85f, 1f, 0.45f);

        [SerializeField] private Image icon;
        [SerializeField] private Image glow;
        [SerializeField] private Image plate;

        public Image Icon => icon;

        public void Configure(Image iconImage, Image glowImage, Image plateImage)
        {
            icon = iconImage;
            glow = glowImage;
            plate = plateImage;
        }

        public void SetSprite(Sprite sprite)
        {
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
                icon.preserveAspect = true;
                if (sprite != null)
                {
                    icon.color = Color.white;
                }
            }
        }

        public void SetActiveVisual(bool active)
        {
            if (icon != null)
            {
                // Keep sprite readable: white base, cyan multiply when active via plate/glow.
                icon.color = active ? Active : new Color(0.75f, 0.9f, 1f, 0.95f);
            }

            if (glow != null)
            {
                glow.enabled = true;
                glow.color = active ? GlowActive : GlowIdle;
            }

            if (plate != null)
            {
                plate.color = active
                    ? new Color(0f, 0.55f, 0.75f, 0.55f)
                    : new Color(0.06f, 0.1f, 0.16f, 0.55f);
            }
        }

        public void Pulse()
        {
            SetActiveVisual(true);
        }
    }
}

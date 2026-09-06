using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub.CharacterBuild
{
    public sealed class CharacterBuildPortraitChipView : MonoBehaviour
    {
        [SerializeField] private Image frame;
        [SerializeField] private Image face;
        [SerializeField] private Button button;
        [SerializeField] private Sprite frameNormal;
        [SerializeField] private Sprite frameSelected;
        [SerializeField] private Sprite frameLocked;

        public Button Button => button;

        public void BindFace(Sprite sprite)
        {
            if (face == null)
            {
                return;
            }

            face.enabled = sprite != null;
            face.sprite = sprite;
            face.preserveAspect = true;
            face.color = Color.white;
        }

        public void BindSlot(Sprite faceSprite, bool locked)
        {
            BindFace(locked ? null : faceSprite);
            if (button != null)
            {
                button.interactable = !locked;
            }

            if (frame == null)
            {
                return;
            }

            var sprite = locked ? frameLocked : frameNormal;
            if (sprite != null)
            {
                frame.sprite = sprite;
                frame.color = Color.white;
            }
        }

        public void SetSelected(bool selected)
        {
            if (frame == null)
            {
                return;
            }

            var sprite = selected ? frameSelected : frameNormal;
            if (sprite != null)
            {
                frame.sprite = sprite;
                frame.color = Color.white;
            }
        }
    }
}

using FracturedChorus.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class BondRosterChipView : MonoBehaviour
    {
        [SerializeField] private Image frame;
        [SerializeField] private Image face;
        [SerializeField] private Image lockIcon;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text roleLabel;
        [SerializeField] private Button button;

        public string NpcId { get; private set; }
        public Button Button => button;

        public void Bind(string npcId, bool selected, Sprite faceSprite, Sprite frameSprite, Sprite lockSprite)
        {
            NpcId = npcId;
            var unlocked = BondPresentation.IsPortraitUnlocked(npcId);

            if (nameLabel != null)
            {
                nameLabel.text = unlocked ? BondPresentation.GetDisplayName(npcId) : string.Empty;
            }

            if (roleLabel != null)
            {
                roleLabel.text = BondPresentation.GetRoleLabel(npcId);
            }

            if (face != null)
            {
                face.sprite = faceSprite;
                face.enabled = faceSprite != null;
                face.color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                face.preserveAspect = true;
            }

            if (frame != null)
            {
                frame.sprite = frameSprite;
                frame.enabled = frameSprite != null;
            }

            if (lockIcon != null)
            {
                lockIcon.sprite = lockSprite;
                lockIcon.enabled = !unlocked && lockSprite != null;
            }

            if (button != null)
            {
                button.interactable = unlocked;
            }
        }
    }
}

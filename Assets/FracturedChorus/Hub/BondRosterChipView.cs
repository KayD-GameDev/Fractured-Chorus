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

        private void Awake()
        {
            Resolve();
        }

        public void Bind(
            string npcId,
            bool selected,
            Sprite faceSprite,
            Sprite frameSprite,
            Sprite lockSprite,
            BondProgress bond)
        {
            Resolve();
            NpcId = npcId;
            var unlocked = BondPresentation.IsPortraitUnlocked(npcId);
            var showLock = BondPresentation.ShouldShowRosterLock(npcId, bond) && lockSprite != null;

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
                frame.raycastTarget = true;
            }

            if (lockIcon != null)
            {
                lockIcon.sprite = lockSprite;
                lockIcon.enabled = showLock;
                lockIcon.color = showLock ? Color.white : new Color(1f, 1f, 1f, 0f);
            }

            if (button != null)
            {
                button.interactable = true;
            }
        }

        private void Resolve()
        {
            if (frame == null)
            {
                frame = FindImage("Frame");
            }

            if (face == null)
            {
                face = FindImage("Face");
            }

            if (lockIcon == null)
            {
                lockIcon = FindImage("Lock");
            }

            if (nameLabel == null)
            {
                nameLabel = FindText("Name");
            }

            if (roleLabel == null)
            {
                roleLabel = FindText("Role");
            }

            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null && button.targetGraphic == null && frame != null)
            {
                button.targetGraphic = frame;
            }
        }

        private Image FindImage(string childName)
        {
            var child = transform.Find(childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private Text FindText(string childName)
        {
            var child = transform.Find(childName);
            return child != null ? child.GetComponent<Text>() : null;
        }
    }
}

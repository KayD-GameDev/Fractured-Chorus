using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub.CharacterBuild
{
    /// <summary>One skill list row — wire children in the CharacterBuild scene hierarchy.</summary>
    public sealed class CharacterBuildSkillRowView : MonoBehaviour
    {
        private static readonly Color Gold = new Color(1f, 0.84f, 0.2f, 1f);

        [SerializeField] private Image icon;
        [SerializeField] private Text nameLabel;
        [SerializeField] private GameObject goldFrame;
        [SerializeField] private Outline goldOutline;
        [SerializeField] private Button button;
        [SerializeField] private Image rowBackground;

        public Button Button => button;
        public bool IsCombatSlot { get; private set; }

        private void Awake()
        {
            EnsureGoldOutline();
            // Legacy solid GoldFrame plate (pre-Outline) must stay hidden.
            if (goldFrame != null)
            {
                var plate = goldFrame.GetComponent<Image>();
                if (plate != null)
                {
                    plate.enabled = false;
                }
            }
        }

        public void BindEmpty(bool combatSlot)
        {
            IsCombatSlot = combatSlot;
            if (nameLabel != null)
            {
                nameLabel.text = "—";
            }

            if (icon != null)
            {
                icon.enabled = false;
            }

            SetGoldFrame(combatSlot);
        }

        public void Bind(string displayName, Sprite skillIcon, bool combatSlot)
        {
            IsCombatSlot = combatSlot;
            if (nameLabel != null)
            {
                nameLabel.text = string.IsNullOrEmpty(displayName) ? "—" : displayName;
            }

            if (icon != null)
            {
                icon.enabled = skillIcon != null;
                icon.sprite = skillIcon;
            }

            SetGoldFrame(combatSlot);
        }

        public void SetGoldFrame(bool visible)
        {
            EnsureGoldOutline();
            if (goldOutline != null)
            {
                goldOutline.enabled = visible;
            }
        }

        public void SetSelected(bool selected)
        {
            if (rowBackground == null)
            {
                return;
            }

            rowBackground.color = selected
                ? new Color(0.1f, 0.14f, 0.34f, 0.96f)
                : new Color(0.06f, 0.08f, 0.24f, 0.92f);
        }

        private void EnsureGoldOutline()
        {
            if (goldOutline != null)
            {
                return;
            }

            var target = rowBackground != null ? rowBackground.gameObject : gameObject;
            goldOutline = target.GetComponent<Outline>();
            if (goldOutline == null)
            {
                goldOutline = target.AddComponent<Outline>();
            }

            goldOutline.effectColor = Gold;
            goldOutline.effectDistance = new Vector2(3f, -3f);
            goldOutline.useGraphicAlpha = true;
        }
    }
}

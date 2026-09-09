using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub.CharacterBuild
{
    /// <summary>One skill list row — wire children in the CharacterBuild scene hierarchy.</summary>
    public sealed class CharacterBuildSkillRowView : MonoBehaviour
    {
        private static readonly Color Gold = new Color(1f, 0.84f, 0.2f, 1f);

        [SerializeField] private Image noteSlot;
        [SerializeField] private Image iconFrame;
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
                nameLabel.color = CharacterBuildStatTheme.LabelColor;
            }

            ApplySkillIcon(null);
            ApplySlotChrome(combatSlot, false);
            SetGoldFrame(combatSlot);
        }

        public void Bind(string displayName, Sprite skillIcon, bool combatSlot)
        {
            IsCombatSlot = combatSlot;
            if (nameLabel != null)
            {
                nameLabel.text = string.IsNullOrEmpty(displayName) ? "—" : displayName;
                nameLabel.color = CharacterBuildStatTheme.LabelColor;
            }

            ApplySkillIcon(skillIcon);
            ApplySlotChrome(combatSlot, skillIcon != null);
            SetGoldFrame(combatSlot);
        }

        private void ApplySkillIcon(Sprite skillIcon)
        {
            var art = EnsureCircularArt();
            if (art == null)
            {
                return;
            }

            art.enabled = skillIcon != null;
            art.sprite = skillIcon;
            art.preserveAspect = true;
            art.color = Color.white;
            if (icon != null)
            {
                icon.enabled = skillIcon != null;
            }
        }

        private Image EnsureCircularArt()
        {
            if (icon == null)
            {
                return null;
            }

            icon.sprite = UiCircleSpriteUtil.Circle;
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            icon.color = Color.white;
            icon.raycastTarget = false;

            var mask = icon.GetComponent<Mask>();
            if (mask == null)
            {
                mask = icon.gameObject.AddComponent<Mask>();
            }

            mask.showMaskGraphic = false;

            var artTf = icon.transform.Find("Art");
            Image art;
            if (artTf == null)
            {
                var artGo = new GameObject("Art", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                artGo.transform.SetParent(icon.transform, false);
                var rect = artGo.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                art = artGo.GetComponent<Image>();
            }
            else
            {
                art = artTf.GetComponent<Image>();
            }

            if (art != null)
            {
                art.raycastTarget = false;
            }

            return art;
        }

        private void ApplySlotChrome(bool combatSlot, bool hasIcon)
        {
            if (iconFrame != null)
            {
                iconFrame.enabled = combatSlot && !hasIcon;
            }

            if (noteSlot != null)
            {
                noteSlot.enabled = !hasIcon;
            }
        }

        public void SetGoldFrame(bool visible)
        {
            EnsureGoldOutline();
            if (goldOutline != null)
            {
                goldOutline.enabled = visible;
            }
        }

        public void SetFixed(bool fixedSlot, Sprite pinSprite)
        {
            if (button != null)
            {
                button.interactable = true;
                button.transition = fixedSlot
                    ? Selectable.Transition.None
                    : Selectable.Transition.ColorTint;

                var graphic = button.targetGraphic as Graphic;
                if (graphic != null)
                {
                    graphic.color = Color.white;
                }
            }

            if (nameLabel != null)
            {
                nameLabel.color = CharacterBuildStatTheme.LabelColor;
            }

            var pin = transform.Find("BasicPin");
            if (!fixedSlot)
            {
                if (pin != null)
                {
                    pin.gameObject.SetActive(false);
                }

                return;
            }

            if (pinSprite == null)
            {
                return;
            }

            if (pin == null)
            {
                var go = new GameObject("BasicPin", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(transform, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.sizeDelta = new Vector2(52f, 52f);
                rect.anchoredPosition = new Vector2(-8f, -8f);
                pin = go.transform;
            }

            pin.SetAsLastSibling();
            pin.gameObject.SetActive(true);
            var image = pin.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            image.raycastTarget = false;
            image.preserveAspect = true;
            image.color = Color.white;
            if (pinSprite != null)
            {
                image.sprite = pinSprite;
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

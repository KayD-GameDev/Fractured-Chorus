using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class BondEpisodeRowView : MonoBehaviour
    {
        private static readonly Color SelectedRowTextColor = new Color(0.08f, 0.1f, 0.22f, 1f);

        [SerializeField] private Image plate;
        [SerializeField] private Image icon;
        [SerializeField] private Text indexLabel;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Button button;
        [SerializeField] private Sprite plateNormal;
        [SerializeField] private Sprite plateSelected;
        [SerializeField] private Sprite playSprite;
        [SerializeField] private Sprite lockSprite;

        public int EpisodeIndex { get; private set; }
        public bool Unlocked { get; private set; }
        public Button Button => button;

        private void Awake()
        {
            if (plate == null)
            {
                plate = GetComponent<Image>();
            }

            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (plateNormal == null)
            {
                plateNormal = BondsPackSprites.EpisodeRowNormal;
            }

            if (plateSelected == null)
            {
                plateSelected = BondsPackSprites.EpisodeRowSelected;
            }
        }

        public void Bind(BondLinkEpisode episode, bool unlocked, bool selected)
        {
            EpisodeIndex = episode.Index;
            Unlocked = unlocked;

            if (plate != null)
            {
                var normal = plateNormal ?? BondsPackSprites.EpisodeRowNormal;
                var selectedPlate = plateSelected ?? BondsPackSprites.EpisodeRowSelected;
                if (normal != null && selectedPlate != null)
                {
                    plate.sprite = selected ? selectedPlate : normal;
                    plate.color = Color.white;
                }
            }

            var labelColor = GetLabelColor(unlocked, selected);

            if (indexLabel != null)
            {
                indexLabel.text = episode.Index.ToString("00");
                indexLabel.color = labelColor;
            }

            if (titleLabel != null)
            {
                titleLabel.text = episode.Title;
                titleLabel.color = labelColor;
            }

            if (icon != null)
            {
                icon.sprite = unlocked ? playSprite : lockSprite;
                icon.enabled = icon.sprite != null;
            }

            if (button != null)
            {
                button.interactable = true;
            }
        }

        private static Color GetLabelColor(bool unlocked, bool selected)
        {
            if (!unlocked)
            {
                return FcColorTokens.Brand.TextMuted;
            }

            return selected
                ? SelectedRowTextColor
                : FcColorTokens.Brand.TextPrimary;
        }
    }
}

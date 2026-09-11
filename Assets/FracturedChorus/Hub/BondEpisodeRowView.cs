using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class BondEpisodeRowView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text indexLabel;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Button button;
        [SerializeField] private Sprite playSprite;
        [SerializeField] private Sprite lockSprite;

        public int EpisodeIndex { get; private set; }
        public bool Unlocked { get; private set; }
        public Button Button => button;

        public void Bind(BondLinkEpisode episode, bool unlocked, bool selected)
        {
            EpisodeIndex = episode.Index;
            Unlocked = unlocked;

            if (indexLabel != null)
            {
                indexLabel.text = episode.Index.ToString("00");
            }

            if (titleLabel != null)
            {
                titleLabel.text = episode.Title;
                titleLabel.color = unlocked
                    ? FracturedChorus.UI.FcColorTokens.Brand.TextPrimary
                    : FracturedChorus.UI.FcColorTokens.Brand.TextMuted;
            }

            if (icon != null)
            {
                icon.sprite = unlocked ? playSprite : lockSprite;
                icon.enabled = icon.sprite != null;
            }

            if (button != null)
            {
                button.interactable = unlocked;
            }
        }
    }
}

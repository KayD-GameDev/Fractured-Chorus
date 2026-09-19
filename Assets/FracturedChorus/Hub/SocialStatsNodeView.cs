using FracturedChorus.Meta;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class SocialStatsNodeView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text rankLabel;
        [SerializeField] private Text flavorLabel;

        public void Bind(SocialStatType stat, int rank, Sprite icon)
        {
            if (nameLabel != null)
            {
                nameLabel.text = SocialStatPresentation.GetDisplayName(stat);
                nameLabel.color = FcColorTokens.Brand.TextPrimary;
            }

            if (rankLabel != null)
            {
                rankLabel.text = $"Rank {Mathf.Clamp(rank, 1, SocialStatsState.MaxRank)}";
                rankLabel.color = FcColorTokens.Brand.TextPrimary;
            }

            if (flavorLabel != null)
            {
                flavorLabel.text = SocialStatPresentation.GetFlavor(stat);
                flavorLabel.color = FcColorTokens.WithAlpha(FcColorTokens.Brand.TextPrimary, 0.82f);
            }

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
                iconImage.preserveAspect = true;
            }
        }

        public void AssignRefs(Image icon, Text name, Text rank, Text flavor)
        {
            iconImage = icon;
            nameLabel = name;
            rankLabel = rank;
            flavorLabel = flavor;
        }
    }
}

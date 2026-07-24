using FracturedChorus.Meta;
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
            }

            if (rankLabel != null)
            {
                rankLabel.text = $"Rank {Mathf.Clamp(rank, 1, SocialStatsState.MaxRank)}";
            }

            if (flavorLabel != null)
            {
                flavorLabel.text = SocialStatPresentation.GetFlavor(stat);
            }

            if (iconImage != null)
            {
                iconImage.enabled = icon != null;
                if (icon != null)
                {
                    iconImage.sprite = icon;
                    iconImage.preserveAspect = true;
                }
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

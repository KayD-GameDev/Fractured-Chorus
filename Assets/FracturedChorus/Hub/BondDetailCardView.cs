using FracturedChorus.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class BondDetailCardView : MonoBehaviour
    {
        [SerializeField] private Image portrait;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text rankLabel;
        [SerializeField] private Text bioLabel;
        [SerializeField] private Text quoteLabel;
        [SerializeField] private Image expFill;
        [SerializeField] private Text expLabel;
        [SerializeField] private Text nextRankLabel;
        [SerializeField] private Text nextHintLabel;

        public void Bind(GameMetaState state, string npcId, Sprite portraitSprite)
        {
            var unlocked = BondPresentation.IsPortraitUnlocked(npcId);
            var bond = state != null ? state.GetBond(npcId) : null;

            if (portrait != null)
            {
                portrait.sprite = portraitSprite;
                portrait.enabled = portraitSprite != null;
                portrait.preserveAspect = true;
                portrait.color = Color.white;
            }

            if (nameLabel != null)
            {
                nameLabel.text = BondPresentation.GetDisplayName(npcId);
            }

            if (rankLabel != null)
            {
                rankLabel.text = bond != null && unlocked ? $"Rank {bond.Rank}" : "Locked";
            }

            if (bioLabel != null)
            {
                bioLabel.text = BondPresentation.GetBio(npcId);
            }

            if (quoteLabel != null)
            {
                var quote = BondPresentation.GetQuote(npcId);
                quoteLabel.text = string.IsNullOrEmpty(quote) ? string.Empty : $"\"{quote}\"";
            }

            var threshold = bond != null ? bond.GetThresholdForRank(bond.Rank) : 0;
            var exp = bond != null ? bond.Exp : 0;

            if (expLabel != null)
            {
                expLabel.text = unlocked ? $"{exp} / {threshold}" : "-";
            }

            if (expFill != null)
            {
                var amount = unlocked && threshold > 0 ? Mathf.Clamp01(exp / (float)threshold) : 0f;
                expFill.fillAmount = amount;
            }

            if (nextRankLabel != null)
            {
                nextRankLabel.text = BondPresentation.NextRankLabel;
            }

            if (nextHintLabel != null)
            {
                nextHintLabel.text = BondPresentation.NextRankHint;
            }
        }
    }
}

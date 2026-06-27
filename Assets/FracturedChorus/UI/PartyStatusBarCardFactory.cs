using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public static class PartyStatusBarCardFactory
    {
        private const string TemplateName = "PartyCardTemplate";

        public static IReadOnlyList<PartyUnitCardView> EnsureCardCount(
            RectTransform cardsRow,
            PartyUnitCardView template,
            int requiredCount)
        {
            if (cardsRow == null || requiredCount < 0)
            {
                return System.Array.Empty<PartyUnitCardView>();
            }

            template ??= ResolveTemplate(cardsRow);
            if (template == null)
            {
                Debug.LogError("[PartyStatusBar] Thiếu PartyCardTemplate dưới CardsRow.");
                return System.Array.Empty<PartyUnitCardView>();
            }

            template.gameObject.SetActive(false);
            template.name = TemplateName;

            var cards = CollectCards(cardsRow, template);

            while (cards.Count < requiredCount)
            {
                var cloneGo = Object.Instantiate(template.gameObject, cardsRow);
                cloneGo.name = $"PartyCard_{cards.Count}";
                cloneGo.SetActive(true);
                var card = cloneGo.GetComponent<PartyUnitCardView>();
                card.WireReferences();
                cards.Add(card);
            }

            for (var i = 0; i < cards.Count; i++)
            {
                var active = i < requiredCount;
                cards[i].gameObject.SetActive(active);
                if (!active)
                {
                    cards[i].Unbind();
                }
            }

            if (requiredCount > 0)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(cardsRow);
            }

            return cards.GetRange(0, requiredCount);
        }

        public static PartyUnitCardView ResolveTemplate(RectTransform cardsRow)
        {
            if (cardsRow == null)
            {
                return null;
            }

            var named = cardsRow.Find(TemplateName);
            if (named != null)
            {
                return named.GetComponent<PartyUnitCardView>();
            }

            var existing = cardsRow.GetComponentInChildren<PartyUnitCardView>(true);
            if (existing != null)
            {
                return existing;
            }

            return null;
        }

        private static List<PartyUnitCardView> CollectCards(RectTransform cardsRow, PartyUnitCardView template)
        {
            var cards = new List<PartyUnitCardView>();
            foreach (Transform child in cardsRow)
            {
                if (child == template.transform)
                {
                    continue;
                }

                var card = child.GetComponent<PartyUnitCardView>();
                if (card != null)
                {
                    cards.Add(card);
                }
            }

            return cards;
        }
    }
}

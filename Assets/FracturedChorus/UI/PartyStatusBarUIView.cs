using System.Collections.Generic;
using System.Linq;
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Hàng thẻ party góc trái trên — giữ CardTemplate trong Hierarchy; clone tối đa 5 thẻ lúc Play.
    /// </summary>
    public class PartyStatusBarUIView : MonoBehaviour
    {
        public const int MaxPartyCards = 5;
        public const float DefaultCardSpacing = 1f;

        [SerializeField] private RectTransform cardsRow;
        [SerializeField] private PartyMemberCardView cardTemplate;
        [SerializeField] private float cardSpacing = DefaultCardSpacing;

        private readonly List<PartyMemberCardView> _spawnedCards = new();

        private void Awake()
        {
            WireReferences();
            HideTemplate();
        }

        public void WireReferences()
        {
            if (cardsRow == null)
            {
                cardsRow = transform.Find("CardsRow") as RectTransform;
            }

            if (cardTemplate == null)
            {
                var templateTransform = transform.Find("CardTemplate");
                if (templateTransform != null)
                {
                    cardTemplate = templateTransform.GetComponent<PartyMemberCardView>();
                }
            }

            ApplyCardSpacing();
            cardTemplate?.WireReferences();
            HideTemplate();
        }

        public void BindFromUnitViews(IReadOnlyList<UnitView> unitViews)
        {
            ClearSpawnedCards();

            if (cardTemplate == null || cardsRow == null || unitViews == null)
            {
                return;
            }

            var playerViews = unitViews
                .Where(v => v != null && v.Side == GridSide.Player && v.Unit != null)
                .ToList();

            PartyCardDisplayOrder.SortUnitViews(playerViews);

            foreach (var view in playerViews.Take(MaxPartyCards))
            {
                SpawnCard(view.Unit, view.ResolvePreset());
            }

            RebuildCardsRowLayout();
        }

        public void BindFromSession(CombatSession session)
        {
            ClearSpawnedCards();

            if (session?.Grid == null || cardTemplate == null || cardsRow == null)
            {
                return;
            }

            var units = session.Grid.PlayerUnits
                .Where(u => u != null)
                .OrderBy(u => u, Comparer<CombatUnit>.Create(PartyCardDisplayOrder.CompareUnits))
                .Take(MaxPartyCards)
                .ToList();

            foreach (var unit in units)
            {
                SpawnCard(unit, ResolvePresetForUnit(unit));
            }

            RebuildCardsRowLayout();
        }

        private void ApplyCardSpacing()
        {
            if (cardsRow == null)
            {
                return;
            }

            var layout = cardsRow.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.enabled = false;
            }
        }

        private void RebuildCardsRowLayout()
        {
            if (cardsRow == null)
            {
                return;
            }

            ApplyCardSpacing();

            var x = 0f;
            for (var i = 0; i < cardsRow.childCount; i++)
            {
                var child = cardsRow.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeSelf)
                {
                    continue;
                }

                PrepareCardRectForRowLayout(child);
                var width = child.sizeDelta.x;
                child.anchoredPosition = new Vector2(x, 0f);
                x += width + cardSpacing;
            }
        }

        private void SpawnCard(CombatUnit unit, UnitPresetSO preset)
        {
            var card = Instantiate(cardTemplate, cardsRow);
            card.gameObject.SetActive(true);
            card.name = $"Card_{unit.DisplayName}";
            PrepareCardRectForRowLayout(card.transform as RectTransform);
            card.WireReferences();
            card.Bind(unit, preset);
            _spawnedCards.Add(card);
        }

        private static void PrepareCardRectForRowLayout(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            var size = rect.sizeDelta;
            if (size.x <= 0f || size.y <= 0f)
            {
                size = new Vector2(72f, 96f);
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;

            var layoutElement = rect.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = rect.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.ignoreLayout = false;
            layoutElement.preferredWidth = size.x;
            layoutElement.preferredHeight = size.y;
        }

        private static UnitPresetSO ResolvePresetForUnit(CombatUnit unit)
        {
            if (unit == null)
            {
                return null;
            }

            var views = FindObjectsByType<UnitView>(FindObjectsInactive.Include);
            foreach (var view in views)
            {
                if (view != null && view.Unit == unit)
                {
                    return view.ResolvePreset();
                }
            }

            return EncounterRuntimeFactory.GetPresetByKey(unit.UnitId);
        }

        private void HideTemplate()
        {
            if (cardTemplate != null)
            {
                cardTemplate.gameObject.SetActive(false);
            }
        }

        private void ClearSpawnedCards()
        {
            foreach (var card in _spawnedCards)
            {
                if (card == null)
                {
                    continue;
                }

                card.Unbind();
                if (Application.isPlaying)
                {
                    Destroy(card.gameObject);
                }
                else
                {
                    DestroyImmediate(card.gameObject);
                }
            }

            _spawnedCards.Clear();
        }
    }
}

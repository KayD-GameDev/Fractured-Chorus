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
    /// Hàng thẻ party góc trái trên — clone từ CardTemplate theo unit player đang trên sân.
    /// </summary>
    public class PartyStatusBarUIView : MonoBehaviour
    {
        public const int MaxPartyCards = 4;
        public const float DefaultCardSpacing = PartyCardLayout.CardGap;

        [SerializeField] private RectTransform cardsRow;
        [SerializeField] private PartyMemberCardView cardTemplate;
        [SerializeField] private float cardSpacing = DefaultCardSpacing;

        private readonly List<PartyMemberCardView> _spawnedCards = new();

        public int BoundUnitCount => _spawnedCards.Count;

        /// <summary>Template thẻ — dùng lại cho thanh thẻ quái.</summary>
        public PartyMemberCardView CardTemplate
        {
            get
            {
                if (cardTemplate == null)
                {
                    WireReferences();
                }

                return cardTemplate;
            }
        }

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

            if (Mathf.Approximately(cardSpacing, 1f) || Mathf.Approximately(cardSpacing, 1.25f) ||
                Mathf.Approximately(cardSpacing, 0.75f) || Mathf.Approximately(cardSpacing, 1.5f) ||
                Mathf.Approximately(cardSpacing, 85f) || Mathf.Approximately(cardSpacing, 95f) ||
                Mathf.Approximately(cardSpacing, 100f) || Mathf.Approximately(cardSpacing, 115.75f) ||
                Mathf.Approximately(cardSpacing, 116.5f) || Mathf.Approximately(cardSpacing, 117f))
            {
                cardSpacing = PartyCardLayout.CardGap;
            }

            ApplyCardSpacing();
            cardTemplate?.WireReferences();
            HideTemplate();
        }

        /// <summary>Đồng bộ thẻ từ UnitView trên sân (lọc player + đã đặt ô lưới).</summary>
        public void BindFromUnitViews(IReadOnlyList<UnitView> unitViews)
        {
            if (cardTemplate == null || cardsRow == null || unitViews == null)
            {
                return;
            }

            var playerViews = CollectFieldPlayerViews(unitViews);
            PartyCardDisplayOrder.SortUnitViews(playerViews);
            SyncCards(playerViews.Select(v => (v.Unit, v.ResolvePreset())).Take(MaxPartyCards).ToList());
        }

        /// <summary>Đồng bộ thẻ từ DualGrid.PlayerUnits — nguồn authoritative khi có session.</summary>
        public void BindFromSession(CombatSession session)
        {
            if (session?.Grid == null || cardTemplate == null || cardsRow == null)
            {
                return;
            }

            ApplyFormationEntries(BuildSessionEntries(session));
        }

        private static List<UnitView> CollectFieldPlayerViews(IReadOnlyList<UnitView> unitViews)
        {
            return unitViews
                .Where(v => v != null &&
                            v.Side == GridSide.Player &&
                            v.Unit != null &&
                            v.IsPlacedOnGrid)
                .ToList();
        }

        private void SyncCards(IReadOnlyList<(CombatUnit unit, UnitPresetSO preset)> entries)
        {
            if (!NeedsResync(entries))
            {
                RebuildCardsRowLayout();
                return;
            }

            ClearSpawnedCards();

            foreach (var (unit, preset) in entries)
            {
                if (unit == null)
                {
                    continue;
                }

                SpawnCard(unit, preset);
            }

            RebuildCardsRowLayout();
        }

        private bool NeedsResync(IReadOnlyList<(CombatUnit unit, UnitPresetSO preset)> entries)
        {
            if (_spawnedCards.Count != entries.Count)
            {
                return true;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                if (_spawnedCards[i]?.BoundUnit != entries[i].unit)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Cập nhật lại thứ tự thẻ khi formation đổi nhưng cùng tập unit.</summary>
        public void RefreshFormationOrderFromUnitViews(IReadOnlyList<UnitView> unitViews)
        {
            if (cardTemplate == null || cardsRow == null || unitViews == null)
            {
                return;
            }

            var playerViews = CollectFieldPlayerViews(unitViews);
            PartyCardDisplayOrder.SortUnitViews(playerViews);
            var entries = playerViews
                .Take(MaxPartyCards)
                .Select(v => (v.Unit, v.ResolvePreset()))
                .ToList();

            ApplyFormationEntries(entries);
        }

        /// <summary>Cập nhật thứ tự thẻ từ DualGrid (sau kéo formation).</summary>
        public void RefreshFormationOrderFromSession(CombatSession session)
        {
            if (session?.Grid == null || cardTemplate == null || cardsRow == null)
            {
                return;
            }

            ApplyFormationEntries(BuildSessionEntries(session));
        }

        private List<(CombatUnit unit, UnitPresetSO preset)> BuildSessionEntries(CombatSession session)
        {
            return session.Grid.PlayerUnits
                .Where(u => u != null)
                .OrderBy(u => u, Comparer<CombatUnit>.Create(PartyCardDisplayOrder.CompareUnits))
                .Take(MaxPartyCards)
                .Select(u => (u, ResolvePresetForUnit(u)))
                .ToList();
        }

        private void ApplyFormationEntries(IReadOnlyList<(CombatUnit unit, UnitPresetSO preset)> entries)
        {
            if (NeedsResync(entries))
            {
                SyncCards(entries);
                return;
            }

            ReorderSpawnedCards(entries);
            RebuildCardsRowLayout();
        }

        private void ReorderSpawnedCards(IReadOnlyList<(CombatUnit unit, UnitPresetSO preset)> entries)
        {
            var lookup = new Dictionary<CombatUnit, PartyMemberCardView>();
            foreach (var card in _spawnedCards)
            {
                if (card?.BoundUnit != null && !lookup.ContainsKey(card.BoundUnit))
                {
                    lookup.Add(card.BoundUnit, card);
                }
            }

            _spawnedCards.Clear();
            foreach (var (unit, _) in entries)
            {
                if (unit != null && lookup.TryGetValue(unit, out var card))
                {
                    _spawnedCards.Add(card);
                }
            }
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
                layout.spacing = cardSpacing;
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

            var activeCards = new List<PartyMemberCardView>();
            foreach (var card in _spawnedCards)
            {
                if (card != null && card.gameObject.activeSelf)
                {
                    activeCards.Add(card);
                }
            }

            var totalCards = activeCards.Count;
            for (var cardIndex = 0; cardIndex < totalCards; cardIndex++)
            {
                var card = activeCards[cardIndex];
                var rect = card.transform as RectTransform;
                if (rect == null)
                {
                    continue;
                }

                PrepareCardRectForRowLayout(rect, GetTemplateCardSize());
                rect.anchoredPosition = PartyCardLayout.GetCardAnchoredPosition(cardIndex, totalCards);
                rect.SetSiblingIndex(cardIndex);
            }
        }

        private Vector2 GetTemplateCardSize()
        {
            if (cardTemplate != null)
            {
                var templateRect = cardTemplate.transform as RectTransform;
                if (templateRect != null && templateRect.sizeDelta.x > 0f && templateRect.sizeDelta.y > 0f)
                {
                    return templateRect.sizeDelta;
                }
            }

            return new Vector2(PartyCardLayout.CardWidth, 167f);
        }

        private void SpawnCard(CombatUnit unit, UnitPresetSO preset)
        {
            var card = Instantiate(cardTemplate, cardsRow);
            card.gameObject.SetActive(true);
            card.name = $"Card_{unit.DisplayName}";
            PrepareCardRectForRowLayout(card.transform as RectTransform, GetTemplateCardSize());
            card.WireReferences();
            card.Bind(unit, preset);
            _spawnedCards.Add(card);
        }

        private static void PrepareCardRectForRowLayout(RectTransform rect, Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            if (size.x <= 0f || size.y <= 0f)
            {
                size = new Vector2(PartyCardLayout.CardWidth, 167f);
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;

            var layoutElement = rect.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = rect.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.ignoreLayout = true;
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
                DestroyCard(card);
            }

            _spawnedCards.Clear();
            RemoveStaleCardsRowChildren();
        }

        private void RemoveStaleCardsRowChildren()
        {
            if (cardsRow == null)
            {
                return;
            }

            for (var i = cardsRow.childCount - 1; i >= 0; i--)
            {
                var child = cardsRow.GetChild(i);
                if (cardTemplate != null && child == cardTemplate.transform)
                {
                    continue;
                }

                var card = child.GetComponent<PartyMemberCardView>();
                if (card == null)
                {
                    continue;
                }

                DestroyCard(card);
            }
        }

        private void DestroyCard(PartyMemberCardView card)
        {
            if (card == null)
            {
                return;
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
    }
}

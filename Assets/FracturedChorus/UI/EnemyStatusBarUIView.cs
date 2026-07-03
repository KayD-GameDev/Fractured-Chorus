using System.Collections.Generic;

using FracturedChorus.Combat.Bootstrap;

using FracturedChorus.Combat.Core;

using FracturedChorus.Combat.Units;

using FracturedChorus.Data;

using UnityEngine;

using UnityEngine.UI;



namespace FracturedChorus.UI

{

    /// <summary>

    /// Thanh thẻ quái góc phải trên — clone từ CardTemplate trong scene (Hierarchy-first).

    /// Kích thước / scale / spacing lấy từ CardTemplate + cardSpacing trên Inspector.

    /// </summary>

    public class EnemyStatusBarUIView : MonoBehaviour

    {

        public const int MaxEnemyCards = 6;

        public const float DefaultCardSpacing = PartyCardLayout.CardGap;



        [SerializeField] private RectTransform cardsRow;

        [SerializeField] private PartyMemberCardView cardTemplate;

        [SerializeField] private float cardSpacing = DefaultCardSpacing;



        private readonly List<PartyMemberCardView> _spawnedCards = new();



        public int BoundUnitCount => _spawnedCards.Count;



        public float CardSpacing => cardSpacing;



        /// <summary>Template thẻ quái — phải có trong Hierarchy (inactive lúc Edit).</summary>

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



            ApplyCardSpacing();

            cardTemplate?.WireReferences();

            HideTemplate();

        }



        /// <summary>Fallback khi scene chưa có CardTemplate riêng — bootstrap có thể mượn party template.</summary>

        public void SetCardTemplate(PartyMemberCardView template)

        {

            if (template == null)

            {

                return;

            }



            cardTemplate = template;

            cardTemplate.WireReferences();

        }



        public void BindFromSession(CombatSession session)

        {

            if (session?.Grid == null || cardTemplate == null || cardsRow == null)

            {

                return;

            }



            var units = new List<CombatUnit>();

            foreach (var unit in session.Grid.EnemyUnits)

            {

                if (unit != null)

                {

                    units.Add(unit);

                }



                if (units.Count >= MaxEnemyCards)

                {

                    break;

                }

            }



            SyncCards(units);

        }



        private void SyncCards(IReadOnlyList<CombatUnit> units)

        {

            if (NeedsResync(units))

            {

                ClearSpawnedCards();



                foreach (var unit in units)

                {

                    if (unit == null)

                    {

                        continue;

                    }



                    SpawnCard(unit);

                }

            }



            RebuildCardsRowLayout();

        }



        private bool NeedsResync(IReadOnlyList<CombatUnit> units)

        {

            if (_spawnedCards.Count != units.Count)

            {

                return true;

            }



            for (var i = 0; i < units.Count; i++)

            {

                if (_spawnedCards[i]?.BoundUnit != units[i])

                {

                    return true;

                }

            }



            return false;

        }



        private void SpawnCard(CombatUnit unit)

        {

            var card = Instantiate(cardTemplate, cardsRow);

            card.gameObject.SetActive(true);

            card.name = $"EnemyCard_{unit.DisplayName}";

            card.WireReferences();

            card.Bind(unit, ResolvePresetForUnit(unit));

            _spawnedCards.Add(card);

        }



        private void RebuildCardsRowLayout()

        {

            if (cardsRow == null)

            {

                return;

            }



            ApplyCardSpacing();



            var cardSize = GetTemplateCardSize();

            var cardScale = GetTemplateCardScale();

            var step = PartyCardLayout.ComputeCardStepX(cardSize.x * cardScale.x, cardSpacing);

            var count = _spawnedCards.Count;



            for (var i = 0; i < count; i++)

            {

                var card = _spawnedCards[i];

                if (card == null || !card.gameObject.activeSelf)

                {

                    continue;

                }



                var rect = card.transform as RectTransform;

                if (rect == null)

                {

                    continue;

                }



                PrepareCardRectForRowLayout(rect, cardSize, cardScale);

                rect.anchoredPosition = new Vector2(-i * step, 0f);

                rect.SetSiblingIndex(i);

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



        private Vector2 GetTemplateCardSize()

        {

            if (cardTemplate != null)

            {

                var templateRect = cardTemplate.transform as RectTransform;

                return RectSizeUtil.ResolveSize(templateRect, new Vector2(PartyCardLayout.CardWidth, PartyCardLayout.CardHeight));

            }



            return new Vector2(PartyCardLayout.CardWidth, PartyCardLayout.CardHeight);

        }



        private Vector3 GetTemplateCardScale()

        {

            if (cardTemplate != null)

            {

                return cardTemplate.transform.localScale;

            }



            return Vector3.one;

        }



        private static void PrepareCardRectForRowLayout(RectTransform rect, Vector2 size, Vector3 scale)

        {

            if (rect == null)

            {

                return;

            }



            if (size.x <= 0f || size.y <= 0f)

            {

                size = new Vector2(PartyCardLayout.CardWidth, PartyCardLayout.CardHeight);

            }



            rect.anchorMin = new Vector2(1f, 1f);

            rect.anchorMax = new Vector2(1f, 1f);

            rect.pivot = new Vector2(1f, 1f);

            rect.sizeDelta = size;

            rect.localScale = scale;



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

            if (cardTemplate != null && cardTemplate.transform.parent == transform)

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



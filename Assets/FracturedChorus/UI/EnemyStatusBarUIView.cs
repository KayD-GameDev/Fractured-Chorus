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
            if (cardTemplate == null)
            {
                var templateTransform = transform.Find("CardTemplate");
                if (templateTransform != null)
                {
                    cardTemplate = templateTransform.GetComponent<PartyMemberCardView>();
                }
            }

            if (cardTemplate != null && cardTemplate.transform.parent == transform)
            {
                cardTemplate.gameObject.SetActive(false);
            }

            WireReferences();
            ConsumeSceneCardTemplate();
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
            // Khóa Hierarchy theo Enemy CardTemplate trước mọi Wire/Bind.
            card.UseEnemyTemplateHierarchy(cardTemplate);
            PrepareCardRectForRowLayout(
                card.transform as RectTransform,
                GetTemplateCardSize(),
                GetTemplateCardScale());
            card.WireReferences();
            card.NormalizeTemplateChrome();
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

            var activeCards = new List<PartyMemberCardView>();
            foreach (var card in _spawnedCards)
            {
                if (card != null && card.gameObject.activeSelf)
                {
                    activeCards.Add(card);
                }
            }

            var cardScale = GetTemplateCardScale();
            var totalCards = activeCards.Count;
            var widths = new float[totalCards];
            for (var cardIndex = 0; cardIndex < totalCards; cardIndex++)
            {
                var card = activeCards[cardIndex];
                var rect = card.transform as RectTransform;
                if (rect == null)
                {
                    continue;
                }

                var cardSize = card.PreferredCardSize;
                widths[cardIndex] = cardSize.x * cardScale.x;
                PrepareCardRectForRowLayout(rect, cardSize, cardScale);
                rect.SetSiblingIndex(cardIndex);
            }

            // Index 0 = rightmost (closest to bar's right edge). Walk further left for later cards.
            var x = 0f;
            for (var cardIndex = 0; cardIndex < totalCards; cardIndex++)
            {
                var rect = activeCards[cardIndex].transform as RectTransform;
                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(-x, 0f);
                }

                x += widths[cardIndex] + cardSpacing;
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
            return RectSizeUtil.ResolveScale(cardTemplate != null ? cardTemplate.transform : null);
        }



        private static void PrepareCardRectForRowLayout(RectTransform rect, Vector2 size, Vector3 scale)
        {
            if (rect == null)
            {
                return;
            }

            // Neo root góc trên-phải — thẻ mọc vào trong bar (trái + xuống). Không đụng Rect con.
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);

            if (size.x > 0f && size.y > 0f)
            {
                rect.sizeDelta = size;
            }

            rect.localScale = scale;

            var layoutElement = rect.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = rect.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.ignoreLayout = true;
            if (size.x > 0f && size.y > 0f)
            {
                layoutElement.preferredWidth = size.x;
                layoutElement.preferredHeight = size.y;
            }
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



        private void ConsumeSceneCardTemplate()
        {
            if (cardTemplate == null)
            {
                return;
            }

            if (cardTemplate.name == "CardTemplate_Runtime")
            {
                cardTemplate.gameObject.SetActive(false);
                return;
            }

            // Borrowed party template lives under another bar — only hide, never destroy.
            if (cardTemplate.transform.parent != transform)
            {
                cardTemplate.gameObject.SetActive(false);
                return;
            }

            var sceneTemplate = cardTemplate;
            sceneTemplate.gameObject.SetActive(false);

            var factoryGo = Instantiate(sceneTemplate.gameObject, transform);
            factoryGo.name = "CardTemplate_Runtime";
            factoryGo.SetActive(false);

            cardTemplate = factoryGo.GetComponent<PartyMemberCardView>();
            // Factory cũng khóa enemy hierarchy — WireReferences không chạy EnsureEmbeddedHierarchy party.
            cardTemplate?.UseEnemyTemplateHierarchy(sceneTemplate);
            cardTemplate?.WireReferences();

            if (Application.isPlaying)
            {
                Destroy(sceneTemplate.gameObject);
            }
            else
            {
                DestroyImmediate(sceneTemplate.gameObject);
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



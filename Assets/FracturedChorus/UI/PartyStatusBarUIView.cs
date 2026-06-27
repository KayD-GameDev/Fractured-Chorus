<<<<<<< HEAD
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Units;
using UnityEngine;

namespace FracturedChorus.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PartyStatusBarSyncBinder))]
    public class PartyStatusBarUIView : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private RectTransform cardsRow;
        [SerializeField] private PartyUnitCardView cardTemplate;
        [Tooltip("Giữ vị trí/kích thước khung ngoài đã chỉnh trong Hierarchy.")]
        [SerializeField] private bool preserveSceneLayout = true;

        [Header("Sync")]
        [SerializeField] private PartyStatusBarSyncBinder syncBinder;

        private CombatSession _session;
        private UnitView[] _unitViews;

        public RectTransform CardsRow => cardsRow;
        public PartyUnitCardView CardTemplate => cardTemplate;
=======
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
        public const float DefaultCardSpacing = 0.5f;

        [SerializeField] private RectTransform cardsRow;
        [SerializeField] private PartyMemberCardView cardTemplate;
        [SerializeField] private float cardSpacing = DefaultCardSpacing;

        private readonly List<PartyMemberCardView> _spawnedCards = new();

        private void Awake()
        {
            WireReferences();
            HideTemplate();
        }
>>>>>>> main

        public void WireReferences()
        {
            if (cardsRow == null)
            {
                cardsRow = transform.Find("CardsRow") as RectTransform;
            }

<<<<<<< HEAD
            if (cardTemplate == null && cardsRow != null)
            {
                cardTemplate = PartyStatusBarCardFactory.ResolveTemplate(cardsRow);
            }

            if (syncBinder == null)
            {
                syncBinder = GetComponent<PartyStatusBarSyncBinder>();
            }
        }

        public void Bind(CombatSession session, UnitView[] unitViews)
        {
            _session = session;
            _unitViews = unitViews;
            gameObject.SetActive(true);
            WireReferences();
            ApplyDefaultRootLayout();

            syncBinder ??= GetComponent<PartyStatusBarSyncBinder>();
            syncBinder?.Sync(session, unitViews, this);
        }

        public void RefreshAll()
        {
            syncBinder?.RefreshAll();
        }

        private void ApplyDefaultRootLayout()
        {
            if (preserveSceneLayout)
=======
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
>>>>>>> main
            {
                return;
            }

<<<<<<< HEAD
            var rootRect = transform as RectTransform;
            if (rootRect == null)
=======
            var playerViews = unitViews
                .Where(v => v != null && v.Side == GridSide.Player && v.Unit != null)
                .ToList();

            PartyCardDisplayOrder.SortUnitViews(playerViews);

            foreach (var view in playerViews.Take(MaxPartyCards))
            {
                SpawnCard(view.Unit, view.ResolvePreset());
            }
        }

        public void BindFromSession(CombatSession session)
        {
            ClearSpawnedCards();

            if (session?.Grid == null || cardTemplate == null || cardsRow == null)
>>>>>>> main
            {
                return;
            }

<<<<<<< HEAD
            var playerCount = CountPlayerUnits(_session, _unitViews);
            var cardCount = Mathf.Max(1, playerCount);

            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(16f, -16f);
            rootRect.sizeDelta = PartyStatusBarLayout.DefaultRootSize(cardCount);
        }

        private static int CountPlayerUnits(CombatSession session, UnitView[] unitViews)
        {
            var count = 0;
            if (unitViews != null)
            {
                foreach (var view in unitViews)
                {
                    if (view != null && view.Side == Combat.Grid.GridSide.Player && view.Unit != null)
                    {
                        count++;
                    }
                }
            }

            if (count == 0 && session?.Grid != null)
            {
                foreach (var unit in session.Grid.PlayerUnits)
                {
                    if (unit != null && unit.Side == Combat.Grid.GridSide.Player)
                    {
                        count++;
                    }
                }
            }

            return count;
=======
            var units = session.Grid.PlayerUnits
                .Where(u => u != null)
                .OrderBy(u => u, Comparer<CombatUnit>.Create(PartyCardDisplayOrder.CompareUnits))
                .Take(MaxPartyCards)
                .ToList();

            foreach (var unit in units)
            {
                SpawnCard(unit, ResolvePresetForUnit(unit));
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
            }
        }

        private void SpawnCard(CombatUnit unit, UnitPresetSO preset)
        {
            var card = Instantiate(cardTemplate, cardsRow);
            card.gameObject.SetActive(true);
            card.name = $"Card_{unit.DisplayName}";
            card.WireReferences();
            card.Bind(unit, preset);
            _spawnedCards.Add(card);
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
>>>>>>> main
        }
    }
}

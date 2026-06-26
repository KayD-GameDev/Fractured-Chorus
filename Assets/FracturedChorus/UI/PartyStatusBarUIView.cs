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

        public void WireReferences()
        {
            if (cardsRow == null)
            {
                cardsRow = transform.Find("CardsRow") as RectTransform;
            }

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
            {
                return;
            }

            var rootRect = transform as RectTransform;
            if (rootRect == null)
            {
                return;
            }

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
        }
    }
}

using System.Collections.Generic;
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Thanh thẻ quái ở góc phải trên màn hình — cùng logic thẻ với party (avatar, bar máu, badge hệ).
    /// Tái dùng PartyMemberCardView; xếp ngang, mọc từ phải sang trái.
    /// </summary>
    public class EnemyStatusBarUIView : MonoBehaviour
    {
        public const int MaxEnemyCards = 9;

        [SerializeField] private RectTransform cardsRow;
        [SerializeField] private PartyMemberCardView cardTemplate;
        [SerializeField] private Vector2 cardSize = new Vector2(80f, 115f);
        [SerializeField] private float horizontalSpacing = 8f;

        private readonly List<PartyMemberCardView> _spawnedCards = new();

        public int BoundUnitCount => _spawnedCards.Count;

        /// <summary>Gắn template (mượn từ party bar) — gọi trước khi Bind.</summary>
        public void SetCardTemplate(PartyMemberCardView template)
        {
            cardTemplate = template;
        }

        public void EnsureRow()
        {
            if (cardsRow != null)
            {
                return;
            }

            var existing = transform.Find("EnemyCardsRow") as RectTransform;
            if (existing != null)
            {
                cardsRow = existing;
                return;
            }

            var go = new GameObject("EnemyCardsRow", typeof(RectTransform));
            cardsRow = go.GetComponent<RectTransform>();
            cardsRow.SetParent(transform, false);
            cardsRow.anchorMin = new Vector2(1f, 1f);
            cardsRow.anchorMax = new Vector2(1f, 1f);
            cardsRow.pivot = new Vector2(1f, 1f);
            cardsRow.anchoredPosition = Vector2.zero;
            cardsRow.sizeDelta = cardSize;
        }

        public void BindFromSession(CombatSession session)
        {
            if (session?.Grid == null)
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
            if (cardTemplate == null)
            {
                Debug.LogWarning("[EnemyStatusBar] Missing card template.");
                return;
            }

            EnsureRow();
            ClearSpawnedCards();

            foreach (var unit in units)
            {
                if (unit == null)
                {
                    continue;
                }

                SpawnCard(unit);
            }

            LayoutRow();
        }

        private void SpawnCard(CombatUnit unit)
        {
            var card = Instantiate(cardTemplate, cardsRow);
            card.gameObject.SetActive(true);
            card.name = $"EnemyCard_{unit.DisplayName}";

            var rect = card.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.localScale = Vector3.one;
                rect.sizeDelta = cardSize;
            }

            card.WireReferences();
            card.Bind(unit, ResolvePresetForUnit(unit));
            _spawnedCards.Add(card);
        }

        private void LayoutRow()
        {
            var count = _spawnedCards.Count;
            if (count == 0)
            {
                return;
            }

            var step = cardSize.x + horizontalSpacing;

            // Mọc từ phải sang trái: thẻ đầu sát mép phải, các thẻ sau lùi trái.
            for (var i = 0; i < count; i++)
            {
                var rect = _spawnedCards[i].transform as RectTransform;
                if (rect == null)
                {
                    continue;
                }

                rect.anchoredPosition = new Vector2(-i * step, 0f);
                rect.SetSiblingIndex(i);
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

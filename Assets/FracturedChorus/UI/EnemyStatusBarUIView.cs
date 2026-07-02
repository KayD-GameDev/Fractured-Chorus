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
    /// Thanh thẻ quái — cùng logic + kích thước thẻ với party (avatar, bar máu, badge hệ),
    /// canh **cùng Y với thẻ players**. Tái dùng PartyMemberCardView; xếp ngang, mọc từ phải sang trái.
    /// </summary>
    public class EnemyStatusBarUIView : MonoBehaviour
    {
        public const int MaxEnemyCards = 6;

        [SerializeField] private RectTransform cardsRow;
        [SerializeField] private PartyMemberCardView cardTemplate;
        [Tooltip("Fallback khi template chưa có size — mặc định khớp thẻ party 115×167.")]
        [SerializeField] private Vector2 cardSize = new Vector2(PartyCardLayout.CardWidth, 167f);
        [SerializeField] private float horizontalSpacing = PartyCardLayout.CardGap;

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
            cardsRow.sizeDelta = GetCardSize();
        }

        /// <summary>Kích thước thẻ = template (khớp thẻ party), fallback về cardSize.</summary>
        private Vector2 GetCardSize()
        {
            if (cardTemplate != null)
            {
                var templateRect = cardTemplate.transform as RectTransform;
                if (templateRect != null && templateRect.sizeDelta.x > 0f && templateRect.sizeDelta.y > 0f)
                {
                    return templateRect.sizeDelta;
                }
            }

            return cardSize;
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
                // Anchor top-right, pivot top → thẻ quái canh **cùng đỉnh Y** với thẻ party (top-aligned).
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.localScale = Vector3.one;
                rect.sizeDelta = GetCardSize();

                var layoutElement = rect.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    layoutElement.ignoreLayout = true;
                }
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

            var step = GetCardSize().x + horizontalSpacing;

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

using System.Collections.Generic;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.UI
{
    [DisallowMultipleComponent]
    public class PartyStatusBarSyncBinder : MonoBehaviour
    {
        [Header("Roster — ô UI ↔ StatBlock / Preset trong Resources")]
        [SerializeField] private PartyStatusBarRosterSO roster;
        [SerializeField] private string rosterResourcesPath = "PartyStatusBar/PartyRoster";
        [SerializeField] private bool loadRosterFromResources = true;

        [Header("Icon hệ (tùy chọn)")]
        [SerializeField] private HarmonyElementVisualSetSO elementVisualSet;
        [SerializeField] private string elementVisualSetResourcesPath = "PartyStatusBar/HarmonyElementVisualSet";
        [SerializeField] private bool loadElementVisualSetFromResources = true;

        private readonly List<PartyUnitCardView> _activeCards = new();
        private CombatSession _session;

        public void Sync(CombatSession session, UnitView[] unitViews, PartyStatusBarUIView barView)
        {
            _session = session;
            barView?.WireReferences();

            if (barView == null || session == null)
            {
                return;
            }

            var cardsRow = barView.CardsRow;
            var template = barView.CardTemplate;
            if (cardsRow == null)
            {
                Debug.LogWarning("[PartyStatusBarSync] Thiếu CardsRow.");
                return;
            }

            var members = OrderMembersByRoster(CollectPlayerMembers(session, unitViews), ResolveRoster());
            _activeCards.Clear();
            _activeCards.AddRange(PartyStatusBarCardFactory.EnsureCardCount(cardsRow, template, members.Count));

            var resolvedRoster = ResolveRoster();
            var elementIcons = ResolveElementVisualSet();

            for (var i = 0; i < _activeCards.Count; i++)
            {
                var member = members[i];
                var slot = ResolveSlot(resolvedRoster, i);
                var presentation = BuildPresentation(slot, member.View, member.Unit, elementIcons);
                _activeCards[i].ApplyPresentation(member.Unit, presentation);
            }

            if (_session != null)
            {
                _session.OnUnitHpChanged -= HandleUnitHpChanged;
                _session.OnUnitHpChanged += HandleUnitHpChanged;
            }
        }

        public void RefreshAll()
        {
            foreach (var card in _activeCards)
            {
                card?.RefreshHp();
            }
        }

        private PartyStatusBarRosterSO ResolveRoster()
        {
            if (roster != null)
            {
                return roster;
            }

            return loadRosterFromResources
                ? PartyResourcesCatalog.LoadRoster(rosterResourcesPath)
                : null;
        }

        private HarmonyElementVisualSetSO ResolveElementVisualSet()
        {
            if (elementVisualSet != null)
            {
                return elementVisualSet;
            }

            return loadElementVisualSetFromResources
                ? PartyResourcesCatalog.LoadElementVisualSet(elementVisualSetResourcesPath)
                : null;
        }

        private static PartyStatusBarSlotDefinition ResolveSlot(PartyStatusBarRosterSO rosterAsset, int slotIndex)
        {
            if (rosterAsset?.slots == null || slotIndex < 0 || slotIndex >= rosterAsset.slots.Length)
            {
                return null;
            }

            return rosterAsset.slots[slotIndex];
        }

        private static PartyCardPresentation BuildPresentation(
            PartyStatusBarSlotDefinition slot,
            UnitView view,
            CombatUnit unit,
            HarmonyElementVisualSetSO elementIcons)
        {
            var statBlock = slot != null
                ? PartyResourcesCatalog.LoadStatBlock(slot.statBlockResourceName)
                : view?.ResolvePreset()?.statBlock;

            var preset = slot != null
                ? PartyResourcesCatalog.LoadUnitPreset(slot.unitPresetResourceName)
                : view?.ResolvePreset();

            if (preset == null && unit != null)
            {
                preset = PartyResourcesCatalog.LoadUnitPreset(unit.UnitId);
            }

            if (statBlock == null)
            {
                statBlock = preset?.statBlock;
            }

            var element = statBlock != null ? statBlock.element : preset?.ResolveStats().Element ?? default;
            var avatar = ResolveAvatar(view, preset);
            var icon = preset?.elementIcon ?? elementIcons?.GetIcon(element);

            return new PartyCardPresentation
            {
                StatBlock = statBlock,
                Preset = preset,
                Avatar = avatar,
                ElementIcon = icon,
                Element = element,
                UnitMatchKey = slot?.unitMatchKey ?? preset?.unitId ?? unit?.UnitId
            };
        }

        private static Sprite ResolveAvatar(UnitView view, UnitPresetSO preset)
        {
            if (preset?.battleSprite != null)
            {
                return preset.battleSprite;
            }

            if (view == null)
            {
                return null;
            }

            var renderer = view.GetComponent<SpriteRenderer>();
            return renderer != null ? renderer.sprite : null;
        }

        private static List<PartyMemberBinding> OrderMembersByRoster(
            List<PartyMemberBinding> members,
            PartyStatusBarRosterSO rosterAsset)
        {
            if (rosterAsset?.slots == null || rosterAsset.slots.Length == 0 || members.Count == 0)
            {
                return members;
            }

            var ordered = new List<PartyMemberBinding>();
            var used = new HashSet<CombatUnit>();

            foreach (var slot in rosterAsset.slots)
            {
                if (string.IsNullOrWhiteSpace(slot?.unitMatchKey))
                {
                    continue;
                }

                var match = FindMemberByKey(members, slot.unitMatchKey, used);
                if (match.HasValue)
                {
                    ordered.Add(match.Value);
                }
            }

            foreach (var member in members)
            {
                if (member.Unit != null && used.Add(member.Unit))
                {
                    ordered.Add(member);
                }
            }

            return ordered;
        }

        private static PartyMemberBinding? FindMemberByKey(
            List<PartyMemberBinding> members,
            string unitMatchKey,
            HashSet<CombatUnit> used)
        {
            foreach (var member in members)
            {
                if (member.Unit == null || used.Contains(member.Unit))
                {
                    continue;
                }

                if (!MatchesUnitKey(member, unitMatchKey))
                {
                    continue;
                }

                used.Add(member.Unit);
                return member;
            }

            return null;
        }

        private static bool MatchesUnitKey(PartyMemberBinding member, string unitMatchKey)
        {
            if (string.IsNullOrWhiteSpace(unitMatchKey))
            {
                return false;
            }

            var key = unitMatchKey.Trim();
            if (member.Unit != null &&
                (string.Equals(member.Unit.UnitId, key, System.StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(member.Unit.DisplayName, key, System.StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (member.View != null &&
                string.Equals(member.View.DemoUnitKey, key, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var preset = member.View?.ResolvePreset();
            return preset != null &&
                   string.Equals(preset.unitId, key, System.StringComparison.OrdinalIgnoreCase);
        }

        private static List<PartyMemberBinding> CollectPlayerMembers(CombatSession session, UnitView[] unitViews)
        {
            var members = new List<PartyMemberBinding>();
            var seen = new HashSet<CombatUnit>();

            if (unitViews != null)
            {
                foreach (var view in unitViews)
                {
                    if (view == null || view.Side != GridSide.Player || view.Unit == null || !seen.Add(view.Unit))
                    {
                        continue;
                    }

                    members.Add(new PartyMemberBinding(view.Unit, view));
                }
            }

            if (members.Count == 0 && session?.Grid != null)
            {
                foreach (var unit in session.Grid.PlayerUnits)
                {
                    if (unit == null || unit.Side != GridSide.Player || !seen.Add(unit))
                    {
                        continue;
                    }

                    members.Add(new PartyMemberBinding(unit, FindViewForUnit(unitViews, unit)));
                }
            }

            return members;
        }

        private static UnitView FindViewForUnit(UnitView[] unitViews, CombatUnit unit)
        {
            if (unitViews == null)
            {
                return null;
            }

            foreach (var view in unitViews)
            {
                if (view != null && view.Unit == unit)
                {
                    return view;
                }
            }

            return null;
        }

        private void HandleUnitHpChanged(CombatUnit unit)
        {
            foreach (var card in _activeCards)
            {
                if (card != null && card.BoundUnit == unit)
                {
                    card.RefreshHp();
                }
            }
        }

        private void OnDestroy()
        {
            if (_session != null)
            {
                _session.OnUnitHpChanged -= HandleUnitHpChanged;
            }
        }

        private readonly struct PartyMemberBinding
        {
            public PartyMemberBinding(CombatUnit unit, UnitView view)
            {
                Unit = unit;
                View = view;
            }

            public CombatUnit Unit { get; }
            public UnitView View { get; }
        }
    }
}

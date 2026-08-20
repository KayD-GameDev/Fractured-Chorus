using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Meta;
using FracturedChorus.Meta.Economy;
using UnityEngine;

namespace FracturedChorus.RunMap
{
    public enum ShopChoiceKind
    {
        HealPotion = 0,
        Prep = 1,
        Armor = 2,
        Revive = 3,
        PlaceCounterPlus1 = 4,
        Leave = 5
    }

    public readonly struct ShopChoiceOffer
    {
        public ShopChoiceOffer(
            ShopChoiceKind kind,
            string title,
            string description,
            string kindLabel,
            int cost,
            bool available)
        {
            Kind = kind;
            Title = title;
            Description = description;
            KindLabel = kindLabel;
            Cost = cost;
            Available = available;
        }

        public ShopChoiceKind Kind { get; }
        public string Title { get; }
        public string Description { get; }
        public string KindLabel { get; }
        public int Cost { get; }
        public bool Available { get; }
    }

    public static class ShopChoiceCatalog
    {
        public static ShopChoiceOffer[] CreateOffers(bool previewAllAvailable = false)
        {
            var notes = GameMetaSession.HasSession ? GameMetaSession.Current.Wallet.Notes : 0;
            var armorPct = Mathf.RoundToInt(EconomyTable.ShopArmorShieldPercent * 100f);
            return new[]
            {
                Paid(
                    ShopChoiceKind.HealPotion,
                    "Bình máu",
                    "Hồi 50% Max HP cho unit còn sống.",
                    "HP",
                    EconomyTable.ShopHealPotionCost,
                    notes,
                    previewAllAvailable || PartyRunHpStore.CanHealLiving(),
                    previewAllAvailable),
                Paid(
                    ShopChoiceKind.Prep,
                    "Prep",
                    $"+{EconomyTable.ShopPrepAmount} Prep khi vào battle kế.",
                    "PREP",
                    EconomyTable.ShopPrepCost,
                    notes,
                    true,
                    previewAllAvailable),
                Paid(
                    ShopChoiceKind.Armor,
                    "Giáp",
                    $"Khiên {armorPct}% Max HP khi vào battle kế.",
                    "ARMOR",
                    EconomyTable.ShopArmorCost,
                    notes,
                    true,
                    previewAllAvailable),
                Paid(
                    ShopChoiceKind.Revive,
                    "Thuốc hồi sinh",
                    "Hồi sinh 1 unit (1 HP).",
                    "REVIVE",
                    EconomyTable.ShopReviveCost,
                    notes,
                    previewAllAvailable || PartyRunHpStore.CanRevive(),
                    previewAllAvailable),
                Paid(
                    ShopChoiceKind.PlaceCounterPlus1,
                    "+1 Node Counter",
                    "Skill đặt lên board kế tiếp +1 counter.",
                    "COUNTER +1",
                    EconomyTable.ShopPlaceCounterCost,
                    notes,
                    true,
                    previewAllAvailable)
            };
        }

        public static ShopChoiceOffer LeaveOffer()
        {
            return new ShopChoiceOffer(
                ShopChoiceKind.Leave,
                "Leave",
                "Rời shop, không mua.",
                "GO",
                0,
                true);
        }

        public static bool TryApply(ShopChoiceOffer offer)
        {
            if (offer.Kind == ShopChoiceKind.Leave)
            {
                return true;
            }

            if (!offer.Available || !GameMetaSession.HasSession)
            {
                return false;
            }

            if (!GameMetaSession.Current.Wallet.Spend(offer.Cost))
            {
                return false;
            }

            switch (offer.Kind)
            {
                case ShopChoiceKind.HealPotion:
                    PartyRunHpStore.HealLivingPercent(CampChoiceCatalog.HealPercent);
                    return true;
                case ShopChoiceKind.Prep:
                    RunEventCombatMods.AddPrep(EconomyTable.ShopPrepAmount);
                    return true;
                case ShopChoiceKind.Armor:
                    RunEventCombatMods.AddArmorShieldPercent(EconomyTable.ShopArmorShieldPercent);
                    return true;
                case ShopChoiceKind.Revive:
                    PartyRunHpStore.ReviveOne(CampChoiceCatalog.ReviveHp);
                    return true;
                case ShopChoiceKind.PlaceCounterPlus1:
                    RunEventCombatMods.AddPlaceCounterPlus(1);
                    return true;
                default:
                    return false;
            }
        }

        private static ShopChoiceOffer Paid(
            ShopChoiceKind kind,
            string title,
            string description,
            string kindLabel,
            int cost,
            int notes,
            bool requirementMet,
            bool previewAllAvailable)
        {
            var available = previewAllAvailable || (requirementMet && notes >= cost);
            return new ShopChoiceOffer(
                kind,
                title,
                description,
                $"{kindLabel} · {cost}♪",
                cost,
                available);
        }
    }
}

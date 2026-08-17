using FracturedChorus.Combat.Bootstrap;

namespace FracturedChorus.RunMap
{
    public enum CampChoiceKind
    {
        Heal50 = 0,
        ReviveOne = 1,
        Continue = 2
    }

    public readonly struct CampChoiceOffer
    {
        public CampChoiceOffer(
            CampChoiceKind kind,
            string title,
            string description,
            string kindLabel,
            bool available)
        {
            Kind = kind;
            Title = title;
            Description = description;
            KindLabel = kindLabel;
            Available = available;
        }

        public CampChoiceKind Kind { get; }
        public string Title { get; }
        public string Description { get; }
        public string KindLabel { get; }
        public bool Available { get; }
    }

    public static class CampChoiceCatalog
    {
        public const float HealPercent = 0.5f;
        public const int ReviveHp = 1;

        public static CampChoiceOffer[] CreateOffers(bool previewAllAvailable = false)
        {
            return new[]
            {
                new CampChoiceOffer(
                    CampChoiceKind.Heal50,
                    "Rest",
                    "Hồi 50% Max HP cho unit còn sống. Không hồi sinh.",
                    "HEAL 50%",
                    previewAllAvailable || PartyRunHpStore.CanHealLiving()),
                new CampChoiceOffer(
                    CampChoiceKind.ReviveOne,
                    "Encore",
                    "Hồi sinh 1 unit (1 HP). Không heal.",
                    "REVIVE",
                    previewAllAvailable || PartyRunHpStore.CanRevive()),
                new CampChoiceOffer(
                    CampChoiceKind.Continue,
                    "Continue",
                    "Rời camp, giữ nguyên HP.",
                    "GO",
                    true)
            };
        }
    }
}

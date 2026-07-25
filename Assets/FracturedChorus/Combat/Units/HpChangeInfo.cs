namespace FracturedChorus.Combat.Units
{
    public enum HpChangeKind
    {
        Silent = 0,
        Damage = 1,
        Heal = 2
    }

    public readonly struct HpChangeInfo
    {
        public static HpChangeInfo Silent { get; } = new(HpChangeKind.Silent, 0, false);

        public HpChangeKind Kind { get; }
        public int Amount { get; }
        public bool IsCritical { get; }

        public HpChangeInfo(HpChangeKind kind, int amount, bool isCritical)
        {
            Kind = kind;
            Amount = amount;
            IsCritical = isCritical;
        }

        public bool ShouldShowFeedback => Kind != HpChangeKind.Silent && Amount > 0;
    }
}

namespace FracturedChorus.Combat.Grid
{
    public static class PositionalModifiers
    {
        public const int FrontColumnIndex = 0;
        public const int BackColumnIndex = DualGrid.Columns - 1;

        public const float FrontIncomingDamageMultiplier = 0.85f;
        public const float BackOutgoingDamageMultiplier = 1.15f;
        public const float BackHealMultiplier = 1.15f;

        public static bool IsFrontColumn(GridPosition position) =>
            position.IsValid() && position.Column == FrontColumnIndex;

        public static bool IsBackColumn(GridPosition position) =>
            position.IsValid() && position.Column == BackColumnIndex;

        public static float GetDamageModifier(GridPosition attackerPos, GridPosition targetPos)
        {
            var mod = 1f;
            if (IsFrontColumn(targetPos))
            {
                mod *= FrontIncomingDamageMultiplier;
            }

            if (IsBackColumn(attackerPos))
            {
                mod *= BackOutgoingDamageMultiplier;
            }

            return mod;
        }

        public static float GetHealPotencyModifier(GridPosition healerPos) =>
            IsBackColumn(healerPos) ? BackHealMultiplier : 1f;
    }
}

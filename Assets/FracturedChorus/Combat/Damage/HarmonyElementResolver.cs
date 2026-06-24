namespace FracturedChorus.Combat.Damage
{
    /// <summary>
    /// Nhịp khắc Giai điệu · Giai điệu khắc Hòa âm · Hòa âm khắc Nhịp.
    /// Advantage = 1.5× · Disadvantage = 0.5× pre-condition.
    /// </summary>
    public static class HarmonyElementResolver
    {
        public static HarmonyRelation GetRelation(HarmonyElement attacker, HarmonyElement defender)
        {
            if (Beats(attacker, defender))
            {
                return HarmonyRelation.Advantage;
            }

            if (Beats(defender, attacker))
            {
                return HarmonyRelation.Disadvantage;
            }

            return HarmonyRelation.Neutral;
        }

        private static bool Beats(HarmonyElement attacker, HarmonyElement defender)
        {
            return (attacker, defender) switch
            {
                (HarmonyElement.Rhythm, HarmonyElement.Melody) => true,
                (HarmonyElement.Melody, HarmonyElement.Harmony) => true,
                (HarmonyElement.Harmony, HarmonyElement.Rhythm) => true,
                _ => false
            };
        }
    }
}

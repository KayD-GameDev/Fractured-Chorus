namespace FracturedChorus.Combat.Damage
{
    public enum HarmonyRelation
    {
        Neutral,
        Advantage,
        Disadvantage
    }

    public static class HarmonyRelationExtensions
    {
        public static float GetPreCondition(this HarmonyRelation relation)
        {
            return relation switch
            {
                HarmonyRelation.Advantage => 1.5f,
                HarmonyRelation.Disadvantage => 0.5f,
                _ => 1f
            };
        }
    }
}

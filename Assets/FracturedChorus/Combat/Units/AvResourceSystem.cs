using FracturedChorus.Data;

namespace FracturedChorus.Combat.Units
{
    /// <summary>
    /// Base AV = action order on the same beat (lower → acts first) and enemy damage target pick
    /// (higher BaseAv preferred). Does not gate skill placement — place freely if S1/S/S2 do not overlap.
    /// </summary>
    public static class AvResourceSystem
    {
        public static float GetActionPriority(CombatUnit unit)
        {
            return unit?.Stats.BaseAv ?? float.MaxValue;
        }

        public static bool CanAffordPhaseAction(CombatUnit unit, SkillDefinitionSO skill)
        {
            return unit != null && skill != null;
        }
    }
}

using FracturedChorus.Data;

namespace FracturedChorus.Combat.Units
{
    /// <summary>
    /// Base AV = thứ tự ưu tiên hành động trên cùng beat (thấp hơn → đi trước).
    /// Chi phí skill chỉ trừ vào Phase AV (party budget), không trừ Base AV.
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

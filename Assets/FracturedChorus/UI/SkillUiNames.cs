using FracturedChorus.Data;

namespace FracturedChorus.UI
{
    public static class SkillUiNames
    {
        public static string GetDisplayName(SkillDefinitionSO skill)
        {
            if (skill == null)
            {
                return "Skill";
            }

            return skill.slotKind switch
            {
                SkillSlotKind.Skill => "Skill 1",
                SkillSlotKind.Ultimate => "Skill 2",
                _ => skill.displayName
            };
        }
    }
}

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

            return !string.IsNullOrEmpty(skill.displayName) ? skill.displayName : skill.skillId;
        }
    }
}

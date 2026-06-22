namespace FracturedChorus.Data
{
    public enum SkillSlotKind
    {
        BasicAttack,
        Skill,
        Ultimate,
        Guard
    }

    public static class SkillAvCosts
    {
        public const int BasicAttack = 0;
        public const int Skill = 25;
        public const int Ultimate = 50;
        public const int Guard = 0;

        public static int GetCost(SkillSlotKind kind)
        {
            return kind switch
            {
                SkillSlotKind.BasicAttack => BasicAttack,
                SkillSlotKind.Skill => Skill,
                SkillSlotKind.Ultimate => Ultimate,
                SkillSlotKind.Guard => Guard,
                _ => 0
            };
        }
    }
}

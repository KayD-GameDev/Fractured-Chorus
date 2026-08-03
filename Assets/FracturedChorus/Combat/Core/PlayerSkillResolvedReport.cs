using FracturedChorus.Combat.Units;
using FracturedChorus.Data;

namespace FracturedChorus.Combat.Core
{
    public readonly struct PlayerSkillResolvedReport
    {
        public PlayerSkillResolvedReport(CombatUnit source, CombatUnit target, SkillDefinitionSO skill, int beatIndex)
        {
            Source = source;
            Target = target;
            Skill = skill;
            BeatIndex = beatIndex;
        }

        public CombatUnit Source { get; }
        public CombatUnit Target { get; }
        public SkillDefinitionSO Skill { get; }
        public int BeatIndex { get; }

        public bool IsValid => Source != null && Skill != null;
    }
}

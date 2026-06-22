using FracturedChorus.Combat.Damage;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;

namespace FracturedChorus.Combat.Actions
{
    public class CombatContext
    {
        public DualGrid Grid { get; set; }
        public BeatTimelineEngine Timeline { get; set; }
        public CombatUnit Source { get; set; }
        public CombatUnit Target { get; set; }
        public SkillDefinitionSO Skill { get; set; }
        public BeatTiming BeatTiming { get; set; } = BeatTiming.OnBeat;
        public HarmonyRelation Harmony { get; set; } = HarmonyRelation.Neutral;
    }
}

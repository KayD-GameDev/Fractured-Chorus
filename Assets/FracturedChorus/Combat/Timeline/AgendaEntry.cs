using System;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;

namespace FracturedChorus.Combat.Timeline
{
    [Serializable]
    public class AgendaEntry
    {
        public string EntryId;
        public CombatUnit Unit;
        public SkillDefinitionSO Skill;
        public int BeatIndex;
        public int Delay;
        public bool IsEmpowered;
        public bool EmpowerResolved;
        public bool EffectPayloadApplied;

        public AgendaEntry(CombatUnit unit, SkillDefinitionSO skill, int beatIndex)
        {
            EntryId = Guid.NewGuid().ToString("N");
            Unit = unit;
            Skill = skill;
            BeatIndex = beatIndex;
            Delay = skill != null ? skill.delay : 0;
        }
    }
}

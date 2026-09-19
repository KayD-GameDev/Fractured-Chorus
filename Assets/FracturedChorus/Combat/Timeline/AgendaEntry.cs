using System;
using System.Collections.Generic;
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
        public int StandingAfterOverride = -1;
        public int ActiveBeatsOverride;
        public float PendingHitDamage = -1f;
        public bool PlanningEffectApplied;
        public int PlanningDelayAmount;
        public readonly List<TelegraphBeatMove> PlanningDelayMoves = new();
        public CombatUnit PlanningReduceTarget;
        public readonly List<CombatUnit> PlanningReduceTargets = new();
        public int PlanningReduceAmount;

        public AgendaEntry(CombatUnit unit, SkillDefinitionSO skill, int beatIndex)
        {
            EntryId = Guid.NewGuid().ToString("N");
            Unit = unit;
            Skill = skill;
            BeatIndex = beatIndex;
            Delay = skill != null ? skill.delay : 0;
        }
    }

    public readonly struct TelegraphBeatMove
    {
        public EnemyTelegraph Telegraph { get; }
        public int FromBeat { get; }
        public int ToBeat { get; }
        public EnemyTelegraph MergedInto { get; }
        public int OccupantHitsBefore { get; }
        public BossNoteTier OccupantTierBefore { get; }

        public bool IsMerge => MergedInto != null;

        public TelegraphBeatMove(EnemyTelegraph telegraph, int fromBeat, int toBeat)
            : this(telegraph, fromBeat, toBeat, null, 0, BossNoteTier.Red)
        {
        }

        public TelegraphBeatMove(
            EnemyTelegraph telegraph,
            int fromBeat,
            int toBeat,
            EnemyTelegraph mergedInto,
            int occupantHitsBefore,
            BossNoteTier occupantTierBefore)
        {
            Telegraph = telegraph;
            FromBeat = fromBeat;
            ToBeat = toBeat;
            MergedInto = mergedInto;
            OccupantHitsBefore = occupantHitsBefore;
            OccupantTierBefore = occupantTierBefore;
        }
    }
}

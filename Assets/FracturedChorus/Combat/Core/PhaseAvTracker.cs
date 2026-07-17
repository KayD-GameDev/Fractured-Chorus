using FracturedChorus.Combat.Timeline;
using UnityEngine;

namespace FracturedChorus.Combat.Core
{
    /// <summary>
    /// Deprecated placement budget. Skill assign is footprint-only (S1/S/S2 overlap).
    /// BaseAv on units still drives action order and enemy damage targeting.
    /// Kept for ResolveTimelinePhaseIndex helpers / UI phase index display.
    /// </summary>
    public class PhaseAvTracker
    {
        public const int Phase1Budget = 150;
        public const int LaterPhaseBudget = 100;

        public int TimelinePhaseIndex { get; private set; }
        public int SpentThisPhase { get; private set; }

        public void ResetForPlanning()
        {
            TimelinePhaseIndex = 0;
            SpentThisPhase = 0;
        }

        public static int ResolveTimelinePhaseIndex(int beatIndex)
        {
            var phaseIndex = TimelineConstants.GetPhaseIndex(beatIndex);
            if (TimelineConstants.IsPhaseDividerAfter(beatIndex))
            {
                phaseIndex = Mathf.Min(TimelineConstants.PhaseCount - 1, phaseIndex + 1);
            }

            return phaseIndex;
        }

        public int GetBudgetForPhase(int phaseIndex)
        {
            return phaseIndex == 0 ? Phase1Budget : LaterPhaseBudget;
        }

        public int CurrentBudget => GetBudgetForPhase(TimelinePhaseIndex);

        public int Remaining => CurrentBudget - SpentThisPhase;

        public bool CanAfford(int cost) => true;

        public void RecordSpend(int cost)
        {
        }

        public void RecordRefund(int cost)
        {
        }
    }
}

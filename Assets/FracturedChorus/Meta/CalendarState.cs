using System;

namespace FracturedChorus.Meta
{
    [Serializable]
    public sealed class CalendarState
    {
        public GameDate CurrentDate = GameDate.Arc1Start;
        public DayPhase CurrentPhase = DayPhase.Morning;
        public int SlotsUsedToday;
        public bool MorningQuizDone;

        public const int MaxSlotsPerDay = 2;

        public bool IsArcComplete => CurrentDate > GameDate.Arc1End;

        public int DaysUntilVaultDeadline => CurrentDate.DaysUntil(GameDate.VaultDeadline);

        public void ResetForNewDay(GameDate date)
        {
            CurrentDate = date;
            CurrentPhase = DayPhase.Morning;
            SlotsUsedToday = 0;
            MorningQuizDone = false;
        }

        public void CompleteMorningQuiz()
        {
            MorningQuizDone = true;
            CurrentPhase = DayPhase.Day;
        }

        public bool ConsumeActivitySlot()
        {
            if (SlotsUsedToday >= MaxSlotsPerDay)
            {
                return false;
            }

            SlotsUsedToday++;

            if (CurrentPhase == DayPhase.Day)
            {
                CurrentPhase = DayPhase.Evening;
                return false;
            }

            if (CurrentPhase == DayPhase.Evening)
            {
                AdvanceDay();
                return true;
            }

            return false;
        }

        public void AdvanceDay()
        {
            CurrentDate = CurrentDate.AddDays(1);
            CurrentPhase = DayPhase.Morning;
            SlotsUsedToday = 0;
            MorningQuizDone = false;
        }

        public void SkipMorningForForcedDay()
        {
            MorningQuizDone = true;
            CurrentPhase = DayPhase.Day;
        }

        public void MarkFullDayConsumed()
        {
            SlotsUsedToday = MaxSlotsPerDay;
            AdvanceDay();
        }
    }
}

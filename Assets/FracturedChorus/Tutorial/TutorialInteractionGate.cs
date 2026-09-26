namespace FracturedChorus.Tutorial
{
    public static class TutorialInteractionGate
    {
        public static bool IsCadenceIntroActive => TutorialDirector.IsCadenceIntroActive;

        public static bool AllowsFormationDrag => TutorialDirector.AllowsFormationDrag;

        public static bool AllowsUnitSkillPanelOpen => TutorialDirector.AllowsUnitSkillPanelOpen;

        public static bool AllowsSkillTimelineDrop => TutorialDirector.AllowsSkillTimelineDrop;

        public static bool AllowsExecute => TutorialDirector.AllowsExecute;

        public static bool BlocksSlideshowCombatUi => TutorialDirector.BlocksSlideshowCombatUi;
    }
}

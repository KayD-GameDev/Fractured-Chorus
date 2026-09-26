using UnityEngine;

namespace FracturedChorus.Tutorial
{
    public enum TutorialStepKind
    {
        Slide = 0,
        PracticeFormation = 1,
        AwaitDeploy = 2,
        AwaitUnitSkillPanel = 3,
        AwaitSkillPlaced = 4,
        AwaitDeployThenQte = 5,
        AwaitDeployUntilPlanning = 6,
        BeginPlanningSandbox = 7
    }

    [CreateAssetMenu(fileName = "TutorialStep", menuName = "Fractured Chorus/Tutorial Step")]
    public sealed class TutorialStepSO : ScriptableObject
    {
        public string stepId;
        public string trackId;
        [TextArea(2, 6)] public string bodyCopy;
        public bool requiresConfirm = true;
        public TutorialStepKind kind = TutorialStepKind.Slide;
        public Sprite coachPortrait;
        public Sprite panelImage;
        [TextArea(2, 4)] public string qteHintCopy;
        [Tooltip("AwaitDeployUntilPlanning: tiếp tục khi planning mở ở segment này (1 = PHASE 2).")]
        public int planningSegmentToContinue = 1;
    }
}

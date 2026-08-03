using UnityEngine;

namespace FracturedChorus.Tutorial
{
    public enum TutorialStepKind
    {
        Slide = 0,
        PracticeFormation = 1,
        AwaitDeploy = 2
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
    }
}

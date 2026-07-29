using UnityEngine;

namespace FracturedChorus.Tutorial
{
    [CreateAssetMenu(fileName = "TutorialStep", menuName = "Fractured Chorus/Tutorial Step")]
    public sealed class TutorialStepSO : ScriptableObject
    {
        public string stepId;
        public string trackId;
        [TextArea(2, 6)] public string bodyCopy;
        public bool requiresConfirm = true;
        public Sprite coachPortrait;
        public Sprite panelImage;
    }
}

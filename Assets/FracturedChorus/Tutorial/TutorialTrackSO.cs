using UnityEngine;

namespace FracturedChorus.Tutorial
{
    [CreateAssetMenu(fileName = "TutorialTrack", menuName = "Fractured Chorus/Tutorial Track")]
    public sealed class TutorialTrackSO : ScriptableObject
    {
        public string trackId = TutorialDirector.TrackCadenceIntro;
        public string completionFlag;
        public bool slideshow = true;
        public TutorialStepSO[] steps;
    }
}

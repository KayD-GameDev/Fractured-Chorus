using UnityEngine;

namespace FracturedChorus.Menu
{
    [CreateAssetMenu(fileName = "OffBeatTrack", menuName = "Fractured Chorus/Off-Beat Track")]
    public sealed class OffBeatTrackSO : ScriptableObject
    {
        public string trackId;
        public string title;
        public string artist;
        public AudioClip clip;
        public Sprite cover;
    }
}

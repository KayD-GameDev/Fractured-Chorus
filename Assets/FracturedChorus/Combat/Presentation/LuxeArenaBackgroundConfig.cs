using UnityEngine;
using UnityEngine.Video;

namespace FracturedChorus.Combat.Presentation
{
    [CreateAssetMenu(
        fileName = "LuxeArenaBackgroundConfig",
        menuName = "Fractured Chorus/Luxe Arena/Background Config")]
    public sealed class LuxeArenaBackgroundConfig : ScriptableObject
    {
        public VideoClip SceneBackgroundVideo;
        public bool LoopSceneVideo = true;
        [Range(0f, 1f)] public float SceneVideoAlpha = 1f;
    }
}

#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;

namespace FracturedChorus.Editor
{
    internal static class EditorPreviewAudio
    {
        private static MethodInfo _playPreviewClip;
        private static MethodInfo _stopAllPreviewClips;

        public static void PlayPreviewClip(AudioClip clip, int startSample, bool loop)
        {
            if (clip == null)
            {
                return;
            }

            var play = ResolvePlayMethod();
            if (play == null)
            {
                Debug.LogWarning("[BeatTap] PlayPreviewClip API not found in this Unity version.");
                return;
            }

            play.Invoke(null, new object[] { clip, startSample, loop });
        }

        public static void StopAllPreviewClips()
        {
            var stop = ResolveStopMethod();
            stop?.Invoke(null, null);
        }

        private static MethodInfo ResolvePlayMethod()
        {
            if (_playPreviewClip != null)
            {
                return _playPreviewClip;
            }

            _playPreviewClip = FindStaticMethod("PlayPreviewClip", typeof(AudioClip), typeof(int), typeof(bool))
                ?? FindStaticMethod("PlayPreviewClip", typeof(AudioClip), typeof(int), typeof(bool), typeof(bool));

            return _playPreviewClip;
        }

        private static MethodInfo ResolveStopMethod()
        {
            if (_stopAllPreviewClips != null)
            {
                return _stopAllPreviewClips;
            }

            _stopAllPreviewClips = FindStaticMethod("StopAllPreviewClips")
                ?? FindStaticMethod("StopAllClips");

            return _stopAllPreviewClips;
        }

        private static MethodInfo FindStaticMethod(string name, params System.Type[] parameterTypes)
        {
            var audioUtil = typeof(UnityEditor.AssetDatabase).Assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtil == null)
            {
                return null;
            }

            return audioUtil.GetMethod(
                name,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                parameterTypes,
                null);
        }
    }
}
#endif

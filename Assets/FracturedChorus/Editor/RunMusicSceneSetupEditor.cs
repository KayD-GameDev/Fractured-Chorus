#if UNITY_EDITOR
using FracturedChorus.Audio;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class RunMusicSceneSetupEditor
    {
        private const string SourcePath = @"C:\Users\Asus\Downloads\Eternal Spark - Candence.mp3";
        private const string ClipPath = "Assets/FracturedChorus/Audio/Music/EternalSpark_Candence.mp3";
        private const string BeatMapPath = "Assets/FracturedChorus/Audio/Music/EternalSpark_Candence_BeatMap.asset";
        private const string ResourcesBeatMapPath = "Assets/FracturedChorus/Resources/Music/EternalSpark_Candence_BeatMap.asset";
        private const float CandenceBpm = 152f;

        [MenuItem("Fractured Chorus/Import Run Candence Music")]
        public static void ImportRunCandenceMusic()
        {
            if (System.IO.File.Exists(SourcePath))
            {
                var destFull = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Application.dataPath) ?? string.Empty,
                    ClipPath.Replace('/', System.IO.Path.DirectorySeparatorChar));
                var destDir = System.IO.Path.GetDirectoryName(destFull);
                if (!string.IsNullOrEmpty(destDir) && !System.IO.Directory.Exists(destDir))
                {
                    System.IO.Directory.CreateDirectory(destDir);
                }

                System.IO.File.Copy(SourcePath, destFull, true);
            }

            AssetDatabase.Refresh();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
            if (clip == null)
            {
                Debug.LogError($"[RunMusic] Missing clip at {ClipPath}");
                return;
            }

            EnsureBeatMapAsset(BeatMapPath, clip);
            EnsureResourcesFolder();
            EnsureBeatMapAsset(ResourcesBeatMapPath, clip);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[RunMusic] Candence imported: {clip.length:F1}s, {AssetDatabase.LoadAssetAtPath<MusicBeatMapSO>(BeatMapPath).TotalBeatsForClip()} beats @ {CandenceBpm} BPM.");
        }

        private static void EnsureResourcesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/FracturedChorus/Resources"))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus", "Resources");
            }

            if (!AssetDatabase.IsValidFolder("Assets/FracturedChorus/Resources/Music"))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus/Resources", "Music");
            }
        }

        private static void EnsureBeatMapAsset(string assetPath, AudioClip clip)
        {
            var beatMap = AssetDatabase.LoadAssetAtPath<MusicBeatMapSO>(assetPath);
            if (beatMap == null)
            {
                beatMap = ScriptableObject.CreateInstance<MusicBeatMapSO>();
                AssetDatabase.CreateAsset(beatMap, assetPath);
            }

            beatMap.EditorSetData(clip, CandenceBpm, 0f);
            EditorUtility.SetDirty(beatMap);
        }
    }
}
#endif

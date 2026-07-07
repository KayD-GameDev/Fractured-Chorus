#if UNITY_EDITOR
using FracturedChorus.Audio;
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FracturedChorus.Editor
{
    public static class CombatMusicSceneSetup
    {
        private const string ClipPath = "Assets/FracturedChorus/Audio/Music/EternalSpark_CadenceRemix.mp3";
        private const string BeatMapPath = "Assets/FracturedChorus/Audio/Music/EternalSpark_CadenceRemix_BeatMap.asset";
        private const string BeatMapCsvPath = "Assets/FracturedChorus/Audio/Music/EternalSpark_CadenceRemix_beats.csv";
        private const string PlanningClipPath = "Assets/FracturedChorus/Audio/Music/EternalSpark_PlanningSilent.mp3";
        private const string PlanningTransitionPath = "Assets/FracturedChorus/Audio/SFX/Combat_PlanningTransition.wav";
        private const string PerfectCounterPath = "Assets/FracturedChorus/Audio/SFX/Combat_PerfectCounter.wav";
        private const string PlanningSourceDownload = @"c:\Users\Asus\Downloads\Eternal Spark - BGM Silent.mp3";
        private const string TransitionSourceDownload = @"c:\Users\Asus\Downloads\Transition Sound.wav";
        private const string PerfectCounterSourceDownload = @"c:\Users\Asus\Downloads\Perfect Sound -1.wav";

        [MenuItem("Fractured Chorus/Import Planning Audio From Downloads")]
        public static void ImportPlanningAudioFromDownloads()
        {
            ImportIfMissing(PlanningSourceDownload, PlanningClipPath);
            ImportAudio(TransitionSourceDownload, PlanningTransitionPath);
            ImportAudio(PerfectCounterSourceDownload, PerfectCounterPath);
            AssetDatabase.Refresh();
            Debug.Log("[Fractured Chorus] Planning BGM + transition WAV + perfect counter WAV imported.");
        }

        private static void ImportIfMissing(string sourcePath, string destAssetPath)
        {
            if (!System.IO.File.Exists(sourcePath))
            {
                Debug.LogWarning($"[CombatMusic] Missing source file: {sourcePath}");
                return;
            }

            ImportAudio(sourcePath, destAssetPath);
        }

        private static void ImportAudio(string sourcePath, string destAssetPath)
        {
            if (!System.IO.File.Exists(sourcePath))
            {
                Debug.LogWarning($"[CombatMusic] Missing source file: {sourcePath}");
                return;
            }

            var destFull = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath) ?? string.Empty,
                destAssetPath.Replace('/', System.IO.Path.DirectorySeparatorChar));

            var destDir = System.IO.Path.GetDirectoryName(destFull);
            if (!string.IsNullOrEmpty(destDir) && !System.IO.Directory.Exists(destDir))
            {
                System.IO.Directory.CreateDirectory(destDir);
            }

            System.IO.File.Copy(sourcePath, destFull, true);
        }

        [MenuItem("Fractured Chorus/Wire Combat Music (Current Scene)")]
        public static void WireCurrentScene()
        {
            ImportPlanningAudioFromDownloads();

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
            var beatMap = AssetDatabase.LoadAssetAtPath<MusicBeatMapSO>(BeatMapPath);
            var beatMapCsv = AssetDatabase.LoadAssetAtPath<TextAsset>(BeatMapCsvPath);
            var planningClip = AssetDatabase.LoadAssetAtPath<AudioClip>(PlanningClipPath);
            var planningTransition = AssetDatabase.LoadAssetAtPath<AudioClip>(PlanningTransitionPath);
            var perfectCounter = AssetDatabase.LoadAssetAtPath<AudioClip>(PerfectCounterPath);
            if (clip == null)
            {
                Debug.LogError($"[CombatMusic] Missing clip at {ClipPath}. Re-import project.");
                return;
            }

            var bootstrap = Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogError("[CombatMusic] No CombatPrototypeBootstrap in scene.");
                return;
            }

            var music = bootstrap.GetComponentInChildren<CombatMusicController>(true);
            if (music == null)
            {
                var go = new GameObject("CombatMusic");
                go.transform.SetParent(bootstrap.transform, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                music = go.AddComponent<CombatMusicController>();
                Undo.RegisterCreatedObjectUndo(go, "Create CombatMusic");
            }

            var sfx = music.GetComponent<CombatSfxController>();
            if (sfx == null)
            {
                sfx = music.gameObject.AddComponent<CombatSfxController>();
            }

            var so = new SerializedObject(music);
            so.FindProperty("bossTrack").objectReferenceValue = clip;
            so.FindProperty("beatMap").objectReferenceValue = beatMap;
            so.FindProperty("beatMapCsv").objectReferenceValue = beatMapCsv;
            if (planningClip != null)
            {
                so.FindProperty("planningClip").objectReferenceValue = planningClip;
            }

            if (planningTransition != null)
            {
                so.FindProperty("planningTransitionClip").objectReferenceValue = planningTransition;
            }

            so.FindProperty("planningStartSec").floatValue = 17f;
            so.FindProperty("planningVolume").floatValue = 0.25f;
            so.FindProperty("planningTransitionVolume").floatValue = 1f;

            var sourceProp = so.FindProperty("source");
            if (sourceProp.objectReferenceValue == null)
            {
                sourceProp.objectReferenceValue = music.GetComponent<AudioSource>();
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            var sfxSo = new SerializedObject(sfx);
            if (perfectCounter != null)
            {
                sfxSo.FindProperty("perfectCounterClip").objectReferenceValue = perfectCounter;
            }

            sfxSo.FindProperty("perfectCounterVolume").floatValue = 1f;
            sfxSo.ApplyModifiedPropertiesWithoutUndo();

            var bootstrapSo = new SerializedObject(bootstrap);
            bootstrapSo.FindProperty("musicController").objectReferenceValue = music;
            bootstrapSo.FindProperty("combatSfxController").objectReferenceValue = sfx;
            bootstrapSo.ApplyModifiedPropertiesWithoutUndo();

            var timeline = Object.FindAnyObjectByType<BeatTimelineUIView>();
            if (timeline != null)
            {
                var timelineSo = new SerializedObject(timeline);
                timelineSo.FindProperty("useMusicSync").boolValue = true;
                timelineSo.FindProperty("musicController").objectReferenceValue = music;
                timelineSo.FindProperty("combatSfxController").objectReferenceValue = sfx;
                timelineSo.FindProperty("autoBeatInterval").floatValue = 60f / 148f;
                timelineSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var cam = Camera.main;
            if (cam != null && cam.GetComponent<AudioListener>() == null)
            {
                Undo.AddComponent<AudioListener>(cam.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] Combat audio wired (boss + planning + transition WAV + perfect counter).");
        }
    }
}
#endif

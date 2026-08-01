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
        private const float BossRemixBpm = 152f;
        private const string ClipPath = "Assets/FracturedChorus/Audio/Music/EternalSpark_BossRemix.mp3";
        private const string BeatMapPath = "Assets/FracturedChorus/Audio/Music/EternalSpark_BossRemix_BeatMap.asset";
        private const string PerfectCounterPath = "Assets/FracturedChorus/Audio/SFX/Perfect sound Game.wav";
        private const string PerfectBlockPath = "Assets/FracturedChorus/Audio/SFX/Perfect sound SFX.wav";
        private const string ClashHitPath = "Assets/FracturedChorus/Audio/SFX/Clash Hit.wav";
        private const string RenSkill1Path = "Assets/FracturedChorus/Audio/SFX/Ren_Skill1.wav";
        private const string RenSkill2Path = "Assets/FracturedChorus/Audio/SFX/Ren_Skill2.wav";
        private const string RenSkill3Path = "Assets/FracturedChorus/Audio/SFX/Ren_Skill3.mp3";
        private const string CodaSkill1Path = "Assets/FracturedChorus/Audio/SFX/Coda_Skill1.mp3";
        private const string CodaSkill23Path = "Assets/FracturedChorus/Audio/SFX/Coda_Skill23.wav";
        private const string PerfectCounterSourceDownload = @"d:\Project 1\Clash Hit Game.wav";
        private const string PerfectBlockSourceDownload = @"d:\Project 1\Perfect sound SFX.wav";
        private const string ClashHitSourceDownload = @"d:\Project 1\Clash Hit Game.wav";
        private const string RenSkill1SourceDownload = @"d:\Project 1\Skill 1 Ren SFX.mp3.wav";
        private const string RenSkill2SourceDownload = @"d:\Project 1\Skill 2 Ren SFX.wav";
        private const string RenSkill3SourceDownload = @"d:\Project 1\Skill 3 Ren SFX.mp3";
        private const string CodaSkill1SourceDownload = @"d:\Project 1\freesound_community-swinging-staff-whoosh-strong-08-44658.mp3";
        private const string CodaSkill23SourceDownload = @"d:\Project 1\Skill 2 3 Coda SFX.wav";
        [MenuItem("Fractured Chorus/Import Combat Audio From Downloads")]
        public static void ImportCombatAudioFromDownloads()
        {
            ImportAudio(PerfectCounterSourceDownload, PerfectCounterPath);
            ImportAudio(PerfectBlockSourceDownload, PerfectBlockPath);
            ImportAudio(ClashHitSourceDownload, ClashHitPath);
            ImportAudio(RenSkill1SourceDownload, RenSkill1Path);
            ImportAudio(RenSkill2SourceDownload, RenSkill2Path);
            ImportAudio(RenSkill3SourceDownload, RenSkill3Path);
            ImportAudio(CodaSkill1SourceDownload, CodaSkill1Path);
            ImportAudio(CodaSkill23SourceDownload, CodaSkill23Path);
            AssetDatabase.Refresh();
            Debug.Log("[Fractured Chorus] Perfect counter/block + clash hit + Ren/Coda skill SFX imported.");
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
            ImportCombatAudioFromDownloads();

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
            var beatMap = AssetDatabase.LoadAssetAtPath<MusicBeatMapSO>(BeatMapPath);
            var perfectCounter = AssetDatabase.LoadAssetAtPath<AudioClip>(PerfectCounterPath);
            var perfectBlock = AssetDatabase.LoadAssetAtPath<AudioClip>(PerfectBlockPath);
            var clashHit = AssetDatabase.LoadAssetAtPath<AudioClip>(ClashHitPath);
            var renSkill1 = AssetDatabase.LoadAssetAtPath<AudioClip>(RenSkill1Path);
            var renSkill2 = AssetDatabase.LoadAssetAtPath<AudioClip>(RenSkill2Path);
            var renSkill3 = AssetDatabase.LoadAssetAtPath<AudioClip>(RenSkill3Path);
            var codaSkill1 = AssetDatabase.LoadAssetAtPath<AudioClip>(CodaSkill1Path);
            var codaSkill23 = AssetDatabase.LoadAssetAtPath<AudioClip>(CodaSkill23Path);
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
            so.FindProperty("fallbackBpm").floatValue = BossRemixBpm;
            so.FindProperty("loopStartBar").intValue = 0;
            so.FindProperty("loopEndBar").intValue = -1;
            so.FindProperty("duckVolume").floatValue = 0.7f;
            so.FindProperty("duckCutoffHz").floatValue = 900f;
            so.FindProperty("duckFadeSec").floatValue = 0.25f;

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

            if (perfectBlock != null)
            {
                sfxSo.FindProperty("perfectBlockClip").objectReferenceValue = perfectBlock;
            }

            if (clashHit != null)
            {
                sfxSo.FindProperty("clashHitClip").objectReferenceValue = clashHit;
            }

            if (renSkill1 != null)
            {
                sfxSo.FindProperty("renSkill1Clip").objectReferenceValue = renSkill1;
            }

            if (renSkill2 != null)
            {
                sfxSo.FindProperty("renSkill2Clip").objectReferenceValue = renSkill2;
            }

            if (renSkill3 != null)
            {
                sfxSo.FindProperty("renSkill3Clip").objectReferenceValue = renSkill3;
            }

            if (codaSkill1 != null)
            {
                sfxSo.FindProperty("codaSkill1Clip").objectReferenceValue = codaSkill1;
            }

            if (codaSkill23 != null)
            {
                sfxSo.FindProperty("codaSkill23Clip").objectReferenceValue = codaSkill23;
            }

            sfxSo.FindProperty("perfectCounterVolume").floatValue = 1f;
            sfxSo.FindProperty("perfectBlockVolume").floatValue = 1f;
            sfxSo.FindProperty("clashHitVolume").floatValue = 1f;
            sfxSo.FindProperty("renSkillVolume").floatValue = 1f;
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
                timelineSo.FindProperty("autoBeatInterval").floatValue = 60f / BossRemixBpm;
                timelineSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var cam = Camera.main;
            if (cam != null && cam.GetComponent<AudioListener>() == null)
            {
                Undo.AddComponent<AudioListener>(cam.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log(
                $"[Fractured Chorus] Combat audio wired: '{clip.name}' @ {BossRemixBpm} BPM, continuous with planning duck.");
        }
    }
}
#endif

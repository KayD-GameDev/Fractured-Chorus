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

        [MenuItem("Fractured Chorus/Wire Combat Music (Current Scene)")]
        public static void WireCurrentScene()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
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

            var so = new SerializedObject(music);
            so.FindProperty("bossTrack").objectReferenceValue = clip;
            var sourceProp = so.FindProperty("source");
            if (sourceProp.objectReferenceValue == null)
            {
                sourceProp.objectReferenceValue = music.GetComponent<AudioSource>();
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            var bootstrapSo = new SerializedObject(bootstrap);
            bootstrapSo.FindProperty("musicController").objectReferenceValue = music;
            bootstrapSo.FindProperty("playBossMusicOnStart").boolValue = true;
            bootstrapSo.ApplyModifiedPropertiesWithoutUndo();

            var timeline = Object.FindAnyObjectByType<BeatTimelineUIView>();
            if (timeline != null)
            {
                var timelineSo = new SerializedObject(timeline);
                timelineSo.FindProperty("useMusicSync").boolValue = true;
                timelineSo.FindProperty("musicController").objectReferenceValue = music;
                timelineSo.FindProperty("autoBeatInterval").floatValue = 60f / 148f;
                timelineSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var cam = Camera.main;
            if (cam != null && cam.GetComponent<AudioListener>() == null)
            {
                Undo.AddComponent<AudioListener>(cam.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] Combat music wired. Save scene, then press Play on CombatPrototype.");
        }
    }
}
#endif

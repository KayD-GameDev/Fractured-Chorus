#if UNITY_EDITOR
using FracturedChorus.Combat.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace FracturedChorus.Editor
{
    public static class LuxeArenaBackgroundSetupEditor
    {
        private const string ArenaRoot = "Assets/FracturedChorus/Art/Backgrounds/LuxeArena";
        private const string ConfigPath = ArenaRoot + "/LuxeArenaBackgroundConfig.asset";
        private const string SceneVideoPath = ArenaRoot + "/luxe_arena_astra_scene_bg_v1.mp4";
        private const string LegacyLayerRootName = "LuxeArenaLayers";
        private const string LegacyImageName = "Image";

        [InitializeOnLoadMethod]
        private static void AutoWireAfterReload()
        {
            EditorApplication.delayCall += TryAutoWireOpenScene;
        }

        private static void TryAutoWireOpenScene()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "CombatPrototype")
            {
                return;
            }

            var bgRoot = GameObject.Find(CombatUiHierarchy.BackgroundCanvasName);
            if (bgRoot == null)
            {
                return;
            }

            if (NeedsRewire(bgRoot))
            {
                WireBossArenaBackground();
            }
        }

        [MenuItem("Fractured Chorus/Luxe Arena/Wire Boss Arena Background To Scene")]
        public static void WireBossArenaBackgroundMenu()
        {
            WireBossArenaBackground();
        }

        [MenuItem("Fractured Chorus/Luxe Arena/Refresh Background Config From Art Folder")]
        public static void RefreshConfigFromArtMenu()
        {
            var config = LoadOrCreateConfig();
            PopulateConfigFromArt(config);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Selection.activeObject = config;
            Debug.Log("[LuxeArena] Config refreshed — Astra scene video.");
        }

        public static void WireFromBatch()
        {
            var scenePath = "Assets/FracturedChorus/Scenes/CombatPrototype.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            WireBossArenaBackground();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[LuxeArena] Batch wire complete.");
        }

        public static void WireBossArenaBackground()
        {
            var bgRoot = GameObject.Find(CombatUiHierarchy.BackgroundCanvasName);
            if (bgRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "Luxe Arena",
                    "Không tìm thấy Background canvas. Mở CombatPrototype trước.",
                    "OK");
                return;
            }

            var config = LoadOrCreateConfig();
            PopulateConfigFromArt(config);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            if (config.SceneBackgroundVideo == null)
            {
                EditorUtility.DisplayDialog(
                    "Luxe Arena",
                    $"Thiếu video tại:\n{SceneVideoPath}",
                    "OK");
                return;
            }

            RemoveLegacyBackground(bgRoot.transform);
            var sceneVideo = EnsureSceneVideo(bgRoot.transform);

            var director = bgRoot.GetComponent<LuxeArenaBackgroundDirector>();
            if (director == null)
            {
                director = Undo.AddComponent<LuxeArenaBackgroundDirector>(bgRoot);
            }

            var so = new SerializedObject(director);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("sceneVideoImage").objectReferenceValue = sceneVideo;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(director);

            EditorSceneManager.MarkSceneDirty(bgRoot.scene);
            EditorSceneManager.SaveScene(bgRoot.scene);
            Selection.activeGameObject = sceneVideo.gameObject;
            Debug.Log("[LuxeArena] Boss background = Astra scene video (legacy layers removed).");
        }

        private static bool NeedsRewire(GameObject bgRoot)
        {
            if (bgRoot.transform.Find(LegacyLayerRootName) != null)
            {
                return true;
            }

            if (bgRoot.transform.Find(LegacyImageName) != null)
            {
                return true;
            }

            var director = bgRoot.GetComponent<LuxeArenaBackgroundDirector>();
            if (director == null)
            {
                return true;
            }

            var so = new SerializedObject(director);
            return so.FindProperty("sceneVideoImage") == null ||
                   so.FindProperty("sceneVideoImage").objectReferenceValue == null ||
                   so.FindProperty("config").objectReferenceValue == null;
        }

        private static void RemoveLegacyBackground(Transform bgRoot)
        {
            DestroyNamedChild(bgRoot, LegacyLayerRootName);
            DestroyNamedChild(bgRoot, LegacyImageName);
            DestroyNamedChild(bgRoot, "AudienceBand");
            DestroyNamedChild(bgRoot, "AudienceLeft");
            DestroyNamedChild(bgRoot, "AudienceCenter");
            DestroyNamedChild(bgRoot, "AudienceRight");
        }

        private static RawImage EnsureSceneVideo(Transform bgRoot)
        {
            var rt = EnsureRect(bgRoot, "SceneVideo");
            StretchFull(rt);
            rt.SetAsFirstSibling();

            var raw = rt.GetComponent<RawImage>();
            if (raw == null)
            {
                raw = Undo.AddComponent<RawImage>(rt.gameObject);
            }

            Undo.RecordObject(raw, "Setup SceneVideo");
            raw.raycastTarget = false;
            raw.color = Color.white;
            raw.enabled = true;
            EditorUtility.SetDirty(raw);
            return raw;
        }

        private static LuxeArenaBackgroundConfig LoadOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<LuxeArenaBackgroundConfig>(ConfigPath);
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<LuxeArenaBackgroundConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static void PopulateConfigFromArt(LuxeArenaBackgroundConfig config)
        {
            Undo.RecordObject(config, "Populate Luxe Arena Config");

            var sceneVideo = AssetDatabase.LoadAssetAtPath<VideoClip>(SceneVideoPath);
            if (sceneVideo != null)
            {
                config.SceneBackgroundVideo = sceneVideo;
                config.LoopSceneVideo = true;
                config.SceneVideoAlpha = 1f;
            }
        }

        private static void DestroyNamedChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                return;
            }

            Undo.DestroyObjectImmediate(child.gameObject);
        }

        private static RectTransform EnsureRect(Transform parent, string name)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
    }
}
#endif

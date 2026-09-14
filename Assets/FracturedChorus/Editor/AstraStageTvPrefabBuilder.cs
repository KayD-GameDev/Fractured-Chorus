#if UNITY_EDITOR
using FracturedChorus.Combat.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FracturedChorus.Editor
{
    public static class AstraStageTvPrefabBuilder
    {
        private const string PrefabPath =
            "Assets/FracturedChorus/Resources/UI/Combat/Boss/Astra/AstraStageTv.prefab";
        private const string ConfigPath =
            "Assets/FracturedChorus/Resources/UI/Combat/Boss/Astra/AstraStageTvConfig.asset";
        private const string CombatPrototypeScene = "CombatPrototype";

        [InitializeOnLoadMethod]
        private static void AutoPlaceOnCombatPrototype()
        {
            EditorApplication.delayCall += TryPlaceIfSceneOpen;
        }

        private static void TryPlaceIfSceneOpen()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != CombatPrototypeScene)
            {
                return;
            }

            if (Object.FindAnyObjectByType<AstraStageTvView>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            PlaceInOpenScene(save: true);
        }

        [MenuItem("Fractured Chorus/Luxe Arena/Place Astra Stage TV In CombatPrototype")]
        public static void PlaceInSceneMenu()
        {
            PlaceInOpenScene(save: true);
        }

        [MenuItem("Fractured Chorus/Luxe Arena/Rebuild Astra Stage TV Prefab")]
        public static void RebuildPrefabMenu()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var config = AssetDatabase.LoadAssetAtPath<AstraStageTvConfig>(ConfigPath);
            if (prefab == null || config == null)
            {
                Debug.LogError("[AstraStageTv] Prefab or config missing. Keep the Resources assets.");
                return;
            }

            var instance = Object.Instantiate(prefab);
            instance.name = AstraStageTvView.ObjectName;
            var view = instance.GetComponent<AstraStageTvView>();
            if (view == null)
            {
                view = instance.AddComponent<AstraStageTvView>();
            }

            var so = new SerializedObject(view);
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();
            view.EnsureBuilt();
            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            Debug.Log("[AstraStageTv] Prefab rebuilt.");
        }

        public static AstraStageTvView PlaceInOpenScene(bool save)
        {
            var bgRoot = GameObject.Find(CombatUiHierarchy.BackgroundCanvasName);
            if (bgRoot == null)
            {
                Debug.LogError("[AstraStageTv] Open CombatPrototype — missing Background canvas.");
                return null;
            }

            var existing = bgRoot.transform.Find(AstraStageTvView.ObjectName)
                ?.GetComponent<AstraStageTvView>();
            if (existing == null)
            {
                existing = Object.FindAnyObjectByType<AstraStageTvView>(FindObjectsInactive.Include);
            }

            if (existing == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab != null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, bgRoot.scene);
                    instance.name = AstraStageTvView.ObjectName;
                    Undo.RegisterCreatedObjectUndo(instance, "Place Astra Stage TV");
                    instance.transform.SetParent(bgRoot.transform, false);
                    existing = instance.GetComponent<AstraStageTvView>();
                }
            }

            if (existing == null)
            {
                var go = new GameObject(AstraStageTvView.ObjectName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, "Create Astra Stage TV");
                go.transform.SetParent(bgRoot.transform, false);
                existing = Undo.AddComponent<AstraStageTvView>(go);
            }

            var config = AssetDatabase.LoadAssetAtPath<AstraStageTvConfig>(ConfigPath);
            var viewSo = new SerializedObject(existing);
            if (config != null)
            {
                viewSo.FindProperty("config").objectReferenceValue = config;
            }

            viewSo.ApplyModifiedPropertiesWithoutUndo();
            existing.EnsureBuilt();
            existing.ShowAtRest();
            existing.PlaceAfterSceneVideo();
            EditorUtility.SetDirty(existing);
            EditorSceneManager.MarkSceneDirty(bgRoot.scene);
            Selection.activeGameObject = existing.gameObject;
            if (save)
            {
                EditorSceneManager.SaveScene(bgRoot.scene);
            }

            Debug.Log("[AstraStageTv] Placed. Tune the RectTransform in the scene — the drop returns to it.");
            return existing;
        }
    }
}
#endif

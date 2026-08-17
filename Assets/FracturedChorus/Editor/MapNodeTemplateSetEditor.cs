#if UNITY_EDITOR
using System;
using FracturedChorus.Data;
using FracturedChorus.RunMap.Core;
using FracturedChorus.RunMap.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class MapNodeTemplateSetEditor
    {
        public const string PrefabFolder = "Assets/FracturedChorus/RunMap/Prefabs";
        public const string NodePrefabPath = PrefabFolder + "/MapNode.prefab";
        public const string ConnectionPrefabPath = PrefabFolder + "/MapConnection.prefab";
        public const string SetPath = MapNodeTemplateSetSO.DefaultAssetPath;

        [MenuItem("Fractured Chorus/Run Map/Extract Node Templates To Prefabs", false, 34)]
        public static void ExtractAndStripScene()
        {
            var set = EnsureAssets();
            if (set == null)
            {
                return;
            }

            AssignToOpenScene(set);
            StripSceneObjects();
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Fractured Chorus] Map node templates → {SetPath}. Scene templates stripped.");
        }

        public static MapNodeTemplateSetSO EnsureAssets()
        {
            if (!AssetDatabase.IsValidFolder("Assets/FracturedChorus/RunMap/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus/RunMap", "Prefabs");
            }

            var nodePrefab = AssetDatabase.LoadAssetAtPath<MapNodeView>(NodePrefabPath);
            var connectionPrefab = AssetDatabase.LoadAssetAtPath<MapConnectionLineView>(ConnectionPrefabPath);
            if (nodePrefab == null || connectionPrefab == null)
            {
                Debug.LogError("[Fractured Chorus] Missing MapNode/MapConnection prefab in " + PrefabFolder);
                return AssetDatabase.LoadAssetAtPath<MapNodeTemplateSetSO>(SetPath);
            }

            var iconSet = MapNodeIconSetupEditor.EnsureIconSetAsset();

            var set = AssetDatabase.LoadAssetAtPath<MapNodeTemplateSetSO>(SetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<MapNodeTemplateSetSO>();
                AssetDatabase.CreateAsset(set, SetPath);
            }

            var types = (MapNodeType[])Enum.GetValues(typeof(MapNodeType));
            var entries = new MapNodeTypePrefab[types.Length];
            for (var i = 0; i < types.Length; i++)
            {
                entries[i] = new MapNodeTypePrefab(types[i], nodePrefab);
            }

            set.EditorAssign(iconSet, nodePrefab, connectionPrefab, entries);
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            return set;
        }

        private static void AssignToOpenScene(MapNodeTemplateSetSO set)
        {
            if (set == null)
            {
                return;
            }
            foreach (var mapView in UnityEngine.Object.FindObjectsByType<RunMapUIView>(FindObjectsInactive.Include))
            {
                var so = new SerializedObject(mapView);
                var templateProp = so.FindProperty("templateSet");
                if (templateProp != null)
                {
                    templateProp.objectReferenceValue = set;
                }
                if (so.FindProperty("iconSet") != null && set.IconSet != null)
                {
                    so.FindProperty("iconSet").objectReferenceValue = set.IconSet;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(mapView);
            }

            foreach (var preview in UnityEngine.Object.FindObjectsByType<RunMapLayoutScenePreview>(FindObjectsInactive.Include))
            {
                var so = new SerializedObject(preview);
                var prop = so.FindProperty("templateSet");
                if (prop != null)
                {
                    prop.objectReferenceValue = set;
                }

                var mapTemplateProp = so.FindProperty("mapTemplate");
                if (mapTemplateProp != null && mapTemplateProp.objectReferenceValue == null)
                {
                    mapTemplateProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<MapTemplateSO>(
                        "Assets/FracturedChorus/Data/ScriptableObjects/Presets/MapTemplate_Default.asset");
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(preview);
            }

        }

        private static void StripSceneObjects()
        {
            DestroyByName("NodeTemplate");
            DestroyByName("ConnectionTemplate");
            DestroyByName("NodeEditPreview");

            foreach (var preview in UnityEngine.Object.FindObjectsByType<RunMapLayoutScenePreview>(FindObjectsInactive.Include))
            {
                preview.Rebuild();
            }
        }

        private static void DestroyByName(string objectName)
        {
            var go = GameObject.Find(objectName);
            if (go == null)
            {
                var all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
                foreach (var t in all)
                {
                    if (t.name == objectName)
                    {
                        go = t.gameObject;
                        break;
                    }
                }
            }

            if (go != null)
            {
                Undo.DestroyObjectImmediate(go);
            }
        }
    }
}
#endif

#if UNITY_EDITOR
using FracturedChorus.UI.Loading;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class LoadingScreenPrefabBuilder
    {
        private const string PrefabPath = "Assets/FracturedChorus/Resources/UI/LoadingScreen.prefab";

        [MenuItem("Fractured Chorus/Build Loading Screen Prefab")]
        public static void Build()
        {
            LoadingScreenController controller = null;

            try
            {
                EnsureFolder("Assets/FracturedChorus/Resources");
                EnsureFolder("Assets/FracturedChorus/Resources/UI");

                controller = LoadingScreenController.BuildRuntimeHierarchy();
                PrefabUtility.SaveAsPrefabAsset(controller.gameObject, PrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log("[Fractured Chorus] Loading screen prefab built at Assets/FracturedChorus/Resources/UI/LoadingScreen.prefab.");
            }
            catch (System.Exception error)
            {
                Debug.LogError("[Fractured Chorus] Failed to build loading screen prefab: " + error);
                EditorUtility.DisplayDialog(
                    "Build Loading Screen Prefab",
                    "Build failed. Check Console for details.",
                    "OK");
            }
            finally
            {
                if (controller != null)
                {
                    Object.DestroyImmediate(controller.gameObject);
                }
            }
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parts = assetPath.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
#endif

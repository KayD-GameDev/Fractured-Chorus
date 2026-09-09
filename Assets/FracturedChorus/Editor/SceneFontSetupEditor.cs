#if UNITY_EDITOR
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class SceneFontSetupEditor
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/FracturedChorus/Scenes/MainMenuStartGame.unity",
            "Assets/FracturedChorus/Scenes/PrologueVN.unity",
            "Assets/FracturedChorus/Scenes/OpeningInvestigation.unity",
            "Assets/FracturedChorus/Scenes/CampusHub.unity",
            "Assets/FracturedChorus/Scenes/FlowerShopWork.unity",
            "Assets/FracturedChorus/Scenes/RunMapPrototype.unity",
            "Assets/FracturedChorus/Scenes/CombatPrototype.unity",
            "Assets/FracturedChorus/Scenes/CombatTutorial.unity",
        };

        [MenuItem("Fractured Chorus/UI/Sync Fonts In Active Scene")]
        public static void SyncFontsInActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("[Fractured Chorus] No active scene.");
                return;
            }

            ApplyToScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Fractured Chorus] Synced UI fonts in {scene.name}.");
        }

        [MenuItem("Fractured Chorus/UI/Sync Fonts In All Game Scenes")]
        public static void SyncFontsInAllScenes()
        {
            var activePath = SceneManager.GetActiveScene().path;
            var touched = 0;
            foreach (var path in ScenePaths)
            {
                if (!System.IO.File.Exists(path))
                {
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                ApplyToScene(scene);
                EditorSceneManager.SaveScene(scene);
                touched++;
            }

            if (!string.IsNullOrEmpty(activePath))
            {
                EditorSceneManager.OpenScene(activePath, OpenSceneMode.Single);
            }

            Debug.Log($"[Fractured Chorus] Synced UI fonts in {touched} scenes.");
        }

        public static void ApplyToOpenScene(bool markDirty = true)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                return;
            }

            ApplyToScene(scene);
            if (markDirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        public static void ApplyToScene(Scene scene)
        {
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (canvas.gameObject.scene != scene)
                {
                    continue;
                }

                EnsureFontBootstrap(canvas.gameObject);
                UiFontCatalog.ApplyHierarchy(canvas.transform, true);
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                UiFontCatalog.ApplyHierarchy(root.transform, true);
            }
        }

        public static void EnsureFontBootstrap(GameObject canvasOrRoot)
        {
            if (canvasOrRoot == null)
            {
                return;
            }

            if (canvasOrRoot.GetComponent<SceneUiFontBootstrap>() == null)
            {
                canvasOrRoot.AddComponent<SceneUiFontBootstrap>();
                EditorUtility.SetDirty(canvasOrRoot);
            }
        }

        public static Text CreateUiText(
            string name,
            Transform parent,
            string content,
            int fontSize,
            TextAnchor anchor,
            Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color ?? Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            UiFontCatalog.ApplyAutomatic(text);
            return text;
        }

        public static void ApplyAutomatic(Text text)
        {
            UiFontCatalog.ApplyAutomatic(text);
        }

        public static void FinalizeSceneCanvas(GameObject canvas)
        {
            EnsureFontBootstrap(canvas);
            UiFontCatalog.ApplyHierarchy(canvas.transform, true);
        }
    }
}
#endif

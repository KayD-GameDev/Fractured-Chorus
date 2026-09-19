#if UNITY_EDITOR
using FracturedChorus.Narrative;
using FracturedChorus.VFX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class ButterflyTransitionPrologueInstaller
    {
        private const string PrologueScenePath = "Assets/FracturedChorus/Scenes/PrologueVN.unity";

        [MenuItem("Fractured Chorus/Narrative/Install Butterfly VFX On PrologueVN")]
        public static void InstallFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Prologue Butterfly VFX", "Exit Play Mode rồi chạy lại.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(PrologueScenePath);
            if (!InstallInternal())
            {
                EditorUtility.DisplayDialog("Prologue Butterfly VFX", "Install failed — xem Console.", "OK");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Fractured Chorus] Butterfly VFX gắn vào PrologueVN (scene đã lưu).");
        }

        public static void BatchInstall()
        {
            if (!System.IO.File.Exists(PrologueScenePath))
            {
                Debug.LogError("[Fractured Chorus] Missing " + PrologueScenePath);
                EditorApplication.Exit(1);
                return;
            }

            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(ButterflyTransitionPrefabBuilder.PrefabPath))
            {
                ButterflyTransitionPrefabBuilder.CreatePrefab();
            }

            EditorSceneManager.OpenScene(PrologueScenePath);
            if (!InstallInternal())
            {
                EditorApplication.Exit(1);
                return;
            }

            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Fractured Chorus] Batch: PrologueVN butterfly VFX installed.");
            EditorApplication.Exit(0);
        }

        public static ButterflyTransitionController EnsureOnPrologueCanvas(
            RectTransform canvasRect,
            PrologueVNController controller)
        {
            if (canvasRect == null || controller == null)
            {
                return null;
            }

            if (!InstallOnCanvas(canvasRect, controller))
            {
                return null;
            }

            return controller.GetComponentInChildren<ButterflyTransitionController>(true);
        }

        private static bool InstallInternal()
        {
            var canvasGo = GameObject.Find("PrologueCanvas");
            if (canvasGo == null)
            {
                Debug.LogError("[Fractured Chorus] PrologueCanvas not found.");
                return false;
            }

            var canvasRect = canvasGo.GetComponent<RectTransform>();
            var controller = Object.FindAnyObjectByType<PrologueVNController>();
            if (controller == null)
            {
                Debug.LogError("[Fractured Chorus] PrologueVNController not found.");
                return false;
            }

            return InstallOnCanvas(canvasRect, controller);
        }

        private static bool InstallOnCanvas(RectTransform canvasRect, PrologueVNController controller)
        {
            var canvasGo = canvasRect.gameObject;

            var legacyBg = canvasGo.transform.Find("ButterflyBackground")?.gameObject;
            if (legacyBg != null)
            {
                legacyBg.SetActive(false);
            }

            var transition = Object.FindAnyObjectByType<ButterflyTransitionController>(FindObjectsInactive.Include);
            GameObject vfxRoot;
            if (transition == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButterflyTransitionPrefabBuilder.PrefabPath);
                if (prefab == null)
                {
                    ButterflyTransitionPrefabBuilder.CreatePrefab();
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButterflyTransitionPrefabBuilder.PrefabPath);
                }

                if (prefab == null)
                {
                    Debug.LogError("[Fractured Chorus] FC_ButterflyTransition prefab missing.");
                    return false;
                }

                vfxRoot = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                vfxRoot.name = ButterflyTransitionHierarchy.RootName;
            }
            else
            {
                vfxRoot = transition.gameObject;
                ButterflyTransitionHierarchy.EnsureMissing(transition);
            }

            transition = vfxRoot.GetComponent<ButterflyTransitionController>();
            if (transition == null)
            {
                Debug.LogError("[Fractured Chorus] ButterflyTransitionController missing on VFX root.");
                return false;
            }

            var vfxRect = vfxRoot.GetComponent<RectTransform>();
            ButterflyTransitionHierarchy.EmbedUnderUiCanvas(vfxRect, canvasRect);
            if (legacyBg != null)
            {
                vfxRoot.transform.SetSiblingIndex(legacyBg.transform.GetSiblingIndex());
            }
            else
            {
                vfxRoot.transform.SetAsFirstSibling();
            }

            transition.ApplyPrologueBackgroundMode();
            transition.ResetTransition();
            transition.SetPresentationAlpha(0f);
            vfxRoot.SetActive(false);

            WireController(controller, transition);
            return true;
        }

        private static void WireController(PrologueVNController controller, ButterflyTransitionController transition)
        {
            var so = new SerializedObject(controller);
            var vfxProp = so.FindProperty("butterflyVfx");
            if (vfxProp != null)
            {
                vfxProp.objectReferenceValue = transition;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }
    }
}
#endif

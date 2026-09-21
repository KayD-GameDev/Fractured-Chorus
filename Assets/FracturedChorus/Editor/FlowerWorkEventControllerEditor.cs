#if UNITY_EDITOR
using FracturedChorus.Hub;
using FracturedChorus.Hub.FlowerWork;
using FracturedChorus.Narrative.Vn;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(FlowerWorkEventController))]
    public sealed class FlowerWorkEventControllerEditor : UnityEditor.Editor
    {
        private const string FlowerShopScenePath = "Assets/FracturedChorus/Scenes/FlowerShopWork.unity";

        [InitializeOnLoadMethod]
        private static void RegisterScenePreviewRefresh()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            if (!scene.path.Replace('\\', '/').EndsWith("FlowerShopWork.unity"))
            {
                return;
            }

            EditorApplication.delayCall += RefreshFlowerShopPreviewInScene;
        }

        private static void RefreshFlowerShopPreviewInScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var controllers = Object.FindObjectsByType<FlowerWorkEventController>(FindObjectsInactive.Include);
            foreach (var controller in controllers)
            {
                controller.ApplyEditorPreview();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            if (Application.isPlaying)
            {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            var controller = (FlowerWorkEventController)target;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Edit Mode Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "ThinkChoice = dialogue khách + khung 3 lựa chọn bên phải (chỉnh layout).\n" +
                "Thiếu Prompt/Options trong Hierarchy → Hub → Ensure FlowerShopWork Edit Preview Hierarchy → Ctrl+S.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Hidden"))
            {
                controller.SetEditorPreview(FlowerWorkEventController.FlowerWorkEditorPreview.Hidden);
                MarkDirty(controller);
            }

            if (GUILayout.Button("Customer"))
            {
                controller.SetEditorPreview(FlowerWorkEventController.FlowerWorkEditorPreview.CustomerLine);
                MarkDirty(controller);
            }

            if (GUILayout.Button("Choice"))
            {
                controller.SetEditorPreview(FlowerWorkEventController.FlowerWorkEditorPreview.ThinkChoice);
                MarkDirty(controller);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Correct Reply"))
            {
                controller.SetEditorPreview(FlowerWorkEventController.FlowerWorkEditorPreview.CorrectReply);
                MarkDirty(controller);
            }

            if (GUILayout.Button("Social + Note"))
            {
                controller.SetEditorPreview(FlowerWorkEventController.FlowerWorkEditorPreview.SocialStatsReward);
                MarkDirty(controller);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Save Scene Layout", GUILayout.Height(26)))
            {
                FlowerShopWorkSceneSetupEditor.SaveFlowerShopWorkSceneLayout();
                MarkDirty(controller);
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Select in Hierarchy", EditorStyles.boldLabel);
            var runtime = controller.GetComponent<VnRuntimeController>();
            if (runtime != null)
            {
                DrawSelect("Background", runtime.BackgroundImage != null ? runtime.BackgroundImage.gameObject : null);
                DrawSelect("Dialogue Panel", runtime.DialoguePanel != null ? runtime.DialoguePanel.gameObject : null);
                DrawSelect("Choice Panel", runtime.GetComponentInChildren<VnChoiceView>(true)?.gameObject);
                DrawSelect("Social Stats", controller.GetComponentInChildren<SocialStatsOverlayUI>(true)?.gameObject);
                var canvas = runtime.BackgroundImage != null ? runtime.BackgroundImage.canvas : null;
                DrawSelect("Reward Note", canvas != null ? canvas.transform.Find("FlowerRewardNote")?.gameObject : null);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawSelect(string label, GameObject go)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(120));
            using (new EditorGUI.DisabledScope(go == null))
            {
                if (GUILayout.Button(go != null ? go.name : "(missing)", EditorStyles.miniButton))
                {
                    Selection.activeGameObject = go;
                    EditorGUIUtility.PingObject(go);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void MarkDirty(FlowerWorkEventController controller)
        {
            EditorUtility.SetDirty(controller);
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }
}
#endif

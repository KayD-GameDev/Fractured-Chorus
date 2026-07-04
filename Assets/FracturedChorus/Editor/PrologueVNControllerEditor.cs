#if UNITY_EDITOR
using FracturedChorus.Narrative;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(PrologueVNController))]
    public class PrologueVNControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (Application.isPlaying)
            {
                return;
            }

            var controller = (PrologueVNController)target;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Edit Mode Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Chọn 1 phân đoạn để chỉ hiện lớp đó trong Scene/Game view — tránh UI chồng nhau khi chỉnh layout.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Disclaimer"))
            {
                controller.SetEditorPreview(PrologueVNController.PrologueEditorPreview.Disclaimer);
            }

            if (GUILayout.Button("Story"))
            {
                controller.SetEditorPreview(PrologueVNController.PrologueEditorPreview.Story);
            }

            if (GUILayout.Button("Choice"))
            {
                controller.SetEditorPreview(PrologueVNController.PrologueEditorPreview.Choice);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Contract"))
            {
                controller.SetEditorPreview(PrologueVNController.PrologueEditorPreview.Contract);
            }

            if (GUILayout.Button("Thank You"))
            {
                controller.SetEditorPreview(PrologueVNController.PrologueEditorPreview.ThankYou);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Contract Layout", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Contract preview → kéo NameInput + SignaturePad → Capture. Kéo NameInput, không phải NameValue.",
                MessageType.None);

            var layoutConfigProp = serializedObject.FindProperty("layoutConfig");
            EditorGUILayout.PropertyField(layoutConfigProp);

            var contractView = serializedObject.FindProperty("contractView").objectReferenceValue as PrologueContractView;
            var hasLayoutTools = layoutConfigProp.objectReferenceValue != null && contractView != null;

            using (new EditorGUI.DisabledScope(!hasLayoutTools))
            {
                if (GUILayout.Button("Capture Contract Layout → Config"))
                {
                    var config = layoutConfigProp.objectReferenceValue as PrologueVNLayoutConfig;
                    if (contractView != null && config != null)
                    {
                        Undo.RecordObject(config, "Capture Contract Layout");
                        Undo.RecordObject(contractView, "Capture Contract Layout");

                        if (contractView.CaptureLayoutToConfig(config))
                        {
                            contractView.ApplyLayoutConfig(config);
                            EditorUtility.SetDirty(config);
                            EditorUtility.SetDirty(contractView);
                            EditorSceneManager.MarkSceneDirty(contractView.gameObject.scene);
                            AssetDatabase.SaveAssets();
                            Debug.Log(
                                "[Fractured Chorus] Captured contract layout → " + config.name +
                                $"\n  Name: min {config.nameLineMin}, max {config.nameLineMax}" +
                                $"\n  Sign: min {config.signatureLineMin}, max {config.signatureLineMax}");
                        }
                    }
                }

                if (GUILayout.Button("Apply Config → Contract Fields"))
                {
                    var config = layoutConfigProp.objectReferenceValue as PrologueVNLayoutConfig;
                    contractView?.SetLayoutConfig(config);
                    contractView?.ApplyLayoutConfig(config);
                    controller.SetEditorPreview(PrologueVNController.PrologueEditorPreview.Contract);
                    EditorUtility.SetDirty(contractView);
                    EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
                }
            }

            if (layoutConfigProp.objectReferenceValue == null &&
                GUILayout.Button("Create PrologueVN Layout Config Asset"))
            {
                CreateLayoutConfigAsset(controller, layoutConfigProp);
            }

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(controller);
                SceneView.RepaintAll();
            }
        }

        private static void CreateLayoutConfigAsset(PrologueVNController controller, SerializedProperty layoutConfigProp)
        {
            const string folder = "Assets/FracturedChorus/Data/ScriptableObjects";
            if (!AssetDatabase.IsValidFolder("Assets/FracturedChorus/Data"))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus", "Data");
            }

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus/Data", "ScriptableObjects");
            }

            const string path = folder + "/PrologueVNLayoutConfig.asset";
            var existing = AssetDatabase.LoadAssetAtPath<PrologueVNLayoutConfig>(path);
            if (existing != null)
            {
                layoutConfigProp.objectReferenceValue = existing;
                controller.SetEditorPreview(PrologueVNController.PrologueEditorPreview.Contract);
                return;
            }

            var config = ScriptableObject.CreateInstance<PrologueVNLayoutConfig>();
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            layoutConfigProp.objectReferenceValue = config;
            controller.SetEditorPreview(PrologueVNController.PrologueEditorPreview.Contract);
            Debug.Log($"[Fractured Chorus] Created {path}");
        }
    }
}
#endif

#if UNITY_EDITOR
using FracturedChorus.Menu;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(MainMenuStartGameController))]
    public class MainMenuStartGameControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (Application.isPlaying)
            {
                return;
            }

            var controller = (MainMenuStartGameController)target;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Edit Mode Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Chọn preview để chỉ hiện 1 lớp trong Scene/Game view — tránh 2 ảnh nền chồng nhau khi chỉnh UI.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Attract"))
            {
                controller.SetEditorPreview(MainMenuStartGameController.MainMenuEditorPreview.Attract);
                EditorUtility.SetDirty(controller);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Main Menu"))
            {
                controller.SetEditorPreview(MainMenuStartGameController.MainMenuEditorPreview.MainMenu);
                EditorUtility.SetDirty(controller);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Config"))
            {
                controller.SetEditorPreview(MainMenuStartGameController.MainMenuEditorPreview.Settings);
                EditorUtility.SetDirty(controller);
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif

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
                "Select a preview layer to show only one layer in Scene/Game view. Preview does not change UI layout.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Attract"))
            {
                Undo.RecordObject(controller, "Preview Attract");
                controller.SetEditorPreview(MainMenuStartGameController.MainMenuEditorPreview.Attract);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Main Menu"))
            {
                Undo.RecordObject(controller, "Preview Main Menu");
                controller.SetEditorPreview(MainMenuStartGameController.MainMenuEditorPreview.MainMenu);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Config"))
            {
                Undo.RecordObject(controller, "Preview Config");
                controller.SetEditorPreview(MainMenuStartGameController.MainMenuEditorPreview.Settings);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Off-Beat"))
            {
                Undo.RecordObject(controller, "Preview Off-Beat Archive");
                controller.SetEditorPreview(MainMenuStartGameController.MainMenuEditorPreview.OffBeatArchive);
                EditorUtility.SetDirty(controller);
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(
                "Off-Beat: chỉnh ArchivePanel / CatalogScroll / PlayerRoot trên MainMenuCanvas.",
                MessageType.None);
        }
    }
}
#endif

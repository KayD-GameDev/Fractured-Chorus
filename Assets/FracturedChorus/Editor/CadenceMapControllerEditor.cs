#if UNITY_EDITOR
using FracturedChorus.RunMap;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(CadenceMapController))]
    public sealed class CadenceMapControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (Application.isPlaying)
            {
                return;
            }

            var controller = (CadenceMapController)target;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Edit Mode Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Chọn một lớp để chỉ hiện đúng màn hình khi chỉnh UI.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Map Select"))
            {
                Undo.RecordObject(controller, "Preview Map Select");
                controller.SetEditorPreview(CadenceMapController.RunMapEditorPreview.MapSelect);
                MarkDirty(controller);
            }

            if (GUILayout.Button("Map Nodes"))
            {
                Undo.RecordObject(controller, "Preview Map Nodes");
                controller.SetEditorPreview(CadenceMapController.RunMapEditorPreview.MapNodes);
                MarkDirty(controller);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Treasure"))
            {
                Undo.RecordObject(controller, "Preview Treasure");
                controller.SetEditorPreview(CadenceMapController.RunMapEditorPreview.Treasure);
                MarkDirty(controller);
            }

            if (GUILayout.Button("Event"))
            {
                Undo.RecordObject(controller, "Preview Event");
                controller.SetEditorPreview(CadenceMapController.RunMapEditorPreview.Event);
                MarkDirty(controller);
            }

            if (GUILayout.Button("Camp"))
            {
                Undo.RecordObject(controller, "Preview Camp");
                controller.SetEditorPreview(CadenceMapController.RunMapEditorPreview.Camp);
                MarkDirty(controller);
            }

            if (GUILayout.Button("Shop"))
            {
                Undo.RecordObject(controller, "Preview Shop");
                controller.SetEditorPreview(CadenceMapController.RunMapEditorPreview.Shop);
                MarkDirty(controller);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(
                "Map Select → MacroMapLayer\nMap Nodes → InnerMapLayer + layout preview\nTreasure / Event → overlay + video BG\nCamp / Shop → overlay + still BG",
                MessageType.None);

            if (GUILayout.Button("Show Map Nodes Preview"))
            {
                Undo.RecordObject(controller, "Show Map Nodes Preview");
                controller.SetEditorPreview(CadenceMapController.RunMapEditorPreview.MapNodes);
                MarkDirty(controller);
            }

            if (GUILayout.Button("Wire Scene Edit Chrome"))
            {
                MapNodeIconSetupEditor.WireSceneEditChrome();
            }
        }

        private static void MarkDirty(CadenceMapController controller)
        {
            EditorUtility.SetDirty(controller);
            SceneView.RepaintAll();
        }
    }
}
#endif

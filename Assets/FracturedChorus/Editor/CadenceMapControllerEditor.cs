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
                "Chọn một lớp để chỉ hiện đúng màn hình khi chỉnh UI — Map Select (macro vault) hoặc Map Nodes (inner path).",
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
            EditorGUILayout.HelpBox(
                "Map Select → MacroMapLayer (vault / map select)\nMap Nodes → InnerMapLayer + NodeEditPreview strip (mọi loại icon) + NodeInfoSidebar",
                MessageType.None);

            if (GUILayout.Button("Rebuild Node Preview Strip"))
            {
                Undo.RecordObject(controller, "Rebuild Node Preview");
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

#if UNITY_EDITOR
using FracturedChorus.Hub;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(CampusHubController))]
    public class CampusHubControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (Application.isPlaying)
            {
                return;
            }

            var controller = (CampusHubController)target;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Edit Mode Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Chọn một layer để chỉ hiện đúng màn hình khi chỉnh UI — tránh các lớp chồng lên nhau.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Morning"))
            {
                controller.SetEditorPreview(CampusHubController.CampusHubEditorPreview.Morning);
                MarkDirty(controller);
            }

            if (GUILayout.Button("Town Day"))
            {
                controller.SetEditorPreview(CampusHubController.CampusHubEditorPreview.TownDay);
                MarkDirty(controller);
            }

            if (GUILayout.Button("Town Night"))
            {
                controller.SetEditorPreview(CampusHubController.CampusHubEditorPreview.TownNight);
                MarkDirty(controller);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("District"))
            {
                controller.SetEditorPreview(CampusHubController.CampusHubEditorPreview.District);
                MarkDirty(controller);
            }

            if (GUILayout.Button("Status Menu"))
            {
                controller.SetEditorPreview(CampusHubController.CampusHubEditorPreview.StatusMenu);
                MarkDirty(controller);
            }

            if (GUILayout.Button("Calendar"))
            {
                controller.SetEditorPreview(CampusHubController.CampusHubEditorPreview.Calendar);
                MarkDirty(controller);
            }

            EditorGUILayout.EndHorizontal();

            var townMap = controller.GetComponentInChildren<TownMapView>(true);
            if (townMap != null && townMap.transform.Find("StatusMenu") == null)
            {
                EditorGUILayout.HelpBox(
                    "Scene chưa có StatusMenu/Calendar. Bấm Status Menu hoặc Calendar để build preview, hoặc Fractured Chorus → Wire Town Map Status Menu rồi Save.",
                    MessageType.Warning);
            }
        }

        private static void MarkDirty(CampusHubController controller)
        {
            EditorUtility.SetDirty(controller);
            SceneView.RepaintAll();
        }
    }
}
#endif

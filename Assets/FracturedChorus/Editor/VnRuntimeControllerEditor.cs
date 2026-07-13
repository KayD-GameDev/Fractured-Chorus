#if UNITY_EDITOR
using FracturedChorus.Narrative.Vn;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(VnRuntimeController))]
    public sealed class VnRuntimeControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            var runtime = (VnRuntimeController)target;
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("VN Layout Tools", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Edit tay trên Scene (không Play):\n" +
                "1) Bấm Preview Dialogue / TextCard.\n" +
                "2) Hierarchy chọn Nameplate, DialoguePanel, DialogueBody, TextCardBody, StoryDateHud, portrait slots.\n" +
                "3) Kéo Anchored Position / Width / Height trên RectTransform.\n" +
                "4) Ctrl+S lưu scene.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview Dialogue + Date HUD"))
            {
                Undo.RecordObject(runtime, "Preview VN Dialogue");
                runtime.EditorPreviewDialogueSample();
                EditorUtility.SetDirty(runtime);
                MarkSceneDirty();
            }

            if (GUILayout.Button("Preview TextCard"))
            {
                Undo.RecordObject(runtime, "Preview VN TextCard");
                runtime.EditorPreviewTextCardSample();
                EditorUtility.SetDirty(runtime);
                MarkSceneDirty();
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Hide Samples"))
            {
                Undo.RecordObject(runtime, "Hide VN Samples");
                runtime.EditorHideSamples();
                EditorUtility.SetDirty(runtime);
                MarkSceneDirty();
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Select in Hierarchy", EditorStyles.boldLabel);
            DrawSelectRow("Nameplate", runtime.NameplateText != null ? runtime.NameplateText.gameObject : null);
            DrawSelectRow("Dialogue Panel", runtime.DialoguePanel != null ? runtime.DialoguePanel.gameObject : null);
            DrawSelectRow(
                "Dialogue Body",
                FindChildNamed(runtime.DialoguePanel != null ? runtime.DialoguePanel.transform : null, "DialogueBody"));
            DrawSelectRow("Text Card Panel", runtime.TextCardPanel != null ? runtime.TextCardPanel.gameObject : null);
            DrawSelectRow("Text Card Body", runtime.TextCardBody != null ? runtime.TextCardBody.gameObject : null);
            DrawSelectRow("Story Date HUD", runtime.DateHud != null ? runtime.DateHud.gameObject : null);
            DrawSelectRow("Background", runtime.BackgroundImage != null ? runtime.BackgroundImage.gameObject : null);

            var portraits = runtime.PortraitView;
            if (portraits != null)
            {
                DrawSelectRow("Portrait Left", portraits.LeftRoot != null ? portraits.LeftRoot.gameObject : null);
                DrawSelectRow("Portrait Right", portraits.RightRoot != null ? portraits.RightRoot.gameObject : null);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawSelectRow(string label, GameObject go)
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

        private static GameObject FindChildNamed(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            var child = parent.Find(childName);
            return child != null ? child.gameObject : null;
        }

        private static void MarkSceneDirty()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }
}
#endif

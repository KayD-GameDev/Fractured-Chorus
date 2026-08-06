#if UNITY_EDITOR
using FracturedChorus.UI;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(BossNoteAuthoring))]
    public sealed class BossNoteAuthoringEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Chọn NoteSingle_* trên Hierarchy — chỉnh Remaining Hits rồi Ctrl+S.\n" +
                "Play: note authored ẩn; runtime spawn từ telegraph (hoặc fallback Remaining Hits nếu beat không có telegraph).",
                MessageType.Info);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("beatIndex"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("remainingHits"),
                new GUIContent("Remaining Hits (đòn đánh quái)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayTier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("numberLabel"));

            serializedObject.ApplyModifiedProperties();

            var authoring = (BossNoteAuthoring)target;
            if (GUILayout.Button("Refresh Number Label"))
            {
                Undo.RecordObject(authoring, "Refresh Note Number");
                authoring.RefreshNumberLabel();
                EditorUtility.SetDirty(authoring);
            }
        }
    }
}
#endif

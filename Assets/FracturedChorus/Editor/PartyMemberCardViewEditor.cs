#if UNITY_EDITOR
using FracturedChorus.Data;
using FracturedChorus.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(PartyMemberCardView))]
    public sealed class PartyMemberCardViewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var view = (PartyMemberCardView)target;
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "previewCharacterIndex");
            serializedObject.ApplyModifiedProperties();

            CombatUiHierarchy.AssignPartyCardPreviewPresets(view);
            serializedObject.Update();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Đổi nhân vật (CardTemplate)", EditorStyles.boldLabel);
            var enemySide = view.GetComponentInParent<EnemyStatusBarUIView>(true) != null;
            EditorGUILayout.HelpBox(
                enemySide
                    ? "Chọn enemy để đổi Avatar + tên (Astra / Kiki / Micro / Eye). Lúc Play, clone lấy art từ unit trên sân."
                    : "Chọn nhân vật để đổi Avatar + tên trên CardTemplate. Lúc Play, thẻ clone vẫn lấy art từ unit trên sân.",
                MessageType.Info);

            var presetsProp = serializedObject.FindProperty("characterCardPresets");
            var indexProp = serializedObject.FindProperty("previewCharacterIndex");
            if (presetsProp == null || indexProp == null)
            {
                return;
            }

            var labels = BuildPresetLabels(presetsProp);
            if (labels.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    enemySide
                        ? "Gán Character Card Presets (Astra / Kiki / Micro / Eye)."
                        : "Gán Character Card Presets (Ren / Coda / Charlotte).",
                    MessageType.Warning);
                return;
            }

            var current = Mathf.Clamp(indexProp.intValue, 0, labels.Length - 1);
            EditorGUI.BeginChangeCheck();
            var next = EditorGUILayout.Popup("Nhân vật", current, labels);
            EditorGUILayout.BeginHorizontal();
            for (var i = 0; i < labels.Length; i++)
            {
                var selected = i == current;
                var prev = GUI.backgroundColor;
                if (selected)
                {
                    GUI.backgroundColor = new Color(0.45f, 0.85f, 1f, 1f);
                }

                if (GUILayout.Button(labels[i]))
                {
                    next = i;
                }

                GUI.backgroundColor = prev;
            }

            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck() || next != current)
            {
                Undo.RecordObject(view, "Preview party card character");
                foreach (var image in view.GetComponentsInChildren<Image>(true))
                {
                    Undo.RecordObject(image, "Preview party card character");
                }

                foreach (var text in view.GetComponentsInChildren<Text>(true))
                {
                    Undo.RecordObject(text, "Preview party card character");
                }

                indexProp.intValue = next;
                serializedObject.ApplyModifiedProperties();
                view.SetPreviewCharacterIndex(next);
                EditorUtility.SetDirty(view);
                SceneView.RepaintAll();
            }
        }

        private static string[] BuildPresetLabels(SerializedProperty presetsProp)
        {
            var labels = new string[presetsProp.arraySize];
            for (var i = 0; i < presetsProp.arraySize; i++)
            {
                var preset = presetsProp.GetArrayElementAtIndex(i).objectReferenceValue as UnitPresetSO;
                if (preset != null && !string.IsNullOrWhiteSpace(preset.displayName))
                {
                    labels[i] = preset.displayName;
                }
                else if (preset != null)
                {
                    labels[i] = preset.name;
                }
                else
                {
                    labels[i] = $"Slot {i + 1}";
                }
            }

            return labels;
        }
    }
}
#endif

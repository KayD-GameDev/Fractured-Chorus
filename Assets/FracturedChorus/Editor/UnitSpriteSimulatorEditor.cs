#if UNITY_EDITOR
using FracturedChorus.UI;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(UnitSpriteSimulator))]
    public sealed class UnitSpriteSimulatorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var layoutsProp = serializedObject.FindProperty("spriteLayouts");
            EditorGUILayout.PropertyField(layoutsProp, new GUIContent("Saved Sprites"), true);
            serializedObject.ApplyModifiedProperties();

            var sim = (UnitSpriteSimulator)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Unit Sprite Simulator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Hierarchy:\n" +
                "  Unit (SpriteRenderer)  ← đổi sprite / scale trên root\n" +
                "  └ FeetAnchor           ← kéo để neo chân xuống honeycomb\n\n" +
                "Thêm/bớt sprite bằng nút bên dưới. Đặt tên từng slot — tab hiện tên đó thay vì V0/V1.\n" +
                "Mỗi slot lưu Tên + Sprite + Scale + FeetAnchor.\n" +
                "Play: chọn tab để xem ngay trên sân (tạm dừng Animator). Resume Animator khi xong.",
                MessageType.Info);

            DrawSpriteCountControls(sim);
            DrawSpriteButtons(sim);
            DrawCurrentSpriteName(sim);
            DrawCurrentSpriteField(sim);
            DrawScaleField(sim);

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Ensure FeetAnchor"))
            {
                Undo.RecordObject(sim, "Ensure Unit FeetAnchor");
                sim.EnsureHandles();
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Save Layout For This Sprite"))
            {
                Undo.RecordObject(sim, "Save Unit Sprite Layout");
                sim.SaveCurrentLayout();
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Snap FeetAnchor → Sprite Bottom"))
            {
                Undo.RecordObject(sim, "Snap Unit FeetAnchor");
                sim.SnapFeetToSpriteBottom();
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Snap Unit → Honeycomb Cell"))
            {
                Undo.RecordObject(sim, "Snap Unit To Cell");
                sim.SnapUnitToHoneycomb();
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }

            if (Application.isPlaying)
            {
                if (GUILayout.Button("Resume Animator (unlock preview)"))
                {
                    Undo.RecordObject(sim, "Resume Unit Animator");
                    sim.ClearPreviewLock();
                    EditorUtility.SetDirty(sim);
                }
            }

            var px = sim.SpritePixelSize;
            var scale = sim.CurrentScale;
            var feet = sim.FeetAnchorLocal;
            EditorGUILayout.HelpBox(
                $"Editing {sim.SpriteTabLabel(sim.SpritePreview)}  ({sim.SpritePreview + 1}/{sim.SpriteCount})  |  Sprite {px.x:0.##}×{px.y:0.##}\n" +
                $"Scale ({scale.x:0.###}, {scale.y:0.###}, {scale.z:0.###})\n" +
                $"FeetAnchor ({feet.x:0.##}, {feet.y:0.##}, {feet.z:0.##})",
                MessageType.None);
        }

        private static void DrawSpriteCountControls(UnitSpriteSimulator sim)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Sprites: {sim.SpriteCount}", GUILayout.Width(90f));
            using (new EditorGUI.DisabledScope(sim.SpriteCount >= UnitSpriteSimulator.MaxSpriteCount))
            {
                if (GUILayout.Button("+ Add Sprite"))
                {
                    Undo.RecordObject(sim, "Add Unit Sprite");
                    sim.AddSprite();
                    EditorUtility.SetDirty(sim);
                    SceneView.RepaintAll();
                }
            }

            using (new EditorGUI.DisabledScope(sim.SpriteCount <= UnitSpriteSimulator.MinSpriteCount))
            {
                if (GUILayout.Button("− Remove Sprite"))
                {
                    Undo.RecordObject(sim, "Remove Unit Sprite");
                    sim.RemoveSprite();
                    EditorUtility.SetDirty(sim);
                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawSpriteButtons(UnitSpriteSimulator sim)
        {
            var count = sim.SpriteCount;
            EditorGUILayout.BeginHorizontal();
            for (var i = 0; i < count; i++)
            {
                if (i > 0 && i % 4 == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }

                DrawSpriteButton(sim, sim.SpriteTabLabel(i), i);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCurrentSpriteName(UnitSpriteSimulator sim)
        {
            serializedObject.Update();
            var layouts = serializedObject.FindProperty("spriteLayouts");
            var index = sim.SpritePreview;
            if (layouts == null || index < 0 || index >= layouts.arraySize)
            {
                return;
            }

            var nameProp = layouts.GetArrayElementAtIndex(index).FindPropertyRelative("displayName");
            if (nameProp == null)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(nameProp, new GUIContent("Tên sprite"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(sim);
            }
            else
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawCurrentSpriteField(UnitSpriteSimulator sim)
        {
            serializedObject.Update();
            var layouts = serializedObject.FindProperty("spriteLayouts");
            var index = sim.SpritePreview;
            if (layouts == null || index < 0 || index >= layouts.arraySize)
            {
                return;
            }

            var spriteProp = layouts.GetArrayElementAtIndex(index).FindPropertyRelative("sprite");
            if (spriteProp == null)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(spriteProp, new GUIContent("Sprite"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                sim.ApplyPreviewSprite(spriteProp.objectReferenceValue as Sprite);
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }
            else
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private static void DrawScaleField(UnitSpriteSimulator sim)
        {
            EditorGUI.BeginChangeCheck();
            var uniform = sim.CurrentScale.x;
            var next = EditorGUILayout.FloatField("Scale (uniform)", uniform);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sim.transform, "Set Unit Sprite Scale");
                Undo.RecordObject(sim, "Set Unit Sprite Scale");
                sim.SetUniformScale(next);
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }
        }

        private static void DrawSpriteButton(UnitSpriteSimulator sim, string label, int preview)
        {
            var selected = sim.SpritePreview == preview;
            var prev = GUI.backgroundColor;
            if (selected)
            {
                GUI.backgroundColor = new Color(0.45f, 0.85f, 1f, 1f);
            }

            if (GUILayout.Button(new GUIContent(label, label)))
            {
                Undo.RecordObject(sim, $"Edit Sprite {label}");
                sim.SetSpritePreview(preview);
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = prev;
        }
    }
}
#endif

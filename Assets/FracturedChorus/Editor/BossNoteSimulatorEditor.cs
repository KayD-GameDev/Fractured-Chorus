#if UNITY_EDITOR
using FracturedChorus.UI;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(BossNoteSimulator))]
    public sealed class BossNoteSimulatorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("timeline"));

            var layoutsProp = serializedObject.FindProperty("shapeLayouts");
            EditorGUILayout.PropertyField(layoutsProp, new GUIContent("Saved Layouts"), true);
            serializedObject.ApplyModifiedProperties();

            var sim = (BossNoteSimulator)target;

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Play: mỗi note dùng layout Knob/RailAnchor/NoteNum + sprite đã lưu; note đôi = 2 note đơn.",
                    MessageType.None);
                return;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Note Simulator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Hierarchy:\n" +
                "  NoteSimulator\n" +
                "  └ Knob          ← kéo để đặt vùng bụng / không gian số\n" +
                "     ├ RailAnchor ← căn giữa Knob (pin lên line quái)\n" +
                "     └ NoteNum    ← căn giữa Knob (số hits)\n\n" +
                "Thêm/bớt shape bằng nút bên dưới. Mỗi shape lưu Knob + RailAnchor + NoteNum + Sprite.",
                MessageType.Info);

            DrawShapeCountControls(sim);
            DrawShapeButtons(sim);
            DrawCurrentShapeSprite(sim);

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Rebuild Hierarchy (Knob → RailAnchor + NoteNum)"))
            {
                Undo.RecordObject(sim, "Rebuild NoteSimulator Hierarchy");
                sim.EnsureKnobHierarchy();
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Save Layout For This Shape"))
            {
                Undo.RecordObject(sim, "Save NoteSimulator Shape Layout");
                sim.SaveCurrentShapeLayout();
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Snap RailAnchor → Boss Line"))
            {
                Undo.RecordObject(sim, "Snap NoteSimulator To Boss Line");
                sim.SnapRailAnchorToBossLine();
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }

            var size = sim.NoteSize;
            var knob = sim.KnobLocal;
            var pin = sim.PinInNoteSpace;
            EditorGUILayout.HelpBox(
                $"Editing V{sim.ShapePreview} / {sim.ShapeCount}  |  Note size {size.x:0.##}×{size.y:0.##}\n" +
                $"Knob ({knob.x:0.##}, {knob.y:0.##}) size {sim.KnobSize.x:0.##}×{sim.KnobSize.y:0.##}\n" +
                $"RailAnchor@Knob ({sim.RailAnchorLocal.x:0.##}, {sim.RailAnchorLocal.y:0.##})  " +
                $"NoteNum@Knob ({sim.NoteNumLocal.x:0.##}, {sim.NoteNumLocal.y:0.##})\n" +
                $"Pin in note space ({pin.x:0.##}, {pin.y:0.##})",
                MessageType.None);
        }

        private static void DrawShapeCountControls(BossNoteSimulator sim)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Shapes: {sim.ShapeCount}", GUILayout.Width(90f));
            using (new EditorGUI.DisabledScope(sim.ShapeCount >= BossNoteSimulator.MaxShapeCount))
            {
                if (GUILayout.Button("+ Add Shape"))
                {
                    Undo.RecordObject(sim, "Add NoteSimulator Shape");
                    sim.AddShape();
                    EditorUtility.SetDirty(sim);
                    SceneView.RepaintAll();
                }
            }

            using (new EditorGUI.DisabledScope(sim.ShapeCount <= BossNoteSimulator.MinShapeCount))
            {
                if (GUILayout.Button("− Remove Shape"))
                {
                    Undo.RecordObject(sim, "Remove NoteSimulator Shape");
                    sim.RemoveShape();
                    EditorUtility.SetDirty(sim);
                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawShapeButtons(BossNoteSimulator sim)
        {
            var count = sim.ShapeCount;
            EditorGUILayout.BeginHorizontal();
            for (var i = 0; i < count; i++)
            {
                if (i > 0 && i % 8 == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }

                DrawShapeButton(sim, "V" + i, i);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCurrentShapeSprite(BossNoteSimulator sim)
        {
            serializedObject.Update();
            var layouts = serializedObject.FindProperty("shapeLayouts");
            var index = sim.ShapePreview;
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
            EditorGUILayout.PropertyField(spriteProp, new GUIContent($"Sprite V{index}"));
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

        private static void DrawShapeButton(BossNoteSimulator sim, string label, int preview)
        {
            var selected = sim.ShapePreview == preview;
            var prev = GUI.backgroundColor;
            if (selected)
            {
                GUI.backgroundColor = new Color(0.45f, 0.85f, 1f, 1f);
            }

            if (GUILayout.Button(label))
            {
                Undo.RecordObject(sim, $"Edit Shape {label}");
                sim.SetShapePreview(preview);
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = prev;
        }
    }
}
#endif

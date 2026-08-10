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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("shapePreview"),
                new GUIContent("Editing Shape"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("shapeLayouts"),
                new GUIContent("Saved Layouts (v0–v4)"), true);
            serializedObject.ApplyModifiedProperties();

            var sim = (BossNoteSimulator)target;

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Play: mỗi note dùng layout Knob/RailAnchor/NoteNum đã lưu; note đôi = 2 note đơn.",
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
                "Mỗi shape V0–V4 lưu riêng Knob + RailAnchor + NoteNum.\n" +
                "Note đôi khi Play = 2 note đơn (1 beat / 1 note).",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            DrawShapeButton(sim, "V0", BossNoteSimulator.NoteShapePreview.V0);
            DrawShapeButton(sim, "V1", BossNoteSimulator.NoteShapePreview.V1);
            DrawShapeButton(sim, "V2", BossNoteSimulator.NoteShapePreview.V2);
            DrawShapeButton(sim, "V3", BossNoteSimulator.NoteShapePreview.V3);
            DrawShapeButton(sim, "V4", BossNoteSimulator.NoteShapePreview.V4);
            EditorGUILayout.EndHorizontal();

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
                $"Editing {sim.ShapePreview}  |  Note size {size.x:0.##}×{size.y:0.##}\n" +
                $"Knob ({knob.x:0.##}, {knob.y:0.##}) size {sim.KnobSize.x:0.##}×{sim.KnobSize.y:0.##}\n" +
                $"RailAnchor@Knob ({sim.RailAnchorLocal.x:0.##}, {sim.RailAnchorLocal.y:0.##})  " +
                $"NoteNum@Knob ({sim.NoteNumLocal.x:0.##}, {sim.NoteNumLocal.y:0.##})\n" +
                $"Pin in note space ({pin.x:0.##}, {pin.y:0.##})",
                MessageType.None);
        }

        private static void DrawShapeButton(
            BossNoteSimulator sim,
            string label,
            BossNoteSimulator.NoteShapePreview preview)
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

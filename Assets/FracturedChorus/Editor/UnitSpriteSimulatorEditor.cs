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
                "Mỗi slot = 1 state UnitView (Idle, Moving, Skill, Guard=Counter, Hurt, Death, NormalHit, SkillHit, UltHit).\n" +
                "Idle: gán cả Animation Clip (Play / hết phase) và Sprite tĩnh (đi vào ô chiến đấu).\n" +
                "Party hit clips (Normal/Skill/Ult) chạy Animator. Guard dùng sprite Counter.\n" +
                "Moving tĩnh dùng khi unit bước vào/ra ô đánh. Combat lấy scale + chân + BoxCollider2D từ slot.",
                MessageType.Info);

            DrawSpriteCountControls(sim);
            DrawSpriteButtons(sim);
            DrawCurrentSlotFields(sim);
            DrawAnchorFields(sim);
            DrawColliderFields(sim);
            DrawScaleField(sim);
            DrawDuplicateStateWarning(sim);

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
            var body = sim.GetComponent<BoxCollider2D>();
            var colText = body != null
                ? $"Collider size ({body.size.x:0.###}, {body.size.y:0.###})  offset ({body.offset.x:0.###}, {body.offset.y:0.###})"
                : "Collider: none";
            EditorGUILayout.HelpBox(
                $"Editing {sim.SpriteTabLabel(sim.SpritePreview)}  ({sim.SpritePreview + 1}/{sim.SpriteCount})  |  Sprite {px.x:0.##}×{px.y:0.##}\n" +
                $"Scale ({scale.x:0.###}, {scale.y:0.###}, {scale.z:0.###})\n" +
                $"FeetAnchor ({feet.x:0.##}, {feet.y:0.##}, {feet.z:0.##})\n" +
                colText,
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

        private void DrawCurrentSlotFields(UnitSpriteSimulator sim)
        {
            serializedObject.Update();
            var layouts = serializedObject.FindProperty("spriteLayouts");
            var index = sim.SpritePreview;
            if (layouts == null || index < 0 || index >= layouts.arraySize)
            {
                return;
            }

            var slot = layouts.GetArrayElementAtIndex(index);
            DrawSlotProperty(sim, slot, "displayName", "Tên sprite");
            DrawSlotProperty(sim, slot, "linkedState", "Linked State");

            var linkedProp = slot.FindPropertyRelative("linkedState");
            var isIdle = linkedProp != null && linkedProp.enumValueIndex == (int)UnitCombatVisualState.Idle;
            if (isIdle)
            {
                DrawIdleBothFields(sim, slot);
                return;
            }

            DrawSlotProperty(sim, slot, "kind", "Kind");

            var kindProp = slot.FindPropertyRelative("kind");
            var isClip = kindProp != null && kindProp.enumValueIndex == (int)UnitSpriteKind.AnimationClip;
            if (isClip)
            {
                EditorGUI.BeginChangeCheck();
                var clipProp = slot.FindPropertyRelative("animationClip");
                EditorGUILayout.PropertyField(clipProp, new GUIContent("Animation Clip"));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    sim.ApplyPreviewClip(clipProp.objectReferenceValue as AnimationClip);
                    EditorUtility.SetDirty(sim);
                    SceneView.RepaintAll();
                }
                else
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                var spriteProp = slot.FindPropertyRelative("sprite");
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
        }

        private void DrawIdleBothFields(UnitSpriteSimulator sim, SerializedProperty slot)
        {
            EditorGUILayout.HelpBox(
                "Idle clip: chạy khi bấm Play và khi hết phase.\n" +
                "Idle tĩnh: ngưng clip khi unit bước vào ô đánh, rồi mới sang Moving tĩnh.",
                MessageType.None);

            EditorGUI.BeginChangeCheck();
            var clipProp = slot.FindPropertyRelative("animationClip");
            EditorGUILayout.PropertyField(clipProp, new GUIContent("Idle Animation Clip"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                sim.ApplyPreviewClip(clipProp.objectReferenceValue as AnimationClip);
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }
            else
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.BeginChangeCheck();
            var spriteProp = slot.FindPropertyRelative("sprite");
            EditorGUILayout.PropertyField(spriteProp, new GUIContent("Idle Still Sprite"));
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

        private void DrawSlotProperty(UnitSpriteSimulator sim, SerializedProperty slot, string field, string label)
        {
            var prop = slot.FindPropertyRelative(field);
            if (prop == null)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(prop, new GUIContent(label));
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

        private void DrawAnchorFields(UnitSpriteSimulator sim)
        {
            serializedObject.Update();
            var layouts = serializedObject.FindProperty("spriteLayouts");
            var index = sim.SpritePreview;
            if (layouts == null || index < 0 || index >= layouts.arraySize)
            {
                return;
            }

            var slot = layouts.GetArrayElementAtIndex(index);
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Feet Anchor (slot này)", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var feetProp = slot.FindPropertyRelative("feetAnchorLocal");
            if (feetProp != null)
            {
                EditorGUILayout.PropertyField(feetProp, new GUIContent("Anchor Local"));
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                sim.ApplyPreviewFeet();
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }
            else
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawColliderFields(UnitSpriteSimulator sim)
        {
            serializedObject.Update();
            var layouts = serializedObject.FindProperty("spriteLayouts");
            var index = sim.SpritePreview;
            if (layouts == null || index < 0 || index >= layouts.arraySize)
            {
                return;
            }

            var slot = layouts.GetArrayElementAtIndex(index);
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Box Collider 2D (slot này)", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var sizeProp = slot.FindPropertyRelative("colliderSize");
            var offsetProp = slot.FindPropertyRelative("colliderOffset");
            if (sizeProp != null)
            {
                EditorGUILayout.PropertyField(sizeProp, new GUIContent("Collider Size"));
            }

            if (offsetProp != null)
            {
                EditorGUILayout.PropertyField(offsetProp, new GUIContent("Collider Offset"));
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                sim.ApplyPreviewCollider();
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }
            else
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private static void DrawDuplicateStateWarning(UnitSpriteSimulator sim)
        {
            var layouts = sim.SpriteLayouts;
            if (layouts == null)
            {
                return;
            }

            foreach (UnitCombatVisualState state in System.Enum.GetValues(typeof(UnitCombatVisualState)))
            {
                if (state == UnitCombatVisualState.None || !sim.HasDuplicateLinkedState(state))
                {
                    continue;
                }

                EditorGUILayout.HelpBox(
                    $"Nhiều slot đang gắn {state}. Combat dùng slot đầu tiên.",
                    MessageType.Warning);
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

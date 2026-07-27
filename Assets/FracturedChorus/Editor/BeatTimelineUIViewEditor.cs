#if UNITY_EDITOR
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    [InitializeOnLoad]
    internal static class BossNoteLayoutPlayModePersist
    {
        private static string _json;
        private static string _globalId;

        static BossNoteLayoutPlayModePersist()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static void Stash(BeatTimelineUIView view)
        {
            if (view == null || view.BossNoteNumberLayout == null)
            {
                return;
            }

            _json = JsonUtility.ToJson(view.BossNoteNumberLayout);
            _globalId = GlobalObjectId.GetGlobalObjectIdSlow(view).ToString();
        }

        public static bool HasStash => !string.IsNullOrEmpty(_json);

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode || string.IsNullOrEmpty(_json))
            {
                return;
            }

            if (!GlobalObjectId.TryParse(_globalId, out var id))
            {
                return;
            }

            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as BeatTimelineUIView;
            if (obj == null || obj.BossNoteNumberLayout == null)
            {
                return;
            }

            Undo.RecordObject(obj, "Restore Boss Note Number Layout");
            JsonUtility.FromJsonOverwrite(_json, obj.BossNoteNumberLayout);
            EditorUtility.SetDirty(obj);
            if (obj.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(obj.gameObject.scene);
            }

            Debug.Log("[BossNoteLayout] Đã giữ layout sau Exit Play. Ctrl+S để lưu scene.");
        }
    }

    [CustomEditor(typeof(BeatTimelineUIView))]
    public sealed class BeatTimelineUIViewEditor : UnityEditor.Editor
    {
        private static bool _dragging;
        private static bool _foldLayoutHelp = true;

        private static bool _foldLeftRail = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(6f);
            _foldLeftRail = EditorGUILayout.Foldout(_foldLeftRail, "Left Rail — chỉnh trên Scene", true);
            if (_foldLeftRail)
            {
                EditorGUILayout.HelpBox(
                    "Edit Mode — Hierarchy:\n" +
                    "• Header/PhaseLabel (+ PhaseArt) → chữ PHASE\n" +
                    "• Header/Budget (+ BudgetText) → khung 0/10\n" +
                    "• Header/Clef/ClefIcon → khóa sol\n" +
                    "• Header/LeftRailBackground → nền cột\n" +
                    "Kéo Rect trên Scene · Preserve Scene Rects = on · Ctrl+S",
                    MessageType.Info);

                var view = (BeatTimelineUIView)target;
                if (GUILayout.Button("Ensure LeftRail hierarchy (Scene)"))
                {
                    serializedObject.ApplyModifiedProperties();
                    Undo.RecordObject(view, "Ensure LeftRail");
                    view.ApplyLeftRailPublic();
                    AssignLeftRailRefs(view);
                    EditorUtility.SetDirty(view);
                    if (view.gameObject.scene.IsValid())
                    {
                        EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
                    }

                    serializedObject.Update();
                    Debug.Log("[LeftRail] Hierarchy sẵn sàng — kéo Clef / Background trên Scene rồi Ctrl+S.");
                }

                if (GUILayout.Button("Apply LeftRail (sprites / alpha / layout)"))
                {
                    serializedObject.ApplyModifiedProperties();
                    Undo.RecordObject(view, "Apply LeftRail");
                    view.ApplyLeftRailPublic();
                    EditorUtility.SetDirty(view);
                    serializedObject.Update();
                }

                if (GUILayout.Button("Bake Clef Rect → LeftRailLayout"))
                {
                    serializedObject.ApplyModifiedProperties();
                    BakeClefToLayout(view);
                    serializedObject.Update();
                }
            }

            EditorGUILayout.Space(6f);
            _foldLayoutHelp = EditorGUILayout.Foldout(_foldLayoutHelp, "Boss Note — kéo tay trên Scene", true);
            if (!_foldLayoutHelp)
            {
                return;
            }

            EditorGUILayout.HelpBox(
                "1) Play Mode → chọn BeatTimelineUIView → Scene: kéo chấm vàng\n" +
                "2) Thả chuột = ghi nudge (chưa cần Bake)\n" +
                "3) Bấm Save layout (giữ sau Exit Play)\n" +
                "4) Stop Play → Ctrl+S lưu scene\n\n" +
                "Bake selected = chỉ khi kéo NoteNum_* bằng Rect Tool.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Save layout (giữ sau Exit Play)"))
                {
                    var view = (BeatTimelineUIView)target;
                    serializedObject.ApplyModifiedProperties();
                    BossNoteLayoutPlayModePersist.Stash(view);
                    EditorUtility.SetDirty(view);
                    Debug.Log("[BossNoteLayout] Đã stash. Stop Play rồi Ctrl+S.");
                }

                if (GUILayout.Button("Rebuild boss notes"))
                {
                    var view = (BeatTimelineUIView)target;
                    view.RebuildBossNoteClustersPublic();
                }

                if (GUILayout.Button("Bake selected NoteNum → Layout"))
                {
                    BakeSelected((BeatTimelineUIView)target);
                }
            }
        }

        private static void AssignLeftRailRefs(BeatTimelineUIView view)
        {
            var so = new SerializedObject(view);
            var header = view.transform.Find("Header");
            if (header == null)
            {
                return;
            }

            var bg = header.Find("LeftRailBackground")?.GetComponent<Image>();
            var clef = header.Find("Clef") as RectTransform;
            var clefIcon = clef != null ? clef.Find("ClefIcon")?.GetComponent<Image>() : null;
            var phaseArt = header.Find("PhaseLabel/PhaseArt")?.GetComponent<Image>();
            var budgetImg = header.Find("Budget")?.GetComponent<Image>();

            so.FindProperty("leftRailBackgroundImage").objectReferenceValue = bg;
            so.FindProperty("leftRailClefRoot").objectReferenceValue = clef;
            so.FindProperty("trebleClefImage").objectReferenceValue = clefIcon;
            so.FindProperty("phaseLabelImage").objectReferenceValue = phaseArt;
            so.FindProperty("avBudgetFrameImage").objectReferenceValue = budgetImg;
            so.ApplyModifiedProperties();
        }

        private static void BakeClefToLayout(BeatTimelineUIView view)
        {
            var clef = view.transform.Find("Header/Clef") as RectTransform;
            if (clef == null)
            {
                EditorUtility.DisplayDialog("Left Rail", "Không thấy Header/Clef trên Hierarchy.", "OK");
                return;
            }

            Undo.RecordObject(view, "Bake Clef → LeftRailLayout");
            var layout = view.LeftRailLayout;
            if (layout == null)
            {
                return;
            }

            layout.clefSize = clef.sizeDelta;
            layout.clefAnchoredPosition = clef.anchoredPosition;
            layout.preserveSceneRects = true;
            EditorUtility.SetDirty(view);
            if (view.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            }

            Debug.Log(
                $"[LeftRail] Bake clef size={layout.clefSize} pos={layout.clefAnchoredPosition}. Ctrl+S.");
        }

        private void BakeSelected(BeatTimelineUIView view)
        {
            var go = Selection.activeGameObject;
            var handle = go != null ? go.GetComponent<BossNoteNumberHandle>() : null;
            if (handle == null)
            {
                EditorUtility.DisplayDialog(
                    "Boss Note Number",
                    "Chọn một NoteNum_* (có BossNoteNumberHandle) trên Hierarchy rồi Bake.",
                    "OK");
                return;
            }

            Undo.RecordObject(view, "Bake Boss Note Number");
            BeatTimelineUIView.SuppressBossNoteClusterRebuild = true;
            BakeToLayout(view, handle);
            BeatTimelineUIView.SuppressBossNoteClusterRebuild = false;
            BossNoteLayoutPlayModePersist.Stash(view);
            EditorUtility.SetDirty(view);
            view.RebuildBossNoteClustersPublic();
        }

        private void OnSceneGUI()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var view = (BeatTimelineUIView)target;
            if (view == null)
            {
                return;
            }

            // Unity 6.4+: parameterless overload — avoid obsolete FindObjectsSortMode.
            var handles = Object.FindObjectsByType<BossNoteNumberHandle>();
            if (handles == null || handles.Length == 0)
            {
                return;
            }

            var e = Event.current;
            if (_dragging && e.button == 0 &&
                (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp))
            {
                _dragging = false;
                BeatTimelineUIView.SuppressBossNoteClusterRebuild = false;
                serializedObject.ApplyModifiedProperties();
                BossNoteLayoutPlayModePersist.Stash(view);
                EditorUtility.SetDirty(view);
                view.RebuildBossNoteClustersPublic();
            }

            foreach (var handle in handles)
            {
                if (handle == null || handle.Rect == null)
                {
                    continue;
                }

                var rt = handle.Rect;
                var world = rt.position;
                var size = HandleUtility.GetHandleSize(world) * 0.12f;
                Handles.color = new Color(1f, 0.92f, 0.2f, 0.95f);
                Handles.DrawSolidDisc(world, Vector3.forward, size);
                Handles.Label(world + Vector3.up * size * 2.2f, handle.gameObject.name, EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                var newWorld = Handles.FreeMoveHandle(
                    world,
                    size * 1.4f,
                    Vector3.zero,
                    Handles.CircleHandleCap);
                if (!EditorGUI.EndChangeCheck())
                {
                    continue;
                }

                if (!_dragging)
                {
                    _dragging = true;
                    BeatTimelineUIView.SuppressBossNoteClusterRebuild = true;
                    Undo.RecordObject(view, "Move Boss Note Number");
                }

                Undo.RecordObject(rt, "Move Boss Note Number");
                var parent = rt.parent as RectTransform;
                if (parent != null)
                {
                    var local = parent.InverseTransformPoint(newWorld);
                    rt.anchoredPosition = new Vector2(local.x, local.y);
                }
                else
                {
                    rt.position = newWorld;
                }

                BakeToLayout(view, handle);
                serializedObject.Update();
                EditorUtility.SetDirty(view);
                SceneView.RepaintAll();
            }
        }

        private void BakeToLayout(BeatTimelineUIView view, BossNoteNumberHandle handle)
        {
            var rt = handle.Rect;
            if (rt == null)
            {
                return;
            }

            var totalNudge = rt.anchoredPosition - handle.BaseLocalPos;
            var so = serializedObject;
            so.Update();
            var layout = so.FindProperty("bossNoteNumberLayout");
            if (layout == null)
            {
                return;
            }

            switch (handle.Role)
            {
                case BossNoteNumberRole.BeamedLeft:
                {
                    var shared = layout.FindPropertyRelative("numberNudgeBeamed").vector2Value;
                    layout.FindPropertyRelative("numberNudgeBeamedLeft").vector2Value =
                        totalNudge - shared;
                    break;
                }
                case BossNoteNumberRole.BeamedRight:
                {
                    var shared = layout.FindPropertyRelative("numberNudgeBeamed").vector2Value;
                    layout.FindPropertyRelative("numberNudgeBeamedRight").vector2Value =
                        totalNudge - shared;
                    break;
                }
                default:
                {
                    var shared = layout.FindPropertyRelative("numberNudgeSingle").vector2Value;
                    var variants = layout.FindPropertyRelative("variantNudges");
                    EnsureVariantArray(variants);
                    var i = Mathf.Clamp(handle.VariantIndex, 0, 4);
                    variants.GetArrayElementAtIndex(i).vector2Value = totalNudge - shared;
                    break;
                }
            }

            so.ApplyModifiedProperties();
        }

        private static void EnsureVariantArray(SerializedProperty variants)
        {
            if (variants == null)
            {
                return;
            }

            if (variants.arraySize != 5)
            {
                variants.arraySize = 5;
            }
        }
    }
}
#endif


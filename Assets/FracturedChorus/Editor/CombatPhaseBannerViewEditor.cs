#if UNITY_EDITOR
using FracturedChorus.Combat.Presentation;
using FracturedChorus.UI;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(CombatPhaseBannerView))]
    public sealed class CombatPhaseBannerViewEditor : UnityEditor.Editor
    {
        private CombatPhaseBannerView.BannerPreviewKind _previewKind =
            CombatPhaseBannerView.BannerPreviewKind.Planning;
        private bool _notesFoldout = true;
        private int _noteIndex;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bannerImage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("planningSprite"),
                new GUIContent("Planning Sprite"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("battleStartSprite"),
                new GUIContent("Battle Start Sprite"));
            serializedObject.ApplyModifiedProperties();

            var view = (CombatPhaseBannerView)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Battle Info", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Hierarchy:\n" +
                "  BattleInfo\n" +
                "  └ Banner  ← kéo sprite Planning / Battle Start vào slot trên\n\n" +
                "Nút Planning | Battle Start preview sprite lên Banner ngay trong Scene.\n" +
                "Play chỉ dùng sprite Inspector — không Resources.Load.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            DrawPreviewButton(view, "Planning", CombatPhaseBannerView.BannerPreviewKind.Planning);
            DrawPreviewButton(view, "Battle Start", CombatPhaseBannerView.BannerPreviewKind.BattleStart);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Apply To Banner"))
            {
                Undo.RecordObject(view, "Apply BattleInfo Banner");
                view.PreviewBanner(_previewKind);
                EditorUtility.SetDirty(view);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Set Native Size"))
            {
                if (view.BannerImage != null)
                {
                    Undo.RecordObject(view.BannerImage.rectTransform, "BattleInfo Native Size");
                }

                view.ApplyBannerNativeSize();
                EditorUtility.SetDirty(view);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Hide Banner"))
            {
                view.HideBannerVisual();
                SceneView.RepaintAll();
            }

            DrawNoteSpritesFoldout();
        }

        private void DrawPreviewButton(
            CombatPhaseBannerView view,
            string label,
            CombatPhaseBannerView.BannerPreviewKind kind)
        {
            var selected = _previewKind == kind;
            var prev = GUI.backgroundColor;
            if (selected)
            {
                GUI.backgroundColor = new Color(0.45f, 0.85f, 1f, 1f);
            }

            if (GUILayout.Button(label))
            {
                _previewKind = kind;
                Undo.RecordObject(view, $"Preview {label}");
                view.PreviewBanner(kind);
                EditorUtility.SetDirty(view);
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = prev;
        }

        private void DrawNoteSpritesFoldout()
        {
            _notesFoldout = EditorGUILayout.Foldout(_notesFoldout, "Note sprites (drag & drop)", true);
            if (!_notesFoldout)
            {
                return;
            }

            var timeline = Object.FindAnyObjectByType<BeatTimelineUIView>(FindObjectsInactive.Include);
            if (timeline == null)
            {
                EditorGUILayout.HelpBox("Không thấy BeatTimelineUI trong scene.", MessageType.Warning);
                return;
            }

            var tso = new SerializedObject(timeline);
            var catalog = tso.FindProperty("noteVisuals");
            if (catalog == null)
            {
                return;
            }

            EnsureMusicArray(catalog.FindPropertyRelative("MusicSingleRed"));
            EnsureMusicArray(catalog.FindPropertyRelative("MusicSingleBlue"));
            EnsureMusicArray(catalog.FindPropertyRelative("MusicSinglePurple"));

            EditorGUILayout.HelpBox(
                "Kéo sprite nốt vào slot. Catalog chỉ Resources.Load khi slot còn trống — sprite kéo thả thắng.",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            for (var i = 0; i < BossNoteClusterBuilder.SingleVariantCount; i++)
            {
                DrawNoteIndexButton(i);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(
                GetMusicElement(catalog, "MusicSingleRed", _noteIndex),
                new GUIContent($"V{_noteIndex} Red"));
            EditorGUILayout.PropertyField(
                GetMusicElement(catalog, "MusicSingleBlue", _noteIndex),
                new GUIContent($"V{_noteIndex} Blue"));
            EditorGUILayout.PropertyField(
                GetMusicElement(catalog, "MusicSinglePurple", _noteIndex),
                new GUIContent($"V{_noteIndex} Purple"));

            tso.ApplyModifiedProperties();
        }

        private void DrawNoteIndexButton(int index)
        {
            var selected = _noteIndex == index;
            var prev = GUI.backgroundColor;
            if (selected)
            {
                GUI.backgroundColor = new Color(0.45f, 0.85f, 1f, 1f);
            }

            if (GUILayout.Button($"V{index}"))
            {
                _noteIndex = index;
            }

            GUI.backgroundColor = prev;
        }

        private static SerializedProperty GetMusicElement(
            SerializedProperty catalog,
            string arrayName,
            int index)
        {
            var arr = catalog.FindPropertyRelative(arrayName);
            EnsureMusicArray(arr);
            return arr.GetArrayElementAtIndex(index);
        }

        private static void EnsureMusicArray(SerializedProperty arr)
        {
            if (arr == null || !arr.isArray)
            {
                return;
            }

            if (arr.arraySize != BossNoteClusterBuilder.SingleVariantCount)
            {
                arr.arraySize = BossNoteClusterBuilder.SingleVariantCount;
            }
        }
    }
}
#endif

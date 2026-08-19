#if UNITY_EDITOR
using FracturedChorus.UI;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(CombatPhaseBannerView))]
    public sealed class CombatPhaseBannerViewEditor : UnityEditor.Editor
    {
        private const string PreviewKindSessionPrefix = "FracturedChorus.BattleInfo.PreviewKind.";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bannerImage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("planningSprite"),
                new GUIContent("Planning Sprite"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("battleStartSprite"),
                new GUIContent("Battle Start Sprite"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("battleStartHoldSec"),
                new GUIContent("Battle Start Hold (sec)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("planningHoldSec"),
                new GUIContent("Planning Hold (sec)"));
            serializedObject.ApplyModifiedProperties();

            var view = (CombatPhaseBannerView)target;
            var previewKind = ReadPreviewKind();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Battle Info", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Hierarchy:\n" +
                "  BattleInfo\n" +
                "  └ Banner  ← size / vị trí / xoay trên scene = lúc xuất hiện\n\n" +
                "Apply To Banner chỉ đổi sprite, không kéo Banner về vị trí mặc định.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            DrawPreviewButton(view, "Planning", CombatPhaseBannerView.BannerPreviewKind.Planning, previewKind);
            DrawPreviewButton(view, "Battle Start", CombatPhaseBannerView.BannerPreviewKind.BattleStart, previewKind);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Apply To Banner"))
            {
                RecordBannerUndo(view, "Apply BattleInfo Banner");
                view.PreviewBanner(previewKind);
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
        }

        private void DrawPreviewButton(
            CombatPhaseBannerView view,
            string label,
            CombatPhaseBannerView.BannerPreviewKind kind,
            CombatPhaseBannerView.BannerPreviewKind selectedKind)
        {
            var selected = selectedKind == kind;
            var prev = GUI.backgroundColor;
            if (selected)
            {
                GUI.backgroundColor = new Color(0.45f, 0.85f, 1f, 1f);
            }

            if (GUILayout.Button(label))
            {
                WritePreviewKind(kind);
                RecordBannerUndo(view, $"Preview {label}");
                view.PreviewBanner(kind);
                EditorUtility.SetDirty(view);
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = prev;
        }

        private CombatPhaseBannerView.BannerPreviewKind ReadPreviewKind()
        {
            return (CombatPhaseBannerView.BannerPreviewKind)SessionState.GetInt(
                PreviewKindSessionKey(),
                (int)CombatPhaseBannerView.BannerPreviewKind.Planning);
        }

        private void WritePreviewKind(CombatPhaseBannerView.BannerPreviewKind kind)
        {
            SessionState.SetInt(PreviewKindSessionKey(), (int)kind);
        }

        private string PreviewKindSessionKey()
        {
            return PreviewKindSessionPrefix + target.GetEntityId();
        }

        private static void RecordBannerUndo(CombatPhaseBannerView view, string name)
        {
            Undo.RecordObject(view, name);
            if (view.BannerImage != null)
            {
                Undo.RecordObject(view.BannerImage, name);
            }
        }
    }
}
#endif

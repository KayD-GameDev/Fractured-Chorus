#if UNITY_EDITOR
using FracturedChorus.Narrative.Vn;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(VnDialoguePortraitView))]
    public sealed class VnDialoguePortraitViewEditor : UnityEditor.Editor
    {
        private const string SpeakersFolder =
            "Assets/FracturedChorus/Data/ScriptableObjects/Narrative/Speakers";
        private const string OpeningScenePath =
            "Assets/FracturedChorus/Scenes/OpeningInvestigation.unity";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            var view = (VnDialoguePortraitView)target;
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Portrait Layout Tools", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Portrait layout:\n" +
                "1) Preview bust mẫu.\n" +
                "2) Chọn DialoguePortrait_Left / _Right → kéo Pos / Size.\n" +
                "3) Save Scene — Play giữ đúng RectTransform trên scene.\n" +
                "Capture Layout = ghi Pos/Size vào field Inspector (backup / Apply Saved).\n\n" +
                "Dialogue box / Nameplate / Date HUD:\n" +
                "Chỉnh Text/Rect trên Scene → Save. Không chạy Heal nếu muốn giữ tay chỉnh.\n" +
                "VnRuntimeController → VN Layout Tools để chọn target nhanh.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview: Mei Lin | Ryo"))
            {
                PreviewPair(view, "Speaker_MeiLin.asset", "Speaker_Ryo.asset");
            }

            if (GUILayout.Button("Preview: Mei Lin | Ren"))
            {
                PreviewPair(view, "Speaker_MeiLin.asset", "Speaker_Ren.asset");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview: Haruto only"))
            {
                PreviewPair(view, "Speaker_Haruto.asset", null);
            }

            if (GUILayout.Button("Hide Preview"))
            {
                Undo.RecordObject(view, "Hide Portrait Preview");
                view.Hide();
                EditorUtility.SetDirty(view);
                MarkSceneDirty();
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Capture Layout From Scene Slots", GUILayout.Height(28)))
            {
                Undo.RecordObject(view, "Capture Portrait Layout");
                view.CaptureLayoutFromSlots();
                EditorUtility.SetDirty(view);
                MarkSceneDirty();
                Debug.Log("[Fractured Chorus] Captured portrait layout from Scene slots.");
            }

            if (GUILayout.Button("Apply Saved Layout To Slots"))
            {
                Undo.RecordObject(view, "Apply Portrait Layout");
                if (view.LeftRoot != null)
                {
                    Undo.RecordObject(view.LeftRoot, "Apply Portrait Layout");
                }

                if (view.RightRoot != null)
                {
                    Undo.RecordObject(view.RightRoot, "Apply Portrait Layout");
                }

                view.ApplySavedLayoutToSlots();
                EditorUtility.SetDirty(view);
                MarkSceneDirty();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void PreviewPair(VnDialoguePortraitView view, string leftFile, string rightFile)
        {
            var left = string.IsNullOrEmpty(leftFile)
                ? null
                : AssetDatabase.LoadAssetAtPath<VnSpeakerDefinitionSO>(SpeakersFolder + "/" + leftFile);
            var right = string.IsNullOrEmpty(rightFile)
                ? null
                : AssetDatabase.LoadAssetAtPath<VnSpeakerDefinitionSO>(SpeakersFolder + "/" + rightFile);

            Undo.RecordObject(view, "Preview Portraits");
            view.ApplyEditorPreview(left, right);
            EditorUtility.SetDirty(view);
            MarkSceneDirty();

            if (view.LeftRoot != null)
            {
                Selection.activeGameObject = view.LeftRoot.gameObject;
            }
        }

        private static void MarkSceneDirty()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        [MenuItem("Fractured Chorus/Narrative/Show Opening Portrait Layout Preview")]
        public static void ShowOpeningPortraitPreview()
        {
            var scene = EditorSceneManager.OpenScene(OpeningScenePath, OpenSceneMode.Single);
            var view = Object.FindAnyObjectByType<VnDialoguePortraitView>(FindObjectsInactive.Include);
            if (view == null)
            {
                EditorUtility.DisplayDialog(
                    "Portrait Preview",
                    "Không tìm thấy VnDialoguePortraitView trong OpeningInvestigation.",
                    "OK");
                return;
            }

            view.gameObject.SetActive(true);
            var left = AssetDatabase.LoadAssetAtPath<VnSpeakerDefinitionSO>(SpeakersFolder + "/Speaker_MeiLin.asset");
            var right = AssetDatabase.LoadAssetAtPath<VnSpeakerDefinitionSO>(SpeakersFolder + "/Speaker_Ryo.asset");
            view.ApplyEditorPreview(left, right);
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = view.LeftRoot != null ? view.LeftRoot.gameObject : view.gameObject;
            EditorGUIUtility.PingObject(Selection.activeGameObject);
            Debug.Log(
                "[Fractured Chorus] Portrait preview ON (Mei Lin trái, Ryo phải). " +
                "Kéo RectTransform trên Scene, Save Scene, rồi Play.");
        }
    }
}
#endif

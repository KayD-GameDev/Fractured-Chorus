#if UNITY_EDITOR
using FracturedChorus.Combat.Presentation;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FracturedChorus.Editor
{
    public static class CodaStarHitPreviewEditor
    {
        private const string PreviewName = "CodaStarHitSizePreview";

        [MenuItem("Fractured Chorus/VFX/Preview Coda Skill 1 Star Hit")]
        public static void SpawnPreview()
        {
            EnsureCombatSceneLoaded();
            var preview = EnsurePreview();
            preview.gameObject.SetActive(true);
            preview.RefreshVisual(true);
            Selection.activeGameObject = preview.gameObject;
            EditorGUIUtility.PingObject(preview.gameObject);
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log(
                "[CodaSkill1] Preview sẵn sàng. Inspector: standoff / hit size / debris → Save To Coda Skill 1.");
        }

        [MenuItem("Fractured Chorus/VFX/Save Coda Skill 1 Star Hit From Preview")]
        public static void SaveFromPreview()
        {
            var preview = Object.FindAnyObjectByType<CodaStarHitSizePreview>(FindObjectsInactive.Include);
            if (preview == null)
            {
                EditorUtility.DisplayDialog(
                    "Coda Skill 1",
                    "Chưa có preview. Chạy Preview Coda Skill 1 Star Hit trước.",
                    "OK");
                return;
            }

            preview.SaveToChoreographer();
            EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
            EditorSceneManager.SaveScene(preview.gameObject.scene);
        }

        [MenuItem("Fractured Chorus/VFX/Save & Hide Coda Skill 1 Star Hit")]
        public static void SaveAndHide()
        {
            var preview = Object.FindAnyObjectByType<CodaStarHitSizePreview>(FindObjectsInactive.Include);
            if (preview == null)
            {
                Debug.LogWarning("[CodaSkill1] Không tìm thấy preview để Save & Hide.");
                return;
            }

            preview.SaveToChoreographer();
            Undo.RecordObject(preview.gameObject, "Hide Coda Skill 1 Preview");
            preview.gameObject.SetActive(false);
            EditorUtility.SetDirty(preview.gameObject);
            EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
            EditorSceneManager.SaveScene(preview.gameObject.scene);
            Debug.Log(
                $"[CodaSkill1] Saved & hidden. standoff={preview.StandoffX:F2} hit={preview.HitWorldSize:F2}");
        }

        [MenuItem("Fractured Chorus/VFX/Clear Coda Skill 1 Star Hit Preview")]
        public static void ClearPreview()
        {
            var preview = Object.FindAnyObjectByType<CodaStarHitSizePreview>(FindObjectsInactive.Include);
            if (preview != null)
            {
                Undo.DestroyObjectImmediate(preview.gameObject);
            }
        }

        private static void EnsureCombatSceneLoaded()
        {
            var active = SceneManager.GetActiveScene();
            if (active.name == "CombatPrototype" || active.name == "CombatTutorial")
            {
                return;
            }

            var path = "Assets/FracturedChorus/Scenes/CombatPrototype.unity";
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }
        }

        private static CodaStarHitSizePreview EnsurePreview()
        {
            var existing = Object.FindAnyObjectByType<CodaStarHitSizePreview>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Bind(existing);
                SyncFromChoreographer(existing);
                return existing;
            }

            var root = GameObject.Find("CombatRoot");
            if (root == null)
            {
                root = new GameObject("CombatRoot");
                Undo.RegisterCreatedObjectUndo(root, "Create CombatRoot");
            }

            var go = new GameObject(PreviewName);
            Undo.RegisterCreatedObjectUndo(go, "Create Coda Skill 1 Preview");
            go.transform.SetParent(root.transform, false);
            var preview = go.AddComponent<CodaStarHitSizePreview>();
            Bind(preview);
            SyncFromChoreographer(preview);
            return preview;
        }

        private static void Bind(CodaStarHitSizePreview preview)
        {
            preview.Bind(ResolveCoda(), ResolveBoss());
        }

        private static void SyncFromChoreographer(CodaStarHitSizePreview preview)
        {
            var choreo = Object.FindAnyObjectByType<CodaSkillChoreographer>();
            if (choreo == null)
            {
                return;
            }

            var so = new SerializedObject(choreo);
            preview.SetTuning(
                so.FindProperty("skill1StandoffX")?.floatValue ?? 2.45f,
                so.FindProperty("skill1ContactHeight")?.floatValue ?? 0.8f,
                so.FindProperty("starHitWorldSize")?.floatValue ?? 2.9f,
                so.FindProperty("starDebrisWorldSize")?.floatValue ?? 0.45f,
                so.FindProperty("starDebrisCount")?.intValue ?? 12);
        }

        private static Transform ResolveCoda()
        {
            foreach (var view in Object.FindObjectsByType<UnitView>(FindObjectsSortMode.None))
            {
                if (CodaSkillChoreographer.IsCodaUnit(view.Unit, view))
                {
                    return view.transform;
                }
            }

            var named = GameObject.Find("Unit_Mage")
                        ?? GameObject.Find("Coda")
                        ?? GameObject.Find("Unit_Coda");
            return named != null ? named.transform : null;
        }

        private static Transform ResolveBoss()
        {
            foreach (var view in Object.FindObjectsByType<UnitView>(FindObjectsSortMode.None))
            {
                var id = view.Unit != null ? view.Unit.UnitId ?? string.Empty : string.Empty;
                var n = view.name ?? string.Empty;
                if (id.IndexOf("boss", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Boss", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Knight", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Despair", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return view.transform;
                }
            }

            var named = GameObject.Find("Unit_Knight of Despair")
                        ?? GameObject.Find("Unit_Boss");
            return named != null ? named.transform : null;
        }
    }

    [CustomEditor(typeof(CodaStarHitSizePreview))]
    public sealed class CodaStarHitSizePreviewInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(8f);
            var preview = (CodaStarHitSizePreview)target;
            if (GUILayout.Button("Save To Coda Skill 1", GUILayout.Height(28f)))
            {
                preview.SaveToChoreographer();
                EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
            }

            if (GUILayout.Button("Save & Hide", GUILayout.Height(26f)))
            {
                CodaStarHitPreviewEditor.SaveAndHide();
            }
        }
    }
}
#endif

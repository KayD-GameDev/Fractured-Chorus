#if UNITY_EDITOR
using FracturedChorus.Combat.Presentation;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FracturedChorus.Editor
{
    public static class CodaBeamPreviewEditor
    {
        private const string PreviewName = "CodaBeamSizePreview";

        [MenuItem("Fractured Chorus/VFX/Preview Coda Skill 2 Pierce Beam")]
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
                "[CodaSkill2] Preview sẵn sàng. Inspector: thickness / pierce / castBack → Save To Coda Skill 2.");
        }

        [MenuItem("Fractured Chorus/VFX/Save Coda Skill 2 Beam From Preview")]
        public static void SaveFromPreview()
        {
            var preview = Object.FindAnyObjectByType<CodaBeamSizePreview>(FindObjectsInactive.Include);
            if (preview == null)
            {
                EditorUtility.DisplayDialog(
                    "Coda Skill 2",
                    "Chưa có preview. Chạy Preview Coda Skill 2 Pierce Beam trước.",
                    "OK");
                return;
            }

            preview.SaveToChoreographer();
            EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
            EditorSceneManager.SaveScene(preview.gameObject.scene);
        }

        [MenuItem("Fractured Chorus/VFX/Save & Hide Coda Skill 2 Pierce Beam")]
        public static void SaveAndHide()
        {
            var preview = Object.FindAnyObjectByType<CodaBeamSizePreview>(FindObjectsInactive.Include);
            if (preview == null)
            {
                Debug.LogWarning("[CodaSkill2] Không tìm thấy preview để Save & Hide.");
                return;
            }

            preview.SaveToChoreographer();
            Undo.RecordObject(preview.gameObject, "Hide Coda Skill 2 Preview");
            preview.gameObject.SetActive(false);
            EditorUtility.SetDirty(preview.gameObject);
            EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
            EditorSceneManager.SaveScene(preview.gameObject.scene);
            Debug.Log(
                $"[CodaSkill2] Saved & hidden. thickness={preview.BeamThickness:F2} pierce={preview.PiercePast:F1}");
        }

        [MenuItem("Fractured Chorus/VFX/Clear Coda Skill 2 Beam Preview")]
        public static void ClearPreview()
        {
            var preview = Object.FindAnyObjectByType<CodaBeamSizePreview>(FindObjectsInactive.Include);
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

        private static CodaBeamSizePreview EnsurePreview()
        {
            var existing = Object.FindAnyObjectByType<CodaBeamSizePreview>(FindObjectsInactive.Include);
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
            Undo.RegisterCreatedObjectUndo(go, "Create Coda Skill 2 Preview");
            go.transform.SetParent(root.transform, false);
            var preview = go.AddComponent<CodaBeamSizePreview>();
            Bind(preview);
            SyncFromChoreographer(preview);
            return preview;
        }

        private static void Bind(CodaBeamSizePreview preview)
        {
            var caster = ResolveCoda();
            var target = ResolveBoss();
            preview.Bind(caster, target);
        }

        private static void SyncFromChoreographer(CodaBeamSizePreview preview)
        {
            var choreo = Object.FindAnyObjectByType<CodaSkillChoreographer>();
            if (choreo == null)
            {
                return;
            }

            var so = new SerializedObject(choreo);
            preview.SetTuning(
                so.FindProperty("skill2CastBackX")?.floatValue ?? 0.55f,
                so.FindProperty("skill2AimHeight")?.floatValue ?? 0.78f,
                so.FindProperty("skill2BeamThickness")?.floatValue ?? 2.65f,
                so.FindProperty("skill2PiercePast")?.floatValue ?? 28f,
                so.FindProperty("skill2PierceThroughMap")?.boolValue ?? true,
                so.FindProperty("skill2ChargeWorldSize")?.floatValue ?? 1.85f,
                so.FindProperty("skill2ImpactWorldSize")?.floatValue ?? 2.9f);
        }

        private static Transform ResolveCoda()
        {
            foreach (var view in Object.FindObjectsByType<UnitView>(FindObjectsInactive.Exclude))
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
            foreach (var view in Object.FindObjectsByType<UnitView>(FindObjectsInactive.Exclude))
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

    [CustomEditor(typeof(CodaBeamSizePreview))]
    public sealed class CodaBeamSizePreviewInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(8f);
            var preview = (CodaBeamSizePreview)target;
            if (GUILayout.Button("Save To Coda Skill 2", GUILayout.Height(28f)))
            {
                preview.SaveToChoreographer();
                EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
            }

            if (GUILayout.Button("Save & Hide", GUILayout.Height(26f)))
            {
                CodaBeamPreviewEditor.SaveAndHide();
            }
        }
    }
}
#endif

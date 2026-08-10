#if UNITY_EDITOR
using FracturedChorus.Combat.Presentation;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FracturedChorus.Editor
{
    public static class RenUltAuraPreviewEditor
    {
        private const string PreviewName = "RenUltAuraSizePreview";

        [MenuItem("Fractured Chorus/VFX/Preview Ren Skill 3 Ult Aura")]
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
                "[RenSkill3] Preview sẵn sàng. Inspector: aura / orbit / bullet → Save To Ren Skill 3.");
        }

        [MenuItem("Fractured Chorus/VFX/Save Ren Skill 3 Ult Aura From Preview")]
        public static void SaveFromPreview()
        {
            var preview = Object.FindAnyObjectByType<RenUltAuraSizePreview>(FindObjectsInactive.Include);
            if (preview == null)
            {
                EditorUtility.DisplayDialog(
                    "Ren Skill 3",
                    "Chưa có preview. Chạy Preview Ren Skill 3 Ult Aura trước.",
                    "OK");
                return;
            }

            preview.SaveToChoreographer();
            EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
            EditorSceneManager.SaveScene(preview.gameObject.scene);
        }

        [MenuItem("Fractured Chorus/VFX/Save & Hide Ren Skill 3 Ult Aura")]
        public static void SaveAndHide()
        {
            var preview = Object.FindAnyObjectByType<RenUltAuraSizePreview>(FindObjectsInactive.Include);
            if (preview == null)
            {
                Debug.LogWarning("[RenSkill3] Không tìm thấy preview để Save & Hide.");
                return;
            }

            preview.SaveToChoreographer();
            Undo.RecordObject(preview.gameObject, "Hide Ren Skill 3 Preview");
            preview.gameObject.SetActive(false);
            EditorUtility.SetDirty(preview.gameObject);
            EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
            EditorSceneManager.SaveScene(preview.gameObject.scene);
            Debug.Log(
                $"[RenSkill3] Saved & hidden. aura={preview.AuraWorldSize:F2} orbit={preview.OrbitRadius:F2}");
        }

        [MenuItem("Fractured Chorus/VFX/Clear Ren Skill 3 Ult Aura Preview")]
        public static void ClearPreview()
        {
            var preview = Object.FindAnyObjectByType<RenUltAuraSizePreview>(FindObjectsInactive.Include);
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

        private static RenUltAuraSizePreview EnsurePreview()
        {
            var existing = Object.FindAnyObjectByType<RenUltAuraSizePreview>(FindObjectsInactive.Include);
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
            Undo.RegisterCreatedObjectUndo(go, "Create Ren Skill 3 Preview");
            go.transform.SetParent(root.transform, false);
            var preview = go.AddComponent<RenUltAuraSizePreview>();
            Bind(preview);
            SyncFromChoreographer(preview);
            return preview;
        }

        private static void Bind(RenUltAuraSizePreview preview)
        {
            preview.Bind(ResolveRen(), ResolveBoss());
        }

        private static void SyncFromChoreographer(RenUltAuraSizePreview preview)
        {
            var choreo = Object.FindAnyObjectByType<PlayerSkillShotChoreographer>();
            if (choreo == null)
            {
                return;
            }

            var so = new SerializedObject(choreo);
            preview.SetTuning(
                so.FindProperty("ultAuraWorldSize")?.floatValue ?? 2.8f,
                so.FindProperty("ultAuraOrbitRadius")?.floatValue ?? 1.15f,
                so.FindProperty("bulletHeadWorldSize")?.floatValue ?? 1.275f,
                0.7f,
                so.FindProperty("aimHeightOffset")?.floatValue ?? 0.55f);
        }

        private static Transform ResolveRen()
        {
            foreach (var view in Object.FindObjectsByType<UnitView>(FindObjectsInactive.Exclude))
            {
                var id = view.Unit != null ? view.Unit.UnitId ?? string.Empty : string.Empty;
                var n = view.name ?? string.Empty;
                if (id.IndexOf("ren", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Ren", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return view.transform;
                }
            }

            var named = GameObject.Find("Unit_Ren") ?? GameObject.Find("Ren");
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

    [CustomEditor(typeof(RenUltAuraSizePreview))]
    public sealed class RenUltAuraSizePreviewInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(8f);
            var preview = (RenUltAuraSizePreview)target;
            if (GUILayout.Button("Save To Ren Skill 3", GUILayout.Height(28f)))
            {
                preview.SaveToChoreographer();
                EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
            }

            if (GUILayout.Button("Save & Hide", GUILayout.Height(26f)))
            {
                RenUltAuraPreviewEditor.SaveAndHide();
            }
        }
    }
}
#endif

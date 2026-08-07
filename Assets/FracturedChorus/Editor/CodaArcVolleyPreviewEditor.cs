#if UNITY_EDITOR
using FracturedChorus.Combat.Presentation;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FracturedChorus.Editor
{
    public static class CodaArcVolleyPreviewEditor
    {
        private const string PreviewName = "CodaArcVolleySizePreview";

        [MenuItem("Fractured Chorus/VFX/Preview Coda Skill 3 Arc Volley")]
        public static void SpawnPreview()
        {
            EnsureCombatSceneLoaded();
            var preview = EnsurePreview();
            preview.gameObject.SetActive(true);
            preview.RefreshVisual(true);
            Selection.activeGameObject = preview.gameObject;
            EditorGUIUtility.PingObject(preview.gameObject);
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log("[CodaSkill3] Preview sẵn sàng. Chỉnh spread/bolt/charge → Save To Coda Skill 3.");
        }

        [MenuItem("Fractured Chorus/VFX/Save & Hide Coda Skill 3 Arc Volley")]
        public static void SaveAndHide()
        {
            var preview = Object.FindAnyObjectByType<CodaArcVolleySizePreview>(FindObjectsInactive.Include);
            if (preview == null)
            {
                Debug.LogWarning("[CodaSkill3] Không tìm thấy preview để Save & Hide.");
                return;
            }

            preview.SaveToChoreographer();
            Undo.RecordObject(preview.gameObject, "Hide Coda Skill 3 Preview");
            preview.gameObject.SetActive(false);
            EditorUtility.SetDirty(preview.gameObject);
            EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
            EditorSceneManager.SaveScene(preview.gameObject.scene);
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

        private static CodaArcVolleySizePreview EnsurePreview()
        {
            var existing = Object.FindAnyObjectByType<CodaArcVolleySizePreview>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.Bind(ResolveCoda(), ResolveBoss());
                return existing;
            }

            var root = GameObject.Find("CombatRoot") ?? new GameObject("CombatRoot");
            var go = new GameObject(PreviewName);
            Undo.RegisterCreatedObjectUndo(go, "Create Coda Skill 3 Preview");
            go.transform.SetParent(root.transform, false);
            var preview = go.AddComponent<CodaArcVolleySizePreview>();
            preview.Bind(ResolveCoda(), ResolveBoss());
            return preview;
        }

        private static Transform ResolveCoda()
        {
            foreach (var view in Object.FindObjectsByType<UnitView>())
            {
                if (CodaSkillChoreographer.IsCodaUnit(view.Unit, view))
                {
                    return view.transform;
                }
            }

            var named = GameObject.Find("Unit_Mage") ?? GameObject.Find("Coda");
            return named != null ? named.transform : null;
        }

        private static Transform ResolveBoss()
        {
            var named = GameObject.Find("Unit_Knight of Despair") ?? GameObject.Find("Unit_Boss");
            return named != null ? named.transform : null;
        }
    }

    [CustomEditor(typeof(CodaArcVolleySizePreview))]
    public sealed class CodaArcVolleySizePreviewInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(8f);
            var preview = (CodaArcVolleySizePreview)target;
            if (GUILayout.Button("Save To Coda Skill 3", GUILayout.Height(28f)))
            {
                preview.SaveToChoreographer();
                EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
            }

            if (GUILayout.Button("Save & Hide", GUILayout.Height(26f)))
            {
                CodaArcVolleyPreviewEditor.SaveAndHide();
            }
        }
    }
}
#endif

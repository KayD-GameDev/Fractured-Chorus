#if UNITY_EDITOR
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FracturedChorus.Editor
{
    public static class DamageNumberPreviewEditor
    {
        [MenuItem("Fractured Chorus/Combat/Spawn Damage Number Preview Dummy")]
        public static void SpawnDummy()
        {
            EnsureCombatSceneLoaded();
            var dummy = Object.FindAnyObjectByType<DamageNumberPreviewDummy>(FindObjectsInactive.Include);
            if (dummy == null)
            {
                var go = new GameObject(DamageNumberPreviewDummy.ObjectName);
                dummy = go.AddComponent<DamageNumberPreviewDummy>();
                Undo.RegisterCreatedObjectUndo(go, "Create Damage Number Preview Dummy");
            }

            dummy.gameObject.SetActive(true);
            dummy.Refresh(true);
            Selection.activeGameObject = dummy.gameObject;
            EditorGUIUtility.PingObject(dummy.gameObject);
            EditorSceneManager.MarkSceneDirty(dummy.gameObject.scene);
            Debug.Log("[DamageNumbers] Preview dummy sẵn sàng. Sửa Amount / Heal / Crit trên Inspector — nhìn Game view trước Play.");
        }

        [MenuItem("Fractured Chorus/Combat/Hide Damage Number Preview Dummy")]
        public static void HideDummy()
        {
            var dummy = Object.FindAnyObjectByType<DamageNumberPreviewDummy>(FindObjectsInactive.Include);
            if (dummy == null)
            {
                Debug.LogWarning("[DamageNumbers] Chưa có preview dummy.");
                return;
            }

            Undo.RecordObject(dummy.gameObject, "Hide Damage Number Preview Dummy");
            dummy.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(dummy.gameObject.scene);
        }

        private static void EnsureCombatSceneLoaded()
        {
            var active = SceneManager.GetActiveScene();
            if (active.name == "CombatPrototype" || active.name == "CombatTutorial")
            {
                return;
            }

            var path = "Assets/FracturedChorus/Scenes/CombatPrototype.unity";
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            EditorSceneManager.SetActiveScene(scene);
        }
    }

    [CustomEditor(typeof(DamageNumberPreviewDummy))]
    public sealed class DamageNumberPreviewDummyInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var dummy = (DamageNumberPreviewDummy)target;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Giá trị hiện: " + dummy.DisplayedValue +
                "\nTrước Play: dùng Amount trên Inspector." +
                "\nGán Follow Unit để neo đúng chỗ số bay. Lúc Play dummy ẩn (Hide When Playing).",
                MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("8"))
            {
                SetAmount(dummy, 8);
            }

            if (GUILayout.Button("24"))
            {
                SetAmount(dummy, 24);
            }

            if (GUILayout.Button("42"))
            {
                SetAmount(dummy, 42);
            }

            if (GUILayout.Button("86"))
            {
                SetAmount(dummy, 86);
            }

            if (GUILayout.Button("120"))
            {
                SetAmount(dummy, 120);
            }

            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Refresh Preview"))
            {
                dummy.Refresh(true);
                EditorUtility.SetDirty(dummy);
            }
        }

        private static void SetAmount(DamageNumberPreviewDummy dummy, int amount)
        {
            Undo.RecordObject(dummy, "Set preview amount");
            dummy.Amount = amount;
            dummy.Refresh(true);
            EditorUtility.SetDirty(dummy);
        }
    }
}
#endif

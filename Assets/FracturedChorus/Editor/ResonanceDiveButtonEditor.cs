#if UNITY_EDITOR
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(ResonanceDiveButton))]
    public sealed class ResonanceDiveButtonEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var button = (ResonanceDiveButton)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Motion preview", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Normal"))
            {
                button.PreviewState(false, false);
            }

            if (GUILayout.Button("Hover"))
            {
                button.PreviewState(true, false);
            }

            if (GUILayout.Button("Pressed"))
            {
                button.PreviewState(true, true);
            }

            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Rebuild hierarchy + sprites"))
            {
                ResonanceDiveHierarchy.Ensure(button);
                button.AssignMissingSprites();
                EditorUtility.SetDirty(button);
            }
        }

        private void OnEnable()
        {
            EditorApplication.update += TickPreview;
        }

        private void OnDisable()
        {
            EditorApplication.update -= TickPreview;
        }

        private void TickPreview()
        {
            if (target == null)
            {
                return;
            }

            var button = (ResonanceDiveButton)target;
            if (!button.isActiveAndEnabled || EditorApplication.isPlaying)
            {
                return;
            }

            button.EditorTick(1f / 60f);
        }
    }

    public static class ResonanceDiveButtonBuilder
    {
        private const string PrefabPath = "Assets/FracturedChorus/UI/Prefabs/ResonanceDiveButton.prefab";

        [MenuItem("Fractured Chorus/Resonance Dive/Rebuild In Open Scene")]
        public static void RebuildOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Resonance Dive", "Exit Play Mode rồi chạy lại.", "OK");
                return;
            }

            var found = Object.FindObjectsByType<ResonanceDiveButton>(FindObjectsInactive.Include);
            if (found.Length == 0)
            {
                EditorUtility.DisplayDialog("Resonance Dive", "Không thấy ResonanceDiveButton trong scene.", "OK");
                return;
            }

            foreach (var button in found)
            {
                ResonanceDiveHierarchy.Ensure(button);
                button.AssignMissingSprites();
                EditorUtility.SetDirty(button);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Resonance Dive] Rebuilt " + found.Length + " button(s). Ctrl+S để lưu scene.");
        }

        [MenuItem("Fractured Chorus/Resonance Dive/Create Prefab")]
        public static void CreatePrefab()
        {
            var go = new GameObject(ResonanceDiveButton.ObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button), typeof(CanvasGroup), typeof(ResonanceDiveButton));
            var button = go.GetComponent<ResonanceDiveButton>();
            ResonanceDiveHierarchy.Ensure(button);
            button.AssignMissingSprites();
            var folder = "Assets/FracturedChorus/UI/Prefabs";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus/UI", "Prefabs");
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            if (prefab != null)
            {
                Selection.activeObject = prefab;
                Debug.Log("[Resonance Dive] Prefab: " + PrefabPath);
            }
        }
    }
}
#endif

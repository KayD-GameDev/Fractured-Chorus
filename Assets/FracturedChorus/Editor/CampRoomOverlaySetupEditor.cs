#if UNITY_EDITOR
using FracturedChorus.RunMap;
using FracturedChorus.RunMap.UI;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class CampRoomOverlaySetupEditor
    {
        private const string ArtBackgroundPath = CampRoomOverlayUIView.BackgroundAssetPath;

        [MenuItem("Fractured Chorus/Run Map/Setup Camp Room Overlay", false, 40)]
        public static void SetupCampRoomOverlay()
        {
            var canvas = FindRunMapCanvas();
            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] RunMapCanvas not found. Open RunMapPrototype first.");
                return;
            }

            var existing = canvas.GetComponentInChildren<CampRoomOverlayUIView>(true);
            CampRoomOverlayUIView view;
            if (existing != null)
            {
                view = existing;
                Undo.RecordObject(view.gameObject, "Refresh Camp Room Overlay");
            }
            else
            {
                var go = new GameObject("CampRoomOverlay", typeof(RectTransform), typeof(CampRoomOverlayUIView));
                Undo.RegisterCreatedObjectUndo(go, "Create Camp Room Overlay");
                go.transform.SetParent(canvas, false);
                view = go.GetComponent<CampRoomOverlayUIView>();
                view.BuildDefaultHierarchy();
            }

            view.WireSceneReferences();
            view.ApplyBackground();
            AssignBackgroundSprite(view);

            var so = new SerializedObject(view);
            so.FindProperty("preserveSceneLayout").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            view.gameObject.SetActive(false);
            WireRunMapController(view);
            UiFontCatalog.ApplyHierarchy(view.transform, true);
            Selection.activeGameObject = view.gameObject;
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            Debug.Log(
                "[Fractured Chorus] CampRoomOverlay sẵn sàng dưới RunMapCanvas (ẩn mặc định). " +
                "Bật GameObject để chỉnh layout, Save scene.");
        }

        private static void AssignBackgroundSprite(CampRoomOverlayUIView view)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtBackgroundPath);
            if (sprite == null)
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/FracturedChorus/Resources/UI/RunMap/camp_room_bg_v1.png");
            }

            if (sprite == null)
            {
                return;
            }

            var serialized = new SerializedObject(view);
            serialized.FindProperty("backgroundSprite").objectReferenceValue = sprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireRunMapController(CampRoomOverlayUIView view)
        {
            var controller = Object.FindAnyObjectByType<RunMapController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                return;
            }

            var so = new SerializedObject(controller);
            so.FindProperty("campOverlay").objectReferenceValue = view;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static Transform FindRunMapCanvas()
        {
            var named = GameObject.Find("RunMapCanvas");
            if (named != null)
            {
                return named.transform;
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            return canvas != null ? canvas.transform : null;
        }
    }
}
#endif

#if UNITY_EDITOR
using FracturedChorus.RunMap;
using FracturedChorus.RunMap.UI;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class ShopRoomOverlaySetupEditor
    {
        private const string ArtBackgroundPath = ShopRoomOverlayUIView.BackgroundAssetPath;

        [MenuItem("Fractured Chorus/Run Map/Setup Shop Room Overlay", false, 41)]
        public static void SetupShopRoomOverlay()
        {
            var canvas = FindRunMapCanvas();
            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] RunMapCanvas not found. Open RunMapPrototype first.");
                return;
            }

            var existing = canvas.GetComponentInChildren<ShopRoomOverlayUIView>(true);
            ShopRoomOverlayUIView view;
            if (existing != null)
            {
                view = existing;
                Undo.RecordObject(view.gameObject, "Refresh Shop Room Overlay");
            }
            else
            {
                var go = new GameObject("ShopRoomOverlay", typeof(RectTransform), typeof(ShopRoomOverlayUIView));
                Undo.RegisterCreatedObjectUndo(go, "Create Shop Room Overlay");
                go.transform.SetParent(canvas, false);
                view = go.GetComponent<ShopRoomOverlayUIView>();
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
                "[Fractured Chorus] ShopRoomOverlay sẵn sàng dưới RunMapCanvas (ẩn mặc định). " +
                "Bật GameObject để chỉnh layout, Save scene.");
        }

        private static void AssignBackgroundSprite(ShopRoomOverlayUIView view)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtBackgroundPath);
            if (sprite == null)
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/FracturedChorus/Resources/UI/RunMap/shop_room_bg_v1.png");
            }

            if (sprite == null)
            {
                return;
            }

            var serialized = new SerializedObject(view);
            serialized.FindProperty("backgroundSprite").objectReferenceValue = sprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireRunMapController(ShopRoomOverlayUIView view)
        {
            var controller = Object.FindAnyObjectByType<RunMapController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                return;
            }

            var so = new SerializedObject(controller);
            so.FindProperty("shopOverlay").objectReferenceValue = view;
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

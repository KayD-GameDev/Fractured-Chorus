#if UNITY_EDITOR
using FracturedChorus.Data;
using FracturedChorus.RunMap;
using FracturedChorus.RunMap.UI;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Video;

namespace FracturedChorus.Editor
{
    public static class EventRoomOverlaySetupEditor
    {
        private const string ChoiceFolder = "Assets/FracturedChorus/Resources/Events";
        private const string ArtBackgroundPath = EventRoomOverlayUIView.BackgroundAssetPath;

        [MenuItem("Fractured Chorus/Run Map/Setup Event Room Overlay", false, 39)]
        public static void SetupEventRoomOverlay()
        {
            var table = EnsureChoiceAssets();
            var canvas = FindRunMapCanvas();
            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] RunMapCanvas not found. Open RunMapPrototype first.");
                return;
            }

            var existing = canvas.GetComponentInChildren<EventRoomOverlayUIView>(true);
            EventRoomOverlayUIView view;
            if (existing != null)
            {
                view = existing;
                Undo.RecordObject(view.gameObject, "Refresh Event Room Overlay");
            }
            else
            {
                var go = new GameObject("EventRoomOverlay", typeof(RectTransform), typeof(EventRoomOverlayUIView));
                Undo.RegisterCreatedObjectUndo(go, "Create Event Room Overlay");
                go.transform.SetParent(canvas, false);
                view = go.GetComponent<EventRoomOverlayUIView>();
                view.BuildDefaultHierarchy();
            }

            view.WireSceneReferences();
            view.SetChoiceTable(table);
            AssignBackgroundSprite(view);
            AssignBackgroundVideo(view);
            view.ApplyBackground();
            AssignTable(view, table);

            var so = new SerializedObject(view);
            so.FindProperty("preserveSceneLayout").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            view.gameObject.SetActive(false);
            WireRunMapController(view, table);
            UiFontCatalog.ApplyHierarchy(view.transform, true);
            Selection.activeGameObject = view.gameObject;
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            Debug.Log(
                "[Fractured Chorus] EventRoomOverlay sẵn sàng dưới RunMapCanvas (ẩn mặc định). " +
                "Bật GameObject để chỉnh layout, Save scene.");
        }

        public static EventChoiceTableSO EnsureChoiceAssets()
        {
            if (!AssetDatabase.IsValidFolder("Assets/FracturedChorus/Resources"))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus", "Resources");
            }

            if (!AssetDatabase.IsValidFolder(ChoiceFolder))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus/Resources", "Events");
            }

            var catalog = EventChoiceSO.CreateDefaultCatalog();
            var assets = new EventChoiceSO[catalog.Length];
            for (var i = 0; i < catalog.Length; i++)
            {
                var source = catalog[i];
                var path = ChoiceFolder + "/EventChoice_" + source.Id + ".asset";
                assets[i] = EnsureChoice(path, source);
                Object.DestroyImmediate(source);
            }

            var table = AssetDatabase.LoadAssetAtPath<EventChoiceTableSO>(EventChoiceTableSO.AssetPath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<EventChoiceTableSO>();
                AssetDatabase.CreateAsset(table, EventChoiceTableSO.AssetPath);
            }

            table.EditorAssign(assets, 3);
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            return table;
        }

        private static EventChoiceSO EnsureChoice(string path, EventChoiceSO source)
        {
            var choice = AssetDatabase.LoadAssetAtPath<EventChoiceSO>(path);
            if (choice == null)
            {
                choice = ScriptableObject.CreateInstance<EventChoiceSO>();
                AssetDatabase.CreateAsset(choice, path);
            }

            choice.EditorAssign(source.Id, source.Title, source.Description, source.Kind, source.Magnitude);
            EditorUtility.SetDirty(choice);
            return choice;
        }

        private static void AssignBackgroundSprite(EventRoomOverlayUIView view)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtBackgroundPath);
            if (sprite == null)
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/FracturedChorus/Resources/UI/RunMap/event_room_bg_v1.png");
            }

            if (sprite == null)
            {
                return;
            }

            var so = new SerializedObject(view);
            so.FindProperty("backgroundSprite").objectReferenceValue = sprite;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignBackgroundVideo(EventRoomOverlayUIView view)
        {
            var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(EventRoomOverlayUIView.BackgroundVideoAssetPath);
            if (clip == null)
            {
                clip = AssetDatabase.LoadAssetAtPath<VideoClip>(
                    "Assets/FracturedChorus/Resources/UI/RunMap/event_room_bg_v1.mp4");
            }

            if (clip == null)
            {
                return;
            }

            var so = new SerializedObject(view);
            so.FindProperty("backgroundVideo").objectReferenceValue = clip;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignTable(EventRoomOverlayUIView view, EventChoiceTableSO table)
        {
            var so = new SerializedObject(view);
            so.FindProperty("choiceTable").objectReferenceValue = table;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireRunMapController(EventRoomOverlayUIView view, EventChoiceTableSO table)
        {
            var controller = Object.FindAnyObjectByType<RunMapController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                return;
            }

            var so = new SerializedObject(controller);
            so.FindProperty("eventOverlay").objectReferenceValue = view;
            so.FindProperty("eventChoices").objectReferenceValue = table;
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

#if UNITY_EDITOR
using FracturedChorus.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(MainMenuStartGameController))]
    public class MainMenuStartGameControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (Application.isPlaying)
            {
                return;
            }

            var controller = (MainMenuStartGameController)target;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Edit Mode Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Select a preview layer to show only one layer in Scene/Game view. Preview does not change UI layout.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Attract"))
            {
                Undo.RecordObject(controller, "Preview Attract");
                controller.SetEditorPreview(MainMenuStartGameController.MainMenuEditorPreview.Attract);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Main Menu"))
            {
                Undo.RecordObject(controller, "Preview Main Menu");
                controller.SetEditorPreview(MainMenuStartGameController.MainMenuEditorPreview.MainMenu);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Config"))
            {
                Undo.RecordObject(controller, "Preview Config");
                controller.SetEditorPreview(MainMenuStartGameController.MainMenuEditorPreview.Settings);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Off-Beat"))
            {
                Undo.RecordObject(controller, "Preview Off-Beat Archive");
                controller.SetEditorPreview(MainMenuStartGameController.MainMenuEditorPreview.OffBeatArchive);
                EditorUtility.SetDirty(controller);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Load"))
            {
                // Dựng sẵn layer trước khi bật preview, không thì bấm xong chẳng thấy gì.
                EnsureLoadLayerBound(controller);
                Undo.RecordObject(controller, "Preview Load Game");
                controller.SetEditorPreview(MainMenuStartGameController.MainMenuEditorPreview.LoadGame);
                EditorUtility.SetDirty(controller);
                FillLoadLayerPreview(controller);
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(
                "Off-Beat: chỉnh ArchivePanel / CatalogScroll / PlayerRoot trên MainMenuCanvas.\n" +
                "Load: chỉnh layout ngay trên LoadLayer trong Hierarchy — runtime đọc lại đúng " +
                "màu, chữ và vị trí bạn đặt ở đó.",
                MessageType.None);

            DrawLoadLayerSection(controller);
        }

        /// <summary>
        /// Ghi header slot thật vào các label của LoadLayer để preview hiện nội dung như lúc chơi,
        /// nhờ vậy canh chiều rộng hàng và panel detail không bị hụt chữ.
        /// </summary>
        private static void FillLoadLayerPreview(MainMenuStartGameController controller)
        {
            var layer = controller.ResolveLoadLayer();
            var view = layer != null ? layer.GetComponent<SaveLoadSlotListView>() : null;
            if (view == null)
            {
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(view.gameObject, "Preview Save Slots");
            view.ApplyEditorPreview(SaveLoadSlotListView.Mode.Load, sessionActive: false);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
        }

        /// <summary>
        /// LoadLayer là GameObject dựng sẵn trong scene chứ không sinh runtime, nên nếu thiếu
        /// thì cho cắm lại ngay tại đây thay vì phải nhớ menu nằm ở đâu.
        /// </summary>
        private void DrawLoadLayerSection(MainMenuStartGameController controller)
        {
            if (controller.ResolveLoadLayer() != null)
            {
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "Chưa có Load Layer. Bấm nút dưới để dựng panel Save/Load vào scene rồi save lại.",
                MessageType.Info);

            if (GUILayout.Button("Build Load Layer"))
            {
                EnsureLoadLayerBound(controller);
            }
        }

        /// <summary>
        /// Tìm LoadLayer có sẵn trong scene (giữ nguyên layout đã chỉnh), chỉ dựng mới khi thật sự
        /// chưa có, rồi trỏ field loadLayer vào đó.
        /// </summary>
        private static void EnsureLoadLayerBound(MainMenuStartGameController controller)
        {
            if (controller.ResolveLoadLayer() != null)
            {
                return;
            }

            var layer = SaveLoadLayerBuilder.EnsureLoadLayer(controller.transform.root);
            if (layer == null)
            {
                return;
            }

            SaveLoadLayerBuilder.BindToController(controller.transform.root, layer);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }
    }
}
#endif

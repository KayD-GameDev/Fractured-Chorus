#if UNITY_EDITOR
using FracturedChorus.Menu;
using FracturedChorus.Meta;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    /// <summary>
    /// Dựng LoadLayer bằng GameObject thật (không sinh runtime) để layout kéo thả chỉnh tay được.
    /// Dùng chung cho scene đang mở và scene main menu production nên hai bên không lệch nhau.
    /// </summary>
    public static class SaveLoadLayerBuilder
    {
        public const string LayerName = "LoadLayer";

        public const string PrefabPath = "Assets/FracturedChorus/Resources/UI/SaveLoadPanel.prefab";

        /// <summary>
        /// Sinh prefab cho scene không có LoadLayer dựng sẵn (status menu trong ván chơi).
        /// Mở prefab ra chỉnh layout là runtime lấy đúng bản đã chỉnh.
        /// </summary>
        [MenuItem("Fractured Chorus/Save/Build Save Load Panel Prefab", false, 21)]
        public static void BuildPanelPrefab()
        {
            EnsureFolder("Assets/FracturedChorus/Resources");
            EnsureFolder("Assets/FracturedChorus/Resources/UI");

            var holder = new GameObject("~SaveLoadPanelBuildRoot");
            try
            {
                var layer = Build(holder.transform, sortingOrder: 500);
                layer.name = "SaveLoadPanel";
                layer.transform.SetParent(null, false);
                layer.SetActive(false);

                PrefabUtility.SaveAsPrefabAsset(layer, PrefabPath);
                Object.DestroyImmediate(layer);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log($"[Fractured Chorus] Save/Load panel prefab dựng xong tại {PrefabPath}.");
            }
            catch (System.Exception error)
            {
                Debug.LogError($"[Fractured Chorus] Dựng Save/Load panel prefab lỗi: {error}");
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>
        /// Cắm LoadLayer vào scene đang mở mà không dựng lại cả hierarchy — dùng cho scene đã chỉnh tay.
        /// </summary>
        [MenuItem("Fractured Chorus/Save/Install Load Layer In Open Scene", false, 20)]
        public static void InstallInOpenScene()
        {
            var root = ResolveSceneRoot();
            if (root == null)
            {
                EditorUtility.DisplayDialog(
                    "Load Layer",
                    "Không tìm thấy root. Chọn GameObject root của scene (MainMenuStartGameRoot) rồi chạy lại.",
                    "OK");
                return;
            }

            var layer = EnsureLoadLayer(root);
            if (layer == null)
            {
                return;
            }

            BindToController(root, layer);

            Selection.activeGameObject = layer;
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Debug.Log($"[Fractured Chorus] LoadLayer sẵn sàng dưới '{root.name}'. Nhớ save scene.");
        }

        /// <summary>
        /// Trỏ field loadLayer của MainMenuStartGameController vào layer vừa dựng để nút preview
        /// "Load" trong Inspector có cái để bật.
        /// </summary>
        public static void BindToController(Transform root, GameObject layer)
        {
            if (root == null || layer == null)
            {
                return;
            }

            var controller = root.GetComponentInChildren<MainMenuStartGameController>(true);
            if (controller == null)
            {
                return;
            }

            var so = new SerializedObject(controller);
            var property = so.FindProperty("loadLayer");
            if (property == null)
            {
                return;
            }

            property.objectReferenceValue = layer.GetComponent<CanvasGroup>();
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static Transform ResolveSceneRoot()
        {
            if (Selection.activeGameObject != null)
            {
                return Selection.activeGameObject.transform.root;
            }

            var scene = EditorSceneManager.GetActiveScene();
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.GetComponentInChildren<Canvas>(true) != null)
                {
                    return go.transform;
                }
            }

            return null;
        }

        public static GameObject EnsureLoadLayer(Transform root, int sortingOrder = 2, bool startActive = false)
        {
            if (root == null)
            {
                Debug.LogError("[Fractured Chorus] SaveLoadLayerBuilder: root null.");
                return null;
            }

            var existing = root.Find(LayerName);
            if (existing != null)
            {
                // Đã dựng rồi thì giữ nguyên layout người dùng đã chỉnh, chỉ nối lại reference.
                var view = existing.GetComponent<SaveLoadSlotListView>();
                if (view != null)
                {
                    return existing.gameObject;
                }

                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            var layer = Build(root, sortingOrder);
            layer.SetActive(startActive);
            Undo.RegisterCreatedObjectUndo(layer, "Create Load Layer");
            return layer;
        }

        private static GameObject Build(Transform root, int sortingOrder)
        {
            var layerGo = new GameObject(
                LayerName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));
            layerGo.transform.SetParent(root, false);

            var canvas = layerGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = layerGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            var dim = CreateImage(layerGo.transform, "Dim", new Color(0f, 0f, 0f, 0.55f));
            Stretch(dim.rectTransform, Vector2.zero, Vector2.one);
            dim.raycastTarget = true;

            var panel = CreateImage(layerGo.transform, "Panel", FcColorTokens.Surface.Modal);
            var panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(1120f, 760f);

            var title = CreateText(panelRect, "Title", "LOAD GAME", 34, TextAnchor.UpperCenter, FontStyle.Bold);
            Stretch(title.rectTransform, new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.98f));
            title.color = FcColorTokens.Brand.Cyan;

            var tabBar = CreateEmpty(panelRect, "TabBar");
            Stretch(tabBar, new Vector2(0.05f, 0.81f), new Vector2(0.95f, 0.885f));
            var loadTab = CreateButton(tabBar, "Tab_Load", "LOAD", new Vector2(0f, 0f), new Vector2(0.485f, 1f));
            var saveTab = CreateButton(tabBar, "Tab_Save", "SAVE", new Vector2(0.515f, 0f), new Vector2(1f, 1f));

            var slotList = CreateEmpty(panelRect, "SlotList");
            Stretch(slotList, new Vector2(0.04f, 0.06f), new Vector2(0.56f, 0.79f));

            var slots = new (Button Button, Image Background, Text Label)[GameMetaSaveLoad.SlotCount];
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i] = CreateSlotRow(slotList, i);
            }

            var detailPanel = CreateImage(panelRect, "DetailPanel", FcColorTokens.Surface.Detail);
            Stretch(detailPanel.rectTransform, new Vector2(0.58f, 0.34f), new Vector2(0.96f, 0.79f));

            var detail = CreateText(detailPanel.rectTransform, "Detail", "Select a slot.", 22, TextAnchor.UpperLeft);
            Stretch(detail.rectTransform, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f));
            detail.color = FcColorTokens.Brand.TextMuted;
            detail.horizontalOverflow = HorizontalWrapMode.Wrap;

            var primary = CreateButton(panelRect, "Btn_Primary", "LOAD", new Vector2(0.58f, 0.23f), new Vector2(0.96f, 0.31f));
            var delete = CreateButton(panelRect, "Btn_Delete", "DELETE", new Vector2(0.58f, 0.14f), new Vector2(0.96f, 0.22f));
            var close = CreateButton(panelRect, "Btn_Close", "CLOSE", new Vector2(0.58f, 0.05f), new Vector2(0.96f, 0.13f));

            var confirm = BuildConfirmDialog(layerGo.transform);

            var view = layerGo.AddComponent<SaveLoadSlotListView>();
            BindView(view, layerGo, title, detail, primary, delete, close, loadTab, saveTab, slots, confirm);

            UiFontCatalog.ApplyHierarchy(layerGo.transform, true);
            return layerGo;
        }

        private static void BindView(
            SaveLoadSlotListView view,
            GameObject layerGo,
            Text title,
            Text detail,
            (Button Button, Text Label) primary,
            (Button Button, Text Label) delete,
            (Button Button, Text Label) close,
            (Button Button, Text Label) loadTab,
            (Button Button, Text Label) saveTab,
            (Button Button, Image Background, Text Label)[] slots,
            ConfirmDialogView confirm)
        {
            // Các field là [SerializeField] private nên phải gán qua SerializedObject.
            var so = new SerializedObject(view);
            so.FindProperty("sceneCanvasGroup").objectReferenceValue = layerGo.GetComponent<CanvasGroup>();
            so.FindProperty("sceneTitleLabel").objectReferenceValue = title;
            so.FindProperty("sceneDetailLabel").objectReferenceValue = detail;
            so.FindProperty("scenePrimaryButton").objectReferenceValue = primary.Button;
            so.FindProperty("scenePrimaryLabel").objectReferenceValue = primary.Label;
            so.FindProperty("sceneDeleteButton").objectReferenceValue = delete.Button;
            so.FindProperty("sceneCloseButton").objectReferenceValue = close.Button;
            so.FindProperty("sceneLoadTabButton").objectReferenceValue = loadTab.Button;
            so.FindProperty("sceneLoadTabLabel").objectReferenceValue = loadTab.Label;
            so.FindProperty("sceneSaveTabButton").objectReferenceValue = saveTab.Button;
            so.FindProperty("sceneSaveTabLabel").objectReferenceValue = saveTab.Label;
            so.FindProperty("sceneConfirmDialog").objectReferenceValue = confirm;

            var slotsProp = so.FindProperty("sceneSlots");
            slotsProp.arraySize = slots.Length;
            for (var i = 0; i < slots.Length; i++)
            {
                var element = slotsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Button").objectReferenceValue = slots[i].Button;
                element.FindPropertyRelative("Background").objectReferenceValue = slots[i].Background;
                element.FindPropertyRelative("Label").objectReferenceValue = slots[i].Label;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
        }

        private static ConfirmDialogView BuildConfirmDialog(Transform parent)
        {
            var rootGo = new GameObject("ConfirmDialog", typeof(RectTransform), typeof(CanvasGroup));
            rootGo.transform.SetParent(parent, false);
            Stretch(rootGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            var dim = CreateImage(rootGo.transform, "Dim", FcColorTokens.Surface.DimmerBlack);
            Stretch(dim.rectTransform, Vector2.zero, Vector2.one);

            var panel = CreateImage(rootGo.transform, "Panel", FcColorTokens.Surface.Modal);
            var panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(620f, 280f);

            var title = CreateText(panelRect, "Title", "CONFIRM", 28, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(title.rectTransform, new Vector2(0.06f, 0.74f), new Vector2(0.94f, 0.92f));
            title.color = FcColorTokens.Brand.Cyan;

            var message = CreateText(panelRect, "Message", string.Empty, 21, TextAnchor.UpperCenter);
            Stretch(message.rectTransform, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.72f));
            message.color = FcColorTokens.Brand.TextPrimary;
            message.horizontalOverflow = HorizontalWrapMode.Wrap;

            var confirmBtn = CreateButton(panelRect, "Btn_Confirm", "YES", new Vector2(0.1f, 0.1f), new Vector2(0.46f, 0.28f));
            var cancelBtn = CreateButton(panelRect, "Btn_Cancel", "NO", new Vector2(0.54f, 0.1f), new Vector2(0.9f, 0.28f));
            confirmBtn.Label.color = FcColorTokens.Brand.RedSelection;

            var dialog = rootGo.AddComponent<ConfirmDialogView>();
            var so = new SerializedObject(dialog);
            so.FindProperty("canvasGroup").objectReferenceValue = rootGo.GetComponent<CanvasGroup>();
            so.FindProperty("titleLabel").objectReferenceValue = title;
            so.FindProperty("messageLabel").objectReferenceValue = message;
            so.FindProperty("confirmButton").objectReferenceValue = confirmBtn.Button;
            so.FindProperty("confirmLabel").objectReferenceValue = confirmBtn.Label;
            so.FindProperty("cancelButton").objectReferenceValue = cancelBtn.Button;
            so.FindProperty("cancelLabel").objectReferenceValue = cancelBtn.Label;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(dialog);

            rootGo.SetActive(false);
            return dialog;
        }

        private static (Button Button, Image Background, Text Label) CreateSlotRow(RectTransform parent, int index)
        {
            var image = CreateImage(parent, $"Slot_{index:00}", FcColorTokens.Surface.Row);
            var rect = image.rectTransform;
            var yMax = 1f - index * 0.1f;
            var yMin = yMax - 0.09f;
            Stretch(rect, new Vector2(0f, yMin), new Vector2(1f, yMax));

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var label = CreateText(rect, "Label", $"SLOT {index + 1:00}  —  EMPTY", 20, TextAnchor.MiddleLeft);
            Stretch(label.rectTransform, new Vector2(0.04f, 0f), new Vector2(0.96f, 1f));
            label.color = Color.white;

            return (button, image, label);
        }

        private static (Button Button, Text Label) CreateButton(
            RectTransform parent,
            string name,
            string labelText,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var image = CreateImage(parent, name, FcColorTokens.Surface.Row);
            Stretch(image.rectTransform, anchorMin, anchorMax);

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var label = CreateText(image.rectTransform, "Label", labelText, 20, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(label.rectTransform, Vector2.zero, Vector2.one);
            label.color = FcColorTokens.Brand.Cyan;

            return (button, label);
        }

        private static RectTransform CreateEmpty(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            TextAnchor anchor,
            FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = UiFontCatalog.Body;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.fontStyle = style;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
#endif

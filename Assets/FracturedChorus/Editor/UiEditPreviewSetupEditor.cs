#if UNITY_EDITOR
using FracturedChorus.Hub;
using FracturedChorus.Meta;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class UiEditPreviewSetupEditor
    {
        private const string RootName = "UI_EditPreview";

        [MenuItem("Fractured Chorus/Create UI Edit Preview (Layered)")]
        public static void CreateUiEditPreview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "UI Edit Preview",
                    "Exit Play Mode trước khi tạo Edit Preview.",
                    "OK");
                return;
            }

            EnsureSpriteImports();

            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog(
                        "UI Edit Preview",
                        "UI_EditPreview đã tồn tại. Xóa và tạo lại?",
                        "Recreate",
                        "Cancel"))
                {
                    return;
                }

                Undo.DestroyObjectImmediate(existing);
            }

            EnsureEventSystem();

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create UI Edit Preview");

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            var preview = root.AddComponent<UiEditPreviewRoot>();

            var layersFolder = new GameObject("Layers");
            layersFolder.transform.SetParent(root.transform, false);

            var statusHost = new GameObject("00_StatusMenu_Layers", typeof(RectTransform));
            statusHost.transform.SetParent(layersFolder.transform, false);
            StretchFull(statusHost.GetComponent<RectTransform>());

            var calendarHost = new GameObject("01_Calendar_Layers", typeof(RectTransform));
            calendarHost.transform.SetParent(layersFolder.transform, false);
            StretchFull(calendarHost.GetComponent<RectTransform>());

            var statusBuilt = MetaStatusMenuUI.Build(statusHost.transform);
            var calendarBuilt = CalendarOverlayUI.Build(calendarHost.transform);

            var statusGo = statusHost.transform.Find("StatusMenu")?.gameObject;
            var calendarGo = calendarHost.transform.Find("CalendarOverlay")?.gameObject;

            if (statusGo != null)
            {
                RenameLayerChildren(statusGo.transform);
                statusGo.SetActive(true);
            }

            if (calendarGo != null)
            {
                RenameLayerChildren(calendarGo.transform);
                calendarGo.SetActive(true);
            }

            if (statusBuilt.MenuButton != null)
            {
                statusBuilt.MenuButton.gameObject.SetActive(false);
            }

            preview.BindLayerRefs(statusGo, statusBuilt.Menu, calendarGo, calendarBuilt.Overlay);

            if (calendarGo != null && calendarBuilt.Overlay != null)
            {
                var mock = GameMetaState.CreateHubStart();
                mock.Calendar.CurrentDate = new GameDate(9, 12);
                mock.Flags.SetBool(StoryFlagIds.VaultQuestActive, true);
                calendarBuilt.Overlay.EnsureRuntimeBindings();
                calendarBuilt.Overlay.Show(mock);
            }

            preview.SetMode(UiEditPreviewRoot.PreviewMode.Calendar);

            CreateHelpNote(root.transform);

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] Created UI_EditPreview at scene root. Toggle layers in Hierarchy; switch mode on UiEditPreviewRoot.");
        }

        [MenuItem("Fractured Chorus/UI Edit Preview/Show Status Menu Layers")]
        public static void ShowStatusLayers()
        {
            SetPreviewMode(UiEditPreviewRoot.PreviewMode.StatusMenu);
        }

        [MenuItem("Fractured Chorus/UI Edit Preview/Show Calendar Layers")]
        public static void ShowCalendarLayers()
        {
            SetPreviewMode(UiEditPreviewRoot.PreviewMode.Calendar);
        }

        private static void SetPreviewMode(UiEditPreviewRoot.PreviewMode mode)
        {
            var preview = Object.FindFirstObjectByType<UiEditPreviewRoot>();
            if (preview == null)
            {
                EditorUtility.DisplayDialog("UI Edit Preview", "Chưa có UI_EditPreview. Chạy Create UI Edit Preview trước.", "OK");
                return;
            }

            Undo.RecordObject(preview, "Switch UI Edit Preview mode");
            preview.SetMode(mode);
            EditorUtility.SetDirty(preview);
        }

        private static void RenameLayerChildren(Transform root)
        {
            RenameIfExists(root, "Background", "L00_Background");
            RenameIfExists(root, "DateChip", "L01_DateChip");
            RenameIfExists(root, "MenuList", "L02_MenuList");
            RenameIfExists(root, "DetailPanel", "L03_DetailPanel");
            RenameIfExists(root, "Tooltip", "L04_Tooltip");
            RenameIfExists(root, "Prompts", "L05_Prompts");
            RenameIfExists(root, "LeftPanel", "L01_LeftPanel");
            RenameIfExists(root, "DateChipBg", "L05_DateChip");
            RenameIfExists(root, "TodayMarker", "L06_TodayMarker");
            RenameIfExists(root, "CloseButton", "L07_Close");

            var left = root.Find("L01_LeftPanel") ?? root.Find("LeftPanel");
            if (left != null)
            {
                RenameIfExists(left, "Title", "L01a_Title");
                RenameIfExists(left, "Year", "L01b_Year");
                RenameIfExists(left, "MonthBig", "L02a_MonthBig");
                RenameIfExists(left, "MonthNext", "L02b_MonthNext");
                RenameIfExists(left, "MonthArrow", "L02c_MonthArrow");
                RenameIfExists(left, "HintQ", "L02d_HintQ");
                RenameIfExists(left, "HintE", "L02e_HintE");
                RenameIfExists(left, "WeekdayRow", "L03_WeekdayRow");
                RenameIfExists(left, "DayGrid", "L04_DayGrid");
                RenameIfExists(left, "SelectedDayInfo", "L04b_SelectedDayInfo");
            }
        }

        private static void RenameIfExists(Transform parent, string from, string to)
        {
            var child = parent.Find(from);
            if (child != null)
            {
                child.name = to;
            }
        }

        private static void CreateHelpNote(Transform parent)
        {
            var note = new GameObject("_README_EditLayers");
            note.transform.SetParent(parent, false);
            note.hideFlags = HideFlags.None;
            var text = note.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.color = new Color(0.7f, 0.9f, 1f, 0.0f);
            text.raycastTarget = false;
            text.text =
                "UI_EditPreview — bật/tắt từng Lxx_* trong Hierarchy để chỉnh.\n" +
                "Component UiEditPreviewRoot: Mode StatusMenu / Calendar.\n" +
                "Menu: Fractured Chorus/UI Edit Preview/...";
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureSpriteImports()
        {
            var folders = new[]
            {
                "Assets/FracturedChorus/Art/UI/StatusMenu",
                "Assets/FracturedChorus/Resources/UI/StatusMenu",
                "Assets/FracturedChorus/Art/UI/Calendar",
                "Assets/FracturedChorus/Resources/UI/Calendar"
            };

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null)
                    {
                        continue;
                    }

                    var dirty = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        dirty = true;
                    }

                    if (importer.spriteImportMode != SpriteImportMode.Single)
                    {
                        importer.spriteImportMode = SpriteImportMode.Single;
                        dirty = true;
                    }

                    if (dirty)
                    {
                        importer.SaveAndReimport();
                    }
                }
            }
        }
    }
}
#endif

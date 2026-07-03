#if UNITY_EDITOR
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Menu;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class MainMenuStartGameSceneSetupEditor
    {
        private const string ScenePath = "Assets/FracturedChorus/Scenes/MainMenuStartGame.unity";
        internal const string ScenePathForAutoUpgrade = ScenePath;
        private const string AttractSpritePath = "Assets/FracturedChorus/Art/UI/TitleScreen/TitleScreen_Attract_PressAnyButton_v2.png";
        private const string MainMenuSpritePath = "Assets/FracturedChorus/Art/UI/TitleScreen/TitleScreen_MainMenu_Background_v5.png";
        private const string MenuBgmPath = "Assets/FracturedChorus/Audio/Music/Midnight_BGM_Menu.mp3";
        private const string MenuFemaleVoicePath = "Assets/FracturedChorus/Audio/Voice/MainMenu_Female_Voice.mp3";
        private const string MenuMaleVoicePath = "Assets/FracturedChorus/Audio/Voice/MainMenu_Male_Voice.mp3";
        private const string MenuChangeMenuSfxPath = "Assets/FracturedChorus/Audio/SFX/MainMenu_ChangeMenu_Ting.mp3";
        private const string MenuButtonPressSfxPath = "Assets/FracturedChorus/Audio/SFX/MainMenu_ButtonPress.mp3";
        private const string ConfigBackgroundPath = "Assets/FracturedChorus/Art/UI/ConfigMenu/ConfigMenu_Background_v1.png";

        [MenuItem("Fractured Chorus/Create MainMenuStartGame Scene")]
        public static void CreateMainMenuStartGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            ConfigureCamera();
            BuildHierarchy();
            EnsureBuildSettings();

            if (!AssetDatabase.IsValidFolder("Assets/FracturedChorus/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus", "Scenes");
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Fractured Chorus] Saved {ScenePath} — Build index 0. Play to test.");
        }

        [MenuItem("Fractured Chorus/Setup MainMenuStartGame Scene Hierarchy")]
        public static void SetupMainMenuStartGameSceneHierarchy()
        {
            var existing = GameObject.Find("MainMenuStartGameRoot");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog(
                        "Setup MainMenuStartGame",
                        "MainMenuStartGameRoot đã tồn tại. Xóa và tạo lại hierarchy?",
                        "Tạo lại",
                        "Cancel"))
                {
                    return;
                }

                Undo.DestroyObjectImmediate(existing);
            }

            ConfigureCamera();
            BuildHierarchy();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] MainMenuStartGame hierarchy created — Save scene (Ctrl+S).");
        }

        public static void BatchCreateMainMenuStartGameScene()
        {
            if (System.IO.File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var existing = GameObject.Find("MainMenuStartGameRoot");
                if (existing != null)
                {
                    Object.DestroyImmediate(existing);
                }

                ConfigureCamera();
                BuildHierarchy();
                EnsureBuildSettings();
                EditorSceneManager.SaveScene(scene);
            }
            else
            {
                CreateMainMenuStartGameScene();
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Fractured Chorus] MainMenuStartGame batch complete.");
            EditorApplication.Exit(0);
        }

        public static void BatchUpgradeMainMenuStartGameMenuAndAudio()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError($"[Fractured Chorus] Scene not found: {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            UpgradeMainMenuStartGameMenuAndAudio();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Fractured Chorus] MainMenuStartGame menu/audio batch upgrade complete.");
            EditorApplication.Exit(0);
        }

        [MenuItem("Fractured Chorus/Upgrade MainMenuStartGame Menu And Audio")]
        public static void UpgradeMainMenuStartGameMenuAndAudio()
        {
            var root = GameObject.Find("MainMenuStartGameRoot");
            var menuPanel = GameObject.Find("MenuPanel");
            var controller = Object.FindAnyObjectByType<MainMenuStartGameController>();
            var menuController = Object.FindAnyObjectByType<MainMenuStartGameMenuController>();
            var canvas = GameObject.Find("MainMenuCanvas")?.transform;

            if (root == null || menuPanel == null || controller == null || menuController == null || canvas == null)
            {
                EditorUtility.DisplayDialog(
                    "Upgrade MainMenuStartGame",
                    "Mở scene MainMenuStartGame và đảm bảo có MainMenuStartGameRoot / MenuPanel.",
                    "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(root, "Upgrade MainMenuStartGame Menu And Audio");

            EnsureRowHitAreas(menuPanel.transform, menuController);
            EnsureOffBeatArchiveRow(menuPanel.transform, menuController);
            EnsureQuitRow(menuPanel.transform);
            RewireMenuOptions(menuController);

            var archiveOverlay = EnsureOffBeatArchiveOverlay(canvas, controller);
            EnsureMainMenuBgm(root.transform);
            EnsureMainMenuTitleVoice(root.transform);
            EnsureMainMenuTransitionSfx(root.transform);
            EnsureMainMenuButtonPressSfx(root.transform);
            FixRaycastLayers(controller);
            WireOverlayBackButtonsInScene(controller);

            SetSerializedField(controller, "offBeatArchiveOverlay", archiveOverlay);
            SetSerializedField(menuController, "screenController", controller);
            controller.SetEditorPreview(MainMenuStartGameController.MainMenuEditorPreview.MainMenu);

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] Menu buttons, BGM loop, OFF-BEAT ARCHIVE upgraded — Save scene.");
        }

        public static void BatchUpgradeMainMenuStartGameConfigUi()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError($"[Fractured Chorus] Scene not found: {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            UpgradeMainMenuStartGameConfigUi();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Fractured Chorus] MainMenuStartGame config UI batch upgrade complete.");
            EditorApplication.Exit(0);
        }

        [MenuItem("Fractured Chorus/Upgrade MainMenuStartGame Config UI")]
        public static void UpgradeMainMenuStartGameConfigUi()
        {
            var canvas = GameObject.Find("MainMenuCanvas")?.transform;
            var controller = Object.FindAnyObjectByType<MainMenuStartGameController>();
            if (canvas == null || controller == null)
            {
                EditorUtility.DisplayDialog(
                    "Upgrade Config UI",
                    "Mở scene MainMenuStartGame trước.",
                    "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Upgrade MainMenuStartGame Config UI");
            var settingsOverlay = CreateSettingsOverlay(canvas, controller);
            SetSerializedField(controller, "settingsOverlay", settingsOverlay);
            SetSerializedField(controller, "configOverlayController", settingsOverlay.GetComponent<MainMenuConfigOverlayController>());
            WireOverlayBackButtonsInScene(controller);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] Config UI upgraded — Save scene.");
        }

        [MenuItem("Fractured Chorus/Upgrade MainMenuStartGame Layers")]
        public static void UpgradeMainMenuStartGameLayers()
        {
            var canvas = GameObject.Find("MainMenuCanvas")?.transform;
            var legacyLayer = GameObject.Find("MainMenuLayer");
            var background = GameObject.Find("MainMenuBackground") ?? legacyLayer;
            var menuPanel = GameObject.Find("MenuPanel");
            var controller = Object.FindAnyObjectByType<MainMenuStartGameController>();

            if (canvas == null || background == null || menuPanel == null)
            {
                EditorUtility.DisplayDialog(
                    "Upgrade MainMenuStartGame",
                    "Không tìm thấy MainMenuCanvas / MainMenuLayer|Background / MenuPanel. Mở scene MainMenuStartGame trước.",
                    "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Upgrade MainMenuStartGame Layers");
            background.name = "MainMenuBackground";
            menuPanel.transform.SetParent(canvas, false);

            var uiGroup = menuPanel.GetComponent<CanvasGroup>() ?? Undo.AddComponent<CanvasGroup>(menuPanel);
            uiGroup.alpha = 1f;

            if (controller != null)
            {
                SetSerializedField(controller, "mainMenuBackground", background.GetComponent<CanvasGroup>());
                SetSerializedField(controller, "mainMenuUi", uiGroup);
                controller.SetEditorPreview(MainMenuStartGameController.MainMenuEditorPreview.Attract);
                EditorUtility.SetDirty(controller);
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] MainMenuStartGame layers upgraded — MenuPanel tách khỏi background. Save scene.");
        }

        private static void BuildHierarchy()
        {
            EnsureEventSystem();

            var root = new GameObject("MainMenuStartGameRoot");
            Undo.RegisterCreatedObjectUndo(root, "Create MainMenuStartGameRoot");
            var controller = Undo.AddComponent<MainMenuStartGameController>(root);

            var canvas = CreateCanvas(root.transform);
            var attractLayer = CreateBackgroundLayer(canvas.transform, "AttractLayer", AttractSpritePath, active: true);
            var mainMenuBackground = CreateBackgroundLayer(canvas.transform, "MainMenuBackground", MainMenuSpritePath, active: false);
            var menuPanel = CreateMenuPanel(canvas.transform, out var menuController, out var statusText);
            var menuUiGroup = menuPanel.GetComponent<CanvasGroup>();
            var settingsOverlay = CreateSettingsOverlay(canvas.transform, controller);
            var archiveOverlay = CreateOffBeatArchiveOverlay(canvas.transform, controller);
            EnsureMainMenuBgm(root.transform);
            EnsureMainMenuTitleVoice(root.transform);
            EnsureMainMenuTransitionSfx(root.transform);
            EnsureMainMenuButtonPressSfx(root.transform);

            mainMenuBackground.GetComponent<CanvasGroup>().alpha = 1f;
            menuUiGroup.alpha = 1f;

            WireController(controller, attractLayer, mainMenuBackground, menuUiGroup, menuController, settingsOverlay, archiveOverlay);
            controller.SetEditorPreview(MainMenuStartGameController.MainMenuEditorPreview.Attract);

            Selection.activeGameObject = root;
        }

        private static void WireController(
            MainMenuStartGameController controller,
            GameObject attractLayer,
            GameObject mainMenuBackground,
            CanvasGroup mainMenuUi,
            MainMenuStartGameMenuController menuController,
            CanvasGroup settingsOverlay,
            CanvasGroup archiveOverlay)
        {
            SetSerializedField(controller, "attractLayer", attractLayer.GetComponent<CanvasGroup>());
            SetSerializedField(controller, "mainMenuBackground", mainMenuBackground.GetComponent<CanvasGroup>());
            SetSerializedField(controller, "mainMenuUi", mainMenuUi);
            SetSerializedField(controller, "settingsOverlay", settingsOverlay);
            SetSerializedField(controller, "configOverlayController", settingsOverlay.GetComponent<MainMenuConfigOverlayController>());
            SetSerializedField(controller, "offBeatArchiveOverlay", archiveOverlay);
            SetSerializedField(controller, "menuController", menuController);
            SetSerializedField(controller, "transitionDuration", 0.35f);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MenuBgmPath);
            SetSerializedField(controller, "menuBgmClip", clip);
            SetSerializedField(menuController, "screenController", controller);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(menuController);
        }

        private static void ConfigureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            cam.backgroundColor = new Color(0.04f, 0.04f, 0.1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.orthographic = true;
        }

        private static void EnsureEventSystem()
        {
            CombatInputSetup.EnsureEventSystem();
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                CombatInputSetup.ApplyInputModule(eventSystem.gameObject, destroyImmediate: true);
            }
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            var canvasGo = new GameObject("MainMenuCanvas");
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create MainMenuCanvas");
            canvasGo.transform.SetParent(parent, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static GameObject CreateBackgroundLayer(Transform canvas, string layerName, string spritePath, bool active)
        {
            var layerGo = CreateUiObject(layerName, canvas);
            layerGo.SetActive(active);
            StretchRect(layerGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var group = layerGo.AddComponent<CanvasGroup>();
            var image = layerGo.AddComponent<Image>();
            image.sprite = LoadSprite(spritePath);
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = false;
            group.alpha = 1f;
            return layerGo;
        }

        private static GameObject CreateMenuPanel(
            Transform parent,
            out MainMenuStartGameMenuController menuController,
            out Text statusText)
        {
            var panelGo = CreateUiObject("MenuPanel", parent);
            StretchRect(panelGo, new Vector2(0.62f, 0.06f), new Vector2(0.96f, 0.52f), Vector2.zero, Vector2.zero);
            panelGo.AddComponent<CanvasGroup>();

            var layout = panelGo.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.LowerRight;
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(0, 0, 0, 8);

            var highlightGo = CreateUiObject("HighlightBar", panelGo.transform);
            var highlightImage = highlightGo.AddComponent<Image>();
            highlightImage.color = new Color(0.102f, 0.227f, 0.361f, 0.95f);
            highlightImage.raycastTarget = false;
            var highlightRect = highlightGo.GetComponent<RectTransform>();
            highlightRect.sizeDelta = new Vector2(0f, 44f);
            highlightGo.SetActive(true);

            var rowNew = CreateMenuRow(panelGo.transform, "NEW GAME", true);
            var rowLoad = CreateMenuRow(panelGo.transform, "LOAD GAME", false);
            var rowArchive = CreateMenuRow(panelGo.transform, "OFF-BEAT ARCHIVE", true);
            var rowConfig = CreateMenuRow(panelGo.transform, "CONFIG", true);
            var rowQuit = CreateMenuRow(panelGo.transform, "QUIT", true);

            var statusGo = CreateUiObject("StatusText", panelGo.transform);
            statusText = statusGo.AddComponent<Text>();
            statusText.text = string.Empty;
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 16;
            statusText.alignment = TextAnchor.LowerRight;
            statusText.color = new Color(0.75f, 0.8f, 0.85f, 0.85f);
            var statusLayout = statusGo.AddComponent<LayoutElement>();
            statusLayout.preferredHeight = 28f;
            statusLayout.flexibleWidth = 1f;

            menuController = panelGo.AddComponent<MainMenuStartGameMenuController>();
            var rows = new System.Collections.Generic.List<(RectTransform row, Button button, Text label, MainMenuButtonRowView rowView, Image hitArea)>
            {
                rowNew,
                rowLoad,
                rowArchive,
                rowConfig,
                rowQuit
            };
            SetMenuOptionsFromRows(menuController, highlightRect, rows, statusText);
            highlightRect.SetParent(rowNew.row, false);
            StretchRect(highlightGo, Vector2.zero, Vector2.one, new Vector2(-12f, -4f), new Vector2(12f, 4f));
            highlightRect.SetAsFirstSibling();

            return panelGo;
        }

        private static (RectTransform row, Button button, Text label, MainMenuButtonRowView rowView, Image hitArea) CreateMenuRow(
            Transform parent,
            string label,
            bool interactable)
        {
            var rowGo = CreateUiObject($"Row_{label.Replace(' ', '_')}", parent);
            var layout = rowGo.AddComponent<LayoutElement>();
            layout.preferredHeight = 44f;
            layout.flexibleWidth = 1f;

            var rowRect = rowGo.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);

            var hitGo = CreateUiObject("HitArea", rowGo.transform);
            StretchRect(hitGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var hitImage = hitGo.AddComponent<Image>();
            hitImage.color = new Color(1f, 1f, 1f, 0.001f);
            hitImage.raycastTarget = true;

            var button = rowGo.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.interactable = interactable;
            button.targetGraphic = hitImage;

            var labelGo = CreateUiObject("Label", rowGo.transform);
            StretchRect(labelGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var text = labelGo.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleRight;
            text.color = interactable ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            text.raycastTarget = false;

            var rowView = rowGo.AddComponent<MainMenuButtonRowView>();

            return (rowRect, button, text, rowView, hitImage);
        }

        private static void SetMenuOptionsFromRows(
            MainMenuStartGameMenuController menuController,
            RectTransform highlightBar,
            System.Collections.Generic.List<(RectTransform row, Button button, Text label, MainMenuButtonRowView rowView, Image hitArea)> rows,
            Text statusText)
        {
            SetSerializedField(menuController, "highlightBar", highlightBar);
            SetSerializedField(menuController, "statusText", statusText);

            var so = new SerializedObject(menuController);
            var optionsProp = so.FindProperty("options");
            optionsProp.arraySize = rows.Count;

            for (var i = 0; i < rows.Count; i++)
            {
                var rowData = rows[i];
                var labelText = rowData.label != null ? rowData.label.text : string.Empty;
                WriteMenuOption(
                    optionsProp.GetArrayElementAtIndex(i),
                    rowData,
                    ResolveMenuActionIndex(labelText),
                    IsMenuRowInteractable(labelText));
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            for (var i = 0; i < rows.Count; i++)
            {
                var rowData = rows[i];
                var labelText = rowData.label != null ? rowData.label.text : string.Empty;
                rowData.rowView.Configure(
                    menuController,
                    i,
                    rowData.label,
                    rowData.hitArea,
                    IsMenuRowInteractable(labelText));
            }

            EditorUtility.SetDirty(menuController);
        }

        private static int ResolveMenuActionIndex(string labelText)
        {
            switch (labelText)
            {
                case "NEW GAME":
                    return 0;
                case "LOAD GAME":
                    return 1;
                case "OFF-BEAT ARCHIVE":
                    return 2;
                case "CONFIG":
                    return 3;
                case "QUIT":
                    return 4;
                default:
                    return 0;
            }
        }

        private static bool IsMenuRowInteractable(string labelText)
        {
            return labelText != "LOAD GAME";
        }

        private static void SetMenuOptions(
            MainMenuStartGameMenuController menuController,
            RectTransform highlightBar,
            (RectTransform row, Button button, Text label, MainMenuButtonRowView rowView, Image hitArea) rowNew,
            (RectTransform row, Button button, Text label, MainMenuButtonRowView rowView, Image hitArea) rowLoad,
            (RectTransform row, Button button, Text label, MainMenuButtonRowView rowView, Image hitArea) rowArchive,
            (RectTransform row, Button button, Text label, MainMenuButtonRowView rowView, Image hitArea) rowConfig,
            Text statusText)
        {
            SetMenuOptionsFromRows(
                menuController,
                highlightBar,
                new System.Collections.Generic.List<(RectTransform, Button, Text, MainMenuButtonRowView, Image)>
                {
                    rowNew,
                    rowLoad,
                    rowArchive,
                    rowConfig
                },
                statusText);
        }

        private static void SetMenuOptions(
            MainMenuStartGameMenuController menuController,
            RectTransform highlightBar,
            (RectTransform row, Button button, Text label, MainMenuButtonRowView rowView, Image hitArea) rowNew,
            (RectTransform row, Button button, Text label, MainMenuButtonRowView rowView, Image hitArea) rowLoad,
            (RectTransform row, Button button, Text label, MainMenuButtonRowView rowView, Image hitArea) rowArchive,
            (RectTransform row, Button button, Text label, MainMenuButtonRowView rowView, Image hitArea) rowConfig,
            (RectTransform row, Button button, Text label, MainMenuButtonRowView rowView, Image hitArea) rowQuit,
            Text statusText)
        {
            SetMenuOptionsFromRows(
                menuController,
                highlightBar,
                new System.Collections.Generic.List<(RectTransform, Button, Text, MainMenuButtonRowView, Image)>
                {
                    rowNew,
                    rowLoad,
                    rowArchive,
                    rowConfig,
                    rowQuit
                },
                statusText);
        }

        private static void WriteMenuOption(
            SerializedProperty element,
            (RectTransform row, Button button, Text label, MainMenuButtonRowView rowView, Image hitArea) rowData,
            int actionIndex,
            bool interactable)
        {
            element.FindPropertyRelative("row").objectReferenceValue = rowData.row;
            element.FindPropertyRelative("button").objectReferenceValue = rowData.button;
            element.FindPropertyRelative("label").objectReferenceValue = rowData.label;
            element.FindPropertyRelative("rowView").objectReferenceValue = rowData.rowView;
            element.FindPropertyRelative("action").enumValueIndex = actionIndex;
            element.FindPropertyRelative("interactable").boolValue = interactable;
        }

        private static CanvasGroup CreateOffBeatArchiveOverlay(Transform canvas, MainMenuStartGameController controller)
        {
            var existing = canvas.Find("OffBeatArchiveOverlay");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var overlayGo = CreateUiObject("OffBeatArchiveOverlay", canvas);
            StretchRect(overlayGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var group = overlayGo.AddComponent<CanvasGroup>();
            var dim = overlayGo.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            dim.raycastTarget = true;

            var panelGo = CreateUiObject("ArchivePanel", overlayGo.transform);
            StretchRect(panelGo, new Vector2(0.22f, 0.18f), new Vector2(0.78f, 0.82f), Vector2.zero, Vector2.zero);
            var panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.12f, 0.18f, 0.95f);

            var title = CreateText("Title", panelGo.transform, "OFF-BEAT ARCHIVE", 32, TextAnchor.UpperCenter);
            StretchRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -56f), new Vector2(-16f, -8f));

            var body = CreateText("Body", panelGo.transform,
                "▶ Midnight (Menu)\n▶ Eternal Spark — Cadence Remix\n\nStub playlist — Phase 1",
                20,
                TextAnchor.UpperLeft);
            StretchRect(body.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(24f, 64f), new Vector2(-24f, -72f));

            var backGo = CreateUiObject("Btn_Back", panelGo.transform);
            StretchRect(backGo, new Vector2(0.25f, 0f), new Vector2(0.75f, 0f), new Vector2(0f, 16f), new Vector2(0f, 56f));
            var backImage = backGo.AddComponent<Image>();
            backImage.color = new Color(0.42f, 0.55f, 0.75f, 1f);
            var backButton = backGo.AddComponent<Button>();
            backButton.targetGraphic = backImage;
            BindPersistentBackButton(backButton, controller, controller.HideOffBeatArchive);
            var backLabel = CreateText("Label", backGo.transform, "BACK", 22, TextAnchor.MiddleCenter);
            StretchRect(backLabel.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            overlayGo.SetActive(false);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }

        private static CanvasGroup EnsureOffBeatArchiveOverlay(Transform canvas, MainMenuStartGameController controller)
        {
            var existing = canvas.Find("OffBeatArchiveOverlay");
            if (existing != null)
            {
                var group = existing.GetComponent<CanvasGroup>();
                WireOverlayBackButtonInHierarchy(existing, controller, controller.HideOffBeatArchive);
                return group;
            }

            return CreateOffBeatArchiveOverlay(canvas, controller);
        }

        private static void EnsureMainMenuBgm(Transform root)
        {
            var existing = root.Find("MainMenuBgm");
            if (existing == null)
            {
                var bgmGo = new GameObject("MainMenuBgm");
                Undo.RegisterCreatedObjectUndo(bgmGo, "Create MainMenuBgm");
                bgmGo.transform.SetParent(root, false);
                existing = bgmGo.transform;
                bgmGo.AddComponent<AudioSource>();
                bgmGo.AddComponent<MainMenuBgmController>();
            }

            var bgm = existing.GetComponent<MainMenuBgmController>();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MenuBgmPath);
            SetSerializedField(bgm, "menuClip", clip);
            SetSerializedField(bgm, "volume", 0.65f);
            var controller = root.GetComponent<MainMenuStartGameController>();
            if (controller != null)
            {
                SetSerializedField(controller, "menuBgmClip", clip);
                EditorUtility.SetDirty(controller);
            }

            EditorUtility.SetDirty(bgm);
        }

        private static void EnsureMainMenuTitleVoice(Transform root)
        {
            var existing = root.Find("MainMenuTitleVoice");
            if (existing == null)
            {
                var voiceGo = new GameObject("MainMenuTitleVoice");
                Undo.RegisterCreatedObjectUndo(voiceGo, "Create MainMenuTitleVoice");
                voiceGo.transform.SetParent(root, false);
                existing = voiceGo.transform;
                voiceGo.AddComponent<AudioSource>();
                voiceGo.AddComponent<MainMenuTitleVoiceController>();
            }

            var voice = existing.GetComponent<MainMenuTitleVoiceController>();
            var femaleClip = AssetDatabase.LoadAssetAtPath<AudioClip>(MenuFemaleVoicePath);
            var maleClip = AssetDatabase.LoadAssetAtPath<AudioClip>(MenuMaleVoicePath);
            SetSerializedField(voice, "femaleVoiceClip", femaleClip);
            SetSerializedField(voice, "maleVoiceClip", maleClip);
            SetSerializedField(voice, "volume", 1f);

            var controller = root.GetComponent<MainMenuStartGameController>();
            if (controller != null)
            {
                SetSerializedField(controller, "menuFemaleVoiceClip", femaleClip);
                SetSerializedField(controller, "menuMaleVoiceClip", maleClip);
                EditorUtility.SetDirty(controller);
            }

            EditorUtility.SetDirty(voice);
        }

        private static void EnsureMainMenuTransitionSfx(Transform root)
        {
            var existing = root.Find("MainMenuTransitionSfx");
            if (existing == null)
            {
                var sfxGo = new GameObject("MainMenuTransitionSfx");
                Undo.RegisterCreatedObjectUndo(sfxGo, "Create MainMenuTransitionSfx");
                sfxGo.transform.SetParent(root, false);
                existing = sfxGo.transform;
                sfxGo.AddComponent<AudioSource>();
                sfxGo.AddComponent<MainMenuTransitionSfxController>();
            }

            var sfx = existing.GetComponent<MainMenuTransitionSfxController>();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MenuChangeMenuSfxPath);
            SetSerializedField(sfx, "changeMenuClip", clip);
            SetSerializedField(sfx, "volume", 1.45f);

            var controller = root.GetComponent<MainMenuStartGameController>();
            if (controller != null)
            {
                SetSerializedField(controller, "changeMenuSfxClip", clip);
                SetSerializedField(controller, "changeMenuSfxVolume", 1.45f);
                SetSerializedField(controller, "bgmLeadInSeconds", 0.45f);
                SetSerializedField(controller, "bgmLeadInProgress", 0.48f);
                SetSerializedField(controller, "menuTransitionFadeScale", 0.55f);
                SetSerializedField(controller, "bgmDuckMultiplier", 0.28f);
                EditorUtility.SetDirty(controller);
            }

            EditorUtility.SetDirty(sfx);
        }

        private static void EnsureMainMenuButtonPressSfx(Transform root)
        {
            var existing = root.Find("MainMenuButtonPressSfx");
            if (existing == null)
            {
                var sfxGo = new GameObject("MainMenuButtonPressSfx");
                Undo.RegisterCreatedObjectUndo(sfxGo, "Create MainMenuButtonPressSfx");
                sfxGo.transform.SetParent(root, false);
                existing = sfxGo.transform;
                sfxGo.AddComponent<AudioSource>();
                sfxGo.AddComponent<MainMenuButtonPressSfxController>();
            }

            var sfx = existing.GetComponent<MainMenuButtonPressSfxController>();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MenuButtonPressSfxPath);
            SetSerializedField(sfx, "buttonPressClip", clip);
            SetSerializedField(sfx, "volume", 1f);

            var controller = root.GetComponent<MainMenuStartGameController>();
            if (controller != null)
            {
                SetSerializedField(controller, "buttonPressSfxClip", clip);
                SetSerializedField(controller, "buttonPressSfxVolume", 1f);
                EditorUtility.SetDirty(controller);
            }

            EditorUtility.SetDirty(sfx);
        }

        private static void EnsureRowHitAreas(Transform menuPanel, MainMenuStartGameMenuController menuController)
        {
            var index = 0;
            foreach (Transform child in menuPanel)
            {
                if (!child.name.StartsWith("Row_"))
                {
                    continue;
                }

                var button = child.GetComponent<Button>();
                if (button == null)
                {
                    continue;
                }

                var hit = child.Find("HitArea")?.GetComponent<Image>();
                if (hit == null)
                {
                    var hitGo = CreateUiObject("HitArea", child);
                    StretchRect(hitGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    hit = hitGo.AddComponent<Image>();
                    hit.color = new Color(1f, 1f, 1f, 0.001f);
                    hit.raycastTarget = true;
                    hitGo.transform.SetAsFirstSibling();
                }

                button.targetGraphic = hit;
                button.transition = Selectable.Transition.None;

                var label = child.Find("Label")?.GetComponent<Text>();
                if (label != null)
                {
                    label.raycastTarget = false;
                }

                var rowView = child.GetComponent<MainMenuButtonRowView>() ?? child.gameObject.AddComponent<MainMenuButtonRowView>();
                rowView.Configure(menuController, index, label, hit, button.interactable);
                index++;
            }
        }

        private static void EnsureOffBeatArchiveRow(Transform menuPanel, MainMenuStartGameMenuController menuController)
        {
            if (menuPanel.Find("Row_OFF-BEAT_ARCHIVE") != null || menuPanel.Find("Row_OFF_BEAT_ARCHIVE") != null)
            {
                return;
            }

            var configRow = menuPanel.Find("Row_CONFIG");
            var archiveRow = CreateMenuRow(menuPanel, "OFF-BEAT ARCHIVE", true);
            if (configRow != null)
            {
                archiveRow.row.SetSiblingIndex(configRow.GetSiblingIndex());
            }

            var highlight = menuPanel.Find("HighlightBar");
            if (highlight != null)
            {
                highlight.SetAsFirstSibling();
            }
        }

        private static void EnsureQuitRow(Transform menuPanel)
        {
            if (menuPanel.Find("Row_QUIT") != null)
            {
                return;
            }

            CreateMenuRow(menuPanel, "QUIT", true);
        }

        private static void RewireMenuOptions(MainMenuStartGameMenuController menuController)
        {
            var panel = menuController.transform;
            var highlight = panel.Find("HighlightBar") as RectTransform;
            var status = panel.Find("StatusText")?.GetComponent<Text>();

            var rows = new System.Collections.Generic.List<(RectTransform row, Button button, Text label, MainMenuButtonRowView rowView, Image hitArea)>();
            foreach (Transform child in panel)
            {
                if (!child.name.StartsWith("Row_"))
                {
                    continue;
                }

                var button = child.GetComponent<Button>();
                var label = child.Find("Label")?.GetComponent<Text>();
                var hit = child.Find("HitArea")?.GetComponent<Image>() ?? child.GetComponent<Image>();
                var rowView = child.GetComponent<MainMenuButtonRowView>();
                if (button == null || label == null)
                {
                    continue;
                }

                rows.Add((child as RectTransform, button, label, rowView, hit));
            }

            if (rows.Count < 4)
            {
                return;
            }

            SetMenuOptionsFromRows(menuController, highlight, rows, status);
        }

        private static void FixRaycastLayers(MainMenuStartGameController controller)
        {
            var so = new SerializedObject(controller);
            var attract = so.FindProperty("attractLayer").objectReferenceValue as CanvasGroup;
            var bg = so.FindProperty("mainMenuBackground").objectReferenceValue as CanvasGroup;
            if (attract != null)
            {
                attract.blocksRaycasts = false;
                attract.interactable = false;
            }

            if (bg != null)
            {
                bg.blocksRaycasts = false;
                bg.interactable = false;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static CanvasGroup CreateSettingsOverlay(Transform canvas, MainMenuStartGameController controller)
        {
            var existing = canvas.Find("SettingsOverlay");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var overlayGo = CreateUiObject("SettingsOverlay", canvas);
            StretchRect(overlayGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var group = overlayGo.AddComponent<CanvasGroup>();
            var blocker = overlayGo.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.001f);
            blocker.raycastTarget = true;

            var bgGo = CreateUiObject("ConfigBackground", overlayGo.transform);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = new Vector2(0f, 72f);
            bgRect.offsetMax = Vector2.zero;
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.sprite = LoadSprite(ConfigBackgroundPath);
            bgImage.preserveAspect = false;
            bgImage.raycastTarget = false;
            bgImage.color = Color.white;

            var uiRootGo = CreateUiObject("ConfigUiRoot", overlayGo.transform);
            StretchRect(uiRootGo, new Vector2(0.04f, 0.16f), new Vector2(0.44f, 0.84f), Vector2.zero, Vector2.zero);

            var infoGo = CreateUiObject("InfoText", uiRootGo.transform);
            var infoText = infoGo.AddComponent<Text>();
            infoText.text = "Adjust master volume for menu and game audio.";
            infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            infoText.fontSize = 18;
            infoText.alignment = TextAnchor.UpperLeft;
            infoText.color = new Color(0.88f, 0.92f, 0.96f, 0.95f);
            infoText.raycastTarget = false;
            StretchRect(infoGo, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -52f), new Vector2(0f, 0f));

            var listGo = CreateUiObject("ConfigList", uiRootGo.transform);
            StretchRect(listGo, new Vector2(0f, 0.12f), new Vector2(1f, 0.88f), Vector2.zero, Vector2.zero);
            var listLayout = listGo.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 10f;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.padding = new RectOffset(0, 0, 8, 8);

            var highlightGo = CreateUiObject("HighlightBar", listGo.transform);
            var highlightImage = highlightGo.AddComponent<Image>();
            highlightImage.color = Color.white;
            highlightImage.raycastTarget = false;
            var highlightRect = highlightGo.GetComponent<RectTransform>();
            highlightRect.sizeDelta = new Vector2(0f, 44f);
            CreateHighlightBorder(highlightGo.transform, true);
            CreateHighlightBorder(highlightGo.transform, false);

            var rowVolume = CreateConfigSliderRow(listGo.transform, "Volume", out var volumeSlider);
            var rowBrightness = CreateConfigSliderRow(listGo.transform, "Background Brightness", out var brightnessSlider);
            var rowDifficulty = CreateConfigDifficultyRow(listGo.transform, out var difficultyValue, out var diffPrev, out var diffNext);

            var footerGo = CreateUiObject("Footer", uiRootGo.transform);
            StretchRect(footerGo, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 44f));
            var footerText = CreateText("Hints", footerGo.transform, "←→ Difficulty   ·   ESC Back", 16, TextAnchor.MiddleLeft);
            StretchRect(footerText.gameObject, new Vector2(0f, 0f), new Vector2(0.72f, 1f), Vector2.zero, Vector2.zero);

            var backGo = CreateUiObject("Btn_Back", footerGo.transform);
            StretchRect(backGo, new Vector2(0.72f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            var backImage = backGo.AddComponent<Image>();
            backImage.color = new Color(0.18f, 0.28f, 0.42f, 0.92f);
            var backButton = backGo.AddComponent<Button>();
            backButton.targetGraphic = backImage;
            BindPersistentBackButton(backButton, controller, controller.HideSettings);
            var backLabel = CreateText("Label", backGo.transform, "BACK", 20, TextAnchor.MiddleCenter);
            StretchRect(backLabel.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var configController = overlayGo.AddComponent<MainMenuConfigOverlayController>();
            var so = new SerializedObject(configController);
            so.FindProperty("screenController").objectReferenceValue = controller;
            so.FindProperty("highlightBar").objectReferenceValue = highlightRect;
            so.FindProperty("infoText").objectReferenceValue = infoText;
            so.FindProperty("volumeSlider").objectReferenceValue = volumeSlider;
            so.FindProperty("brightnessSlider").objectReferenceValue = brightnessSlider;
            so.FindProperty("difficultyValueText").objectReferenceValue = difficultyValue;
            so.FindProperty("difficultyPrevButton").objectReferenceValue = diffPrev;
            so.FindProperty("difficultyNextButton").objectReferenceValue = diffNext;
            var rowsProp = so.FindProperty("rows");
            rowsProp.arraySize = 3;
            WriteConfigRow(rowsProp.GetArrayElementAtIndex(0), rowVolume.row, rowVolume.label);
            WriteConfigRow(rowsProp.GetArrayElementAtIndex(1), rowBrightness.row, rowBrightness.label);
            WriteConfigRow(rowsProp.GetArrayElementAtIndex(2), rowDifficulty.row, rowDifficulty.label);
            so.ApplyModifiedPropertiesWithoutUndo();

            highlightRect.SetParent(rowVolume.row, false);
            StretchRect(highlightGo, Vector2.zero, Vector2.one, new Vector2(-8f, -3f), new Vector2(8f, 3f));
            highlightRect.SetAsFirstSibling();

            SetSerializedField(controller, "configOverlayController", configController);

            overlayGo.SetActive(false);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }

        private static void CreateHighlightBorder(Transform parent, bool top)
        {
            var borderGo = CreateUiObject(top ? "BorderTop" : "BorderBottom", parent);
            var borderImage = borderGo.AddComponent<Image>();
            borderImage.color = new Color(0.82f, 0.12f, 0.18f, 1f);
            borderImage.raycastTarget = false;
            var rect = borderGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rect.sizeDelta = new Vector2(0f, 2f);
            rect.anchoredPosition = Vector2.zero;
        }

        private static (RectTransform row, Text label) CreateConfigSliderRow(
            Transform parent,
            string labelText,
            out Slider slider)
        {
            var rowGo = CreateUiObject($"Row_{labelText.Replace(' ', '_')}", parent);
            var layout = rowGo.AddComponent<LayoutElement>();
            layout.preferredHeight = 44f;
            layout.flexibleWidth = 1f;

            var rowLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;
            rowLayout.padding = new RectOffset(12, 12, 0, 0);

            var label = CreateText("Label", rowGo.transform, labelText, 22, TextAnchor.MiddleLeft);
            var labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 210f;
            labelLayout.flexibleWidth = 0f;
            label.color = Color.white;
            label.fontStyle = FontStyle.Bold;

            slider = CreateStyledSlider(rowGo.transform);
            var sliderLayout = slider.GetComponent<LayoutElement>() ?? slider.gameObject.AddComponent<LayoutElement>();
            sliderLayout.flexibleWidth = 1f;
            sliderLayout.preferredHeight = 28f;

            return (rowGo.GetComponent<RectTransform>(), label);
        }

        private static (RectTransform row, Text label) CreateConfigDifficultyRow(
            Transform parent,
            out Text valueText,
            out Button prevButton,
            out Button nextButton)
        {
            var rowGo = CreateUiObject("Row_Difficulty", parent);
            var layout = rowGo.AddComponent<LayoutElement>();
            layout.preferredHeight = 44f;

            var rowLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.padding = new RectOffset(12, 12, 0, 0);

            var label = CreateText("Label", rowGo.transform, "Difficulty", 22, TextAnchor.MiddleLeft);
            var labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 210f;
            label.color = Color.white;
            label.fontStyle = FontStyle.Bold;

            prevButton = CreateSmallButton(rowGo.transform, "Lt", 36f);
            var prevLabel = prevButton.transform.Find("Label")?.GetComponent<Text>();
            if (prevLabel != null)
            {
                prevLabel.text = "<";
            }

            valueText = CreateText("Value", rowGo.transform, "CADENCE", 22, TextAnchor.MiddleCenter);
            var valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
            valueLayout.flexibleWidth = 1f;
            valueText.color = new Color(0.55f, 0.85f, 1f, 1f);
            valueText.fontStyle = FontStyle.Bold;

            nextButton = CreateSmallButton(rowGo.transform, "Gt", 36f);
            var nextLabel = nextButton.transform.Find("Label")?.GetComponent<Text>();
            if (nextLabel != null)
            {
                nextLabel.text = ">";
            }

            return (rowGo.GetComponent<RectTransform>(), label);
        }

        private static Button CreateSmallButton(Transform parent, string name, float width)
        {
            var go = CreateUiObject(name, parent);
            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = 32f;
            var image = go.AddComponent<Image>();
            image.color = new Color(0.12f, 0.18f, 0.28f, 0.95f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var label = CreateText("Label", go.transform, name, 20, TextAnchor.MiddleCenter);
            StretchRect(label.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static Slider CreateStyledSlider(Transform parent)
        {
            var sliderGo = CreateUiObject("Slider", parent);
            var slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.85f;

            var bgGo = CreateUiObject("Background", sliderGo.transform);
            StretchRect(bgGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0.06f, 0.1f, 0.18f, 0.95f);

            var fillAreaGo = CreateUiObject("Fill Area", sliderGo.transform);
            StretchRect(fillAreaGo, Vector2.zero, Vector2.one, new Vector2(8f, 6f), new Vector2(-8f, -6f));
            var fillGo = CreateUiObject("Fill", fillAreaGo.transform);
            StretchRect(fillGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fillImage = fillGo.AddComponent<Image>();
            fillImage.color = new Color(0.35f, 0.78f, 0.95f, 1f);

            var handleAreaGo = CreateUiObject("Handle Slide Area", sliderGo.transform);
            StretchRect(handleAreaGo, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
            var handleGo = CreateUiObject("Handle", handleAreaGo.transform);
            var handleRect = handleGo.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(14f, 24f);
            var handleImage = handleGo.AddComponent<Image>();
            handleImage.color = Color.white;

            slider.fillRect = fillGo.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            return slider;
        }

        private static void WriteConfigRow(SerializedProperty element, RectTransform row, Text label)
        {
            element.FindPropertyRelative("row").objectReferenceValue = row;
            element.FindPropertyRelative("label").objectReferenceValue = label;
        }

        private static void WireOverlayBackButtonsInScene(MainMenuStartGameController controller)
        {
            var settings = GameObject.Find("SettingsOverlay")?.transform;
            if (settings != null)
            {
                WireOverlayBackButtonInHierarchy(settings, controller, controller.HideSettings);
            }

            var archive = GameObject.Find("OffBeatArchiveOverlay")?.transform;
            if (archive != null)
            {
                WireOverlayBackButtonInHierarchy(archive, controller, controller.HideOffBeatArchive);
            }
        }

        private static void WireOverlayBackButtonInHierarchy(
            Transform overlayRoot,
            MainMenuStartGameController controller,
            UnityEngine.Events.UnityAction handler)
        {
            foreach (var button in overlayRoot.GetComponentsInChildren<Button>(true))
            {
                if (button.gameObject.name != "Btn_Back")
                {
                    continue;
                }

                BindPersistentBackButton(button, controller, handler);
                return;
            }
        }

        private static void BindPersistentBackButton(
            Button button,
            MainMenuStartGameController controller,
            UnityEngine.Events.UnityAction handler)
        {
            for (var i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            }

            UnityEventTools.AddPersistentListener(button.onClick, handler);
            EditorUtility.SetDirty(button);
        }

        private static void EnsureBuildSettings()
        {
            var scenes = new[]
            {
                ScenePath,
                "Assets/FracturedChorus/Scenes/PrologueVN.unity",
                "Assets/FracturedChorus/Scenes/RunMapPrototype.unity",
                "Assets/FracturedChorus/Scenes/CombatPrototype.unity"
            };

            var buildScenes = new EditorBuildSettingsScene[scenes.Length];
            for (var i = 0; i < scenes.Length; i++)
            {
                buildScenes[i] = new EditorBuildSettingsScene(scenes[i], true);
            }

            EditorBuildSettings.scenes = buildScenes;
        }

        private static Sprite LoadSprite(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                return sprite;
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var asset in assets)
            {
                if (asset is Sprite found)
                {
                    return found;
                }
            }

            Debug.LogWarning($"[Fractured Chorus] Sprite not found: {assetPath}");
            return null;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor anchor)
        {
            var go = CreateUiObject(name, parent);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        private static void StretchRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetSerializedField(Object target, string fieldName, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                return;
            }

            switch (value)
            {
                case bool boolValue:
                    prop.boolValue = boolValue;
                    break;
                case Object objValue:
                    prop.objectReferenceValue = objValue;
                    break;
                case float floatValue:
                    prop.floatValue = floatValue;
                    break;
                case int intValue:
                    prop.intValue = intValue;
                    break;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }

    [InitializeOnLoad]
    internal static class MainMenuStartGameAutoUpgrade
    {
        private const string SessionKey = "FC_MainMenuStartGame_MenuAudio_v4";

        static MainMenuStartGameAutoUpgrade()
        {
            EditorApplication.delayCall += TryUpgradeActiveScene;
        }

        private static void TryUpgradeActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != MainMenuStartGameSceneSetupEditor.ScenePathForAutoUpgrade)
            {
                return;
            }

            if (GameObject.Find("MenuPanel")?.transform.Find("Row_OFF-BEAT_ARCHIVE") != null &&
                GameObject.Find("MenuPanel")?.transform.Find("Row_QUIT") != null &&
                GameObject.Find("MainMenuBgm") != null &&
                GameObject.Find("MainMenuTitleVoice") != null &&
                GameObject.Find("MainMenuTransitionSfx") != null &&
                GameObject.Find("MainMenuButtonPressSfx") != null &&
                GameObject.Find("OffBeatArchiveOverlay") != null)
            {
                SessionState.SetBool(SessionKey, true);
                return;
            }

            MainMenuStartGameSceneSetupEditor.UpgradeMainMenuStartGameMenuAndAudio();
            EditorSceneManager.SaveOpenScenes();
            SessionState.SetBool(SessionKey, true);
        }
    }

    [InitializeOnLoad]
    internal static class MainMenuStartGameConfigUiAutoUpgrade
    {
        private const string SessionKey = "FC_MainMenuStartGame_ConfigUi_v1";

        static MainMenuStartGameConfigUiAutoUpgrade()
        {
            EditorApplication.delayCall += TryUpgradeActiveScene;
        }

        private static void TryUpgradeActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != MainMenuStartGameSceneSetupEditor.ScenePathForAutoUpgrade)
            {
                return;
            }

            var settings = GameObject.Find("SettingsOverlay");
            if (settings != null && settings.transform.Find("ConfigBackground") != null &&
                settings.GetComponent<MainMenuConfigOverlayController>() != null)
            {
                SessionState.SetBool(SessionKey, true);
                return;
            }

            MainMenuStartGameSceneSetupEditor.UpgradeMainMenuStartGameConfigUi();
            EditorSceneManager.SaveOpenScenes();
            SessionState.SetBool(SessionKey, true);
        }
    }
}
#endif

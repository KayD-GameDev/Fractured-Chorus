#if UNITY_EDITOR
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Menu;
using FracturedChorus.UI;
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
        private const string AttractSpritePath = "Assets/FracturedChorus/Art/UI/TitleScreen/TitleScreen_Attract_NoUI_v1.png";
        private const string MainMenuSpritePath = "Assets/FracturedChorus/Art/UI/TitleScreen/TitleScreen_MainMenu_Env_v1.png";
        private const string MenuBgmPath = "Assets/FracturedChorus/Audio/Music/Midnight_BGM_Menu.mp3";
        private const string MenuFemaleVoicePath = "Assets/FracturedChorus/Audio/Voice/MainMenu_Female_Voice.mp3";
        private const string MenuMaleVoicePath = "Assets/FracturedChorus/Audio/Voice/MainMenu_Male_Voice.mp3";
        private const string MenuChangeMenuSfxPath = "Assets/FracturedChorus/Audio/SFX/MainMenu_ChangeMenu_Ting.mp3";
        private const string MenuButtonPressSfxPath = "Assets/FracturedChorus/Audio/SFX/MainMenu_ButtonPress.wav";
        private const string ConfigBackgroundPath = "Assets/FracturedChorus/Art/UI/ConfigMenu/config_bg_memory_hall_v1.png";
        private const string ConfigRenPosePath = "Assets/FracturedChorus/Art/Characters/Ren/School/ren_config_pose_fx_v1.png";

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
                        "MainMenuStartGameRoot already exists. Delete and recreate hierarchy?",
                        "Recreate",
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
                    "Open the MainMenuStartGame scene and ensure MainMenuStartGameRoot / MenuPanel exist.",
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
            SetSerializedField(controller, "offBeatArchiveController",
                archiveOverlay != null ? archiveOverlay.GetComponent<OffBeatArchiveController>() : null);
            SetSerializedField(menuController, "screenController", controller);
            controller.SetEditorPreview(MainMenuStartGameController.MainMenuEditorPreview.MainMenu);

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] Menu buttons, BGM loop, OFF-BEAT ARCHIVE upgraded — Save scene.");
        }

        [MenuItem("Fractured Chorus/Upgrade Off-Beat Transport Icons")]
        public static void UpgradeOffBeatTransportIcons()
        {
            var controller = Object.FindAnyObjectByType<OffBeatArchiveController>();
            if (controller == null)
            {
                EditorUtility.DisplayDialog(
                    "Off-Beat Transport Icons",
                    "Open MainMenuStartGame and ensure OffBeatArchiveOverlay exists.",
                    "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Upgrade Off-Beat Transport Icons");
            var so = new SerializedObject(controller);
            void AssignSprite(string field, string artPath)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
                if (sprite == null)
                {
                    var objs = AssetDatabase.LoadAllAssetsAtPath(artPath);
                    foreach (var o in objs)
                    {
                        if (o is Sprite s)
                        {
                            sprite = s;
                            break;
                        }
                    }
                }

                var prop = so.FindProperty(field);
                if (prop != null)
                {
                    prop.objectReferenceValue = sprite;
                }
            }

            const string root = "Assets/FracturedChorus/Art/UI/OffBeat/";
            AssignSprite("playSprite", root + "offbeat_btn_play_v2.png");
            AssignSprite("pauseSprite", root + "offbeat_btn_pause_v2.png");
            AssignSprite("nextSprite", root + "offbeat_btn_next_v1.png");
            AssignSprite("previousSprite", root + "offbeat_btn_prev_v1.png");
            AssignSprite("repeatSprite", root + "offbeat_btn_repeat_v2.png");
            AssignSprite("shuffleSprite", root + "offbeat_btn_shuffle_v2.png");
            so.ApplyModifiedPropertiesWithoutUndo();

            // Force rebuild icon children
            controller.EnsureTransportIcons();

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] Off-Beat transport icons assigned — Save scene.");
        }

        [MenuItem("Fractured Chorus/Upgrade Off-Beat SyncPod Layout")]
        public static void UpgradeOffBeatSyncPodLayout()
        {
            var rootController = Object.FindAnyObjectByType<MainMenuStartGameController>();
            var canvas = GameObject.Find("MainMenuCanvas")?.transform;
            if (rootController == null || canvas == null)
            {
                EditorUtility.DisplayDialog(
                    "Upgrade Off-Beat SyncPod",
                    "Open MainMenuStartGame scene.",
                    "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Upgrade Off-Beat SyncPod Layout");
            EnsureOffBeatCatalogAssets();
            var archiveOverlay = RebuildOffBeatArchiveOverlay(canvas, rootController);
            SetSerializedField(rootController, "offBeatArchiveOverlay", archiveOverlay);
            SetSerializedField(rootController, "offBeatArchiveController",
                archiveOverlay.GetComponent<OffBeatArchiveController>());
            WireOverlayBackButtonsInScene(rootController);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] Off-Beat SyncPod layout rebuilt — Save scene.");
        }

        [MenuItem("Fractured Chorus/Upgrade Off-Beat Archive Player")]
        public static void UpgradeOffBeatArchivePlayer()
        {
            var controller = Object.FindAnyObjectByType<MainMenuStartGameController>();
            var canvas = GameObject.Find("MainMenuCanvas")?.transform;
            if (controller == null || canvas == null)
            {
                EditorUtility.DisplayDialog(
                    "Upgrade Off-Beat Archive",
                    "Open MainMenuStartGame scene (MainMenuCanvas required).",
                    "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Upgrade Off-Beat Archive Player");
            EnsureOffBeatCatalogAssets();
            var archiveOverlay = RebuildOffBeatArchiveOverlay(canvas, controller);
            SetSerializedField(controller, "offBeatArchiveOverlay", archiveOverlay);
            SetSerializedField(controller, "offBeatArchiveController",
                archiveOverlay.GetComponent<OffBeatArchiveController>());
            WireOverlayBackButtonsInScene(controller);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] Off-Beat Archive player+catalog upgraded — Save scene.");
        }

        public static void BatchUpgradeOffBeatArchivePlayer()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError($"[Fractured Chorus] Scene not found: {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            UpgradeOffBeatArchivePlayer();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Fractured Chorus] Off-Beat Archive batch upgrade complete.");
            EditorApplication.Exit(0);
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

        public static void BatchEnsureConfigSkipUnreadRow()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError($"[Fractured Chorus] Scene not found: {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EnsureConfigSkipUnreadRow();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Fractured Chorus] Config Skip Unread row batch ensure complete.");
            EditorApplication.Exit(0);
        }

        [MenuItem("Fractured Chorus/Upgrade MainMenuStartGame Config UI")]
        public static void UpgradeMainMenuStartGameConfigUi()
        {
            EnsureMainMenuStartGameConfigUi(preserveLayout: true);
        }

        [MenuItem("Fractured Chorus/Rebuild MainMenuStartGame Config UI (Resets Layout)")]
        public static void RebuildMainMenuStartGameConfigUi()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild Config UI",
                    "This deletes SettingsOverlay and recreates default layout. Custom Pos/Scale will be lost.",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            EnsureMainMenuStartGameConfigUi(preserveLayout: false);
        }

        private static void EnsureMainMenuStartGameConfigUi(bool preserveLayout)
        {
            var canvas = GameObject.Find("MainMenuCanvas")?.transform;
            var controller = Object.FindAnyObjectByType<MainMenuStartGameController>();
            if (canvas == null || controller == null)
            {
                EditorUtility.DisplayDialog(
                    "Upgrade Config UI",
                    "Open the MainMenuStartGame scene first.",
                    "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, preserveLayout
                ? "Ensure MainMenuStartGame Config UI"
                : "Rebuild MainMenuStartGame Config UI");

            CanvasGroup settingsOverlay;
            if (preserveLayout && canvas.Find("SettingsOverlay") != null)
            {
                StripConfigLayoutGroups();
                EnsureConfigSkipUnreadRow();
                EnsureConfigRenArt();
                settingsOverlay = GameObject.Find("SettingsOverlay")?.GetComponent<CanvasGroup>();
            }
            else
            {
                settingsOverlay = CreateSettingsOverlay(canvas, controller);
            }

            WireConfigOverlayReferences(controller);
            ConfigUiKitApply.Apply(setPreview: true);

            if (settingsOverlay != null)
            {
                SetSerializedField(controller, "settingsOverlay", settingsOverlay);
                SetSerializedField(controller, "configOverlayController", settingsOverlay.GetComponent<MainMenuConfigOverlayController>());
            }

            WireOverlayBackButtonsInScene(controller);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log(preserveLayout
                ? "[Fractured Chorus] Config UI ensured without resetting layout — Save scene."
                : "[Fractured Chorus] Config UI rebuilt from defaults — Save scene.");
        }

        [MenuItem("Fractured Chorus/Unlock Config UI Free Layout")]
        public static void UnlockConfigUiFreeLayoutMenu()
        {
            if (!UnlockConfigUiFreeLayout())
            {
                EditorUtility.DisplayDialog(
                    "Unlock Config UI",
                    "ConfigList / ConfigUiRoot not found. Open MainMenuStartGame and select Config preview.",
                    "OK");
                return;
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] Config UI unlocked for free Pos/Scale — Save scene (Ctrl+S).");
        }

        public static bool UnlockConfigUiFreeLayout()
        {
            var configRoot = GameObject.Find("ConfigUiRoot");
            var configList = GameObject.Find("ConfigList");
            if (configRoot == null || configList == null)
            {
                return false;
            }

            Undo.RegisterFullObjectHierarchyUndo(configRoot, "Unlock Config UI Free Layout");
            StripConfigLayoutGroups();
            EnsureConfigSkipUnreadRow();
            EditorUtility.SetDirty(configRoot);
            EditorUtility.SetDirty(configList);
            return true;
        }

        private static void StripConfigLayoutGroups()
        {
            var configRoot = GameObject.Find("ConfigUiRoot");
            var configList = GameObject.Find("ConfigList");
            if (configRoot == null || configList == null)
            {
                return;
            }

            StripLayoutComponents(configList);
            foreach (Transform child in configList.transform)
            {
                StripLayoutComponents(child.gameObject);
                StripConfigRowChildren(child);
            }

            StripLayoutComponentsInChildren(configRoot.transform);
        }

        private static void StripLayoutComponentsInChildren(Transform root)
        {
            foreach (Transform child in root)
            {
                if (child.name == "ConfigList")
                {
                    continue;
                }

                StripLayoutComponents(child.gameObject);
                StripLayoutComponentsInChildren(child);
            }
        }

        private static void StripLayoutComponents(GameObject go)
        {
            DestroyComponents<VerticalLayoutGroup>(go);
            DestroyComponents<HorizontalLayoutGroup>(go);
            DestroyComponents<LayoutElement>(go);
        }

        private static void StripConfigRowChildren(Transform row)
        {
            foreach (Transform child in row)
            {
                StripLayoutComponents(child.gameObject);
            }
        }

        [MenuItem("Fractured Chorus/Ensure Config Skip Unread Row")]
        public static void EnsureConfigSkipUnreadRowMenu()
        {
            if (!EnsureConfigSkipUnreadRow())
            {
                EditorUtility.DisplayDialog(
                    "Ensure Skip Unread Row",
                    "ConfigList / SettingsOverlay not found. Open MainMenuStartGame first.",
                    "OK");
                return;
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] Skip Unread Text row ensured — Save scene (Ctrl+S).");
        }

        public static bool EnsureConfigSkipUnreadRow()
        {
            var configList = GameObject.Find("ConfigList")?.transform;
            var settingsOverlay = GameObject.Find("SettingsOverlay");
            if (configList == null || settingsOverlay == null)
            {
                return false;
            }

            var configController = settingsOverlay.GetComponent<MainMenuConfigOverlayController>();
            if (configController == null)
            {
                return false;
            }

            var volumeRow = configList.Find("Row_Volume");
            var brightnessRow = configList.Find("Row_Background_Brightness");
            var difficultyRow = configList.Find("Row_Difficulty");
            var skipRowTransform = configList.Find("Row_Skip_Unread_Text");
            Slider skipUnreadSlider = null;
            Text skipLabel = null;
            RectTransform skipRowRect = null;

            if (skipRowTransform == null)
            {
                var created = CreateConfigToggleSliderRow(configList, "Skip Unread Text", out skipUnreadSlider);
                skipRowRect = created.row;
                skipLabel = created.label;
                skipRowTransform = skipRowRect.transform;
                PlaceNewRowFromReference(skipRowTransform.gameObject, brightnessRow, difficultyRow);
            }
            else
            {
                skipRowRect = skipRowTransform.GetComponent<RectTransform>();
                skipLabel = skipRowTransform.Find("Label")?.GetComponent<Text>();
                skipUnreadSlider = skipRowTransform.Find("Slider")?.GetComponent<Slider>();
            }

            var skipUnreadToggle = EnsureToggleSwitchOnSlider(skipUnreadSlider);

            var so = new SerializedObject(configController);
            so.FindProperty("skipUnreadToggle").objectReferenceValue = skipUnreadToggle;

            var rowsProp = so.FindProperty("rows");
            rowsProp.arraySize = 4;
            WriteConfigRow(rowsProp.GetArrayElementAtIndex(0), volumeRow?.GetComponent<RectTransform>(), volumeRow?.Find("Label")?.GetComponent<Text>());
            WriteConfigRow(rowsProp.GetArrayElementAtIndex(1), brightnessRow?.GetComponent<RectTransform>(), brightnessRow?.Find("Label")?.GetComponent<Text>());
            WriteConfigRow(rowsProp.GetArrayElementAtIndex(2), skipRowRect, skipLabel);
            WriteConfigRow(rowsProp.GetArrayElementAtIndex(3), difficultyRow?.GetComponent<RectTransform>(), difficultyRow?.Find("Label")?.GetComponent<Text>());
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(configController);
            return true;
        }

        private static void PlaceNewRowFromReference(GameObject newRow, Transform upperRow, Transform lowerRow)
        {
            var rect = newRow.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            Transform template = upperRow ?? lowerRow;
            if (template != null)
            {
                CopyRectTransformLayout(rect, template.GetComponent<RectTransform>());
            }
            else
            {
                SetFreeRect(newRow, new Vector2(0.5f, 1f), new Vector2(744f, 44f), new Vector2(0f, -138f));
            }

            if (upperRow != null && lowerRow != null)
            {
                var upper = upperRow.GetComponent<RectTransform>();
                var lower = lowerRow.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(
                    upper.anchoredPosition.x,
                    (upper.anchoredPosition.y + lower.anchoredPosition.y) * 0.5f);
            }
            else if (upperRow != null)
            {
                var upper = upperRow.GetComponent<RectTransform>();
                rect.anchoredPosition = upper.anchoredPosition + new Vector2(0f, -54f);
            }
            else if (lowerRow != null)
            {
                var lower = lowerRow.GetComponent<RectTransform>();
                rect.anchoredPosition = lower.anchoredPosition + new Vector2(0f, 54f);
            }
        }

        private static void CopyRectTransformLayout(RectTransform target, RectTransform source)
        {
            if (target == null || source == null)
            {
                return;
            }

            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.sizeDelta = source.sizeDelta;
            target.anchoredPosition = source.anchoredPosition;
            target.localScale = source.localScale;
            target.localRotation = source.localRotation;
        }

        private static void WireConfigOverlayReferences(MainMenuStartGameController controller)
        {
            var settingsOverlay = GameObject.Find("SettingsOverlay");
            var configList = GameObject.Find("ConfigList")?.transform;
            if (settingsOverlay == null || configList == null)
            {
                return;
            }

            var configController = settingsOverlay.GetComponent<MainMenuConfigOverlayController>();
            if (configController == null)
            {
                return;
            }

            var volumeRow = configList.Find("Row_Volume");
            var brightnessRow = configList.Find("Row_Background_Brightness");
            var skipRow = configList.Find("Row_Skip_Unread_Text");
            var difficultyRow = configList.Find("Row_Difficulty");
            var highlightBar = configList.Find("HighlightBar")?.GetComponent<RectTransform>();
            var infoText = settingsOverlay.transform.Find("ConfigUiRoot/InfoText")?.GetComponent<Text>();

            var so = new SerializedObject(configController);
            so.FindProperty("screenController").objectReferenceValue = controller;
            if (highlightBar != null)
            {
                so.FindProperty("highlightBar").objectReferenceValue = highlightBar;
            }

            if (infoText != null)
            {
                so.FindProperty("infoText").objectReferenceValue = infoText;
            }

            so.FindProperty("volumeSlider").objectReferenceValue =
                volumeRow?.Find("Slider")?.GetComponent<Slider>();
            so.FindProperty("brightnessSlider").objectReferenceValue =
                brightnessRow?.Find("Slider")?.GetComponent<Slider>();
            so.FindProperty("skipUnreadToggle").objectReferenceValue =
                EnsureToggleSwitchOnSlider(skipRow?.Find("Slider")?.GetComponent<Slider>());
            so.FindProperty("difficultyValueText").objectReferenceValue =
                difficultyRow?.Find("Value")?.GetComponent<Text>();
            so.FindProperty("difficultyPrevButton").objectReferenceValue =
                difficultyRow?.Find("Lt")?.GetComponent<Button>();
            so.FindProperty("difficultyNextButton").objectReferenceValue =
                difficultyRow?.Find("Gt")?.GetComponent<Button>();

            var rowsProp = so.FindProperty("rows");
            rowsProp.arraySize = 4;
            WriteConfigRow(rowsProp.GetArrayElementAtIndex(0), volumeRow?.GetComponent<RectTransform>(), volumeRow?.Find("Label")?.GetComponent<Text>());
            WriteConfigRow(rowsProp.GetArrayElementAtIndex(1), brightnessRow?.GetComponent<RectTransform>(), brightnessRow?.Find("Label")?.GetComponent<Text>());
            WriteConfigRow(rowsProp.GetArrayElementAtIndex(2), skipRow?.GetComponent<RectTransform>(), skipRow?.Find("Label")?.GetComponent<Text>());
            WriteConfigRow(rowsProp.GetArrayElementAtIndex(3), difficultyRow?.GetComponent<RectTransform>(), difficultyRow?.Find("Label")?.GetComponent<Text>());
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(configController);
        }

        private static void DestroyComponents<T>(GameObject go) where T : Component
        {
            var components = go.GetComponents<T>();
            for (var i = 0; i < components.Length; i++)
            {
                Undo.DestroyObjectImmediate(components[i]);
            }
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
                    "MainMenuCanvas / MainMenuLayer|Background / MenuPanel not found. Open the MainMenuStartGame scene first.",
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
            Debug.Log("[Fractured Chorus] MainMenuStartGame layers upgraded — MenuPanel separated from background. Save scene.");
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
            var sceneFadeOverlay = EnsureSceneFadeOverlay(canvas.transform);
            EnsureMainMenuBgm(root.transform);
            EnsureMainMenuTitleVoice(root.transform);
            EnsureMainMenuTransitionSfx(root.transform);
            EnsureMainMenuButtonPressSfx(root.transform);

            mainMenuBackground.GetComponent<CanvasGroup>().alpha = 1f;
            menuUiGroup.alpha = 1f;

            WireController(controller, attractLayer, mainMenuBackground, menuUiGroup, menuController, settingsOverlay, archiveOverlay, sceneFadeOverlay);
            TitleScreenChromeApply.Apply(root);
            controller.SetEditorPreview(MainMenuStartGameController.MainMenuEditorPreview.Attract);
            SceneFontSetupEditor.FinalizeSceneCanvas(canvas.gameObject);

            Selection.activeGameObject = root;
        }

        private static void WireController(
            MainMenuStartGameController controller,
            GameObject attractLayer,
            GameObject mainMenuBackground,
            CanvasGroup mainMenuUi,
            MainMenuStartGameMenuController menuController,
            CanvasGroup settingsOverlay,
            CanvasGroup archiveOverlay,
            CanvasGroup sceneFadeOverlay = null)
        {
            SetSerializedField(controller, "attractLayer", attractLayer.GetComponent<CanvasGroup>());
            SetSerializedField(controller, "mainMenuBackground", mainMenuBackground.GetComponent<CanvasGroup>());
            SetSerializedField(controller, "mainMenuUi", mainMenuUi);
            SetSerializedField(controller, "settingsOverlay", settingsOverlay);
            SetSerializedField(controller, "configOverlayController", settingsOverlay.GetComponent<MainMenuConfigOverlayController>());
            SetSerializedField(controller, "offBeatArchiveOverlay", archiveOverlay);
            SetSerializedField(controller, "offBeatArchiveController",
                archiveOverlay != null ? archiveOverlay.GetComponent<OffBeatArchiveController>() : null);
            SetSerializedField(controller, "menuController", menuController);
            SetSerializedField(controller, "sceneFadeOverlay", sceneFadeOverlay);
            SetSerializedField(controller, "transitionDuration", 0.35f);
            SetSerializedField(controller, "newGameFadeDuration", 1.15f);
            SetSerializedField(controller, "newGameFadeHoldSeconds", 0.35f);
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

        private static CanvasGroup EnsureSceneFadeOverlay(Transform canvas)
        {
            var existing = canvas.Find("SceneFadeOverlay");
            if (existing != null && existing.TryGetComponent<CanvasGroup>(out var existingGroup))
            {
                return existingGroup;
            }

            var overlayGo = CreateUiObject("SceneFadeOverlay", canvas);
            overlayGo.transform.SetAsLastSibling();
            StretchRect(overlayGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var image = overlayGo.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;

            var group = overlayGo.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
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
            statusText.fontSize = 16;
            statusText.alignment = TextAnchor.LowerRight;
            statusText.color = new Color(0.75f, 0.8f, 0.85f, 0.85f);
            SceneFontSetupEditor.ApplyAutomatic(statusText);
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
            text.fontSize = 28;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleRight;
            text.color = interactable ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            text.raycastTarget = false;
            SceneFontSetupEditor.ApplyAutomatic(text);

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
            return RebuildOffBeatArchiveOverlay(canvas, controller);
        }

        private static CanvasGroup RebuildOffBeatArchiveOverlay(Transform canvas, MainMenuStartGameController controller)
        {
            EnsureOffBeatCatalogAssets();
            var existing = canvas.Find("OffBeatArchiveOverlay");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var cyan = FcColorTokens.Brand.Cyan;
            var overlayGo = CreateUiObject("OffBeatArchiveOverlay", canvas);
            StretchRect(overlayGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var group = overlayGo.AddComponent<CanvasGroup>();
            var dim = overlayGo.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;

            var panelGo = CreateUiObject("ArchivePanel", overlayGo.transform);
            StretchRect(panelGo, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
            var panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0.04f, 0.06f, 0.12f, 0.96f);

            var title = CreateText("Title", panelGo.transform, "OFF-BEAT ARCHIVE", 30, TextAnchor.MiddleLeft);
            title.color = cyan;
            title.fontStyle = FontStyle.Bold;
            StretchRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -56f), new Vector2(-24f, -8f));

            var catalogRoot = CreateUiObject("CatalogScroll", panelGo.transform);
            StretchRect(catalogRoot, new Vector2(0f, 0f), new Vector2(0.38f, 1f), new Vector2(20f, 72f), new Vector2(-8f, -64f));
            var catalogBg = catalogRoot.AddComponent<Image>();
            catalogBg.color = new Color(0.02f, 0.04f, 0.1f, 0.85f);
            var scroll = catalogRoot.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = CreateUiObject("Viewport", catalogRoot.transform);
            StretchRect(viewport, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            viewport.AddComponent<RectMask2D>();
            var viewportImg = viewport.AddComponent<Image>();
            viewportImg.color = new Color(1f, 1f, 1f, 0.01f);
            scroll.viewport = viewport.GetComponent<RectTransform>();

            var content = CreateUiObject("Content", viewport.transform);
            StretchRect(content, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.pivot = new Vector2(0.5f, 1f);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.spacing = 4f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRt;

            var playerRoot = CreateUiObject("PlayerRoot", panelGo.transform);
            StretchRect(playerRoot, new Vector2(0.38f, 0f), new Vector2(1f, 1f), new Vector2(8f, 72f), new Vector2(-20f, -64f));
            var playerBg = playerRoot.AddComponent<Image>();
            playerBg.color = new Color(0.02f, 0.03f, 0.08f, 0.35f);
            playerBg.raycastTarget = false;

            var syncBgGo = CreateUiObject("SyncPodBg", playerRoot.transform);
            StretchRect(syncBgGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var syncBg = syncBgGo.AddComponent<Image>();
            syncBg.raycastTarget = false;
            syncBg.preserveAspect = true;
            var syncBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/FracturedChorus/Art/UI/OffBeat/offbeat_syncpod_bg_v2.png");
            if (syncBgSprite == null)
            {
                var objs = AssetDatabase.LoadAllAssetsAtPath(
                    "Assets/FracturedChorus/Art/UI/OffBeat/offbeat_syncpod_bg_v2.png");
                foreach (var o in objs)
                {
                    if (o is Sprite s)
                    {
                        syncBgSprite = s;
                        break;
                    }
                }
            }

            syncBg.sprite = syncBgSprite;
            syncBg.color = Color.white;
            syncBg.preserveAspect = true;

            var volRoot = CreateUiObject("VolumeArcRoot", playerRoot.transform);
            var volRt = volRoot.GetComponent<RectTransform>();
            volRt.anchorMin = new Vector2(0.5f, 0.5f);
            volRt.anchorMax = new Vector2(0.5f, 0.5f);
            volRt.pivot = new Vector2(0.5f, 0.5f);
            volRt.sizeDelta = new Vector2(228.89f, 208.41f);
            volRt.anchoredPosition = new Vector2(0f, 155.7f);
            volRt.localEulerAngles = new Vector3(0f, 0f, -368.749f);
            var volTrack = CreateUiObject("Track", volRoot.transform);
            StretchRect(volTrack, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var volTrackImg = volTrack.AddComponent<Image>();
            volTrackImg.raycastTarget = false;
            var volFillGo = CreateUiObject("Fill", volRoot.transform);
            StretchRect(volFillGo, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            var volFill = volFillGo.AddComponent<Image>();
            volFill.raycastTarget = false;
            var volHit = CreateUiObject("HitArea", volRoot.transform);
            StretchRect(volHit, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var volHitImg = volHit.AddComponent<Image>();
            volHitImg.color = new Color(1f, 1f, 1f, 0f);
            volHitImg.raycastTarget = true;
            var volumeArc = volHit.AddComponent<OffBeatVolumeArcView>();
            volumeArc.Bind(volTrackImg, volFill);

            var discFace = CreateUiObject("DiscFace", playerRoot.transform);
            var discRt = discFace.GetComponent<RectTransform>();
            discRt.anchorMin = new Vector2(0.5f, 0.5f);
            discRt.anchorMax = new Vector2(0.5f, 0.5f);
            discRt.pivot = new Vector2(0.5f, 0.5f);
            discRt.sizeDelta = new Vector2(420f, 420f);
            discRt.anchoredPosition = new Vector2(0f, 20f);
            var discImg = discFace.AddComponent<Image>();
            discImg.color = new Color(1f, 1f, 1f, 0f);
            discImg.raycastTarget = true;
            discFace.AddComponent<RectMask2D>();
            var swipe = discFace.AddComponent<OffBeatDiscSwipeZone>();

            var coverGo = CreateUiObject("CoverImage", discFace.transform);
            var coverRt = coverGo.GetComponent<RectTransform>();
            coverRt.anchorMin = new Vector2(0.5f, 1f);
            coverRt.anchorMax = new Vector2(0.5f, 1f);
            coverRt.pivot = new Vector2(0.5f, 1f);
            coverRt.anchoredPosition = new Vector2(0f, -28.4f);
            coverRt.sizeDelta = new Vector2(88f, 88f);
            var coverImage = coverGo.AddComponent<Image>();
            coverImage.color = new Color(0.08f, 0.12f, 0.2f, 1f);
            coverImage.raycastTarget = false;

            var songTitle = CreateText("SongTitle", discFace.transform, "Song title", 22, TextAnchor.MiddleCenter);
            songTitle.fontStyle = FontStyle.Bold;
            songTitle.color = cyan;
            songTitle.raycastTarget = false;
            songTitle.resizeTextForBestFit = false;
            songTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            var songTitleRect = songTitle.rectTransform;
            songTitleRect.anchorMin = new Vector2(0f, 0.5f);
            songTitleRect.anchorMax = new Vector2(1f, 0.5f);
            songTitleRect.pivot = new Vector2(0.5f, 0.5f);
            songTitleRect.anchoredPosition = Vector2.zero;
            songTitleRect.offsetMin = new Vector2(20.1f, -36f);
            songTitleRect.offsetMax = new Vector2(-19.28f, 36f);
            songTitle.gameObject.AddComponent<RectMask2D>();
            var songTitleLabel = CreateText("Label", songTitle.transform, "Song title", 22, TextAnchor.MiddleCenter);
            songTitleLabel.fontStyle = FontStyle.Bold;
            songTitleLabel.color = cyan;
            songTitleLabel.raycastTarget = false;
            songTitleLabel.resizeTextForBestFit = false;
            songTitleLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            StretchRect(songTitleLabel.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            songTitle.gameObject.AddComponent<MarqueeTextUI>().BindLabel(songTitleLabel);
            songTitle.enabled = false;

            var artist = CreateText("ArtistName", discFace.transform, string.Empty, 14, TextAnchor.MiddleCenter);
            artist.color = new Color(0.7f, 0.85f, 1f, 0.75f);
            artist.raycastTarget = false;
            artist.gameObject.SetActive(false);

            var controls = CreateUiObject("Controls", discFace.transform);
            var controlsRt = controls.GetComponent<RectTransform>();
            controlsRt.anchorMin = new Vector2(0.5f, 0.5f);
            controlsRt.anchorMax = new Vector2(0.5f, 0.5f);
            controlsRt.anchoredPosition = new Vector2(0.4f, -40.8f);
            controlsRt.sizeDelta = new Vector2(135.76f, 32.6f);
            var hlg = controls.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            var shuffleBtn = CreateTransportButton(controls.transform, "Shuffle", "⇄", out var shuffleIcon);
            var playBtn = CreateTransportButton(controls.transform, "PlayPause", "▶", out _, out var playLabel, large: true);
            var repeatBtn = CreateTransportButton(controls.transform, "Repeat", "↻", out var repeatIcon);

            var waveGo = CreateUiObject("Waveform", discFace.transform);
            StretchRect(waveGo, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.38f), Vector2.zero, Vector2.zero);
            var waveRt = waveGo.GetComponent<RectTransform>();
            waveRt.anchoredPosition = new Vector2(0f, 31f);
            waveRt.sizeDelta = new Vector2(-68f, 0f);
            var waveBg = waveGo.AddComponent<Image>();
            waveBg.color = new Color(1f, 1f, 1f, 0f);
            waveBg.raycastTarget = false;
            var drawGo = CreateUiObject("WaveDraw", waveGo.transform);
            StretchRect(drawGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var waveformView = drawGo.AddComponent<OffBeatWaveformView>();
            waveformView.raycastTarget = false;
            waveformView.color = Color.white;

            Button prevBtn = null;
            Button nextBtn = null;
            Slider seek = null;
            Text timeCurrent = null;
            Text timeTotal = null;
            Button favButton = null;
            Image favImg = null;

            var backGo = CreateUiObject("Btn_Back", panelGo.transform);
            StretchRect(backGo, new Vector2(0.35f, 0f), new Vector2(0.65f, 0f), new Vector2(0f, 16f), new Vector2(0f, 56f));
            var backImage = backGo.AddComponent<Image>();
            backImage.color = new Color(0.12f, 0.28f, 0.42f, 1f);
            var backButton = backGo.AddComponent<Button>();
            backButton.targetGraphic = backImage;
            BindPersistentBackButton(backButton, controller, controller.HideOffBeatArchive);
            var backLabel = CreateText("Label", backGo.transform, "BACK", 22, TextAnchor.MiddleCenter);
            backLabel.color = cyan;
            StretchRect(backLabel.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var archiveController = overlayGo.AddComponent<OffBeatArchiveController>();
            if (overlayGo.GetComponent<AudioSource>() == null)
            {
                overlayGo.AddComponent<AudioSource>();
            }

            var musicPlayer = overlayGo.GetComponent<OffBeatMusicPlayer>();
            if (musicPlayer == null)
            {
                musicPlayer = overlayGo.AddComponent<OffBeatMusicPlayer>();
            }

            var catalog = AssetDatabase.LoadAssetAtPath<OffBeatCatalogSO>(OffBeatCatalogPath);
            var bgm = Object.FindAnyObjectByType<MainMenuBgmController>();
            SetSerializedField(archiveController, "catalog", catalog);
            SetSerializedField(archiveController, "musicPlayer", musicPlayer);
            SetSerializedField(archiveController, "menuBgm", bgm);
            SetSerializedField(archiveController, "catalogContent", content.transform);
            SetSerializedField(archiveController, "coverImage", coverImage);
            SetSerializedField(archiveController, "songTitleLabel", songTitle);
            SetSerializedField(archiveController, "artistLabel", artist);
            SetSerializedField(archiveController, "favoriteButton", favButton);
            SetSerializedField(archiveController, "favoriteIcon", favImg);
            SetSerializedField(archiveController, "seekSlider", seek);
            SetSerializedField(archiveController, "timeCurrentLabel", timeCurrent);
            SetSerializedField(archiveController, "timeTotalLabel", timeTotal);
            SetSerializedField(archiveController, "shuffleButton", shuffleBtn);
            SetSerializedField(archiveController, "previousButton", prevBtn);
            SetSerializedField(archiveController, "playPauseButton", playBtn);
            SetSerializedField(archiveController, "playPauseLabel", playLabel);
            SetSerializedField(archiveController, "nextButton", nextBtn);
            SetSerializedField(archiveController, "repeatButton", repeatBtn);
            SetSerializedField(archiveController, "shuffleIcon", shuffleIcon);
            SetSerializedField(archiveController, "repeatIcon", repeatIcon);
            SetSerializedField(archiveController, "waveformImage", waveBg);
            SetSerializedField(archiveController, "waveformView", waveformView);
            SetSerializedField(archiveController, "syncPodBackground", syncBg);
            SetSerializedField(archiveController, "discSwipeZone", swipe);
            SetSerializedField(archiveController, "volumeArcView", volumeArc);

            overlayGo.SetActive(false);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }

        private static Button CreateTransportButton(Transform parent, string name, string label, out Image iconImage)
        {
            return CreateTransportButton(parent, name, label, out iconImage, out _, large: false);
        }

        private static Button CreateTransportButton(
            Transform parent,
            string name,
            string label,
            out Image iconImage,
            out Text labelText,
            bool large)
        {
            var go = CreateUiObject(name, parent);
            var le = go.AddComponent<LayoutElement>();
            le.ignoreLayout = false;
            le.preferredWidth = large ? 64f : 56f;
            le.preferredHeight = large ? 64f : 56f;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(le.preferredWidth, le.preferredHeight);
            var img = go.AddComponent<Image>();
            img.color = large
                ? new Color(0f, 0.55f, 0.75f, 0.35f)
                : new Color(0.08f, 0.12f, 0.2f, 0.45f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = img;
            labelText = CreateText("Label", go.transform, label, large ? 26 : 22, TextAnchor.MiddleCenter);
            labelText.color = FcColorTokens.Brand.Cyan;
            labelText.raycastTarget = false;
            StretchRect(labelText.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            iconImage = img;
            return button;
        }

        private static void BuildWaveformBars(Transform parent, Color cyan)
        {
            var heights = new[] { 0.25f, 0.55f, 0.4f, 0.85f, 0.35f, 0.7f, 0.5f, 0.95f, 0.45f, 0.65f, 0.3f, 0.8f, 0.4f, 0.6f, 0.35f, 0.75f, 0.5f, 0.9f, 0.4f, 0.55f, 0.3f, 0.7f, 0.45f, 0.6f };
            var count = heights.Length;
            for (var i = 0; i < count; i++)
            {
                var bar = CreateUiObject($"Bar_{i}", parent);
                var rt = bar.GetComponent<RectTransform>();
                var x0 = i / (float)count;
                var x1 = (i + 1) / (float)count;
                var h = heights[i];
                rt.anchorMin = new Vector2(x0, 0.5f - h * 0.5f);
                rt.anchorMax = new Vector2(x1, 0.5f + h * 0.5f);
                rt.offsetMin = new Vector2(2f, 0f);
                rt.offsetMax = new Vector2(-2f, 0f);
                var img = bar.AddComponent<Image>();
                img.color = new Color(cyan.r, cyan.g, cyan.b, 0.55f);
                img.raycastTarget = false;
            }
        }

        private const string OffBeatResourcesFolder = "Assets/FracturedChorus/Resources/OffBeat";
        private const string OffBeatCatalogPath = "Assets/FracturedChorus/Resources/OffBeat/OffBeatCatalog.asset";

        private static void EnsureOffBeatCatalogAssets()
        {
            EnsureFolder("Assets/FracturedChorus/Resources");
            EnsureFolder(OffBeatResourcesFolder);

            var defs = new[]
            {
                ("midnight", "Midnight", "Fractured Chorus", "Assets/FracturedChorus/Audio/Music/Midnight_BGM_Menu.mp3"),
                ("eternal_spark", "Eternal Spark", "LUXE", "Assets/FracturedChorus/Audio/Music/EternalSpark.mp3"),
                ("eternal_spark_boss", "Eternal Spark — Boss Remix", "Astra", "Assets/FracturedChorus/Audio/Music/EternalSpark_BossRemix.mp3"),
                ("bring_me_home", "Bring Me Home", "Fractured Chorus", "Assets/FracturedChorus/Audio/Music/Bring_Me_Home.mp3"),
                ("velvet_reverie", "Velvet Reverie", "Fractured Chorus", "Assets/FracturedChorus/Audio/Music/Velvet_Reverie_BGM.mp3"),
                ("locked_vault", "The Locked Vault", "Fractured Chorus", "Assets/FracturedChorus/Audio/Music/The_Locked_Vault.mp3"),
            };

            var tracks = new OffBeatTrackSO[defs.Length];
            for (var i = 0; i < defs.Length; i++)
            {
                var (id, title, artist, clipPath) = defs[i];
                var assetPath = $"{OffBeatResourcesFolder}/Track_{id}.asset";
                var track = AssetDatabase.LoadAssetAtPath<OffBeatTrackSO>(assetPath);
                if (track == null)
                {
                    track = ScriptableObject.CreateInstance<OffBeatTrackSO>();
                    AssetDatabase.CreateAsset(track, assetPath);
                }

                track.trackId = id;
                track.title = title;
                track.artist = artist;
                track.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                EditorUtility.SetDirty(track);
                tracks[i] = track;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<OffBeatCatalogSO>(OffBeatCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<OffBeatCatalogSO>();
                AssetDatabase.CreateAsset(catalog, OffBeatCatalogPath);
            }

            catalog.tracks = tracks;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }

        private static CanvasGroup EnsureOffBeatArchiveOverlay(Transform canvas, MainMenuStartGameController controller)
        {
            var existing = canvas.Find("OffBeatArchiveOverlay");
            if (existing != null && existing.GetComponent<OffBeatArchiveController>() != null
                && existing.Find("ArchivePanel/PlayerRoot") != null
                && existing.Find("ArchivePanel/PlayerRoot/SyncPodBg") != null
                && existing.Find("ArchivePanel/PlayerRoot/DiscFace") != null)
            {
                WireOverlayBackButtonInHierarchy(existing, controller, controller.HideOffBeatArchive);
                return existing.GetComponent<CanvasGroup>();
            }

            return RebuildOffBeatArchiveOverlay(canvas, controller);
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

        public static bool EnsureConfigRenArt()
        {
            var settings = GameObject.Find("SettingsOverlay");
            if (settings == null)
            {
                return false;
            }

            EnsureConfigRenArt(settings.transform);
            return true;
        }

        private static void EnsureConfigRenArt(Transform overlay)
        {
            var existing = overlay.Find("ConfigRen");
            var created = existing == null;
            var go = created ? CreateUiObject("ConfigRen", overlay) : existing.gameObject;
            var image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
            }
            image.sprite = LoadSprite(ConfigRenPosePath);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.useSpriteMesh = false;

            if (created)
            {
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.76f, 0.46f);
                rect.anchorMax = new Vector2(0.76f, 0.46f);
                rect.pivot = new Vector2(0.52f, 0.42f);
                rect.sizeDelta = new Vector2(980f, 1470f);
                rect.anchoredPosition = new Vector2(24f, -8f);
                rect.localScale = Vector3.one;
            }

            var bg = overlay.Find("ConfigBackground");
            if (bg != null)
            {
                go.transform.SetSiblingIndex(bg.GetSiblingIndex() + 1);
            }

            EditorUtility.SetDirty(go);
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

            EnsureConfigRenArt(overlayGo.transform);

            var uiRootGo = CreateUiObject("ConfigUiRoot", overlayGo.transform);
            SetFreeRect(uiRootGo, new Vector2(0.24f, 0.5f), new Vector2(768f, 734f), Vector2.zero);

            var infoGo = CreateUiObject("InfoText", uiRootGo.transform);
            var infoText = infoGo.AddComponent<Text>();
            infoText.text = "Adjust master volume for menu and game audio.";
            infoText.fontSize = 18;
            infoText.alignment = TextAnchor.UpperLeft;
            infoText.color = new Color(0.88f, 0.92f, 0.96f, 0.95f);
            infoText.raycastTarget = false;
            SceneFontSetupEditor.ApplyAutomatic(infoText);
            SetFreeRect(infoGo, new Vector2(0.5f, 1f), new Vector2(768f, 52f), new Vector2(0f, -26f));

            var listGo = CreateUiObject("ConfigList", uiRootGo.transform);
            SetFreeRect(listGo, new Vector2(0.5f, 0.5f), new Vector2(768f, 556f), new Vector2(0f, 12f));

            var highlightGo = CreateUiObject("HighlightBar", listGo.transform);
            var highlightImage = highlightGo.AddComponent<Image>();
            highlightImage.color = Color.white;
            highlightImage.raycastTarget = false;
            var highlightRect = highlightGo.GetComponent<RectTransform>();
            highlightRect.sizeDelta = new Vector2(0f, 44f);
            CreateHighlightBorder(highlightGo.transform, true);
            CreateHighlightBorder(highlightGo.transform, false);

            var rowVolume = CreateConfigSliderRow(listGo.transform, "Volume", out var volumeSlider);
            SetFreeRect(rowVolume.row.gameObject, new Vector2(0.5f, 1f), new Vector2(744f, 44f), new Vector2(0f, -30f));
            var rowBrightness = CreateConfigSliderRow(listGo.transform, "Background Brightness", out var brightnessSlider);
            SetFreeRect(rowBrightness.row.gameObject, new Vector2(0.5f, 1f), new Vector2(744f, 44f), new Vector2(0f, -84f));
            var rowSkipUnread = CreateConfigToggleSliderRow(listGo.transform, "Skip Unread Text", out var skipUnreadSlider);
            SetFreeRect(rowSkipUnread.row.gameObject, new Vector2(0.5f, 1f), new Vector2(744f, 44f), new Vector2(0f, -138f));
            var skipUnreadToggle = EnsureToggleSwitchOnSlider(skipUnreadSlider);
            var rowDifficulty = CreateConfigDifficultyRow(listGo.transform, out var difficultyValue, out var diffPrev, out var diffNext);
            SetFreeRect(rowDifficulty.row.gameObject, new Vector2(0.5f, 1f), new Vector2(744f, 44f), new Vector2(0f, -192f));

            var footerGo = CreateUiObject("Footer", uiRootGo.transform);
            SetFreeRect(footerGo, new Vector2(0.5f, 0f), new Vector2(768f, 44f), new Vector2(0f, 22f));
            var footerText = CreateText("Hints", footerGo.transform, "←→ Difficulty   ·   ESC Back", 16, TextAnchor.MiddleLeft);
            SetFreeRect(footerText.gameObject, new Vector2(0f, 0.5f), new Vector2(520f, 44f), new Vector2(260f, 0f));

            var backGo = CreateUiObject("Btn_Back", footerGo.transform);
            SetFreeRect(backGo, new Vector2(1f, 0.5f), new Vector2(200f, 44f), new Vector2(-100f, 0f));
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
            so.FindProperty("skipUnreadToggle").objectReferenceValue = skipUnreadToggle;
            so.FindProperty("difficultyValueText").objectReferenceValue = difficultyValue;
            so.FindProperty("difficultyPrevButton").objectReferenceValue = diffPrev;
            so.FindProperty("difficultyNextButton").objectReferenceValue = diffNext;
            var rowsProp = so.FindProperty("rows");
            rowsProp.arraySize = 4;
            WriteConfigRow(rowsProp.GetArrayElementAtIndex(0), rowVolume.row, rowVolume.label);
            WriteConfigRow(rowsProp.GetArrayElementAtIndex(1), rowBrightness.row, rowBrightness.label);
            WriteConfigRow(rowsProp.GetArrayElementAtIndex(2), rowSkipUnread.row, rowSkipUnread.label);
            WriteConfigRow(rowsProp.GetArrayElementAtIndex(3), rowDifficulty.row, rowDifficulty.label);
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

            var label = CreateText("Label", rowGo.transform, labelText, 22, TextAnchor.MiddleLeft);
            label.color = Color.white;
            label.fontStyle = FontStyle.Bold;
            SetFreeRect(label.gameObject, new Vector2(0f, 0.5f), new Vector2(210f, 44f), new Vector2(117f, 0f));

            slider = CreateStyledSlider(rowGo.transform);
            SetFreeRect(slider.gameObject, new Vector2(1f, 0.5f), new Vector2(490f, 28f), new Vector2(-257f, 0f));

            return (rowGo.GetComponent<RectTransform>(), label);
        }

        private static (RectTransform row, Text label) CreateConfigToggleSliderRow(
            Transform parent,
            string labelText,
            out Slider slider)
        {
            var row = CreateConfigSliderRow(parent, labelText, out slider);
            slider.wholeNumbers = true;
            slider.value = 0f;
            EnsureToggleSwitchOnSlider(slider);
            return row;
        }

        private static MainMenuConfigToggleSwitch EnsureToggleSwitchOnSlider(Slider slider)
        {
            if (slider == null)
            {
                return null;
            }

            slider.wholeNumbers = true;
            slider.interactable = false;

            if (slider.GetComponent<CanvasRenderer>() == null)
            {
                slider.gameObject.AddComponent<CanvasRenderer>();
            }

            var image = slider.GetComponent<Image>() ?? slider.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.001f);
            image.raycastTarget = true;

            var toggle = slider.GetComponent<MainMenuConfigToggleSwitch>() ??
                         slider.gameObject.AddComponent<MainMenuConfigToggleSwitch>();
            var so = new SerializedObject(toggle);
            so.FindProperty("visualSlider").objectReferenceValue = slider;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(toggle);
            return toggle;
        }

        private static (RectTransform row, Text label) CreateConfigDifficultyRow(
            Transform parent,
            out Text valueText,
            out Button prevButton,
            out Button nextButton)
        {
            var rowGo = CreateUiObject("Row_Difficulty", parent);

            var label = CreateText("Label", rowGo.transform, "Difficulty", 22, TextAnchor.MiddleLeft);
            label.color = Color.white;
            label.fontStyle = FontStyle.Bold;
            SetFreeRect(label.gameObject, new Vector2(0f, 0.5f), new Vector2(210f, 44f), new Vector2(117f, 0f));

            prevButton = CreateSmallButton(rowGo.transform, "Lt", 36f);
            SetFreeRect(prevButton.gameObject, new Vector2(0f, 0.5f), new Vector2(36f, 32f), new Vector2(246f, 0f));
            var prevLabel = prevButton.transform.Find("Label")?.GetComponent<Text>();
            if (prevLabel != null)
            {
                prevLabel.text = "<";
            }

            valueText = CreateText("Value", rowGo.transform, "CADENCE", 22, TextAnchor.MiddleCenter);
            valueText.color = new Color(0.55f, 0.85f, 1f, 1f);
            valueText.fontStyle = FontStyle.Bold;
            SetFreeRect(valueText.gameObject, new Vector2(0.5f, 0.5f), new Vector2(280f, 44f), new Vector2(80f, 0f));

            nextButton = CreateSmallButton(rowGo.transform, "Gt", 36f);
            SetFreeRect(nextButton.gameObject, new Vector2(1f, 0.5f), new Vector2(36f, 32f), new Vector2(-30f, 0f));
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
            return SceneFontSetupEditor.CreateUiText(name, parent, content, fontSize, anchor);
        }

        private static void StretchRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetFreeRect(GameObject go, Vector2 anchor, Vector2 size, Vector2 anchoredPosition)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            rect.localScale = Vector3.one;
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
        private const string SessionKey = "FC_MainMenuStartGame_ConfigUi_v9";

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

            SessionState.SetBool(SessionKey, true);

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != MainMenuStartGameSceneSetupEditor.ScenePathForAutoUpgrade)
            {
                return;
            }

            var settings = GameObject.Find("SettingsOverlay");
            if (settings == null || settings.transform.Find("ConfigBackground") == null ||
                settings.GetComponent<MainMenuConfigOverlayController>() == null)
            {
                MainMenuStartGameSceneSetupEditor.UpgradeMainMenuStartGameConfigUi();
                EditorSceneManager.MarkSceneDirty(scene);
                return;
            }

            if (GameObject.Find("ConfigList")?.transform.Find("Row_Skip_Unread_Text") == null)
            {
                MainMenuStartGameSceneSetupEditor.EnsureConfigSkipUnreadRow();
                EditorSceneManager.MarkSceneDirty(scene);
            }

            if (settings.transform.Find("ConfigRen") == null)
            {
                MainMenuStartGameSceneSetupEditor.EnsureConfigRenArt();
                EditorSceneManager.MarkSceneDirty(scene);
            }

            var volumeFill = GameObject.Find("Row_Volume")?.transform.Find("Slider/Fill Area/Fill")?.GetComponent<UnityEngine.UI.Image>();
            var volumeHandle = GameObject.Find("Row_Volume")?.transform.Find("Slider/Handle Slide Area/Handle")?.GetComponent<UnityEngine.UI.Image>();
            var volumeTrack = GameObject.Find("Row_Volume")?.transform.Find("Slider/Background")?.GetComponent<UnityEngine.UI.Image>();
            if (GameObject.Find("ConfigUiRoot")?.transform.Find("Panel") == null ||
                volumeTrack == null || volumeTrack.sprite == null ||
                volumeFill == null || volumeFill.sprite == null ||
                volumeHandle == null || volumeHandle.sprite == null)
            {
                ConfigUiKitApply.Apply(setPreview: true);
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }

    [InitializeOnLoad]
    internal static class MainMenuStartGameTitleChromeAutoApply
    {
        private const string SessionKey = "FC_MainMenuStartGame_TitleChrome_v4";

        static MainMenuStartGameTitleChromeAutoApply()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += TryApplyActiveScene;
        }

        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            TryApply(scene);
        }

        private static void TryApplyActiveScene()
        {
            TryApply(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static void TryApply(UnityEngine.SceneManagement.Scene scene)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (scene.path != MainMenuStartGameSceneSetupEditor.ScenePathForAutoUpgrade)
            {
                return;
            }

            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            var root = GameObject.Find("MainMenuStartGameRoot");
            if (root == null)
            {
                return;
            }

            TitleScreenChromeApply.Apply(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            SessionState.SetBool(SessionKey, true);
        }
    }

    [InitializeOnLoad]
    internal static class MainMenuStartGameOffBeatArchiveAutoUpgrade
    {
        private const string SessionKey = "FC_MainMenuStartGame_OffBeatArchive_v1";

        static MainMenuStartGameOffBeatArchiveAutoUpgrade()
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

            SessionState.SetBool(SessionKey, true);

            var overlay = GameObject.Find("OffBeatArchiveOverlay");
            if (overlay != null
                && overlay.GetComponent<OffBeatArchiveController>() != null
                && overlay.transform.Find("ArchivePanel/PlayerRoot") != null)
            {
                return;
            }

            MainMenuStartGameSceneSetupEditor.UpgradeOffBeatArchivePlayer();
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
#endif

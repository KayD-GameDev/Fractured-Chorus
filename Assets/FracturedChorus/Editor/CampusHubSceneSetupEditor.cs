#if UNITY_EDITOR
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Hub;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class CampusHubSceneSetupEditor
    {
        private const string ScenePath = "Assets/FracturedChorus/Scenes/CampusHub.unity";
        private const string DayBgPath = "Assets/FracturedChorus/Art/Backgrounds/lumina-city-town-map-bg_v1.png";
        private const string NightBgPath = "Assets/FracturedChorus/Art/Backgrounds/lumina-city-town-map-bg_night_v1.png";
        private const string UiRoot = "Assets/FracturedChorus/Art/UI/TownMap/";

        [MenuItem("Fractured Chorus/Create CampusHub Scene")]
        public static void CreateCampusHubScene()
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
            Debug.Log($"[Fractured Chorus] Saved {ScenePath}");
        }

        [MenuItem("Fractured Chorus/Setup CampusHub Scene Hierarchy")]
        public static void SetupCampusHubSceneHierarchy()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Setup CampusHub",
                    "Không thể Setup Hierarchy khi đang Play Mode.\nDừng Play (Exit Play Mode) rồi chạy lại menu.",
                    "OK");
                return;
            }

            var existing = GameObject.Find("CampusHubRoot");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog(
                        "Setup CampusHub",
                        "CampusHubRoot already exists. Delete and recreate hierarchy?",
                        "Recreate",
                        "Cancel"))
                {
                    return;
                }

                Undo.DestroyObjectImmediate(existing);
            }

            ConfigureCamera();
            BuildHierarchy();
            EnsureBuildSettings();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] CampusHub hierarchy rebuilt. Save the scene.");
        }

        [MenuItem("Fractured Chorus/Wire Town Map Status Menu")]
        public static void WireTownMapStatusMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Wire Status Menu",
                    "Không thể wire khi đang Play Mode.\nExit Play Mode rồi chạy lại.",
                    "OK");
                return;
            }

            var townMap = UnityEngine.Object.FindAnyObjectByType<TownMapView>();
            if (townMap == null)
            {
                EditorUtility.DisplayDialog("Wire Status Menu", "Không tìm thấy TownMapView trong scene.", "OK");
                return;
            }

            var oldMenu = townMap.transform.Find("StatusMenu");
            if (oldMenu != null)
            {
                Undo.DestroyObjectImmediate(oldMenu.gameObject);
            }

            EnsureStatusMenuSpriteImport();

            var built = MetaStatusMenuUI.Build(townMap.transform);
            var sfx = townMap.GetComponentInChildren<TownMapSfxController>(true);
            if (sfx != null)
            {
                built.Menu.BindSfx(sfx);
            }

            var so = new SerializedObject(townMap);
            so.FindProperty("menuButton").objectReferenceValue = built.MenuButton;
            so.FindProperty("statusMenu").objectReferenceValue = built.Menu;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] Wired Town Map MENU button + Status panel (v6 art). Save the scene.");
        }

        [MenuItem("Fractured Chorus/Wire Social Stats Overlay")]
        public static void WireSocialStatsOverlay()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Wire Social Stats",
                    "Không thể wire khi đang Play Mode.\nExit Play Mode rồi chạy lại.",
                    "OK");
                return;
            }

            var townMap = UnityEngine.Object.FindAnyObjectByType<TownMapView>();
            if (townMap == null)
            {
                EditorUtility.DisplayDialog("Wire Social Stats", "Không tìm thấy TownMapView trong scene.", "OK");
                return;
            }

            var statusMenu = townMap.GetComponentInChildren<MetaStatusMenuUI>(true);
            if (statusMenu == null)
            {
                var built = MetaStatusMenuUI.Build(townMap.transform);
                statusMenu = built.Menu;
                var sfx = townMap.GetComponentInChildren<TownMapSfxController>(true);
                if (sfx != null)
                {
                    statusMenu.BindSfx(sfx);
                }
            }

            statusMenu.EnsureSocialStatsOverlay(townMap.transform);
            EditorUtility.SetDirty(statusMenu);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] Wired Social Stats Overlay under TownMap. Save the scene.");
        }

        private static void EnsureStatusMenuSpriteImport()
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

                var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                foreach (var guid in guids)
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

        private static void ConfigureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            cam.orthographic = true;
            cam.backgroundColor = new Color(0.05f, 0.08f, 0.14f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        private static void BuildHierarchy()
        {
            CombatInputSetup.EnsureEventSystem();

            var root = new GameObject("CampusHubRoot");
            var controller = root.AddComponent<CampusHubController>();

            var canvasGo = new GameObject("CampusHubCanvas");
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var townMapGo = new GameObject("TownMap");
            townMapGo.transform.SetParent(canvasGo.transform, false);
            Stretch(townMapGo.AddComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var townMap = townMapGo.AddComponent<TownMapView>();

            var mapRoot = new GameObject("MapRoot");
            mapRoot.transform.SetParent(townMapGo.transform, false);
            var mapRootRect = mapRoot.AddComponent<RectTransform>();
            Stretch(mapRootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var dayBg = CreateImage("DayBackground", mapRoot.transform, LoadSprite(DayBgPath), Color.white);
            Stretch(dayBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dayBg.preserveAspect = false;

            var nightBg = CreateImage("NightBackground", mapRoot.transform, LoadSprite(NightBgPath), Color.white);
            Stretch(nightBg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            nightBg.preserveAspect = false;
            nightBg.gameObject.SetActive(false);

            var pinTemplate = CreatePinTemplate(mapRoot.transform);

            var header = CreatePanel("SelectMapHeader", townMapGo.transform, Color.clear);
            Stretch(header, new Vector2(0f, 1f), new Vector2(0.42f, 1f), new Vector2(24f, -140f), new Vector2(-8f, -16f));
            var headerPin = CreateImage("HeaderPin", header, LoadSprite(UiRoot + "townmap_header_pin.png"), Color.white);
            Stretch(headerPin.rectTransform, new Vector2(0f, 0.55f), new Vector2(0f, 1f), new Vector2(0f, -8f), new Vector2(72f, 0f));
            var selectTitle = CreateText("SelectTitle", header, "SELECT MAP", 42, TextAnchor.MiddleLeft);
            Stretch(selectTitle.rectTransform, new Vector2(0f, 0.55f), new Vector2(1f, 1f), new Vector2(84f, 0f), Vector2.zero);
            selectTitle.fontStyle = FontStyle.Bold;
            var selectSubtitle = CreateText("SelectSubtitle", header, "Where should I go?", 20, TextAnchor.UpperLeft);
            Stretch(selectSubtitle.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.55f), new Vector2(84f, 0f), new Vector2(0f, -4f));
            selectSubtitle.color = new Color(0.7f, 0.9f, 0.95f);

            var wordmark = CreateText("Wordmark", townMapGo.transform, "TOWNMAP", 72, TextAnchor.LowerLeft);
            Stretch(wordmark.rectTransform, new Vector2(0f, 0f), new Vector2(0.45f, 0f), new Vector2(16f, 8f), new Vector2(0f, 96f));
            wordmark.fontStyle = FontStyle.Bold;
            wordmark.color = new Color(0.05f, 0.12f, 0.28f, 0.55f);

            var slash = CreateSlashBanner(townMapGo.transform);
            var district = CreateDistrictPanel(townMapGo.transform);
            var promptBar = CreatePromptBar(townMapGo.transform);
            var wordmarkImage = CreateImage("WordmarkImage", townMapGo.transform, LoadSprite(UiRoot + "townmap_wordmark.png"), Color.white);
            Stretch(wordmarkImage.rectTransform, new Vector2(0f, 0f), new Vector2(0.42f, 0f), new Vector2(8f, 4f), new Vector2(0f, 110f));
            wordmarkImage.preserveAspect = true;
            wordmarkImage.raycastTarget = false;

            var sfxGo = new GameObject("TownMapSfx");
            sfxGo.transform.SetParent(townMapGo.transform, false);
            sfxGo.AddComponent<AudioSource>();
            var sfx = sfxGo.AddComponent<TownMapSfxController>();
            var buttonClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/FracturedChorus/Audio/SFX/MainMenu_ButtonPress.wav");
            sfx.Configure(buttonClip, buttonClip, buttonClip, buttonClip);

            EnsureStatusMenuSpriteImport();

            var statusBuilt = MetaStatusMenuUI.Build(townMapGo.transform);
            statusBuilt.Menu.BindSfx(sfx);

            var townSo = new SerializedObject(townMap);
            townSo.FindProperty("mapRoot").objectReferenceValue = mapRootRect;
            townSo.FindProperty("dayBackground").objectReferenceValue = dayBg;
            townSo.FindProperty("nightBackground").objectReferenceValue = nightBg;
            townSo.FindProperty("pinTemplate").objectReferenceValue = pinTemplate;
            townSo.FindProperty("districtPanel").objectReferenceValue = district.Panel;
            townSo.FindProperty("slashBanner").objectReferenceValue = slash.Banner;
            townSo.FindProperty("selectMapTitle").objectReferenceValue = selectTitle;
            townSo.FindProperty("selectMapSubtitle").objectReferenceValue = selectSubtitle;
            townSo.FindProperty("headerPinImage").objectReferenceValue = headerPin;
            townSo.FindProperty("wordmarkLabel").objectReferenceValue = wordmark;
            townSo.FindProperty("wordmarkImage").objectReferenceValue = wordmarkImage;
            townSo.FindProperty("promptBar").objectReferenceValue = promptBar;
            townSo.FindProperty("sfx").objectReferenceValue = sfx;
            townSo.FindProperty("menuButton").objectReferenceValue = statusBuilt.MenuButton;
            townSo.FindProperty("statusMenu").objectReferenceValue = statusBuilt.Menu;
            townSo.FindProperty("pinIdle").objectReferenceValue = LoadSprite(UiRoot + "townmap_pin_idle.png");
            townSo.FindProperty("pinSelected").objectReferenceValue = LoadSprite(UiRoot + "townmap_pin_selected.png");
            townSo.FindProperty("iconSchool").objectReferenceValue = LoadSprite(UiRoot + "townmap_icon_school.png");
            townSo.FindProperty("iconShop").objectReferenceValue = LoadSprite(UiRoot + "townmap_icon_shop.png");
            townSo.FindProperty("iconFlower").objectReferenceValue = LoadSprite(UiRoot + "townmap_icon_flower.png");
            townSo.FindProperty("iconShrine").objectReferenceValue = LoadSprite(UiRoot + "townmap_icon_shrine.png");
            townSo.FindProperty("iconVault").objectReferenceValue = LoadSprite(UiRoot + "townmap_icon_vault.png");
            townSo.FindProperty("wordmarkSprite").objectReferenceValue = LoadSprite(UiRoot + "townmap_wordmark.png");
            townSo.ApplyModifiedPropertiesWithoutUndo();

            var districtSo = new SerializedObject(district.Panel);
            districtSo.FindProperty("sfx").objectReferenceValue = sfx;
            districtSo.ApplyModifiedPropertiesWithoutUndo();

            wordmark.gameObject.SetActive(false);

            var morningPanel = CreatePanel("MorningPanel", canvasGo.transform, new Color(0.05f, 0.07f, 0.12f, 0.92f));
            Stretch(morningPanel, new Vector2(0.18f, 0.28f), new Vector2(0.82f, 0.72f), Vector2.zero, Vector2.zero);
            var morningMessage = CreateText("MorningMessage", morningPanel, "Buổi sáng tại Lumina.", 26, TextAnchor.MiddleCenter);
            Stretch(morningMessage.rectTransform, new Vector2(0.06f, 0.35f), new Vector2(0.94f, 0.9f), Vector2.zero, Vector2.zero);
            var morningContinue = CreateButton(
                "MorningContinue",
                morningPanel,
                "Tiếp tục",
                LoadSprite(UiRoot + "townmap_row_selected.png"));
            Stretch(morningContinue.GetComponent<RectTransform>(), new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.22f), Vector2.zero, Vector2.zero);
            var morningUi = morningPanel.gameObject.AddComponent<MorningBeatUI>();
            var morningSo = new SerializedObject(morningUi);
            morningSo.FindProperty("messageLabel").objectReferenceValue = morningMessage;
            morningSo.FindProperty("continueButton").objectReferenceValue = morningContinue;
            morningSo.ApplyModifiedPropertiesWithoutUndo();
            morningPanel.gameObject.SetActive(false);

            var statusLabel = CreateText("StatusLabel", canvasGo.transform, "Lumina Town Map", 18, TextAnchor.LowerCenter);
            Stretch(statusLabel.rectTransform, new Vector2(0.35f, 0f), new Vector2(0.95f, 0f), new Vector2(0f, 12f), new Vector2(0f, 44f));

            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("backgroundImage").objectReferenceValue = dayBg;
            controllerSo.FindProperty("calendarView").objectReferenceValue = null;
            controllerSo.FindProperty("slashBanner").objectReferenceValue = slash.Banner;
            controllerSo.FindProperty("morningBeatUi").objectReferenceValue = morningUi;
            controllerSo.FindProperty("townMapView").objectReferenceValue = townMap;
            controllerSo.FindProperty("statusLabel").objectReferenceValue = statusLabel;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            controller.SetEditorPreview(CampusHubController.CampusHubEditorPreview.TownDay);
            EditorUtility.SetDirty(controller);
        }

        private static TownMapPinView CreatePinTemplate(Transform parent)
        {
            var go = new GameObject("PinTemplate");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(96f, 96f);

            var pinImg = go.AddComponent<Image>();
            pinImg.sprite = LoadSprite(UiRoot + "townmap_pin_idle.png");
            pinImg.raycastTarget = true;
            var button = go.AddComponent<Button>();
            button.targetGraphic = pinImg;

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            var icon = iconGo.AddComponent<Image>();
            icon.sprite = LoadSprite(UiRoot + "townmap_icon_school.png");
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Stretch(icon.rectTransform, new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.75f), Vector2.zero, Vector2.zero);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var label = labelGo.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.alignment = TextAnchor.UpperCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            Stretch(label.rectTransform, new Vector2(-0.4f, -0.55f), new Vector2(1.4f, 0f), Vector2.zero, Vector2.zero);

            var pin = go.AddComponent<TownMapPinView>();
            var so = new SerializedObject(pin);
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("pinImage").objectReferenceValue = pinImg;
            so.FindProperty("iconImage").objectReferenceValue = icon;
            so.FindProperty("labelText").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
            go.SetActive(false);
            return pin;
        }

        private static (CalendarSlashBanner Banner, Text Date, Text Phase) CreateSlashBanner(Transform parent)
        {
            var go = new GameObject("SlashBanner");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            Stretch(rect, new Vector2(0.62f, 1f), new Vector2(1f, 1f), new Vector2(0f, -150f), Vector2.zero);

            var image = go.AddComponent<Image>();
            image.sprite = LoadSprite(UiRoot + "townmap_slash_banner.png");
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var date = CreateText("DateLabel", go.transform, "01/09", 36, TextAnchor.MiddleRight);
            Stretch(date.rectTransform, new Vector2(0.08f, 0.45f), new Vector2(0.78f, 0.95f), Vector2.zero, Vector2.zero);
            date.fontStyle = FontStyle.Bold;

            var phaseIcon = CreateImage("PhaseIcon", go.transform, LoadSprite(UiRoot + "townmap_icon_sun.png"), Color.white);
            Stretch(phaseIcon.rectTransform, new Vector2(0.8f, 0.35f), new Vector2(0.96f, 0.9f), Vector2.zero, Vector2.zero);

            var phase = CreateText("PhaseLabel", go.transform, "After School", 22, TextAnchor.MiddleRight);
            Stretch(phase.rectTransform, new Vector2(0.08f, 0.15f), new Vector2(0.78f, 0.5f), Vector2.zero, Vector2.zero);
            phase.color = new Color(0.55f, 0.9f, 0.95f);

            var slot = CreateText("SlotLabel", go.transform, "Slot 0/2", 16, TextAnchor.LowerRight);
            Stretch(slot.rectTransform, new Vector2(0.08f, 0f), new Vector2(0.92f, 0.22f), Vector2.zero, Vector2.zero);

            var deadline = CreateText("DeadlineLabel", go.transform, "Vault", 14, TextAnchor.UpperRight);
            Stretch(deadline.rectTransform, new Vector2(0.08f, 0.95f), new Vector2(0.92f, 1.15f), Vector2.zero, Vector2.zero);
            deadline.color = new Color(1f, 0.85f, 0.35f);
            deadline.gameObject.SetActive(false);

            var banner = go.AddComponent<CalendarSlashBanner>();
            var so = new SerializedObject(banner);
            so.FindProperty("bannerImage").objectReferenceValue = image;
            so.FindProperty("dateLabel").objectReferenceValue = date;
            so.FindProperty("phaseLabel").objectReferenceValue = phase;
            so.FindProperty("slotLabel").objectReferenceValue = slot;
            so.FindProperty("deadlineLabel").objectReferenceValue = deadline;
            so.FindProperty("phaseIcon").objectReferenceValue = phaseIcon;
            so.FindProperty("sunSprite").objectReferenceValue = LoadSprite(UiRoot + "townmap_icon_sun.png");
            so.FindProperty("moonSprite").objectReferenceValue = LoadSprite(UiRoot + "townmap_icon_moon.png");
            so.FindProperty("dawnSprite").objectReferenceValue = LoadSprite(UiRoot + "townmap_icon_dawn.png");
            so.ApplyModifiedPropertiesWithoutUndo();
            return (banner, date, phase);
        }

        private static TownMapPromptBar CreatePromptBar(Transform parent)
        {
            var go = new GameObject("PromptBar");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            Stretch(rect, new Vector2(0.45f, 0f), new Vector2(0.98f, 0f), new Vector2(0f, 10f), new Vector2(0f, 64f));

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(8, 8, 4, 4);

            var travel = CreatePromptEntry(go.transform, "Travel", UiRoot + "townmap_prompt_travel.png");
            var info = CreatePromptEntry(go.transform, "Town Info", UiRoot + "townmap_prompt_info.png");
            var confirm = CreatePromptEntry(go.transform, "Confirm", UiRoot + "townmap_prompt_confirm.png");
            var close = CreatePromptEntry(go.transform, "Close", UiRoot + "townmap_prompt_close.png");

            var bar = go.AddComponent<TownMapPromptBar>();
            var so = new SerializedObject(bar);
            SetPromptEntry(so, "travel", travel);
            SetPromptEntry(so, "info", info);
            SetPromptEntry(so, "confirm", confirm);
            SetPromptEntry(so, "close", close);
            so.FindProperty("travelSprite").objectReferenceValue = LoadSprite(UiRoot + "townmap_prompt_travel.png");
            so.FindProperty("infoSprite").objectReferenceValue = LoadSprite(UiRoot + "townmap_prompt_info.png");
            so.FindProperty("confirmSprite").objectReferenceValue = LoadSprite(UiRoot + "townmap_prompt_confirm.png");
            so.FindProperty("closeSprite").objectReferenceValue = LoadSprite(UiRoot + "townmap_prompt_close.png");
            so.FindProperty("confirmKeySprite").objectReferenceValue = LoadSprite(UiRoot + "townmap_prompt_key_enter.png");
            so.FindProperty("closeKeySprite").objectReferenceValue = LoadSprite(UiRoot + "townmap_prompt_key_esc.png");
            so.ApplyModifiedPropertiesWithoutUndo();
            return bar;
        }

        private static (Image Icon, Text Label) CreatePromptEntry(Transform parent, string label, string spritePath)
        {
            var go = new GameObject($"Prompt_{label}");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150f, 48f);
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 140f;
            le.preferredWidth = 150f;

            var icon = CreateImage("Icon", go.transform, LoadSprite(spritePath), Color.white);
            Stretch(icon.rectTransform, new Vector2(0f, 0.15f), new Vector2(0f, 0.85f), new Vector2(0f, 0f), new Vector2(36f, 0f));

            var text = CreateText("Label", go.transform, label, 16, TextAnchor.MiddleLeft);
            Stretch(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(42f, 0f), Vector2.zero);
            return (icon, text);
        }

        private static void SetPromptEntry(SerializedObject so, string field, (Image Icon, Text Label) entry)
        {
            var prop = so.FindProperty(field);
            prop.FindPropertyRelative("Icon").objectReferenceValue = entry.Icon;
            prop.FindPropertyRelative("Label").objectReferenceValue = entry.Label;
        }

        private static (DistrictSelectPanel Panel, Button Confirm) CreateDistrictPanel(Transform parent)
        {
            var go = new GameObject("DistrictPanel");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            Stretch(rect, new Vector2(0.02f, 0.12f), new Vector2(0.34f, 0.78f), Vector2.zero, Vector2.zero);

            var bg = go.AddComponent<Image>();
            bg.sprite = LoadSprite(UiRoot + "townmap_panel_left.png");
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;

            var headerTitle = CreateText("HeaderTitle", go.transform, "SELECT MAP", 28, TextAnchor.UpperLeft);
            Stretch(headerTitle.rectTransform, new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.98f), Vector2.zero, Vector2.zero);
            headerTitle.fontStyle = FontStyle.Bold;

            var headerSubtitle = CreateText("HeaderSubtitle", go.transform, "Location", 18, TextAnchor.UpperLeft);
            Stretch(headerSubtitle.rectTransform, new Vector2(0.08f, 0.8f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);
            headerSubtitle.color = new Color(0.7f, 0.9f, 0.95f);

            var headerPin = CreateImage("PanelHeaderPin", go.transform, LoadSprite(UiRoot + "townmap_header_pin.png"), Color.white);
            Stretch(headerPin.rectTransform, new Vector2(0.78f, 0.88f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero);

            var rowRoot = new GameObject("RowRoot");
            rowRoot.transform.SetParent(go.transform, false);
            var rowRootRect = rowRoot.AddComponent<RectTransform>();
            Stretch(rowRootRect, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.78f), Vector2.zero, Vector2.zero);
            var layout = rowRoot.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var rowTemplate = CreateButton(
                "RowTemplate",
                rowRoot.transform,
                "Sub-location",
                LoadSprite(UiRoot + "townmap_row_normal.png"));
            var rowImage = rowTemplate.GetComponent<Image>();
            rowImage.type = Image.Type.Sliced;
            var le = rowTemplate.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 52f;
            rowTemplate.gameObject.SetActive(false);

            var confirm = CreateButton(
                "ConfirmButton",
                go.transform,
                "Confirm",
                LoadSprite(UiRoot + "townmap_row_selected.png"));
            Stretch(confirm.GetComponent<RectTransform>(), new Vector2(0.1f, 0.04f), new Vector2(0.48f, 0.14f), Vector2.zero, Vector2.zero);
            var confirmIcon = CreateImage("ConfirmIcon", confirm.transform, LoadSprite(UiRoot + "townmap_prompt_confirm.png"), Color.white);
            Stretch(confirmIcon.rectTransform, new Vector2(0f, 0.2f), new Vector2(0f, 0.8f), new Vector2(8f, 0f), new Vector2(36f, 0f));

            var close = CreateButton(
                "CloseButton",
                go.transform,
                "Close",
                LoadSprite(UiRoot + "townmap_row_normal.png"));
            Stretch(close.GetComponent<RectTransform>(), new Vector2(0.52f, 0.04f), new Vector2(0.9f, 0.14f), Vector2.zero, Vector2.zero);
            var closeIcon = CreateImage("CloseIcon", close.transform, LoadSprite(UiRoot + "townmap_prompt_close.png"), Color.white);
            Stretch(closeIcon.rectTransform, new Vector2(0f, 0.2f), new Vector2(0f, 0.8f), new Vector2(8f, 0f), new Vector2(36f, 0f));

            var panel = go.AddComponent<DistrictSelectPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("panelBackground").objectReferenceValue = bg;
            so.FindProperty("headerTitle").objectReferenceValue = headerTitle;
            so.FindProperty("headerSubtitle").objectReferenceValue = headerSubtitle;
            so.FindProperty("headerPin").objectReferenceValue = headerPin;
            so.FindProperty("rowRoot").objectReferenceValue = rowRootRect;
            so.FindProperty("rowTemplate").objectReferenceValue = rowTemplate;
            so.FindProperty("rowNormalSprite").objectReferenceValue = LoadSprite(UiRoot + "townmap_row_normal.png");
            so.FindProperty("rowSelectedSprite").objectReferenceValue = LoadSprite(UiRoot + "townmap_row_selected.png");
            so.FindProperty("confirmButton").objectReferenceValue = confirm;
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.ApplyModifiedPropertiesWithoutUndo();
            go.SetActive(false);
            return (panel, confirm);
        }

        private static void EnsureBuildSettings()
        {
            var scenes = new[]
            {
                "Assets/FracturedChorus/Scenes/MainMenuStartGame.unity",
                "Assets/FracturedChorus/Scenes/PrologueVN.unity",
                ScenePath,
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

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            var image = go.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            return image;
        }

        private static Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Sprite background = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            var useLightRow = background != null && background.name.Contains("selected");
            if (background != null)
            {
                image.sprite = background;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.12f, 0.28f, 0.4f, 0.95f);
            }

            var button = go.AddComponent<Button>();
            var text = CreateText("Label", go.transform, label, 18, TextAnchor.MiddleCenter);
            text.color = useLightRow ? new Color(0.04f, 0.1f, 0.2f) : Color.white;
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(40f, 0f), Vector2.zero);
            return button;
        }

        private static void Stretch(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
#endif

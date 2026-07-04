#if UNITY_EDITOR
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Data;
using FracturedChorus.RunMap;
using FracturedChorus.RunMap.Core;
using FracturedChorus.RunMap.UI;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class RunMapSceneSetupEditor
    {
        private const string ScenePath = "Assets/FracturedChorus/Scenes/RunMapPrototype.unity";
        private const string TemplateAssetPath = "Assets/FracturedChorus/Data/ScriptableObjects/Presets/MapTemplate_Default.asset";
        private const string CadenceLayoutAssetPath = "Assets/FracturedChorus/Data/ScriptableObjects/Presets/CadenceMapLayout_Default.asset";
        private const string PinkyVaultConfigPath = "Assets/FracturedChorus/Data/ScriptableObjects/Presets/PinkyVaultConfig_Default.asset";
        private const string CadenceBackgroundPath = "Assets/FracturedChorus/Art/Backgrounds/cadence_macro_map_bg_v2_5fingers.png";

        [MenuItem("Fractured Chorus/Run Map/Setup Cadence Macro Layer", false, 20)]
        public static void SetupCadenceMacroMapLayer()
        {
            var root = GameObject.Find("RunMapRoot");
            if (root == null)
            {
                EditorUtility.DisplayDialog(
                    "Cadence Macro Map",
                    "Không tìm thấy RunMapRoot. Chạy Run Map → Setup Scene Hierarchy trước.",
                    "OK");
                return;
            }

            var canvas = GameObject.Find("RunMapCanvas");
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Cadence Macro Map", "Không tìm thấy RunMapCanvas.", "OK");
                return;
            }

            var layout = EnsureCadenceMapLayoutAsset();
            var pinkyConfig = EnsurePinkyVaultConfigAsset();
            var cadence = root.GetComponent<CadenceMapController>() ?? Undo.AddComponent<CadenceMapController>(root);
            var bootstrap = root.GetComponent<RunMapBootstrap>();
            var controller = root.GetComponent<RunMapController>();

            var innerRoot = GameObject.Find("InnerMapLayer");
            if (innerRoot == null)
            {
                innerRoot = CreateUiObject("InnerMapLayer", canvas.transform);
                StretchRect(innerRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            ReparentIfExists("MapScrollView", innerRoot.transform);
            ReparentIfExists("LegendPanel", innerRoot.transform);

            foreach (Transform child in canvas.transform)
            {
                if (child.gameObject == innerRoot || child.name == "MacroMapLayer")
                {
                    continue;
                }

                if (child.name == "TopBar")
                {
                    continue;
                }

                child.SetParent(innerRoot.transform, true);
            }

            var macroRoot = GameObject.Find("MacroMapLayer");
            if (macroRoot == null)
            {
                macroRoot = CreateUiObject("MacroMapLayer", canvas.transform);
                StretchRect(macroRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                macroRoot.transform.SetAsFirstSibling();
            }

            var macroView = macroRoot.GetComponent<CadenceMacroMapView>() ?? Undo.AddComponent<CadenceMacroMapView>(macroRoot);
            var bgGo = GameObject.Find("MacroBackground");
            if (bgGo == null)
            {
                bgGo = CreateUiObject("MacroBackground", macroRoot.transform);
                StretchRect(bgGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var bgImage = bgGo.AddComponent<Image>();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CadenceBackgroundPath);
                if (sprite == null)
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(CadenceBackgroundPath);
                    if (tex != null)
                    {
                        sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                }

                bgImage.sprite = sprite;
                bgImage.preserveAspect = false;
                bgImage.color = Color.white;
                bgImage.raycastTarget = false;
            }

            var territoryLayerGo = GameObject.Find("TerritoryLayer");
            if (territoryLayerGo == null)
            {
                territoryLayerGo = CreateUiObject("TerritoryLayer", macroRoot.transform);
                StretchRect(territoryLayerGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            VaultTerritoryGraphic template = null;
            var templateGo = GameObject.Find("TerritoryTemplate");
            if (templateGo == null)
            {
                templateGo = CreateUiObject("TerritoryTemplate", territoryLayerGo.transform);
                StretchRect(templateGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                template = templateGo.AddComponent<VaultTerritoryGraphic>();
                templateGo.SetActive(false);
            }
            else
            {
                template = templateGo.GetComponent<VaultTerritoryGraphic>();
            }

            var hintGo = GameObject.Find("MacroHint");
            Text hintLabel;
            if (hintGo == null)
            {
                hintGo = CreateUiObject("MacroHint", macroRoot.transform);
                StretchRect(hintGo, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.12f), Vector2.zero, Vector2.zero);
                hintLabel = hintGo.AddComponent<Text>();
                hintLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                hintLabel.fontSize = 22;
                hintLabel.alignment = TextAnchor.MiddleCenter;
                hintLabel.color = new Color(0.92f, 0.94f, 0.96f);
                hintLabel.text = "Select a Vault to Resonance Dive.";
            }
            else
            {
                hintLabel = hintGo.GetComponent<Text>();
            }

            var backBtnGo = GameObject.Find("BackToMacroButton");
            Button backButton;
            if (backBtnGo == null)
            {
                backBtnGo = CreateUiObject("BackToMacroButton", macroRoot.transform);
                var backRect = backBtnGo.GetComponent<RectTransform>();
                backRect.anchorMin = new Vector2(0.02f, 0.92f);
                backRect.anchorMax = new Vector2(0.18f, 0.98f);
                backRect.offsetMin = Vector2.zero;
                backRect.offsetMax = Vector2.zero;
                backBtnGo.AddComponent<Image>().color = new Color(0.18f, 0.2f, 0.24f, 0.92f);
                backButton = backBtnGo.AddComponent<Button>();
                var label = CreateText("Label", backBtnGo.transform, "← Cadence", 18, TextAnchor.MiddleCenter);
                StretchRect(label.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
            else
            {
                backButton = backBtnGo.GetComponent<Button>();
            }

            SetSerializedField(macroView, "layout", layout);
            SetSerializedField(macroView, "backgroundImage", bgGo.GetComponent<Image>());
            SetSerializedField(macroView, "territoryLayer", territoryLayerGo.GetComponent<RectTransform>());
            SetSerializedField(macroView, "territoryTemplate", template);
            SetSerializedField(macroView, "hintLabel", hintLabel);

            SetSerializedField(cadence, "layout", layout);
            SetSerializedField(cadence, "pinkyVaultConfig", pinkyConfig);
            SetSerializedField(cadence, "macroView", macroView);
            SetSerializedField(cadence, "macroMapRoot", macroRoot);
            SetSerializedField(cadence, "innerMapRoot", innerRoot);
            SetSerializedField(cadence, "mapScrollView", GameObject.Find("MapScrollView"));
            SetSerializedField(cadence, "legendPanel", GameObject.Find("LegendPanel"));
            SetSerializedField(cadence, "innerController", controller);
            SetSerializedField(cadence, "bootstrap", bootstrap);
            SetSerializedField(cadence, "backToMacroButton", backButton);
            SetSerializedField(cadence, "simulateBossVictoryOnReturn", true);

            var topBar = GameObject.Find("TopBar");
            if (topBar != null)
            {
                var status = topBar.transform.Find("Status")?.GetComponent<Text>();
                SetSerializedField(cadence, "statusLabel", status);
            }

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = macroRoot;
            Debug.Log("[Fractured Chorus] Cadence macro map layer wired — Save scene → Play.");
        }

        private static CadenceMapLayoutSO EnsureCadenceMapLayoutAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<CadenceMapLayoutSO>(CadenceLayoutAssetPath);
            if (existing != null)
            {
                return existing;
            }

            if (!AssetDatabase.IsValidFolder("Assets/FracturedChorus/Data/ScriptableObjects/Presets"))
            {
                EnsureDefaultMapTemplateAsset();
            }

            var asset = ScriptableObject.CreateInstance<CadenceMapLayoutSO>();
            asset.territories = CadenceMapLayoutSO.DefaultTerritories();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CadenceBackgroundPath);
            if (sprite == null)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(CadenceBackgroundPath);
                if (tex != null)
                {
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }

            asset.backgroundSprite = sprite;
            AssetDatabase.CreateAsset(asset, CadenceLayoutAssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static PinkyVaultConfigSO EnsurePinkyVaultConfigAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PinkyVaultConfigSO>(PinkyVaultConfigPath);
            if (existing != null)
            {
                return existing;
            }

            if (!AssetDatabase.IsValidFolder("Assets/FracturedChorus/Data/ScriptableObjects/Presets"))
            {
                EnsureDefaultMapTemplateAsset();
            }

            var asset = ScriptableObject.CreateInstance<PinkyVaultConfigSO>();
            asset.pulse = PinkyVaultConfigSO.SectorConfig.Default(PinkySectorId.Pulse);
            asset.echo = PinkyVaultConfigSO.SectorConfig.Default(PinkySectorId.Echo);
            asset.canticle = PinkyVaultConfigSO.SectorConfig.Default(PinkySectorId.Canticle);
            AssetDatabase.CreateAsset(asset, PinkyVaultConfigPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        [MenuItem("Fractured Chorus/Run Map/Upgrade Legend Panel", false, 40)]
        public static void UpgradeRunMapLegendPanel()
        {
            var panelGo = GameObject.Find("LegendPanel");
            if (panelGo == null)
            {
                EditorUtility.DisplayDialog(
                    "Upgrade Legend Panel",
                    "Không tìm thấy LegendPanel trong scene active. Mở RunMapPrototype hoặc chạy Run Map → Setup Scene Hierarchy.",
                    "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(panelGo, "Upgrade Run Map Legend Panel");
            RebuildLegendPanel(panelGo);
            EditorSceneManager.MarkSceneDirty(panelGo.scene);
            Selection.activeGameObject = panelGo;
            Debug.Log("[Fractured Chorus] Legend panel upgraded — Save scene.");
        }

        [MenuItem("Fractured Chorus/Run Map/Save Scene Upgrades", false, 50)]
        public static void SaveRunMapSceneUpgrades()
        {
            var panelGo = GameObject.Find("LegendPanel");
            if (panelGo != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(panelGo, "Upgrade Run Map Legend Panel");
                RebuildLegendPanel(panelGo);
            }

            EnsureScrollDriverInScene();
            if (GameObject.Find("RunMapRoot") != null)
            {
                SetupCadenceMacroMapLayer();
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] Run map scene upgraded — Save scene (Ctrl+S).");
        }

        /// <summary>Mở RunMapPrototype, rebuild legend + scroll driver, save (batch / CI).</summary>
        public static void BatchSaveRunMapScene()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError($"[Fractured Chorus] Scene not found: {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var panelGo = GameObject.Find("LegendPanel");
            if (panelGo != null)
            {
                RebuildLegendPanel(panelGo);
            }

            EnsureScrollDriverInScene();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Fractured Chorus] RunMapPrototype saved (legend + scroll).");
            EditorApplication.Exit(0);
        }

        /// <summary>Legacy batch entry — legend only.</summary>
        public static void BatchSaveRunMapLegendPanel() => BatchSaveRunMapScene();

        private static void EnsureScrollDriverInScene()
        {
            var scrollGo = GameObject.Find("MapScrollView");
            if (scrollGo == null)
            {
                return;
            }

            var scroll = scrollGo.GetComponent<ScrollRect>();
            if (scroll == null)
            {
                return;
            }

            var driver = scrollGo.GetComponent<RunMapScrollDriver>() ?? scrollGo.AddComponent<RunMapScrollDriver>();
            SetSerializedField(driver, "scrollRect", scroll);
            driver.ApplyScrollFeel();
            EditorUtility.SetDirty(driver);

            var mapView = Object.FindAnyObjectByType<RunMapUIView>();
            if (mapView != null)
            {
                SetSerializedField(mapView, "scrollDriver", driver);
                EditorUtility.SetDirty(mapView);
            }
        }

        private static void RebuildLegendPanel(GameObject panelGo)
        {
            for (var i = panelGo.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(panelGo.transform.GetChild(i).gameObject);
            }

            StretchRect(panelGo, new Vector2(0.74f, 0.06f), new Vector2(0.96f, 0.94f), Vector2.zero, Vector2.zero);
            EnsureLegendPanelChrome(panelGo);
            PopulateLegendPanelContent(panelGo.transform);
            panelGo.GetComponent<RunMapLegendPanelView>()?.Apply();
        }

        [MenuItem("Fractured Chorus/Run Map/Setup Scene Hierarchy", false, 10)]
        public static void SetupRunMapSceneHierarchy()
        {
            var existing = GameObject.Find("RunMapRoot");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog(
                        "Setup Run Map Scene",
                        "RunMapRoot đã tồn tại. Xóa và tạo lại hierarchy?",
                        "Tạo lại",
                        "Cancel"))
                {
                    return;
                }

                Undo.DestroyObjectImmediate(existing);
            }

            EnsureCamera();
            EnsureEventSystem();

            var root = new GameObject("RunMapRoot");
            Undo.RegisterCreatedObjectUndo(root, "Create RunMapRoot");

            var controller = Undo.AddComponent<RunMapController>(root);
            var bootstrap = Undo.AddComponent<RunMapBootstrap>(root);

            var canvas = CreateRunMapCanvas(root.transform);
            var topBar = CreateTopBar(canvas.transform);
            var scroll = CreateMapScrollView(canvas.transform, out var mapView, out var contentRect);
            var legend = CreateLegendPanel(canvas.transform);

            WireBootstrap(bootstrap, controller);
            WireController(controller, mapView, topBar.status, topBar.seed);
            SetupCadenceMacroMapLayer();

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;

            Debug.Log("[Fractured Chorus] Run map + Cadence macro layer created. Save scene → Play.");
        }

        [MenuItem("Fractured Chorus/Run Map/Create Prototype Scene", false, 0)]
        public static void CreateRunMapPrototypeScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.backgroundColor = new Color(0.11f, 0.12f, 0.15f);
                cam.orthographic = true;
            }

            SetupRunMapSceneHierarchy();
            EnsureDefaultMapTemplateAsset();

            if (!AssetDatabase.IsValidFolder("Assets/FracturedChorus/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus", "Scenes");
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[Fractured Chorus] Saved {ScenePath}. Add to Build Settings → Play.");
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            var camGo = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(camGo, "Create Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.11f, 0.12f, 0.15f);
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

        private static Canvas CreateRunMapCanvas(Transform parent)
        {
            var canvasGo = new GameObject("RunMapCanvas");
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create RunMapCanvas");
            canvasGo.transform.SetParent(parent, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static (Text status, Text seed) CreateTopBar(Transform canvas)
        {
            var bar = CreateUiObject("TopBar", canvas);
            StretchRect(bar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -56f), Vector2.zero);
            bar.AddComponent<Image>().color = new Color(0.12f, 0.15f, 0.18f, 0.95f);

            var title = CreateText("Title", bar.transform, "Fractured Chorus — Run Map (StS clone)", 20, TextAnchor.MiddleLeft);
            StretchRect(title.gameObject, new Vector2(0f, 0f), new Vector2(0.55f, 1f), new Vector2(16f, 0f), new Vector2(-8f, 0f));

            var seed = CreateText("SeedLabel", bar.transform, "Seed —", 14, TextAnchor.MiddleRight);
            StretchRect(seed.gameObject, new Vector2(0.55f, 0.5f), new Vector2(1f, 1f), new Vector2(8f, 0f), new Vector2(-16f, -4f));

            var status = CreateText("StatusLabel", bar.transform, "Select F1 node to start.", 14, TextAnchor.MiddleRight);
            StretchRect(status.gameObject, new Vector2(0.55f, 0f), new Vector2(1f, 0.5f), new Vector2(8f, 4f), new Vector2(-16f, 0f));

            return (status, seed);
        }

        private static ScrollRect CreateMapScrollView(Transform canvas, out RunMapUIView mapView, out RectTransform contentRect)
        {
            var scrollGo = CreateUiObject("MapScrollView", canvas);
            StretchRect(scrollGo, new Vector2(0.02f, 0.05f), new Vector2(0.78f, 0.95f), Vector2.zero, Vector2.zero);
            scrollGo.AddComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 0.92f);
            scrollGo.GetComponent<Image>().raycastTarget = false;

            var viewport = CreateUiObject("Viewport", scrollGo.transform);
            StretchRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = Color.white;
            viewportImage.raycastTarget = false;

            var content = CreateUiObject("MapContent", viewport.transform);
            contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0f);
            contentRect.anchorMax = new Vector2(0.5f, 0f);
            contentRect.pivot = new Vector2(0.5f, 0f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(640f, 1400f);

            var connectionsLayer = CreateUiObject("ConnectionsLayer", content.transform);
            ConfigureBottomLayer(connectionsLayer, contentRect.sizeDelta);

            var nodesLayer = CreateUiObject("NodesLayer", content.transform);
            ConfigureBottomLayer(nodesLayer, contentRect.sizeDelta);

            var floorLabelsLayer = CreateUiObject("FloorLabelsLayer", content.transform);
            ConfigureBottomLayer(floorLabelsLayer, contentRect.sizeDelta);

            var nodeTemplate = CreateNodeTemplate(nodesLayer.transform);
            var connectionTemplate = CreateConnectionTemplate(connectionsLayer.transform);

            mapView = Undo.AddComponent<RunMapUIView>(content);
            SetSerializedField(mapView, "connectionsLayer", connectionsLayer.GetComponent<RectTransform>());
            SetSerializedField(mapView, "nodesLayer", nodesLayer.GetComponent<RectTransform>());
            SetSerializedField(mapView, "floorLabelsLayer", floorLabelsLayer.GetComponent<RectTransform>());
            SetSerializedField(mapView, "nodeTemplate", nodeTemplate);
            SetSerializedField(mapView, "connectionTemplate", connectionTemplate);
            SetSerializedField(mapView, "fitToViewport", true);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var scrollDriver = Undo.AddComponent<RunMapScrollDriver>(scrollGo);
            scrollDriver.ApplyScrollFeel();

            SetSerializedField(mapView, "scrollRect", scroll);
            SetSerializedField(mapView, "scrollDriver", scrollDriver);

            return scroll;
        }

        private static MapNodeView CreateNodeTemplate(Transform parent)
        {
            var go = CreateUiObject("NodeTemplate", parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(36f, 36f);
            go.SetActive(false);

            var stroke = CreateUiObject("Stroke", go.transform);
            StretchRect(stroke, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var strokeImg = stroke.AddComponent<Image>();
            strokeImg.sprite = UiCircleSpriteUtil.Circle;
            strokeImg.color = Color.white;

            var fill = CreateUiObject("Fill", go.transform);
            StretchRect(fill, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
            var fillImg = fill.AddComponent<Image>();
            fillImg.sprite = UiCircleSpriteUtil.Circle;
            fillImg.color = Color.white;

            var label = CreateText("Label", go.transform, "?", MapLayoutConstants.NodeLabelFontSize(MapNodeType.Battle, false), TextAnchor.MiddleCenter);
            StretchRect(label.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var button = go.AddComponent<Button>();
            button.targetGraphic = fillImg;

            var view = Undo.AddComponent<MapNodeView>(go);
            view.WireImages(fillImg, strokeImg, label, button);
            return view;
        }

        private static MapConnectionLineView CreateConnectionTemplate(Transform parent)
        {
            var go = CreateUiObject("ConnectionTemplate", parent);
            go.SetActive(false);
            var image = go.AddComponent<Image>();
            image.sprite = UiCircleSpriteUtil.White;
            image.raycastTarget = false;
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.85f);
            var line = Undo.AddComponent<MapConnectionLineView>(go);
            line.WireImage(image);
            return line;
        }

        private static GameObject CreateLegendPanel(Transform canvas)
        {
            var panel = CreateUiObject("LegendPanel", canvas);
            StretchRect(panel, new Vector2(0.74f, 0.06f), new Vector2(0.96f, 0.94f), Vector2.zero, Vector2.zero);
            EnsureLegendPanelChrome(panel);
            PopulateLegendPanelContent(panel.transform);
            return panel;
        }

        private static void EnsureLegendPanelChrome(GameObject panel)
        {
            var image = panel.GetComponent<Image>();
            if (image == null)
            {
                image = panel.AddComponent<Image>();
            }

            image.color = new Color(0.1f, 0.11f, 0.13f, 0.94f);

            var vlg = panel.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
            {
                vlg = panel.AddComponent<VerticalLayoutGroup>();
            }

            vlg.padding = new RectOffset(22, 22, 28, 22);
            vlg.spacing = MapLayoutConstants.LegendVerticalSpacing;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = false;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            var views = panel.GetComponents<RunMapLegendPanelView>();
            if (views.Length == 0)
            {
                panel.AddComponent<RunMapLegendPanelView>();
            }
            else
            {
                for (var i = 1; i < views.Length; i++)
                {
                    Object.DestroyImmediate(views[i]);
                }
            }
        }

        private static void PopulateLegendPanelContent(Transform panel)
        {
            CreateLegendTitle(panel, "Node types (FC)", MapLayoutConstants.LegendTitleFontSize);

            var entries = new[]
            {
                (MapNodeType.Battle, "Battle — standard combat"),
                (MapNodeType.Event, "Event — random event"),
                (MapNodeType.Elite, "Elite — hard combat"),
                (MapNodeType.Camp, "Camp — rest / heal"),
                (MapNodeType.Relay, "Relay — shop"),
                (MapNodeType.Treasure, "Treasure — chest"),
                (MapNodeType.Boss, "Boss — Oni F16")
            };

            foreach (var (type, desc) in entries)
            {
                CreateLegendRow(panel, type, desc);
            }

            CreateLegendFlexibleSpacer(panel);

            var hint = CreateText(
                "Hint",
                panel,
                "Scroll map · click F1 → follow path\nOrange line = chosen path · StS 7×15 + boss F16",
                MapLayoutConstants.LegendHintFontSize,
                TextAnchor.UpperLeft);
            hint.color = new Color(0.62f, 0.65f, 0.7f);
            hint.lineSpacing = MapLayoutConstants.LegendHintLineSpacing;
            AddLayoutElement(hint.gameObject, minHeight: MapLayoutConstants.LegendHintMinHeight, flexibleHeight: 0f);
        }

        private static void CreateLegendTitle(Transform parent, string text, int fontSize)
        {
            var title = CreateText("LegendTitle", parent, text, fontSize, TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.92f, 0.94f, 0.96f);
            AddLayoutElement(title.gameObject, minHeight: MapLayoutConstants.LegendTitleHeight, flexibleHeight: 0f);
        }

        private static void CreateLegendFlexibleSpacer(Transform parent)
        {
            var spacer = CreateUiObject("LegendSpacer", parent);
            AddLayoutElement(spacer, minHeight: 0f, flexibleHeight: 0f);
        }

        private static void CreateLegendRow(Transform parent, MapNodeType type, string desc)
        {
            var row = CreateUiObject($"Legend_{type}", parent);
            AddLayoutElement(
                row,
                minHeight: MapLayoutConstants.LegendRowMinHeight,
                flexibleHeight: 0f,
                flexibleWidth: 0f);

            var rowFitter = row.AddComponent<ContentSizeFitter>();
            rowFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(0f, 1f);
            rowRect.pivot = new Vector2(0f, 1f);

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = MapLayoutConstants.LegendRowHorizontalSpacing;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.padding = new RectOffset(6, 6, 4, 4);

            var dot = CreateLegendSwatchDot(row.transform, type, MapLayoutConstants.LegendDotSize);

            var label = CreateText("Desc", row.transform, desc, MapLayoutConstants.LegendDescFontSize, TextAnchor.MiddleLeft);
            label.color = new Color(0.88f, 0.9f, 0.93f);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            AddLayoutElement(
                label.gameObject,
                minHeight: 0f,
                flexibleWidth: 0f,
                flexibleHeight: 0f);
            var descFitter = label.gameObject.AddComponent<ContentSizeFitter>();
            descFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            descFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>Swatch giống node: viền StrokeColor + lõi FillColor.</summary>
        private static GameObject CreateLegendSwatchDot(Transform parent, MapNodeType type, float diameter)
        {
            var dot = CreateUiObject("Dot", parent);
            AddLayoutElement(
                dot,
                minWidth: diameter,
                minHeight: diameter,
                preferredWidth: diameter,
                preferredHeight: diameter,
                flexibleWidth: 0f,
                flexibleHeight: 0f);

            var dotRect = dot.GetComponent<RectTransform>();
            dotRect.sizeDelta = new Vector2(diameter, diameter);

            var fitter = dot.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fitter.aspectRatio = 1f;

            var strokeGo = CreateUiObject("Stroke", dot.transform);
            StretchRect(strokeGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var strokeImg = strokeGo.AddComponent<Image>();
            strokeImg.sprite = UiCircleSpriteUtil.Circle;
            strokeImg.color = MapNodePalette.StrokeColor(type);
            strokeImg.raycastTarget = false;

            var inset = diameter * (3f / MapLayoutConstants.NodeDiameter);
            var fillGo = CreateUiObject("Fill", dot.transform);
            StretchRect(fillGo, Vector2.zero, Vector2.one, new Vector2(inset, inset), new Vector2(-inset, -inset));
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.sprite = UiCircleSpriteUtil.Circle;
            fillImg.color = MapNodePalette.FillColor(type);
            fillImg.raycastTarget = false;

            return dot;
        }

        private static void AddLayoutElement(
            GameObject go,
            float minWidth = -1f,
            float minHeight = -1f,
            float preferredWidth = -1f,
            float preferredHeight = -1f,
            float flexibleWidth = -1f,
            float flexibleHeight = -1f)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            if (minWidth >= 0f)
            {
                le.minWidth = minWidth;
            }

            if (minHeight >= 0f)
            {
                le.minHeight = minHeight;
            }

            if (preferredWidth >= 0f)
            {
                le.preferredWidth = preferredWidth;
            }

            if (preferredHeight >= 0f)
            {
                le.preferredHeight = preferredHeight;
            }

            if (flexibleWidth >= 0f)
            {
                le.flexibleWidth = flexibleWidth;
            }

            if (flexibleHeight >= 0f)
            {
                le.flexibleHeight = flexibleHeight;
            }
        }

        private static void WireBootstrap(RunMapBootstrap bootstrap, RunMapController controller)
        {
            var template = EnsureDefaultMapTemplateAsset();
            SetSerializedField(bootstrap, "template", template);
            SetSerializedField(bootstrap, "randomizeSeedOnPlay", true);
        }

        private static void WireController(RunMapController controller, RunMapUIView mapView, Text status, Text seed)
        {
            controller.WireView(mapView, status, seed);
            EditorUtility.SetDirty(controller);
        }

        private static MapTemplateSO EnsureDefaultMapTemplateAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MapTemplateSO>(TemplateAssetPath);
            if (existing != null)
            {
                return existing;
            }

            if (!AssetDatabase.IsValidFolder("Assets/FracturedChorus/Data/ScriptableObjects/Presets"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/FracturedChorus/Data/ScriptableObjects"))
                {
                    AssetDatabase.CreateFolder("Assets/FracturedChorus/Data", "ScriptableObjects");
                }

                AssetDatabase.CreateFolder("Assets/FracturedChorus/Data/ScriptableObjects", "Presets");
            }

            var asset = ScriptableObject.CreateInstance<MapTemplateSO>();
            asset.useReferenceDemoOnPlay = false;
            asset.randomizeSeedOnPlay = true;
            asset.defaultSeed = 42;
            AssetDatabase.CreateAsset(asset, TemplateAssetPath);
            AssetDatabase.SaveAssets();
            return asset;
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
            text.color = new Color(0.88f, 0.9f, 0.92f);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void ConfigureBottomLayer(GameObject layer, Vector2 size)
        {
            var rect = layer.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private static void StretchRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void ReparentIfExists(string objectName, Transform parent)
        {
            var go = GameObject.Find(objectName);
            if (go == null || go.transform.parent == parent)
            {
                return;
            }

            go.transform.SetParent(parent, true);
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
}
#endif

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

        [MenuItem("Fractured Chorus/Setup Run Map Scene Hierarchy")]
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

            WireBootstrap(bootstrap, controller, mapView);
            WireController(controller, mapView, topBar.status, topBar.seed);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;

            Debug.Log("[Fractured Chorus] Run map hierarchy created (StS-style 7×15 + F16). Save scene → Play.");
        }

        [MenuItem("Fractured Chorus/Create Run Map Prototype Scene")]
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

            var status = CreateText("StatusLabel", bar.transform, "Chọn node F1 để bắt đầu.", 14, TextAnchor.MiddleRight);
            StretchRect(status.gameObject, new Vector2(0.55f, 0f), new Vector2(1f, 0.5f), new Vector2(8f, 4f), new Vector2(-16f, 0f));

            return (status, seed);
        }

        private static ScrollRect CreateMapScrollView(Transform canvas, out RunMapUIView mapView, out RectTransform contentRect)
        {
            var scrollGo = CreateUiObject("MapScrollView", canvas);
            StretchRect(scrollGo, new Vector2(0.02f, 0.05f), new Vector2(0.78f, 0.95f), Vector2.zero, Vector2.zero);
            scrollGo.AddComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 0.92f);

            var viewport = CreateUiObject("Viewport", scrollGo.transform);
            StretchRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>().color = Color.white;

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
            SetSerializedField(mapView, "scrollRect", scroll);

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

            var label = CreateText("Label", go.transform, "?", 14, TextAnchor.MiddleCenter);
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
            image.raycastTarget = false;
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.85f);
            var line = Undo.AddComponent<MapConnectionLineView>(go);
            line.WireImage(image);
            return line;
        }

        private static GameObject CreateLegendPanel(Transform canvas)
        {
            var panel = CreateUiObject("LegendPanel", canvas);
            StretchRect(panel, new Vector2(0.8f, 0.05f), new Vector2(0.98f, 0.95f), Vector2.zero, Vector2.zero);
            panel.AddComponent<Image>().color = new Color(0.1f, 0.11f, 0.13f, 0.94f);

            var title = CreateText("LegendTitle", panel.transform, "Node types (FC)", 16, TextAnchor.UpperLeft);
            StretchRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -36f), new Vector2(-12f, -8f));

            var entries = new[]
            {
                (MapNodeType.Battle, "Battle — combat thường"),
                (MapNodeType.Event, "Event — sự kiện ?"),
                (MapNodeType.Elite, "Elite — combat khó"),
                (MapNodeType.Camp, "Camp — nghỉ / hồi"),
                (MapNodeType.Relay, "Relay — shop"),
                (MapNodeType.Treasure, "Treasure — rương"),
                (MapNodeType.Boss, "Boss — Oni F16")
            };

            for (var i = 0; i < entries.Length; i++)
            {
                var y = -56f - i * 36f;
                CreateLegendRow(panel.transform, entries[i].Item1, entries[i].Item2, y);
            }

            var hint = CreateText("Hint", panel.transform,
                "Scroll map · click node F1 → đi theo path · đường cam = path đã chọn\nRef: StS 7×15 + boss · docs/diagrams",
                11, TextAnchor.UpperLeft);
            StretchRect(hint.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(12f, 12f), new Vector2(-12f, 120f));

            return panel;
        }

        private static void CreateLegendRow(Transform parent, MapNodeType type, string desc, float y)
        {
            var row = CreateUiObject($"Legend_{type}", parent);
            var rect = row.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(-24f, 28f);

            var dot = CreateUiObject("Dot", row.transform);
            var dotRect = dot.GetComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0f, 0.5f);
            dotRect.anchorMax = new Vector2(0f, 0.5f);
            dotRect.anchoredPosition = new Vector2(18f, 0f);
            dotRect.sizeDelta = new Vector2(16f, 16f);
            var dotImg = dot.AddComponent<Image>();
            dotImg.sprite = UiCircleSpriteUtil.Circle;
            dotImg.color = MapNodePalette.FillColor(type);

            var text = CreateText("Desc", row.transform, desc, 12, TextAnchor.MiddleLeft);
            StretchRect(text.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(40f, 0f), Vector2.zero);
        }

        private static void WireBootstrap(RunMapBootstrap bootstrap, RunMapController controller, RunMapUIView mapView)
        {
            var template = EnsureDefaultMapTemplateAsset();
            SetSerializedField(bootstrap, "template", template);
            SetSerializedField(bootstrap, "controller", controller);
            SetSerializedField(bootstrap, "mapView", mapView);
            SetSerializedField(bootstrap, "respectSceneAuthoring", true);
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

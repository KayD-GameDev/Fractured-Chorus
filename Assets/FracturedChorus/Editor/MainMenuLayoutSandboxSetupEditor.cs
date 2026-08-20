#if UNITY_EDITOR
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Menu;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class MainMenuLayoutSandboxSetupEditor
    {
        public const string ScenePath = "Assets/FracturedChorus/Scenes/MainMenuLayoutSandbox.unity";
        private const string EnvSpritePath = "Assets/FracturedChorus/Art/UI/TitleScreen/TitleScreen_MainMenu_Env_v1.png";
        private const string EnvFallbackPath = "Assets/FracturedChorus/Art/UI/TitleScreen/SheetV1/title_env_bg_v1.png";
        private const string AttractSpritePath = "Assets/FracturedChorus/Art/UI/TitleScreen/TitleScreen_Attract_NoUI_v1.png";
        private const string LogoSpritePath = "Assets/FracturedChorus/Art/UI/TitleScreen/logo_fracture_chorus_ui_v1.png";
        private const string PoseDir = "Assets/FracturedChorus/Art/UI/TitleScreen/SheetV1/";
        private const string BtnNormalPath = PoseDir + "ui_btn_shard_normal_v1_alpha.png";
        private const string BtnSelectedPath = PoseDir + "ui_btn_shard_selected_v1_alpha.png";
        private const string HudRingPath = PoseDir + "ui_hud_ring_v2_full.png";
        private const string PressAnyKeyPath = PoseDir + "ui_press_any_key_v1.png";
        private const float CharacterFitHeight = 1080f;

        [MenuItem("Fractured Chorus/Create MainMenu Layout Sandbox Scene")]
        public static void CreateScene()
        {
            EnsureFolder("Assets/FracturedChorus/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            ConfigureCamera();
            BuildHierarchy();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Fractured Chorus] Saved {ScenePath} — layout sandbox, not in Build Settings.");
        }

        [MenuItem("Fractured Chorus/Apply MainMenu Layout Sandbox Art")]
        public static void ApplyArtToOpenScene()
        {
            var root = GameObject.Find("MainMenuLayoutSandboxRoot");
            if (root == null)
            {
                EditorUtility.DisplayDialog(
                    "Apply Layout Sandbox Art",
                    "Open MainMenuLayoutSandbox và đảm bảo MainMenuLayoutSandboxRoot tồn tại.",
                    "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(root, "Apply MainMenu Layout Sandbox Art");
            EnsureTwoLayers(root);
            ApplyAttract(root.transform);
            EnsureHudRings(root.transform);
            ApplyLogo(root.transform);
            ApplyCharacters(root.transform);
            ApplyMenuButtons(root.transform);
            UiFontCatalog.ApplyHierarchy(root.transform, true);
            var layers = root.GetComponent<MainMenuLayoutSandboxLayers>();
            if (layers != null)
            {
                layers.ShowMainMenu();
            }
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("[Fractured Chorus] Sandbox art applied — Attract / Main Menu layers at root. Save scene.");
        }

        [MenuItem("Fractured Chorus/Ensure MainMenu Layout Sandbox Layers")]
        public static void EnsureLayersMenu()
        {
            var root = GameObject.Find("MainMenuLayoutSandboxRoot");
            if (root == null)
            {
                EditorUtility.DisplayDialog(
                    "Ensure Layout Sandbox Layers",
                    "Open MainMenuLayoutSandbox và đảm bảo MainMenuLayoutSandboxRoot tồn tại.",
                    "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(root, "Ensure MainMenu Layout Sandbox Layers");
            EnsureTwoLayers(root);
            ApplyAttract(root.transform);
            EnsureHudRings(root.transform);
            ApplyLogo(root.transform);
            ApplyMenuButtons(root.transform);
            var layers = root.GetComponent<MainMenuLayoutSandboxLayers>();
            if (layers != null)
            {
                layers.ShowMainMenu();
            }
            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            Debug.Log("[Fractured Chorus] AttractLayer + MainMenuLayer at sandbox root. Toggle in Hierarchy or Inspector.");
        }

        [MenuItem("Fractured Chorus/Rebuild MainMenu Layout Sandbox Hierarchy")]
        public static void RebuildActiveScene()
        {
            var existing = GameObject.Find("MainMenuLayoutSandboxRoot");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog(
                        "Rebuild MainMenu Layout Sandbox",
                        "Xóa hierarchy sandbox hiện tại và tạo lại? RectTransform đã chỉnh sẽ mất.",
                        "Rebuild",
                        "Cancel"))
                {
                    return;
                }

                Undo.DestroyObjectImmediate(existing);
            }

            ConfigureCamera();
            BuildHierarchy();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] MainMenu Layout Sandbox hierarchy rebuilt — Save scene (Ctrl+S).");
        }

        public static void BatchCreateScene()
        {
            CreateScene();
            EditorApplication.Exit(0);
        }

        private static void BuildHierarchy()
        {
            EnsureEventSystem();

            var root = new GameObject("MainMenuLayoutSandboxRoot");
            CreateAttractLayer(root.transform);
            var menuLayer = CreateMenuCanvas(root.transform);
            CreateEnvBackground(menuLayer);
            CreateCastLayer(menuLayer);
            CreateLogo(menuLayer);
            CreateMenuPanel(menuLayer);
            BindLayers(root);
            EnsureHudRings(root.transform);
            ApplyLogo(root.transform);
            UiFontCatalog.ApplyHierarchy(root.transform, true);
        }

        private static void EnsureTwoLayers(GameObject root)
        {
            var menu = root.transform.Find("MainMenuLayer");
            if (menu == null)
            {
                menu = root.transform.Find("LayoutCanvas");
            }

            if (menu == null)
            {
                var canvas = root.GetComponentInChildren<Canvas>(true);
                if (canvas != null && canvas.transform != root.transform)
                {
                    menu = canvas.transform;
                }
            }

            if (menu != null)
            {
                menu.name = "MainMenuLayer";
                var canvas = menu.GetComponent<Canvas>();
                if (canvas != null)
                {
                    canvas.sortingOrder = 1;
                }
            }
            else
            {
                menu = CreateMenuCanvas(root.transform);
            }

            var attract = root.transform.Find("AttractLayer");
            if (attract == null)
            {
                attract = CreateAttractLayer(root.transform).transform;
            }

            attract.SetAsFirstSibling();
            BindLayers(root);
            EnsureHudRings(root.transform);
            ApplyLogo(root.transform);
        }

        private static void BindLayers(GameObject root)
        {
            var layers = EnsureComponent<MainMenuLayoutSandboxLayers>(root);
            var attract = root.transform.Find("AttractLayer");
            var menu = root.transform.Find("MainMenuLayer");
            layers.Bind(attract != null ? attract.gameObject : null, menu != null ? menu.gameObject : null);
            EditorUtility.SetDirty(layers);
        }

        private static Transform CreateAttractLayer(Transform root)
        {
            var go = new GameObject("AttractLayer", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            go.transform.SetParent(root, false);
            ConfigureOverlayCanvas(go.GetComponent<Canvas>(), go.GetComponent<CanvasScaler>(), 0);
            ApplyAttractImage(go);
            return go.transform;
        }

        private static Transform CreateMenuCanvas(Transform root)
        {
            var canvasGo = new GameObject("MainMenuLayer", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(root, false);
            ConfigureOverlayCanvas(canvasGo.GetComponent<Canvas>(), canvasGo.GetComponent<CanvasScaler>(), 1);
            return canvasGo.transform;
        }

        private static void ConfigureOverlayCanvas(Canvas canvas, CanvasScaler scaler, int sortingOrder)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        }

        private static void ApplyAttract(Transform root)
        {
            var attract = FindDeep(root, "AttractLayer");
            if (attract == null)
            {
                return;
            }

            ApplyAttractImage(attract.gameObject);
        }

        private static void ApplyAttractImage(GameObject attractGo)
        {
            var bgTf = attractGo.transform.Find("AttractBackground");
            var bgGo = bgTf != null ? bgTf.gameObject : CreateUiObject("AttractBackground", attractGo.transform);
            Stretch(bgGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var image = EnsureComponent<Image>(bgGo);
            image.sprite = LoadLargestSprite(AttractSpritePath);
            image.preserveAspect = false;
            image.raycastTarget = false;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.useSpriteMesh = false;
        }

        private static void EnsureHudRings(Transform root)
        {
            var attract = FindDeep(root, "AttractLayer");
            if (attract != null)
            {
                EnsureHudRing(attract, new Vector2(0.24f, 0.58f), new Vector2(780f, 790f), 0.62f);
            }

            var menu = FindDeep(root, "MainMenuLayer");
            if (menu == null)
            {
                return;
            }

            var ring = EnsureHudRing(menu, new Vector2(0.22f, 0.82f), new Vector2(620f, 628f), 0.78f);
            var env = menu.Find("EnvBackground");
            if (ring != null && env != null)
            {
                ring.SetSiblingIndex(env.GetSiblingIndex() + 1);
            }
        }

        private static RectTransform EnsureHudRing(
            Transform parent,
            Vector2 anchor,
            Vector2 size,
            float alpha)
        {
            var existing = parent.Find("HudRing");
            var created = existing == null;
            GameObject go;
            if (created)
            {
                go = CreateUiObject("HudRing", parent);
            }
            else
            {
                go = existing.gameObject;
            }

            var image = EnsureComponent<Image>(go);
            image.sprite = LoadLargestSprite(HudRingPath)
                           ?? LoadLargestSprite(PoseDir + "ui_hud_ring_v2_alpha.png");
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.type = Image.Type.Simple;
            image.useSpriteMesh = false;
            image.color = new Color(1f, 1f, 1f, alpha);

            if (go.GetComponent<TitleHudRingMotion>() == null)
            {
                go.AddComponent<TitleHudRingMotion>();
            }

            var rect = go.GetComponent<RectTransform>();
            if (created)
            {
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = size;
                rect.anchoredPosition = Vector2.zero;
            }

            return rect;
        }

        private static void ConfigureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            cam.backgroundColor = new Color(0.94f, 0.96f, 1f);
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

        private static void CreateEnvBackground(Transform canvas)
        {
            var go = CreateUiObject("EnvBackground", canvas);
            Stretch(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var image = go.AddComponent<Image>();
            image.sprite = LoadLargestSprite(EnvSpritePath) ?? LoadLargestSprite(EnvFallbackPath);
            image.preserveAspect = false;
            image.raycastTarget = false;
            image.color = Color.white;
            image.useSpriteMesh = false;
        }

        private static void CreateCastLayer(Transform canvas)
        {
            var layer = CreateUiObject("CastLayer", canvas);
            Stretch(layer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            CreateCharacter(layer.transform, "Char_Astra", PoseDir + "char_astra_title_pose_v1_alpha.png", 1000f, 12f);
            CreateCharacter(layer.transform, "Char_Charlotte", PoseDir + "char_charlotte_title_pose_v1_alpha.png", 1640f, 16f);
            CreateCharacter(layer.transform, "Char_Coda", PoseDir + "char_coda_title_pose_v1_alpha.png", 1420f, 20f);
            CreateCharacter(layer.transform, "Char_Ren", PoseDir + "char_ren_title_pose_v1_alpha.png", 1220f, 0f);
        }

        private static void CreateCharacter(Transform parent, string name, string spritePath, float x, float y)
        {
            var go = CreateUiObject(name, parent);
            var image = go.AddComponent<Image>();
            FitUncroppedCharacter(image, LoadLargestSprite(spritePath), go.GetComponent<RectTransform>(), x, y);
        }

        private static void FitUncroppedCharacter(Image image, Sprite sprite, RectTransform rect, float x, float y)
        {
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.useSpriteMesh = false;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0f);
            var width = CharacterFitHeight * 0.42f;
            if (sprite != null && sprite.rect.height > 1f)
            {
                width = CharacterFitHeight * (sprite.rect.width / sprite.rect.height);
            }

            rect.sizeDelta = new Vector2(width, CharacterFitHeight);
            rect.anchoredPosition = new Vector2(x, y);
        }

        private static void CreateLogo(Transform canvas)
        {
            var go = CreateUiObject("Logo", canvas);
            ApplyMenuLogoLayout(go.GetComponent<RectTransform>(), go.AddComponent<Image>(), true);
        }

        private static void BindLogoImage(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.type = Image.Type.Simple;
            image.useSpriteMesh = false;
            image.color = Color.white;
        }

        private static Vector2 LogoSize(Sprite sprite, float width)
        {
            var size = new Vector2(width, width * 380f / 1501f);
            if (sprite != null && sprite.rect.width > 1f)
            {
                size.y = size.x * (sprite.rect.height / sprite.rect.width);
            }

            return size;
        }

        private static void ApplyMenuLogoLayout(RectTransform rect, Image image, bool created)
        {
            var sprite = LoadLargestSprite(LogoSpritePath);
            BindLogoImage(image, sprite);
            if (!created)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = LogoSize(sprite, 720f);
            rect.anchoredPosition = new Vector2(72f, -48f);
        }

        private static void EnsureAttractLogo(Transform attract)
        {
            var existing = attract.Find("Logo");
            var created = existing == null;
            var go = created ? CreateUiObject("Logo", attract) : existing.gameObject;
            var image = EnsureComponent<Image>(go);
            var sprite = LoadLargestSprite(LogoSpritePath);
            BindLogoImage(image, sprite);

            var rect = go.GetComponent<RectTransform>();
            if (created)
            {
                rect.anchorMin = new Vector2(0.24f, 0.58f);
                rect.anchorMax = new Vector2(0.24f, 0.58f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = LogoSize(sprite, 560f);
                rect.anchoredPosition = Vector2.zero;
            }

            var ring = attract.Find("HudRing");
            if (ring != null)
            {
                go.transform.SetSiblingIndex(ring.GetSiblingIndex() + 1);
            }
        }

        private static void EnsureAttractPressAnyKey(Transform attract)
        {
            var existing = attract.Find("PressAnyKey");
            var created = existing == null;
            var go = created ? CreateUiObject("PressAnyKey", attract) : existing.gameObject;
            var image = EnsureComponent<Image>(go);
            var sprite = LoadLargestSprite(PressAnyKeyPath)
                         ?? LoadLargestSprite(PoseDir + "Press any key Button.png");
            BindLogoImage(image, sprite);

            var group = EnsureComponent<CanvasGroup>(go);
            group.blocksRaycasts = false;
            group.interactable = false;

            var rect = go.GetComponent<RectTransform>();
            if (created)
            {
                rect.anchorMin = new Vector2(0.24f, 0.17f);
                rect.anchorMax = new Vector2(0.24f, 0.17f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                var size = new Vector2(1200f, 1200f * 86f / 2031f);
                if (sprite != null && sprite.rect.width > 1f)
                {
                    size.y = size.x * (sprite.rect.height / sprite.rect.width);
                }

                rect.sizeDelta = size;
                rect.anchoredPosition = Vector2.zero;
            }

            var logo = attract.Find("Logo");
            if (logo != null)
            {
                go.transform.SetSiblingIndex(logo.GetSiblingIndex() + 1);
            }

            var leftover = attract.GetComponent<TitleAttractPrompt>();
            if (leftover != null)
            {
                UnityEngine.Object.DestroyImmediate(leftover);
            }

            var prompt = EnsureComponent<TitleAttractPrompt>(go);
            prompt.Bind(attract.GetComponentInParent<MainMenuLayoutSandboxLayers>(), group);
            EditorUtility.SetDirty(prompt);
            EnsureCrystalField(attract, "AttractBackground", 18);
        }

        private static void EnsureCrystalField(Transform parent, string behindName, int shardCount)
        {
            var existing = parent.Find("CrystalField");
            var go = existing == null ? CreateUiObject("CrystalField", parent) : existing.gameObject;
            Stretch(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var field = EnsureComponent<TitleAttractCrystalField>(go);
            var sprites = new[]
            {
                LoadLargestSprite(PoseDir + "ui_crystal_shard_a_v1.png"),
                LoadLargestSprite(PoseDir + "ui_crystal_shard_b_v1.png"),
                LoadLargestSprite(PoseDir + "ui_crystal_shard_c_v1.png")
            };
            field.Bind(sprites, shardCount);
            EditorUtility.SetDirty(field);

            var behind = parent.Find(behindName);
            if (behind != null)
            {
                go.transform.SetSiblingIndex(behind.GetSiblingIndex() + 1);
            }
        }

        private static void CreateMenuPanel(Transform canvas)
        {
            var panel = CreateUiObject("MenuPanel", canvas);
            Stretch(panel, new Vector2(0.04f, 0.06f), new Vector2(0.38f, 0.58f), Vector2.zero, Vector2.zero);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(0, 0, 0, 8);

            CreateMenuRow(panel.transform, "NEW GAME", true);
            CreateMenuRow(panel.transform, "LOAD GAME", false);
            CreateMenuRow(panel.transform, "OFF-BEAT ARCHIVE", true);
            CreateMenuRow(panel.transform, "CONFIG", true);
            CreateMenuRow(panel.transform, "QUIT", true);
            BindSandboxMenu(panel.transform);
        }

        private static void CreateMenuRow(Transform parent, string label, bool enabled)
        {
            var row = CreateUiObject($"Row_{label.Replace(' ', '_')}", parent);
            ConfigureMenuRow(row, label, enabled);
        }

        private static void ConfigureMenuRow(GameObject row, string label, bool enabled)
        {
            var layout = EnsureComponent<LayoutElement>(row);
            layout.preferredHeight = 88f;
            layout.flexibleWidth = 1f;

            var image = EnsureComponent<Image>(row);
            image.sprite = LoadLargestSprite(BtnNormalPath)
                           ?? LoadLargestSprite(PoseDir + "ui_btn_shard_normal_v1.png");
            image.preserveAspect = false;
            image.raycastTarget = true;
            image.type = Image.Type.Simple;
            image.useSpriteMesh = false;
            image.color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.45f);

            BindChoiceIcon(row.transform, label, enabled);

            var note = row.transform.Find("Note");
            if (note != null)
            {
                note.gameObject.SetActive(false);
            }

            var labelTf = row.transform.Find("Label");
            GameObject textGo;
            if (labelTf != null)
            {
                textGo = labelTf.gameObject;
            }
            else
            {
                textGo = CreateUiObject("Label", row.transform);
            }

            Stretch(textGo, Vector2.zero, Vector2.one, new Vector2(78f, 0f), new Vector2(-36f, 0f));
            var text = EnsureComponent<Text>(textGo);
            text.text = label;
            text.fontSize = 26;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = enabled
                ? new Color(0.07f, 0.1f, 0.22f, 0.95f)
                : new Color(0.12f, 0.16f, 0.28f, 0.4f);
            text.raycastTarget = false;
            SceneFontSetupEditor.ApplyAutomatic(text);

            if (row.GetComponent<MainMenuLayoutSandboxRow>() == null)
            {
                row.AddComponent<MainMenuLayoutSandboxRow>();
            }
        }

        private static void BindChoiceIcon(Transform row, string label, bool enabled)
        {
            var path = IconPathForLabel(label);
            var iconTf = row.Find("Icon");
            if (string.IsNullOrEmpty(path))
            {
                if (iconTf != null)
                {
                    iconTf.gameObject.SetActive(false);
                }

                return;
            }

            var iconGo = iconTf != null ? iconTf.gameObject : CreateUiObject("Icon", row);
            iconGo.SetActive(true);
            var iconImage = EnsureComponent<Image>(iconGo);
            BindLogoImage(iconImage, LoadLargestSprite(path));
            iconImage.color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.4f);

            var rect = iconGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(42f, 42f);
            rect.anchoredPosition = new Vector2(48f, 0f);
        }

        private static string IconPathForLabel(string label)
        {
            switch (label)
            {
                case "NEW GAME":
                    return PoseDir + "ui_icon_play_v1.png";
                case "LOAD GAME":
                    return PoseDir + "ui_icon_power_v1.png";
                case "OFF-BEAT ARCHIVE":
                    return PoseDir + "ui_icon_gallery_v1.png";
                case "CONFIG":
                    return PoseDir + "ui_icon_gear_v1.png";
                case "QUIT":
                    return PoseDir + "ui_icon_power_v1.png";
                default:
                    return null;
            }
        }

        private static void ApplyLogo(Transform root)
        {
            var attract = FindDeep(root, "AttractLayer");
            if (attract != null)
            {
                EnsureAttractLogo(attract);
                EnsureAttractPressAnyKey(attract);
            }

            var menu = FindDeep(root, "MainMenuLayer");
            if (menu == null)
            {
                menu = FindDeep(root, "LayoutCanvas");
            }

            if (menu == null)
            {
                return;
            }

            EnsureCrystalField(menu, "EnvBackground", 16);

            var logo = menu.Find("Logo");
            var created = logo == null;
            if (created)
            {
                CreateLogo(menu);
                return;
            }

            var image = EnsureComponent<Image>(logo.gameObject);
            BindLogoImage(image, LoadLargestSprite(LogoSpritePath));
        }

        private static void ApplyCharacters(Transform root)
        {
            ApplyOneCharacter(root, "Char_Astra", PoseDir + "char_astra_title_pose_v1_alpha.png");
            ApplyOneCharacter(root, "Char_Ren", PoseDir + "char_ren_title_pose_v1_alpha.png");
            ApplyOneCharacter(root, "Char_Coda", PoseDir + "char_coda_title_pose_v1_alpha.png");
            ApplyOneCharacter(root, "Char_Charlotte", PoseDir + "char_charlotte_title_pose_v1_alpha.png");
        }

        private static void ApplyOneCharacter(Transform root, string name, string spritePath)
        {
            var rect = FindDeep(root, name);
            if (rect == null)
            {
                return;
            }

            var image = EnsureComponent<Image>(rect.gameObject);
            var pos = rect.anchoredPosition;
            FitUncroppedCharacter(image, LoadLargestSprite(spritePath), rect, pos.x, pos.y);
        }

        private static void ApplyMenuButtons(Transform root)
        {
            ApplyRow(root, "Row_NEW_GAME", "NEW GAME", true);
            ApplyRow(root, "Row_LOAD_GAME", "LOAD GAME", false);
            ApplyRow(root, "Row_OFF-BEAT_ARCHIVE", "OFF-BEAT ARCHIVE", true);
            ApplyRow(root, "Row_CONFIG", "CONFIG", true);
            ApplyRow(root, "Row_QUIT", "QUIT", true);

            var panel = FindDeep(root, "MenuPanel");
            if (panel == null)
            {
                return;
            }

            var layout = panel.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 8f;
            }

            BindSandboxMenu(panel);
        }

        private static void BindSandboxMenu(Transform panel)
        {
            var menu = EnsureComponent<MainMenuLayoutSandboxMenu>(panel.gameObject);
            var specs = new[]
            {
                ("Row_NEW_GAME", true),
                ("Row_LOAD_GAME", false),
                ("Row_OFF-BEAT_ARCHIVE", true),
                ("Row_CONFIG", true),
                ("Row_QUIT", true)
            };
            var bound = new MainMenuLayoutSandboxRow[specs.Length];
            var normal = LoadLargestSprite(BtnNormalPath)
                         ?? LoadLargestSprite(PoseDir + "ui_btn_shard_normal_v1.png");
            var highlight = LoadLargestSprite(BtnSelectedPath)
                            ?? LoadLargestSprite(PoseDir + "ui_btn_shard_selected_v1.png");
            for (var i = 0; i < specs.Length; i++)
            {
                var row = panel.Find(specs[i].Item1);
                if (row == null)
                {
                    continue;
                }

                var view = EnsureComponent<MainMenuLayoutSandboxRow>(row.gameObject);
                var shard = row.GetComponent<Image>();
                var icon = row.Find("Icon") != null ? row.Find("Icon").GetComponent<Image>() : null;
                var label = row.Find("Label") != null ? row.Find("Label").GetComponent<Text>() : null;
                view.Configure(menu, i, shard, icon, label, normal, highlight, specs[i].Item2);
                bound[i] = view;
            }

            menu.Bind(bound);
        }

        private static void ApplyRow(Transform root, string name, string label, bool enabled)
        {
            var row = FindDeep(root, name);
            if (row == null)
            {
                return;
            }

            ConfigureMenuRow(row.gameObject, label, enabled);
        }

        private static RectTransform FindDeep(Transform root, string name)
        {
            if (root.name == name)
            {
                return root as RectTransform ?? root.GetComponent<RectTransform>();
            }

            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == name)
                {
                    return transforms[i] as RectTransform ?? transforms[i].GetComponent<RectTransform>();
                }
            }

            return null;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component == null)
            {
                component = go.AddComponent<T>();
            }

            return component;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Sprite LoadLargestSprite(string assetPath)
        {
            Sprite best = null;
            var bestArea = -1f;
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (var i = 0; i < assets.Length; i++)
            {
                if (assets[i] is not Sprite sprite)
                {
                    continue;
                }

                var area = sprite.rect.width * sprite.rect.height;
                if (area <= bestArea)
                {
                    continue;
                }

                best = sprite;
                bestArea = area;
            }

            if (best != null)
            {
                return best;
            }

            Debug.LogWarning($"[Fractured Chorus] Sprite not found: {assetPath}");
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = "Assets/FracturedChorus";
            if (!AssetDatabase.IsValidFolder(parent))
            {
                AssetDatabase.CreateFolder("Assets", "FracturedChorus");
            }

            AssetDatabase.CreateFolder(parent, "Scenes");
        }
    }
}
#endif

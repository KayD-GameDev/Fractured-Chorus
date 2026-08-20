#if UNITY_EDITOR
using FracturedChorus.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class TitleScreenChromeApply
    {
        private const string AttractSpritePath = "Assets/FracturedChorus/Art/UI/TitleScreen/TitleScreen_Attract_NoUI_v1.png";
        private const string EnvSpritePath = "Assets/FracturedChorus/Art/UI/TitleScreen/TitleScreen_MainMenu_Env_v1.png";
        private const string LogoSpritePath = "Assets/FracturedChorus/Art/UI/TitleScreen/logo_fracture_chorus_ui_v1.png";
        private const string PoseDir = "Assets/FracturedChorus/Art/UI/TitleScreen/SheetV1/";
        private const string HudRingPath = PoseDir + "ui_hud_ring_v2_full.png";
        private const string PressAnyKeyPath = PoseDir + "ui_press_any_key_v1.png";
        private const string BtnNormalPath = PoseDir + "ui_btn_shard_normal_v1_alpha.png";
        private const string BtnSelectedPath = PoseDir + "ui_btn_shard_selected_v1_alpha.png";
        private const string SandboxScenePath = "Assets/FracturedChorus/Scenes/MainMenuLayoutSandbox.unity";
        private const float CharacterFitHeight = 1080f;

        [MenuItem("Fractured Chorus/Apply Title Screen Art To MainMenuStartGame")]
        public static void ApplyToOpenScene()
        {
            var root = GameObject.Find("MainMenuStartGameRoot");
            if (root == null)
            {
                EditorUtility.DisplayDialog(
                    "Apply Title Screen Art",
                    "Mở scene MainMenuStartGame và đảm bảo MainMenuStartGameRoot tồn tại.",
                    "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(root, "Apply Title Screen Art To MainMenuStartGame");
            Apply(root);
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("[Fractured Chorus] Title screen art applied to MainMenuStartGame. Save scene.");
        }

        public static bool HasChrome(GameObject root)
        {
            if (root == null)
            {
                return false;
            }

            var attract = FindDeep(root.transform, "AttractLayer");
            var menuBg = FindDeep(root.transform, "MainMenuBackground");
            return attract != null && attract.Find("PressAnyKey") != null &&
                   menuBg != null && menuBg.Find("CastLayer") != null;
        }

        public static void Apply(GameObject root)
        {
            var attract = FindDeep(root.transform, "AttractLayer");
            var menuBg = FindDeep(root.transform, "MainMenuBackground");
            var menuPanel = FindDeep(root.transform, "MenuPanel");
            if (attract == null || menuBg == null || menuPanel == null)
            {
                Debug.LogWarning("[Fractured Chorus] AttractLayer / MainMenuBackground / MenuPanel missing.");
                return;
            }

            BindLayerSprite(attract.gameObject, AttractSpritePath);
            BindLayerSprite(menuBg.gameObject, EnvSpritePath);
            EnsureCrystalField(attract, 18);
            EnsureHudRing(attract, new Vector2(0.24f, 0.58f), new Vector2(780f, 790f), 0.62f);
            EnsureLogo(attract, new Vector2(0.24f, 0.58f), 560f, centerPivot: true);
            EnsurePressAnyKey(attract);
            EnsureCrystalField(menuBg, 16);
            EnsureHudRing(menuBg, new Vector2(0.22f, 0.82f), new Vector2(620f, 628f), 0.78f);
            EnsureCast(menuBg);
            EnsureLogo(menuBg, new Vector2(0f, 1f), 720f, centerPivot: false);
            ApplyMenuPanel(menuPanel);
            CopyLayoutFromSandbox(attract, menuBg, menuPanel);
            var controller = root.GetComponent<MainMenuStartGameController>();
            if (controller != null)
            {
                controller.SetEditorPreview(MainMenuStartGameController.MainMenuEditorPreview.Attract);
                EditorUtility.SetDirty(controller);
            }
        }

        private static void CopyLayoutFromSandbox(Transform attract, Transform menuBg, RectTransform menuPanel)
        {
            var sandbox = FindLoadedScene(SandboxScenePath);
            var opened = !sandbox.IsValid();
            if (opened)
            {
                sandbox = EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Additive);
            }

            try
            {
                var sandboxRoot = FindRoot(sandbox, "MainMenuLayoutSandboxRoot");
                if (sandboxRoot == null)
                {
                    Debug.LogWarning("[Fractured Chorus] Sandbox root missing — layout copy skipped.");
                    return;
                }

                var srcAttract = FindDeep(sandboxRoot.transform, "AttractLayer");
                var srcMenu = FindDeep(sandboxRoot.transform, "MainMenuLayer");
                if (srcAttract == null || srcMenu == null)
                {
                    Debug.LogWarning("[Fractured Chorus] Sandbox Attract/MainMenu layer missing — layout copy skipped.");
                    return;
                }

                CopyChildRect(srcAttract, attract, "Logo");
                CopyChildRect(srcAttract, attract, "HudRing");
                CopyChildRect(srcAttract, attract, "PressAnyKey");
                CopyChildRect(srcMenu, menuBg, "Logo");
                CopyChildRect(srcMenu, menuBg, "HudRing");
                CopyChildRect(srcMenu, menuBg, "CastLayer");
                var srcCast = srcMenu.Find("CastLayer");
                var dstCast = menuBg.Find("CastLayer");
                if (srcCast != null && dstCast != null)
                {
                    CopyChildRect(srcCast, dstCast, "Char_Astra");
                    CopyChildRect(srcCast, dstCast, "Char_Charlotte");
                    CopyChildRect(srcCast, dstCast, "Char_Coda");
                    CopyChildRect(srcCast, dstCast, "Char_Ren");
                }

                var srcPanel = srcMenu.Find("MenuPanel") as RectTransform;
                if (srcPanel != null)
                {
                    CopyRect(srcPanel, menuPanel);
                    var srcLayout = srcPanel.GetComponent<VerticalLayoutGroup>();
                    var dstLayout = menuPanel.GetComponent<VerticalLayoutGroup>();
                    if (srcLayout != null && dstLayout != null)
                    {
                        dstLayout.padding = new RectOffset(
                            srcLayout.padding.left,
                            srcLayout.padding.right,
                            srcLayout.padding.top,
                            srcLayout.padding.bottom);
                        dstLayout.spacing = srcLayout.spacing;
                        dstLayout.childAlignment = srcLayout.childAlignment;
                        dstLayout.childControlWidth = srcLayout.childControlWidth;
                        dstLayout.childControlHeight = srcLayout.childControlHeight;
                        dstLayout.childForceExpandWidth = srcLayout.childForceExpandWidth;
                        dstLayout.childForceExpandHeight = srcLayout.childForceExpandHeight;
                    }

                    CopyButtonRows(srcPanel, menuPanel);
                }
            }
            finally
            {
                if (opened && sandbox.IsValid())
                {
                    EditorSceneManager.CloseScene(sandbox, true);
                }
            }
        }

        private static void CopyButtonRows(Transform srcPanel, Transform dstPanel)
        {
            foreach (Transform dstRow in dstPanel)
            {
                if (!dstRow.name.StartsWith("Row_"))
                {
                    continue;
                }

                var srcRow = srcPanel.Find(dstRow.name);
                if (srcRow == null)
                {
                    continue;
                }

                var srcLayout = srcRow.GetComponent<LayoutElement>();
                var dstLayout = dstRow.GetComponent<LayoutElement>();
                if (srcLayout != null && dstLayout != null)
                {
                    dstLayout.preferredHeight = srcLayout.preferredHeight;
                    dstLayout.minHeight = srcLayout.minHeight;
                    dstLayout.flexibleWidth = srcLayout.flexibleWidth;
                    dstLayout.flexibleHeight = srcLayout.flexibleHeight;
                    dstLayout.ignoreLayout = srcLayout.ignoreLayout;
                }

                var srcShard = srcRow.GetComponent<Image>();
                var dstShard = dstRow.GetComponent<Image>();
                if (srcShard != null && dstShard != null)
                {
                    dstShard.color = srcShard.color;
                    dstShard.preserveAspect = srcShard.preserveAspect;
                    dstShard.type = srcShard.type;
                    dstShard.useSpriteMesh = srcShard.useSpriteMesh;
                    dstShard.pixelsPerUnitMultiplier = srcShard.pixelsPerUnitMultiplier;
                }

                CopyChildRect(srcRow, dstRow, "Icon");
                CopyChildRect(srcRow, dstRow, "Label");
                var srcText = srcRow.Find("Label")?.GetComponent<Text>();
                var dstText = dstRow.Find("Label")?.GetComponent<Text>();
                if (srcText != null && dstText != null)
                {
                    dstText.fontSize = srcText.fontSize;
                    dstText.fontStyle = srcText.fontStyle;
                    dstText.alignment = srcText.alignment;
                    dstText.color = srcText.color;
                    dstText.horizontalOverflow = srcText.horizontalOverflow;
                    dstText.verticalOverflow = srcText.verticalOverflow;
                    dstText.resizeTextForBestFit = srcText.resizeTextForBestFit;
                    EditorUtility.SetDirty(dstText);
                }

                EditorUtility.SetDirty(dstRow);
            }
        }

        private static Scene FindLoadedScene(string path)
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.path == path)
                {
                    return scene;
                }
            }

            return default;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            if (!scene.IsValid())
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == name)
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static void CopyChildRect(Transform srcParent, Transform dstParent, string childName)
        {
            var src = srcParent.Find(childName) as RectTransform;
            var dst = dstParent.Find(childName) as RectTransform;
            if (src == null || dst == null)
            {
                return;
            }

            CopyRect(src, dst);
            var srcImage = src.GetComponent<Image>();
            var dstImage = dst.GetComponent<Image>();
            if (srcImage != null && dstImage != null)
            {
                dstImage.color = srcImage.color;
            }
        }

        private static void CopyRect(RectTransform src, RectTransform dst)
        {
            dst.anchorMin = src.anchorMin;
            dst.anchorMax = src.anchorMax;
            dst.pivot = src.pivot;
            dst.sizeDelta = src.sizeDelta;
            dst.anchoredPosition = src.anchoredPosition;
            dst.localScale = src.localScale;
            dst.localRotation = src.localRotation;
            EditorUtility.SetDirty(dst);
        }

        private static void BindLayerSprite(GameObject layer, string path)
        {
            var image = EnsureComponent<Image>(layer);
            image.sprite = LoadLargestSprite(path);
            image.preserveAspect = false;
            image.raycastTarget = false;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.useSpriteMesh = false;
        }

        private static void EnsureCrystalField(Transform parent, int count)
        {
            var existing = parent.Find("CrystalField");
            var go = existing == null ? CreateUi("CrystalField", parent) : existing.gameObject;
            Stretch(go);
            var field = EnsureComponent<TitleAttractCrystalField>(go);
            field.Bind(
                new[]
                {
                    LoadLargestSprite(PoseDir + "ui_crystal_shard_a_v1.png"),
                    LoadLargestSprite(PoseDir + "ui_crystal_shard_b_v1.png"),
                    LoadLargestSprite(PoseDir + "ui_crystal_shard_c_v1.png")
                },
                count);
            EditorUtility.SetDirty(field);
            var bg = parent.Find("ConfigBackground");
            var sibling = bg != null ? bg.GetSiblingIndex() + 1 : 0;
            go.transform.SetSiblingIndex(sibling);
        }

        private static void EnsureHudRing(Transform parent, Vector2 anchor, Vector2 size, float alpha)
        {
            var existing = parent.Find("HudRing");
            var created = existing == null;
            var go = created ? CreateUi("HudRing", parent) : existing.gameObject;
            var image = EnsureComponent<Image>(go);
            image.sprite = LoadLargestSprite(HudRingPath) ?? LoadLargestSprite(PoseDir + "ui_hud_ring_v2_alpha.png");
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.type = Image.Type.Simple;
            image.useSpriteMesh = false;
            image.color = new Color(1f, 1f, 1f, alpha);
            if (go.GetComponent<TitleHudRingMotion>() == null)
            {
                go.AddComponent<TitleHudRingMotion>();
            }

            if (created)
            {
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = size;
                rect.anchoredPosition = Vector2.zero;
            }

            var crystals = parent.Find("CrystalField");
            if (crystals != null)
            {
                go.transform.SetSiblingIndex(crystals.GetSiblingIndex() + 1);
            }
        }

        private static void EnsureLogo(Transform parent, Vector2 anchor, float width, bool centerPivot)
        {
            var existing = parent.Find("Logo");
            var created = existing == null;
            var go = created ? CreateUi("Logo", parent) : existing.gameObject;
            var image = EnsureComponent<Image>(go);
            var sprite = LoadLargestSprite(LogoSpritePath);
            BindImage(image, sprite);
            if (created)
            {
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                if (centerPivot)
                {
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;
                }
                else
                {
                    rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = new Vector2(72f, -48f);
                }

                rect.sizeDelta = LogoSize(sprite, width);
            }

            var ring = parent.Find("HudRing");
            if (ring != null)
            {
                go.transform.SetSiblingIndex(ring.GetSiblingIndex() + 1);
            }
        }

        private static void EnsurePressAnyKey(Transform attract)
        {
            var existing = attract.Find("PressAnyKey");
            var created = existing == null;
            var go = created ? CreateUi("PressAnyKey", attract) : existing.gameObject;
            var image = EnsureComponent<Image>(go);
            var sprite = LoadLargestSprite(PressAnyKeyPath);
            BindImage(image, sprite);
            var group = EnsureComponent<CanvasGroup>(go);
            group.blocksRaycasts = false;
            group.interactable = false;
            if (created)
            {
                var rect = go.GetComponent<RectTransform>();
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

            var leftover = attract.GetComponent<TitleAttractPrompt>();
            if (leftover != null)
            {
                UnityEngine.Object.DestroyImmediate(leftover);
            }

            var prompt = EnsureComponent<TitleAttractPrompt>(go);
            prompt.Bind(null, group);
            EditorUtility.SetDirty(prompt);
            var logo = attract.Find("Logo");
            if (logo != null)
            {
                go.transform.SetSiblingIndex(logo.GetSiblingIndex() + 1);
            }
        }

        private static void EnsureCast(Transform menuBg)
        {
            var layer = menuBg.Find("CastLayer");
            var layerGo = layer == null ? CreateUi("CastLayer", menuBg) : layer.gameObject;
            Stretch(layerGo);
            EnsureCharacter(layerGo.transform, "Char_Astra", PoseDir + "char_astra_title_pose_v1_alpha.png", 1000f, 12f);
            EnsureCharacter(layerGo.transform, "Char_Charlotte", PoseDir + "char_charlotte_title_pose_v1_alpha.png", 1640f, 16f);
            EnsureCharacter(layerGo.transform, "Char_Coda", PoseDir + "char_coda_title_pose_v1_alpha.png", 1420f, 20f);
            EnsureCharacter(layerGo.transform, "Char_Ren", PoseDir + "char_ren_title_pose_v1_alpha.png", 1220f, 0f);
            var logo = menuBg.Find("Logo");
            if (logo != null)
            {
                layerGo.transform.SetSiblingIndex(Mathf.Max(0, logo.GetSiblingIndex()));
            }
        }

        private static void EnsureCharacter(Transform parent, string name, string path, float x, float y)
        {
            var existing = parent.Find(name);
            var created = existing == null;
            var go = created ? CreateUi(name, parent) : existing.gameObject;
            var image = EnsureComponent<Image>(go);
            var sprite = LoadLargestSprite(path);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.useSpriteMesh = false;
            if (!created)
            {
                return;
            }

            var rect = go.GetComponent<RectTransform>();
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

        private static void ApplyMenuPanel(RectTransform panel)
        {
            var highlight = panel.Find("HighlightBar");
            if (highlight != null)
            {
                highlight.gameObject.SetActive(false);
            }

            var nested = panel.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < nested.Length; i++)
            {
                if (nested[i].name == "HighlightBar")
                {
                    nested[i].gameObject.SetActive(false);
                }
            }

            var menuController = panel.GetComponent<MainMenuStartGameMenuController>();
            if (menuController != null)
            {
                var so = new SerializedObject(menuController);
                var bar = so.FindProperty("highlightBar");
                if (bar != null)
                {
                    bar.objectReferenceValue = null;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(menuController);
            }

            var status = panel.Find("StatusText")?.GetComponent<Text>();
            if (status != null)
            {
                status.alignment = TextAnchor.LowerLeft;
            }

            var normal = LoadLargestSprite(BtnNormalPath);
            var selected = LoadLargestSprite(BtnSelectedPath);
            foreach (Transform child in panel)
            {
                if (!child.name.StartsWith("Row_"))
                {
                    continue;
                }

                StyleRow(child, normal, selected);
            }
        }

        private static void StyleRow(Transform row, Sprite normal, Sprite selected)
        {
            var layout = EnsureComponent<LayoutElement>(row.gameObject);
            layout.preferredHeight = 88f;
            layout.flexibleWidth = 1f;

            var shard = EnsureComponent<Image>(row.gameObject);
            shard.sprite = normal;
            shard.preserveAspect = false;
            shard.raycastTarget = false;
            shard.type = Image.Type.Simple;
            shard.useSpriteMesh = false;
            shard.color = Color.white;

            var hit = row.Find("HitArea");
            var hitGo = hit != null ? hit.gameObject : CreateUi("HitArea", row);
            Stretch(hitGo);
            hitGo.transform.SetAsFirstSibling();
            var hitImage = EnsureComponent<Image>(hitGo);
            hitImage.sprite = null;
            hitImage.overrideSprite = null;
            hitImage.color = new Color(1f, 1f, 1f, 0.001f);
            hitImage.raycastTarget = true;

            var labelTf = row.Find("Label");
            if (labelTf != null)
            {
                var text = labelTf.GetComponent<Text>();
                if (text != null)
                {
                    text.alignment = TextAnchor.MiddleLeft;
                    text.fontSize = 28;
                    text.fontStyle = FontStyle.Normal;
                    text.raycastTarget = false;
                    text.color = new Color(0.07f, 0.1f, 0.22f, 0.95f);
                }
            }

            var iconPath = IconPathForRow(row.name);
            Image iconImage = null;
            if (!string.IsNullOrEmpty(iconPath))
            {
                var iconTf = row.Find("Icon");
                var iconGo = iconTf != null ? iconTf.gameObject : CreateUi("Icon", row);
                iconGo.SetActive(true);
                iconImage = EnsureComponent<Image>(iconGo);
                BindImage(iconImage, LoadLargestSprite(iconPath));
            }

            var button = row.GetComponent<Button>();
            if (button != null)
            {
                button.targetGraphic = shard;
                button.transition = Selectable.Transition.None;
            }

            var view = row.GetComponent<MainMenuButtonRowView>();
            if (view != null)
            {
                view.ConfigureShard(shard, iconImage, normal, selected);
                EditorUtility.SetDirty(view);
            }
        }

        private static string IconPathForRow(string rowName)
        {
            switch (rowName)
            {
                case "Row_NEW_GAME":
                    return PoseDir + "ui_icon_play_v1.png";
                case "Row_LOAD_GAME":
                    return PoseDir + "ui_icon_power_v1.png";
                case "Row_OFF-BEAT_ARCHIVE":
                    return PoseDir + "ui_icon_gallery_v1.png";
                case "Row_CONFIG":
                    return PoseDir + "ui_icon_gear_v1.png";
                case "Row_QUIT":
                    return PoseDir + "ui_icon_power_v1.png";
                default:
                    return null;
            }
        }

        private static void BindImage(Image image, Sprite sprite)
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

        private static void Stretch(GameObject go)
        {
            Stretch(go, Vector2.zero, Vector2.zero);
        }

        private static void Stretch(GameObject go, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static GameObject CreateUi(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
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

        private static RectTransform FindDeep(Transform root, string name)
        {
            if (root.name == name)
            {
                return root as RectTransform;
            }

            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == name)
                {
                    return transforms[i] as RectTransform;
                }
            }

            return null;
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

            if (best == null)
            {
                Debug.LogWarning($"[Fractured Chorus] Sprite not found: {assetPath}");
            }

            return best;
        }
    }
}
#endif

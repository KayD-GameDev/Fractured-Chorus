#if UNITY_EDITOR
using System;
using System.IO;
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Hub;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FracturedChorus.Editor
{
    public static class BondsSceneSetupEditor
    {
        private const string ScenePath = "Assets/FracturedChorus/Scenes/Bonds.unity";
        private const string BackgroundPath = "Assets/FracturedChorus/Art/UI/StatusMenu/statusmenu_hima_city_bg_v1.jpg";
        private const string MockGuidePath = "Assets/FracturedChorus/Art/UI/Bonds/_ref/_ref_bonds_menu_align.jpg";
        private const string BondsRoot = "Assets/FracturedChorus/Art/UI/Bonds/";
        private const string StatCrystalDir = "Assets/FracturedChorus/Art/UI/StatMenu/CrystalKit/";
        private const string PackMapPath = BondsRoot + "bonds_pack_scene_map.json";
        private const string PromoFrameChromeScenePath = "BondsCanvas/LinkEpisodes/PromoFrame/PromoFrameChrome";
        private const string RenPortraitPath = "Assets/FracturedChorus/Art/Characters/Ren/VnBust/ren_bust_neutral_v1.png";
        private const string CharlottePortraitPath = "Assets/FracturedChorus/Art/Characters/Charlotte/VnBust/charlotte_bust_neutral_v1.png";
        private const string CodaPortraitPath = "Assets/FracturedChorus/Art/Characters/Coda/VnBust/coda_bust_neutral_v1.png";
        private const string AstraPortraitPath = "Assets/FracturedChorus/Art/Characters/_Reference/LuxeConcert/astra_ref.png";
        private const float MockGuideAlpha = 0.4f;

        [MenuItem("Fractured Chorus/Bonds/Create Bonds Scene")]
        public static void CreateScene()
        {
            EnsureFolder("Assets/FracturedChorus/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildHierarchy();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Fractured Chorus] Saved {ScenePath}.");
        }

        [MenuItem("Fractured Chorus/Bonds/Heal Bonds Hierarchy")]
        public static void HealScene()
        {
            if (!EditorUtility.DisplayDialog(
                    "Heal Bonds Sandbox",
                    "Destroys BondsCanvas and rebuilds. Manual layout is lost.\nSave snapshot first if needed.",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            if (!File.Exists(ScenePath))
            {
                CreateScene();
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DestroyIfFound("BondsCanvas");
            DestroyIfFound("Main Camera");
            var eventSystem = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (eventSystem != null)
            {
                Object.DestroyImmediate(eventSystem.gameObject);
            }

            BuildHierarchy();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Fractured Chorus] Healed Bonds hierarchy.");
        }

        [MenuItem("Fractured Chorus/Bonds/Attach Missing Layout Objects")]
        public static void AttachMissing()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                EditorUtility.DisplayDialog(
                    "Attach Missing Bonds Objects",
                    "Open Bonds in Unity, then run this menu again. This action will not open scenes automatically.",
                    "OK");
                return;
            }

            var canvas = GameObject.Find("BondsCanvas")?.transform;
            if (canvas == null)
            {
                EditorUtility.DisplayDialog(
                    "Attach Missing Bonds Objects",
                    "BondsCanvas was not found in the open scene. Restore the sandbox hierarchy first, then run this menu again.",
                    "OK");
                return;
            }

            EnsureCamera();
            ConfigureCanvas(canvas.gameObject);
            AttachMissingComponents(canvas);
            FixPromoStack(canvas);
            FixEpisodeRowButtons(canvas);
            FixGlassPanelFillCenter(canvas);
            FixBondExpBar(canvas);
            EnsureSparkDriftFx(canvas);
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[Fractured Chorus] Attached missing Bonds sandbox objects. Save the scene.");
        }

        [MenuItem("Fractured Chorus/Bonds/Fix Promo Stack")]
        public static void FixPromoStackMenu()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                EditorUtility.DisplayDialog(
                    "Fix Promo Stack",
                    "Open Bonds, then run this menu again.",
                    "OK");
                return;
            }

            var canvas = GameObject.Find("BondsCanvas")?.transform;
            if (canvas == null)
            {
                return;
            }

            FixPromoStack(canvas);
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[Fractured Chorus] Promo stack: PromoImage behind PromoFrameChrome.");
        }

        private static void FixPromoStack(Transform canvas)
        {
            var promoFrame = canvas.Find("LinkEpisodes/PromoFrame");
            if (promoFrame == null)
            {
                return;
            }

            var promoImage = promoFrame.Find("PromoImage")?.GetComponent<Image>();
            var chromeTransform = promoFrame.Find("PromoFrameChrome");
            Image promoChrome;
            if (chromeTransform == null)
            {
                promoChrome = EnsureImage(promoFrame, "PromoFrameChrome", out _);
                ConfigureImage(
                    promoChrome,
                    LoadPackByPath(PromoFrameChromeScenePath),
                    Image.Type.Simple,
                    false,
                    Color.white,
                    true,
                    false);
                StretchFull(promoChrome.rectTransform);
            }
            else
            {
                promoChrome = chromeTransform.GetComponent<Image>();
                if (promoChrome != null && promoChrome.sprite == null)
                {
                    ConfigureImage(
                        promoChrome,
                        LoadPackByPath(PromoFrameChromeScenePath),
                        Image.Type.Simple,
                        false,
                        Color.white,
                        true,
                        false);
                }
            }

            var legacyFrameImage = promoFrame.GetComponent<Image>();
            if (legacyFrameImage != null)
            {
                legacyFrameImage.enabled = false;
            }

            if (promoImage != null)
            {
                promoImage.transform.SetAsFirstSibling();
            }

            if (promoChrome != null)
            {
                promoChrome.raycastTarget = false;
                promoChrome.transform.SetAsLastSibling();
            }
        }

        private static void FixEpisodeRowButtons(Transform canvas)
        {
            var episodes = canvas.Find("LinkEpisodes");
            if (episodes == null)
            {
                return;
            }

            for (var i = 1; i <= 5; i++)
            {
                var row = episodes.Find($"Row_{i:00}")?.GetComponent<Button>();
                if (row != null)
                {
                    row.interactable = true;
                }
            }
        }

        [MenuItem("Fractured Chorus/Bonds/Save Layout Snapshot")]
        public static void SaveLayoutSnapshot()
        {
            var scenePath = EditorSceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(scenePath) || !scenePath.EndsWith("Bonds.unity"))
            {
                EditorUtility.DisplayDialog(
                    "Save Bonds Layout",
                    "Focus Bonds, Ctrl+S, run again.",
                    "OK");
                return;
            }

            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                EditorSceneManager.SaveOpenScenes();
            }

            RunNodeTool(
                "Tools/save-bonds-sandbox-layout-snapshot.mjs",
                "Save Bonds Layout",
                out var stdout,
                out var stderr,
                out var exitCode);
            if (exitCode != 0)
            {
                Debug.LogError($"[Fractured Chorus] Snapshot failed: {stderr}\n{stdout}");
                return;
            }

            AssetDatabase.Refresh();
            Debug.Log(
                "[Fractured Chorus] Saved Bonds layout snapshot → " +
                "Assets/FracturedChorus/Art/UI/Bonds/bonds_sandbox_layout_snapshot.json\n" +
                stdout);
        }

        [MenuItem("Fractured Chorus/Bonds/Toggle Mock Guide")]
        public static void ToggleMockGuide()
        {
            var guide = GameObject.Find("BondsCanvas/MockGuide");
            if (guide == null)
            {
                return;
            }

            guide.SetActive(!guide.activeSelf);
            EditorSceneManager.MarkSceneDirty(guide.scene);
        }

        [MenuItem("Fractured Chorus/Bonds/Apply Roster Face Template On Scene")]
        public static void ApplyRosterFaceTemplateOnScene()
        {
            RunNodeTool(
                "Tools/apply-bonds-roster-face-template.mjs",
                "Apply roster face template",
                out var stdout,
                out var stderr,
                out var exitCode);
            if (exitCode != 0)
            {
                Debug.LogError($"[Fractured Chorus] Roster face template failed: {stderr}\n{stdout}");
                return;
            }

            AssetDatabase.Refresh();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.isDirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log("[Fractured Chorus] Applied roster face template on Bonds.\n" + stdout);
        }

        [MenuItem("Fractured Chorus/Bonds/Apply Bootstrap Layout")]
        public static void ApplyBootstrapLayout()
        {
            if (!EditorUtility.DisplayDialog(
                    "Apply Bonds Bootstrap Layout",
                    "Writes bootstrap RectTransforms into Bonds.unity from bonds_sandbox_bootstrap_layout.json.\nManual layout edits are overwritten for listed nodes only.",
                    "Apply",
                    "Cancel"))
            {
                return;
            }

            RunNodeTool(
                "Tools/apply-bonds-sandbox-bootstrap-layout.mjs",
                "Apply Bonds Bootstrap Layout",
                out var stdout,
                out var stderr,
                out var exitCode);
            if (exitCode != 0)
            {
                Debug.LogError($"[Fractured Chorus] Bootstrap layout failed: {stderr}\n{stdout}");
                return;
            }

            AssetDatabase.Refresh();
            Debug.Log("[Fractured Chorus] Applied Bonds bootstrap layout.\n" + stdout);
        }

        private static void BuildHierarchy()
        {
            EnsureCamera();
            EnsureEventSystem();
            var canvas = EnsureCanvas();

            EnsureBackground(canvas);
            EnsureCornerHud(canvas);
            EnsureHeader(canvas);
            EnsureLeftNav(canvas);
            EnsureCenterStats(canvas);
            EnsureDetailCard(canvas);
            EnsureLinkEpisodes(canvas);
            EnsureFooter(canvas);
            EnsureWordmark(canvas);
            EnsureDivider(canvas);
            EnsureTaglineRight(canvas);
            EnsureCompass(canvas);
            EnsureMockGuide(canvas);
            UiFontCatalog.ApplyHierarchy(canvas, true);
        }

        private static Transform EnsureCanvas()
        {
            var existing = GameObject.Find("BondsCanvas");
            if (existing != null)
            {
                ConfigureCanvas(existing);
                return existing.transform;
            }

            var canvasGo = new GameObject(
                "BondsCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(BondsMenuUI));
            ConfigureCanvas(canvasGo);
            return canvasGo.transform;
        }

        private static void ConfigureCanvas(GameObject canvasGo)
        {
            var canvas = EnsureComponent<Canvas>(canvasGo);
            var camera = Camera.main;
            if (camera != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 100f;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            canvas.overrideSorting = false;
            canvas.sortingOrder = 0;

            var scaler = EnsureComponent<CanvasScaler>(canvasGo);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            EnsureComponent<GraphicRaycaster>(canvasGo);
            EnsureComponent<BondsMenuUI>(canvasGo);
        }

        private static void FixGlassPanelFillCenter(Transform canvas)
        {
            var images = canvas.GetComponentsInChildren<Image>(true);
            for (var i = 0; i < images.Length; i++)
            {
                var image = images[i];
                if (image.type != Image.Type.Sliced || image.sprite == null)
                {
                    continue;
                }

                var spritePath = AssetDatabase.GetAssetPath(image.sprite);
                if (spritePath == null)
                {
                    continue;
                }

                if (spritePath.EndsWith("ui_bonds_panel_glass_v1.png") ||
                    spritePath.EndsWith("ui_bonds_header_plate_v1.png"))
                {
                    image.fillCenter = false;
                }
            }
        }

        private static void EnsureBackground(Transform canvas)
        {
            var image = EnsureImage(canvas, "Background", out var created);
            if (!created)
            {
                return;
            }

            StretchFull(image.rectTransform);
            ConfigureImage(image, LoadSprite(BackgroundPath), Image.Type.Simple, false, Color.white, false);
        }

        private static void EnsureCornerHud(Transform canvas)
        {
            var root = EnsurePanel(canvas, "CornerHud", out _);
            var hud = EnsureComponent<HubCornerInfoHud>(root.gameObject);
            EnsureText(root, "DateLabel", "09 / 11", UiFontRole.Display);
            EnsureText(root, "DayLabel", "Fri", UiFontRole.Display);
            var phaseIcon = EnsureImage(root, "PhaseIcon", out var phaseCreated);
            if (phaseCreated)
            {
                ConfigureImage(
                    phaseIcon,
                    LoadPackByPath("BondsCanvas/CornerHud/PhaseIcon"),
                    Image.Type.Simple,
                    false,
                    Color.white,
                    true);
            }

            EnsureText(root, "LocationLabel", BondPresentation.Location, UiFontRole.Display);
            EnsureText(root, "TaglineLabel", BondPresentation.LocationTagline, UiFontRole.Body);
            hud.WireReferences();
            hud.ApplyFonts();
        }

        private static void EnsureHeader(Transform canvas)
        {
            var header = EnsureImage(canvas, "HeaderBonds", out var created);
            if (created)
            {
                ConfigureImage(
                    header,
                    LoadPackByPath("BondsCanvas/HeaderBonds"),
                    Image.Type.Simple,
                    false,
                    Color.white,
                    true,
                    false);
            }

            var icon = EnsureImage(header.transform, "Icon", out created);
            if (created)
            {
                ConfigureImage(
                    icon,
                    LoadPackByPath("BondsCanvas/HeaderBonds/Icon"),
                    Image.Type.Simple,
                    false,
                    Color.white,
                    true);
            }

            EnsureText(header.transform, "Label", BondPresentation.Title, UiFontRole.Display);
            EnsureText(header.transform, "LabelJp", BondPresentation.TitleJp, UiFontRole.Display);
        }

        private static void EnsureLeftNav(Transform canvas)
        {
            var nav = EnsurePanel(canvas, "LeftNav", out _);
            EnsureNavRow(nav, "Row_SocialStats", "Social Stats", true);
            EnsureNavRow(nav, "Row_Link", "Link", true);
            EnsureNavRow(nav, "Row_Conversations", "Conversations", true);
            EnsureNavRow(nav, "Row_Memories", "Memories", true);
            EnsureNavRow(nav, "Row_Gallery", "Gallery", true);
        }

        private static void EnsureNavRow(Transform parent, string name, string label, bool interactable)
        {
            var row = EnsureButton(parent, name, out var created);
            row.interactable = interactable;
            var rowPath = "BondsCanvas/LeftNav/" + name;
            if (created)
            {
                ConfigureImage(
                    row.GetComponent<Image>(),
                    LoadPackByPath(rowPath),
                    Image.Type.Simple,
                    true,
                    Color.white,
                    true,
                    false);
            }

            var icon = EnsureImage(row.transform, "Icon", out created);
            if (created)
            {
                ConfigureImage(
                    icon,
                    LoadPackByPath(rowPath + "/Icon"),
                    Image.Type.Simple,
                    false,
                    Color.white,
                    true);
            }

            EnsureText(row.transform, "Label", label, UiFontRole.Body);
        }

        private static void EnsureCenterStats(Transform canvas)
        {
            var center = EnsureImage(canvas, "CenterStats", out var created);
            if (created)
            {
                ConfigureImage(
                    center,
                    LoadPackByPath("BondsCanvas/CenterStats"),
                    Image.Type.Simple,
                    false,
                    Color.white,
                    true,
                    false);
            }

            EnsureText(center.transform, "Title", BondPresentation.SocialStatsTitle, UiFontRole.Display);
            EnsureText(center.transform, "TitleJp", BondPresentation.SocialStatsJp, UiFontRole.Display);

            var chartRoot = EnsurePanel(center.transform, "ChartRoot", out _);
            EnsureRadar(chartRoot, "Radar");

            EnsureStatNode(center.transform, "Node_Resonance");
            EnsureStatNode(center.transform, "Node_Cadence");
            EnsureStatNode(center.transform, "Node_Pulse");
            EnsureStatNode(center.transform, "Node_Harmony");
            EnsureStatNode(center.transform, "Node_Rhythm");

            var roster = EnsurePanel(center.transform, "Roster", out _);
            EnsureChip(roster, 0, "Ren", "Player", LoadSprite("Assets/FracturedChorus/Art/Characters/Ren/VnBust/ren_bust_neutral_v1.png"), false);
            EnsureChip(roster, 1, "Charlotte", string.Empty, LoadSprite("Assets/FracturedChorus/Art/Characters/Charlotte/VnBust/charlotte_bust_neutral_v1.png"), false);
            EnsureChip(roster, 2, "Coda", string.Empty, LoadSprite("Assets/FracturedChorus/Art/Characters/Coda/VnBust/coda_bust_neutral_v1.png"), false);
            EnsureChip(roster, 3, "Astra", string.Empty, LoadSprite("Assets/FracturedChorus/Art/Characters/_Reference/LuxeConcert/astra_ref.png"), false);
            var locked = LoadSprite(BondsRoot + "Decor/ui_bonds_silhouette_locked_v1.png");
            EnsureChip(roster, 4, "Ryo", string.Empty, locked, true);

            var chevron = EnsureImage(roster, "Chevron", out created);
            if (created)
            {
                ConfigureImage(
                    chevron,
                    LoadPackByPath("BondsCanvas/CenterStats/Roster/Chevron"),
                    Image.Type.Simple,
                    false,
                    Color.white,
                    true);
            }

            var chevronPrev = EnsureImage(roster, "ChevronPrev", out created);
            if (created)
            {
                ConfigureImage(
                    chevronPrev,
                    LoadPackByPath("BondsCanvas/CenterStats/Roster/Chevron"),
                    Image.Type.Simple,
                    false,
                    Color.white,
                    true);
                var src = chevron.rectTransform;
                var dst = chevronPrev.rectTransform;
                dst.anchorMin = src.anchorMin;
                dst.anchorMax = src.anchorMax;
                dst.pivot = src.pivot;
                dst.sizeDelta = src.sizeDelta;
                dst.anchoredPosition = new Vector2(-src.anchoredPosition.x, src.anchoredPosition.y);
                dst.localScale = new Vector3(-1f, 1f, 1f);
            }
        }

        private static SocialStatsRadarGraphic EnsureRadar(Transform parent, string name)
        {
            var rect = Ensure(parent, name, out _);
            EnsureComponent<CanvasRenderer>(rect.gameObject);
            return EnsureComponent<SocialStatsRadarGraphic>(rect.gameObject);
        }

        private static void EnsureStatNode(Transform parent, string name)
        {
            var root = EnsurePanel(parent, name, out _);
            EnsureComponent<SocialStatsNodeView>(root.gameObject);
            EnsureStatNodeWidgets(root);
        }

        private static void EnsureChip(Transform parent, int index, string displayName, string role, Sprite faceSprite, bool locked)
        {
            var chip = EnsurePanel(parent, $"Chip_{index}", out _);

            var frame = EnsureImage(chip, "Frame", out var created);
            if (created)
            {
                ConfigureImage(
                    frame,
                    LoadPackByPath("BondsCanvas/CenterStats/Roster/Chip_0/Frame"),
                    Image.Type.Simple,
                    false,
                    Color.white,
                    true,
                    false);
            }

            var face = EnsureImage(chip, "Face", out created);
            if (created)
            {
                ConfigureImage(face, faceSprite, Image.Type.Simple, false, Color.white, true);
            }

            var lockImage = EnsureImage(chip, "Lock", out created);
            if (created)
            {
                ConfigureImage(
                    lockImage,
                    LoadPackFile(PackMap().refs.lockIcon),
                    Image.Type.Simple,
                    false,
                    locked ? Color.white : new Color(1f, 1f, 1f, 0f),
                    true);
            }

            EnsureText(chip, "Name", displayName, UiFontRole.Body);
            EnsureText(chip, "Role", role, UiFontRole.Body);
            EnsureChipChildOrder(chip);
        }

        private static void EnsureChipChildOrder(Transform chip)
        {
            if (chip == null)
            {
                return;
            }

            var order = new[] { "Frame", "Face", "Lock", "Name", "Role" };
            for (var i = 0; i < order.Length; i++)
            {
                var child = chip.Find(order[i]);
                if (child != null)
                {
                    child.SetSiblingIndex(i);
                }
            }
        }

        private static void EnsureDetailCard(Transform canvas)
        {
            var card = EnsureImage(canvas, "DetailCard", out var created);
            if (created)
            {
                ConfigureImage(
                    card,
                    LoadPackByPath("BondsCanvas/DetailCard"),
                    Image.Type.Simple,
                    false,
                    Color.white,
                    true,
                    false);
            }

            var portrait = EnsureImage(card.transform, "Portrait", out created);
            if (created)
            {
                ConfigureImage(
                    portrait,
                    LoadSprite("Assets/FracturedChorus/Art/Characters/Charlotte/VnBust/charlotte_bust_neutral_v1.png"),
                    Image.Type.Simple,
                    false,
                    Color.white,
                    true);
            }

            EnsureText(card.transform, "Name", "Charlotte", UiFontRole.Display);
            EnsureText(card.transform, "Rank", "Rank 1", UiFontRole.Display);
            EnsureText(card.transform, "Bio", BondPresentation.GetBio("charlotte"), UiFontRole.Body);
            EnsureText(card.transform, "Quote", BondPresentation.GetQuote("charlotte"), UiFontRole.Body);

            var expTrack = EnsureImage(card.transform, "ExpTrack", out created);
            if (created)
            {
                ConfigureImage(
                    expTrack,
                    LoadSprite(StatCrystalDir + "ui_stat_bar_track_v3.png"),
                    Image.Type.Simple,
                    false,
                    Color.white,
                    false);
            }

            var expFill = EnsureImage(expTrack.transform, "ExpFill", out created);
            if (created)
            {
                ConfigureBondExpFill(expFill);
            }

            EnsureText(card.transform, "ExpLabel", "0 / 10", UiFontRole.Display);
            EnsureText(card.transform, "NextRank", BondPresentation.NextRankLabel, UiFontRole.Display);
            EnsureText(card.transform, "NextHint", BondPresentation.NextRankHint, UiFontRole.Body);
        }

        private static void EnsureLinkEpisodes(Transform canvas)
        {
            var root = EnsureImage(canvas, "LinkEpisodes", out var created);
            if (created)
            {
                ConfigureImage(
                    root,
                    LoadPackByPath("BondsCanvas/LinkEpisodes"),
                    Image.Type.Simple,
                    false,
                    Color.white,
                    true,
                    false);
            }

            EnsureText(root.transform, "Title", BondPresentation.LinkEpisodesTitle, UiFontRole.Display);
            EnsureText(root.transform, "TitleJp", BondPresentation.LinkEpisodesJp, UiFontRole.Display);
            EnsureText(root.transform, "Hint", BondPresentation.EpisodeLockHint, UiFontRole.Body);

            var promoFrame = EnsurePanel(root.transform, "PromoFrame", out created);

            var promoImage = EnsureImage(promoFrame, "PromoImage", out var promoImageCreated);
            if (promoImageCreated)
            {
                ConfigureImage(
                    promoImage,
                    null,
                    Image.Type.Simple,
                    false,
                    Color.white,
                    true);
            }

            var promoChrome = EnsureImage(promoFrame, "PromoFrameChrome", out var chromeCreated);
            if (chromeCreated)
            {
                ConfigureImage(
                    promoChrome,
                    LoadPackByPath(PromoFrameChromeScenePath),
                    Image.Type.Simple,
                    false,
                    Color.white,
                    true,
                    false);
                StretchFull(promoChrome.rectTransform);
            }

            var legacyFrameImage = promoFrame.GetComponent<Image>();
            if (legacyFrameImage != null)
            {
                legacyFrameImage.enabled = false;
            }

            if (promoImageCreated)
            {
                promoImage.transform.SetAsFirstSibling();
            }

            if (chromeCreated)
            {
                promoChrome.transform.SetAsLastSibling();
            }

            EnsureText(root.transform, "PromoCaption", BondPresentation.PromoCaption, UiFontRole.Body);
            EnsureEpisodeRow(root.transform, 1, "A Usual Day");
            EnsureEpisodeRow(root.transform, 2, "After Class");
            EnsureEpisodeRow(root.transform, 3, "A Different Melody");
            EnsureEpisodeRow(root.transform, 4, "Unspoken Words");
            EnsureEpisodeRow(root.transform, 5, "Toward Tomorrow");
        }

        private static void EnsureEpisodeRow(Transform parent, int index, string title)
        {
            var row = EnsureButton(parent, $"Row_{index:00}", out var created);
            row.interactable = true;
            if (created)
            {
                ConfigureImage(
                    row.GetComponent<Image>(),
                    LoadPackByPath($"BondsCanvas/LinkEpisodes/Row_{index:00}"),
                    Image.Type.Simple,
                    true,
                    Color.white,
                    true,
                    false);
            }

            var icon = EnsureImage(row.transform, "Icon", out created);
            if (created)
            {
                ConfigureImage(
                    icon,
                    LoadPackByPath($"BondsCanvas/LinkEpisodes/Row_{index:00}/Icon"),
                    Image.Type.Simple,
                    false,
                    Color.white,
                    true);
            }

            EnsureText(row.transform, "Index", index.ToString("00"), UiFontRole.Display);
            EnsureText(row.transform, "Label", title, UiFontRole.Body);
        }

        private static void EnsureFooter(Transform canvas)
        {
            var footer = EnsurePanel(canvas, "Footer", out _);
            var confirmIcon = EnsureImage(footer, "ConfirmIcon", out var created);
            if (created)
            {
                ConfigureImage(
                    confirmIcon,
                    LoadPackByPath("BondsCanvas/Footer/ConfirmIcon"),
                    Image.Type.Simple,
                    false,
                    Color.white,
                    true);
            }

            EnsureText(footer, "ConfirmLabel", "Confirm", UiFontRole.Display);
            var backIcon = EnsureImage(footer, "BackIcon", out created);
            if (created)
            {
                ConfigureImage(
                    backIcon,
                    LoadPackByPath("BondsCanvas/Footer/BackIcon"),
                    Image.Type.Simple,
                    false,
                    Color.white,
                    true);
            }

            EnsureText(footer, "BackLabel", "Back", UiFontRole.Display);
        }

        private static void EnsureWordmark(Transform canvas)
        {
            var root = EnsurePanel(canvas, "Wordmark", out _);
            EnsureText(root, "Title", BondPresentation.Wordmark, UiFontRole.Display);
            EnsureText(root, "Sub", BondPresentation.WordmarkSub, UiFontRole.Body);
        }

        private static void EnsureDivider(Transform canvas)
        {
            var divider = EnsureImage(canvas, "Divider", out var created);
            if (!created)
            {
                return;
            }

            ConfigureImage(
                divider,
                LoadPackByPath("BondsCanvas/Divider"),
                Image.Type.Simple,
                false,
                Color.white,
                true);
        }

        private static void EnsureTaglineRight(Transform canvas)
        {
            EnsureText(canvas, "TaglineRight", BondPresentation.TaglineRight, UiFontRole.Display);
        }

        private static void EnsureCompass(Transform canvas)
        {
            var compass = EnsureImage(canvas, "Compass", out var created);
            if (!created)
            {
                return;
            }

            ConfigureImage(
                compass,
                LoadPackByPath("BondsCanvas/Compass"),
                Image.Type.Simple,
                false,
                Color.white,
                true);
        }

        private static void EnsureMockGuide(Transform canvas)
        {
            var image = EnsureImage(canvas, "MockGuide", out var created);
            if (!created)
            {
                return;
            }

            StretchFull(image.rectTransform);
            ConfigureImage(
                image,
                LoadSprite(MockGuidePath),
                Image.Type.Simple,
                false,
                new Color(1f, 1f, 1f, MockGuideAlpha),
                false);
            image.gameObject.SetActive(false);
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null)
            {
                ConfigureCamera(Camera.main);
                return;
            }

            var existing = GameObject.Find("Main Camera");
            if (existing != null)
            {
                ConfigureCamera(EnsureComponent<Camera>(existing));
                EnsureComponent<AudioListener>(existing);
                return;
            }

            var cameraGo = new GameObject(
                "Main Camera",
                typeof(Camera),
                typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            ConfigureCamera(cameraGo.GetComponent<Camera>());
        }

        private static void ConfigureCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.011764706f, 0.03529412f, 0.078431375f, 1f);
            camera.depth = -1f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            EnsureComponent<UniversalAdditionalCameraData>(camera.gameObject);
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem));
                CombatInputSetup.ApplyInputModule(go, destroyImmediate: true);
                return;
            }

            CombatInputSetup.ApplyInputModule(eventSystem.gameObject, destroyImmediate: true);
        }

        private static RectTransform EnsurePanel(Transform parent, string name, out bool created)
        {
            return Ensure(parent, name, out created);
        }

        private static Image EnsureImage(Transform parent, string name, out bool created)
        {
            var rect = Ensure(parent, name, out created);
            EnsureComponent<CanvasRenderer>(rect.gameObject);
            return EnsureComponent<Image>(rect.gameObject);
        }

        private static Button EnsureButton(Transform parent, string name, out bool created)
        {
            var image = EnsureImage(parent, name, out created);
            var button = EnsureComponent<Button>(image.gameObject);
            button.targetGraphic = image;
            return button;
        }

        private static Text EnsureText(Transform parent, string name, string content, UiFontRole role)
        {
            var rect = Ensure(parent, name, out _);
            EnsureComponent<CanvasRenderer>(rect.gameObject);
            var text = EnsureComponent<Text>(rect.gameObject);
            text.text = content;
            text.color = Color.white;
            text.raycastTarget = false;
            UiFontCatalog.Apply(text, role, text.fontSize > 0 ? text.fontSize : 18);
            return text;
        }

        private static void FixBondExpBar(Transform canvas)
        {
            var detail = FindPath(canvas, "CenterStats/Roster/DetailCard") ?? FindPath(canvas, "DetailCard");
            if (detail == null)
            {
                return;
            }

            var track = FindPath(detail, "ExpTrack")?.GetComponent<Image>();
            if (track != null)
            {
                ConfigureImage(
                    track,
                    LoadSprite(StatCrystalDir + "ui_stat_bar_track_v3.png"),
                    Image.Type.Simple,
                    false,
                    Color.white,
                    false);

                var mask = track.GetComponent<RectMask2D>();
                if (mask != null)
                {
                    Object.DestroyImmediate(mask);
                }
            }

            var fill = FindPath(detail, "ExpTrack/ExpFill")?.GetComponent<Image>();
            if (fill != null)
            {
                ConfigureBondExpFill(fill);
            }
        }

        private static void ConfigureBondExpFill(Image fill)
        {
            ConfigureImage(
                fill,
                LoadSprite(StatCrystalDir + "ui_stat_bar_fill_v3.png"),
                Image.Type.Filled,
                false,
                Color.white,
                false);
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0.35f;
            StretchFull(fill.rectTransform);
        }

        private static void EnsureSparkDriftFx(Transform canvas)
        {
            var background = FindPath(canvas, "Background");
            if (background == null)
            {
                return;
            }

            var sparkRoot = background.Find("SparkFx");
            if (sparkRoot == null)
            {
                return;
            }

            EnsureComponent<BondsMenuSparkDrift>(sparkRoot.gameObject);
            var sparkSprite = LoadPackFile("30_Decor_Spark.png");
            if (sparkSprite == null)
            {
                return;
            }

            for (var i = 0; i < sparkRoot.childCount; i++)
            {
                var image = sparkRoot.GetChild(i).GetComponent<Image>();
                if (image == null || image.sprite != null)
                {
                    continue;
                }

                ConfigureImage(image, sparkSprite, Image.Type.Simple, false, image.color, true);
            }
        }

        private static void ConfigureImage(
            Image image,
            Sprite sprite,
            Image.Type type,
            bool raycastTarget,
            Color color,
            bool preserveAspect,
            bool fillCenter = true)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = type;
            image.raycastTarget = raycastTarget;
            image.color = color;
            image.preserveAspect = preserveAspect;
            if (type == Image.Type.Sliced)
            {
                image.fillCenter = fillCenter;
            }
        }

        [Serializable]
        private class PackSceneMap
        {
            public string packDir;
            public PackAssign[] assignments;
            public PackRefs refs;
        }

        [Serializable]
        private class PackAssign
        {
            public string path;
            public string file;
        }

        [Serializable]
        private class PackRefs
        {
            public string[] statIcons;
            public string chipFrameNormal;
            public string chipFrameSelected;
            public string chipFrameLocked;
            public string lockIcon;
            public string playSprite;
            public string lockSprite;
            public string menuNormal;
            public string menuSelected;
            public string episodeRowNormal;
            public string episodeRowSelected;
        }

        private static PackSceneMap _packMap;

        private static PackSceneMap PackMap()
        {
            if (_packMap != null)
            {
                return _packMap;
            }

            _packMap = JsonUtility.FromJson<PackSceneMap>(File.ReadAllText(PackMapPath));
            return _packMap;
        }

        private static Sprite LoadPackByPath(string scenePath)
        {
            var map = PackMap();
            for (var i = 0; i < map.assignments.Length; i++)
            {
                if (map.assignments[i].path == scenePath)
                {
                    return LoadPackFile(map.assignments[i].file);
                }
            }

            return null;
        }

        private static Sprite LoadPackFile(string file)
        {
            return LoadSprite(PackMap().packDir + "/" + file);
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (var i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite found)
                {
                    return found;
                }
            }

            return null;
        }

        private static RectTransform Ensure(Transform parent, string name, out bool created)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                created = false;
                var rt = existing as RectTransform ?? existing.GetComponent<RectTransform>();
                return rt;
            }

            created = true;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void AttachMissingComponents(Transform canvas)
        {
            var menu = EnsureComponent<BondsMenuUI>(canvas.gameObject);
            var cornerHud = AttachCornerHud(canvas);
            var radar = AttachRadar(canvas);
            var detail = AttachDetailCard(canvas);
            var chips = AttachChipViews(canvas);
            var rows = AttachEpisodeRows(canvas);
            var nav = AttachNavRows(canvas);
            var nodes = AttachStatNodes(canvas);

            var so = new SerializedObject(menu);
            SetObjectRef(so, "root", canvas.gameObject);
            SetObjectRef(so, "cornerHud", cornerHud);
            SetObjectRef(so, "radar", radar);
            SetObjectArray(so.FindProperty("nodes"), nodes);
            var refs = PackMap().refs;
            SetSpriteArray(
                so.FindProperty("statIcons"),
                new[]
                {
                    LoadPackFile(refs.statIcons[0]),
                    LoadPackFile(refs.statIcons[1]),
                    LoadPackFile(refs.statIcons[2]),
                    LoadPackFile(refs.statIcons[3]),
                    LoadPackFile(refs.statIcons[4])
                });
            SetObjectArray(so.FindProperty("chips"), chips);
            SetObjectRef(so, "rosterChevron", AttachRosterChevron(canvas, "CenterStats/Roster/Chevron"));
            SetObjectRef(so, "rosterChevronPrev", AttachRosterChevron(canvas, "CenterStats/Roster/ChevronPrev"));
            SetObjectRef(so, "chipFrameNormal", LoadPackFile(refs.chipFrameNormal));
            SetObjectRef(so, "chipFrameSelected", LoadPackFile(refs.chipFrameSelected));
            SetObjectRef(so, "chipFrameLocked", LoadPackFile(refs.chipFrameLocked));
            SetObjectRef(so, "lockIcon", LoadPackFile(refs.lockIcon));
            var portraits = so.FindProperty("portraitSprites");
            if (portraits != null && (portraits.arraySize == 0 || portraits.GetArrayElementAtIndex(0).objectReferenceValue == null))
            {
                var lockedPortrait = LoadSprite(BondsRoot + "Decor/ui_bonds_silhouette_locked_v1.png");
                SetSpriteArray(
                    portraits,
                    new[]
                    {
                        LoadSprite(RenPortraitPath),
                        LoadSprite(CharlottePortraitPath),
                        LoadSprite(CodaPortraitPath),
                        LoadSprite(AstraPortraitPath),
                        lockedPortrait,
                        lockedPortrait
                    });
                SetObjectRef(so, "reservedPortrait", lockedPortrait);
            }

            SetObjectRef(
                so,
                "storyHiddenPortrait",
                LoadSprite("Assets/FracturedChorus/Art/UI/Bonds/Cards/bond_card_story_hidden_v1.png"));
            SetObjectRef(so, "detail", detail);
            SetObjectArray(so.FindProperty("episodeRows"), rows);
            SetObjectArray(so.FindProperty("navRows"), nav);
            SetObjectRef(so, "navPlateNormal", LoadPackFile(refs.menuNormal));
            SetObjectRef(so, "navPlateSelected", LoadPackFile(refs.menuSelected));
            SetObjectRef(so, "socialStatsTitle", FindPath(canvas, "CenterStats/Title")?.GetComponent<Text>());
            SetObjectRef(so, "socialStatsJp", FindPath(canvas, "CenterStats/TitleJp")?.GetComponent<Text>());
            SetObjectRef(so, "linkTitle", FindPath(canvas, "LinkEpisodes/Title")?.GetComponent<Text>());
            SetObjectRef(so, "linkJp", FindPath(canvas, "LinkEpisodes/TitleJp")?.GetComponent<Text>());
            SetObjectRef(so, "episodeHint", FindPath(canvas, "LinkEpisodes/Hint")?.GetComponent<Text>());
            SetObjectRef(so, "headerLabel", FindPath(canvas, "HeaderBonds/Label")?.GetComponent<Text>());
            SetObjectRef(so, "headerJp", FindPath(canvas, "HeaderBonds/LabelJp")?.GetComponent<Text>());
            SetObjectRef(so, "confirmLabel", FindPath(canvas, "Footer/ConfirmLabel")?.GetComponent<Text>());
            SetObjectRef(so, "backLabel", FindPath(canvas, "Footer/BackLabel")?.GetComponent<Text>());
            SetObjectRef(
                so,
                "promoImage",
                FindPath(canvas, "LinkEpisodes/PromoFrame/PromoImage")?.GetComponent<Image>());
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static HubCornerInfoHud AttachCornerHud(Transform canvas)
        {
            var root = FindPath(canvas, "CornerHud");
            if (root == null)
            {
                return null;
            }

            var hud = EnsureComponent<HubCornerInfoHud>(root.gameObject);
            var so = new SerializedObject(hud);
            SetObjectRef(so, "dateLabel", FindPath(root, "DateLabel")?.GetComponent<Text>());
            SetObjectRef(so, "dayLabel", FindPath(root, "DayLabel")?.GetComponent<Text>());
            SetObjectRef(so, "phaseIcon", FindPath(root, "PhaseIcon")?.GetComponent<Image>());
            SetObjectRef(so, "locationLabel", FindPath(root, "LocationLabel")?.GetComponent<Text>());
            SetObjectRef(so, "taglineLabel", FindPath(root, "TaglineLabel")?.GetComponent<Text>());
            so.ApplyModifiedPropertiesWithoutUndo();
            hud.ApplyFonts();
            return hud;
        }

        private static SocialStatsRadarGraphic AttachRadar(Transform canvas)
        {
            var radarRoot = FindPath(canvas, "CenterStats/ChartRoot/Radar");
            return radarRoot != null ? EnsureComponent<SocialStatsRadarGraphic>(radarRoot.gameObject) : null;
        }

        private static SocialStatsNodeView[] AttachStatNodes(Transform canvas)
        {
            return new[]
            {
                AttachStatNode(FindPath(canvas, "CenterStats/Node_Resonance")),
                AttachStatNode(FindPath(canvas, "CenterStats/Node_Cadence")),
                AttachStatNode(FindPath(canvas, "CenterStats/Node_Pulse")),
                AttachStatNode(FindPath(canvas, "CenterStats/Node_Harmony")),
                AttachStatNode(FindPath(canvas, "CenterStats/Node_Rhythm"))
            };
        }

        private static SocialStatsNodeView AttachStatNode(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            var view = EnsureComponent<SocialStatsNodeView>(root.gameObject);
            var icon = EnsureStatNodeIcon(root);
            var name = EnsureStatNodeText(root, "Name", UiFontRole.Body);
            var rank = EnsureStatNodeText(root, "Rank", UiFontRole.Body);
            var flavor = EnsureStatNodeText(root, "Flavor", UiFontRole.Body);
            var so = new SerializedObject(view);
            SetObjectRef(so, "iconImage", icon);
            SetObjectRef(so, "nameLabel", name);
            SetObjectRef(so, "rankLabel", rank);
            SetObjectRef(so, "flavorLabel", flavor);
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static void EnsureStatNodeWidgets(Transform root)
        {
            if (root == null)
            {
                return;
            }

            EnsureStatNodeIcon(root);
            EnsureStatNodeText(root, "Name", UiFontRole.Body);
            EnsureStatNodeText(root, "Rank", UiFontRole.Body);
            EnsureStatNodeText(root, "Flavor", UiFontRole.Body);
        }

        private static Image EnsureStatNodeIcon(Transform root)
        {
            var rect = Ensure(root, "Icon", out _);
            EnsureComponent<CanvasRenderer>(rect.gameObject);
            var image = EnsureComponent<Image>(rect.gameObject);
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        private static Text EnsureStatNodeText(Transform root, string name, UiFontRole role)
        {
            var rect = Ensure(root, name, out var created);
            EnsureComponent<CanvasRenderer>(rect.gameObject);
            var text = EnsureComponent<Text>(rect.gameObject);
            if (created || string.IsNullOrEmpty(text.text))
            {
                text.text = name switch
                {
                    "Name" => root.name.Replace("Node_", string.Empty),
                    "Rank" => "Rank 1",
                    "Flavor" => "Preview copy.",
                    _ => string.Empty
                };
            }

            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            UiFontCatalog.Apply(text, role, text.fontSize > 0 ? text.fontSize : name == "Flavor" ? 11 : 14);
            return text;
        }

        private static BondRosterChipView[] AttachChipViews(Transform canvas)
        {
            var roster = FindPath(canvas, "CenterStats/Roster");
            if (roster == null)
            {
                return new BondRosterChipView[0];
            }

            var chips = new BondRosterChipView[BondPresentation.VisibleChipSlots];
            for (var i = 0; i < chips.Length; i++)
            {
                chips[i] = AttachChipView(FindPath(roster, $"Chip_{i}"));
            }

            for (var i = BondPresentation.VisibleChipSlots; ; i++)
            {
                var extra = FindPath(roster, $"Chip_{i}");
                if (extra == null)
                {
                    break;
                }

                extra.gameObject.SetActive(false);
            }

            return chips;
        }

        private static Button AttachRosterChevron(Transform canvas, string path)
        {
            var root = FindPath(canvas, path);
            if (root == null)
            {
                return null;
            }

            var button = EnsureComponent<Button>(root.gameObject);
            var image = root.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                button.targetGraphic = image;
            }

            return button;
        }

        private static BondRosterChipView AttachChipView(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            var button = EnsureComponent<Button>(root.gameObject);
            var frame = FindPath(root, "Frame")?.GetComponent<Image>();
            if (frame != null)
            {
                frame.raycastTarget = true;
                frame.preserveAspect = true;
                button.targetGraphic = frame;
            }

            var face = FindPath(root, "Face")?.GetComponent<Image>();
            if (face != null)
            {
                face.raycastTarget = false;
                face.preserveAspect = true;
            }

            var view = EnsureComponent<BondRosterChipView>(root.gameObject);
            var so = new SerializedObject(view);
            SetObjectRef(so, "frame", frame);
            SetObjectRef(so, "face", face);
            SetObjectRef(so, "lockIcon", FindPath(root, "Lock")?.GetComponent<Image>());
            SetObjectRef(so, "nameLabel", FindPath(root, "Name")?.GetComponent<Text>());
            SetObjectRef(so, "roleLabel", FindPath(root, "Role")?.GetComponent<Text>());
            SetObjectRef(so, "button", button);
            so.ApplyModifiedPropertiesWithoutUndo();
            EnsureChipChildOrder(root);
            return view;
        }

        private static BondDetailCardView AttachDetailCard(Transform canvas)
        {
            var root = FindPath(canvas, "CenterStats/Roster/DetailCard") ?? FindPath(canvas, "DetailCard");
            if (root == null)
            {
                return null;
            }

            var view = EnsureComponent<BondDetailCardView>(root.gameObject);
            var so = new SerializedObject(view);
            SetObjectRef(so, "portrait", FindPath(root, "Portrait")?.GetComponent<Image>());
            SetObjectRef(so, "nameLabel", FindPath(root, "Name")?.GetComponent<Text>());
            SetObjectRef(so, "rankLabel", FindPath(root, "Rank")?.GetComponent<Text>());
            SetObjectRef(so, "bioLabel", FindPath(root, "Bio")?.GetComponent<Text>());
            SetObjectRef(so, "quoteLabel", FindPath(root, "Quote")?.GetComponent<Text>());
            SetObjectRef(so, "expFill", FindPath(root, "ExpTrack/ExpFill")?.GetComponent<Image>());
            SetObjectRef(so, "expLabel", FindPath(root, "ExpLabel")?.GetComponent<Text>());
            SetObjectRef(so, "nextRankLabel", FindPath(root, "NextRank")?.GetComponent<Text>());
            SetObjectRef(so, "nextHintLabel", FindPath(root, "NextHint")?.GetComponent<Text>());
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static BondEpisodeRowView[] AttachEpisodeRows(Transform canvas)
        {
            var root = FindPath(canvas, "LinkEpisodes");
            if (root == null)
            {
                return new BondEpisodeRowView[0];
            }

            return new[]
            {
                AttachEpisodeRow(FindPath(root, "Row_01")),
                AttachEpisodeRow(FindPath(root, "Row_02")),
                AttachEpisodeRow(FindPath(root, "Row_03")),
                AttachEpisodeRow(FindPath(root, "Row_04")),
                AttachEpisodeRow(FindPath(root, "Row_05"))
            };
        }

        private static BondEpisodeRowView AttachEpisodeRow(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            var view = EnsureComponent<BondEpisodeRowView>(root.gameObject);
            var button = EnsureComponent<Button>(root.gameObject);
            var image = root.GetComponent<Image>();
            if (image != null)
            {
                button.targetGraphic = image;
            }

            var refs = PackMap().refs;
            var so = new SerializedObject(view);
            SetObjectRef(so, "plate", image);
            SetObjectRef(so, "icon", FindPath(root, "Icon")?.GetComponent<Image>());
            SetObjectRef(so, "indexLabel", FindPath(root, "Index")?.GetComponent<Text>());
            SetObjectRef(so, "titleLabel", FindPath(root, "Label")?.GetComponent<Text>());
            SetObjectRef(so, "button", button);
            SetObjectRef(so, "plateNormal", LoadPackFile(refs.episodeRowNormal));
            SetObjectRef(so, "plateSelected", LoadPackFile(refs.episodeRowSelected));
            SetObjectRef(so, "playSprite", LoadPackFile(refs.playSprite));
            SetObjectRef(so, "lockSprite", LoadPackFile(refs.lockSprite));
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static BondNavRowView[] AttachNavRows(Transform canvas)
        {
            var nav = FindPath(canvas, "LeftNav");
            if (nav == null)
            {
                return new BondNavRowView[0];
            }

            return new[]
            {
                AttachNavRow(FindPath(nav, "Row_SocialStats")),
                AttachNavRow(FindPath(nav, "Row_Link")),
                AttachNavRow(FindPath(nav, "Row_Conversations")),
                AttachNavRow(FindPath(nav, "Row_Memories")),
                AttachNavRow(FindPath(nav, "Row_Gallery"))
            };
        }

        private static BondNavRowView AttachNavRow(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            var button = EnsureComponent<Button>(root.gameObject);
            var plate = root.GetComponent<Image>();
            if (plate != null)
            {
                button.targetGraphic = plate;
            }

            button.interactable = true;
            button.transition = Selectable.Transition.None;
            var view = EnsureComponent<BondNavRowView>(root.gameObject);
            var refs = PackMap().refs;
            var so = new SerializedObject(view);
            SetObjectRef(so, "plate", plate);
            SetObjectRef(so, "label", FindPath(root, "Label")?.GetComponent<Text>());
            SetObjectRef(so, "button", button);
            SetObjectRef(so, "plateNormal", LoadPackFile(refs.menuNormal));
            SetObjectRef(so, "plateSelected", LoadPackFile(refs.menuSelected));
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static Transform FindPath(Transform root, string path)
        {
            if (root == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            var parts = path.Split('/');
            var current = root;
            for (var i = 0; i < parts.Length; i++)
            {
                current = current.Find(parts[i]);
                if (current == null)
                {
                    return null;
                }
            }

            return current;
        }

        private static void SetObjectRef(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static void SetObjectArray<T>(SerializedProperty prop, T[] values) where T : Object
        {
            if (prop == null)
            {
                return;
            }

            prop.arraySize = values?.Length ?? 0;
            for (var i = 0; i < prop.arraySize; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static void SetSpriteArray(SerializedProperty prop, Sprite[] values)
        {
            if (prop == null)
            {
                return;
            }

            prop.arraySize = values?.Length ?? 0;
            for (var i = 0; i < prop.arraySize; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static void DestroyIfFound(string objectName)
        {
            var found = GameObject.Find(objectName);
            if (found != null)
            {
                Object.DestroyImmediate(found);
            }
        }

        private static void RunNodeTool(string relativeToolPath, string label)
        {
            RunNodeTool(relativeToolPath, label, out _, out _, out _);
        }

        private static void RunNodeTool(
            string relativeToolPath,
            string label,
            out string stdout,
            out string stderr,
            out int exitCode)
        {
            stdout = string.Empty;
            stderr = string.Empty;
            exitCode = -1;

            var tool = Path.GetFullPath(relativeToolPath);
            if (!File.Exists(tool))
            {
                Debug.LogError($"[Fractured Chorus] Missing tool: {tool}");
                return;
            }

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "node",
                Arguments = $"\"{tool}\"",
                WorkingDirectory = Path.GetFullPath("."),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using (var proc = System.Diagnostics.Process.Start(psi))
            {
                stdout = proc.StandardOutput.ReadToEnd();
                stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                exitCode = proc.ExitCode;
            }

            if (exitCode != 0)
            {
                Debug.LogError($"[Fractured Chorus] {label} failed: {stderr}\n{stdout}");
                return;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Fractured Chorus] {label} OK.\n{stdout}");
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            return go.AddComponent<T>();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif

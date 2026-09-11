#if UNITY_EDITOR
using System.IO;
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Hub;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FracturedChorus.Editor
{
    public static class BondsSceneSetupEditor
    {
        private const string ScenePath = "Assets/FracturedChorus/Scenes/BondsLayoutSandbox.unity";
        private const string BackgroundPath = "Assets/FracturedChorus/Art/UI/StatusMenu/statusmenu_hima_city_bg_v1.jpg";
        private const string MockGuidePath = "Assets/FracturedChorus/Art/UI/Bonds/_ref/_ref_bonds_menu_v1.jpg";
        private const string BondsRoot = "Assets/FracturedChorus/Art/UI/Bonds/";
        private const float MockGuideAlpha = 0.4f;

        [MenuItem("Fractured Chorus/Bonds/Create Layout Sandbox Scene")]
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

        [MenuItem("Fractured Chorus/Bonds/Heal Layout Sandbox Hierarchy")]
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
            Debug.Log("[Fractured Chorus] Healed BondsLayoutSandbox hierarchy.");
        }

        [MenuItem("Fractured Chorus/Bonds/Attach Missing Layout Objects")]
        public static void AttachMissing()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                EditorUtility.DisplayDialog(
                    "Attach Missing Bonds Objects",
                    "Open BondsLayoutSandbox in Unity, then run this menu again. This action will not open scenes automatically.",
                    "OK");
                return;
            }

            BuildHierarchy();
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[Fractured Chorus] Attached missing Bonds sandbox objects. Save the scene.");
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
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = EnsureComponent<CanvasScaler>(canvasGo);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            EnsureComponent<GraphicRaycaster>(canvasGo);
            EnsureComponent<BondsMenuUI>(canvasGo);
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
                    LoadSprite("Assets/FracturedChorus/Art/UI/TownMap/townmap_icon_sun.png"),
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
                    LoadSprite(BondsRoot + "Kit/ui_bonds_header_plate_v1.png"),
                    Image.Type.Sliced,
                    false,
                    Color.white,
                    false);
            }

            var icon = EnsureImage(header.transform, "Icon", out created);
            if (created)
            {
                ConfigureImage(
                    icon,
                    LoadSprite(BondsRoot + "Icons/ui_bonds_icon_people_v1.png"),
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
            EnsureNavRow(nav, "Row_SocialStats", "Social Stats", "ui_bonds_icon_social_stats_v1.png", true);
            EnsureNavRow(nav, "Row_Link", "Link", "ui_bonds_icon_link_v1.png", false);
            EnsureNavRow(nav, "Row_Conversations", "Conversations", "ui_bonds_icon_conversations_v1.png", false);
            EnsureNavRow(nav, "Row_Memories", "Memories", "ui_bonds_icon_memories_v1.png", false);
            EnsureNavRow(nav, "Row_Gallery", "Gallery", "ui_bonds_icon_gallery_v1.png", false);
        }

        private static void EnsureNavRow(Transform parent, string name, string label, string iconFile, bool interactable)
        {
            var row = EnsureButton(parent, name, out var created);
            row.interactable = interactable;
            if (created)
            {
                ConfigureImage(
                    row.GetComponent<Image>(),
                    LoadSprite(BondsRoot + "Kit/ui_bonds_nav_selected_v1.png"),
                    Image.Type.Sliced,
                    true,
                    Color.white,
                    false);
            }

            var icon = EnsureImage(row.transform, "Icon", out created);
            if (created)
            {
                ConfigureImage(
                    icon,
                    LoadSprite(BondsRoot + "Icons/" + iconFile),
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
                    LoadSprite(BondsRoot + "Kit/ui_bonds_panel_glass_v1.png"),
                    Image.Type.Sliced,
                    false,
                    Color.white,
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
            EnsureChip(roster, 5, "Mei Lin", string.Empty, locked, true);
            EnsureChip(roster, 6, "Reserved", string.Empty, locked, true);

            var chevron = EnsureImage(roster, "Chevron", out created);
            if (created)
            {
                ConfigureImage(
                    chevron,
                    LoadSprite(BondsRoot + "Icons/ui_bonds_icon_chevron_v1.png"),
                    Image.Type.Simple,
                    false,
                    Color.white,
                    true);
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
        }

        private static void EnsureChip(Transform parent, int index, string displayName, string role, Sprite faceSprite, bool locked)
        {
            var chip = EnsurePanel(parent, $"Chip_{index}", out _);

            var frame = EnsureImage(chip, "Frame", out var created);
            if (created)
            {
                var framePath = locked
                    ? BondsRoot + "Kit/ui_bonds_chip_locked_v1.png"
                    : BondsRoot + "Kit/ui_bonds_chip_frame_normal_v1.png";
                ConfigureImage(frame, LoadSprite(framePath), Image.Type.Sliced, false, Color.white, false);
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
                    LoadSprite(BondsRoot + "Icons/ui_bonds_icon_lock_v1.png"),
                    Image.Type.Simple,
                    false,
                    locked ? Color.white : new Color(1f, 1f, 1f, 0f),
                    true);
            }

            EnsureText(chip, "Name", displayName, UiFontRole.Body);
            EnsureText(chip, "Role", role, UiFontRole.Body);
        }

        private static void EnsureDetailCard(Transform canvas)
        {
            var card = EnsureImage(canvas, "DetailCard", out var created);
            if (created)
            {
                ConfigureImage(
                    card,
                    LoadSprite(BondsRoot + "Kit/ui_bonds_panel_glass_v1.png"),
                    Image.Type.Sliced,
                    false,
                    Color.white,
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
                    LoadSprite(BondsRoot + "Kit/ui_bonds_bar_track_v1.png"),
                    Image.Type.Sliced,
                    false,
                    Color.white,
                    false);
            }

            var expFill = EnsureImage(expTrack.transform, "ExpFill", out created);
            if (created)
            {
                ConfigureImage(
                    expFill,
                    LoadSprite(BondsRoot + "Kit/ui_bonds_bar_fill_v1.png"),
                    Image.Type.Sliced,
                    false,
                    Color.white,
                    false);
                StretchFull(expFill.rectTransform);
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
                    LoadSprite(BondsRoot + "Kit/ui_bonds_panel_glass_v1.png"),
                    Image.Type.Sliced,
                    false,
                    Color.white,
                    false);
            }

            EnsureText(root.transform, "Title", BondPresentation.LinkEpisodesTitle, UiFontRole.Display);
            EnsureText(root.transform, "TitleJp", BondPresentation.LinkEpisodesJp, UiFontRole.Display);
            EnsureText(root.transform, "Hint", BondPresentation.EpisodeLockHint, UiFontRole.Body);

            var promoFrame = EnsureImage(root.transform, "PromoFrame", out created);
            if (created)
            {
                ConfigureImage(
                    promoFrame,
                    LoadSprite(BondsRoot + "Kit/ui_bonds_panel_glass_v1.png"),
                    Image.Type.Sliced,
                    false,
                    Color.white,
                    false);
            }

            var promoImage = EnsureImage(promoFrame.transform, "PromoImage", out created);
            if (created)
            {
                ConfigureImage(
                    promoImage,
                    LoadSprite(BondsRoot + "Decor/ui_bonds_promo_piano_v1.png"),
                    Image.Type.Simple,
                    false,
                    Color.white,
                    true);
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
            row.interactable = index == 1;
            if (created)
            {
                ConfigureImage(
                    row.GetComponent<Image>(),
                    LoadSprite(BondsRoot + "Kit/ui_bonds_episode_row_v1.png"),
                    Image.Type.Sliced,
                    true,
                    Color.white,
                    false);
            }

            var icon = EnsureImage(row.transform, "Icon", out created);
            if (created)
            {
                var spriteName = index == 1 ? "ui_bonds_icon_play_v1.png" : "ui_bonds_icon_lock_v1.png";
                ConfigureImage(icon, LoadSprite(BondsRoot + "Icons/" + spriteName), Image.Type.Simple, false, Color.white, true);
            }

            EnsureText(row.transform, "Index", index.ToString("00"), UiFontRole.Display);
            EnsureText(row.transform, "Label", title, UiFontRole.Body);
        }

        private static void EnsureFooter(Transform canvas)
        {
            var footer = EnsurePanel(canvas, "Footer", out _);
            EnsureText(footer, "ConfirmLabel", "Confirm", UiFontRole.Display);
            EnsureText(footer, "BackLabel", "Back", UiFontRole.Display);
        }

        private static void EnsureWordmark(Transform canvas)
        {
            var root = EnsurePanel(canvas, "Wordmark", out _);
            EnsureText(root, "Title", BondPresentation.Wordmark, UiFontRole.Display);
            EnsureText(root, "Sub", BondPresentation.WordmarkSub, UiFontRole.Body);
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
                LoadSprite(BondsRoot + "Icons/ui_bonds_icon_compass_v1.png"),
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

        private static void ConfigureImage(
            Image image,
            Sprite sprite,
            Image.Type type,
            bool raycastTarget,
            Color color,
            bool preserveAspect)
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

        private static void DestroyIfFound(string objectName)
        {
            var found = GameObject.Find(objectName);
            if (found != null)
            {
                Object.DestroyImmediate(found);
            }
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

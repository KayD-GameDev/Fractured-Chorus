#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Hub.CharacterBuild;
using FracturedChorus.Menu;
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
    public static class CharacterBuildSceneSetupEditor
    {
        public const string SandboxScenePath =
            "Assets/FracturedChorus/Scenes/CharacterBuildLayoutSandbox.unity";
        public const string ProductionScenePath =
            "Assets/FracturedChorus/Scenes/CharacterBuild.unity";
        private const string ScenePath = SandboxScenePath;
        private const string KitDir = "Assets/FracturedChorus/Art/UI/StatMenu/Kit/";
        private const string IconDir = "Assets/FracturedChorus/Art/UI/StatMenu/Icons/";
        private const string CrystalDir = "Assets/FracturedChorus/Art/UI/StatMenu/CrystalKit/";
        private const string DecorDir = "Assets/FracturedChorus/Art/UI/StatMenu/Decor/";
        private const string HeaderMockPath =
            "Assets/FracturedChorus/Art/UI/StatMenu/MockKit/sandbox_header.json";
        private const string LayoutSnapshotPath =
            "Assets/FracturedChorus/Art/UI/StatMenu/MockKit/sandbox_layout_snapshot.json";
        private const string MockScreenPath =
            "Assets/FracturedChorus/Art/UI/StatMenu/_ref/_ref_stats_mock_screen.png";
        private const string StatBgPath = DecorDir + "ui_stat_bg_v1.jpg";
        private const float MockGuideAlpha = 0.4f;
        private const string SkillEquipPreviewRootName = "SkillEquip_EditPreview";
        private const string SkillEquipDimName = "L00_Dim";
        private const int SkillEquipPreviewSortingOrder = 50;
        private const string StatDetailsPreviewRootName = "StatDetails_EditPreview";
        private const int StatDetailsPreviewSortingOrder = 45;
        private const string ChipAvatarDir =
            "Assets/FracturedChorus/Art/UI/Combat/Timeline/LeftRail/Avatars/";
        private const string RenVnBustPath =
            "Assets/FracturedChorus/Art/Characters/Ren/VnBust/ren_bust_neutral_v1.png";
        private const string CharlotteVnBustPath =
            "Assets/FracturedChorus/Art/Characters/Charlotte/VnBust/charlotte_bust_neutral_v1.png";
        private const string CodaVnBustPath =
            "Assets/FracturedChorus/Art/Characters/Coda/VnBust/coda_bust_neutral_v1.png";
        private const string RenPortraitPath = RenVnBustPath;
        private const string CharlottePortraitPath = CharlotteVnBustPath;
        private const string CodaPortraitPath = CodaVnBustPath;
        private const string RenChipPath = "Assets/FracturedChorus/Art/UI/Combat/Characters/Ren_Clear_charCard.png";
        private const string CharlotteChipPath =
            "Assets/FracturedChorus/Art/UI/Combat/Characters/Charlott_Clear_CharCard.png";
        private const string CodaChipPath = "Assets/FracturedChorus/Art/UI/Combat/Characters/Coda_Clear_Card.png";

        [MenuItem("Fractured Chorus/Create CharacterBuild Layout Sandbox Scene")]
        public static void CreateScene()
        {
            EnsureFolder("Assets/FracturedChorus/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildHierarchy();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Fractured Chorus] Saved {ScenePath} — layout sandbox, not in Build Settings.");
        }

        [MenuItem("Fractured Chorus/Clear CharacterBuild Layout Sandbox")]
        public static void ClearSandbox()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                Object.DestroyImmediate(roots[i]);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Fractured Chorus] Cleared CharacterBuild layout sandbox.");
        }

        [MenuItem("Fractured Chorus/Seed CharacterBuild Sandbox Header")]
        public static void SeedSandboxHeader()
        {
            AssetDatabase.Refresh();
            var mock = LoadHeaderMock();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                Object.DestroyImmediate(roots[i]);
            }

            EnsureCamera(ToColor(mock.background));
            EnsureEventSystem();
            var canvasGo = CreateSandboxCanvas(RequireVec2(mock.referenceResolution, "referenceResolution"));
            var bg = CreateImage(canvasGo.transform, "Background", ToColor(mock.background));
            StretchFull(bg.rectTransform);
            CreateHudCorner(canvasGo.transform, mock);
            CreateChipRow(canvasGo.transform, mock);

            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                CombatInputSetup.ApplyInputModule(eventSystem.gameObject, destroyImmediate: true);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Fractured Chorus] Seeded sandbox header from MockKit/sandbox_header.json.");
        }

        [MenuItem("Fractured Chorus/Attach CrystalKit Missing To Sandbox")]
        public static void AttachCrystalKitMissing()
        {
            const string crystalDir = "Assets/FracturedChorus/Art/UI/StatMenu/CrystalKit/";
            AssetDatabase.Refresh();
            var canvas = GameObject.Find("BuildCanvas");
            if (canvas == null)
            {
                if (!System.IO.File.Exists(ScenePath))
                {
                    Debug.LogError("[Fractured Chorus] BuildCanvas missing. Seed header first.");
                    return;
                }

                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                canvas = GameObject.Find("BuildCanvas");
            }

            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] BuildCanvas missing. Seed header first.");
                return;
            }

            var root = canvas.transform;
            Sprite Sp(string file) => LoadSprite(crystalDir + file);

            EnsureNamedImage(root, "PortraitPanel", Sp("ui_stat_panel_portrait_v2.png"), new Vector2(0.15f, 0.10f), new Vector2(0.40f, 0.78f), Image.Type.Sliced, false);
            EnsureNamedImage(root, "HeaderBar", Sp("ui_stat_header_bar_v2.png"), new Vector2(0.42f, 0.78f), new Vector2(0.90f, 0.86f), Image.Type.Sliced, false);
            EnsureNamedImage(root, "MemoryPanel", Sp("ui_stat_panel_memory_v2.png"), new Vector2(0.15f, 0.02f), new Vector2(0.34f, 0.16f), Image.Type.Sliced, false);
            EnsureNamedImage(root, "CloseBtn", Sp("ui_stat_btn_close_v2.png"), new Vector2(0.93f, 0.90f), new Vector2(0.985f, 0.98f), Image.Type.Simple, true, true);
            EnsureNamedImage(root, "NoteCircle", Sp("ui_stat_btn_note_circle_v2.png"), new Vector2(0.43f, 0.04f), new Vector2(0.50f, 0.14f), Image.Type.Simple, true, true);
            EnsureNamedImage(root, "OrbFilled", Sp("ui_stat_btn_orb_filled_v2.png"), new Vector2(0.52f, 0.04f), new Vector2(0.59f, 0.14f), Image.Type.Simple, true, true);
            EnsureNamedImage(root, "CrystalShards", Sp("ui_stat_crystal_shards_v2.png"), new Vector2(0.88f, 0.02f), new Vector2(0.98f, 0.16f), Image.Type.Simple, true);

            if (root.Find("NavColumn") == null)
            {
                CreateNavColumn(root, Sp("ui_stat_btn_nav_normal_v2.png"), Sp("ui_stat_btn_nav_selected_v2.png"));
            }

            if (root.Find("SkillsPanel") == null)
            {
                var skills = CreatePanel(root, "SkillsPanel");
                Stretch(skills, new Vector2(0.72f, 0.10f), new Vector2(0.97f, 0.76f), Vector2.zero, Vector2.zero);
                var skillSprite = Sp("ui_stat_slot_skill_v2.png");
                for (var i = 0; i < 3; i++)
                {
                    var y0 = 0.72f - i * 0.22f;
                    EnsureNamedImage(skills, $"SkillSlot_{i}", skillSprite, new Vector2(0.06f, y0), new Vector2(0.94f, y0 + 0.18f), Image.Type.Simple, false, true);
                }
            }

            if (root.Find("StatsPanel") == null)
            {
                var stats = CreatePanel(root, "StatsPanel");
                Stretch(stats, new Vector2(0.42f, 0.18f), new Vector2(0.70f, 0.76f), Vector2.zero, Vector2.zero);
                SeedStatAttrRows(stats);
            }
            else if (root.Find("StatsPanel/StatRow_Strength") == null)
            {
                SeedStatAttrRows(root.Find("StatsPanel"));
            }

            EditorSceneManager.MarkSceneDirty(canvas.scene);
            Debug.Log("[Fractured Chorus] Attached missing CrystalKit only. Hierarchy + RectTransforms preserved (scene is layout SoT). Ctrl+S to save.");
        }

        [MenuItem("Fractured Chorus/Show StatMenu Mock Guide")]
        public static void ShowStatMenuMockGuide()
        {
            var canvas = GameObject.Find("BuildCanvas");
            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] Open CharacterBuild or CharacterBuildLayoutSandbox (BuildCanvas missing).");
                return;
            }

            EnsureMockGuide(canvas.transform);
            EditorSceneManager.MarkSceneDirty(canvas.scene);
            Debug.Log("[Fractured Chorus] Mock on Background (alpha 0.4) + MockGuide overlay. Scene view needs Screen Space Camera.");
        }

        [MenuItem("Fractured Chorus/Create CharacterBuild Skill Equip Edit Preview")]
        public static void CreateSkillEquipEditPreviewMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Skill Equip Edit Preview",
                    "Exit Play Mode trước khi tạo Edit Preview.",
                    "OK");
                return;
            }

            var canvas = GameObject.Find("BuildCanvas");
            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] Open CharacterBuild (BuildCanvas missing).");
                return;
            }

            EnsureSkillEquipEditPreview(canvas.GetComponent<Canvas>());
            EditorSceneManager.MarkSceneDirty(canvas.scene);
            var preview = FindSkillEquipPreviewRoot(canvas.scene);
            if (preview != null)
            {
                Selection.activeGameObject = preview;
            }

            Debug.Log(
                "[Fractured Chorus] SkillEquip_EditPreview is a scene-root canvas. Toggle L00_Dim / SkillEquipOverlay, or disable BuildCanvas. Overlay RectTransform unchanged.");
        }

        [MenuItem("Fractured Chorus/Create CharacterBuild Stat Details Edit Preview")]
        public static void CreateStatDetailsEditPreviewMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Stat Details Edit Preview",
                    "Exit Play Mode trước khi tạo Edit Preview.",
                    "OK");
                return;
            }

            var canvas = GameObject.Find("BuildCanvas");
            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] Open CharacterBuild (BuildCanvas missing).");
                return;
            }

            EnsureStatDetailsOverlay(canvas.transform, out var overlay, out var body);
            WireStatDetailsMenuRefs(canvas.GetComponent<CharacterBuildMenuUI>(), canvas.transform, overlay, body);
            EditorSceneManager.MarkSceneDirty(canvas.scene);
            var preview = FindStatDetailsPreviewRoot(canvas.scene);
            if (preview != null)
            {
                preview.SetActive(true);
                Selection.activeGameObject = overlay != null ? overlay : preview;
            }

            Debug.Log(
                "[Fractured Chorus] StatDetails_EditPreview is a scene-root canvas. Chỉnh StatDetailsOverlay trên Hierarchy, Ctrl+S. Runtime không ghi Rect.");
        }

        [MenuItem("Fractured Chorus/Ensure CharacterBuild Stat Background")]
        public static void EnsureCharacterBuildStatBackground()
        {
            var canvas = GameObject.Find("BuildCanvas");
            if (canvas == null && File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                canvas = GameObject.Find("BuildCanvas");
            }

            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] BuildCanvas missing. Open CharacterBuildLayoutSandbox first.");
                return;
            }

            var sprite = LoadSprite(StatBgPath);
            if (sprite == null)
            {
                Debug.LogError($"[Fractured Chorus] Missing stat background: {StatBgPath}");
                return;
            }

            var canvasRt = canvas.GetComponent<RectTransform>();
            if (canvasRt != null && canvasRt.localScale == Vector3.zero)
            {
                canvasRt.localScale = Vector3.one;
                Debug.LogWarning("[Fractured Chorus] BuildCanvas localScale was 0 — reset to 1.");
            }

            var bgTf = canvas.transform.Find("Background");
            if (bgTf != null)
            {
                var bg = bgTf.GetComponent<Image>();
                if (bg != null)
                {
                    ApplySprite(bg, sprite, Image.Type.Simple, false);
                    bg.preserveAspect = false;
                    bg.color = Color.white;
                    bg.raycastTarget = false;
                    EditorUtility.SetDirty(bg);
                }
            }

            var mockGuide = canvas.transform.Find("MockGuide");
            if (mockGuide != null)
            {
                Object.DestroyImmediate(mockGuide.gameObject);
            }

            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.36f, 0.33f, 0.48f, 1f);
                EditorUtility.SetDirty(cam);
            }

            EditorSceneManager.MarkSceneDirty(canvas.scene);
            Debug.Log("[Fractured Chorus] Stat BG applied; MockGuide removed. RectTransforms + Hierarchy order preserved. Ctrl+S to save.");
        }

        [MenuItem("Fractured Chorus/Ensure CharacterBuild Crystal Field")]
        public static void EnsureCharacterBuildCrystalField()
        {
            var canvas = GameObject.Find("BuildCanvas");
            if (canvas == null && File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                canvas = GameObject.Find("BuildCanvas");
            }

            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] BuildCanvas missing. Open CharacterBuildLayoutSandbox first.");
                return;
            }

            var canvasRt = canvas.GetComponent<RectTransform>();
            if (canvasRt != null && canvasRt.localScale == Vector3.zero)
            {
                canvasRt.localScale = Vector3.one;
                Debug.LogWarning("[Fractured Chorus] BuildCanvas localScale was 0 — reset to 1.");
            }

            EnsureSandboxCrystalField(canvas.transform);
            EditorSceneManager.MarkSceneDirty(canvas.scene);
            Debug.Log("[Fractured Chorus] CrystalField ensured above BG, below UI. Play mode to animate. Ctrl+S to save.");
        }

        [MenuItem("Fractured Chorus/Apply Stat Attr Kit Sprites")]
        public static void ApplyStatAttrKitSprites()
        {
            var canvas = GameObject.Find("BuildCanvas");
            if (canvas == null && System.IO.File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                canvas = GameObject.Find("BuildCanvas");
            }

            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] Open CharacterBuild or CharacterBuildLayoutSandbox (BuildCanvas missing).");
                return;
            }

            if (canvas.transform.Find("StatsPanel") == null)
            {
                var stats = CreatePanel(canvas.transform, "StatsPanel");
                Stretch(stats, new Vector2(0.42f, 0.18f), new Vector2(0.70f, 0.76f), Vector2.zero, Vector2.zero);
                SeedStatAttrRows(stats);
            }
            else if (canvas.transform.Find("StatsPanel/StatRow_Strength") == null)
            {
                SeedStatAttrRows(canvas.transform.Find("StatsPanel"));
            }

            var trackSprite = LoadSprite(CrystalDir + "ui_stat_bar_track_v3.png")
                              ?? LoadSprite(CrystalDir + "ui_stat_attr_slider_track_v2.png");
            var fillSprite = LoadSprite(CrystalDir + "ui_stat_bar_fill_v3.png")
                             ?? LoadSprite(CrystalDir + "ui_stat_attr_slider_fill_v2.png");
            var handleSprite = LoadSprite(CrystalDir + "ui_stat_attr_slider_handle_v2.png");
            var rows = new[]
            {
                ("StatRow_Strength", IconDir + "ui_stat_icon_strength_v2.png"),
                ("StatRow_Magic", IconDir + "ui_stat_icon_magic_v2.png"),
                ("StatRow_Endurance", IconDir + "ui_stat_icon_endurance_v2.png"),
                ("StatRow_HeartBeat", IconDir + "ui_stat_icon_heartbeat_v2.png"),
                ("StatRow_Luck", IconDir + "ui_stat_icon_luck_v2.png"),
            };

            var applied = 0;
            for (var i = 0; i < rows.Length; i++)
            {
                var (rowName, iconPath) = rows[i];
                var rowTf = FindDeep(canvas.transform, rowName);
                if (rowTf == null)
                {
                    continue;
                }

                var icon = rowTf.Find("Icon")?.GetComponent<Image>();
                if (icon != null)
                {
                    ApplySprite(icon, LoadSprite(iconPath), Image.Type.Simple, false);
                    icon.preserveAspect = true;
                    icon.color = CharacterBuildStatTheme.IconImageColor;
                }

                var labelArtTf = rowTf.Find("NameLabelArt");
                if (labelArtTf != null)
                {
                    labelArtTf.gameObject.SetActive(false);
                }

                Text nameText = null;
                Text subText = null;
                Text valueText = null;
                if (CharacterBuildStatTheme.TryForRowName(rowName, out var meta))
                {
                    nameText = EnsureStatRowText(
                        rowTf,
                        "NameLabel",
                        meta.Title,
                        UiFontRole.Display,
                        18,
                        TextAnchor.UpperLeft,
                        CharacterBuildStatTheme.LabelColor,
                        labelArtTf,
                        true);
                    subText = EnsureStatRowText(
                        rowTf,
                        "SubLabel",
                        meta.Subtitle,
                        UiFontRole.Body,
                        11,
                        TextAnchor.LowerLeft,
                        CharacterBuildStatTheme.SubLabelColor,
                        labelArtTf,
                        false);
                    valueText = EnsureStatRowText(
                        rowTf,
                        "ValueLabel",
                        "10",
                        UiFontRole.Display,
                        16,
                        TextAnchor.MiddleRight,
                        CharacterBuildStatTheme.LabelColor,
                        null,
                        false,
                        true);
                }

                var track = rowTf.Find("BarTrack")?.GetComponent<Image>();
                if (track != null)
                {
                    ApplySprite(track, trackSprite, Image.Type.Sliced, false);
                    track.preserveAspect = false;
                }

                var fill = rowTf.Find("BarTrack/BarFill")?.GetComponent<Image>();
                if (fill != null)
                {
                    ApplySprite(fill, fillSprite, Image.Type.Filled, false);
                    fill.type = Image.Type.Filled;
                    fill.fillMethod = Image.FillMethod.Horizontal;
                    fill.fillOrigin = (int)Image.OriginHorizontal.Left;
                    fill.preserveAspect = false;
                    fill.raycastTarget = false;
                }

                Image handle = null;
                if (track != null)
                {
                    var handleTf = track.transform.Find("BarHandle");
                    if (handleTf != null)
                    {
                        handle = handleTf.GetComponent<Image>() ?? handleTf.gameObject.AddComponent<Image>();
                    }
                    else
                    {
                        handle = EnsureNamedImage(
                            track.transform,
                            "BarHandle",
                            handleSprite,
                            new Vector2(0.3f, 0.5f),
                            new Vector2(0.3f, 0.5f),
                            Image.Type.Simple,
                            true);
                        handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                        handle.rectTransform.sizeDelta = new Vector2(28f, 28f);
                        ApplySprite(handle, handleSprite, Image.Type.Simple, false);
                        handle.preserveAspect = true;
                        handle.color = CharacterBuildStatTheme.HandleTint;
                    }

                    if (handle != null)
                    {
                        handle.raycastTarget = false;
                    }

                    if (handleTf != null)
                    {
                        ApplySprite(handle, handleSprite, Image.Type.Simple, false);
                        handle.preserveAspect = true;
                        handle.color = CharacterBuildStatTheme.HandleTint;
                    }
                }

                var view = rowTf.GetComponent<CharacterBuildStatRowView>();
                if (view != null)
                {
                    var so = new SerializedObject(view);
                    so.FindProperty("icon").objectReferenceValue = icon;
                    so.FindProperty("nameLabel").objectReferenceValue = nameText;
                    so.FindProperty("subLabel").objectReferenceValue = subText;
                    so.FindProperty("valueLabel").objectReferenceValue = valueText;
                    so.FindProperty("barFill").objectReferenceValue = fill;
                    so.FindProperty("nameLabelArt").objectReferenceValue =
                        labelArtTf != null ? labelArtTf.GetComponent<Image>() : null;
                    so.FindProperty("barHandle").objectReferenceValue = handle;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    view.ApplyTheme();
                }

                applied++;
            }

            EditorSceneManager.MarkSceneDirty(canvas.scene);
            Debug.Log(
                $"[Fractured Chorus] Applied ExpBar slider style (track/fill v3) to {applied} stat rows. Layout preserved. Ctrl+S to save.");
        }

        [MenuItem("Fractured Chorus/Ensure CharacterBuild Portrait Chip Chibis")]
        public static void EnsureCharacterBuildPortraitChipChibis()
        {
            var canvas = GameObject.Find("BuildCanvas");
            if (canvas == null && File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                canvas = GameObject.Find("BuildCanvas");
            }

            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] BuildCanvas missing. Open CharacterBuildLayoutSandbox first.");
                return;
            }

            var canvasRt = canvas.GetComponent<RectTransform>();
            if (canvasRt != null && canvasRt.localScale == Vector3.zero)
            {
                canvasRt.localScale = Vector3.one;
                Debug.LogWarning("[Fractured Chorus] BuildCanvas localScale was 0 — reset to 1.");
            }

            var faces = new[]
            {
                ("PortraitChip_0", ChipAvatarDir + "ren_chibi_avatar_v1.png"),
                ("PortraitChip_1", ChipAvatarDir + "charlotte_chibi_avatar_v1.png"),
                ("PortraitChip_2", ChipAvatarDir + "coda_chibi_avatar_v1.png"),
            };

            var applied = 0;
            for (var i = 0; i < faces.Length; i++)
            {
                var (chipName, spritePath) = faces[i];
                var chip = FindDeep(canvas.transform, chipName);
                if (chip == null)
                {
                    continue;
                }

                var sprite = LoadSprite(spritePath);
                if (sprite == null)
                {
                    Debug.LogError($"[Fractured Chorus] Missing chip chibi sprite: {spritePath}");
                    continue;
                }

                var faceTf = chip.Find("Face");
                var frameTf = chip.Find("Frame");
                if (faceTf != null && frameTf != null && faceTf.GetSiblingIndex() < frameTf.GetSiblingIndex())
                {
                    faceTf.SetAsLastSibling();
                }

                var openFrame = LoadSprite(CrystalDir + "ui_stat_slot_portrait_v2.png")
                                ?? LoadSprite(KitDir + "ui_stat_slot_portrait_v1.png");
                var view = chip.GetComponent<CharacterBuildPortraitChipView>();
                if (view != null)
                {
                    if (openFrame != null)
                    {
                        var so = new SerializedObject(view);
                        so.FindProperty("frameNormal").objectReferenceValue = openFrame;
                        so.FindProperty("frameSelected").objectReferenceValue = openFrame;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }

                    view.BindSlot(sprite, locked: false);
                }

                if (frameTf != null && openFrame != null)
                {
                    var frameImg = frameTf.GetComponent<Image>();
                    if (frameImg != null)
                    {
                        frameImg.sprite = openFrame;
                        frameImg.color = Color.white;
                        EditorUtility.SetDirty(frameImg);
                    }
                }

                var face = faceTf != null
                    ? faceTf.GetComponent<Image>()
                    : chip.Find("Face")?.GetComponent<Image>();
                if (face == null)
                {
                    continue;
                }

                face.enabled = true;
                face.sprite = sprite;
                face.preserveAspect = true;
                face.color = Color.white;
                face.gameObject.SetActive(true);
                EditorUtility.SetDirty(face);
                applied++;
            }

            EditorSceneManager.MarkSceneDirty(canvas.scene);
            Debug.Log(
                $"[Fractured Chorus] Portrait chip chibis applied ({applied}/3). Face RectTransforms preserved. Ctrl+S to save.");
        }

        [MenuItem("Fractured Chorus/Ensure CharacterBuild Portrait Panel Busts")]
        public static void EnsureCharacterBuildPortraitPanelBusts()
        {
            var canvas = GameObject.Find("BuildCanvas");
            if (canvas == null && File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                canvas = GameObject.Find("BuildCanvas");
            }

            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] BuildCanvas missing. Open CharacterBuildLayoutSandbox first.");
                return;
            }

            var canvasRt = canvas.GetComponent<RectTransform>();
            if (canvasRt != null && canvasRt.localScale == Vector3.zero)
            {
                canvasRt.localScale = Vector3.one;
                Debug.LogWarning("[Fractured Chorus] BuildCanvas localScale was 0 — reset to 1.");
            }

            var panel = canvas.transform.Find("PortraitPanel");
            if (panel == null)
            {
                Debug.LogError("[Fractured Chorus] PortraitPanel missing.");
                return;
            }

            var template = panel.Find("Ren_Portrait");
            if (template == null)
            {
                Debug.LogError("[Fractured Chorus] Ren_Portrait missing — place Ren bust in PortraitPanel first.");
                return;
            }

            var layers = new[]
            {
                ("Charlotte_Portrait", CharlotteVnBustPath),
                ("Coda_Portrait", CodaVnBustPath),
            };

            var created = 0;
            foreach (var (name, spritePath) in layers)
            {
                var existing = panel.Find(name);
                if (existing != null)
                {
                    continue;
                }

                var sprite = LoadSprite(spritePath);
                if (sprite == null)
                {
                    Debug.LogError($"[Fractured Chorus] Missing VN bust: {spritePath}");
                    continue;
                }

                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(panel, false);
                var rt = go.GetComponent<RectTransform>();
                var templateRt = template as RectTransform ?? template.GetComponent<RectTransform>();
                if (templateRt != null)
                {
                    rt.anchorMin = templateRt.anchorMin;
                    rt.anchorMax = templateRt.anchorMax;
                    rt.pivot = templateRt.pivot;
                    rt.anchoredPosition = templateRt.anchoredPosition;
                    rt.sizeDelta = templateRt.sizeDelta;
                    rt.localRotation = templateRt.localRotation;
                    rt.localScale = templateRt.localScale;
                }
                else
                {
                    go.transform.localPosition = template.localPosition;
                    go.transform.localRotation = template.localRotation;
                    go.transform.localScale = template.localScale;
                }

                var image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                go.SetActive(false);
                created++;
                EditorUtility.SetDirty(go);
            }

            EditorSceneManager.MarkSceneDirty(canvas.scene);
            Debug.Log(
                created > 0
                    ? $"[Fractured Chorus] Added {created} portrait layer(s) under PortraitPanel (Ren unchanged). Adjust layout in scene, then Ctrl+S."
                    : "[Fractured Chorus] Charlotte/Coda portrait layers already present. Ren unchanged.");
        }

        [MenuItem("Fractured Chorus/Save CharacterBuild Sandbox Layout")]
        public static void SaveCharacterBuildSandboxLayout()
        {
            var canvas = GameObject.Find("BuildCanvas");
            if (canvas == null)
            {
                if (!File.Exists(ScenePath))
                {
                    Debug.LogError("[Fractured Chorus] CharacterBuildLayoutSandbox missing.");
                    return;
                }

                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                canvas = GameObject.Find("BuildCanvas");
            }

            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] BuildCanvas missing.");
                return;
            }

            var canvasRt = canvas.GetComponent<RectTransform>();
            if (canvasRt != null && canvasRt.localScale == Vector3.zero)
            {
                canvasRt.localScale = Vector3.one;
                Debug.LogWarning("[Fractured Chorus] BuildCanvas localScale was 0 — reset to 1 before save.");
            }

            var scene = canvas.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError("[Fractured Chorus] Failed to save scene.");
                return;
            }

            ExportLayoutSnapshot(canvas.transform);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "[Fractured Chorus] Sandbox layout saved to scene (SoT). Snapshot: MockKit/sandbox_layout_snapshot.json — reference only, not applied by code.");
        }

        [MenuItem("Fractured Chorus/Sync CharacterBuild Production From Sandbox")]
        public static void SyncCharacterBuildProductionFromSandboxMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Sync CharacterBuild Production",
                    "Replace BuildCanvas in CharacterBuild.unity with the sandbox layout and wire runtime bindings.\n\nLayout comes from the sandbox scene file on disk — save sandbox (Ctrl+S) first if Unity has unsaved edits.",
                    "Sync",
                    "Cancel"))
            {
                return;
            }

            SyncCharacterBuildProductionFromSandbox();
        }

        public static void BatchSyncCharacterBuildProductionFromSandbox()
        {
            SyncCharacterBuildProductionFromSandbox();
            EditorApplication.Exit(0);
        }

        private static void SyncCharacterBuildProductionFromSandbox()
        {
            if (!File.Exists(SandboxScenePath))
            {
                Debug.LogError("[Fractured Chorus] Sandbox scene missing.");
                return;
            }

            if (!File.Exists(ProductionScenePath))
            {
                Debug.LogError("[Fractured Chorus] CharacterBuild.unity missing.");
                return;
            }

            var prodScene = EditorSceneManager.OpenScene(ProductionScenePath, OpenSceneMode.Single);
            var mainCam = Object.FindAnyObjectByType<Camera>();

            var oldCanvas = GameObject.Find("BuildCanvas");
            if (oldCanvas != null)
            {
                Object.DestroyImmediate(oldCanvas);
            }

            var sandboxScene = EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Additive);
            GameObject sandboxCanvasGo = null;
            foreach (var root in sandboxScene.GetRootGameObjects())
            {
                if (root.name == "BuildCanvas")
                {
                    sandboxCanvasGo = root;
                    break;
                }
            }

            if (sandboxCanvasGo == null)
            {
                EditorSceneManager.CloseScene(sandboxScene, true);
                Debug.LogError("[Fractured Chorus] BuildCanvas missing in sandbox.");
                return;
            }

            var canvasGo = Object.Instantiate(sandboxCanvasGo);
            canvasGo.name = "BuildCanvas";
            EditorSceneManager.MoveGameObjectToScene(canvasGo, prodScene);
            EditorSceneManager.CloseScene(sandboxScene, true);

            var canvasRt = canvasGo.GetComponent<RectTransform>();
            if (canvasRt != null && canvasRt.localScale == Vector3.zero)
            {
                canvasRt.localScale = Vector3.one;
            }

            var canvas = canvasGo.GetComponent<Canvas>();
            if (canvas != null && mainCam != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = mainCam;
                canvas.planeDistance = 100f;
            }

            var menu = canvasGo.GetComponent<CharacterBuildMenuUI>() ?? canvasGo.AddComponent<CharacterBuildMenuUI>();
            WireProductionCharacterBuild(canvasGo.transform, menu);

            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                CombatInputSetup.ApplyInputModule(eventSystem.gameObject, destroyImmediate: true);
            }

            EditorSceneManager.MarkSceneDirty(prodScene);
            EditorSceneManager.SaveScene(prodScene);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "[Fractured Chorus] CharacterBuild.unity synced from sandbox layout + runtime wired. Play scene to verify.");
        }

        private static void WireProductionCharacterBuild(Transform canvas, CharacterBuildMenuUI menu)
        {
            WirePortraitLayers(canvas);
            EnsureStatAllocControls(canvas);
            var statsPanel = canvas.Find("StatsPanel");
            if (statsPanel != null)
            {
                foreach (var rowName in new[] { "StatRow_Strength", "StatRow_Magic", "StatRow_Endurance", "StatRow_HeartBeat" })
                {
                    var rowTf = statsPanel.Find(rowName);
                    if (rowTf != null)
                    {
                        LayoutStatAllocRow(rowTf);
                    }
                }
            }

            EnsureSkillEquipOverlay(canvas, out var overlay, out var equipTitle, out var equipPool, out var equipClose);
            EnsureStatDetailsOverlay(canvas, out var detailsOverlay, out var detailsBody);
            EnsureCloseButton(canvas);

            var hud = canvas.Find("HudCorner");
            var stats = statsPanel;
            var skills = canvas.Find("SkillsPanel");
            var chipRow = canvas.Find("PortraitChipRow");
            var battleStyle = canvas.Find("Battle_Style");

            var skillRows = new CharacterBuildSkillRowView[3];
            for (var i = 0; i < skillRows.Length; i++)
            {
                var slot = skills != null ? skills.Find($"SkillSlot_{i + 1}") : null;
                skillRows[i] = slot != null ? EnsureSkillSlotView(slot, true) : null;
            }

            if (skills != null)
            {
                for (var i = 4; i <= 10; i++)
                {
                    var slot = skills.Find($"SkillSlot_{i}");
                    if (slot != null)
                    {
                        EnsureSkillSlotOrbLayout(slot, false);
                    }
                }
            }

            var chips = new CharacterBuildPortraitChipView[3];
            for (var i = 0; i < chips.Length; i++)
            {
                var chip = chipRow != null ? chipRow.Find($"PortraitChip_{i}") : null;
                chips[i] = chip != null ? chip.GetComponent<CharacterBuildPortraitChipView>() : null;
            }

            var chipSprites = new[]
            {
                LoadSprite(ChipAvatarDir + "ren_chibi_avatar_v1.png"),
                LoadSprite(ChipAvatarDir + "charlotte_chibi_avatar_v1.png"),
                LoadSprite(ChipAvatarDir + "coda_chibi_avatar_v1.png"),
            };

            var so = new SerializedObject(menu);
            so.FindProperty("indexLabel").objectReferenceValue = FindText(hud, "IndexLabel");
            so.FindProperty("nameLabel").objectReferenceValue = FindText(hud, "PortraitNameLabel");
            so.FindProperty("elementLabel").objectReferenceValue = null;
            so.FindProperty("battleStyleNameLabel").objectReferenceValue =
                battleStyle != null ? FindText(battleStyle, "BalanceLabel") : null;
            so.FindProperty("battleStyleDescLine1").objectReferenceValue =
                battleStyle != null ? FindText(battleStyle, "BalanceDescLine1") : null;
            so.FindProperty("battleStyleDescLine2").objectReferenceValue =
                battleStyle != null ? FindText(battleStyle, "BalanceDescLine2") : null;
            so.FindProperty("levelLabel").objectReferenceValue = FindText(hud, "LevelLabel");
            so.FindProperty("nextExpLabel").objectReferenceValue = FindText(hud, "ExpLabel");

            so.FindProperty("elementIcons").arraySize = 0;
            so.FindProperty("elementHighlightRings").arraySize = 0;

            var chipsProp = so.FindProperty("portraitChips");
            chipsProp.arraySize = chips.Length;
            for (var i = 0; i < chips.Length; i++)
            {
                chipsProp.GetArrayElementAtIndex(i).objectReferenceValue = chips[i];
            }

            so.FindProperty("prevButton").objectReferenceValue = null;
            so.FindProperty("nextButton").objectReferenceValue = null;

            var rowsProp = so.FindProperty("skillRows");
            rowsProp.arraySize = skillRows.Length;
            for (var i = 0; i < skillRows.Length; i++)
            {
                rowsProp.GetArrayElementAtIndex(i).objectReferenceValue = skillRows[i];
            }

            so.FindProperty("strengthRow").objectReferenceValue =
                stats != null ? stats.Find("StatRow_Strength")?.GetComponent<CharacterBuildStatRowView>() : null;
            so.FindProperty("magicRow").objectReferenceValue =
                stats != null ? stats.Find("StatRow_Magic")?.GetComponent<CharacterBuildStatRowView>() : null;
            so.FindProperty("enduranceRow").objectReferenceValue =
                stats != null ? stats.Find("StatRow_Endurance")?.GetComponent<CharacterBuildStatRowView>() : null;
            so.FindProperty("heartBeatRow").objectReferenceValue =
                stats != null ? stats.Find("StatRow_HeartBeat")?.GetComponent<CharacterBuildStatRowView>() : null;
            so.FindProperty("luckRow").objectReferenceValue =
                stats != null ? stats.Find("StatRow_Luck")?.GetComponent<CharacterBuildStatRowView>() : null;

            var skillPointPanel = skills != null ? skills.Find("SkillPointPanel") : null;
            so.FindProperty("remainingPointsLabel").objectReferenceValue =
                skillPointPanel != null ? FindText(skillPointPanel, "SkillPointValue") : null;

            var portraitPanel = canvas.Find("PortraitPanel");
            so.FindProperty("portraitLayers").objectReferenceValue =
                portraitPanel != null ? portraitPanel.GetComponent<CharacterBuildPortraitLayersView>() : null;
            so.FindProperty("portraitImage").objectReferenceValue = null;

            var portraitsProp = so.FindProperty("menuPortraits");
            portraitsProp.arraySize = 0;

            var facesProp = so.FindProperty("chipFaces");
            facesProp.arraySize = chipSprites.Length;
            for (var i = 0; i < chipSprites.Length; i++)
            {
                facesProp.GetArrayElementAtIndex(i).objectReferenceValue = chipSprites[i];
            }

            var closeTf = canvas.Find("CloseBtn");
            so.FindProperty("backButton").objectReferenceValue =
                closeTf != null ? closeTf.GetComponent<Button>() : null;
            so.FindProperty("viewSkillsButton").objectReferenceValue = null;
            so.FindProperty("skillEquipOverlay").objectReferenceValue = overlay;
            so.FindProperty("skillEquipDimmer").objectReferenceValue =
                overlay != null && overlay.transform.parent != null
                    ? overlay.transform.parent.Find("L00_Dim")?.gameObject
                    : null;
            so.FindProperty("skillEquipTitleLabel").objectReferenceValue = equipTitle;

            var slotViewsProp = so.FindProperty("equipSlotViews");
            if (slotViewsProp != null)
            {
                slotViewsProp.arraySize = 0;
            }

            var poolViewsProp = so.FindProperty("equipPoolViews");
            poolViewsProp.arraySize = equipPool.Length;
            for (var i = 0; i < equipPool.Length; i++)
            {
                poolViewsProp.GetArrayElementAtIndex(i).objectReferenceValue = equipPool[i];
            }

            so.FindProperty("skillEquipCloseButton").objectReferenceValue = equipClose;
            so.FindProperty("skillSlotUnlocked").objectReferenceValue =
                LoadSprite(CrystalDir + "ui_stat_slot_skill_v2.png");
            so.FindProperty("skillSlotLocked").objectReferenceValue =
                LoadSprite(CrystalDir + "ui_stat_slot_skill_locked_v2.png");
            WireStatDetailsMenuRefs(menu, canvas, detailsOverlay, detailsBody);
            so.FindProperty("seedUnspentWhenEmpty").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(menu);
        }

        private static void WirePortraitLayers(Transform canvas)
        {
            var panel = canvas.Find("PortraitPanel");
            if (panel == null)
            {
                return;
            }

            var view = panel.GetComponent<CharacterBuildPortraitLayersView>()
                       ?? panel.gameObject.AddComponent<CharacterBuildPortraitLayersView>();
            var layerNames = new[] { "Ren_Portrait", "Charlotte_Portrait", "Coda_Portrait" };
            var layers = new GameObject[layerNames.Length];
            for (var i = 0; i < layerNames.Length; i++)
            {
                var child = panel.Find(layerNames[i]);
                if (child == null)
                {
                    Debug.LogWarning($"[Fractured Chorus] Missing portrait layer: {layerNames[i]}");
                    return;
                }

                layers[i] = child.gameObject;
            }

            var so = new SerializedObject(view);
            var layersProp = so.FindProperty("layers");
            layersProp.arraySize = layers.Length;
            for (var i = 0; i < layers.Length; i++)
            {
                layersProp.GetArrayElementAtIndex(i).objectReferenceValue = layers[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
        }

        private static void EnsureStatAllocControls(Transform canvas)
        {
            var stats = canvas.Find("StatsPanel");
            if (stats == null)
            {
                return;
            }

            var rowNames = new[]
            {
                ("StatRow_Strength", true),
                ("StatRow_Magic", true),
                ("StatRow_Endurance", true),
                ("StatRow_HeartBeat", true),
                ("StatRow_Luck", false),
            };

            var plusSprite = LoadSprite(IconDir + "ui_stat_icon_plus_v1.png");
            var minusSprite = LoadSprite(IconDir + "ui_stat_icon_minus_v1.png");

            foreach (var (rowName, allocatable) in rowNames)
            {
                var rowTf = stats.Find(rowName);
                var row = rowTf != null ? rowTf.GetComponent<CharacterBuildStatRowView>() : null;
                if (row == null || !allocatable)
                {
                    continue;
                }

                var rowSo = new SerializedObject(row);
                if (rowSo.FindProperty("plusButton").objectReferenceValue != null)
                {
                    LayoutStatAllocRow(rowTf);
                    continue;
                }

                var alloc = CreatePanel(rowTf, "AllocControls");
                Stretch(alloc, new Vector2(0.82f, 0.06f), new Vector2(0.99f, 0.94f), Vector2.zero, Vector2.zero);
                alloc.transform.SetAsLastSibling();

                var minus = CreateIconButton(alloc, "MinusBtn", "-", minusSprite);
                Stretch(minus.GetComponent<RectTransform>(), new Vector2(0f, 0.08f), new Vector2(0.30f, 0.92f), Vector2.zero, Vector2.zero);

                var spent = CreateText(alloc, "SpentLabel", "0", 14, TextAnchor.MiddleCenter, FontStyle.Bold);
                Stretch(spent.rectTransform, new Vector2(0.32f, 0.08f), new Vector2(0.68f, 0.92f), Vector2.zero, Vector2.zero);

                var plus = CreateIconButton(alloc, "PlusBtn", "+", plusSprite);
                Stretch(plus.GetComponent<RectTransform>(), new Vector2(0.70f, 0.08f), new Vector2(1f, 0.92f), Vector2.zero, Vector2.zero);

                rowSo.FindProperty("minusButton").objectReferenceValue = minus;
                rowSo.FindProperty("spentLabel").objectReferenceValue = spent;
                rowSo.FindProperty("plusButton").objectReferenceValue = plus;
                rowSo.FindProperty("allocControlsRoot").objectReferenceValue = alloc.gameObject;
                rowSo.ApplyModifiedPropertiesWithoutUndo();
                row.ApplyTheme();
                LayoutStatAllocRow(rowTf);
            }
        }

        [MenuItem("Fractured Chorus/Ensure CharacterBuild Stat Alloc Visible")]
        public static void EnsureCharacterBuildStatAllocVisibleMenu()
        {
            var canvas = ResolveBuildCanvas();
            if (canvas == null)
            {
                return;
            }

            EnsureStatAllocControls(canvas);
            var stats = canvas.Find("StatsPanel");
            if (stats == null)
            {
                return;
            }

            var rowNames = new[] { "StatRow_Strength", "StatRow_Magic", "StatRow_Endurance", "StatRow_HeartBeat" };
            foreach (var rowName in rowNames)
            {
                var rowTf = stats.Find(rowName);
                if (rowTf != null)
                {
                    LayoutStatAllocRow(rowTf);
                }
            }

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Debug.Log("[Fractured Chorus] Stat +/- controls visible on alloc rows. Ctrl+S to save.");
        }

        [MenuItem("Fractured Chorus/Ensure CharacterBuild Skill Orb Icons")]
        public static void EnsureCharacterBuildSkillOrbIconsMenu()
        {
            var canvas = ResolveBuildCanvas();
            if (canvas == null)
            {
                return;
            }

            var skills = canvas.Find("SkillsPanel");
            if (skills == null)
            {
                Debug.LogError("[Fractured Chorus] SkillsPanel missing.");
                return;
            }

            var applied = 0;
            for (var i = 1; i <= 10; i++)
            {
                var slot = skills.Find($"SkillSlot_{i}");
                if (slot == null)
                {
                    continue;
                }

                EnsureSkillSlotOrbLayout(slot, combatSlot: i <= 3);
                applied++;
            }

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Debug.Log($"[Fractured Chorus] Skill orb/icon layout applied to {applied} slots. Ctrl+S to save.");
        }

        private static Transform ResolveBuildCanvas()
        {
            var canvas = GameObject.Find("BuildCanvas")?.transform;
            if (canvas != null)
            {
                return canvas;
            }

            if (File.Exists(ProductionScenePath))
            {
                EditorSceneManager.OpenScene(ProductionScenePath, OpenSceneMode.Single);
            }
            else if (File.Exists(SandboxScenePath))
            {
                EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            }

            return GameObject.Find("BuildCanvas")?.transform;
        }

        private static void LayoutStatAllocRow(Transform rowTf)
        {
            var allocTf = rowTf.Find("AllocControls");
            if (allocTf != null)
            {
                allocTf.gameObject.SetActive(true);
            }
        }

        private static void EnsureSkillSlotOrbLayout(Transform slot, bool combatSlot)
        {
            var noteTf = slot.Find("NoteCircle");
            if (noteTf == null)
            {
                return;
            }

            var noteImg = noteTf.GetComponent<Image>();
            if (noteImg != null)
            {
                if (combatSlot)
                {
                    ApplySprite(
                        noteImg,
                        LoadSprite(CrystalDir + "ui_stat_btn_note_circle_v2.png"),
                        Image.Type.Simple,
                        false);
                    noteImg.enabled = true;
                }
                else
                {
                    noteImg.enabled = false;
                }

                noteImg.preserveAspect = true;
                noteImg.raycastTarget = false;
            }

            var orbSprite = LoadSprite(CrystalDir + "ui_stat_btn_orb_filled_v2.png");
            var iconTf = noteTf.Find("SkillIcon");
            Image skillIcon;
            if (iconTf == null)
            {
                skillIcon = CreateImage(noteTf, "SkillIcon", Color.white);
                Stretch(skillIcon.rectTransform, new Vector2(0.14f, 0.14f), new Vector2(0.86f, 0.86f), Vector2.zero, Vector2.zero);
                skillIcon.preserveAspect = true;
                skillIcon.raycastTarget = false;
                skillIcon.enabled = false;
            }
            else
            {
                skillIcon = iconTf.GetComponent<Image>();
            }

            var frameTf = noteTf.Find("SkillOrbFrame");
            Image frameImg;
            if (frameTf == null)
            {
                frameImg = CreateImage(noteTf, "SkillOrbFrame", Color.white);
                Stretch(frameImg.rectTransform, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero);
                ApplySprite(frameImg, orbSprite, Image.Type.Simple, false);
                frameImg.preserveAspect = true;
                frameImg.raycastTarget = false;
            }
            else
            {
                frameImg = frameTf.GetComponent<Image>();
                ApplySprite(frameImg, orbSprite, Image.Type.Simple, false);
            }

            if (skillIcon != null)
            {
                skillIcon.transform.SetSiblingIndex(1);
            }

            if (frameImg != null)
            {
                frameImg.enabled = combatSlot;
                if (combatSlot)
                {
                    frameImg.transform.SetAsLastSibling();
                }
                else
                {
                    frameImg.gameObject.SetActive(false);
                }
            }

            if (!combatSlot)
            {
                if (skillIcon != null)
                {
                    skillIcon.gameObject.SetActive(false);
                }

                var lockSprite = LoadSprite(CrystalDir + "ui_stat_skill_lock_circle_v1.png");
                var lockTf = noteTf.Find("LockIcon");
                Image lockImg;
                if (lockTf == null)
                {
                    lockImg = CreateImage(noteTf, "LockIcon", Color.white);
                    Stretch(lockImg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                }
                else
                {
                    lockImg = lockTf.GetComponent<Image>();
                }

                if (lockImg != null)
                {
                    ApplySprite(lockImg, lockSprite, Image.Type.Simple, false);
                    lockImg.preserveAspect = true;
                    lockImg.raycastTarget = false;
                    lockImg.enabled = true;
                    lockImg.gameObject.SetActive(true);
                    lockImg.transform.SetAsLastSibling();
                }
            }

            var view = slot.GetComponent<CharacterBuildSkillRowView>() ?? slot.gameObject.AddComponent<CharacterBuildSkillRowView>();
            var button = slot.GetComponent<Button>();
            if (button == null)
            {
                button = slot.gameObject.AddComponent<Button>();
                var slotBg = slot.GetComponent<Image>();
                if (slotBg != null)
                {
                    button.targetGraphic = slotBg;
                    slotBg.raycastTarget = true;
                }
            }

            var outline = slot.GetComponent<Outline>();
            if (outline == null)
            {
                outline = slot.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.84f, 0.2f, 1f);
                outline.effectDistance = new Vector2(3f, -3f);
                outline.useGraphicAlpha = true;
            }

            outline.enabled = combatSlot;

            Text nameText;
            var nameTf = slot.Find("SkillName");
            if (nameTf == null)
            {
                nameText = CreateText(slot, "SkillName", "—", 26, TextAnchor.LowerCenter, FontStyle.Normal);
                nameText.color = CharacterBuildStatTheme.LabelColor;
                Stretch(nameText.rectTransform, new Vector2(0.04f, 0f), new Vector2(0.96f, 0.32f), Vector2.zero, Vector2.zero);
                nameText.raycastTarget = false;
            }
            else
            {
                nameText = nameTf.GetComponent<Text>();
            }

            var so = new SerializedObject(view);
            so.FindProperty("noteSlot").objectReferenceValue = noteImg;
            so.FindProperty("iconFrame").objectReferenceValue = frameImg;
            so.FindProperty("icon").objectReferenceValue = skillIcon;
            so.FindProperty("nameLabel").objectReferenceValue = nameText;
            so.FindProperty("goldFrame").objectReferenceValue = null;
            so.FindProperty("goldOutline").objectReferenceValue = outline;
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("rowBackground").objectReferenceValue = slot.GetComponent<Image>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static CharacterBuildSkillRowView EnsureSkillSlotView(Transform slot, bool combatSlot)
        {
            EnsureSkillSlotOrbLayout(slot, combatSlot);
            return slot.GetComponent<CharacterBuildSkillRowView>();
        }

        private static void EnsureSkillEquipOverlay(
            Transform canvas,
            out GameObject overlayGo,
            out Text title,
            out CharacterBuildEquipSlotView[] pool,
            out Button closeBtn)
        {
            overlayGo = FindSkillEquipOverlay(canvas);
            if (overlayGo == null)
            {
                var previewRoot = EnsureSkillEquipEditPreview(canvas.GetComponent<Canvas>());
                var overlayParent = previewRoot != null ? previewRoot.transform : canvas;
                var panelSprite = LoadSprite(CrystalDir + "ui_stat_panel_memory_v2.png")
                                  ?? LoadSprite(KitDir + "ui_stat_panel_v1.png");
                var navSprite = LoadSprite(KitDir + "ui_stat_btn_nav_v1.png");
                var overlay = CreateGlassPanel(overlayParent, "SkillEquipOverlay", panelSprite);
                ApplySprite(overlay, panelSprite, Image.Type.Simple, true);
                Stretch(overlay.rectTransform, new Vector2(0.28f, 0.22f), new Vector2(0.72f, 1f), Vector2.zero, Vector2.zero);
                overlay.raycastTarget = true;
                overlayGo = overlay.gameObject;
                overlayGo.SetActive(false);

                title = CreateText(overlay.transform, "Title", "Skill Equip", 26, TextAnchor.MiddleCenter, FontStyle.Normal);
                title.color = new Color(0.227451f, 0.258824f, 0.4f, 1f);
                UiFontCatalog.Apply(title, UiFontRole.Display, 26);
                Stretch(title.rectTransform, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero);

                var slotSprite = LoadSprite(CrystalDir + "ui_stat_slot_skill_v2.png")
                                 ?? LoadSprite(KitDir + "ui_stat_row_v1.png");
                pool = new CharacterBuildEquipSlotView[10];
                for (var i = 0; i < pool.Length; i++)
                {
                    EquipPoolStackCell(i, out var amin, out var amax);
                    pool[i] = CreateEquipSlotCell(overlay.transform, $"EquipPool_{i}", amin, amax, slotSprite);
                }

                closeBtn = CreatePromptButton(
                    overlay.transform,
                    "CloseButton",
                    "Close",
                    new Vector2(0.35f, 0.04f),
                    new Vector2(0.65f, 0.12f),
                    navSprite);
                return;
            }

            DestroyEquipSlotChildren(overlayGo.transform);
            title = overlayGo.transform.Find("Title")?.GetComponent<Text>();
            closeBtn = overlayGo.transform.Find("CloseButton")?.GetComponent<Button>();
            pool = new CharacterBuildEquipSlotView[10];
            var existingSlotSprite = LoadSprite(CrystalDir + "ui_stat_slot_skill_v2.png")
                             ?? LoadSprite(KitDir + "ui_stat_row_v1.png");
            for (var i = 0; i < pool.Length; i++)
            {
                pool[i] = overlayGo.transform.Find($"EquipPool_{i}")?.GetComponent<CharacterBuildEquipSlotView>();
                if (pool[i] != null)
                {
                    EnsureEquipPoolCellStyle(pool[i], existingSlotSprite);
                    continue;
                }

                EquipPoolStackCell(i, out var amin, out var amax);
                pool[i] = CreateEquipSlotCell(overlayGo.transform, $"EquipPool_{i}", amin, amax, existingSlotSprite);
            }

            if (title != null)
            {
                title.color = new Color(0.227451f, 0.258824f, 0.4f, 1f);
            }

            var overlayImg = overlayGo.GetComponent<Image>();
            if (overlayImg != null)
            {
                ApplySprite(
                    overlayImg,
                    LoadSprite(CrystalDir + "ui_stat_panel_memory_v2.png")
                    ?? LoadSprite(KitDir + "ui_stat_panel_v1.png"),
                    Image.Type.Simple,
                    true);
            }
        }

        private static void WireStatDetailsMenuRefs(
            CharacterBuildMenuUI menu,
            Transform canvas,
            GameObject overlay,
            Text body)
        {
            if (menu == null)
            {
                return;
            }

            var so = new SerializedObject(menu);
            var detailsPanel = canvas != null ? canvas.Find("DetailsPanel") : null;
            so.FindProperty("detailsButton").objectReferenceValue =
                detailsPanel != null ? detailsPanel.GetComponent<Button>() : null;
            var preview = overlay != null && overlay.transform.parent != null
                ? overlay.transform.parent.gameObject
                : FindStatDetailsPreviewRoot(canvas != null ? canvas.gameObject.scene : default);
            so.FindProperty("statDetailsHost").objectReferenceValue = preview;
            so.FindProperty("statDetailsOverlay").objectReferenceValue = overlay;
            so.FindProperty("statDetailsDimmer").objectReferenceValue =
                preview != null ? preview.transform.Find(SkillEquipDimName)?.gameObject : null;
            so.FindProperty("statDetailsBodyLabel").objectReferenceValue = body;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(menu);
        }

        private static void EnsureStatDetailsOverlay(Transform canvas, out GameObject overlayGo, out Text body)
        {
            overlayGo = FindStatDetailsOverlay(canvas);
            if (overlayGo != null)
            {
                body = overlayGo.transform.Find("Body")?.GetComponent<Text>()
                       ?? overlayGo.transform.Find("Panel/Body")?.GetComponent<Text>();
                return;
            }

            var previewRoot = canvas != null
                ? EnsureStatDetailsEditPreview(canvas.GetComponent<Canvas>())
                : null;
            var overlayParent = previewRoot != null ? previewRoot.transform : canvas;
            var panelSprite = LoadSprite(CrystalDir + "ui_stat_panel_memory_v2.png")
                              ?? LoadSprite(KitDir + "ui_stat_panel_v1.png");
            var closeSprite = LoadSprite(CrystalDir + "ui_stat_btn_close_v2.png")
                              ?? LoadSprite(KitDir + "ui_stat_btn_close_v1.png");
            var ink = CharacterBuildStatTheme.LabelColor;

            var overlay = CreateGlassPanel(overlayParent, "StatDetailsOverlay", panelSprite);
            ApplySprite(overlay, panelSprite, Image.Type.Simple, true);
            overlay.preserveAspect = true;
            overlay.raycastTarget = true;
            Stretch(overlay.rectTransform, new Vector2(0.28f, 0.22f), new Vector2(0.72f, 0.82f), Vector2.zero, Vector2.zero);
            overlayGo = overlay.gameObject;
            Undo.RegisterCreatedObjectUndo(overlayGo, "Create Stat Details Overlay");

            var title = CreateText(overlay.transform, "Title", "STAT DETAILS", 26, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.color = ink;
            UiFontCatalog.Apply(title, UiFontRole.Display, 26, FontStyle.Bold);
            Stretch(title.rectTransform, new Vector2(0.10f, 0.80f), new Vector2(0.90f, 0.93f), Vector2.zero, Vector2.zero);

            body = CreateText(overlay.transform, "Body", string.Empty, 26, TextAnchor.UpperLeft, FontStyle.Normal);
            body.color = ink;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            UiFontCatalog.Apply(body, UiFontRole.Body, 26);
            Stretch(body.rectTransform, new Vector2(0.12f, 0.18f), new Vector2(0.88f, 0.76f), Vector2.zero, Vector2.zero);

            var close = CreateImage(overlay.transform, "CloseButton", Color.white);
            ApplySprite(close, closeSprite, Image.Type.Simple, true);
            close.preserveAspect = true;
            Stretch(close.rectTransform, new Vector2(0.42f, 0.04f), new Vector2(0.58f, 0.16f), Vector2.zero, Vector2.zero);
            var closeBtn = close.gameObject.AddComponent<Button>();
            closeBtn.targetGraphic = close;
        }

        private static void DestroyEquipSlotChildren(Transform overlay)
        {
            if (overlay == null)
            {
                return;
            }

            for (var i = overlay.childCount - 1; i >= 0; i--)
            {
                var child = overlay.GetChild(i);
                if (child == null || !child.name.StartsWith("EquipSlot_", StringComparison.Ordinal))
                {
                    continue;
                }

                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void EnsureCloseButton(Transform canvas)
        {
            var closeTf = canvas.Find("CloseBtn");
            if (closeTf == null)
            {
                return;
            }

            var btn = closeTf.GetComponent<Button>();
            if (btn != null)
            {
                return;
            }

            btn = closeTf.gameObject.AddComponent<Button>();
            var img = closeTf.GetComponent<Image>();
            if (img != null)
            {
                btn.targetGraphic = img;
                img.raycastTarget = true;
            }
        }

        private static Text FindText(Transform parent, string childName)
        {
            return parent != null ? parent.Find(childName)?.GetComponent<Text>() : null;
        }

        [MenuItem("Fractured Chorus/Wire CharacterBuild Portrait Layers")]
        public static void WireCharacterBuildPortraitLayers()
        {
            var canvas = GameObject.Find("BuildCanvas");
            if (canvas == null && File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                canvas = GameObject.Find("BuildCanvas");
            }

            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] BuildCanvas missing.");
                return;
            }

            var panel = canvas.transform.Find("PortraitPanel");
            if (panel == null)
            {
                Debug.LogError("[Fractured Chorus] PortraitPanel missing.");
                return;
            }

            var view = panel.GetComponent<CharacterBuildPortraitLayersView>()
                       ?? panel.gameObject.AddComponent<CharacterBuildPortraitLayersView>();
            var layerNames = new[] { "Ren_Portrait", "Charlotte_Portrait", "Coda_Portrait" };
            var layers = new GameObject[layerNames.Length];
            for (var i = 0; i < layerNames.Length; i++)
            {
                var child = panel.Find(layerNames[i]);
                if (child == null)
                {
                    Debug.LogError($"[Fractured Chorus] Missing portrait layer: PortraitPanel/{layerNames[i]}");
                    return;
                }

                layers[i] = child.gameObject;
            }

            var so = new SerializedObject(view);
            var layersProp = so.FindProperty("layers");
            layersProp.arraySize = layers.Length;
            for (var i = 0; i < layers.Length; i++)
            {
                layersProp.GetArrayElementAtIndex(i).objectReferenceValue = layers[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(canvas.scene);
            Debug.Log("[Fractured Chorus] Portrait layers wired on PortraitPanel (transforms unchanged). Ctrl+S to save.");
        }

        private static void ExportLayoutSnapshot(Transform root)
        {
            var nodes = new List<LayoutSnapshotNode>();
            CollectLayoutSnapshotNodes(root, root.name, nodes);
            var snapshot = new LayoutSnapshotFile
            {
                scene = ScenePath,
                savedAtUtc = System.DateTime.UtcNow.ToString("o"),
                note = "Reference backup only. Layout SoT is CharacterBuildLayoutSandbox.unity — editor menus must not re-apply these values.",
                buildCanvasChildren = GetChildNames(root),
                nodes = nodes.ToArray(),
            };

            var json = JsonUtility.ToJson(snapshot, true);
            var fullPath = Path.Combine(
                Application.dataPath,
                "FracturedChorus/Art/UI/StatMenu/MockKit/sandbox_layout_snapshot.json");
            File.WriteAllText(fullPath, json);
            AssetDatabase.ImportAsset(LayoutSnapshotPath);
        }

        private static string[] GetChildNames(Transform parent)
        {
            var names = new string[parent.childCount];
            for (var i = 0; i < parent.childCount; i++)
            {
                names[i] = parent.GetChild(i).name;
            }

            return names;
        }

        private static void CollectLayoutSnapshotNodes(
            Transform node,
            string path,
            List<LayoutSnapshotNode> nodes)
        {
            var entry = new LayoutSnapshotNode
            {
                path = path,
                siblingIndex = node.GetSiblingIndex(),
                activeSelf = node.gameObject.activeSelf,
            };

            if (node is RectTransform rect)
            {
                entry.layoutKind = "RectTransform";
                entry.anchorMin = rect.anchorMin;
                entry.anchorMax = rect.anchorMax;
                entry.anchoredPosition = rect.anchoredPosition;
                entry.sizeDelta = rect.sizeDelta;
                entry.pivot = rect.pivot;
                entry.localScale = rect.localScale;
            }
            else
            {
                entry.layoutKind = "Transform";
                entry.localPosition = node.localPosition;
                entry.localScale = node.localScale;
            }

            var spriteRenderer = node.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                entry.spritePath = AssetDatabase.GetAssetPath(spriteRenderer.sprite);
            }

            var image = node.GetComponent<Image>();
            if (image != null && image.sprite != null)
            {
                entry.spritePath = AssetDatabase.GetAssetPath(image.sprite);
            }

            nodes.Add(entry);
            for (var i = 0; i < node.childCount; i++)
            {
                var child = node.GetChild(i);
                CollectLayoutSnapshotNodes(child, path + "/" + child.name, nodes);
            }
        }

        [System.Serializable]
        private sealed class LayoutSnapshotFile
        {
            public string scene;
            public string savedAtUtc;
            public string note;
            public string[] buildCanvasChildren;
            public LayoutSnapshotNode[] nodes;
        }

        [System.Serializable]
        private sealed class LayoutSnapshotNode
        {
            public string path;
            public int siblingIndex;
            public bool activeSelf;
            public string layoutKind;
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 anchoredPosition;
            public Vector2 sizeDelta;
            public Vector2 pivot;
            public Vector3 localPosition;
            public Vector3 localScale;
            public string spritePath;
        }

        [MenuItem("Fractured Chorus/Ensure CharacterBuild Sandbox Labels")]
        public static void EnsureCharacterBuildSandboxLabels()
        {
            var canvas = GameObject.Find("BuildCanvas");
            if (canvas == null && File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                canvas = GameObject.Find("BuildCanvas");
            }

            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] BuildCanvas missing. Open CharacterBuildLayoutSandbox first.");
                return;
            }

            var root = canvas.transform;
            var added = 0;
            added += EnsureSandboxHeaderLabels(root);
            added += EnsureSandboxPortraitLabels(root);
            added += EnsureSandboxMemoryLabels(root);
            added += EnsureSandboxStatsSectionLabels(root);
            added += EnsureSandboxBattleStyleLabels(root);
            added += EnsureSandboxSkillLabels(root);
            added += EnsureSandboxNavLabels(root);

            EditorSceneManager.MarkSceneDirty(canvas.scene);
            Debug.Log(
                $"[Fractured Chorus] Sandbox labels ensured ({added} created). Existing RectTransforms preserved. Ctrl+S to save.");
        }

        [MenuItem("Fractured Chorus/Ensure CharacterBuild Sandbox Chrome")]
        public static void EnsureCharacterBuildSandboxChrome()
        {
            var canvas = GameObject.Find("BuildCanvas");
            if (canvas == null && File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                canvas = GameObject.Find("BuildCanvas");
            }

            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] BuildCanvas missing. Open CharacterBuildLayoutSandbox first.");
                return;
            }

            var root = canvas.transform;
            var added = 0;
            added += EnsureSandboxPortraitChrome(root);
            added += EnsureSandboxStatsChrome(root);
            added += EnsureSandboxSkillChrome(root);

            EditorSceneManager.MarkSceneDirty(canvas.scene);
            Debug.Log(
                $"[Fractured Chorus] Sandbox chrome ensured ({added} created). Existing RectTransforms preserved. Ctrl+S to save.");
        }

        private static int EnsureSandboxPortraitChrome(Transform root)
        {
            var hud = root.Find("HudCorner");
            if (hud == null)
            {
                return 0;
            }

            var added = 0;
            HealPortraitNameLabel(hud);

            Sprite Sp(string file) => LoadSprite(CrystalDir + file);

            if (TryCreateNamedImage(
                    hud,
                    "PortraitNamePlate",
                    Sp("ui_stat_header_bar_v2.png"),
                    new Vector2(0.12f, 0.80f),
                    new Vector2(0.76f, 0.99f),
                    Image.Type.Sliced,
                    false))
            {
                added++;
                var namePlate = hud.Find("PortraitNamePlate");
                var nameLabel = hud.Find("PortraitNameLabel");
                if (namePlate != null && nameLabel != null)
                {
                    SetSiblingBeforeTransform(namePlate, nameLabel);
                }
            }

            added += EnsureSandboxExpBar(root);
            return added;
        }

        private static int EnsureSandboxExpBar(Transform root)
        {
            Sprite Sp(string file) => LoadSprite(CrystalDir + file);
            var added = 0;

            var expBarRoot = root.Find("ExpBar");
            if (expBarRoot == null)
            {
                var nested = root.Find("HudCorner/ExpBar");
                if (nested != null)
                {
                    nested.SetParent(root, true);
                    expBarRoot = nested;
                }
            }

            if (expBarRoot == null)
            {
                var panel = CreatePanel(root, "ExpBar");
                Stretch(panel, new Vector2(0.02f, 0.755f), new Vector2(0.28f, 0.82f), Vector2.zero, Vector2.zero);
                expBarRoot = panel;
                added++;

                var hud = root.Find("HudCorner");
                if (hud != null)
                {
                    SetSiblingAfterTransform(expBarRoot, hud);
                }
            }

            TryCreateNamedImage(
                expBarRoot,
                "ExpBarTrack",
                Sp("ui_stat_bar_track_v3.png"),
                Vector2.zero,
                Vector2.one,
                Image.Type.Simple,
                false);

            var createdFill = TryCreateNamedImage(
                expBarRoot,
                "ExpBarFill",
                Sp("ui_stat_bar_fill_v3.png"),
                new Vector2(0.035f, 0.22f),
                new Vector2(0.965f, 0.78f),
                Image.Type.Filled,
                false);

            var fill = expBarRoot.Find("ExpBarFill")?.GetComponent<Image>();
            if (fill != null)
            {
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = (int)Image.OriginHorizontal.Left;
                fill.preserveAspect = false;
                if (createdFill || fill.fillAmount <= 0f)
                {
                    fill.fillAmount = 0.45f;
                }
            }

            return added;
        }

        private static int EnsureSandboxStatsChrome(Transform root)
        {
            var stats = root.Find("StatsPanel");
            if (stats == null)
            {
                return 0;
            }

            var added = 0;
            Sprite Sp(string file) => LoadSprite(CrystalDir + file);
            Sprite Ip(string file) => LoadSprite(IconDir + file);

            if (TryCreateNamedImage(
                    stats,
                    "StatusHeaderPlate",
                    Sp("ui_stat_header_bar_v2.png"),
                    new Vector2(0.02f, 0.90f),
                    new Vector2(0.36f, 1.04f),
                    Image.Type.Sliced,
                    false))
            {
                added++;
                var statusPlate = stats.Find("StatusHeaderPlate");
                var statusTitle = stats.Find("StatusTitle");
                if (statusPlate != null && statusTitle != null)
                {
                    SetSiblingBeforeTransform(statusPlate, statusTitle);
                }
            }

            var detailsLabel = stats.Find("DetailsLabel");
            var detailsBtn = stats.Find("DetailsBtn");
            var createdBtn = false;
            if (detailsBtn == null)
            {
                var btnImage = CreateImage(stats, "DetailsBtn", Color.white);
                ApplySprite(btnImage, Sp("ui_stat_btn_nav_normal_v2.png"), Image.Type.Sliced, true);
                Stretch(btnImage.rectTransform, new Vector2(0.58f, 0.90f), new Vector2(0.98f, 1.04f), Vector2.zero, Vector2.zero);
                detailsBtn = btnImage.transform;
                createdBtn = true;
                added++;
            }

            if (detailsBtn != null)
            {
                if (TryCreateNamedImage(
                        detailsBtn,
                        "DetailsSearchIcon",
                        Ip("ui_stat_icon_search_v1.png"),
                        new Vector2(0.06f, 0.20f),
                        new Vector2(0.22f, 0.80f),
                        Image.Type.Simple,
                        true))
                {
                    added++;
                }

                if (detailsLabel != null)
                {
                    if (createdBtn && detailsLabel.parent == stats)
                    {
                        detailsLabel.SetParent(detailsBtn, false);
                        Stretch(
                            detailsLabel.GetComponent<RectTransform>(),
                            new Vector2(0.24f, 0.08f),
                            new Vector2(0.94f, 0.92f),
                            Vector2.zero,
                            Vector2.zero);
                    }
                }
                else if (createdBtn)
                {
                    EnsureSandboxLabel(
                        detailsBtn,
                        "DetailsLabel",
                        "DETAILS",
                        UiFontRole.Body,
                        12,
                        TextAnchor.MiddleRight,
                        CharacterBuildStatTheme.SubLabelColor,
                        new Vector2(0.24f, 0.08f),
                        new Vector2(0.94f, 0.92f));
                }

                if (createdBtn)
                {
                    var statusTitle = stats.Find("StatusTitle");
                    if (statusTitle != null)
                    {
                        SetSiblingBeforeTransform(detailsBtn, statusTitle);
                    }
                    else if (detailsLabel != null && detailsLabel.parent == stats)
                    {
                        SetSiblingBeforeTransform(detailsBtn, detailsLabel);
                    }
                }
            }

            return added;
        }

        private static int EnsureSandboxSkillChrome(Transform root)
        {
            var skills = root.Find("SkillsPanel");
            if (skills == null)
            {
                return 0;
            }

            var added = 0;
            var plusSprite = LoadSprite(IconDir + "ui_stat_icon_plus_v1.png");
            for (var i = 1; i <= 10; i++)
            {
                var slot = skills.Find($"SkillSlot_{i}");
                if (slot == null || slot.Find("SkillPlus") != null)
                {
                    continue;
                }

                var plus = CreateImage(slot, "SkillPlus", Color.white);
                ApplySprite(plus, plusSprite, Image.Type.Simple, true);
                plus.preserveAspect = true;
                Stretch(plus.rectTransform, new Vector2(0.34f, 0.02f), new Vector2(0.66f, 0.22f), Vector2.zero, Vector2.zero);
                added++;
            }

            return added;
        }

        private static void HealPortraitNameLabel(Transform hud)
        {
            var label = hud.Find("PortraitNameLabel");
            if (label == null)
            {
                var orphan = GameObject.Find("PortraitNameLabel");
                if (orphan != null)
                {
                    orphan.transform.SetParent(hud, false);
                    label = orphan.transform;
                }
            }

            if (label == null)
            {
                return;
            }

            label.gameObject.SetActive(true);
        }

        private static bool TryCreateNamedImage(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 amin,
            Vector2 amax,
            Image.Type type,
            bool preserveAspect,
            bool raycast = false)
        {
            if (parent.Find(name) != null)
            {
                var existing = parent.Find(name).GetComponent<Image>();
                if (existing != null)
                {
                    ApplySprite(existing, sprite, type, raycast);
                    existing.preserveAspect = preserveAspect;
                }

                return false;
            }

            EnsureNamedImage(parent, name, sprite, amin, amax, type, preserveAspect, raycast);
            return true;
        }

        private static void SetSiblingBeforeTransform(Transform child, Transform before)
        {
            if (child == null || before == null || child.parent != before.parent)
            {
                return;
            }

            child.SetSiblingIndex(before.GetSiblingIndex());
        }

        private static int EnsureSandboxHeaderLabels(Transform root)
        {
            var header = root.Find("HeaderBar");
            if (header == null)
            {
                return 0;
            }

            HealMisplacedHeaderLabels(header);

            var added = 0;
            if (EnsureSandboxLabel(
                    header,
                    "TitleText",
                    "STAT",
                    UiFontRole.Display,
                    32,
                    TextAnchor.MiddleLeft,
                    CharacterBuildStatTheme.LabelColor,
                    new Vector2(0.04f, 0.38f),
                    new Vector2(0.38f, 0.96f)))
            {
                added++;
            }

            var titleText = header.Find("TitleText")?.GetComponent<Text>();
            if (titleText != null)
            {
                ApplySandboxTextOverflow(titleText);
            }

            if (EnsureSandboxLabel(
                    header,
                    "TitleSub",
                    "\u30B9\u30C6\u30FC\u30BF\u30B9",
                    UiFontRole.Body,
                    11,
                    TextAnchor.MiddleLeft,
                    CharacterBuildStatTheme.SubLabelColor,
                    new Vector2(0.04f, 0.08f),
                    new Vector2(0.38f, 0.40f)))
            {
                added++;
            }

            return added;
        }

        private static void HealMisplacedHeaderLabels(Transform header)
        {
            ReparentIfFound("TitleText", header);
            ReparentIfFound("TitleSub", header);
        }

        private static void ReparentIfFound(string objectName, Transform expectedParent)
        {
            var existing = expectedParent.Find(objectName);
            if (existing != null)
            {
                return;
            }

            var orphan = GameObject.Find(objectName);
            if (orphan == null)
            {
                return;
            }

            orphan.transform.SetParent(expectedParent, false);
        }

        private static int EnsureSandboxPortraitLabels(Transform root)
        {
            var hud = root.Find("HudCorner");
            if (hud == null)
            {
                return 0;
            }

            var added = 0;
            MigratePortraitNameLabel(hud);
            added += EnsurePortraitHudLabel(
                hud,
                "IndexLabel",
                "01",
                UiFontRole.Display,
                28,
                TextAnchor.MiddleLeft,
                CharacterBuildStatTheme.AccentColor,
                new Vector2(0.02f, 0.82f),
                new Vector2(0.14f, 0.98f));
            added += EnsurePortraitHudLabel(
                hud,
                "PortraitNameLabel",
                "REN",
                UiFontRole.Display,
                24,
                TextAnchor.MiddleLeft,
                CharacterBuildStatTheme.LabelColor,
                new Vector2(0.14f, 0.82f),
                new Vector2(0.72f, 0.98f));
            added += EnsurePortraitHudLabel(
                hud,
                "LevelLabel",
                "LV. 01 / 99",
                UiFontRole.Body,
                16,
                TextAnchor.MiddleLeft,
                CharacterBuildStatTheme.LabelColor,
                new Vector2(0.02f, 0.68f),
                new Vector2(0.52f, 0.82f));
            added += EnsurePortraitHudLabel(
                hud,
                "ExpLabel",
                "EXP 0 / 100",
                UiFontRole.Body,
                13,
                TextAnchor.MiddleLeft,
                CharacterBuildStatTheme.SubLabelColor,
                new Vector2(0.02f, 0.54f),
                new Vector2(0.72f, 0.68f));

            return added;
        }

        private static int EnsurePortraitHudLabel(
            Transform hud,
            string objectName,
            string content,
            UiFontRole role,
            int fontSize,
            TextAnchor alignment,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var created = EnsureSandboxLabel(
                hud,
                objectName,
                content,
                role,
                fontSize,
                alignment,
                color,
                anchorMin,
                anchorMax);
            var text = hud.Find(objectName)?.GetComponent<Text>();
            if (text != null)
            {
                ApplySandboxTextOverflow(text);
            }

            return created ? 1 : 0;
        }

        private static void MigratePortraitNameLabel(Transform hud)
        {
            if (hud.Find("PortraitNameLabel") != null || hud.Find("NameLabel") == null)
            {
                return;
            }

            hud.Find("NameLabel").name = "PortraitNameLabel";
        }

        private static void ApplySandboxTextOverflow(Text text)
        {
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static int EnsureSandboxMemoryLabels(Transform root)
        {
            var memory = root.Find("MemoryPanel");
            if (memory == null)
            {
                return 0;
            }

            var added = 0;
            if (EnsureSandboxLabel(
                    memory,
                    "MemoryTitle",
                    "MEMORY FRAGMENT",
                    UiFontRole.Body,
                    10,
                    TextAnchor.LowerLeft,
                    CharacterBuildStatTheme.SubLabelColor,
                    new Vector2(0.18f, 0.08f),
                    new Vector2(0.92f, 0.38f)))
            {
                added++;
            }

            if (EnsureSandboxLabel(
                    memory,
                    "MemoryPercent",
                    "0%",
                    UiFontRole.Display,
                    36,
                    TextAnchor.LowerLeft,
                    CharacterBuildStatTheme.LabelColor,
                    new Vector2(0.18f, 0.38f),
                    new Vector2(0.92f, 0.92f)))
            {
                added++;
            }

            return added;
        }

        private static int EnsureSandboxStatsSectionLabels(Transform root)
        {
            var stats = root.Find("StatsPanel");
            if (stats == null)
            {
                return 0;
            }

            var added = 0;
            if (EnsureSandboxLabel(
                    stats,
                    "StatusTitle",
                    "STATUS",
                    UiFontRole.Display,
                    18,
                    TextAnchor.MiddleLeft,
                    CharacterBuildStatTheme.LabelColor,
                    new Vector2(0.04f, 0.92f),
                    new Vector2(0.34f, 1.02f)))
            {
                added++;
            }

            if (EnsureSandboxLabel(
                    stats,
                    "DetailsLabel",
                    "DETAILS",
                    UiFontRole.Body,
                    12,
                    TextAnchor.MiddleRight,
                    CharacterBuildStatTheme.SubLabelColor,
                    new Vector2(0.62f, 0.92f),
                    new Vector2(0.96f, 1.02f)))
            {
                added++;
            }

            if (EnsureSandboxLabel(
                    stats,
                    "ResistTitle",
                    "RESIST",
                    UiFontRole.Display,
                    14,
                    TextAnchor.MiddleLeft,
                    CharacterBuildStatTheme.LabelColor,
                    new Vector2(0.04f, 0.02f),
                    new Vector2(0.30f, 0.10f)))
            {
                added++;
            }

            var resistAnchors = new[]
            {
                new Vector2(0.04f, 0.02f),
                new Vector2(0.20f, 0.02f),
                new Vector2(0.36f, 0.02f),
                new Vector2(0.52f, 0.02f),
                new Vector2(0.68f, 0.02f),
                new Vector2(0.84f, 0.02f),
            };
            for (var i = 0; i < resistAnchors.Length; i++)
            {
                var x0 = resistAnchors[i].x;
                if (EnsureSandboxLabel(
                        stats,
                        $"ResistValue_{i}",
                        "0%",
                        UiFontRole.Body,
                        11,
                        TextAnchor.UpperCenter,
                        CharacterBuildStatTheme.SubLabelColor,
                        new Vector2(x0, 0.02f),
                        new Vector2(x0 + 0.14f, 0.10f)))
                {
                    added++;
                }
            }

            return added;
        }

        private static int EnsureSandboxBattleStyleLabels(Transform root)
        {
            var battle = root.Find("Battle_Style");
            if (battle == null)
            {
                return 0;
            }

            var added = 0;
            added += EnsureBattleStyleLabel(
                battle,
                "BattleStyleTitle",
                "BATTLE STYLE",
                UiFontRole.Display,
                14,
                TextAnchor.UpperLeft,
                CharacterBuildStatTheme.LabelColor,
                new Vector2(0.04f, 0.78f),
                new Vector2(0.46f, 0.98f));
            added += EnsureBattleStyleLabel(
                battle,
                "BalanceLabel",
                "Chủ lực sát thương",
                UiFontRole.Display,
                20,
                TextAnchor.UpperLeft,
                CharacterBuildStatTheme.LabelColor,
                new Vector2(0.22f, 0.48f),
                new Vector2(0.72f, 0.76f));
            added += EnsureBattleStyleLabel(
                battle,
                "BalanceDescLine1",
                "DPS · Melody",
                UiFontRole.Body,
                11,
                TextAnchor.UpperLeft,
                CharacterBuildStatTheme.SubLabelColor,
                new Vector2(0.22f, 0.28f),
                new Vector2(0.96f, 0.48f));
            added += EnsureBattleStyleLabel(
                battle,
                "BalanceDescLine2",
                string.Empty,
                UiFontRole.Body,
                11,
                TextAnchor.UpperLeft,
                CharacterBuildStatTheme.SubLabelColor,
                new Vector2(0.22f, 0.08f),
                new Vector2(0.96f, 0.28f));

            return added;
        }

        private static int EnsureBattleStyleLabel(
            Transform parent,
            string objectName,
            string content,
            UiFontRole role,
            int fontSize,
            TextAnchor alignment,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var created = EnsureSandboxLabel(
                parent,
                objectName,
                content,
                role,
                fontSize,
                alignment,
                color,
                anchorMin,
                anchorMax);
            var text = parent.Find(objectName)?.GetComponent<Text>();
            if (text != null)
            {
                ApplySandboxTextOverflow(text);
            }

            return created ? 1 : 0;
        }

        private static int EnsureSandboxSkillLabels(Transform root)
        {
            var skills = root.Find("SkillsPanel");
            if (skills == null)
            {
                return 0;
            }

            var added = 0;
            if (EnsureSandboxLabel(
                    skills,
                    "SkillTitle",
                    "SKILL",
                    UiFontRole.Display,
                    18,
                    TextAnchor.MiddleLeft,
                    CharacterBuildStatTheme.LabelColor,
                    new Vector2(0.04f, 0.92f),
                    new Vector2(0.28f, 1.02f)))
            {
                added++;
            }

            if (EnsureSandboxLabel(
                    skills,
                    "SkillPointLabel",
                    "SKILL POINT",
                    UiFontRole.Body,
                    11,
                    TextAnchor.MiddleRight,
                    CharacterBuildStatTheme.SubLabelColor,
                    new Vector2(0.58f, 0.92f),
                    new Vector2(0.84f, 1.02f)))
            {
                added++;
            }

            if (EnsureSandboxLabel(
                    skills,
                    "SkillPointValue",
                    "0",
                    UiFontRole.Display,
                    22,
                    TextAnchor.MiddleCenter,
                    CharacterBuildStatTheme.LabelColor,
                    new Vector2(0.84f, 0.90f),
                    new Vector2(0.96f, 1.02f)))
            {
                added++;
            }

            for (var i = 1; i <= 10; i++)
            {
                var slot = skills.Find($"SkillSlot_{i}");
                if (slot == null)
                {
                    continue;
                }

                var label = i.ToString("00");
                if (EnsureSandboxLabel(
                        slot,
                        "IndexLabel",
                        label,
                        UiFontRole.Display,
                        14,
                        TextAnchor.UpperCenter,
                        CharacterBuildStatTheme.LabelColor,
                        new Vector2(0.08f, 0.82f),
                        new Vector2(0.92f, 0.98f)))
                {
                    added++;
                }
            }

            return added;
        }

        private static int EnsureSandboxNavLabels(Transform root)
        {
            var nav = root.Find("NavColumn");
            if (nav == null)
            {
                return 0;
            }

            var labels = new[] { "STAT", "BONDS", "CALENDAR", "SYSTEM" };
            var added = 0;
            for (var i = 0; i < labels.Length; i++)
            {
                var btn = nav.Find($"Nav_{labels[i]}");
                if (btn == null)
                {
                    continue;
                }

                var labelTf = btn.Find("Label");
                if (labelTf == null)
                {
                    if (EnsureSandboxLabel(
                            btn,
                            "Label",
                            labels[i],
                            UiFontRole.Body,
                            14,
                            TextAnchor.MiddleLeft,
                            i == 0 ? CharacterBuildStatTheme.LabelColor : CharacterBuildStatTheme.SubLabelColor,
                            new Vector2(0.32f, 0f),
                            new Vector2(0.96f, 1f)))
                    {
                        added++;
                    }

                    continue;
                }

                var text = labelTf.GetComponent<Text>();
                if (text == null)
                {
                    text = labelTf.gameObject.AddComponent<Text>();
                    added++;
                }

                UiFontCatalog.Apply(text, UiFontRole.Body, 14);
                text.text = labels[i];
                text.alignment = TextAnchor.MiddleLeft;
                text.color = i == 0 ? CharacterBuildStatTheme.LabelColor : CharacterBuildStatTheme.SubLabelColor;
                text.raycastTarget = false;
            }

            return added;
        }

        private static bool EnsureSandboxLabel(
            Transform parent,
            string objectName,
            string content,
            UiFontRole role,
            int fontSize,
            TextAnchor alignment,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var existing = parent.Find(objectName);
            Text text;
            var created = false;
            if (existing != null)
            {
                text = existing.GetComponent<Text>() ?? existing.gameObject.AddComponent<Text>();
            }
            else
            {
                text = CreateText(parent, objectName, content, fontSize, alignment, FontStyle.Normal);
                created = true;
            }

            if (created)
            {
                UiFontCatalog.Apply(text, role, fontSize);
                text.text = content;
                text.alignment = alignment;
                Stretch(text.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            }
            else
            {
                var size = text.fontSize > 0 ? text.fontSize : fontSize;
                UiFontCatalog.Apply(text, role, size);
                if (string.IsNullOrWhiteSpace(text.text))
                {
                    text.text = content;
                }
            }

            text.color = color;
            text.raycastTarget = false;
            text.enabled = true;

            return created;
        }

        private static Text EnsureStatRowText(
            Transform rowTf,
            string objectName,
            string content,
            UiFontRole role,
            int fontSize,
            TextAnchor anchor,
            Color color,
            Transform layoutRef,
            bool upperHalf,
            bool valueSlot = false)
        {
            var existing = rowTf.Find(objectName);
            Text text;
            var created = false;
            if (existing != null)
            {
                text = existing.GetComponent<Text>() ?? existing.gameObject.AddComponent<Text>();
            }
            else
            {
                text = CreateText(rowTf, objectName, content, fontSize, anchor, FontStyle.Normal);
                created = true;
            }

            text.enabled = true;
            text.raycastTarget = false;
            if (created)
            {
                UiFontCatalog.Apply(text, role, fontSize);
                text.text = content;
                text.alignment = anchor;
                text.color = color;
            }
            else if (string.IsNullOrWhiteSpace(text.text))
            {
                text.text = content;
            }

            if (created)
            {
                if (valueSlot)
                {
                    Stretch(
                        text.rectTransform,
                        new Vector2(0.88f, 0.45f),
                        new Vector2(0.98f, 0.95f),
                        Vector2.zero,
                        Vector2.zero);
                }
                else if (layoutRef != null)
                {
                    var artRt = layoutRef.GetComponent<RectTransform>();
                    var xMin = artRt.anchorMin.x;
                    var xMax = Mathf.Min(artRt.anchorMax.x, xMin + 0.16f);
                    if (upperHalf)
                    {
                        Stretch(
                            text.rectTransform,
                            new Vector2(xMin, 0.58f),
                            new Vector2(xMax, 0.98f),
                            Vector2.zero,
                            Vector2.zero);
                    }
                    else
                    {
                        Stretch(
                            text.rectTransform,
                            new Vector2(xMin, 0.10f),
                            new Vector2(artRt.anchorMax.x, 0.54f),
                            Vector2.zero,
                            Vector2.zero);
                    }
                }
                else
                {
                    Stretch(
                        text.rectTransform,
                        new Vector2(0.22f, upperHalf ? 0.58f : 0.10f),
                        new Vector2(0.38f, upperHalf ? 0.98f : 0.54f),
                        Vector2.zero,
                        Vector2.zero);
                }
            }

            return text;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var hit = FindDeep(root.GetChild(i), name);
                if (hit != null)
                {
                    return hit;
                }
            }

            return null;
        }

        private static void EnsureSandboxCrystalField(Transform canvas)
        {
            var existing = canvas.Find("CrystalField");
            var created = existing == null;
            GameObject go;
            if (created)
            {
                go = new GameObject("CrystalField", typeof(RectTransform), typeof(TitleAttractCrystalField));
                go.transform.SetParent(canvas, false);
                StretchFull(go.GetComponent<RectTransform>());
                var bg = canvas.Find("Background");
                if (bg != null)
                {
                    go.transform.SetSiblingIndex(bg.GetSiblingIndex() + 1);
                }
            }
            else
            {
                go = existing.gameObject;
            }

            if (!go.activeSelf)
            {
                go.SetActive(true);
            }

            var field = go.GetComponent<TitleAttractCrystalField>();
            field.Bind(LoadStatCrystalShards(), 14);
            var so = new SerializedObject(field);
            so.FindProperty("sizeRange").vector2Value = new Vector2(22f, 58f);
            so.FindProperty("speedRange").vector2Value = new Vector2(10f, 24f);
            so.FindProperty("spinRange").vector2Value = new Vector2(-14f, 14f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(field);
        }

        private static Sprite[] LoadStatCrystalShards()
        {
            const string titleShardDir = "Assets/FracturedChorus/Art/UI/TitleScreen/SheetV1/";
            return new[]
            {
                LoadLargestSprite(titleShardDir + "ui_crystal_shard_a_v1.png"),
                LoadLargestSprite(titleShardDir + "ui_crystal_shard_b_v1.png"),
                LoadLargestSprite(titleShardDir + "ui_crystal_shard_c_v1.png"),
            };
        }

        private static void EnsureMockGuide(Transform canvas)
        {
            var sprite = LoadSprite(MockScreenPath) ?? LoadLargestSprite(
                "Assets/FracturedChorus/Art/UI/StatMenu/_ref/_ref_stats_mock_full.png");
            var canvasComp = canvas.GetComponent<Canvas>();
            if (canvasComp != null)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    canvasComp.renderMode = RenderMode.ScreenSpaceCamera;
                    canvasComp.worldCamera = cam;
                    canvasComp.planeDistance = 100f;
                }
            }

            var bg = canvas.Find("Background")?.GetComponent<Image>();
            if (bg != null)
            {
                ApplySprite(bg, sprite, Image.Type.Simple, false);
                bg.preserveAspect = false;
                bg.color = new Color(1f, 1f, 1f, MockGuideAlpha);
                bg.raycastTarget = false;
            }

            var existing = canvas.Find("MockGuide");
            Image guide;
            var createdGuide = false;
            if (existing != null)
            {
                guide = existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
            }
            else
            {
                guide = CreateImage(canvas, "MockGuide", Color.white);
                createdGuide = true;
            }

            ApplySprite(guide, sprite, Image.Type.Simple, false);
            guide.preserveAspect = false;
            guide.color = new Color(1f, 1f, 1f, 0.45f);
            guide.raycastTarget = false;
            if (createdGuide)
            {
                StretchFull(guide.rectTransform);
                var bgTf = canvas.Find("Background");
                if (bgTf != null)
                {
                    guide.transform.SetSiblingIndex(bgTf.GetSiblingIndex() + 1);
                }
            }
        }

        [MenuItem("Fractured Chorus/Apply CharacterBuild Sandbox Canvas Layers")]
        public static void ApplySandboxCanvasLayersMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Reset canvas layer order?",
                    "This reorders BuildCanvas siblings (Background, portrait stack, StatsPanel, etc.) and overwrites your Hierarchy arrangement.\n\nOnly use when setting up a fresh sandbox.",
                    "Reset layer order",
                    "Cancel"))
            {
                return;
            }

            var canvas = GameObject.Find("BuildCanvas");
            if (canvas == null && System.IO.File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                canvas = GameObject.Find("BuildCanvas");
            }

            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] BuildCanvas missing.");
                return;
            }

            ApplySandboxCanvasLayers(canvas.transform);
            EditorSceneManager.MarkSceneDirty(canvas.scene);
            Debug.Log("[Fractured Chorus] Canvas layer order reset (RectTransforms unchanged). Ctrl+S to save.");
        }

        /// <summary>
        /// Optional setup helper only — never call from attach/mock/stat-kit flows; scene Hierarchy order is user SoT.
        /// </summary>
        private static void ApplySandboxCanvasLayers(Transform canvas)
        {
            var background = canvas.Find("Background");
            if (background != null)
            {
                background.SetAsFirstSibling();
            }

            var mockGuide = canvas.Find("MockGuide");
            if (mockGuide != null)
            {
                mockGuide.SetSiblingIndex(background != null ? 1 : 0);
            }

            var crystalField = canvas.Find("CrystalField");
            if (crystalField != null && background != null)
            {
                crystalField.SetSiblingIndex(background.GetSiblingIndex() + 1);
            }

            var afterUnderlay = crystalField != null
                ? crystalField
                : mockGuide != null
                    ? mockGuide
                    : background;
            var character = canvas.Find("CharacterPortrait");
            var portraitPanel = canvas.Find("PortraitPanel");
            var memory = canvas.Find("MemoryPanel");

            if (portraitPanel != null)
            {
                SetSiblingAfterTransform(portraitPanel, afterUnderlay);
            }

            if (character != null)
            {
                SetSiblingAfterTransform(
                    character,
                    portraitPanel != null ? portraitPanel : afterUnderlay);
            }

            if (memory != null)
            {
                SetSiblingAfterTransform(
                    memory,
                    character != null ? character : portraitPanel != null ? portraitPanel : afterUnderlay);
            }

            var stats = canvas.Find("StatsPanel");
            if (stats != null)
            {
                var closeBtn = canvas.Find("CloseBtn");
                if (closeBtn != null)
                {
                    stats.SetSiblingIndex(closeBtn.GetSiblingIndex());
                }
                else
                {
                    stats.SetAsLastSibling();
                }
            }
        }

        private static void SetSiblingAfterTransform(Transform child, Transform after)
        {
            if (child == null || child.parent == null)
            {
                return;
            }

            var index = after != null ? after.GetSiblingIndex() + 1 : 0;
            child.SetSiblingIndex(Mathf.Clamp(index, 0, child.parent.childCount - 1));
        }

        private static void SetSiblingAfter(Transform canvas, string childName, Transform after)
        {
            var child = canvas.Find(childName);
            if (child == null)
            {
                return;
            }

            SetSiblingAfterTransform(child, after);
        }

        private static Image EnsureNamedImage(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 amin,
            Vector2 amax,
            Image.Type type,
            bool preserveAspect,
            bool raycast = false)
        {
            var existing = parent.Find(name);
            Image image;
            var created = false;
            if (existing != null)
            {
                image = existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
            }
            else
            {
                image = CreateImage(parent, name, Color.white);
                created = true;
            }

            ApplySprite(image, sprite, type, raycast);
            image.preserveAspect = preserveAspect;
            if (created)
            {
                Stretch(image.rectTransform, amin, amax, Vector2.zero, Vector2.zero);
            }

            return image;
        }

        [MenuItem("Fractured Chorus/Heal CharacterBuild Layout Sandbox Hierarchy")]
        public static void HealScene()
        {
            if (!EditorUtility.DisplayDialog(
                    "Heal CharacterBuild Sandbox",
                    "This destroys BuildCanvas and rebuilds from code — all manual layout is lost.\n\nUse Save CharacterBuild Sandbox Layout first if you need a backup.",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var existing = Object.FindAnyObjectByType<CharacterBuildMenuUI>();
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var canvas = GameObject.Find("BuildCanvas");
            if (canvas != null)
            {
                Object.DestroyImmediate(canvas);
            }

            var cam = GameObject.Find("Main Camera");
            if (cam != null)
            {
                Object.DestroyImmediate(cam);
            }

            var es = Object.FindAnyObjectByType<EventSystem>();
            if (es != null)
            {
                Object.DestroyImmediate(es.gameObject);
            }

            BuildHierarchy();
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                CombatInputSetup.ApplyInputModule(eventSystem.gameObject, destroyImmediate: true);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Fractured Chorus] Healed CharacterBuild layout sandbox hierarchy + bindings.");
        }

        public static void BatchHealCharacterBuildScene()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                CreateScene();
            }
            else
            {
                HealScene();
            }

            EditorApplication.Exit(0);
        }

        private static void BuildHierarchy()
        {
            EnsureCamera();
            EnsureEventSystem();

            var canvasGo = new GameObject(
                "BuildCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var menu = canvasGo.AddComponent<CharacterBuildMenuUI>();
            var panelSprite = LoadSprite(KitDir + "ui_stat_panel_v1.png");
            var headerSprite = LoadSprite(KitDir + "ui_stat_header_v1.png");
            var navSprite = LoadSprite(KitDir + "ui_stat_btn_nav_v1.png");
            var navSelectedSprite = LoadSprite(KitDir + "ui_stat_btn_nav_selected_v1.png");
            var skillSlotSprite = LoadSprite(KitDir + "ui_stat_slot_skill_v1.png");
            var portraitSlotSprite = LoadSprite(CrystalDir + "ui_stat_slot_portrait_v2.png")
                                     ?? LoadSprite(KitDir + "ui_stat_slot_portrait_v1.png");
            var closeSprite = LoadSprite(KitDir + "ui_stat_btn_close_v1.png");
            var rowSprite = LoadSprite(KitDir + "ui_stat_row_v1.png");
            var trackSprite = LoadSprite(CrystalDir + "ui_stat_attr_slider_track_v2.png")
                              ?? LoadSprite(KitDir + "ui_stat_bar_track_v1.png");
            var fillSprite = LoadSprite(CrystalDir + "ui_stat_attr_slider_fill_v2.png")
                             ?? LoadSprite(KitDir + "ui_stat_bar_fill_v1.png");
            var handleSprite = LoadSprite(CrystalDir + "ui_stat_attr_slider_handle_v2.png");
            var plusSprite = LoadSprite(IconDir + "ui_stat_icon_plus_v1.png");
            var minusSprite = LoadSprite(IconDir + "ui_stat_icon_minus_v1.png");
            var closeIcon = LoadSprite(IconDir + "ui_stat_icon_close_v1.png");
            var ringSprite = LoadSprite(DecorDir + "ui_stat_hud_ring_v1.png");
            var crystalSprite = LoadSprite(DecorDir + "ui_stat_crystal_v1.png");
            var portraits = new[]
            {
                LoadSprite(RenPortraitPath),
                LoadSprite(CharlottePortraitPath),
                LoadSprite(CodaPortraitPath)
            };
            var chipFaces = new[]
            {
                LoadSprite(RenChipPath),
                LoadSprite(CharlotteChipPath),
                LoadSprite(CodaChipPath)
            };

            var bg = CreateImage(canvasGo.transform, "Background", FcColorTokens.Surface.Dim);
            StretchFull(bg.rectTransform);

            PlaceDecor(canvasGo.transform, crystalSprite);
            CreateHudCorner(canvasGo.transform);
            CreateNavColumn(canvasGo.transform, navSprite, navSelectedSprite);
            CreateHeader(canvasGo.transform, chipFaces, portraitSlotSprite, closeSprite, closeIcon, out var chips, out var closeBtn);
            var portraitColumn = CreatePortraitColumn(
                canvasGo.transform,
                panelSprite,
                portraits[0],
                ringSprite,
                out var indexLabel,
                out var nameLabel,
                out var elementLabel,
                out var levelLabel,
                out var nextExpLabel,
                out var portrait);
            var prevBtn = CreatePromptButton(
                portraitColumn,
                "NavPrev",
                "[Q]",
                new Vector2(0.02f, 0.42f),
                new Vector2(0.16f, 0.52f),
                navSprite);
            var nextBtn = CreatePromptButton(
                portraitColumn,
                "NavNext",
                "[E]",
                new Vector2(0.84f, 0.42f),
                new Vector2(0.98f, 0.52f),
                navSprite);

            var statsPanel = CreateGlassPanel(canvasGo.transform, "StatsPanel", panelSprite);
            Stretch(statsPanel.rectTransform, new Vector2(0.425f, 0.08f), new Vector2(0.68f, 0.86f), Vector2.zero, Vector2.zero);
            var statsTitle = CreateText(statsPanel.transform, "StatsTitle", "STATS", 22, TextAnchor.MiddleLeft, FontStyle.Bold);
            statsTitle.color = FcColorTokens.Brand.Cyan;
            Stretch(statsTitle.rectTransform, new Vector2(0.06f, 0.90f), new Vector2(0.7f, 0.98f), Vector2.zero, Vector2.zero);

            var strengthRow = CreateStatRow(
                statsPanel.transform, "StatRow_Strength", CharacterBuildStatKind.Strength, "STR", 0.76f, true,
                minusSprite, plusSprite, rowSprite, trackSprite, fillSprite, handleSprite,
                LoadSprite(IconDir + "ui_stat_icon_strength_v2.png"),
                LoadSprite(IconDir + "ui_stat_label_str_v2.png"));
            var magicRow = CreateStatRow(
                statsPanel.transform, "StatRow_Magic", CharacterBuildStatKind.Magic, "MA", 0.60f, true,
                minusSprite, plusSprite, rowSprite, trackSprite, fillSprite, handleSprite,
                LoadSprite(IconDir + "ui_stat_icon_magic_v2.png"),
                LoadSprite(IconDir + "ui_stat_label_ma_v2.png"));
            var enduranceRow = CreateStatRow(
                statsPanel.transform, "StatRow_Endurance", CharacterBuildStatKind.Endurance, "EN", 0.44f, true,
                minusSprite, plusSprite, rowSprite, trackSprite, fillSprite, handleSprite,
                LoadSprite(IconDir + "ui_stat_icon_endurance_v2.png"),
                LoadSprite(IconDir + "ui_stat_label_en_v2.png"));
            var heartBeatRow = CreateStatRow(
                statsPanel.transform, "StatRow_HeartBeat", CharacterBuildStatKind.HeartBeat, "HB", 0.28f, true,
                minusSprite, plusSprite, rowSprite, trackSprite, fillSprite, handleSprite,
                LoadSprite(IconDir + "ui_stat_icon_heartbeat_v2.png"),
                LoadSprite(IconDir + "ui_stat_label_hb_v2.png"));
            var luckRow = CreateStatRow(
                statsPanel.transform, "StatRow_Luck", CharacterBuildStatKind.Luck, "LUCK", 0.12f, false,
                minusSprite, plusSprite, rowSprite, trackSprite, fillSprite, handleSprite,
                LoadSprite(IconDir + "ui_stat_icon_luck_v2.png"),
                LoadSprite(IconDir + "ui_stat_label_luck_v2.png"));

            var elementRow = CreatePanel(statsPanel.transform, "ElementIconRow");
            Stretch(elementRow, new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.12f), Vector2.zero, Vector2.zero);
            var elementIcons = new Image[3];
            var elementRings = new GameObject[3];
            var elementSprites = new[]
            {
                LoadSprite(IconDir + "ui_stat_icon_heartbeat_v1.png"),
                LoadSprite(IconDir + "ui_stat_icon_note_v1.png"),
                LoadSprite(IconDir + "ui_stat_icon_harmony_v1.png")
            };
            for (var i = 0; i < 3; i++)
            {
                var icon = CreateImage(elementRow, $"ElementIcon_{i}", Color.white);
                Stretch(
                    icon.rectTransform,
                    new Vector2(0.06f + i * 0.32f, 0.08f),
                    new Vector2(0.26f + i * 0.32f, 0.92f),
                    Vector2.zero,
                    Vector2.zero);
                ApplySprite(icon, elementSprites[i], Image.Type.Simple, false);
                icon.preserveAspect = true;
                elementIcons[i] = icon;

                var ring = CreateImage(icon.transform, "HighlightRing", FcColorTokens.Semantic.EventGold);
                StretchFull(ring.rectTransform);
                ring.raycastTarget = false;
                ring.transform.SetAsFirstSibling();
                ring.rectTransform.offsetMin = new Vector2(-5f, -5f);
                ring.rectTransform.offsetMax = new Vector2(5f, 5f);
                ring.gameObject.SetActive(false);
                elementRings[i] = ring.gameObject;
            }

            var skillsPanel = CreateGlassPanel(canvasGo.transform, "SkillsPanel", panelSprite);
            Stretch(skillsPanel.rectTransform, new Vector2(0.695f, 0.08f), new Vector2(0.97f, 0.86f), Vector2.zero, Vector2.zero);

            var remainingRoot = CreateGlassPanel(skillsPanel.transform, "RemainingPointsRoot", headerSprite);
            Stretch(remainingRoot.rectTransform, new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.97f), Vector2.zero, Vector2.zero);
            remainingRoot.raycastTarget = false;
            var remaining = CreateText(
                remainingRoot.transform,
                "RemainingPoints",
                "Remaining Points: 0",
                20,
                TextAnchor.MiddleCenter,
                FontStyle.Bold);
            remaining.color = FcColorTokens.Semantic.EventGold;
            StretchFull(remaining.rectTransform);

            var skillRows = new CharacterBuildSkillRowView[3];
            for (var i = 0; i < 3; i++)
            {
                skillRows[i] = CreateSkillRow(skillsPanel.transform, i, 0.80f - i * 0.24f, skillSlotSprite);
            }

            var viewSkillsBtn = CreatePromptButton(
                skillsPanel.transform,
                "FooterViewSkills",
                "[V] View Skills",
                new Vector2(0.06f, 0.02f),
                new Vector2(0.94f, 0.10f),
                navSprite);

            var overlay = CreateGlassPanel(canvasGo.transform, "SkillEquipOverlay", panelSprite);
            var skillEquipBg = LoadSprite(CrystalDir + "ui_stat_panel_memory_v2.png") ?? panelSprite;
            ApplySprite(overlay, skillEquipBg, Image.Type.Simple, true);
            Stretch(overlay.rectTransform, new Vector2(0.28f, 0.22f), new Vector2(0.72f, 1f), Vector2.zero, Vector2.zero);
            overlay.raycastTarget = true;
            overlay.gameObject.SetActive(false);

            var equipTitle = CreateText(overlay.transform, "Title", "Skill Equip", 26, TextAnchor.MiddleCenter, FontStyle.Normal);
            equipTitle.color = new Color(0.227451f, 0.258824f, 0.4f, 1f);
            UiFontCatalog.Apply(equipTitle, UiFontRole.Display, 26);
            Stretch(equipTitle.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero);

            var slotSprite = LoadSprite(CrystalDir + "ui_stat_slot_skill_v2.png") ?? rowSprite;
            var equipPool = new CharacterBuildEquipSlotView[10];
            for (var i = 0; i < equipPool.Length; i++)
            {
                EquipPoolStackCell(i, out var amin, out var amax);
                equipPool[i] = CreateEquipSlotCell(overlay.transform, $"EquipPool_{i}", amin, amax, slotSprite);
            }

            var equipClose = CreatePromptButton(
                overlay.transform,
                "CloseButton",
                "Close",
                new Vector2(0.35f, 0.03f),
                new Vector2(0.65f, 0.11f),
                navSprite);

            var so = new SerializedObject(menu);
            so.FindProperty("indexLabel").objectReferenceValue = indexLabel;
            so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("elementLabel").objectReferenceValue = elementLabel;
            so.FindProperty("levelLabel").objectReferenceValue = levelLabel;
            so.FindProperty("nextExpLabel").objectReferenceValue = nextExpLabel;

            var iconsProp = so.FindProperty("elementIcons");
            iconsProp.arraySize = 3;
            for (var i = 0; i < 3; i++)
            {
                iconsProp.GetArrayElementAtIndex(i).objectReferenceValue = elementIcons[i];
            }

            var ringsProp = so.FindProperty("elementHighlightRings");
            ringsProp.arraySize = 3;
            for (var i = 0; i < 3; i++)
            {
                ringsProp.GetArrayElementAtIndex(i).objectReferenceValue = elementRings[i];
            }

            var chipsProp = so.FindProperty("portraitChips");
            chipsProp.arraySize = chips.Length;
            for (var i = 0; i < chips.Length; i++)
            {
                chipsProp.GetArrayElementAtIndex(i).objectReferenceValue = chips[i];
            }

            so.FindProperty("prevButton").objectReferenceValue = prevBtn;
            so.FindProperty("nextButton").objectReferenceValue = nextBtn;

            var rowsProp = so.FindProperty("skillRows");
            rowsProp.arraySize = 3;
            for (var i = 0; i < 3; i++)
            {
                rowsProp.GetArrayElementAtIndex(i).objectReferenceValue = skillRows[i];
            }

            so.FindProperty("strengthRow").objectReferenceValue = strengthRow;
            so.FindProperty("magicRow").objectReferenceValue = magicRow;
            so.FindProperty("enduranceRow").objectReferenceValue = enduranceRow;
            so.FindProperty("heartBeatRow").objectReferenceValue = heartBeatRow;
            so.FindProperty("luckRow").objectReferenceValue = luckRow;
            so.FindProperty("remainingPointsLabel").objectReferenceValue = remaining;
            so.FindProperty("portraitImage").objectReferenceValue = portrait;

            var portraitsProp = so.FindProperty("menuPortraits");
            portraitsProp.arraySize = 3;
            for (var i = 0; i < 3; i++)
            {
                portraitsProp.GetArrayElementAtIndex(i).objectReferenceValue = portraits[i];
            }

            var facesProp = so.FindProperty("chipFaces");
            facesProp.arraySize = 3;
            for (var i = 0; i < 3; i++)
            {
                facesProp.GetArrayElementAtIndex(i).objectReferenceValue = chipFaces[i];
            }

            so.FindProperty("backButton").objectReferenceValue = closeBtn;
            so.FindProperty("viewSkillsButton").objectReferenceValue = viewSkillsBtn;
            so.FindProperty("skillEquipOverlay").objectReferenceValue = overlay.gameObject;
            so.FindProperty("skillEquipDimmer").objectReferenceValue =
                overlay.transform.parent != null
                    ? overlay.transform.parent.Find("L00_Dim")?.gameObject
                    : null;
            so.FindProperty("skillEquipTitleLabel").objectReferenceValue = equipTitle;
            var slotViewsProp = so.FindProperty("equipSlotViews");
            if (slotViewsProp != null)
            {
                slotViewsProp.arraySize = 0;
            }

            var poolViewsProp = so.FindProperty("equipPoolViews");
            poolViewsProp.arraySize = equipPool.Length;
            for (var i = 0; i < equipPool.Length; i++)
            {
                poolViewsProp.GetArrayElementAtIndex(i).objectReferenceValue = equipPool[i];
            }

            so.FindProperty("skillEquipCloseButton").objectReferenceValue = equipClose;
            so.FindProperty("skillSlotUnlocked").objectReferenceValue =
                LoadSprite(CrystalDir + "ui_stat_slot_skill_v2.png");
            so.FindProperty("skillSlotLocked").objectReferenceValue =
                LoadSprite(CrystalDir + "ui_stat_slot_skill_locked_v2.png");
            EnsureStatDetailsOverlay(canvasGo.transform, out var detailsOverlay, out var detailsBody);
            WireStatDetailsMenuRefs(menu, canvasGo.transform, detailsOverlay, detailsBody);
            so.FindProperty("seedUnspentWhenEmpty").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(menu);
        }

        private static void PlaceDecor(Transform canvas, Sprite crystal)
        {
            var c2 = CreateImage(canvas, "Crystal_BR", Color.white);
            ApplySprite(c2, crystal, Image.Type.Simple, false);
            c2.preserveAspect = true;
            Stretch(c2.rectTransform, new Vector2(0.92f, 0.02f), new Vector2(0.99f, 0.16f), Vector2.zero, Vector2.zero);
        }

        private static void CreateHudCorner(Transform canvas)
        {
            CreateHudCorner(canvas, LoadHeaderMock());
        }

        private static void CreateHudCorner(Transform canvas, SandboxHeaderMock mock)
        {
            var root = CreatePanel(canvas, "HudCorner");
            Stretch(root, RequireVec2(mock.hudMin, "hudMin"), RequireVec2(mock.hudMax, "hudMax"), Vector2.zero, Vector2.zero);

            var ringRoot = CreatePanel(root, "Ring");
            Stretch(ringRoot, RequireVec2(mock.ringMin, "ringMin"), RequireVec2(mock.ringMax, "ringMax"), Vector2.zero, Vector2.zero);

            var ringSprite = LoadLargestSprite(mock.ringOuter) ?? LoadLargestSprite(mock.ringFallback);
            var outer = CreateImage(ringRoot, "HudRing", Color.white);
            ApplySprite(outer, ringSprite, Image.Type.Simple, false);
            outer.preserveAspect = true;
            outer.useSpriteMesh = false;
            var ringAlpha = mock.ringAlpha > 0f ? mock.ringAlpha : 1f;
            outer.color = new Color(1f, 1f, 1f, ringAlpha);
            StretchFull(outer.rectTransform);
            AddRingMotion(outer.gameObject, mock.ringOuterDegreesPerSecond, mock.ringOuterPulse, mock.ringGlitchEverySeconds);

            if (!string.IsNullOrEmpty(mock.ringInner))
            {
                var inner = CreateImage(ringRoot, "RingInner", Color.white);
                ApplySprite(inner, LoadLargestSprite(mock.ringInner), Image.Type.Simple, false);
                inner.preserveAspect = true;
                StretchFull(inner.rectTransform);
                AddRingMotion(inner.gameObject, mock.ringInnerDegreesPerSecond, mock.ringInnerPulse, mock.ringGlitchEverySeconds);
            }

            if (!string.IsNullOrEmpty(mock.ringStub))
            {
                var stub = CreateImage(ringRoot, "RingStub", Color.white);
                ApplySprite(stub, LoadSprite(mock.ringStub), Image.Type.Simple, false);
                stub.preserveAspect = true;
                StretchFull(stub.rectTransform);
            }

            var frames = LoadWaveformFrames(mock.hudDir, mock.waveformPrefix);
            var wave = CreateImage(root, "Waveform", Color.white);
            ApplySprite(wave, frames.Length > 0 ? frames[0] : null, Image.Type.Simple, false);
            wave.preserveAspect = true;
            Stretch(wave.rectTransform, RequireVec2(mock.waveMin, "waveMin"), RequireVec2(mock.waveMax, "waveMax"), Vector2.zero, Vector2.zero);
            BindWaveform(wave, frames, mock.waveformFps, mock.waveformCrossfade);

            var bar = CreateImage(root, "HudBar", Color.white);
            ApplySprite(bar, LoadSprite(mock.hudBar), Image.Type.Simple, false);
            bar.preserveAspect = true;
            Stretch(bar.rectTransform, RequireVec2(mock.barMin, "barMin"), RequireVec2(mock.barMax, "barMax"), Vector2.zero, Vector2.zero);
        }

        private static void CreateChipRow(Transform canvas, SandboxHeaderMock mock)
        {
            var row = CreatePanel(canvas, "PortraitChipRow");
            Stretch(row, RequireVec2(mock.chipsMin, "chipsMin"), RequireVec2(mock.chipsMax, "chipsMax"), Vector2.zero, Vector2.zero);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.spacing = mock.chipSpacing;
            layout.padding = new RectOffset(0, 0, 0, 0);

            var frameOpen = LoadLargestSprite(mock.frameOpen);
            var frameLocked = LoadLargestSprite(mock.frameLocked);
            var faces = ResolveUnlockedFacePaths(mock);
            for (var i = 0; i < mock.slotCount; i++)
            {
                var locked = i >= faces.Length;
                var face = locked ? null : LoadLargestSprite(faces[i]);
                if (!locked && face == null)
                {
                    Debug.LogWarning($"[Fractured Chorus] Missing chip face sprite: {faces[i]}");
                }

                var chip = CreatePortraitChip(
                    row,
                    i,
                    face,
                    frameOpen,
                    frameOpen,
                    frameLocked,
                    RequireVec2(mock.chipFaceMin, "chipFaceMin"),
                    RequireVec2(mock.chipFaceMax, "chipFaceMax"));
                chip.BindSlot(face, locked);
            }
        }

        private static void BindWaveform(Image wave, Sprite[] frames, float framesPerSecond, bool crossfade)
        {
            var blend = CreateImage(wave.transform, "Blend", Color.white);
            StretchFull(blend.rectTransform);
            blend.preserveAspect = true;
            if (frames.Length > 1)
            {
                ApplySprite(blend, frames[1], Image.Type.Simple, false);
            }

            var flip = wave.gameObject.AddComponent<StatHudWaveformFlipbook>();
            flip.Bind(frames);
            var flipSo = new SerializedObject(flip);
            var framesProp = flipSo.FindProperty("frames");
            framesProp.arraySize = frames.Length;
            for (var i = 0; i < frames.Length; i++)
            {
                framesProp.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            }

            flipSo.FindProperty("target").objectReferenceValue = wave;
            flipSo.FindProperty("blendTarget").objectReferenceValue = blend;
            flipSo.FindProperty("framesPerSecond").floatValue = framesPerSecond;
            flipSo.FindProperty("crossfade").boolValue = crossfade;
            flipSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddRingMotion(GameObject go, float degreesPerSecond, float pulse, float glitchEverySeconds = 0f)
        {
            var motion = go.AddComponent<TitleHudRingMotion>();
            var so = new SerializedObject(motion);
            so.FindProperty("degreesPerSecond").floatValue = degreesPerSecond;
            so.FindProperty("pulseAmplitude").floatValue = pulse;
            so.FindProperty("pulseHz").floatValue = 0.28f;
            so.FindProperty("glitchEverySeconds").floatValue = glitchEverySeconds;
            so.FindProperty("glitchDegrees").floatValue = 5.5f;
            so.FindProperty("glitchSeconds").floatValue = 0.07f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateNavColumn(Transform canvas, Sprite navSprite, Sprite navSelectedSprite)
        {
            var nav = CreatePanel(canvas, "NavColumn");
            Stretch(nav, new Vector2(0.03f, 0.20f), new Vector2(0.145f, 0.78f), Vector2.zero, Vector2.zero);
            var labels = new[] { "STAT", "BONDS", "CALENDAR", "SYSTEM" };
            var icons = new[]
            {
                LoadSprite(IconDir + "ui_stat_icon_user_v1.png"),
                LoadSprite(IconDir + "ui_stat_icon_party_v1.png"),
                LoadSprite(IconDir + "ui_stat_icon_calendar_v1.png"),
                LoadSprite(IconDir + "ui_stat_icon_system_v1.png")
            };
            for (var i = 0; i < labels.Length; i++)
            {
                var selected = i == 0;
                var btn = CreateChipLabeledButton(
                    nav,
                    $"Nav_{labels[i]}",
                    labels[i],
                    selected ? navSelectedSprite : navSprite,
                    icons[i],
                    selected);
                Stretch(
                    btn.GetComponent<RectTransform>(),
                    new Vector2(0f, 0.76f - i * 0.24f),
                    new Vector2(1f, 0.96f - i * 0.24f),
                    Vector2.zero,
                    Vector2.zero);
                btn.interactable = selected;
            }
        }

        private static void CreateHeader(
            Transform canvas,
            Sprite[] chipFaces,
            Sprite portraitSlotSprite,
            Sprite closeSprite,
            Sprite closeIcon,
            out CharacterBuildPortraitChipView[] chips,
            out Button closeBtn)
        {
            var header = CreateGlassPanel(canvas, "Header", LoadSprite(KitDir + "ui_stat_header_v1.png"));
            Stretch(header.rectTransform, new Vector2(0.16f, 0.88f), new Vector2(0.97f, 0.97f), Vector2.zero, Vector2.zero);

            var title = CreateText(header.transform, "Title", "STAT", 36, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.color = FcColorTokens.Brand.TextPrimary;
            Stretch(title.rectTransform, new Vector2(0.02f, 0f), new Vector2(0.18f, 1f), Vector2.zero, Vector2.zero);

            var chipRow = CreatePanel(header.transform, "PortraitChipRow");
            Stretch(chipRow, new Vector2(0.22f, 0.08f), new Vector2(0.78f, 0.92f), Vector2.zero, Vector2.zero);
            chips = new CharacterBuildPortraitChipView[3];
            for (var i = 0; i < 3; i++)
            {
                chips[i] = CreatePortraitChip(
                    chipRow,
                    i,
                    chipFaces != null && i < chipFaces.Length ? chipFaces[i] : null,
                    portraitSlotSprite,
                    LoadSprite(CrystalDir + "ui_stat_slot_portrait_v2.png")
                    ?? LoadSprite(KitDir + "ui_stat_slot_portrait_v1.png"));
                Stretch(
                    chips[i].GetComponent<RectTransform>(),
                    new Vector2(0.04f + i * 0.32f, 0f),
                    new Vector2(0.30f + i * 0.32f, 1f),
                    Vector2.zero,
                    Vector2.zero);
            }

            closeBtn = CreatePromptButton(
                header.transform,
                "FooterBack",
                string.Empty,
                new Vector2(0.90f, 0.12f),
                new Vector2(0.985f, 0.88f),
                closeSprite);
            var icon = CreateImage(closeBtn.transform, "Icon", Color.white);
            ApplySprite(icon, closeIcon, Image.Type.Simple, false);
            icon.preserveAspect = true;
            Stretch(icon.rectTransform, new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f), Vector2.zero, Vector2.zero);
        }

        private static RectTransform CreatePortraitColumn(
            Transform canvas,
            Sprite panelSprite,
            Sprite renPortrait,
            Sprite ringSprite,
            out Text indexLabel,
            out Text nameLabel,
            out Text elementLabel,
            out Text levelLabel,
            out Text nextExpLabel,
            out Image portrait)
        {
            var column = CreateGlassPanel(canvas, "PortraitColumn", panelSprite);
            Stretch(column.rectTransform, new Vector2(0.16f, 0.08f), new Vector2(0.41f, 0.86f), Vector2.zero, Vector2.zero);

            indexLabel = CreateText(column.transform, "IndexLabel", "01", 28, TextAnchor.MiddleLeft, FontStyle.Bold);
            indexLabel.color = FcColorTokens.Brand.Cyan;
            Stretch(indexLabel.rectTransform, new Vector2(0.06f, 0.90f), new Vector2(0.22f, 0.98f), Vector2.zero, Vector2.zero);

            nameLabel = CreateText(column.transform, "NameLabel", "Ren Takahashi", 26, TextAnchor.MiddleLeft, FontStyle.Bold);
            nameLabel.color = FcColorTokens.Brand.TextPrimary;
            Stretch(nameLabel.rectTransform, new Vector2(0.24f, 0.90f), new Vector2(0.94f, 0.98f), Vector2.zero, Vector2.zero);

            elementLabel = CreateText(column.transform, "ElementLabel", "Melody", 18, TextAnchor.MiddleLeft, FontStyle.Bold);
            elementLabel.color = FcColorTokens.Brand.Cyan;
            Stretch(elementLabel.rectTransform, new Vector2(0.06f, 0.83f), new Vector2(0.42f, 0.90f), Vector2.zero, Vector2.zero);

            levelLabel = CreateText(column.transform, "LevelLabel", "Lv 15", 22, TextAnchor.MiddleLeft, FontStyle.Bold);
            Stretch(levelLabel.rectTransform, new Vector2(0.44f, 0.83f), new Vector2(0.70f, 0.90f), Vector2.zero, Vector2.zero);

            nextExpLabel = CreateText(column.transform, "NextExpLabel", "NEXT EXP 3600", 16, TextAnchor.MiddleLeft, FontStyle.Normal);
            nextExpLabel.color = FcColorTokens.Brand.TextMuted;
            Stretch(nextExpLabel.rectTransform, new Vector2(0.06f, 0.76f), new Vector2(0.94f, 0.83f), Vector2.zero, Vector2.zero);

            var hudRing = CreateImage(column.transform, "HudRing", Color.white);
            ApplySprite(hudRing, ringSprite, Image.Type.Simple, false);
            hudRing.preserveAspect = true;
            Stretch(hudRing.rectTransform, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.72f), Vector2.zero, Vector2.zero);

            portrait = CreateImage(column.transform, "Portrait", Color.white);
            Stretch(portrait.rectTransform, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.75f), Vector2.zero, Vector2.zero);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            if (renPortrait != null)
            {
                portrait.sprite = renPortrait;
            }

            return column.rectTransform;
        }

        private static CharacterBuildPortraitChipView CreatePortraitChip(
            Transform parent,
            int index,
            Sprite faceSprite,
            Sprite frameNormal,
            Sprite frameSelected,
            Sprite frameLocked = null,
            Vector2? faceMin = null,
            Vector2? faceMax = null)
        {
            var go = new GameObject(
                $"PortraitChip_{index}",
                typeof(RectTransform),
                typeof(Button),
                typeof(CharacterBuildPortraitChipView));
            go.transform.SetParent(parent, false);
            if (parent.GetComponent<HorizontalLayoutGroup>() != null)
            {
                var le = go.AddComponent<LayoutElement>();
                var side = ResolveChipSide(parent);
                le.minWidth = side;
                le.preferredWidth = side;
                le.flexibleWidth = 0f;
                le.flexibleHeight = 1f;
            }

            var face = CreateImage(go.transform, "Face", Color.white);
            Stretch(
                face.rectTransform,
                faceMin ?? new Vector2(0.18f, 0.12f),
                faceMax ?? new Vector2(0.82f, 0.88f),
                Vector2.zero,
                Vector2.zero);
            face.preserveAspect = true;
            face.raycastTarget = false;
            if (faceSprite != null)
            {
                ApplySprite(face, faceSprite, Image.Type.Simple, false);
                face.preserveAspect = true;
            }

            var frame = CreateImage(go.transform, "Frame", Color.white);
            ApplySprite(frame, frameNormal, Image.Type.Simple, true);
            frame.preserveAspect = true;
            StretchFull(frame.rectTransform);
            var button = go.GetComponent<Button>();
            button.targetGraphic = frame;

            var view = go.GetComponent<CharacterBuildPortraitChipView>();
            var so = new SerializedObject(view);
            so.FindProperty("frame").objectReferenceValue = frame;
            so.FindProperty("face").objectReferenceValue = face;
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("frameNormal").objectReferenceValue = frameNormal;
            so.FindProperty("frameSelected").objectReferenceValue = frameSelected;
            so.FindProperty("frameLocked").objectReferenceValue = frameLocked;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static float ResolveChipSide(Transform parent)
        {
            var row = parent as RectTransform;
            if (row != null && row.rect.height > 1f)
            {
                return row.rect.height;
            }

            return 138f;
        }

        private static Button CreateChipLabeledButton(
            Transform parent,
            string name,
            string label,
            Sprite sprite,
            Sprite iconSprite,
            bool selected)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            ApplySprite(image, sprite, Image.Type.Sliced, true);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var icon = CreateImage(go.transform, "Icon", Color.white);
            ApplySprite(icon, iconSprite, Image.Type.Simple, false);
            icon.preserveAspect = true;
            Stretch(icon.rectTransform, new Vector2(0.06f, 0.18f), new Vector2(0.28f, 0.82f), Vector2.zero, Vector2.zero);
            var text = CreateText(
                go.transform,
                "Label",
                label,
                14,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);
            text.color = selected ? FcColorTokens.Brand.TextPrimary : FcColorTokens.Brand.TextMuted;
            Stretch(text.rectTransform, new Vector2(0.32f, 0f), new Vector2(0.96f, 1f), Vector2.zero, Vector2.zero);
            text.raycastTarget = false;
            return button;
        }

        private static Image CreateGlassPanel(Transform parent, string name, Sprite sprite)
        {
            var panel = CreateImage(parent, name, Color.white);
            ApplySprite(panel, sprite, Image.Type.Sliced, false);
            return panel;
        }

        private static CharacterBuildSkillRowView CreateSkillRow(Transform parent, int index, float yMax, Sprite slotSprite)
        {
            var rowGo = new GameObject(
                $"SkillRow_{index}",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(CharacterBuildSkillRowView));
            rowGo.transform.SetParent(parent, false);
            Stretch(
                rowGo.GetComponent<RectTransform>(),
                new Vector2(0.06f, yMax - 0.22f),
                new Vector2(0.94f, yMax),
                Vector2.zero,
                Vector2.zero);
            var bg = rowGo.GetComponent<Image>();
            ApplySprite(bg, slotSprite, Image.Type.Sliced, true);

            var goldMarker = new GameObject("GoldFrame", typeof(RectTransform));
            goldMarker.transform.SetParent(rowGo.transform, false);

            var outline = rowGo.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.84f, 0.2f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = true;
            outline.enabled = true;

            var icon = CreateImage(rowGo.transform, "Icon", Color.white);
            Stretch(icon.rectTransform, new Vector2(0.08f, 0.22f), new Vector2(0.36f, 0.78f), Vector2.zero, Vector2.zero);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var name = CreateText(rowGo.transform, "Name", "—", 20, TextAnchor.MiddleLeft, FontStyle.Normal);
            Stretch(name.rectTransform, new Vector2(0.40f, 0f), new Vector2(0.94f, 1f), Vector2.zero, Vector2.zero);

            var view = rowGo.GetComponent<CharacterBuildSkillRowView>();
            var so = new SerializedObject(view);
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("nameLabel").objectReferenceValue = name;
            so.FindProperty("goldFrame").objectReferenceValue = goldMarker;
            so.FindProperty("goldOutline").objectReferenceValue = outline;
            so.FindProperty("button").objectReferenceValue = rowGo.GetComponent<Button>();
            so.FindProperty("rowBackground").objectReferenceValue = bg;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static void SeedStatAttrRows(Transform stats)
        {
            if (stats == null || stats.Find("StatRow_Strength") != null)
            {
                return;
            }

            var trackSprite = LoadSprite(CrystalDir + "ui_stat_attr_slider_track_v2.png");
            var fillSprite = LoadSprite(CrystalDir + "ui_stat_attr_slider_fill_v2.png");
            var handleSprite = LoadSprite(CrystalDir + "ui_stat_attr_slider_handle_v2.png");
            CreateStatRow(
                stats, "StatRow_Strength", CharacterBuildStatKind.Strength, "STR", 0.76f, true,
                null, null, null, trackSprite, fillSprite, handleSprite,
                LoadSprite(IconDir + "ui_stat_icon_strength_v2.png"),
                LoadSprite(IconDir + "ui_stat_label_str_v2.png"));
            CreateStatRow(
                stats, "StatRow_Magic", CharacterBuildStatKind.Magic, "MA", 0.60f, true,
                null, null, null, trackSprite, fillSprite, handleSprite,
                LoadSprite(IconDir + "ui_stat_icon_magic_v2.png"),
                LoadSprite(IconDir + "ui_stat_label_ma_v2.png"));
            CreateStatRow(
                stats, "StatRow_Endurance", CharacterBuildStatKind.Endurance, "EN", 0.44f, true,
                null, null, null, trackSprite, fillSprite, handleSprite,
                LoadSprite(IconDir + "ui_stat_icon_endurance_v2.png"),
                LoadSprite(IconDir + "ui_stat_label_en_v2.png"));
            CreateStatRow(
                stats, "StatRow_HeartBeat", CharacterBuildStatKind.HeartBeat, "HB", 0.28f, true,
                null, null, null, trackSprite, fillSprite, handleSprite,
                LoadSprite(IconDir + "ui_stat_icon_heartbeat_v2.png"),
                LoadSprite(IconDir + "ui_stat_label_hb_v2.png"));
            CreateStatRow(
                stats, "StatRow_Luck", CharacterBuildStatKind.Luck, "LUCK", 0.12f, false,
                null, null, null, trackSprite, fillSprite, handleSprite,
                LoadSprite(IconDir + "ui_stat_icon_luck_v2.png"),
                LoadSprite(IconDir + "ui_stat_label_luck_v2.png"));
        }

        private static CharacterBuildStatRowView CreateStatRow(
            Transform parent,
            string objectName,
            CharacterBuildStatKind kind,
            string label,
            float yMax,
            bool allocatable,
            Sprite minusSprite,
            Sprite plusSprite,
            Sprite rowSprite,
            Sprite trackSprite,
            Sprite fillSprite,
            Sprite handleSprite,
            Sprite iconSprite,
            Sprite labelSprite)
        {
            var rowGo = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(CharacterBuildStatRowView));
            rowGo.transform.SetParent(parent, false);
            Stretch(
                rowGo.GetComponent<RectTransform>(),
                new Vector2(0.05f, yMax),
                new Vector2(0.95f, yMax + 0.14f),
                Vector2.zero,
                Vector2.zero);
            ApplySprite(rowGo.GetComponent<Image>(), rowSprite, Image.Type.Sliced, false);

            var icon = CreateImage(rowGo.transform, "Icon", Color.white);
            ApplySprite(icon, iconSprite, Image.Type.Simple, false);
            icon.preserveAspect = true;
            icon.color = CharacterBuildStatTheme.IconImageColor;
            Stretch(icon.rectTransform, new Vector2(0.02f, 0.48f), new Vector2(0.12f, 0.96f), Vector2.zero, Vector2.zero);

            var name = CreateText(rowGo.transform, "NameLabel", label, 18, TextAnchor.UpperLeft, FontStyle.Normal);
            UiFontCatalog.Apply(name, UiFontRole.Display, 18);
            name.color = CharacterBuildStatTheme.LabelColor;
            Stretch(name.rectTransform, new Vector2(0.14f, 0.58f), new Vector2(0.30f, 0.98f), Vector2.zero, Vector2.zero);

            var sub = CreateText(rowGo.transform, "SubLabel", CharacterBuildStatTheme.ForKind(kind).Subtitle, 11, TextAnchor.LowerLeft, FontStyle.Normal);
            UiFontCatalog.Apply(sub, UiFontRole.Body, 11);
            sub.color = CharacterBuildStatTheme.SubLabelColor;
            Stretch(sub.rectTransform, new Vector2(0.14f, 0.10f), new Vector2(0.48f, 0.54f), Vector2.zero, Vector2.zero);

            Image nameArt = null;
            if (labelSprite != null)
            {
                nameArt = CreateImage(rowGo.transform, "NameLabelArt", Color.white);
                ApplySprite(nameArt, labelSprite, Image.Type.Simple, false);
                nameArt.preserveAspect = true;
                Stretch(nameArt.rectTransform, new Vector2(0.14f, 0.50f), new Vector2(0.48f, 1f), Vector2.zero, Vector2.zero);
                nameArt.gameObject.SetActive(false);
            }

            var value = CreateText(rowGo.transform, "ValueLabel", "10", 16, TextAnchor.MiddleRight, FontStyle.Normal);
            UiFontCatalog.Apply(value, UiFontRole.Display, 16);
            value.color = CharacterBuildStatTheme.LabelColor;
            Stretch(value.rectTransform, new Vector2(0.88f, 0.45f), new Vector2(0.98f, 0.95f), Vector2.zero, Vector2.zero);

            var track = CreateImage(rowGo.transform, "BarTrack", Color.white);
            ApplySprite(track, trackSprite, Image.Type.Sliced, false);
            Stretch(track.rectTransform, new Vector2(0.02f, 0.08f), new Vector2(0.62f, 0.42f), Vector2.zero, Vector2.zero);

            var fill = CreateImage(track.transform, "BarFill", Color.white);
            ApplySprite(fill, fillSprite, Image.Type.Filled, false);
            StretchFull(fill.rectTransform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0.3f;
            fill.raycastTarget = false;

            Image handle = null;
            if (handleSprite != null)
            {
                handle = CreateImage(track.transform, "BarHandle", Color.white);
                ApplySprite(handle, handleSprite, Image.Type.Simple, false);
                handle.preserveAspect = true;
                handle.rectTransform.anchorMin = new Vector2(0.3f, 0.5f);
                handle.rectTransform.anchorMax = new Vector2(0.3f, 0.5f);
                handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                handle.rectTransform.sizeDelta = new Vector2(28f, 28f);
                handle.rectTransform.anchoredPosition = Vector2.zero;
                handle.color = CharacterBuildStatTheme.HandleTint;
                handle.raycastTarget = false;
            }

            var alloc = CreatePanel(rowGo.transform, "AllocControls");
            Stretch(alloc, new Vector2(0.66f, 0.08f), new Vector2(1f, 0.92f), Vector2.zero, Vector2.zero);
            alloc.gameObject.SetActive(allocatable);

            var minus = CreateIconButton(alloc, "MinusBtn", "-", minusSprite);
            Stretch(minus.GetComponent<RectTransform>(), new Vector2(0f, 0.10f), new Vector2(0.28f, 0.90f), Vector2.zero, Vector2.zero);

            var spent = CreateText(alloc, "SpentLabel", "0", 16, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(spent.rectTransform, new Vector2(0.30f, 0.10f), new Vector2(0.70f, 0.90f), Vector2.zero, Vector2.zero);

            var plus = CreateIconButton(alloc, "PlusBtn", "+", plusSprite);
            Stretch(plus.GetComponent<RectTransform>(), new Vector2(0.72f, 0.10f), new Vector2(1f, 0.90f), Vector2.zero, Vector2.zero);

            var view = rowGo.GetComponent<CharacterBuildStatRowView>();
            var so = new SerializedObject(view);
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("nameLabel").objectReferenceValue = name;
            so.FindProperty("subLabel").objectReferenceValue = sub;
            so.FindProperty("nameLabelArt").objectReferenceValue = nameArt;
            so.FindProperty("valueLabel").objectReferenceValue = value;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("barFill").objectReferenceValue = fill;
            so.FindProperty("barHandle").objectReferenceValue = handle;
            so.FindProperty("minusButton").objectReferenceValue = minus;
            so.FindProperty("spentLabel").objectReferenceValue = spent;
            so.FindProperty("plusButton").objectReferenceValue = plus;
            so.FindProperty("allocControlsRoot").objectReferenceValue = alloc.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static void EquipGridCell(
            int columns,
            int col,
            float yMin,
            float yMax,
            out Vector2 amin,
            out Vector2 amax)
        {
            const float inset = 0.07f;
            const float gap = 0.012f;
            var usable = 1f - inset * 2f;
            var w = (usable - gap * (columns - 1)) / columns;
            var x = inset + col * (w + gap);
            amin = new Vector2(x, yMin);
            amax = new Vector2(x + w, yMax);
        }

        private static void EquipPoolStackCell(int index, out Vector2 amin, out Vector2 amax)
        {
            var col = index / 5;
            var row = index % 5;
            const float yMax0 = 0.86f;
            const float rowH = 0.108f;
            const float gapY = 0.022f;
            const float leftMin = 0.06f;
            const float leftMax = 0.46f;
            const float rightMin = 0.54f;
            const float rightMax = 0.94f;
            var yMax = yMax0 - row * (rowH + gapY);
            var yMin = yMax - rowH;
            amin = col == 0 ? new Vector2(leftMin, yMin) : new Vector2(rightMin, yMin);
            amax = col == 0 ? new Vector2(leftMax, yMax) : new Vector2(rightMax, yMax);
        }

        private static CharacterBuildEquipSlotView CreateEquipSlotCell(
            Transform parent,
            string name,
            Vector2 amin,
            Vector2 amax,
            Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CharacterBuildEquipSlotView));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), amin, amax, Vector2.zero, Vector2.zero);
            var image = go.GetComponent<Image>();
            ApplySprite(image, sprite, Image.Type.Simple, true);
            image.preserveAspect = false;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var label = CreateText(go.transform, "Label", string.Empty, 26, TextAnchor.MiddleCenter, FontStyle.Normal);
            label.color = CharacterBuildStatTheme.LabelColor;
            UiFontCatalog.Apply(label, UiFontRole.Body, 26, FontStyle.Normal);
            Stretch(label.rectTransform, new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.8f), Vector2.zero, Vector2.zero);
            label.raycastTarget = false;

            var view = go.GetComponent<CharacterBuildEquipSlotView>();
            var so = new SerializedObject(view);
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("frame").objectReferenceValue = image;
            so.FindProperty("labelBackground").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static void EnsureEquipPoolCellStyle(CharacterBuildEquipSlotView view, Sprite unlockedBg)
        {
            if (view == null)
            {
                return;
            }

            var so = new SerializedObject(view);
            var frame = so.FindProperty("frame").objectReferenceValue as Image
                        ?? view.GetComponent<Image>();
            if (frame != null && unlockedBg != null)
            {
                ApplySprite(frame, unlockedBg, Image.Type.Simple, true);
                frame.preserveAspect = false;
            }

            var label = so.FindProperty("label").objectReferenceValue as Text
                        ?? view.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                so.FindProperty("label").objectReferenceValue = label;
            }

            var labelBg = so.FindProperty("labelBackground").objectReferenceValue as Image
                          ?? view.transform.Find("LabelBg")?.GetComponent<Image>();
            if (labelBg != null)
            {
                labelBg.enabled = false;
            }

            so.FindProperty("labelBackground").objectReferenceValue = labelBg;
            so.FindProperty("frame").objectReferenceValue = frame;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static CharacterBuildEquipSlotView CreateEquipSlot(Transform parent, string name, float yMax, Sprite rowSprite)
        {
            EquipGridCell(1, 0, yMax - 0.08f, yMax, out var amin, out var amax);
            return CreateEquipSlotCell(parent, name, amin, amax, rowSprite);
        }

        private static Button CreateIconButton(Transform parent, string name, string fallback, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
                image.preserveAspect = true;
            }
            else
            {
                image.color = new Color(0.1f, 0.18f, 0.3f, 0.95f);
            }

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            if (sprite == null)
            {
                var text = CreateText(go.transform, "Label", fallback, 18, TextAnchor.MiddleCenter, FontStyle.Bold);
                StretchFull(text.rectTransform);
            }

            return button;
        }

        private static Button CreatePromptButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var image = go.GetComponent<Image>();
            ApplySprite(image, sprite, Image.Type.Sliced, true);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            if (!string.IsNullOrEmpty(label))
            {
                var text = CreateText(go.transform, "Label", label, 16, TextAnchor.MiddleCenter, FontStyle.Bold);
                StretchFull(text.rectTransform);
            }

            return button;
        }

        private static void ApplySprite(Image image, Sprite sprite, Image.Type type, bool raycast)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = type;
            image.color = Color.white;
            image.raycastTarget = raycast;
            image.fillCenter = true;
            var so = new SerializedObject(image);
            so.FindProperty("m_Sprite").objectReferenceValue = sprite;
            so.FindProperty("m_RaycastTarget").boolValue = raycast;
            so.FindProperty("m_Enabled").boolValue = sprite != null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateSandboxCanvas(Vector2 referenceResolution)
        {
            var canvasGo = new GameObject(
                "BuildCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            return canvasGo;
        }

        private static void EnsureCamera()
        {
            EnsureCamera(new Color(0.02f, 0.04f, 0.1f, 1f));
        }

        private static void EnsureCamera(Color backgroundColor)
        {
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
            cam.orthographic = true;
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var es = new GameObject("EventSystem", typeof(EventSystem));
            CombatInputSetup.ApplyInputModule(es, destroyImmediate: true);
            es.transform.SetAsFirstSibling();
        }

        private static RectTransform CreatePanel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            TextAnchor anchor,
            FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            UiFontCatalog.Apply(text, UiFontRole.Body, fontSize);
            text.text = content;
            text.alignment = anchor;
            text.fontStyle = style;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
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

        private static Sprite LoadLargestSprite(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            Sprite best = null;
            var bestArea = -1f;
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
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

            return best ?? LoadSprite(path);
        }

        private static GameObject FindSkillEquipOverlay(Transform canvas)
        {
            if (canvas != null)
            {
                var underCanvas = canvas.Find("SkillEquipOverlay")?.gameObject;
                if (underCanvas != null)
                {
                    return underCanvas;
                }
            }

            return FindSkillEquipOverlayInScene(canvas != null ? canvas.gameObject.scene : default);
        }

        private static GameObject FindSkillEquipOverlayInScene(Scene scene)
        {
            if (!scene.IsValid())
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root.name == "SkillEquipOverlay")
                {
                    return root;
                }

                var nested = root.transform.Find("SkillEquipOverlay")?.gameObject;
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static GameObject FindStatDetailsOverlay(Transform canvas)
        {
            var scene = canvas != null ? canvas.gameObject.scene : default;
            var preview = FindStatDetailsPreviewRoot(scene);
            if (preview != null)
            {
                var underPreview = preview.transform.Find("StatDetailsOverlay")?.gameObject;
                if (underPreview != null)
                {
                    return underPreview;
                }
            }

            if (canvas != null)
            {
                var underCanvas = canvas.Find("StatDetailsOverlay")?.gameObject;
                if (underCanvas != null)
                {
                    return underCanvas;
                }
            }

            return FindStatDetailsOverlayInScene(scene);
        }

        private static GameObject FindStatDetailsOverlayInScene(Scene scene)
        {
            if (!scene.IsValid())
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root.name == "StatDetailsOverlay")
                {
                    return root;
                }

                var nested = root.transform.Find("StatDetailsOverlay")?.gameObject;
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static GameObject FindStatDetailsPreviewRoot(Scene scene)
        {
            if (!scene.IsValid())
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == StatDetailsPreviewRootName)
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static GameObject EnsureStatDetailsEditPreview(Canvas buildCanvas)
        {
            if (buildCanvas == null)
            {
                return null;
            }

            var scene = buildCanvas.gameObject.scene;
            var preview = FindStatDetailsPreviewRoot(scene);
            if (preview == null)
            {
                preview = new GameObject(
                    StatDetailsPreviewRootName,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(preview, "Create StatDetails Edit Preview");
                var canvas = preview.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = buildCanvas.worldCamera;
                canvas.planeDistance = buildCanvas.planeDistance;
                canvas.sortingOrder = StatDetailsPreviewSortingOrder;
                var scaler = preview.GetComponent<CanvasScaler>();
                var sourceScaler = buildCanvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = sourceScaler != null
                    ? sourceScaler.referenceResolution
                    : new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = sourceScaler != null ? sourceScaler.matchWidthOrHeight : 0.5f;
            }

            var dimTf = preview.transform.Find(SkillEquipDimName);
            if (dimTf == null)
            {
                var dimGo = new GameObject(
                    SkillEquipDimName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));
                dimGo.transform.SetParent(preview.transform, false);
                StretchFull(dimGo.GetComponent<RectTransform>());
                var img = dimGo.GetComponent<Image>();
                img.color = new Color(0.05f, 0.04f, 0.12f, 0.72f);
                img.raycastTarget = true;
                var dimBtn = dimGo.GetComponent<Button>();
                dimBtn.targetGraphic = img;
                dimBtn.transition = Selectable.Transition.None;
            }

            return preview;
        }

        private static GameObject FindSkillEquipPreviewRoot(Scene scene)
        {
            if (!scene.IsValid())
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == SkillEquipPreviewRootName)
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static GameObject EnsureSkillEquipEditPreview(Canvas buildCanvas)
        {
            if (buildCanvas == null)
            {
                return null;
            }

            var scene = buildCanvas.gameObject.scene;
            var preview = FindSkillEquipPreviewRoot(scene);
            if (preview == null)
            {
                preview = new GameObject(
                    SkillEquipPreviewRootName,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(preview, "Create SkillEquip Edit Preview");
                var canvas = preview.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = buildCanvas.worldCamera;
                canvas.planeDistance = buildCanvas.planeDistance;
                canvas.sortingOrder = SkillEquipPreviewSortingOrder;
                var scaler = preview.GetComponent<CanvasScaler>();
                var sourceScaler = buildCanvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = sourceScaler != null
                    ? sourceScaler.referenceResolution
                    : new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = sourceScaler != null ? sourceScaler.matchWidthOrHeight : 0.5f;
            }

            var dimTf = preview.transform.Find(SkillEquipDimName);
            if (dimTf == null)
            {
                var dimGo = new GameObject(
                    SkillEquipDimName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                dimGo.transform.SetParent(preview.transform, false);
                StretchFull(dimGo.GetComponent<RectTransform>());

                var img = dimGo.GetComponent<Image>();
                img.color = new Color(0.05f, 0.04f, 0.12f, 0.72f);
                img.raycastTarget = true;
            }

            return preview;
        }

        private static void StretchFull(RectTransform rect)
        {
            Stretch(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
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

        private static string[] ResolveUnlockedFacePaths(SandboxHeaderMock mock)
        {
            if (mock.unlockedFacePaths != null && mock.unlockedFacePaths.Length > 0)
            {
                return mock.unlockedFacePaths;
            }

            return new[] { RenChipPath, CharlotteChipPath, CodaChipPath };
        }

        private static SandboxHeaderMock LoadHeaderMock()
        {
            if (!File.Exists(HeaderMockPath))
            {
                throw new System.InvalidOperationException("Missing " + HeaderMockPath);
            }

            var mock = JsonUtility.FromJson<SandboxHeaderMock>(File.ReadAllText(HeaderMockPath));
            if (mock == null || mock.slotCount <= 0)
            {
                throw new System.InvalidOperationException("Invalid " + HeaderMockPath);
            }

            return mock;
        }

        private static Sprite[] LoadWaveformFrames(string hudDir, string prefix)
        {
            var folder = (hudDir ?? string.Empty).TrimEnd('/', '\\');
            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(prefix) || !AssetDatabase.IsValidFolder(folder))
            {
                return new Sprite[0];
            }

            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
            var list = new List<SpriteEntry>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var name = Path.GetFileNameWithoutExtension(path);
                if (!HasPrefixThenDigits(name, prefix))
                {
                    continue;
                }

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    continue;
                }

                list.Add(new SpriteEntry { name = name, sprite = sprite });
            }

            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            var frames = new Sprite[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                frames[i] = list[i].sprite;
            }

            return frames;
        }

        private static bool HasPrefixThenDigits(string name, string prefix)
        {
            if (string.IsNullOrEmpty(name) || !name.StartsWith(prefix) || name.Length <= prefix.Length)
            {
                return false;
            }

            return char.IsDigit(name[prefix.Length]);
        }

        private static Color ToColor(float[] rgba)
        {
            if (rgba == null || rgba.Length < 3)
            {
                throw new System.InvalidOperationException(HeaderMockPath + " missing background");
            }

            return new Color(rgba[0], rgba[1], rgba[2], rgba.Length > 3 ? rgba[3] : 1f);
        }

        private static Vector2 RequireVec2(float[] xy, string field)
        {
            if (xy == null || xy.Length < 2)
            {
                throw new System.InvalidOperationException(HeaderMockPath + " missing " + field);
            }

            return new Vector2(xy[0], xy[1]);
        }

        [System.Serializable]
        private sealed class SandboxHeaderMock
        {
            public int slotCount;
            public string[] unlockedFacePaths;
            public string frameOpen;
            public string frameLocked;
            public string hudDir;
            public string ringOuter;
            public string ringFallback;
            public string ringInner;
            public string ringStub;
            public string hudBar;
            public string waveformPrefix;
            public float[] background;
            public float[] referenceResolution;
            public float[] hudMin;
            public float[] hudMax;
            public float[] chipsMin;
            public float[] chipsMax;
            public float chipSpacing;
            public float[] chipFaceMin;
            public float[] chipFaceMax;
            public float[] ringMin;
            public float[] ringMax;
            public float[] waveMin;
            public float[] waveMax;
            public float[] barMin;
            public float[] barMax;
            public float ringOuterDegreesPerSecond;
            public float ringInnerDegreesPerSecond;
            public float ringOuterPulse;
            public float ringInnerPulse;
            public float ringGlitchEverySeconds;
            public float ringAlpha;
            public float waveformFps;
            public bool waveformCrossfade;
        }

        private struct SpriteEntry
        {
            public string name;
            public Sprite sprite;
        }
    }
}
#endif

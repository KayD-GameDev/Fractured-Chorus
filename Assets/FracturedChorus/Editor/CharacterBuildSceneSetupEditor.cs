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
        public const string ProductionScenePath =
            "Assets/FracturedChorus/Scenes/CharacterBuild.unity";
        private const string ScenePath = ProductionScenePath;
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
            AssignStatDetailsRefs(so, canvasGo.transform);
            so.FindProperty("seedUnspentWhenEmpty").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(menu);
        }

        private static void AssignStatDetailsRefs(SerializedObject menu, Transform canvas)
        {
            var overlay = FindStatDetailsOverlay(canvas);
            var detailsButton = canvas != null ? canvas.Find("DetailsPanel")?.GetComponent<Button>() : null;
            var body = overlay != null
                ? overlay.transform.Find("Body") ?? overlay.transform.Find("Panel/Body")
                : null;
            menu.FindProperty("detailsButton").objectReferenceValue = detailsButton;
            menu.FindProperty("statDetailsOverlay").objectReferenceValue = overlay;
            menu.FindProperty("statDetailsHost").objectReferenceValue =
                overlay != null && overlay.transform.parent != null
                    ? overlay.transform.parent.gameObject
                    : null;
            menu.FindProperty("statDetailsDimmer").objectReferenceValue =
                overlay != null ? overlay.transform.parent?.Find("L00_Dim")?.gameObject : null;
            menu.FindProperty("statDetailsBodyLabel").objectReferenceValue =
                body != null ? body.GetComponent<Text>() : null;
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

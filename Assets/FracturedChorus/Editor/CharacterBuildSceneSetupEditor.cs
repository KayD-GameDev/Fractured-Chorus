#if UNITY_EDITOR
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Hub.CharacterBuild;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    /// <summary>
    /// Creates CharacterBuild.unity with a full hierarchy and wires SerializeFields — no runtime BuildHierarchy.
    /// Menu: Fractured Chorus → Create CharacterBuild Scene
    /// </summary>
    public static class CharacterBuildSceneSetupEditor
    {
        private const string ScenePath = "Assets/FracturedChorus/Scenes/CharacterBuild.unity";
        private const string BgPath = "Assets/FracturedChorus/Art/UI/StatusMenu/statusmenu_ren_bg_v6.png";
        private const string RenPortraitPath =
            "Assets/FracturedChorus/Art/UI/StatusMenu/ren_hima_uniform_menu_fullbody_v1.png";
        private const string SlashPath = "Assets/FracturedChorus/Art/UI/StatusMenu/statusmenu_slash_accent.png";

        [MenuItem("Fractured Chorus/Create CharacterBuild Scene")]
        public static void CreateScene()
        {
            EnsureFolder("Assets/FracturedChorus/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildHierarchy();
            EnsureBuildSettings();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Fractured Chorus] Saved {ScenePath}. Open and Play to test Build UI.");
        }

        [MenuItem("Fractured Chorus/Heal CharacterBuild Scene Hierarchy")]
        public static void HealScene()
        {
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
            Debug.Log("[Fractured Chorus] Healed CharacterBuild hierarchy + bindings.");
        }

        private static void BuildHierarchy()
        {
            EnsureCamera();
            EnsureEventSystem();

            var canvasGo = new GameObject("BuildCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var menu = canvasGo.AddComponent<CharacterBuildMenuUI>();
            var renPortrait = AssetDatabase.LoadAssetAtPath<Sprite>(RenPortraitPath);
            var slashSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SlashPath);

            var bg = CreateImage(canvasGo.transform, "Background", Color.white);
            StretchFull(bg.rectTransform);
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BgPath);
            if (bgSprite != null)
            {
                bg.sprite = bgSprite;
                bg.preserveAspect = false;
            }
            else
            {
                bg.color = FcColorTokens.Surface.Dim;
            }

            var vignette = CreateImage(canvasGo.transform, "LeftVignette",
                FcColorTokens.WithAlpha(new Color(0.02f, 0.04f, 0.12f), 0.55f));
            Stretch(vignette.rectTransform, new Vector2(0f, 0f), new Vector2(0.42f, 1f), Vector2.zero, Vector2.zero);

            var portrait = CreateImage(canvasGo.transform, "Portrait", Color.white);
            Stretch(portrait.rectTransform, new Vector2(0.46f, -0.04f), new Vector2(1.02f, 1.04f), Vector2.zero, Vector2.zero);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            if (renPortrait != null)
            {
                portrait.sprite = renPortrait;
            }

            var header = CreatePanel(canvasGo.transform, "Header");
            Stretch(header, new Vector2(0.045f, 0.74f), new Vector2(0.48f, 0.96f), Vector2.zero, Vector2.zero);

            var nameLabel = CreateText(header, "NameLabel", "Ren Takahashi", 48, TextAnchor.LowerLeft, FontStyle.Bold);
            Stretch(nameLabel.rectTransform, new Vector2(0f, 0.52f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            nameLabel.color = FcColorTokens.Brand.TextPrimary;

            var elementLabel = CreateText(header, "ElementLabel", "Melody", 20, TextAnchor.MiddleLeft, FontStyle.Bold);
            elementLabel.color = FcColorTokens.Brand.Cyan;
            Stretch(elementLabel.rectTransform, new Vector2(0f, 0.34f), new Vector2(0.28f, 0.54f), Vector2.zero, Vector2.zero);

            var levelLabel = CreateText(header, "LevelLabel", "Lv 15", 30, TextAnchor.MiddleLeft, FontStyle.Bold);
            Stretch(levelLabel.rectTransform, new Vector2(0f, 0.14f), new Vector2(0.22f, 0.36f), Vector2.zero, Vector2.zero);

            var nextExpLabel = CreateText(header, "NextExpLabel", "NEXT EXP 3600", 18, TextAnchor.MiddleLeft, FontStyle.Normal);
            nextExpLabel.color = FcColorTokens.Brand.TextMuted;
            Stretch(nextExpLabel.rectTransform, new Vector2(0.22f, 0.14f), new Vector2(0.72f, 0.36f), Vector2.zero, Vector2.zero);

            var elementRow = CreatePanel(header, "ElementIconRow");
            Stretch(elementRow, new Vector2(0f, 0f), new Vector2(0.7f, 0.14f), Vector2.zero, Vector2.zero);
            var elementIcons = new Image[3];
            var elementRings = new GameObject[3];
            var elementColors = new[]
            {
                FcColorTokens.Semantic.ElementRhythm,
                FcColorTokens.Semantic.ElementMelody,
                FcColorTokens.Semantic.ElementHarmony
            };
            for (var i = 0; i < 3; i++)
            {
                var icon = CreateImage(elementRow, $"ElementIcon_{i}", elementColors[i]);
                Stretch(icon.rectTransform, new Vector2(i * 0.14f, 0f), new Vector2(i * 0.14f + 0.12f, 1f), Vector2.zero, Vector2.zero);
                elementIcons[i] = icon;

                var ring = CreateImage(icon.transform, "HighlightRing", FcColorTokens.Semantic.EventGold);
                StretchFull(ring.rectTransform);
                ring.raycastTarget = false;
                ring.transform.SetAsFirstSibling();
                var ringRect = ring.rectTransform;
                ringRect.offsetMin = new Vector2(-5f, -5f);
                ringRect.offsetMax = new Vector2(5f, 5f);
                ring.gameObject.SetActive(false);
                elementRings[i] = ring.gameObject;
            }

            var prevBtn = CreatePromptButton(canvasGo.transform, "NavPrev", "[Q] <<",
                new Vector2(0.012f, 0.46f), new Vector2(0.075f, 0.56f));
            var nextBtn = CreatePromptButton(canvasGo.transform, "NavNext", "[E] >>",
                new Vector2(0.40f, 0.48f), new Vector2(0.465f, 0.58f));

            var skillsPanel = CreateImage(canvasGo.transform, "SkillsPanel",
                FcColorTokens.WithAlpha(FcColorTokens.Surface.Panel, 0.88f));
            Stretch(skillsPanel.rectTransform, new Vector2(0.035f, 0.10f), new Vector2(0.30f, 0.58f), Vector2.zero, Vector2.zero);
            AddPanelChrome(skillsPanel);

            var skillsTitle = CreateText(skillsPanel.transform, "SkillsTitle", "SKILLS", 24, TextAnchor.MiddleLeft, FontStyle.Bold);
            skillsTitle.color = FcColorTokens.Brand.Cyan;
            Stretch(skillsTitle.rectTransform, new Vector2(0.06f, 0.88f), new Vector2(0.7f, 0.98f), Vector2.zero, Vector2.zero);

            var skillsUnderline = CreateImage(skillsPanel.transform, "SkillsUnderline", FcColorTokens.Brand.Cyan);
            Stretch(skillsUnderline.rectTransform, new Vector2(0.06f, 0.86f), new Vector2(0.42f, 0.875f), Vector2.zero, Vector2.zero);
            if (slashSprite != null)
            {
                var slash = CreateImage(skillsPanel.transform, "SkillsSlash", Color.white);
                slash.sprite = slashSprite;
                slash.preserveAspect = true;
                Stretch(slash.rectTransform, new Vector2(0.55f, 0.86f), new Vector2(0.72f, 0.98f), Vector2.zero, Vector2.zero);
            }

            var skillRows = new CharacterBuildSkillRowView[5];
            for (var i = 0; i < 5; i++)
            {
                skillRows[i] = CreateSkillRow(skillsPanel.transform, i, 0.74f - i * 0.14f);
            }

            var remainingRoot = CreateImage(canvasGo.transform, "RemainingPointsRoot",
                FcColorTokens.WithAlpha(FcColorTokens.Surface.Modal, 0.9f));
            Stretch(remainingRoot.rectTransform, new Vector2(0.32f, 0.40f), new Vector2(0.58f, 0.47f), Vector2.zero, Vector2.zero);
            AddPanelChrome(remainingRoot);
            var remaining = CreateText(remainingRoot.transform, "RemainingPoints", "Remaining Points: 0", 20,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            remaining.color = FcColorTokens.Semantic.EventGold;
            StretchFull(remaining.rectTransform);

            var statsPanel = CreateImage(canvasGo.transform, "StatsPanel",
                FcColorTokens.WithAlpha(FcColorTokens.Surface.Panel, 0.9f));
            Stretch(statsPanel.rectTransform, new Vector2(0.32f, 0.07f), new Vector2(0.60f, 0.39f), Vector2.zero, Vector2.zero);
            AddPanelChrome(statsPanel);

            var strengthRow = CreateStatRow(statsPanel.transform, "StatRow_Strength", CharacterBuildStatKind.Strength, "Strength", 0.78f, true);
            var magicRow = CreateStatRow(statsPanel.transform, "StatRow_Magic", CharacterBuildStatKind.Magic, "Magic", 0.60f, true);
            var enduranceRow = CreateStatRow(statsPanel.transform, "StatRow_Endurance", CharacterBuildStatKind.Endurance, "Endurance", 0.42f, true);
            var heartBeatRow = CreateStatRow(statsPanel.transform, "StatRow_HeartBeat", CharacterBuildStatKind.HeartBeat, "HeartBeat", 0.24f, true);
            var luckRow = CreateStatRow(statsPanel.transform, "StatRow_Luck", CharacterBuildStatKind.Luck, "Luck", 0.06f, false);

            var backBtn = CreatePromptButton(canvasGo.transform, "FooterBack", "[Esc] Back",
                new Vector2(0.78f, 0.015f), new Vector2(0.88f, 0.065f));
            var viewSkillsBtn = CreatePromptButton(canvasGo.transform, "FooterViewSkills", "[V] View Skills",
                new Vector2(0.885f, 0.015f), new Vector2(0.99f, 0.065f));

            var overlay = CreateImage(canvasGo.transform, "SkillEquipOverlay",
                FcColorTokens.WithAlpha(FcColorTokens.Surface.Modal, 0.97f));
            Stretch(overlay.rectTransform, new Vector2(0.28f, 0.18f), new Vector2(0.72f, 0.82f), Vector2.zero, Vector2.zero);
            overlay.raycastTarget = true;
            overlay.gameObject.SetActive(false);
            AddPanelChrome(overlay);

            var equipTitle = CreateText(overlay.transform, "Title", "Skill Equip", 26, TextAnchor.MiddleCenter, FontStyle.Bold);
            equipTitle.color = FcColorTokens.Brand.Cyan;
            Stretch(equipTitle.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero);

            var slotRow = CreatePanel(overlay.transform, "SlotRow");
            Stretch(slotRow, new Vector2(0.06f, 0.55f), new Vector2(0.94f, 0.84f), Vector2.zero, Vector2.zero);
            ConfigureLayout(slotRow.gameObject.AddComponent<VerticalLayoutGroup>());

            var poolRow = CreatePanel(overlay.transform, "PoolRow");
            Stretch(poolRow, new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.52f), Vector2.zero, Vector2.zero);
            ConfigureLayout(poolRow.gameObject.AddComponent<VerticalLayoutGroup>());

            var equipClose = CreatePromptButton(overlay.transform, "CloseButton", "Close",
                new Vector2(0.35f, 0.03f), new Vector2(0.65f, 0.11f));

            var so = new SerializedObject(menu);
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

            so.FindProperty("prevButton").objectReferenceValue = prevBtn;
            so.FindProperty("nextButton").objectReferenceValue = nextBtn;

            var rowsProp = so.FindProperty("skillRows");
            rowsProp.arraySize = 5;
            for (var i = 0; i < 5; i++)
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
            so.FindProperty("renMenuPortrait").objectReferenceValue = renPortrait;
            so.FindProperty("backButton").objectReferenceValue = backBtn;
            so.FindProperty("viewSkillsButton").objectReferenceValue = viewSkillsBtn;
            so.FindProperty("skillEquipOverlay").objectReferenceValue = overlay.gameObject;
            so.FindProperty("skillEquipTitleLabel").objectReferenceValue = equipTitle;
            so.FindProperty("skillEquipSlotRow").objectReferenceValue = slotRow;
            so.FindProperty("skillEquipPoolRow").objectReferenceValue = poolRow;
            so.FindProperty("skillEquipCloseButton").objectReferenceValue = equipClose;
            so.FindProperty("seedUnspentWhenEmpty").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(menu);
        }

        private static void AddPanelChrome(Image panel)
        {
            if (panel == null)
            {
                return;
            }

            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = FcColorTokens.WithAlpha(FcColorTokens.Brand.Cyan, 0.55f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;
        }

        private static CharacterBuildSkillRowView CreateSkillRow(Transform parent, int index, float yMax)
        {
            var rowGo = new GameObject($"SkillRow_{index}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CharacterBuildSkillRowView));
            rowGo.transform.SetParent(parent, false);
            Stretch(rowGo.GetComponent<RectTransform>(), new Vector2(0.04f, yMax - 0.12f), new Vector2(0.96f, yMax), Vector2.zero, Vector2.zero);
            var bg = rowGo.GetComponent<Image>();
            bg.color = FcColorTokens.Surface.Row;

            var goldMarker = new GameObject("GoldFrame", typeof(RectTransform));
            goldMarker.transform.SetParent(rowGo.transform, false);
            goldMarker.SetActive(index < 3);

            var outline = rowGo.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.84f, 0.2f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = true;
            outline.enabled = index < 3;

            var icon = CreateImage(rowGo.transform, "Icon", FcColorTokens.Brand.CyanSoft);
            Stretch(icon.rectTransform, new Vector2(0.03f, 0.15f), new Vector2(0.18f, 0.85f), Vector2.zero, Vector2.zero);
            icon.raycastTarget = false;

            var name = CreateText(rowGo.transform, "Name", "—", 18, TextAnchor.MiddleLeft, FontStyle.Normal);
            Stretch(name.rectTransform, new Vector2(0.22f, 0f), new Vector2(0.95f, 1f), Vector2.zero, Vector2.zero);

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

        private static CharacterBuildStatRowView CreateStatRow(
            Transform parent,
            string objectName,
            CharacterBuildStatKind kind,
            string label,
            float yMax,
            bool allocatable)
        {
            var rowGo = new GameObject(objectName, typeof(RectTransform), typeof(CharacterBuildStatRowView));
            rowGo.transform.SetParent(parent, false);
            Stretch(rowGo.GetComponent<RectTransform>(), new Vector2(0.03f, yMax), new Vector2(0.97f, yMax + 0.16f), Vector2.zero, Vector2.zero);

            var name = CreateText(rowGo.transform, "NameLabel", label, 15, TextAnchor.MiddleLeft, FontStyle.Bold);
            Stretch(name.rectTransform, new Vector2(0f, 0.48f), new Vector2(0.34f, 1f), Vector2.zero, Vector2.zero);

            var value = CreateText(rowGo.transform, "ValueLabel", "0", 15, TextAnchor.MiddleLeft, FontStyle.Normal);
            Stretch(value.rectTransform, new Vector2(0.34f, 0.48f), new Vector2(0.48f, 1f), Vector2.zero, Vector2.zero);

            var track = CreateImage(rowGo.transform, "BarTrack", FcColorTokens.Surface.Track);
            Stretch(track.rectTransform, new Vector2(0f, 0.08f), new Vector2(0.52f, 0.42f), Vector2.zero, Vector2.zero);

            var fill = CreateImage(track.transform, "BarFill", FcColorTokens.Brand.CyanSoft);
            StretchFull(fill.rectTransform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0.3f;
            fill.raycastTarget = false;

            var alloc = CreatePanel(rowGo.transform, "AllocControls");
            Stretch(alloc, new Vector2(0.56f, 0.08f), new Vector2(1f, 0.92f), Vector2.zero, Vector2.zero);
            alloc.gameObject.SetActive(allocatable);

            var minus = CreateSmallButton(alloc, "MinusBtn", "-");
            Stretch(minus.GetComponent<RectTransform>(), new Vector2(0f, 0.15f), new Vector2(0.28f, 0.85f), Vector2.zero, Vector2.zero);

            var spent = CreateText(alloc, "SpentLabel", "0", 16, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(spent.rectTransform, new Vector2(0.3f, 0.15f), new Vector2(0.68f, 0.85f), Vector2.zero, Vector2.zero);

            var plus = CreateSmallButton(alloc, "PlusBtn", "+");
            Stretch(plus.GetComponent<RectTransform>(), new Vector2(0.72f, 0.15f), new Vector2(1f, 0.85f), Vector2.zero, Vector2.zero);

            var view = rowGo.GetComponent<CharacterBuildStatRowView>();
            var so = new SerializedObject(view);
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("nameLabel").objectReferenceValue = name;
            so.FindProperty("valueLabel").objectReferenceValue = value;
            so.FindProperty("barFill").objectReferenceValue = fill;
            so.FindProperty("minusButton").objectReferenceValue = minus;
            so.FindProperty("spentLabel").objectReferenceValue = spent;
            so.FindProperty("plusButton").objectReferenceValue = plus;
            so.FindProperty("allocControlsRoot").objectReferenceValue = alloc.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static Button CreateSmallButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.1f, 0.18f, 0.3f, 0.95f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var text = CreateText(go.transform, "Label", label, 18, TextAnchor.MiddleCenter, FontStyle.Bold);
            StretchFull(text.rectTransform);
            return button;
        }

        private static Button CreatePromptButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.08f, 0.12f, 0.22f, 0.9f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var text = CreateText(go.transform, "Label", label, 16, TextAnchor.MiddleCenter, FontStyle.Bold);
            StretchFull(text.rectTransform);
            return button;
        }

        private static void EnsureCamera()
        {
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.04f, 0.1f, 1f);
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

        private static void EnsureBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes)
            {
                if (s.path == ScenePath)
                {
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ConfigureLayout(VerticalLayoutGroup layout)
        {
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(8, 8, 8, 8);
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

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor, FontStyle style)
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

        private static void StretchFull(RectTransform rect)
        {
            Stretch(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
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
    }
}
#endif

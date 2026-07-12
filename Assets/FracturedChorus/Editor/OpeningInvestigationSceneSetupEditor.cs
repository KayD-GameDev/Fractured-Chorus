#if UNITY_EDITOR
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Narrative;
using FracturedChorus.Narrative.Vn;
using FracturedChorus.RunMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class OpeningInvestigationSceneSetupEditor
    {
        private const string ScenePath = "Assets/FracturedChorus/Scenes/OpeningInvestigation.unity";
        private const string DialogueFramePath =
            "Assets/FracturedChorus/Art/UI/Narrative/DialogueBox_Frame_LightBlueHolo_v1.png";
        private const string NightBgPath =
            "Assets/FracturedChorus/Art/Backgrounds/lumina_street_night_rain_v1.png";
        private const string CatalogPath =
            "Assets/FracturedChorus/Data/ScriptableObjects/Narrative/VnSpeakerCatalog.asset";
        private const string TypingPath = "Assets/FracturedChorus/Audio/SFX/Prologue_Typing.mp3";
        private const string StreetBgPath =
            "Assets/FracturedChorus/Art/Backgrounds/lumina_street_night_rain_v1.png";
        private const string AlleyBgPath =
            "Assets/FracturedChorus/Art/Backgrounds/lumina_alley_night_rain_v1.png";
        private const string HarutoBodyBgPath =
            "Assets/FracturedChorus/Art/Backgrounds/lumina_alley_haruto_body_close_v1.png";
        private const string RainPath = "Assets/FracturedChorus/Audio/SFX/Ambience_Rain_Loop.mp3";
        private const string BringMeHomePath = "Assets/FracturedChorus/Audio/Music/Bring_Me_Home.mp3";
        private const string EternalSparkPath = "Assets/FracturedChorus/Audio/Music/EternalSpark.mp3";

        [MenuItem("Fractured Chorus/Narrative/Populate Opening Investigation Script")]
        public static void PopulateScriptAsset()
        {
            EnsureFolder("Assets/FracturedChorus/Narrative");
            EnsureFolder("Assets/FracturedChorus/Narrative/Scripts");

            var script = AssetDatabase.LoadAssetAtPath<VnScriptSO>(OpeningInvestigationScriptBuilder.ScriptAssetPath);
            if (script == null)
            {
                script = ScriptableObject.CreateInstance<VnScriptSO>();
                AssetDatabase.CreateAsset(script, OpeningInvestigationScriptBuilder.ScriptAssetPath);
            }

            OpeningInvestigationScriptBuilder.ApplyTo(script);
            EditorUtility.SetDirty(script);
            AssetDatabase.SaveAssets();
            Selection.activeObject = script;
            Debug.Log($"[Fractured Chorus] Populated {OpeningInvestigationScriptBuilder.ScriptAssetPath} ({script.beats.Length} beats).");
        }

        [MenuItem("Fractured Chorus/Narrative/Heal OpeningInvestigation Dialogue UI")]
        public static void HealOpeningDialogueUi()
        {
            PopulateScriptAsset();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var runtime = Object.FindFirstObjectByType<VnRuntimeController>();
            if (runtime == null)
            {
                Debug.LogError("[Fractured Chorus] VnRuntimeController missing in OpeningInvestigation.");
                return;
            }

            var typing = AssetDatabase.LoadAssetAtPath<AudioClip>(TypingPath);
            SetSerializedField(runtime, "typingClip", typing);

            var typewriter = runtime.GetComponentInChildren<PrologueTypewriterView>(true);
            if (typewriter != null && typing != null)
            {
                typewriter.BindTypingClip(typing);
                SetSerializedField(typewriter, "typingClip", typing);
            }

            var cueResolver = Object.FindFirstObjectByType<VnCueResolver>();
            if (cueResolver != null)
            {
                BindOpeningBackgroundCues(cueResolver);
            }

            var bg = GameObject.Find("Background")?.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = LoadSprite(StreetBgPath);
                bg.preserveAspect = false;
                bg.color = Color.white;
            }

            var dialogueFrame = GameObject.Find("DialogueFrame")?.GetComponent<Image>();
            if (dialogueFrame != null)
            {
                dialogueFrame.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DialogueFramePath);
                dialogueFrame.type = Image.Type.Sliced;
                dialogueFrame.preserveAspect = false;
                dialogueFrame.fillCenter = true;
                dialogueFrame.raycastTarget = false;
            }

            var dialoguePanel = GameObject.Find("DialoguePanel");
            if (dialoguePanel != null)
            {
                StretchRect(dialoguePanel, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.36f), Vector2.zero, Vector2.zero);
                dialoguePanel.transform.SetAsLastSibling();
            }

            ApplyPortraitLayoutInScene();

            var nameplate = GameObject.Find("Nameplate")?.GetComponent<Text>();
            var body = GameObject.Find("DialogueBody")?.GetComponent<Text>();
            var textCard = GameObject.Find("TextCardBody")?.GetComponent<Text>();
            VnUiFont.Apply(nameplate, 26, FontStyle.Bold);
            VnUiFont.Apply(body, 30, FontStyle.Normal);
            VnUiFont.Apply(textCard, 40, FontStyle.Normal);

            if (nameplate != null)
            {
                nameplate.alignment = TextAnchor.MiddleCenter;
                nameplate.raycastTarget = false;
                StretchRect(nameplate.gameObject, new Vector2(0.05f, 0.80f), new Vector2(0.24f, 0.96f), Vector2.zero, Vector2.zero);
            }

            if (body != null)
            {
                body.raycastTarget = false;
                StretchRect(body.gameObject, new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.72f), Vector2.zero, Vector2.zero);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Fractured Chorus] Healed OpeningInvestigation UI, BG cues, portraits, and script.");
        }

        private static void BindOpeningBackgroundCues(VnCueResolver cueResolver)
        {
            var street = LoadSprite(StreetBgPath);
            var alley = LoadSprite(AlleyBgPath);
            var harutoBody = LoadSprite(HarutoBodyBgPath);
            var rain = AssetDatabase.LoadAssetAtPath<AudioClip>(RainPath);
            var bringMeHome = AssetDatabase.LoadAssetAtPath<AudioClip>(BringMeHomePath);
            var eternalSpark = AssetDatabase.LoadAssetAtPath<AudioClip>(EternalSparkPath);
            var so = new SerializedObject(cueResolver);
            var entries = so.FindProperty("entries");
            entries.arraySize = 6;
            SetCueEntry(entries.GetArrayElementAtIndex(0), VnBgIds.LuminaStreetNight, street, null);
            SetCueEntry(entries.GetArrayElementAtIndex(1), VnBgIds.LuminaAlleyNight, alley, null);
            SetCueEntry(entries.GetArrayElementAtIndex(2), VnBgIds.LuminaAlleyHarutoBody, harutoBody, null);
            SetCueEntry(entries.GetArrayElementAtIndex(3), VnAudioIds.RainAmbience, null, rain);
            SetCueEntry(entries.GetArrayElementAtIndex(4), VnAudioIds.BringMeHome, null, bringMeHome);
            SetCueEntry(entries.GetArrayElementAtIndex(5), VnAudioIds.EternalSpark, null, eternalSpark);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cueResolver);
        }

        private static void SetCueEntry(SerializedProperty entry, string id, Sprite sprite, AudioClip clip)
        {
            entry.FindPropertyRelative("id").stringValue = id;
            entry.FindPropertyRelative("sprite").objectReferenceValue = sprite;
            entry.FindPropertyRelative("clip").objectReferenceValue = clip;
        }

        private static void ApplyPortraitLayoutInScene()
        {
            foreach (var view in Object.FindObjectsByType<VnDialoguePortraitView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(view);
                ApplySlotRect(so, "leftRoot", true);
                ApplySlotRect(so, "rightRoot", false);
                var legacy = so.FindProperty("leftRoot")?.objectReferenceValue as RectTransform;
                if (legacy != null)
                {
                    LayoutPortraitRect(legacy, true);
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(view);
            }

            var left = GameObject.Find("DialoguePortrait_Left")?.GetComponent<RectTransform>();
            var right = GameObject.Find("DialoguePortrait_Right")?.GetComponent<RectTransform>();
            var single = GameObject.Find("DialoguePortrait")?.GetComponent<RectTransform>();
            if (left != null)
            {
                LayoutPortraitRect(left, true);
            }

            if (right != null)
            {
                LayoutPortraitRect(right, false);
            }

            if (single != null)
            {
                LayoutPortraitRect(single, true);
            }
        }

        private static void ApplySlotRect(SerializedObject so, string propName, bool left)
        {
            var prop = so.FindProperty(propName);
            if (prop?.objectReferenceValue is RectTransform rect)
            {
                LayoutPortraitRect(rect, left);
            }
        }

        private static void LayoutPortraitRect(RectTransform rect, bool left)
        {
            if (rect == null)
            {
                return;
            }

            if (left)
            {
                rect.anchorMin = VnDialoguePortraitLayout.LeftAnchorMin;
                rect.anchorMax = VnDialoguePortraitLayout.LeftAnchorMax;
                rect.pivot = VnDialoguePortraitLayout.LeftPivot;
                rect.anchoredPosition = VnDialoguePortraitLayout.LeftAnchoredPosition;
            }
            else
            {
                rect.anchorMin = VnDialoguePortraitLayout.RightAnchorMin;
                rect.anchorMax = VnDialoguePortraitLayout.RightAnchorMax;
                rect.pivot = VnDialoguePortraitLayout.RightPivot;
                rect.anchoredPosition = VnDialoguePortraitLayout.RightAnchoredPosition;
            }

            rect.sizeDelta = VnDialoguePortraitLayout.SizeDelta;
            EditorUtility.SetDirty(rect);
        }

        [MenuItem("Fractured Chorus/Create OpeningInvestigation Scene")]
        public static void CreateScene()
        {
            PopulateScriptAsset();
            VnPortraitAssetSetupEditor.CreateSpeakerAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            ConfigureCamera();
            BuildHierarchy();
            EnsureBuildSettings();
            EnsureFolder("Assets/FracturedChorus/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Fractured Chorus] Saved {ScenePath}");
        }

        [MenuItem("Fractured Chorus/Setup OpeningInvestigation Scene Hierarchy")]
        public static void SetupHierarchy()
        {
            PopulateScriptAsset();
            VnPortraitAssetSetupEditor.CreateSpeakerAssets();

            var existing = GameObject.Find("OpeningInvestigationRoot");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog(
                        "Setup OpeningInvestigation",
                        "OpeningInvestigationRoot already exists. Delete and recreate hierarchy?",
                        "Recreate",
                        "Cancel"))
                {
                    return;
                }

                Object.DestroyImmediate(existing);
            }

            ConfigureCamera();
            BuildHierarchy();
            EnsureBuildSettings();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] OpeningInvestigation hierarchy created — Save scene (Ctrl+S).");
        }

        public static void BatchCreateOpeningInvestigationScene()
        {
            CreateScene();
            EditorApplication.Exit(0);
        }

        [InitializeOnLoadMethod]
        private static void TryAutoCreateFromFlag()
        {
            EditorApplication.delayCall += () =>
            {
                var flag = System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(Application.dataPath, "..", "Library", "vn_create_opening_scene.flag"));
                if (!System.IO.File.Exists(flag))
                {
                    return;
                }

                try
                {
                    System.IO.File.Delete(flag);
                    CreateScene();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Fractured Chorus] Auto create OpeningInvestigation failed: {ex}");
                }
            };
        }

        private static void ConfigureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            cam.orthographic = true;
            cam.backgroundColor = Color.black;
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        private static void BuildHierarchy()
        {
            CombatInputSetup.EnsureEventSystem();

            var root = new GameObject("OpeningInvestigationRoot");
            var runtime = root.AddComponent<VnRuntimeController>();
            var cueResolver = root.AddComponent<VnCueResolver>();
            var audioPlayer = root.AddComponent<VnAudioPlayer>();
            BindOpeningBackgroundCues(cueResolver);

            var canvasGo = new GameObject("OpeningCanvas");
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = CreateImage("Background", canvasGo.transform, LoadSprite(StreetBgPath), Color.white);
            StretchRect(bg.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bg.preserveAspect = false;

            var portraitParent = CreateUiObject("DialoguePortraits", canvasGo.transform);
            StretchRect(portraitParent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            RectTransform MakePortraitSlot(string name, bool left)
            {
                var slot = CreateUiObject(name, portraitParent.transform);
                var rect = slot.GetComponent<RectTransform>();
                if (left)
                {
                    rect.anchorMin = VnDialoguePortraitLayout.LeftAnchorMin;
                    rect.anchorMax = VnDialoguePortraitLayout.LeftAnchorMax;
                    rect.pivot = VnDialoguePortraitLayout.LeftPivot;
                    rect.anchoredPosition = VnDialoguePortraitLayout.LeftAnchoredPosition;
                }
                else
                {
                    rect.anchorMin = VnDialoguePortraitLayout.RightAnchorMin;
                    rect.anchorMax = VnDialoguePortraitLayout.RightAnchorMax;
                    rect.pivot = VnDialoguePortraitLayout.RightPivot;
                    rect.anchoredPosition = VnDialoguePortraitLayout.RightAnchoredPosition;
                }

                rect.sizeDelta = VnDialoguePortraitLayout.SizeDelta;
                var shadowImage = CreateImage("Shadow", slot.transform, null, VnDialoguePortraitLayout.DefaultShadowColor);
                StretchRect(shadowImage.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                shadowImage.preserveAspect = true;
                shadowImage.raycastTarget = false;
                shadowImage.rectTransform.anchoredPosition = VnDialoguePortraitLayout.DefaultShadowOffset;
                var portraitImage = CreateImage("Portrait", slot.transform, null, Color.white);
                StretchRect(portraitImage.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                portraitImage.preserveAspect = true;
                portraitImage.raycastTarget = false;
                slot.SetActive(false);
                return rect;
            }

            var leftRect = MakePortraitSlot("DialoguePortrait_Left", true);
            var rightRect = MakePortraitSlot("DialoguePortrait_Right", false);
            var portraitViewHost = portraitParent.AddComponent<VnDialoguePortraitView>();
            portraitViewHost.Bind(
                leftRect,
                leftRect.Find("Shadow")?.GetComponent<Image>(),
                leftRect.Find("Portrait")?.GetComponent<Image>(),
                rightRect,
                rightRect.Find("Shadow")?.GetComponent<Image>(),
                rightRect.Find("Portrait")?.GetComponent<Image>());
            var portraitView = portraitViewHost;

            var dialogueRoot = CreateUiObject("DialoguePanel", canvasGo.transform);
            StretchRect(dialogueRoot, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.36f), Vector2.zero, Vector2.zero);
            var dialogueGroup = dialogueRoot.AddComponent<CanvasGroup>();
            var dialogueFrame = CreateImage("DialogueFrame", dialogueRoot.transform, LoadSprite(DialogueFramePath), Color.white);
            StretchRect(dialogueFrame.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dialogueFrame.preserveAspect = false;
            dialogueFrame.type = Image.Type.Sliced;
            dialogueFrame.fillCenter = true;
            dialogueFrame.raycastTarget = false;

            var nameplateGo = CreateUiObject("Nameplate", dialogueRoot.transform);
            StretchRect(nameplateGo, new Vector2(0.05f, 0.82f), new Vector2(0.28f, 0.96f), Vector2.zero, Vector2.zero);
            var nameplateText = nameplateGo.AddComponent<Text>();
            VnUiFont.Apply(nameplateText, 26, FontStyle.Bold);
            nameplateText.alignment = TextAnchor.MiddleCenter;
            nameplateText.color = Color.white;
            nameplateText.raycastTarget = false;

            var bodyGo = CreateUiObject("DialogueBody", dialogueRoot.transform);
            StretchRect(bodyGo, new Vector2(0.07f, 0.12f), new Vector2(0.93f, 0.74f), Vector2.zero, Vector2.zero);
            var bodyText = bodyGo.AddComponent<Text>();
            VnUiFont.Apply(bodyText, 30, FontStyle.Normal);
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.color = new Color(0.92f, 0.97f, 1f, 1f);
            bodyText.raycastTarget = false;
            var typewriter = dialogueRoot.AddComponent<PrologueTypewriterView>();
            SetSerializedField(typewriter, "bodyText", bodyText);

            var textCardRoot = CreateUiObject("TextCardPanel", canvasGo.transform);
            StretchRect(textCardRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var textCardGroup = textCardRoot.AddComponent<CanvasGroup>();
            textCardGroup.alpha = 0f;
            textCardRoot.SetActive(false);
            var textCardBg = CreateImage("TextCardDim", textCardRoot.transform, null, new Color(0f, 0f, 0f, 0.72f));
            StretchRect(textCardBg.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var textCardBody = CreateText("TextCardBody", textCardRoot.transform, string.Empty, 40, TextAnchor.MiddleCenter);
            StretchRect(textCardBody.gameObject, new Vector2(0.15f, 0.35f), new Vector2(0.85f, 0.65f), Vector2.zero, Vector2.zero);
            textCardBody.color = new Color(0.9f, 0.95f, 1f, 1f);

            var fadeGo = CreateUiObject("FadeOverlay", canvasGo.transform);
            StretchRect(fadeGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fadeImage = fadeGo.AddComponent<Image>();
            fadeImage.color = Color.black;
            var fadeGroup = fadeGo.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;

            var script = AssetDatabase.LoadAssetAtPath<VnScriptSO>(OpeningInvestigationScriptBuilder.ScriptAssetPath);
            var catalog = AssetDatabase.LoadAssetAtPath<VnSpeakerCatalogSO>(CatalogPath);

            SetSerializedField(runtime, "script", script);
            SetSerializedField(runtime, "speakerCatalog", catalog);
            SetSerializedField(runtime, "cueResolver", cueResolver);
            SetSerializedField(runtime, "audioPlayer", audioPlayer);
            SetSerializedField(runtime, "portraitView", portraitView);
            SetSerializedField(runtime, "typewriter", typewriter);
            SetSerializedField(runtime, "nameplateText", nameplateText);
            SetSerializedField(runtime, "textCardBody", textCardBody);
            SetSerializedField(runtime, "dialoguePanel", dialogueGroup);
            SetSerializedField(runtime, "textCardPanel", textCardGroup);
            SetSerializedField(runtime, "fadeOverlay", fadeGroup);
            SetSerializedField(runtime, "backgroundImage", bg);
            SetSerializedField(runtime, "typingClip", AssetDatabase.LoadAssetAtPath<AudioClip>(TypingPath));
            SetSerializedField(runtime, "beginHubOnEnd", true);
            SetSerializedField(runtime, "playOnStart", true);
            SetSerializedField(audioPlayer, "cueResolver", cueResolver);
            if (typewriter != null)
            {
                SetSerializedField(typewriter, "typingClip", AssetDatabase.LoadAssetAtPath<AudioClip>(TypingPath));
            }
        }

        private static void EnsureBuildSettings()
        {
            var scenes = new[]
            {
                "Assets/FracturedChorus/Scenes/MainMenuStartGame.unity",
                "Assets/FracturedChorus/Scenes/PrologueVN.unity",
                ScenePath,
                "Assets/FracturedChorus/Scenes/CampusHub.unity",
                "Assets/FracturedChorus/Scenes/RunMapPrototype.unity",
                "Assets/FracturedChorus/Scenes/CombatPrototype.unity"
            };

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var path in scenes)
            {
                if (System.IO.File.Exists(path) || path == ScenePath)
                {
                    list.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            foreach (var existing in EditorBuildSettings.scenes)
            {
                if (list.Exists(s => s.path == existing.path))
                {
                    continue;
                }

                list.Add(existing);
            }

            EditorBuildSettings.scenes = list.ToArray();
        }

        private static Sprite LoadSprite(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                return sprite;
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var asset in assets)
            {
                if (asset is Sprite found)
                {
                    return found;
                }
            }

            return null;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = CreateUiObject(name, parent);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor anchor)
        {
            var go = CreateUiObject(name, parent);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.alignment = anchor;
            text.color = Color.white;
            VnUiFont.Apply(text, fontSize, FontStyle.Normal);
            return text;
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
                Debug.LogWarning($"[Fractured Chorus] Missing field {fieldName} on {target.name}");
                return;
            }

            switch (value)
            {
                case null:
                    prop.objectReferenceValue = null;
                    break;
                case Object obj:
                    prop.objectReferenceValue = obj;
                    break;
                case bool b:
                    prop.boolValue = b;
                    break;
                case string s:
                    prop.stringValue = s;
                    break;
                case float f:
                    prop.floatValue = f;
                    break;
                case int i:
                    prop.intValue = i;
                    break;
                default:
                    Debug.LogWarning($"[Fractured Chorus] Unsupported serialize type for {fieldName}");
                    break;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
#endif

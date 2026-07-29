#if UNITY_EDITOR
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Hub.FlowerWork;
using FracturedChorus.Narrative;
using FracturedChorus.Narrative.Vn;
using FracturedChorus.RunMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class FlowerShopWorkSceneSetupEditor
    {
        private const string ScenePath = "Assets/FracturedChorus/Scenes/FlowerShopWork.unity";
        private const string DialogueFramePath =
            "Assets/FracturedChorus/Art/UI/Narrative/DialogueBox_Frame_LightBlueHolo_v1.png";
        private const string CatalogPath =
            "Assets/FracturedChorus/Data/ScriptableObjects/Narrative/VnSpeakerCatalog.asset";
        private const string TypingPath = "Assets/FracturedChorus/Audio/SFX/Prologue_Typing.mp3";
        private const string FlowerArtRoot = "Assets/FracturedChorus/Art/Narrative/Events/FlowerShop/";
        private const string ScenarioResourcesPath = "Assets/FracturedChorus/Resources/FlowerWork";

        [MenuItem("Fractured Chorus/Hub/Create Flower Work Scenarios")]
        public static void CreateScenarioAssets()
        {
            EnsureFolder("Assets/FracturedChorus/Resources");
            EnsureFolder(ScenarioResourcesPath);

            CreateScenario(
                "FlowerWorkScenario_apology",
                "apology",
                "I want to apologize to someone important… what flower says 'sincere apology'?",
                "Which bouquet fits a sincere apology?",
                new[] { "White lilies", "Scarlet roses", "Bright sunflowers" },
                0,
                "White lilies — quiet and sincere. Good call.",
                "That reads too celebratory. White lilies would have been safer.");

            CreateScenario(
                "FlowerWorkScenario_celebration",
                "celebration",
                "They're celebrating a promotion tonight. I need something flashy!",
                "What fits a flashy celebration?",
                new[] { "Baby's breath only", "Gerbera mix", "Dried herbs" },
                1,
                "Gerbera mix pops under neon. Perfect for a promotion party.",
                "Too muted. Gerbera mix would've carried the energy.");

            CreateScenario(
                "FlowerWorkScenario_rich_aroma",
                "rich_aroma",
                "They'd like flashy flowers with a rich aroma.",
                "Flashy and fragrant — which do you pick?",
                new[] { "Scented roses", "Artificial silk blooms", "Cactus arrangement" },
                0,
                "Scented roses hit both flash and aroma. Nice.",
                "No fragrance there. Scented roses were the brief.");

            CreateScenario(
                "FlowerWorkScenario_teacher_gift",
                "teacher_gift",
                "I need a respectful gift for my teacher. Nothing romantic.",
                "Respectful, not romantic — your pick?",
                new[] { "Red rose bouquet", "Yellow chrysanthemums", "Heart-shaped arrangement" },
                1,
                "Yellow chrysanthemums feel respectful without romance. Solid.",
                "That leans romantic. Chrysanthemums were the safer gift.");

            CreateScenario(
                "FlowerWorkScenario_get_well",
                "get_well",
                "A friend is recovering at home. Something gentle would help.",
                "Gentle get-well flowers?",
                new[] { "Soft pastel carnations", "Thorny cactus", "All-black wrap" },
                0,
                "Pastel carnations feel gentle. The customer smiles.",
                "Too harsh for recovery. Pastel carnations next time.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Fractured Chorus] Flower work scenarios created under Resources/FlowerWork.");
        }

        [MenuItem("Fractured Chorus/Hub/Create FlowerShopWork Scene")]
        public static void CreateScene()
        {
            CreateScenarioAssets();
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

        [InitializeOnLoadMethod]
        private static void TryAutoCreateFromFlag()
        {
            EditorApplication.delayCall += () =>
            {
                var flag = System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(Application.dataPath, "..", "Library", "fc_create_flower_shop_work_scene.flag"));
                if (!System.IO.File.Exists(flag))
                {
                    return;
                }

                try
                {
                    System.IO.File.Delete(flag);
                    if (!System.IO.File.Exists(ScenePath))
                    {
                        CreateScene();
                    }
                    else
                    {
                        EnsureBuildSettings();
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Fractured Chorus] Auto create FlowerShopWork failed: {ex}");
                }
            };
        }

        [MenuItem("Fractured Chorus/Hub/Setup FlowerShopWork Scene Hierarchy")]
        public static void SetupHierarchy()
        {
            CreateScenarioAssets();
            var existing = GameObject.Find("FlowerShopWorkRoot");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog(
                        "Setup FlowerShopWork",
                        "FlowerShopWorkRoot already exists. Delete and recreate hierarchy?",
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
            Debug.Log("[Fractured Chorus] FlowerShopWork hierarchy created — Save scene (Ctrl+S).");
        }

        private static void CreateScenario(
            string fileName,
            string id,
            string customerLine,
            string thinkPrompt,
            string[] choices,
            int correctIndex,
            string correctReply,
            string wrongReply)
        {
            var path = $"{ScenarioResourcesPath}/{fileName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<FlowerWorkScenarioSO>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<FlowerWorkScenarioSO>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.id = id;
            asset.customerLine = customerLine;
            asset.thinkPrompt = thinkPrompt;
            asset.choices = choices;
            asset.correctIndex = correctIndex;
            asset.correctReply = correctReply;
            asset.wrongReply = wrongReply;
            EditorUtility.SetDirty(asset);
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

            var root = new GameObject("FlowerShopWorkRoot");
            var eventController = root.AddComponent<FlowerWorkEventController>();
            var runtime = root.AddComponent<VnRuntimeController>();
            var cueResolver = root.AddComponent<VnCueResolver>();
            var audioPlayer = root.AddComponent<VnAudioPlayer>();
            BindFlowerBackgroundCues(cueResolver);

            var canvasGo = new GameObject("FlowerCanvas");
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = CreateImage("Background", canvasGo.transform, null, Color.black);
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

            var dialogueRoot = CreateUiObject("DialoguePanel", canvasGo.transform);
            StretchRect(dialogueRoot, VnDialoguePanelLayout.DialoguePanelAnchorMin, VnDialoguePanelLayout.DialoguePanelAnchorMax, Vector2.zero, Vector2.zero);
            var dialogueGroup = dialogueRoot.AddComponent<CanvasGroup>();
            var dialogueFrame = CreateImage("DialogueFrame", dialogueRoot.transform, LoadSprite(DialogueFramePath), Color.white);
            StretchRect(dialogueFrame.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dialogueFrame.preserveAspect = false;
            dialogueFrame.type = Image.Type.Sliced;
            dialogueFrame.fillCenter = true;
            dialogueFrame.raycastTarget = false;

            var bodyBacking = CreateImage("DialogueBodyBacking", dialogueRoot.transform, null, VnDialoguePanelLayout.BodyBackingColor);
            StretchRect(bodyBacking.gameObject, VnDialoguePanelLayout.BodyBackingAnchorMin, VnDialoguePanelLayout.BodyBackingAnchorMax, Vector2.zero, Vector2.zero);
            bodyBacking.raycastTarget = false;

            var nameplateGo = CreateUiObject("Nameplate", dialogueRoot.transform);
            StretchRect(nameplateGo, VnDialoguePanelLayout.NameplateAnchorMin, VnDialoguePanelLayout.NameplateAnchorMax, Vector2.zero, Vector2.zero);
            var nameplateText = nameplateGo.AddComponent<Text>();
            VnUiFont.ApplyReadableNameplate(nameplateText);
            nameplateText.raycastTarget = false;

            var bodyGo = CreateUiObject("DialogueBody", dialogueRoot.transform);
            StretchRect(bodyGo, VnDialoguePanelLayout.BodyAnchorMin, VnDialoguePanelLayout.BodyAnchorMax, Vector2.zero, Vector2.zero);
            var bodyText = bodyGo.AddComponent<Text>();
            VnUiFont.ApplyReadableBody(bodyText);
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.raycastTarget = false;
            var typewriter = dialogueRoot.AddComponent<PrologueTypewriterView>();
            SetSerializedField(typewriter, "bodyText", bodyText);

            var textCardRoot = CreateUiObject("TextCardPanel", canvasGo.transform);
            StretchRect(textCardRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var textCardGroup = textCardRoot.AddComponent<CanvasGroup>();
            textCardGroup.alpha = 0f;
            textCardRoot.SetActive(false);
            var textCardBg = CreateImage("TextCardDim", textCardRoot.transform, null, VnDialoguePanelLayout.TextCardDimColor);
            StretchRect(textCardBg.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var textCardBody = CreateText("TextCardBody", textCardRoot.transform, string.Empty, VnDialoguePanelLayout.TextCardFontSize, TextAnchor.MiddleCenter);
            StretchRect(textCardBody.gameObject, VnDialoguePanelLayout.TextCardBodyAnchorMin, VnDialoguePanelLayout.TextCardBodyAnchorMax, Vector2.zero, Vector2.zero);
            VnUiFont.ApplyReadableBody(textCardBody, VnDialoguePanelLayout.TextCardFontSize);

            var choiceGo = CreateUiObject("ChoicePanel", canvasGo.transform);
            StretchRect(choiceGo, VnDialoguePanelLayout.ChoicePanelAnchorMin, VnDialoguePanelLayout.ChoicePanelAnchorMax, Vector2.zero, Vector2.zero);
            var choiceGroup = choiceGo.AddComponent<CanvasGroup>();
            var choiceView = choiceGo.AddComponent<VnChoiceView>();
            SetSerializedField(choiceView, "root", choiceGroup);

            var dateHud = VnSceneUiSetupEditor.EnsureStoryDateHud(canvasGo.transform);

            var fadeGo = CreateUiObject("FadeOverlay", canvasGo.transform);
            StretchRect(fadeGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fadeImage = fadeGo.AddComponent<Image>();
            fadeImage.color = Color.black;
            var fadeGroup = fadeGo.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;

            var convenience = VnConvenienceUiSetupEditor.EnsureConvenienceUi(canvasGo.transform);
            var catalog = AssetDatabase.LoadAssetAtPath<VnSpeakerCatalogSO>(CatalogPath);
            var scenarioList = new System.Collections.Generic.List<FlowerWorkScenarioSO>();
            foreach (var guid in AssetDatabase.FindAssets("t:FlowerWorkScenarioSO", new[] { ScenarioResourcesPath }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<FlowerWorkScenarioSO>(path);
                if (so != null)
                {
                    scenarioList.Add(so);
                }
            }

            SetSerializedField(runtime, "speakerCatalog", catalog);
            SetSerializedField(runtime, "cueResolver", cueResolver);
            SetSerializedField(runtime, "audioPlayer", audioPlayer);
            SetSerializedField(runtime, "portraitView", portraitViewHost);
            SetSerializedField(runtime, "typewriter", typewriter);
            SetSerializedField(runtime, "nameplateText", nameplateText);
            SetSerializedField(runtime, "textCardBody", textCardBody);
            SetSerializedField(runtime, "dialoguePanel", dialogueGroup);
            SetSerializedField(runtime, "textCardPanel", textCardGroup);
            SetSerializedField(runtime, "fadeOverlay", fadeGroup);
            SetSerializedField(runtime, "backgroundImage", bg);
            SetSerializedField(runtime, "dateHud", dateHud);
            SetSerializedField(runtime, "choiceView", choiceView);
            SetSerializedField(runtime, "openingDateDisplay", "01/09");
            SetSerializedField(runtime, "openingPhaseDisplay", "After School");
            SetSerializedField(runtime, "typingClip", AssetDatabase.LoadAssetAtPath<AudioClip>(TypingPath));
            SetSerializedField(runtime, "beginHubOnEnd", false);
            SetSerializedField(runtime, "playOnStart", false);
            SetSerializedField(runtime, "loadNextSceneOnEnd", false);
            SetSerializedField(runtime, "convenience", convenience);
            SetSerializedField(audioPlayer, "cueResolver", cueResolver);
            if (typewriter != null)
            {
                SetSerializedField(typewriter, "typingClip", AssetDatabase.LoadAssetAtPath<AudioClip>(TypingPath));
            }

            SetSerializedField(eventController, "runtime", runtime);
            SetSerializedField(eventController, "playOnStart", true);
            var eventSo = new SerializedObject(eventController);
            var poolProp = eventSo.FindProperty("scenarioPool");
            poolProp.arraySize = scenarioList.Count;
            for (var i = 0; i < scenarioList.Count; i++)
            {
                poolProp.GetArrayElementAtIndex(i).objectReferenceValue = scenarioList[i];
            }

            eventSo.ApplyModifiedPropertiesWithoutUndo();
            SceneFontSetupEditor.FinalizeSceneCanvas(canvasGo);
        }

        private static void BindFlowerBackgroundCues(VnCueResolver cueResolver)
        {
            var arrive = LoadSprite(FlowerArtRoot + "flower_event_01_arrive_shift_v1.png");
            var customer = LoadSprite(FlowerArtRoot + "flower_event_02_customer_ask_v1.png");
            var think = LoadSprite(FlowerArtRoot + "flower_event_03_contemplate_v1.png");
            var happy = LoadSprite(FlowerArtRoot + "flower_event_04_happy_v1.png");
            var so = new SerializedObject(cueResolver);
            var entries = so.FindProperty("entries");
            entries.arraySize = 4;
            SetCueEntry(entries.GetArrayElementAtIndex(0), VnBgIds.FlowerArrive, arrive, null);
            SetCueEntry(entries.GetArrayElementAtIndex(1), VnBgIds.FlowerCustomer, customer, null);
            SetCueEntry(entries.GetArrayElementAtIndex(2), VnBgIds.FlowerThink, think, null);
            SetCueEntry(entries.GetArrayElementAtIndex(3), VnBgIds.FlowerHappy, happy, null);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static VnStoryDateHud EnsureStoryDateHud(Transform canvas)
        {
            return VnSceneUiSetupEditor.EnsureStoryDateHud(canvas);
        }

        private static void EnsureBuildSettings()
        {
            var path = ScenePath;
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            var found = false;
            foreach (var existing in EditorBuildSettings.scenes)
            {
                if (existing.path == path)
                {
                    found = true;
                    list.Add(new EditorBuildSettingsScene(path, true));
                }
                else
                {
                    list.Add(existing);
                }
            }

            if (!found)
            {
                list.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = list.ToArray();
        }

        private static void SetCueEntry(SerializedProperty entry, string id, Sprite sprite, AudioClip clip)
        {
            entry.FindPropertyRelative("id").stringValue = id;
            entry.FindPropertyRelative("sprite").objectReferenceValue = sprite;
            entry.FindPropertyRelative("clip").objectReferenceValue = clip;
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

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture != null)
            {
                return Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
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

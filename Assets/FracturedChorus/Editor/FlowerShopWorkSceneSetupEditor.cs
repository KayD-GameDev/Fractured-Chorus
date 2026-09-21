#if UNITY_EDITOR
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Hub;
using FracturedChorus.Hub.FlowerWork;
using FracturedChorus.Narrative;
using FracturedChorus.Narrative.Vn;
using FracturedChorus.RunMap;
using FracturedChorus.UI;
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
        private const string FlowerShopBgPath = FlowerArtRoot + "flower_shop_bg_v1.jpg";
        private const string FlowerShopGreetAudioPath =
            "Assets/FracturedChorus/Audio/SFX/FlowerWork/FlowerShop_Greet.mp3";
        private const string ScenarioResourcesPath = "Assets/FracturedChorus/Resources/FlowerWork";

        [MenuItem("Fractured Chorus/Hub/Create Flower Work Scenarios")]
        public static void CreateScenarioAssets()
        {
            EnsureFolder("Assets/FracturedChorus/Resources");
            EnsureFolder(ScenarioResourcesPath);

            CreateScenario(
                "FlowerWorkScenario_apology",
                "apology",
                VnSpeakerIds.FlowerCustomerGentleman,
                "I want to apologize to someone important… what flower says 'sincere apology'?",
                "Which bouquet fits a sincere apology?",
                new[] { "White lilies", "Scarlet roses", "Bright sunflowers" },
                0,
                "White lilies — quiet and sincere. Good call.",
                "That reads too celebratory. White lilies would have been safer.");

            CreateScenario(
                "FlowerWorkScenario_celebration",
                "celebration",
                VnSpeakerIds.FlowerCustomerWoman,
                "They're celebrating a promotion tonight. I need something flashy!",
                "What fits a flashy celebration?",
                new[] { "Baby's breath only", "Gerbera mix", "Dried herbs" },
                1,
                "Gerbera mix pops under neon. Perfect for a promotion party.",
                "Too muted. Gerbera mix would've carried the energy.");

            CreateScenario(
                "FlowerWorkScenario_rich_aroma",
                "rich_aroma",
                VnSpeakerIds.FlowerCustomerGentleman,
                "They'd like flashy flowers with a rich aroma.",
                "Flashy and fragrant — which do you pick?",
                new[] { "Scented roses", "Artificial silk blooms", "Cactus arrangement" },
                0,
                "Scented roses hit both flash and aroma. Nice.",
                "No fragrance there. Scented roses were the brief.");

            CreateScenario(
                "FlowerWorkScenario_teacher_gift",
                "teacher_gift",
                VnSpeakerIds.FlowerCustomerWoman,
                "I need a respectful gift for my teacher. Nothing romantic.",
                "Respectful, not romantic — your pick?",
                new[] { "Red rose bouquet", "Yellow chrysanthemums", "Heart-shaped arrangement" },
                1,
                "Yellow chrysanthemums feel respectful without romance. Solid.",
                "That leans romantic. Chrysanthemums were the safer gift.");

            CreateScenario(
                "FlowerWorkScenario_get_well",
                "get_well",
                VnSpeakerIds.FlowerCustomerGirl,
                "A friend is recovering at home. Something gentle would help.",
                "Gentle get-well flowers?",
                new[] { "Soft pastel carnations", "Thorny cactus", "All-black wrap" },
                0,
                "Pastel carnations feel gentle. The customer smiles.",
                "Too harsh for recovery. Pastel carnations next time.");

            CreateScenario(
                "FlowerWorkScenario_anniversary",
                "anniversary",
                VnSpeakerIds.FlowerCustomerGentleman,
                "It's our anniversary tonight. I want roses that feel classic, not cliché.",
                "Classic anniversary roses — which set fits?",
                new[] { "Deep red roses", "Artificial tulips", "Single daisy" },
                0,
                "Deep red roses — timeless. She'll love them.",
                "Too playful for an anniversary. Deep red roses were the mark.");

            CreateScenario(
                "FlowerWorkScenario_boy_birthday",
                "boy_birthday",
                VnSpeakerIds.FlowerCustomerBoy,
                "It's my mom's birthday. I saved up — what should I get her?",
                "A kid's budget — what's the best pick for Mom?",
                new[] { "Pink carnation bunch", "Black roses", "Empty vase" },
                0,
                "Pink carnations — sweet and within budget. Good heart.",
                "Too heavy for a kid's gift. Carnations were the kind choice.");

            CreateScenario(
                "FlowerWorkScenario_girl_recital",
                "girl_recital",
                VnSpeakerIds.FlowerCustomerGirl,
                "My sister has a piano recital tomorrow. Something encouraging?",
                "Encouraging flowers for a recital?",
                new[] { "Pale lavender bouquet", "Cactus with spikes", "Wilted stems" },
                0,
                "Lavender feels calm and proud. She'll feel supported.",
                "Too sharp for stage nerves. Lavender was the gentle pick.");

            CreateScenario(
                "FlowerWorkScenario_elder_memorial",
                "elder_memorial",
                VnSpeakerIds.FlowerCustomerElder,
                "I'd like something quiet for a memorial service. Nothing loud.",
                "Quiet memorial flowers — your recommendation?",
                new[] { "White chrysanthemums", "Neon gerbera mix", "Party confetti wrap" },
                0,
                "White chrysanthemums — restrained and respectful.",
                "Too bright for a memorial. White chrysanthemums were right.");

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

        [MenuItem("Fractured Chorus/Hub/Rebind FlowerShopWork Background")]
        public static void RebindBackgroundOnly()
        {
            var resolver = Object.FindAnyObjectByType<VnCueResolver>();
            if (resolver == null)
            {
                Debug.LogError("[Fractured Chorus] Open FlowerShopWork then run Rebind FlowerShopWork Background.");
                return;
            }

            BindFlowerBackgroundCues(resolver);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] FlowerShopWork cues rebound to flower_shop_bg_v1 — Ctrl+S.");
        }

        [MenuItem("Fractured Chorus/Hub/Ensure FlowerShopWork Edit Preview Hierarchy")]
        public static void EnsureEditPreviewHierarchy()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "FlowerShop Edit Preview",
                    "Thoát Play Mode trước khi ensure hierarchy.",
                    "OK");
                return;
            }

            var activePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            if (activePath != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var root = GameObject.Find("FlowerShopWorkRoot");
            if (root == null)
            {
                Debug.LogError("[Fractured Chorus] FlowerShopWorkRoot missing — chạy Setup FlowerShopWork Scene Hierarchy.");
                return;
            }

            var runtime = root.GetComponent<VnRuntimeController>();
            var eventController = root.GetComponent<FlowerWorkEventController>();
            var rewardNote = root.GetComponent<FlowerWorkRewardNoteDirector>()
                             ?? Undo.AddComponent<FlowerWorkRewardNoteDirector>(root);

            var canvas = root.transform.Find("FlowerCanvas");
            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] FlowerCanvas missing.");
                return;
            }

            var choiceView = canvas.GetComponentInChildren<VnChoiceView>(true);
            if (choiceView != null)
            {
                EnsureVnChoiceHierarchy(choiceView);
            }

            var social = EnsureSocialStatsOverlay(canvas);
            var noteImage = EnsureRewardNoteImage(canvas, LoadSprite(NoteResourcePath));
            var overlayRect = social != null ? social.transform as RectTransform : null;

            SetSerializedField(rewardNote, "noteSprite", LoadSprite(NoteResourcePath));
            SetSerializedField(rewardNote, "noteImage", noteImage);
            SetSerializedField(rewardNote, "overlayRoot", overlayRect);

            if (eventController != null)
            {
                SetSerializedField(eventController, "rewardNote", rewardNote);
                SetSerializedField(eventController, "socialStatsOverlay", social);
            }

            if (runtime != null && choiceView != null)
            {
                SetSerializedField(runtime, "choiceView", choiceView);
            }

            EnsureCanvasScale(canvas);
            if (eventController != null)
            {
                eventController.SetEditorPreview(FlowerWorkEventController.FlowerWorkEditorPreview.ThinkChoice);
            }

            SaveFlowerShopWorkSceneLayoutInternal(showDialog: false);
            Debug.Log("[Fractured Chorus] FlowerShopWork edit-preview hierarchy ensured and saved.");
        }

        [MenuItem("Fractured Chorus/Hub/Save FlowerShopWork Scene Layout")]
        public static void SaveFlowerShopWorkSceneLayout()
        {
            SaveFlowerShopWorkSceneLayoutInternal(showDialog: true);
        }

        [MenuItem("Fractured Chorus/Hub/Apply FlowerShop Dialogue Layout (Active Scene)")]
        public static void ApplyFlowerShopDialogueLayoutActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Apply layout", "Thoát Play Mode trước.", "OK");
                return;
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.path.Replace('\\', '/').Contains("FlowerShopWork"))
            {
                EditorUtility.DisplayDialog(
                    "Apply layout",
                    "Mở scene FlowerShopWork rồi chạy lại — không OpenScene tự động để tránh ghi đè chỉnh tay chưa lưu.",
                    "OK");
                return;
            }

            var runtime = Object.FindAnyObjectByType<VnRuntimeController>();
            if (runtime?.DialoguePanel == null)
            {
                Debug.LogError("[Fractured Chorus] DialoguePanel missing.");
                return;
            }

            VnDialoguePanelLayoutSnapshotEditor.ApplyLayout(
                runtime.DialoguePanel,
                VnDialoguePanelLayoutSnapshotEditor.FlowerShopWorkLayoutAssetPath);

            var portrait = Object.FindAnyObjectByType<VnDialoguePortraitView>();
            if (portrait != null)
            {
                portrait.ApplySavedLayoutToSlots();
                EditorUtility.SetDirty(portrait);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Fractured Chorus] Applied FlowerShop dialogue layout from snapshot (Ctrl+S to persist).");
        }

        [MenuItem("Fractured Chorus/Hub/Sync FlowerShop Corner HUD")]
        public static void SyncFlowerShopCornerHud()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Sync Corner HUD", "Thoát Play Mode trước.", "OK");
                return;
            }

            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var activePath = activeScene.path.Replace('\\', '/');
            if (activePath == ScenePath && activeScene.isDirty)
            {
                if (!EditorUtility.DisplayDialog(
                        "Sync Corner HUD",
                        "Scene FlowerShopWork đang có thay đổi chưa lưu. Lưu (Ctrl+S) trước, hoặc Cancel.",
                        "Tiếp tục anyway",
                        "Cancel"))
                {
                    return;
                }
            }

            if (activePath != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            if (!WireFlowerShopCornerHud(out var runtime))
            {
                return;
            }

            EditorSceneManager.MarkSceneDirty(runtime.gameObject.scene);
            EditorSceneManager.SaveScene(runtime.gameObject.scene, ScenePath);
            Debug.Log("[Fractured Chorus] Synced FlowerShop CornerHud (Bonds/Stat style).");
        }

        private static bool WireFlowerShopCornerHud(out VnRuntimeController runtime)
        {
            runtime = null;
            var root = GameObject.Find("FlowerShopWorkRoot");
            runtime = root != null ? root.GetComponent<VnRuntimeController>() : null;
            var canvas = root != null ? root.transform.Find("FlowerCanvas") : null;
            if (canvas == null || runtime == null)
            {
                Debug.LogError("[Fractured Chorus] FlowerShopWorkRoot / FlowerCanvas / VnRuntimeController missing.");
                return false;
            }

            var cornerHud = BondsSceneSetupEditor.AttachCornerHud(canvas);
            if (cornerHud == null)
            {
                Debug.LogError(
                    "[Fractured Chorus] FlowerCanvas/CornerHud missing. Chạy Tools/copy-bonds-corner-hud-to-flower-scene.mjs hoặc copy từ Bonds scene.");
                return false;
            }

            SetSerializedField(runtime, "hubCornerInfoHud", cornerHud);
            if (runtime.DateHud != null)
            {
                runtime.DateHud.gameObject.SetActive(false);
            }

            EditorUtility.SetDirty(runtime);
            return true;
        }

        public static void SaveFlowerShopWorkSceneLayoutInternal(bool showDialog)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Save FlowerShopWork",
                    "Thoát Play Mode trước khi lưu layout.",
                    "OK");
                return;
            }

            var saveScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var activePath = saveScene.path.Replace('\\', '/');
            if (activePath == ScenePath && saveScene.isDirty)
            {
                Debug.Log("[Fractured Chorus] Saving FlowerShopWork from open scene (includes unsaved Rect edits).");
            }
            else if (activePath != ScenePath)
            {
                EditorUtility.DisplayDialog(
                    "Save FlowerShopWork",
                    "Scene FlowerShopWork chưa mở — sẽ load từ disk. Nếu vừa chỉnh layout trong Unity, mở FlowerShopWork và Ctrl+S trước khi Save Layout.",
                    "Load from disk");
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var root = GameObject.Find("FlowerShopWorkRoot");
            if (root == null)
            {
                Debug.LogError("[Fractured Chorus] FlowerShopWorkRoot missing.");
                return;
            }

            var runtime = root.GetComponent<VnRuntimeController>();
            var eventController = root.GetComponent<FlowerWorkEventController>();
            var rewardNote = root.GetComponent<FlowerWorkRewardNoteDirector>()
                             ?? Undo.AddComponent<FlowerWorkRewardNoteDirector>(root);
            var canvas = root.transform.Find("FlowerCanvas");

            if (canvas != null)
            {
                EnsureCanvasScale(canvas);
            }

            var portraitView = root.GetComponentInChildren<VnDialoguePortraitView>(true);
            if (portraitView != null)
            {
                portraitView.CaptureLayoutFromSlots();
                EditorUtility.SetDirty(portraitView);
            }

            var choiceView = root.GetComponentInChildren<VnChoiceView>(true);
            if (choiceView != null)
            {
                WireChoiceViewFromHierarchy(choiceView);
                EditorUtility.SetDirty(choiceView);
            }

            SocialStatsOverlayUI social = null;
            Image noteImage = null;
            if (canvas != null)
            {
                var socialTf = canvas.Find("SocialStatsOverlay");
                if (socialTf != null)
                {
                    social = socialTf.GetComponent<SocialStatsOverlayUI>()
                             ?? socialTf.gameObject.AddComponent<SocialStatsOverlayUI>();
                    social.EnsureRuntimeBindings();
                    social.Rewire();
                }

                var noteTf = canvas.Find("FlowerRewardNote");
                if (noteTf != null)
                {
                    noteImage = noteTf.GetComponent<Image>();
                }
            }

            if (rewardNote != null)
            {
                SetSerializedField(rewardNote, "noteSprite", LoadSprite(NoteResourcePath));
                SetSerializedField(rewardNote, "noteImage", noteImage);
                SetSerializedField(rewardNote, "overlayRoot", social != null ? social.transform as RectTransform : null);
                EditorUtility.SetDirty(rewardNote);
            }

            if (eventController != null)
            {
                SetSerializedField(eventController, "rewardNote", rewardNote);
                SetSerializedField(eventController, "socialStatsOverlay", social);
                EditorUtility.SetDirty(eventController);
            }

            if (runtime != null && choiceView != null)
            {
                SetSerializedField(runtime, "choiceView", choiceView);
                EditorUtility.SetDirty(runtime);
            }

            if (runtime != null && canvas != null)
            {
                var cornerHud = BondsSceneSetupEditor.AttachCornerHud(canvas);
                if (cornerHud != null)
                {
                    SetSerializedField(runtime, "hubCornerInfoHud", cornerHud);
                    if (runtime.DateHud != null)
                    {
                        runtime.DateHud.gameObject.SetActive(false);
                    }

                    EditorUtility.SetDirty(runtime);
                }
            }

            var cueResolver = root.GetComponent<VnCueResolver>();
            if (cueResolver != null)
            {
                BindFlowerBackgroundCues(cueResolver);
                EditorUtility.SetDirty(cueResolver);
            }

            if (runtime?.BackgroundImage != null && cueResolver != null)
            {
                var bg = runtime.BackgroundImage;
                if (cueResolver.TryGetSprite(VnBgIds.FlowerShop, out var shopBg) && shopBg != null)
                {
                    bg.sprite = shopBg;
                    bg.color = Color.white;
                    bg.preserveAspect = true;
                    EditorUtility.SetDirty(bg);
                }
            }

            if (runtime?.DialoguePanel != null)
            {
                VnDialoguePanelLayoutSnapshotEditor.CaptureAndWriteDialogueLayout(
                    runtime.DialoguePanel,
                    VnDialoguePanelLayoutSnapshotEditor.FlowerShopWorkLayoutAssetPath,
                    ScenePath);
            }

            MarkDirtyRecursive(root);
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Save FlowerShopWork",
                    "Đã lưu RectTransform, choice wiring, portrait backup và scene file.",
                    "OK");
            }

            Debug.Log($"[Fractured Chorus] Saved {ScenePath}");
        }

        private static void WireChoiceViewFromHierarchy(VnChoiceView choiceView)
        {
            if (choiceView == null)
            {
                return;
            }

            var prompt = choiceView.transform.Find("Prompt")?.GetComponent<Text>();
            var options = choiceView.transform.Find("Options")?.GetComponent<RectTransform>();
            SetSerializedField(choiceView, "promptText", prompt);
            SetSerializedField(choiceView, "optionsRoot", options);
        }

        private static void MarkDirtyRecursive(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            EditorUtility.SetDirty(go);
            foreach (Component component in go.GetComponents<Component>())
            {
                if (component != null)
                {
                    EditorUtility.SetDirty(component);
                }
            }

            for (var i = 0; i < go.transform.childCount; i++)
            {
                MarkDirtyRecursive(go.transform.GetChild(i).gameObject);
            }
        }

        private static void EnsureCanvasScale(Transform canvas)
        {
            if (canvas == null)
            {
                return;
            }

            var scale = canvas.localScale;
            if (scale.x == 0f || scale.y == 0f || scale.z == 0f)
            {
                canvas.localScale = Vector3.one;
            }
        }

        private const string NoteResourcePath = "Assets/FracturedChorus/Resources/UI/FlowerWork/note_resonance.png";

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
            string customerSpeakerId,
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
            asset.customerSpeakerId = customerSpeakerId;
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
            var rewardNote = root.AddComponent<FlowerWorkRewardNoteDirector>();
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
            canvasGo.transform.localScale = Vector3.one;

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
            SetSerializedField(eventController, "rewardNote", rewardNote);
            SetSerializedField(eventController, "playOnStart", true);
            var eventSo = new SerializedObject(eventController);
            var poolProp = eventSo.FindProperty("scenarioPool");
            poolProp.arraySize = scenarioList.Count;
            for (var i = 0; i < scenarioList.Count; i++)
            {
                poolProp.GetArrayElementAtIndex(i).objectReferenceValue = scenarioList[i];
            }

            eventSo.ApplyModifiedPropertiesWithoutUndo();

            EnsureVnChoiceHierarchy(choiceView);
            var socialOverlay = EnsureSocialStatsOverlay(canvasGo.transform);
            var noteImage = EnsureRewardNoteImage(canvasGo.transform, LoadSprite(NoteResourcePath));
            SetSerializedField(rewardNote, "noteSprite", LoadSprite(NoteResourcePath));
            SetSerializedField(rewardNote, "noteImage", noteImage);
            SetSerializedField(rewardNote, "overlayRoot", socialOverlay != null ? socialOverlay.transform as RectTransform : null);
            SetSerializedField(eventController, "socialStatsOverlay", socialOverlay);

            SceneFontSetupEditor.FinalizeSceneCanvas(canvasGo);
        }

        private static SocialStatsOverlayUI EnsureSocialStatsOverlay(Transform canvas)
        {
            var existing = canvas.Find("SocialStatsOverlay");
            if (existing != null)
            {
                var overlay = existing.GetComponent<SocialStatsOverlayUI>()
                              ?? existing.gameObject.AddComponent<SocialStatsOverlayUI>();
                overlay.EnsureRuntimeBindings();
                overlay.Rewire();
                existing.gameObject.SetActive(false);
                return overlay;
            }

            var built = SocialStatsOverlayUI.Build(canvas);
            built.Overlay.EnsureRuntimeBindings();
            built.Overlay.Rewire();
            built.Overlay.gameObject.SetActive(false);
            return built.Overlay;
        }

        private static Image EnsureRewardNoteImage(Transform canvas, Sprite sprite)
        {
            var existing = canvas.Find("FlowerRewardNote");
            if (existing != null)
            {
                var image = existing.GetComponent<Image>();
                if (image != null && sprite != null)
                {
                    image.sprite = sprite;
                }

                existing.gameObject.SetActive(false);
                return image;
            }

            var go = CreateUiObject("FlowerRewardNote", canvas);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(FlowerWorkRewardNoteDirector.NoteSize, FlowerWorkRewardNoteDirector.NoteSize);

            var note = go.AddComponent<Image>();
            note.sprite = sprite;
            note.preserveAspect = true;
            note.raycastTarget = false;
            go.SetActive(false);
            return note;
        }

        private static void EnsureVnChoiceHierarchy(VnChoiceView choiceView)
        {
            if (choiceView == null)
            {
                return;
            }

            choiceView.EnsureEditorHierarchy();
            EditorUtility.SetDirty(choiceView);
        }

        private static void BindFlowerBackgroundCues(VnCueResolver cueResolver)
        {
            var shop = LoadSprite(FlowerShopBgPath);
            var greetClip = LoadAudioClip(FlowerShopGreetAudioPath);
            var so = new SerializedObject(cueResolver);
            var entries = so.FindProperty("entries");
            entries.arraySize = 6;
            SetCueEntry(entries.GetArrayElementAtIndex(0), VnBgIds.FlowerShop, shop, null);
            SetCueEntry(entries.GetArrayElementAtIndex(1), VnBgIds.FlowerArrive, shop, null);
            SetCueEntry(entries.GetArrayElementAtIndex(2), VnBgIds.FlowerCustomer, shop, null);
            SetCueEntry(entries.GetArrayElementAtIndex(3), VnBgIds.FlowerThink, shop, null);
            SetCueEntry(entries.GetArrayElementAtIndex(4), VnBgIds.FlowerHappy, shop, null);
            SetCueEntry(entries.GetArrayElementAtIndex(5), VnAudioIds.FlowerShopGreet, null, greetClip);
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

        private static AudioClip LoadAudioClip(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
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

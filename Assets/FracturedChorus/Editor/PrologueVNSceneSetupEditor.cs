#if UNITY_EDITOR
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Narrative;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class PrologueVNSceneSetupEditor
    {
        private const string ScenePath = "Assets/FracturedChorus/Scenes/PrologueVN.unity";
        private const string ButterflyBgPath = "Assets/FracturedChorus/Art/Backgrounds/PrologueVN_ButterflyVoid_v1.png";
        private const string DialogueFramePath = "Assets/FracturedChorus/Art/UI/Narrative/DialogueBox_Frame_LightBlueHolo_v1.png";
        private const string ContractPath = "Assets/FracturedChorus/Art/UI/Narrative/Contract_Document_Realistic_v2.png";
        private const string BgmPath = "Assets/FracturedChorus/Audio/Music/Velvet_Reverie_BGM.mp3";
        private const string TypingPath = "Assets/FracturedChorus/Audio/SFX/Prologue_Typing.mp3";
        private const string ButterflyPath = "Assets/FracturedChorus/Audio/SFX/Prologue_ButterflyWings.mp3";
        private const string PenSignPath = "Assets/FracturedChorus/Audio/SFX/Prologue_PenSign.mp3";
        private const string ButtonPressPath = "Assets/FracturedChorus/Audio/SFX/MainMenu_ButtonPress.wav";
        private const string MenuTingPath = "Assets/FracturedChorus/Audio/SFX/MainMenu_ChangeMenu_Ting.mp3";

        [MenuItem("Fractured Chorus/Create PrologueVN Scene")]
        public static void CreatePrologueVNScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            ConfigureCamera();
            BuildHierarchy();
            EnsureBuildSettings();

            if (!AssetDatabase.IsValidFolder("Assets/FracturedChorus/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus", "Scenes");
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Fractured Chorus] Saved {ScenePath}");
        }

        [MenuItem("Fractured Chorus/Setup PrologueVN Scene Hierarchy")]
        public static void SetupPrologueVNSceneHierarchy()
        {
            var existing = GameObject.Find("PrologueVNRoot");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog(
                        "Setup PrologueVN",
                        "PrologueVNRoot already exists. Delete and recreate hierarchy?",
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
            Debug.Log("[Fractured Chorus] PrologueVN hierarchy created — Save scene (Ctrl+S).");
        }

        public static void BatchCreatePrologueVNScene()
        {
            if (System.IO.File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var existing = GameObject.Find("PrologueVNRoot");
                if (existing != null)
                {
                    Object.DestroyImmediate(existing);
                }

                ConfigureCamera();
                BuildHierarchy();
                EnsureBuildSettings();
                EditorSceneManager.SaveScene(scene);
            }
            else
            {
                CreatePrologueVNScene();
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Fractured Chorus] PrologueVN batch complete.");
            EditorApplication.Exit(0);
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

            var root = new GameObject("PrologueVNRoot");
            var controller = root.AddComponent<PrologueVNController>();
            var audio = root.AddComponent<PrologueAudioController>();

            var canvasGo = new GameObject("PrologueCanvas");
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var blackBg = CreateImage("BlackBackground", canvasGo.transform, null, Color.black);
            StretchRect(blackBg.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var butterflyBg = CreateImage("ButterflyBackground", canvasGo.transform, LoadSprite(ButterflyBgPath), Color.white);
            StretchRect(butterflyBg.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            butterflyBg.gameObject.SetActive(false);

            var disclaimerGo = CreateUiObject("DisclaimerText", canvasGo.transform);
            StretchRect(disclaimerGo, new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.7f), Vector2.zero, Vector2.zero);
            var disclaimerText = disclaimerGo.AddComponent<Text>();
            disclaimerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            disclaimerText.fontSize = 40;
            disclaimerText.fontStyle = FontStyle.Italic;
            disclaimerText.alignment = TextAnchor.MiddleCenter;
            disclaimerText.color = new Color(0.72f, 0.8f, 0.9f, 1f);
            disclaimerText.horizontalOverflow = HorizontalWrapMode.Wrap;
            disclaimerText.verticalOverflow = VerticalWrapMode.Overflow;
            disclaimerText.raycastTarget = false;
            disclaimerGo.transform.localRotation = Quaternion.identity;
            var disclaimerTypewriter = disclaimerGo.AddComponent<PrologueTypewriterView>();
            SetSerializedField(disclaimerTypewriter, "bodyText", disclaimerText);

            var dialogueRoot = CreateUiObject("DialoguePanel", canvasGo.transform);
            StretchRect(dialogueRoot, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.72f), Vector2.zero, Vector2.zero);
            var dialogueGroup = dialogueRoot.AddComponent<CanvasGroup>();
            dialogueGroup.alpha = 0f;
            var dialogueFrame = CreateImage("DialogueFrame", dialogueRoot.transform, LoadSprite(DialogueFramePath), Color.white);
            StretchRect(dialogueFrame.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dialogueFrame.preserveAspect = false;
            dialogueFrame.gameObject.SetActive(false);

            var dialogueBodyGo = CreateUiObject("DialogueBody", dialogueRoot.transform);
            StretchRect(dialogueBodyGo, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.82f), Vector2.zero, Vector2.zero);
            var dialogueText = dialogueBodyGo.AddComponent<Text>();
            dialogueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dialogueText.fontSize = 40;
            dialogueText.alignment = TextAnchor.MiddleCenter;
            dialogueText.color = new Color(0.92f, 0.97f, 1f, 1f);
            dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            dialogueText.verticalOverflow = VerticalWrapMode.Overflow;
            dialogueText.raycastTarget = false;
            var dialogueTypewriter = dialogueRoot.AddComponent<PrologueTypewriterView>();
            SetSerializedField(dialogueTypewriter, "bodyText", dialogueText);

            var choiceRoot = CreateUiObject("ChoicePanel", canvasGo.transform);
            StretchRect(choiceRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var choiceGroup = choiceRoot.AddComponent<CanvasGroup>();
            choiceGroup.alpha = 0f;
            choiceGroup.gameObject.SetActive(false);
            var choiceView = choiceRoot.AddComponent<PrologueChoiceView>();

            var choicePromptGo = CreateUiObject("ChoicePrompt", choiceRoot.transform);
            StretchRect(choicePromptGo, new Vector2(0.18f, 0.42f), new Vector2(0.82f, 0.58f), Vector2.zero, Vector2.zero);
            var choicePrompt = choicePromptGo.AddComponent<Text>();
            choicePrompt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            choicePrompt.fontSize = 34;
            choicePrompt.alignment = TextAnchor.MiddleCenter;
            choicePrompt.color = new Color(0.88f, 0.94f, 1f, 1f);
            choicePrompt.horizontalOverflow = HorizontalWrapMode.Wrap;

            var agreeRow = CreateUiObject("AgreeRow", choiceRoot.transform);
            StretchRect(agreeRow, new Vector2(0.34f, 0.24f), new Vector2(0.66f, 0.34f), Vector2.zero, Vector2.zero);
            var agreeHighlight = CreateImage("AgreeHighlight", agreeRow.transform, null,
                new Color(0.35f, 0.72f, 1f, 0.92f));
            StretchRect(agreeHighlight.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            agreeHighlight.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -4f);
            var agreeText = CreateText("AgreeLabel", agreeRow.transform, "I agree.", 36, TextAnchor.MiddleCenter);
            StretchRect(agreeText.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var disagreeRow = CreateUiObject("DisagreeRow", choiceRoot.transform);
            StretchRect(disagreeRow, new Vector2(0.34f, 0.12f), new Vector2(0.66f, 0.22f), Vector2.zero, Vector2.zero);
            var disagreeHighlight = CreateImage("DisagreeHighlight", disagreeRow.transform, null,
                new Color(0.35f, 0.72f, 1f, 0f));
            StretchRect(disagreeHighlight.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            disagreeHighlight.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 4f);
            var disagreeText = CreateText("DisagreeLabel", disagreeRow.transform, "I do not agree.", 36, TextAnchor.MiddleCenter);
            StretchRect(disagreeText.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            SetSerializedField(choiceView, "root", choiceGroup);
            SetSerializedField(choiceView, "promptText", choicePrompt);
            SetSerializedField(choiceView, "agreeLabel", agreeText);
            SetSerializedField(choiceView, "disagreeLabel", disagreeText);
            SetSerializedField(choiceView, "agreeHighlight", agreeHighlight);
            SetSerializedField(choiceView, "disagreeHighlight", disagreeHighlight);

            var contractRoot = CreateUiObject("ContractPanel", canvasGo.transform);
            StretchRect(contractRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var contractGroup = contractRoot.AddComponent<CanvasGroup>();
            contractGroup.alpha = 0f;
            contractGroup.gameObject.SetActive(false);
            var contractView = contractRoot.AddComponent<PrologueContractView>();

            var contractPaper = CreateImage("ContractPaper", contractRoot.transform, LoadSprite(ContractPath), Color.white);
            StretchRect(contractPaper.gameObject, new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.92f), Vector2.zero, Vector2.zero);
            contractPaper.preserveAspect = true;

            var nameValueGo = CreateUiObject("NameValue", contractPaper.transform);
            StretchRect(nameValueGo, PrologueContractLayout.NameLineMin, PrologueContractLayout.NameLineMax, Vector2.zero, Vector2.zero);
            var nameValueText = nameValueGo.AddComponent<Text>();
            nameValueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameValueText.fontSize = 26;
            nameValueText.alignment = TextAnchor.MiddleLeft;
            nameValueText.color = new Color(0.08f, 0.12f, 0.28f, 1f);

            var nameInputGo = CreateUiObject("NameInput", contractPaper.transform);
            StretchRect(nameInputGo, PrologueContractLayout.NameLineMin, PrologueContractLayout.NameLineMax, Vector2.zero, Vector2.zero);
            var nameInputBg = nameInputGo.AddComponent<Image>();
            nameInputBg.color = Color.clear;
            var nameInput = nameInputGo.AddComponent<InputField>();
            var nameInputTextGo = CreateUiObject("Text", nameInputGo.transform);
            StretchRect(nameInputTextGo, Vector2.zero, Vector2.one, new Vector2(12f, 6f), new Vector2(-12f, -6f));
            var nameInputText = nameInputTextGo.AddComponent<Text>();
            nameInputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameInputText.fontSize = 26;
            nameInputText.color = new Color(0.08f, 0.12f, 0.28f, 1f);
            nameInputText.supportRichText = false;
            var placeholderGo = CreateUiObject("Placeholder", nameInputGo.transform);
            StretchRect(placeholderGo, Vector2.zero, Vector2.one, new Vector2(12f, 6f), new Vector2(-12f, -6f));
            var placeholderText = placeholderGo.AddComponent<Text>();
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = 26;
            placeholderText.fontStyle = FontStyle.Italic;
            placeholderText.color = new Color(0.08f, 0.12f, 0.28f, 0.45f);
            placeholderText.text = RunProfile.DefaultNameSuggestion;
            nameInput.textComponent = nameInputText;
            nameInput.placeholder = placeholderText;

            var signatureGo = CreateUiObject("SignaturePad", contractPaper.transform);
            StretchRect(signatureGo, PrologueContractLayout.SignatureLineMin, PrologueContractLayout.SignatureLineMax, Vector2.zero, Vector2.zero);
            var signatureBg = signatureGo.AddComponent<Image>();
            signatureBg.color = Color.clear;
            var signatureRawGo = CreateUiObject("SignatureDraw", signatureGo.transform);
            StretchRect(signatureRawGo, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            var signatureRaw = signatureRawGo.AddComponent<RawImage>();
            signatureRaw.color = Color.white;
            var signaturePad = signatureGo.AddComponent<PrologueSignaturePad>();
            SetSerializedField(signaturePad, "targetImage", signatureRaw);

            var hintText = CreateText("HintText", contractRoot.transform, string.Empty, 22, TextAnchor.LowerCenter);
            StretchRect(hintText.gameObject, new Vector2(0.2f, 0.02f), new Vector2(0.8f, 0.08f), Vector2.zero, Vector2.zero);
            hintText.color = new Color(0.85f, 0.92f, 1f, 1f);

            var confirmGo = CreateUiObject("ConfirmButton", contractRoot.transform);
            StretchRect(confirmGo, new Vector2(0.38f, 0.03f), new Vector2(0.62f, 0.11f), Vector2.zero, Vector2.zero);
            var confirmImage = confirmGo.AddComponent<Image>();
            confirmImage.sprite = LoadSprite("Assets/FracturedChorus/Art/UI/Narrative/prologue_contract_confirm_button_holo_v1.png");
            confirmImage.color = Color.white;
            confirmImage.preserveAspect = true;
            var confirmButton = confirmGo.AddComponent<Button>();
            var confirmLabel = CreateText("Label", confirmGo.transform, "Confirm", 28, TextAnchor.MiddleCenter);
            StretchRect(confirmLabel.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            confirmLabel.color = Color.white;
            confirmLabel.gameObject.SetActive(false);

            SetSerializedField(contractView, "root", contractGroup);
            SetSerializedField(contractView, "contractPaper", contractPaper);
            SetSerializedField(contractView, "nameValueText", nameValueText);
            SetSerializedField(contractView, "nameInput", nameInput);
            SetSerializedField(contractView, "hintText", hintText);
            SetSerializedField(contractView, "confirmButton", confirmButton);
            SetSerializedField(contractView, "signaturePad", signaturePad);

            var fadeGo = CreateUiObject("FadeOverlay", canvasGo.transform);
            StretchRect(fadeGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fadeImage = fadeGo.AddComponent<Image>();
            fadeImage.color = Color.black;
            var fadeGroup = fadeGo.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 1f;
            fadeGroup.blocksRaycasts = false;

            var convenience = VnConvenienceUiSetupEditor.EnsureConvenienceUi(canvasGo.transform);

            SetSerializedField(audio, "bgmClip", LoadAudio(BgmPath));
            SetSerializedField(audio, "butterflyWingsClip", LoadAudio(ButterflyPath));
            SetSerializedField(audio, "typingClip", LoadAudio(TypingPath));
            SetSerializedField(audio, "penSignClip", LoadAudio(PenSignPath));
            SetSerializedField(audio, "buttonPressClip", LoadAudio(ButtonPressPath));
            SetSerializedField(audio, "menuTingClip", LoadAudio(MenuTingPath));

            SetSerializedField(controller, "fadeOverlay", fadeGroup);
            SetSerializedField(controller, "butterflyBackground", butterflyBg);
            SetSerializedField(controller, "dialoguePanel", dialogueGroup);
            SetSerializedField(controller, "disclaimerTypewriter", disclaimerTypewriter);
            SetSerializedField(controller, "disclaimerText", disclaimerText);
            SetSerializedField(controller, "dialogueTypewriter", dialogueTypewriter);
            SetSerializedField(controller, "choiceView", choiceView);
            SetSerializedField(controller, "contractView", contractView);
            SetSerializedField(controller, "audioController", audio);
            SetSerializedField(controller, "choiceBackdrop", choiceGroup);
            SetSerializedField(controller, "convenience", convenience);
        }

        private static void EnsureBuildSettings()
        {
            var scenes = new[]
            {
                "Assets/FracturedChorus/Scenes/MainMenuStartGame.unity",
                ScenePath,
                "Assets/FracturedChorus/Scenes/RunMapPrototype.unity",
                "Assets/FracturedChorus/Scenes/CombatPrototype.unity"
            };

            var buildScenes = new EditorBuildSettingsScene[scenes.Length];
            for (var i = 0; i < scenes.Length; i++)
            {
                buildScenes[i] = new EditorBuildSettingsScene(scenes[i], true);
            }

            EditorBuildSettings.scenes = buildScenes;
        }

        [MenuItem("Fractured Chorus/Create PrologueVN Layout Config")]
        public static void CreatePrologueVNLayoutConfigAsset()
        {
            const string folder = "Assets/FracturedChorus/Data/ScriptableObjects";
            if (!AssetDatabase.IsValidFolder("Assets/FracturedChorus/Data"))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus", "Data");
            }

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus/Data", "ScriptableObjects");
            }

            const string path = folder + "/PrologueVNLayoutConfig.asset";
            if (AssetDatabase.LoadAssetAtPath<PrologueVNLayoutConfig>(path) != null)
            {
                Debug.Log($"[Fractured Chorus] Layout config already exists: {path}");
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<PrologueVNLayoutConfig>(path);
                return;
            }

            var config = ScriptableObject.CreateInstance<PrologueVNLayoutConfig>();
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = config;
            Debug.Log($"[Fractured Chorus] Created {path}");
        }

        [MenuItem("Fractured Chorus/Fix Contract Sprite Import")]
        public static void FixContractSpriteImport()
        {
            var importer = AssetImporter.GetAtPath(PrologueContractLayout.ContractSpritePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[Fractured Chorus] Contract sprite not found.");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            Debug.Log("[Fractured Chorus] Contract sprite reimported as Single sprite.");
        }

        [MenuItem("Fractured Chorus/Upgrade PrologueVN Choice UI")]
        public static void UpgradePrologueChoiceUi()
        {
            var choiceView = Object.FindAnyObjectByType<PrologueChoiceView>();
            if (choiceView == null)
            {
                Debug.LogWarning("[Fractured Chorus] PrologueChoiceView not found in active scene.");
                return;
            }

            var serialized = new SerializedObject(choiceView);
            serialized.FindProperty("idleColor").colorValue = new Color(0.04f, 0.04f, 0.06f, 0.94f);
            serialized.FindProperty("selectedColor").colorValue = new Color(0.35f, 0.72f, 1f, 0.92f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] PrologueVN choice colors updated — Save scene (Ctrl+S).");
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

            Debug.LogWarning($"[Fractured Chorus] Sprite not found: {assetPath}");
            return null;
        }

        private static AudioClip LoadAudio(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
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
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
                case string s:
                    prop.stringValue = s;
                    break;
                case int i:
                    prop.intValue = i;
                    break;
                case float f:
                    prop.floatValue = f;
                    break;
                case bool b:
                    prop.boolValue = b;
                    break;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using FracturedChorus.Narrative.Vn;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    [InitializeOnLoad]
    public static class VnPortraitAssetSetupEditor
    {
        private const string PortraitsFolder = "Assets/FracturedChorus/Art/UI/Narrative/Portraits";
        private const string SpeakersFolder = "Assets/FracturedChorus/Data/ScriptableObjects/Narrative/Speakers";
        private const string CatalogPath = "Assets/FracturedChorus/Data/ScriptableObjects/Narrative/VnSpeakerCatalog.asset";
        private const string PrologueScenePath = "Assets/FracturedChorus/Scenes/PrologueVN.unity";
        private const string PreviewSceneHint =
            "Open any VN canvas and run Fractured Chorus/Narrative/Build Dialogue Portrait Slot Under Selection";

        private static readonly Color RenShadow = new Color(0.05f, 0.12f, 0.35f, 0.92f);
        private static readonly Color HarutoShadow = new Color(0.08f, 0.14f, 0.28f, 0.9f);
        private static readonly Color RyoShadow = new Color(0.06f, 0.18f, 0.14f, 0.9f);
        private static readonly Color MeiLinShadow = new Color(0.22f, 0.08f, 0.14f, 0.9f);

        private static string AutoInstallFlagPath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "vn_portrait_install_prologue.flag"));

        static VnPortraitAssetSetupEditor()
        {
            EditorApplication.delayCall += TryAutoInstallPortraitOnPrologue;
        }

        private static void TryAutoInstallPortraitOnPrologue()
        {
            var flagPath = AutoInstallFlagPath;
            if (!File.Exists(flagPath))
            {
                return;
            }

            try
            {
                File.Delete(flagPath);
                InstallPortraitOnPrologueVnInternal(false);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[Fractured Chorus] Auto portrait install failed: " + ex);
            }
        }

        [MenuItem("Fractured Chorus/Narrative/Bind Haruto Expression Sprites")]
        public static void BindHarutoExpressions()
        {
            BindExpressionSet(
                "Speaker_Haruto.asset",
                "Assets/FracturedChorus/Art/Characters/Haruto/VnBust",
                "haruto_bust_",
                PortraitsFolder + "/haruto_bust_neutral_v1.png",
                new[] { "neutral", "startled", "pain", "fear", "agony", "desperate" });
        }

        [MenuItem("Fractured Chorus/Narrative/Bind Mei Lin Expression Sprites")]
        public static void BindMeiLinExpressions()
        {
            BindExpressionSet(
                "Speaker_MeiLin.asset",
                "Assets/FracturedChorus/Art/Characters/MeiLin/VnBust",
                "mei_lin_bust_",
                PortraitsFolder + "/mei_lin_bust_neutral_v1.png",
                new[] { "neutral", "stern", "weary", "concerned", "warning" });
        }

        [MenuItem("Fractured Chorus/Narrative/Bind Ren Expression Sprites")]
        public static void BindRenExpressions()
        {
            BindExpressionSet(
                "Speaker_Ren.asset",
                "Assets/FracturedChorus/Art/Characters/Ren/VnBust",
                "ren_bust_",
                PortraitsFolder + "/ren_school_bust_neutral_v1.png",
                new[] { "neutral", "startled", "smile", "curious", "annoyed" });
        }

        [MenuItem("Fractured Chorus/Narrative/Bind Ryo Expression Sprites")]
        public static void BindRyoExpressions()
        {
            BindExpressionSet(
                "Speaker_Ryo.asset",
                "Assets/FracturedChorus/Art/Characters/Ryo/VnBust",
                "ryo_bust_",
                PortraitsFolder + "/ryo_bust_neutral_v1.png",
                new[] { "neutral", "nervous", "startled", "uneasy", "concerned" });
        }

        [MenuItem("Fractured Chorus/Narrative/Bind All Opening VN Expression Sprites")]
        public static void BindAllOpeningExpressions()
        {
            BindHarutoExpressions();
            BindMeiLinExpressions();
            BindRyoExpressions();
            BindRenExpressions();
        }

        private static void BindExpressionSet(
            string speakerFileName,
            string bustFolder,
            string filePrefix,
            string portraitNeutralPath,
            string[] expressionIds)
        {
            var speaker = AssetDatabase.LoadAssetAtPath<VnSpeakerDefinitionSO>(SpeakersFolder + "/" + speakerFileName);
            if (speaker == null)
            {
                CreateSpeakerAssets();
                speaker = AssetDatabase.LoadAssetAtPath<VnSpeakerDefinitionSO>(SpeakersFolder + "/" + speakerFileName);
            }

            if (speaker == null)
            {
                Debug.LogError("[Fractured Chorus] Missing speaker: " + speakerFileName);
                return;
            }

            ConfigureTextureImporter(portraitNeutralPath);
            var list = new List<VnExpressionSprite>();
            for (var i = 0; i < expressionIds.Length; i++)
            {
                var id = expressionIds[i];
                var path = bustFolder + "/" + filePrefix + id + "_v1.png";
                ConfigureTextureImporter(path);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    Debug.LogWarning("[Fractured Chorus] Missing bust: " + path);
                    continue;
                }

                list.Add(new VnExpressionSprite
                {
                    expressionId = id,
                    sprite = sprite
                });
            }

            AssetDatabase.Refresh();
            Sprite neutralPortrait = null;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].expressionId == "neutral")
                {
                    neutralPortrait = list[i].sprite;
                    break;
                }
            }

            if (neutralPortrait == null && list.Count > 0)
            {
                neutralPortrait = list[0].sprite;
            }

            if (neutralPortrait == null)
            {
                ConfigureTextureImporter(portraitNeutralPath);
                neutralPortrait = AssetDatabase.LoadAssetAtPath<Sprite>(portraitNeutralPath);
            }

            speaker.bustSprite = neutralPortrait;
            speaker.expressionSprites = list.ToArray();
            EditorUtility.SetDirty(speaker);
            AssetDatabase.SaveAssets();
            Selection.activeObject = speaker;
            Debug.Log("[Fractured Chorus] Bound " + list.Count + " expression sprites for " + speakerFileName);
        }

        [MenuItem("Fractured Chorus/Narrative/Create VN Portrait Speaker Assets")]
        public static void CreateSpeakerAssets()
        {
            EnsureFolder("Assets/FracturedChorus/Data/ScriptableObjects");
            EnsureFolder("Assets/FracturedChorus/Data/ScriptableObjects/Narrative");
            EnsureFolder(SpeakersFolder);

            ConfigureTextureImporter(PortraitsFolder + "/ren_school_bust_neutral_v1.png");
            ConfigureTextureImporter(PortraitsFolder + "/haruto_bust_neutral_v1.png");
            ConfigureTextureImporter(PortraitsFolder + "/ryo_bust_neutral_v1.png");
            ConfigureTextureImporter(PortraitsFolder + "/mei_lin_bust_neutral_v1.png");
            AssetDatabase.Refresh();

            var ren = UpsertSpeaker(
                SpeakersFolder + "/Speaker_Ren.asset",
                VnSpeakerIds.Ren,
                "Ren",
                PortraitsFolder + "/ren_school_bust_neutral_v1.png",
                RenShadow,
                VnDialoguePortraitLayout.DefaultShadowOffset);

            var haruto = UpsertSpeaker(
                SpeakersFolder + "/Speaker_Haruto.asset",
                VnSpeakerIds.Haruto,
                "Haruto",
                PortraitsFolder + "/haruto_bust_neutral_v1.png",
                HarutoShadow,
                VnDialoguePortraitLayout.DefaultShadowOffset);

            var ryo = UpsertSpeaker(
                SpeakersFolder + "/Speaker_Ryo.asset",
                VnSpeakerIds.Ryo,
                "Ryo",
                PortraitsFolder + "/ryo_bust_neutral_v1.png",
                RyoShadow,
                VnDialoguePortraitLayout.DefaultShadowOffset);

            var mei = UpsertSpeaker(
                SpeakersFolder + "/Speaker_MeiLin.asset",
                VnSpeakerIds.MeiLin,
                "Mei Lin",
                PortraitsFolder + "/mei_lin_bust_neutral_v1.png",
                MeiLinShadow,
                VnDialoguePortraitLayout.DefaultShadowOffset);

            var catalog = AssetDatabase.LoadAssetAtPath<VnSpeakerCatalogSO>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<VnSpeakerCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.EditorReplaceAll(new List<VnSpeakerDefinitionSO> { ren, haruto, ryo, mei });
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Fractured Chorus] VN speaker assets ready. Catalog=" + CatalogPath +
                ". Ren bust=school uniform. " + PreviewSceneHint);
        }

        [MenuItem("Fractured Chorus/Narrative/Build Dialogue Portrait Slot Under Selection")]
        public static void BuildPortraitSlotUnderSelection()
        {
            var parent = Selection.activeTransform;
            if (parent == null)
            {
                EditorUtility.DisplayDialog(
                    "VN Portrait Slot",
                    "Select the VN Canvas (or a panel under it). Portrait will be created as a sibling behind the dialogue frame.",
                    "OK");
                return;
            }

            BuildPortraitSlot(parent, true);
        }

        [MenuItem("Fractured Chorus/Narrative/Install Dialogue Portrait On PrologueVN")]
        public static void InstallPortraitOnPrologueVn()
        {
            InstallPortraitOnPrologueVnInternal(false);
        }

        public static void BatchInstallPortraitOnPrologueVn()
        {
            try
            {
                InstallPortraitOnPrologueVnInternal(true);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[Fractured Chorus] Batch portrait install failed: " + ex);
                EditorApplication.Exit(1);
            }
        }

        private static void InstallPortraitOnPrologueVnInternal(bool exitEditor)
        {
            CreateSpeakerAssets();

            var scene = EditorSceneManager.OpenScene(PrologueScenePath, OpenSceneMode.Single);
            var canvas = GameObject.Find("PrologueCanvas");
            if (canvas == null)
            {
                throw new System.InvalidOperationException("PrologueCanvas not found in PrologueVN.");
            }

            BuildPortraitSlot(canvas.transform, false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Fractured Chorus] DialoguePortrait installed under PrologueCanvas and scene saved.");

            if (exitEditor)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void BuildPortraitSlot(Transform parent, bool askConfirmRecreate)
        {
            var existing = parent.Find("DialoguePortrait");
            if (existing != null)
            {
                if (askConfirmRecreate &&
                    !EditorUtility.DisplayDialog(
                        "VN Portrait Slot",
                        "DialoguePortrait already exists under selection. Recreate?",
                        "Recreate",
                        "Cancel"))
                {
                    return;
                }

                Object.DestroyImmediate(existing.gameObject);
            }

            var rootGo = new GameObject("DialoguePortrait", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            rootGo.transform.SetAsFirstSibling();
            var root = rootGo.GetComponent<RectTransform>();
            root.anchorMin = VnDialoguePortraitLayout.AnchorMin;
            root.anchorMax = VnDialoguePortraitLayout.AnchorMax;
            root.pivot = VnDialoguePortraitLayout.Pivot;
            root.anchoredPosition = VnDialoguePortraitLayout.AnchoredPosition;
            root.sizeDelta = VnDialoguePortraitLayout.SizeDelta;

            var shadowGo = CreateImageChild(root, "Shadow");
            var portraitGo = CreateImageChild(root, "Portrait");

            var view = rootGo.GetComponent<VnDialoguePortraitView>();
            if (view == null)
            {
                view = rootGo.AddComponent<VnDialoguePortraitView>();
            }

            view.Bind(root, shadowGo.GetComponent<Image>(), portraitGo.GetComponent<Image>());

            var ren = AssetDatabase.LoadAssetAtPath<VnSpeakerDefinitionSO>(SpeakersFolder + "/Speaker_Ren.asset");
            if (ren != null)
            {
                view.Show(ren);
            }

            Selection.activeGameObject = rootGo;
            EditorUtility.SetDirty(rootGo);
            Debug.Log("[Fractured Chorus] DialoguePortrait slot created (behind dialogue frame).");
        }

        private static GameObject CreateImageChild(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return go;
        }

        private static VnSpeakerDefinitionSO UpsertSpeaker(
            string assetPath,
            string id,
            string displayName,
            string spritePath,
            Color shadowColor,
            Vector2 shadowOffset)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                Debug.LogError("[Fractured Chorus] Missing sprite at " + spritePath + ". Import PNG as Sprite first.");
            }

            var so = AssetDatabase.LoadAssetAtPath<VnSpeakerDefinitionSO>(assetPath);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<VnSpeakerDefinitionSO>();
                AssetDatabase.CreateAsset(so, assetPath);
            }

            so.speakerId = id;
            so.displayName = displayName;
            so.bustSprite = sprite;
            so.shadowColor = shadowColor;
            so.shadowOffsetPixels = shadowOffset;
            EditorUtility.SetDirty(so);
            return so;
        }

        private static void ConfigureTextureImporter(string assetPath)
        {
            var fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("[Fractured Chorus] Portrait file missing: " + assetPath);
                return;
            }

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
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

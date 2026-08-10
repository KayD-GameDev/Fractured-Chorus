#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FracturedChorus.Combat.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class LuxeArenaBackgroundSetupEditor
    {
        private const string ArenaRoot = "Assets/FracturedChorus/Art/Backgrounds/LuxeArena";
        private const string ConfigPath = ArenaRoot + "/LuxeArenaBackgroundConfig.asset";
        private const string FloorPath = ArenaRoot + "/luxe_arena_floor_v2.png";
        private const string GrandstandPath = ArenaRoot + "/luxe_arena_grandstand_v1.png";
        private const string TvFramePath = ArenaRoot + "/luxe_arena_layer_tv_v1.png";
        private const string EmotionContentPath = ArenaRoot + "/luxe_arena_emotion_screen_square_v1.png";
        private const string EmotionScreenPath = ArenaRoot + "/luxe_arena_emotion_screen_v4.png";
        private const string AudienceFxDir = ArenaRoot + "/Audience";
        private const string SoftConePath = ArenaRoot + "/Lights/luxe_arena_soft_cone_v1.png";
        private const string LayerRootName = "LuxeArenaLayers";
        private const string AudienceRootName = "AudienceFx";

        [InitializeOnLoadMethod]
        private static void AutoWireAfterReload()
        {
            EditorApplication.delayCall += TryAutoWireOpenScene;
        }

        private static void TryAutoWireOpenScene()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "CombatPrototype")
            {
                return;
            }

            var bgRoot = GameObject.Find(CombatUiHierarchy.BackgroundCanvasName);
            if (bgRoot == null)
            {
                return;
            }

            if (NeedsRewire(bgRoot))
            {
                Debug.Log("[LuxeArena] Auto-wire: Grandstand + single AudienceFx.");
                WireBossArenaBackground();
            }
        }

        [MenuItem("Fractured Chorus/Luxe Arena/Wire Boss Arena Background To Scene")]
        public static void WireBossArenaBackgroundMenu()
        {
            WireBossArenaBackground();
        }

        [MenuItem("Fractured Chorus/Luxe Arena/Refresh Background Config From Art Folder")]
        public static void RefreshConfigFromArtMenu()
        {
            var config = LoadOrCreateConfig();
            PopulateConfigFromArt(config);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Selection.activeObject = config;
            Debug.Log("[LuxeArena] Config refreshed from Art/LuxeArena.");
        }

        public static void WireFromBatch()
        {
            var scenePath = "Assets/FracturedChorus/Scenes/CombatPrototype.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            WireBossArenaBackground();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[LuxeArena] Batch wire complete.");
        }

        public static void WireBossArenaBackground()
        {
            var bgRoot = GameObject.Find(CombatUiHierarchy.BackgroundCanvasName);
            if (bgRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "Luxe Arena",
                    "Không tìm thấy Background canvas. Mở CombatPrototype trước.",
                    "OK");
                return;
            }

            var config = LoadOrCreateConfig();
            PopulateConfigFromArt(config);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            var floorSprite = config.Floor != null ? config.Floor : config.BasePlate;
            if (floorSprite == null ||
                config.Grandstand == null ||
                config.AudienceFrames == null ||
                config.AudienceFrames.Length == 0 ||
                config.SoftCone == null)
            {
                EditorUtility.DisplayDialog(
                    "Luxe Arena",
                    "Config thiếu Floor/BasePlate / Grandstand / AudienceFrames / SoftCone.",
                    "OK");
                return;
            }

            var baseImage = ResolveBaseImage(bgRoot);
            if (baseImage == null)
            {
                EditorUtility.DisplayDialog("Luxe Arena", "Không tìm thấy Image nền.", "OK");
                return;
            }

            PurgeLegacyAudience(bgRoot);

            Undo.RecordObject(baseImage, "Luxe Arena Base");
            baseImage.sprite = floorSprite;
            baseImage.color = Color.white;
            baseImage.raycastTarget = false;
            baseImage.preserveAspect = false;
            EditorUtility.SetDirty(baseImage);

            var layers = EnsureRect(bgRoot.transform, LayerRootName);
            StretchFull(layers);

            var grandstand = EnsureLayerImage(layers, "Grandstand", config.Grandstand, Color.white);
            PlaceRect(grandstand, config.GrandstandAnchorMin, config.GrandstandAnchorMax);

            var floor = EnsureLayerImage(layers, "Floor", floorSprite, Color.white);
            PlaceRect(floor, config.FloorAnchorMin, config.FloorAnchorMax);

            BuildTvRig(layers, config);

            var spotRig = EnsureRect(layers, "SpotlightRig");
            StretchFull(spotRig);
            var spots = BuildSpotlights(spotRig, config);

            var audienceRoot = EnsureRect(layers, AudienceRootName);
            PlaceRect(audienceRoot, config.AudienceAnchorMin, config.AudienceAnchorMax);
            DestroyNamedChild(audienceRoot, "AudienceLeft");
            DestroyNamedChild(audienceRoot, "AudienceCenter");
            DestroyNamedChild(audienceRoot, "AudienceRight");
            CleanupDuplicateMiddles(audienceRoot);

            var primary = EnsureRaw(audienceRoot, "Primary", config.AudienceFrames[0], new Rect(0f, 0f, 1f, 1f), 0.7f);
            var secondary = EnsureRaw(
                audienceRoot,
                "Secondary",
                config.AudienceFrames.Length > 1 ? config.AudienceFrames[1] : config.AudienceFrames[0],
                new Rect(0f, 0f, 1f, 1f),
                0f);

            grandstand.SetAsFirstSibling();
            floor.SetSiblingIndex(1);
            var tv = layers.Find("TV") as RectTransform;
            if (tv != null)
            {
                tv.SetSiblingIndex(2);
            }

            DestroyNamedChild(layers, "EmotionScreen");

            spotRig.SetAsLastSibling();
            audienceRoot.SetAsLastSibling();

            var director = bgRoot.GetComponent<LuxeArenaBackgroundDirector>();
            if (director == null)
            {
                director = Undo.AddComponent<LuxeArenaBackgroundDirector>(bgRoot);
            }

            WireDirector(
                director,
                config,
                baseImage,
                floor.GetComponent<Image>(),
                grandstand.GetComponent<Image>(),
                layers,
                primary,
                secondary,
                spots);

            EditorSceneManager.MarkSceneDirty(bgRoot.scene);
            EditorSceneManager.SaveScene(bgRoot.scene);
            Selection.activeGameObject = floor.gameObject;
            Debug.Log(
                $"[LuxeArena] Wired Floor + Grandstand + TV + AudienceFx ({config.AudienceFrames.Length} frames).");
        }

        private static bool NeedsRewire(GameObject bgRoot)
        {
            if (bgRoot.transform.Find(LayerRootName + "/AudienceBand") != null)
            {
                return true;
            }

            if (bgRoot.transform.Find(LayerRootName + "/AudienceLeft") != null ||
                FindDeep(bgRoot.transform, "AudienceLeft") != null)
            {
                return true;
            }

            var director = bgRoot.GetComponent<LuxeArenaBackgroundDirector>();
            if (director == null)
            {
                return true;
            }

            var layers = bgRoot.transform.Find(LayerRootName);
            if (layers == null ||
                layers.Find("Grandstand") == null ||
                layers.Find("Floor") == null ||
                layers.Find("TV") == null ||
                layers.Find(AudienceRootName) == null)
            {
                return true;
            }

            var so = new SerializedObject(director);
            return so.FindProperty("audiencePrimary") == null ||
                   so.FindProperty("audiencePrimary").objectReferenceValue == null ||
                   so.FindProperty("floorImage") == null ||
                   so.FindProperty("floorImage").objectReferenceValue == null ||
                   so.FindProperty("config").objectReferenceValue == null;
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

        private static void WireDirector(
            LuxeArenaBackgroundDirector director,
            LuxeArenaBackgroundConfig config,
            Image baseImage,
            Image floor,
            Image grandstand,
            RectTransform layers,
            RawImage primary,
            RawImage secondary,
            (RectTransform transform, Image image, float baseAngle)[] spots)
        {
            var so = new SerializedObject(director);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("baseImage").objectReferenceValue = baseImage;
            so.FindProperty("floorImage").objectReferenceValue = floor;
            so.FindProperty("grandstandImage").objectReferenceValue = grandstand;
            so.FindProperty("layersRoot").objectReferenceValue = layers;
            so.FindProperty("audiencePrimary").objectReferenceValue = primary;
            so.FindProperty("audienceSecondary").objectReferenceValue = secondary;

            var frames = config.AudienceFrames;
            so.FindProperty("audienceWaveFrames").arraySize = frames.Length;
            for (var i = 0; i < frames.Length; i++)
            {
                so.FindProperty("audienceWaveFrames").GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            }

            var spotsProp = so.FindProperty("spotlights");
            spotsProp.arraySize = spots.Length;
            for (var i = 0; i < spots.Length; i++)
            {
                var el = spotsProp.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("Transform").objectReferenceValue = spots[i].transform;
                el.FindPropertyRelative("Image").objectReferenceValue = spots[i].image;
                el.FindPropertyRelative("BaseAngle").floatValue = spots[i].baseAngle;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(director);
        }

        private static void PurgeLegacyAudience(GameObject bgRoot)
        {
            var layers = bgRoot != null ? bgRoot.transform.Find(LayerRootName) : null;
            if (layers != null)
            {
                DestroyNamedChild(layers, "AudienceBand");
                DestroyNamedChild(layers, "AudienceWave");
                DestroyNamedChild(layers, "AudienceWaveB");
                DestroyNamedChild(layers, "AudienceFar");
                DestroyNamedChild(layers, "AudienceMid");
                DestroyNamedChild(layers, "AudienceNear");
                DestroyChildrenByPrefix(layers, "LightsSweep");
            }

            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (var i = transforms.Length - 1; i >= 0; i--)
            {
                var t = transforms[i];
                if (t == null)
                {
                    continue;
                }

                if (t.name is "AudienceLeft" or "AudienceCenter" or "AudienceRight" or "AudienceBand" or
                    "AudienceMiddle")
                {
                    Undo.DestroyObjectImmediate(t.gameObject);
                }
            }
        }

        private static bool IsAudienceFxAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            var normalized = path.Replace('\\', '/');
            if (!normalized.StartsWith(AudienceFxDir + "/"))
            {
                return false;
            }

            if (normalized.Contains("/Wave/") ||
                normalized.Contains("audience_wave") ||
                normalized.Contains("/AudienceWave/"))
            {
                return false;
            }

            return normalized.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase);
        }

        private static Texture2D[] LoadAudienceFxFrames()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { AudienceFxDir });
            var list = new List<Texture2D>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!IsAudienceFxAssetPath(path))
                {
                    continue;
                }

                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null)
                {
                    list.Add(tex);
                }
            }

            return list.OrderBy(t => t.name, System.StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private static LuxeArenaBackgroundConfig LoadOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<LuxeArenaBackgroundConfig>(ConfigPath);
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<LuxeArenaBackgroundConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static void PopulateConfigFromArt(LuxeArenaBackgroundConfig config)
        {
            Undo.RecordObject(config, "Populate Luxe Arena Config");

            var floor = LoadFirstSprite(FloorPath);
            if (floor != null)
            {
                config.BasePlate = floor;
                config.Floor = floor;
            }

            var grandstand = LoadFirstSprite(GrandstandPath);
            if (grandstand != null)
            {
                config.Grandstand = grandstand;
            }

            config.FloorAnchorMin = Vector2.zero;
            config.FloorAnchorMax = Vector2.one;
            config.GrandstandAnchorMin = Vector2.zero;
            config.GrandstandAnchorMax = Vector2.one;

            var tvFrame = LoadFirstSprite(TvFramePath);
            if (tvFrame == null)
            {
                tvFrame = LoadFirstSprite(
                    "Assets/FracturedChorus/Resources/Backgrounds/LuxeArena/Layers/TV/luxe_arena_layer_tv_v1.png");
            }

            if (tvFrame != null)
            {
                config.TvFrame = tvFrame;
            }

            var emotionContent = LoadFirstSprite(EmotionContentPath);
            if (emotionContent == null)
            {
                emotionContent = LoadFirstSprite(
                    "Assets/FracturedChorus/Resources/Backgrounds/LuxeArena/luxe_arena_emotion_screen_square_v1.png");
            }

            if (emotionContent == null)
            {
                emotionContent = LoadFirstSprite(EmotionScreenPath);
            }

            if (emotionContent == null)
            {
                emotionContent = LoadFirstSprite(ArenaRoot + "/luxe_arena_emotion_screen_v2.png");
            }

            if (emotionContent == null)
            {
                emotionContent = LoadFirstSprite(ArenaRoot + "/luxe_arena_emotion_screen_v1.png");
            }

            if (emotionContent != null)
            {
                config.EmotionScreen = emotionContent;
            }

            var cone = LoadFirstSprite(SoftConePath);
            if (cone != null)
            {
                config.SoftCone = cone;
            }

            config.AudienceFrames = LoadAudienceFxFrames();

            if (config.Spotlights == null || config.Spotlights.Length == 0)
            {
                config.Spotlights = CreateDefaultSpotlightSeeds();
            }
        }

        private static LuxeArenaBackgroundConfig.SpotlightSeed[] CreateDefaultSpotlightSeeds()
        {
            return new[]
            {
                new LuxeArenaBackgroundConfig.SpotlightSeed
                    { AnchorX = 0.12f, AnchorY = 0.93f, Angle = -18f, Scale = 0.95f },
                new LuxeArenaBackgroundConfig.SpotlightSeed
                    { AnchorX = 0.22f, AnchorY = 0.95f, Angle = -10f, Scale = 1.05f },
                new LuxeArenaBackgroundConfig.SpotlightSeed
                    { AnchorX = 0.34f, AnchorY = 0.96f, Angle = -4f, Scale = 0.9f },
                new LuxeArenaBackgroundConfig.SpotlightSeed
                    { AnchorX = 0.66f, AnchorY = 0.96f, Angle = 4f, Scale = 0.9f },
                new LuxeArenaBackgroundConfig.SpotlightSeed
                    { AnchorX = 0.78f, AnchorY = 0.95f, Angle = 10f, Scale = 1.05f },
                new LuxeArenaBackgroundConfig.SpotlightSeed
                    { AnchorX = 0.88f, AnchorY = 0.93f, Angle = 18f, Scale = 0.95f },
            };
        }

        private static int DestroyNamedChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                return 0;
            }

            Undo.DestroyObjectImmediate(child.gameObject);
            return 1;
        }

        private static int DestroyChildrenByPrefix(Transform parent, string prefix)
        {
            var removed = 0;
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child.name.StartsWith(prefix))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    removed++;
                }
            }

            return removed;
        }

        private static void CleanupDuplicateMiddles(Transform audienceRoot)
        {
            for (var i = audienceRoot.childCount - 1; i >= 0; i--)
            {
                var child = audienceRoot.GetChild(i);
                if (child.name == "AudienceMiddle" || child.name.StartsWith("AudienceMiddle ("))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }
        }

        private static void BuildTvRig(Transform layers, LuxeArenaBackgroundConfig config)
        {
            if (config.TvFrame == null && config.EmotionScreen == null)
            {
                DestroyNamedChild(layers, "TV");
                DestroyNamedChild(layers, "EmotionScreen");
                return;
            }

            var tv = EnsureRect(layers, "TV");
            PlaceRect(tv, config.TvAnchorMin, config.TvAnchorMax);

            DestroyNamedChild(tv, "EmotionScreen");

            if (config.EmotionScreen != null)
            {
                var contentRt = EnsureRect(tv, "Content");
                PlaceRect(contentRt, config.TvContentInsetMin, config.TvContentInsetMax);
                var raw = contentRt.GetComponent<RawImage>();
                if (raw == null)
                {
                    raw = Undo.AddComponent<RawImage>(contentRt.gameObject);
                }

                var image = contentRt.GetComponent<Image>();
                if (image != null)
                {
                    Undo.DestroyObjectImmediate(image);
                }

                Undo.RecordObject(raw, "Setup TV Content");
                raw.texture = config.EmotionScreen.texture;
                raw.uvRect = config.TvContentUvRect;
                raw.color = Color.white;
                raw.raycastTarget = false;
                EditorUtility.SetDirty(raw);
                contentRt.SetAsFirstSibling();
            }
            else
            {
                DestroyNamedChild(tv, "Content");
            }

            if (config.TvFrame != null)
            {
                var frameRt = EnsureLayerImage(tv, "Frame", config.TvFrame, Color.white);
                StretchFull(frameRt);
                frameRt.SetAsLastSibling();
            }
            else
            {
                DestroyNamedChild(tv, "Frame");
            }
        }

        private static RectTransform EnsureLayerImage(
            Transform parent,
            string name,
            Sprite sprite,
            Color color)
        {
            var rt = EnsureRect(parent, name);
            var image = rt.GetComponent<Image>();
            if (image == null)
            {
                image = Undo.AddComponent<Image>(rt.gameObject);
            }

            Undo.RecordObject(image, "Setup " + name);
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            image.preserveAspect = false;
            EditorUtility.SetDirty(image);
            return rt;
        }

        private static void PlaceRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
        {
            Undo.RecordObject(rt, "Place " + rt.name);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            EditorUtility.SetDirty(rt);
        }

        private static RawImage EnsureRaw(Transform parent, string name, Texture2D tex, Rect uv, float alpha)
        {
            var rt = EnsureRect(parent, name);
            StretchFull(rt);
            var raw = rt.GetComponent<RawImage>();
            if (raw == null)
            {
                raw = Undo.AddComponent<RawImage>(rt.gameObject);
            }

            Undo.RecordObject(raw, "Setup " + name);
            raw.texture = tex;
            raw.uvRect = uv;
            raw.color = new Color(1f, 1f, 1f, alpha);
            raw.raycastTarget = false;
            EditorUtility.SetDirty(raw);
            return raw;
        }

        private static (RectTransform transform, Image image, float baseAngle)[] BuildSpotlights(
            RectTransform rig,
            LuxeArenaBackgroundConfig config)
        {
            var seeds = config.Spotlights;
            if (seeds == null || seeds.Length == 0)
            {
                seeds = CreateDefaultSpotlightSeeds();
                config.Spotlights = seeds;
                EditorUtility.SetDirty(config);
            }

            for (var i = rig.childCount - 1; i >= 0; i--)
            {
                var child = rig.GetChild(i);
                if (child.name.StartsWith("Spot_"))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }

            var result = new (RectTransform transform, Image image, float baseAngle)[seeds.Length];
            for (var i = 0; i < seeds.Length; i++)
            {
                var seed = seeds[i];
                var rt = EnsureRect(rig, "Spot_" + i);
                rt.anchorMin = new Vector2(seed.AnchorX, seed.AnchorY);
                rt.anchorMax = new Vector2(seed.AnchorX, seed.AnchorY);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = seed.Size * seed.Scale;
                rt.localRotation = Quaternion.Euler(0f, 0f, seed.Angle);

                var image = rt.GetComponent<Image>();
                if (image == null)
                {
                    image = Undo.AddComponent<Image>(rt.gameObject);
                }

                Undo.RecordObject(image, "Setup spot");
                image.sprite = config.SoftCone;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.color = seed.Color;
                EditorUtility.SetDirty(image);
                result[i] = (rt, image, seed.Angle);
            }

            return result;
        }

        private static Image ResolveBaseImage(GameObject bgRoot)
        {
            var named = bgRoot.transform.Find("Image");
            if (named != null)
            {
                var img = named.GetComponent<Image>();
                if (img != null)
                {
                    return img;
                }
            }

            return bgRoot.GetComponentInChildren<Image>(true);
        }

        private static RectTransform EnsureRect(Transform parent, string name)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private static void StretchFull(RectTransform rt)
        {
            PlaceRect(rt, Vector2.zero, Vector2.one);
        }

        private static Sprite LoadFirstSprite(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                return sprite;
            }

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset is Sprite s)
                {
                    return s;
                }
            }

            return null;
        }
    }
}
#endif

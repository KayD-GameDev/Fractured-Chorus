#if UNITY_EDITOR
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
        private const string PreviewPath = ArenaRoot + "/luxe_arena_preview_v6.png";
        private const string AudienceWaveDir = ArenaRoot + "/Audience/Wave";
        private const string SoftConePath = ArenaRoot + "/Lights/luxe_arena_soft_cone_v1.png";
        private const string LayoutPath = ArenaRoot + "/LuxeArenaAudienceLayout.asset";
        private const string LayerRootName = "LuxeArenaLayers";

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

            var director = Object.FindAnyObjectByType<LuxeArenaBackgroundDirector>(FindObjectsInactive.Include);
            if (director == null)
            {
                return;
            }

            var layers = GameObject.Find(CombatUiHierarchy.BackgroundCanvasName)?.transform.Find(LayerRootName);
            var band = layers?.Find("AudienceBand");
            var hasCenter = band != null && band.Find("AudienceCenter") != null;
            var so = new SerializedObject(director);
            var panels = so.FindProperty("audiencePanels");
            if (hasCenter && panels != null && panels.arraySize >= 3 &&
                panels.GetArrayElementAtIndex(0).FindPropertyRelative("Primary").objectReferenceValue != null)
            {
                return;
            }

            Debug.Log("[LuxeArena] Auto-wire scene hierarchy (AudienceBand L/C/R + SpotlightRig).");
            WireBossArenaBackground();
        }

        private static readonly SpotSeed[] SpotSeeds =
        {
            new SpotSeed(0.12f, 0.93f, -18f, 0.95f),
            new SpotSeed(0.22f, 0.95f, -10f, 1.05f),
            new SpotSeed(0.34f, 0.96f, -4f, 0.9f),
            new SpotSeed(0.66f, 0.96f, 4f, 0.9f),
            new SpotSeed(0.78f, 0.95f, 10f, 1.05f),
            new SpotSeed(0.88f, 0.93f, 18f, 0.95f),
        };

        private readonly struct SpotSeed
        {
            public readonly float Ax;
            public readonly float Ay;
            public readonly float Angle;
            public readonly float Scale;

            public SpotSeed(float ax, float ay, float angle, float scale)
            {
                Ax = ax;
                Ay = ay;
                Angle = angle;
                Scale = scale;
            }
        }

        [MenuItem("Fractured Chorus/Luxe Arena/Wire Boss Arena Background To Scene")]
        public static void WireBossArenaBackgroundMenu()
        {
            WireBossArenaBackground();
        }

        [MenuItem("Fractured Chorus/Luxe Arena/Save Audience Layout From Scene")]
        public static void SaveAudienceLayoutFromScene()
        {
            var band = FindAudienceBand();
            if (band == null)
            {
                EditorUtility.DisplayDialog("Luxe Arena", "Không tìm thấy AudienceBand.", "OK");
                return;
            }

            var layout = LoadOrCreateLayout();
            Undo.RecordObject(layout, "Save Audience Layout");
            CaptureBand(band, layout);
            CapturePanel(band.Find("AudienceLeft") as RectTransform, layout.Left);
            CapturePanel(band.Find("AudienceCenter") as RectTransform, layout.Center);
            CapturePanel(band.Find("AudienceRight") as RectTransform, layout.Right);
            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssets();
            Debug.Log("[LuxeArena] Đã lưu vị trí Audience L/C/R → " + LayoutPath);
        }

        [MenuItem("Fractured Chorus/Luxe Arena/Apply Layout + Wave To All 3 Panels")]
        public static void ApplyLayoutAndWaveToAllThree()
        {
            var bgRoot = GameObject.Find(CombatUiHierarchy.BackgroundCanvasName);
            if (bgRoot == null)
            {
                EditorUtility.DisplayDialog("Luxe Arena", "Không tìm thấy Background canvas.", "OK");
                return;
            }

            var frames = LoadWaveTextures(AudienceWaveDir, "luxe_arena_audience_wave_", 4);
            if (frames.Length < 2)
            {
                EditorUtility.DisplayDialog("Luxe Arena", "Thiếu audience wave textures.", "OK");
                return;
            }

            var layout = LoadOrCreateLayout();
            var layers = EnsureRect(bgRoot.transform, LayerRootName);
            StretchFull(layers);
            var audienceRoot = EnsureRect(layers, "AudienceBand");
            ApplyBand(audienceRoot, layout);
            CleanupDuplicateMiddles(audienceRoot);

            var left = BuildAudiencePanel(audienceRoot, "AudienceLeft", layout.Left, frames);
            var center = BuildAudiencePanel(audienceRoot, "AudienceCenter", layout.Center, frames);
            var right = BuildAudiencePanel(audienceRoot, "AudienceRight", layout.Right, frames);
            audienceRoot.Find("AudienceCenter")?.SetSiblingIndex(1);

            var director = bgRoot.GetComponent<LuxeArenaBackgroundDirector>();
            if (director == null)
            {
                director = Undo.AddComponent<LuxeArenaBackgroundDirector>(bgRoot);
            }

            WireAudiencePanelsToDirector(director, audienceRoot, frames);
            AssignWaveTextures(left, center, right, frames);
            EditorSceneManager.MarkSceneDirty(bgRoot.scene);
            EditorSceneManager.SaveScene(bgRoot.scene);
            Selection.activeGameObject = audienceRoot.gameObject;
            Debug.Log("[LuxeArena] Applied layout + waveform cho cả 3 panel L/C/R.");
        }

        [MenuItem("Fractured Chorus/Luxe Arena/Create Audience Center (hand-tune)")]
        public static void CreateAudienceCenterMenu()
        {
            ApplyLayoutAndWaveToAllThree();
            var band = FindAudienceBand();
            var center = band != null ? band.Find("AudienceCenter") : null;
            if (center != null)
            {
                Selection.activeGameObject = center.gameObject;
                EditorGUIUtility.PingObject(center.gameObject);
            }
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

            var baseImage = ResolveBaseImage(bgRoot);
            if (baseImage == null)
            {
                EditorUtility.DisplayDialog("Luxe Arena", "Không tìm thấy Image nền.", "OK");
                return;
            }

            var preview = LoadFirstSprite(PreviewPath);
            var frames = LoadWaveTextures(AudienceWaveDir, "luxe_arena_audience_wave_", 4);
            var cone = LoadFirstSprite(SoftConePath);
            if (preview == null || frames.Length == 0 || cone == null)
            {
                EditorUtility.DisplayDialog(
                    "Luxe Arena",
                    "Thiếu preview / audience wave / soft cone sprites.",
                    "OK");
                return;
            }

            Undo.RecordObject(baseImage, "Luxe Arena Base");
            baseImage.sprite = preview;
            baseImage.color = Color.white;
            baseImage.raycastTarget = false;
            EditorUtility.SetDirty(baseImage);

            var layers = EnsureRect(bgRoot.transform, LayerRootName);
            StretchFull(layers);
            DisableLegacy(layers);

            var spotRig = EnsureRect(layers, "SpotlightRig");
            StretchFull(spotRig);
            spotRig.SetAsFirstSibling();
            var spots = BuildSpotlights(spotRig, cone);

            var audienceRoot = EnsureRect(layers, "AudienceBand");
            audienceRoot.SetAsLastSibling();

            DisableOldFullAudience(layers);
            CleanupDuplicateMiddles(audienceRoot);

            var layout = LoadOrCreateLayout();
            ApplyBand(audienceRoot, layout);
            var left = BuildAudiencePanel(audienceRoot, "AudienceLeft", layout.Left, frames);
            var center = BuildAudiencePanel(audienceRoot, "AudienceCenter", layout.Center, frames);
            var right = BuildAudiencePanel(audienceRoot, "AudienceRight", layout.Right, frames);
            audienceRoot.Find("AudienceCenter")?.SetSiblingIndex(1);
            AssignWaveTextures(left, center, right, frames);

            var director = bgRoot.GetComponent<LuxeArenaBackgroundDirector>();
            if (director == null)
            {
                director = Undo.AddComponent<LuxeArenaBackgroundDirector>(bgRoot);
            }

            var so = new SerializedObject(director);
            so.FindProperty("baseImage").objectReferenceValue = baseImage;
            so.FindProperty("layersRoot").objectReferenceValue = layers;
            so.ApplyModifiedPropertiesWithoutUndo();
            WireAudiencePanelsToDirector(director, audienceRoot, frames);

            so = new SerializedObject(director);
            var spotsProp = so.FindProperty("spotlights");
            spotsProp.arraySize = spots.Length;
            for (var i = 0; i < spots.Length; i++)
            {
                var el = spotsProp.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("Transform").objectReferenceValue = spots[i].transform;
                el.FindPropertyRelative("Image").objectReferenceValue = spots[i].image;
                el.FindPropertyRelative("BaseAngle").floatValue = spots[i].baseAngle;
            }

            so.FindProperty("audienceFps").floatValue = 1.8f;
            so.FindProperty("audienceAlpha").floatValue = 0.5f;
            so.FindProperty("crossfadeSeconds").floatValue = 0.55f;
            so.FindProperty("enableSpotlightRig").boolValue = true;
            so.FindProperty("spotlightMaxAlpha").floatValue = 0.28f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(director);

            EditorSceneManager.MarkSceneDirty(bgRoot.scene);
            EditorSceneManager.SaveScene(bgRoot.scene);
            Selection.activeGameObject = audienceRoot.gameObject;
            Debug.Log("[LuxeArena] Wired L/C/R từ layout đã lưu + waveform cả 3 panel.");
        }

        private static void WireAudiencePanelsToDirector(
            LuxeArenaBackgroundDirector director,
            Transform audienceRoot,
            Texture2D[] frames)
        {
            var left = ResolvePanel(audienceRoot, "AudienceLeft");
            var center = ResolvePanel(audienceRoot, "AudienceCenter");
            var right = ResolvePanel(audienceRoot, "AudienceRight");

            var so = new SerializedObject(director);
            if (frames != null && frames.Length > 0)
            {
                so.FindProperty("audienceWaveFrames").arraySize = frames.Length;
                for (var i = 0; i < frames.Length; i++)
                {
                    so.FindProperty("audienceWaveFrames").GetArrayElementAtIndex(i).objectReferenceValue =
                        frames[i];
                }
            }

            var panelsProp = so.FindProperty("audiencePanels");
            var list = new System.Collections.Generic.List<(RawImage p, RawImage s)>(3);
            if (left.primary != null)
            {
                list.Add(left);
            }

            if (center.primary != null)
            {
                list.Add(center);
            }

            if (right.primary != null)
            {
                list.Add(right);
            }

            panelsProp.arraySize = list.Count;
            for (var i = 0; i < list.Count; i++)
            {
                WritePanel(panelsProp.GetArrayElementAtIndex(i), list[i].p, list[i].s);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(director);
        }

        private static (RawImage primary, RawImage secondary) ResolvePanel(Transform audienceRoot, string name)
        {
            var root = audienceRoot.Find(name);
            if (root == null)
            {
                return (null, null);
            }

            var primary = root.Find("Primary")?.GetComponent<RawImage>();
            var secondary = root.Find("Secondary")?.GetComponent<RawImage>();
            return (primary, secondary);
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

        private static void WritePanel(SerializedProperty panel, RawImage primary, RawImage secondary)
        {
            panel.FindPropertyRelative("Primary").objectReferenceValue = primary;
            panel.FindPropertyRelative("Secondary").objectReferenceValue = secondary;
            if (primary != null)
            {
                var uv = primary.uvRect;
                panel.FindPropertyRelative("WaveAnchorX").floatValue = Mathf.Clamp01(uv.x + uv.width * 0.5f);
            }
        }

        private static RectTransform FindAudienceBand()
        {
            var bg = GameObject.Find(CombatUiHierarchy.BackgroundCanvasName);
            return bg != null
                ? bg.transform.Find(LayerRootName + "/AudienceBand") as RectTransform
                : null;
        }

        private static LuxeArenaAudienceLayout LoadOrCreateLayout()
        {
            var layout = AssetDatabase.LoadAssetAtPath<LuxeArenaAudienceLayout>(LayoutPath);
            if (layout != null)
            {
                return layout;
            }

            layout = ScriptableObject.CreateInstance<LuxeArenaAudienceLayout>();
            AssetDatabase.CreateAsset(layout, LayoutPath);
            AssetDatabase.SaveAssets();
            return layout;
        }

        private static void CaptureBand(RectTransform band, LuxeArenaAudienceLayout layout)
        {
            layout.BandAnchorMin = band.anchorMin;
            layout.BandAnchorMax = band.anchorMax;
            layout.BandAnchoredPosition = band.anchoredPosition;
            layout.BandSizeDelta = band.sizeDelta;
        }

        private static void CapturePanel(RectTransform root, LuxeArenaAudienceLayout.PanelLayout panel)
        {
            if (root == null || panel == null)
            {
                return;
            }

            panel.AnchorMin = root.anchorMin;
            panel.AnchorMax = root.anchorMax;
            panel.AnchoredPosition = root.anchoredPosition;
            panel.SizeDelta = root.sizeDelta;
            var primary = root.Find("Primary")?.GetComponent<RawImage>();
            if (primary != null)
            {
                panel.UvRect = primary.uvRect;
            }
        }

        private static void ApplyBand(RectTransform band, LuxeArenaAudienceLayout layout)
        {
            Undo.RecordObject(band, "Apply Audience Band");
            band.anchorMin = layout.BandAnchorMin;
            band.anchorMax = layout.BandAnchorMax;
            band.anchoredPosition = layout.BandAnchoredPosition;
            band.sizeDelta = layout.BandSizeDelta;
            band.localScale = Vector3.one;
            band.localRotation = Quaternion.identity;
            EditorUtility.SetDirty(band);
        }

        private static void ApplyPanelRect(RectTransform root, LuxeArenaAudienceLayout.PanelLayout panel)
        {
            Undo.RecordObject(root, "Apply Audience Panel");
            root.anchorMin = panel.AnchorMin;
            root.anchorMax = panel.AnchorMax;
            root.anchoredPosition = panel.AnchoredPosition;
            root.sizeDelta = panel.SizeDelta;
            root.localScale = Vector3.one;
            root.localRotation = Quaternion.identity;
            EditorUtility.SetDirty(root);
        }

        private static void AssignWaveTextures(
            (RawImage primary, RawImage secondary) left,
            (RawImage primary, RawImage secondary) center,
            (RawImage primary, RawImage secondary) right,
            Texture2D[] frames)
        {
            var frame0 = frames[0];
            var frame1 = frames.Length > 1 ? frames[1] : frames[0];
            ApplyWavePair(left, frame0, frame1);
            ApplyWavePair(center, frame0, frame1);
            ApplyWavePair(right, frame0, frame1);
        }

        private static void ApplyWavePair(
            (RawImage primary, RawImage secondary) panel,
            Texture2D frame0,
            Texture2D frame1)
        {
            if (panel.primary != null)
            {
                Undo.RecordObject(panel.primary, "Wave Primary");
                panel.primary.texture = frame0;
                var c = panel.primary.color;
                c.a = 0.5f;
                panel.primary.color = c;
                EditorUtility.SetDirty(panel.primary);
            }

            if (panel.secondary != null)
            {
                Undo.RecordObject(panel.secondary, "Wave Secondary");
                panel.secondary.texture = frame1;
                var c = panel.secondary.color;
                c.a = 0f;
                panel.secondary.color = c;
                EditorUtility.SetDirty(panel.secondary);
            }
        }

        private static void DisableLegacy(Transform layers)
        {
            for (var i = 0; i < layers.childCount; i++)
            {
                var child = layers.GetChild(i);
                if (child.name.StartsWith("LightsSweep"))
                {
                    Undo.RecordObject(child.gameObject, "Disable legacy lights");
                    child.gameObject.SetActive(false);
                }
            }
        }

        private static void DisableOldFullAudience(Transform layers)
        {
            var old = layers.Find("AudienceWave");
            if (old != null)
            {
                Undo.RecordObject(old.gameObject, "Disable old AudienceWave");
                old.gameObject.SetActive(false);
            }

            var oldB = layers.Find("AudienceWaveB");
            if (oldB != null)
            {
                Undo.RecordObject(oldB.gameObject, "Disable old AudienceWaveB");
                oldB.gameObject.SetActive(false);
            }
        }

        private static (RawImage primary, RawImage secondary) BuildAudiencePanel(
            Transform parent,
            string name,
            LuxeArenaAudienceLayout.PanelLayout layout,
            Texture2D[] frames)
        {
            var root = EnsureRect(parent, name);
            ApplyPanelRect(root, layout);
            var frame0 = frames[0];
            var frame1 = frames.Length > 1 ? frames[1] : frames[0];
            var primary = EnsureRaw(root, "Primary", frame0, layout.UvRect, 0.5f);
            var secondary = EnsureRaw(root, "Secondary", frame1, layout.UvRect, 0f);
            return (primary, secondary);
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
            Sprite cone)
        {
            var result = new (RectTransform transform, Image image, float baseAngle)[SpotSeeds.Length];
            for (var i = 0; i < SpotSeeds.Length; i++)
            {
                var seed = SpotSeeds[i];
                var rt = EnsureRect(rig, "Spot_" + i);
                rt.anchorMin = new Vector2(seed.Ax, seed.Ay);
                rt.anchorMax = new Vector2(seed.Ax, seed.Ay);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(220f * seed.Scale, 520f * seed.Scale);
                rt.localRotation = Quaternion.Euler(0f, 0f, seed.Angle);

                var image = rt.GetComponent<Image>();
                if (image == null)
                {
                    image = Undo.AddComponent<Image>(rt.gameObject);
                }

                Undo.RecordObject(image, "Setup spot");
                image.sprite = cone;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.color = new Color(0.72f, 0.45f, 1f, 0.18f);
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
            SetAnchors(rt, Vector2.zero, Vector2.one);
        }

        private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        private static Texture2D[] LoadWaveTextures(string dir, string prefix, int count)
        {
            var list = new System.Collections.Generic.List<Texture2D>(count);
            for (var i = 1; i <= count; i++)
            {
                var path = $"{dir}/{prefix}{i:00}.png";
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null)
                {
                    list.Add(tex);
                }
            }

            return list.ToArray();
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

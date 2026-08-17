#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using FracturedChorus.Data;
using FracturedChorus.RunMap.Core;
using FracturedChorus.RunMap.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class RunMapPlayerMarkerSetupEditor
    {
        private const string SourceChibiPath =
            "Assets/FracturedChorus/Art/Characters/Ren/Chibi/ren_chibi_fullbody_v1.png";
        private const string PlayerArtRoot = "Assets/FracturedChorus/Art/UI/RunMap/Player/";
        private const string IdleSpritePath = PlayerArtRoot + "runmap_ren_chibi_idle_v1.png";
        private const string TravelSpritePath = PlayerArtRoot + "runmap_ren_chibi_travel_v1.png";
        private const string ConfigPath =
            "Assets/FracturedChorus/Data/ScriptableObjects/Presets/RunMapPlayerMarker_Default.asset";
        private const string LayoutConfigPath =
            "Assets/FracturedChorus/Data/ScriptableObjects/Presets/RunMapLayout_Default.asset";

        private const string PinMarkerPath = PlayerArtRoot + "runmap_ren_pin_marker_v1.png";
        private const string PinBuildScript = "Tools/build-runmap-ren-pin-marker.mjs";

        [MenuItem("Fractured Chorus/Run Map/Process Ren Player Sprites", false, 37)]
        public static void ProcessRenPlayerSprites()
        {
            Directory.CreateDirectory(PlayerArtRoot);

            if (!TryBuildRenPinMarker())
            {
                return;
            }

            AssetDatabase.ImportAsset(PinMarkerPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(IdleSpritePath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(TravelSpritePath, ImportAssetOptions.ForceUpdate);
            ConfigureSpriteImport(PinMarkerPath);
            ConfigureSpriteImport(IdleSpritePath);
            ConfigureSpriteImport(TravelSpritePath);

            var config = EnsureConfigAsset();
            var marker = AssetDatabase.LoadAssetAtPath<Sprite>(PinMarkerPath);
            var so = new SerializedObject(config);
            so.FindProperty("idleSprite").objectReferenceValue = marker;
            so.FindProperty("travelSprite").objectReferenceValue = marker;
            so.FindProperty("markerSize").vector2Value = new Vector2(53f, 73f);
            so.FindProperty("footOffset").vector2Value = Vector2.zero;
            so.FindProperty("jumpHeight").floatValue = 50f;
            so.FindProperty("travelDuration").floatValue = 0.35f;
            so.FindProperty("travelDuration").floatValue = 0.35f;
            so.FindProperty("spinTurns").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);

            WireSceneMarkers(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[Fractured Chorus] Ren pin map marker built and wired.");
        }

        private static bool TryBuildRenPinMarker()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var scriptPath = Path.Combine(projectRoot, PinBuildScript);
            if (!File.Exists(scriptPath))
            {
                Debug.LogError($"[Fractured Chorus] Missing pin build script: {scriptPath}");
                return false;
            }

            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = $"\"{scriptPath}\"",
                    WorkingDirectory = projectRoot,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null)
                {
                    Debug.LogError("[Fractured Chorus] Failed to start node for pin marker build.");
                    return false;
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    Debug.LogError($"[Fractured Chorus] Pin marker build failed:\n{error}\n{output}");
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(output))
                {
                    Debug.Log($"[Fractured Chorus] {output.Trim()}");
                }

                return File.Exists(Path.GetFullPath(Path.Combine(projectRoot, PinMarkerPath)));
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Fractured Chorus] Pin marker build error: {ex.Message}");
                return false;
            }
        }

        public static RunMapPlayerMarkerConfigSO EnsureConfigAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RunMapPlayerMarkerConfigSO>(ConfigPath);
            if (existing != null)
            {
                return existing;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            var config = ScriptableObject.CreateInstance<RunMapPlayerMarkerConfigSO>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        public static void WireSceneMarkers(RunMapPlayerMarkerConfigSO config = null)
        {
            config ??= EnsureConfigAsset();

            foreach (var mapView in Object.FindObjectsByType<RunMapUIView>(FindObjectsInactive.Include))
            {
                EnsurePlayerMarkerLayer(mapView, config);
                var scrollRect = mapView.GetComponentInParent<ScrollRect>();
                var content = scrollRect != null ? scrollRect.content : mapView.transform as RectTransform;
                var layer = content != null ? content.Find("PlayerMarkerLayer") as RectTransform : null;
                var marker = layer != null
                    ? layer.Find("RenMarker")?.GetComponent<RunMapPlayerMarkerView>()
                    : mapView.GetComponentInChildren<RunMapPlayerMarkerView>(true);
                var so = new SerializedObject(mapView);
                so.FindProperty("playerMarkerConfig").objectReferenceValue = config;
                so.FindProperty("playerMarkerLayer").objectReferenceValue = layer;
                so.FindProperty("playerMarker").objectReferenceValue = marker;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(mapView);
            }

            EditorSceneManager.MarkAllScenesDirty();
        }

        private static void EnsurePlayerMarkerLayer(RunMapUIView mapView, RunMapPlayerMarkerConfigSO config)
        {
            var scrollRect = mapView.GetComponentInParent<ScrollRect>();
            var content = scrollRect != null ? scrollRect.content : mapView.transform as RectTransform;
            if (content == null)
            {
                return;
            }

            RemoveDuplicatePlayerMarkerLayers(content, scrollRect?.viewport);

            var layer = content.Find("PlayerMarkerLayer") as RectTransform;
            if (layer == null)
            {
                var go = new GameObject("PlayerMarkerLayer", typeof(RectTransform));
                go.transform.SetParent(content, false);
                layer = go.transform as RectTransform;
            }
            else if (layer.parent != content)
            {
                layer.SetParent(content, false);
            }

            ApplyContentLayerRect(layer);
            RemoveNestedCanvas(layer);

            MigrateLegacyMarkerOnLayer(layer, config);

            var renTransform = layer.Find("RenMarker");
            if (renTransform == null)
            {
                var go = new GameObject(
                    "RenMarker",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(RunMapPlayerMarkerView));
                go.transform.SetParent(layer, false);
                renTransform = go.transform;
            }
            else
            {
                if (renTransform.GetComponent<Image>() == null)
                {
                    renTransform.gameObject.AddComponent<Image>();
                }

                if (renTransform.GetComponent<RunMapPlayerMarkerView>() == null)
                {
                    renTransform.gameObject.AddComponent<RunMapPlayerMarkerView>();
                }
            }

            var renRect = renTransform as RectTransform;
            if (renRect != null)
            {
                renRect.anchorMin = new Vector2(0.5f, 0f);
                renRect.anchorMax = new Vector2(0.5f, 0f);
                renRect.pivot = new Vector2(0.5f, 0f);
                renRect.sizeDelta = config.MarkerSize;
            }

            var markerViews = renTransform.GetComponents<RunMapPlayerMarkerView>();
            for (var i = 1; i < markerViews.Length; i++)
            {
                Object.DestroyImmediate(markerViews[i]);
            }

            var markerView = renTransform.GetComponent<RunMapPlayerMarkerView>();
            markerView.Configure(config);
            markerView.SetVisible(true);
            layer.SetAsLastSibling();
            mapView.EnsureEditModePlayerMarker();
        }

        private static void RemoveDuplicatePlayerMarkerLayers(RectTransform content, RectTransform viewport)
        {
            var layers = new System.Collections.Generic.List<Transform>();
            CollectPlayerMarkerLayer(content, layers);
            if (viewport != null && viewport != content)
            {
                CollectPlayerMarkerLayer(viewport, layers);
            }

            if (layers.Count <= 1)
            {
                return;
            }

            Transform keep = content.Find("PlayerMarkerLayer");
            keep ??= layers[0];

            foreach (var layer in layers)
            {
                if (layer == keep)
                {
                    continue;
                }

                var ren = layer.Find("RenMarker");
                if (ren != null && keep.Find("RenMarker") == null)
                {
                    ren.SetParent(keep, false);
                }

                Object.DestroyImmediate(layer.gameObject);
            }
        }

        private static void CollectPlayerMarkerLayer(Transform root, System.Collections.Generic.List<Transform> results)
        {
            if (root == null)
            {
                return;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == "PlayerMarkerLayer")
                {
                    results.Add(child);
                }
            }
        }

        private static void ApplyContentLayerRect(RectTransform layer)
        {
            if (layer == null)
            {
                return;
            }

            layer.anchorMin = new Vector2(0.5f, 0f);
            layer.anchorMax = new Vector2(0.5f, 0f);
            layer.pivot = new Vector2(0.5f, 0f);
            layer.anchoredPosition = Vector2.zero;
        }

        private static void RemoveNestedCanvas(Transform layer)
        {
            var nestedCanvas = layer.GetComponent<Canvas>();
            if (nestedCanvas != null)
            {
                Object.DestroyImmediate(nestedCanvas);
            }
        }

        private static void MigrateLegacyMarkerOnLayer(Transform layer, RunMapPlayerMarkerConfigSO config)
        {
            if (layer == null || layer.Find("RenMarker") != null)
            {
                return;
            }

            var legacyView = layer.GetComponent<RunMapPlayerMarkerView>();
            var legacyImage = layer.GetComponent<Image>();
            if (legacyView == null && legacyImage == null)
            {
                return;
            }

            var renGo = new GameObject(
                "RenMarker",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(RunMapPlayerMarkerView));
            renGo.transform.SetParent(layer, false);
            var renRect = renGo.GetComponent<RectTransform>();
            renRect.anchorMin = new Vector2(0.5f, 0f);
            renRect.anchorMax = new Vector2(0.5f, 0f);
            renRect.pivot = new Vector2(0.5f, 0f);
            renRect.sizeDelta = config != null ? config.MarkerSize : new Vector2(72f, 96f);

            var renImage = renGo.GetComponent<Image>();
            if (legacyImage != null)
            {
                renImage.sprite = legacyImage.sprite;
                renImage.color = legacyImage.color;
                renImage.preserveAspect = legacyImage.preserveAspect;
                renImage.raycastTarget = false;
            }

            var renView = renGo.GetComponent<RunMapPlayerMarkerView>();
            renView.Configure(config);

            if (legacyView != null)
            {
                Object.DestroyImmediate(legacyView);
            }

            if (legacyImage != null)
            {
                Object.DestroyImmediate(legacyImage);
            }
        }

        private static bool TryExportTransparentSprite(string sourceAssetPath, string outputAssetPath)
        {
            var importer = AssetImporter.GetAtPath(sourceAssetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[Fractured Chorus] Missing source texture: {sourceAssetPath}");
                return false;
            }

            var wasReadable = importer.isReadable;
            if (!wasReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            var source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourceAssetPath);
            if (source == null)
            {
                Debug.LogError($"[Fractured Chorus] Could not load {sourceAssetPath}");
                return false;
            }

            const int floodTolerance = 22;
            const int edgeFeather = 16;
            const int cropPad = 16;

            var width = source.width;
            var height = source.height;
            var pixels = source.GetPixels32();
            var bg = SampleCornerBackgroundColor(pixels, width, height);
            var backgroundMask = FloodBackgroundMask(pixels, width, height, bg, floodTolerance);

            var minX = width;
            var minY = height;
            var maxX = -1;
            var maxY = -1;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = y * width + x;
                    var p = pixels[index];
                    byte alpha;
                    if (backgroundMask[index])
                    {
                        alpha = 0;
                    }
                    else
                    {
                        alpha = AlphaForForegroundPixel(p, bg, floodTolerance, edgeFeather);
                    }

                    if (alpha <= 0)
                    {
                        pixels[index] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    var unmul = UnmultiplyRgb(p, alpha);
                    pixels[index] = new Color32(unmul.r, unmul.g, unmul.b, alpha);

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < 0)
            {
                Debug.LogError($"[Fractured Chorus] Matte removed all pixels from {sourceAssetPath}");
                return false;
            }

            minX = Mathf.Max(0, minX - cropPad);
            minY = Mathf.Max(0, minY - cropPad);
            maxX = Mathf.Min(width - 1, maxX + cropPad);
            maxY = Mathf.Min(height - 1, maxY + cropPad);

            var cropWidth = maxX - minX + 1;
            var cropHeight = maxY - minY + 1;
            var cropped = new Color32[cropWidth * cropHeight];
            for (var y = 0; y < cropHeight; y++)
            {
                for (var x = 0; x < cropWidth; x++)
                {
                    cropped[y * cropWidth + x] = pixels[(minY + y) * width + (minX + x)];
                }
            }

            var output = new Texture2D(cropWidth, cropHeight, TextureFormat.RGBA32, false);
            output.SetPixels32(cropped);
            output.Apply();

            var bytes = output.EncodeToPNG();
            File.WriteAllBytes(Path.GetFullPath(outputAssetPath), bytes);
            Object.DestroyImmediate(output);

            if (!wasReadable)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }

            return true;
        }

        private static Color32 SampleCornerBackgroundColor(Color32[] pixels, int width, int height)
        {
            var samples = new[]
            {
                pixels[0],
                pixels[width - 1],
                pixels[(height - 1) * width],
                pixels[height * width - 1],
            };

            var r = 0;
            var g = 0;
            var b = 0;
            foreach (var sample in samples)
            {
                r += sample.r;
                g += sample.g;
                b += sample.b;
            }

            return new Color32((byte)(r / samples.Length), (byte)(g / samples.Length), (byte)(b / samples.Length), 255);
        }

        private static bool[] FloodBackgroundMask(Color32[] pixels, int width, int height, Color32 bg, int tolerance)
        {
            var total = width * height;
            var isBackground = new bool[total];
            var queue = new Queue<int>();

            void TrySeed(int x, int y)
            {
                var index = y * width + x;
                if (isBackground[index] || !MatchesBackground(pixels[index], bg, tolerance))
                {
                    return;
                }

                isBackground[index] = true;
                queue.Enqueue(index);
            }

            for (var x = 0; x < width; x++)
            {
                TrySeed(x, 0);
                TrySeed(x, height - 1);
            }

            for (var y = 0; y < height; y++)
            {
                TrySeed(0, y);
                TrySeed(width - 1, y);
            }

            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                var x = index % width;
                var y = index / width;

                if (x > 0)
                {
                    EnqueueNeighbor(index - 1, pixels, isBackground, queue, bg, tolerance);
                }

                if (x < width - 1)
                {
                    EnqueueNeighbor(index + 1, pixels, isBackground, queue, bg, tolerance);
                }

                if (y > 0)
                {
                    EnqueueNeighbor(index - width, pixels, isBackground, queue, bg, tolerance);
                }

                if (y < height - 1)
                {
                    EnqueueNeighbor(index + width, pixels, isBackground, queue, bg, tolerance);
                }
            }

            return isBackground;
        }

        private static void EnqueueNeighbor(
            int index,
            Color32[] pixels,
            bool[] isBackground,
            Queue<int> queue,
            Color32 bg,
            int tolerance)
        {
            if (isBackground[index] || !MatchesBackground(pixels[index], bg, tolerance))
            {
                return;
            }

            isBackground[index] = true;
            queue.Enqueue(index);
        }

        private static bool MatchesBackground(Color32 pixel, Color32 bg, int tolerance)
        {
            var dist = Mathf.Abs(pixel.r - bg.r) + Mathf.Abs(pixel.g - bg.g) + Mathf.Abs(pixel.b - bg.b);
            return dist <= tolerance;
        }

        private static byte AlphaForForegroundPixel(Color32 pixel, Color32 bg, int tolerance, int feather)
        {
            var dist = Mathf.Abs(pixel.r - bg.r) + Mathf.Abs(pixel.g - bg.g) + Mathf.Abs(pixel.b - bg.b);
            if (dist <= tolerance)
            {
                return 0;
            }

            if (dist >= tolerance + feather)
            {
                return 255;
            }

            return (byte)Mathf.RoundToInt((dist - tolerance) / (float)feather * 255f);
        }

        private static Color32 UnmultiplyRgb(Color32 pixel, byte alpha)
        {
            if (alpha <= 0)
            {
                return new Color32(0, 0, 0, 0);
            }

            if (alpha >= 255)
            {
                return new Color32(pixel.r, pixel.g, pixel.b, alpha);
            }

            var a = alpha / 255f;
            return new Color32(
                (byte)Mathf.Min(255, Mathf.RoundToInt(pixel.r / a)),
                (byte)Mathf.Min(255, Mathf.RoundToInt(pixel.g / a)),
                (byte)Mathf.Min(255, Mathf.RoundToInt(pixel.b / a)),
                alpha);
        }

        private static void ConfigureSpriteImport(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.maxTextureSize = 1024;
            importer.spritePivot = new Vector2(0.5f, 0f);
            importer.SaveAndReimport();
        }

        [MenuItem("Fractured Chorus/Run Map/Wire Map Layout", false, 38)]
        public static void WireMapLayout()
        {
            var layout = EnsureLayoutAsset();
            var markerConfig = EnsureConfigAsset();
            WireSceneMarkers(markerConfig);

            foreach (var mapView in Object.FindObjectsByType<RunMapUIView>(FindObjectsInactive.Include))
            {
                WireMapView(mapView, layout, markerConfig);
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[Fractured Chorus] Wired Run Map layout + scene preview + Ren marker.");
        }

        public static RunMapLayoutConfigSO EnsureLayoutAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RunMapLayoutConfigSO>(LayoutConfigPath);
            if (existing != null)
            {
                return existing;
            }

            var layout = ScriptableObject.CreateInstance<RunMapLayoutConfigSO>();
            layout.ResetToDefaults();
            AssetDatabase.CreateAsset(layout, LayoutConfigPath);
            AssetDatabase.SaveAssets();
            return layout;
        }

        public static void WireMapView(
            RunMapUIView mapView,
            RunMapLayoutConfigSO layout = null,
            RunMapPlayerMarkerConfigSO markerConfig = null)
        {
            if (mapView == null)
            {
                return;
            }

            layout ??= EnsureLayoutAsset();
            markerConfig ??= EnsureConfigAsset();

            var preview = mapView.GetComponent<RunMapLayoutScenePreview>();
            if (preview == null)
            {
                preview = mapView.gameObject.AddComponent<RunMapLayoutScenePreview>();
            }

            var mapSo = new SerializedObject(mapView);
            mapSo.FindProperty("layoutConfig").objectReferenceValue = layout;
            mapSo.ApplyModifiedPropertiesWithoutUndo();

            var iconSet = mapSo.FindProperty("iconSet").objectReferenceValue as MapNodeIconSetSO;
            var templates = mapSo.FindProperty("templateSet")?.objectReferenceValue as MapNodeTemplateSetSO;
            preview.Configure(layout, iconSet, markerConfig, mapView, templates);
            SnapMarkerToStart(mapView, layout, markerConfig);

            EditorUtility.SetDirty(mapView);
            EditorUtility.SetDirty(preview);
        }

        private static void SnapMarkerToStart(
            RunMapUIView mapView,
            RunMapLayoutConfigSO layout,
            RunMapPlayerMarkerConfigSO markerConfig)
        {
            if (mapView == null || layout == null || markerConfig == null)
            {
                return;
            }

            mapView.EnsureEditModePlayerMarker();
        }
    }

    [CustomEditor(typeof(RunMapLayoutConfigSO))]
    public sealed class RunMapLayoutConfigSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Apply To Scene Preview"))
            {
                ApplyToScenePreviews();
                RunMapPlayerMarkerSetupEditor.WireMapLayout();
            }

            if (GUI.changed)
            {
                ApplyToScenePreviews();
            }
        }

        private static void ApplyToScenePreviews()
        {
            foreach (var preview in Object.FindObjectsByType<RunMapLayoutScenePreview>(FindObjectsInactive.Include))
            {
                preview.Rebuild();
            }
        }
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class LoadingScreenArtImportEditor
    {
        private const string SourceSheetPath = @"C:\Users\Asus\.cursor\projects\d-Fractured-Chorus1\assets\c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_Loading_Screen_Part-421ee341-baf0-4f90-b671-0648ba5ede20.png";
        private const string WishSheetPath = @"C:\Users\Asus\.cursor\projects\d-Fractured-Chorus1\assets\c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_Loading_Screen_Wissh-83a9b4f3-e7f0-4282-a864-e37007175254.png";
        private const string OutputFolder = "Assets/FracturedChorus/Art/UI/LoadingScreen";
        private const string SourceFolder = OutputFolder + "/_source";
        private const string PartCopyPath = SourceFolder + "/loading_screen_part.jpg";
        private const string WishCopyPath = SourceFolder + "/loading_screen_wish.jpg";
        private const int AlphaThreshold = 18;
        private const int MinBlobArea = 80;
        private const int BuildingsMinArea = 200;
        private const int CropPadding = 4;

        [MenuItem("Fractured Chorus/Import Loading Screen Art")]
        public static void Import()
        {
            try
            {
                EnsureFolder(OutputFolder);
                EnsureFolder(SourceFolder);

                CopySource(SourceSheetPath, PartCopyPath);
                CopySource(WishSheetPath, WishCopyPath);

                var bytes = File.ReadAllBytes(ToFullPath(PartCopyPath));
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes))
                {
                    throw new InvalidOperationException("Unable to decode loading screen component sheet.");
                }

                WriteLayers(texture, ExtractBlobs(texture));
                ConfigureTextureImporter(PartCopyPath);
                ConfigureTextureImporter(WishCopyPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                UnityEngine.Object.DestroyImmediate(texture);
                Debug.Log("[Fractured Chorus] Loading screen art imported.");
            }
            catch (Exception error)
            {
                Debug.LogError("[Fractured Chorus] Loading screen art import failed: " + error);
                EditorUtility.DisplayDialog(
                    "Import Loading Screen Art",
                    "Import failed. Check Console for details.",
                    "OK");
            }
        }

        private static void CopySource(string sourcePath, string assetPath)
        {
            var targetPath = ToFullPath(assetPath);
            File.Copy(sourcePath, targetPath, true);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        private static List<Blob> ExtractBlobs(Texture2D texture)
        {
            var width = texture.width;
            var height = texture.height;
            var pixels = texture.GetPixels32();
            var opaque = new bool[pixels.Length];
            for (var i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                opaque[i] = Math.Max(pixel.r, Math.Max(pixel.g, pixel.b)) >= AlphaThreshold;
            }

            var visited = new bool[pixels.Length];
            var blobs = new List<Blob>();
            var queue = new Queue<int>();
            var neighbors = new[] { new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(0, 1) };
            var nextId = 0;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = x + (y * width);
                    if (visited[index] || !opaque[index])
                    {
                        continue;
                    }

                    visited[index] = true;
                    queue.Enqueue(index);
                    var blob = new Blob(width, height);

                    while (queue.Count > 0)
                    {
                        var current = queue.Dequeue();
                        var cx = current % width;
                        var cy = current / width;
                        blob.AddPixel(cx, cy, current);

                        for (var i = 0; i < neighbors.Length; i++)
                        {
                            var nx = cx + neighbors[i].x;
                            var ny = cy + neighbors[i].y;
                            if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                            {
                                continue;
                            }

                            var neighborIndex = nx + (ny * width);
                            if (visited[neighborIndex] || !opaque[neighborIndex])
                            {
                                continue;
                            }

                            visited[neighborIndex] = true;
                            queue.Enqueue(neighborIndex);
                        }
                    }

                    if (blob.Area < MinBlobArea || IsUiBar(blob, width, height))
                    {
                        continue;
                    }

                    blob.Id = nextId;
                    nextId++;
                    blobs.Add(blob);
                }
            }

            return blobs;
        }

        private static bool IsUiBar(Blob blob, int width, int height)
        {
            return blob.CentroidYBottom > 0.78f * height
                   && blob.CentroidX > 0.58f * width
                   && ((float)blob.Width / Math.Max(1, blob.Height)) > 2.2f;
        }

        private static void WriteLayers(Texture2D source, List<Blob> blobs)
        {
            var width = source.width;
            var height = source.height;
            Blob clefSource = null;
            Blob skylineSource = null;
            Blob floor = null;
            var clouds = new List<Blob>();
            var notes = new List<Blob>();
            var buildings = new List<Blob>();

            for (var i = 0; i < blobs.Count; i++)
            {
                var blob = blobs[i];
                if (blob.CentroidTopY < 0.28f * height &&
                    blob.CentroidX < 0.42f * width &&
                    (blob.Width > 100 || blob.Height > 40 || blob.Area > 1500))
                {
                    clouds.Add(blob);
                    continue;
                }

                if (blob.CentroidTopY < 0.40f * height &&
                    blob.CentroidX > 0.40f * width &&
                    blob.Width < 120 &&
                    blob.Height < 160 &&
                    blob.Area < 8000)
                {
                    notes.Add(blob);
                    continue;
                }

                if (blob.Width > 0.45f * width &&
                    (skylineSource == null || blob.Width > skylineSource.Width))
                {
                    skylineSource = blob;
                }

                if (blob.CentroidX >= 0.28f * width &&
                    blob.CentroidX <= 0.72f * width &&
                    blob.CentroidYBottom >= 0.55f * height &&
                    (clefSource == null || blob.Area > clefSource.Area))
                {
                    clefSource = blob;
                }

                if (blob.CentroidTopY > 0.58f * height &&
                    (floor == null || blob.Area > floor.Area))
                {
                    floor = blob;
                }
            }

            for (var i = 0; i < blobs.Count; i++)
            {
                var blob = blobs[i];
                if (Contains(clouds, blob) || Contains(notes, blob))
                {
                    continue;
                }

                if (floor != null && blob.Id == floor.Id)
                {
                    continue;
                }

                if (blob.Area > BuildingsMinArea &&
                    (clefSource == null || blob.Id != clefSource.Id))
                {
                    buildings.Add(blob);
                }
            }

            WriteBlobLayer(source, "loading_clouds.png", clouds);
            WriteBlobLayer(source, "loading_notes_stars.png", notes);
            WriteMaskedLayer(
                source,
                "loading_skyline.png",
                skylineSource,
                (x, topY, w, h) => x < 0.66f * w && topY >= 0.30f * h && topY <= 0.64f * h);
            WriteBlobLayer(source, "loading_buildings_signs.png", buildings);
            WriteMaskedLayer(
                source,
                "loading_clef.png",
                clefSource,
                (x, topY, w, h) => x >= 0.24f * w && x <= 0.76f * w && topY >= 0.03f * h && topY <= 0.37f * h);
            WriteBlobLayer(source, "loading_floor.png", new List<Blob> { floor });
        }

        private static void ConfigureTextureImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static bool Contains(List<Blob> list, Blob blob)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Id == blob.Id)
                {
                    return true;
                }
            }

            return false;
        }

        private static void WriteBlobLayer(Texture2D source, string fileName, List<Blob> blobs)
        {
            if (blobs == null || blobs.Count == 0)
            {
                throw new InvalidOperationException("Layer classification produced no blobs for " + fileName);
            }

            var bounds = blobs[0].Bounds;
            for (var i = 1; i < blobs.Count; i++)
            {
                bounds = Union(bounds, blobs[i].Bounds);
            }

            bounds.xMin = Math.Max(0, bounds.xMin - CropPadding);
            bounds.yMin = Math.Max(0, bounds.yMin - CropPadding);
            bounds.xMax = Math.Min(source.width, bounds.xMax + CropPadding);
            bounds.yMax = Math.Min(source.height, bounds.yMax + CropPadding);

            var output = new Texture2D(bounds.width, bounds.height, TextureFormat.RGBA32, false);
            var outputPixels = new Color32[bounds.width * bounds.height];
            var sourcePixels = source.GetPixels32();
            for (var i = 0; i < blobs.Count; i++)
            {
                var blob = blobs[i];
                for (var j = 0; j < blob.PixelIndices.Count; j++)
                {
                    var index = blob.PixelIndices[j];
                    var x = index % source.width;
                    var y = index / source.width;
                    var localX = x - bounds.xMin;
                    var localY = y - bounds.yMin;
                    outputPixels[localX + (localY * bounds.width)] = sourcePixels[index];
                }
            }

            output.SetPixels32(outputPixels);
            output.Apply(false, false);
            SaveOutput(output, fileName);
        }

        private static void WriteMaskedLayer(Texture2D source, string fileName, Blob blob, PixelPredicate predicate)
        {
            if (blob == null)
            {
                throw new InvalidOperationException("Missing blob for " + fileName);
            }

            var selected = new List<int>();
            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;
            for (var i = 0; i < blob.PixelIndices.Count; i++)
            {
                var index = blob.PixelIndices[i];
                var x = index % source.width;
                var y = index / source.width;
                var topY = (source.height - 1) - y;
                if (!predicate(x, topY, source.width, source.height))
                {
                    continue;
                }

                selected.Add(index);
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }

            if (selected.Count == 0)
            {
                throw new InvalidOperationException("Layer mask produced no pixels for " + fileName);
            }

            var bounds = RectInt.MinMaxRect(
                Math.Max(0, minX - CropPadding),
                Math.Max(0, minY - CropPadding),
                Math.Min(source.width, maxX + CropPadding + 1),
                Math.Min(source.height, maxY + CropPadding + 1));

            var output = new Texture2D(bounds.width, bounds.height, TextureFormat.RGBA32, false);
            var outputPixels = new Color32[bounds.width * bounds.height];
            var sourcePixels = source.GetPixels32();
            for (var i = 0; i < selected.Count; i++)
            {
                var index = selected[i];
                var x = index % source.width;
                var y = index / source.width;
                outputPixels[(x - bounds.xMin) + ((y - bounds.yMin) * bounds.width)] = sourcePixels[index];
            }

            output.SetPixels32(outputPixels);
            output.Apply(false, false);
            SaveOutput(output, fileName);
        }

        private static void SaveOutput(Texture2D output, string fileName)
        {
            var assetPath = OutputFolder + "/" + fileName;
            File.WriteAllBytes(ToFullPath(assetPath), output.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(output);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            ConfigureTextureImporter(assetPath);
        }

        private static RectInt Union(RectInt a, RectInt b)
        {
            var xMin = Math.Min(a.xMin, b.xMin);
            var yMin = Math.Min(a.yMin, b.yMin);
            var xMax = Math.Max(a.xMax, b.xMax);
            var yMax = Math.Max(a.yMax, b.yMax);
            return RectInt.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static string ToFullPath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parts = assetPath.Split('/');
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

        private sealed class Blob
        {
            public int Id;
            public readonly List<int> PixelIndices = new List<int>();
            private readonly int height;
            private int maxX = int.MinValue;
            private int maxY = int.MinValue;
            private int minX = int.MaxValue;
            private int minY = int.MaxValue;
            private long sumX;
            private long sumYBottom;
            private long sumYTop;

            public Blob(int width, int height)
            {
                this.height = height;
            }

            public int Area => PixelIndices.Count;
            public int Width => maxX - minX + 1;
            public int Height => maxY - minY + 1;
            public float CentroidX => Area == 0 ? 0f : (float)sumX / Area;
            public float CentroidYBottom => Area == 0 ? 0f : (float)sumYBottom / Area;
            public float CentroidTopY => Area == 0 ? 0f : (float)sumYTop / Area;
            public RectInt Bounds => new RectInt(minX, minY, Width, Height);

            public void AddPixel(int x, int y, int index)
            {
                PixelIndices.Add(index);
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
                sumX += x;
                sumYBottom += y;
                sumYTop += (height - 1) - y;
            }
        }

        private delegate bool PixelPredicate(int x, int topY, int width, int height);
    }
}
#endif

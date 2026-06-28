#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    /// <summary>
    /// Slice Charlotte idle ref sheets at 727×850 (match LCB_Sinner_Ishmael_Idle_Sprite) and rebuild Charlott_Idle clip.
    /// </summary>
    public static class CharlotteIdleSheetSetup
    {
        private const string IdleFolder = "Assets/FracturedChorus/Art/Characters/Charlotte/Animation/Idle";
        private const string ClipPath = "Assets/FracturedChorus/Art/Characters/Charlotte/Animation/Charlott_Idle.anim";
        private const int CellW = 727;
        private const int CellH = 850;
        private const int FramesPerSheet = 10;
        private const float PixelsPerUnit = 100f;
        private const float FrameDurationSec = 0.05f; // GIF 50 ms/frame

        [MenuItem("Fractured Chorus/Charlotte/Import Idle Sheets (727×850 slice)")]
        public static void ImportIdleSheets()
        {
            var sheetPaths = FindSheetPaths();
            if (sheetPaths.Count == 0)
            {
                Debug.LogError(
                    "[Fractured Chorus] No Charlotte_idle_ref_sheet_*_10f.png in Animation/Idle. " +
                    "Run scripts/charlotte_ishmael_idle_sheets.py first.");
                return;
            }

            var frameOffset = 0;
            foreach (var path in sheetPaths)
            {
                SliceSheet(path, frameOffset);
                frameOffset += FramesPerSheet;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Fractured Chorus] Sliced {sheetPaths.Count} idle sheet(s) @ {CellW}×{CellH}, PPU {PixelsPerUnit}.");
        }

        [MenuItem("Fractured Chorus/Charlotte/Rebuild Charlott_Idle Animation Clip")]
        public static void RebuildIdleClip()
        {
            var sprites = LoadOrderedIdleSprites();
            if (sprites.Count == 0)
            {
                Debug.LogError("[Fractured Chorus] No Charlotte_Idle_XX sprites found. Run Import Idle Sheets first.");
                return;
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "Charlott_Idle" };
                AssetDatabase.CreateAsset(clip, ClipPath);
            }

            var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[sprites.Count];
            for (var i = 0; i < sprites.Count; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i * FrameDurationSec,
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            clip.frameRate = 1f / FrameDurationSec;
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Fractured Chorus] Rebuilt {ClipPath} with {sprites.Count} frames @ {FrameDurationSec * 1000f}ms.");
        }

        [MenuItem("Fractured Chorus/Charlotte/Import Idle Sheets + Rebuild Clip")]
        public static void ImportAndRebuild()
        {
            ImportIdleSheets();
            RebuildIdleClip();
        }

        private static List<string> FindSheetPaths()
        {
            if (!AssetDatabase.IsValidFolder(IdleFolder))
            {
                return new List<string>();
            }

            return AssetDatabase.FindAssets("Charlotte_idle_ref_sheet_", new[] { IdleFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith("_10f.png", System.StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p, System.StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void SliceSheet(string assetPath, int frameOffset)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[Fractured Chorus] Not a texture: {assetPath}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsToUnits = PixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;

            var metas = new SpriteMetaData[FramesPerSheet];
            for (var i = 0; i < FramesPerSheet; i++)
            {
                metas[i] = new SpriteMetaData
                {
                    name = $"Charlotte_Idle_{frameOffset + i + 1:00}",
                    rect = new Rect(i * CellW, 0, CellW, CellH),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = Vector2.zero,
                    border = Vector4.zero
                };
            }

            importer.spritesheet = metas;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        private static List<Sprite> LoadOrderedIdleSprites()
        {
            var byIndex = new SortedDictionary<int, Sprite>();

            foreach (var sheetPath in FindSheetPaths())
            {
                foreach (var obj in AssetDatabase.LoadAllAssetRepresentationsAtPath(sheetPath))
                {
                    if (obj is not Sprite sprite)
                    {
                        continue;
                    }

                    if (!sprite.name.StartsWith("Charlotte_Idle_", System.StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var suffix = sprite.name.Substring("Charlotte_Idle_".Length);
                    if (int.TryParse(suffix, out var index))
                    {
                        byIndex[index] = sprite;
                    }
                }
            }

            return byIndex.Values.ToList();
        }
    }
}
#endif

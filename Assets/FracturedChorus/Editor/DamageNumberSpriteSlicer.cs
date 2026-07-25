using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class DamageNumberSpriteSlicer
    {
        private static readonly string[] SheetPaths =
        {
            "Assets/FracturedChorus/Resources/UI/Combat/DamageNumbers/combat_dmg_digits_holo_v2.png",
            "Assets/FracturedChorus/Resources/UI/Combat/DamageNumbers/combat_dmg_digits_holo_v1.png",
            "Assets/FracturedChorus/Resources/UI/Combat/DamageNumbers/combat_heal_digits_holo_v1.png",
            "Assets/FracturedChorus/Art/UI/Combat/DamageNumbers/combat_dmg_digits_holo_v2.png",
            "Assets/FracturedChorus/Art/UI/Combat/DamageNumbers/combat_dmg_digits_holo_v1.png",
            "Assets/FracturedChorus/Art/UI/Combat/DamageNumbers/combat_heal_digits_holo_v1.png"
        };

        [MenuItem("Fractured Chorus/Slice Damage Number Digits")]
        public static void SliceAll()
        {
            var count = 0;
            foreach (var path in SheetPaths)
            {
                if (SliceDigitSheet(path))
                {
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DamageNumbers] Sliced {count} digit sheet(s) into 0–9 sprites. isReadable=ON.");
        }

        private static bool SliceDigitSheet(string assetPath)
        {
            if (!File.Exists(assetPath))
            {
                return false;
            }

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return false;
            }

            importer.isReadable = true;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null)
            {
                return false;
            }

            var width = tex.width;
            var height = tex.height;
            const int digitCount = 10;
            var sprites = new List<SpriteMetaData>(digitCount);
            for (var i = 0; i < digitCount; i++)
            {
                var x0 = Mathf.RoundToInt(i * (width / (float)digitCount));
                var x1 = Mathf.RoundToInt((i + 1) * (width / (float)digitCount));
                sprites.Add(new SpriteMetaData
                {
                    name = Path.GetFileNameWithoutExtension(assetPath) + "_" + i,
                    rect = new Rect(x0, 0, x1 - x0, height),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                });
            }

            importer.spritesheet = sprites.ToArray();
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            return true;
        }
    }
}

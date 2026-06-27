#if UNITY_EDITOR
using FracturedChorus.Combat.Damage;
using FracturedChorus.Data;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class ElementBadgeIconSetup
    {
        private const string IconFolder = "Assets/FracturedChorus/Art/UI";

        [MenuItem("Fractured Chorus/Apply Element Badge Icons (Stat Blocks)")]
        public static void ApplyToStatBlocks()
        {
            EnsureSpriteImportSettings();

            AssignIcon("StatBlock_Tank", HarmonyElement.Rhythm, "icon_he_nhip.png");
            AssignIcon("StatBlock_Ren", HarmonyElement.Melody, "icon_he_giai_dieu.png");
            AssignIcon("StatBlock_Mage", HarmonyElement.Harmony, "icon_he_hoa_am.png");

            AssetDatabase.SaveAssets();
            Debug.Log("[Fractured Chorus] Element badge icons assigned to Tank/Ren/Mage stat blocks.");
        }

        private static void EnsureSpriteImportSettings()
        {
            ImportAsSprite($"{IconFolder}/icon_he_nhip.png");
            ImportAsSprite($"{IconFolder}/icon_he_giai_dieu.png");
            ImportAsSprite($"{IconFolder}/icon_he_hoa_am.png");
        }

        private static void ImportAsSprite(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[Fractured Chorus] Missing icon texture: {assetPath}");
                return;
            }

            var changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void AssignIcon(string blockFileName, HarmonyElement element, string iconFileName)
        {
            var blockPath = $"Assets/FracturedChorus/Resources/StatBlocks/{blockFileName}.asset";
            var block = AssetDatabase.LoadAssetAtPath<UnitStatBlockSO>(blockPath);
            if (block == null)
            {
                Debug.LogWarning($"[Fractured Chorus] Stat block not found: {blockPath}");
                return;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{IconFolder}/{iconFileName}");
            if (sprite == null)
            {
                Debug.LogWarning($"[Fractured Chorus] Sprite not found: {IconFolder}/{iconFileName}");
                return;
            }

            block.element = element;
            block.elementBadgeIcon = sprite;
            EditorUtility.SetDirty(block);
        }
    }
}
#endif

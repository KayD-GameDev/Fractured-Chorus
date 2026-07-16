#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    /// <summary>
    /// Ensures Deploy/Execute button sprites stay Readable + uncompressed so
    /// Image.alphaHitTestMinimumThreshold works without Console errors.
    /// </summary>
    public static class CombatButtonSpriteImportSettings
    {
        private static readonly string[] ButtonSpritePaths =
        {
            "Assets/FracturedChorus/Resources/UI/Combat/combat_btn_deploy_v1.png",
            "Assets/FracturedChorus/Resources/UI/Combat/combat_btn_execute_v1.png"
        };

        [InitializeOnLoadMethod]
        private static void EnsureOnLoad()
        {
            EditorApplication.delayCall += EnsureImportSettings;
        }

        [MenuItem("Fractured Chorus/Ensure Combat Button Sprites Readable")]
        public static void EnsureImportSettingsMenu()
        {
            EnsureImportSettings();
            Debug.Log("[CombatButtonSprites] Import settings applied (Read/Write + Uncompressed).");
        }

        private static void EnsureImportSettings()
        {
            var changed = false;
            foreach (var path in ButtonSpritePaths)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                var dirty = false;
                if (!importer.isReadable)
                {
                    importer.isReadable = true;
                    dirty = true;
                }

                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    dirty = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }

                if (!dirty)
                {
                    continue;
                }

                importer.SaveAndReimport();
                changed = true;
            }

            if (changed)
            {
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif

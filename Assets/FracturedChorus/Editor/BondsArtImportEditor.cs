#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class BondsArtImportEditor
    {
        private static readonly (string path, int l, int b, int r, int t)[] Sliced =
        {
            ("Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_panel_glass_v1.png", 48, 48, 48, 48),
            ("Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_header_plate_v1.png", 80, 40, 80, 40),
            ("Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_nav_selected_v1.png", 40, 24, 40, 24),
            ("Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_chip_frame_normal_v1.png", 24, 24, 24, 24),
            ("Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_chip_frame_selected_v1.png", 24, 24, 24, 24),
            ("Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_chip_locked_v1.png", 24, 24, 24, 24),
            ("Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_episode_row_v1.png", 32, 20, 32, 20),
            ("Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_bar_track_v1.png", 8, 6, 8, 6),
            ("Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_bar_fill_v1.png", 8, 6, 8, 6)
        };

        private static readonly string[] Simple =
        {
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_people_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_social_stats_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_link_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_conversations_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_memories_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_gallery_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_stat_resonance_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_stat_cadence_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_stat_pulse_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_stat_harmony_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_stat_rhythm_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_lock_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_play_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_chevron_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_compass_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Decor/ui_bonds_promo_piano_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Decor/ui_bonds_silhouette_locked_v1.png"
        };

        [MenuItem("Fractured Chorus/Bonds/Configure Art Importers")]
        public static void ConfigureAll()
        {
            foreach (var entry in Sliced)
            {
                Configure(entry.path, entry.l, entry.b, entry.r, entry.t);
            }

            foreach (var path in Simple)
            {
                Configure(path, 0, 0, 0, 0);
            }

            Configure("Assets/FracturedChorus/Art/UI/Bonds/_ref/_ref_bonds_menu_v1.jpg", 0, 0, 0, 0, sprite: false);
            AssetDatabase.SaveAssets();
        }

        private static void Configure(string assetPath, int l, int b, int r, int t, bool sprite = true)
        {
            if (!File.Exists(ToFullPath(assetPath)))
            {
                Debug.LogWarning("[Bonds art] missing " + assetPath);
                return;
            }

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            if (sprite)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = new Vector4(l, b, r, t);
            }

            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static string ToFullPath(string assetPath)
        {
            return Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
#endif

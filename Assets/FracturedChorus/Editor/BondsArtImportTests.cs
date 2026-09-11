using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Tests
{
    public class BondsArtImportTests
    {
        private static readonly string[] Paths =
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
            "Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_panel_glass_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_header_plate_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_nav_selected_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_chip_frame_normal_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_chip_frame_selected_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_chip_locked_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_episode_row_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_bar_track_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_bar_fill_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Decor/ui_bonds_promo_piano_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Decor/ui_bonds_silhouette_locked_v1.png"
        };

        [Test]
        public void P0Sprites_Exist()
        {
            foreach (var path in Paths)
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(path), path);
            }
        }

        [Test]
        public void MockRef_Exists()
        {
            Assert.IsNotNull(
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/FracturedChorus/Art/UI/Bonds/_ref/_ref_bonds_menu_v1.jpg"));
        }

        [Test]
        public void CityBackground_Reused()
        {
            Assert.IsNotNull(
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/FracturedChorus/Art/UI/StatusMenu/statusmenu_hima_city_bg_v1.jpg"));
        }
    }
}

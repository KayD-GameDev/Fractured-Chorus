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
            "Assets/FracturedChorus/Art/UI/Bonds/Decor/ui_bonds_silhouette_locked_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/bonds_episode_promo_placeholder_v1.jpg",
            "Assets/FracturedChorus/Art/UI/Bonds/Cards/bond_card_story_hidden_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/charlotte_ep01_after_the_bell_v1.jpg",
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/charlotte_ep02_the_page_he_keeps_v1.jpg",
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/charlotte_ep03_faded_ink_v1.jpg",
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/charlotte_ep04_grandfathers_mark_v1.jpg",
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/charlotte_ep05_unfinished_rest_v1.jpg",
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/ren_ep01_the_file_he_gave_away_v1.jpg",
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/ren_ep02_demo_seven_v1.jpg",
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/ren_ep03_dead_handle_v1.jpg",
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/ren_ep04_no_name_no_face_v1.jpg",
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/ren_ep05_still_keeping_the_files_v1.jpg"
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

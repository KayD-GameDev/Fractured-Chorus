using FracturedChorus.Meta;

namespace FracturedChorus.Hub
{
    public static class BondPromoCatalog
    {
        public const string DefaultPromoPath =
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/bonds_episode_promo_placeholder_v1.jpg";

        public static readonly string[] CharlotteEpisodePromoPaths =
        {
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/charlotte_ep01_after_the_bell_v1.jpg",
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/charlotte_ep02_the_page_he_keeps_v1.jpg",
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/charlotte_ep03_faded_ink_v1.jpg",
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/charlotte_ep04_grandfathers_mark_v1.jpg",
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/charlotte_ep05_unfinished_rest_v1.jpg"
        };

        public static readonly string[] RenEpisodePromoPaths =
        {
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/ren_ep01_the_file_he_gave_away_v1.jpg",
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/ren_ep02_demo_seven_v1.jpg",
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/ren_ep03_dead_handle_v1.jpg",
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/ren_ep04_no_name_no_face_v1.jpg",
            "Assets/FracturedChorus/Art/UI/Bonds/Promo/ren_ep05_still_keeping_the_files_v1.jpg"
        };

        public static int EpisodePromoIndex(string npcId, int episodeRowIndex) =>
            episodeRowIndex < 0 ? 0 : episodeRowIndex;

        public static bool UsesEpisodePromos(string npcId) =>
            npcId == BondNpcIds.Charlotte || npcId == BondNpcIds.Ren;

        public static string[] PathsFor(string npcId) =>
            npcId == BondNpcIds.Ren
                ? RenEpisodePromoPaths
                : npcId == BondNpcIds.Charlotte
                    ? CharlotteEpisodePromoPaths
                    : System.Array.Empty<string>();
    }
}

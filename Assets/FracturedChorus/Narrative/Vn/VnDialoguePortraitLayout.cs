using UnityEngine;

namespace FracturedChorus.Narrative.Vn
{
    public static class VnDialoguePortraitLayout
    {
        public static readonly Vector2 LeftAnchorMin = new Vector2(0f, 0f);
        public static readonly Vector2 LeftAnchorMax = new Vector2(0f, 0f);
        public static readonly Vector2 LeftPivot = new Vector2(0f, 0f);
        public static readonly Vector2 LeftAnchoredPosition = new Vector2(28f, 141f);

        public static readonly Vector2 RightAnchorMin = new Vector2(1f, 0f);
        public static readonly Vector2 RightAnchorMax = new Vector2(1f, 0f);
        public static readonly Vector2 RightPivot = new Vector2(1f, 0f);
        public static readonly Vector2 RightAnchoredPosition = new Vector2(-28f, 137f);

        public static readonly Vector2 SizeDelta = new Vector2(440f, 600f);
        public static readonly Vector2 DefaultShadowOffset = new Vector2(-16f, 12f);
        public static readonly Color DefaultShadowColor = new Color(0.05f, 0.12f, 0.35f, 0.92f);
        public static readonly Color InactiveTint = new Color(0.55f, 0.55f, 0.62f, 0.85f);

        public static readonly Vector2 AnchorMin = LeftAnchorMin;
        public static readonly Vector2 AnchorMax = LeftAnchorMax;
        public static readonly Vector2 Pivot = LeftPivot;
        public static readonly Vector2 AnchoredPosition = LeftAnchoredPosition;
    }

    public static class VnBgIds
    {
        public const string Black = "bg_black";
        public const string LuxeConcert = "luxe_concert";
        public const string LuminaCrossingDay = "lumina_crossing_day";
        public const string StellaWorksHq = "stellaworks_hq";
        public const string SyncPodAdWall = "syncpod_ad_wall";
        public const string HimaCampusDay = "hima_campus_day";
        public const string HimaRecitalHall = "hima_recital_hall";
        public const string LuminaApartmentTv = "lumina_apartment_tv";
        public const string NewsDrainCases = "news_drain_cases";
        public const string LuminaStreetNight = "lumina_street_night";
        public const string LuminaAlleyNight = "lumina_alley_night";
        public const string LuminaAlleyHarutoBody = "lumina_alley_haruto_body";
        public const string LuminaSquareNight = "lumina_square_night";
        public const string HimaDormMorning = "hima_dorm_morning";
        public const string CgRenMirrorUniform = "cg_ren_mirror_uniform";
        public const string LuminaTrainMorning = "lumina_train_morning";
        public const string HimaHallwayDay = "hima_hallway_day";
        public const string CgHallwayBump = "cg_hallway_bump";
        public const string CgSyncpodsFloor = "cg_syncpods_floor";
        public const string CgIndiePlayerBreath = "cg_indie_player_breath";
        public const string HimaClassroomDay = "hima_classroom_day";
        public const string HimaCeremonyHall = "hima_ceremony_hall";
        public const string HimaCeremonyDesync = "hima_ceremony_desync";
        public const string CadenceFracturePull = "cadence_fracture_pull";
        public const string FlowerShop = "flower_shop";
        public const string FlowerArrive = "flower_arrive";
        public const string FlowerCustomer = "flower_customer";
        public const string FlowerThink = "flower_think";
        public const string FlowerHappy = "flower_happy";
    }
}

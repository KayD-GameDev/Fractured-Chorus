namespace FracturedChorus.RunMap.Core
{
    public sealed class MapGenerationProfile
    {
        public int ColumnCount = MapLayoutConstants.ColumnCount;
        public int FloorCount = MapLayoutConstants.FloorCount;
        public int BossFloor = MapLayoutConstants.BossFloor;
        public int PathCount = MapLayoutConstants.DefaultPathCount;
        public PinkySectorId Sector = PinkySectorId.Pulse;

        public static MapGenerationProfile Default => new MapGenerationProfile();

        public static MapGenerationProfile ForSector(PinkySectorId sector) => sector switch
        {
            PinkySectorId.Pulse => new MapGenerationProfile
            {
                FloorCount = 10,
                BossFloor = 11,
                Sector = PinkySectorId.Pulse
            },
            PinkySectorId.Echo => new MapGenerationProfile
            {
                FloorCount = 10,
                BossFloor = 11,
                Sector = PinkySectorId.Echo
            },
            PinkySectorId.Canticle => new MapGenerationProfile
            {
                FloorCount = 12,
                BossFloor = 13,
                Sector = PinkySectorId.Canticle
            },
            _ => Default
        };

        public int TreasureFloor => System.Math.Max(2, (int)(FloorCount * 0.6f));
    }
}

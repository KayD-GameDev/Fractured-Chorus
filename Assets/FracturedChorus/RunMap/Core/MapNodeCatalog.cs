namespace FracturedChorus.RunMap.Core
{
    public static class MapNodeCatalog
    {
        public static string Title(MapNodeType type) => type switch
        {
            MapNodeType.Start => "DEPARTURE",
            MapNodeType.Battle => "BATTLE",
            MapNodeType.Elite => "ELITE",
            MapNodeType.Event => "EVENT",
            MapNodeType.Camp => "CAMP",
            MapNodeType.Relay => "SHOP",
            MapNodeType.Treasure => "TREASURE",
            MapNodeType.Boss => "BOSS",
            _ => type.ToString().ToUpperInvariant()
        };

        public static string Description(MapNodeType type) => type switch
        {
            MapNodeType.Start =>
                "Điểm xuất phát của run. Có thể lưu tiến trình và chọn nhánh F1 kế tiếp.",
            MapNodeType.Battle =>
                "Trận chiến thường. Thắng để mở node kế — không lưu khi vào.",
            MapNodeType.Elite =>
                "Trận elite khó hơn. Phần thưởng cao hơn — không lưu khi vào.",
            MapNodeType.Event =>
                "Sự kiện ngẫu nhiên. Không lưu khi vào.",
            MapNodeType.Camp =>
                "Nghỉ, hồi HP và lưu tiến trình run.",
            MapNodeType.Relay =>
                "Cửa hàng tiện lợi — mua vật phẩm. Không lưu khi vào.",
            MapNodeType.Treasure =>
                "Rương thưởng Notes. Không lưu khi vào.",
            MapNodeType.Boss =>
                "Trận boss cuối sector. Hoàn thành sẽ lưu và mở map kế.",
            _ => string.Empty
        };

        public static bool IsSavePoint(MapNodeType type, bool isBoss, bool bossCleared) =>
            type == MapNodeType.Start
            || type == MapNodeType.Camp
            || (isBoss && bossCleared);
    }
}

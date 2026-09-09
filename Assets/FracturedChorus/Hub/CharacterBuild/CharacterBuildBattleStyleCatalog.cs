using FracturedChorus.Meta;

namespace FracturedChorus.Hub.CharacterBuild
{
    public readonly struct CharacterBuildBattleStyle
    {
        public CharacterBuildBattleStyle(string title, string line1, string line2)
        {
            Title = title;
            Line1 = line1;
            Line2 = line2;
        }

        public string Title { get; }
        public string Line1 { get; }
        public string Line2 { get; }
    }

    public static class CharacterBuildBattleStyleCatalog
    {
        public static CharacterBuildBattleStyle For(string characterId)
        {
            return characterId switch
            {
                PartyCharacterIds.Ren => new CharacterBuildBattleStyle(
                    "Chủ lực sát thương",
                    "DPS · Melody",
                    "Counter nhiều nốt, gây sát thương chính, hồi skill lâu"),
                PartyCharacterIds.Charlotte => new CharacterBuildBattleStyle(
                    "Đỡ đòn",
                    "Tank · Rhythm",
                    "Tạo khiên chặn sát thương địch, đẩy lùi các nốt nhạc của địch"),
                PartyCharacterIds.Coda => new CharacterBuildBattleStyle(
                    "Hỗ trợ",
                    "Support · Harmony",
                    "Hồi máu và giảm thời gian hồi chiêu của đồng đội"),
                _ => new CharacterBuildBattleStyle(string.Empty, string.Empty, string.Empty)
            };
        }
    }
}

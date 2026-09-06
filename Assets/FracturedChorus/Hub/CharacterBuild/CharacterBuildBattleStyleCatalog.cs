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
                    "DPS · Melody · sát thương vật lý.",
                    string.Empty),
                PartyCharacterIds.Charlotte => new CharacterBuildBattleStyle(
                    "Đỡ đòn",
                    "Tank · Rhythm · HP cao, nhận đòn thay đội.",
                    "Counter nhiều nốt, thời gian hồi lâu."),
                PartyCharacterIds.Coda => new CharacterBuildBattleStyle(
                    "Hỗ trợ",
                    "Support · Harmony · sát thương phép.",
                    "Mend hồi máu. Encore giảm S2."),
                _ => new CharacterBuildBattleStyle(string.Empty, string.Empty, string.Empty)
            };
        }
    }
}

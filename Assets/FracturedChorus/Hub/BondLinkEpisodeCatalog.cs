using FracturedChorus.Meta;

namespace FracturedChorus.Hub
{
    public readonly struct BondLinkEpisode
    {
        public BondLinkEpisode(int index, string title, int requiredRank)
        {
            Index = index;
            Title = title;
            RequiredRank = requiredRank;
        }

        public int Index { get; }
        public string Title { get; }
        public int RequiredRank { get; }
    }

    public static class BondLinkEpisodeCatalog
    {
        private static readonly BondLinkEpisode[] DefaultEpisodes =
        {
            new BondLinkEpisode(1, "A Usual Day", 1),
            new BondLinkEpisode(2, "After Class", 2),
            new BondLinkEpisode(3, "A Different Melody", 3),
            new BondLinkEpisode(4, "Unspoken Words", 4),
            new BondLinkEpisode(5, "Toward Tomorrow", 5)
        };

        private static readonly BondLinkEpisode[] CharlotteEpisodes =
        {
            new BondLinkEpisode(1, "After the Bell", 1),
            new BondLinkEpisode(2, "The Page He Keeps", 2),
            new BondLinkEpisode(3, "Faded Ink", 3),
            new BondLinkEpisode(4, "Grandfather's Mark", 4),
            new BondLinkEpisode(5, "An Unfinished Rest", 5)
        };

        private static readonly BondLinkEpisode[] RenEpisodes =
        {
            new BondLinkEpisode(1, "The File He Gave Away", 1),
            new BondLinkEpisode(2, "Demo Seven", 2),
            new BondLinkEpisode(3, "Dead Handle", 3),
            new BondLinkEpisode(4, "No Name, No Face", 4),
            new BondLinkEpisode(5, "Still Keeping the Files", 5)
        };

        public static BondLinkEpisode[] Episodes => DefaultEpisodes;

        public static BondLinkEpisode[] GetEpisodes(string npcId) =>
            npcId == BondNpcIds.Charlotte
                ? CharlotteEpisodes
                : npcId == BondNpcIds.Ren
                    ? RenEpisodes
                    : DefaultEpisodes;

        public static bool IsUnlocked(int bondRank, int requiredRank) =>
            bondRank >= requiredRank && requiredRank >= 1;
    }
}

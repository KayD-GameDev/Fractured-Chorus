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
        public static readonly BondLinkEpisode[] Episodes =
        {
            new BondLinkEpisode(1, "A Usual Day", 1),
            new BondLinkEpisode(2, "After Class", 2),
            new BondLinkEpisode(3, "A Different Melody", 3),
            new BondLinkEpisode(4, "Unspoken Words", 4),
            new BondLinkEpisode(5, "Toward Tomorrow", 5)
        };

        public static bool IsUnlocked(int bondRank, int requiredRank) =>
            bondRank >= requiredRank && requiredRank >= 1;
    }
}

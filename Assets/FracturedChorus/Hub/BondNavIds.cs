namespace FracturedChorus.Hub
{
    public static class BondNavIds
    {
        public const int SocialStats = 0;
        public const int Link = 1;
        public const int Conversations = 2;
        public const int Memories = 3;
        public const int Gallery = 4;

        public static bool IsImplemented(int navIndex) => navIndex <= Link;
    }
}

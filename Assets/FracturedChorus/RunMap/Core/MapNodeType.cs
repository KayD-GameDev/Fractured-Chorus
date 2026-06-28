namespace FracturedChorus.RunMap.Core
{
    /// <summary>Loại node trên run map — map từ StS (Monster→Battle, Merchant→Relay, Rest→Camp).</summary>
    public enum MapNodeType
    {
        Battle = 0,
        Event = 1,
        Elite = 2,
        Camp = 3,
        Relay = 4,
        Treasure = 5,
        Boss = 6
    }
}

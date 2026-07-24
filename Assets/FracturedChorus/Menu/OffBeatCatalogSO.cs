using UnityEngine;

namespace FracturedChorus.Menu
{
    [CreateAssetMenu(fileName = "OffBeatCatalog", menuName = "Fractured Chorus/Off-Beat Catalog")]
    public sealed class OffBeatCatalogSO : ScriptableObject
    {
        public OffBeatTrackSO[] tracks = System.Array.Empty<OffBeatTrackSO>();
    }
}

using UnityEngine;

namespace FracturedChorus.Data
{
    [CreateAssetMenu(fileName = "MapTemplate", menuName = "Fractured Chorus/Map Template")]
    public class MapTemplateSO : ScriptableObject
    {
        [Header("Grid (StS reference)")]
        public int columnCount = 7;
        public int floorCount = 15;
        public int bossFloor = 16;
        public int pathCount = 6;

        [Header("Generation")]
        public bool useReferenceDemoOnPlay = false;
        public bool randomizeSeedOnPlay = true;
        public int defaultSeed = 42;

        [Header("Node type weights (random roll)")]
        [Range(0f, 1f)] public float battleWeight = 0.26f;
        [Range(0f, 1f)] public float eliteWeight = 0.32f;
        [Range(0f, 1f)] public float eventWeight = 0.17f;
        [Range(0f, 1f)] public float relayWeight = 0.05f;
        [Range(0f, 1f)] public float campWeight = 0.06f;
        [Range(0f, 1f)] public float treasureWeight = 0.14f;
    }
}

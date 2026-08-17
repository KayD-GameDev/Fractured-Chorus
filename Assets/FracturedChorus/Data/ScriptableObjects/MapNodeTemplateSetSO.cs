using System;
using FracturedChorus.RunMap.Core;
using FracturedChorus.RunMap.UI;
using UnityEngine;

namespace FracturedChorus.Data
{
    [CreateAssetMenu(fileName = "MapNodeTemplateSet", menuName = "Fractured Chorus/Map Node Template Set")]
    public sealed class MapNodeTemplateSetSO : ScriptableObject
    {
        public const string DefaultAssetPath =
            "Assets/FracturedChorus/Data/ScriptableObjects/Presets/MapNodeTemplateSet_Default.asset";

        [SerializeField] private MapNodeIconSetSO iconSet;
        [SerializeField] private MapNodeView defaultNodePrefab;
        [SerializeField] private MapConnectionLineView connectionPrefab;
        [SerializeField] private MapNodeTypePrefab[] typePrefabs = Array.Empty<MapNodeTypePrefab>();

        public MapNodeIconSetSO IconSet => iconSet;
        public MapNodeView DefaultNodePrefab => defaultNodePrefab;
        public MapConnectionLineView ConnectionPrefab => connectionPrefab;

        public MapNodeView ResolveNodePrefab(MapNodeType type)
        {
            if (typePrefabs != null)
            {
                for (var i = 0; i < typePrefabs.Length; i++)
                {
                    var entry = typePrefabs[i];
                    if (entry.Type == type && entry.Prefab != null)
                    {
                        return entry.Prefab;
                    }
                }
            }

            return defaultNodePrefab;
        }

#if UNITY_EDITOR
        public void EditorAssign(
            MapNodeIconSetSO icons,
            MapNodeView nodePrefab,
            MapConnectionLineView linePrefab,
            MapNodeTypePrefab[] entries)
        {
            iconSet = icons;
            defaultNodePrefab = nodePrefab;
            connectionPrefab = linePrefab;
            typePrefabs = entries ?? Array.Empty<MapNodeTypePrefab>();
        }
#endif
    }

    [Serializable]
    public struct MapNodeTypePrefab
    {
        [SerializeField] private MapNodeType type;
        [SerializeField] private MapNodeView prefab;

        public MapNodeType Type => type;
        public MapNodeView Prefab => prefab;

        public MapNodeTypePrefab(MapNodeType type, MapNodeView prefab)
        {
            this.type = type;
            this.prefab = prefab;
        }
    }
}

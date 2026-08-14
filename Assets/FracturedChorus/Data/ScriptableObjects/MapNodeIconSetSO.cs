using FracturedChorus.RunMap.Core;
using UnityEngine;

namespace FracturedChorus.Data
{
    [CreateAssetMenu(fileName = "MapNodeIconSet", menuName = "Fractured Chorus/Map Node Icon Set")]
    public sealed class MapNodeIconSetSO : ScriptableObject
    {
        public const string StartIconAssetPath =
            "Assets/FracturedChorus/Art/UI/RunMap/Nodes/runmap_node_start_v1.png";

        [SerializeField] private Sprite battle;
        [SerializeField] private Sprite elite;
        [SerializeField] private Sprite treasure;
        [SerializeField] private Sprite eventNode;
        [SerializeField] private Sprite camp;
        [SerializeField] private Sprite relay;
        [SerializeField] private Sprite start;
        [SerializeField] private Sprite bossFloorI;
        [SerializeField] private Sprite bossFloorII;
        [SerializeField] private Sprite bossFinal;

        private static Sprite s_startFallback;

        public Sprite Resolve(MapNodeType type, bool isBoss, PinkySectorId sector)
        {
            if (type == MapNodeType.Start)
            {
                return ResolveStartIcon();
            }

            if (type == MapNodeType.Boss || isBoss)
            {
                return ResolveBoss(sector);
            }

            return type switch
            {
                MapNodeType.Battle => battle,
                MapNodeType.Elite => elite,
                MapNodeType.Treasure => treasure,
                MapNodeType.Event => eventNode,
                MapNodeType.Camp => camp,
                MapNodeType.Relay => relay,
                _ => null
            };
        }

        public Sprite ResolveBoss(PinkySectorId sector) => sector switch
        {
            PinkySectorId.Pulse => bossFloorI != null ? bossFloorI : bossFinal,
            PinkySectorId.Echo => bossFloorII != null ? bossFloorII : bossFinal,
            PinkySectorId.Canticle => bossFinal != null ? bossFinal : bossFloorII,
            _ => bossFinal
        };

        private Sprite ResolveStartIcon()
        {
            if (start != null)
            {
                return start;
            }

            if (s_startFallback != null)
            {
                return s_startFallback;
            }

#if UNITY_EDITOR
            s_startFallback = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(StartIconAssetPath);
            if (s_startFallback == null)
            {
                var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(StartIconAssetPath);
                foreach (var asset in assets)
                {
                    if (asset is Sprite sprite)
                    {
                        s_startFallback = sprite;
                        start = sprite;
                        break;
                    }
                }
            }
#endif
            return s_startFallback;
        }

#if UNITY_EDITOR
        public void EditorAssign(
            Sprite battleSprite,
            Sprite eliteSprite,
            Sprite treasureSprite,
            Sprite eventSprite,
            Sprite bossFloorISprite,
            Sprite bossFloorIISprite,
            Sprite bossFinalSprite,
            Sprite campSprite = null,
            Sprite relaySprite = null,
            Sprite startSprite = null)
        {
            battle = battleSprite;
            elite = eliteSprite;
            treasure = treasureSprite;
            eventNode = eventSprite;
            bossFloorI = bossFloorISprite;
            bossFloorII = bossFloorIISprite;
            bossFinal = bossFinalSprite;
            camp = campSprite;
            relay = relaySprite;
            start = startSprite;
        }
#endif
    }
}

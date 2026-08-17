using System.Collections.Generic;
using UnityEngine;

namespace FracturedChorus.Data
{
    [CreateAssetMenu(fileName = "TreasureRewardTable", menuName = "Fractured Chorus/Treasure Reward Table")]
    public sealed class TreasureRewardTableSO : ScriptableObject
    {
        public const string ResourcesPath = "Treasure/TreasureRewardTable_Default";
        public const string AssetPath = "Assets/FracturedChorus/Resources/Treasure/TreasureRewardTable_Default.asset";

        [SerializeField] private TreasureRewardSO[] rewards;
        [SerializeField, Min(1)] private int offerCount = 3;

        public IReadOnlyList<TreasureRewardSO> Rewards => rewards;
        public int OfferCount => offerCount;

        public TreasureRewardSO[] PickOffers(int seed)
        {
            return PickOffers(rewards, seed, offerCount);
        }

        public static TreasureRewardSO[] PickOffers(IReadOnlyList<TreasureRewardSO> pool, int seed, int count)
        {
            return SeededOfferPicker.Pick(pool, seed, count);
        }

        public static TreasureRewardTableSO LoadOrCreateDefault()
        {
            var loaded = Resources.Load<TreasureRewardTableSO>(ResourcesPath);
            if (loaded != null && loaded.rewards != null && loaded.rewards.Length > 0)
            {
                return loaded;
            }

            return CreateRuntimeDefault();
        }

        public static TreasureRewardTableSO CreateRuntimeDefault()
        {
            var table = CreateInstance<TreasureRewardTableSO>();
            table.EditorAssign(TreasureRewardSO.CreateDefaultCatalog(), 3);
            return table;
        }

        public void EditorAssign(TreasureRewardSO[] pool, int count)
        {
            rewards = pool;
            offerCount = Mathf.Max(1, count);
        }
    }
}

using FracturedChorus.Combat.Bootstrap;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Tests
{
    public class CombatPoolPrefabLoadTests
    {
        [Test]
        public void LoadPrefab_ResolvesAllPoolKeys()
        {
            var keys = new[]
            {
                CombatEnemyKeys.Enemy1,
                CombatEnemyKeys.Enemy2,
                CombatEnemyKeys.Enemy3,
                CombatEnemyKeys.Elite1,
                CombatEnemyKeys.Elite2,
                CombatEnemyKeys.Elite3
            };

            foreach (var key in keys)
            {
                var prefab = CombatPoolUnitVisuals.LoadPrefab(key);
                Assert.IsNotNull(prefab, $"Missing Enemy Pool prefab for {key}");
                Assert.IsNotNull(prefab.GetComponent<SpriteRenderer>(), key);
                Assert.IsNotNull(prefab.GetComponent<Animator>(), key);
            }
        }

        [Test]
        public void CatalogAsset_ExistsInResources()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CombatPoolPrefabCatalogSO>(
                "Assets/FracturedChorus/Resources/CombatPoolPrefabs/CombatPoolPrefabCatalog.asset");
            Assert.IsNotNull(catalog);
            Assert.IsNotNull(catalog.GetPrefab(CombatEnemyKeys.Enemy1));
        }

        [Test]
        public void PrefabFolder_ContainsAuthoredAssets()
        {
            Assert.IsNotNull(
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/FracturedChorus/Prefabs/Enemy Pool/Enemy 1 - Pink_Shoes.prefab"));
        }
    }
}

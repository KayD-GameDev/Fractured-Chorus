using UnityEngine;

namespace FracturedChorus.Combat.Bootstrap
{
    [CreateAssetMenu(
        fileName = "CombatPoolPrefabCatalog",
        menuName = "Fractured Chorus/Combat Pool Prefab Catalog")]
    public sealed class CombatPoolPrefabCatalogSO : ScriptableObject
    {
        [SerializeField] private GameObject enemy1;
        [SerializeField] private GameObject enemy2;
        [SerializeField] private GameObject enemy3;
        [SerializeField] private GameObject elite1;
        [SerializeField] private GameObject elite2;
        [SerializeField] private GameObject elite3;

        public GameObject GetPrefab(string unitKey)
        {
            switch (unitKey)
            {
                case CombatEnemyKeys.Enemy1:
                    return enemy1;
                case CombatEnemyKeys.Enemy2:
                    return enemy2;
                case CombatEnemyKeys.Enemy3:
                    return enemy3;
                case CombatEnemyKeys.Elite1:
                    return elite1;
                case CombatEnemyKeys.Elite2:
                    return elite2;
                case CombatEnemyKeys.Elite3:
                    return elite3;
                default:
                    return null;
            }
        }
    }
}

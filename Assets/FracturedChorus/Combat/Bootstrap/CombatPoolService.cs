using System;
using UnityEngine;

namespace FracturedChorus.Combat.Bootstrap
{
    public static class CombatPoolService
    {
        public const int EnemyPoolSize = 3;
        public const int ElitePoolSize = 3;
        public const int BackgroundPoolSize = 4;

        public static readonly string[] EnemyKeys =
        {
            CombatEnemyKeys.Enemy1,
            CombatEnemyKeys.Enemy2,
            CombatEnemyKeys.Enemy3
        };

        public static readonly string[] EliteKeys =
        {
            CombatEnemyKeys.Elite1,
            CombatEnemyKeys.Elite2,
            CombatEnemyKeys.Elite3
        };

        public static CombatPoolRoll RollBattle(int runSeed, int nodeId)
        {
            var rng = CreateRng(runSeed, nodeId, 11);
            return new CombatPoolRoll
            {
                IsEliteEncounter = false,
                EnemyKeys = new[]
                {
                    PickEnemy(rng),
                    PickEnemy(rng),
                    PickEnemy(rng)
                },
                GridSlots = CombatPoolPlacement.RollBattleSlots(runSeed, nodeId),
                BackgroundIndex = rng.Next(BackgroundPoolSize)
            };
        }

        public static CombatPoolRoll RollElite(int runSeed, int nodeId)
        {
            var rng = CreateRng(runSeed, nodeId, 23);
            return new CombatPoolRoll
            {
                IsEliteEncounter = true,
                EnemyKeys = new[]
                {
                    PickElite(rng),
                    PickEnemy(rng),
                    PickEnemy(rng)
                },
                GridSlots = CombatPoolPlacement.RollEliteSlots(runSeed, nodeId),
                BackgroundIndex = rng.Next(BackgroundPoolSize)
            };
        }

        public static CombatPoolRoll RollForEncounter(string encounterId, int runSeed, int nodeId) =>
            encounterId == EncounterCatalog.EliteGrunts
                ? RollElite(runSeed, nodeId)
                : RollBattle(runSeed, nodeId);

        private static System.Random CreateRng(int runSeed, int nodeId, int salt)
        {
            unchecked
            {
                var mixed = runSeed ^ (nodeId * 73856093) ^ (salt * 19349663);
                return new System.Random(mixed);
            }
        }

        private static string PickEnemy(System.Random rng) => EnemyKeys[rng.Next(EnemyPoolSize)];

        private static string PickElite(System.Random rng) => EliteKeys[rng.Next(ElitePoolSize)];
    }

    public static class CombatEnemyKeys
    {
        public const string Enemy1 = "enemy_1";
        public const string Enemy2 = "enemy_2";
        public const string Enemy3 = "enemy_3";
        public const string Elite1 = "elite_1";
        public const string Elite2 = "elite_2";
        public const string Elite3 = "elite_3";
    }
}

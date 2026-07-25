using FracturedChorus.Data;
using FracturedChorus.RunMap.Core;
using UnityEngine;

namespace FracturedChorus.Combat.Bootstrap
{
    public static class EncounterCatalog
    {
        public const string BattleGrunts = "Encounter_Battle_Grunts";
        public const string EliteGrunts = "Encounter_Elite_Grunts";
        public const string BossDespair = "Encounter_Boss_Despair";

        public static string ForNodeType(MapNodeType type) => type switch
        {
            MapNodeType.Battle => BattleGrunts,
            MapNodeType.Elite => EliteGrunts,
            MapNodeType.Boss => BossDespair,
            _ => null
        };

        public static EncounterDefinitionSO LoadOrCreate(string encounterId)
        {
            if (string.IsNullOrWhiteSpace(encounterId))
            {
                return null;
            }

            var fromResources = Resources.Load<EncounterDefinitionSO>($"Encounters/{encounterId}");
            if (fromResources != null)
            {
                return fromResources;
            }

            return EncounterRuntimeFactory.CreateById(encounterId);
        }
    }
}

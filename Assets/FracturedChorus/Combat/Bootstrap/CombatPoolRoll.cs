using System;

namespace FracturedChorus.Combat.Bootstrap
{
    [Serializable]
    public sealed class CombatPoolRoll
    {
        public string[] EnemyKeys = Array.Empty<string>();
        public int[] GridSlots = Array.Empty<int>();
        public int BackgroundIndex;

        public bool IsEliteEncounter { get; set; }

        public static bool IsPooledEncounterId(string encounterId) =>
            encounterId == EncounterCatalog.BattleGrunts
            || encounterId == EncounterCatalog.EliteGrunts;
    }
}

using FracturedChorus.Combat.Grid;
using UnityEngine;

namespace FracturedChorus.Data
{
    [System.Serializable]
    public struct EncounterUnitSpawn
    {
        public UnitPresetSO preset;
        public GridSide side;
        public int row;
        public int column;
    }

    [CreateAssetMenu(fileName = "Encounter", menuName = "Fractured Chorus/Encounter Definition")]
    public class EncounterDefinitionSO : ScriptableObject
    {
        public string encounterId;
        public EncounterUnitSpawn[] units;
    }
}

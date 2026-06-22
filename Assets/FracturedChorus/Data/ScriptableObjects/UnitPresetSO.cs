using FracturedChorus.Combat.Units;
using UnityEngine;

namespace FracturedChorus.Data
{
    [CreateAssetMenu(fileName = "UnitPreset", menuName = "Fractured Chorus/Unit Preset")]
    public class UnitPresetSO : ScriptableObject
    {
        public string unitId;
        public string displayName;
        public UnitRole role = UnitRole.Dps;
        public UnitStats stats = new UnitStats();
        public SkillDefinitionSO[] skills;
        public Color placeholderColor = Color.white;
    }
}

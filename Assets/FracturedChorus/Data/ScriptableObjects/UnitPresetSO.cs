using FracturedChorus.Combat.Damage;
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
        [Tooltip("Chỉ số gốc — chỉnh tay, dùng chung cho nhiều preset nếu cần.")]
        public UnitStatBlockSO statBlock;
        [Tooltip("Legacy inline stats — dùng khi statBlock chưa gán.")]
        public UnitStats stats = new UnitStats();
        public SkillDefinitionSO[] skills;
        public Color placeholderColor = Color.white;
        [Tooltip("Sprite combat m?c d?nh � d�ng khi scene chua g�n ho?c b? placeholder 1�1.")]
        public Sprite battleSprite;

        public UnitStats ResolveStats()
        {
            return statBlock != null ? statBlock.ToRuntimeStats() : stats?.Clone() ?? new UnitStats();
        }
    }
}

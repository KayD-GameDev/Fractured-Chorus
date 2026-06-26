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
        [Tooltip("Sprite combat mặc định — dùng khi scene chưa gán hoặc bị placeholder 1×1.")]
        public Sprite battleSprite;
        [Tooltip("Portrait thẻ party UI — nếu trống dùng battleSprite.")]
        public Sprite portraitSprite;

        public Sprite ResolvePortraitSprite()
        {
            return portraitSprite != null ? portraitSprite : battleSprite;
        }

        public UnitStats ResolveStats()
        {
            return statBlock != null ? statBlock.ToRuntimeStats() : stats?.Clone() ?? new UnitStats();
        }
    }
}

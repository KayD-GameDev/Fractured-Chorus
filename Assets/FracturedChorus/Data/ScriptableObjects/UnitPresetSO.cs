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
        [Tooltip("Base stats — edit manually; can be shared across multiple presets.")]
        public UnitStatBlockSO statBlock;
        [Tooltip("Legacy inline stats — used when statBlock is not assigned.")]
        public UnitStats stats = new UnitStats();
        public SkillDefinitionSO[] skills;
        public Color placeholderColor = Color.white;
        [Tooltip("Default combat sprite — used when the scene has none assigned or a 1×1 placeholder.")]
        public Sprite battleSprite;
        [Tooltip("Party card portrait — if empty, uses battleSprite.")]
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

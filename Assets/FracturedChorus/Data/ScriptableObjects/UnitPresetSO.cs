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
        [Tooltip("Timeline beat lane line + label tint. Alpha 0 = fall back to PlaceholderColor.")]
        public Color timelineLaneColor = new Color(0f, 0f, 0f, 0f);
        [Tooltip("Default combat sprite — used when the scene has none assigned or a 1×1 placeholder.")]
        public Sprite battleSprite;
        [Tooltip("Circular chibi bust for timeline LaneAvatar gutter. Falls back to timeline lane color when null.")]
        public Sprite timelineAvatarSprite;
        [Tooltip("Full tilted combat card art (name + HP/Prep slots). Party/enemy card UI displays this on CardArt.")]
        public Sprite combatCardSprite;
        [Tooltip("Khi load: chỉ ghi đè BarStack.anchoredPosition.y (Hierarchy). < 0 = giữ Y CardTemplate.")]
        public float barStackAnchoredY = -1f;
        [Tooltip("Enemy: Inspector Top của HealthSlot (giữ chiều cao slot). < 0 = giữ CardTemplate.")]
        public float healthSlotTop = -1f;
        [Tooltip("Impact telegraphs planned per timeline phase for this unit.")]
        [Min(1)] public int telegraphAttacksPerPhase = 1;

        public bool HasCombatCardArt => combatCardSprite != null;

        public Sprite ResolveCombatCardSprite()
        {
            return combatCardSprite;
        }

        /// <summary>Lane line / label color on Beat Timeline. Falls back to placeholderColor when unset.</summary>
        public Color ResolveTimelineLaneColor()
        {
            return timelineLaneColor.a > 0.001f ? timelineLaneColor : placeholderColor;
        }

        public UnitStats ResolveStats()
        {
            return statBlock != null ? statBlock.ToRuntimeStats() : stats?.Clone() ?? new UnitStats();
        }
    }
}

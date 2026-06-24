using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.Combat.Bootstrap
{
    public static class EncounterRuntimeFactory
    {
        public static EncounterDefinitionSO CreateDemoEncounter()
        {
            var encounter = ScriptableObject.CreateInstance<EncounterDefinitionSO>();
            encounter.encounterId = "demo_encounter_01";
            encounter.units = new[]
            {
                CreateSpawn(GetPresetByKey("tank"), GridSide.Player, 2, 1),
                CreateSpawn(GetPresetByKey("ren"), GridSide.Player, 2, 2),
                CreateSpawn(GetPresetByKey("mage"), GridSide.Player, 2, 3),
                CreateSpawn(GetPresetByKey("grunt_left"), GridSide.Enemy, 2, 1),
                CreateSpawn(GetPresetByKey("grunt_right"), GridSide.Enemy, 2, 3)
            };
            return encounter;
        }

        public static UnitPresetSO GetPresetByKey(string key)
        {
            return key switch
            {
                "ren" => CreateRenPreset(),
                "tank" => CreateTankPreset(),
                "mage" => CreateMagePreset(),
                "grunt_left" => CreateGruntPreset("grunt_left"),
                "grunt_right" => CreateGruntPreset("grunt_right"),
                _ => CreateGruntPreset(key ?? "grunt")
            };
        }

        private static EncounterUnitSpawn CreateSpawn(UnitPresetSO preset, GridSide side, int displayRow, int displayCol)
        {
            var pos = HoneycombIndex.FromDisplay(side, displayRow, displayCol);
            return new EncounterUnitSpawn
            {
                preset = preset,
                side = side,
                row = pos.Row,
                column = pos.Column
            };
        }

        private static UnitPresetSO CreateRenPreset()
        {
            var preset = ScriptableObject.CreateInstance<UnitPresetSO>();
            preset.unitId = "ren";
            preset.displayName = "Ren";
            preset.role = UnitRole.Dps;
            preset.stats = UnitStats.CreateRenPreset();
            preset.placeholderColor = new Color(0.9f, 0.35f, 0.45f);
            preset.skills = CreateStandardKit("ren", "Strike", "Riposte", "Finale", "Guard");
            return preset;
        }

        private static UnitPresetSO CreateTankPreset()
        {
            var preset = ScriptableObject.CreateInstance<UnitPresetSO>();
            preset.unitId = "tank";
            preset.displayName = "Tank";
            preset.role = UnitRole.Tank;
            preset.stats = UnitStats.CreateTankPreset();
            preset.placeholderColor = new Color(0.35f, 0.55f, 0.95f);
            preset.skills = CreateStandardKit("tank", "Ram", "Bulwark", "Hold", "Parry");
            return preset;
        }

        private static UnitPresetSO CreateMagePreset()
        {
            var preset = ScriptableObject.CreateInstance<UnitPresetSO>();
            preset.unitId = "mage";
            preset.displayName = "Mage";
            preset.role = UnitRole.Mage;
            preset.stats = UnitStats.CreateMagePreset();
            preset.placeholderColor = new Color(0.65f, 0.35f, 0.95f);
            preset.skills = CreateStandardKit("mage", "Pulse", "Arc", "Cataclysm", "Ward");
            return preset;
        }

        private static UnitPresetSO CreateGruntPreset(string id)
        {
            var preset = ScriptableObject.CreateInstance<UnitPresetSO>();
            preset.unitId = id;
            preset.displayName = "Grunt";
            preset.role = UnitRole.Grunt;
            preset.stats = UnitStats.CreateGruntPreset();
            preset.placeholderColor = new Color(0.85f, 0.25f, 0.2f);
            preset.skills = new[]
            {
                CreateGruntStrike($"{id}_strike", "Strike")
            };
            return preset;
        }

        private static SkillDefinitionSO CreateGruntStrike(string id, string name)
        {
            var skill = CreateSkill(id, name, SkillSlotKind.BasicAttack, 1, ActionGlowType.Attack);
            skill.baseDamage = 20;
            return skill;
        }

        private static SkillDefinitionSO[] CreateStandardKit(string prefix, string basic, string skill, string ult, string guard)
        {
            return new[]
            {
                CreateSkill($"{prefix}_basic", basic, SkillSlotKind.BasicAttack, 1, ActionGlowType.Attack),
                CreateSkill($"{prefix}_skill", skill, SkillSlotKind.Skill, 2, ActionGlowType.Attack),
                CreateSkill($"{prefix}_ult", ult, SkillSlotKind.Ultimate, 3, ActionGlowType.Rush),
                CreateSkill($"{prefix}_guard", guard, SkillSlotKind.Guard, 1, ActionGlowType.Guard)
            };
        }

        private static SkillDefinitionSO CreateSkill(string id, string name, SkillSlotKind kind, int tier, ActionGlowType glow)
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinitionSO>();
            skill.skillId = id;
            skill.displayName = name;
            skill.slotKind = kind;
            skill.avCost = SkillAvCosts.GetCost(kind);
            skill.delay = kind switch
            {
                SkillSlotKind.BasicAttack => 2,
                SkillSlotKind.Guard => 2,
                SkillSlotKind.Ultimate => 5,
                _ => 3
            };
            skill.skillTier = tier;
            skill.glowType = glow;
            skill.targetType = glow == ActionGlowType.Guard ? SkillTargetType.Self : SkillTargetType.SingleEnemy;
            skill.baseDamage = kind == SkillSlotKind.Guard ? 0 : 10 + tier * 5;
            return skill;
        }
    }
}

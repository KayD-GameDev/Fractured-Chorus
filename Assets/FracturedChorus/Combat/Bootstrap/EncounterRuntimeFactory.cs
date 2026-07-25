using FracturedChorus.Combat.Damage;
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
            encounter.units = Merge(
                CreateDefaultPartySpawns(),
                CreateBossEnemySpawns());
            return encounter;
        }

        public static EncounterDefinitionSO CreateById(string encounterId)
        {
            return encounterId switch
            {
                EncounterCatalog.BattleGrunts => CreateBattleEncounter(),
                EncounterCatalog.EliteGrunts => CreateEliteEncounter(),
                EncounterCatalog.BossDespair => CreateBossEncounter(),
                _ => CreateDemoEncounter()
            };
        }

        public static EncounterDefinitionSO CreateBattleEncounter()
        {
            var encounter = ScriptableObject.CreateInstance<EncounterDefinitionSO>();
            encounter.encounterId = EncounterCatalog.BattleGrunts;
            encounter.units = CreateBattleEnemySpawns();
            return encounter;
        }

        public static EncounterDefinitionSO CreateEliteEncounter()
        {
            var encounter = ScriptableObject.CreateInstance<EncounterDefinitionSO>();
            encounter.encounterId = EncounterCatalog.EliteGrunts;
            encounter.units = CreateEliteEnemySpawns();
            return encounter;
        }

        public static EncounterDefinitionSO CreateBossEncounter()
        {
            var encounter = ScriptableObject.CreateInstance<EncounterDefinitionSO>();
            encounter.encounterId = EncounterCatalog.BossDespair;
            encounter.units = CreateBossEnemySpawns();
            return encounter;
        }

        public static EncounterUnitSpawn[] CreateDefaultPartySpawns() => new[]
        {
            CreateSpawn(GetPresetByKey("tank"), GridSide.Player, 2, 1),
            CreateSpawn(GetPresetByKey("ren"), GridSide.Player, 2, 2),
            CreateSpawn(GetPresetByKey("mage"), GridSide.Player, 2, 3)
        };

        public static EncounterUnitSpawn[] CreateBattleEnemySpawns() => new[]
        {
            CreateSpawn(GetPresetByKey("grunt_left"), GridSide.Enemy, 2, 1),
            CreateSpawn(GetPresetByKey("grunt_right"), GridSide.Enemy, 2, 3)
        };

        public static EncounterUnitSpawn[] CreateEliteEnemySpawns() => new[]
        {
            CreateSpawn(GetPresetByKey("grunt_left"), GridSide.Enemy, 2, 1),
            CreateSpawnInternal(GetPresetByKey("grunt"), GridSide.Enemy, 1, 1),
            CreateSpawn(GetPresetByKey("grunt_right"), GridSide.Enemy, 2, 3)
        };

        public static EncounterUnitSpawn[] CreateBossEnemySpawns() => new[]
        {
            CreateSpawn(GetPresetByKey("grunt_left"), GridSide.Enemy, 2, 1),
            CreateSpawnInternal(GetPresetByKey("boss_despair"), GridSide.Enemy, 1, 1),
            CreateSpawn(GetPresetByKey("grunt_right"), GridSide.Enemy, 2, 3)
        };

        private static EncounterUnitSpawn[] Merge(EncounterUnitSpawn[] a, EncounterUnitSpawn[] b)
        {
            var merged = new EncounterUnitSpawn[a.Length + b.Length];
            System.Array.Copy(a, 0, merged, 0, a.Length);
            System.Array.Copy(b, 0, merged, a.Length, b.Length);
            return merged;
        }

        public static UnitPresetSO GetPresetByKey(string key)
        {
            var assetKey = key switch
            {
                "grunt" or "grunt_left" or "grunt_right" => "Grunt",
                "boss_despair" => "Boss_Despair",
                "ren" => "Ren",
                "tank" => "Tank",
                "mage" => "Mage",
                _ => key
            };

            var fromResources = Resources.Load<UnitPresetSO>($"UnitPresets/UnitPreset_{assetKey}");
            if (fromResources != null)
            {
                return fromResources;
            }

            return key switch
            {
                "ren" => CreateRenPreset(),
                "tank" => CreateTankPreset(),
                "mage" => CreateMagePreset(),
                "grunt_left" => CreateGruntPreset("grunt_left"),
                "grunt_right" => CreateGruntPreset("grunt_right"),
                "boss_despair" => CreateBossDespairPreset(),
                _ => CreateGruntPreset(key ?? "grunt")
            };
        }

        private static EncounterUnitSpawn CreateSpawnInternal(
            UnitPresetSO preset,
            GridSide side,
            int row,
            int column)
        {
            return new EncounterUnitSpawn
            {
                preset = preset,
                side = side,
                row = row,
                column = column
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
            preset.skills = CreateStandardKit("ren", "Strike", "Riposte", "Finale");
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
            preset.skills = CreateStandardKit("tank", "Ram", "Bulwark", "Hold");
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
            preset.skills = CreateStandardKit("mage", "Pulse", "Arc", "Cataclysm");
            return preset;
        }

        private static UnitPresetSO CreateBossDespairPreset()
        {
            var preset = ScriptableObject.CreateInstance<UnitPresetSO>();
            preset.unitId = "boss_despair";
            preset.displayName = "Knight of Despair";
            preset.role = UnitRole.Boss;
            preset.stats = new UnitStats
            {
                Element = HarmonyElement.Rhythm,
                StrengthType = DamageType.Physical,
                Strength = 58f,
                Endurance = 20f,
                HeartBeat = 130,
                BaseLuck = 5f,
                CritMultiplier = 1.1f,
                MaxHp = 1680,
                BaseSpeed = 8
            };
            preset.placeholderColor = new Color(0.45f, 0.2f, 0.55f);
            preset.skills = new[]
            {
                CreateGruntStrike("boss_despair_core", "Core Strike")
            };
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
            skill.baseDamage = 0;
            return skill;
        }

        private static SkillDefinitionSO[] CreateStandardKit(
            string prefix,
            string basic,
            string skill,
            string ult)
        {
            return new[]
            {
                CreateSkill($"{prefix}_basic", basic, SkillSlotKind.BasicAttack, 1, ActionGlowType.Attack),
                CreateSkill($"{prefix}_skill", skill, SkillSlotKind.Skill, 2, ActionGlowType.Attack),
                CreateSkill($"{prefix}_ult", ult, SkillSlotKind.Ultimate, 3, ActionGlowType.Rush)
            };
        }

        private static SkillDefinitionSO CreateSkill(
            string id,
            string name,
            SkillSlotKind kind,
            int tier,
            ActionGlowType glow)
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
            skill.baseDamage = 0;
            return skill;
        }
    }
}

#if UNITY_EDITOR
using System.IO;
using FracturedChorus.Combat.Damage;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class CombatDataAssetGenerator
    {
        private const string StatBlockFolder = "Assets/FracturedChorus/Resources/StatBlocks";
        private const string PresetFolder = "Assets/FracturedChorus/Resources/UnitPresets";
        private const string SkillFolder = "Assets/FracturedChorus/Resources/Skills";

        [MenuItem("Fractured Chorus/Create Default Stat Blocks & Presets")]
        public static void CreateDefaultAssets()
        {
            EnsureFolder(StatBlockFolder);
            EnsureFolder(PresetFolder);
            EnsureFolder(SkillFolder);

            // Baseline = Lv15 optimal build (xem docs/combat/CHARACTER_LEVEL_PROGRESS.md).
            var renBlock = CreateStatBlock("StatBlock_Ren", HarmonyElement.Melody, DamageType.Physical, 42, 10.8f, 167, 18, 1.35f, 114, 12);
            var tankBlock = CreateStatBlock("StatBlock_Tank", HarmonyElement.Rhythm, DamageType.Physical, 35, 18.2f, 127, 8, 1.15f, 260, 8);
            var mageBlock = CreateStatBlock("StatBlock_Mage", HarmonyElement.Harmony, DamageType.Magical, 50, 9.8f, 147, 16, 1.3f, 73, 10);
            var gruntBlock = CreateStatBlock("StatBlock_Grunt", HarmonyElement.Rhythm, DamageType.Physical, 60, 8, 120, 5, 1.1f, 150, 9);

            var renSkills = CreateStandardSkills("ren", "Strike", "Riposte", "Finale");
            var tankSkills = CreateStandardSkills("tank", "Ram", "Bulwark", "Hold");
            var mageSkills = CreateStandardSkills("mage", "Pulse", "Arc", "Cataclysm");
            var gruntStrike = CreateSkillAsset("grunt_strike", "Strike", SkillSlotKind.BasicAttack, 1,
                ActionGlowType.Attack, 0);

            CreatePreset("UnitPreset_Ren", "ren", "Ren", UnitRole.Dps, renBlock, renSkills,
                new Color(0.9f, 0.35f, 0.45f));
            CreatePreset("UnitPreset_Tank", "tank", "Tank", UnitRole.Tank, tankBlock, tankSkills,
                new Color(0.35f, 0.55f, 0.95f));
            CreatePreset("UnitPreset_Mage", "mage", "Mage", UnitRole.Mage, mageBlock, mageSkills,
                new Color(0.65f, 0.35f, 0.95f));
            CreatePreset("UnitPreset_Grunt", "grunt", "Grunt", UnitRole.Grunt, gruntBlock,
                new[] { gruntStrike }, new Color(0.85f, 0.25f, 0.2f));

            CreateBossDespairAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Fractured Chorus] Stat blocks + presets in Resources/StatBlocks and Resources/UnitPresets.");
        }

        [MenuItem("Fractured Chorus/Create Boss — Knight of Despair Assets")]
        public static void CreateBossDespairAssetsMenu()
        {
            CreateBossDespairAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Fractured Chorus] Boss assets: StatBlock_Boss_Despair, UnitPreset_Boss_Despair, boss_despair_core.");
        }

        public static void CreateBossDespairAssets()
        {
            EnsureFolder(StatBlockFolder);
            EnsureFolder(PresetFolder);
            EnsureFolder(SkillFolder);

            var bossBlock = CreateStatBlock(
                "StatBlock_Boss_Despair",
                HarmonyElement.Rhythm,
                DamageType.Physical,
                58f,
                20f,
                130,
                5f,
                1.1f,
                1680,
                8);

            var coreStrike = CreateSkillAsset(
                "boss_despair_core",
                "Core Strike",
                SkillSlotKind.BasicAttack,
                1,
                ActionGlowType.Attack,
                0);

            var battleSprite = LoadKnightOfDespairSprite();
            var preset = CreatePreset(
                "UnitPreset_Boss_Despair",
                "boss_despair",
                "Knight of Despair",
                UnitRole.Boss,
                bossBlock,
                new[] { coreStrike },
                new Color(0.45f, 0.2f, 0.55f));
            preset.battleSprite = battleSprite;
            preset.telegraphAttacksPerPhase = 3;
            EditorUtility.SetDirty(preset);
        }

        private static Sprite LoadKnightOfDespairSprite()
        {
            string[] paths =
            {
                "Assets/FracturedChorus/Art/Characters/KnightOfDespair/The_Knight_of_Despair_Idle_Sprite.png",
                "Assets/FracturedChorus/Art/Characters/The_Knight_of_Despair_Idle_Sprite.png"
            };

            foreach (var path in paths)
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var asset in assets)
                {
                    if (asset is Sprite sprite)
                    {
                        return sprite;
                    }
                }
            }

            Debug.LogWarning(
                "[Fractured Chorus] Knight sprite missing — place a PNG in Art/Characters/KnightOfDespair/, then run the menu again.");
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
                var name = Path.GetFileName(path);
                if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                {
                    EnsureFolder(parent);
                }

                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static UnitStatBlockSO CreateStatBlock(
            string fileName,
            HarmonyElement element,
            DamageType strengthType,
            float strength,
            float endurance,
            int heartBeat,
            float baseLuck,
            float critMultiplier,
            int maxHp,
            int baseSpeed)
        {
            var path = $"{StatBlockFolder}/{fileName}.asset";
            var block = LoadOrCreate<UnitStatBlockSO>(path);
            block.blockId = fileName;
            block.element = element;
            block.strengthType = strengthType;
            block.strength = strength;
            block.endurance = endurance;
            block.heartBeat = heartBeat;
            block.baseLuck = baseLuck;
            block.critMultiplier = critMultiplier;
            block.maxHp = maxHp;
            block.baseSpeed = baseSpeed;
            EditorUtility.SetDirty(block);
            return block;
        }

        private static SkillDefinitionSO[] CreateStandardSkills(
            string prefix,
            string basic,
            string skill,
            string ult)
        {
            return new[]
            {
                CreateSkillAsset($"{prefix}_basic", basic, SkillSlotKind.BasicAttack, 1, ActionGlowType.Attack, 0),
                CreateSkillAsset($"{prefix}_skill", skill, SkillSlotKind.Skill, 2, ActionGlowType.Attack, 0),
                CreateSkillAsset($"{prefix}_ult", ult, SkillSlotKind.Ultimate, 3, ActionGlowType.Rush, 0)
            };
        }

        private static SkillDefinitionSO CreateSkillAsset(
            string id,
            string displayName,
            SkillSlotKind kind,
            int tier,
            ActionGlowType glow,
            int baseDamage)
        {
            var path = $"{SkillFolder}/{id}.asset";
            var skill = LoadOrCreate<SkillDefinitionSO>(path);
            skill.skillId = id;
            skill.displayName = displayName;
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
            skill.baseDamage = baseDamage;
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static UnitPresetSO CreatePreset(
            string fileName,
            string unitId,
            string displayName,
            UnitRole role,
            UnitStatBlockSO statBlock,
            SkillDefinitionSO[] skills,
            Color color)
        {
            var path = $"{PresetFolder}/{fileName}.asset";
            var preset = LoadOrCreate<UnitPresetSO>(path);
            preset.unitId = unitId;
            preset.displayName = displayName;
            preset.role = role;
            preset.statBlock = statBlock;
            preset.skills = skills;
            preset.placeholderColor = color;
            EditorUtility.SetDirty(preset);
            return preset;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }
    }
}
#endif

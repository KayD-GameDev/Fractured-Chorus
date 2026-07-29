using System.Collections.Generic;
using FracturedChorus.Combat.Difficulty;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using FracturedChorus.Meta;
using UnityEngine;

namespace FracturedChorus.Combat.Bootstrap
{
    public static class PartyLoadoutApplicator
    {
        private static SkillDefinitionSO[] s_allSkills;

        public static void ApplyToUnit(CombatUnit unit)
        {
            if (unit == null || unit.Side != GridSide.Player || !GameMetaSession.HasSession)
            {
                return;
            }

            var characterId = ResolveCharacterId(unit);
            if (string.IsNullOrEmpty(characterId))
            {
                return;
            }

            var entry = GameMetaSession.Current.Loadout.GetOrCreate(characterId);
            EnsureDefaultEquipped(entry, characterId);
            ApplyStatPoints(unit, entry);

            var skills = ResolveEquippedSkills(entry.EquippedSkillIds);
            if (skills.Length > 0)
            {
                unit.ReplaceSkills(skills);
            }
        }

        public static void ApplyTutorialBasics(CombatUnit unit)
        {
            if (unit == null || unit.Side != GridSide.Player)
            {
                return;
            }

            var characterId = ResolveCharacterId(unit);
            if (string.IsNullOrEmpty(characterId))
            {
                return;
            }

            if (GameMetaSession.HasSession)
            {
                var entry = GameMetaSession.Current.Loadout.GetOrCreate(characterId);
                ApplyStatPoints(unit, entry);
            }

            var basicId = characterId switch
            {
                PartyCharacterIds.Ren => "ren_basic",
                PartyCharacterIds.Coda => "mage_basic",
                PartyCharacterIds.Charlotte => "Charlott_basic",
                _ => null
            };

            if (string.IsNullOrEmpty(basicId))
            {
                return;
            }

            var skills = ResolveEquippedSkills(new[] { basicId });
            if (skills.Length > 0)
            {
                unit.ReplaceSkills(skills);
            }
        }

        public static void ApplyDifficultyToEnemy(CombatUnit unit)
        {
            if (unit == null || unit.Side != GridSide.Enemy)
            {
                return;
            }

            var difficulty = GameMetaSession.HasSession
                ? GameMetaSession.Current.Difficulty
                : DifficultyRuntime.Cadence;
            var mult = DifficultyRuntime.Get(difficulty);
            if (Mathf.Approximately(mult.EnemyHp, 1f))
            {
                return;
            }

            var newMax = Mathf.Max(1, Mathf.RoundToInt(unit.Stats.MaxHp * mult.EnemyHp));
            unit.Stats.MaxHp = newMax;
            unit.SetCurrentHp(newMax);
        }

        private static void ApplyStatPoints(CombatUnit unit, CharacterLoadoutEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            unit.Stats.Strength += entry.StrPoints;
            unit.Stats.Endurance += entry.EnPoints;
            unit.Stats.HeartBeat += entry.HbPoints * 5;
            unit.Stats.MaxHp += entry.StrPoints * (unit.UnitId.Contains("tank") || unit.UnitId.Contains("charlotte") ? 6 : 2);
            unit.SetCurrentHp(unit.Stats.MaxHp);
        }

        private static void EnsureDefaultEquipped(CharacterLoadoutEntry entry, string characterId)
        {
            if (entry.EquippedSkillIds == null || entry.EquippedSkillIds.Length != 3)
            {
                entry.EquippedSkillIds = new string[3];
            }

            var any = false;
            for (var i = 0; i < entry.EquippedSkillIds.Length; i++)
            {
                if (!string.IsNullOrEmpty(entry.EquippedSkillIds[i]))
                {
                    any = true;
                    break;
                }
            }

            if (any)
            {
                return;
            }

            switch (characterId)
            {
                case PartyCharacterIds.Ren:
                    entry.EquippedSkillIds = new[] { "ren_basic", "ren_skill", "ren_ult" };
                    break;
                case PartyCharacterIds.Charlotte:
                    entry.EquippedSkillIds = new[] { "Charlott_basic", "tank_skill", "tank_ult" };
                    break;
                case PartyCharacterIds.Coda:
                    entry.EquippedSkillIds = new[] { "mage_basic", "mage_skill", "mage_ult" };
                    break;
            }
        }

        private static SkillDefinitionSO[] ResolveEquippedSkills(string[] skillIds)
        {
            EnsureSkillCache();
            var list = new List<SkillDefinitionSO>();
            if (skillIds == null)
            {
                return list.ToArray();
            }

            foreach (var id in skillIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                var skill = FindSkill(id);
                if (skill != null)
                {
                    list.Add(skill);
                }
            }

            return list.ToArray();
        }

        private static SkillDefinitionSO FindSkill(string id)
        {
            foreach (var skill in s_allSkills)
            {
                if (skill == null)
                {
                    continue;
                }

                if (string.Equals(skill.skillId, id, System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(skill.name, id, System.StringComparison.OrdinalIgnoreCase))
                {
                    return skill;
                }
            }

            return Resources.Load<SkillDefinitionSO>($"Skills/{id}");
        }

        private static void EnsureSkillCache()
        {
            if (s_allSkills != null)
            {
                return;
            }

            s_allSkills = Resources.LoadAll<SkillDefinitionSO>("Skills");
            if (s_allSkills == null)
            {
                s_allSkills = System.Array.Empty<SkillDefinitionSO>();
            }
        }

        private static string ResolveCharacterId(CombatUnit unit)
        {
            var id = unit.UnitId?.ToLowerInvariant() ?? string.Empty;
            if (id.Contains("ren"))
            {
                return PartyCharacterIds.Ren;
            }

            if (id.Contains("tank") || id.Contains("charlotte") || id.Contains("charlott"))
            {
                return PartyCharacterIds.Charlotte;
            }

            if (id.Contains("mage") || id.Contains("coda"))
            {
                return PartyCharacterIds.Coda;
            }

            return null;
        }
    }
}

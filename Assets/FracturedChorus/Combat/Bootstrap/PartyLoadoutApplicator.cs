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
            ApplyStatPoints(unit, entry, characterId);

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
                ApplyStatPoints(unit, entry, characterId);
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
            if (Mathf.Approximately(mult.ResolvedEnemyHp, 1f))
            {
                return;
            }

            var newMax = Mathf.Max(1, Mathf.RoundToInt(unit.Stats.MaxHp * mult.ResolvedEnemyHp));
            unit.Stats.MaxHp = newMax;
            unit.SetCurrentHp(newMax);
        }

        private static void ApplyStatPoints(CombatUnit unit, CharacterLoadoutEntry entry, string characterId)
        {
            if (entry == null)
            {
                return;
            }

            unit.Stats.Strength += entry.StrPoints;
            unit.Stats.Magic += entry.MaPoints;
            unit.Stats.Endurance += entry.EnPoints;
            unit.Stats.HeartBeat += entry.HbPoints * 5;
            unit.Stats.MaxHp = ResolveMaxHp(characterId, unit.Stats.Strength, unit.Stats.Magic);
            unit.SetCurrentHp(unit.Stats.MaxHp);
        }

        /// <summary>SoT HP formulas — progression design §3.</summary>
        public static int ResolveMaxHp(string characterId, float strength, float magic)
        {
            return characterId switch
            {
                PartyCharacterIds.Charlotte => Mathf.RoundToInt(strength * 6f + 50f),
                PartyCharacterIds.Coda => Mathf.RoundToInt(strength * 2f + magic * 0.35f + 15f),
                _ => Mathf.RoundToInt(strength * 2f + 30f)
            };
        }

        private static void EnsureDefaultEquipped(CharacterLoadoutEntry entry, string characterId)
        {
            if (entry.EquippedSkillIds == null
                || entry.EquippedSkillIds.Length != CharacterLoadoutEntry.EquippedSkillSlotCount)
            {
                entry.EquippedSkillIds = PartyLoadoutState.NormalizeSkillSlots(entry.EquippedSkillIds);
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
                    entry.EquippedSkillIds = PartyLoadoutState.NormalizeSkillSlots(
                        new[] { "ren_basic", "ren_skill", "ren_ult" });
                    break;
                case PartyCharacterIds.Charlotte:
                    entry.EquippedSkillIds = PartyLoadoutState.NormalizeSkillSlots(
                        new[] { "Charlott_basic", "tank_skill", "tank_ult" });
                    break;
                case PartyCharacterIds.Coda:
                    entry.EquippedSkillIds = PartyLoadoutState.NormalizeSkillSlots(
                        new[] { "mage_basic", "mage_skill", "mage_ult" });
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

        /// <summary>
        /// Bảng map tường minh, xếp theo thứ tự ưu tiên. Khớp chính xác trước, sau đó mới khớp tiền tố
        /// để các preset biến thể (vd "ren_alt") vẫn nhận đúng nhân vật.
        /// "dps" là unitId cũ của UnitPreset_Ren.asset, giữ lại để save đời trước còn đọc được.
        /// </summary>
        private static readonly (string Key, string CharacterId)[] CharacterIdAliases =
        {
            ("ren", PartyCharacterIds.Ren),
            ("dps", PartyCharacterIds.Ren),
            ("charlotte", PartyCharacterIds.Charlotte),
            ("charlott", PartyCharacterIds.Charlotte),
            ("tank", PartyCharacterIds.Charlotte),
            ("coda", PartyCharacterIds.Coda),
            ("mage", PartyCharacterIds.Coda)
        };

        public static string ResolveCharacterId(CombatUnit unit)
        {
            if (unit == null)
            {
                return null;
            }

            var resolved = ResolveCharacterId(unit.UnitId) ?? ResolveCharacterId(unit.DisplayName);
            if (resolved == null)
            {
                Debug.LogWarning(
                    $"[Fractured Chorus] PartyLoadoutApplicator: không map được party member " +
                    $"unitId='{unit.UnitId}' displayName='{unit.DisplayName}' sang characterId. " +
                    "Skill đã trang bị và stat point sẽ bị bỏ qua — kiểm tra unitId của UnitPresetSO.");
            }

            return resolved;
        }

        public static string ResolveCharacterId(string rawId)
        {
            if (string.IsNullOrWhiteSpace(rawId))
            {
                return null;
            }

            var key = rawId.Trim();
            foreach (var alias in CharacterIdAliases)
            {
                if (string.Equals(key, alias.Key, System.StringComparison.OrdinalIgnoreCase))
                {
                    return alias.CharacterId;
                }
            }

            foreach (var alias in CharacterIdAliases)
            {
                if (key.StartsWith(alias.Key, System.StringComparison.OrdinalIgnoreCase))
                {
                    return alias.CharacterId;
                }
            }

            return null;
        }
    }
}

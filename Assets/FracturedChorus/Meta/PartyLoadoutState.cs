using System;
using System.Collections.Generic;

namespace FracturedChorus.Meta
{
    public static class PartyCharacterIds
    {
        public const string Ren = "ren";
        public const string Charlotte = "charlotte";
        public const string Coda = "coda";
    }

    [Serializable]
    public sealed class CharacterLoadoutEntry
    {
        /// <summary>Level khởi điểm của party, thay cho stub hardcode 15 nằm rải trong UI.</summary>
        public const int DefaultLevel = 15;

        public string CharacterId;
        public const int EquippedSkillSlotCount = 5;

        public string[] EquippedSkillIds =
        {
            string.Empty, string.Empty, string.Empty, string.Empty, string.Empty
        };
        public int UnspentStatPoints;
        public int StrPoints;
        public int MaPoints;
        public int EnPoints;
        public int HbPoints;
        public int Level = DefaultLevel;
        public int Exp;

        public CharacterLoadoutEntry()
        {
        }

        public CharacterLoadoutEntry(string characterId)
        {
            CharacterId = characterId;
        }
    }

    [Serializable]
    public sealed class PartyLoadoutState
    {
        private readonly List<CharacterLoadoutEntry> _entries = new List<CharacterLoadoutEntry>();

        public IReadOnlyList<CharacterLoadoutEntry> Entries => _entries;

        public PartyLoadoutState()
        {
            SeedDefaults();
        }

        public CharacterLoadoutEntry GetOrCreate(string characterId)
        {
            foreach (var entry in _entries)
            {
                if (string.Equals(entry.CharacterId, characterId, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            var created = new CharacterLoadoutEntry(characterId);
            _entries.Add(created);
            return created;
        }

        public bool TryGet(string characterId, out CharacterLoadoutEntry entry)
        {
            foreach (var candidate in _entries)
            {
                if (string.Equals(candidate.CharacterId, characterId, StringComparison.Ordinal))
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = null;
            return false;
        }

        public void ImportEntry(CharacterLoadoutEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.CharacterId))
            {
                return;
            }

            var existing = GetOrCreate(entry.CharacterId);
            existing.EquippedSkillIds = NormalizeSkillSlots(entry.EquippedSkillIds);
            existing.UnspentStatPoints = Math.Max(0, entry.UnspentStatPoints);
            existing.StrPoints = Math.Max(0, entry.StrPoints);
            existing.MaPoints = Math.Max(0, entry.MaPoints);
            existing.EnPoints = Math.Max(0, entry.EnPoints);
            existing.HbPoints = Math.Max(0, entry.HbPoints);

            // Save version 2 không có Level nên đọc lên là 0 — coi như level khởi điểm.
            existing.Level = entry.Level > 0 ? entry.Level : CharacterLoadoutEntry.DefaultLevel;
            existing.Exp = Math.Max(0, entry.Exp);
        }

        public void Clear()
        {
            _entries.Clear();
            SeedDefaults();
        }

        private void SeedDefaults()
        {
            _entries.Clear();
            _entries.Add(new CharacterLoadoutEntry(PartyCharacterIds.Ren));
            _entries.Add(new CharacterLoadoutEntry(PartyCharacterIds.Charlotte));
            _entries.Add(new CharacterLoadoutEntry(PartyCharacterIds.Coda));
        }

        public static string[] NormalizeSkillSlots(string[] source)
        {
            var slots = new string[CharacterLoadoutEntry.EquippedSkillSlotCount];
            if (source == null)
            {
                return slots;
            }

            for (var i = 0; i < slots.Length; i++)
            {
                slots[i] = i < source.Length ? source[i] ?? string.Empty : string.Empty;
            }

            return slots;
        }
    }
}

using System;
using System.Collections.Generic;

namespace FracturedChorus.Meta
{
    [Serializable]
    public sealed class PartyUnitVitals
    {
        public string UnitId;
        public int Hp;
        public int MaxHp;

        public PartyUnitVitals()
        {
        }

        public PartyUnitVitals(string unitId, int hp, int maxHp)
        {
            UnitId = unitId;
            Hp = hp;
            MaxHp = maxHp;
        }
    }

    /// <summary>
    /// HP hiện tại của party giữa các trận. Trước đây chỉ sống trong PartyRunHpStore (static, mất khi tắt game);
    /// state này là bản chụp được ghi xuống file save.
    /// </summary>
    [Serializable]
    public sealed class PartyVitalsState
    {
        private readonly List<PartyUnitVitals> _entries = new List<PartyUnitVitals>();

        public IReadOnlyList<PartyUnitVitals> Entries => _entries;

        public bool HasData => _entries.Count > 0;

        public void Set(string unitId, int hp, int maxHp)
        {
            if (string.IsNullOrWhiteSpace(unitId))
            {
                return;
            }

            var entry = Find(unitId);
            if (entry == null)
            {
                entry = new PartyUnitVitals(unitId, 0, 0);
                _entries.Add(entry);
            }

            entry.MaxHp = Math.Max(0, maxHp);
            entry.Hp = Math.Clamp(hp, 0, entry.MaxHp > 0 ? entry.MaxHp : int.MaxValue);
        }

        public bool TryGet(string unitId, out int hp, out int maxHp)
        {
            var entry = Find(unitId);
            if (entry == null)
            {
                hp = 0;
                maxHp = 0;
                return false;
            }

            hp = entry.Hp;
            maxHp = entry.MaxHp;
            return true;
        }

        public void Import(PartyUnitVitals entry)
        {
            if (entry == null)
            {
                return;
            }

            Set(entry.UnitId, entry.Hp, entry.MaxHp);
        }

        public void Clear()
        {
            _entries.Clear();
        }

        private PartyUnitVitals Find(string unitId)
        {
            foreach (var entry in _entries)
            {
                if (string.Equals(entry.UnitId, unitId, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }
    }
}

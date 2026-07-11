using System;
using System.Collections.Generic;

namespace FracturedChorus.Meta
{
    [Serializable]
    public sealed class StoryFlags
    {
        private readonly Dictionary<string, bool> _flags = new Dictionary<string, bool>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _counters = new Dictionary<string, int>(StringComparer.Ordinal);

        public bool GetBool(string flagId, bool defaultValue = false)
        {
            return _flags.TryGetValue(flagId, out var value) ? value : defaultValue;
        }

        public void SetBool(string flagId, bool value)
        {
            _flags[flagId] = value;
        }

        public int GetInt(string flagId, int defaultValue = 0)
        {
            return _counters.TryGetValue(flagId, out var value) ? value : defaultValue;
        }

        public void SetInt(string flagId, int value)
        {
            _counters[flagId] = value;
        }

        public bool Has(string flagId) => _flags.ContainsKey(flagId) && _flags[flagId];

        public IReadOnlyDictionary<string, bool> ExportBools() => new Dictionary<string, bool>(_flags);

        public IReadOnlyDictionary<string, int> ExportInts() => new Dictionary<string, int>(_counters);

        public void ImportBool(string flagId, bool value) => _flags[flagId] = value;

        public void ImportInt(string flagId, int value) => _counters[flagId] = value;
    }
}

using System;
using System.Collections.Generic;

namespace FracturedChorus.Meta
{
    public static class BondNpcIds
    {
        public const string Ren = "ren";
        public const string Charlotte = "charlotte";
        public const string Coda = "coda";
        public const string Ryo = "ryo";
        public const string MeiLin = "mei_lin";
        public const string Astra = "astra";
    }

    [Serializable]
    public sealed class BondState
    {
        private readonly Dictionary<string, BondProgress> _bonds = new Dictionary<string, BondProgress>(StringComparer.Ordinal);

        public BondState()
        {
            SeedDefaults();
        }

        public IReadOnlyDictionary<string, BondProgress> Bonds => _bonds;

        public BondProgress GetOrCreate(string npcId)
        {
            if (!_bonds.TryGetValue(npcId, out var bond))
            {
                bond = new BondProgress(npcId, EchoKey.Melody);
                _bonds[npcId] = bond;
            }

            return bond;
        }

        public bool TryGet(string npcId, out BondProgress bond) => _bonds.TryGetValue(npcId, out bond);

        public void SeedDefaults()
        {
            _bonds[BondNpcIds.Ren] = new BondProgress(BondNpcIds.Ren, EchoKey.Melody, arcCap: 4);
            _bonds[BondNpcIds.Charlotte] = new BondProgress(BondNpcIds.Charlotte, EchoKey.Bass, arcCap: 3);
            _bonds[BondNpcIds.Coda] = new BondProgress(BondNpcIds.Coda, EchoKey.Harmony, arcCap: 4);
            _bonds[BondNpcIds.Ryo] = new BondProgress(BondNpcIds.Ryo, EchoKey.Measure, arcCap: 2);
            _bonds[BondNpcIds.MeiLin] = new BondProgress(BondNpcIds.MeiLin, EchoKey.Dissonance, arcCap: 2);
            _bonds[BondNpcIds.Astra] = new BondProgress(BondNpcIds.Astra, EchoKey.Pulse, arcCap: 5);
        }

        public void ImportBond(BondProgress progress)
        {
            if (progress == null || string.IsNullOrWhiteSpace(progress.NpcId))
            {
                return;
            }

            _bonds[progress.NpcId] = progress;
        }
    }
}

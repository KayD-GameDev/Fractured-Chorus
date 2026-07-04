namespace FracturedChorus.RunMap.Core
{
    public sealed class CadenceRunProgress
    {
        private static CadenceRunProgress s_session;

        public VaultFingerId ActiveVault { get; private set; } = VaultFingerId.Pinky;
        public PinkySectorId CurrentSector { get; private set; } = PinkySectorId.Pulse;
        public bool PulseCleared { get; private set; }
        public bool EchoCleared { get; private set; }
        public bool CanticleCleared { get; private set; }
        public int RunSeed { get; private set; }

        public bool IsPinkyComplete => PulseCleared && EchoCleared && CanticleCleared;

        public static CadenceRunProgress Session
        {
            get
            {
                if (s_session == null)
                {
                    s_session = new CadenceRunProgress();
                }

                return s_session;
            }
        }

        public static void ResetSession()
        {
            s_session = new CadenceRunProgress();
        }

        public void BeginPinkyRun(int seed)
        {
            RunSeed = seed;
            ActiveVault = VaultFingerId.Pinky;
            CurrentSector = PinkySectorId.Pulse;
            PulseCleared = false;
            EchoCleared = false;
            CanticleCleared = false;
        }

        public void MarkSectorCleared(PinkySectorId sector)
        {
            switch (sector)
            {
                case PinkySectorId.Pulse:
                    PulseCleared = true;
                    CurrentSector = PinkySectorId.Echo;
                    break;
                case PinkySectorId.Echo:
                    EchoCleared = true;
                    CurrentSector = PinkySectorId.Canticle;
                    break;
                case PinkySectorId.Canticle:
                    CanticleCleared = true;
                    break;
            }
        }

        public bool HasNextSector(PinkySectorId sector) => sector switch
        {
            PinkySectorId.Pulse => true,
            PinkySectorId.Echo => true,
            PinkySectorId.Canticle => false,
            _ => false
        };

        public PinkySectorId? NextSector(PinkySectorId sector) => sector switch
        {
            PinkySectorId.Pulse => PinkySectorId.Echo,
            PinkySectorId.Echo => PinkySectorId.Canticle,
            _ => null
        };

        public string SectorBossLabel(PinkySectorId sector) => sector switch
        {
            PinkySectorId.Pulse => "Mimi — The Pulse",
            PinkySectorId.Echo => "Kiki — The Echo",
            PinkySectorId.Canticle => "Astra — Chart Lord",
            _ => "Boss"
        };
    }
}

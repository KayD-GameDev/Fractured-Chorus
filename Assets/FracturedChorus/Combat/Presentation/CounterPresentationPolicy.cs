using System.Collections.Generic;

namespace FracturedChorus.Combat.Presentation
{
    public enum CounterBodyMode
    {
        Restart,
        HitRetrigger,
        Burst
    }

    public sealed class CounterPresentationPolicy
    {
        public float RestartGapSec = 0.28f;
        public float BurstWindowSec = 0.9f;
        public int BurstCount = 3;

        private readonly List<double> _partyHits = new();
        private readonly Dictionary<string, double> _lastUnitHit = new();
        private bool _burstFiredThisWindow;

        public void Reset()
        {
            _partyHits.Clear();
            _lastUnitHit.Clear();
            _burstFiredThisWindow = false;
        }

        public int RegisterPartyPerfect(double dspNow, out bool burstTriggered)
        {
            Prune(dspNow);
            _partyHits.Add(dspNow);
            var count = _partyHits.Count;
            burstTriggered = false;
            if (count >= BurstCount && !_burstFiredThisWindow)
            {
                _burstFiredThisWindow = true;
                burstTriggered = true;
            }

            return count;
        }

        public CounterBodyMode DecideUnitBody(string unitKey, double dspNow, bool useBurst)
        {
            if (string.IsNullOrEmpty(unitKey))
            {
                unitKey = "_";
            }

            if (useBurst)
            {
                _lastUnitHit[unitKey] = dspNow;
                return CounterBodyMode.Burst;
            }

            var gapOk = true;
            if (_lastUnitHit.TryGetValue(unitKey, out var last))
            {
                gapOk = (dspNow - last) >= RestartGapSec;
            }

            _lastUnitHit[unitKey] = dspNow;
            return gapOk ? CounterBodyMode.Restart : CounterBodyMode.HitRetrigger;
        }

        public int PartyHitCountInWindow(double dspNow)
        {
            Prune(dspNow);
            return _partyHits.Count;
        }

        private void Prune(double dspNow)
        {
            var cutoff = dspNow - BurstWindowSec;
            _partyHits.RemoveAll(t => t < cutoff);
            if (_partyHits.Count == 0)
            {
                _burstFiredThisWindow = false;
            }
        }
    }
}

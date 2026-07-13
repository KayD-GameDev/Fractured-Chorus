using System.Collections.Generic;
using FracturedChorus.Audio;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public class CounterPresentationDriver : MonoBehaviour
    {
        [SerializeField] private float restartGapSec = 0.28f;
        [SerializeField] private float burstWindowSec = 0.9f;
        [SerializeField] private int burstCount = 3;
        [SerializeField] private CombatSfxController sfx;
        [SerializeField] private BeatTimelineUIView timelineView;

        private readonly CounterPresentationPolicy _policy = new();
        private readonly List<CombatUnit> _playersScratch = new();
        private readonly List<CombatUnit> _enemiesScratch = new();

        public void Configure(CombatSfxController sfxController, BeatTimelineUIView timeline)
        {
            if (sfxController != null)
            {
                sfx = sfxController;
            }

            if (timeline != null)
            {
                timelineView = timeline;
            }

            ApplyTunablesToPolicy();
        }

        public void ResetPresentation()
        {
            ApplyTunablesToPolicy();
            _policy.Reset();
            timelineView?.HideMultiBanner();
        }

        public void NotifyPerfect(int beatIndex, BeatTimelineEngine timeline)
        {
            if (timeline == null || beatIndex < 0)
            {
                return;
            }

            ApplyTunablesToPolicy();
            var dspNow = AudioSettings.dspTime;
            var partyCount = _policy.RegisterPartyPerfect(dspNow, out var burstTriggered);

            if (sfx != null)
            {
                sfx.PlayPerfectCounter();
            }

            CombatCounterResolver.CollectCounteringPlayerUnits(timeline, beatIndex, _playersScratch);
            var burstAssigned = false;
            foreach (var unit in _playersScratch)
            {
                var useBurst = burstTriggered && !burstAssigned;
                if (useBurst)
                {
                    burstAssigned = true;
                }

                var mode = _policy.DecideUnitBody(unit.UnitId, dspNow, useBurst);
                PlayPlayerBody(unit, mode);
            }

            CombatCounterResolver.CollectCounteredEnemyUnits(timeline, beatIndex, _enemiesScratch);
            foreach (var unit in _enemiesScratch)
            {
                var mode = _policy.DecideUnitBody("enemy:" + unit.UnitId, dspNow, useBurst: false);
                PlayEnemyBody(unit, mode);
            }

            var tier = BossNoteTier.Red;
            var telegraphs = timeline.GetImpactTelegraphsAtBeat(beatIndex);
            if (telegraphs != null && telegraphs.Count > 0 && telegraphs[0] != null)
            {
                tier = telegraphs[0].NoteTier;
            }

            timelineView?.SpawnNoteResolveChip(beatIndex, tier, 1);
            if (partyCount >= burstCount)
            {
                timelineView?.ShowOrRefreshMultiBanner(partyCount);
            }
        }

        private void ApplyTunablesToPolicy()
        {
            _policy.RestartGapSec = restartGapSec;
            _policy.BurstWindowSec = burstWindowSec;
            _policy.BurstCount = Mathf.Max(1, burstCount);
        }

        private static void PlayPlayerBody(CombatUnit unit, CounterBodyMode mode)
        {
            var view = UnitView.FindForUnit(unit);
            if (view == null)
            {
                return;
            }

            switch (mode)
            {
                case CounterBodyMode.HitRetrigger:
                    view.PlayCounterHitRetrigger();
                    break;
                case CounterBodyMode.Burst:
                    view.PlayCounterBurst();
                    break;
                default:
                    view.PlayCounterRestart();
                    break;
            }
        }

        private static void PlayEnemyBody(CombatUnit unit, CounterBodyMode mode)
        {
            var view = UnitView.FindForUnit(unit);
            if (view == null)
            {
                return;
            }

            if (mode == CounterBodyMode.HitRetrigger)
            {
                view.PlayBeCounteredHitRetrigger();
            }
            else
            {
                view.PlayBeCounteredRestart();
            }
        }
    }
}

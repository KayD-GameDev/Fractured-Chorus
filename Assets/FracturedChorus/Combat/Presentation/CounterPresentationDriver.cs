using System.Collections;
using System.Collections.Generic;
using FracturedChorus.Audio;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public class CounterPresentationDriver : MonoBehaviour
    {
        [Header("Feel")]
        [SerializeField] private float restartGapSec = 0.28f;
        [SerializeField] private float burstWindowSec = 0.9f;
        [SerializeField] private int burstCount = 3;

        [Header("Perfect popup")]
        [SerializeField] private Vector2 perfectChipSize = new Vector2(168f, 112f);
        [SerializeField] private float perfectChipDuration = 0.55f;

        [Header("Refs")]
        [SerializeField] private CombatSfxController sfx;
        [SerializeField] private BeatTimelineUIView timelineView;

        private readonly CounterPresentationPolicy _policy = new();
        private readonly List<CombatUnit> _playersScratch = new();
        private readonly List<CombatUnit> _enemiesScratch = new();
        private readonly List<AgendaEntry> _entriesScratch = new();

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
            ApplyPerfectChipDefaults();
        }

        public void ResetPresentation()
        {
            ApplyTunablesToPolicy();
            ApplyPerfectChipDefaults();
            _policy.Reset();
            CombatCounterResolver.ClearPresentationMarkers();
            timelineView?.HideMultiBanner();
        }

        private void OnValidate()
        {
            ApplyPerfectChipDefaults();
        }

        private void ApplyPerfectChipDefaults()
        {
            CounterNoteResolveChipView.DisplaySize = perfectChipSize;
            CounterNoteResolveChipView.DefaultDuration = Mathf.Max(0.05f, perfectChipDuration);
        }

        private void ApplyTunablesToPolicy()
        {
            _policy.RestartGapSec = restartGapSec;
            _policy.BurstWindowSec = burstWindowSec;
            _policy.BurstCount = Mathf.Max(1, burstCount);
            ApplyPerfectChipDefaults();
        }

        public void NotifyPerfect(int beatIndex, BeatTimelineEngine timeline, double targetDspTime = -1d)
        {
            if (timeline == null || beatIndex < 0)
            {
                return;
            }

            if (!CombatCounterResolver.ShouldPresentCounterBodyAtBeat(timeline, beatIndex))
            {
                return;
            }

            ApplyTunablesToPolicy();
            var dspNow = AudioSettings.dspTime;
            var partyCount = _policy.RegisterPartyPerfect(dspNow, out var burstTriggered);
            var deferPerfectToEncounter = EncounterDirector.ActiveInstance != null;

            if (!deferPerfectToEncounter && sfx != null)
            {
                sfx.PlayPerfectCounter(targetDspTime);
            }

            var tier = BossNoteTier.Red;
            var telegraphs = timeline.GetImpactTelegraphsAtBeat(beatIndex);
            if (telegraphs != null && telegraphs.Count > 0 && telegraphs[0] != null)
            {
                tier = telegraphs[0].NoteTier;
            }

            CombatCounterResolver.CollectCounteringPlayerUnits(timeline, beatIndex, _playersScratch);
            CollectCounteringEntries(timeline, beatIndex);

            var counterBody = CombatCounterResolver.SelectCounterBody(_playersScratch);
            var ownsChoreo = EnemyStrikeChoreographer.OwnsCounterPresentation;

            if (!ownsChoreo)
            {
                CombatCounterResolver.MarkCounterPresentations(_entriesScratch);

                if (counterBody != null)
                {
                    var useBurst = burstTriggered;
                    var mode = _policy.DecideUnitBody(counterBody.UnitId, dspNow, useBurst);
                    PlayPlayerBody(counterBody, mode);
                    if (!deferPerfectToEncounter)
                    {
                        SpawnPerfectAboveUnit(counterBody, tier);
                    }
                }
            }
            else if (counterBody != null && !deferPerfectToEncounter)
            {
                SpawnPerfectAboveUnit(counterBody, tier);
            }

            CombatCounterResolver.CollectCounteredEnemyUnits(timeline, beatIndex, _enemiesScratch);
            foreach (var unit in _enemiesScratch)
            {
                var mode = _policy.DecideUnitBody("enemy:" + unit.UnitId, dspNow, useBurst: false);
                PlayEnemyBody(unit, mode);
            }

            if (partyCount >= burstCount)
            {
                timelineView?.ShowOrRefreshMultiBanner(partyCount);
            }
        }

        public void SpawnPerfectForUnit(CombatUnit unit, BossNoteTier tier = BossNoteTier.Red)
        {
            PresentPerfectInEncounter(unit, tier);
        }

        public void PresentPerfectInEncounter(CombatUnit unit, BossNoteTier tier = BossNoteTier.Red)
        {
            ApplyPerfectChipDefaults();
            if (sfx != null)
            {
                sfx.PlayPerfectCounter(-1d);
            }
            else
            {
                var found = FindAnyObjectByType<CombatSfxController>();
                found?.PlayPerfectCounter(-1d);
            }

            SpawnPerfectAboveUnit(unit, tier);
        }

        private void CollectCounteringEntries(BeatTimelineEngine timeline, int beatIndex)
        {
            _entriesScratch.Clear();
            if (timeline == null || beatIndex < 0)
            {
                return;
            }

            foreach (var entry in timeline.Agenda)
            {
                if (entry?.Unit == null || entry.Unit.Side != GridSide.Player ||
                    entry.Skill == null || entry.Skill.IsGuard)
                {
                    continue;
                }

                var active = false;
                foreach (var beat in CombatCounterResolver.GetActiveBeatIndices(entry))
                {
                    if (beat == beatIndex)
                    {
                        active = true;
                        break;
                    }
                }

                if (!active || !_playersScratch.Contains(entry.Unit))
                {
                    continue;
                }

                _entriesScratch.Add(entry);
            }
        }

        private static void SpawnPerfectAboveUnit(CombatUnit unit, BossNoteTier tier)
        {
            var view = UnitView.FindForUnit(unit);
            if (view == null)
            {
                return;
            }

            CounterNoteResolveChipView.SpawnAboveWorld(
                view.GetSkillPanelAboveAnchorWorld() + Vector3.up * 0.15f,
                tier);
        }

        private void PlayPlayerBody(CombatUnit unit, CounterBodyMode mode)
        {
            if (EnemyStrikeChoreographer.OwnsCounterPresentation)
            {
                return;
            }

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

            if (CharlotteCounterShieldView.TrySpawnFor(view, maxHoldSeconds: 0.55f) != null)
            {
                StartCoroutine(DismissCharlotteShieldAfterBlock());
            }
        }

        private IEnumerator DismissCharlotteShieldAfterBlock()
        {
            yield return new WaitForSeconds(0.32f);
            yield return CharlotteCounterShieldView.DismissAllAndWait();
        }

        private static void PlayEnemyBody(CombatUnit unit, CounterBodyMode mode)
        {
            if (EnemyStrikeChoreographer.IsChoreographing(unit))
            {
                return;
            }

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

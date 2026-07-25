using System;
using System.Collections.Generic;
using System.Linq;
using FracturedChorus.Combat.Actions;
using FracturedChorus.Combat.AI;
using FracturedChorus.Combat.Block;
using FracturedChorus.Combat.Cover;
using FracturedChorus.Combat.Damage;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.Combat.Core
{
    public class CombatSession
    {
        public DualGrid Grid { get; private set; }
        public BeatTimelineEngine Timeline { get; private set; }
        public CombatPhase Phase => Timeline?.Phase ?? CombatPhase.Planning;

        public event Action<CombatPhase> OnPhaseChanged;
        public event Action<AgendaEntry> OnActionAssigned;
        public event Action<int> OnScanBeat;
        public event Action<CombatUnit> OnUnitHpChanged;
        public event Action OnEncounterEnded;
        public event Action<int> OnTelegraphsPlanned;

        private SimpleEnemyAI _enemyAi;
        private readonly HashSet<int> _resolvedBeats = new();
        private int _roundSegmentIndex;
        private int _lastScanBeat = -1;
        public PhaseAvTracker PhaseAv { get; } = new();
        public BlockBarrierTracker BlockBarriers { get; } = new();
        public CoverRuntime Cover { get; } = new();
        /// <summary>0 = phases 1–2 (beats 0–31), 1 = phases 3–4, …</summary>
        public int RoundSegmentIndex => _roundSegmentIndex;

        /// <summary>True only before Execute — player may drag units onto grid cells.</summary>
        public bool AllowPlayerReposition { get; private set; } = true;

        /// <summary>True while Deploy reposition / planning pause / between-segment planning — Cover button gate.</summary>
        public bool AllowCoverActivate { get; set; } = true;

        public bool IsEncounterOver =>
            Phase == CombatPhase.Victory || Phase == CombatPhase.Defeat;

        public void Initialize(DualGrid grid, BeatTimelineEngine timeline)
        {
            Grid = grid;
            Timeline = timeline;
            _enemyAi = new SimpleEnemyAI();

            Timeline.OnPhaseChanged += phase => OnPhaseChanged?.Invoke(phase);
            Timeline.OnActionAssigned += entry => OnActionAssigned?.Invoke(entry);
            Timeline.OnScanAdvanced += beat => OnScanBeat?.Invoke(beat);

            foreach (var unit in Grid.GetAllUnits())
            {
                unit.OnHpChanged += u => OnUnitHpChanged?.Invoke(u);
                if (unit.Side == GridSide.Enemy)
                {
                    unit.OnDied += HandleEnemyDied;
                }
            }

            BeginPlanningRound();
        }

        /// <summary>Pre-plan both phases of the current segment (Deploy + planning preview).</summary>
        public void PrepareTelegraphsForCurrentSegment()
        {
            if (Grid.EnemyUnits.All(u => !u.IsAlive))
            {
                return;
            }

            PrePlanTelegraphsForSegment(_roundSegmentIndex);
            OnTelegraphsPlanned?.Invoke(TimelineConstants.RoundPhaseCount * _roundSegmentIndex);
        }

        public void OnTimelineScanBeat(int beatIndex)
        {
            _lastScanBeat = beatIndex;
        }

        private void HandleEnemyDied(CombatUnit unit)
        {
            if (unit == null || unit.Side != GridSide.Enemy || Timeline == null || IsEncounterOver)
            {
                return;
            }

            RemoveTelegraphsForDeadUnit(unit);
            OnTelegraphsPlanned?.Invoke(GetDeathPhaseIndex());
        }

        /// <summary>Strip dead unit telegraphs from death phase through end of current segment.</summary>
        private void RemoveTelegraphsForDeadUnit(CombatUnit unit)
        {
            var deathPhase = GetDeathPhaseIndex();
            var segmentEnd = TimelineConstants.GetSegmentEndBeatExclusive(_roundSegmentIndex);
            TimelineConstants.GetPhaseBeatRange(deathPhase, out var fromBeat, out _);
            var beatCount = segmentEnd - fromBeat;
            if (beatCount > 0)
            {
                Timeline.RemoveTelegraphsForUnitInRange(unit, fromBeat, beatCount);
            }
        }

        private int GetDeathPhaseIndex()
        {
            var segmentPhaseStart = TimelineConstants.RoundPhaseCount * _roundSegmentIndex;

            if (_lastScanBeat < 0)
            {
                return segmentPhaseStart;
            }

            var scanPhase = TimelineConstants.GetPhaseIndex(_lastScanBeat);
            var segmentPhaseEnd = segmentPhaseStart + TimelineConstants.RoundPhaseCount;
            if (scanPhase >= segmentPhaseStart && scanPhase < segmentPhaseEnd)
            {
                return scanPhase;
            }

            return segmentPhaseStart;
        }

        private void PrePlanTelegraphsForSegment(int segmentIndex)
        {
            var phaseA = TimelineConstants.RoundPhaseCount * segmentIndex;
            var phaseB = phaseA + 1;

            TimelineConstants.GetPhaseBeatRange(phaseA, out _, out var countA);
            if (countA > 0)
            {
                _enemyAi.PlanTelegraphsForPhase(phaseA, Grid, Timeline);
            }

            TimelineConstants.GetPhaseBeatRange(phaseB, out _, out var countB);
            if (countB > 0)
            {
                _enemyAi.PlanTelegraphsForPhase(phaseB, Grid, Timeline);
            }
        }

        public bool TryAssignPlayerAction(CombatUnit unit, SkillDefinitionSO skill, int beatIndex = -1)
        {
            if (unit == null || unit.Side != GridSide.Player || Phase != CombatPhase.Planning || skill == null)
            {
                return false;
            }

            if (beatIndex < 0)
            {
                beatIndex = Timeline.FindNextEmptyBeat();
            }

            if (beatIndex < 0 || !Timeline.CanAssignAction(unit, skill, beatIndex))
            {
                return false;
            }

            if (!Timeline.TryAssignAction(unit, skill, beatIndex))
            {
                return false;
            }

            var entry = Timeline.FindPlayerEntry(unit, beatIndex);
            if (entry != null)
            {
                entry.StandingAfterOverride = SkillFootprintUtil.GetStandingAfter(skill, unit);
                if (unit.PendingReduceS2 > 0)
                {
                    unit.SetPendingReduceS2(0);
                }

                ApplyPlanningUtilityEffects(entry);
            }

            Debug.Log($"[Combat] {unit.DisplayName} → {skill.displayName} @ beat {beatIndex}");
            return true;
        }

        public bool TryRemovePlayerAction(CombatUnit unit, int beatIndex)
        {
            if (unit == null || Phase != CombatPhase.Planning || Timeline == null)
            {
                return false;
            }

            var entry = Timeline.FindPlayerEntry(unit, beatIndex);
            if (entry?.Skill == null)
            {
                return false;
            }

            RevertPlanningUtilityEffects(entry);

            if (!Timeline.TryRemovePlayerAction(unit, beatIndex))
            {
                return false;
            }

            return true;
        }

        private void ApplyPlanningUtilityEffects(AgendaEntry entry)
        {
            if (entry?.Skill == null || entry.Unit == null || Timeline == null)
            {
                return;
            }

            var skill = entry.Skill;
            var empowerPreview = skill.usesPrepEmpower
                && entry.Unit.Prep >= Mathf.Max(1, skill.prepEmpowerThreshold);

            if (skill.effectKind == SkillEffectKind.DelayBossNote)
            {
                var delay = Mathf.Max(1, skill.ResolveEffectValue(empowerPreview));
                var sEnd = entry.BeatIndex + SkillFootprintUtil.GetActiveBeats(skill) - 1;
                var phase = TimelineConstants.GetPhaseIndex(entry.BeatIndex);
                TimelineConstants.GetPhaseBeatRange(phase, out var startBeat, out var count);
                var phaseEndExclusive = startBeat + count;
                var moves = Timeline.DelayImpactTelegraphsAfterBeat(sEnd, phaseEndExclusive, delay);
                entry.PlanningDelayMoves.Clear();
                entry.PlanningDelayMoves.AddRange(moves);
                entry.PlanningDelayAmount = delay;
                entry.PlanningEffectApplied = true;
                entry.EffectPayloadApplied = true;
                Debug.Log(
                    $"[Planning] {entry.Unit.DisplayName} Delay notes after S@{sEnd} +{delay} → {moves.Count} notes" +
                    (empowerPreview ? " (empower preview)" : string.Empty));
                return;
            }

            if (skill.effectKind != SkillEffectKind.ReduceS2)
            {
                return;
            }

            var amount = Mathf.Max(1, skill.ResolveEffectValue(false));
            entry.PlanningReduceTargets.Clear();
            entry.PlanningReduceAmount = amount;

            if (empowerPreview && skill.empowerPartyReduceS2)
            {
                foreach (var ally in Grid.GetAllies(entry.Unit.Side))
                {
                    if (ally == null || !ally.IsAlive)
                    {
                        continue;
                    }

                    ally.SetPendingReduceS2(Mathf.Max(ally.PendingReduceS2, amount));
                    entry.PlanningReduceTargets.Add(ally);
                }
            }
            else
            {
                var target = Grid.GetAllies(entry.Unit.Side)
                    .FirstOrDefault(u => u != null && u.IsAlive && u != entry.Unit)
                    ?? Grid.GetAllies(entry.Unit.Side).FirstOrDefault(u => u != null && u.IsAlive);
                if (target != null)
                {
                    target.SetPendingReduceS2(Mathf.Max(target.PendingReduceS2, amount));
                    entry.PlanningReduceTarget = target;
                    entry.PlanningReduceTargets.Add(target);
                }
            }

            if (empowerPreview && skill.empowerGiftPrepToTarget)
            {
                var giftTarget = entry.PlanningReduceTarget
                    ?? entry.PlanningReduceTargets.FirstOrDefault(u => u != null && u != entry.Unit)
                    ?? entry.PlanningReduceTargets.FirstOrDefault();
                if (giftTarget != null)
                {
                    giftTarget.GainPrep(1);
                    entry.PlanningReduceTarget ??= giftTarget;
                }
            }

            entry.PlanningEffectApplied = true;
            entry.EffectPayloadApplied = true;
            Debug.Log(
                $"[Planning] {entry.Unit.DisplayName} ReduceS2 -{amount} → {entry.PlanningReduceTargets.Count} ally" +
                (empowerPreview ? " (empower preview)" : string.Empty));
        }

        private void RevertPlanningUtilityEffects(AgendaEntry entry)
        {
            if (entry == null || Timeline == null)
            {
                return;
            }

            if (entry.PlanningDelayMoves.Count > 0)
            {
                Timeline.RevertTelegraphMoves(entry.PlanningDelayMoves);
                entry.PlanningDelayMoves.Clear();
                entry.PlanningDelayAmount = 0;
            }

            foreach (var ally in entry.PlanningReduceTargets)
            {
                if (ally != null && ally.PendingReduceS2 > 0)
                {
                    ally.SetPendingReduceS2(0);
                }
            }

            entry.PlanningReduceTargets.Clear();
            entry.PlanningReduceTarget = null;
            entry.PlanningReduceAmount = 0;
            entry.PlanningEffectApplied = false;
            entry.EffectPayloadApplied = false;
        }

        public void EndRoundSegment()
        {
            if (TryEndEncounterIfDecided())
            {
                return;
            }

            Timeline.ClearPlayerAgenda();
            BlockBarriers.Clear();
            _resolvedBeats.Clear();
            _roundSegmentIndex++;
            PrePlanTelegraphsForSegment(_roundSegmentIndex);
            _lastScanBeat = TimelineConstants.GetSegmentStartBeat(_roundSegmentIndex) - 1;
            AllowPlayerReposition = false;
            PhaseAv.ResetForPlanning();
            Timeline.SetPhase(CombatPhase.Planning);
        }

        public void BeginPlanningRound()
        {
            _roundSegmentIndex = 0;
            _resolvedBeats.Clear();
            _lastScanBeat = -1;
            BlockBarriers.Clear();
            AllowPlayerReposition = true;
            AllowCoverActivate = true;
            Timeline.ResetForPlanning();
            PhaseAv.ResetForPlanning();
            Cover.Reset();
            Timeline.SetPhase(CombatPhase.Planning);
        }

        public void LockPlayerReposition()
        {
            AllowPlayerReposition = false;
        }

        public void ConfirmPlanningAndExecute()
        {
            if (Phase != CombatPhase.Planning)
            {
                return;
            }

            if (IsEncounterOver)
            {
                return;
            }

            Timeline.SetPhase(CombatPhase.Executing);
            Cover.BeginWindowIfPending();
            ResolveAnyRemainingBeats();

            if (IsEncounterOver)
            {
                return;
            }

            Timeline.SetPhase(CombatPhase.Upkeep);
            ResolveUpkeep();
        }

        /// <summary>Gọi khi scan bar đi qua một beat — resolve player attack + enemy telegraph.</summary>
        public void ResolveBeatAtScan(int beatIndex)
        {
            if (Timeline == null || beatIndex < 0 || beatIndex >= TimelineConstants.TotalBeats || IsEncounterOver)
            {
                return;
            }

            if (!_resolvedBeats.Add(beatIndex))
            {
                return;
            }

            var telegraphs = Timeline.GetImpactTelegraphsAtBeat(beatIndex);
            var playerEntries = GetPlayerEntriesActiveAtBeat(beatIndex);

            Cover.BeginWindowIfPending();

            TryResolveEmpowerAtBeat(beatIndex, playerEntries);
            TryChannelPrepAtBeat(beatIndex, playerEntries, telegraphs);
            ResolvePlayerAttacksAtBeat(beatIndex, playerEntries, telegraphs);

            foreach (var telegraph in telegraphs)
            {
                ResolveEnemyTelegraphAtBeat(telegraph, beatIndex);
            }

            Cover.TickBeat();
            TryEndEncounterIfDecided();
        }

        private List<AgendaEntry> GetPlayerEntriesActiveAtBeat(int beatIndex)
        {
            var result = new List<AgendaEntry>();
            foreach (var entry in Timeline.Agenda)
            {
                if (entry.Unit == null || entry.Unit.Side != GridSide.Player || entry.Skill == null || entry.Skill.IsGuard)
                {
                    continue;
                }

                foreach (var activeBeat in CombatCounterResolver.GetActiveBeatIndices(entry))
                {
                    if (activeBeat == beatIndex)
                    {
                        result.Add(entry);
                        break;
                    }
                }
            }

            return result;
        }

        private static void TryResolveEmpowerAtBeat(int beatIndex, IReadOnlyList<AgendaEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            foreach (var entry in entries)
            {
                if (entry?.Unit == null || entry.Skill == null || entry.EmpowerResolved)
                {
                    continue;
                }

                var firstActive = entry.BeatIndex;
                if (beatIndex != firstActive)
                {
                    continue;
                }

                entry.EmpowerResolved = true;
                var skill = entry.Skill;
                if (!skill.usesPrepEmpower)
                {
                    continue;
                }

                var threshold = Mathf.Max(1, skill.prepEmpowerThreshold);
                var cost = Mathf.Max(1, skill.prepEmpowerCost);
                if (entry.Unit.Prep < threshold)
                {
                    continue;
                }

                if (!entry.Unit.TrySpendPrep(cost))
                {
                    continue;
                }

                entry.IsEmpowered = true;
                Debug.Log(
                    $"[Prep] {entry.Unit.DisplayName} empower {skill.displayName} (-{cost}) → Prep {entry.Unit.Prep}/{CombatUnit.PrepCap}");
            }
        }

        private void TryChannelPrepAtBeat(
            int beatIndex,
            IReadOnlyList<AgendaEntry> entries,
            IReadOnlyList<EnemyTelegraph> telegraphs)
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            if (telegraphs != null && telegraphs.Count > 0)
            {
                return;
            }

            foreach (var entry in entries)
            {
                if (entry?.Unit == null || entry.Skill == null)
                {
                    continue;
                }

                if (entry.Skill.slotKind == SkillSlotKind.BasicAttack)
                {
                    continue;
                }

                entry.Unit.GainPrep(1);
                Debug.Log(
                    $"[Prep] {entry.Unit.DisplayName} +1 @ beat {beatIndex} ({entry.Skill.displayName}) → {entry.Unit.Prep}/{CombatUnit.PrepCap}");

                if (Cover.TryCharge(1))
                {
                    Debug.Log(
                        $"[Cover] +1 @ beat {beatIndex} ({entry.Skill.displayName}) → {Cover.Gauge}/{CoverConstants.GaugeCap}");
                }
            }
        }

        private void ResolvePlayerAttacksAtBeat(int beatIndex, IReadOnlyList<AgendaEntry> entries,
            IReadOnlyList<EnemyTelegraph> telegraphs)
        {
            var players = entries
                .Where(e => e.Unit != null && e.Skill != null && !e.Skill.IsGuard)
                .OrderBy(e => e.Unit.ActionPriority)
                .ToList();

            var enemyBeat = telegraphs.Count > 0 ? telegraphs[0].BeatIndex : beatIndex;
            foreach (var entry in players)
            {
                if (IsEncounterOver)
                {
                    break;
                }

                var timing = Cover.RemapPlayerTiming(
                    BeatTimingResolver.Resolve(entry.BeatIndex, enemyBeat));
                ResolvePlayerAttack(entry, timing);
            }
        }

        private void ResolveEnemyTelegraphAtBeat(EnemyTelegraph telegraph, int beatIndex)
        {
            if (telegraph?.Unit == null || telegraph.Skill == null || !telegraph.Unit.IsAlive)
            {
                return;
            }

            if (CombatCounterResolver.IsTelegraphFullyCountered(telegraph, Timeline))
            {
                Debug.Log(
                    $"[Counter] Cancelled {telegraph.Unit.DisplayName} @ beat {beatIndex} ({telegraph.NoteTier}, need {telegraph.HitsRequired})");
                return;
            }

            var target = CombatTargetPicker.PickEnemyAttackTargetForBeat(Grid, Timeline, beatIndex);
            if (target == null)
            {
                return;
            }

            var damageResult = DamageCalculator.Calculate(
                telegraph.Unit.Stats,
                target.Stats,
                telegraph.Skill.skillTier,
                telegraph.Unit.Stats.StrengthType,
                BeatTiming.OnBeat,
                HarmonyElementResolver.GetRelation(telegraph.Unit.Stats.Element, target.Stats.Element));

            var finalDamage = damageResult.FinalDamage;
            var blockTiming = BlockBarriers.TryGetBlockTiming(beatIndex, Timeline);
            if (blockTiming.HasValue)
            {
                var timing = Cover.RemapGuardTiming(blockTiming.Value);
                var reduction = timing.GetDamageReduction();
                var before = finalDamage;
                finalDamage *= 1f - reduction;
                Debug.Log(
                    $"[Block] {timing} @ beat {beatIndex} → dmg {before:F0} → {finalDamage:F0} (-{reduction * 100f:F0}%)");
            }

            target.TakeDamage(finalDamage, damageResult.IsCritical);
            Debug.Log(
                $"[Enemy] {telegraph.Unit.DisplayName} hits {target.DisplayName} for {finalDamage:F0} @ beat {beatIndex}");
        }

        private bool TryEndEncounterIfDecided()
        {
            if (IsEncounterOver)
            {
                return true;
            }

            var outcome = CheckOutcome();
            if (outcome != CombatPhase.Victory && outcome != CombatPhase.Defeat)
            {
                return false;
            }

            Timeline.SetPhase(outcome);
            Cover.Reset();
            OnEncounterEnded?.Invoke();
            Debug.Log(outcome == CombatPhase.Victory ? "[Combat] Victory — all enemies defeated!" : "[Combat] Defeat!");
            return true;
        }

        private void ResolveAnyRemainingBeats()
        {
            for (var beat = 0; beat < TimelineConstants.TotalBeats; beat++)
            {
                if (!_resolvedBeats.Contains(beat))
                {
                    ResolveBeatAtScan(beat);
                }
            }

            TryEndEncounterIfDecided();
        }

        private void ResolvePlayerAttack(AgendaEntry entry, BeatTiming timing)
        {
            var target = PickTarget(entry);
            var ctx = new CombatContext
            {
                Grid = Grid,
                Timeline = Timeline,
                Source = entry.Unit,
                Target = target,
                Skill = entry.Skill,
                BeatTiming = timing,
                IsEmpowered = entry.IsEmpowered,
                Entry = entry,
                CoverOutgoingMultiplier = Cover.OutgoingDamageMultiplier
            };

            var command = new SkillActionCommand(entry.Skill);
            if (command.CanExecute(ctx))
            {
                command.Execute(ctx);
                Debug.Log(
                    $"[Beat] {entry.Unit.DisplayName} {entry.Skill.displayName} @ beat {entry.BeatIndex} (prio {entry.Unit.ActionPriority:F0}) → {timing}" +
                    (entry.IsEmpowered ? " [empowered]" : string.Empty));
            }
        }

        private CombatUnit PickTarget(AgendaEntry entry)
        {
            var source = entry.Unit;
            var skill = entry.Skill;
            if (skill.targetType == SkillTargetType.Self)
            {
                return source;
            }

            if (skill.targetType == SkillTargetType.SingleAlly)
            {
                return Grid.GetAllies(source.Side).FirstOrDefault(u => u.IsAlive);
            }

            if (skill.targetType == SkillTargetType.SingleEnemy && source.Side == GridSide.Player)
            {
                var counterTarget = CombatCounterResolver.ResolvePlayerCounterTarget(entry, Timeline);
                if (counterTarget != null)
                {
                    return counterTarget;
                }

                return CombatTargetPicker.PickPlayerAttackTarget(Grid);
            }

            var opponents = Grid.GetOpponents(source.Side).Where(u => u.IsAlive).ToList();
            if (opponents.Count == 0)
            {
                return null;
            }

            return opponents[UnityEngine.Random.Range(0, opponents.Count)];
        }

        private void ResolveUpkeep()
        {
            if (TryEndEncounterIfDecided())
            {
                return;
            }

            BeginPlanningRound();
        }

        private CombatPhase CheckOutcome()
        {
            var playersAlive = Grid.PlayerUnits.Any(u => u.IsAlive);
            var enemiesAlive = Grid.EnemyUnits.Any(u => u.IsAlive);

            if (!enemiesAlive)
            {
                return CombatPhase.Victory;
            }

            if (!playersAlive)
            {
                return CombatPhase.Defeat;
            }

            return CombatPhase.Planning;
        }
    }
}

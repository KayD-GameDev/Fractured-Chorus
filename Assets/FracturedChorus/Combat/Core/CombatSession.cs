using System;
using System.Collections.Generic;
using System.Linq;
using FracturedChorus.Combat.Actions;
using FracturedChorus.Combat.AI;
using FracturedChorus.Combat.Block;
using FracturedChorus.Combat.Cover;
using FracturedChorus.Combat.Damage;
using FracturedChorus.Combat.Difficulty;
using FracturedChorus.Combat.Formation;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using FracturedChorus.Meta;
using FracturedChorus.RunMap;
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
        public event Action<int, BlockTiming> OnBlockResolved;
        public event Action<EnemyStrikeReport> OnEnemyStrikeResolved;
        public event Action<PlayerSkillResolvedReport> OnPlayerSkillResolved;
        public event Action<int> OnBeforeResolveBeat;

        private SimpleEnemyAI _enemyAi;
        private readonly HashSet<int> _resolvedBeats = new();
        private readonly HashSet<string> _bulwarkGuardChargeGranted = new();
        private int _roundSegmentIndex;
        private int _lastScanBeat = -1;
        public PhaseAvTracker PhaseAv { get; } = new();
        public BlockBarrierTracker BlockBarriers { get; } = new();
        public CoverRuntime Cover { get; } = new();
        /// <summary>0-based execute segment index (each segment = 1 × 19-beat phase).</summary>
        public int RoundSegmentIndex => _roundSegmentIndex;

        /// <summary>True while the scan is advancing; false whenever a planning window is open.</summary>
        public bool IsTimelineRunning { get; private set; }

        /// <summary>True during the one-shot full-music intro before the first Planning window.</summary>
        public bool IsCombatIntroActive { get; private set; }

        private bool _combatIntroCompleted;
        private readonly HashSet<int> _plannedTelegraphPhases = new();
        private CombatUnit _presentationPlayer;
        private CombatUnit _presentationEnemy;

        /// <summary>
        /// The single gate for player agency: repositioning units and assigning skills are both
        /// allowed exactly when the timeline is parked in Planning (not during intro).
        /// </summary>
        public bool IsPlanningWindowOpen =>
            Phase == CombatPhase.Planning
            && !IsTimelineRunning
            && !IsCombatIntroActive
            && !IsEncounterOver;

        /// <summary>Cover button gate — open during any planning window.</summary>
        public bool AllowCoverActivate { get; set; } = true;

        /// <summary>
        /// Lock incoming/outgoing resolve to the 1v1 pair currently on stage.
        /// Cleared after <see cref="ResolveBeatAtScan"/>.
        /// </summary>
        public void SetPresentationResolvePair(CombatUnit player, CombatUnit enemy)
        {
            _presentationPlayer = player;
            _presentationEnemy = enemy;
        }

        public void ClearPresentationResolvePair()
        {
            _presentationPlayer = null;
            _presentationEnemy = null;
        }

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
                unit.OnDied += HandleUnitDied;
            }

            BeginPlanningRound();
        }

        /// <summary>
        /// Fill boss notes for the current phase + lookahead window.
        /// </summary>
        public void PrepareTelegraphsForCurrentSegment()
        {
            if (Grid.EnemyUnits.All(u => !u.IsAlive))
            {
                return;
            }

            EnsureTelegraphLookahead(TimelineConstants.RoundPhaseCount * _roundSegmentIndex);
        }

        public void OnTimelineScanBeat(int beatIndex)
        {
            _lastScanBeat = beatIndex;
        }

        private void HandleUnitDied(CombatUnit unit)
        {
            if (unit == null || Timeline == null || IsEncounterOver)
            {
                return;
            }

            if (unit.Side == GridSide.Player)
            {
                RemoveAgendaForDeadUnit(unit);
                return;
            }

            if (unit.Side == GridSide.Enemy)
            {
                RemoveTelegraphsForDeadUnit(unit);
                OnTelegraphsPlanned?.Invoke(GetDeathPhaseIndex());
            }
        }

        private void RemoveAgendaForDeadUnit(CombatUnit unit)
        {
            if (unit == null || Timeline == null)
            {
                return;
            }

            var entries = Timeline.Agenda
                .Where(a => a != null && a.Unit == unit && a.Skill != null)
                .ToList();
            if (entries.Count == 0)
            {
                return;
            }

            if (Phase == CombatPhase.Planning)
            {
                foreach (var entry in entries)
                {
                    RevertPlanningUtilityEffects(entry);
                }
            }

            var removed = Timeline.RemoveAgendaEntriesForUnit(unit);
            if (removed > 0)
            {
                Debug.Log(
                    $"[Combat] Removed {removed} timeline skill(s) for dead unit {unit.DisplayName}");
                OnTelegraphsPlanned?.Invoke(GetDeathPhaseIndex());
            }
        }

        /// <summary>Strip dead unit telegraphs from death phase through lookahead horizon.</summary>
        private void RemoveTelegraphsForDeadUnit(CombatUnit unit)
        {
            var deathPhase = GetDeathPhaseIndex();
            var lastPhase = System.Math.Min(
                TimelineConstants.PhaseCount - 1,
                deathPhase + TimelineConstants.TelegraphLookaheadPhases - 1);
            TimelineConstants.GetPhaseBeatRange(deathPhase, out var fromBeat, out _);
            TimelineConstants.GetPhaseBeatRange(lastPhase, out var lastStart, out var lastCount);
            var beatCount = lastStart + lastCount - fromBeat;
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

        /// <summary>
        /// Fill boss notes for the current phase window only
        /// (phase N ⇒ plan N..N+<see cref="TimelineConstants.TelegraphLookaheadPhases"/>-1;
        /// matches the visible UI window N / N+1 / N+2).
        /// Already-planned phases are skipped so Delay-pushed notes are not wiped.
        /// </summary>
        private void EnsureTelegraphLookahead(int currentPhaseIndex)
        {
            var lastExclusive = System.Math.Min(
                TimelineConstants.PhaseCount,
                currentPhaseIndex + TimelineConstants.TelegraphLookaheadPhases);

            for (var phase = currentPhaseIndex; phase < lastExclusive; phase++)
            {
                if (!_plannedTelegraphPhases.Add(phase))
                {
                    continue;
                }

                TimelineConstants.GetPhaseBeatRange(phase, out _, out var count);
                if (count > 0)
                {
                    _enemyAi.PlanTelegraphsForPhase(phase, Grid, Timeline);
                }
            }

            OnTelegraphsPlanned?.Invoke(currentPhaseIndex);
        }

        public bool TryAssignPlayerAction(CombatUnit unit, SkillDefinitionSO skill, int beatIndex = -1)
        {
            if (unit == null
                || unit.Side != GridSide.Player
                || !IsPlanningWindowOpen
                || skill == null)
            {
                return false;
            }

            var previousReduce = unit.PendingReduceS2;
            var armedFirstPlace = RunEventCombatMods.TryArmFirstPlaceReduceS2(unit);

            if (beatIndex < 0)
            {
                beatIndex = Timeline.FindFirstAssignableBeat(unit, skill);
            }

            if (beatIndex < 0 || !Timeline.CanAssignAction(unit, skill, beatIndex))
            {
                if (armedFirstPlace)
                {
                    unit.SetPendingReduceS2(previousReduce);
                }

                return false;
            }

            if (!Timeline.TryAssignAction(unit, skill, beatIndex))
            {
                if (armedFirstPlace)
                {
                    unit.SetPendingReduceS2(previousReduce);
                }

                return false;
            }

            if (armedFirstPlace)
            {
                RunEventCombatMods.ConsumeFirstPlaceReduceS2();
            }

            var entry = Timeline.FindPlayerEntry(unit, beatIndex);
            if (entry != null)
            {
                entry.StandingAfterOverride = SkillFootprintUtil.GetStandingAfter(skill, unit);
                if (RunEventCombatMods.TryConsumePlaceCounterPlus())
                {
                    entry.ActiveBeatsOverride = SkillFootprintUtil.GetActiveBeats(skill) + 1;
                }

                if (unit.PendingReduceS2 > 0)
                {
                    unit.SetPendingReduceS2(0);
                }

                ApplyPlanningUtilityEffects(entry);
            }

            Debug.Log($"[Combat] {unit.DisplayName} → {skill.displayName} @ beat {beatIndex}");
            return true;
        }

        /// <summary>
        /// Relocate drop onto another same-unit Active beat: partner moves to <paramref name="fromBeat"/>,
        /// dragged skill is assigned at <paramref name="toBeat"/>. Partner must occupy
        /// <paramref name="hoverBeat"/> as Active.
        /// </summary>
        public bool TrySwapRelocatePlayerAction(
            CombatUnit unit,
            SkillDefinitionSO skill,
            int fromBeat,
            int toBeat,
            int hoverBeat,
            out AgendaEntry partner)
        {
            partner = null;
            if (unit == null
                || unit.Side != GridSide.Player
                || !IsPlanningWindowOpen
                || skill == null
                || Timeline == null)
            {
                return false;
            }

            if (!Timeline.CanSwapRelocate(unit, skill, fromBeat, toBeat, hoverBeat))
            {
                return false;
            }

            if (!SkillFootprintUtil.TryGetEntryAtBeat(Timeline.Agenda, unit, hoverBeat, out partner, out var role)
                || partner?.Skill == null
                || role != FootprintBeatRole.Active)
            {
                partner = null;
                return false;
            }

            var partnerOldBeat = partner.BeatIndex;
            RevertPlanningUtilityEffects(partner);
            partner.BeatIndex = fromBeat;

            if (!TryAssignPlayerAction(unit, skill, toBeat))
            {
                partner.BeatIndex = partnerOldBeat;
                ApplyPlanningUtilityEffects(partner);
                partner = null;
                return false;
            }

            ApplyPlanningUtilityEffects(partner);
            Debug.Log(
                $"[Combat] Swap {unit.DisplayName} {skill.displayName} @{fromBeat} ↔ {partner.Skill.displayName} @{partnerOldBeat}");
            return true;
        }

        /// <summary>
        /// Remove <paramref name="victim"/> from the line, then try to place <paramref name="skill"/>.
        /// Victim is not restored if assign fails.
        /// </summary>
        public bool TryEatThenAssign(
            CombatUnit unit,
            SkillDefinitionSO skill,
            int placementBeat,
            AgendaEntry victim)
        {
            if (unit == null || skill == null || victim?.Skill == null || Timeline == null)
            {
                return false;
            }

            if (!TryRemovePlayerAction(victim.Unit, victim.BeatIndex))
            {
                return false;
            }

            return TryAssignPlayerAction(unit, skill, placementBeat);
        }

        /// <summary>
        /// Drop on empty beat → assign. Hover Active of another same-unit skill → swap if relocate
        /// and valid, else eat that skill then assign. Hover Standing → eat then assign.
        /// </summary>
        public bool TryResolveSkillDrop(
            CombatUnit unit,
            SkillDefinitionSO skill,
            int placementBeat,
            int hoverBeat,
            int relocateFromBeat,
            out AgendaEntry swapPartner,
            out SkillDefinitionSO displacedSkill,
            out int displacedBeat)
        {
            swapPartner = null;
            displacedSkill = null;
            displacedBeat = -1;

            if (unit == null || skill == null || Timeline == null)
            {
                return false;
            }

            if (!SkillFootprintUtil.TryGetEntryAtBeat(
                    Timeline.Agenda, unit, hoverBeat, out var victim, out var role)
                || victim?.Skill == null)
            {
                return TryAssignPlayerAction(unit, skill, placementBeat);
            }

            displacedSkill = victim.Skill;
            displacedBeat = victim.BeatIndex;

            if (role == FootprintBeatRole.Active
                && relocateFromBeat >= 0
                && TrySwapRelocatePlayerAction(
                    unit, skill, relocateFromBeat, placementBeat, hoverBeat, out swapPartner)
                && swapPartner != null)
            {
                return true;
            }

            swapPartner = null;
            return TryEatThenAssign(unit, skill, placementBeat, victim);
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
                var sEnd = entry.BeatIndex + SkillFootprintUtil.GetActiveBeats(skill, entry.Unit, entry) - 1;
                var moves = Timeline.DelayImpactTelegraphsAfterBeat(sEnd, CombatTimelineProfile.TotalBeats, delay);
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
            _bulwarkGuardChargeGranted.Clear();
            CombatCounterResolver.ClearPresentationMarkers();
            _roundSegmentIndex++;
            EnsureTelegraphLookahead(TimelineConstants.RoundPhaseCount * _roundSegmentIndex);
            _lastScanBeat = TimelineConstants.GetSegmentStartBeat(_roundSegmentIndex) - 1;
            IsTimelineRunning = false;
            PhaseAv.ResetForPlanning();
            Timeline.SetPhase(CombatPhase.Planning);
        }

        public void BeginPlanningRound()
        {
            _roundSegmentIndex = 0;
            _resolvedBeats.Clear();
            _bulwarkGuardChargeGranted.Clear();
            CombatCounterResolver.ClearPresentationMarkers();
            _lastScanBeat = -1;
            BlockBarriers.Clear();
            IsTimelineRunning = false;
            IsCombatIntroActive = !_combatIntroCompleted;
            AllowCoverActivate = _combatIntroCompleted;
            _plannedTelegraphPhases.Clear();
            Timeline.ResetForPlanning();
            PhaseAv.ResetForPlanning();
            Cover.Reset();
            Timeline.SetPhase(CombatPhase.Planning);
            TimelineConstants.EnemyNoteFloorBeat = 0;
            if (!IsCombatIntroActive)
            {
                PrepareTelegraphsForCurrentSegment();
            }
        }

        public void EndCombatIntro(int introEndBeat = 0)
        {
            IsCombatIntroActive = false;
            _combatIntroCompleted = true;
            AllowCoverActivate = true;
            TimelineConstants.EnemyNoteFloorBeat = 0;
            PrepareTelegraphsForCurrentSegment();
        }

        public void SetTimelineRunning(bool running)
        {
            IsTimelineRunning = running;
        }

        public void ConfirmPlanningAndExecute()
        {
            if (!IsPlanningWindowOpen)
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

        public bool IsBeatResolved(int beatIndex) => _resolvedBeats.Contains(beatIndex);

        public bool HasResolvableNoteAt(int beatIndex)
        {
            return TryGetResolvePairAtBeat(beatIndex, out _, out _);
        }

        public bool TryGetResolvePairAtBeat(int beatIndex, out CombatUnit player, out CombatUnit enemy)
        {
            player = null;
            enemy = null;
            if (Timeline == null || Grid == null || beatIndex < 0 || beatIndex >= CombatTimelineProfile.TotalBeats)
            {
                return false;
            }

            if (_resolvedBeats.Contains(beatIndex))
            {
                return false;
            }

            var telegraphs = Timeline.GetImpactTelegraphsAtBeat(beatIndex);
            var playerEntries = GetPlayerEntriesActiveAtBeat(beatIndex);
            if ((telegraphs == null || telegraphs.Count == 0) && (playerEntries == null || playerEntries.Count == 0))
            {
                return false;
            }

            if (playerEntries != null && playerEntries.Count > 0)
            {
                player = CombatCounterResolver.SelectCounterBody(
                    playerEntries.Select(e => e.Unit).Where(u => u != null && u.IsAlive).ToList());
                if (player == null)
                {
                    player = playerEntries[0].Unit;
                }
            }

            if (telegraphs != null && telegraphs.Count > 0)
            {
                for (var i = 0; i < telegraphs.Count; i++)
                {
                    var telegraph = telegraphs[i];
                    if (telegraph?.Unit != null && telegraph.Unit.IsAlive)
                    {
                        enemy = telegraph.Unit;
                        break;
                    }
                }
            }

            if (player == null && enemy != null)
            {
                player = CombatTargetPicker.PickEnemyAttackTargetForBeat(Grid, Timeline, beatIndex);
            }

            if (enemy == null && player != null && playerEntries != null && playerEntries.Count > 0)
            {
                enemy = PickTarget(playerEntries[0]);
                if (enemy != null && enemy.Side == GridSide.Player)
                {
                    enemy = Grid.EnemyUnits.FirstOrDefault(u => u != null && u.IsAlive);
                }
            }

            if (player == null)
            {
                player = Grid.PlayerUnits.FirstOrDefault(u => u != null && u.IsAlive);
            }

            if (enemy == null)
            {
                enemy = Grid.EnemyUnits.FirstOrDefault(u => u != null && u.IsAlive);
            }

            return player != null && enemy != null && player.IsAlive && enemy.IsAlive;
        }

        /// <summary>Gọi khi scan bar đi qua một beat — resolve player attack + enemy telegraph.</summary>
        public void ResolveBeatAtScan(int beatIndex)
        {
            if (Timeline == null || beatIndex < 0 || beatIndex >= CombatTimelineProfile.TotalBeats || IsEncounterOver)
            {
                return;
            }

            if (!_resolvedBeats.Add(beatIndex))
            {
                return;
            }

            var telegraphs = Timeline.GetImpactTelegraphsAtBeat(beatIndex);
            var playerEntries = GetPlayerEntriesActiveAtBeat(beatIndex);

            TickTimedShields(beatIndex);

            OnBeforeResolveBeat?.Invoke(beatIndex);

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

        private void TickTimedShields(int beatIndex)
        {
            if (Grid == null)
            {
                return;
            }

            foreach (var unit in Grid.PlayerUnits)
            {
                unit?.TickTimedShieldExpiry(beatIndex);
            }

            foreach (var unit in Grid.EnemyUnits)
            {
                unit?.TickTimedShieldExpiry(beatIndex);
            }
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
                .Where(e => e.Unit != null && e.Unit.IsAlive && e.Skill != null && !e.Skill.IsGuard)
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

            var target = ResolvePresentationEnemyAttackTarget(beatIndex);

            var swordCount = telegraph.HitsRequired > 0
                ? telegraph.HitsRequired
                : System.Math.Max(1, (int)telegraph.NoteTier);

            if (CombatCounterResolver.IsTelegraphFullyCountered(telegraph, Timeline))
            {
                Debug.Log(
                    $"[Counter] Cancelled {telegraph.Unit.DisplayName} @ beat {beatIndex} ({telegraph.NoteTier}, need {telegraph.HitsRequired})");
                OnEnemyStrikeResolved?.Invoke(
                    new EnemyStrikeReport(telegraph.Unit, target, wasCountered: true, beatIndex, swordCount, telegraph.Skill));
                return;
            }

            if (target == null)
            {
                return;
            }

            var positionalMod = Grid.GetCoverModifier(
                telegraph.Unit.GridPosition,
                target.GridPosition);
            var damageResult = DamageCalculator.Calculate(
                telegraph.Unit.Stats,
                target.Stats,
                telegraph.Skill.skillTier,
                telegraph.Skill.damageType,
                BeatTiming.OnBeat,
                HarmonyElementResolver.GetRelation(telegraph.Unit.Stats.Element, target.Stats.Element),
                positionalMod);

            var difficulty = GameMetaSession.HasSession
                ? GameMetaSession.Current.Difficulty
                : DifficultyRuntime.Cadence;
            var difficultyMult = DifficultyRuntime.Get(difficulty);
            var finalDamage = damageResult.FinalDamage * difficultyMult.EnemyDamage;
            var blockTiming = BlockBarriers.TryGetBlockTiming(beatIndex, Timeline);
            if (blockTiming.HasValue)
            {
                var timing = Cover.RemapGuardTiming(blockTiming.Value);
                timing = BlockBarriers.ConsumeGuardChargeRemap(timing);
                var reduction = timing.GetDamageReduction();
                if (timing is BlockTiming.Early or BlockTiming.Late)
                {
                    reduction = Mathf.Max(0f, reduction - difficultyMult.EarlyLateBlockPenalty);
                }

                var before = finalDamage;
                finalDamage *= 1f - reduction;
                Debug.Log(
                    $"[Block] {timing} @ beat {beatIndex} → dmg {before:F0} → {finalDamage:F0} (-{reduction * 100f:F0}%)" +
                    (BlockBarriers.GuardCharges > 0 ? $" · charges={BlockBarriers.GuardCharges}" : string.Empty));

                if (timing == BlockTiming.OnBeat && TryGrantBulwarkGuardCharge(beatIndex))
                {
                    BlockBarriers.AddGuardCharge(1);
                    Debug.Log($"[Block] GuardCharge +1 from Bulwark @ beat {beatIndex} (total {BlockBarriers.GuardCharges})");
                }

                OnBlockResolved?.Invoke(beatIndex, timing);
            }

            target.TakeDamage(
                RunEventCombatMods.ModifyIncoming(target.Side, finalDamage),
                damageResult.IsCritical);
            ApplyColumnSlamIfNeeded(target, finalDamage * 0.45f, damageResult.IsCritical);
            TryFormationDisrupt();
            Debug.Log(
                $"[Enemy] {telegraph.Unit.DisplayName} hits {target.DisplayName} for {finalDamage:F0} @ beat {beatIndex}" +
                (Mathf.Approximately(positionalMod, 1f) ? string.Empty : $" pos×={positionalMod:F2}"));

            OnEnemyStrikeResolved?.Invoke(
                new EnemyStrikeReport(telegraph.Unit, target, wasCountered: false, beatIndex, swordCount, telegraph.Skill));
        }

        private void ApplyColumnSlamIfNeeded(CombatUnit primaryTarget, float splashDamage, bool isCritical)
        {
            var profile = BossFormationRuntime.Active;
            if (profile == null || profile.columnSlamColumn < 0 || Grid == null)
            {
                return;
            }

            foreach (var unit in Grid.PlayerUnits)
            {
                if (unit == null || !unit.IsAlive || unit == primaryTarget)
                {
                    continue;
                }

                if (unit.GridPosition.Column != profile.columnSlamColumn)
                {
                    continue;
                }

                unit.TakeDamage(RunEventCombatMods.ModifyIncoming(unit.Side, splashDamage), isCritical);
            }
        }

        private void TryFormationDisrupt()
        {
            var profile = BossFormationRuntime.Active;
            if (profile == null || profile.formationDisrupt == FormationDisruptKind.None || Grid == null)
            {
                return;
            }

            if (profile.formationDisrupt != FormationDisruptKind.ForceSwapAdjacent)
            {
                return;
            }

            if (UnityEngine.Random.value > 0.2f)
            {
                return;
            }

            var alive = Grid.PlayerUnits.Where(u => u != null && u.IsAlive).OrderBy(u => u.GridPosition.Column).ToList();
            if (alive.Count < 2)
            {
                return;
            }

            var a = alive[0];
            var b = alive[1];
            if (Grid.TrySwapUnits(a, b.GridPosition))
            {
                Debug.Log($"[Formation] Disrupt swap {a.DisplayName} ↔ {b.DisplayName}");
            }
        }

        private bool TryGrantBulwarkGuardCharge(int beatIndex)
        {
            if (Timeline == null)
            {
                return false;
            }

            foreach (var entry in Timeline.Agenda)
            {
                if (entry?.Unit == null ||
                    entry.Skill == null ||
                    string.IsNullOrEmpty(entry.EntryId) ||
                    entry.Unit.Side != GridSide.Player ||
                    !entry.IsEmpowered ||
                    !entry.Skill.empowerGuardChargeOnPerfect ||
                    _bulwarkGuardChargeGranted.Contains(entry.EntryId))
                {
                    continue;
                }

                foreach (var info in SkillFootprintUtil.EnumerateFootprintBeats(entry.Skill, entry.BeatIndex))
                {
                    if (info.Role != FootprintBeatRole.Active || info.BeatIndex != beatIndex)
                    {
                        continue;
                    }

                    _bulwarkGuardChargeGranted.Add(entry.EntryId);
                    return true;
                }
            }

            return false;
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
            for (var beat = 0; beat < CombatTimelineProfile.TotalBeats; beat++)
            {
                if (!_resolvedBeats.Contains(beat))
                {
                    ResolveBeatAtScan(beat);
                }
            }

            TryEndEncounterIfDecided();
        }

        public void ApplyPlayerSkillDamagePulse(int beatIndex, int pulseIndex, int pulseCount)
        {
            if (Timeline == null || pulseCount < 1 || pulseIndex < 0 || IsEncounterOver)
            {
                return;
            }

            var telegraphs = Timeline.GetImpactTelegraphsAtBeat(beatIndex);
            var entries = GetPlayerEntriesActiveAtBeat(beatIndex);
            var enemyBeat = telegraphs != null && telegraphs.Count > 0 ? telegraphs[0].BeatIndex : beatIndex;
            foreach (var entry in entries)
            {
                if (entry?.Unit == null || entry.Skill == null || !entry.Unit.IsAlive)
                {
                    continue;
                }

                if (entry.EffectPayloadApplied && pulseIndex > 0 && entry.PendingHitDamage <= 0f)
                {
                    continue;
                }

                var target = PickTarget(entry);
                var timing = Cover.RemapPlayerTiming(
                    BeatTimingResolver.Resolve(entry.BeatIndex, enemyBeat));
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

                if (pulseIndex == 0 || entry.PendingHitDamage < 0f)
                {
                    entry.PendingHitDamage = SkillActionCommand.ComputeOutgoingDamage(ctx, target, out _);
                }

                var hitsLeft = pulseCount - pulseIndex;
                var leftover = Mathf.Max(0f, entry.PendingHitDamage);
                var amount = hitsLeft <= 1 ? leftover : leftover / hitsLeft;
                entry.PendingHitDamage = leftover - amount;
                if (target != null && amount > 0f)
                {
                    var crit = false;
                    SkillActionCommand.ComputeOutgoingDamage(ctx, target, out crit);
                    target.TakeDamage(amount, crit);
                }

                if (pulseIndex >= pulseCount - 1)
                {
                    entry.EffectPayloadApplied = true;
                    entry.PendingHitDamage = 0f;
                }
            }
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
                OnPlayerSkillResolved?.Invoke(
                    new PlayerSkillResolvedReport(entry.Unit, target, entry.Skill, entry.BeatIndex));
            }
        }

        private CombatUnit ResolvePresentationEnemyAttackTarget(int beatIndex)
        {
            if (_presentationPlayer != null && _presentationPlayer.IsAlive)
            {
                return _presentationPlayer;
            }

            return CombatTargetPicker.PickEnemyAttackTargetForBeat(Grid, Timeline, beatIndex);
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
                if (_presentationEnemy != null && _presentationEnemy.IsAlive)
                {
                    return _presentationEnemy;
                }

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

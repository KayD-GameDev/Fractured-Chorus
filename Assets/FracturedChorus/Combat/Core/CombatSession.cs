using System;
using System.Collections.Generic;
using System.Linq;
using FracturedChorus.Combat.Actions;
using FracturedChorus.Combat.AI;
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
        private enum ScanActionKind
        {
            PlayerAttack,
            EnemyAttack
        }

        private struct ScanAction
        {
            public ScanActionKind Kind;
            public AgendaEntry Entry;
            public EnemyTelegraph Telegraph;
            public float Priority;
        }

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
        private int _lastTelegraphPlanTriggerBeat = -1;
        public PhaseAvTracker PhaseAv { get; } = new();

        /// <summary>True only before Execute — player may drag units onto grid cells.</summary>
        public bool AllowPlayerReposition { get; private set; } = true;

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
            }

            BeginPlanningRound();
        }

        public void OnTimelineScanBeat(int beatIndex)
        {
            if (!IsEncounterOver)
            {
                TryPlanTelegraphsForScanBeat(beatIndex);
            }

            PhaseAv.SyncToTimelinePhase(beatIndex);
        }

        private void TryPlanTelegraphsForScanBeat(int beatIndex)
        {
            if (Grid.EnemyUnits.All(u => !u.IsAlive))
            {
                return;
            }

            if (!TimelineConstants.IsFirstBeatOfPhase(beatIndex))
            {
                return;
            }

            if (beatIndex == _lastTelegraphPlanTriggerBeat)
            {
                return;
            }

            var phaseIndex = TimelineConstants.GetPhaseIndex(beatIndex);
            _enemyAi.PlanTelegraphsForPhase(phaseIndex, Grid, Timeline);
            _lastTelegraphPlanTriggerBeat = beatIndex;
            OnTelegraphsPlanned?.Invoke(phaseIndex);
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

            if (beatIndex < 0 || !Timeline.CanAssignAction(unit, beatIndex))
            {
                return false;
            }

            var cost = skill.GetAvCost();
            if (!PhaseAv.CanAfford(cost))
            {
                return false;
            }

            if (!Timeline.TryAssignAction(unit, skill, beatIndex))
            {
                return false;
            }

            PhaseAv.RecordSpend(cost);
            Debug.Log(
                $"[Phase AV] {unit.DisplayName} → {skill.displayName} (-{cost}) | còn {PhaseAv.Remaining}/{PhaseAv.CurrentBudget} (priority {unit.ActionPriority:F0})");
            return true;
        }

        public void BeginPlanningRound()
        {
            _resolvedBeats.Clear();
            _lastTelegraphPlanTriggerBeat = -1;
            AllowPlayerReposition = true;
            Timeline.ResetForPlanning();
            PhaseAv.ResetForPlanning();
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
            ResolveAnyRemainingBeats();

            if (IsEncounterOver)
            {
                return;
            }

            Timeline.SetPhase(CombatPhase.Upkeep);
            ResolveUpkeep();
        }

        /// <summary>
        /// Gọi khi scan bar đi qua một beat — resolve guard, player attack, enemy telegraph ngay tại beat đó.
        /// </summary>
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

            var telegraph = Timeline.GetTelegraphAtBeat(beatIndex);
            var entries = Timeline.GetEntriesAtBeat(beatIndex);

            var guardTiming = ResolveGuardTimingOnBeat(beatIndex, entries);
            if (guardTiming.HasValue)
            {
                Debug.Log($"[Guard] Active @ beat {beatIndex} → {guardTiming.Value}");
            }

            var actions = BuildScanActions(beatIndex, entries, telegraph);
            actions.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            foreach (var action in actions)
            {
                if (IsEncounterOver)
                {
                    break;
                }

                if (action.Kind == ScanActionKind.PlayerAttack)
                {
                    var enemyBeat = telegraph?.BeatIndex ?? beatIndex;
                    var timing = BeatTimingResolver.Resolve(action.Entry.BeatIndex, enemyBeat);
                    ResolvePlayerAttack(action.Entry, timing);
                }
                else
                {
                    ResolveEnemyTelegraph(action.Telegraph, guardTiming);
                }
            }

            TryEndEncounterIfDecided();
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
        }

        private static BeatTiming? ResolveGuardTimingOnBeat(int beat, IReadOnlyList<AgendaEntry> entries)
        {
            BeatTiming? best = null;

            foreach (var entry in entries)
            {
                if (entry.Unit.Side != GridSide.Player || entry.Skill == null || !entry.Skill.IsGuard)
                {
                    continue;
                }

                var timing = BeatTimingResolver.Resolve(entry.BeatIndex, beat);
                if (!best.HasValue || timing.GetMultiplier() > best.Value.GetMultiplier())
                {
                    best = timing;
                }
            }

            return best;
        }

        private static List<ScanAction> BuildScanActions(int beat, IReadOnlyList<AgendaEntry> entries,
            EnemyTelegraph telegraph)
        {
            var actions = new List<ScanAction>();

            foreach (var entry in entries)
            {
                if (entry.Unit.Side != GridSide.Player || entry.Skill == null || entry.Skill.IsGuard)
                {
                    continue;
                }

                actions.Add(new ScanAction
                {
                    Kind = ScanActionKind.PlayerAttack,
                    Entry = entry,
                    Priority = entry.Unit.ActionPriority
                });
            }

            if (telegraph != null && telegraph.Unit != null && telegraph.Unit.IsAlive && telegraph.Skill != null)
            {
                actions.Add(new ScanAction
                {
                    Kind = ScanActionKind.EnemyAttack,
                    Telegraph = telegraph,
                    Priority = telegraph.Unit.ActionPriority
                });
            }

            return actions;
        }

        private void ResolvePlayerAttack(AgendaEntry entry, BeatTiming timing)
        {
            var target = PickTarget(entry.Unit, entry.Skill);
            var ctx = new CombatContext
            {
                Grid = Grid,
                Timeline = Timeline,
                Source = entry.Unit,
                Target = target,
                Skill = entry.Skill,
                BeatTiming = timing
            };

            var command = new SkillActionCommand(entry.Skill);
            if (command.CanExecute(ctx))
            {
                command.Execute(ctx);
                Debug.Log(
                    $"[Beat] {entry.Unit.DisplayName} {entry.Skill.displayName} @ beat {entry.BeatIndex} (prio {entry.Unit.ActionPriority:F0}) → {timing}");
            }
        }

        private void ResolveEnemyTelegraph(EnemyTelegraph telegraph, BeatTiming? guardTimingOnBeat)
        {
            if (telegraph.Unit == null || telegraph.Skill == null || !telegraph.Unit.IsAlive)
            {
                return;
            }

            var target = CombatTargetPicker.PickEnemyAttackTarget(Grid);
            if (target == null)
            {
                return;
            }

            var rawCtx = new CombatContext
            {
                Grid = Grid,
                Timeline = Timeline,
                Source = telegraph.Unit,
                Target = target,
                Skill = telegraph.Skill,
                BeatTiming = BeatTiming.OnBeat
            };

            var command = new SkillActionCommand(telegraph.Skill);
            if (!command.CanExecute(rawCtx))
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
            if (guardTimingOnBeat.HasValue)
            {
                finalDamage = BeatTimingResolver.ApplyGuardReduction(finalDamage, guardTimingOnBeat.Value);
                Debug.Log(
                    $"[Guard] Block @ beat {telegraph.BeatIndex} ({guardTimingOnBeat.Value}) → dmg {damageResult.FinalDamage:F0} → {finalDamage:F0}");
            }

            target.TakeDamage(finalDamage);
            Debug.Log(
                $"[Enemy] {telegraph.Unit.DisplayName} (prio {telegraph.Unit.ActionPriority:F0}) hits {target.DisplayName} for {finalDamage:F0} @ beat {telegraph.BeatIndex} " +
                $"(rand={damageResult.SkillRandomRoll:F2}×str={telegraph.Unit.Stats.Strength:F0} raw={damageResult.RawDamage:F0} " +
                $"crit={damageResult.IsCritical} mult={damageResult.CritDamageMultiplier:F2})");
        }

        private CombatUnit PickTarget(CombatUnit source, SkillDefinitionSO skill)
        {
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

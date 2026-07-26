using System;
using System.Collections.Generic;
using System.Linq;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.Combat.Timeline
{
    public enum BossNoteTier
    {
        Red = 1,
        Blue = 2,
        Purple = 3
    }

    [Serializable]
    public class EnemyTelegraph
    {
        public CombatUnit Unit;
        public SkillDefinitionSO Skill;
        public int BeatIndex;
        /// <summary>True = S1 wind-up beat; damage resolves only on impact telegraphs.</summary>
        public bool IsWindupOnly;
        public BossNoteTier NoteTier;
        public int HitsRequired = 1;
    }

    public class BeatTimelineEngine
    {
        public const int BeatCount = TimelineConstants.TotalBeats;

        private readonly List<AgendaEntry> _agenda = new();
        private readonly List<EnemyTelegraph> _telegraphs = new();
        private int _scanBeatIndex;

        public IReadOnlyList<AgendaEntry> Agenda => _agenda;
        public IReadOnlyList<EnemyTelegraph> Telegraphs => _telegraphs;
        public int ScanBeatIndex => _scanBeatIndex;
        public int ScrollOffset { get; private set; }
        /// <summary>Beat đầu tiên hợp lệ để đặt S1 (phía sau thanh đỏ). Cập nhật mỗi khi vào planning.</summary>
        public int PlanningHorizonBeat { get; set; }
        public int VisibleWindowSize { get; set; } = TimelineConstants.DefaultVisibleBeatHint;
        public CombatPhase Phase { get; private set; } = CombatPhase.Planning;

        public event Action<CombatPhase> OnPhaseChanged;
        public event Action<AgendaEntry> OnActionAssigned;
        public event Action<int> OnScanAdvanced;
        public event Action OnAgendaCleared;
        public event Action OnTelegraphsChanged;
        public event Action<int, int, int> OnTelegraphMoved;
        public event Action<IReadOnlyList<TelegraphBeatMove>> OnTelegraphsDelayedBatch;

        public void SetPhase(CombatPhase phase)
        {
            Phase = phase;
            OnPhaseChanged?.Invoke(phase);
        }

        public void ClearTelegraphs()
        {
            _telegraphs.Clear();
            OnTelegraphsChanged?.Invoke();
        }

        public void ClearTelegraphsInRange(int startBeat, int beatCount)
        {
            var endBeat = startBeat + beatCount;
            _telegraphs.RemoveAll(t => t.BeatIndex >= startBeat && t.BeatIndex < endBeat);
            _agenda.RemoveAll(a => a.Unit != null && a.Unit.Side == GridSide.Enemy
                && a.BeatIndex >= startBeat && a.BeatIndex < endBeat);
            OnTelegraphsChanged?.Invoke();
        }

        public void RemoveTelegraphsForUnitInRange(CombatUnit unit, int startBeat, int beatCount)
        {
            if (unit == null || beatCount <= 0)
            {
                return;
            }

            var endBeat = startBeat + beatCount;
            var removed = _telegraphs.RemoveAll(t => t.Unit == unit && t.BeatIndex >= startBeat && t.BeatIndex < endBeat);
            if (removed > 0)
            {
                OnTelegraphsChanged?.Invoke();
            }
        }

        public void AddTelegraph(CombatUnit unit, SkillDefinitionSO skill, int beatIndex, bool isWindupOnly = false,
            BossNoteTier noteTier = BossNoteTier.Red, int hitsRequired = 1)
        {
            if (unit == null || skill == null || beatIndex < 0 || beatIndex >= BeatCount)
            {
                return;
            }

            _telegraphs.Add(new EnemyTelegraph
            {
                Unit = unit,
                Skill = skill,
                BeatIndex = beatIndex,
                IsWindupOnly = isWindupOnly,
                NoteTier = noteTier,
                HitsRequired = hitsRequired
            });
            OnTelegraphsChanged?.Invoke();
        }

        public List<EnemyTelegraph> GetImpactTelegraphsAtBeat(int beatIndex)
        {
            return _telegraphs.Where(t => t.BeatIndex == beatIndex && !t.IsWindupOnly).ToList();
        }

        public EnemyTelegraph GetTelegraphAtBeat(int beatIndex)
        {
            return _telegraphs.FirstOrDefault(t => t.BeatIndex == beatIndex);
        }

        /// <summary>Impact telegraph at beat (excludes S1 wind-up markers).</summary>
        public EnemyTelegraph GetImpactTelegraphAtBeat(int beatIndex)
        {
            return _telegraphs.FirstOrDefault(t => t.BeatIndex == beatIndex && !t.IsWindupOnly);
        }

        public int DelayImpactTelegraphsOnBeats(IEnumerable<int> activeBeats, int delayBeats)
        {
            if (activeBeats == null || delayBeats <= 0)
            {
                return 0;
            }

            var beatSet = new HashSet<int>(activeBeats);
            var toMove = _telegraphs
                .Where(t => t != null && !t.IsWindupOnly && beatSet.Contains(t.BeatIndex))
                .OrderByDescending(t => t.BeatIndex)
                .ToList();
            if (toMove.Count == 0)
            {
                return 0;
            }

            var moves = new List<TelegraphBeatMove>(toMove.Count);
            foreach (var telegraph in toMove)
            {
                var from = telegraph.BeatIndex;
                var to = Mathf.Min(BeatCount - 1, from + delayBeats);
                if (to == from)
                {
                    continue;
                }

                telegraph.BeatIndex = to;
                moves.Add(new TelegraphBeatMove(telegraph, from, to));
                OnTelegraphMoved?.Invoke(from, to, delayBeats);
            }

            if (moves.Count > 0)
            {
                OnTelegraphsDelayedBatch?.Invoke(moves);
                OnTelegraphsChanged?.Invoke();
            }

            return moves.Count;
        }

        public IReadOnlyList<TelegraphBeatMove> DelayAllImpactTelegraphsInBeatRange(
            int startBeat,
            int beatCount,
            int delayBeats)
        {
            if (delayBeats <= 0 || beatCount <= 0)
            {
                return Array.Empty<TelegraphBeatMove>();
            }

            var endBeat = startBeat + beatCount;
            return DelayImpactTelegraphsInRange(startBeat, endBeat, delayBeats, afterBeatExclusive: startBeat - 1);
        }

        /// <summary>
        /// Delay impact notes with BeatIndex &gt; afterBeat and BeatIndex &lt; phaseEndExclusive.
        /// Notes on/under Anchor S (≤ afterBeat) stay put.
        /// </summary>
        public IReadOnlyList<TelegraphBeatMove> DelayImpactTelegraphsAfterBeat(
            int afterBeat,
            int phaseEndExclusive,
            int delayBeats)
        {
            if (delayBeats <= 0)
            {
                return Array.Empty<TelegraphBeatMove>();
            }

            return DelayImpactTelegraphsInRange(afterBeat + 1, phaseEndExclusive, delayBeats, afterBeat);
        }

        private IReadOnlyList<TelegraphBeatMove> DelayImpactTelegraphsInRange(
            int rangeStartInclusive,
            int rangeEndExclusive,
            int delayBeats,
            int afterBeatExclusive)
        {
            var toMove = _telegraphs
                .Where(t => t != null
                    && !t.IsWindupOnly
                    && t.BeatIndex > afterBeatExclusive
                    && t.BeatIndex >= rangeStartInclusive
                    && t.BeatIndex < rangeEndExclusive)
                .OrderByDescending(t => t.BeatIndex)
                .ToList();
            if (toMove.Count == 0)
            {
                return Array.Empty<TelegraphBeatMove>();
            }

            var moves = new List<TelegraphBeatMove>(toMove.Count);
            foreach (var telegraph in toMove)
            {
                var from = telegraph.BeatIndex;
                var to = Mathf.Min(BeatCount - 1, from + delayBeats);
                if (to == from)
                {
                    continue;
                }

                telegraph.BeatIndex = to;
                moves.Add(new TelegraphBeatMove(telegraph, from, to));
            }

            if (moves.Count > 0)
            {
                OnTelegraphsDelayedBatch?.Invoke(moves);
                OnTelegraphsChanged?.Invoke();
            }

            return moves;
        }

        public void RevertTelegraphMoves(IReadOnlyList<TelegraphBeatMove> moves)
        {
            if (moves == null || moves.Count == 0)
            {
                return;
            }

            foreach (var move in moves)
            {
                if (move.Telegraph == null || !_telegraphs.Contains(move.Telegraph))
                {
                    continue;
                }

                move.Telegraph.BeatIndex = move.FromBeat;
            }

            OnTelegraphsChanged?.Invoke();
        }

        private int ResolveDelayDestination(EnemyTelegraph self, int fromBeat, int delayBeats)
        {
            var dest = Mathf.Min(BeatCount - 1, fromBeat + delayBeats);
            while (dest < BeatCount - 1 && IsImpactOccupied(dest, self))
            {
                dest++;
            }

            if (IsImpactOccupied(dest, self))
            {
                return fromBeat;
            }

            return dest;
        }

        private bool IsImpactOccupied(int beatIndex, EnemyTelegraph self)
        {
            foreach (var t in _telegraphs)
            {
                if (t == null || t == self || t.IsWindupOnly)
                {
                    continue;
                }

                if (t.BeatIndex == beatIndex)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryAssignAction(CombatUnit unit, SkillDefinitionSO skill, int beatIndex)
        {
            if (Phase != CombatPhase.Planning || unit == null || skill == null)
            {
                return false;
            }

            if (!CanAssignAction(unit, skill, beatIndex))
            {
                return false;
            }

            var entry = new AgendaEntry(unit, skill, beatIndex);
            _agenda.Add(entry);
            OnActionAssigned?.Invoke(entry);
            return true;
        }

        public bool CanAssignAction(CombatUnit unit, SkillDefinitionSO skill, int beatIndex)
        {
            if (Phase != CombatPhase.Planning || unit == null || skill == null)
            {
                return false;
            }

            if (beatIndex < 0 || beatIndex >= BeatCount)
            {
                return false;
            }

            if (!SkillFootprintUtil.CanPlace(_agenda, unit, skill, beatIndex, PlanningHorizonBeat))
            {
                return false;
            }

            return !CombatCounterResolver.ActiveOverlapsFullyCounteredNote(this, skill, beatIndex, unit);
        }

        public void ClearPlayerAgenda()
        {
            _agenda.RemoveAll(a => a.Unit != null && a.Unit.Side == GridSide.Player);
        }

        public bool TryRemovePlayerAction(CombatUnit unit, int placementBeat)
        {
            if (Phase != CombatPhase.Planning || unit == null || unit.Side != GridSide.Player)
            {
                return false;
            }

            var removed = _agenda.RemoveAll(a =>
                a.Unit == unit && a.BeatIndex == placementBeat && a.Unit.Side == GridSide.Player);
            return removed > 0;
        }

        public AgendaEntry FindPlayerEntry(CombatUnit unit, int placementBeat)
        {
            return _agenda.FirstOrDefault(a => a.Unit == unit && a.BeatIndex == placementBeat);
        }

        public int FindNextEmptyBeat(int startIndex = 0)
        {
            for (var i = startIndex; i < BeatCount; i++)
            {
                if (_agenda.All(a => a.BeatIndex != i))
                {
                    return i;
                }
            }

            return -1;
        }

        public int FindFirstAssignableBeat(CombatUnit unit, SkillDefinitionSO skill, int startIndex = 0)
        {
            if (unit == null || skill == null)
            {
                return -1;
            }

            for (var i = startIndex; i < BeatCount; i++)
            {
                if (CanAssignAction(unit, skill, i))
                {
                    return i;
                }
            }

            return -1;
        }

        public void ClearAgenda()
        {
            _agenda.Clear();
            _scanBeatIndex = 0;
            ScrollOffset = 0;
            OnAgendaCleared?.Invoke();
        }

        public List<AgendaEntry> GetEntriesAtBeat(int beatIndex)
        {
            return _agenda.Where(a => a.BeatIndex == beatIndex).ToList();
        }

        public bool AdvanceScan()
        {
            if (_scanBeatIndex >= BeatCount)
            {
                return false;
            }

            ScrollOffset = Mathf.Max(0, _scanBeatIndex - VisibleWindowSize + 1);
            OnScanAdvanced?.Invoke(_scanBeatIndex);
            _scanBeatIndex++;
            return _scanBeatIndex <= BeatCount;
        }

        public void ResetScan()
        {
            _scanBeatIndex = 0;
            ScrollOffset = 0;
        }

        public bool IsScanComplete()
        {
            return _scanBeatIndex >= BeatCount;
        }

        public void ResetForPlanning()
        {
            ResetScan();
            ClearAgenda();
            ClearTelegraphs();
            SetPhase(CombatPhase.Planning);
        }
    }
}

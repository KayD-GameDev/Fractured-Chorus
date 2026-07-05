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
    [Serializable]
    public class EnemyTelegraph
    {
        public CombatUnit Unit;
        public SkillDefinitionSO Skill;
        public int BeatIndex;
        /// <summary>True = S1 wind-up beat; damage resolves only on impact telegraphs.</summary>
        public bool IsWindupOnly;
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
        public int VisibleWindowSize { get; set; } = TimelineConstants.DefaultVisibleBeatHint;
        public CombatPhase Phase { get; private set; } = CombatPhase.Planning;

        public event Action<CombatPhase> OnPhaseChanged;
        public event Action<AgendaEntry> OnActionAssigned;
        public event Action<int> OnScanAdvanced;
        public event Action OnAgendaCleared;
        public event Action OnTelegraphsChanged;

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

        public void AddTelegraph(CombatUnit unit, SkillDefinitionSO skill, int beatIndex, bool isWindupOnly = false)
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
                IsWindupOnly = isWindupOnly
            });
            OnTelegraphsChanged?.Invoke();
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

            return SkillFootprintUtil.CanPlace(_agenda, unit, skill, beatIndex);
        }

        public void ClearPlayerAgenda()
        {
            _agenda.RemoveAll(a => a.Unit != null && a.Unit.Side == GridSide.Player);
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

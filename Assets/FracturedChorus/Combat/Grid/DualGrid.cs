using System.Collections.Generic;
using FracturedChorus.Combat.Units;
using UnityEngine;

namespace FracturedChorus.Combat.Grid
{
    public class DualGrid
    {
        public const int Rows = 3;
        public const int Columns = 3;
        public const int MaxPlayerUnits = 5;
        public const int MaxEnemyUnits = 9;

        private readonly Dictionary<GridPosition, GridCell> _cells = new();
        private readonly List<CombatUnit> _playerUnits = new();
        private readonly List<CombatUnit> _enemyUnits = new();

        public IReadOnlyList<CombatUnit> PlayerUnits => _playerUnits;
        public IReadOnlyList<CombatUnit> EnemyUnits => _enemyUnits;

        public DualGrid()
        {
            foreach (GridSide side in System.Enum.GetValues(typeof(GridSide)))
            {
                for (var row = 0; row < Rows; row++)
                {
                    for (var col = 0; col < Columns; col++)
                    {
                        var pos = new GridPosition(side, row, col);
                        _cells[pos] = new GridCell(pos);
                    }
                }
            }
        }

        public GridCell GetCell(GridPosition position)
        {
            return _cells.TryGetValue(position, out var cell) ? cell : null;
        }

        public bool IsOccupied(GridPosition position)
        {
            var cell = GetCell(position);
            return cell != null && cell.IsOccupied;
        }

        public bool TryPlaceUnit(CombatUnit unit, GridPosition position)
        {
            if (!position.IsValid())
            {
                return false;
            }

            var maxUnits = position.Side == GridSide.Player ? MaxPlayerUnits : MaxEnemyUnits;
            var list = position.Side == GridSide.Player ? _playerUnits : _enemyUnits;
            if (list.Count >= maxUnits)
            {
                return false;
            }

            var cell = GetCell(position);
            if (cell == null || !cell.TryPlace(unit))
            {
                return false;
            }

            unit.SetGridPosition(position);
            list.Add(unit);
            return true;
        }

        public float GetCoverModifier(GridPosition attackerPos, GridPosition targetPos)
        {
            // Stub: full cover logic deferred to later phase.
            return 1f;
        }

        public Vector3 GetWorldPosition(GridPosition position, float cellWidth, float cellHeight, float sideGap)
        {
            var xBase = position.Side == GridSide.Player ? -sideGap : sideGap;
            var colOffset = position.Side == GridSide.Player
                ? (2 - position.Column) * cellWidth
                : position.Column * cellWidth;
            var x = xBase + colOffset;
            var y = (1 - position.Row) * cellHeight;
            var depthStagger = position.Row * 0.1f + position.Column * 0.05f;
            return new Vector3(x, y + depthStagger, depthStagger);
        }

        public IEnumerable<CombatUnit> GetAllUnits()
        {
            foreach (var unit in _playerUnits)
            {
                yield return unit;
            }

            foreach (var unit in _enemyUnits)
            {
                yield return unit;
            }
        }

        public IEnumerable<CombatUnit> GetOpponents(GridSide side)
        {
            return side == GridSide.Player ? _enemyUnits : _playerUnits;
        }

        public IEnumerable<CombatUnit> GetAllies(GridSide side)
        {
            return side == GridSide.Player ? _playerUnits : _enemyUnits;
        }
    }
}

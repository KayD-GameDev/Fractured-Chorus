using System.Collections.Generic;
using FracturedChorus.Combat.Units;
using UnityEngine;

namespace FracturedChorus.Combat.Grid
{
    public class DualGrid
    {
        public const int Rows = 2;
        public const int Columns = 3;
        public const int MaxPlayerUnits = 4;
        public const int MaxEnemyUnits = 6;

        private readonly Dictionary<GridPosition, GridCell> _cells = new();
        private readonly List<CombatUnit> _playerUnits = new();
        private readonly List<CombatUnit> _enemyUnits = new();
        private int _nextPlayerBarOrder;

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
            if (position.Side == GridSide.Player)
            {
                unit.PartyBarOrder = _nextPlayerBarOrder++;
            }

            return true;
        }

        public CombatUnit GetOccupant(GridPosition position)
        {
            return GetCell(position)?.Occupant;
        }

        public bool TryReleaseUnit(CombatUnit unit)
        {
            if (unit == null)
            {
                return false;
            }

            var cell = GetCell(unit.GridPosition);
            if (cell == null || cell.Occupant != unit)
            {
                return false;
            }

            cell.Clear();
            var list = unit.Side == GridSide.Player ? _playerUnits : _enemyUnits;
            return list.Remove(unit);
        }

        public bool TrySwapUnits(CombatUnit unit, GridPosition targetPosition)
        {
            if (unit == null || !targetPosition.IsValid() || targetPosition.Side != unit.Side)
            {
                return false;
            }

            var sourcePosition = unit.GridPosition;
            if (sourcePosition.Equals(targetPosition))
            {
                return true;
            }

            var sourceCell = GetCell(sourcePosition);
            var targetCell = GetCell(targetPosition);
            if (sourceCell == null || targetCell == null || !targetCell.IsOccupied)
            {
                return false;
            }

            var other = targetCell.Occupant;
            if (other == null || other == unit || other.Side != unit.Side)
            {
                return false;
            }

            sourceCell.Clear();
            targetCell.Clear();
            targetCell.TryPlace(unit);
            sourceCell.TryPlace(other);
            unit.SetGridPosition(targetPosition);
            other.SetGridPosition(sourcePosition);
            return true;
        }

        public bool TryMoveUnit(CombatUnit unit, GridPosition newPosition)
        {
            if (unit == null || !newPosition.IsValid() || newPosition.Side != unit.Side)
            {
                return false;
            }

            var oldPosition = unit.GridPosition;
            if (oldPosition.Equals(newPosition))
            {
                return true;
            }

            var targetCell = GetCell(newPosition);
            if (targetCell == null || targetCell.IsOccupied)
            {
                return false;
            }

            var oldCell = GetCell(oldPosition);
            oldCell?.Clear();
            targetCell.TryPlace(unit);
            unit.SetGridPosition(newPosition);
            return true;
        }

        public float GetCoverModifier(GridPosition attackerPos, GridPosition targetPos) =>
            PositionalModifiers.GetDamageModifier(attackerPos, targetPos);

        public float GetHealPotencyModifier(GridPosition healerPos) =>
            PositionalModifiers.GetHealPotencyModifier(healerPos);

        public Vector3 GetWorldPosition(GridPosition position, float cellWidth, float cellHeight, float sideGap)
        {
            return HexBoardLayout.GetWorldPosition(position, sideGap);
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

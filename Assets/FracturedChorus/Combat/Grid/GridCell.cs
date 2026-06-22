using FracturedChorus.Combat.Units;

namespace FracturedChorus.Combat.Grid
{
    public class GridCell
    {
        public GridPosition Position { get; }
        public CombatUnit Occupant { get; private set; }

        public GridCell(GridPosition position)
        {
            Position = position;
        }

        public bool IsOccupied => Occupant != null && Occupant.IsAlive;

        public bool TryPlace(CombatUnit unit)
        {
            if (IsOccupied)
            {
                return false;
            }

            Occupant = unit;
            return true;
        }

        public void Clear()
        {
            Occupant = null;
        }
    }
}

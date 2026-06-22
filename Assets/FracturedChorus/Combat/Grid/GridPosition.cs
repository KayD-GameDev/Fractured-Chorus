using System;

namespace FracturedChorus.Combat.Grid
{
    [Serializable]
    public struct GridPosition
    {
        public GridSide Side;
        public int Row;
        public int Column;

        public GridPosition(GridSide side, int row, int column)
        {
            Side = side;
            Row = row;
            Column = column;
        }

        public bool IsValid()
        {
            return Row is >= 0 and <= 2 && Column is >= 0 and <= 2;
        }

        public override string ToString()
        {
            return $"{Side} R{Row} C{Column}";
        }
    }
}

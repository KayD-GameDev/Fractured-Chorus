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
            return Row >= 0 && Row < DualGrid.Rows && Column >= 0 && Column < DualGrid.Columns;
        }

        public override string ToString()
        {
            return HoneycombIndex.Format(Side, Row, Column);
        }
    }
}

using UnityEngine;

namespace FracturedChorus.Combat.Grid
{
    /// <summary>
    /// Honeycomb rows/columns numbered 1–3 (design Board margin.drawio).
    /// Internal index = display − 1.
    /// Player columns: 1 = right/front → center. Enemy columns: 1 = left/front → center (mirrored board).
    /// </summary>
    public static class HoneycombIndex
    {
        public const int Unplaced = -1;

        public static int ToIndex(int displayOneBased) => displayOneBased - 1;

        public static int ToDisplay(int index) => index + 1;

        public static bool IsValidIndex(int index) => index is >= 0 and <= 2;

        public static string Format(GridSide side, int rowIndex, int columnIndex)
        {
            return $"{side} H{ToDisplay(rowIndex)} C{ToDisplay(columnIndex)}";
        }

        public static GridPosition FromDisplay(GridSide side, int displayRow, int displayColumn)
        {
            return new GridPosition(side, ToIndex(displayRow), ToIndex(displayColumn));
        }
    }
}

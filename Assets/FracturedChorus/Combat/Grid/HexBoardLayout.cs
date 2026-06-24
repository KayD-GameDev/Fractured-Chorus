using UnityEngine;

namespace FracturedChorus.Combat.Grid
{
    /// <summary>
    /// Honeycomb margin — vị trí khớp board player (xanh) trong scene Unity.
    /// Enemy = lật trục X so với player (cùng khoảng cách honeycomb).
    /// </summary>
    public static class HexBoardLayout
    {
        public const float DefaultSideGap = 3.5f;
        public const float HexRadius = 0.55f;
        /// <summary>Vertical pitch between honeycomb rows (R0↔R1 = R1↔R2).</summary>
        public const float RowVerticalPitch = 1.35f;

        private static readonly float[] RowY =
        {
            -RowVerticalPitch,
            0f,
            RowVerticalPitch
        };

        private static readonly Vector2[,] PlayerLocalOffsets =
        {
            { new Vector2(0.7f, RowY[0]), new Vector2(-1.07f, RowY[0]), new Vector2(-2.83f, RowY[0]) },
            { new Vector2(1.4f, RowY[1]), new Vector2(-0.37f, RowY[1]), new Vector2(-2.13f, RowY[1]) },
            { new Vector2(0.7f, RowY[2]), new Vector2(-1.07f, RowY[2]), new Vector2(-2.83f, RowY[2]) }
        };

        public static Vector3 GetWorldPosition(GridPosition position, float sideGap = DefaultSideGap)
        {
            return GetWorldPosition(position.Side, position.Row, position.Column, sideGap);
        }

        public static Vector3 GetWorldPosition(GridSide side, int row, int column, float sideGap = DefaultSideGap)
        {
            var local = PlayerLocalOffsets[row, column];
            if (side == GridSide.Enemy)
            {
                local.x = -local.x;
            }

            var anchorX = side == GridSide.Player ? -sideGap : sideGap;
            var depth = row * 0.1f + column * 0.05f;
            return new Vector3(anchorX + local.x, local.y, depth);
        }

        public static Vector2[] GetHexOutlineVertices(float radius)
        {
            var points = new Vector2[6];
            for (var i = 0; i < 6; i++)
            {
                var angleDeg = 30f + i * 60f;
                var rad = angleDeg * Mathf.Deg2Rad;
                points[i] = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
            }

            return points;
        }
    }
}

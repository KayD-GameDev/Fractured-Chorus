using FracturedChorus.Combat.Grid;

namespace FracturedChorus.Combat.Bootstrap
{
    /// <summary>
    /// Default spawn cells (display row 2, columns 1–3) for demo party.
    /// </summary>
    public static class DefaultPartyFormation
    {
        public static bool TryGetStartupCell(string unitKey, GridSide side, out GridPosition position)
        {
            position = default;

            if (side == GridSide.Player)
            {
                switch (unitKey)
                {
                    case "tank":
                        position = HoneycombIndex.FromDisplay(GridSide.Player, 2, 1);
                        return true;
                    case "ren":
                        position = HoneycombIndex.FromDisplay(GridSide.Player, 2, 2);
                        return true;
                    case "mage":
                        position = HoneycombIndex.FromDisplay(GridSide.Player, 2, 3);
                        return true;
                }
            }
            else if (side == GridSide.Enemy)
            {
                switch (unitKey)
                {
                    case "grunt_left":
                        position = HoneycombIndex.FromDisplay(GridSide.Enemy, 2, 1);
                        return true;
                    case "grunt_right":
                        position = HoneycombIndex.FromDisplay(GridSide.Enemy, 2, 3);
                        return true;
                }
            }

            return false;
        }
    }
}

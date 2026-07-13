using FracturedChorus.Combat.Timeline;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public static class BossNoteTierColors
    {
        public static Color ForTier(BossNoteTier tier)
        {
            return tier switch
            {
                BossNoteTier.Purple => new Color(0.55f, 0.2f, 0.75f, 1f),
                BossNoteTier.Blue => new Color(0.25f, 0.85f, 0.35f, 1f),
                _ => new Color(0.85f, 0.2f, 0.2f, 1f)
            };
        }
    }
}

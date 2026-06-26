using UnityEngine;

namespace FracturedChorus.UI
{
    public static class PartyStatusBarLayout
    {
        public const float DefaultCardWidth = 78f;
        public const float DefaultCardHeight = 98f;
        public const float DefaultCardSpacing = 8f;
        public const int DefaultPlayerCardCount = 3;

        public static Vector2 DefaultRootSize(int cardCount = DefaultPlayerCardCount)
        {
            var count = Mathf.Max(1, cardCount);
            return new Vector2(
                DefaultCardWidth * count + DefaultCardSpacing * (count - 1) + 16f,
                DefaultCardHeight);
        }
    }
}

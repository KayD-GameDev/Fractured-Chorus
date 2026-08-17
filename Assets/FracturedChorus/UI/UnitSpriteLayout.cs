using System;
using UnityEngine;

namespace FracturedChorus.UI
{
    /// <summary>
    /// One authored combat sprite: art + scale + feet pin on the honeycomb.
    /// </summary>
    [Serializable]
    public struct UnitSpriteLayout
    {
        [Tooltip("Tab name in Unit Sprite Simulator. Empty = V0, V1, …")]
        public string displayName;

        public Sprite sprite;
        public Vector3 localScale;
        public Vector3 feetAnchorLocal;

        public string TabLabel(int index)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName.Trim();
            }

            return "V" + index;
        }

        public bool HasSprite => sprite != null;

        public bool HasData =>
            sprite != null
            || !Mathf.Approximately(localScale.sqrMagnitude, 0f)
            || !Mathf.Approximately(feetAnchorLocal.sqrMagnitude, 0f);

        public bool Matches(Sprite other)
        {
            if (sprite == null || other == null)
            {
                return false;
            }

            if (sprite == other)
            {
                return true;
            }

            return string.Equals(sprite.name, other.name, StringComparison.Ordinal);
        }
    }
}

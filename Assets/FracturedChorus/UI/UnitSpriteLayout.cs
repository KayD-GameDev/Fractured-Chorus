using System;
using UnityEngine;

namespace FracturedChorus.UI
{
    public enum UnitSpriteKind
    {
        StillSprite = 0,
        AnimationClip = 1
    }

    public enum UnitSpriteApplyMode
    {
        Auto = 0,
        PreferStill = 1,
        PreferClip = 2
    }

    public enum UnitCombatVisualState
    {
        None = 0,
        Idle = 1,
        Moving = 2,
        Skill = 3,
        Guard = 4,
        Counter = 5,
        Hurt = 6,
        Death = 7
    }

    /// <summary>
    /// One authored combat sprite: still or clip, linked to a UnitView pose, plus scale and feet pin.
    /// </summary>
    [Serializable]
    public struct UnitSpriteLayout
    {
        [Tooltip("Tab name in Unit Sprite Simulator. Empty = linked state or V0, V1, …")]
        public string displayName;

        public UnitSpriteKind kind;
        public Sprite sprite;
        public AnimationClip animationClip;
        public UnitCombatVisualState linkedState;
        public Vector3 localScale;
        public Vector3 feetAnchorLocal;

        public bool UsesStillArt => kind == UnitSpriteKind.StillSprite && sprite != null;

        public bool HasStillSprite => sprite != null;

        public string ClipStateName => animationClip != null ? animationClip.name : null;

        public bool ShouldApplyStill(UnitSpriteApplyMode mode)
        {
            if (sprite == null)
            {
                return false;
            }

            return mode switch
            {
                UnitSpriteApplyMode.PreferStill => true,
                UnitSpriteApplyMode.PreferClip => false,
                _ => UsesStillArt
            };
        }

        public string TabLabel(int index)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName.Trim();
            }

            if (linkedState != UnitCombatVisualState.None)
            {
                return linkedState.ToString();
            }

            return "V" + index;
        }

        public bool HasSprite => sprite != null;

        public bool HasData =>
            sprite != null
            || animationClip != null
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

        public static UnitCombatVisualState InferLinkedState(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return UnitCombatVisualState.None;
            }

            var name = displayName.Trim();
            if (ContainsToken(name, "idle"))
            {
                return UnitCombatVisualState.Idle;
            }

            if (ContainsToken(name, "moving") || name.Equals("move", StringComparison.OrdinalIgnoreCase))
            {
                return UnitCombatVisualState.Moving;
            }

            if (ContainsToken(name, "skill") || ContainsToken(name, "attack") || ContainsToken(name, "cast"))
            {
                return UnitCombatVisualState.Skill;
            }

            if (ContainsToken(name, "guard"))
            {
                return UnitCombatVisualState.Guard;
            }

            if (ContainsToken(name, "counter"))
            {
                return UnitCombatVisualState.Counter;
            }

            if (ContainsToken(name, "hurt") || ContainsToken(name, "be countered"))
            {
                return UnitCombatVisualState.Hurt;
            }

            if (ContainsToken(name, "death") || ContainsToken(name, "dead") || name.Equals("die", StringComparison.OrdinalIgnoreCase))
            {
                return UnitCombatVisualState.Death;
            }

            return UnitCombatVisualState.None;
        }

        private static bool ContainsToken(string name, string token)
        {
            return name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

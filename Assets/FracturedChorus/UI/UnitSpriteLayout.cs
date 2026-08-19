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
        Death = 7,
        NormalHit = 8,
        SkillHit = 9,
        UltHit = 10
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
        public Vector2 colliderSize;
        public Vector2 colliderOffset;

        public bool UsesStillArt => kind == UnitSpriteKind.StillSprite && sprite != null;

        public bool HasCollider => colliderSize.x > 0.001f && colliderSize.y > 0.001f;

        public bool HasFeetAnchor => !Mathf.Approximately(feetAnchorLocal.sqrMagnitude, 0f);

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

            var name = NormalizeName(displayName);
            if (ContainsToken(name, "idle"))
            {
                return UnitCombatVisualState.Idle;
            }

            if (ContainsToken(name, "moving") || name.Equals("move", StringComparison.OrdinalIgnoreCase))
            {
                return UnitCombatVisualState.Moving;
            }

            if (ContainsToken(name, "norhit")
                || ContainsToken(name, "normal hit")
                || ContainsToken(name, "normalhit")
                || ContainsSkillIndex(name, 1))
            {
                return UnitCombatVisualState.NormalHit;
            }

            if (ContainsToken(name, "ultimate")
                || ContainsToken(name, "ult hit")
                || ContainsToken(name, "ulthit")
                || ContainsSkillIndex(name, 3))
            {
                return UnitCombatVisualState.UltHit;
            }

            if (ContainsSkillIndex(name, 2)
                || ContainsToken(name, "charlott skill")
                || ContainsToken(name, "skill hit")
                || ContainsToken(name, "skillhit"))
            {
                return UnitCombatVisualState.SkillHit;
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

        private static string NormalizeName(string name)
        {
            return name.Trim().Replace('_', ' ').Replace('-', ' ');
        }

        private static bool ContainsSkillIndex(string name, int index)
        {
            return ContainsToken(name, "skill " + index) || ContainsToken(name, "skill" + index);
        }

        private static bool ContainsToken(string name, string token)
        {
            return name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

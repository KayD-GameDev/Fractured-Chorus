using System;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public static class SkillVfxShotView
    {
        public static bool HasRenderableProfile(SkillVfxProfileSO profile) =>
            profile != null && profile.HasVisual;

        public static SkillVfxProfileSO ResolveProfile(SkillDefinitionSO skill, CombatUnit attacker = null)
        {
            if (HasRenderableProfile(skill != null ? skill.vfxProfile : null))
            {
                return skill.vfxProfile;
            }

            var skills = attacker != null ? attacker.Skills : null;
            if (skills == null)
            {
                return null;
            }

            for (var i = 0; i < skills.Length; i++)
            {
                var candidate = skills[i];
                if (HasRenderableProfile(candidate != null ? candidate.vfxProfile : null))
                {
                    return candidate.vfxProfile;
                }
            }

            return null;
        }

        public static BossSwordShotMode ResolveMode(SkillVfxProfileSO profile, bool countered)
        {
            if (!countered)
            {
                return BossSwordShotMode.Hit;
            }

            if (profile == null)
            {
                return BossSwordShotMode.Deflect;
            }

            return profile.counteredShotMode switch
            {
                SkillVfxCounterBehavior.Vanish => BossSwordShotMode.Vanish,
                SkillVfxCounterBehavior.Hit => BossSwordShotMode.Hit,
                _ => BossSwordShotMode.Deflect
            };
        }

        public static BossSwordShotSettings BuildSettings(
            SkillVfxProfileSO profile,
            Material additiveMaterial,
            Action onImpact)
        {
            if (profile == null)
            {
                return null;
            }

            return new BossSwordShotSettings
            {
                Sword = profile.projectileSprite,
                Impact = profile.hideImpactSprite ? null : profile.impactSprite,
                AdditiveMaterial = additiveMaterial,
                TravelSeconds = Mathf.Max(0.01f, profile.travelSeconds),
                SpawnHoldSeconds = Mathf.Max(0f, profile.spawnHoldSeconds),
                ImpactSeconds = Mathf.Max(0.01f, profile.impactSeconds),
                DeflectSeconds = Mathf.Max(0.01f, profile.deflectSeconds),
                SwordWorldLength = Mathf.Max(0.05f, profile.projectileWorldSize),
                ImpactWorldSize = Mathf.Max(0.05f, profile.impactWorldSize),
                DeflectTravel = Mathf.Max(0.1f, profile.deflectTravel),
                SpriteFacingOffsetDegrees = profile.kind is SkillVfxKind.SpellBurst
                    or SkillVfxKind.Hit
                    or SkillVfxKind.Buff
                    ? profile.projectileEuler.z
                    : profile.ResolveProjectileFacingDegrees(),
                ProjectileAdditive = profile.projectileAdditive,
                SortingOrder = profile.sortingOrder,
                OnImpact = onImpact
            };
        }

        public static void ResolveShotEnds(
            SkillVfxProfileSO profile,
            UnitView caster,
            UnitView target,
            int shotIndex,
            int shotCount,
            out Vector3 from,
            out Vector3 to)
        {
            from = SkillVfxAnchorResolver.ResolveOrigin(caster, profile);
            to = SkillVfxAnchorResolver.ResolveDestination(target, profile);

            if (profile != null && profile.PlaysAtCaster)
            {
                to = from;
            }
            else if (profile != null && profile.PlaysAtTarget)
            {
                from = to;
            }

            if (profile == null || shotCount <= 1 || profile.verticalSpread <= 0.0001f)
            {
                return;
            }

            var offsetY = (shotIndex - (shotCount - 1) * 0.5f) * profile.verticalSpread;
            from += new Vector3(0f, offsetY, 0f);
            to += new Vector3(0f, offsetY * 0.35f, 0f);
        }

        public static Component Spawn(
            SkillVfxProfileSO profile,
            Vector3 from,
            Vector3 to,
            BossSwordShotMode mode,
            Material additiveMaterial,
            Transform parent,
            Action onImpact)
        {
            var settings = BuildSettings(profile, additiveMaterial, onImpact);
            if (settings == null)
            {
                return null;
            }

            if (profile != null && !profile.Travels)
            {
                if (profile.kind == SkillVfxKind.Hit)
                {
                    settings.Sword = profile.projectileSprite;
                    settings.SwordWorldLength = Mathf.Max(0.05f, profile.projectileWorldSize);
                    settings.Impact = profile.ShowsImpactSprite ? profile.impactSprite : null;
                }

                if (settings.Sword == null)
                {
                    return null;
                }

                var world = profile.PlaysAtTarget ? to : from;
                return SkillVfxAttachView.Spawn(world, settings, parent);
            }

            if (settings.Sword == null)
            {
                return null;
            }

            return BossSwordShotView.Spawn(from, to, settings, mode, parent);
        }

        public static float EstimateVolleySeconds(SkillVfxProfileSO profile, BossSwordShotMode mode, int shotCount)
        {
            if (profile == null)
            {
                return 0.01f;
            }

            if (!profile.HasPattern || profile.PlaysAtCaster || profile.PlaysAtTarget)
            {
                return Mathf.Max(0.01f, profile.impactSeconds);
            }

            var hold = Mathf.Max(0f, profile.spawnHoldSeconds);
            var travel = Mathf.Max(0.01f, profile.travelSeconds);
            var impact = Mathf.Max(0.01f, profile.impactSeconds);
            var shot = mode switch
            {
                BossSwordShotMode.Deflect =>
                    hold + travel * 0.55f + impact + Mathf.Max(0.01f, profile.deflectSeconds),
                BossSwordShotMode.Vanish => hold + travel * 0.55f + impact * 2f,
                _ => hold + travel + impact
            };

            var extraGaps = Mathf.Max(0, shotCount - 1) * Mathf.Max(0f, profile.shotGapSeconds);
            return shot + extraGaps;
        }

        public static void FitSpriteToWorldSize(SpriteRenderer sr, float worldSize)
        {
            if (sr == null || sr.sprite == null)
            {
                return;
            }

            var native = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
            if (native < 0.001f)
            {
                return;
            }

            var parent = sr.transform.parent;
            var parentScale = 1f;
            if (parent != null)
            {
                parentScale = Mathf.Max(
                    Mathf.Abs(parent.lossyScale.x),
                    Mathf.Abs(parent.lossyScale.y));
            }

            if (parentScale < 0.0001f)
            {
                parentScale = 1f;
            }

            var local = Mathf.Max(0.05f, worldSize) / (native * parentScale);
            sr.transform.localScale = new Vector3(local, local, 1f);
        }
    }
}

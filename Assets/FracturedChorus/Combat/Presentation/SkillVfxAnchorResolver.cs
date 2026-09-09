using FracturedChorus.Data;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public static class SkillVfxAnchorResolver
    {
        public static Vector3 ResolveOrigin(UnitView caster, SkillVfxProfileSO profile = null)
        {
            var world = caster != null ? caster.ProjectileWorld : Vector3.zero;
            return world + (profile != null ? profile.fromOffset : Vector3.zero);
        }

        public static Vector3 ResolveDestination(UnitView target, SkillVfxProfileSO profile = null)
        {
            var world = target != null ? target.ReceiveDmgWorld : Vector3.zero;
            return world + (profile != null ? profile.toOffset : Vector3.zero);
        }

        public static Vector3 ResolveAnchorOrigin(UnitView caster)
        {
            return caster != null ? caster.ProjectileWorld : Vector3.zero;
        }

        public static Vector3 ResolveAnchorDestination(UnitView target)
        {
            return target != null ? target.ReceiveDmgWorld : Vector3.zero;
        }

        public static Vector3 Resolve(
            SkillVfxAnchor anchor,
            UnitView caster,
            UnitView target,
            Vector3 offset)
        {
            return ResolveWithoutOffset(anchor, caster, target) + offset;
        }

        public static Vector3 ResolveWithoutOffset(
            SkillVfxAnchor anchor,
            UnitView caster,
            UnitView target)
        {
            if (IsCaster(anchor))
            {
                return ResolveAnchorOrigin(caster);
            }

            return ResolveAnchorDestination(target);
        }

        public static bool IsCaster(SkillVfxAnchor anchor)
        {
            return anchor is SkillVfxAnchor.CasterHead
                or SkillVfxAnchor.CasterBody
                or SkillVfxAnchor.CasterAim
                or SkillVfxAnchor.CasterFeet;
        }
    }
}

using UnityEngine;
using UnityEngine.Serialization;

namespace FracturedChorus.Data
{
    public enum SkillVfxKind
    {
        Projectile = 0,
        Buff = 1,
        SpellBurst = 2,
        Hit = 3
    }

    public enum SkillVfxAnchor
    {
        CasterHead = 0,
        CasterBody = 1,
        CasterAim = 2,
        CasterFeet = 3,
        TargetHead = 4,
        TargetBody = 5,
        TargetAim = 6,
        TargetFeet = 7
    }

    public enum SkillVfxCounterBehavior
    {
        Hit = 0,
        Deflect = 1,
        Vanish = 2
    }

    [CreateAssetMenu(fileName = "SkillVfx", menuName = "Fractured Chorus/Skill VFX Profile")]
    public class SkillVfxProfileSO : ScriptableObject
    {
        [Header("Kind")]
        [FormerlySerializedAs("patternKind")]
        [Tooltip("Projectile: bay A→B. Buff: đứng tại A. SpellBurst: bung từ A tới B. Hit: đánh tại B.")]
        public SkillVfxKind kind = SkillVfxKind.Projectile;

        [Tooltip("Tắt = chỉ hiện sprite tại vị trí đã lưu, không chạy pattern.")]
        public bool disablePattern;

        [HideInInspector] public SkillVfxAnchor fromAnchor = SkillVfxAnchor.CasterHead;
        [HideInInspector] public SkillVfxAnchor toAnchor = SkillVfxAnchor.TargetBody;

        [Header("Saved spawn (relative to A Projectile / B ReceiveDmg)")]
        public Vector3 fromOffset;
        public Vector3 toOffset;
        [Tooltip("Rotation projectile đã save. Z = lệch so với hướng A→B (Projectile), hoặc góc tuyệt đối (Buff/Hit).")]
        public Vector3 projectileEuler;

        [Header("Sprites")]
        public Sprite projectileSprite;
        public Sprite impactSprite;
        [Tooltip("Ẩn ảnh impact. Hit vẫn chạy khi projectile tới B.")]
        public bool hideImpactSprite;
        [Tooltip("Tới B thì ẩn projectile (điều kiện contact).")]
        public bool hideProjectileOnArrive = true;
        [Tooltip("Projectile: tới B. Hit: mốc clip (0–1) khi hiện VFX tại ReceiveDmg.")]
        [Range(0.05f, 1f)] public float arriveAtBNormalized = 1f;

        [Header("Size")]
        [Min(0.05f)] public float projectileWorldSize = 1.9f;
        [Min(0.05f)] public float impactWorldSize = 1.7f;

        [Header("Timing")]
        [Min(0f)] public float spawnHoldSeconds;
        [Min(0.01f)] public float travelSeconds = 0.32f;
        [Min(0.01f)] public float impactSeconds = 0.18f;
        [Min(0.01f)] public float deflectSeconds = 0.22f;
        [Min(0.1f)] public float deflectTravel = 2.4f;

        [Header("Volley")]
        [Tooltip("0 = dùng SwordCount từ telegraph (1–3).")]
        [Min(0)] public int projectileCount;
        [Min(0f)] public float verticalSpread = 0.22f;
        [Min(0f)] public float shotGapSeconds = 0.08f;

        [Header("Facing / counter")]
        public float facingOffsetDegrees = 135f;
        public SkillVfxCounterBehavior counteredShotMode = SkillVfxCounterBehavior.Deflect;
        public bool projectileAdditive;
        public int sortingOrder = 42;

        public bool HasPattern => !disablePattern;

        public bool HasVisual => projectileSprite != null || (!hideImpactSprite && impactSprite != null);

        public bool HasProjectile => projectileSprite != null;

        public bool ShowsImpactSprite => !hideImpactSprite && impactSprite != null;

        public bool HasArrivedAtB(float travelT) =>
            travelT >= Mathf.Clamp01(arriveAtBNormalized);

        public float ResolveContactNormalized(float fallback)
        {
            if (kind != SkillVfxKind.Hit)
            {
                return Mathf.Clamp(fallback, 0.05f, 0.95f);
            }

            return Mathf.Clamp(arriveAtBNormalized, 0.05f, 0.95f);
        }

        public bool Travels => HasPattern && kind is SkillVfxKind.Projectile or SkillVfxKind.SpellBurst;

        public bool PlaysAtCaster => kind == SkillVfxKind.Buff;

        public bool PlaysAtTarget => kind == SkillVfxKind.Hit;

        public float ResolveProjectileFacingDegrees()
        {
            if (projectileEuler.sqrMagnitude > 0.0001f)
            {
                return projectileEuler.z;
            }

            return facingOffsetDegrees;
        }

        public Quaternion ResolveProjectileRotation(Vector3 travelDirection)
        {
            var facing = ResolveProjectileFacingDegrees();
            if (kind is SkillVfxKind.Buff or SkillVfxKind.Hit)
            {
                return Quaternion.Euler(projectileEuler.x, projectileEuler.y, facing);
            }

            var dir = travelDirection;
            dir.z = 0f;
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = Vector3.left;
            }

            dir.Normalize();
            var travel = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            return Quaternion.Euler(projectileEuler.x, projectileEuler.y, travel + facing);
        }

        public int ResolveShotCount(int telegraphCount)
        {
            if (projectileCount > 0)
            {
                return Mathf.Clamp(projectileCount, 1, 8);
            }

            return Mathf.Clamp(telegraphCount, 1, 3);
        }

        public float ContactDelaySeconds =>
            !HasPattern || PlaysAtCaster || PlaysAtTarget
                ? 0f
                : Mathf.Max(0f, spawnHoldSeconds) + Mathf.Max(0.01f, travelSeconds) * 0.55f;

        public void ApplySavedLayout(float worldSize, Vector3 savedFromOffset, Vector3 savedToOffset)
        {
            ApplySavedLayout(worldSize, impactWorldSize, savedFromOffset, savedToOffset, projectileEuler);
        }

        public void ApplySavedLayout(
            float worldSize,
            float impactSize,
            Vector3 savedFromOffset,
            Vector3 savedToOffset)
        {
            ApplySavedLayout(worldSize, impactSize, savedFromOffset, savedToOffset, projectileEuler);
        }

        public void ApplySavedLayout(
            float worldSize,
            float impactSize,
            Vector3 savedFromOffset,
            Vector3 savedToOffset,
            Vector3 savedProjectileEuler)
        {
            if (kind == SkillVfxKind.Hit)
            {
                impactWorldSize = Mathf.Max(0.05f, impactSize);
            }
            else
            {
                projectileWorldSize = Mathf.Max(0.05f, worldSize);
            }

            fromOffset = savedFromOffset;
            toOffset = savedToOffset;
            projectileEuler = savedProjectileEuler;
            facingOffsetDegrees = savedProjectileEuler.z;
        }

        public void EnsurePattern(SkillVfxKind nextKind)
        {
            kind = nextKind;
            disablePattern = false;
            ApplyDefaultPatternTiming();
        }

        public void ClearPattern()
        {
            disablePattern = true;
        }

        public void ApplyDefaultPatternTiming()
        {
            switch (kind)
            {
                case SkillVfxKind.Buff:
                    impactSeconds = Mathf.Max(0.12f, impactSeconds);
                    travelSeconds = Mathf.Max(0.01f, travelSeconds);
                    spawnHoldSeconds = 0f;
                    facingOffsetDegrees = 0f;
                    break;
                case SkillVfxKind.Hit:
                    impactSeconds = Mathf.Max(0.12f, impactSeconds);
                    travelSeconds = Mathf.Max(0.01f, travelSeconds);
                    spawnHoldSeconds = 0f;
                    facingOffsetDegrees = 0f;
                    break;
                case SkillVfxKind.SpellBurst:
                    spawnHoldSeconds = Mathf.Max(0.08f, spawnHoldSeconds);
                    travelSeconds = Mathf.Max(0.2f, travelSeconds);
                    facingOffsetDegrees = 0f;
                    break;
                default:
                    spawnHoldSeconds = Mathf.Max(0.04f, spawnHoldSeconds);
                    travelSeconds = Mathf.Max(0.2f, travelSeconds);
                    break;
            }
        }
    }
}

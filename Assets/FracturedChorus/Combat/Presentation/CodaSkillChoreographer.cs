using System;
using System.Collections;
using FracturedChorus.Audio;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public class CodaSkillChoreographer : MonoBehaviour
    {
        private const string ResourceRoot = "VFX/Combat/Coda/";

        [SerializeField] private Transform vfxParent;
        [SerializeField] private Material additiveMaterial;
        [SerializeField] private int sortingOrder = 45;

        [Header("Skill 1 — Star pulse")]
        [SerializeField] private float skill1StandoffX = 2.45f;
        [SerializeField] private float skill1LungeSpeed = 26f;
        [SerializeField] private float skill1LungeSeconds = 0.16f;
        [SerializeField] private float skill1RetreatSeconds = 0.2f;
        [SerializeField] [Range(0.1f, 0.9f)] private float skill1ImpactNormalized = 0.5f;
        [SerializeField] private Sprite starHitSprite;
        [SerializeField] private Sprite[] starDebrisSprites;
        [SerializeField] private float starHitWorldSize = 2.9f;
        [SerializeField] private float starHitSeconds = 0.26f;
        [SerializeField] private float starDebrisWorldSize = 0.45f;
        [SerializeField] private int starDebrisCount = 12;
        [SerializeField] private float starBurstSeconds = 0.62f;
        [SerializeField] private float skill1ContactHeight = 0.8f;

        [Header("Skill 2 — Pierce beam")]
        [SerializeField] private float skill2CastBackX = 0.55f;
        [SerializeField] private float skill2RetreatSeconds = 0.14f;
        [SerializeField] private float skill2AimHeight = 0.78f;
        [SerializeField] private float skill2AnimSampleRate = 24f;
        [SerializeField] private int skill2ChargeEndFrame = 16;
        [SerializeField] private int skill2BeamFireFrame = 17;
        [SerializeField] private float skill2BeamHoldSeconds = 0.3f;
        [SerializeField] private float skill2BeamFadeSeconds = 0.18f;
        [SerializeField] private float skill2BeamThickness = 2.65f;
        [SerializeField] private float skill2PiercePast = 28f;
        [SerializeField] private bool skill2PierceThroughMap = true;
        [SerializeField] private float skill2ChargeWorldSize = 1.85f;
        [SerializeField] private float skill2ImpactWorldSize = 2.9f;
        [SerializeField] private Sprite beamChargeSprite;
        [SerializeField] private Sprite beamSprite;
        [SerializeField] private Sprite beamImpactSprite;

        [Header("Skill 3 — Arc volley")]
        [SerializeField] private float skill3CastBackX = 0.35f;
        [SerializeField] private float skill3CastStepSeconds = 0.14f;
        [SerializeField] private float skill3RetreatSeconds = 0.22f;
        [SerializeField] private float skill3AimHeight = 0.8f;
        [SerializeField] private float skill3ChargeSeconds = 0.95f;
        [SerializeField] private float skill3ChargeWorldSize = 3.05f;
        [SerializeField] private int skill3BoltCount = 5;
        [SerializeField] private float skill3BoltWorldSize = 3.05f;
        [SerializeField] private float skill3FlightSeconds = 0.48f;
        [SerializeField] private float skill3StaggerSeconds = 0.07f;
        [SerializeField] private float skill3ArcSpreadY = 2.85f;
        [SerializeField] private float skill3ControlBulge = 2.55f;
        [SerializeField] private float skill3ImpactWorldSize = 4.2f;
        [SerializeField] private float skill3ImpactSeconds = 0.3f;
        [SerializeField] private float skill3FinaleImpactScale = 1.55f;
        [SerializeField] private float skill3FinaleImpactSeconds = 0.42f;
        [SerializeField] private float skill3AftermathHoldSeconds = 0.35f;
        [SerializeField] private float skill3BoltFacingOffsetDegrees = 180f;
        [SerializeField] private bool skill3InvertArcNormal;
        [SerializeField] private Sprite arcChargeSprite;
        [SerializeField] private Sprite arcBoltSprite;
        [SerializeField] private Sprite arcImpactSprite;

        private Material _runtimeAdditive;

        public bool Handles(SkillDefinitionSO skill, UnitView sourceView = null)
        {
            if (skill == null)
            {
                return false;
            }

            var slotOk = skill.slotKind is SkillSlotKind.BasicAttack
                         or SkillSlotKind.Skill
                         or SkillSlotKind.Ultimate;
            if (!slotOk)
            {
                return false;
            }

            if (IsCodaSkillId(skill.skillId))
            {
                return true;
            }

            return IsCodaUnit(sourceView != null ? sourceView.Unit : null, sourceView);
        }

        public IEnumerator PlaySkillRoutine(
            UnitView coda,
            UnitView boss,
            SkillDefinitionSO skill,
            bool returnHome = true,
            Action onImpact = null)
        {
            if (coda == null || boss == null || skill == null || !Handles(skill, coda))
            {
                yield break;
            }

            EnsureDefaults();
            switch (skill.slotKind)
            {
                case SkillSlotKind.Skill:
                    yield return PlayPierceBeam(coda, boss, skill, returnHome, onImpact);
                    break;
                case SkillSlotKind.Ultimate:
                    yield return PlayArcVolley(coda, boss, skill, returnHome, onImpact);
                    break;
                default:
                    yield return PlayStarPulse(coda, boss, skill, returnHome, onImpact);
                    break;
            }
        }

        private IEnumerator PlayStarPulse(
            UnitView coda,
            UnitView boss,
            SkillDefinitionSO skill,
            bool returnHome,
            Action onImpact)
        {
            var home = coda.transform.position;
            if (returnHome)
            {
                coda.CaptureAnchor();
            }

            var strikeFeet = ResolveStandoffFeet(coda, boss, skill1StandoffX);
            coda.PlayMovingLoop();
            yield return coda.MoveFeetToRoutine(
                strikeFeet,
                ResolveMoveSeconds(coda.FeetWorldPosition, strikeFeet, skill1LungeSpeed, skill1LungeSeconds));

            coda.PlayAttackAnimationHold(skill);
            var clip = Mathf.Max(0.25f, coda.EstimateSkillClipLength(skill));
            var impactAt = clip * skill1ImpactNormalized;
            if (impactAt > 0f)
            {
                yield return new WaitForSeconds(impactAt);
            }

            SpawnStarHit(coda, boss, skill);
            boss.PlayBeCounteredHold();
            onImpact?.Invoke();

            var tail = clip * (1f - skill1ImpactNormalized);
            if (tail > 0f)
            {
                yield return new WaitForSeconds(tail);
            }

            if (!returnHome)
            {
                yield break;
            }

            yield return ReturnHome(coda, home, skill1RetreatSeconds);
        }

        private IEnumerator PlayPierceBeam(
            UnitView coda,
            UnitView boss,
            SkillDefinitionSO skill,
            bool returnHome,
            Action onImpact)
        {
            var home = coda.transform.position;
            if (returnHome)
            {
                coda.CaptureAnchor();
            }

            coda.PlayAttackAnimationHold(skill);

            var chargeSeconds = ResolveSkill2ChargeSeconds();
            var fireAt = ResolveSkill2BeamFireSeconds();
            var from = ResolveBeamFrom(coda, boss, skill2CastBackX, skill2AimHeight);
            var through = ResolveAim(boss, skill2AimHeight);
            var impactFired = false;
            Action fireImpact = () =>
            {
                if (impactFired)
                {
                    return;
                }

                impactFired = true;
                FindAnyObjectByType<CombatSfxController>()?.PlaySkillSfxImmediate(skill);
                boss.PlayBeCounteredHold();
                onImpact?.Invoke();
            };

            var shot = CodaBeamShotView.Spawn(
                from,
                through,
                new CodaBeamShotSettings
                {
                    Charge = beamChargeSprite,
                    Beam = beamSprite,
                    Impact = beamImpactSprite != null ? beamImpactSprite : starHitSprite,
                    AdditiveMaterial = ResolveAdditive(),
                    ChargeWorldSize = Mathf.Max(0.6f, skill2ChargeWorldSize),
                    ChargeSeconds = chargeSeconds,
                    BeamThickness = Mathf.Max(0.35f, skill2BeamThickness),
                    BeamHoldSeconds = Mathf.Max(0.08f, skill2BeamHoldSeconds),
                    BeamFadeSeconds = Mathf.Max(0.05f, skill2BeamFadeSeconds),
                    PiercePast = Mathf.Max(1.5f, skill2PiercePast),
                    PierceThroughMap = skill2PierceThroughMap,
                    ImpactWorldSize = Mathf.Max(1f, skill2ImpactWorldSize),
                    ImpactSeconds = 0.24f,
                    AimHeight = skill2AimHeight,
                    SortingOrder = sortingOrder,
                    OnImpact = fireImpact
                },
                vfxParent != null ? vfxParent : transform);

            if (shot != null)
            {
                while (shot != null)
                {
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(fireAt);
                fireImpact();
                yield return new WaitForSeconds(skill2BeamHoldSeconds);
            }

            if (!impactFired)
            {
                fireImpact();
            }

            var clipLen = Mathf.Max(fireAt + 0.05f, coda.EstimateSkillClipLength(skill));
            var clipTail = clipLen - fireAt - skill2BeamHoldSeconds - skill2BeamFadeSeconds;
            if (clipTail > 0.01f)
            {
                yield return new WaitForSeconds(clipTail);
            }

            if (!returnHome)
            {
                yield break;
            }

            yield return ReturnHome(coda, home, skill2RetreatSeconds);
        }

        private float ResolveSkill2ChargeSeconds()
        {
            var fps = Mathf.Max(1f, skill2AnimSampleRate);
            var chargeEnd = Mathf.Max(0, skill2ChargeEndFrame);
            var fire = Mathf.Max(chargeEnd + 1, skill2BeamFireFrame);
            return fire / fps;
        }

        private float ResolveSkill2BeamFireSeconds()
        {
            var fps = Mathf.Max(1f, skill2AnimSampleRate);
            var fire = Mathf.Max(0, skill2BeamFireFrame);
            return fire / fps;
        }

        private void SpawnStarHit(UnitView coda, UnitView boss, SkillDefinitionSO skill)
        {
            EnsureDefaults();
            FindAnyObjectByType<CombatSfxController>()?.PlaySkillSfxImmediate(skill);

            if (coda == null || boss == null)
            {
                return;
            }

            var contact = boss.FeetWorldPosition + Vector3.up * skill1ContactHeight;
            var away = boss.FeetWorldPosition.x - coda.FeetWorldPosition.x;
            var burstDir = new Vector2(Mathf.Approximately(away, 0f) ? 1f : Mathf.Sign(away), 0.35f);
            var settings = new CodaStarHitSettings
            {
                Impact = starHitSprite,
                Stars = starDebrisSprites,
                AdditiveMaterial = ResolveAdditive(),
                ImpactWorldSize = Mathf.Max(1.2f, starHitWorldSize),
                ImpactSeconds = Mathf.Max(0.1f, starHitSeconds),
                StarWorldSize = Mathf.Max(0.15f, starDebrisWorldSize),
                StarCount = Mathf.Clamp(starDebrisCount, 5, 18),
                StarBurstSeconds = Mathf.Max(0.2f, starBurstSeconds),
                SortingOrder = sortingOrder,
                BurstDir = burstDir
            };
            CodaStarHitView.Spawn(contact, settings, vfxParent != null ? vfxParent : transform);
        }

        public void ApplySkill1Tuning(
            float standoffX,
            float contactHeight,
            float hitWorldSize,
            float debrisWorldSize,
            int debrisCount)
        {
            skill1StandoffX = Mathf.Max(0.2f, standoffX);
            skill1ContactHeight = contactHeight;
            starHitWorldSize = Mathf.Max(0.4f, hitWorldSize);
            starDebrisWorldSize = Mathf.Max(0.1f, debrisWorldSize);
            starDebrisCount = Mathf.Clamp(debrisCount, 5, 18);
        }

        public void ApplySkill2Tuning(
            float castBackX,
            float aimHeight,
            float beamThickness,
            float piercePast,
            bool pierceThroughMap,
            float chargeWorldSize,
            float impactWorldSize)
        {
            skill2CastBackX = castBackX;
            skill2AimHeight = aimHeight;
            skill2BeamThickness = Mathf.Max(0.2f, beamThickness);
            skill2PiercePast = Mathf.Max(1f, piercePast);
            skill2PierceThroughMap = pierceThroughMap;
            skill2ChargeWorldSize = Mathf.Max(0.3f, chargeWorldSize);
            skill2ImpactWorldSize = Mathf.Max(0.4f, impactWorldSize);
        }

        public void ApplySkill3Tuning(
            float castBackX,
            float aimHeight,
            float chargeSeconds,
            float chargeWorldSize,
            float boltWorldSize,
            float arcSpreadY,
            float controlBulge,
            float impactWorldSize,
            float boltFacingOffsetDegrees = 180f,
            bool invertArcNormal = false)
        {
            skill3CastBackX = castBackX;
            skill3AimHeight = aimHeight;
            skill3ChargeSeconds = Mathf.Max(0.12f, chargeSeconds);
            skill3ChargeWorldSize = Mathf.Max(0.4f, chargeWorldSize);
            skill3BoltWorldSize = Mathf.Max(0.4f, boltWorldSize);
            skill3ArcSpreadY = Mathf.Max(0.2f, arcSpreadY);
            skill3ControlBulge = Mathf.Max(0.2f, controlBulge);
            skill3ImpactWorldSize = Mathf.Max(0.4f, impactWorldSize);
            skill3BoltFacingOffsetDegrees = boltFacingOffsetDegrees;
            skill3InvertArcNormal = invertArcNormal;
        }

        private IEnumerator PlayArcVolley(
            UnitView coda,
            UnitView boss,
            SkillDefinitionSO skill,
            bool returnHome,
            Action onImpact)
        {
            var home = coda.transform.position;
            if (returnHome)
            {
                coda.CaptureAnchor();
            }

            if (Mathf.Abs(skill3CastBackX) > 0.01f)
            {
                var castFeet = ResolveCastBackFeet(coda, boss, skill3CastBackX);
                if (Vector2.Distance(
                        new Vector2(coda.FeetWorldPosition.x, coda.FeetWorldPosition.y),
                        new Vector2(castFeet.x, castFeet.y)) > 0.04f)
                {
                    coda.PlayMovingLoop();
                    yield return coda.MoveFeetToRoutine(
                        castFeet,
                        Mathf.Max(0.04f, skill3CastStepSeconds));
                }
            }

            yield return EncounterDirector.PresentArmedCaster();
            coda.PlayAttackAnimationHold(skill);

            var from = ResolveAimFromFeet(coda, skill3AimHeight);
            var through = ResolveAimFromFeet(boss, skill3AimHeight);
            var impactFired = false;
            Action fireImpact = () =>
            {
                if (impactFired)
                {
                    return;
                }

                impactFired = true;
                FindAnyObjectByType<CombatSfxController>()?.PlaySkillSfxImmediate(skill);
                if (!EncounterDirector.TryQueueArmedVictimHit(() => onImpact?.Invoke()))
                {
                    onImpact?.Invoke();
                }
            };

            var impactSprite = arcImpactSprite
                               ?? LoadSprite("coda_vfx_arc_impact_v1")
                               ?? starHitSprite
                               ?? LoadSprite("coda_vfx_star_hit_v1");
            var boltSprite = arcBoltSprite
                             ?? LoadSprite("coda_vfx_arc_bolt_v1")
                             ?? LoadSprite("coda_vfx_crescent_slash_v1");
            var chargeSprite = arcChargeSprite
                               ?? beamChargeSprite
                               ?? LoadSprite("coda_vfx_beam_charge_v1");

            var volley = CodaArcVolleyView.Spawn(
                from,
                through,
                new CodaArcVolleySettings
                {
                    Charge = chargeSprite,
                    Bolt = boltSprite,
                    Impact = impactSprite,
                    AdditiveMaterial = ResolveAdditive(),
                    ChargeWorldSize = Mathf.Max(0.5f, skill3ChargeWorldSize),
                    ChargeSeconds = Mathf.Max(0.12f, skill3ChargeSeconds),
                    BoltWorldSize = Mathf.Max(0.4f, skill3BoltWorldSize),
                    ImpactWorldSize = Mathf.Max(1.2f, skill3ImpactWorldSize),
                    ImpactSeconds = Mathf.Max(0.12f, skill3ImpactSeconds),
                    FinaleImpactScale = Mathf.Max(1f, skill3FinaleImpactScale),
                    FinaleImpactSeconds = Mathf.Max(0.12f, skill3FinaleImpactSeconds),
                    AftermathHoldSeconds = 0f,
                    FlightSeconds = Mathf.Max(0.1f, skill3FlightSeconds),
                    StaggerSeconds = Mathf.Max(0f, skill3StaggerSeconds),
                    BoltCount = Mathf.Clamp(skill3BoltCount, 1, 8),
                    ArcSpreadY = Mathf.Max(0.2f, skill3ArcSpreadY),
                    ControlBulge = Mathf.Max(0.2f, skill3ControlBulge),
                    BoltFacingOffsetDegrees = skill3BoltFacingOffsetDegrees,
                    InvertArcNormal = skill3InvertArcNormal,
                    SortingOrder = sortingOrder + 1,
                    OnImpact = fireImpact
                },
                vfxParent != null ? vfxParent : transform);

            if (volley != null)
            {
                while (volley != null)
                {
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(skill3ChargeSeconds);
                fireImpact();
                if (skill3AftermathHoldSeconds > 0f)
                {
                    yield return new WaitForSeconds(skill3AftermathHoldSeconds);
                }
            }

            if (!impactFired)
            {
                fireImpact();
            }

            yield return EncounterDirector.WaitArmedVictimFocus();

            if (returnHome)
            {
                yield return new WaitForSeconds(1f);
                coda.SnapFeetTo(home, coda.transform.position.z);
                coda.CaptureAnchor();
                coda.PlayIdleState();
            }
        }

        public void EnsureDefaults()
        {
            if (starHitSprite == null)
            {
                starHitSprite = LoadSprite("coda_vfx_star_hit_v1")
                                ?? LoadSprite("coda_vfx_impact_v1");
            }

            if (starDebrisSprites == null || starDebrisSprites.Length == 0 || AllNull(starDebrisSprites))
            {
                starDebrisSprites = LoadStarDebris();
            }

            if (beamChargeSprite == null)
            {
                beamChargeSprite = LoadSprite("coda_vfx_beam_charge_v1")
                                   ?? LoadSprite("coda_vfx_impact_v1");
            }

            if (beamSprite == null)
            {
                beamSprite = LoadSprite("coda_vfx_beam_v1");
            }

            if (beamImpactSprite == null)
            {
                beamImpactSprite = LoadSprite("coda_vfx_star_hit_v1")
                                   ?? LoadSprite("coda_vfx_impact_v1")
                                   ?? starHitSprite;
            }

            if (arcChargeSprite == null)
            {
                arcChargeSprite = LoadSprite("coda_vfx_beam_charge_v1")
                                  ?? beamChargeSprite;
            }

            if (arcBoltSprite == null)
            {
                arcBoltSprite = LoadSprite("coda_vfx_arc_bolt_v1")
                                ?? LoadSprite("coda_vfx_crescent_slash_v1");
            }

            if (arcImpactSprite == null)
            {
                arcImpactSprite = LoadSprite("coda_vfx_arc_impact_v1")
                                  ?? starHitSprite;
            }
        }

        private Sprite[] LoadStarDebris()
        {
            var path = ResourceRoot + "coda_vfx_star_debris_v1";
            var sliced = Resources.LoadAll<Sprite>(path);
            if (sliced != null && sliced.Length > 1)
            {
                return sliced;
            }

            var tex = Resources.Load<Texture2D>(path);
            if (tex != null)
            {
                var fromSheet = CodaStarHitView.SliceStarSheet(tex);
                if (fromSheet != null && fromSheet.Length > 0)
                {
                    return fromSheet;
                }
            }

            var single = Resources.Load<Sprite>(path) ?? starHitSprite;
            return single != null ? new[] { single } : null;
        }

        private IEnumerator ReturnHome(UnitView coda, Vector3 home, float seconds)
        {
            coda.PlayMovingLoop();
            yield return coda.MoveToRoutine(home, seconds);
            coda.transform.position = new Vector3(home.x, home.y, coda.transform.position.z);
            coda.CaptureAnchor();
            coda.PlayIdleState();
        }

        private static Vector3 ResolveAim(UnitView view, float height)
        {
            if (view == null)
            {
                return Vector3.zero;
            }

            return view.FeetWorldPosition + Vector3.up * height;
        }

        private static Vector3 ResolveBodyAim(UnitView view, float heightFallback)
        {
            if (view == null)
            {
                return Vector3.zero;
            }

            var sr = view.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                var b = sr.bounds;
                return new Vector3(b.center.x, b.center.y, view.transform.position.z);
            }

            var col = view.GetComponent<Collider2D>();
            if (col != null)
            {
                var b = col.bounds;
                return new Vector3(b.center.x, b.center.y, view.transform.position.z);
            }

            return ResolveAimFromFeet(view, heightFallback);
        }

        private static Vector3 ResolveAimFromFeet(UnitView view, float aimHeight)
        {
            if (view == null)
            {
                return Vector3.up * Mathf.Max(0f, aimHeight);
            }

            var feet = view.FeetWorldPosition;
            return new Vector3(feet.x, feet.y + Mathf.Max(0f, aimHeight), feet.z);
        }

        private static Vector3 ResolveBeamFrom(UnitView coda, UnitView boss, float castBackX, float height)
        {
            if (coda == null)
            {
                return Vector3.zero;
            }

            var feet = coda.FeetWorldPosition;
            var dir = 1f;
            if (boss != null)
            {
                dir = Mathf.Sign(boss.FeetWorldPosition.x - feet.x);
                if (Mathf.Approximately(dir, 0f))
                {
                    dir = 1f;
                }
            }

            return new Vector3(feet.x - dir * castBackX, feet.y + height, feet.z);
        }

        private static bool IsCodaSkillId(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            return skillId.StartsWith("mage_", StringComparison.OrdinalIgnoreCase)
                   || skillId.StartsWith("coda_", StringComparison.OrdinalIgnoreCase)
                   || skillId.StartsWith("Coda_", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsCodaUnit(CombatUnit unit, UnitView view)
        {
            if (unit != null)
            {
                var id = unit.UnitId ?? string.Empty;
                if (id.IndexOf("coda", StringComparison.OrdinalIgnoreCase) >= 0
                    || id.IndexOf("mage", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                var name = unit.DisplayName ?? string.Empty;
                if (name.IndexOf("coda", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            if (view != null)
            {
                var n = view.name ?? string.Empty;
                if (n.IndexOf("coda", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("mage", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector3 ResolveStandoffFeet(UnitView coda, UnitView boss, float standoffX)
        {
            var bossFeet = boss.FeetWorldPosition;
            var codaFeet = coda.FeetWorldPosition;
            var dir = Mathf.Sign(bossFeet.x - codaFeet.x);
            if (Mathf.Approximately(dir, 0f))
            {
                dir = 1f;
            }

            return new Vector3(bossFeet.x - dir * standoffX, bossFeet.y, codaFeet.z);
        }

        private static Vector3 ResolveCastBackFeet(UnitView coda, UnitView boss, float backX)
        {
            var bossFeet = boss.FeetWorldPosition;
            var codaFeet = coda.FeetWorldPosition;
            var dir = Mathf.Sign(bossFeet.x - codaFeet.x);
            if (Mathf.Approximately(dir, 0f))
            {
                dir = 1f;
            }

            return new Vector3(codaFeet.x - dir * backX, codaFeet.y, codaFeet.z);
        }

        private static float ResolveMoveSeconds(Vector3 from, Vector3 to, float speed, float fallback)
        {
            var distance = Vector2.Distance(new Vector2(from.x, from.y), new Vector2(to.x, to.y));
            if (speed <= 0.01f)
            {
                return Mathf.Max(0.04f, fallback);
            }

            return Mathf.Clamp(distance / speed, 0.04f, Mathf.Max(0.04f, fallback));
        }

        private Material ResolveAdditive()
        {
            if (additiveMaterial != null)
            {
                return additiveMaterial;
            }

            if (_runtimeAdditive != null)
            {
                return _runtimeAdditive;
            }

            var shader = Shader.Find("FracturedChorus/VFX/RenBulletAdditive")
                         ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return null;
            }

            _runtimeAdditive = new Material(shader)
            {
                name = "CodaSkillAdditive_Runtime",
                hideFlags = HideFlags.HideAndDontSave
            };
            return _runtimeAdditive;
        }

        private static bool AllNull(Sprite[] sprites)
        {
            for (var i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                {
                    return false;
                }
            }

            return true;
        }

        private static Sprite LoadSprite(string fileName)
        {
            var path = ResourceRoot + fileName;
            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            var sprites = Resources.LoadAll<Sprite>(path);
            if (sprites != null && sprites.Length > 0)
            {
                return sprites[0];
            }

            var tex = Resources.Load<Texture2D>(path);
            if (tex == null)
            {
                return null;
            }

            return Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private void OnDestroy()
        {
            if (_runtimeAdditive != null)
            {
                Destroy(_runtimeAdditive);
                _runtimeAdditive = null;
            }
        }
    }
}

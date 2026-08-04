using System;
using System.Collections;
using FracturedChorus.Audio;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public class PlayerSkillShotChoreographer : MonoBehaviour
    {
        private const string ResourceRoot = "VFX/Combat/Ren/";

        [Header("Placement")]
        [SerializeField] private Transform shotParent;
        [SerializeField] private float aimHeightOffset = 0.55f;

        [Header("Skill 1 — Gun butt (BasicAttack)")]
        [SerializeField] private Sprite meleeArcSprite;
        [SerializeField] private Sprite meleeImpactSprite;
        [SerializeField] private float meleeArcSeconds = 0.14f;
        [SerializeField] private float meleeImpactSeconds = 0.18f;
        [SerializeField] private float meleeArcWorldSize = 1.65f;
        [SerializeField] private float meleeImpactWorldSize = 1.55f;
        [SerializeField] private float meleeStandoffX = 1.85f;
        [SerializeField] private float meleeHitReach = 0.62f;
        [SerializeField] private float meleeLungeSpeed = 34f;
        [SerializeField] private float meleeLungeSeconds = 0.14f;
        [SerializeField] private float meleeRetreatSeconds = 0.18f;
        [SerializeField] private float meleeAnimSampleRate = 24f;
        [SerializeField] private int meleeImpactFrame = 6;
        [SerializeField] [Range(0.05f, 0.95f)] private float meleeImpactNormalizedTime = 0.35f;

        [Header("Skill 2 / 3 — Bullet")]
        [SerializeField] private Sprite bulletHeadSprite;
        [SerializeField] private Sprite bulletTrailSprite;
        [SerializeField] private Sprite bulletImpactSprite;
        [SerializeField] private Sprite[] bulletFlightFrames;
        [SerializeField] private Sprite glassCrackSprite;
        [SerializeField] private Sprite glassShatterSprite;
        [SerializeField] private float bulletMuzzleDelaySeconds = 0.1f;
        [SerializeField] private float bulletTravelSeconds = 0.16f;
        [SerializeField] private float bulletTravelSpeed = 40f;
        [SerializeField] private float bulletImpactSeconds = 0.1f;
        [SerializeField] private float bulletHeadWorldSize = 1.275f;
        [SerializeField] private float bulletTrailHeight = 0.825f;
        [SerializeField] private float glassWorldSize = 2.55f;
        [SerializeField] private float glassCrackInSeconds = 0.04f;
        [SerializeField] private float glassCrackHoldSeconds = 0.06f;
        [SerializeField] private float glassShatterSeconds = 0.28f;

        [Header("Skill 3 — Multi shot (Ultimate)")]
        [SerializeField] [Min(1)] private int skill3BulletCount = 2;
        [SerializeField] private float skill3AnimSampleRate = 24f;
        [SerializeField] private int skill3ShotFrameA = 6;
        [SerializeField] private int skill3ShotFrameB = 10;
        [SerializeField] private float skill3VerticalSpread = 0.18f;
        [SerializeField] private float skill3GlassDelaySeconds = 0.08f;
        [SerializeField] private Sprite ultAuraGlowSprite;
        [SerializeField] private Sprite ultAuraWaveformSprite;
        [SerializeField] private Sprite ultAuraNotesSprite;
        [SerializeField] private Sprite[] ultAuraWaveVariants;
        [SerializeField] private Sprite[] ultAuraNoteVariants;
        [SerializeField] private float ultAuraWorldSize = 3.4f;
        [SerializeField] private float ultAuraOrbitRadius = 1.45f;
        [SerializeField] [Range(0.35f, 0.9f)] private float ultAuraConvergeNormalized = 0.72f;

        [Header("Shared")]
        [SerializeField] private Material additiveMaterial;
        [SerializeField] private int sortingOrder = 40;
        [SerializeField] private bool loadResourcesFallback = true;

        private CombatSession _session;
        private Material _runtimeAdditive;
        private int _deferredCounterMeleeBeat = -1;

        public bool IsMeleeSkill(SkillDefinitionSO skill) =>
            ResolvePresentation(skill) == RenSkillPresentation.MeleeStock;

        public void Configure(CombatSession session)
        {
            Unsubscribe();
            EnsureDefaults();
            _session = session;
            _deferredCounterMeleeBeat = -1;
            if (_session == null)
            {
                return;
            }

            _session.OnPlayerSkillResolved += HandlePlayerSkillResolved;
        }

        private void OnValidate()
        {
            skill3BulletCount = Mathf.Max(1, skill3BulletCount);
            skill3AnimSampleRate = Mathf.Max(1f, skill3AnimSampleRate);
            skill3ShotFrameA = Mathf.Max(0, skill3ShotFrameA);
            skill3ShotFrameB = Mathf.Max(skill3ShotFrameA, skill3ShotFrameB);
            meleeAnimSampleRate = Mathf.Max(1f, meleeAnimSampleRate);
            meleeImpactFrame = Mathf.Max(0, meleeImpactFrame);
            meleeStandoffX = Mathf.Max(0.35f, meleeStandoffX);
            meleeHitReach = Mathf.Max(0.1f, meleeHitReach);
            skill3GlassDelaySeconds = Mathf.Max(0f, skill3GlassDelaySeconds);
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (_runtimeAdditive != null)
            {
                Destroy(_runtimeAdditive);
                _runtimeAdditive = null;
            }
        }

        private void Unsubscribe()
        {
            if (_session != null)
            {
                _session.OnPlayerSkillResolved -= HandlePlayerSkillResolved;
            }

            _session = null;
        }

        public void PlayBulletPresentationForCutscene(
            SkillDefinitionSO skill,
            Vector3 from,
            Vector3 to)
        {
            PlayBulletPresentationForCutscene(skill, null, from, to);
        }

        public void PlayBulletPresentationForCutscene(
            SkillDefinitionSO skill,
            UnitView sourceView,
            Vector3 from,
            Vector3 to)
        {
            StartCoroutine(PlayBulletPresentationForCutsceneRoutine(skill, sourceView, from, to));
        }

        public IEnumerator PlayBulletPresentationForCutsceneRoutine(
            SkillDefinitionSO skill,
            UnitView sourceView,
            Vector3 from,
            Vector3 to,
            Action onImpact = null)
        {
            if (skill == null)
            {
                yield break;
            }

            EnsureDefaults();
            var parent = shotParent != null ? shotParent : transform;
            switch (ResolvePresentation(skill))
            {
                case RenSkillPresentation.SingleBullet:
                    yield return PlaySingleBulletRoutine(skill, from, to, parent, onImpact);
                    break;
                case RenSkillPresentation.MultiBullet:
                    yield return PlayMultiBulletRoutine(sourceView, skill, from, to, parent, onImpact);
                    break;
            }
        }

        private RenBulletShotSettings BuildPierceBulletSettings(
            Vector3 impactWorld,
            bool withGlassShatter = false)
        {
            var settings = BuildBulletSettings();
            settings.PierceThroughScreen = true;
            settings.ImpactWorld = impactWorld;
            settings.TravelSpeed = Mathf.Max(24f, bulletTravelSpeed);
            settings.TravelSeconds = Mathf.Max(0.08f, bulletTravelSeconds);
            if (withGlassShatter)
            {
                settings.GlassShatter = BuildGlassShatterSettings();
            }

            return settings;
        }

        private void HandlePlayerSkillResolved(PlayerSkillResolvedReport report)
        {
            if (EncounterDirector.IsPresenting)
            {
                return;
            }

            if (!report.IsValid || report.Target == null || !IsRenDamageSkill(report.Skill))
            {
                return;
            }

            var sourceView = UnitView.FindForUnit(report.Source);
            var targetView = UnitView.FindForUnit(report.Target);
            if (sourceView == null || targetView == null)
            {
                return;
            }

            var presentation = ResolvePresentation(report.Skill);
            if (presentation == RenSkillPresentation.MeleeStock)
            {
                if (ShouldDeferMeleeToCounterChoreo(report))
                {
                    _deferredCounterMeleeBeat = report.BeatIndex;
                    return;
                }

                StartCoroutine(PlayMeleeEngageRoutine(sourceView, targetView, report.Skill));
                return;
            }

            var from = ResolveAimPoint(sourceView);
            var to = ResolveAimPoint(targetView);
            var parent = shotParent != null ? shotParent : transform;

            switch (presentation)
            {
                case RenSkillPresentation.SingleBullet:
                    StartCoroutine(PlaySingleBulletRoutine(report.Skill, from, to, parent, null));
                    break;
                case RenSkillPresentation.MultiBullet:
                    StartCoroutine(PlayMultiBulletRoutine(sourceView, report.Skill, from, to, parent, null));
                    break;
            }
        }

        public IEnumerator PlaySingleBulletRoutine(
            SkillDefinitionSO skill,
            Vector3 from,
            Vector3 to,
            Transform parent,
            Action onImpact)
        {
            EnsureDefaults();
            parent = parent != null ? parent : (shotParent != null ? shotParent : transform);

            if (bulletMuzzleDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(bulletMuzzleDelaySeconds);
            }

            FindAnyObjectByType<CombatSfxController>()?.PlaySkillSfxImmediate(skill);
            var settings = BuildPierceBulletSettings(to, withGlassShatter: true);
            if (onImpact != null)
            {
                settings.OnImpact = _ => onImpact.Invoke();
            }

            RenBulletShotView.Spawn(from, to, settings, parent);
            yield return new WaitForSeconds(
                RenBulletShotView.EstimatePresentationSeconds(from, to, settings));
        }

        public bool TryBeginCounterMeleeEngage(
            int beatIndex,
            UnitView renView,
            UnitView bossView,
            SkillDefinitionSO skill,
            Action onImpact,
            out IEnumerator engageRoutine)
        {
            engageRoutine = null;
            if (renView == null || bossView == null || !IsMeleeSkill(skill))
            {
                return false;
            }

            if (_deferredCounterMeleeBeat >= 0 && _deferredCounterMeleeBeat != beatIndex)
            {
                return false;
            }

            _deferredCounterMeleeBeat = -1;
            engageRoutine = PlayMeleeEngageRoutine(renView, bossView, skill, returnHome: true, onImpact);
            return true;
        }

        public IEnumerator PlayMeleeEngageRoutine(
            UnitView renView,
            UnitView targetView,
            SkillDefinitionSO skill,
            bool returnHome = true,
            Action onImpact = null)
        {
            if (renView == null || targetView == null)
            {
                yield break;
            }

            var home = ResolveAuthoritativeHome(renView);
            if (returnHome)
            {
                renView.CaptureAnchor();
            }

            var strikeFeet = ResolveMeleeStrikeFeet(renView, targetView);
            renView.PlayMovingLoop();
            yield return renView.MoveFeetToRoutine(
                strikeFeet,
                ResolveMoveSeconds(renView.FeetWorldPosition, strikeFeet, meleeLungeSpeed, meleeLungeSeconds));

            renView.PlayAttackAnimationHold(skill);
            SpawnCloseMeleeHit(renView, targetView);

            var clipLength = Mathf.Max(renView.EstimateSkillClipLength(skill), meleeArcSeconds + meleeImpactSeconds);
            var animSpeed = Mathf.Max(0.01f, renView.AnimatorSpeed);
            var sampleRate = Mathf.Max(1f, meleeAnimSampleRate);
            var impactDelay = meleeImpactFrame / sampleRate / animSpeed;
            if (impactDelay <= 0f && clipLength > 0f)
            {
                impactDelay = clipLength * meleeImpactNormalizedTime / animSpeed;
            }

            if (impactDelay > 0f)
            {
                yield return new WaitForSeconds(impactDelay);
            }

            FindAnyObjectByType<CombatSfxController>()?.PlaySkillSfxImmediate(skill);
            onImpact?.Invoke();

            var tail = Mathf.Max(0f, clipLength / animSpeed - impactDelay);
            if (tail > 0f)
            {
                yield return new WaitForSeconds(tail);
            }

            if (!returnHome)
            {
                yield break;
            }

            renView.PlayMovingLoop();
            yield return renView.MoveToRoutine(home, meleeRetreatSeconds);
            renView.transform.position = new Vector3(home.x, home.y, renView.transform.position.z);
            renView.CaptureAnchor();
            renView.PlayIdleState();
        }

        private void SpawnCloseMeleeHit(UnitView renView, UnitView targetView)
        {
            var contact = ResolveAimPoint(targetView);
            var renAim = ResolveAimPoint(renView);
            var delta = contact - renAim;
            delta.z = 0f;
            if (delta.sqrMagnitude < 0.0001f)
            {
                delta = Vector3.right;
            }

            var dir = delta.normalized;
            var swingFrom = contact - dir * meleeHitReach;
            var parent = shotParent != null ? shotParent : transform;
            RenMeleeStrikeView.Spawn(swingFrom, contact, BuildMeleeSettings(), parent);
        }

        private Vector3 ResolveMeleeStrikeFeet(UnitView renView, UnitView targetView)
        {
            var targetFeet = targetView.FeetWorldPosition;
            var renFeet = renView.FeetWorldPosition;
            var towardTarget = Mathf.Sign(targetFeet.x - renFeet.x);
            if (Mathf.Approximately(towardTarget, 0f))
            {
                towardTarget = 1f;
            }

            var strikeX = targetFeet.x - towardTarget * meleeStandoffX;
            return new Vector3(strikeX, targetFeet.y, renFeet.z);
        }

        private static Vector3 ResolveAuthoritativeHome(UnitView view)
        {
            if (view == null)
            {
                return Vector3.zero;
            }

            if (EncounterDirector.ActiveInstance != null
                && EncounterDirector.ActiveInstance.TryGetPhaseHomeRoot(view, out var phaseRoot))
            {
                return phaseRoot;
            }

            return view.transform.position;
        }

        private static float ResolveMoveSeconds(Vector3 from, Vector3 to, float speed, float fallbackSeconds)
        {
            var distance = Vector2.Distance(new Vector2(from.x, from.y), new Vector2(to.x, to.y));
            if (speed <= 0.01f)
            {
                return Mathf.Max(0.04f, fallbackSeconds);
            }

            return Mathf.Clamp(distance / speed, 0.04f, Mathf.Max(0.04f, fallbackSeconds));
        }

        private bool ShouldDeferMeleeToCounterChoreo(PlayerSkillResolvedReport report)
        {
            if (!EnemyStrikeChoreographer.OwnsCounterPresentation || _session?.Timeline == null)
            {
                return false;
            }

            if (!CombatCounterResolver.ShouldPresentCounterBodyAtBeat(_session.Timeline, report.BeatIndex))
            {
                return false;
            }

            var telegraphs = _session.Timeline.GetImpactTelegraphsAtBeat(report.BeatIndex);
            if (telegraphs == null || telegraphs.Count == 0)
            {
                return false;
            }

            foreach (var telegraph in telegraphs)
            {
                if (!CombatCounterResolver.IsTelegraphFullyCountered(telegraph, _session.Timeline))
                {
                    continue;
                }

                foreach (var entry in _session.Timeline.Agenda)
                {
                    if (entry?.Unit != report.Source || entry.Skill != report.Skill)
                    {
                        continue;
                    }

                    if (CombatCounterResolver.IsCounterEntry(entry, telegraph))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private IEnumerator PlayMultiBulletRoutine(
            UnitView sourceView,
            SkillDefinitionSO skill,
            Vector3 from,
            Vector3 to,
            Transform parent,
            Action onImpact)
        {
            EnsureDefaults();
            var count = Mathf.Max(1, skill3BulletCount);
            var settings = BuildBulletSettings();
            var sampleRate = Mathf.Max(1f, skill3AnimSampleRate);
            var frameA = Mathf.Max(0, skill3ShotFrameA);
            var frameB = Mathf.Max(frameA, skill3ShotFrameB);
            var timeA = frameA / sampleRate;
            var timeB = frameB / sampleRate;
            var resolveFired = false;
            Action fireResolve = () =>
            {
                if (resolveFired || onImpact == null)
                {
                    return;
                }

                resolveFired = true;
                onImpact.Invoke();
            };

            if (sourceView != null && skill != null)
            {
                sourceView.PlayAttackAnimationHold(skill);
            }

            RenRedMusicAuraView aura = null;
            if (IsRenUnit(sourceView, skill))
            {
                var auraCenter = sourceView != null
                    ? sourceView.FeetWorldPosition + Vector3.up * 0.7f
                    : from;
                aura = RenRedMusicAuraView.Spawn(
                    sourceView != null ? sourceView.transform : null,
                    auraCenter,
                    BuildUltAuraSettings(),
                    parent);

                var charge = Mathf.Max(0.12f, timeA);
                var convergeAt = Mathf.Clamp01(ultAuraConvergeNormalized);
                var orbitSeconds = charge * convergeAt;
                var convergeSeconds = charge * (1f - convergeAt);
                yield return aura.PlayOrbitThenConverge(from, orbitSeconds, convergeSeconds);
            }
            else if (timeA > 0f)
            {
                yield return new WaitForSeconds(timeA);
            }

            settings = BuildPierceBulletSettings(to, withGlassShatter: true);
            var impactFxFired = false;
            settings.OnImpact = world =>
            {
                fireResolve();
                if (impactFxFired)
                {
                    return;
                }

                impactFxFired = true;
                FindAnyObjectByType<CombatSfxController>()?.PlayRenSkillSlotImmediate(SkillSlotKind.Ultimate);
            };

            FindAnyObjectByType<CombatSfxController>()?.PlayRenSkillSlotImmediate(SkillSlotKind.Skill);
            SpawnSkill3Bullet(from, to, 0, count, settings, parent, withGlassShatter: true);

            if (count > 1)
            {
                var gap = Mathf.Max(0f, timeB - timeA);
                if (gap > 0f)
                {
                    yield return new WaitForSeconds(gap);
                }

                FindAnyObjectByType<CombatSfxController>()?.PlayRenSkillSlotImmediate(SkillSlotKind.Skill);
                SpawnSkill3Bullet(from, to, 1, count, settings, parent, withGlassShatter: false);
            }

            var flightWait = RenBulletShotView.EstimatePresentationSeconds(from, to, settings);
            var wait = Mathf.Max(
                flightWait,
                EstimateSkill3ImpactTravelSeconds(from, to, settings)
                + RenGlassShatterView.EstimateSeconds(BuildGlassShatterSettings()));
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait);
            }

            if (!resolveFired)
            {
                fireResolve();
            }

            aura?.StopAndDestroy();
        }

        private float EstimateSkill3ImpactTravelSeconds(
            Vector3 from,
            Vector3 to,
            RenBulletShotSettings settings)
        {
            if (settings == null)
            {
                return 0.16f;
            }

            var impactAt = settings.ImpactWorld ?? to;
            var end = settings.PierceThroughScreen
                ? RenBulletShotView.ResolveScreenExit(from, to)
                : to;
            var fullDist = Vector3.Distance(from, end);
            var impactDist = Vector3.Distance(from, impactAt);
            var travel = RenBulletShotView.ResolveTravelSeconds(fullDist, settings);
            if (fullDist < 0.01f)
            {
                return travel;
            }

            var ratio = Mathf.Clamp01(impactDist / fullDist);
            var t = 1f - Mathf.Sqrt(Mathf.Max(0f, 1f - ratio));
            return travel * t;
        }

        private void SpawnSkill3Bullet(
            Vector3 from,
            Vector3 to,
            int index,
            int count,
            RenBulletShotSettings settings,
            Transform parent,
            bool withGlassShatter)
        {
            var offsetY = count <= 1
                ? 0f
                : Mathf.Lerp(-skill3VerticalSpread, skill3VerticalSpread, index / (float)(count - 1));
            var shotFrom = from + new Vector3(0f, offsetY, 0f);
            var shotTo = to + new Vector3(0f, offsetY * 0.35f, 0f);
            var shotSettings = CloneBulletSettings(settings);
            shotSettings.PierceThroughScreen = true;
            shotSettings.ImpactWorld = shotTo;
            shotSettings.OnImpact = settings.OnImpact;
            if (!withGlassShatter)
            {
                shotSettings.GlassShatter = null;
            }

            RenBulletShotView.Spawn(shotFrom, shotTo, shotSettings, parent);
        }

        private static bool IsRenUnit(UnitView view, SkillDefinitionSO skill)
        {
            if (skill != null && !string.IsNullOrEmpty(skill.skillId)
                && skill.skillId.StartsWith("ren_"))
            {
                return true;
            }

            var unit = view != null ? view.Unit : null;
            if (unit != null && unit.UnitId == "ren")
            {
                return true;
            }

            return view != null && view.DemoUnitKey == "ren";
        }

        private RenBulletShotSettings CloneBulletSettings(RenBulletShotSettings source)
        {
            var built = BuildPierceBulletSettings(source != null && source.ImpactWorld.HasValue
                ? source.ImpactWorld.Value
                : Vector3.zero);
            if (source == null)
            {
                return built;
            }

            built.TravelSeconds = source.TravelSeconds;
            built.TravelSpeed = source.TravelSpeed;
            built.ImpactSeconds = source.ImpactSeconds;
            built.PierceThroughScreen = source.PierceThroughScreen;
            built.ImpactWorld = source.ImpactWorld;
            built.GlassShatter = source.GlassShatter;
            built.OnImpact = source.OnImpact;
            return built;
        }

        private RenGlassShatterSettings BuildGlassShatterSettings()
        {
            EnsureDefaults();
            if (glassCrackSprite == null && glassShatterSprite == null)
            {
                return null;
            }

            return new RenGlassShatterSettings
            {
                Crack = glassCrackSprite,
                Shatter = glassShatterSprite,
                AdditiveMaterial = ResolveAdditiveMaterial(),
                WorldSize = glassWorldSize,
                CrackInSeconds = Mathf.Max(0.01f, glassCrackInSeconds),
                CrackHoldSeconds = Mathf.Max(0.01f, glassCrackHoldSeconds),
                ShatterSeconds = Mathf.Max(0.01f, glassShatterSeconds),
                SortingOrder = sortingOrder + 2,
                OnShatter = () => FindAnyObjectByType<CombatSfxController>()?.PlayMirrorBreaking()
            };
        }

        private RenRedMusicAuraSettings BuildUltAuraSettings()
        {
            EnsureDefaults();
            return new RenRedMusicAuraSettings
            {
                Glow = ultAuraGlowSprite,
                Waveform = ultAuraWaveformSprite,
                WaveVariants = ultAuraWaveVariants,
                Notes = ultAuraNotesSprite,
                NoteVariants = ultAuraNoteVariants,
                AdditiveMaterial = ResolveAdditiveMaterial(),
                WorldSize = ultAuraWorldSize,
                OrbitRadius = ultAuraOrbitRadius,
                WaveCount = 12,
                NoteCount = 10,
                SortingOrder = sortingOrder - 2,
                Tint = new Color(1f, 0.14f, 0.22f, 1f)
            };
        }

        private static RenSkillPresentation ResolvePresentation(SkillDefinitionSO skill)
        {
            if (skill == null)
            {
                return RenSkillPresentation.None;
            }

            return skill.slotKind switch
            {
                SkillSlotKind.BasicAttack => RenSkillPresentation.MeleeStock,
                SkillSlotKind.Skill => RenSkillPresentation.SingleBullet,
                SkillSlotKind.Ultimate => RenSkillPresentation.MultiBullet,
                _ => skill.skillId == "ren_basic"
                    ? RenSkillPresentation.MeleeStock
                    : RenSkillPresentation.SingleBullet
            };
        }

        private static bool IsRenDamageSkill(SkillDefinitionSO skill)
        {
            if (skill == null || string.IsNullOrEmpty(skill.skillId))
            {
                return false;
            }

            if (!skill.skillId.StartsWith("ren_"))
            {
                return false;
            }

            if (skill.IsGuard || skill.effectKind != SkillEffectKind.Damage)
            {
                return false;
            }

            return skill.targetType == SkillTargetType.SingleEnemy
                   || skill.targetType == SkillTargetType.AllEnemies;
        }

        private Vector3 ResolveAimPoint(UnitView view)
        {
            var feet = view.FeetWorldPosition;
            return new Vector3(feet.x, feet.y + aimHeightOffset, feet.z);
        }

        private RenMeleeStrikeSettings BuildMeleeSettings()
        {
            return new RenMeleeStrikeSettings
            {
                Arc = meleeArcSprite,
                Impact = meleeImpactSprite,
                AdditiveMaterial = ResolveAdditiveMaterial(),
                ArcSeconds = meleeArcSeconds,
                ImpactSeconds = meleeImpactSeconds,
                ArcWorldSize = meleeArcWorldSize,
                ImpactWorldSize = meleeImpactWorldSize,
                SortingOrder = sortingOrder
            };
        }

        private RenBulletShotSettings BuildBulletSettings()
        {
            return new RenBulletShotSettings
            {
                Head = bulletHeadSprite,
                Trail = bulletTrailSprite,
                Impact = bulletImpactSprite,
                FlightFrames = bulletFlightFrames,
                AdditiveMaterial = ResolveAdditiveMaterial(),
                TravelSeconds = bulletTravelSeconds,
                TravelSpeed = bulletTravelSpeed,
                ImpactSeconds = bulletImpactSeconds,
                HeadWorldSize = bulletHeadWorldSize,
                TrailHeight = bulletTrailHeight,
                SortingOrder = sortingOrder
            };
        }

        private Material ResolveAdditiveMaterial()
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
                name = "RenSkillVfxAdditive_Runtime",
                hideFlags = HideFlags.HideAndDontSave
            };
            return _runtimeAdditive;
        }

        [ContextMenu("Assign Default Ren VFX From Resources")]
        public void EnsureDefaults()
        {
            if (!loadResourcesFallback)
            {
                return;
            }

            if (meleeArcSprite == null)
            {
                meleeArcSprite = LoadSprite("ren_melee_arc_v1");
            }

            if (meleeImpactSprite == null)
            {
                meleeImpactSprite = LoadSprite("ren_melee_impact_v1");
            }

            if (bulletHeadSprite == null)
            {
                bulletHeadSprite = LoadSprite("ren_bullet_head_v1");
            }

            if (bulletTrailSprite == null)
            {
                bulletTrailSprite = LoadSprite("ren_bullet_trail_v1");
            }

            if (bulletImpactSprite == null)
            {
                bulletImpactSprite = LoadSprite("ren_bullet_impact_v1");
            }

            if (glassCrackSprite == null)
            {
                glassCrackSprite = LoadSprite("ren_vfx_glass_crack_v1");
            }

            if (glassShatterSprite == null)
            {
                glassShatterSprite = LoadSprite("ren_vfx_glass_shatter_v1");
            }

            if (ultAuraGlowSprite == null)
            {
                ultAuraGlowSprite = LoadSprite("ren_ult_eerie_aura_glow_v1")
                                    ?? LoadSprite("ren_ult_red_aura_glow_v1");
            }

            if (ultAuraWaveformSprite == null)
            {
                ultAuraWaveformSprite = LoadSprite("ren_ult_eerie_waveforms_v1");
            }

            if (ultAuraNotesSprite == null)
            {
                ultAuraNotesSprite = LoadSprite("ren_ult_eerie_notes_v1")
                                     ?? LoadSprite("ren_ult_red_notes_v1");
            }

            if (ultAuraNoteVariants == null || ultAuraNoteVariants.Length == 0
                || AllNull(ultAuraNoteVariants))
            {
                ultAuraNoteVariants = SliceGridSprites("ren_ult_eerie_notes_v1", 4, 4);
            }

            if (ultAuraWaveVariants == null || ultAuraWaveVariants.Length == 0
                || AllNull(ultAuraWaveVariants))
            {
                ultAuraWaveVariants = SliceWaveformVariants("ren_ult_eerie_waveforms_v1");
            }

            if (bulletFlightFrames == null || bulletFlightFrames.Length == 0
                || AllNull(bulletFlightFrames))
            {
                bulletFlightFrames = new[]
                {
                    LoadSprite("ren_bullet_flight_01_v1"),
                    LoadSprite("ren_bullet_flight_02_v1"),
                    LoadSprite("ren_bullet_flight_03_v1"),
                    LoadSprite("ren_bullet_flight_04_v1")
                };
            }
        }

        private static readonly Rect[] EerieWaveRects =
        {
            new Rect(381f, 422f, 713f, 219f),
            new Rect(848f, 26f, 611f, 300f),
            new Rect(469f, 788f, 537f, 193f),
            new Rect(490f, 591f, 534f, 209f),
            new Rect(1017f, 811f, 473f, 190f),
            new Rect(11f, 761f, 453f, 250f),
            new Rect(313f, 10f, 608f, 224f),
            new Rect(36f, 57f, 297f, 295f),
            new Rect(960f, 649f, 535f, 161f),
            new Rect(242f, 181f, 316f, 188f),
            new Rect(42f, 536f, 182f, 296f),
            new Rect(683f, 165f, 223f, 263f)
        };

        private static Sprite[] SliceWaveformVariants(string fileName)
        {
            var tex = LoadReadableTexture(fileName);
            if (tex == null)
            {
                return null;
            }

            var list = new Sprite[EerieWaveRects.Length];
            for (var i = 0; i < EerieWaveRects.Length; i++)
            {
                var r = EerieWaveRects[i];
                if (r.xMax > tex.width || r.yMax > tex.height)
                {
                    continue;
                }

                list[i] = Sprite.Create(tex, r, new Vector2(0.5f, 0.5f), 100f);
            }

            return list;
        }

        private static Sprite[] SliceGridSprites(string fileName, int cols, int rows)
        {
            var tex = LoadReadableTexture(fileName);
            if (tex == null || cols <= 0 || rows <= 0)
            {
                return null;
            }

            var cellW = tex.width / cols;
            var cellH = tex.height / rows;
            var list = new Sprite[cols * rows];
            var index = 0;
            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < cols; col++)
                {
                    var y = (rows - 1 - row) * cellH;
                    var rect = new Rect(col * cellW, y, cellW, cellH);
                    list[index++] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
                }
            }

            return list;
        }

        private static Texture2D LoadReadableTexture(string fileName)
        {
            var tex = Resources.Load<Texture2D>(ResourceRoot + fileName);
            if (tex == null || !tex.isReadable)
            {
                return null;
            }

            return tex;
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

        private enum RenSkillPresentation
        {
            None,
            MeleeStock,
            SingleBullet,
            MultiBullet
        }
    }
}

using System;
using System.Collections;
using FracturedChorus.Audio;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public class CharlotteSkillChoreographer : MonoBehaviour
    {
        private const string ResourceRoot = "VFX/Combat/Charlotte/";

        [SerializeField] private Transform vfxParent;
        [SerializeField] private Sprite hitSprite;
        [SerializeField] private Sprite domeRingSprite;
        [SerializeField] private Sprite[] domeWaveOrbitFrames;
        [SerializeField] private Material additiveMaterial;
        [SerializeField] private int sortingOrder = 44;

        [Header("NorHit")]
        [SerializeField] private float norHitStandoffX = 1.05f;
        [SerializeField] private float norHitLungeSpeed = 30f;
        [SerializeField] private float norHitLungeSeconds = 0.16f;
        [SerializeField] private float norHitRetreatSeconds = 0.18f;
        [SerializeField] [Range(0.05f, 0.95f)] private float norHitImpactNormalized = 0.38f;
        [SerializeField] private float personalShieldWorldSize = 3.54f;
        [SerializeField] private float personalShieldHeightOffset = 0.39f;
        [SerializeField] private float personalOrbitRadius = 0.93f;
        [SerializeField] private Sprite[] personalNoteSprites;

        [Header("Skill 2 — Pierce slide")]
        [SerializeField] private float skill2PiercePastX = 1.8f;
        [SerializeField] private float skill2SlideSpeed = 26f;
        [SerializeField] private float skill2SlideSeconds = 0.32f;
        [SerializeField] [Range(0.1f, 0.9f)] private float skill2HitNormalized = 0.45f;

        [Header("Ultimate")]
        [SerializeField] private float ultWindupSeconds = 0.48f;
        [SerializeField] private float ultStandoffX = 0.95f;
        [SerializeField] private float ultLungeSpeed = 22f;
        [SerializeField] private float ultLungeSeconds = 0.2f;
        [SerializeField] private float ultAnimSampleRate = 20f;
        [SerializeField] private int ultImpactFrame = 7;
        [SerializeField] private float ultImpactHoldSeconds = 0.18f;
        [SerializeField] private float ultBossKnockDistance = 3.25f;
        [SerializeField] private float ultBossKnockSeconds = 0.34f;
        [SerializeField] private float ultDomeWorldSize = 7.6f;
        [SerializeField] private float ultDomeXOffset = -2.39f;
        [SerializeField] private float ultDomeHeightOffset = 1.64f;
        [SerializeField] private float ultDomeHoldSeconds = 0.9f;
        [SerializeField] private float ultDomeWaveFps = 24f;
        [SerializeField] private float ultDomeWaveSizeScale = 1.22f;
        [SerializeField] private Sprite ultSmashImpactSprite;
        [SerializeField] private Sprite[] ultRockSprites;
        [SerializeField] private float ultSmashWorldSize = 5.2f;
        [SerializeField] private float ultSmashSeconds = 0.48f;
        [SerializeField] private float ultRockWorldSize = 0.72f;
        [SerializeField] private int ultRockCount = 14;
        [SerializeField] private float ultRockBurstSeconds = 0.78f;
        [SerializeField] private float ultAftermathHoldSeconds = 0.4f;

        [Header("Hit VFX")]
        [SerializeField] private float hitWorldSize = 2.1f;
        [SerializeField] private float hitSeconds = 0.2f;

        private Material _runtimeAdditive;
        private UnitView _pendingDomeCarrier;
        private float _pendingDomeHoldSeconds;
        private bool _pendingPartyDome;

        public bool Handles(SkillDefinitionSO skill, UnitView sourceView = null)
        {
            if (skill == null)
            {
                return false;
            }

            if (IsCharlotteSkillId(skill.skillId))
            {
                return true;
            }

            if (!CharlotteCounterShieldView.IsCharlotteUnit(
                    sourceView != null ? sourceView.Unit : null, sourceView))
            {
                return false;
            }

            return skill.slotKind == SkillSlotKind.BasicAttack
                   || skill.slotKind == SkillSlotKind.Skill
                   || skill.slotKind == SkillSlotKind.Ultimate;
        }

        private static bool IsCharlotteSkillId(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            return skillId.StartsWith("Charlott", StringComparison.OrdinalIgnoreCase)
                   || skillId.StartsWith("charlotte", StringComparison.OrdinalIgnoreCase)
                   || skillId is "tank_basic" or "tank_skill" or "tank_ult";
        }

        public IEnumerator PlaySkillRoutine(
            UnitView charlotte,
            UnitView boss,
            SkillDefinitionSO skill,
            bool returnHome = true,
            Action onImpact = null)
        {
            if (charlotte == null || boss == null || skill == null || !Handles(skill, charlotte))
            {
                yield break;
            }

            EnsureDefaults();
            yield return CharlotteCounterShieldView.DismissAllAndWait();

            switch (skill.slotKind)
            {
                case SkillSlotKind.BasicAttack:
                    yield return PlayNorHit(charlotte, boss, skill, returnHome, onImpact);
                    break;
                case SkillSlotKind.Skill:
                    yield return PlayPierceSlide(charlotte, boss, skill, returnHome, onImpact);
                    break;
                case SkillSlotKind.Ultimate:
                    yield return PlayUltimate(charlotte, boss, skill, returnHome, onImpact);
                    break;
                default:
                    yield return PlayNorHit(charlotte, boss, skill, returnHome, onImpact);
                    break;
            }
        }

        private IEnumerator PlayNorHit(
            UnitView charlotte,
            UnitView boss,
            SkillDefinitionSO skill,
            bool returnHome,
            Action onImpact)
        {
            var home = ResolveHome(charlotte);
            if (returnHome)
            {
                charlotte.CaptureAnchor();
            }

            var strikeFeet = ResolveStandoffFeet(charlotte, boss, norHitStandoffX);
            if (charlotte.TryBeginCombatTravelTo(strikeFeet))
            {
                yield return charlotte.MoveFeetToRoutine(
                    strikeFeet,
                    ResolveMoveSeconds(charlotte.FeetWorldPosition, strikeFeet, norHitLungeSpeed, norHitLungeSeconds));
            }

            charlotte.ArriveAtCombatCell();
            charlotte.PlayAttackAnimationHold(skill);
            var clip = Mathf.Max(0.2f, charlotte.EstimateSkillClipLength(skill));
            var impactAt = clip * norHitImpactNormalized;
            if (impactAt > 0f)
            {
                yield return new WaitForSeconds(impactAt);
            }

            SpawnHitVfx(charlotte, boss, skill);
            boss.PlayBeCounteredHold();
            onImpact?.Invoke();
            SpawnPersonalShield(charlotte, skill);

            var tail = clip * (1f - norHitImpactNormalized);
            if (tail > 0f)
            {
                yield return new WaitForSeconds(tail);
            }

            if (!returnHome)
            {
                yield break;
            }

            yield return ReturnHome(charlotte, home, norHitRetreatSeconds);
        }

        private IEnumerator PlayPierceSlide(
            UnitView charlotte,
            UnitView boss,
            SkillDefinitionSO skill,
            bool returnHome,
            Action onImpact)
        {
            var home = ResolveHome(charlotte);
            if (returnHome)
            {
                charlotte.CaptureAnchor();
            }

            var bossFeet = boss.FeetWorldPosition;
            var fromFeet = charlotte.FeetWorldPosition;
            var dir = Mathf.Sign(bossFeet.x - fromFeet.x);
            if (Mathf.Approximately(dir, 0f))
            {
                dir = 1f;
            }

            var startFeet = new Vector3(bossFeet.x - dir * 1.35f, bossFeet.y, fromFeet.z);
            var endFeet = new Vector3(bossFeet.x + dir * skill2PiercePastX, bossFeet.y, fromFeet.z);

            if (charlotte.TryBeginCombatTravelTo(startFeet))
            {
                yield return charlotte.MoveFeetToRoutine(
                    startFeet,
                    ResolveMoveSeconds(fromFeet, startFeet, skill2SlideSpeed, 0.12f));
            }

            charlotte.ArriveAtCombatCell();
            FindAnyObjectByType<CombatSfxController>()?.PlayCharlotteSkill2Dash();
            charlotte.PlayAttackAnimationHold(skill);
            var slideSeconds = Mathf.Max(
                0.12f,
                ResolveMoveSeconds(startFeet, endFeet, skill2SlideSpeed, skill2SlideSeconds));
            var hitAt = slideSeconds * skill2HitNormalized;
            var slide = StartCoroutine(charlotte.MoveFeetToRoutine(endFeet, slideSeconds));

            if (hitAt > 0f)
            {
                yield return new WaitForSeconds(hitAt);
            }

            SpawnHitVfx(charlotte, boss, skill);
            boss.PlayBeCounteredHold();
            onImpact?.Invoke();

            if (slide != null)
            {
                yield return slide;
            }

            if (returnHome)
            {
                SnapToHomeImmediate(charlotte, home);
                yield break;
            }

            var clearFeet = ResolveStandoffFeet(charlotte, boss, norHitStandoffX);
            yield return charlotte.MoveFeetToRoutine(
                clearFeet,
                ResolveMoveSeconds(charlotte.FeetWorldPosition, clearFeet, skill2SlideSpeed, 0.12f));
            charlotte.PlayIdleState();
        }

        private IEnumerator PlayUltimate(
            UnitView charlotte,
            UnitView boss,
            SkillDefinitionSO skill,
            bool returnHome,
            Action onImpact)
        {
            var home = ResolveHome(charlotte);
            var bossHome = ResolveHome(boss);
            if (returnHome)
            {
                charlotte.CaptureAnchor();
            }

            if (ultWindupSeconds > 0f)
            {
                yield return new WaitForSeconds(ultWindupSeconds);
            }

            var strikeFeet = ResolveStandoffFeet(charlotte, boss, ultStandoffX);
            if (charlotte.TryBeginCombatTravelTo(strikeFeet))
            {
                yield return charlotte.MoveFeetToRoutine(
                    strikeFeet,
                    ResolveMoveSeconds(charlotte.FeetWorldPosition, strikeFeet, ultLungeSpeed, ultLungeSeconds));
            }

            charlotte.ArriveAtCombatCell();
            yield return EncounterDirector.PresentArmedCaster();
            charlotte.PlayAttackAnimationHold(skill);
            var impactAt = ResolveUltImpactSeconds();
            if (impactAt > 0f)
            {
                yield return new WaitForSeconds(impactAt);
            }

            SpawnUltSmashVfx(charlotte, boss, skill);
            CombatImpactFeel.PunchUltimateNow();
            if (!EncounterDirector.TryQueueArmedVictimHit(() => onImpact?.Invoke()))
            {
                onImpact?.Invoke();
            }

            yield return EncounterDirector.WaitArmedVictimFocus();

            var holdSeconds = Mathf.Max(ResolveShieldHoldSeconds(skill), ultDomeHoldSeconds);
            if (EncounterDirector.IsPresenting)
            {
                QueuePartyDome(charlotte, holdSeconds);
            }
            else
            {
                SpawnPartyDome(charlotte, holdSeconds);
            }

            var knockDir = Mathf.Sign(boss.FeetWorldPosition.x - charlotte.FeetWorldPosition.x);
            if (Mathf.Approximately(knockDir, 0f))
            {
                knockDir = 1f;
            }

            var knockFeet = boss.FeetWorldPosition + Vector3.right * (knockDir * ultBossKnockDistance);
            yield return boss.MoveFeetToRoutine(
                knockFeet,
                ResolveMoveSeconds(boss.FeetWorldPosition, knockFeet, 14f, ultBossKnockSeconds));

            if (!returnHome)
            {
                charlotte.PlayIdleState();
                yield break;
            }

            yield return new WaitForSeconds(1f);
            yield return ReturnHome(charlotte, home, norHitRetreatSeconds);
            yield return boss.MoveFeetToRoutine(
                bossHome,
                ResolveMoveSeconds(boss.FeetWorldPosition, bossHome, 10f, 0.42f));
            boss.FinishCombatPhaseIdle();
        }

        private float ResolveUltImpactSeconds()
        {
            var fps = Mathf.Max(1f, ultAnimSampleRate);
            var frame = Mathf.Max(0, ultImpactFrame);
            return frame / fps;
        }

        private void SpawnUltSmashVfx(UnitView charlotte, UnitView boss, SkillDefinitionSO skill)
        {
            EnsureDefaults();
            FindAnyObjectByType<CombatSfxController>()?.PlaySkillSfxImmediate(skill);

            if (charlotte == null || boss == null)
            {
                return;
            }

            var contact = boss.FeetWorldPosition + Vector3.up * 0.85f;
            var away = boss.FeetWorldPosition.x - charlotte.FeetWorldPosition.x;
            var burstDir = new Vector2(Mathf.Approximately(away, 0f) ? 1f : Mathf.Sign(away), 0.2f);
            var settings = new CharlotteUltSmashSettings
            {
                Impact = ultSmashImpactSprite != null ? ultSmashImpactSprite : hitSprite,
                Rocks = ultRockSprites,
                AdditiveMaterial = ResolveAdditive(),
                ImpactWorldSize = Mathf.Max(1.5f, ultSmashWorldSize),
                ImpactSeconds = Mathf.Max(0.12f, ultSmashSeconds),
                RockWorldSize = Mathf.Max(0.2f, ultRockWorldSize),
                RockCount = Mathf.Clamp(ultRockCount, 4, 16),
                RockBurstSeconds = Mathf.Max(0.2f, ultRockBurstSeconds),
                SortingOrder = sortingOrder + 2,
                BurstDir = burstDir
            };
            CharlotteUltSmashView.Spawn(
                contact,
                settings,
                vfxParent != null ? vfxParent : transform);
        }

        private void SpawnHitVfx(UnitView charlotte, UnitView boss, SkillDefinitionSO skill = null)
        {
            EnsureDefaults();
            var sfx = FindAnyObjectByType<CombatSfxController>();
            if (skill != null && skill.slotKind == SkillSlotKind.Skill)
            {
                sfx?.PlayClashHit();
            }
            else
            {
                sfx?.PlaySkillSfxImmediate(skill);
            }

            if (hitSprite == null || charlotte == null || boss == null)
            {
                return;
            }

            var contact = boss.FeetWorldPosition + Vector3.up * 0.7f;
            var from = charlotte.FeetWorldPosition + Vector3.up * 0.65f;
            var settings = new RenMeleeStrikeSettings
            {
                Arc = hitSprite,
                Impact = hitSprite,
                AdditiveMaterial = ResolveAdditive(),
                ArcSeconds = hitSeconds * 0.45f,
                ImpactSeconds = hitSeconds,
                ArcWorldSize = hitWorldSize * 0.85f,
                ImpactWorldSize = hitWorldSize,
                SortingOrder = sortingOrder
            };
            RenMeleeStrikeView.Spawn(from, contact, settings, vfxParent != null ? vfxParent : transform);
        }

        public void ApplyDomeTuning(float worldSize, float xOffset, float heightOffset)
        {
            ultDomeWorldSize = Mathf.Max(0.2f, worldSize);
            ultDomeXOffset = xOffset;
            ultDomeHeightOffset = heightOffset;
        }

        public void ApplyPersonalShieldTuning(float worldSize, float heightOffset, float orbitRadius)
        {
            personalShieldWorldSize = Mathf.Max(0.2f, worldSize);
            personalShieldHeightOffset = heightOffset;
            personalOrbitRadius = Mathf.Max(0.35f, orbitRadius);
        }

        private void SpawnPersonalShield(UnitView charlotte, SkillDefinitionSO skill)
        {
            EnsureDefaults();
            if (charlotte == null)
            {
                return;
            }

            if (skill == null || !skill.grantTimedShield || skill.timedShieldAllAllies)
            {
                return;
            }

            if ((domeWaveOrbitFrames == null || AllNull(domeWaveOrbitFrames))
                && (personalNoteSprites == null || AllNull(personalNoteSprites)))
            {
                return;
            }

            var tuning = CharlotteShieldTuning.Resolve();
            var size = tuning != null ? tuning.PersonalWorldSize : personalShieldWorldSize;
            var height = tuning != null ? tuning.PersonalHeightOffset : personalShieldHeightOffset;
            var orbit = tuning != null ? tuning.PersonalOrbitRadius : personalOrbitRadius;
            var center = charlotte.FeetWorldPosition + Vector3.up * height;
            var settings = new CharlotteMusicOrbitShieldSettings
            {
                WaveFrames = domeWaveOrbitFrames,
                NoteSprites = personalNoteSprites,
                AdditiveMaterial = ResolveAdditive(),
                WorldSize = Mathf.Max(0.2f, size),
                OrbitRadius = Mathf.Max(0.35f, orbit),
                WaveCount = 6,
                NoteCount = 8,
                WaveFps = ultDomeWaveFps,
                HoldSeconds = ResolveShieldHoldSeconds(skill),
                FadeSeconds = 0.2f,
                SortingOrder = sortingOrder - 1,
                Tint = new Color(1f, 0.85f, 0.3f, 1f)
            };
            CharlotteMusicOrbitShieldView.Spawn(
                charlotte.transform,
                center,
                settings,
                vfxParent != null ? vfxParent : transform);
        }

        private void QueuePartyDome(UnitView charlotte, float holdSeconds)
        {
            var hold = Mathf.Max(0.05f, holdSeconds);
            if (_pendingPartyDome)
            {
                _pendingDomeHoldSeconds += hold;
                if (charlotte != null)
                {
                    _pendingDomeCarrier = charlotte;
                }

                return;
            }

            _pendingPartyDome = true;
            _pendingDomeCarrier = charlotte;
            _pendingDomeHoldSeconds = hold;
        }

        public void FlushPendingPartyDome()
        {
            if (!_pendingPartyDome)
            {
                return;
            }

            _pendingPartyDome = false;
            var carrier = _pendingDomeCarrier;
            var hold = _pendingDomeHoldSeconds;
            _pendingDomeCarrier = null;
            if (carrier != null)
            {
                SpawnPartyDome(carrier, hold);
            }
        }

        private void SpawnPartyDome(UnitView charlotte, float holdSeconds = -1f)
        {
            EnsureDefaults();
            if (domeRingSprite == null || charlotte == null)
            {
                return;
            }

            if (domeWaveOrbitFrames == null || domeWaveOrbitFrames.Length == 0
                || AllNull(domeWaveOrbitFrames))
            {
                EnsureDefaults();
            }

            var tuning = CharlotteShieldTuning.Resolve();
            var size = tuning != null ? tuning.DomeWorldSize : ultDomeWorldSize;
            var x = tuning != null ? tuning.DomeXOffset : ultDomeXOffset;
            var height = tuning != null ? tuning.DomeHeightOffset : ultDomeHeightOffset;
            var center = charlotte.FeetWorldPosition + new Vector3(x, height, 0f);
            var hold = holdSeconds > 0f ? holdSeconds : ultDomeHoldSeconds;
            CharlotteDomeRingView.SpawnOrExtend(
                charlotte.transform,
                center,
                domeRingSprite,
                ResolveAdditive(),
                size,
                hold,
                0.22f,
                sortingOrder - 2,
                vfxParent != null ? vfxParent : transform,
                domeWaveOrbitFrames,
                ultDomeWaveFps,
                ultDomeWaveSizeScale);
        }

        private static float ResolveShieldHoldSeconds(SkillDefinitionSO skill)
        {
            var beatSec = 60f / TimelineConstants.BossRemixBpm;
            if (skill != null && skill.timedShieldUntilPhaseEnd)
            {
                return TimelineConstants.LaterPhaseSlotCount * beatSec;
            }

            if (skill != null && skill.timedShieldDurationBeats > 0)
            {
                return skill.timedShieldDurationBeats * beatSec;
            }

            return 0.55f;
        }

        private static Vector3 ResolveStandoffFeet(UnitView charlotte, UnitView boss, float standoffX)
        {
            var bossFeet = boss.FeetWorldPosition;
            var charlotteFeet = charlotte.FeetWorldPosition;
            var dir = Mathf.Sign(bossFeet.x - charlotteFeet.x);
            if (Mathf.Approximately(dir, 0f))
            {
                dir = 1f;
            }

            return new Vector3(bossFeet.x - dir * standoffX, bossFeet.y, charlotteFeet.z);
        }

        private static Vector3 ResolveHome(UnitView view)
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

        private IEnumerator ReturnHome(UnitView charlotte, Vector3 home, float seconds)
        {
            if (!charlotte.IsRootNear(home))
            {
                charlotte.PlayMovingLoop();
                yield return charlotte.MoveToRoutine(home, seconds);
            }

            charlotte.RestoreTravelFacing();
            charlotte.transform.position = new Vector3(home.x, home.y, charlotte.transform.position.z);
            charlotte.CaptureAnchor();
            charlotte.FinishCombatPhaseIdle();
        }

        private static void SnapToHomeImmediate(UnitView view, Vector3 home)
        {
            if (view == null)
            {
                return;
            }

            view.SnapFeetTo(home, view.transform.position.z);
            view.CaptureAnchor();
            if (view.Unit != null && !view.Unit.IsAlive)
            {
                view.PlayDeathAnimation();
                return;
            }

            view.PlayIdleState();
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

        public void EnsureDefaults()
        {
            if (hitSprite == null)
            {
                hitSprite = LoadSprite("charlotte_vfx_melee_hit_v1");
            }

            if (ultSmashImpactSprite == null)
            {
                ultSmashImpactSprite = LoadSprite("charlotte_vfx_ult_smash_impact_v1")
                                       ?? hitSprite;
            }

            if (ultRockSprites == null || ultRockSprites.Length == 0 || AllNull(ultRockSprites))
            {
                ultRockSprites = LoadUltRockSprites();
            }

            if (domeRingSprite == null)
            {
                domeRingSprite = LoadSprite("charlotte_vfx_dome_ring_v1");
            }

            if (domeWaveOrbitFrames == null || domeWaveOrbitFrames.Length == 0
                || AllNull(domeWaveOrbitFrames))
            {
                domeWaveOrbitFrames = new[]
                {
                    LoadSprite("charlotte_vfx_dome_wave_orbit_f1"),
                    LoadSprite("charlotte_vfx_dome_wave_orbit_f2"),
                    LoadSprite("charlotte_vfx_dome_wave_orbit_f3"),
                    LoadSprite("charlotte_vfx_dome_wave_orbit_f4")
                };
            }

            if (personalNoteSprites == null || personalNoteSprites.Length == 0
                || AllNull(personalNoteSprites))
            {
                personalNoteSprites = LoadNoteSprites();
            }
        }

        private Sprite[] LoadUltRockSprites()
        {
            var path = ResourceRoot + "charlotte_vfx_ult_rock_debris_v1";
            var sliced = Resources.LoadAll<Sprite>(path);
            if (sliced != null && sliced.Length > 1)
            {
                return sliced;
            }

            var tex = Resources.Load<Texture2D>(path);
            if (tex == null)
            {
                return null;
            }

            var fromSheet = CharlotteUltSmashView.SliceRockSheet(tex);
            if (fromSheet != null && fromSheet.Length > 0)
            {
                return fromSheet;
            }

            var single = Resources.Load<Sprite>(path);
            return single != null ? new[] { single } : null;
        }

        private Sprite[] LoadNoteSprites()
        {
            var scatter = LoadSprite("charlotte_vfx_note_scatter_v1");
            var renNotes = Resources.LoadAll<Sprite>("VFX/Combat/Ren/ren_ult_eerie_notes_v1");
            if (renNotes != null && renNotes.Length > 1)
            {
                return renNotes;
            }

            var renSingle = Resources.Load<Sprite>("VFX/Combat/Ren/ren_ult_eerie_notes_v1")
                            ?? Resources.Load<Sprite>("VFX/Combat/Ren/ren_ult_red_notes_v1");
            if (scatter != null && renSingle != null)
            {
                return new[] { scatter, renSingle };
            }

            if (scatter != null)
            {
                return new[] { scatter };
            }

            return renSingle != null ? new[] { renSingle } : null;
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
                name = "CharlotteSkillAdditive_Runtime",
                hideFlags = HideFlags.HideAndDontSave
            };
            return _runtimeAdditive;
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

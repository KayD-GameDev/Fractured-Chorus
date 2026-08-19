using FracturedChorus.Audio;
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Presentation;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Unit in scene — grid row/column assigned at runtime when placed on a honeycomb cell.
    /// Root BoxCollider2D = pointer hit target for click (skill panel) and drag (reposition).
    /// Child FeetAnchor = snap point only (Transform, no collider).
    /// </summary>
    public class UnitView : MonoBehaviour
    {
        private const string FeetAnchorObjectName = "FeetAnchor";

        [Header("Unit Data")]
        [SerializeField] private UnitPresetSO preset;
        [Tooltip("Used when Preset asset is not assigned — survives scene save")]
        [SerializeField] private string demoUnitKey = "ren";
        [SerializeField] private GridSide side = GridSide.Player;
        [SerializeField] private int row = HoneycombIndex.Unplaced;
        [SerializeField] private int column = HoneycombIndex.Unplaced;

        [Header("Scene References (optional — auto-created if empty)")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TextMesh hpLabel;
        [FormerlySerializedAs("clickCollider")]
        [SerializeField] private BoxCollider2D bodyCollider;
        [SerializeField] private UnitFeetAnchor feetAnchor;
        [SerializeField] private Animator animator;
        [Header("Combat poses — Counter is also Guard")]
        [SerializeField] private string counterStateName;
        [SerializeField, HideInInspector, FormerlySerializedAs("guardStateName")]
        private string guardStateName;
        [SerializeField] private string beCounteredStateName;
        [SerializeField] private string idleStateName;
        [SerializeField] private string movingStateName;
        [SerializeField] private string deathStateName;
        [Header("Attack clips — 1 Normal Hit, 2 Skill, 3 Ult")]
        [SerializeField] private string normalHitStateName;
        [FormerlySerializedAs("skillHitStateName")]
        [SerializeField] private string skillStateName;
        [FormerlySerializedAs("ultHitStateName")]
        [SerializeField] private string ultStateName;
        [SerializeField] [Range(0f, 1f)] private float hitRetriggerNormalizedTime = 0.35f;
        [Tooltip("Keep sprite/color/Transform scale authored in the scene.")]
        [SerializeField] private bool preserveSceneVisuals = true;
        [Tooltip("Keep BoxCollider2D size/offset authored in the scene — used as click + drag area.")]
        [SerializeField] private bool preserveSceneCollider = true;
        [SerializeField] private bool showWorldHpLabel;

        public CombatUnit Unit { get; private set; }
        public UnitPresetSO Preset => preset;
        public string DemoUnitKey => demoUnitKey;
        public UnitFeetAnchor FeetAnchor => feetAnchor;
        public SpriteRenderer BodySpriteRenderer
        {
            get
            {
                ResolveSpriteRendererReference();
                return spriteRenderer;
            }
        }

        public BoxCollider2D BodyCollider
        {
            get
            {
                ResolveBodyColliderReference();
                return bodyCollider;
            }
        }

        public void ApplyBodyColliderShape(Vector2 size, Vector2 offset)
        {
            ResolveBodyColliderReference();
            if (bodyCollider == null || size.x <= 0.001f || size.y <= 0.001f)
            {
                return;
            }

            bodyCollider.size = size;
            bodyCollider.offset = offset;
        }

        public void EnsureDefaultCombatAnimStates()
        {
            MergeGuardIntoCounter();
            var key = demoUnitKey ?? string.Empty;
            switch (key)
            {
                case "ren":
                    SetAnimStateIfEmpty(ref idleStateName, "Ren Idle");
                    SetAnimStateIfEmpty(ref movingStateName, "Ren Moving");
                    SetAnimStateIfEmpty(ref beCounteredStateName, "Ren Hurt");
                    SetAnimStateIfEmpty(ref counterStateName, "Ren Counter");
                    SetAnimStateIfEmpty(ref deathStateName, "Ren Death");
                    SetAnimStateIfEmpty(ref normalHitStateName, "Ren Skill 1");
                    SetAnimStateIfEmpty(ref skillStateName, "Ren - Skill 2");
                    SetAnimStateIfEmpty(ref ultStateName, "Ren - Skill 3");
                    break;
                case "mage":
                    SetAnimStateIfEmpty(ref idleStateName, "Coda - Idle");
                    SetAnimStateIfEmpty(ref counterStateName, "Coda - Counter");
                    SetAnimStateIfEmpty(ref beCounteredStateName, "Coda - Hurt");
                    SetAnimStateIfEmpty(ref movingStateName, "Coda - Moving");
                    SetAnimStateIfEmpty(ref deathStateName, "Coda - Death");
                    SetAnimStateIfEmpty(ref normalHitStateName, "Coda Skill 1");
                    SetAnimStateIfEmpty(ref skillStateName, "Coda Skill 2");
                    SetAnimStateIfEmpty(ref ultStateName, "Coda Skill 3");
                    break;
                case "Charlott":
                case "charlotte":
                case "tank":
                    SetAnimStateIfEmpty(ref idleStateName, "Charlott_Idle");
                    SetAnimStateIfEmpty(ref counterStateName, "Charlott_Counter");
                    SetAnimStateIfEmpty(ref beCounteredStateName, "Charlott_Hurt");
                    SetAnimStateIfEmpty(ref movingStateName, "Charlott_Moving");
                    SetAnimStateIfEmpty(ref deathStateName, "Charlott_Death");
                    SetAnimStateIfEmpty(ref normalHitStateName, "Charlott_NorHit");
                    SetAnimStateIfEmpty(ref skillStateName, "Charlott_Skill");
                    SetAnimStateIfEmpty(ref ultStateName, "Charlott_Ultimate");
                    break;
                case "grunt_left":
                    SetAnimStateIfEmpty(ref beCounteredStateName, "Mini 1 - Hurt");
                    SetAnimStateIfEmpty(ref idleStateName, "Mini 1 -Idle");
                    SetAnimStateIfEmpty(ref movingStateName, "Mini 1 - Moving");
                    SetAnimStateIfEmpty(ref deathStateName, "Mini 1 - Death");
                    break;
                case "grunt_right":
                    SetAnimStateIfEmpty(ref beCounteredStateName, "Mini 2 - Hurt");
                    SetAnimStateIfEmpty(ref idleStateName, "Mini 2 - Idle");
                    SetAnimStateIfEmpty(ref movingStateName, "Mini 2 - Moving");
                    SetAnimStateIfEmpty(ref deathStateName, "Mini 2 - Death");
                    break;
                case CombatEnemyKeys.Enemy1:
                    SetAnimStateIfEmpty(ref beCounteredStateName, "Enemy 1 - Hurt");
                    SetAnimStateIfEmpty(ref idleStateName, "Enemy 1 - Idle");
                    SetAnimStateIfEmpty(ref movingStateName, "Enemy 1 - Moving");
                    SetAnimStateIfEmpty(ref counterStateName, "Enemy 1 - Guard");
                    SetAnimStateIfEmpty(ref deathStateName, "Enemy 1 - Death");
                    SetAnimStateIfEmpty(ref normalHitStateName, "Enemy 1 - Guard");
                    SetAnimStateIfEmpty(ref skillStateName, "Enemy 1 - Guard");
                    break;
                case CombatEnemyKeys.Enemy2:
                    SetAnimStateIfEmpty(ref beCounteredStateName, "Enemy 2 - Hurt");
                    SetAnimStateIfEmpty(ref idleStateName, "Enemy 2 - Idle");
                    SetAnimStateIfEmpty(ref movingStateName, "Enemy 2 - Moving");
                    SetAnimStateIfEmpty(ref deathStateName, "Enemy 2 - Dead");
                    SetAnimStateIfEmpty(ref normalHitStateName, "Enemy 2 - Skill");
                    SetAnimStateIfEmpty(ref skillStateName, "Enemy 2 - Skill");
                    break;
                case CombatEnemyKeys.Enemy3:
                    SetAnimStateIfEmpty(ref beCounteredStateName, "Enemy 3 - Hurt");
                    SetAnimStateIfEmpty(ref idleStateName, "Enemy 3 - Idle");
                    SetAnimStateIfEmpty(ref movingStateName, "Enemy 3 - Moving");
                    SetAnimStateIfEmpty(ref deathStateName, "Enemy 3 - Dead");
                    SetAnimStateIfEmpty(ref normalHitStateName, "Enemy 3 - Skill 1");
                    SetAnimStateIfEmpty(ref skillStateName, "Enemy 3 - Skill 1");
                    SetAnimStateIfEmpty(ref ultStateName, "Enemy 3 - Skill 2");
                    break;
                case CombatEnemyKeys.Elite1:
                    SetAnimStateIfEmpty(ref beCounteredStateName, "Elite 1 -Hurt Sprite");
                    SetAnimStateIfEmpty(ref idleStateName, "Elite 1 -Idle Sprite");
                    SetAnimStateIfEmpty(ref movingStateName, "Elite 1 -Moving Sprite");
                    SetAnimStateIfEmpty(ref counterStateName, "Elite 1 -Guard Sprite");
                    SetAnimStateIfEmpty(ref deathStateName, "Elite 1 - Death Sprite");
                    SetAnimStateIfEmpty(ref normalHitStateName, "Elite 1 - Skill Sprite");
                    SetAnimStateIfEmpty(ref skillStateName, "Elite 1 - Skill Sprite");
                    SetAnimStateIfEmpty(ref ultStateName, "Elite 1 - Skill 2 Sprite");
                    break;
                case CombatEnemyKeys.Elite2:
                    SetAnimStateIfEmpty(ref beCounteredStateName, "Elite 2 - Hurt Sprite");
                    SetAnimStateIfEmpty(ref idleStateName, "Elite 2 - Idle Sprite");
                    SetAnimStateIfEmpty(ref movingStateName, "Elite 2 - Moving Sprite");
                    SetAnimStateIfEmpty(ref counterStateName, "Elite 2 - Guard Sprite");
                    SetAnimStateIfEmpty(ref deathStateName, "Elite 2 - Death Sprite");
                    SetAnimStateIfEmpty(ref normalHitStateName, "Elite 2 - Skill Sprite");
                    SetAnimStateIfEmpty(ref skillStateName, "Elite 2 - Skill Sprite");
                    break;
                case CombatEnemyKeys.Elite3:
                    SetAnimStateIfEmpty(ref beCounteredStateName, "Elite 3 - Hurt");
                    SetAnimStateIfEmpty(ref idleStateName, "Elite 3 - Idle");
                    SetAnimStateIfEmpty(ref movingStateName, "Elite 3 - Moving");
                    SetAnimStateIfEmpty(ref counterStateName, "Elite 3 - Guard");
                    SetAnimStateIfEmpty(ref deathStateName, "Elite 3 - Hurt");
                    SetAnimStateIfEmpty(ref normalHitStateName, "Elite 3 - Skill");
                    SetAnimStateIfEmpty(ref skillStateName, "Elite 3 - Skill");
                    SetAnimStateIfEmpty(ref ultStateName, "Elite 3 - Skill 2");
                    break;
                case "boss_despair":
                    SetAnimStateIfEmpty(ref beCounteredStateName, "Boss - Be Countered");
                    SetAnimStateIfEmpty(ref idleStateName, "Boss Idle");
                    SetAnimStateIfEmpty(ref movingStateName, "Boss - Moving");
                    SetAnimStateIfEmpty(ref deathStateName, "Boss - Death");
                    break;
                case "kiki_ueda":
                case "kiki":
                    SetAnimStateIfEmpty(ref idleStateName, "Kiki-Idle");
                    SetAnimStateIfEmpty(ref counterStateName, "Kiki-Counter");
                    SetAnimStateIfEmpty(ref beCounteredStateName, "Kiki-Hurt");
                    SetAnimStateIfEmpty(ref movingStateName, "Kiki-Moving");
                    SetAnimStateIfEmpty(ref deathStateName, "Kiki-Death");
                    break;
            }
        }

        private void MergeGuardIntoCounter()
        {
            if (string.IsNullOrWhiteSpace(counterStateName) && !string.IsNullOrWhiteSpace(guardStateName))
            {
                counterStateName = guardStateName;
            }
        }

        private static void SetAnimStateIfEmpty(ref string field, string value)
        {
            if (string.IsNullOrWhiteSpace(field))
            {
                field = value;
            }
        }

        public static UnitView FindForUnit(CombatUnit unit)
        {
            if (unit == null)
            {
                return null;
            }

            // Include inactive: EncounterDirector hides non-participants during duels.
            foreach (var view in Object.FindObjectsByType<UnitView>(FindObjectsInactive.Include))
            {
                if (view != null && view.Unit == unit)
                {
                    return view;
                }
            }

            return null;
        }

        public void PlayCounterAnimation() => PlayCounterRestart();

        public void PlayCounterRestart()
        {
            PlayCounterInternal(0f, scheduleIdle: true);
        }

        public void PlayCounterHold()
        {
            PlayCounterInternal(0f, scheduleIdle: false);
        }

        private void PlayCounterInternal(float normalizedTime, bool scheduleIdle)
        {
            if (TryPlaySimulatorVisual(UnitCombatVisualState.Counter, normalizedTime, scheduleIdle))
            {
                return;
            }

            ResolveAnimatorReference();
            var clip = ResolveCounterClip(out var stateName);
            if (clip == null)
            {
                clip = ResolveGuardClip(out stateName);
            }

            PlayCombatAnimation(clip, stateName, normalizedTime, scheduleIdle);
        }

        public void PlayCounterHitRetrigger()
        {
            if (TryPlaySimulatorVisual(UnitCombatVisualState.Counter, hitRetriggerNormalizedTime, scheduleIdle: true))
            {
                return;
            }

            ResolveAnimatorReference();
            var clip = ResolveCounterClip(out var stateName);
            if (clip == null)
            {
                clip = ResolveGuardClip(out stateName);
            }

            PlayCombatAnimation(clip, stateName, hitRetriggerNormalizedTime, scheduleIdle: true);
        }

        public void PlayCounterBurst() => PlayCounterRestart();

        public void PlayBeCounteredAnimation() => PlayBeCounteredRestart();

        public void PlayBeCounteredRestart()
        {
            PlayBeCounteredInternal(0f, scheduleIdle: true);
        }

        public void PlayBeCounteredHold()
        {
            PlayBeCounteredInternal(0f, scheduleIdle: false);
        }

        private void PlayBeCounteredInternal(float normalizedTime, bool scheduleIdle)
        {
            if (TryPlaySimulatorVisual(UnitCombatVisualState.Hurt, normalizedTime, scheduleIdle))
            {
                return;
            }

            ResolveAnimatorReference();
            var clip = ResolveBeCounteredClip(out var stateName);
            if (string.IsNullOrEmpty(stateName) && !string.IsNullOrEmpty(beCounteredStateName))
            {
                stateName = beCounteredStateName;
            }

            PlayCombatAnimation(clip, stateName, normalizedTime, scheduleIdle);
        }

        public void PlayBeCounteredHitRetrigger()
        {
            if (TryPlaySimulatorVisual(UnitCombatVisualState.Hurt, hitRetriggerNormalizedTime, scheduleIdle: true))
            {
                return;
            }

            ResolveAnimatorReference();
            var clip = ResolveBeCounteredClip(out var stateName);
            PlayCombatAnimation(clip, stateName, hitRetriggerNormalizedTime, scheduleIdle: true);
        }

        public void PlayDeathAnimation()
        {
            RestoreTravelFacing();
            if (TryPlaySimulatorVisual(UnitCombatVisualState.Death, 0f, scheduleIdle: false))
            {
                return;
            }

            ResolveAnimatorReference();
            var clip = ResolveDeathClip(out var stateName);
            if (string.IsNullOrEmpty(stateName) && !string.IsNullOrEmpty(deathStateName))
            {
                stateName = deathStateName;
            }

            if (clip == null && string.IsNullOrEmpty(stateName))
            {
                clip = ResolveBeCounteredClip(out stateName);
            }

            if (string.IsNullOrEmpty(stateName))
            {
                return;
            }

            PlayCombatAnimation(clip, stateName, 0f, scheduleIdle: false);
            SnapDeathSpriteToCell();
        }

        private void SnapDeathSpriteToCell()
        {
            var feet = FeetWorldPosition;
            PositionFeetAnchorAtSpriteBase();
            PlaceFeetAt(feet);
        }

        public void PlayCastHold(SkillDefinitionSO skill = null)
        {
            if (TryPlayAttackVisual(skill, scheduleIdle: false))
            {
                return;
            }

            ResolveAnimatorReference();
            if (skill != null && skill.IsGuard)
            {
                var guard = ResolveGuardClip(out var guardState);
                if (guard != null)
                {
                    PlayCombatAnimation(guard, guardState, 0f, scheduleIdle: false);
                    return;
                }

                PlayCounterHold();
                return;
            }

            var clip = ResolveSkillClip(skill, out var stateName);
            if (clip == null && !string.IsNullOrEmpty(normalHitStateName))
            {
                clip = ResolveClipByKeyword(normalHitStateName, normalHitStateName, out stateName);
            }

            if (clip == null)
            {
                clip = ResolveClipByKeyword(null, "Skill 1", out stateName);
            }

            if (clip == null)
            {
                clip = ResolveClipByKeyword(null, "Skill", out stateName);
            }

            if (clip == null)
            {
                clip = ResolveGuardClip(out stateName);
            }

            if (clip == null && !string.IsNullOrEmpty(normalHitStateName))
            {
                PlayCombatAnimation(null, normalHitStateName, 0f, scheduleIdle: false);
                return;
            }

            if (clip == null)
            {
                PlayCounterHold();
                return;
            }

            PlayCombatAnimation(clip, stateName, 0f, scheduleIdle: false);
        }

        private const float MinCombatPoseHoldSec = 0.4f;

        private Coroutine _combatAnimRoutine;
        private Coroutine _hpPunchRoutine;
        private Vector3 _hpPunchBaseScale = Vector3.one;
        private bool _hpPunchBaseCaptured;
        private Vector3 _anchorPosition;
        private bool _anchorCaptured;
        private Color _baseSpriteColor = Color.white;
        private bool _baseColorCaptured;
        private float _dimFactor = 1f;
        private bool _authoredFlipX;
        private bool _returnFacingActive;

        public const float CombatFeetArriveEpsilon = 0.08f;

        private void PlayCombatAnimation(AnimationClip clip, string stateName, float normalizedTime, bool scheduleIdle)
        {
            if (animator == null || clip == null || string.IsNullOrEmpty(stateName))
            {
                return;
            }

            stateName = stateName.Trim();
            var hash = Animator.StringToHash(stateName);
            if (!HasPlayableAnimatorState(stateName))
            {
                return;
            }

            animator.enabled = true;
            if (_combatAnimRoutine != null)
            {
                StopCoroutine(_combatAnimRoutine);
                _combatAnimRoutine = null;
            }

            animator.Play(hash, 0, Mathf.Clamp01(normalizedTime));

            animator.Update(0.016f);
            if (!scheduleIdle)
            {
                return;
            }

            var t = Mathf.Clamp01(normalizedTime);
            var clipHold = clip != null ? clip.length * (1f - t) : 0f;
            var remaining = Mathf.Max(MinCombatPoseHoldSec, clipHold);
            _combatAnimRoutine = StartCoroutine(ReturnToIdleAfter(remaining));
        }

        private bool TryPlayAttackVisual(SkillDefinitionSO skill, bool scheduleIdle)
        {
            if (skill != null && skill.IsGuard)
            {
                if (TryPlaySimulatorVisual(UnitCombatVisualState.Guard, 0f, scheduleIdle))
                {
                    return true;
                }

                return TryPlaySimulatorVisual(UnitCombatVisualState.Counter, 0f, scheduleIdle);
            }

            var primary = ResolveAttackVisualState(skill);
            if (TryPlaySimulatorVisual(primary, 0f, scheduleIdle))
            {
                return true;
            }

            return primary != UnitCombatVisualState.Skill
                && TryPlaySimulatorVisual(UnitCombatVisualState.Skill, 0f, scheduleIdle);
        }

        private static UnitCombatVisualState ResolveAttackVisualState(SkillDefinitionSO skill)
        {
            if (skill == null)
            {
                return UnitCombatVisualState.Skill;
            }

            return skill.slotKind switch
            {
                SkillSlotKind.BasicAttack => UnitCombatVisualState.NormalHit,
                SkillSlotKind.Skill => UnitCombatVisualState.SkillHit,
                SkillSlotKind.Ultimate => UnitCombatVisualState.UltHit,
                SkillSlotKind.Guard => UnitCombatVisualState.Guard,
                _ => UnitCombatVisualState.Skill
            };
        }

        private AnimationClip ResolveAttackSimulatorClip(SkillDefinitionSO skill)
        {
            if (skill != null && skill.IsGuard)
            {
                return ResolveSimulatorClip(UnitCombatVisualState.Guard)
                    ?? ResolveSimulatorClip(UnitCombatVisualState.Counter);
            }

            var primary = ResolveAttackVisualState(skill);
            return ResolveSimulatorClip(primary) ?? ResolveSimulatorClip(UnitCombatVisualState.Skill);
        }

        private bool TryPlaySimulatorVisual(
            UnitCombatVisualState visualState,
            float normalizedTime,
            bool scheduleIdle,
            UnitSpriteApplyMode mode = UnitSpriteApplyMode.Auto)
        {
            EnsureDefaultCombatAnimStates();
            var sim = GetComponent<UnitSpriteSimulator>();
            if (sim == null || !sim.TryGetLayout(visualState, out var layout))
            {
                return false;
            }

            if (_combatAnimRoutine != null)
            {
                StopCoroutine(_combatAnimRoutine);
                _combatAnimRoutine = null;
            }

            sim.ApplyLayoutForState(visualState, keepWorldFeet: true, mode);
            RecaptureHpPunchBase();

            if (layout.ShouldApplyStill(mode))
            {
                if (scheduleIdle && visualState != UnitCombatVisualState.Idle)
                {
                    _combatAnimRoutine = StartCoroutine(ReturnToIdleAfter(MinCombatPoseHoldSec));
                }

                return true;
            }

            if (mode == UnitSpriteApplyMode.PreferStill)
            {
                return false;
            }

            ResolveAnimatorReference();
            var clip = layout.animationClip;
            var stateName = layout.ClipStateName;
            if (string.IsNullOrEmpty(stateName) || !HasAnimatorState(stateName))
            {
                clip = ResolveFallbackClip(visualState, out stateName);
            }

            if (clip != null && HasPlayableAnimatorState(stateName))
            {
                PlayCombatAnimation(clip, stateName, normalizedTime, scheduleIdle);
                return true;
            }

            if (layout.HasStillSprite)
            {
                sim.ApplyLayoutForState(visualState, keepWorldFeet: true, UnitSpriteApplyMode.PreferStill);
                RecaptureHpPunchBase();
                if (scheduleIdle && visualState != UnitCombatVisualState.Idle)
                {
                    _combatAnimRoutine = StartCoroutine(ReturnToIdleAfter(MinCombatPoseHoldSec));
                }

                return true;
            }

            return false;
        }

        private bool HasAnimatorState(string stateName)
        {
            ResolveAnimatorReference();
            if (animator == null || string.IsNullOrWhiteSpace(stateName))
            {
                return false;
            }

            return animator.HasState(0, Animator.StringToHash(stateName.Trim()));
        }

        private bool HasPlayableAnimatorState(string stateName)
        {
            stateName = string.IsNullOrWhiteSpace(stateName) ? stateName : stateName.Trim();
            if (!HasAnimatorState(stateName) || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            var clips = animator.runtimeAnimatorController.animationClips;
            if (clips == null)
            {
                return false;
            }

            for (var i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null && clips[i].name == stateName)
                {
                    return true;
                }
            }

            return false;
        }

        private AnimationClip ResolveFallbackClip(UnitCombatVisualState visualState, out string stateName)
        {
            switch (visualState)
            {
                case UnitCombatVisualState.Idle:
                    stateName = ResolveIdleStateName();
                    return ResolveClipByKeyword(stateName, "Idle", out stateName);
                case UnitCombatVisualState.Moving:
                    return ResolveMovingClip(out stateName);
                case UnitCombatVisualState.Skill:
                    return ResolveClipByKeyword(skillStateName, "Skill", out stateName);
                case UnitCombatVisualState.NormalHit:
                    return ResolveClipByKeyword(normalHitStateName, "Skill 1", out stateName);
                case UnitCombatVisualState.SkillHit:
                    return ResolveClipByKeyword(skillStateName, "Skill 2", out stateName);
                case UnitCombatVisualState.UltHit:
                    return ResolveClipByKeyword(ultStateName, "Ultimate", out stateName);
                case UnitCombatVisualState.Guard:
                    return ResolveGuardClip(out stateName);
                case UnitCombatVisualState.Counter:
                    return ResolveCounterClip(out stateName);
                case UnitCombatVisualState.Hurt:
                    return ResolveBeCounteredClip(out stateName);
                case UnitCombatVisualState.Death:
                    return ResolveDeathClip(out stateName);
                default:
                    stateName = null;
                    return null;
            }
        }

        private void RecaptureHpPunchBase()
        {
            if (_hpPunchRoutine != null)
            {
                StopCoroutine(_hpPunchRoutine);
                _hpPunchRoutine = null;
            }

            _hpPunchBaseScale = transform.localScale;
            _hpPunchBaseCaptured = true;
        }

        private AnimationClip ResolveSimulatorClip(UnitCombatVisualState visualState)
        {
            var sim = GetComponent<UnitSpriteSimulator>();
            if (sim == null || !sim.TryGetLayout(visualState, out var layout))
            {
                return null;
            }

            return layout.animationClip;
        }

        private IEnumerator ReturnToIdleAfter(float seconds)
        {
            if (seconds > 0f)
            {
                yield return new WaitForSeconds(seconds);
            }

            if (Unit != null && !Unit.IsAlive)
            {
                _combatAnimRoutine = null;
                yield break;
            }

            PlayIdleState();
            _combatAnimRoutine = null;
        }

        public float AnimatorSpeed
        {
            get
            {
                ResolveAnimatorReference();
                return animator != null ? animator.speed : 1f;
            }
        }

        public void SetAnimatorSpeed(float speed)
        {
            ResolveAnimatorReference();
            if (animator != null)
            {
                animator.speed = Mathf.Max(0.01f, speed);
            }
        }

        private void ResolveAnimatorReference()
        {
            if (animator != null)
            {
                return;
            }

            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private AnimationClip ResolveCounterClip(out string stateName)
        {
            var authored = !string.IsNullOrWhiteSpace(counterStateName) ? counterStateName : guardStateName;
            var clip = ResolveClipByKeyword(authored, "Counter", out stateName);
            if (clip != null)
            {
                return clip;
            }

            clip = ResolveClipByKeyword(authored, "Guard", out stateName);
            if (clip != null)
            {
                return clip;
            }

            clip = ResolveClipByKeyword(null, "Evade", out stateName);
            if (clip != null)
            {
                return clip;
            }

            clip = ResolveClipByKeyword(null, "Skill 1", out stateName);
            if (clip != null)
            {
                return clip;
            }

            return ResolveClipByKeyword(null, "Skill", out stateName);
        }

        private AnimationClip ResolveGuardClip(out string stateName)
        {
            return ResolveCounterClip(out stateName);
        }

        private AnimationClip ResolveBeCounteredClip(out string stateName)
        {
            var clip = ResolveClipByKeyword(beCounteredStateName, "Be Countered", out stateName);
            if (clip != null)
            {
                return clip;
            }

            return ResolveClipByKeyword(null, "Hurt", out stateName);
        }

        private AnimationClip ResolveDeathClip(out string stateName)
        {
            var clip = ResolveClipByKeyword(deathStateName, "Death", out stateName);
            if (clip != null)
            {
                return clip;
            }

            clip = ResolveClipByKeyword(deathStateName, "Die", out stateName);
            if (clip != null)
            {
                return clip;
            }

            return ResolveClipByKeyword(null, "Dead", out stateName);
        }

        private AnimationClip ResolveMovingClip(out string stateName)
        {
            var clip = ResolveClipByKeyword(movingStateName, "Moving", out stateName);
            if (clip != null)
            {
                return clip;
            }

            return ResolveClipByKeyword(null, "Move", out stateName);
        }

        /// <summary>Loop still (or clip) locomotion until another combat pose or idle clip takes over.</summary>
        public void PlayMovingLoop()
        {
            if (Unit != null && !Unit.IsAlive)
            {
                PlayDeathAnimation();
                return;
            }

            if (TryPlaySimulatorVisual(
                    UnitCombatVisualState.Moving,
                    0f,
                    scheduleIdle: false,
                    UnitSpriteApplyMode.PreferStill))
            {
                return;
            }

            if (TryPlaySimulatorVisual(UnitCombatVisualState.Moving, 0f, scheduleIdle: false))
            {
                return;
            }

            ResolveAnimatorReference();
            var clip = ResolveMovingClip(out var stateName);
            if (clip != null && HasPlayableAnimatorState(stateName))
            {
                PlayCombatAnimation(clip, stateName, 0f, scheduleIdle: false);
                return;
            }

            PlayIdleStill();
        }

        /// <summary>Default rest pose: idle animation clip (Play Mode / hết phase).</summary>
        public void PlayIdleState()
        {
            if (Unit != null && !Unit.IsAlive)
            {
                PlayDeathAnimation();
                return;
            }

            RestoreTravelFacing();

            if (_combatAnimRoutine != null)
            {
                StopCoroutine(_combatAnimRoutine);
                _combatAnimRoutine = null;
            }

            if (TryPlaySimulatorVisual(
                    UnitCombatVisualState.Idle,
                    0f,
                    scheduleIdle: false,
                    UnitSpriteApplyMode.PreferClip))
            {
                return;
            }

            if (TryPlaySimulatorVisual(UnitCombatVisualState.Idle, 0f, scheduleIdle: false))
            {
                return;
            }

            ResolveAnimatorReference();
            var idleState = ResolveIdleStateName();
            if (HasPlayableAnimatorState(idleState))
            {
                animator.enabled = true;
                animator.Play(Animator.StringToHash(idleState), 0, 0f);
                return;
            }

            PlayIdleStill();
        }

        /// <summary>Stop idle clip and freeze the authored idle still — used when stepping into a fight cell.</summary>
        public void PlayIdleStill()
        {
            if (Unit != null && !Unit.IsAlive)
            {
                PlayDeathAnimation();
                return;
            }

            if (_combatAnimRoutine != null)
            {
                StopCoroutine(_combatAnimRoutine);
                _combatAnimRoutine = null;
            }

            if (TryPlaySimulatorVisual(
                    UnitCombatVisualState.Idle,
                    0f,
                    scheduleIdle: false,
                    UnitSpriteApplyMode.PreferStill))
            {
                return;
            }

            ResolveAnimatorReference();
            if (animator != null)
            {
                animator.enabled = false;
            }
        }

        /// <summary>Stop idle clip and play moving while walking onto the fight cell.</summary>
        public void BeginCombatTravel()
        {
            if (Unit != null && !Unit.IsAlive)
            {
                PlayDeathAnimation();
                return;
            }

            RestoreTravelFacing();
            PlayMovingLoop();
        }

        /// <summary>
        /// Play moving toward a fight cell. Returns false when already there so callers skip the walk pose.
        /// </summary>
        public bool TryBeginCombatTravelTo(Vector3 destinationFeet, float epsilon = CombatFeetArriveEpsilon)
        {
            if (Unit != null && !Unit.IsAlive)
            {
                PlayDeathAnimation();
                return false;
            }

            if (IsFeetNear(destinationFeet, epsilon))
            {
                return false;
            }

            BeginCombatTravel();
            return true;
        }

        /// <summary>
        /// Play moving while walking home, mirrored on X without changing authored scale.
        /// </summary>
        public void BeginCombatReturn()
        {
            if (Unit != null && !Unit.IsAlive)
            {
                PlayDeathAnimation();
                return;
            }

            PlayMovingLoop();
            ApplyReturnFacing();
        }

        /// <summary>
        /// Play mirrored moving toward home. Returns false when already on the home cell.
        /// </summary>
        public bool TryBeginCombatReturnTo(Vector3 destinationFeet, float epsilon = CombatFeetArriveEpsilon)
        {
            if (Unit != null && !Unit.IsAlive)
            {
                PlayDeathAnimation();
                return false;
            }

            if (IsFeetNear(destinationFeet, epsilon))
            {
                RestoreTravelFacing();
                return false;
            }

            BeginCombatReturn();
            return true;
        }

        public bool IsFeetNear(Vector3 feetWorld, float epsilon = CombatFeetArriveEpsilon)
        {
            var feet = FeetWorldPosition;
            return Vector2.Distance(
                new Vector2(feet.x, feet.y),
                new Vector2(feetWorld.x, feetWorld.y)) <= Mathf.Max(0.01f, epsilon);
        }

        public bool IsRootNear(Vector3 worldPosition, float epsilon = CombatFeetArriveEpsilon)
        {
            var pos = transform.position;
            return Vector2.Distance(
                new Vector2(pos.x, pos.y),
                new Vector2(worldPosition.x, worldPosition.y)) <= Mathf.Max(0.01f, epsilon);
        }

        /// <summary>Idle still after arriving on the fight cell — combat poses start after this.</summary>
        public void ArriveAtCombatCell()
        {
            if (Unit != null && !Unit.IsAlive)
            {
                PlayDeathAnimation();
                return;
            }

            RestoreTravelFacing();
            PlayIdleStill();
        }

        /// <summary>After a phase: still idle, then resume looping idle clip.</summary>
        public void FinishCombatPhaseIdle()
        {
            if (Unit != null && !Unit.IsAlive)
            {
                PlayDeathAnimation();
                return;
            }

            RestoreTravelFacing();
            PlayIdleStill();
            PlayIdleState();
        }

        /// <summary>
        /// Mirror the current sprite on X only. Magnitude of localScale stays authored.
        /// </summary>
        public void ApplyReturnFacing()
        {
            ResolveSpriteRendererReference();
            if (spriteRenderer == null)
            {
                return;
            }

            if (!_returnFacingActive)
            {
                _authoredFlipX = spriteRenderer.flipX;
                _returnFacingActive = true;
            }

            spriteRenderer.flipX = !_authoredFlipX;
        }

        public void RestoreTravelFacing()
        {
            if (!_returnFacingActive)
            {
                return;
            }

            ResolveSpriteRendererReference();
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = _authoredFlipX;
            }

            _returnFacingActive = false;
        }

        public Vector3 AnchorPosition => _anchorCaptured ? _anchorPosition : transform.position;

        public void CaptureAnchor()
        {
            _anchorPosition = transform.position;
            _anchorCaptured = true;
        }

        public void SnapToAnchor()
        {
            if (_anchorCaptured)
            {
                transform.position = _anchorPosition;
            }
        }

        /// <summary>Slide the unit so its feet reach <paramref name="feetWorld"/>; keeps the current depth.</summary>
        public IEnumerator MoveFeetToRoutine(Vector3 feetWorld, float seconds)
        {
            var rootToFeet = transform.position - FeetWorldPosition;
            var from = transform.position;
            var to = new Vector3(feetWorld.x + rootToFeet.x, feetWorld.y + rootToFeet.y, from.z);
            yield return MoveToRoutine(to, seconds);
        }

        public IEnumerator MoveToRoutine(Vector3 worldPosition, float seconds)
        {
            var from = transform.position;
            var to = new Vector3(worldPosition.x, worldPosition.y, from.z);
            if (seconds <= 0f)
            {
                transform.position = to;
                yield break;
            }

            var t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                var p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / seconds));
                transform.position = Vector3.Lerp(from, to, p);
                yield return null;
            }

            transform.position = to;
        }

        /// <summary>1 = full brightness. Multiplies the authored sprite color for focus-dim effects.</summary>
        public void SetVisualDimFactor(float factor)
        {
            _dimFactor = Mathf.Clamp01(factor);
            ApplySpriteTint();
        }

        private void CaptureBaseSpriteColor()
        {
            ResolveSpriteRendererReference();
            if (spriteRenderer == null || _baseColorCaptured)
            {
                return;
            }

            _baseSpriteColor = spriteRenderer.color;
            _baseColorCaptured = true;
        }

        private void ApplySpriteTint()
        {
            ResolveSpriteRendererReference();
            if (spriteRenderer == null)
            {
                return;
            }

            CaptureBaseSpriteColor();
            var tinted = _baseSpriteColor;
            tinted.r *= _dimFactor;
            tinted.g *= _dimFactor;
            tinted.b *= _dimFactor;
            spriteRenderer.color = tinted;
        }

        public void PlayAttackAnimation(SkillDefinitionSO skill = null)
        {
            PlayAttackAnimationInternal(skill, scheduleIdle: true);
        }

        public void PlayAttackAnimationHold(SkillDefinitionSO skill = null)
        {
            PlayAttackAnimationInternal(skill, scheduleIdle: false);
        }

        private void PlayAttackAnimationInternal(SkillDefinitionSO skill, bool scheduleIdle)
        {
            if (TryPlayAttackVisual(skill, scheduleIdle))
            {
                ScheduleSkillSfx(skill, ResolveAttackSimulatorClip(skill));
                return;
            }

            ResolveAnimatorReference();
            if (skill != null && skill.IsGuard)
            {
                var guard = ResolveGuardClip(out var guardState);
                PlayCombatAnimation(guard, guardState, 0f, scheduleIdle);
                return;
            }

            var clip = ResolveSkillClip(skill, out var stateName);
            if (clip == null && !string.IsNullOrEmpty(normalHitStateName))
            {
                clip = ResolveClipByKeyword(normalHitStateName, normalHitStateName, out stateName);
            }

            if (clip == null)
            {
                clip = ResolveClipByKeyword(null, "Skill", out stateName);
            }

            if (clip == null)
            {
                clip = ResolveClipByKeyword(null, "Attack", out stateName);
            }

            if (clip == null)
            {
                clip = ResolveGuardClip(out stateName);
            }

            if (clip == null && !string.IsNullOrEmpty(normalHitStateName))
            {
                stateName = normalHitStateName;
            }

            PlayCombatAnimation(clip, stateName, 0f, scheduleIdle);
            ScheduleSkillSfx(skill, clip);
        }

        private static void ScheduleSkillSfx(SkillDefinitionSO skill, AnimationClip clip)
        {
            if (skill == null || clip == null)
            {
                return;
            }

            var sfx = FindAnyObjectByType<CombatSfxController>();
            sfx?.PlaySkillSfxAtClipCue(skill, clip.length);
        }

        public float EstimateCounterClipLength()
        {
            ResolveAnimatorReference();
            var clip = ResolveCounterClip(out _);
            if (clip == null)
            {
                clip = ResolveGuardClip(out _);
            }

            return clip != null ? Mathf.Max(MinCombatPoseHoldSec, clip.length) : MinCombatPoseHoldSec;
        }

        public float EstimateBeCounteredClipLength()
        {
            ResolveAnimatorReference();
            var clip = ResolveBeCounteredClip(out _);
            return clip != null ? Mathf.Max(MinCombatPoseHoldSec, clip.length) : MinCombatPoseHoldSec;
        }

        public float EstimateSkillClipLength(SkillDefinitionSO skill)
        {
            var clip = ResolveAttackSimulatorClip(skill);
            if (clip == null)
            {
                ResolveAnimatorReference();
                clip = ResolveSkillClip(skill, out _);
            }

            if (clip == null)
            {
                clip = ResolveClipByKeyword(null, "Attack", out _);
            }

            return clip != null ? Mathf.Max(MinCombatPoseHoldSec, clip.length) : MinCombatPoseHoldSec;
        }

        private AnimationClip ResolveSkillClip(SkillDefinitionSO skill, out string stateName)
        {
            stateName = null;
            if (skill == null)
            {
                return null;
            }

            // Authored party clip names (Normal / Skill / Ult)
            var authored = skill.slotKind switch
            {
                SkillSlotKind.BasicAttack => normalHitStateName,
                SkillSlotKind.Skill => skillStateName,
                SkillSlotKind.Ultimate => ultStateName,
                _ => normalHitStateName
            };
            if (!string.IsNullOrEmpty(authored))
            {
                var byAuthoredSlot = ResolveClipByKeyword(authored, authored, out stateName);
                if (byAuthoredSlot != null && !IsSkillClipMismatch(skill.slotKind, byAuthoredSlot.name))
                {
                    return byAuthoredSlot;
                }
            }

            if (!string.IsNullOrEmpty(skill.displayName))
            {
                var byDisplayName = ResolveClipByKeyword(skill.displayName, skill.displayName, out stateName);
                if (byDisplayName != null)
                {
                    return byDisplayName;
                }
            }

            // Charlotte: NorHit / Skill / Ultimate · party chung: Skill 1/2/3
            string[] keywords = skill.slotKind switch
            {
                SkillSlotKind.BasicAttack => new[] { "NorHit", "NormalHit", "Skill 1", "Attack", "Basic" },
                SkillSlotKind.Skill => new[] { "Skill 2", "Charlott_Skill", "_Skill" },
                SkillSlotKind.Ultimate => new[] { "Ultimate", "Skill 3", "Ult", "Skill 2" },
                _ => null
            };

            if (keywords != null)
            {
                foreach (var keyword in keywords)
                {
                    var bySlot = ResolveClipByKeyword(null, keyword, out stateName);
                    if (bySlot != null && !IsSkillClipMismatch(skill.slotKind, bySlot.name))
                    {
                        return bySlot;
                    }
                }
            }

            if (!string.IsNullOrEmpty(skillStateName))
            {
                var byAuthored = ResolveClipByKeyword(skillStateName, skillStateName, out stateName);
                if (byAuthored != null)
                {
                    return byAuthored;
                }
            }

            var bySkill = ResolveClipByKeyword(null, "Skill", out stateName);
            if (bySkill != null && !IsSkillClipMismatch(skill.slotKind, bySkill.name))
            {
                return bySkill;
            }

            return null;
        }

        private static bool IsSkillClipMismatch(SkillSlotKind slot, string clipName)
        {
            if (string.IsNullOrEmpty(clipName))
            {
                return true;
            }

            var lower = clipName.ToLowerInvariant();
            return slot switch
            {
                SkillSlotKind.BasicAttack =>
                    lower.Contains("ultimate") || lower.Contains("skill 2") || lower.Contains("skill 3"),
                SkillSlotKind.Skill =>
                    lower.Contains("norhit") || lower.Contains("ultimate") || lower.Contains("skill 1") ||
                    lower.Contains("skill 3"),
                SkillSlotKind.Ultimate =>
                    lower.Contains("norhit") || lower.Contains("normalhit"),
                _ => false
            };
        }

        private AnimationClip ResolveClipByKeyword(string preferredName, string keyword, out string stateName)
        {
            stateName = string.IsNullOrWhiteSpace(preferredName) ? preferredName : preferredName.Trim();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return null;
            }

            var clips = animator.runtimeAnimatorController.animationClips;
            if (!string.IsNullOrEmpty(stateName))
            {
                foreach (var clip in clips)
                {
                    if (clip != null && clip.name == stateName)
                    {
                        return clip;
                    }
                }
            }

            foreach (var clip in clips)
            {
                if (clip == null || clip.name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                stateName = clip.name;
                return clip;
            }

            if (string.IsNullOrEmpty(preferredName))
            {
                stateName = null;
            }

            return null;
        }

        private string ResolveIdleStateName()
        {
            if (!string.IsNullOrEmpty(idleStateName))
            {
                return idleStateName;
            }

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return null;
            }

            AnimationClip best = null;
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip == null || clip.name.IndexOf("Idle", System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (best == null || clip.name.Length < best.name.Length)
                {
                    best = clip;
                }
            }

            return best != null ? best.name : null;
        }

        /// <summary>World position used for grid snap / drop detection.</summary>
        public Vector3 FeetWorldPosition =>
            feetAnchor != null ? feetAnchor.transform.position : transform.position;

        /// <summary>Anchor cạnh phải thân nhân vật — dùng cho skill panel UI.</summary>
        public Vector3 GetSkillPanelAnchorWorld()
        {
            ResolveSpriteRendererReference();
            ResolveBodyColliderReference();

            if (bodyCollider != null)
            {
                var bounds = bodyCollider.bounds;
                return new Vector3(bounds.max.x, bounds.center.y, bounds.center.z);
            }

            if (spriteRenderer != null)
            {
                var bounds = spriteRenderer.bounds;
                return new Vector3(bounds.max.x, bounds.center.y, bounds.center.z);
            }

            return transform.position + Vector3.right * 0.5f;
        }

        /// <summary>Anchor đỉnh đầu nhân vật — dùng đặt bảng skill phía trên unit.</summary>
        public Vector3 GetSkillPanelAboveAnchorWorld()
        {
            ResolveSpriteRendererReference();
            ResolveBodyColliderReference();

            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                var bounds = spriteRenderer.bounds;
                return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            }

            if (bodyCollider != null)
            {
                var bounds = bodyCollider.bounds;
                return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            }

            return transform.position + Vector3.up * 0.8f;
        }

        public UnitPresetSO ResolvePreset()
        {
            if (preset != null)
            {
                return preset;
            }

            if (!string.IsNullOrEmpty(demoUnitKey))
            {
                return EncounterRuntimeFactory.GetPresetByKey(demoUnitKey);
            }

            return null;
        }
        public GridSide Side => side;
        public bool IsPlacedOnGrid => new GridPosition(side, row, column).IsValid();
        public GridPosition GridPosition => new GridPosition(side, row, column);

        public void SetGridCoordinates(int gridRow, int gridColumn)
        {
            row = gridRow;
            column = gridColumn;
            Unit?.SetGridPosition(new GridPosition(side, row, column));
        }

        public void PlaceOnGrid(GridPosition position)
        {
            side = position.Side;
            row = position.Row;
            column = position.Column;
            Unit?.SetGridPosition(position);
        }

        /// <summary>Align feet (not transform pivot) to a world XY; optional Z for draw order.</summary>
        public void SnapFeetTo(Vector3 cellWorldCenter, float? depthZ = null)
        {
            var rootToFeet = transform.position - FeetWorldPosition;
            var target = cellWorldCenter + rootToFeet;
            if (depthZ.HasValue)
            {
                target.z = depthZ.Value;
            }

            transform.position = target;
        }

        /// <summary>Move unit so feet follow pointer while dragging.</summary>
        public void PlaceFeetAt(Vector3 feetWorld)
        {
            var rootToFeet = transform.position - FeetWorldPosition;
            transform.position = new Vector3(feetWorld.x + rootToFeet.x, feetWorld.y + rootToFeet.y,
                transform.position.z);
        }

        public void ClearGridPlacement()
        {
            row = HoneycombIndex.Unplaced;
            column = HoneycombIndex.Unplaced;
        }

        public void ConfigureDemo(string unitKey, GridSide gridSide)
        {
            demoUnitKey = unitKey;
            preset = null;
            side = gridSide;
            ClearGridPlacement();
            var resolved = ResolvePreset();
            name = $"Unit_{resolved?.displayName ?? unitKey}";
        }

        public void Bind(CombatUnit unit)
        {
            if (Unit != null)
            {
                Unit.OnHpChanged -= HandleHpChanged;
            }

            Unit = unit;
            ResolveAnimatorReference();
            ResolveSpriteRendererReference();
            EnsureHpLabel();
            EnsureInteractionColliders();
            TryRestoreSpriteFromPresetIfNeeded();
            CaptureBaseSpriteColor();
            CaptureAnchor();
            ApplyVisuals();
            unit.OnHpChanged += HandleHpChanged;
            RefreshHp();
            UnitSpriteSimulator.EnsureOn(this);
            PlayIdleState();
        }

        /// <summary>Body/feet colliders — không đụng sprite. Giữ size/offset scene khi preserveSceneCollider.</summary>
        public void EnsureInteractionColliders()
        {
            ResolveSpriteRendererReference();
            RemoveLegacyBoxCollider();
            EnsureBodyCollider2D();
            EnsureFeetAnchor();
        }

        /// <summary>Editor/menu — ghi đè collider theo sprite (bỏ qua preserveSceneCollider).</summary>
        public void RefitBodyColliderToSprite()
        {
            ResolveSpriteRendererReference();
            ResolveBodyColliderReference();
            if (bodyCollider == null)
            {
                bodyCollider = gameObject.AddComponent<BoxCollider2D>();
            }

            bodyCollider.enabled = true;
            bodyCollider.isTrigger = false;
            RemoveDuplicateBodyColliders();
            FitBodyColliderToSprite();
            RefreshFeetAnchor();
        }

        private void EnsureVisuals()
        {
            ResolveSpriteRendererReference();
            EnsureHpLabel();
            EnsureInteractionColliders();
        }

        private void ResolveSpriteRendererReference()
        {
            if (spriteRenderer != null)
            {
                return;
            }

            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>Chỉ gán placeholder/preset khi chưa có art thật — không ghi đè sprite scene.</summary>
        private void TryRestoreSpriteFromPresetIfNeeded()
        {
            ResolveSpriteRendererReference();
            if (spriteRenderer == null)
            {
                return;
            }

            if (spriteRenderer.sprite != null && !IsGeneratedPlaceholderSprite(spriteRenderer.sprite))
            {
                return;
            }

            var preset = ResolvePreset();
            if (preset?.battleSprite != null)
            {
                spriteRenderer.sprite = preset.battleSprite;
                return;
            }

            if (!preserveSceneVisuals && spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = CreatePlaceholderSprite();
            }
        }

        private static bool IsGeneratedPlaceholderSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return false;
            }

            return sprite.rect.width <= 1f && sprite.rect.height <= 1f;
        }

        private void RemoveLegacyBoxCollider()
        {
            var legacy = GetComponent<BoxCollider>();
            if (legacy == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(legacy);
            }
            else
            {
                DestroyImmediate(legacy);
            }
        }

        private void EnsureBodyCollider2D()
        {
            ResolveBodyColliderReference();

            if (bodyCollider == null)
            {
                bodyCollider = gameObject.AddComponent<BoxCollider2D>();
            }

            bodyCollider.enabled = true;
            bodyCollider.isTrigger = false;
            RemoveDuplicateBodyColliders();

            if (!ShouldPreserveSceneCollider() && IsDefaultColliderShape())
            {
                FitBodyColliderToSprite();
            }
        }

        private void ResolveBodyColliderReference()
        {
            if (bodyCollider != null && bodyCollider.gameObject == gameObject)
            {
                return;
            }

            bodyCollider = GetComponent<BoxCollider2D>();
        }

        private bool ShouldPreserveSceneCollider()
        {
            return preserveSceneCollider && bodyCollider != null;
        }

        private bool IsDefaultColliderShape()
        {
            return bodyCollider.size == Vector2.one && bodyCollider.offset == Vector2.zero;
        }

        private void RemoveDuplicateBodyColliders()
        {
            foreach (var col in GetComponents<BoxCollider2D>())
            {
                if (col == bodyCollider)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(col);
                }
                else
                {
                    DestroyImmediate(col);
                }
            }
        }

        public void FitBodyColliderToSprite()
        {
            if (bodyCollider == null || spriteRenderer == null || spriteRenderer.sprite == null)
            {
                if (bodyCollider != null)
                {
                    bodyCollider.size = Vector2.one;
                    bodyCollider.offset = Vector2.zero;
                }

                return;
            }

            var bounds = spriteRenderer.bounds;
            var lossyScale = transform.lossyScale;
            var scaleX = Mathf.Max(Mathf.Abs(lossyScale.x), 0.0001f);
            var scaleY = Mathf.Max(Mathf.Abs(lossyScale.y), 0.0001f);
            bodyCollider.size = new Vector2(bounds.size.x / scaleX, bounds.size.y / scaleY);
            bodyCollider.offset = transform.InverseTransformPoint(bounds.center);
        }

        public void RefreshFeetAnchor()
        {
            EnsureFeetAnchor();
            PositionFeetAnchorAtSpriteBase();
        }

        private void EnsureFeetAnchor()
        {
            if (feetAnchor == null)
            {
                feetAnchor = GetComponentInChildren<UnitFeetAnchor>(true);
            }

            if (feetAnchor == null)
            {
                var existing = transform.Find(FeetAnchorObjectName);
                if (existing != null)
                {
                    feetAnchor = existing.GetComponent<UnitFeetAnchor>();
                    if (feetAnchor == null)
                    {
                        feetAnchor = existing.gameObject.AddComponent<UnitFeetAnchor>();
                    }
                }
            }

            if (feetAnchor == null)
            {
                var feetGo = new GameObject(FeetAnchorObjectName);
                feetGo.transform.SetParent(transform, false);
                feetAnchor = feetGo.AddComponent<UnitFeetAnchor>();
                PositionFeetAnchorAtSpriteBase();
            }

            feetAnchor.WireReferences();
        }

        private void PositionFeetAnchorAtSpriteBase()
        {
            if (feetAnchor == null)
            {
                return;
            }

            var localFeet = new Vector3(0f, -0.5f, 0f);
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                var worldFeet = new Vector3(
                    transform.position.x,
                    spriteRenderer.bounds.min.y,
                    transform.position.z);
                localFeet = transform.InverseTransformPoint(worldFeet);
                localFeet.x = 0f;
                localFeet.z = 0f;
            }

            feetAnchor.transform.localPosition = localFeet;
        }

        private void EnsureHpLabel()
        {
            if (!showWorldHpLabel)
            {
                DisableWorldHpLabel();
                return;
            }

            if (IsHpLabelValid())
            {
                return;
            }

            hpLabel = null;
            var labelTransform = transform.Find("HpLabel");
            if (labelTransform != null && labelTransform.IsChildOf(transform))
            {
                hpLabel = labelTransform.GetComponent<TextMesh>();
            }

            if (hpLabel != null)
            {
                hpLabel.gameObject.SetActive(true);
                return;
            }

            var labelGo = new GameObject("HpLabel");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, -0.7f, 0f);
            hpLabel = labelGo.AddComponent<TextMesh>();
            hpLabel.characterSize = 0.08f;
            hpLabel.fontSize = 48;
            hpLabel.anchor = TextAnchor.MiddleCenter;
            hpLabel.color = Color.white;
        }

        private void DisableWorldHpLabel()
        {
            if (hpLabel != null)
            {
                hpLabel.text = string.Empty;
                hpLabel.gameObject.SetActive(false);
                return;
            }

            var labelTransform = transform.Find("HpLabel");
            if (labelTransform != null)
            {
                labelTransform.gameObject.SetActive(false);
            }
        }

        private bool IsHpLabelValid()
        {
            if (hpLabel == null)
            {
                return false;
            }

            var labelTransform = hpLabel.transform;
            return labelTransform != null && labelTransform.IsChildOf(transform);
        }

        private void ApplyVisuals()
        {
            if (Unit == null || spriteRenderer == null)
            {
                return;
            }

            if (!preserveSceneVisuals)
            {
                _baseSpriteColor = Unit.PlaceholderColor;
                _baseColorCaptured = true;
                spriteRenderer.sortingOrder = 10 + Unit.GridPosition.Row;
            }

            ApplySpriteTint();
        }

        private void HandleHpChanged(CombatUnit unit)
        {
            RefreshHp();
            if (unit == null)
            {
                return;
            }

            if (!unit.IsAlive)
            {
                PlayDeathAnimation();
                return;
            }

            if (EncounterDirector.IsPresenting)
            {
                return;
            }

            if (unit.LastHpChange.Kind == HpChangeKind.Damage && unit.LastHpChange.ShouldShowFeedback)
            {
                PlayBeCounteredHold();
            }
        }

        public Vector3 GetDamageNumberAnchor()
        {
            if (spriteRenderer != null)
            {
                return spriteRenderer.bounds.center + Vector3.up * (spriteRenderer.bounds.extents.y * 0.65f);
            }

            return transform.position + Vector3.up * 0.6f;
        }

        public void PlayHpFeedback(HpChangeInfo change, bool playHitReaction = true)
        {
            if (!change.ShouldShowFeedback)
            {
                return;
            }

            var heal = change.Kind == HpChangeKind.Heal;
            DamageNumberPopupView.Spawn(GetDamageNumberAnchor(), change.Amount, heal, change.IsCritical);
            PunchBody(change.IsCritical);

            if (heal || Unit == null)
            {
                return;
            }

            if (!Unit.IsAlive)
            {
                PlayDeathAnimation();
            }
        }

        private void PunchBody(bool isCritical)
        {
            // EncounterDirector may hide non-duel units; coroutines cannot start on inactive objects.
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            {
                return;
            }

            if (!_hpPunchBaseCaptured)
            {
                _hpPunchBaseScale = transform.localScale;
                _hpPunchBaseCaptured = true;
            }

            if (_hpPunchRoutine != null)
            {
                StopCoroutine(_hpPunchRoutine);
                transform.localScale = _hpPunchBaseScale;
            }

            _hpPunchRoutine = StartCoroutine(HpPunchRoutine(isCritical));
        }

        private IEnumerator HpPunchRoutine(bool isCritical)
        {
            var peak = _hpPunchBaseScale * (isCritical ? 1.12f : 1.07f);
            const float up = 0.05f;
            const float down = 0.12f;
            var t = 0f;
            while (t < up)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(_hpPunchBaseScale, peak, Mathf.Clamp01(t / up));
                yield return null;
            }

            t = 0f;
            while (t < down)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(peak, _hpPunchBaseScale, Mathf.Clamp01(t / down));
                yield return null;
            }

            transform.localScale = _hpPunchBaseScale;
            _hpPunchRoutine = null;
        }

        private void RefreshHp()
        {
            if (!showWorldHpLabel)
            {
                DisableWorldHpLabel();
                return;
            }

            if (hpLabel != null && Unit != null)
            {
                hpLabel.text = Unit.CurrentHp.ToString();
            }
        }

        private static Sprite CreatePlaceholderSprite()
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private void OnDestroy()
        {
            if (_combatAnimRoutine != null)
            {
                StopCoroutine(_combatAnimRoutine);
                _combatAnimRoutine = null;
            }

            if (_hpPunchRoutine != null)
            {
                StopCoroutine(_hpPunchRoutine);
                _hpPunchRoutine = null;
                if (_hpPunchBaseCaptured)
                {
                    transform.localScale = _hpPunchBaseScale;
                }
            }

            if (Unit != null)
            {
                Unit.OnHpChanged -= HandleHpChanged;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            MergeGuardIntoCounter();
            if (bodyCollider == null)
            {
                bodyCollider = GetComponent<BoxCollider2D>();
            }
        }

        [ContextMenu("Add Unit Sprite Simulator")]
        private void AddUnitSpriteSimulator()
        {
            UnitSpriteSimulator.EnsureOn(this);
        }
#endif
    }
}

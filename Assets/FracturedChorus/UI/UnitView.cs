using FracturedChorus.Audio;
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Combat.Grid;
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
        [SerializeField] private string counterStateName;
        [SerializeField] private string guardStateName;
        [SerializeField] private string beCounteredStateName;
        [SerializeField] private string idleStateName;
        [SerializeField] private string movingStateName;
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

        public void EnsureDefaultCombatAnimStates()
        {
            var key = demoUnitKey ?? string.Empty;
            switch (key)
            {
                case "ren":
                    SetAnimStateIfEmpty(ref counterStateName, "Ren Counter");
                    SetAnimStateIfEmpty(ref beCounteredStateName, "Ren Hurt");
                    SetAnimStateIfEmpty(ref idleStateName, "Ren Idle");
                    SetAnimStateIfEmpty(ref movingStateName, "Ren Moving");
                    break;
                case "mage":
                    SetAnimStateIfEmpty(ref counterStateName, "Coda - Counter");
                    SetAnimStateIfEmpty(ref beCounteredStateName, "Coda - Hurt");
                    SetAnimStateIfEmpty(ref idleStateName, "Coda - Idle");
                    SetAnimStateIfEmpty(ref movingStateName, "Coda - Moving");
                    break;
                case "Charlott":
                case "charlotte":
                case "tank":
                    SetAnimStateIfEmpty(ref counterStateName, "Charlott_Guard");
                    SetAnimStateIfEmpty(ref guardStateName, "Charlott_Guard");
                    SetAnimStateIfEmpty(ref beCounteredStateName, "Charlott_Hurt");
                    SetAnimStateIfEmpty(ref idleStateName, "Charlott_Idle");
                    break;
                case "grunt_left":
                    SetAnimStateIfEmpty(ref beCounteredStateName, "Mini 1 - Hurt");
                    SetAnimStateIfEmpty(ref idleStateName, "Mini 1 -Idle");
                    break;
                case "grunt_right":
                    SetAnimStateIfEmpty(ref beCounteredStateName, "Mini 2 - Hurt");
                    SetAnimStateIfEmpty(ref idleStateName, "Mini 2 - Idle");
                    SetAnimStateIfEmpty(ref movingStateName, "Mini 2 - Moving");
                    break;
                case "boss_despair":
                    SetAnimStateIfEmpty(ref beCounteredStateName, "Boss - Be Countered");
                    SetAnimStateIfEmpty(ref idleStateName, "Boss Idle");
                    SetAnimStateIfEmpty(ref movingStateName, "Boss - Moving");
                    break;
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
            ResolveAnimatorReference();
            var clip = ResolveBeCounteredClip(out var stateName);
            PlayCombatAnimation(clip, stateName, normalizedTime, scheduleIdle);
        }

        public void PlayBeCounteredHitRetrigger()
        {
            ResolveAnimatorReference();
            var clip = ResolveBeCounteredClip(out var stateName);
            PlayCombatAnimation(clip, stateName, hitRetriggerNormalizedTime, scheduleIdle: true);
        }

        private Coroutine _combatAnimRoutine;
        private Coroutine _hpPunchRoutine;
        private Vector3 _hpPunchBaseScale = Vector3.one;
        private bool _hpPunchBaseCaptured;
        private Vector3 _anchorPosition;
        private bool _anchorCaptured;
        private Color _baseSpriteColor = Color.white;
        private bool _baseColorCaptured;
        private float _dimFactor = 1f;

        private void PlayCombatAnimation(AnimationClip clip, string stateName, float normalizedTime, bool scheduleIdle)
        {
            if (animator == null || clip == null || string.IsNullOrEmpty(stateName))
            {
                return;
            }

            if (_combatAnimRoutine != null)
            {
                StopCoroutine(_combatAnimRoutine);
                _combatAnimRoutine = null;
            }

            var t = Mathf.Clamp01(normalizedTime);
            animator.Play(stateName, 0, t);
            if (!scheduleIdle)
            {
                return;
            }

            var remaining = clip.length * (1f - t);
            _combatAnimRoutine = StartCoroutine(ReturnToIdleAfter(remaining));
        }

        private IEnumerator ReturnToIdleAfter(float seconds)
        {
            if (seconds > 0f)
            {
                yield return new WaitForSeconds(seconds);
            }

            var idleState = ResolveIdleStateName();
            if (animator != null && !string.IsNullOrEmpty(idleState))
            {
                animator.Play(idleState, 0, 0f);
            }

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
            return ResolveClipByKeyword(counterStateName, "Counter", out stateName);
        }

        private AnimationClip ResolveGuardClip(out string stateName)
        {
            return ResolveClipByKeyword(guardStateName, "Guard", out stateName);
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

        private AnimationClip ResolveMovingClip(out string stateName)
        {
            var clip = ResolveClipByKeyword(movingStateName, "Moving", out stateName);
            if (clip != null)
            {
                return clip;
            }

            return ResolveClipByKeyword(null, "Move", out stateName);
        }

        /// <summary>Loop the locomotion clip until another combat animation or idle takes over.</summary>
        public void PlayMovingLoop()
        {
            ResolveAnimatorReference();
            var clip = ResolveMovingClip(out var stateName);
            PlayCombatAnimation(clip, stateName, 0f, scheduleIdle: false);
        }

        public void PlayIdleState()
        {
            ResolveAnimatorReference();
            if (_combatAnimRoutine != null)
            {
                StopCoroutine(_combatAnimRoutine);
                _combatAnimRoutine = null;
            }

            var idleState = ResolveIdleStateName();
            if (animator != null && !string.IsNullOrEmpty(idleState))
            {
                animator.Play(idleState, 0, 0f);
            }
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
            ResolveAnimatorReference();
            var clip = ResolveSkillClip(skill, out var stateName);
            if (clip == null)
            {
                clip = ResolveClipByKeyword(null, "Attack", out stateName);
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

            return clip != null ? clip.length : 0.25f;
        }

        public float EstimateBeCounteredClipLength()
        {
            ResolveAnimatorReference();
            var clip = ResolveBeCounteredClip(out _);
            return clip != null ? clip.length : 0.25f;
        }

        public float EstimateSkillClipLength(SkillDefinitionSO skill)
        {
            ResolveAnimatorReference();
            var clip = ResolveSkillClip(skill, out _);
            if (clip == null)
            {
                clip = ResolveClipByKeyword(null, "Attack", out _);
            }

            return clip != null ? clip.length : 0.3f;
        }

        private AnimationClip ResolveSkillClip(SkillDefinitionSO skill, out string stateName)
        {
            stateName = null;
            if (skill == null)
            {
                return null;
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
                SkillSlotKind.Ultimate => new[] { "Ultimate", "Skill 3", "Ult" },
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

            if (skill.slotKind == SkillSlotKind.Skill)
            {
                var bySkill = ResolveClipByKeyword(null, "Skill", out stateName);
                if (bySkill != null && !IsSkillClipMismatch(SkillSlotKind.Skill, bySkill.name))
                {
                    return bySkill;
                }
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
                    lower.Contains("norhit") || lower.Contains("skill 1") || lower.Contains("skill 2"),
                _ => false
            };
        }

        private AnimationClip ResolveClipByKeyword(string preferredName, string keyword, out string stateName)
        {
            stateName = preferredName;
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

            stateName = null;
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

        private void FitBodyColliderToSprite()
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

        private void EnsureFeetAnchor()
        {
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

            var localFeetY = -0.5f;
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                localFeetY = spriteRenderer.bounds.min.y - transform.position.y;
            }

            feetAnchor.transform.localPosition = new Vector3(0f, localFeetY, 0f);
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
            if (!unit.IsAlive && spriteRenderer != null && Unit != null)
            {
                _baseSpriteColor = new Color(Unit.PlaceholderColor.r, Unit.PlaceholderColor.g,
                    Unit.PlaceholderColor.b, 0.35f);
                _baseColorCaptured = true;
                ApplySpriteTint();
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

        public void PlayHpFeedback(HpChangeInfo change)
        {
            if (!change.ShouldShowFeedback)
            {
                return;
            }

            var heal = change.Kind == HpChangeKind.Heal;
            DamageNumberPopupView.Spawn(GetDamageNumberAnchor(), change.Amount, heal, change.IsCritical);
            PunchBody(change.IsCritical);
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
            if (bodyCollider == null)
            {
                bodyCollider = GetComponent<BoxCollider2D>();
            }
        }
#endif
    }
}

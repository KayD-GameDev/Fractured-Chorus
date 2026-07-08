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
        [Tooltip("Keep sprite/color/Transform scale authored in the scene.")]
        [SerializeField] private bool preserveSceneVisuals = true;
        [Tooltip("Keep BoxCollider2D size/offset authored in the scene — used as click + drag area.")]
        [SerializeField] private bool preserveSceneCollider = true;

        public CombatUnit Unit { get; private set; }
        public UnitPresetSO Preset => preset;
        public string DemoUnitKey => demoUnitKey;
        public UnitFeetAnchor FeetAnchor => feetAnchor;

        public static UnitView FindForUnit(CombatUnit unit)
        {
            if (unit == null)
            {
                return null;
            }

            foreach (var view in Object.FindObjectsByType<UnitView>(FindObjectsInactive.Exclude))
            {
                if (view != null && view.Unit == unit)
                {
                    return view;
                }
            }

            return null;
        }

        public void PlayCounterAnimation()
        {
            ResolveAnimatorReference();
            var clip = ResolveCounterClip(out var stateName);
            if (clip == null)
            {
                clip = ResolveGuardClip(out stateName);
            }

            PlayCombatAnimation(clip, stateName);
        }

        public void PlayBeCounteredAnimation()
        {
            ResolveAnimatorReference();
            var clip = ResolveBeCounteredClip(out var stateName);
            PlayCombatAnimation(clip, stateName);
        }

        private Coroutine _combatAnimRoutine;

        private void PlayCombatAnimation(AnimationClip clip, string stateName)
        {
            if (animator == null || clip == null || string.IsNullOrEmpty(stateName))
            {
                return;
            }

            if (_combatAnimRoutine != null)
            {
                StopCoroutine(_combatAnimRoutine);
            }

            animator.Play(stateName, 0, 0f);
            _combatAnimRoutine = StartCoroutine(ReturnToIdleAfter(clip.length));
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
            return ResolveClipByKeyword(beCounteredStateName, "Be Countered", out stateName);
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
                spriteRenderer.color = Unit.PlaceholderColor;
                spriteRenderer.sortingOrder = 10 + Unit.GridPosition.Row;
            }
        }

        private void HandleHpChanged(CombatUnit unit)
        {
            RefreshHp();
            if (!unit.IsAlive && spriteRenderer != null && Unit != null)
            {
                spriteRenderer.color = new Color(Unit.PlaceholderColor.r, Unit.PlaceholderColor.g,
                    Unit.PlaceholderColor.b, 0.35f);
            }
        }

        private void RefreshHp()
        {
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

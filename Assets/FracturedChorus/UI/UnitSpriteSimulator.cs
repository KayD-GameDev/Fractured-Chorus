using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FracturedChorus.UI
{
    /// <summary>
    /// Author combat sprites on a unit: swap art, scale, and FeetAnchor while looking at the real board.
    /// Hierarchy: Unit (SpriteRenderer + scale) / FeetAnchor (drag to pin feet).
    /// Each slot links to one UnitView combat state (clip or still).
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(UnitView))]
    public sealed class UnitSpriteSimulator : MonoBehaviour
    {
        public const int MinSpriteCount = 1;
        public const int MaxSpriteCount = 16;
        public const int DefaultSpriteCount = 1;

        [SerializeField] private int spritePreview;
        [SerializeField] private UnitSpriteLayout[] spriteLayouts = new UnitSpriteLayout[DefaultSpriteCount];

        private UnitView _view;
        private bool _syncing;
        private int _loadedPreview = -1;
        private Sprite _appliedSprite;
        private bool _previewLocked;
        private bool _combatPoseActive;
        private float _heldAnimatorSpeed = 1f;
        private bool _animatorHeld;

        public int SpritePreview => spritePreview;
        public int SpriteCount =>
            spriteLayouts != null && spriteLayouts.Length > 0
                ? spriteLayouts.Length
                : DefaultSpriteCount;
        public UnitSpriteLayout[] SpriteLayouts => spriteLayouts;

        public string SpriteTabLabel(int index)
        {
            EnsureLayouts();
            if (spriteLayouts == null || index < 0 || index >= spriteLayouts.Length)
            {
                return "V" + index;
            }

            return spriteLayouts[index].TabLabel(index);
        }

        public Vector3 CurrentScale => transform.localScale;

        public Vector3 FeetAnchorLocal
        {
            get
            {
                var feet = ResolveFeet();
                return feet != null ? feet.localPosition : Vector3.zero;
            }
        }

        public Sprite CurrentSprite
        {
            get
            {
                var sr = ResolveRenderer();
                return sr != null ? sr.sprite : null;
            }
        }

        public Vector2 SpritePixelSize
        {
            get
            {
                var sprite = CurrentSprite;
                if (sprite == null)
                {
                    return Vector2.zero;
                }

                var rect = sprite.rect;
                return new Vector2(rect.width, rect.height);
            }
        }

        public static UnitSpriteSimulator EnsureOn(UnitView view)
        {
            if (view == null)
            {
                return null;
            }

            var sim = view.GetComponent<UnitSpriteSimulator>();
            if (sim == null)
            {
                sim = view.gameObject.AddComponent<UnitSpriteSimulator>();
            }

            sim.EnsureLayouts();
            sim.EnsureHandles();
            return sim;
        }

        private void Awake()
        {
            _view = GetComponent<UnitView>();
            EnsureLayouts();
            EnsureHandles();
        }

        private void OnEnable()
        {
            _appliedSprite = null;
            InferLinkedStates();
        }

        private void OnDisable()
        {
            RestoreAnimator();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            InferLinkedStates();
        }
#endif

        private void LateUpdate()
        {
            if (_previewLocked || _combatPoseActive || !Application.isPlaying)
            {
                return;
            }

            TryApplyLayoutForCurrentSprite();
        }

        public bool TryGetLayout(UnitCombatVisualState state, out UnitSpriteLayout layout)
        {
            if (TryGetExactLayout(state, out layout))
            {
                return true;
            }

            if (state == UnitCombatVisualState.Guard)
            {
                return TryGetExactLayout(UnitCombatVisualState.Counter, out layout);
            }

            return false;
        }

        public bool TryGetExactLayout(UnitCombatVisualState state, out UnitSpriteLayout layout)
        {
            layout = default;
            if (state == UnitCombatVisualState.None || spriteLayouts == null)
            {
                return false;
            }

            EnsureLayouts();
            for (var i = 0; i < spriteLayouts.Length; i++)
            {
                if (spriteLayouts[i].linkedState != state)
                {
                    continue;
                }

                layout = spriteLayouts[i];
                return true;
            }

            return false;
        }

        public bool ApplyLayoutForState(
            UnitCombatVisualState state,
            bool keepWorldFeet = true,
            UnitSpriteApplyMode mode = UnitSpriteApplyMode.Auto)
        {
            if (!TryGetLayout(state, out var layout))
            {
                return false;
            }

            _combatPoseActive = Application.isPlaying;
            ApplyLayout(layout, keepWorldFeet, mode);
            return true;
        }

        public bool HasDuplicateLinkedState(UnitCombatVisualState state)
        {
            if (state == UnitCombatVisualState.None || spriteLayouts == null)
            {
                return false;
            }

            var count = 0;
            for (var i = 0; i < spriteLayouts.Length; i++)
            {
                if (spriteLayouts[i].linkedState == state)
                {
                    count++;
                }
            }

            return count > 1;
        }

        public void AuthorCurrentAsState(UnitCombatVisualState state)
        {
            EnsureLayouts();
            SaveCurrentLayout();
            var i = ClampPreview(spritePreview);
            var layout = spriteLayouts[i];
            layout.linkedState = state;
            if (string.IsNullOrWhiteSpace(layout.displayName) && state != UnitCombatVisualState.None)
            {
                layout.displayName = state.ToString();
            }

            spriteLayouts[i] = layout;
            MarkDirty();
        }

        public void EnsureIdleAnimationClip(AnimationClip clip)
        {
            if (clip == null)
            {
                return;
            }

            EnsureLayouts();
            if (!TryFindLayoutIndex(UnitCombatVisualState.Idle, out var i))
            {
                return;
            }

            var layout = spriteLayouts[i];
            layout.animationClip = clip;
            if (string.IsNullOrWhiteSpace(layout.displayName))
            {
                layout.displayName = "Idle";
            }

            spriteLayouts[i] = layout;
            MarkDirty();
        }

        public void EnsureCounterStillKind()
        {
            EnsureLayouts();
            if (!TryFindLayoutIndex(UnitCombatVisualState.Counter, out var i))
            {
                return;
            }

            var layout = spriteLayouts[i];
            if (layout.kind != UnitSpriteKind.AnimationClip)
            {
                return;
            }

            layout.kind = UnitSpriteKind.StillSprite;
            layout.animationClip = null;
            spriteLayouts[i] = layout;
            MarkDirty();
        }

        public void EnsureClipLinkedState(
            UnitCombatVisualState state,
            string displayName,
            AnimationClip clip)
        {
            if (state == UnitCombatVisualState.None || clip == null)
            {
                return;
            }

            EnsureLayouts();
            TryGetExactLayout(UnitCombatVisualState.Idle, out var idle);
            if (TryFindLayoutIndex(state, out var i))
            {
                var layout = spriteLayouts[i];
                layout.kind = UnitSpriteKind.AnimationClip;
                layout.animationClip = clip;
                layout.linkedState = state;
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    layout.displayName = displayName;
                }

                if (layout.localScale.sqrMagnitude < 0.0001f && idle.localScale.sqrMagnitude > 0.0001f)
                {
                    layout.localScale = idle.localScale;
                }

                if (layout.feetAnchorLocal.sqrMagnitude < 0.0001f)
                {
                    layout.feetAnchorLocal = idle.feetAnchorLocal;
                }

                if (!layout.HasCollider && idle.HasCollider)
                {
                    layout.colliderSize = idle.colliderSize;
                    layout.colliderOffset = idle.colliderOffset;
                }

                spriteLayouts[i] = layout;
                MarkDirty();
                return;
            }

            if (spriteLayouts.Length >= MaxSpriteCount)
            {
                return;
            }

            var next = new UnitSpriteLayout[spriteLayouts.Length + 1];
            Array.Copy(spriteLayouts, next, spriteLayouts.Length);
            next[next.Length - 1] = new UnitSpriteLayout
            {
                displayName = string.IsNullOrWhiteSpace(displayName) ? state.ToString() : displayName,
                kind = UnitSpriteKind.AnimationClip,
                sprite = idle.sprite,
                animationClip = clip,
                linkedState = state,
                localScale = idle.localScale.sqrMagnitude > 0.0001f ? idle.localScale : transform.localScale,
                feetAnchorLocal = idle.feetAnchorLocal,
                colliderSize = idle.colliderSize,
                colliderOffset = idle.colliderOffset
            };
            spriteLayouts = next;
            MarkDirty();
        }

        private bool TryFindLayoutIndex(UnitCombatVisualState state, out int index)
        {
            index = -1;
            if (state == UnitCombatVisualState.None || spriteLayouts == null)
            {
                return false;
            }

            for (var i = 0; i < spriteLayouts.Length; i++)
            {
                if (spriteLayouts[i].linkedState != state)
                {
                    continue;
                }

                index = i;
                return true;
            }

            return false;
        }

        public void SetSpritePreview(int preview)
        {
            if (_syncing)
            {
                return;
            }

            EnsureLayouts();
            preview = ClampPreview(preview);
            if (_loadedPreview >= 0 && spritePreview == _loadedPreview)
            {
                SaveCurrentLayout();
            }

            spritePreview = preview;
            _previewLocked = true;
            HoldAnimator();
            ApplyPreview(loadSaved: true);
        }

        public void ClearPreviewLock()
        {
            _previewLocked = false;
            RestoreAnimator();
            _appliedSprite = null;
        }

        public void AddSprite()
        {
            EnsureLayouts();
            if (spriteLayouts.Length >= MaxSpriteCount)
            {
                return;
            }

            SaveCurrentLayout();
            var next = new UnitSpriteLayout[spriteLayouts.Length + 1];
            Array.Copy(spriteLayouts, next, spriteLayouts.Length);
            var copy = CaptureHandles();
            copy.sprite = null;
            copy.animationClip = null;
            copy.displayName = string.Empty;
            copy.linkedState = UnitCombatVisualState.None;
            copy.kind = UnitSpriteKind.StillSprite;
            next[next.Length - 1] = copy;
            spriteLayouts = next;
            SetSpritePreview(next.Length - 1);
        }

        public void RemoveSprite()
        {
            EnsureLayouts();
            if (spriteLayouts.Length <= MinSpriteCount)
            {
                return;
            }

            SaveCurrentLayout();
            var remove = ClampPreview(spritePreview);
            var next = new UnitSpriteLayout[spriteLayouts.Length - 1];
            for (int i = 0, j = 0; i < spriteLayouts.Length; i++)
            {
                if (i == remove)
                {
                    continue;
                }

                next[j++] = spriteLayouts[i];
            }

            spriteLayouts = next;
            _loadedPreview = -1;
            SetSpritePreview(Mathf.Min(remove, next.Length - 1));
        }

        public void ApplyPreviewSprite(Sprite sprite)
        {
            EnsureLayouts();
            var i = ClampPreview(spritePreview);
            var layout = spriteLayouts[i];
            layout.sprite = sprite;
            if (layout.animationClip == null)
            {
                layout.kind = UnitSpriteKind.StillSprite;
            }

            spriteLayouts[i] = layout;
            ApplySprite(sprite);
            KeepFeetWorld();
            MarkDirty();
        }

        public void ApplyPreviewCollider()
        {
            EnsureLayouts();
            ApplyCollider(spriteLayouts[ClampPreview(spritePreview)]);
        }

        public void ApplyPreviewFeet()
        {
            EnsureLayouts();
            EnsureHandles();
            var layout = spriteLayouts[ClampPreview(spritePreview)];
            var feetTf = ResolveFeet();
            if (feetTf == null || !layout.HasFeetAnchor)
            {
                return;
            }

#if UNITY_EDITOR
            Undo.RecordObject(feetTf, "Set Sprite Feet Anchor");
#endif
            feetTf.localPosition = layout.feetAnchorLocal;
            MarkDirty();
        }

        public void ApplyPreviewClip(AnimationClip clip)
        {
            EnsureLayouts();
            var i = ClampPreview(spritePreview);
            var layout = spriteLayouts[i];
            layout.animationClip = clip;
            if (layout.sprite == null)
            {
                layout.kind = UnitSpriteKind.AnimationClip;
            }

            spriteLayouts[i] = layout;
            PlayClipPreview(clip);
            KeepFeetWorld();
            MarkDirty();
        }

        public void ApplyPreview(bool loadSaved)
        {
            if (_syncing)
            {
                return;
            }

            _syncing = true;
            try
            {
                EnsureLayouts();
                EnsureHandles();
                spritePreview = ClampPreview(spritePreview);
                var layout = spriteLayouts[spritePreview];
                if (loadSaved && layout.HasData)
                {
                    ApplyLayout(layout, keepWorldFeet: false, UnitSpriteApplyMode.Auto);
                }
                else if (layout.UsesStillArt)
                {
                    ApplySprite(layout.sprite);
                }
                else if (layout.animationClip != null)
                {
                    PlayClipPreview(layout.animationClip);
                }

                _loadedPreview = spritePreview;
                _appliedSprite = CurrentSprite;
            }
            finally
            {
                _syncing = false;
            }
        }

        public void SaveCurrentLayout()
        {
            EnsureLayouts();
            EnsureHandles();
            var i = ClampPreview(spritePreview);
            spriteLayouts[i] = CaptureHandles();
            MarkDirty();
        }

        public void SnapFeetToSpriteBottom()
        {
            EnsureHandles();
            var view = ResolveView();
            view?.RefreshFeetAnchor();
            KeepFeetWorld();
            MarkDirty();
        }

        public void SnapUnitToHoneycomb()
        {
            var view = ResolveView();
            if (view == null || !view.IsPlacedOnGrid)
            {
                return;
            }

            var cell = GridCellMarker.ResolveWorld(view.GridPosition);
            view.SnapFeetTo(cell, view.transform.position.z);
            MarkDirty();
        }

        public void SetUniformScale(float scale)
        {
            var s = Mathf.Max(0.01f, scale);
            var feet = ResolveView()?.FeetWorldPosition ?? transform.position;
            transform.localScale = new Vector3(s, s, s);
            ResolveView()?.PlaceFeetAt(feet);
            MarkDirty();
        }

        private void TryApplyLayoutForCurrentSprite()
        {
            var sprite = CurrentSprite;
            if (sprite == null || sprite == _appliedSprite)
            {
                return;
            }

            if (!TryFindLayout(sprite, out var layout))
            {
                _appliedSprite = sprite;
                return;
            }

            ApplyLayout(layout, keepWorldFeet: true, UnitSpriteApplyMode.Auto);
            _appliedSprite = sprite;
        }

        private bool TryFindLayout(Sprite sprite, out UnitSpriteLayout layout)
        {
            layout = default;
            if (spriteLayouts == null)
            {
                return false;
            }

            for (var i = 0; i < spriteLayouts.Length; i++)
            {
                if (!spriteLayouts[i].Matches(sprite))
                {
                    continue;
                }

                layout = spriteLayouts[i];
                return layout.HasData;
            }

            return false;
        }

        private void ApplyLayout(UnitSpriteLayout layout, bool keepWorldFeet, UnitSpriteApplyMode mode)
        {
            var view = ResolveView();
            var feet = view != null ? view.FeetWorldPosition : transform.position;
            var useStill = layout.ShouldApplyStill(mode);
            if (useStill)
            {
                SetAnimatorEnabled(false);
                ApplySprite(layout.sprite);
            }
            else if (mode == UnitSpriteApplyMode.PreferStill)
            {
                SetAnimatorEnabled(false);
            }
            else
            {
                SetAnimatorEnabled(true);
                if (layout.animationClip != null && (_previewLocked || !Application.isPlaying))
                {
                    PlayClipPreview(layout.animationClip);
                }
            }

            if (layout.localScale.sqrMagnitude > 0.0001f)
            {
                transform.localScale = layout.localScale;
            }

            var feetTf = ResolveFeet();
            if (feetTf != null && layout.HasFeetAnchor)
            {
                feetTf.localPosition = layout.feetAnchorLocal;
            }

            if (keepWorldFeet)
            {
                view?.PlaceFeetAt(feet);
            }

            ApplyCollider(layout);
            _appliedSprite = CurrentSprite;
        }

        private void ApplyCollider(UnitSpriteLayout layout)
        {
            if (!layout.HasCollider)
            {
                return;
            }

            ResolveView()?.ApplyBodyColliderShape(layout.colliderSize, layout.colliderOffset);
        }

        private UnitSpriteLayout CaptureHandles()
        {
            var scale = transform.localScale;
            if (scale.sqrMagnitude < 0.0001f)
            {
                scale = Vector3.one;
            }

            var displayName = string.Empty;
            var kind = UnitSpriteKind.StillSprite;
            Sprite sprite = CurrentSprite;
            AnimationClip clip = null;
            var linkedState = UnitCombatVisualState.None;
            var colliderSize = Vector2.zero;
            var colliderOffset = Vector2.zero;
            if (spriteLayouts != null && spriteLayouts.Length > 0)
            {
                var i = Mathf.Clamp(spritePreview, 0, spriteLayouts.Length - 1);
                var current = spriteLayouts[i];
                displayName = current.displayName;
                kind = current.kind;
                clip = current.animationClip;
                linkedState = current.linkedState;
                colliderSize = current.colliderSize;
                colliderOffset = current.colliderOffset;
                if (kind != UnitSpriteKind.StillSprite)
                {
                    sprite = current.sprite;
                }
            }

            var body = ResolveView()?.BodyCollider;
            if (body != null)
            {
                colliderSize = body.size;
                colliderOffset = body.offset;
            }

            return new UnitSpriteLayout
            {
                displayName = displayName,
                kind = kind,
                sprite = sprite,
                animationClip = clip,
                linkedState = linkedState,
                localScale = scale,
                feetAnchorLocal = FeetAnchorLocal,
                colliderSize = colliderSize,
                colliderOffset = colliderOffset
            };
        }

        private void ApplySprite(Sprite sprite)
        {
            var sr = ResolveRenderer();
            if (sr == null)
            {
                return;
            }

            sr.sprite = sprite;
        }

        private void PlayClipPreview(AnimationClip clip)
        {
            if (clip == null)
            {
                return;
            }

            var animator = GetComponent<Animator>();
            if (animator == null)
            {
                return;
            }

            animator.enabled = true;
            var stateName = clip.name;
            var hash = Animator.StringToHash(stateName);
            if (animator.runtimeAnimatorController != null && animator.HasState(0, hash))
            {
                animator.Play(hash, 0, 0f);
            }
            else
            {
                animator.Play(stateName, 0, 0f);
            }

            animator.Update(0f);
        }

        private void KeepFeetWorld()
        {
            var view = ResolveView();
            if (view == null)
            {
                return;
            }

            view.PlaceFeetAt(view.FeetWorldPosition);
        }

        private void EnsureLayouts()
        {
            if (spriteLayouts == null || spriteLayouts.Length == 0)
            {
                spriteLayouts = new[] { CaptureHandles() };
                spritePreview = 0;
                InferLinkedStates();
                return;
            }

            if (spriteLayouts.Length == 1 && !spriteLayouts[0].HasData)
            {
                spriteLayouts[0] = CaptureHandles();
            }

            InferLinkedStates();
        }

        private void InferLinkedStates()
        {
            if (spriteLayouts == null || spriteLayouts.Length == 0)
            {
                return;
            }

            var changed = false;
            for (var i = 0; i < spriteLayouts.Length; i++)
            {
                var layout = spriteLayouts[i];
                if (layout.linkedState != UnitCombatVisualState.None)
                {
                    continue;
                }

                var inferred = UnitSpriteLayout.InferLinkedState(layout.displayName);
                if (inferred == UnitCombatVisualState.None)
                {
                    continue;
                }

                layout.linkedState = inferred;
                spriteLayouts[i] = layout;
                changed = true;
            }

            if (spriteLayouts.Length == 1 && spriteLayouts[0].linkedState == UnitCombatVisualState.None)
            {
                var layout = spriteLayouts[0];
                layout.linkedState = UnitCombatVisualState.Idle;
                spriteLayouts[0] = layout;
                changed = true;
            }

            if (changed && !Application.isPlaying)
            {
                MarkDirty();
            }
        }

        public void EnsureHandles()
        {
            var view = ResolveView();
            view?.EnsureInteractionColliders();
        }

        private void HoldAnimator()
        {
            var animator = GetComponent<Animator>();
            if (animator == null || _animatorHeld)
            {
                return;
            }

            _heldAnimatorSpeed = animator.speed;
            animator.speed = 0f;
            _animatorHeld = true;
        }

        private void RestoreAnimator()
        {
            if (!_animatorHeld)
            {
                return;
            }

            var animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.speed = _heldAnimatorSpeed > 0.01f ? _heldAnimatorSpeed : 1f;
            }

            _animatorHeld = false;
        }

        private void SetAnimatorEnabled(bool enabled)
        {
            var animator = GetComponent<Animator>();
            if (animator == null)
            {
                return;
            }

            if (!enabled)
            {
                if (!_animatorHeld)
                {
                    _heldAnimatorSpeed = animator.speed;
                    _animatorHeld = true;
                }

                animator.enabled = false;
                return;
            }

            animator.enabled = true;
            if (_animatorHeld && !_previewLocked)
            {
                animator.speed = _heldAnimatorSpeed > 0.01f ? _heldAnimatorSpeed : 1f;
                _animatorHeld = false;
            }
        }

        private UnitView ResolveView()
        {
            if (_view == null)
            {
                _view = GetComponent<UnitView>();
            }

            return _view;
        }

        private SpriteRenderer ResolveRenderer()
        {
            var view = ResolveView();
            if (view != null)
            {
                return view.BodySpriteRenderer;
            }

            return GetComponent<SpriteRenderer>();
        }

        private Transform ResolveFeet()
        {
            var view = ResolveView();
            if (view != null && view.FeetAnchor != null)
            {
                return view.FeetAnchor.transform;
            }

            var existing = transform.Find("FeetAnchor");
            return existing;
        }

        private int ClampPreview(int preview)
        {
            EnsureLayouts();
            return Mathf.Clamp(preview, 0, spriteLayouts.Length - 1);
        }

        private void MarkDirty()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            if (_view != null)
            {
                EditorUtility.SetDirty(_view);
            }
#endif
        }
    }
}

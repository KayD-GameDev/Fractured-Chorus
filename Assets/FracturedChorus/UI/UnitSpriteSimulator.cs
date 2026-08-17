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
        }

        private void OnDisable()
        {
            RestoreAnimator();
        }

        private void LateUpdate()
        {
            if (_previewLocked || !Application.isPlaying)
            {
                return;
            }

            TryApplyLayoutForCurrentSprite();
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
            copy.displayName = string.Empty;
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
            spriteLayouts[i] = layout;
            ApplySprite(sprite);
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
                    ApplyLayout(layout, keepWorldFeet: false);
                }
                else if (layout.sprite != null)
                {
                    ApplySprite(layout.sprite);
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

            ApplyLayout(layout, keepWorldFeet: true);
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

        private void ApplyLayout(UnitSpriteLayout layout, bool keepWorldFeet)
        {
            var view = ResolveView();
            var feet = view != null ? view.FeetWorldPosition : transform.position;
            if (layout.sprite != null)
            {
                ApplySprite(layout.sprite);
            }

            if (layout.localScale.sqrMagnitude > 0.0001f)
            {
                transform.localScale = layout.localScale;
            }

            var feetTf = ResolveFeet();
            if (feetTf != null && layout.HasData)
            {
                feetTf.localPosition = layout.feetAnchorLocal;
            }

            if (keepWorldFeet)
            {
                view?.PlaceFeetAt(feet);
            }
        }

        private UnitSpriteLayout CaptureHandles()
        {
            var scale = transform.localScale;
            if (scale.sqrMagnitude < 0.0001f)
            {
                scale = Vector3.one;
            }

            string displayName = null;
            if (spriteLayouts != null && spriteLayouts.Length > 0)
            {
                var i = Mathf.Clamp(spritePreview, 0, spriteLayouts.Length - 1);
                displayName = spriteLayouts[i].displayName;
            }

            return new UnitSpriteLayout
            {
                displayName = displayName,
                sprite = CurrentSprite,
                localScale = scale,
                feetAnchorLocal = FeetAnchorLocal
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
                return;
            }

            if (spriteLayouts.Length == 1 && !spriteLayouts[0].HasData)
            {
                spriteLayouts[0] = CaptureHandles();
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

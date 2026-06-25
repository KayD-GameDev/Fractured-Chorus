using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
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
        [Tooltip("Giữ sprite/màu/scale Transform đã chỉnh trong scene.")]
        [SerializeField] private bool preserveSceneVisuals = true;
        [Tooltip("Giữ size/offset BoxCollider2D đã chỉnh trong scene — dùng làm vùng click + kéo thả.")]
        [SerializeField] private bool preserveSceneCollider = true;

        public CombatUnit Unit { get; private set; }
        public UnitPresetSO Preset => preset;
        public string DemoUnitKey => demoUnitKey;
        public UnitFeetAnchor FeetAnchor => feetAnchor;

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
        public bool IsPlacedOnGrid => HoneycombIndex.IsValidIndex(row) && HoneycombIndex.IsValidIndex(column);
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
            if (hpLabel == null)
            {
                var labelTransform = transform.Find("HpLabel");
                if (labelTransform != null && labelTransform.IsChildOf(transform))
                {
                    hpLabel = labelTransform.GetComponent<TextMesh>();
                }

                if (hpLabel == null)
                {
                    var labelGo = new GameObject("HpLabel");
                    labelGo.transform.SetParent(transform, false);
                    labelGo.transform.localPosition = new Vector3(0f, -0.7f, 0f);
                    hpLabel = labelGo.AddComponent<TextMesh>();
                    hpLabel.characterSize = 0.08f;
                    hpLabel.fontSize = 48;
                    hpLabel.anchor = TextAnchor.MiddleCenter;
                    hpLabel.color = Color.white;
                }
            }
            else if (!hpLabel.transform.IsChildOf(transform))
            {
                hpLabel = null;
                EnsureVisuals();
            }
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

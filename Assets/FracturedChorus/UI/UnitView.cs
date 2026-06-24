using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Unit in scene — grid row/column assigned at runtime when placed on a honeycomb cell.
    /// </summary>
    public class UnitView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
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
        [SerializeField] private BoxCollider clickCollider;

        public CombatUnit Unit { get; private set; }
        public UnitPresetSO Preset => preset;
        public string DemoUnitKey => demoUnitKey;

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

        private System.Action<CombatUnit, UnitView> _onSelected;
        private BoardDragController _dragController;
        private bool _dragStarted;
        private bool _suppressClick;
        private Vector2 _dragStartScreen;
        private const float ClickDragThresholdPx = 8f;

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

        public void Bind(CombatUnit unit, System.Action<CombatUnit, UnitView> onSelected,
            BoardDragController dragController = null)
        {
            if (Unit != null)
            {
                Unit.OnHpChanged -= HandleHpChanged;
            }

            Unit = unit;
            _onSelected = onSelected;
            _dragController = dragController;
            EnsureVisuals();
            ApplyVisuals();
            unit.OnHpChanged += HandleHpChanged;
            RefreshHp();
        }

        private void EnsureVisuals()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            if (spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = CreatePlaceholderSprite();
            }

            if (clickCollider == null)
            {
                clickCollider = GetComponent<BoxCollider>();
                if (clickCollider == null)
                {
                    clickCollider = gameObject.AddComponent<BoxCollider>();
                }

                clickCollider.size = Vector3.one;
            }

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

            spriteRenderer.color = Unit.PlaceholderColor;
            spriteRenderer.sortingOrder = 10 + Unit.GridPosition.Row;
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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_suppressClick || _dragStarted)
            {
                return;
            }

            if (Unit != null && Unit.IsAlive && Unit.Side == GridSide.Player)
            {
                _onSelected?.Invoke(Unit, this);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragStarted = false;
            _suppressClick = false;
            _dragStartScreen = eventData.position;
            if (_dragController == null || !_dragController.CanDragUnit(this))
            {
                return;
            }

            _dragStarted = true;
            _dragController.BeginDrag(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragStarted || _dragController == null)
            {
                return;
            }

            _dragController.UpdateDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragStarted || _dragController == null)
            {
                return;
            }

            _dragController.EndDrag(this);
            _suppressClick = Vector2.Distance(_dragStartScreen, eventData.position) > ClickDragThresholdPx;
            _dragStarted = false;
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
    }
}

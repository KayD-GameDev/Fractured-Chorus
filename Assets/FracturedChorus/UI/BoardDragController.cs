using System;
using System.Collections.Generic;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Units;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FracturedChorus.UI
{
    /// <summary>
    /// Press-and-hold on a player unit to drag between grid cells (Planning, pre-Execute).
    /// Click without drag after Execute opens the skill panel.
    /// Uses Physics2D pick — reliable with Screen Space Overlay UI + Input System.
    /// </summary>
    public class BoardDragController : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private float cellPickRadius = 1.15f;
        [SerializeField] private float clickDragThresholdPx = 8f;

        private CombatSession _session;
        private DualGrid _grid;
        private readonly Dictionary<GridPosition, GridCellMarker> _markers = new();
        private readonly List<RaycastResult> _uiRaycastBuffer = new();
        private readonly Collider2D[] _overlapHits = new Collider2D[8];
        private ContactFilter2D _unitPickFilter;
        private GridCellMarker _highlightedCell;
        private UnitView _draggingUnit;
        private UnitView _pointerDownUnit;
        private Vector2 _pointerDownScreen;
        private bool _dragPointerActive;
        private Action<CombatUnit, UnitView> _onUnitClicked;
        private Action _onFormationChanged;

        private void Awake()
        {
            _unitPickFilter.useLayerMask = true;
            _unitPickFilter.layerMask = Physics2D.AllLayers;
            _unitPickFilter.useTriggers = false;
        }

        public bool IsDragging => _draggingUnit != null;

        public bool IsPreExecuteRepositionPhase =>
            _session != null && _session.Phase == CombatPhase.Planning && _session.AllowPlayerReposition;

        public void Initialize(CombatSession session, DualGrid grid, IEnumerable<GridCellMarker> markers,
            Camera camera = null)
        {
            _session = session;
            _grid = grid;
            worldCamera = camera != null ? camera : Camera.main;
            _markers.Clear();

            if (markers == null)
            {
                return;
            }

            foreach (var marker in markers)
            {
                if (marker == null)
                {
                    continue;
                }

                _markers[marker.Position] = marker;
            }
        }

        public void SetUnitClickHandler(Action<CombatUnit, UnitView> onUnitClicked)
        {
            _onUnitClicked = onUnitClicked;
        }

        public void SetFormationChangedHandler(Action onFormationChanged)
        {
            _onFormationChanged = onFormationChanged;
        }

        public bool CanDragUnit(UnitView view)
        {
            return view != null
                   && view.Unit != null
                   && view.Unit.IsAlive
                   && view.Side == GridSide.Player
                   && _session != null
                   && _session.Phase == CombatPhase.Planning
                   && _session.AllowPlayerReposition;
        }

        private void Update()
        {
            if (_session == null)
            {
                return;
            }

            var screenPos = GetPointerScreenPosition();

            if (WasPointerPressedThisFrame())
            {
                HandlePointerDown(screenPos);
            }

            if (IsPointerHeld() && _dragPointerActive && _draggingUnit != null)
            {
                UpdateDragAtScreen(screenPos);
            }

            if (WasPointerReleasedThisFrame())
            {
                HandlePointerUp(screenPos);
            }
        }

        private void HandlePointerDown(Vector2 screenPos)
        {
            _pointerDownUnit = null;
            _dragPointerActive = false;

            if (IsScreenPointBlockedByUi(screenPos))
            {
                return;
            }

            var view = PickUnitAtScreen(screenPos);
            if (view == null)
            {
                return;
            }

            _pointerDownUnit = view;
            _pointerDownScreen = screenPos;

            if (CanDragUnit(view))
            {
                BeginDrag(view);
                _dragPointerActive = true;
            }
        }

        private void HandlePointerUp(Vector2 screenPos)
        {
            var moved = _pointerDownScreen != Vector2.zero
                        && Vector2.Distance(_pointerDownScreen, screenPos) > clickDragThresholdPx;

            if (_dragPointerActive && _draggingUnit != null)
            {
                EndDrag(_draggingUnit);
            }
            else if (!moved && _pointerDownUnit != null && CanOpenSkillPanelFor(_pointerDownUnit))
            {
                _onUnitClicked?.Invoke(_pointerDownUnit.Unit, _pointerDownUnit);
            }

            _pointerDownUnit = null;
            _dragPointerActive = false;
        }

        private bool CanOpenSkillPanelFor(UnitView view)
        {
            return view != null
                   && view.Unit != null
                   && view.Unit.IsAlive
                   && view.Side == GridSide.Player
                   && _session != null
                   && !IsPreExecuteRepositionPhase;
        }

        public void CancelActiveDrag()
        {
            if (_draggingUnit == null)
            {
                return;
            }

            CancelDrag(_draggingUnit);
            _dragPointerActive = false;
            _pointerDownUnit = null;
        }

        public void BeginDrag(UnitView view)
        {
            if (!CanDragUnit(view))
            {
                return;
            }

            _draggingUnit = view;
            ClearHighlight();
        }

        public void UpdateDragAtScreen(Vector2 screenPos)
        {
            if (_draggingUnit == null || !CanDragUnit(_draggingUnit))
            {
                return;
            }

            var world = ScreenToWorld(screenPos);
            _draggingUnit.PlaceFeetAt(new Vector3(world.x, world.y, _draggingUnit.transform.position.z));
            SetHighlight(FindDropCell(_draggingUnit.FeetWorldPosition, _draggingUnit.Side), _draggingUnit);
        }

        public void EndDrag(UnitView view)
        {
            if (_draggingUnit != view)
            {
                ClearHighlight();
                _draggingUnit = null;
                return;
            }

            var target = FindDropCell(view.FeetWorldPosition, view.Side);
            ClearHighlight();

            if (!CanDragUnit(view))
            {
                if (_markers.TryGetValue(view.GridPosition, out var lockedHome))
                {
                    SnapUnitToCell(view, lockedHome);
                }

                _draggingUnit = null;
                return;
            }

            if (target != null && IsValidDrop(target, view) && _grid != null && view.Unit != null)
            {
                var oldPosition = view.GridPosition;
                var moved = false;

                if (target.Position.Equals(oldPosition))
                {
                    SnapUnitToCell(view, target);
                    moved = true;
                }
                else if (_grid.IsOccupied(target.Position))
                {
                    moved = TrySwapUnits(view, target, oldPosition);
                }
                else if (_grid.TryMoveUnit(view.Unit, target.Position))
                {
                    view.PlaceOnGrid(target.Position);
                    SnapUnitToCell(view, target);
                    moved = true;
                }

                if (moved)
                {
                    _onFormationChanged?.Invoke();
                }
                else if (_markers.TryGetValue(oldPosition, out var home))
                {
                    SnapUnitToCell(view, home);
                }
            }
            else if (_markers.TryGetValue(view.GridPosition, out var fallback))
            {
                SnapUnitToCell(view, fallback);
            }

            _draggingUnit = null;
        }

        public void CancelDrag(UnitView view)
        {
            if (_draggingUnit != view)
            {
                return;
            }

            ClearHighlight();
            if (_markers.TryGetValue(view.GridPosition, out var home))
            {
                SnapUnitToCell(view, home);
            }

            _draggingUnit = null;
        }

        private UnitView PickUnitAtScreen(Vector2 screenPos)
        {
            var world = ScreenToWorld(screenPos);
            var count = Physics2D.OverlapPoint(new Vector2(world.x, world.y), _unitPickFilter, _overlapHits);

            UnitView best = null;
            var bestOrder = int.MinValue;

            for (var i = 0; i < count; i++)
            {
                var hit = _overlapHits[i];
                if (hit == null)
                {
                    continue;
                }

                var view = hit.GetComponent<UnitView>() ?? hit.GetComponentInParent<UnitView>();
                if (view == null || view.Unit == null || !view.Unit.IsAlive)
                {
                    continue;
                }

                var sr = view.GetComponent<SpriteRenderer>();
                var order = sr != null ? sr.sortingOrder : 0;
                if (order < bestOrder)
                {
                    continue;
                }

                bestOrder = order;
                best = view;
            }

            return best;
        }

        private bool IsScreenPointBlockedByUi(Vector2 screenPos)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            var pointerData = new PointerEventData(EventSystem.current) { position = screenPos };
            _uiRaycastBuffer.Clear();
            EventSystem.current.RaycastAll(pointerData, _uiRaycastBuffer);

            foreach (var result in _uiRaycastBuffer)
            {
                if (result.module is not GraphicRaycaster)
                {
                    continue;
                }

                var go = result.gameObject;
                if (go.GetComponentInParent<BeatTimelineUIView>() != null)
                {
                    return true;
                }

                if (go.GetComponentInParent<SkillPanelUIView>() != null)
                {
                    return true;
                }

                if (go.GetComponentInParent<CombatExecuteOverlayUIView>() != null)
                {
                    return true;
                }

                if (go.GetComponent<Button>() != null || go.GetComponent<ScrollRect>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private GridCellMarker FindDropCell(Vector3 world, GridSide side)
        {
            GridCellMarker best = null;
            var bestDist = float.MaxValue;

            foreach (var pair in _markers)
            {
                var marker = pair.Value;
                if (marker == null || marker.Side != side)
                {
                    continue;
                }

                var cellPos = marker.transform.position;
                var dist = Vector2.Distance(new Vector2(world.x, world.y), new Vector2(cellPos.x, cellPos.y));
                if (dist > cellPickRadius || dist >= bestDist)
                {
                    continue;
                }

                bestDist = dist;
                best = marker;
            }

            return best;
        }

        private void SetHighlight(GridCellMarker marker, UnitView view)
        {
            if (!IsValidDrop(marker, view))
            {
                marker = null;
            }

            if (_highlightedCell == marker)
            {
                return;
            }

            _highlightedCell?.SetDropHighlight(false);
            _highlightedCell = marker;
            _highlightedCell?.SetDropHighlight(true);
        }

        private bool IsValidDrop(GridCellMarker marker, UnitView view)
        {
            if (marker == null || view?.Unit == null || _grid == null)
            {
                return false;
            }

            if (marker.Position.Equals(view.GridPosition))
            {
                return true;
            }

            if (marker.Side != view.Side)
            {
                return false;
            }

            if (!_grid.IsOccupied(marker.Position))
            {
                return true;
            }

            var occupant = _grid.GetOccupant(marker.Position);
            return occupant != null && occupant != view.Unit && occupant.Side == view.Side;
        }

        private bool TrySwapUnits(UnitView draggedView, GridCellMarker target, GridPosition sourcePosition)
        {
            if (draggedView?.Unit == null || _grid == null)
            {
                return false;
            }

            if (!_grid.TrySwapUnits(draggedView.Unit, target.Position))
            {
                return false;
            }

            draggedView.PlaceOnGrid(target.Position);
            SnapUnitToCell(draggedView, target);

            var swappedUnit = _grid.GetOccupant(sourcePosition);
            if (swappedUnit == null)
            {
                return true;
            }

            var swappedView = FindViewForUnit(swappedUnit);
            if (swappedView == null)
            {
                return true;
            }

            swappedView.PlaceOnGrid(sourcePosition);
            if (_markers.TryGetValue(sourcePosition, out var sourceMarker))
            {
                SnapUnitToCell(swappedView, sourceMarker);
            }

            return true;
        }

        private static UnitView FindViewForUnit(CombatUnit unit)
        {
            if (unit == null)
            {
                return null;
            }

            foreach (var view in UnityEngine.Object.FindObjectsByType<UnitView>(FindObjectsInactive.Exclude))
            {
                if (view != null && view.Unit == unit)
                {
                    return view;
                }
            }

            return null;
        }

        private void ClearHighlight()
        {
            _highlightedCell?.SetDropHighlight(false);
            _highlightedCell = null;
        }

        private static void SnapUnitToCell(UnitView view, GridCellMarker marker)
        {
            if (view == null || marker == null)
            {
                return;
            }

            var pos = marker.transform.position;
            var gridPos = view.GridPosition;
            var depth = gridPos.Row * 0.1f + gridPos.Column * 0.05f;
            view.SnapFeetTo(new Vector3(pos.x, pos.y, 0f), -0.05f + depth);
        }

        private Vector3 ScreenToWorld(Vector2 screenPoint)
        {
            var cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null)
            {
                return Vector3.zero;
            }

            var depth = Mathf.Abs(cam.transform.position.z);
            return cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, depth));
        }

        private static Vector2 GetPointerScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                return Touchscreen.current.primaryTouch.position.ReadValue();
            }
#endif
            return Input.mousePosition;
        }

        private static bool WasPointerPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }

            return false;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        private static bool IsPointerHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                return true;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                return true;
            }

            return false;
#else
            return Input.GetMouseButton(0);
#endif
        }

        private static bool WasPointerReleasedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                return true;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            {
                return true;
            }

            return false;
#else
            return Input.GetMouseButtonUp(0);
#endif
        }
    }
}

using System.Collections.Generic;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FracturedChorus.UI
{
    public class BoardDragController : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private float cellPickRadius = 1.15f;

        private CombatSession _session;
        private DualGrid _grid;
        private readonly Dictionary<GridPosition, GridCellMarker> _markers = new();
        private GridCellMarker _highlightedCell;
        private UnitView _draggingUnit;

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

        public void CancelActiveDrag()
        {
            if (_draggingUnit == null)
            {
                return;
            }

            CancelDrag(_draggingUnit);
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

        public void UpdateDrag(PointerEventData eventData)
        {
            if (_draggingUnit == null || eventData == null || !CanDragUnit(_draggingUnit))
            {
                return;
            }

            var world = ScreenToWorld(eventData.position);
            _draggingUnit.transform.position = new Vector3(world.x, world.y, _draggingUnit.transform.position.z);
            SetHighlight(FindDropCell(world, _draggingUnit.Side), _draggingUnit);
        }

        public void EndDrag(UnitView view)
        {
            if (_draggingUnit != view)
            {
                ClearHighlight();
                _draggingUnit = null;
                return;
            }

            var world = view.transform.position;
            var target = FindDropCell(world, view.Side);
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
                if (_grid.TryMoveUnit(view.Unit, target.Position))
                {
                    view.PlaceOnGrid(target.Position);
                    SnapUnitToCell(view, target);
                }
                else if (_markers.TryGetValue(view.GridPosition, out var home))
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

            return !_grid.IsOccupied(marker.Position);
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
            view.transform.position = new Vector3(pos.x, pos.y, -0.05f + depth);
        }

        private Vector3 ScreenToWorld(Vector2 screenPoint)
        {
            var cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null)
            {
                return Vector3.zero;
            }

            var depth = Mathf.Abs(cam.transform.position.z);
            var world = cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, depth));
            return world;
        }
    }
}

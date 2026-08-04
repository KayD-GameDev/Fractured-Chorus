using FracturedChorus.Combat.Units;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FracturedChorus.UI
{
    public class TimelineLaneSkillDragHandle : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private BeatTimelineUIView _timeline;
        private CombatUnit _unit;
        private int _placementBeat;
        private bool _dragging;
        private Vector2 _lastScreenPos;

        public void Configure(BeatTimelineUIView timeline, CombatUnit unit, int placementBeat)
        {
            _timeline = timeline;
            _unit = unit;
            _placementBeat = placementBeat;
        }

        public void SetInteractionEnabled(bool enabled)
        {
            var image = GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.raycastTarget = enabled;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = false;
            ResolveTimeline();

            if (_timeline == null || !_timeline.CanRelocateLaneMarker())
            {
                return;
            }

            _lastScreenPos = eventData.position;
            _dragging = _timeline.TryBeginLaneMarkerRelocate(_unit, _placementBeat);
            if (_dragging)
            {
                _timeline.UpdateLaneMarkerRelocate(eventData.position);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || _timeline == null)
            {
                return;
            }

            _lastScreenPos = eventData.position;
            _timeline.UpdateLaneMarkerRelocate(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            FinishDrag(eventData != null ? eventData.position : _lastScreenPos);
        }

        private void OnDisable()
        {
            if (!_dragging)
            {
                return;
            }

            FinishDrag(_lastScreenPos);
        }

        private void FinishDrag(Vector2 screenPos)
        {
            if (!_dragging)
            {
                return;
            }

            _dragging = false;
            ResolveTimeline();
            _timeline?.EndLaneMarkerRelocate(screenPos);
        }

        private void ResolveTimeline()
        {
            if (_timeline == null)
            {
                _timeline = GetComponentInParent<BeatTimelineUIView>();
            }
        }
    }
}

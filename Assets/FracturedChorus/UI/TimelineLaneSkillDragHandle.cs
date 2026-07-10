using FracturedChorus.Combat.Units;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FracturedChorus.UI
{
    /// <summary>Raycast target + drag handle for relocating an assigned skill on a character lane.</summary>
    public class TimelineLaneSkillDragHandle : MonoBehaviour,
        IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private BeatTimelineUIView _timeline;
        private CombatUnit _unit;
        private int _placementBeat;
        private bool _dragging;

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

        public void OnPointerDown(PointerEventData eventData)
        {
            ResolveTimeline();
            if (_timeline == null || !_timeline.CanRelocateLaneMarker())
            {
                return;
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

            _timeline.UpdateLaneMarkerRelocate(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging || _timeline == null)
            {
                return;
            }

            _timeline.EndLaneMarkerRelocate(eventData.position);
            _dragging = false;
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

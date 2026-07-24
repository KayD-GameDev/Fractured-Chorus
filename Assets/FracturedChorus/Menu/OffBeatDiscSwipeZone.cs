using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.Menu
{
    public sealed class OffBeatDiscSwipeZone : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private float swipeThresholdPx = 80f;
        [SerializeField] private float maxVerticalRatio = 0.65f;

        private Vector2 _start;
        private bool _dragging;
        private bool _swiped;

        public event Action SwipeNext;
        public event Action SwipePrevious;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsOnButton(eventData))
            {
                _dragging = false;
                return;
            }

            _dragging = true;
            _swiped = false;
            _start = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || _swiped)
            {
                return;
            }

            var delta = eventData.position - _start;
            if (Mathf.Abs(delta.x) < swipeThresholdPx)
            {
                return;
            }

            if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x) * maxVerticalRatio)
            {
                return;
            }

            _swiped = true;
            if (delta.x < 0f)
            {
                SwipeNext?.Invoke();
            }
            else
            {
                SwipePrevious?.Invoke();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
        }

        private static bool IsOnButton(PointerEventData eventData)
        {
            if (eventData.pointerEnter != null &&
                eventData.pointerEnter.GetComponentInParent<Button>() != null)
            {
                return true;
            }

            return eventData.pointerPress != null &&
                   eventData.pointerPress.GetComponentInParent<Button>() != null;
        }
    }
}

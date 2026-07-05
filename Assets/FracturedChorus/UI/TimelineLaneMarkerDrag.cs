using System;
using FracturedChorus.Combat.Units;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FracturedChorus.UI
{
    /// <summary>Drag assigned skill marker off timeline to remove during planning.</summary>
    public class TimelineLaneMarkerDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private CombatUnit _unit;
        private int _placementBeat;
        private RectTransform _rect;
        private Canvas _canvas;
        private Vector2 _dragOffset;
        private Func<bool> _canDrag;
        private Action<CombatUnit, int> _onRemove;
        private Action _onSnapBack;
        private bool _removedOnBegin;

        public void Configure(
            CombatUnit unit,
            int placementBeat,
            Func<bool> canDrag,
            Action<CombatUnit, int> onRemove,
            Action onSnapBack = null)
        {
            _unit = unit;
            _placementBeat = placementBeat;
            _canDrag = canDrag;
            _onRemove = onRemove;
            _onSnapBack = onSnapBack;
            _rect = transform as RectTransform;
            _canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _removedOnBegin = false;

            if (_canDrag != null && !_canDrag())
            {
                return;
            }

            _onRemove?.Invoke(_unit, _placementBeat);
            _removedOnBegin = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_removedOnBegin || _rect == null || _canvas == null)
            {
                return;
            }

            if (_canDrag != null && !_canDrag())
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rect.parent as RectTransform,
                    eventData.position,
                    _canvas.worldCamera,
                    out var local))
            {
                _rect.anchoredPosition = local - _dragOffset;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_removedOnBegin)
            {
                return;
            }

            if (_canDrag != null && !_canDrag())
            {
                return;
            }

            _onSnapBack?.Invoke();
        }
    }
}

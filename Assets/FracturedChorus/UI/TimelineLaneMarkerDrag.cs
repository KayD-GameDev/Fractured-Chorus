using System;
using FracturedChorus.Combat.Units;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FracturedChorus.UI
{
    /// <summary>Drag assigned skill marker off timeline to remove during planning.</summary>
    public class TimelineLaneMarkerDrag : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        private CombatUnit _unit;
        private int _placementBeat;
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

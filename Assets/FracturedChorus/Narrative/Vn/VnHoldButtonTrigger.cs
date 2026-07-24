using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FracturedChorus.Narrative.Vn
{
    public sealed class VnHoldButtonTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public event Action Pressed;
        public event Action Released;

        private bool _held;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_held)
            {
                return;
            }

            _held = true;
            Pressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Release();
        }

        private void Release()
        {
            if (!_held)
            {
                return;
            }

            _held = false;
            Released?.Invoke();
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.RunMap.UI
{
    /// <summary>Forwards mouse wheel from map node buttons to the parent ScrollRect.</summary>
    public class MapNodeScrollForwarder : MonoBehaviour, IScrollHandler
    {
        private RunMapScrollDriver _scrollDriver;
        private ScrollRect _scrollRect;

        private void Awake()
        {
            ResolveScrollTargets();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (_scrollDriver == null && _scrollRect == null)
            {
                ResolveScrollTargets();
            }

            if (_scrollDriver != null)
            {
                _scrollDriver.ApplyWheelScroll(eventData);
                return;
            }

            _scrollRect?.OnScroll(eventData);
        }

        private void ResolveScrollTargets()
        {
            _scrollDriver = GetComponentInParent<RunMapScrollDriver>();
            _scrollRect = _scrollDriver != null
                ? _scrollDriver.ScrollRect
                : GetComponentInParent<ScrollRect>();
        }
    }
}

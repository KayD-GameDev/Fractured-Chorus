using UnityEngine;
using UnityEngine.EventSystems;

namespace FracturedChorus.Narrative
{
    public class PrologueSignaturePointerRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        private PrologueSignaturePad _pad;

        public void Bind(PrologueSignaturePad pad)
        {
            _pad = pad;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pad?.ForwardPointerDown(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _pad?.ForwardDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pad?.ForwardPointerUp(eventData);
        }
    }
}

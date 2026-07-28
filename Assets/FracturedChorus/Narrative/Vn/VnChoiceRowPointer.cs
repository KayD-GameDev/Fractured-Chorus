using UnityEngine;
using UnityEngine.EventSystems;

namespace FracturedChorus.Narrative.Vn
{
    public sealed class VnChoiceRowPointer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private VnChoiceView _view;
        private int _optionIndex;

        public void Initialize(VnChoiceView view, int optionIndex)
        {
            _view = view;
            _optionIndex = optionIndex;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _view?.SetPointerHover(_optionIndex);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _view?.ClearPointerHover(_optionIndex);
        }
    }
}

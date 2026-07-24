using UnityEngine;
using UnityEngine.EventSystems;

namespace FracturedChorus.Narrative
{
    public class PrologueChoiceOptionHitbox : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private PrologueChoiceView _view;
        private int _optionIndex;

        public void Initialize(PrologueChoiceView view, int optionIndex)
        {
            _view = view;
            _optionIndex = optionIndex;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _view?.HoverOption(_optionIndex);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _view?.HoverExitOption(_optionIndex);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _view?.ClickOption(_optionIndex);
        }
    }
}

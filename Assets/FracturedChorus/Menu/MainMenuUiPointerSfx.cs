using UnityEngine;
using UnityEngine.EventSystems;

namespace FracturedChorus.Menu
{
    public class MainMenuUiPointerSfx : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private MainMenuStartGameController _controller;
        private MainMenuButtonRowView _rowView;

        public void Bind(MainMenuStartGameController controller, MainMenuButtonRowView rowView = null)
        {
            _controller = controller;
            _rowView = rowView;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _rowView?.NotifyPointerEnter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _rowView?.NotifyPointerExit();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            var button = GetComponentInParent<UnityEngine.UI.Button>();
            if (button != null && !button.interactable)
            {
                return;
            }

            _controller?.PlayButtonPressSfx();
        }
    }
}

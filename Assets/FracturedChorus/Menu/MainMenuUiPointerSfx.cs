using UnityEngine;
using UnityEngine.EventSystems;

namespace FracturedChorus.Menu
{
    public class MainMenuUiPointerSfx : MonoBehaviour, IPointerDownHandler
    {
        private MainMenuStartGameController _controller;

        public void Bind(MainMenuStartGameController controller)
        {
            _controller = controller;
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

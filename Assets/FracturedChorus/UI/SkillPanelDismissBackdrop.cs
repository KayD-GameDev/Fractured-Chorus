using UnityEngine;
using UnityEngine.EventSystems;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Full-screen transparent hit area — click closes the skill panel.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class SkillPanelDismissBackdrop : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private SkillPanelUIView panel;

        public void SetPanel(SkillPanelUIView skillPanel)
        {
            panel = skillPanel;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (panel != null && panel.ShouldIgnoreOutsideDismiss)
            {
                return;
            }

            panel?.Hide();
        }
    }
}

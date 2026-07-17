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
            if (panel == null)
            {
                return;
            }

            // W/A/D (or armed) drop: backdrop sits above the timeline, so lane clicks
            // hit here first — treat as place attempt instead of dismissing the panel.
            if (panel.TryConsumeArmedSkillDrop(eventData.position))
            {
                return;
            }

            if (panel.ShouldIgnoreOutsideDismiss)
            {
                return;
            }

            panel.Hide();
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class SkillPanelDismissBackdrop : MonoBehaviour, IPointerClickHandler, ICanvasRaycastFilter
    {
        [SerializeField] private SkillPanelUIView panel;

        private BeatTimelineUIView _timeline;

        public void SetPanel(SkillPanelUIView skillPanel)
        {
            panel = skillPanel;
        }

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (panel == null)
            {
                return true;
            }

            if (panel.IsSkillArmed)
            {
                return true;
            }

            if (_timeline == null)
            {
                _timeline = FindAnyObjectByType<BeatTimelineUIView>();
            }

            if (_timeline != null && _timeline.IsScreenPointInViewport(sp))
            {
                return false;
            }

            return true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (panel == null)
            {
                return;
            }

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

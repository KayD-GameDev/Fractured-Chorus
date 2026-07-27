using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Full-screen transparent hit area — click closes the skill panel.
    /// Không chắn raycast lên BeatTimelineUI để vẫn kéo skill trên lane khi panel đang mở.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class SkillPanelDismissBackdrop : MonoBehaviour, IPointerClickHandler, ICanvasRaycastFilter
    {
        [SerializeField] private SkillPanelUIView panel;

        private BeatTimelineUIView _timeline;

        public void SetPanel(SkillPanelUIView skillPanel)
        {
            panel = skillPanel;
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (panel == null)
            {
                return true;
            }

            // Armed drop: backdrop vẫn nhận click để consume drop.
            if (panel.IsSkillArmed)
            {
                return true;
            }

            if (_timeline == null)
            {
                _timeline = FindAnyObjectByType<BeatTimelineUIView>();
            }

            // Cho phép event xuyên xuống timeline viewport (kéo/relocate trên lane).
            if (_timeline != null && _timeline.IsScreenPointInViewport(screenPoint))
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

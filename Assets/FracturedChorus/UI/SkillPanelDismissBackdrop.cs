using UnityEngine;
using UnityEngine.EventSystems;

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
            // Cho phép event xuyên xuống timeline (kéo/relocateskill trên lane).
            if (IsOverBeatTimeline(screenPoint, eventCamera))
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

        private bool IsOverBeatTimeline(Vector2 screenPoint, Camera eventCamera)
        {
            if (_timeline == null)
            {
                _timeline = FindAnyObjectByType<BeatTimelineUIView>();
            }

            if (_timeline == null)
            {
                return false;
            }

            var timelineRect = _timeline.transform as RectTransform;
            if (timelineRect == null)
            {
                return false;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(timelineRect, screenPoint, eventCamera);
        }
    }
}

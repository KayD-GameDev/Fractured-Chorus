using FracturedChorus.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Narrative.Vn
{
    public sealed class VnStoryDateHud : MonoBehaviour
    {
        [SerializeField] private Image bannerImage;
        [SerializeField] private Text dateLabel;
        [SerializeField] private Text phaseLabel;
        [SerializeField] private Image phaseIcon;
        [SerializeField] private Sprite sunSprite;
        [SerializeField] private Sprite moonSprite;
        [SerializeField] private Sprite dawnSprite;

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void ShowStatic(string date, string phase, bool useMoon = true)
        {
            gameObject.SetActive(true);
            if (dateLabel != null)
            {
                dateLabel.text = date ?? string.Empty;
            }

            if (phaseLabel != null)
            {
                phaseLabel.text = phase ?? string.Empty;
            }

            if (phaseIcon != null)
            {
                phaseIcon.sprite = useMoon
                    ? (moonSprite != null ? moonSprite : sunSprite)
                    : (sunSprite != null ? sunSprite : moonSprite);
                phaseIcon.enabled = phaseIcon.sprite != null;
            }
        }

        public void ShowFromMeta()
        {
            if (!GameMetaSession.HasSession)
            {
                Hide();
                return;
            }

            var calendar = GameMetaSession.Current.Calendar;
            gameObject.SetActive(true);

            if (dateLabel != null)
            {
                dateLabel.text = calendar.CurrentDate.ToDisplayString();
            }

            if (phaseLabel != null)
            {
                phaseLabel.text = PhaseDisplay(calendar.CurrentPhase);
            }

            if (phaseIcon != null)
            {
                phaseIcon.sprite = ResolvePhaseSprite(calendar.CurrentPhase);
                phaseIcon.enabled = phaseIcon.sprite != null;
            }
        }

        private Sprite ResolvePhaseSprite(DayPhase phase) => phase switch
        {
            DayPhase.Morning => dawnSprite != null ? dawnSprite : sunSprite,
            DayPhase.Day => sunSprite,
            DayPhase.Evening => moonSprite,
            _ => sunSprite
        };

        private static string PhaseDisplay(DayPhase phase) => phase switch
        {
            DayPhase.Morning => "Morning",
            DayPhase.Day => "After School",
            DayPhase.Evening => "Evening",
            _ => phase.ToString()
        };
    }
}

using FracturedChorus.Meta;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class HubCornerInfoHud : MonoBehaviour
    {
        [SerializeField] private Text dateLabel;
        [SerializeField] private Text dayLabel;
        [SerializeField] private Image phaseIcon;
        [SerializeField] private Text locationLabel;
        [SerializeField] private Text taglineLabel;
        [SerializeField] private Sprite sunSprite;
        [SerializeField] private Sprite moonSprite;
        [SerializeField] private Sprite dawnSprite;

        private void Awake()
        {
            WireReferences();
            ApplyFonts();
        }

        public void WireReferences()
        {
            if (dateLabel == null)
            {
                dateLabel = transform.Find("DateLabel")?.GetComponent<Text>();
            }

            if (dayLabel == null)
            {
                dayLabel = transform.Find("DayLabel")?.GetComponent<Text>();
            }

            if (phaseIcon == null)
            {
                phaseIcon = transform.Find("PhaseIcon")?.GetComponent<Image>();
            }

            if (locationLabel == null)
            {
                locationLabel = transform.Find("LocationLabel")?.GetComponent<Text>();
            }

            if (taglineLabel == null)
            {
                taglineLabel = transform.Find("TaglineLabel")?.GetComponent<Text>();
            }
        }

        public void ApplyFonts()
        {
            UiFontCatalog.Apply(dateLabel, UiFontRole.Display);
            UiFontCatalog.Apply(dayLabel, UiFontRole.Display);
            UiFontCatalog.Apply(locationLabel, UiFontRole.Display);
            UiFontCatalog.Apply(taglineLabel, UiFontRole.Body);
        }

        public void Refresh(GameMetaState state)
        {
            if (state == null)
            {
                return;
            }

            var calendar = state.Calendar;
            var date = calendar.CurrentDate;

            if (dateLabel != null)
            {
                dateLabel.text = date.ToCornerHudDateString();
            }

            if (dayLabel != null)
            {
                dayLabel.text = HubDateHudFormat.ShortDay(date.GetDayOfWeek());
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

    }
}

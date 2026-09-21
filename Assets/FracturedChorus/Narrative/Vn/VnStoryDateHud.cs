using FracturedChorus.UI;
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

        private void Awake()
        {
            ApplyFonts();
        }

        public void ApplyFonts()
        {
            UiFontCatalog.Apply(dateLabel, UiFontRole.Display, VnDialoguePanelLayout.DateLabelFontSize);
            UiFontCatalog.Apply(phaseLabel, UiFontRole.DisplaySecondary, VnDialoguePanelLayout.PhaseLabelFontSize);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void ShowStatic(string date, string phase, bool useMoon = true)
        {
            var dayPhase = HubDateHudFormat.ResolvePhaseFromLabel(phase, useMoon);
            if (HubDateHudFormat.TryParseDisplayDate(date, out var gameDate))
            {
                ShowCalendar(gameDate, dayPhase);
                return;
            }

            gameObject.SetActive(true);
            if (dateLabel != null)
            {
                dateLabel.text = date ?? string.Empty;
            }

            if (phaseLabel != null)
            {
                phaseLabel.text = string.Empty;
            }

            ApplyPhaseIcon(dayPhase);
        }

        public void ShowFromMeta()
        {
            if (!GameMetaSession.HasSession)
            {
                Hide();
                return;
            }

            var calendar = GameMetaSession.Current.Calendar;
            ShowCalendar(calendar.CurrentDate, calendar.CurrentPhase);
        }

        public void ShowCalendar(GameDate date, DayPhase phase)
        {
            gameObject.SetActive(true);

            if (dateLabel != null)
            {
                dateLabel.text = date.ToCornerHudDateString();
            }

            if (phaseLabel != null)
            {
                phaseLabel.text = HubDateHudFormat.ShortDay(date.GetDayOfWeek());
            }

            ApplyPhaseIcon(phase);
        }

        private void ApplyPhaseIcon(DayPhase phase)
        {
            if (phaseIcon == null)
            {
                return;
            }

            phaseIcon.sprite = ResolvePhaseSprite(phase);
            phaseIcon.enabled = phaseIcon.sprite != null;
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

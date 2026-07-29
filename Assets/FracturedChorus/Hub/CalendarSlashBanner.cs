using FracturedChorus.Meta;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class CalendarSlashBanner : MonoBehaviour
    {
        [SerializeField] private Image bannerImage;
        [SerializeField] private Text dateLabel;
        [SerializeField] private Text phaseLabel;
        [SerializeField] private Text slotLabel;
        [SerializeField] private Text deadlineLabel;
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
            UiFontCatalog.Apply(dateLabel, UiFontRole.Display);
            UiFontCatalog.Apply(phaseLabel, UiFontRole.DisplaySecondary);
            UiFontCatalog.Apply(slotLabel, UiFontRole.DisplaySecondary);
            UiFontCatalog.Apply(deadlineLabel, UiFontRole.DisplaySecondary);
        }

        public void Refresh(GameMetaState state)
        {
            if (state == null)
            {
                return;
            }

            var calendar = state.Calendar;

            if (dateLabel != null)
            {
                dateLabel.text = calendar.CurrentDate.ToDisplayString();
            }

            if (phaseLabel != null)
            {
                phaseLabel.text = PhaseDisplay(calendar.CurrentPhase);
            }

            if (slotLabel != null)
            {
                slotLabel.text = $"Slot {calendar.SlotsUsedToday}/{CalendarState.MaxSlotsPerDay}";
            }

            if (phaseIcon != null)
            {
                phaseIcon.sprite = ResolvePhaseSprite(calendar.CurrentPhase);
                phaseIcon.enabled = phaseIcon.sprite != null;
            }

            if (deadlineLabel != null)
            {
                var show = state.Flags.Has(StoryFlagIds.VaultQuestActive)
                           && !state.Flags.Has(StoryFlagIds.VaultCleared)
                           && !state.Flags.Has(StoryFlagIds.VaultMissedDeadline);
                deadlineLabel.gameObject.SetActive(show);
                if (show)
                {
                    deadlineLabel.text = $"Vault · {calendar.DaysUntilVaultDeadline}d";
                }
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

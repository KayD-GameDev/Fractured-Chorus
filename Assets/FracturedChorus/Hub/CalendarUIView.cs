using FracturedChorus.UI;
using FracturedChorus.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class CalendarUIView : MonoBehaviour
    {
        [SerializeField] private Text dateLabel;
        [SerializeField] private Text phaseLabel;
        [SerializeField] private Text slotLabel;
        [SerializeField] private Text deadlineLabel;

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
                phaseLabel.text = $"Phase: {calendar.CurrentPhase}";
            }

            if (slotLabel != null)
            {
                slotLabel.text = $"Slot {calendar.SlotsUsedToday}/{CalendarState.MaxSlotsPerDay}";
            }

            if (deadlineLabel != null)
            {
                if (state.Flags.Has(StoryFlagIds.VaultQuestActive)
                    && !state.Flags.Has(StoryFlagIds.VaultCleared)
                    && !state.Flags.Has(StoryFlagIds.VaultMissedDeadline))
                {
                    deadlineLabel.gameObject.SetActive(true);
                    deadlineLabel.text = $"Vault deadline: còn {calendar.DaysUntilVaultDeadline} ngày (20/09)";
                }
                else
                {
                    deadlineLabel.gameObject.SetActive(false);
                }
            }
        }
    }
}

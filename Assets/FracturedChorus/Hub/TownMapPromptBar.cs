using System;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    [Serializable]
    public sealed class TownMapPromptEntry
    {
        public Image Icon;
        public Text Label;
    }

    public sealed class TownMapPromptBar : MonoBehaviour
    {
        [SerializeField] private TownMapPromptEntry travel;
        [SerializeField] private TownMapPromptEntry info;
        [SerializeField] private TownMapPromptEntry confirm;
        [SerializeField] private TownMapPromptEntry close;

        [Header("Gamepad icons")]
        [SerializeField] private Sprite travelSprite;
        [SerializeField] private Sprite infoSprite;
        [SerializeField] private Sprite confirmSprite;
        [SerializeField] private Sprite closeSprite;

        [Header("Keyboard icons")]
        [SerializeField] private Sprite confirmKeySprite;
        [SerializeField] private Sprite closeKeySprite;

        private TownMapPromptScheme _lastScheme = (TownMapPromptScheme)(-1);

        private void Update()
        {
            TownMapInput.RefreshScheme();
            if (_lastScheme == TownMapInput.CurrentScheme)
            {
                return;
            }

            _lastScheme = TownMapInput.CurrentScheme;
            ApplyScheme(_lastScheme);
        }

        public void ApplyDefaultLabels()
        {
            SetLabel(travel, "Travel");
            SetLabel(info, "Menu");
            SetLabel(confirm, "Confirm");
            SetLabel(close, "Close");
            ApplyScheme(TownMapInput.CurrentScheme);
        }

        public void ApplyScheme(TownMapPromptScheme scheme)
        {
            SetEntry(travel, travelSprite);
            SetEntry(info, infoSprite);

            if (scheme == TownMapPromptScheme.Keyboard)
            {
                SetEntry(confirm, confirmKeySprite != null ? confirmKeySprite : confirmSprite);
                SetEntry(close, closeKeySprite != null ? closeKeySprite : closeSprite);
                SetLabel(confirm, "Enter");
                SetLabel(close, "Esc");
            }
            else
            {
                SetEntry(confirm, confirmSprite);
                SetEntry(close, closeSprite);
                SetLabel(confirm, "Confirm");
                SetLabel(close, "Close");
            }
        }

        private static void SetEntry(TownMapPromptEntry entry, Sprite sprite)
        {
            if (entry?.Icon == null)
            {
                return;
            }

            if (sprite != null)
            {
                entry.Icon.sprite = sprite;
            }

            entry.Icon.enabled = entry.Icon.sprite != null;
        }

        private static void SetLabel(TownMapPromptEntry entry, string label)
        {
            if (entry?.Label != null)
            {
                entry.Label.text = label;
            }
        }
    }
}

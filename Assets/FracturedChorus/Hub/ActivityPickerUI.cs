using System;
using System.Collections.Generic;
using FracturedChorus.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class ActivityPickerUI : MonoBehaviour
    {
        [SerializeField] private RectTransform buttonRoot;
        [SerializeField] private Button buttonTemplate;
        [SerializeField] private Text headerLabel;

        private readonly List<Button> _spawnedButtons = new List<Button>();
        private Action<HubActivityOption> _onSelected;

        private void Awake()
        {
            if (buttonTemplate != null)
            {
                buttonTemplate.gameObject.SetActive(false);
            }
        }

        public void Show(DayPhase phase, GameMetaState state, Action<HubActivityOption> onSelected)
        {
            _onSelected = onSelected;
            gameObject.SetActive(true);

            if (headerLabel != null)
            {
                headerLabel.text = phase == DayPhase.Day ? "Chọn hoạt động — Ban ngày" : "Chọn hoạt động — Buổi tối";
            }

            ClearButtons();

            foreach (var option in HubActivityCatalog.GetForPhase(phase))
            {
                if (!CampusHubStoryBeats.IsDungeonActivityAvailable(state, option))
                {
                    continue;
                }

                var button = Instantiate(buttonTemplate, buttonRoot);
                button.gameObject.SetActive(true);

                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = option.Label;
                }

                var captured = option;
                button.onClick.AddListener(() => _onSelected?.Invoke(captured));
                _spawnedButtons.Add(button);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            ClearButtons();
        }

        private void ClearButtons()
        {
            foreach (var button in _spawnedButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            _spawnedButtons.Clear();
        }
    }
}

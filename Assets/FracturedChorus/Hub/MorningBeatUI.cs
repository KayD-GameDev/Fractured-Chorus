using System;
using FracturedChorus.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class MorningBeatUI : MonoBehaviour
    {
        [SerializeField] private Text messageLabel;
        [SerializeField] private Button continueButton;

        private Action _onContinue;

        private void Awake()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(HandleContinue);
            }
        }

        public void Show(GameMetaState state, Action onContinue)
        {
            _onContinue = onContinue;
            gameObject.SetActive(true);

            if (messageLabel != null)
            {
                if (CampusHubStoryBeats.TryGetMorningMessage(state, out var message))
                {
                    messageLabel.text = message;
                }
                else
                {
                    messageLabel.text = "Buổi sáng mới.";
                }
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void HandleContinue()
        {
            _onContinue?.Invoke();
            Hide();
        }
    }
}

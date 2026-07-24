using System;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Narrative.Vn
{
    public sealed class VnConvenienceBarView : MonoBehaviour
    {
        [SerializeField] private Button logButton;
        [SerializeField] private Button autoButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Text autoLabel;
        [SerializeField] private Text skipLabel;
        [SerializeField] private Color normalColor = new Color(0.82f, 0.92f, 1f, 0.92f);
        [SerializeField] private Color activeColor = new Color(0.45f, 0.95f, 1f, 1f);

        public event Action LogClicked;
        public event Action AutoClicked;
        public event Action SkipClicked;

        private bool _autoActive;
        private bool _skipActive;

        private void Awake()
        {
            ApplyFont(autoLabel);
            ApplyFont(skipLabel);
            ApplyFont(logButton?.GetComponentInChildren<Text>(true));
            logButton?.onClick.AddListener(() => LogClicked?.Invoke());
            autoButton?.onClick.AddListener(() => AutoClicked?.Invoke());
            skipButton?.onClick.AddListener(() => SkipClicked?.Invoke());
        }

        public void SetAutoActive(bool active)
        {
            _autoActive = active;
            if (autoLabel != null)
            {
                autoLabel.color = active ? activeColor : normalColor;
            }
        }

        public void SetSkipActive(bool active)
        {
            _skipActive = active;
            if (skipLabel != null)
            {
                skipLabel.color = active ? activeColor : normalColor;
            }
        }

        public bool IsSkipActive => _skipActive;

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private static void ApplyFont(Text text)
        {
            if (text != null)
            {
                VnUiFont.ApplyAssetOnly(text);
            }
        }
    }
}

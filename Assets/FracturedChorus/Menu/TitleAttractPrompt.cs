using UnityEngine;

namespace FracturedChorus.Menu
{
    /// <summary>
    /// Nhấp nháy dòng "PRESS ANY KEY" trên attract screen. Việc bắt phím để rời attract do
    /// MainMenuStartGameController lo, component này chỉ lo phần nhìn.
    /// </summary>
    public sealed class TitleAttractPrompt : MonoBehaviour
    {
        [SerializeField] private CanvasGroup promptGroup;
        [SerializeField] private float blinkHz = 0.5f;
        [SerializeField] private float minAlpha = 0.18f;
        [SerializeField] private float maxAlpha = 1f;
        [SerializeField] private float onDuty = 0.7f;

        public void Bind(CanvasGroup group)
        {
            promptGroup = group;
            blinkHz = 0.5f;
            onDuty = 0.7f;
        }

        private void Awake()
        {
            if (promptGroup == null)
            {
                promptGroup = GetComponent<CanvasGroup>();
            }

            if (promptGroup == null)
            {
                promptGroup = gameObject.AddComponent<CanvasGroup>();
                promptGroup.blocksRaycasts = false;
                promptGroup.interactable = false;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying || promptGroup == null)
            {
                return;
            }

            var u = Mathf.Repeat(Time.unscaledTime * blinkHz, 1f);
            if (u < onDuty)
            {
                promptGroup.alpha = maxAlpha;
                return;
            }

            var fade = Mathf.InverseLerp(1f, onDuty, u);
            promptGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, fade * fade);
        }
    }
}

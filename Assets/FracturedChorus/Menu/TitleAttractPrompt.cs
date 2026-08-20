using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FracturedChorus.Menu
{
    public sealed class TitleAttractPrompt : MonoBehaviour
    {
        [SerializeField] private MainMenuLayoutSandboxLayers layers;
        [SerializeField] private CanvasGroup promptGroup;
        [SerializeField] private float blinkHz = 0.5f;
        [SerializeField] private float minAlpha = 0.18f;
        [SerializeField] private float maxAlpha = 1f;
        [SerializeField] private float onDuty = 0.7f;

        public void Bind(MainMenuLayoutSandboxLayers boundLayers, CanvasGroup group)
        {
            layers = boundLayers;
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

            if (layers == null)
            {
                layers = GetComponentInParent<MainMenuLayoutSandboxLayers>();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (promptGroup != null)
            {
                var u = Mathf.Repeat(Time.unscaledTime * blinkHz, 1f);
                float alpha;
                if (u < onDuty)
                {
                    alpha = maxAlpha;
                }
                else
                {
                    var fade = Mathf.InverseLerp(1f, onDuty, u);
                    alpha = Mathf.Lerp(minAlpha, maxAlpha, fade * fade);
                }

                promptGroup.alpha = alpha;
            }

            if (layers == null)
            {
                layers = GetComponentInParent<MainMenuLayoutSandboxLayers>();
            }

            if (layers == null || layers.AttractLayer == null || !layers.AttractLayer.activeInHierarchy)
            {
                return;
            }

            if (layers.MainMenuLayer != null && layers.MainMenuLayer.activeInHierarchy)
            {
                return;
            }

            if (WasAnyInputPressed())
            {
                layers.ShowMainMenu();
            }
        }

        private static bool WasAnyInputPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                return true;
            }

            if (Mouse.current != null &&
                (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
            {
                return true;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }

            if (Gamepad.current != null &&
                (Gamepad.current.buttonSouth.wasPressedThisFrame ||
                 Gamepad.current.startButton.wasPressedThisFrame ||
                 Gamepad.current.selectButton.wasPressedThisFrame))
            {
                return true;
            }

            return false;
#else
            return Input.anyKeyDown;
#endif
        }
    }
}

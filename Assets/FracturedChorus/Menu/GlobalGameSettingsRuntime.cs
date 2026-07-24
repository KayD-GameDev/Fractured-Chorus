using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FracturedChorus.Menu
{
    public static class GlobalGameSettingsRuntime
    {
        private const string RootName = "GlobalGameSettingsRuntime";

        private static GameObject _root;
        private static Image _dimmer;
        private static bool _bootstrapped;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_bootstrapped)
            {
                return;
            }

            _bootstrapped = true;
            EnsureRoot();
            ApplyAll();
            MainMenuGameSettings.SettingsChanged += ApplyAll;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureRoot();
            ApplyAll();
        }

        private static void EnsureRoot()
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject(RootName);
            Object.DontDestroyOnLoad(_root);

            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;

            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var dimmerGo = new GameObject("BrightnessDimmer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dimmerGo.transform.SetParent(_root.transform, false);

            var rt = dimmerGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _dimmer = dimmerGo.GetComponent<Image>();
            _dimmer.color = new Color(0f, 0f, 0f, 0f);
            _dimmer.raycastTarget = false;
        }

        private static void ApplyAll()
        {
            AudioListener.volume = Mathf.Clamp01(MainMenuGameSettings.MasterVolume);
            ApplyBrightness(MainMenuGameSettings.BackgroundBrightness);
        }

        private static void ApplyBrightness(float brightness)
        {
            if (_dimmer == null)
            {
                return;
            }

            brightness = Mathf.Clamp01(brightness);
            var alpha = Mathf.Lerp(0.65f, 0f, brightness);
            _dimmer.color = new Color(0f, 0f, 0f, alpha);
            _dimmer.enabled = alpha > 0.001f;
        }
    }
}

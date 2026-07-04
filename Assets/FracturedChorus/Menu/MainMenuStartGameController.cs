using System.Collections;
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.RunMap;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FracturedChorus.Menu
{
    public class MainMenuStartGameController : MonoBehaviour
    {
        public enum MainMenuEditorPreview
        {
            Attract,
            MainMenu,
            Settings
        }

        [SerializeField] private CanvasGroup attractLayer;
        [SerializeField] private CanvasGroup mainMenuBackground;
        [SerializeField] private CanvasGroup mainMenuUi;
        [SerializeField] private CanvasGroup settingsOverlay;
        [SerializeField] private CanvasGroup offBeatArchiveOverlay;
        [SerializeField] private MainMenuStartGameMenuController menuController;
        [SerializeField] private MainMenuConfigOverlayController configOverlayController;
        [SerializeField] private AudioClip menuBgmClip;
        [SerializeField] private AudioClip menuFemaleVoiceClip;
        [SerializeField] private AudioClip menuMaleVoiceClip;
        [SerializeField] private AudioClip changeMenuSfxClip;
        [SerializeField] private AudioClip buttonPressSfxClip;
        [SerializeField] private CanvasGroup sceneFadeOverlay;
        [SerializeField] private float transitionDuration = 0.35f;
        [SerializeField] [Range(0.2f, 0.95f)] private float menuTransitionFadeScale = 0.72f;
        [SerializeField] private float newGameFadeDuration = 1.15f;
        [SerializeField] private float newGameFadeHoldSeconds = 0.35f;
        [SerializeField] [Range(0f, 0.5f)] private float bgmLeadInSeconds = 0.45f;
        [SerializeField] [Range(0.2f, 0.9f)] private float bgmLeadInProgress = 0.48f;
        [SerializeField] [Range(0f, 1f)] private float bgmDuckMultiplier = 0.28f;
        [SerializeField] [Range(0.5f, 2f)] private float changeMenuSfxVolume = 1.45f;
        [SerializeField] [Range(0.1f, 2f)] private float buttonPressSfxVolume = 1f;
        [SerializeField] private MainMenuEditorPreview editorPreview = MainMenuEditorPreview.Attract;

        private bool _transitioning;
        private bool _attractDismissed;
        private MainMenuBgmController _bgmController;
        private MainMenuTitleVoiceController _titleVoiceController;
        private MainMenuTransitionSfxController _transitionSfxController;
        private MainMenuButtonPressSfxController _buttonPressSfxController;

        private void Awake()
        {
            CombatInputSetup.EnsureEventSystem();
            EnsureMenuAudio();
            EnsureSceneFadeOverlay();
            ApplyLayerState(showAttract: true, immediate: true);
            if (settingsOverlay != null)
            {
                settingsOverlay.alpha = 0f;
                settingsOverlay.interactable = false;
                settingsOverlay.blocksRaycasts = false;
                settingsOverlay.gameObject.SetActive(false);
            }

            if (offBeatArchiveOverlay != null)
            {
                offBeatArchiveOverlay.alpha = 0f;
                offBeatArchiveOverlay.interactable = false;
                offBeatArchiveOverlay.blocksRaycasts = false;
                offBeatArchiveOverlay.gameObject.SetActive(false);
            }

            if (attractLayer != null)
            {
                attractLayer.blocksRaycasts = false;
                attractLayer.interactable = false;
            }

            if (mainMenuBackground != null)
            {
                mainMenuBackground.blocksRaycasts = false;
                mainMenuBackground.interactable = false;
            }

            WireOverlayBackButtons();
            menuController?.SetEnabled(false);
            ApplyBackgroundBrightness(MainMenuGameSettings.BackgroundBrightness);
            ApplyMasterVolume(MainMenuGameSettings.MasterVolume);
            MainMenuGameSettings.SettingsChanged += OnGameSettingsChanged;
        }

        private void OnDestroy()
        {
            MainMenuGameSettings.SettingsChanged -= OnGameSettingsChanged;
        }

        private void OnGameSettingsChanged()
        {
            ApplyBackgroundBrightness(MainMenuGameSettings.BackgroundBrightness);
            ApplyMasterVolume(MainMenuGameSettings.MasterVolume);
        }

        public void PlayAttractTransitionSfx()
        {
            _transitionSfxController?.PlayChangeMenu();
        }

        public void BeginNewGame()
        {
            if (_transitioning)
            {
                return;
            }

            StartCoroutine(BeginNewGameRoutine());
        }

        public void PlayButtonPressSfx()
        {
            _buttonPressSfxController?.PlayButtonPress();
        }

        public void StopButtonPressSfx()
        {
            _buttonPressSfxController?.StopButtonPress();
        }

        public void ApplyBackgroundBrightness(float brightness)
        {
            brightness = Mathf.Clamp01(brightness);
            ApplyLayerImageBrightness(attractLayer, brightness);
            ApplyLayerImageBrightness(mainMenuBackground, brightness);
        }

        private static void ApplyLayerImageBrightness(CanvasGroup layer, float brightness)
        {
            if (layer == null)
            {
                return;
            }

            var image = layer.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            var c = image.color;
            var scalar = Mathf.Lerp(0.35f, 1f, brightness);
            image.color = new Color(scalar, scalar, scalar, c.a);
        }

        private void ApplyMasterVolume(float masterVolume)
        {
            masterVolume = Mathf.Clamp01(masterVolume);
            _bgmController?.ApplyMasterVolume(masterVolume);
            _titleVoiceController?.ApplyMasterVolume(masterVolume);
            _transitionSfxController?.ApplyMasterVolume(masterVolume);
            _buttonPressSfxController?.ApplyMasterVolume(masterVolume);
        }

        private void Start()
        {
            StartCoroutine(BootMenuAudioSequence());
        }

        private IEnumerator BootMenuAudioSequence()
        {
            _bgmController?.StopLoop();

            var bgmStarted = false;
            void StartBgmOnce()
            {
                if (bgmStarted)
                {
                    return;
                }

                bgmStarted = true;
                _bgmController?.StartLoop();
            }

            if (_titleVoiceController != null)
            {
                yield return _titleVoiceController.PlayRandomIntroRoutine(
                    bgmLeadInSeconds,
                    bgmLeadInProgress,
                    StartBgmOnce);
            }

            StartBgmOnce();
        }

        private void WireOverlayBackButtons()
        {
            WireOverlayBackButton(settingsOverlay, HideSettings);
            WireOverlayBackButton(offBeatArchiveOverlay, HideOffBeatArchive);
        }

        private void WireOverlayBackButton(CanvasGroup overlay, UnityAction handler)
        {
            if (overlay == null || handler == null)
            {
                return;
            }

            foreach (var button in overlay.GetComponentsInChildren<Button>(true))
            {
                if (button.gameObject.name != "Btn_Back")
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(handler);
                return;
            }
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += ApplyEditorPreviewDeferred;
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EditorApplication.delayCall += ApplyEditorPreviewDeferred;
        }

        private void ApplyEditorPreviewDeferred()
        {
            if (this == null || Application.isPlaying)
            {
                return;
            }

            ApplyEditorPreview();
        }

        public void SetEditorPreview(MainMenuEditorPreview preview)
        {
            editorPreview = preview;
            ApplyEditorPreview();
        }

        public void ApplyEditorPreview()
        {
            if (attractLayer == null)
            {
                return;
            }

            switch (editorPreview)
            {
                case MainMenuEditorPreview.Attract:
                    attractLayer.gameObject.SetActive(true);
                    attractLayer.alpha = 1f;
                    SetMainMenuEditorVisible(false);
                    SetSettingsEditorVisible(false);
                    SetOffBeatArchiveEditorVisible(false);
                    break;
                case MainMenuEditorPreview.MainMenu:
                    attractLayer.gameObject.SetActive(false);
                    SetMainMenuEditorVisible(true);
                    SetSettingsEditorVisible(false);
                    SetOffBeatArchiveEditorVisible(false);
                    break;
                case MainMenuEditorPreview.Settings:
                    attractLayer.gameObject.SetActive(false);
                    SetMainMenuEditorVisible(false);
                    SetOffBeatArchiveEditorVisible(false);
                    SetSettingsEditorVisible(true);
                    break;
            }
        }

        private void SetOffBeatArchiveEditorVisible(bool visible)
        {
            if (offBeatArchiveOverlay == null)
            {
                return;
            }

            offBeatArchiveOverlay.gameObject.SetActive(visible);
            offBeatArchiveOverlay.alpha = visible ? 1f : 0f;
            offBeatArchiveOverlay.interactable = visible;
            offBeatArchiveOverlay.blocksRaycasts = visible;
        }

        private void SetMainMenuEditorVisible(bool visible)
        {
            if (mainMenuBackground != null)
            {
                mainMenuBackground.gameObject.SetActive(visible);
                mainMenuBackground.alpha = 1f;
            }

            if (mainMenuUi != null)
            {
                mainMenuUi.gameObject.SetActive(visible);
                mainMenuUi.alpha = 1f;
                mainMenuUi.interactable = true;
                mainMenuUi.blocksRaycasts = true;
            }
        }

        private void SetSettingsEditorVisible(bool visible)
        {
            if (settingsOverlay == null)
            {
                return;
            }

            settingsOverlay.gameObject.SetActive(visible);
            settingsOverlay.alpha = visible ? 1f : 0f;
            settingsOverlay.interactable = visible;
            settingsOverlay.blocksRaycasts = visible;

            if (visible)
            {
                configOverlayController?.SetActive(true);
            }
            else
            {
                configOverlayController?.SetActive(false);
            }
        }
#endif

        private void Update()
        {
            if (_transitioning)
            {
                return;
            }

            if (!_attractDismissed)
            {
                if (WasAnyInputPressed())
                {
                    StartCoroutine(TransitionToMainMenu());
                }

                return;
            }

            if (settingsOverlay != null && settingsOverlay.gameObject.activeSelf)
            {
                if (WasCancelPressed())
                {
                    HideSettings();
                }
                else
                {
                    configOverlayController?.HandleInput();
                }

                return;
            }

            if (offBeatArchiveOverlay != null && offBeatArchiveOverlay.gameObject.activeSelf)
            {
                if (WasCancelPressed())
                {
                    HideOffBeatArchive();
                }

                return;
            }

            if (_attractDismissed && WasCancelPressed())
            {
                StartCoroutine(TransitionToAttract());
                return;
            }

            menuController?.HandleInput();
        }

        public void ReturnToAttract()
        {
            if (!_attractDismissed || _transitioning)
            {
                return;
            }

            StartCoroutine(TransitionToAttract());
        }

        public void ShowSettings()
        {
            if (settingsOverlay == null)
            {
                return;
            }

            settingsOverlay.gameObject.SetActive(true);
            settingsOverlay.alpha = 1f;
            settingsOverlay.interactable = true;
            settingsOverlay.blocksRaycasts = true;
            menuController?.SetEnabled(false);
            configOverlayController?.SetActive(true);
        }

        public void HideSettings()
        {
            if (settingsOverlay == null)
            {
                return;
            }

            settingsOverlay.alpha = 0f;
            settingsOverlay.interactable = false;
            settingsOverlay.blocksRaycasts = false;
            settingsOverlay.gameObject.SetActive(false);
            configOverlayController?.SetActive(false);
            menuController?.SetEnabled(true);
        }

        public void ShowOffBeatArchive()
        {
            if (offBeatArchiveOverlay == null)
            {
                return;
            }

            offBeatArchiveOverlay.gameObject.SetActive(true);
            offBeatArchiveOverlay.alpha = 1f;
            offBeatArchiveOverlay.interactable = true;
            offBeatArchiveOverlay.blocksRaycasts = true;
            menuController?.SetEnabled(false);
        }

        public void HideOffBeatArchive()
        {
            if (offBeatArchiveOverlay == null)
            {
                return;
            }

            offBeatArchiveOverlay.alpha = 0f;
            offBeatArchiveOverlay.interactable = false;
            offBeatArchiveOverlay.blocksRaycasts = false;
            offBeatArchiveOverlay.gameObject.SetActive(false);
            menuController?.SetEnabled(true);
        }

        private void EnsureMenuAudio()
        {
#if UNITY_EDITOR
            if (menuBgmClip == null)
            {
                menuBgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/FracturedChorus/Audio/Music/Midnight_BGM_Menu.mp3");
            }

            if (menuFemaleVoiceClip == null)
            {
                menuFemaleVoiceClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/FracturedChorus/Audio/Voice/MainMenu_Female_Voice.mp3");
            }

            if (menuMaleVoiceClip == null)
            {
                menuMaleVoiceClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/FracturedChorus/Audio/Voice/MainMenu_Male_Voice.mp3");
            }

            if (changeMenuSfxClip == null)
            {
                changeMenuSfxClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/FracturedChorus/Audio/SFX/MainMenu_ChangeMenu_Ting.mp3");
            }

            if (buttonPressSfxClip == null)
            {
                buttonPressSfxClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/FracturedChorus/Audio/SFX/MainMenu_ButtonPress.mp3");
            }
#endif

            _bgmController = GetComponentInChildren<MainMenuBgmController>();
            if (_bgmController == null)
            {
                var bgmGo = new GameObject("MainMenuBgm");
                bgmGo.transform.SetParent(transform, false);
                _bgmController = bgmGo.AddComponent<MainMenuBgmController>();
            }

            if (menuBgmClip != null)
            {
                _bgmController.SetClip(menuBgmClip);
            }

            _titleVoiceController = GetComponentInChildren<MainMenuTitleVoiceController>();
            if (_titleVoiceController == null)
            {
                var voiceGo = new GameObject("MainMenuTitleVoice");
                voiceGo.transform.SetParent(transform, false);
                _titleVoiceController = voiceGo.AddComponent<MainMenuTitleVoiceController>();
            }

            _titleVoiceController.Configure(menuFemaleVoiceClip, menuMaleVoiceClip);

            _transitionSfxController = GetComponentInChildren<MainMenuTransitionSfxController>();
            if (_transitionSfxController == null)
            {
                var sfxGo = new GameObject("MainMenuTransitionSfx");
                sfxGo.transform.SetParent(transform, false);
                _transitionSfxController = sfxGo.AddComponent<MainMenuTransitionSfxController>();
            }

            _transitionSfxController.Configure(changeMenuSfxClip, changeMenuSfxVolume);

            _buttonPressSfxController = GetComponentInChildren<MainMenuButtonPressSfxController>();
            if (_buttonPressSfxController == null)
            {
                var pressGo = new GameObject("MainMenuButtonPressSfx");
                pressGo.transform.SetParent(transform, false);
                _buttonPressSfxController = pressGo.AddComponent<MainMenuButtonPressSfxController>();
            }

            _buttonPressSfxController.Configure(buttonPressSfxClip, buttonPressSfxVolume);
        }

        private IEnumerator BeginNewGameRoutine()
        {
            _transitioning = true;
            menuController?.SetEnabled(false);
            HideSettingsImmediate();
            HideOffBeatArchiveImmediate();

            PlayAttractTransitionSfx();
            _bgmController?.Duck(bgmDuckMultiplier);

            EnsureSceneFadeOverlay();
            if (sceneFadeOverlay != null)
            {
                sceneFadeOverlay.gameObject.SetActive(true);
                sceneFadeOverlay.blocksRaycasts = true;
                sceneFadeOverlay.interactable = false;
            }

            var sfxDuration = _transitionSfxController != null
                ? _transitionSfxController.GetChangeMenuDuration()
                : newGameFadeDuration;
            var fadeDuration = Mathf.Max(newGameFadeDuration, sfxDuration * menuTransitionFadeScale);

            yield return FadeSceneOverlayTo(1f, fadeDuration);

            if (_transitionSfxController != null)
            {
                yield return _transitionSfxController.WaitUntilFinishedRoutine();
            }

            if (newGameFadeHoldSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(newGameFadeHoldSeconds);
            }

            RunMapSceneLoader.LoadByName(RunMapSceneCatalog.PrologueVN);
        }

        private IEnumerator TransitionToAttract()
        {
            _transitioning = true;
            menuController?.SetEnabled(false);
            HideSettingsImmediate();
            HideOffBeatArchiveImmediate();

            var duration = Mathf.Max(0.01f, transitionDuration);
            var elapsed = 0f;

            attractLayer.gameObject.SetActive(true);
            attractLayer.alpha = 0f;
            SetMainMenuAlpha(1f, interactable: false, blocksRaycasts: false);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                attractLayer.alpha = t;
                SetMainMenuAlpha(1f - t, interactable: false, blocksRaycasts: false);
                yield return null;
            }

            _attractDismissed = false;
            ApplyLayerState(showAttract: true, immediate: true);
            _transitioning = false;
        }

        private void HideSettingsImmediate()
        {
            if (settingsOverlay == null)
            {
                return;
            }

            settingsOverlay.alpha = 0f;
            settingsOverlay.interactable = false;
            settingsOverlay.blocksRaycasts = false;
            settingsOverlay.gameObject.SetActive(false);
        }

        private void HideOffBeatArchiveImmediate()
        {
            if (offBeatArchiveOverlay == null)
            {
                return;
            }

            offBeatArchiveOverlay.alpha = 0f;
            offBeatArchiveOverlay.interactable = false;
            offBeatArchiveOverlay.blocksRaycasts = false;
            offBeatArchiveOverlay.gameObject.SetActive(false);
        }

        private IEnumerator TransitionToMainMenu()
        {
            _transitioning = true;
            _attractDismissed = true;

            PlayAttractTransitionSfx();
            menuController?.SetEnabled(false);
            SetMainMenuRuntimeActive(true);
            SetMainMenuAlpha(0f, interactable: false, blocksRaycasts: false);
            _bgmController?.Duck(bgmDuckMultiplier);

            var sfxDuration = _transitionSfxController != null
                ? _transitionSfxController.GetChangeMenuDuration()
                : transitionDuration;
            var duration = Mathf.Max(0.28f, sfxDuration * menuTransitionFadeScale);

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                attractLayer.alpha = 1f - t;
                SetMainMenuAlpha(t, interactable: false, blocksRaycasts: false);
                yield return null;
            }

            ApplyLayerState(showAttract: false, immediate: true);
            menuController?.SetEnabled(true);

            if (_transitionSfxController != null)
            {
                yield return _transitionSfxController.WaitUntilFinishedRoutine();
            }

            _bgmController?.RestoreVolume();
            _transitioning = false;
        }

        private void ApplyLayerState(bool showAttract, bool immediate)
        {
            attractLayer.gameObject.SetActive(showAttract || !immediate);

            if (showAttract)
            {
                attractLayer.alpha = 1f;
                attractLayer.interactable = false;
                attractLayer.blocksRaycasts = false;
                SetMainMenuRuntimeActive(!immediate);
                SetMainMenuAlpha(0f, interactable: false, blocksRaycasts: false);
            }
            else
            {
                attractLayer.alpha = 0f;
                attractLayer.interactable = false;
                attractLayer.blocksRaycasts = false;
                attractLayer.gameObject.SetActive(false);
                SetMainMenuRuntimeActive(true);
                SetMainMenuAlpha(1f, interactable: true, blocksRaycasts: true);
            }
        }

        private void SetMainMenuRuntimeActive(bool active)
        {
            if (mainMenuBackground != null)
            {
                mainMenuBackground.gameObject.SetActive(active);
            }

            if (mainMenuUi != null)
            {
                mainMenuUi.gameObject.SetActive(active);
            }
        }

        private void SetMainMenuAlpha(float alpha, bool interactable, bool blocksRaycasts)
        {
            if (mainMenuBackground != null)
            {
                mainMenuBackground.alpha = alpha;
                mainMenuBackground.interactable = false;
                mainMenuBackground.blocksRaycasts = false;
            }

            if (mainMenuUi != null)
            {
                mainMenuUi.alpha = alpha;
                mainMenuUi.interactable = interactable;
                mainMenuUi.blocksRaycasts = blocksRaycasts;
            }
        }

        private void EnsureSceneFadeOverlay()
        {
            if (sceneFadeOverlay != null)
            {
                sceneFadeOverlay.alpha = 0f;
                sceneFadeOverlay.blocksRaycasts = false;
                return;
            }

            var canvasTransform = ResolveMenuCanvasTransform();
            if (canvasTransform == null)
            {
                return;
            }

            var existing = canvasTransform.Find("SceneFadeOverlay");
            if (existing != null && existing.TryGetComponent<CanvasGroup>(out var existingGroup))
            {
                sceneFadeOverlay = existingGroup;
                sceneFadeOverlay.alpha = 0f;
                sceneFadeOverlay.blocksRaycasts = false;
                return;
            }

            var overlayGo = new GameObject(
                "SceneFadeOverlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            overlayGo.transform.SetParent(canvasTransform, false);
            overlayGo.transform.SetAsLastSibling();

            var rect = overlayGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = overlayGo.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;

            sceneFadeOverlay = overlayGo.GetComponent<CanvasGroup>();
            sceneFadeOverlay.alpha = 0f;
            sceneFadeOverlay.interactable = false;
            sceneFadeOverlay.blocksRaycasts = false;
        }

        private Transform ResolveMenuCanvasTransform()
        {
            if (attractLayer != null)
            {
                return attractLayer.transform.parent;
            }

            if (mainMenuUi != null)
            {
                return mainMenuUi.transform.parent;
            }

            if (mainMenuBackground != null)
            {
                return mainMenuBackground.transform.parent;
            }

            return null;
        }

        private IEnumerator FadeSceneOverlayTo(float alpha, float duration)
        {
            if (sceneFadeOverlay == null)
            {
                yield break;
            }

            var start = sceneFadeOverlay.alpha;
            var elapsed = 0f;
            duration = Mathf.Max(0.01f, duration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                sceneFadeOverlay.alpha = Mathf.Lerp(start, alpha, elapsed / duration);
                yield return null;
            }

            sceneFadeOverlay.alpha = alpha;
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

        private static bool WasCancelPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null &&
                (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.bKey.wasPressedThisFrame))
            {
                return true;
            }

            if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
            {
                return true;
            }

            return false;
#else
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.B);
#endif
        }
    }
}

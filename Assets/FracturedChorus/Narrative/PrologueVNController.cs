using System.Collections;
using FracturedChorus.Meta;
using FracturedChorus.Narrative.Vn;
using FracturedChorus.RunMap;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FracturedChorus.Narrative
{
    public class PrologueVNController : MonoBehaviour
    {
        public enum PrologueEditorPreview
        {
            Disclaimer,
            Story,
            Choice,
            Contract,
            ThankYou
        }

        private const string OpeningLine = "This story is a work of fiction.";

        private static readonly string[] StoryLines =
        {
            "Music is not decoration. It is the oldest language we never agreed to forget.",
            "Before words named the world, rhythm named what could not be named.",
            "Every note carries a memory of something that once trembled in the dark—and chose to become sound.",
            "They have learned to bend melody until it obeys.",
            "Charts, contracts, silence where a chorus should rise—music reduced to a lever in someone else's hand.",
            "They call it curation. They call it order. They call it progress.",
            "But music was never born to serve a master.",
            "Strip away every market, every gate, every polished lie—and the pulse remains.",
            "You cannot own what lives inside the listener. You can only try to drown it out.",
            "This is a journey back to the source—the place where every melody, however small, must be honored.",
            "The path ahead is cruel: fractured roads, broken cadences, trials that will test everything you believe about sound."
        };

        private const string ChoicePrompt =
            "Only those who have agreed to the above\nhave the privilege of partaking in this game.";

        private static readonly string[] DisagreeLines =
        {
            "Perhaps you are not ready yet—we will wait for you.",
            "The cadence does not hurry those who still need silence.",
            "We will keep your place at the threshold until you choose to listen again.",
            "Return when the melody calls to you. We will be here."
        };

        private const string ThankYouLine =
            "Thank you, {0}. The cadence remembers your name.\nMay your melody find its way home.";

        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private Image butterflyBackground;
        [SerializeField] private CanvasGroup dialoguePanel;
        [SerializeField] private PrologueTypewriterView disclaimerTypewriter;
        [SerializeField] private Text disclaimerText;
        [SerializeField] private PrologueTypewriterView dialogueTypewriter;
        [SerializeField] private PrologueChoiceView choiceView;
        [SerializeField] private PrologueContractView contractView;
        [SerializeField] private PrologueAudioController audioController;
        [SerializeField] private float fadeDuration = 0.85f;
        [SerializeField] private float openingFadeDuration = 1.05f;
        [SerializeField] private float disclaimerExitFadeDuration = 0.8f;
        [SerializeField] private float disclaimerExitHoldSeconds = 0.4f;
        [SerializeField] private float butterflyFadeInDuration = 1.45f;
        [SerializeField] private float contractRevealDuration = 1.35f;
        [SerializeField] private string nextSceneName = RunMapSceneCatalog.OpeningInvestigation;
        [SerializeField] private CanvasGroup choiceBackdrop;
        [SerializeField] private PrologueVNLayoutConfig layoutConfig;
        [SerializeField] private PrologueEditorPreview editorPreview = PrologueEditorPreview.Contract;
        [SerializeField] private VnConvenienceController convenience;

        private bool _waitingAdvance;
        private bool _running;
        private bool _acceptNarrationInput;
        private string _pendingTypeText;
        private string _currentLineReadKey;
        private PrologueTypewriterView _activeTypewriter;
        [SerializeField] private int narrationFontSize = 40;
        private Color _butterflyBaseColor = Color.white;

        private void OnEnable()
        {
            BindConvenience();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += ApplyEditorPreviewDeferred;
            }
#endif
        }

        private void Start()
        {
            RunProfile.ResetForNewRun();
            GameMetaSession.ResetSession();
            disclaimerTypewriter?.Bind(audioController);
            dialogueTypewriter?.Bind(audioController);
            choiceView?.Bind(audioController);
            contractView?.Bind(audioController);
            if (layoutConfig != null)
            {
                contractView?.SetLayoutConfig(layoutConfig);
            }
            ApplyCenteredNarrationLayout();
            BindConvenience();
            convenience?.ResetSession();

            if (butterflyBackground != null)
            {
                _butterflyBaseColor = butterflyBackground.color;
                butterflyBackground.gameObject.SetActive(false);
                SetButterflyAlpha(0f);
            }

            if (dialoguePanel != null)
            {
                dialoguePanel.alpha = 0f;
                dialoguePanel.gameObject.SetActive(false);
            }

            choiceView?.Hide();
            contractView?.Hide();
            SetFade(1f);
            StartCoroutine(RunPrologueRoutine());
        }

        private void Update()
        {
            if (!_running || !_acceptNarrationInput)
            {
                return;
            }

            if (convenience != null && convenience.LogOpen)
            {
                if (PrologueInput.WasCancelPressedThisFrame() ||
                    PrologueInput.WasKeyboardAdvancePressedThisFrame())
                {
                    convenience.CloseLog();
                }

                return;
            }

            if (!PrologueInput.WasAdvancePressedThisFrame())
            {
                return;
            }

            if (_activeTypewriter != null && _activeTypewriter.IsTyping)
            {
                _activeTypewriter.SkipToEnd(_pendingTypeText);
                return;
            }

            if (_waitingAdvance)
            {
                _waitingAdvance = false;
            }
        }

        private IEnumerator RunPrologueRoutine()
        {
            _running = true;
            yield return FadeTo(0f, openingFadeDuration);

            yield return RunDisclaimerPhase();
            yield return RunButterflyStoryPhase();
            yield return RunChoicePhase();
        }

        private IEnumerator RunDisclaimerPhase()
        {
            _acceptNarrationInput = true;

            if (disclaimerText != null)
            {
                disclaimerText.gameObject.SetActive(true);
            }

            var typed = false;
            BeginTypeLine(disclaimerTypewriter, OpeningLine, "prologue_disclaimer", () => typed = true);
            while (!typed)
            {
                yield return null;
            }

            yield return WaitForAdvance();

            if (disclaimerText != null)
            {
                disclaimerText.gameObject.SetActive(false);
            }

            yield return FadeTo(1f, disclaimerExitFadeDuration);

            if (disclaimerExitHoldSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(disclaimerExitHoldSeconds);
            }
        }

        private IEnumerator RunButterflyStoryPhase()
        {
            if (butterflyBackground != null)
            {
                butterflyBackground.gameObject.SetActive(true);
                SetButterflyAlpha(0f);
            }

            audioController?.StartBgm();
            audioController?.StartButterflyWings();

            if (dialoguePanel != null)
            {
                dialoguePanel.gameObject.SetActive(true);
                dialoguePanel.alpha = 0f;
            }

            yield return FadeStoryReveal(openingFadeDuration, butterflyFadeInDuration);

            if (dialoguePanel != null)
            {
                dialoguePanel.alpha = 1f;
            }

            for (var i = 0; i < StoryLines.Length; i++)
            {
                var displayLine = PrologueNarrationText.WrapBalanced(StoryLines[i]);
                var typed = false;
                BeginTypeLine(dialogueTypewriter, displayLine, $"prologue_story_{i}", () => typed = true);
                while (!typed)
                {
                    yield return null;
                }

                yield return WaitForAdvance();
            }
        }

        private IEnumerator RunChoicePhase()
        {
            HideChoiceUi();

            var displayLine = PrologueNarrationText.WrapBalanced(ChoicePrompt);
            var typed = false;
            BeginTypeLine(dialogueTypewriter, displayLine, "prologue_choice", () => typed = true);
            while (!typed)
            {
                yield return null;
            }

            yield return WaitForAdvance();
            yield return null;

            _acceptNarrationInput = false;
            audioController?.StopButterflyWings();

            if (butterflyBackground != null)
            {
                butterflyBackground.gameObject.SetActive(false);
            }

            if (choiceBackdrop != null)
            {
                choiceBackdrop.gameObject.SetActive(true);
                choiceBackdrop.alpha = 1f;
                choiceBackdrop.interactable = true;
                choiceBackdrop.blocksRaycasts = true;
            }

            var decided = false;
            var agreed = false;
            choiceView?.ShowOptions(
                "I agree.",
                "I do not agree.",
                result =>
                {
                    agreed = result;
                    decided = true;
                });

            while (!decided)
            {
                yield return null;
            }

            HideChoiceUi();

            if (!agreed)
            {
                yield return RunDisagreePhase();
                yield return ReturnToMainMenuRoutine();
                yield break;
            }

            HideDialogueUi();

            var tingDuration = audioController != null ? audioController.PlayMenuTing() : 0f;
            if (tingDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(tingDuration);
            }

            yield return RunContractPhase();
        }

        private IEnumerator RunDisagreePhase()
        {
            _acceptNarrationInput = true;

            if (dialoguePanel != null)
            {
                dialoguePanel.gameObject.SetActive(true);
                dialoguePanel.alpha = 1f;
            }

            for (var i = 0; i < DisagreeLines.Length; i++)
            {
                var displayLine = PrologueNarrationText.WrapBalanced(DisagreeLines[i]);
                var typed = false;
                BeginTypeLine(dialogueTypewriter, displayLine, $"prologue_disagree_{i}", () => typed = true);
                while (!typed)
                {
                    yield return null;
                }

                yield return WaitForAdvance();
            }
        }

        private void HideChoiceUi()
        {
            choiceView?.Hide();

            if (choiceBackdrop == null)
            {
                return;
            }

            choiceBackdrop.alpha = 0f;
            choiceBackdrop.interactable = false;
            choiceBackdrop.blocksRaycasts = false;
            choiceBackdrop.gameObject.SetActive(false);
        }

        private void HideDialogueUi()
        {
            dialogueTypewriter?.Clear();

            if (dialoguePanel == null)
            {
                return;
            }

            dialoguePanel.alpha = 0f;
            dialoguePanel.gameObject.SetActive(false);
        }

        private IEnumerator RunContractPhase()
        {
            var signed = false;
            string playerName = RunProfile.DefaultNameSuggestion;
            contractView?.PrepareShow(name =>
            {
                playerName = name;
                signed = true;
            });

            if (contractView != null)
            {
                yield return contractView.FadeIn(contractRevealDuration);
            }

            while (!signed)
            {
                yield return null;
            }

            RunProfile.SetPlayerName(playerName);
            RunProfile.MarkContractSigned();

            _acceptNarrationInput = true;

            if (dialoguePanel != null)
            {
                dialoguePanel.gameObject.SetActive(true);
                dialoguePanel.alpha = 1f;
            }

            if (butterflyBackground != null)
            {
                butterflyBackground.gameObject.SetActive(true);
            }

            var thankYou = PrologueNarrationText.WrapBalanced(string.Format(ThankYouLine, RunProfile.PlayerName));
            var typed = false;
            BeginTypeLine(dialogueTypewriter, thankYou, "prologue_thank_you", () => typed = true);
            while (!typed)
            {
                yield return null;
            }

            yield return WaitForAdvance();
            yield return LoadNextSceneRoutine();
        }

        private IEnumerator ReturnToMainMenuRoutine()
        {
            _acceptNarrationInput = false;
            audioController?.FadeOutAll(fadeDuration);
            yield return FadeTo(1f, fadeDuration);
            RunMapSceneLoader.LoadByName(RunMapSceneCatalog.MainMenuStartGame);
        }

        private IEnumerator LoadNextSceneRoutine()
        {
            _acceptNarrationInput = false;
            audioController?.FadeOutAll(fadeDuration);
            yield return FadeTo(1f, fadeDuration);
            RunMapSceneLoader.LoadByName(nextSceneName);
        }

        private void BeginTypeLine(PrologueTypewriterView typewriter, string text, string readKey, System.Action onComplete)
        {
            _activeTypewriter = typewriter;
            _pendingTypeText = text;
            _currentLineReadKey = readKey;
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (convenience == null)
                {
                    convenience = FindAnyObjectByType<VnConvenienceController>();
                }

                if (convenience != null)
                {
                    convenience.AppendLog(string.Empty, text);
                }
                else
                {
                    VnDialogueLog.Session.Append(string.Empty, text);
                }
            }
            typewriter?.Type(text, () =>
            {
                if (!string.IsNullOrEmpty(_currentLineReadKey))
                {
                    VnReadTracker.MarkRead(_currentLineReadKey);
                }

                _activeTypewriter = null;
                onComplete?.Invoke();
            });
        }

        private void BindConvenience()
        {
            if (convenience == null)
            {
                convenience = FindAnyObjectByType<VnConvenienceController>();
            }

            if (convenience == null)
            {
                return;
            }

            convenience.Bind(new VnConvenienceBindings
            {
                IsRunning = () => _running,
                IsPlaybackActive = () => _running && _acceptNarrationInput,
                IsTyping = () => _activeTypewriter != null && _activeTypewriter.IsTyping,
                IsWaitingAdvance = () => _waitingAdvance,
                IsTransitionBusy = () => false,
                IsAtSkipStop = () => !_acceptNarrationInput,
                IsCurrentLineRead = () => string.IsNullOrEmpty(_currentLineReadKey) || VnReadTracker.IsRead(_currentLineReadKey),
                RequestAdvance = () =>
                {
                    if (_waitingAdvance)
                    {
                        _waitingAdvance = false;
                    }
                },
                SkipTyping = () => _activeTypewriter?.SkipToEnd(_pendingTypeText),
                RequestSkipTransition = () => { }
            });
        }

        private IEnumerator WaitForAdvance()
        {
            _waitingAdvance = true;
            while (_waitingAdvance)
            {
                yield return null;
            }
        }

        private IEnumerator FadeTo(float alpha, float duration)
        {
            if (fadeOverlay == null)
            {
                yield break;
            }

            var start = fadeOverlay.alpha;
            var elapsed = 0f;
            duration = Mathf.Max(0.01f, duration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeOverlay.alpha = Mathf.Lerp(start, alpha, elapsed / duration);
                yield return null;
            }

            fadeOverlay.alpha = alpha;
        }

        private IEnumerator FadeStoryReveal(float overlayDuration, float butterflyDuration)
        {
            overlayDuration = Mathf.Max(0.01f, overlayDuration);
            butterflyDuration = Mathf.Max(0.01f, butterflyDuration);
            var startOverlay = fadeOverlay != null ? fadeOverlay.alpha : 1f;
            SetButterflyAlpha(0f);

            var elapsed = 0f;
            var total = Mathf.Max(overlayDuration, butterflyDuration);
            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                if (fadeOverlay != null)
                {
                    var overlayT = Mathf.Clamp01(elapsed / overlayDuration);
                    fadeOverlay.alpha = Mathf.Lerp(startOverlay, 0f, overlayT);
                }

                SetButterflyAlpha(Mathf.Clamp01(elapsed / butterflyDuration));
                yield return null;
            }

            SetFade(0f);
            SetButterflyAlpha(1f);
        }

        private void SetButterflyAlpha(float alpha)
        {
            if (butterflyBackground == null)
            {
                return;
            }

            var color = _butterflyBaseColor;
            color.a = _butterflyBaseColor.a * Mathf.Clamp01(alpha);
            butterflyBackground.color = color;
        }

        private void SetFade(float alpha)
        {
            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = alpha;
            }
        }

        private void ApplyCenteredNarrationLayout()
        {
            ApplyCenterRect(disclaimerText != null ? disclaimerText.rectTransform : null, 0.06f, 0.32f, 0.94f, 0.68f);
            if (disclaimerText != null)
            {
                disclaimerText.alignment = TextAnchor.MiddleCenter;
                VnUiFont.Apply(disclaimerText, narrationFontSize, FontStyle.Italic);
                disclaimerText.lineSpacing = 1.12f;
                disclaimerText.horizontalOverflow = HorizontalWrapMode.Wrap;
                disclaimerText.transform.localRotation = Quaternion.identity;
                disclaimerText.raycastTarget = false;
            }

            if (dialoguePanel != null)
            {
                ApplyCenterRect(dialoguePanel.GetComponent<RectTransform>(), 0.06f, 0.28f, 0.94f, 0.72f);
            }

            var dialogueBody = dialoguePanel != null
                ? dialoguePanel.transform.Find("DialogueBody")?.GetComponent<Text>()
                : null;
            if (dialogueBody != null)
            {
                dialogueBody.alignment = TextAnchor.MiddleCenter;
                VnUiFont.Apply(dialogueBody, narrationFontSize, FontStyle.Normal);
                dialogueBody.lineSpacing = 1.12f;
                dialogueBody.horizontalOverflow = HorizontalWrapMode.Wrap;
                dialogueBody.raycastTarget = false;
            }

            var dialogueFrame = dialoguePanel != null ? dialoguePanel.transform.Find("DialogueFrame") : null;
            if (dialogueFrame != null)
            {
                dialogueFrame.gameObject.SetActive(false);
            }
        }

        private static void ApplyCenterRect(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

#if UNITY_EDITOR
        private Transform _canvasTransform;
        private bool _applyingEditorPreview;

        private void ApplyEditorPreviewDeferred()
        {
            if (this == null || Application.isPlaying)
            {
                return;
            }

            ApplyEditorPreview();
        }

        public void SetEditorPreview(PrologueEditorPreview preview)
        {
            editorPreview = preview;
            ApplyEditorPreview();
        }

        public void ApplyEditorPreview()
        {
            if (_applyingEditorPreview)
            {
                return;
            }

            _applyingEditorPreview = true;
            try
            {
                ApplyCenteredNarrationLayout();
                if (layoutConfig != null)
                {
                    contractView?.SetLayoutConfig(layoutConfig);
                }

                HideAllEditorLayers();

                switch (editorPreview)
                {
                    case PrologueEditorPreview.Disclaimer:
                        PreviewDisclaimer();
                        break;
                    case PrologueEditorPreview.Story:
                        PreviewStory();
                        break;
                    case PrologueEditorPreview.Choice:
                        PreviewChoice();
                        break;
                    case PrologueEditorPreview.Contract:
                        PreviewContract();
                        break;
                    case PrologueEditorPreview.ThankYou:
                        PreviewThankYou();
                        break;
                }

                SetFade(0f);
            }
            finally
            {
                _applyingEditorPreview = false;
            }
        }

        private Transform CanvasTransform =>
            _canvasTransform != null ? _canvasTransform : (_canvasTransform = transform.Find("PrologueCanvas"));

        private GameObject BlackBackground =>
            CanvasTransform != null ? CanvasTransform.Find("BlackBackground")?.gameObject : null;

        private void HideAllEditorLayers()
        {
            SetGameObjectActive(BlackBackground, false);

            if (butterflyBackground != null)
            {
                butterflyBackground.gameObject.SetActive(false);
            }

            SetCanvasGroupActive(dialoguePanel, false);
            SetGameObjectActive(disclaimerText != null ? disclaimerText.gameObject : null, false);
            SetCanvasGroupActive(choiceBackdrop, false);
            contractView?.Hide();

            if (confirmButtonTransform() != null)
            {
                confirmButtonTransform().gameObject.SetActive(false);
            }

            if (hintTextTransform() != null)
            {
                hintTextTransform().gameObject.SetActive(false);
            }
        }

        private void PreviewDisclaimer()
        {
            SetGameObjectActive(BlackBackground, true);
            SetGameObjectActive(disclaimerText != null ? disclaimerText.gameObject : null, true);
            if (disclaimerText != null)
            {
                disclaimerText.text = OpeningLine;
            }
        }

        private void PreviewStory()
        {
            if (butterflyBackground != null)
            {
                butterflyBackground.gameObject.SetActive(true);
            }

            SetCanvasGroupActive(dialoguePanel, true);
            SetDialogueSample(PrologueNarrationText.WrapBalanced(StoryLines[0]));
        }

        private void PreviewChoice()
        {
            SetGameObjectActive(BlackBackground, true);
            if (butterflyBackground != null)
            {
                butterflyBackground.gameObject.SetActive(false);
            }

            SetCanvasGroupActive(dialoguePanel, true);
            SetDialogueSample(PrologueNarrationText.WrapBalanced(ChoicePrompt));
            SetCanvasGroupActive(choiceBackdrop, true);
            choiceView?.ApplyEditorPreview(showPrompt: false);
        }

        private void PreviewContract()
        {
            SetGameObjectActive(BlackBackground, true);
            if (layoutConfig != null)
            {
                contractView?.SetLayoutConfig(layoutConfig);
            }

            contractView?.ApplyEditorPreview();

            if (hintTextTransform() != null)
            {
                hintTextTransform().gameObject.SetActive(true);
            }

            if (confirmButtonTransform() != null)
            {
                confirmButtonTransform().gameObject.SetActive(true);
            }
        }

        private void PreviewThankYou()
        {
            if (butterflyBackground != null)
            {
                butterflyBackground.gameObject.SetActive(true);
            }

            SetCanvasGroupActive(dialoguePanel, true);
            SetDialogueSample(PrologueNarrationText.WrapBalanced(
                string.Format(ThankYouLine, RunProfile.DefaultNameSuggestion)));
        }

        private void SetDialogueSample(string text)
        {
            var dialogueBody = dialoguePanel != null
                ? dialoguePanel.transform.Find("DialogueBody")?.GetComponent<Text>()
                : null;
            if (dialogueBody != null)
            {
                dialogueBody.text = text;
            }
        }

        private Transform hintTextTransform()
        {
            return contractView != null ? contractView.transform.Find("HintText") : null;
        }

        private Transform confirmButtonTransform()
        {
            return contractView != null ? contractView.transform.Find("ConfirmButton") : null;
        }

        private static void SetGameObjectActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private static void SetCanvasGroupActive(CanvasGroup group, bool active)
        {
            if (group == null)
            {
                return;
            }

            group.gameObject.SetActive(active);
            group.alpha = active ? 1f : 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
#endif
    }
}

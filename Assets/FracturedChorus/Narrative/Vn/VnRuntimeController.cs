using System.Collections;
using FracturedChorus.Meta;
using FracturedChorus.RunMap;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Narrative.Vn
{
    public sealed class VnRuntimeController : MonoBehaviour
    {
        [SerializeField] private VnScriptSO script;
        [SerializeField] private VnSpeakerCatalogSO speakerCatalog;
        [SerializeField] private VnCueResolver cueResolver;
        [SerializeField] private VnAudioPlayer audioPlayer;
        [SerializeField] private VnDialoguePortraitView portraitView;
        [SerializeField] private PrologueTypewriterView typewriter;
        [SerializeField] private Text nameplateText;
        [SerializeField] private Text textCardBody;
        [SerializeField] private CanvasGroup dialoguePanel;
        [SerializeField] private CanvasGroup textCardPanel;
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private VnStoryDateHud dateHud;
        [SerializeField] private AudioClip typingClip;
        [SerializeField] private float defaultFadeSeconds = 0.6f;
        [SerializeField] private float dramaticBgFadeSeconds = 0.28f;
        [SerializeField] private bool beginHubOnEnd;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private string openingDateDisplay = "17/08";
        [SerializeField] private string openingPhaseDisplay = "Late Night";
        [SerializeField] private VnConvenienceController convenience;

        private int _index;
        private bool _waitingAdvance;
        private bool _running;
        private string _pendingText;
        private string _currentBgId;
        private Coroutine _beatRoutine;
        private bool _transitionBusy;
        private bool _skipTransitionRequested;
        private bool _textCardBusy;
        private bool _skipTextCardRequested;
        private int _lastLoggedIndex = -1;
        private string _dateHudDate;
        private string _dateHudPhase;
        private AudioSource _textCardTypingSource;

        public Text NameplateText => nameplateText;
        public Text TextCardBody => textCardBody;
        public CanvasGroup DialoguePanel => dialoguePanel;
        public CanvasGroup TextCardPanel => textCardPanel;
        public VnStoryDateHud DateHud => dateHud;
        public Image BackgroundImage => backgroundImage;
        public VnDialoguePortraitView PortraitView => portraitView;

        public void SetScript(VnScriptSO next)
        {
            script = next;
        }

        private void Awake()
        {
            ApplyDialogueFonts();
            if (typingClip != null)
            {
                typewriter?.BindTypingClip(typingClip);
            }

            BindConvenience();
        }

        private void OnEnable()
        {
            BindConvenience();
        }

        private void Start()
        {
            audioPlayer?.Bind(cueResolver);
            ApplyDialogueFonts();
            if (typingClip != null)
            {
                typewriter?.BindTypingClip(typingClip);
            }

            if (playOnStart)
            {
                Begin();
            }
        }

        private void ApplyDialogueFonts()
        {
            VnUiFont.ApplyAssetOnly(nameplateText);
            var body = typewriter != null ? typewriter.BodyText : null;
            if (body == null && typewriter != null)
            {
                body = typewriter.GetComponent<Text>() ?? typewriter.GetComponentInChildren<Text>(true);
            }

            VnUiFont.ApplyAssetOnly(body);
            VnUiFont.ApplyAssetOnly(textCardBody);
            if (dateHud != null)
            {
                foreach (var label in dateHud.GetComponentsInChildren<Text>(true))
                {
                    VnUiFont.ApplyAssetOnly(label);
                }
            }
        }

        private void Update()
        {
            if (!_running)
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

            if (!VnInput.WasAdvancePressedThisFrame())
            {
                return;
            }

            if (_transitionBusy)
            {
                _skipTransitionRequested = true;
                return;
            }

            if (_textCardBusy)
            {
                _skipTextCardRequested = true;
                return;
            }

            if (typewriter != null && typewriter.IsTyping)
            {
                typewriter.SkipToEnd(_pendingText);
                return;
            }

            if (!_waitingAdvance)
            {
                return;
            }

            _waitingAdvance = false;
            Advance();
        }

        public void Begin()
        {
            if (script == null || script.beats == null || script.beats.Length == 0)
            {
                Debug.LogError("[VnRuntime] Script missing or empty.");
                return;
            }

            StopBeatRoutine();
            StopAllCoroutines();
            convenience?.ResetSession();
            _lastLoggedIndex = -1;
            _running = true;
            _index = 0;
            _waitingAdvance = false;
            _transitionBusy = false;
            _textCardBusy = false;
            _skipTransitionRequested = false;
            _skipTextCardRequested = false;
            SetPanel(textCardPanel, false);
            SetPanel(dialoguePanel, false);
            _dateHudDate = openingDateDisplay;
            _dateHudPhase = openingPhaseDisplay;
            dateHud?.Hide();
            audioPlayer?.StopAmbience();
            ApplyBlackBackground();
            _currentBgId = VnBgIds.Black;
            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = 0f;
                fadeOverlay.blocksRaycasts = false;
            }

            PlayBeat(script.beats[_index]);
        }

        private void Advance()
        {
            _index++;
            if (script == null || script.beats == null || _index >= script.beats.Length)
            {
                Finish(null);
                return;
            }

            PlayBeat(script.beats[_index]);
        }

        private void PlayBeat(VnBeat beat)
        {
            if (beat == null)
            {
                Advance();
                return;
            }

            StopBeatRoutine();

            if (NeedsDramaticBackgroundCrossfade(beat))
            {
                _beatRoutine = StartCoroutine(PlayBeatWithBgCrossfade(beat));
                return;
            }

            ApplyCues(beat);
            DispatchBeatView(beat);
        }

        private void StopBeatRoutine()
        {
            if (_beatRoutine != null)
            {
                StopCoroutine(_beatRoutine);
                _beatRoutine = null;
            }

            StopTextCardTypingSound();
            _transitionBusy = false;
            _textCardBusy = false;
            _skipTransitionRequested = false;
            _skipTextCardRequested = false;
        }

        private bool NeedsDramaticBackgroundCrossfade(VnBeat beat)
        {
            if (fadeOverlay == null || string.IsNullOrWhiteSpace(beat.bgId))
            {
                return false;
            }

            if (beat.kind == VnBeatKind.Fade || beat.kind == VnBeatKind.End)
            {
                return false;
            }

            if (string.Equals(_currentBgId, beat.bgId, System.StringComparison.Ordinal))
            {
                return false;
            }

            return IsDramaticBackground(_currentBgId) || IsDramaticBackground(beat.bgId);
        }

        private static bool IsDramaticBackground(string bgId)
        {
            return bgId == VnBgIds.Black
                || bgId == VnBgIds.LuxeConcert
                || bgId == VnBgIds.LuminaSquareNight;
        }

        private IEnumerator PlayBeatWithBgCrossfade(VnBeat beat)
        {
            _transitionBusy = true;
            _skipTransitionRequested = false;
            _waitingAdvance = false;
            SetPanel(textCardPanel, false);
            SetPanel(dialoguePanel, false);

            if (fadeOverlay != null)
            {
                fadeOverlay.blocksRaycasts = true;
                var half = Mathf.Max(0.08f, dramaticBgFadeSeconds * 0.5f);
                yield return FadeCanvasSkippable(fadeOverlay, fadeOverlay.alpha, 1f, half);
                ApplyCues(beat);
                yield return FadeCanvasSkippable(fadeOverlay, 1f, 0f, half);
                fadeOverlay.alpha = 0f;
                fadeOverlay.blocksRaycasts = false;
            }
            else
            {
                ApplyCues(beat);
            }

            _transitionBusy = false;
            _beatRoutine = null;
            DispatchBeatView(beat);
        }

        private void DispatchBeatView(VnBeat beat)
        {
            switch (beat.kind)
            {
                case VnBeatKind.Cue:
                    Advance();
                    break;
                case VnBeatKind.Fade:
                    _beatRoutine = StartCoroutine(FadeRoutine(beat.duration > 0f ? beat.duration : defaultFadeSeconds));
                    break;
                case VnBeatKind.TextCard:
                    _beatRoutine = StartCoroutine(TextCardRoutine(beat));
                    break;
                case VnBeatKind.End:
                    Finish(beat);
                    break;
                case VnBeatKind.Choice:
                    Debug.LogWarning("[VnRuntime] Choice reserved — treating as Line.");
                    ShowLine(beat);
                    break;
                default:
                    ShowLine(beat);
                    break;
            }
        }

        private void ShowLine(VnBeat beat)
        {
            SetPanel(textCardPanel, false);
            SetPanel(dialoguePanel, true);

            var isNarration = beat.kind == VnBeatKind.Narration || string.IsNullOrWhiteSpace(beat.speakerId);
            if (isNarration)
            {
                if (nameplateText != null)
                {
                    nameplateText.text = string.Empty;
                }

                portraitView?.DimAll();
            }
            else if (speakerCatalog != null && speakerCatalog.TryGet(beat.speakerId, out var speaker))
            {
                if (nameplateText != null)
                {
                    nameplateText.text = speaker.displayName;
                }

                portraitView?.Show(speaker, beat.expression);
            }
            else
            {
                if (nameplateText != null)
                {
                    nameplateText.text = beat.speakerId ?? string.Empty;
                }

                portraitView?.DimAll();
            }

            _pendingText = beat.text ?? string.Empty;
            AppendDialogueLog(beat);
            if (typewriter == null)
            {
                _waitingAdvance = true;
                return;
            }

            _waitingAdvance = false;
            typewriter.Type(_pendingText, () =>
            {
                MarkCurrentBeatRead();
                _waitingAdvance = true;
            });
        }

        private void AppendDialogueLog(VnBeat beat)
        {
            if (convenience == null || beat == null)
            {
                return;
            }

            if (_lastLoggedIndex == _index)
            {
                return;
            }

            _lastLoggedIndex = _index;

            var speaker = string.Empty;
            if (beat.kind != VnBeatKind.Narration && !string.IsNullOrWhiteSpace(beat.speakerId))
            {
                if (speakerCatalog != null && speakerCatalog.TryGet(beat.speakerId, out var speakerDef))
                {
                    speaker = speakerDef.displayName;
                }
                else
                {
                    speaker = beat.speakerId;
                }
            }

            convenience.AppendLog(speaker, beat.text);
        }

        private void BindConvenience()
        {
            if (convenience == null)
            {
                return;
            }

            convenience.Bind(new VnConvenienceBindings
            {
                IsRunning = () => _running,
                IsPlaybackActive = () => _running,
                IsTyping = () => (typewriter != null && typewriter.IsTyping) || _textCardBusy,
                IsWaitingAdvance = () => _waitingAdvance,
                IsTransitionBusy = () => _transitionBusy,
                IsAtSkipStop = IsAtSkipStop,
                IsCurrentLineRead = () => VnReadTracker.IsRead(GetReadScope(), _index),
                RequestAdvance = ConvenienceAdvance,
                SkipTyping = ConvenienceSkipTyping,
                RequestSkipTransition = () =>
                {
                    _skipTransitionRequested = true;
                    _skipTextCardRequested = true;
                }
            });
        }

        private bool IsAtSkipStop()
        {
            if (!_running || script == null || script.beats == null || _index < 0)
            {
                return true;
            }

            if (_index >= script.beats.Length)
            {
                return true;
            }

            var beat = script.beats[_index];
            if (beat == null)
            {
                return false;
            }

            return beat.kind == VnBeatKind.Choice || beat.kind == VnBeatKind.End;
        }

        private void ConvenienceAdvance()
        {
            if (!_waitingAdvance || _transitionBusy)
            {
                return;
            }

            _waitingAdvance = false;
            Advance();
        }

        private void ConvenienceSkipTyping()
        {
            if (_textCardBusy)
            {
                _skipTextCardRequested = true;
                return;
            }

            if (typewriter != null && typewriter.IsTyping)
            {
                typewriter.SkipToEnd(_pendingText);
            }
        }

        private string GetReadScope()
        {
            return script != null ? script.name : "vn";
        }

        private void MarkCurrentBeatRead()
        {
            VnReadTracker.MarkRead(GetReadScope(), _index);
        }

        private IEnumerator TextCardRoutine(VnBeat beat)
        {
            _textCardBusy = true;
            _skipTextCardRequested = false;
            _waitingAdvance = false;
            SetPanel(dialoguePanel, false);
            portraitView?.ClearStage();
            SetPanel(textCardPanel, true);
            AppendDialogueLog(beat);

            yield return null;

            var fullText = beat.text ?? string.Empty;
            var hold = beat.duration > 0f ? beat.duration : 1.2f;

            if (textCardBody != null && !string.IsNullOrEmpty(fullText))
            {
                textCardBody.text = string.Empty;
                yield return StartCoroutine(TypeTextCard(fullText));
            }
            else if (textCardBody != null)
            {
                textCardBody.text = fullText;
            }

            var elapsed = 0f;
            while (elapsed < hold && !_skipTextCardRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            StopTextCardTypingSound();
            MarkCurrentBeatRead();
            _textCardBusy = false;
            _beatRoutine = null;
            _waitingAdvance = true;
        }

        private IEnumerator TypeTextCard(string text)
        {
            if (textCardBody == null)
            {
                yield break;
            }

            BeginTextCardTypingSound();
            const float charsPerSecond = 36f;
            var builder = new System.Text.StringBuilder();
            for (var i = 0; i < text.Length; i++)
            {
                if (_skipTextCardRequested && i > 1)
                {
                    textCardBody.text = text;
                    StopTextCardTypingSound();
                    yield break;
                }

                builder.Append(text[i]);
                textCardBody.text = builder.ToString();
                var c = text[i];
                var delay = 1f / charsPerSecond;
                if (c == '.' || c == '!' || c == '?' || c == ',' || c == '—' || c == '\n')
                {
                    delay *= 1.8f;
                }

                var waited = 0f;
                while (waited < delay)
                {
                    if (_skipTextCardRequested)
                    {
                        textCardBody.text = text;
                        StopTextCardTypingSound();
                        yield break;
                    }

                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            StopTextCardTypingSound();
        }

        private void BeginTextCardTypingSound()
        {
            if (typingClip == null)
            {
                return;
            }

            if (_textCardTypingSource == null)
            {
                var go = new GameObject("VnTextCardTyping");
                go.transform.SetParent(transform, false);
                _textCardTypingSource = go.AddComponent<AudioSource>();
                _textCardTypingSource.playOnAwake = false;
                _textCardTypingSource.loop = true;
            }

            _textCardTypingSource.clip = typingClip;
            _textCardTypingSource.volume = 0.55f;
            _textCardTypingSource.time = 0f;
            _textCardTypingSource.Play();
        }

        private void StopTextCardTypingSound()
        {
            if (_textCardTypingSource != null && _textCardTypingSource.isPlaying)
            {
                _textCardTypingSource.Stop();
            }
        }

        private IEnumerator FadeRoutine(float seconds)
        {
            _transitionBusy = true;
            _skipTransitionRequested = false;
            _waitingAdvance = false;

            if (fadeOverlay == null)
            {
                _transitionBusy = false;
                _beatRoutine = null;
                Advance();
                yield break;
            }

            fadeOverlay.blocksRaycasts = true;
            var half = Mathf.Max(0.05f, seconds * 0.5f);
            yield return FadeCanvasSkippable(fadeOverlay, 0f, 1f, half);
            yield return FadeCanvasSkippable(fadeOverlay, 1f, 0f, half);
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;
            _transitionBusy = false;
            _beatRoutine = null;
            Advance();
        }

        private void ApplyCues(VnBeat beat)
        {
            if (!string.IsNullOrWhiteSpace(beat.bgId) && backgroundImage != null)
            {
                if (beat.bgId == VnBgIds.Black)
                {
                    ApplyBlackBackground();
                }
                else if (cueResolver != null && cueResolver.TryGetSprite(beat.bgId, out var sprite))
                {
                    backgroundImage.sprite = sprite;
                    backgroundImage.color = Color.white;
                    backgroundImage.enabled = true;
                }

                _currentBgId = beat.bgId;
                ApplyAmbienceForBackground(beat.bgId);
            }

            if (!string.IsNullOrWhiteSpace(beat.bgmId))
            {
                var pitch = beat.bgmPitch > 0.01f ? beat.bgmPitch : 1f;
                audioPlayer?.PlayBgm(beat.bgmId, true, pitch, beat.bgmStartTime);
            }
            else if (beat.bgmPitch > 0.01f)
            {
                audioPlayer?.SetBgmPitch(beat.bgmPitch);
            }

            if (!string.IsNullOrWhiteSpace(beat.sfxId))
            {
                audioPlayer?.PlaySfx(beat.sfxId);
            }

            ApplyDateHud(beat);
        }

        private void ApplyDateHud(VnBeat beat)
        {
            if (dateHud == null || beat == null)
            {
                return;
            }

            if (beat.hideDateHud
                || beat.kind == VnBeatKind.TextCard
                || beat.kind == VnBeatKind.Fade
                || beat.kind == VnBeatKind.End)
            {
                dateHud.Hide();
                return;
            }

            var showForDialogue = beat.kind == VnBeatKind.Line
                || beat.kind == VnBeatKind.Narration
                || beat.kind == VnBeatKind.Choice
                || beat.showDateHud;
            if (!showForDialogue)
            {
                return;
            }

            if (beat.dateHudFromMeta)
            {
                dateHud.ShowFromMeta();
                return;
            }

            if (!string.IsNullOrWhiteSpace(beat.dateHudDate))
            {
                _dateHudDate = beat.dateHudDate.Trim();
            }

            if (!string.IsNullOrWhiteSpace(beat.dateHudPhase))
            {
                _dateHudPhase = beat.dateHudPhase.Trim();
            }

            if (string.IsNullOrWhiteSpace(_dateHudDate))
            {
                _dateHudDate = openingDateDisplay;
            }

            if (string.IsNullOrWhiteSpace(_dateHudPhase))
            {
                _dateHudPhase = openingPhaseDisplay;
            }

            dateHud.ShowStatic(_dateHudDate, _dateHudPhase, useMoon: true);
        }

        private void ApplyAmbienceForBackground(string bgId)
        {
            if (bgId == VnBgIds.LuminaStreetNight
                || bgId == VnBgIds.LuminaAlleyNight
                || bgId == VnBgIds.LuminaAlleyHarutoBody
                || bgId == VnBgIds.LuminaSquareNight)
            {
                audioPlayer?.PlayAmbience(VnAudioIds.RainAmbience);
                return;
            }

            if (bgId == VnBgIds.Black || bgId == VnBgIds.LuxeConcert)
            {
                audioPlayer?.StopAmbience();
            }
        }

        private void ApplyBlackBackground()
        {
            if (backgroundImage == null)
            {
                return;
            }

            backgroundImage.sprite = null;
            backgroundImage.color = Color.black;
            backgroundImage.enabled = true;
        }

        private void Finish(VnBeat endBeat)
        {
            _running = false;
            _waitingAdvance = false;
            StopBeatRoutine();
            audioPlayer?.StopBgm();
            audioPlayer?.StopAmbience();

            if (beginHubOnEnd)
            {
                GameMetaSession.BeginHubAfterOpening();
            }

            ApplyFlags(endBeat?.setFlags);

            var next = script != null ? script.nextScene : null;
            if (string.IsNullOrWhiteSpace(next))
            {
                next = RunMapSceneCatalog.CampusHub;
            }

            if (!RunMapSceneLoader.LoadByName(next))
            {
                Debug.LogError($"[VnRuntime] Failed to load next scene '{next}'.");
            }
        }

        private static void ApplyFlags(string[] flags)
        {
            if (flags == null || flags.Length == 0)
            {
                return;
            }

            var state = GameMetaSession.Current;
            for (var i = 0; i < flags.Length; i++)
            {
                var flag = flags[i];
                if (!string.IsNullOrWhiteSpace(flag))
                {
                    state.SetFlag(flag);
                }
            }

            GameMetaSession.Save();
        }

        private IEnumerator FadeCanvasSkippable(CanvasGroup group, float from, float to, float seconds)
        {
            if (group == null)
            {
                yield break;
            }

            if (_skipTransitionRequested || seconds <= 0.001f)
            {
                group.alpha = to;
                yield break;
            }

            var t = 0f;
            group.alpha = from;
            while (t < seconds)
            {
                if (_skipTransitionRequested)
                {
                    group.alpha = to;
                    yield break;
                }

                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }

            group.alpha = to;
        }

        private static void SetPanel(CanvasGroup group, bool active)
        {
            if (group == null)
            {
                return;
            }

            group.gameObject.SetActive(active);
            group.alpha = active ? 1f : 0f;
            group.blocksRaycasts = active;
            group.interactable = active;
        }

#if UNITY_EDITOR
        public void EditorPreviewDialogueSample()
        {
            SetPanel(textCardPanel, false);
            SetPanel(dialoguePanel, true);
            if (nameplateText != null)
            {
                nameplateText.text = "Mei Lin";
            }

            if (typewriter != null && typewriter.BodyText != null)
            {
                typewriter.BodyText.text = "Sample dialogue — kéo Nameplate / DialoguePanel / DialogueBody trên Scene.";
            }

            dateHud?.ShowStatic(openingDateDisplay, openingPhaseDisplay, useMoon: true);
        }

        public void EditorPreviewTextCardSample()
        {
            SetPanel(dialoguePanel, false);
            SetPanel(textCardPanel, true);
            if (textCardBody != null)
            {
                textCardBody.text = "AT NEON CROSSING, LUMINA CITY";
            }

            dateHud?.Hide();
        }

        public void EditorHideSamples()
        {
            SetPanel(dialoguePanel, false);
            SetPanel(textCardPanel, false);
            if (nameplateText != null)
            {
                nameplateText.text = string.Empty;
            }

            if (typewriter != null && typewriter.BodyText != null)
            {
                typewriter.BodyText.text = string.Empty;
            }

            if (textCardBody != null)
            {
                textCardBody.text = string.Empty;
            }

            dateHud?.Hide();
            portraitView?.Hide();
        }
#endif
    }
}

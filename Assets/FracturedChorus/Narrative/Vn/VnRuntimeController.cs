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
        [SerializeField] private AudioClip typingClip;
        [SerializeField] private float defaultFadeSeconds = 0.6f;
        [SerializeField] private bool beginHubOnEnd;
        [SerializeField] private bool playOnStart = true;

        private int _index;
        private bool _waitingAdvance;
        private bool _running;
        private string _pendingText;

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
            VnUiFont.Apply(nameplateText, 26, FontStyle.Bold);
            var body = typewriter != null ? typewriter.BodyText : null;
            if (body == null && typewriter != null)
            {
                body = typewriter.GetComponent<Text>() ?? typewriter.GetComponentInChildren<Text>(true);
            }

            VnUiFont.Apply(body, 30, FontStyle.Normal);
            VnUiFont.Apply(textCardBody, 40, FontStyle.Normal);
        }

        private void Update()
        {
            if (!_running)
            {
                return;
            }

            if (!VnInput.WasAdvancePressedThisFrame())
            {
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

            StopAllCoroutines();
            _running = true;
            _index = 0;
            _waitingAdvance = false;
            SetPanel(textCardPanel, false);
            SetPanel(dialoguePanel, true);
            audioPlayer?.PlayAmbience(VnAudioIds.RainAmbience);
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

            ApplyCues(beat);

            switch (beat.kind)
            {
                case VnBeatKind.Cue:
                    Advance();
                    break;
                case VnBeatKind.Fade:
                    StartCoroutine(FadeRoutine(beat.duration > 0f ? beat.duration : defaultFadeSeconds));
                    break;
                case VnBeatKind.TextCard:
                    StartCoroutine(TextCardRoutine(beat));
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
            if (typewriter == null)
            {
                _waitingAdvance = true;
                return;
            }

            _waitingAdvance = false;
            typewriter.Type(_pendingText, () => { _waitingAdvance = true; });
        }

        private IEnumerator TextCardRoutine(VnBeat beat)
        {
            SetPanel(dialoguePanel, false);
            portraitView?.ClearStage();
            SetPanel(textCardPanel, true);
            if (textCardBody != null)
            {
                textCardBody.text = beat.text ?? string.Empty;
            }

            var hold = beat.duration > 0f ? beat.duration : 1.2f;
            var elapsed = 0f;
            while (elapsed < hold)
            {
                if (elapsed > 0.2f && VnInput.WasAdvancePressedThisFrame())
                {
                    break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            _waitingAdvance = true;
        }

        private IEnumerator FadeRoutine(float seconds)
        {
            if (fadeOverlay == null)
            {
                Advance();
                yield break;
            }

            fadeOverlay.blocksRaycasts = true;
            var half = Mathf.Max(0.05f, seconds * 0.5f);
            yield return FadeCanvas(fadeOverlay, 0f, 1f, half);
            yield return FadeCanvas(fadeOverlay, 1f, 0f, half);
            fadeOverlay.blocksRaycasts = false;
            Advance();
        }

        private void ApplyCues(VnBeat beat)
        {
            if (!string.IsNullOrWhiteSpace(beat.bgId) && backgroundImage != null)
            {
                if (cueResolver != null && cueResolver.TryGetSprite(beat.bgId, out var sprite))
                {
                    backgroundImage.sprite = sprite;
                    backgroundImage.enabled = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(beat.bgmId))
            {
                var pitch = beat.bgmPitch > 0.01f ? beat.bgmPitch : 1f;
                audioPlayer?.PlayBgm(beat.bgmId, true, pitch);
            }
            else if (beat.bgmPitch > 0.01f)
            {
                audioPlayer?.SetBgmPitch(beat.bgmPitch);
            }

            if (!string.IsNullOrWhiteSpace(beat.sfxId))
            {
                audioPlayer?.PlaySfx(beat.sfxId);
            }
        }

        private void Finish(VnBeat endBeat)
        {
            _running = false;
            _waitingAdvance = false;
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

        private static IEnumerator FadeCanvas(CanvasGroup group, float from, float to, float seconds)
        {
            var t = 0f;
            group.alpha = from;
            while (t < seconds)
            {
                t += Time.deltaTime;
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
    }
}

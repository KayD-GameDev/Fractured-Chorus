using FracturedChorus.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class EncounterLetterboxOverlay : MonoBehaviour
    {
        private const int SpectrumSize = 256;
        private const int HologramStyleVersion = 2;
        private const string NoteStripResource = "VFX/Combat/Encounter/encounter_hologram_note_strip_v1";
        private const string WaveNotesResourceV2 = "VFX/Combat/Encounter/encounter_hologram_wave_notes_v2";
        private const string WaveNotesResource = "VFX/Combat/Encounter/encounter_hologram_wave_notes_v1";

        [Header("Layout")]
        [SerializeField] [Range(0.06f, 0.32f)] private float barHeightNormalized = 0.18f;
        [SerializeField] private float waveBandHeight = 110f;
        [SerializeField] private float noteStripHeight = 52f;
        [SerializeField] private Color barColor = Color.black;
        [SerializeField] private Color waveColor = new Color(0.2f, 0.98f, 1f, 1f);
        [SerializeField] private Color glowColor = new Color(1f, 0.22f, 0.86f, 0.55f);
        [SerializeField] private Color noteTint = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private int canvasSortOrder = 520;

        [Header("Waveform")]
        [SerializeField] private FFTWindow fftWindow = FFTWindow.BlackmanHarris;
        [SerializeField] private float amplitude = 96f;
        [SerializeField] private float glowAmplitude = 118f;
        [SerializeField] private float spectrumGain = 58f;
        [SerializeField] [Range(0f, 0.9f)] private float smoothing = 0.03f;
        [SerializeField] private bool spectrumHue = false;
        [SerializeField] private int hologramBarCount = 96;

        [Header("Hologram note scroll")]
        [SerializeField] private Sprite noteStripSprite;
        [SerializeField] private Sprite waveNotesSprite;
        [SerializeField] private float noteScrollBaseSpeed = 220f;
        [SerializeField] private float noteScrollBeatBoost = 720f;
        [SerializeField] private bool loadResourcesFallback = true;

        private Canvas _canvas;
        private CanvasGroup _group;
        private RectTransform _topBar;
        private RectTransform _bottomBar;
        private MusicWaveformGraphic _topWave;
        private MusicWaveformGraphic _bottomWave;
        private RectTransform _topNoteA;
        private RectTransform _topNoteB;
        private RectTransform _bottomNoteA;
        private RectTransform _bottomNoteB;
        private CombatMusicController _music;
        private readonly float[] _spectrum = new float[SpectrumSize];
        private bool _visible;
        private bool _built;
        private int _builtStyleVersion;
        private float _scrollX;
        private float _stripWidth = 960f;

        public bool IsVisible => _visible;

        public void Show(CombatMusicController music)
        {
            EnsureBuilt();
            _music = music;
            _visible = true;
            _scrollX = 0f;
            if (_group != null)
            {
                _group.alpha = 1f;
                _group.blocksRaycasts = false;
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _visible = false;
            if (_topWave != null)
            {
                _topWave.ClearWave();
            }

            if (_bottomWave != null)
            {
                _bottomWave.ClearWave();
            }

            if (_group != null)
            {
                _group.alpha = 0f;
            }

            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!_visible || !_built)
            {
                return;
            }

            if (_topWave == null || _bottomWave == null)
            {
                return;
            }

            var level = 0f;
            if (_music != null && _music.TryFillSpectrum(_spectrum, fftWindow))
            {
                _topWave.SetSamples(_spectrum);
                _bottomWave.SetSamples(_spectrum);
                level = _topWave.GetAverageLevel();
            }

            var speed = noteScrollBaseSpeed + level * noteScrollBeatBoost;
            _scrollX += speed * Time.unscaledDeltaTime;
            if (_scrollX > _stripWidth)
            {
                _scrollX -= _stripWidth;
            }

            ApplyScroll(_topNoteA, _topNoteB, _scrollX);
            ApplyScroll(_bottomNoteA, _bottomNoteB, -_scrollX);
        }

        private static void ApplyScroll(RectTransform a, RectTransform b, float x)
        {
            if (a == null || b == null)
            {
                return;
            }

            var w = a.sizeDelta.x;
            if (w < 8f)
            {
                w = 960f;
            }

            var wrapped = x % w;
            if (wrapped < 0f)
            {
                wrapped += w;
            }

            a.anchoredPosition = new Vector2(-wrapped, a.anchoredPosition.y);
            b.anchoredPosition = new Vector2(-wrapped + w, b.anchoredPosition.y);
        }

        private void EnsureBuilt()
        {
            if (_built && _builtStyleVersion == HologramStyleVersion)
            {
                return;
            }

            ClearBuiltChildren();
            _built = true;
            _builtStyleVersion = HologramStyleVersion;
            EnsureSprites();
            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
            }

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = canvasSortOrder;
            if (GetComponent<CanvasScaler>() == null)
            {
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            _group = GetComponent<CanvasGroup>();
            if (_group == null)
            {
                _group = gameObject.AddComponent<CanvasGroup>();
            }

            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var root = transform as RectTransform;
            if (root != null)
            {
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.offsetMin = Vector2.zero;
                root.offsetMax = Vector2.zero;
            }

            _topBar = CreateBar("TopBar", true);
            _bottomBar = CreateBar("BottomBar", false);
            _topWave = CreateWave("TopWave", _topBar, flipVertical: true);
            _bottomWave = CreateWave("BottomWave", _bottomBar, flipVertical: false);
            CreateNotePair("TopNotes", _topBar, true, out _topNoteA, out _topNoteB);
            CreateNotePair("BottomNotes", _bottomBar, false, out _bottomNoteA, out _bottomNoteB);
        }

        private void EnsureSprites()
        {
            if (!loadResourcesFallback)
            {
                return;
            }

            if (noteStripSprite == null)
            {
                noteStripSprite = Resources.Load<Sprite>(NoteStripResource);
            }

            if (waveNotesSprite == null)
            {
                waveNotesSprite = Resources.Load<Sprite>(WaveNotesResourceV2)
                                 ?? Resources.Load<Sprite>(WaveNotesResource);
            }
        }

        private RectTransform CreateBar(string name, bool top)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            var image = go.GetComponent<Image>();
            image.color = barColor;
            image.raycastTarget = false;

            if (top)
            {
                rt.anchorMin = new Vector2(0f, 1f - barHeightNormalized);
                rt.anchorMax = new Vector2(1f, 1f);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, barHeightNormalized);
            }

            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private MusicWaveformGraphic CreateWave(string name, RectTransform parent, bool flipVertical)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(MusicWaveformGraphic));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (flipVertical)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(0f, waveBandHeight);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(0f, waveBandHeight);
            }

            var wave = go.GetComponent<MusicWaveformGraphic>();
            wave.raycastTarget = false;
            wave.color = Color.white;
            wave.ConfigureHologramStyle(
                waveColor,
                glowColor,
                amplitude,
                glowAmplitude,
                flipVertical,
                spectrumGain,
                smoothing,
                hologramBarCount);
            if (spectrumHue)
            {
                wave.Configure(
                    waveColor,
                    glowColor,
                    amplitude,
                    glowAmplitude,
                    flipVertical,
                    spectrumGain,
                    smoothing,
                    bars: true,
                    hueSpectrum: true);
            }

            return wave;
        }

        private void CreateNotePair(
            string name,
            RectTransform parent,
            bool top,
            out RectTransform a,
            out RectTransform b)
        {
            var host = new GameObject(name, typeof(RectTransform), typeof(RectMask2D));
            host.transform.SetParent(parent, false);
            var hostRt = host.GetComponent<RectTransform>();
            if (top)
            {
                hostRt.anchorMin = new Vector2(0f, 1f);
                hostRt.anchorMax = new Vector2(1f, 1f);
                hostRt.pivot = new Vector2(0.5f, 1f);
                hostRt.anchoredPosition = new Vector2(0f, -4f);
            }
            else
            {
                hostRt.anchorMin = new Vector2(0f, 0f);
                hostRt.anchorMax = new Vector2(1f, 0f);
                hostRt.pivot = new Vector2(0.5f, 0f);
                hostRt.anchoredPosition = new Vector2(0f, 4f);
            }

            hostRt.sizeDelta = new Vector2(0f, noteStripHeight);

            var sprite = waveNotesSprite != null ? waveNotesSprite : noteStripSprite;
            var screenW = Mathf.Max(1920f, Screen.width);
            a = CreateScrollTile(hostRt, "A", sprite, screenW);
            b = CreateScrollTile(hostRt, "B", sprite, screenW);
            _stripWidth = a != null ? Mathf.Max(screenW, a.sizeDelta.x) : screenW;
            if (b != null)
            {
                b.anchoredPosition = new Vector2(_stripWidth, b.anchoredPosition.y);
            }

            host.transform.SetAsLastSibling();
        }

        private RectTransform CreateScrollTile(
            RectTransform parent,
            string name,
            Sprite sprite,
            float minWidth)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = noteTint;
            image.raycastTarget = false;
            image.preserveAspect = false;
            image.type = Image.Type.Simple;

            var height = noteStripHeight;
            var width = Mathf.Max(minWidth, 1920f);
            if (sprite != null)
            {
                var aspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
                width = Mathf.Max(minWidth, height * aspect);
            }

            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }

        private void ClearBuiltChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }

            _topBar = null;
            _bottomBar = null;
            _topWave = null;
            _bottomWave = null;
            _topNoteA = null;
            _topNoteB = null;
            _bottomNoteA = null;
            _bottomNoteB = null;
            _built = false;
        }

        public static EncounterLetterboxOverlay EnsureCreated()
        {
            var existing = FindAnyObjectByType<EncounterLetterboxOverlay>(FindObjectsInactive.Include);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject("EncounterLetterboxOverlay", typeof(RectTransform));
            return go.AddComponent<EncounterLetterboxOverlay>();
        }
    }
}

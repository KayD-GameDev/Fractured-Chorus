using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FracturedChorus.Menu
{
    public sealed class OffBeatArchiveController : MonoBehaviour
    {
        private const string FavoriteKeyPrefix = "fc_offbeat_fav_";
        private static readonly Color Cyan = new Color(0f, 0.831f, 1f, 1f);
        private static readonly Color CyanDim = new Color(0f, 0.55f, 0.7f, 0.55f);

        [SerializeField] private OffBeatCatalogSO catalog;
        [SerializeField] private OffBeatMusicPlayer musicPlayer;
        [SerializeField] private MainMenuBgmController menuBgm;
        [SerializeField] private Transform catalogContent;
        [SerializeField] private OffBeatTrackRowView trackRowPrefab;
        [SerializeField] private Image coverImage;
        [SerializeField] private Sprite coverPlaceholder;
        [SerializeField] private Text songTitleLabel;
        [SerializeField] private Text artistLabel;
        [SerializeField] private Button favoriteButton;
        [SerializeField] private Image favoriteIcon;
        [SerializeField] private Slider seekSlider;
        [SerializeField] private Text timeCurrentLabel;
        [SerializeField] private Text timeTotalLabel;
        [SerializeField] private Button shuffleButton;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button playPauseButton;
        [SerializeField] private Text playPauseLabel;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button repeatButton;
        [SerializeField] private Image shuffleIcon;
        [SerializeField] private Image repeatIcon;
        [SerializeField] private Image playPauseIcon;
        [SerializeField] private OffBeatTransportButtonView shuffleView;
        [SerializeField] private OffBeatTransportButtonView previousView;
        [SerializeField] private OffBeatTransportButtonView playPauseView;
        [SerializeField] private OffBeatTransportButtonView nextView;
        [SerializeField] private OffBeatTransportButtonView repeatView;
        [SerializeField] private Sprite playSprite;
        [SerializeField] private Sprite pauseSprite;
        [SerializeField] private Sprite nextSprite;
        [SerializeField] private Sprite previousSprite;
        [SerializeField] private Sprite repeatSprite;
        [SerializeField] private Sprite shuffleSprite;
        [SerializeField] private Image waveformImage;
        [SerializeField] private OffBeatWaveformView waveformView;
        [SerializeField] private Image syncPodBackground;
        [SerializeField] private OffBeatDiscSwipeZone discSwipeZone;
        [SerializeField] private OffBeatVolumeArcView volumeArcView;
        [SerializeField] [Range(0f, 1f)] private float archiveDuckMultiplier = 0.12f;

        private float _archiveVolume = 0.85f;
        private bool _swipeWired;
        private bool _volumeWired;

        private readonly List<OffBeatTrackRowView> _rows = new List<OffBeatTrackRowView>();
        private readonly List<OffBeatTrackSO> _tracks = new List<OffBeatTrackSO>();
        private bool _active;
        private bool _seeking;
        private bool _wired;
        private int _focusIndex;

        public bool IsActive => _active;

        private void Awake()
        {
            EnsureMusicPlayer();
            EnsureSyncPodLayout();
            EnsureTransportIcons();
            Wire();
            WireSwipeAndVolume();
        }

        private void OnDestroy()
        {
            UnwirePlayerEvents();
            if (discSwipeZone != null)
            {
                discSwipeZone.SwipeNext -= OnDiscSwipeNext;
                discSwipeZone.SwipePrevious -= OnDiscSwipePrevious;
            }

            if (volumeArcView != null)
            {
                volumeArcView.VolumeChanged -= OnArchiveVolumeChanged;
            }
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            RefreshSeekUi();
            HandleArchiveInput();
        }

        public void OnShow()
        {
            EnsureMusicPlayer();
            EnsureSyncPodLayout();
            EnsureTransportIcons();
            if (catalog == null)
            {
                catalog = Resources.Load<OffBeatCatalogSO>("OffBeat/OffBeatCatalog");
            }

            Wire();
            WireSwipeAndVolume();
            RebuildCatalog();
            _active = true;
            _focusIndex = Mathf.Clamp(_focusIndex, 0, Mathf.Max(0, _tracks.Count - 1));
            if (_tracks.Count > 0)
            {
                musicPlayer.SelectIndex(_focusIndex, autoPlay: false);
            }

            ApplyArchiveVolume();
            RefreshAll();
        }

        public void OnHide()
        {
            _active = false;
            _seeking = false;
            if (musicPlayer != null)
            {
                musicPlayer.Stop();
            }

            RestoreMenuBgm();
        }

        public void ApplyMasterVolume(float masterVolume)
        {
            musicPlayer?.ApplyMasterVolume(masterVolume);
            ApplyArchiveVolume();
        }

        private void EnsureMusicPlayer()
        {
            if (musicPlayer == null)
            {
                musicPlayer = GetComponent<OffBeatMusicPlayer>();
            }

            if (musicPlayer == null)
            {
                musicPlayer = gameObject.AddComponent<OffBeatMusicPlayer>();
            }

            if (menuBgm == null)
            {
                menuBgm = FindAnyObjectByType<MainMenuBgmController>();
            }

            EnsureWaveform();
        }

        private void EnsureSyncPodLayout()
        {
            var playerRoot = transform.Find("ArchivePanel/PlayerRoot");
            if (playerRoot == null)
            {
                return;
            }

            HideLegacyTransport(playerRoot);

            if (previousButton != null)
            {
                previousButton.gameObject.SetActive(false);
            }

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(false);
            }

            if (seekSlider != null)
            {
                seekSlider.gameObject.SetActive(false);
            }

            if (timeCurrentLabel != null)
            {
                timeCurrentLabel.gameObject.SetActive(false);
            }

            if (timeTotalLabel != null)
            {
                timeTotalLabel.gameObject.SetActive(false);
            }

            if (syncPodBackground == null)
            {
                var bgTf = playerRoot.Find("SyncPodBg");
                if (bgTf != null)
                {
                    syncPodBackground = bgTf.GetComponent<Image>();
                }
            }

            if (syncPodBackground != null && syncPodBackground.sprite == null)
            {
                syncPodBackground.sprite = Resources.Load<Sprite>("UI/OffBeat/offbeat_syncpod_bg_v2");
                if (syncPodBackground.sprite == null)
                {
                    var tex = Resources.Load<Texture2D>("UI/OffBeat/offbeat_syncpod_bg_v2");
                    if (tex != null)
                    {
                        syncPodBackground.sprite = Sprite.Create(
                            tex,
                            new Rect(0f, 0f, tex.width, tex.height),
                            new Vector2(0.5f, 0.5f),
                            100f);
                    }
                }

                syncPodBackground.color = Color.white;
                syncPodBackground.preserveAspect = true;
                syncPodBackground.raycastTarget = false;
            }

            var discFace = playerRoot.Find("DiscFace");
            if (discFace == null)
            {
                discFace = EnsureDiscFace(playerRoot);
            }

            if (discFace != null)
            {
                if (discFace.GetComponent<RectMask2D>() == null)
                {
                    discFace.gameObject.AddComponent<RectMask2D>();
                }

                if (discSwipeZone == null)
                {
                    discSwipeZone = discFace.GetComponent<OffBeatDiscSwipeZone>();
                }

                if (discSwipeZone == null)
                {
                    discSwipeZone = discFace.gameObject.AddComponent<OffBeatDiscSwipeZone>();
                }

                RelocateWaveformToFace(playerRoot, discFace);
                RelocateTransportToFace(playerRoot, discFace);
            }

            if (volumeArcView == null)
            {
                volumeArcView = playerRoot.GetComponentInChildren<OffBeatVolumeArcView>(true);
            }

            if (volumeArcView != null)
            {
                var volRoot = volumeArcView.transform.parent;
                if (volRoot != null && volRoot.name == "VolumeArcRoot")
                {
                    var trackTf = volRoot.Find("Track");
                    var fillTf = volRoot.Find("Fill");
                    var trackImg = trackTf != null ? trackTf.GetComponent<Image>() : null;
                    var fillImg = fillTf != null ? fillTf.GetComponent<Image>() : null;
                    volumeArcView.Bind(trackImg, fillImg);
                    volRoot.SetAsLastSibling();
                }
            }
            else if (discFace != null)
            {
                discFace.SetAsLastSibling();
            }

            EnsureDiscCover(discFace);
        }

        private static void HideLegacyTransport(Transform playerRoot)
        {
            SetInactive(playerRoot.Find("Controls/Previous"));
            SetInactive(playerRoot.Find("Controls/Next"));
            SetInactive(playerRoot.Find("Controls/Seek"));
            SetInactive(playerRoot.Find("Controls/SeekSlider"));
            SetInactive(playerRoot.Find("Seek"));
            SetInactive(playerRoot.Find("SeekSlider"));
            SetInactive(playerRoot.Find("TimeCurrent"));
            SetInactive(playerRoot.Find("TimeTotal"));
            SetInactive(playerRoot.Find("Controls/TimeCurrent"));
            SetInactive(playerRoot.Find("Controls/TimeTotal"));
            SetInactive(playerRoot.Find("DiscFace/Controls/Previous"));
            SetInactive(playerRoot.Find("DiscFace/Controls/Next"));
        }

        private static void SetInactive(Transform tf)
        {
            if (tf != null)
            {
                tf.gameObject.SetActive(false);
            }
        }

        private Transform EnsureDiscFace(Transform playerRoot)
        {
            var go = new GameObject("DiscFace", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(playerRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(420f, 420f);
            rt.anchoredPosition = new Vector2(0f, 20f);
            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;
            return go.transform;
        }

        private void EnsureDiscCover(Transform discFace)
        {
            if (discFace == null)
            {
                return;
            }

            var circle = GetOrCreateCircleSprite();
            var discPlate = discFace.Find("DiscPlate");
            if (discPlate == null)
            {
                var plateGo = new GameObject("DiscPlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                plateGo.transform.SetParent(discFace, false);
                plateGo.transform.SetAsFirstSibling();
                var plateRt = plateGo.GetComponent<RectTransform>();
                plateRt.anchorMin = new Vector2(0.5f, 1f);
                plateRt.anchorMax = new Vector2(0.5f, 1f);
                plateRt.pivot = new Vector2(0.5f, 1f);
                plateRt.anchoredPosition = new Vector2(0f, -18f);
                plateRt.sizeDelta = new Vector2(112f, 112f);
                var plateImg = plateGo.GetComponent<Image>();
                plateImg.sprite = circle;
                plateImg.type = Image.Type.Simple;
                plateImg.preserveAspect = true;
                plateImg.raycastTarget = false;
                plateImg.color = new Color(0.1f, 0.14f, 0.2f, 1f);

                var grooveGo = new GameObject("Grooves", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                grooveGo.transform.SetParent(plateGo.transform, false);
                var grooveRt = grooveGo.GetComponent<RectTransform>();
                grooveRt.anchorMin = Vector2.zero;
                grooveRt.anchorMax = Vector2.one;
                grooveRt.offsetMin = new Vector2(10f, 10f);
                grooveRt.offsetMax = new Vector2(-10f, -10f);
                var grooveImg = grooveGo.GetComponent<Image>();
                grooveImg.sprite = GetOrCreateRingSprite();
                grooveImg.type = Image.Type.Simple;
                grooveImg.preserveAspect = true;
                grooveImg.raycastTarget = false;
                grooveImg.color = new Color(0f, 0.75f, 1f, 0.4f);

                var hubGo = new GameObject("Hub", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                hubGo.transform.SetParent(plateGo.transform, false);
                var hubRt = hubGo.GetComponent<RectTransform>();
                hubRt.anchorMin = new Vector2(0.5f, 0.5f);
                hubRt.anchorMax = new Vector2(0.5f, 0.5f);
                hubRt.pivot = new Vector2(0.5f, 0.5f);
                hubRt.sizeDelta = new Vector2(28f, 28f);
                var hubImg = hubGo.GetComponent<Image>();
                hubImg.sprite = circle;
                hubImg.preserveAspect = true;
                hubImg.raycastTarget = false;
                hubImg.color = new Color(0.05f, 0.08f, 0.12f, 1f);
                discPlate = plateGo.transform;
            }

            if (coverImage == null)
            {
                var coverTf = discFace.Find("CoverImage");
                if (coverTf != null)
                {
                    coverImage = coverTf.GetComponent<Image>();
                }
            }

            if (coverImage == null)
            {
                var coverGo = new GameObject("CoverImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                coverGo.transform.SetParent(discFace, false);
                if (discPlate != null)
                {
                    coverGo.transform.SetSiblingIndex(discPlate.GetSiblingIndex() + 1);
                }

                var coverRtNew = coverGo.GetComponent<RectTransform>();
                coverRtNew.anchorMin = new Vector2(0.5f, 1f);
                coverRtNew.anchorMax = new Vector2(0.5f, 1f);
                coverRtNew.pivot = new Vector2(0.5f, 1f);
                coverRtNew.anchoredPosition = new Vector2(0f, -34f);
                coverRtNew.sizeDelta = new Vector2(64f, 64f);
                coverImage = coverGo.GetComponent<Image>();
            }

            if (coverPlaceholder == null)
            {
                coverPlaceholder = circle;
            }

            coverImage.preserveAspect = true;
            coverImage.raycastTarget = false;
            if (coverImage.sprite == null)
            {
                coverImage.sprite = coverPlaceholder;
                coverImage.color = new Color(0.12f, 0.22f, 0.32f, 1f);
            }
        }

        private static Sprite s_circleSprite;
        private static Sprite s_ringSprite;

        private static Sprite GetOrCreateCircleSprite()
        {
            if (s_circleSprite != null)
            {
                return s_circleSprite;
            }

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "OffBeatDiscCircle",
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var center = (size - 1) * 0.5f;
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var d = Mathf.Sqrt(dx * dx + dy * dy);
                    var a = d <= center - 1f ? (byte)255 : (byte)0;
                    if (d > center - 2f && d <= center)
                    {
                        a = (byte)Mathf.Clamp(Mathf.RoundToInt((center - d) * 255f), 0, 255);
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            s_circleSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            return s_circleSprite;
        }

        private static Sprite GetOrCreateRingSprite()
        {
            if (s_ringSprite != null)
            {
                return s_ringSprite;
            }

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "OffBeatDiscRing",
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var center = (size - 1) * 0.5f;
            var outer = center - 2f;
            var inner = center * 0.55f;
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var d = Mathf.Sqrt(dx * dx + dy * dy);
                    byte a = 0;
                    if (d <= outer && d >= inner)
                    {
                        a = 180;
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            s_ringSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            return s_ringSprite;
        }

        private void RelocateWaveformToFace(Transform playerRoot, Transform discFace)
        {
            var wave = discFace.Find("Waveform");
            if (wave == null)
            {
                wave = playerRoot.Find("Waveform");
            }

            if (wave == null)
            {
                return;
            }

            if (wave.parent != discFace)
            {
                wave.SetParent(discFace, worldPositionStays: true);
            }

            if (waveformImage == null)
            {
                waveformImage = wave.GetComponent<Image>();
            }
        }

        private void RelocateTransportToFace(Transform playerRoot, Transform discFace)
        {
            var controls = discFace.Find("Controls");
            if (controls == null)
            {
                controls = playerRoot.Find("Controls");
                if (controls != null)
                {
                    controls.SetParent(discFace, worldPositionStays: true);
                }
            }

            if (coverImage != null && coverImage.transform.parent != discFace)
            {
                coverImage.transform.SetParent(discFace, worldPositionStays: true);
            }

            if (songTitleLabel != null && songTitleLabel.transform.parent != discFace)
            {
                songTitleLabel.transform.SetParent(discFace, worldPositionStays: true);
            }
        }

        private void WireSwipeAndVolume()
        {
            if (!_swipeWired && discSwipeZone != null)
            {
                _swipeWired = true;
                discSwipeZone.SwipeNext += OnDiscSwipeNext;
                discSwipeZone.SwipePrevious += OnDiscSwipePrevious;
            }

            if (!_volumeWired && volumeArcView != null)
            {
                _volumeWired = true;
                volumeArcView.VolumeChanged += OnArchiveVolumeChanged;
                _archiveVolume = volumeArcView.Volume;
                musicPlayer?.SetVolume(_archiveVolume);
            }
        }

        private void OnDiscSwipeNext()
        {
            musicPlayer?.Next();
            if (musicPlayer != null && musicPlayer.IsPlaying)
            {
                DuckMenuBgm();
            }

            RefreshAll();
        }

        private void OnDiscSwipePrevious()
        {
            musicPlayer?.Previous();
            if (musicPlayer != null && musicPlayer.IsPlaying)
            {
                DuckMenuBgm();
            }

            RefreshAll();
        }

        private void OnArchiveVolumeChanged(float value)
        {
            _archiveVolume = value;
            musicPlayer?.SetVolume(value);
        }

        private void ApplyArchiveVolume()
        {
            if (volumeArcView != null)
            {
                _archiveVolume = volumeArcView.Volume;
            }
            else
            {
                _archiveVolume = PlayerPrefs.GetFloat("fc_offbeat_volume", 0.85f);
            }

            musicPlayer?.SetVolume(_archiveVolume);
        }

        private void EnsureWaveform()
        {
            Transform wave = null;
            if (waveformImage != null)
            {
                wave = waveformImage.transform;
            }

            if (wave == null)
            {
                wave = transform.Find("ArchivePanel/PlayerRoot/DiscFace/Waveform");
            }

            if (wave == null)
            {
                wave = transform.Find("ArchivePanel/PlayerRoot/Waveform");
            }

            if (wave == null)
            {
                return;
            }

            for (var i = 0; i < wave.childCount; i++)
            {
                var child = wave.GetChild(i);
                if (child.name.StartsWith("Bar_"))
                {
                    child.gameObject.SetActive(false);
                }
            }

            var panel = wave.GetComponent<Image>();
            if (panel != null)
            {
                panel.enabled = true;
                panel.color = new Color(0.97f, 0.94f, 0.98f, 0f);
                panel.raycastTarget = false;
                waveformImage = panel;
            }

            var draw = wave.Find("WaveDraw");
            if (draw == null)
            {
                var drawGo = new GameObject("WaveDraw", typeof(RectTransform), typeof(CanvasRenderer));
                drawGo.transform.SetParent(wave, false);
                var rt = drawGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                draw = drawGo.transform;
            }

            var existingOnRoot = wave.GetComponent<OffBeatWaveformView>();
            if (existingOnRoot != null)
            {
                Destroy(existingOnRoot);
            }

            waveformView = draw.GetComponent<OffBeatWaveformView>();
            if (waveformView == null)
            {
                waveformView = draw.gameObject.AddComponent<OffBeatWaveformView>();
            }

            waveformView.raycastTarget = false;
            waveformView.color = Color.white;
            if (musicPlayer != null)
            {
                waveformView.Bind(musicPlayer.Source);
            }
        }

        private void Wire()
        {
            if (_wired)
            {
                return;
            }

            _wired = true;
            WirePlayerEvents();

            if (favoriteButton != null)
            {
                favoriteButton.onClick.AddListener(ToggleFavorite);
            }

            if (shuffleButton != null)
            {
                shuffleButton.onClick.AddListener(() =>
                {
                    musicPlayer?.ToggleShuffle();
                    RefreshTransport();
                });
            }

            if (previousButton != null)
            {
                previousButton.onClick.AddListener(() =>
                {
                    musicPlayer?.Previous();
                    StartCoroutine(FlashTransport(previousView));
                });
            }

            if (playPauseButton != null)
            {
                playPauseButton.onClick.AddListener(OnPlayPauseClicked);
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(() =>
                {
                    musicPlayer?.Next();
                    StartCoroutine(FlashTransport(nextView));
                });
            }

            if (repeatButton != null)
            {
                repeatButton.onClick.AddListener(() =>
                {
                    musicPlayer?.ToggleRepeat();
                    RefreshTransport();
                });
            }

            if (seekSlider != null)
            {
                seekSlider.onValueChanged.AddListener(OnSeekValueChanged);
                var events = seekSlider.gameObject.GetComponent<OffBeatSeekEvents>();
                if (events == null)
                {
                    events = seekSlider.gameObject.AddComponent<OffBeatSeekEvents>();
                }

                events.Bind(BeginSeek, EndSeek);
            }
        }

        private void WirePlayerEvents()
        {
            if (musicPlayer == null)
            {
                return;
            }

            musicPlayer.TrackChanged -= OnTrackChanged;
            musicPlayer.PlaybackStateChanged -= OnPlaybackStateChanged;
            musicPlayer.TrackChanged += OnTrackChanged;
            musicPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
        }

        private void UnwirePlayerEvents()
        {
            if (musicPlayer == null)
            {
                return;
            }

            musicPlayer.TrackChanged -= OnTrackChanged;
            musicPlayer.PlaybackStateChanged -= OnPlaybackStateChanged;
        }

        private void RebuildCatalog()
        {
            _tracks.Clear();
            if (catalog != null && catalog.tracks != null)
            {
                for (var i = 0; i < catalog.tracks.Length; i++)
                {
                    var track = catalog.tracks[i];
                    if (track != null && track.clip != null)
                    {
                        _tracks.Add(track);
                    }
                }
            }

            if (catalogContent == null)
            {
                musicPlayer?.SetPlaylist(_tracks, 0);
                return;
            }

            for (var i = catalogContent.childCount - 1; i >= 0; i--)
            {
                var child = catalogContent.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }

            _rows.Clear();

            for (var i = 0; i < _tracks.Count; i++)
            {
                var row = CreateRow(i, _tracks[i]);
                if (row != null)
                {
                    _rows.Add(row);
                }
            }

            musicPlayer?.SetPlaylist(_tracks, 0);
        }

        private OffBeatTrackRowView CreateRow(int index, OffBeatTrackSO track)
        {
            OffBeatTrackRowView row;
            if (trackRowPrefab != null)
            {
                row = Instantiate(trackRowPrefab, catalogContent);
                row.gameObject.SetActive(true);
            }
            else
            {
                row = BuildRuntimeRow(catalogContent);
            }

            row.Bind(index, track, IsFavorite(track.trackId), SelectTrack, FocusTrack);
            row.SetSelected(false);
            return row;
        }

        private static OffBeatTrackRowView BuildRuntimeRow(Transform parent)
        {
            var go = new GameObject("TrackRow", typeof(RectTransform), typeof(Image), typeof(OffBeatTrackRowView), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 64f);
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 64f;
            le.preferredHeight = 64f;

            var bg = go.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0f);
            bg.raycastTarget = true;

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(go.transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.45f);
            titleRt.anchorMax = new Vector2(0.9f, 1f);
            titleRt.offsetMin = new Vector2(12f, 0f);
            titleRt.offsetMax = new Vector2(-8f, -4f);
            var title = titleGo.GetComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 20;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.LowerLeft;
            title.raycastTarget = false;

            var artistGo = new GameObject("Artist", typeof(RectTransform), typeof(Text));
            artistGo.transform.SetParent(go.transform, false);
            var artistRt = artistGo.GetComponent<RectTransform>();
            artistRt.anchorMin = new Vector2(0f, 0f);
            artistRt.anchorMax = new Vector2(0.9f, 0.5f);
            artistRt.offsetMin = new Vector2(12f, 4f);
            artistRt.offsetMax = new Vector2(-8f, 0f);
            var artist = artistGo.GetComponent<Text>();
            artist.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            artist.fontSize = 15;
            artist.alignment = TextAnchor.UpperLeft;
            artist.raycastTarget = false;

            var favGo = new GameObject("Fav", typeof(RectTransform), typeof(Image));
            favGo.transform.SetParent(go.transform, false);
            var favRt = favGo.GetComponent<RectTransform>();
            favRt.anchorMin = new Vector2(0.9f, 0.3f);
            favRt.anchorMax = new Vector2(1f, 0.7f);
            favRt.offsetMin = Vector2.zero;
            favRt.offsetMax = Vector2.zero;
            var fav = favGo.GetComponent<Image>();
            fav.color = Cyan;
            fav.raycastTarget = false;
            fav.enabled = false;

            var row = go.GetComponent<OffBeatTrackRowView>();
            row.Configure(title, artist, bg, fav);
            return row;
        }

        private void SelectTrack(int index)
        {
            _focusIndex = index;
            musicPlayer?.SelectIndex(index, autoPlay: true);
            DuckMenuBgm();
            RefreshAll();
        }

        private void FocusTrack(int index)
        {
            _focusIndex = index;
            RefreshRowSelection();
        }

        private void OnPlayPauseClicked()
        {
            if (musicPlayer == null)
            {
                return;
            }

            musicPlayer.TogglePlayPause();
            if (musicPlayer.IsPlaying)
            {
                DuckMenuBgm();
            }
            else
            {
                RestoreMenuBgm();
            }

            RefreshTransport();
        }

        private void OnTrackChanged()
        {
            _focusIndex = musicPlayer != null ? musicPlayer.CurrentIndex : _focusIndex;
            if (musicPlayer != null && musicPlayer.IsPlaying)
            {
                DuckMenuBgm();
            }

            RefreshAll();
        }

        private void OnPlaybackStateChanged()
        {
            if (musicPlayer != null && musicPlayer.IsPlaying)
            {
                DuckMenuBgm();
            }
            else
            {
                RestoreMenuBgm();
            }

            RefreshTransport();
        }

        private void BeginSeek()
        {
            _seeking = true;
            musicPlayer?.BeginSeek();
        }

        private void EndSeek()
        {
            _seeking = false;
            musicPlayer?.EndSeek();
            if (musicPlayer != null && musicPlayer.IsPlaying)
            {
                DuckMenuBgm();
            }
        }

        private void OnSeekValueChanged(float value)
        {
            if (!_seeking)
            {
                return;
            }

            musicPlayer?.SetNormalizedTime(value);
            RefreshTimeLabels();
        }

        private void RefreshAll()
        {
            RefreshNowPlaying();
            RefreshRowSelection();
            RefreshTransport();
            RefreshSeekUi();
        }

        private void RefreshNowPlaying()
        {
            var track = musicPlayer != null ? musicPlayer.CurrentTrack : null;
            if (songTitleLabel != null)
            {
                songTitleLabel.text = track != null ? track.title : "No track";
            }

            if (artistLabel != null)
            {
                artistLabel.text = track != null ? track.artist : string.Empty;
            }

            if (coverImage != null)
            {
                if (track != null && track.cover != null)
                {
                    coverImage.sprite = track.cover;
                    coverImage.color = Color.white;
                }
                else
                {
                    coverImage.sprite = coverPlaceholder != null ? coverPlaceholder : GetOrCreateCircleSprite();
                    coverImage.color = new Color(0.12f, 0.22f, 0.32f, 1f);
                }

                coverImage.preserveAspect = true;
            }

            RefreshFavoriteIcon(track);
        }

        private void RefreshFavoriteIcon(OffBeatTrackSO track)
        {
            var fav = track != null && IsFavorite(track.trackId);
            if (favoriteIcon != null)
            {
                favoriteIcon.color = fav ? Cyan : CyanDim;
            }
        }

        private void RefreshRowSelection()
        {
            var selected = musicPlayer != null ? musicPlayer.CurrentIndex : _focusIndex;
            for (var i = 0; i < _rows.Count; i++)
            {
                _rows[i].SetSelected(i == selected || i == _focusIndex);
            }
        }

        private void RefreshTransport()
        {
            var playing = musicPlayer != null && musicPlayer.IsPlaying;
            var shuffleOn = musicPlayer != null && musicPlayer.ShuffleEnabled;
            var repeatOn = musicPlayer != null
                           && musicPlayer.Repeat != OffBeatMusicPlayer.RepeatMode.Off;

            var playPauseSprite = playing ? pauseSprite : playSprite;
            if (playPauseIcon != null)
            {
                if (playPauseSprite != null)
                {
                    playPauseIcon.sprite = playPauseSprite;
                    playPauseIcon.enabled = true;
                    playPauseIcon.preserveAspect = true;
                    playPauseIcon.color = Color.white;
                }
            }

            if (playPauseLabel != null)
            {
                var showLabel = playPauseSprite == null;
                playPauseLabel.enabled = showLabel;
                if (showLabel)
                {
                    playPauseLabel.text = playing ? "II" : "▶";
                }
                else
                {
                    playPauseLabel.text = string.Empty;
                }
            }

            playPauseView?.SetSprite(playPauseSprite);
            playPauseView?.SetActiveVisual(playing);

            shuffleView?.SetSprite(shuffleSprite);
            shuffleView?.SetActiveVisual(shuffleOn);
            if (shuffleIcon != null && shuffleView == null)
            {
                shuffleIcon.color = shuffleOn ? Cyan : CyanDim;
            }

            repeatView?.SetSprite(repeatSprite);
            repeatView?.SetActiveVisual(repeatOn);
            if (repeatIcon != null && repeatView == null)
            {
                repeatIcon.color = repeatOn ? Cyan : CyanDim;
            }

            previousView?.SetSprite(previousSprite);
            previousView?.SetActiveVisual(false);
            nextView?.SetSprite(nextSprite);
            nextView?.SetActiveVisual(false);
        }

        public void EnsureTransportIcons()
        {
            playSprite = ResolveIcon(null, "offbeat_btn_play_v2")
                           ?? ResolveIcon(playSprite, "offbeat_btn_play_v1");
            pauseSprite = ResolveIcon(null, "offbeat_btn_pause_v2")
                            ?? ResolveIcon(pauseSprite, "offbeat_btn_pause_v1");
            nextSprite = ResolveIcon(nextSprite, "offbeat_btn_next_v1");
            previousSprite = ResolveIcon(previousSprite, "offbeat_btn_prev_v1");
            repeatSprite = ResolveIcon(null, "offbeat_btn_repeat_v2")
                           ?? ResolveIcon(repeatSprite, "offbeat_btn_repeat_v1");
            shuffleSprite = ResolveIcon(null, "offbeat_btn_shuffle_v2")
                            ?? ResolveIcon(shuffleSprite, "offbeat_btn_shuffle_v1");

            Image prevIcon = null;
            Image nextIcon = null;

            // Never reuse plate images serialized as shuffleIcon/repeatIcon from old setup.
            if (shuffleIcon != null && shuffleButton != null &&
                ReferenceEquals(shuffleIcon.gameObject, shuffleButton.gameObject))
            {
                shuffleIcon = null;
            }

            if (repeatIcon != null && repeatButton != null &&
                ReferenceEquals(repeatIcon.gameObject, repeatButton.gameObject))
            {
                repeatIcon = null;
            }

            shuffleView = EnsureTransportView(shuffleButton, shuffleView, shuffleSprite, ref shuffleIcon);
            previousView = EnsureTransportView(previousButton, previousView, previousSprite, ref prevIcon);
            playPauseView = EnsureTransportView(playPauseButton, playPauseView, playSprite, ref playPauseIcon);
            nextView = EnsureTransportView(nextButton, nextView, nextSprite, ref nextIcon);
            repeatView = EnsureTransportView(repeatButton, repeatView, repeatSprite, ref repeatIcon);

            RepairCollapsedTransportButtons();

            if (playPauseLabel != null && playPauseIcon != null && playPauseIcon.sprite != null)
            {
                playPauseLabel.enabled = false;
                playPauseLabel.text = string.Empty;
            }

            RefreshTransport();
        }

        private void RepairCollapsedTransportButtons()
        {
            RepairCollapsedButton(shuffleButton, 56f);
            RepairCollapsedButton(playPauseButton, 64f);
            RepairCollapsedButton(repeatButton, 56f);

            if (playPauseButton == null)
            {
                return;
            }

            var controls = playPauseButton.transform.parent as RectTransform;
            if (controls == null)
            {
                return;
            }

            if (controls.rect.width < 8f || controls.rect.height < 8f)
            {
                controls.sizeDelta = new Vector2(220f, 64f);
            }

            var hlg = controls.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
            }
        }

        private static void RepairCollapsedButton(Button button, float fallbackSize)
        {
            if (button == null)
            {
                return;
            }

            var rt = button.transform as RectTransform;
            if (rt == null)
            {
                return;
            }

            var collapsed = rt.rect.width < 1f || rt.rect.height < 1f
                            || (Mathf.Abs(rt.sizeDelta.x) < 0.01f && Mathf.Abs(rt.sizeDelta.y) < 0.01f);
            if (!collapsed)
            {
                return;
            }

            rt.sizeDelta = new Vector2(fallbackSize, fallbackSize);
            var le = button.GetComponent<LayoutElement>();
            if (le == null)
            {
                le = button.gameObject.AddComponent<LayoutElement>();
            }

            le.ignoreLayout = false;
            if (le.preferredWidth < 1f)
            {
                le.preferredWidth = fallbackSize;
            }

            if (le.preferredHeight < 1f)
            {
                le.preferredHeight = fallbackSize;
            }
        }

        private static Sprite ResolveIcon(Sprite current, string resourceName)
        {
            if (current != null)
            {
                return current;
            }

            var path = "UI/OffBeat/" + resourceName;
            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            var sprites = Resources.LoadAll<Sprite>(path);
            if (sprites != null && sprites.Length > 0)
            {
                return sprites[0];
            }

            var tex = Resources.Load<Texture2D>(path);
            if (tex != null)
            {
                return Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }

            Debug.LogWarning($"[OffBeat] Missing transport icon Resources/{path}");
            return null;
        }

        private static OffBeatTransportButtonView EnsureTransportView(
            Button button,
            OffBeatTransportButtonView existing,
            Sprite sprite,
            ref Image iconRef)
        {
            if (button == null)
            {
                return existing;
            }

            var view = existing != null ? existing : button.GetComponent<OffBeatTransportButtonView>();
            if (view == null)
            {
                view = button.gameObject.AddComponent<OffBeatTransportButtonView>();
            }

            var plate = button.targetGraphic as Image;
            if (plate == null)
            {
                plate = button.GetComponent<Image>();
            }

            Image icon = null;
            var iconTf = button.transform.Find("Icon");
            if (iconTf != null)
            {
                icon = iconTf.GetComponent<Image>();
            }

            if (icon == null || (plate != null && ReferenceEquals(icon, plate)))
            {
                var labelTf = button.transform.Find("Label");
                if (labelTf != null)
                {
                    var label = labelTf.GetComponent<Text>();
                    if (label != null)
                    {
                        label.enabled = false;
                        label.text = string.Empty;
                    }
                }

                if (icon == null || (plate != null && ReferenceEquals(icon, plate)))
                {
                    var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    iconGo.transform.SetParent(button.transform, false);
                    var rt = iconGo.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.1f, 0.1f);
                    rt.anchorMax = new Vector2(0.9f, 0.9f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    icon = iconGo.GetComponent<Image>();
                }

                icon.raycastTarget = false;
                icon.preserveAspect = true;
                icon.color = Color.white;
            }

            var glowTf = button.transform.Find("Glow");
            Image glow;
            if (glowTf == null)
            {
                var glowGo = new GameObject("Glow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                glowGo.transform.SetParent(button.transform, false);
                glowGo.transform.SetAsFirstSibling();
                var grt = glowGo.GetComponent<RectTransform>();
                grt.anchorMin = Vector2.zero;
                grt.anchorMax = Vector2.one;
                grt.offsetMin = new Vector2(-6f, -6f);
                grt.offsetMax = new Vector2(6f, 6f);
                glow = glowGo.GetComponent<Image>();
                glow.raycastTarget = false;
                glow.color = new Color(0f, 0.85f, 1f, 0f);
            }
            else
            {
                glow = glowTf.GetComponent<Image>();
            }

            iconRef = icon;
            view.Configure(icon, glow, plate);
            if (sprite != null)
            {
                view.SetSprite(sprite);
                icon.color = Color.white;
            }

            view.SetActiveVisual(false);
            return view;
        }

        private System.Collections.IEnumerator FlashTransport(OffBeatTransportButtonView view)
        {
            if (view == null)
            {
                yield break;
            }

            view.SetActiveVisual(true);
            yield return new WaitForSecondsRealtime(0.18f);
            RefreshTransport();
        }

        private void RefreshSeekUi()
        {
            if (musicPlayer == null)
            {
                return;
            }

            if (!_seeking && seekSlider != null)
            {
                seekSlider.SetValueWithoutNotify(musicPlayer.NormalizedTime);
            }

            RefreshTimeLabels();
        }

        private void RefreshTimeLabels()
        {
            if (musicPlayer == null)
            {
                return;
            }

            if (timeCurrentLabel != null)
            {
                timeCurrentLabel.text = FormatTime(musicPlayer.Time);
            }

            if (timeTotalLabel != null)
            {
                timeTotalLabel.text = FormatTime(musicPlayer.Duration);
            }
        }

        private void ToggleFavorite()
        {
            var track = musicPlayer != null ? musicPlayer.CurrentTrack : null;
            if (track == null || string.IsNullOrEmpty(track.trackId))
            {
                return;
            }

            var key = FavoriteKeyPrefix + track.trackId;
            var next = !IsFavorite(track.trackId);
            PlayerPrefs.SetInt(key, next ? 1 : 0);
            PlayerPrefs.Save();
            RefreshFavoriteIcon(track);

            if (_focusIndex >= 0 && _focusIndex < _rows.Count && _focusIndex < _tracks.Count)
            {
                _rows[_focusIndex].Bind(
                    _focusIndex,
                    _tracks[_focusIndex],
                    IsFavorite(_tracks[_focusIndex].trackId),
                    SelectTrack,
                    FocusTrack);
                _rows[_focusIndex].SetSelected(true);
            }
        }

        private static bool IsFavorite(string trackId)
        {
            if (string.IsNullOrEmpty(trackId))
            {
                return false;
            }

            return PlayerPrefs.GetInt(FavoriteKeyPrefix + trackId, 0) == 1;
        }

        private void DuckMenuBgm()
        {
            menuBgm?.Duck(archiveDuckMultiplier);
        }

        private void RestoreMenuBgm()
        {
            menuBgm?.ApplyMasterVolume(MainMenuGameSettings.MasterVolume);
        }

        private void HandleArchiveInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null)
            {
                return;
            }

            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
            {
                MoveFocus(-1);
            }
            else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
            {
                MoveFocus(1);
            }
            else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                SelectTrack(_focusIndex);
            }
            else if (kb.spaceKey.wasPressedThisFrame)
            {
                OnPlayPauseClicked();
            }
            else if (kb.leftArrowKey.wasPressedThisFrame)
            {
                if (musicPlayer != null)
                {
                    musicPlayer.Time = Mathf.Max(0f, musicPlayer.Time - 5f);
                }
            }
            else if (kb.rightArrowKey.wasPressedThisFrame)
            {
                if (musicPlayer != null)
                {
                    musicPlayer.Time = Mathf.Min(musicPlayer.Duration, musicPlayer.Time + 5f);
                }
            }
#endif
        }

        private void MoveFocus(int delta)
        {
            if (_tracks.Count == 0)
            {
                return;
            }

            _focusIndex = (_focusIndex + delta + _tracks.Count) % _tracks.Count;
            RefreshRowSelection();
        }

        private static string FormatTime(float seconds)
        {
            if (seconds < 0f || float.IsNaN(seconds) || float.IsInfinity(seconds))
            {
                return "0:00";
            }

            var total = Mathf.FloorToInt(seconds);
            var m = total / 60;
            var s = total % 60;
            return $"{m}:{s:00}";
        }
    }

    public sealed class OffBeatSeekEvents : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler,
        UnityEngine.EventSystems.IPointerUpHandler
    {
        private System.Action _begin;
        private System.Action _end;

        public void Bind(System.Action begin, System.Action end)
        {
            _begin = begin;
            _end = end;
        }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            _begin?.Invoke();
        }

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
        {
            _end?.Invoke();
        }
    }
}

using FracturedChorus.Audio;
using FracturedChorus.Combat.Block;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Presentation;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using FracturedChorus.UI;

namespace FracturedChorus.UI
{
    public class BeatTimelineUIView : MonoBehaviour
    {
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform slotsRow;
        [Tooltip("LaneFootprint on Viewport — layout (anchor/pivot/Y/height) is scene SoT.")]
        [SerializeField] private RectTransform laneFootprint;
        [Tooltip("BossTrackFrame on Viewport — layout (anchor/pivot/Y/height) is scene SoT.")]
        [SerializeField] private RectTransform bossTrackFrame;
        [SerializeField] private BeatSegmentView segmentTemplate;
        [Tooltip("Sibling of Beat_0 under ScrollContent — placed after beat 22 of each phase.")]
        [SerializeField] private RectTransform phaseDividerTemplate;
        [SerializeField] private RectTransform scanBar;
        [SerializeField] private RectTransform trackLine;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Text phaseLabel;
        [SerializeField] private Text budgetLabel;
        [SerializeField] private Text avLabel;
        [SerializeField] private float slotWidth = TimelineLayoutLock.SlotWidth;
        [SerializeField] private float minSlotWidth = TimelineLayoutLock.MinSlotWidth;
        [SerializeField] private float laneMarkerSize = TimelineLayoutLock.LaneMarkerSize;
        [SerializeField] private float activeFootprintDotSize = TimelineLayoutLock.ActiveFootprintDotSize;
        [Tooltip("Footprint dot size (gray S1/S2 · colored active S) around the skill chip.")]
        [SerializeField] private float footprintDotSize = TimelineLayoutLock.FootprintDotSize;
        [SerializeField] private bool autoPlayOnStart;
        [SerializeField] private float autoBeatInterval = 0.405405f;
        [SerializeField] private bool useMusicSync = true;
        [Tooltip("Vị trí hit trong mỗi slot (0 = đầu nốt / downbeat, 0.5 = giữa, 1 = cuối). SFX + resolve dùng cùng anchor.")]
        [SerializeField] [Range(0f, 1f)] private float beatHitAnchorT = 0f;
        [SerializeField] private CombatMusicController musicController;
        private ICombatMusicSync _musicSync;
        private ICombatMusicSync ActiveMusic => _musicSync ?? musicController;
        [SerializeField] private CombatSfxController combatSfxController;
        [SerializeField] private CounterPresentationDriver counterPresentation;
        [SerializeField] private TimelineNoteVisualCatalog noteVisuals = new TimelineNoteVisualCatalog();
        [Tooltip("Boss note Y trong beat (0=đáy, 1=đỉnh). Legacy — rail dùng bossNoteRailAnchoredY.")]
        [SerializeField] [Range(0.55f, 0.92f)] private float noteBandNormalizedY = 0.78f;
        [Tooltip("Note rail / BorderTop anchored Y từ đáy Viewport (px).")]
        [SerializeField] private float bossNoteRailAnchoredY = 215f;
        [Tooltip("Fallback only when preserveSceneLayout=false — gap dưới note rail.")]
        [SerializeField] private float laneGapBelowRail = 32f;
        [Tooltip("Fallback only when preserveSceneLayout=false — LaneLines Top inset.")]
        [SerializeField] private float laneLinesTopInset = 15f;
        [Tooltip("Fallback only when preserveSceneLayout=false — LaneLines Bottom inset.")]
        [SerializeField] private float laneLinesBottomInset = -15f;
        [Header("Boss Note Number Layout (chỉnh tay vị trí số)")]
        [SerializeField] private BossNoteNumberLayout bossNoteNumberLayout = new BossNoteNumberLayout();

        public static bool SuppressBossNoteClusterRebuild { get; set; }

        public BossNoteNumberLayout BossNoteNumberLayout => bossNoteNumberLayout;

        /// <summary>
        /// Monster note rail Y (BorderTop) in viewport bottom-space pixels.
        /// Prefers authored BossTrackFrame on scene when preserveSceneLayout.
        /// </summary>
        public float BossNoteRailAnchoredY => ResolveNoteRailAnchoredY(
            viewport != null ? viewport.rect.height : 0f);

        public void RebuildBossNoteClustersPublic() => RebuildBossNoteClusters();

        /// <summary>
        /// Rebuild character lanes immediately to match current party-card formation order.
        /// </summary>
        public void SyncPartyLanesNow()
        {
            if (_session == null)
            {
                return;
            }

            BuildLanes();
            RefreshLaneMarkers();
        }
        [Tooltip("Fallback only when preserveSceneLayout=false — mép dưới band (normalized).")]
        [SerializeField] [Range(0.05f, 0.45f)] private float laneBandMinNormalizedY = 0.12f;
        [Tooltip("Fallback only when preserveSceneLayout=false — mép trên band (normalized).")]
        [SerializeField] [Range(0.25f, 0.6f)] private float laneBandMaxNormalizedY = 0.42f;
        [SerializeField] private Color bossTrackFrameBorderTop = new Color(0.45f, 0.98f, 1f, 0.95f);
        [Tooltip("Fallback BorderTop thickness when BossTrackFrame is not authored on scene.")]
        [SerializeField] private float bossTrackFrameBorderThickness = 2f;
        [SerializeField] private Sprite timelineStaffBackground;
        [SerializeField] [Range(0.15f, 1f)] private float timelineStaffBackgroundAlpha = 1f;
        [Header("Left Rail (Clef Column)")]
        [SerializeField] private Sprite leftRailBackground;
        [SerializeField] private Sprite trebleClefSprite;
        [SerializeField] private Sprite laneAvatarRingSprite;
        [SerializeField] private Sprite laneAvatarBossFrameSprite;
        [SerializeField] private Sprite avatarColumnBackground;
        [SerializeField] private Sprite phaseLabelSprite;
        [SerializeField] private Sprite avBudgetFrameSprite;
        [SerializeField] private Image leftRailBackgroundImage;
        [SerializeField] private Image trebleClefImage;
        [SerializeField] private Image phaseLabelImage;
        [SerializeField] private Image avBudgetFrameImage;
        [SerializeField] private Image avatarColumnBackgroundImage;
        [SerializeField] private RectTransform leftRailClefRoot;
        [SerializeField] private LeftRailLayout leftRailLayout = new LeftRailLayout();
        [Tooltip("Scene RectTransforms are source of truth. ScrollContent / LaneFootprint follow scroll X; LaneLines and BossTrackFrame stay pinned to the Viewport.")]
        [SerializeField] private bool preserveSceneLayout = true;

        [Header("Browse Chevrons (scene SoT — layout on RectTransform)")]
        [SerializeField] private Button browseLeftButton;
        [SerializeField] private Button browseRightButton;
        [Tooltip("Browse pan speed (px/sec). Authored on scene component.")]
        [SerializeField] private float browsePanSpeedPx = 900f;

        private BeatTimelineEngine _timeline;
        private CombatSession _session;
        private BeatSegmentView[] _slots;
        private RectTransform[] _phaseDividers;
        private float[] _slotWidths;
        private float[] _slotOffsetPx;
        private float _contentWidthPx;
        private float _pixelsPerSecond = 1f;
        private Coroutine _autoPlayRoutine;
        private bool _slotsBuilt;
        private bool _autoPlayCompleted;
        private float _lastViewportWidth;
        private int _autoPlayBeat;
        private Action _onPlanningPause;
        private Action _onRoundSegmentComplete;
        private float _scanSpeedMultiplier = 1f;
        private float _totalScrollPx;
        private float _localBeat;
        private int _lastFiredBeat = -1;
        private bool _isPlaybackActive;
        private bool _suppressTelegraphRefresh;
        private Coroutine _delaySlideRoutine;
        private bool _pausedForPlanning;
        private bool _pausedForEncounter;
        private int _lastHighlightedSlotIndex = -1;
        private int _lastCounterSfxBeat = -1;
        private float _lastScanLineContentPos = -1f;
        private readonly HashSet<int> _precomputedCounterBeats = new();
        private readonly List<CombatUnit> _counterUnitsScratch = new();
        private readonly List<CombatUnit> _counteredEnemyUnitsScratch = new();
        private RectTransform _resolveChipLayer;
        private readonly List<CounterNoteResolveChipView> _resolveChipPool = new();
        private readonly Queue<CounterNoteResolveChipView> _resolveChipActive = new();
        private CounterMultiBannerView _multiBanner;
        private const int ResolveChipPoolCap = 6;
        private const int MaxTimelinePartyLanes = DualGrid.MaxPlayerUnits;

        /// <summary>Lead so we never target a beat already mid-crossing; keep tiny for shortest Execute delay.</summary>
        private const float ResumeLeadBeats = 0.05f;

        private float _roundStartMusicalBeat;
        private int _roundSegmentIndex;
        private int _segmentStartBeat;
        private int _windowStartBeat;
        private int _introBeatCount;
        private Action _introCompleteCallback;

        [SerializeField] private RectTransform laneAvatarGutter;

        private RectTransform _laneLinesLayer;
        private RectTransform _laneMarkersLayer;
        private RectTransform _footprintLayer;
        private RectTransform _bossTrackFrame;
        private bool _bossTrackFrameAuthoredInScene;
        private bool _laneFootprintAuthoredInScene;
        private bool _scrollContentAuthoredInScene;
        private SceneRectLock _scrollContentLock;
        private SceneRectLock _laneFootprintLock;
        private SceneRectLock _bossTrackFrameLock;
        private bool _laneLinesLayerAuthoredInScene;
        /// <summary>Scene-authored vertical band (viewport bottom-space). Captured once before redistribute.</summary>
        private bool _sceneLaneBandCaptured;
        private float _sceneLaneBandMinY;
        private float _sceneLaneBandMaxY;
        /// <summary>Even-layout boss rail Y in viewport bottom-space (slot 0). &lt;0 = unset.</summary>
        private float _layoutBossRailY = -1f;
        private Image _staffBackground;
        private readonly List<CombatUnit> _laneUnits = new();
        private readonly Dictionary<CombatUnit, int> _laneIndex = new();
        /// <summary>Scene shell index (Lane_i / LaneAvatar_i) for each active entry in _laneUnits.</summary>
        private readonly List<int> _laneShellIndices = new();
        private readonly List<TimelineLaneAvatarSlotView> _laneAvatarSlots = new();
        private Action<CombatUnit> _onLaneAvatarClicked;
        private CombatUnit _selectedLaneUnit;

        private const string LeftRailResourceRoot = "UI/Combat/Timeline/LeftRail/";
        private const int PhaseChipFontSize = 22;
        private static readonly Color PhaseChipTextColor = new Color(0.918f, 0.984f, 1f, 1f);

        public LeftRailLayout LeftRailLayout => leftRailLayout;

        public void ApplyLeftRailPublic() => EnsureLeftRailVisuals();
        private readonly List<RectTransform> _laneLines = new();
        private readonly Dictionary<(CombatUnit unit, int beat), Image> _footprintDots = new();
        private readonly Dictionary<(CombatUnit unit, int beat), TimelineLaneMarkerView> _laneMarkers = new();
        private TimelineLaneMarkerView _dropGhost;
        private (CombatUnit unit, int beat)? _relocatePendingKey;
        private Func<CombatUnit, int, bool> _onBeginSkillRelocate;
        private Action<Vector2> _onSkillRelocateDrag;
        private Action<Vector2> _onEndSkillRelocate;
        private RectTransform _blockBarrierLayer;
        private readonly List<Image> _blockBarrierViews = new();
        private BlockBarrierTracker _blockBarriers;
        private RectTransform _bossNoteClusterLayer;
        private BossNoteClusterView _bossNoteClusters;
        private readonly List<Image> _dropPreviewDots = new();
        private readonly List<Image> _dropCoverOverlays = new();
        private readonly Dictionary<(CombatUnit unit, int beat), Color> _overlapTintSaved = new();
        private readonly List<TimelineLaneMarkerView> _overlapTintedMarkers = new();

        private static readonly Color StandingDotColor = new Color(0.5f, 0.5f, 0.55f, 0.4f);
        private static readonly Color DropOverlapTint = new Color(0xAF / 255f, 0x2C / 255f, 0x42 / 255f, 1f);

        // Intro-pause sau Deploy: snap cuối beat 0 vào ScanBar (anchor-based, không dùng localBeat threshold).
        private const float PhaseDividerVisualOffsetPx = 2f;
        private const float PhaseDividerWidthPx = 3f;
        private const float AnchorScrollEpsilonPx = 0.01f;
        private const float BrowsePanEpsilonPx = 0.5f;

        private float _browsePanPx;
        private float _playheadHoldScrollPx;
        private float _scanBarHomeAnchoredX;
        private bool _scanBarHomeCaptured;
        private int _browseHoldDir;
        private Coroutine _browsePanRoutine;
        private bool _browseInputBound;

        private static int TotalBeats => TimelineConstants.TotalBeats;
        private static int UiSlotCount => TimelineConstants.UiSlotCount;

        private int AbsoluteBeatFromSlot(int slotIndex) => _windowStartBeat + slotIndex;

        private int SlotIndexFromAbsolute(int absoluteBeat)
        {
            if (_slots == null)
            {
                return -1;
            }

            var slot = absoluteBeat - _windowStartBeat;
            return slot >= 0 && slot < _slots.Length ? slot : -1;
        }

        private BeatSegmentView TryGetSlotView(int absoluteBeat)
        {
            var slot = SlotIndexFromAbsolute(absoluteBeat);
            return slot >= 0 ? _slots[slot] : null;
        }

        private void Awake()
        {
            WireReferences();
        }

        private void Start()
        {
            RebuildLayout();
        }

        private void LateUpdate()
        {
            if (viewport == null || !_slotsBuilt)
            {
                return;
            }

            var w = viewport.rect.width;
            if (Mathf.Abs(w - _lastViewportWidth) > 0.5f)
            {
                RebuildLayout();
                ApplyScrollVisual(_totalScrollPx);
            }
        }

        public void WireReferences()
        {
            if (viewport == null)
            {
                viewport = transform.Find("Viewport") as RectTransform;
            }

            if (slotsRow == null)
            {
                slotsRow = transform.Find("Viewport/ScrollContent") as RectTransform
                    ?? FindTimelineRect("ScrollContent");
            }

            if (laneFootprint == null)
            {
                laneFootprint = FindTimelineRect("LaneFootprint");
            }

            if (bossTrackFrame == null)
            {
                bossTrackFrame = FindTimelineRect("BossTrackFrame");
            }

            if (laneFootprint != null)
            {
                _footprintLayer = laneFootprint;
            }

            if (bossTrackFrame != null)
            {
                _bossTrackFrame = bossTrackFrame;
            }

            CaptureScrollContentSceneRect();
            CaptureLaneFootprintSceneRect();
            CaptureBossTrackFrameSceneRect();

            if (segmentTemplate == null && slotsRow != null)
            {
                var beat0 = slotsRow.Find("Beat_0");
                if (beat0 != null)
                {
                    segmentTemplate = beat0.GetComponent<BeatSegmentView>();
                }
            }

            if (phaseDividerTemplate == null && slotsRow != null)
            {
                phaseDividerTemplate = slotsRow.Find("PhaseDivider") as RectTransform;
            }

            if (scanBar == null)
            {
                scanBar = transform.Find("Viewport/ScanBar") as RectTransform;
            }

            // ScanBar nằm trên LaneMarkers — tắt raycast để không chặn kéo skill trên lane.
            if (scanBar != null)
            {
                var scanImage = scanBar.GetComponent<Image>();
                if (scanImage != null)
                {
                    scanImage.raycastTarget = false;
                }
            }

            if (trackLine == null && viewport != null)
            {
                trackLine = viewport.Find("TrackLine") as RectTransform;
            }

            EnsureTrackLine();
            EnsureViewportMask();
            EnsureStaffBackground();

            try
            {
                EnsureLeftRailVisuals();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LeftRail] Bỏ qua visual cột trái: {ex.Message}");
            }

            if (confirmButton == null)
            {
                confirmButton = transform.Find("ConfirmButton")?.GetComponent<Button>();
            }

            WireBrowseChevronReferences();
            CaptureScanBarHomeFromScene();
            BindBrowseChevronInput();
            RefreshBrowseChevronVisibility();

            if (phaseLabel == null)
            {
                phaseLabel = transform.Find("Header/PhaseLabel")?.GetComponent<Text>()
                    ?? transform.Find("ConfirmButton/PhaseLabel")?.GetComponent<Text>();
            }

            EnsureClefGlyph();

            if (budgetLabel == null)
            {
                budgetLabel = transform.Find("Header/Budget/BudgetText")?.GetComponent<Text>();
            }

            if (avLabel == null)
            {
                avLabel = transform.Find("Header/AvLabel")?.GetComponent<Text>();
            }

            if (avLabel != null)
            {
                avLabel.gameObject.SetActive(false);
            }

            if (phaseLabel != null &&
                string.Equals(phaseLabel.text, "PHARSE", System.StringComparison.OrdinalIgnoreCase))
            {
                phaseLabel.text = "PHASE";
            }

            ConfigureAvLabelLayout();
            ExpandViewportWidth();

            if (ActiveMusic == null)
            {
                musicController = FindAnyObjectByType<CombatMusicController>();
                _musicSync = FindAnyObjectByType<RunCombatMusicBridge>();
            }

            EnsureCombatSfx();
            EnsureNoteVisuals();
        }

        /// <summary>
        /// Bind scene-authored browse buttons only. Never rewrite RectTransform layout
        /// when preserveSceneLayout — position/size live on the scene hierarchy.
        /// </summary>
        private void WireBrowseChevronReferences()
        {
            if (browseLeftButton == null)
            {
                browseLeftButton = transform.Find("BrowseLeftButton")?.GetComponent<Button>();
            }

            if (browseRightButton == null)
            {
                browseRightButton = transform.Find("BrowseRightButton")?.GetComponent<Button>();
            }
        }

        private void CaptureScanBarHomeFromScene()
        {
            if (scanBar == null || _browsePanPx > BrowsePanEpsilonPx)
            {
                return;
            }

            _scanBarHomeAnchoredX = scanBar.anchoredPosition.x;
            _scanBarHomeCaptured = true;
        }

        public TimelineNoteVisualCatalog NoteVisuals
        {
            get
            {
                EnsureNoteVisuals();
                return noteVisuals;
            }
        }

        /// <summary>
        /// Apply size/alpha from NoteSingle template so Play spawn matches Scene edit sizing.
        /// Does not change telegraph spawn rules.
        /// </summary>
        public void ApplyBossNoteTemplateSettings(Vector2 size, float alpha)
        {
            EnsureNoteVisuals();
            if (size.x > 1f)
            {
                noteVisuals.NoteDisplayWidth = size.x;
                noteVisuals.NoteDisplaySize = size.x;
            }

            if (size.y > 1f)
            {
                noteVisuals.NoteDisplayHeight = size.y;
            }

            if (alpha > 0.01f)
            {
                noteVisuals.NoteAlpha = Mathf.Clamp(alpha, 0.35f, 1f);
            }
        }

        private void EnsureNoteVisuals()
        {
            if (noteVisuals == null)
            {
                noteVisuals = new TimelineNoteVisualCatalog();
            }

            noteVisuals.EnsureDefaultsLoaded();
        }

        /// <summary>
        /// Legacy fallback when scene has no LeftRail/ClefIcon.
        /// If ClefIcon or trebleClefSprite exists, do not inject clef_g_v1.
        /// </summary>
        private void EnsureClefGlyph()
        {
            var clef = transform.Find("Header/Clef");
            if (clef == null)
            {
                return;
            }

            if (clef.Find("ClefIcon") != null || trebleClefImage != null || trebleClefSprite != null)
            {
                var rootImage = clef.GetComponent<Image>();
                if (rootImage != null)
                {
                    rootImage.enabled = false;
                    rootImage.sprite = null;
                }

                return;
            }

            var sprite = Resources.Load<Sprite>("UI/clef_g_v1");
            if (sprite == null)
            {
                return;
            }

            var image = clef.GetComponent<Image>();
            if (image == null)
            {
                var legacyText = clef.GetComponent<Text>();
                if (legacyText != null)
                {
                    legacyText.enabled = false;
                    legacyText.text = string.Empty;

                    var child = clef.Find("ClefSprite");
                    if (child == null)
                    {
                        var go = new GameObject("ClefSprite", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                        child = go.transform;
                        child.SetParent(clef, false);
                        var rt = (RectTransform)child;
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;
                    }

                    image = child.GetComponent<Image>();
                }
                else
                {
                    image = clef.gameObject.AddComponent<Image>();
                }
            }

            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            image.enabled = true;
        }

        private void ConfigureAvLabelLayout()
        {
            if (avLabel == null)
            {
                return;
            }

            avLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            avLabel.verticalOverflow = VerticalWrapMode.Truncate;
            avLabel.resizeTextForBestFit = false;
            avLabel.fontSize = Mathf.Max(avLabel.fontSize, 11);

            var avRect = avLabel.rectTransform;
            if (avRect != null && avRect.sizeDelta.x < 96f)
            {
                avRect.sizeDelta = new Vector2(96f, avRect.sizeDelta.y);
            }
        }

        private void ExpandViewportWidth()
        {
            if (preserveSceneLayout || viewport == null)
            {
                return;
            }

            viewport.offsetMax = new Vector2(-8f, viewport.offsetMax.y);
        }

        private void EnsureViewportMask()
        {
            if (viewport == null)
            {
                return;
            }

            if (viewport.GetComponent<RectMask2D>() == null && viewport.GetComponent<Mask>() == null)
            {
                viewport.gameObject.AddComponent<RectMask2D>();
            }

            var viewportImage = viewport.GetComponent<Image>();
            if (viewportImage != null)
            {
                viewportImage.raycastTarget = false;
            }
        }

        private void EnsureStaffBackground()
        {
            if (viewport == null)
            {
                return;
            }

            if (timelineStaffBackground == null)
            {
                timelineStaffBackground = Resources.Load<Sprite>("UI/Combat/Timeline/timeline_staff_holo_bg_v1");
            }

            if (_staffBackground == null)
            {
                var existing = viewport.Find("StaffBackground")?.GetComponent<Image>();
                if (existing != null)
                {
                    _staffBackground = existing;
                }
                else
                {
                    var go = new GameObject("StaffBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    var rect = go.GetComponent<RectTransform>();
                    rect.SetParent(viewport, false);
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    _staffBackground = go.GetComponent<Image>();
                    _staffBackground.raycastTarget = false;
                    _staffBackground.type = Image.Type.Simple;
                    _staffBackground.preserveAspect = false;
                }
            }

            _staffBackground.transform.SetAsFirstSibling();
            if (timelineStaffBackground != null)
            {
                _staffBackground.sprite = timelineStaffBackground;
                _staffBackground.color = new Color(1f, 1f, 1f, timelineStaffBackgroundAlpha);
                _staffBackground.enabled = true;
            }
            else
            {
                _staffBackground.enabled = false;
            }

            OrderViewportLayers();
        }

        private void EnsureLeftRailVisuals()
        {
            leftRailLayout ??= new LeftRailLayout();
            LoadLeftRailSpritesIfNeeded();

            var header = transform.Find("Header") as RectTransform;
            if (header == null)
            {
                return;
            }

            if (leftRailBackgroundImage == null)
            {
                leftRailBackgroundImage = header.Find("LeftRailBackground")?.GetComponent<Image>();
            }

            if (leftRailBackgroundImage == null)
            {
                var go = new GameObject("LeftRailBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(header, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                leftRailBackgroundImage = go.GetComponent<Image>();
                leftRailBackgroundImage.raycastTarget = false;
                leftRailBackgroundImage.type = Image.Type.Simple;
                leftRailBackgroundImage.preserveAspect = false;
            }

            leftRailBackgroundImage.transform.SetAsFirstSibling();
            if (leftRailBackground != null)
            {
                leftRailBackgroundImage.sprite = leftRailBackground;
                leftRailBackgroundImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(leftRailLayout.backgroundAlpha));
                leftRailBackgroundImage.enabled = true;
            }
            else
            {
                leftRailBackgroundImage.enabled = false;
            }

            if (leftRailClefRoot == null)
            {
                leftRailClefRoot = header.Find("Clef") as RectTransform;
            }

            if (leftRailClefRoot == null)
            {
                var clefGo = new GameObject("Clef", typeof(RectTransform));
                leftRailClefRoot = clefGo.GetComponent<RectTransform>();
                leftRailClefRoot.SetParent(header, false);
                leftRailClefRoot.anchorMin = new Vector2(0f, 0.5f);
                leftRailClefRoot.anchorMax = new Vector2(0f, 0.5f);
                leftRailClefRoot.pivot = new Vector2(0.5f, 0.5f);
            }

            if (!leftRailLayout.preserveSceneRects)
            {
                leftRailClefRoot.sizeDelta = leftRailLayout.clefSize;
                leftRailClefRoot.anchoredPosition = leftRailLayout.clefAnchoredPosition;
            }

            if (trebleClefImage == null)
            {
                trebleClefImage = leftRailClefRoot.Find("ClefIcon")?.GetComponent<Image>();
            }

            if (trebleClefImage == null)
            {
                trebleClefImage = GetOrCreateChildImage(leftRailClefRoot, "ClefIcon");
            }

            if (trebleClefImage == null)
            {
                return;
            }

            var rootClefImage = leftRailClefRoot.GetComponent<Image>();
            if (rootClefImage != null && rootClefImage != trebleClefImage)
            {
                rootClefImage.enabled = false;
                rootClefImage.sprite = null;
            }

            trebleClefImage.preserveAspect = true;
            if (trebleClefSprite != null)
            {
                trebleClefImage.sprite = trebleClefSprite;
                trebleClefImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(leftRailLayout.clefAlpha));
                trebleClefImage.enabled = true;
            }
            else
            {
                trebleClefImage.enabled = false;
            }

            var clefText = leftRailClefRoot.GetComponent<Text>();
            if (clefText != null)
            {
                clefText.enabled = trebleClefSprite == null;
            }

            ApplyPhaseAndBudgetArt(header);
            EnsureAvatarColumnShell();
        }

        private void EnsureAvatarColumnShell()
        {
            EnsureAvatarColumnRoot();
            if (laneAvatarGutter == null)
            {
                return;
            }

            leftRailLayout ??= new LeftRailLayout();

            // Hierarchy-first: keep LaneAvatarGutter rect authored on Scene.
            var keepScene = preserveSceneLayout || leftRailLayout.preserveSceneRects;
            if (leftRailLayout.forceAvatarLayout && !keepScene)
            {
                LayoutLaneAvatarGutterFlushToViewport();
            }
            else if (!keepScene)
            {
                var gutterW = Mathf.Max(24f, leftRailLayout.avatarGutterWidth);
                laneAvatarGutter.anchorMin = new Vector2(0f, 0f);
                laneAvatarGutter.anchorMax = new Vector2(0f, 1f);
                laneAvatarGutter.pivot = new Vector2(0f, 0.5f);
                laneAvatarGutter.sizeDelta = new Vector2(gutterW, 0f);
                laneAvatarGutter.anchoredPosition = new Vector2(ResolveAvatarGutterOffsetX(), 0f);
            }

            ApplyAvatarColumnBackground();
            if (leftRailLayout.forceAvatarLayout && !keepScene)
            {
                laneAvatarGutter.SetAsLastSibling();
            }
        }

        private float ResolveAvatarGutterOffsetX()
        {
            leftRailLayout ??= new LeftRailLayout();
            var gutterW = Mathf.Max(24f, leftRailLayout.avatarGutterWidth);

            if (!leftRailLayout.forceAvatarLayout)
            {
                return leftRailLayout.avatarGutterOffsetX;
            }

            var viewportLeft = leftRailLayout.avatarGutterOffsetX + gutterW;
            if (viewport != null)
            {
                var worldLeft = viewport.TransformPoint(new Vector3(viewport.rect.xMin, 0f, 0f));
                viewportLeft = transform.InverseTransformPoint(worldLeft).x;
            }
            else
            {
                var header = transform.Find("Header") as RectTransform;
                if (header != null)
                {
                    viewportLeft = header.rect.xMax;
                }
            }

            return Mathf.Max(0f, viewportLeft - gutterW);
        }

        private void ApplyPhaseAndBudgetArt(RectTransform header)
        {
            if (header == null)
            {
                return;
            }

            if (phaseLabel == null)
            {
                phaseLabel = header.Find("PhaseLabel")?.GetComponent<Text>();
            }

            if (phaseLabel != null)
            {
                phaseLabel.gameObject.SetActive(true);
                phaseLabel.enabled = phaseLabelSprite == null;
                if (phaseLabelSprite != null)
                {
                    phaseLabel.text = string.Empty;
                }
                else if (string.IsNullOrEmpty(phaseLabel.text) ||
                         string.Equals(phaseLabel.text, "PHARSE", StringComparison.OrdinalIgnoreCase))
                {
                    phaseLabel.text = "PHASE";
                }

                if (phaseLabelImage == null)
                {
                    phaseLabelImage = phaseLabel.transform.Find("PhaseArt")?.GetComponent<Image>();
                }

                if (phaseLabelImage == null && phaseLabelSprite != null)
                {
                    phaseLabelImage = GetOrCreateChildImage(phaseLabel.rectTransform, "PhaseArt");
                }

                if (phaseLabelImage != null)
                {
                    phaseLabelImage.preserveAspect = true;
                    if (phaseLabelSprite != null)
                    {
                        phaseLabelImage.sprite = phaseLabelSprite;
                        phaseLabelImage.color = Color.white;
                        phaseLabelImage.enabled = true;
                    }
                    else
                    {
                        phaseLabelImage.enabled = false;
                    }
                }

                phaseLabel.transform.SetAsLastSibling();
            }

            var budgetRt = header.Find("Budget") as RectTransform;
            if (budgetRt != null)
            {
                budgetRt.gameObject.SetActive(true);
                if (avBudgetFrameImage == null)
                {
                    avBudgetFrameImage = budgetRt.GetComponent<Image>();
                }

                if (avBudgetFrameImage != null && avBudgetFrameSprite != null)
                {
                    avBudgetFrameImage.sprite = avBudgetFrameSprite;
                    avBudgetFrameImage.type = Image.Type.Simple;
                    avBudgetFrameImage.preserveAspect = true;
                    avBudgetFrameImage.color = Color.white;
                    avBudgetFrameImage.enabled = true;
                }

                if (budgetLabel == null)
                {
                    budgetLabel = budgetRt.Find("BudgetText")?.GetComponent<Text>();
                }

                if (budgetLabel != null)
                {
                    budgetLabel.enabled = true;
                    budgetLabel.color = PhaseChipTextColor;
                    budgetLabel.fontStyle = FontStyle.Bold;
                    budgetLabel.fontSize = PhaseChipFontSize;
                    budgetLabel.transform.SetAsLastSibling();
                }

                budgetRt.SetAsLastSibling();
            }
        }

        private void LoadLeftRailSpritesIfNeeded()
        {
            if (leftRailBackground == null)
            {
                leftRailBackground = Resources.Load<Sprite>(LeftRailResourceRoot + "left_rail_bg_v1");
            }

            if (trebleClefSprite == null)
            {
                trebleClefSprite = Resources.Load<Sprite>(LeftRailResourceRoot + "treble_clef_v4");
            }

            if (laneAvatarRingSprite == null)
            {
                laneAvatarRingSprite = Resources.Load<Sprite>(LeftRailResourceRoot + "lane_avatar_frame_pc_v1");
            }

            if (laneAvatarRingSprite == null)
            {
                laneAvatarRingSprite = Resources.Load<Sprite>(LeftRailResourceRoot + "lane_avatar_ring_v1");
            }

            if (laneAvatarBossFrameSprite == null)
            {
                laneAvatarBossFrameSprite = Resources.Load<Sprite>(LeftRailResourceRoot + "lane_avatar_frame_boss_v1");
            }

            if (avatarColumnBackground == null)
            {
                avatarColumnBackground = Resources.Load<Sprite>(LeftRailResourceRoot + "avatar_column_bg_v1");
            }

            if (phaseLabelSprite == null)
            {
                phaseLabelSprite = Resources.Load<Sprite>(LeftRailResourceRoot + "phase_label_v4");
            }

            if (avBudgetFrameSprite == null)
            {
                avBudgetFrameSprite = Resources.Load<Sprite>(LeftRailResourceRoot + "phase_chip_v3");
            }
        }

        private static Image GetOrCreateChildImage(RectTransform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            var existing = parent.Find(childName)?.GetComponent<Image>();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.type = Image.Type.Simple;
            return image;
        }

        public void Bind(BeatTimelineEngine timeline, CombatSession session,
            ICombatMusicSync music = null, Action onPlanningPause = null, Action onRoundSegmentComplete = null,
            CombatSfxController combatSfx = null, CounterPresentationDriver presentation = null)
        {
            if (music != null)
            {
                _musicSync = music;
                musicController = music as CombatMusicController;
            }

            if (combatSfx != null)
            {
                combatSfxController = combatSfx;
            }

            if (presentation != null)
            {
                counterPresentation = presentation;
            }

            if (_timeline != null)
            {
                _timeline.OnTelegraphsChanged -= HandleTelegraphsChanged;
                _timeline.OnTelegraphMoved -= HandleTelegraphMoved;
                _timeline.OnTelegraphsDelayedBatch -= HandleTelegraphsDelayedBatch;
            }

            if (_session != null)
            {
                _session.OnScanBeat -= HandleScanBeat;
                _session.OnTelegraphsPlanned -= HandleTelegraphsPlanned;
                _session.OnEncounterEnded -= HandleEncounterEnded;
                _session.OnBlockResolved -= HandleBlockResolved;
            }

            _timeline = timeline;
            _session = session;
            _onPlanningPause = onPlanningPause;
            _onRoundSegmentComplete = onRoundSegmentComplete;
            if (_timeline != null)
            {
                _timeline.PlanningHorizonBeat = 0;
                _timeline.OnTelegraphsChanged -= HandleTelegraphsChanged;
                _timeline.OnTelegraphsChanged += HandleTelegraphsChanged;
                _timeline.OnTelegraphMoved -= HandleTelegraphMoved;
                _timeline.OnTelegraphMoved += HandleTelegraphMoved;
                _timeline.OnTelegraphsDelayedBatch -= HandleTelegraphsDelayedBatch;
                _timeline.OnTelegraphsDelayedBatch += HandleTelegraphsDelayedBatch;
                _timeline.OnAgendaChanged -= HandleAgendaChanged;
                _timeline.OnAgendaChanged += HandleAgendaChanged;
            }

            WireReferences();
            RebuildLayout();

            if (_session != null)
            {
                _session.OnScanBeat += HandleScanBeat;
                _session.OnTelegraphsPlanned += HandleTelegraphsPlanned;
                _session.OnEncounterEnded += HandleEncounterEnded;
                _session.OnBlockResolved += HandleBlockResolved;
            }

            BuildLanes();
            PopulateAllSlots();
            RefreshPhaseHeader(0);
            RefreshPhaseAvLabel();
            counterPresentation?.ResetPresentation();
        }

        private void HandleTelegraphsChanged()
        {
            if (_suppressTelegraphRefresh)
            {
                return;
            }

            RefreshTelegraphsAndSlots();
        }

        private void HandleAgendaChanged()
        {
            if (_relocatePendingKey.HasValue)
            {
                return;
            }

            RefreshAll();
        }

        private void HandleTelegraphMoved(int fromBeat, int toBeat, int delayBeats)
        {
        }

        private void HandleTelegraphsDelayedBatch(IReadOnlyList<TelegraphBeatMove> moves)
        {
            if (moves == null || moves.Count == 0)
            {
                return;
            }

            if (_delaySlideRoutine != null)
            {
                StopCoroutine(_delaySlideRoutine);
            }

            _suppressTelegraphRefresh = true;
            _delaySlideRoutine = StartCoroutine(NoteDelaySlideRoutine(moves));
        }

        public void ShowNoteDelayFeedback(int fromBeat, int toBeat, int delayBeats)
        {
            HandleTelegraphsDelayedBatch(new[]
            {
                new TelegraphBeatMove(null, fromBeat, toBeat)
            });
        }

        private IEnumerator NoteDelaySlideRoutine(IReadOnlyList<TelegraphBeatMove> moves)
        {
            EnsureResolveFeedbackUi();
            var overlays = new List<(RectTransform rt, Image img, Vector2 from, Vector2 to)>();
            if (_resolveChipLayer != null && _slots != null && _slotOffsetPx != null)
            {
                foreach (var move in moves)
                {
                    if (move.FromBeat < 0 || move.ToBeat < 0
                        || move.FromBeat >= TotalBeats || move.ToBeat >= TotalBeats)
                    {
                        continue;
                    }

                    var fromPos = GetBeatNoteLocalPos(move.FromBeat);
                    var toPos = GetBeatNoteLocalPos(move.ToBeat);
                    var go = new GameObject("DelaySlideNote", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    var rt = go.GetComponent<RectTransform>();
                    rt.SetParent(_resolveChipLayer, false);
                    var noteSize = noteVisuals != null ? noteVisuals.NoteDisplaySize : 40f;
                    rt.sizeDelta = new Vector2(noteSize, noteSize);
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = fromPos;

                    var img = go.GetComponent<Image>();
                    img.raycastTarget = false;
                    var tier = move.Telegraph != null ? move.Telegraph.NoteTier : BossNoteTier.Red;
                    var sprite = noteVisuals != null ? noteVisuals.NoteForTier(tier) : null;
                    if (sprite != null)
                    {
                        img.sprite = sprite;
                        img.color = Color.white;
                        img.preserveAspect = true;
                    }
                    else
                    {
                        img.sprite = UiCircleSpriteUtil.Circle;
                        img.color = new Color(0.45f, 0.95f, 1f, 0.9f);
                    }

                    var badgeGo = new GameObject("DelayBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                    var badgeRt = badgeGo.GetComponent<RectTransform>();
                    badgeRt.SetParent(rt, false);
                    badgeRt.anchorMin = new Vector2(0.5f, 1f);
                    badgeRt.anchorMax = new Vector2(0.5f, 1f);
                    badgeRt.pivot = new Vector2(0.5f, 0f);
                    badgeRt.anchoredPosition = new Vector2(0f, 2f);
                    badgeRt.sizeDelta = new Vector2(40f, 18f);
                    var badge = badgeGo.GetComponent<Text>();
                    badge.font = UiFontCatalog.Body;
                    badge.fontSize = 14;
                    badge.alignment = TextAnchor.MiddleCenter;
                    badge.color = new Color(0.45f, 0.95f, 1f, 1f);
                    badge.raycastTarget = false;
                    badge.text = $"+{Mathf.Max(1, move.ToBeat - move.FromBeat)}";

                    overlays.Add((rt, img, fromPos, toPos));
                }
            }

            // Hide destination slots' note portraits during slide to avoid double-draw / pop.
            if (_slots != null)
            {
                foreach (var move in moves)
                {
                    TryGetSlotView(move.ToBeat)?.ClearEnemyVisualOnly();
                    TryGetSlotView(move.FromBeat)?.ClearEnemyVisualOnly();
                }
            }

            const float duration = 0.35f;
            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                foreach (var o in overlays)
                {
                    if (o.rt != null)
                    {
                        o.rt.anchoredPosition = Vector2.Lerp(o.from, o.to, u);
                    }
                }

                yield return null;
            }

            foreach (var o in overlays)
            {
                if (o.rt != null)
                {
                    Destroy(o.rt.gameObject);
                }
            }

            _suppressTelegraphRefresh = false;
            _delaySlideRoutine = null;
            RefreshTelegraphsAndSlots();
        }

        private Vector2 GetBeatNoteLocalPos(int beatIndex)
        {
            if (_resolveChipLayer == null)
            {
                return Vector2.zero;
            }

            var slot = TryGetSlotView(beatIndex);
            if (slot == null)
            {
                // Out-of-window beats still need a stable X for delay slides / chips.
                var fallbackX = ContentXForBeat(Mathf.Clamp(beatIndex, 0, TotalBeats - 1));
                var bandY = (viewport != null ? viewport.rect.height : 200f) * noteBandNormalizedY;
                return new Vector2(fallbackX + (slotsRow != null ? slotsRow.anchoredPosition.x : 0f), bandY);
            }

            var slotRt = slot.transform as RectTransform;
            if (slotRt == null)
            {
                return Vector2.zero;
            }

            var world = slotRt.TransformPoint(new Vector3(slotRt.rect.center.x, slotRt.rect.yMin + slotRt.rect.height * noteBandNormalizedY, 0f));
            return _resolveChipLayer.InverseTransformPoint(world);
        }

        private float GetBeatCenterLocalX(int beatIndex)
        {
            return GetBeatNoteLocalPos(beatIndex).x;
        }

        private float GetNoteBandLocalY()
        {
            return GetBeatNoteLocalPos(Mathf.Clamp(_autoPlayBeat, 0, TotalBeats - 1)).y;
        }

        private IEnumerator NoteDelayFeedbackRoutine(int fromBeat, int toBeat, int delayBeats)
        {
            yield break;
        }

        public void SetCounterPresentation(CounterPresentationDriver presentation)
        {
            counterPresentation = presentation;
        }

        public void SpawnNoteResolveChip(int beatIndex, BossNoteTier tier, int hitsDelta)
        {
            EnsureResolveFeedbackUi();
            if (_resolveChipLayer == null)
            {
                return;
            }

            BringResolveFeedbackToFront();

            while (_resolveChipActive.Count >= ResolveChipPoolCap)
            {
                var oldest = _resolveChipActive.Dequeue();
                oldest?.ForceHide();
            }

            var chip = RentResolveChip();
            chip.transform.SetParent(_resolveChipLayer, false);
            chip.transform.SetAsLastSibling();

            var stack = _resolveChipActive.Count;
            var pos = GetPerfectChipAnchoredPos(stack);
            chip.Play(pos, tier);
            _resolveChipActive.Enqueue(chip);
        }

        public void ShowOrRefreshMultiBanner(int count)
        {
            EnsureResolveFeedbackUi();
            BringResolveFeedbackToFront();
            _multiBanner?.ShowOrRefresh(count);
        }

        public void HideMultiBanner()
        {
            _multiBanner?.HideImmediate();
            while (_resolveChipActive.Count > 0)
            {
                _resolveChipActive.Dequeue()?.ForceHide();
            }
        }

        private Vector2 GetPerfectChipAnchoredPos(int stackIndex)
        {
            var offset = new Vector2(stackIndex * 14f - 8f, 56f + stackIndex * 8f);
            if (_resolveChipLayer == null || scanBar == null)
            {
                return offset;
            }

            var scanLocal = (Vector2)_resolveChipLayer.InverseTransformPoint(scanBar.position);
            var halfW = CounterNoteResolveChipView.DisplaySize.x * 0.5f;
            var minCenterX = GetHeaderRightEdgeInResolveLayer() + halfW + 12f;
            scanLocal.x = Mathf.Max(scanLocal.x, minCenterX);
            return scanLocal + offset;
        }

        private float GetHeaderRightEdgeInResolveLayer()
        {
            if (_resolveChipLayer == null)
            {
                return 0f;
            }

            var header = transform.Find("Header") as RectTransform;
            if (header == null)
            {
                return 0f;
            }

            var worldRight = header.TransformPoint(new Vector3(header.rect.xMax, 0f, 0f));
            return _resolveChipLayer.InverseTransformPoint(worldRight).x;
        }

        private const int ResolveOverlaySortOrder = 400;

        private void BringResolveFeedbackToFront()
        {
            if (_resolveChipLayer == null)
            {
                return;
            }

            ConfigureResolveOverlayCanvas();

            var root = GetResolveOverlayRoot();
            if (root != null && _resolveChipLayer.parent != root)
            {
                _resolveChipLayer.SetParent(root, false);
                StretchFullRect(_resolveChipLayer);
            }

            _resolveChipLayer.SetAsLastSibling();

            if (_multiBanner != null && _multiBanner.transform.parent == _resolveChipLayer)
            {
                _multiBanner.transform.SetAsLastSibling();
            }
        }

        private RectTransform GetResolveOverlayRoot()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return transform as RectTransform;
            }

            var root = canvas.rootCanvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            return root != null ? root : canvas.transform as RectTransform;
        }

        private void ConfigureResolveOverlayCanvas()
        {
            if (_resolveChipLayer == null)
            {
                return;
            }

            var overlay = _resolveChipLayer.GetComponent<Canvas>();
            if (overlay == null)
            {
                overlay = _resolveChipLayer.gameObject.AddComponent<Canvas>();
            }

            overlay.overrideSorting = true;
            overlay.sortingOrder = ResolveOverlaySortOrder;
            overlay.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
        }

        private static void StretchFullRect(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private void EnsureResolveFeedbackUi()
        {
            if (viewport == null)
            {
                WireReferences();
            }

            var host = transform as RectTransform;
            if (host == null)
            {
                return;
            }

            var root = GetResolveOverlayRoot();

            if (_resolveChipLayer == null)
            {
                var existing = host.Find("ResolveChipLayer") as RectTransform
                    ?? (viewport != null ? viewport.Find("ResolveChipLayer") as RectTransform : null)
                    ?? (root != null ? root.Find("ResolveChipLayer") as RectTransform : null);
                if (existing != null)
                {
                    _resolveChipLayer = existing;
                }
                else
                {
                    var go = new GameObject("ResolveChipLayer", typeof(RectTransform));
                    _resolveChipLayer = go.GetComponent<RectTransform>();
                }
            }

            var parent = root != null ? root : host;
            if (_resolveChipLayer.parent != parent)
            {
                _resolveChipLayer.SetParent(parent, false);
            }

            StretchFullRect(_resolveChipLayer);
            BringResolveFeedbackToFront();

            if (_multiBanner == null)
            {
                var existingBanner = _resolveChipLayer.Find("MultiBanner");
                if (existingBanner == null && viewport != null)
                {
                    existingBanner = viewport.Find("MultiBanner");
                }

                if (existingBanner != null)
                {
                    _multiBanner = existingBanner.GetComponent<CounterMultiBannerView>();
                    if (_multiBanner != null && existingBanner.parent != _resolveChipLayer)
                    {
                        existingBanner.SetParent(_resolveChipLayer, false);
                    }
                }

                if (_multiBanner == null)
                {
                    _multiBanner = CounterMultiBannerView.Create(_resolveChipLayer);
                    var bannerRect = _multiBanner.transform as RectTransform;
                    if (bannerRect != null && scanBar != null)
                    {
                        var scanLocal = (Vector2)_resolveChipLayer.InverseTransformPoint(scanBar.position);
                        var halfW = CounterNoteResolveChipView.DisplaySize.x * 0.5f;
                        var minCenterX = GetHeaderRightEdgeInResolveLayer() + halfW + 12f;
                        scanLocal.x = Mathf.Max(scanLocal.x, minCenterX);
                        bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
                        bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
                        bannerRect.pivot = new Vector2(0.5f, 0f);
                        bannerRect.anchoredPosition = scanLocal + new Vector2(0f, 72f);
                    }
                }
            }
        }

        private CounterNoteResolveChipView RentResolveChip()
        {
            var parent = _resolveChipLayer != null ? _resolveChipLayer : viewport;
            foreach (var chip in _resolveChipPool)
            {
                if (chip != null && !chip.gameObject.activeSelf)
                {
                    return chip;
                }
            }

            var created = CounterNoteResolveChipView.Create(parent);
            _resolveChipPool.Add(created);
            return created;
        }

        public void SetSkillRelocateHandlers(
            Func<CombatUnit, int, bool> onBeginRelocate,
            Action<Vector2> onRelocateDrag,
            Action<Vector2> onEndRelocate)
        {
            _onBeginSkillRelocate = onBeginRelocate;
            _onSkillRelocateDrag = onRelocateDrag;
            _onEndSkillRelocate = onEndRelocate;
            RefreshLaneMarkerDragWiring();
        }

        /// <summary>Kéo lại skill trên lane — cùng cửa sổ planning với gán skill và dời unit.</summary>
        public bool CanRelocateLaneMarker()
        {
            return _session != null && _session.IsPlanningWindowOpen;
        }

        public bool TryBeginLaneMarkerRelocate(CombatUnit unit, int beatIndex)
        {
            return _onBeginSkillRelocate != null && _onBeginSkillRelocate.Invoke(unit, beatIndex);
        }

        public void UpdateLaneMarkerRelocate(Vector2 screenPos)
        {
            _onSkillRelocateDrag?.Invoke(screenPos);
        }

        public void EndLaneMarkerRelocate(Vector2 screenPos)
        {
            _onEndSkillRelocate?.Invoke(screenPos);
        }

        public void PrepareLaneMarkerRelocate(CombatUnit unit, int beatIndex)
        {
            _relocatePendingKey = (unit, beatIndex);
            if (_laneMarkers.TryGetValue((unit, beatIndex), out var marker) && marker != null)
            {
                marker.SetRelocateVisualHidden(true);
            }
        }

        public void SoftHideFootprintsForRelocate(CombatUnit unit, SkillDefinitionSO skill, int placementBeat)
        {
            if (unit == null || skill == null)
            {
                return;
            }

            foreach (var info in SkillFootprintUtil.EnumerateFootprintBeats(skill, placementBeat, unit))
            {
                if (!_footprintDots.TryGetValue((unit, info.BeatIndex), out var dot) || dot == null)
                {
                    continue;
                }

                var c = dot.color;
                dot.color = new Color(c.r, c.g, c.b, 0f);
                dot.raycastTarget = false;
            }
        }

        public void ClearLaneMarkerRelocatePrepare()
        {
            if (_relocatePendingKey.HasValue
                && _laneMarkers.TryGetValue(_relocatePendingKey.Value, out var marker)
                && marker != null)
            {
                marker.SetRelocateVisualHidden(false);
            }

            _relocatePendingKey = null;
            RefreshLaneMarkerDragWiring();
        }

        public bool IsScreenPointInViewport(Vector2 screen)
        {
            if (viewport == null)
            {
                return false;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(viewport, screen, GetUiCameraForTimeline());
        }

        public void SetLaneAvatarClickHandler(Action<CombatUnit> handler)
        {
            _onLaneAvatarClicked = handler;
        }

        public void SetSelectedLaneUnit(CombatUnit unit)
        {
            _selectedLaneUnit = unit;
            foreach (var slot in _laneAvatarSlots)
            {
                if (slot != null)
                {
                    slot.SetSelected(slot.Unit == unit);
                }
            }
        }

        public void BindBlockBarriers(BlockBarrierTracker barriers)
        {
            if (_blockBarriers != null)
            {
                _blockBarriers.OnBarriersChanged -= RefreshBlockBarriers;
            }

            _blockBarriers = barriers;
            if (_blockBarriers != null)
            {
                _blockBarriers.OnBarriersChanged += RefreshBlockBarriers;
                RefreshBlockBarriers();
            }
        }

        public void BeginRoundPlayback(bool continueFromHold = false)
        {
            if (!continueFromHold)
            {
                _session?.BlockBarriers.Clear();
            }

            if (_isPlaybackActive)
            {
                return;
            }

            ResetBrowsePanToPlayhead();

            _introBeatCount = 0;
            _introCompleteCallback = null;
            _autoPlayCompleted = false;
            _pausedForPlanning = false;
            _pausedForEncounter = false;
            ResetCounterSfxState();
            counterPresentation?.ResetPresentation();

            if (_autoPlayRoutine != null)
            {
                StopCoroutine(_autoPlayRoutine);
            }

            _autoPlayRoutine = CanUseMusicSync()
                ? StartCoroutine(MusicDrivenScanRoutine(false, continueFromHold))
                : StartCoroutine(ContinuousScanRoutine(false, continueFromHold));
        }

        /// <summary>
        /// Fight-start intro: scan advances with music for intro beats, then holds for Planning.
        /// </summary>
        public void BeginIntroPlayback(float durationSec, Action onComplete)
        {
            var introBeats = CombatTimelineProfile.CombatIntroBeatCount;
            if (introBeats <= 0 && durationSec <= 0f)
            {
                onComplete?.Invoke();
                return;
            }

            if (_isPlaybackActive)
            {
                return;
            }

            _introBeatCount = introBeats > 0
                ? introBeats
                : Mathf.Max(1, Mathf.RoundToInt(durationSec / Mathf.Max(0.05f, GetBeatWaitDuration())));
            _introCompleteCallback = onComplete;
            _autoPlayCompleted = false;
            _pausedForPlanning = false;
            ResetCounterSfxState();
            counterPresentation?.ResetPresentation();

            if (_autoPlayRoutine != null)
            {
                StopCoroutine(_autoPlayRoutine);
            }

            _autoPlayRoutine = CanUseMusicSync()
                ? StartCoroutine(IntroMusicDrivenScanRoutine())
                : StartCoroutine(IntroContinuousScanRoutine());
        }

        private IEnumerator IntroMusicDrivenScanRoutine()
        {
            if (ActiveMusic == null || !ActiveMusic.IsPlaying)
            {
                yield return IntroContinuousScanRoutine();
                yield break;
            }

            _isPlaybackActive = true;
            PrepareSegmentScanStart(useMusicSync: true, continueFromHold: false);
            _roundStartMusicalBeat = ActiveMusic.TotalMusicalBeat;
            _localBeat = 0f;
            EnsureTrackLine();

            while (_isPlaybackActive)
            {
                _localBeat = Mathf.Max(0f, ActiveMusic.TotalMusicalBeat - _roundStartMusicalBeat);
                if (_localBeat >= _introBeatCount)
                {
                    EnterIntroPlanningHold();
                    yield break;
                }

                _totalScrollPx = ScrollPxForContentAnchor(PxOfAbsoluteBeat(GetAbsolutePlaybackBeat()));
                ApplyScrollVisual(_totalScrollPx);

                if (_session != null && _session.IsEncounterOver)
                {
                    break;
                }

                yield return null;
            }

            _autoPlayRoutine = null;
        }

        private IEnumerator IntroContinuousScanRoutine()
        {
            _isPlaybackActive = true;
            PrepareSegmentScanStart(useMusicSync: false, continueFromHold: false);
            _localBeat = 0f;
            EnsureTrackLine();

            while (_isPlaybackActive)
            {
                _localBeat += Time.deltaTime / GetBeatWaitDuration();
                if (_localBeat >= _introBeatCount)
                {
                    EnterIntroPlanningHold();
                    yield break;
                }

                _totalScrollPx = ScrollPxForContentAnchor(PxOfAbsoluteBeat(GetAbsolutePlaybackBeat()));
                ApplyScrollVisual(_totalScrollPx);

                if (_session != null && _session.IsEncounterOver)
                {
                    break;
                }

                yield return null;
            }

            _autoPlayRoutine = null;
        }

        /// <summary>
        /// Intro is visual-only: snap back to segment start so the first phase still runs a full phase.
        /// </summary>
        private void EnterIntroPlanningHold()
        {
            _isPlaybackActive = false;
            _pausedForPlanning = false;
            if (_autoPlayRoutine != null)
            {
                StopCoroutine(_autoPlayRoutine);
                _autoPlayRoutine = null;
            }

            SyncSegmentFromSession();
            _localBeat = 0f;
            _lastFiredBeat = _segmentStartBeat - 1;
            _totalScrollPx = GetSegmentStartScrollPx();
            CapturePlayheadHoldScroll();
            ResetBrowsePanToPlayhead();
            if (_timeline != null)
            {
                _timeline.PlanningHorizonBeat = _segmentStartBeat;
            }

            ResetAllScanHighlights();
            RefreshLaneMarkers();
            var callback = _introCompleteCallback;
            _introCompleteCallback = null;
            _introBeatCount = 0;
            callback?.Invoke();
        }

        public void ResetForNextPlanningSegment()
        {
            StopAutoPlay();
            _autoPlayCompleted = false;
            _pausedForPlanning = false;
            _localBeat = 0f;
            _lastFiredBeat = -1;
            _totalScrollPx = 0f;
            ResetScrollState();
            CapturePlayheadHoldScroll();
            ResetBrowsePanToPlayhead();
            ResetAllScanHighlights();
            ActiveMusic?.EnterPlanningDuck();
        }

        /// <summary>
        /// Park scan on the completed segment's phase divider. Next Execute keeps this scroll
        /// and only advances forward (no snap back to the next phase's first note).
        /// </summary>
        public void HoldAtRoundEnd()
        {
            StopAutoPlay();
            _autoPlayCompleted = true;
            _autoPlayRoutine = null;
            _pausedForPlanning = false;

            SyncSegmentFromSession();
            var completedSegmentIndex = Mathf.Max(0, _roundSegmentIndex - 1);
            _localBeat = 0f;
            _lastFiredBeat = _segmentStartBeat - 1;
            SnapScrollToAnchor(GetSegmentPhaseDividerAnchorPx(completedSegmentIndex));
            CapturePlayheadHoldScroll();
            ResetBrowsePanToPlayhead();
            ResetAllScanHighlights();
            if (_timeline != null)
            {
                _timeline.PlanningHorizonBeat = _segmentStartBeat;
            }
        }

        /// <summary>Scan đang đóng băng chờ player set up skill (nhạc vẫn chạy).</summary>
        public bool IsPausedForPlanning => _pausedForPlanning;

        /// <summary>Scan đóng băng cho Encounter system (nhạc vẫn chạy, không duck).</summary>
        public bool IsPausedForEncounter => _pausedForEncounter;

        private int _tutorialPauseAtBeat = -1;

        public void ArmTutorialScanPause(int absoluteBeat)
        {
            _tutorialPauseAtBeat = absoluteBeat;
        }

        public void ClearTutorialScanPause()
        {
            _tutorialPauseAtBeat = -1;
        }

        public void EnterTutorialPlanningHold()
        {
            _pausedForPlanning = true;
            _isPlaybackActive = false;
            ActiveMusic?.EnterPlanningDuck();
            _onPlanningPause?.Invoke();
            ResetAllScanHighlights();
            RefreshLaneMarkers();
            RefreshTelegraphsAndSlots();
        }

        public RectTransform TryGetBeatSlotRect(int absoluteBeat)
        {
            var slot = TryGetSlotView(absoluteBeat);
            return slot != null ? slot.transform as RectTransform : null;
        }

        public RectTransform TryGetBossNoteRect(int absoluteBeat) => TryGetBeatSlotRect(absoluteBeat);

        /// <summary>Scan chạy tiếp từ vị trí đã đóng băng, bám vào mốc bar kế của nhạc đang chạy.</summary>
        public void ResumeRoundPlayback()
        {
            if (!_pausedForPlanning)
            {
                return;
            }

            _pausedForPlanning = false;
            ActiveMusic?.ExitPlanningDuck();
            ResumeScanFromFrozenLocalBeat();
        }

        public void PauseForEncounter()
        {
            _pausedForEncounter = true;
            _isPlaybackActive = false;
            if (_autoPlayRoutine != null)
            {
                StopCoroutine(_autoPlayRoutine);
                _autoPlayRoutine = null;
            }
        }

        /// <summary>Resume sweep mượt từ đúng vị trí pause; remap theo nhạc đang chạy.</summary>
        public void ResumeAfterEncounter()
        {
            if (!_pausedForEncounter)
            {
                return;
            }

            _pausedForEncounter = false;
            ResumeScanFromFrozenLocalBeat();
        }

        private void ResumeScanFromFrozenLocalBeat()
        {
            AnchorTimelineToNextBar();
            ResetCounterSfxState();
            RebuildCounterBeatCache();

            if (_autoPlayRoutine != null)
            {
                StopCoroutine(_autoPlayRoutine);
                _autoPlayRoutine = null;
            }

            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            {
                return;
            }

            _autoPlayRoutine = CanUseMusicSync()
                ? StartCoroutine(MusicDrivenScanRoutine(true))
                : StartCoroutine(ContinuousScanRoutine(true));
        }

        private void StartAutoPlayIfNeeded()
        {
            if (!autoPlayOnStart || _autoPlayCompleted)
            {
                return;
            }

            BeginRoundPlayback();
        }

        private bool CanUseMusicSync()
        {
            return useMusicSync && ActiveMusic != null;
        }

        private IEnumerator MusicDrivenScanRoutine(bool resume, bool continueFromHold = false)
        {
            if (ActiveMusic == null || !ActiveMusic.IsPlaying)
            {
                yield return ContinuousScanRoutine(resume, continueFromHold);
                yield break;
            }

            _isPlaybackActive = true;

            if (!resume)
            {
                PrepareSegmentScanStart(useMusicSync: true, continueFromHold);
            }

            EnsureTrackLine();

            while (_isPlaybackActive)
            {
                _localBeat = Mathf.Max(_localBeat, ActiveMusic.TotalMusicalBeat - _roundStartMusicalBeat);
                if (_localBeat >= GetSegmentBeatSpan())
                {
                    break;
                }

                AdvanceScrollToAbsoluteBeat(GetAbsolutePlaybackBeat());

                if (HasReachedSegmentDivider())
                {
                    SnapToSegmentDividerAndStop();
                    break;
                }

                if (!_isPlaybackActive)
                {
                    break;
                }

                if (_session != null && _session.IsEncounterOver)
                {
                    break;
                }

                yield return null;
            }

            if (_pausedForPlanning || _pausedForEncounter)
            {
                _autoPlayRoutine = null;
                yield break;
            }

            FinishRoundSegment();
        }

        private IEnumerator ContinuousScanRoutine(bool resume, bool continueFromHold = false)
        {
            _isPlaybackActive = true;

            if (!resume)
            {
                PrepareSegmentScanStart(useMusicSync: false, continueFromHold);
            }

            EnsureTrackLine();

            while (_isPlaybackActive && _localBeat < GetSegmentBeatSpan())
            {
                _localBeat += Time.deltaTime / GetBeatWaitDuration();
                AdvanceScrollToAbsoluteBeat(GetAbsolutePlaybackBeat());

                if (HasReachedSegmentDivider())
                {
                    SnapToSegmentDividerAndStop();
                    break;
                }

                if (!_isPlaybackActive)
                {
                    break;
                }

                if (_session != null && _session.IsEncounterOver)
                {
                    break;
                }

                yield return null;
            }

            if (_pausedForPlanning || _pausedForEncounter)
            {
                _autoPlayRoutine = null;
                yield break;
            }

            FinishRoundSegment();
        }

        private void FinishRoundSegment()
        {
            FlushUnfiredSegmentBeats();
            _isPlaybackActive = false;
            _autoPlayCompleted = true;
            _autoPlayRoutine = null;
            ResetAllScanHighlights();
            _onRoundSegmentComplete?.Invoke();
        }

        /// <summary>
        /// Resolve any remaining beats in the segment (last-note counter/damage) before phase transition waits.
        /// </summary>
        private void FlushUnfiredSegmentBeats()
        {
            if (_session == null)
            {
                return;
            }

            SyncSegmentFromSession();
            var segmentEnd = GetSegmentEndBeatExclusive();
            for (var beat = _lastFiredBeat + 1; beat < segmentEnd; beat++)
            {
                if (_session.IsEncounterOver)
                {
                    break;
                }

                FireScanBeat(beat);
            }
        }

        public void StopTimelinePlayback()
        {
            StopAutoPlay();
            _autoPlayCompleted = true;
            _pausedForPlanning = false;
            _pausedForEncounter = false;
            ResetCounterSfxState();
            ResetAllScanHighlights();
        }

        private void HandleTelegraphsPlanned(int phaseIndex)
        {
            RefreshAll();
        }

        private void HandleEncounterEnded()
        {
            StopTimelinePlayback();
            ActiveMusic?.StopMusic();
        }

        private void StopAutoPlay()
        {
            _isPlaybackActive = false;
            if (_autoPlayRoutine != null)
            {
                StopCoroutine(_autoPlayRoutine);
                _autoPlayRoutine = null;
            }
        }

        private float ComputePixelsPerSecond()
        {
            var beatMap = ActiveMusic != null ? ActiveMusic.BeatMap : null;
            var avgSpan = beatMap != null && beatMap.HasData
                ? beatMap.AverageBeatSpanSec()
                : autoBeatInterval;

            if (avgSpan <= 0.0001f)
            {
                avgSpan = autoBeatInterval > 0f ? autoBeatInterval : 60f / 152f;
            }

            return slotWidth / avgSpan;
        }

        private void SyncSegmentFromSession()
        {
            _roundSegmentIndex = _session != null ? _session.RoundSegmentIndex : 0;
            _segmentStartBeat = TimelineConstants.GetSegmentStartBeat(_roundSegmentIndex);
            SyncBeatWindow();
        }

        /// <summary>
        /// Sliding UI window: phases N / N+1 / N+2. When the active segment advances,
        /// N+1 becomes N and a new N+2 is bound onto the recycled slot pool.
        /// </summary>
        private void SyncBeatWindow(bool forceRebind = false)
        {
            var phase = TimelineConstants.GetPhaseIndex(_segmentStartBeat);
            var newStart = TimelineConstants.GetUiWindowStartBeat(phase);
            if (!forceRebind && _slotsBuilt && _slots != null && newStart == _windowStartBeat)
            {
                return;
            }

            _windowStartBeat = newStart;
            if (_slotsBuilt && _slots != null)
            {
                RebindWindowSlotRects();
                PopulateAllSlots();
            }
        }

        private void RebindWindowSlotRects()
        {
            if (_slots == null || _slotOffsetPx == null || _slotWidths == null)
            {
                return;
            }

            ClearHighlightedSlot();
            for (var i = 0; i < _slots.Length; i++)
            {
                var absBeat = AbsoluteBeatFromSlot(i);
                if (absBeat < 0 || absBeat >= TotalBeats)
                {
                    if (_slots[i] != null)
                    {
                        _slots[i].gameObject.SetActive(false);
                    }

                    continue;
                }

                if (_slots[i] != null)
                {
                    _slots[i].gameObject.SetActive(true);
                }

                ApplySlotRect(_slots[i], _slotWidths[absBeat], _slotOffsetPx[absBeat]);
            }

            LayoutPhaseDividers();
        }

        private int GetSegmentBeatSpan() => TimelineConstants.GetSegmentBeatCountForSegment(_roundSegmentIndex);

        private int GetSegmentEndBeatExclusive() => _segmentStartBeat + GetSegmentBeatSpan();

        private float GetAbsolutePlaybackBeat() => _segmentStartBeat + _localBeat;

        private const float SegmentScrollEpsilonPx = 1f;

        private float GetBeatEndContentPx(int beatIndex)
        {
            if (beatIndex < 0)
            {
                return 0f;
            }

            if (_slotOffsetPx != null && beatIndex + 1 < _slotOffsetPx.Length)
            {
                return _slotOffsetPx[beatIndex + 1];
            }

            return (beatIndex + 1) * TimelineLayoutLock.ClampSlotWidth(slotWidth);
        }

        private float GetPhaseDividerContentPx(int beatIndex) =>
            GetBeatEndContentPx(beatIndex) + PhaseDividerVisualOffsetPx;

        private float GetScanBarReadLineX() => GetScanLineX();

        private float ScrollPxForContentAnchor(float anchorPx) => anchorPx - GetScanBarReadLineX();

        private void SnapScrollToAnchor(float anchorPx)
        {
            _totalScrollPx = ScrollPxForContentAnchor(anchorPx);
            ApplyScrollVisual(_totalScrollPx);
        }

        private void CapturePlayheadHoldScroll()
        {
            _playheadHoldScrollPx = _totalScrollPx;
        }

        private void ResetBrowsePanToPlayhead()
        {
            StopBrowsePan();
            _browsePanPx = 0f;
            CapturePlayheadHoldScroll();
            // Restore ScanBar to cached scene home — do not re-capture from a mid-browse position.
            ApplyScrollVisual(_totalScrollPx);
            RefreshBrowseChevronVisibility();
        }

        private void BindBrowseChevronInput()
        {
            if (_browseInputBound)
            {
                return;
            }

            BindBrowseButtonHold(browseLeftButton, -1);
            BindBrowseButtonHold(browseRightButton, +1);
            _browseInputBound = browseLeftButton != null || browseRightButton != null;
        }

        private void BindBrowseButtonHold(Button button, int direction)
        {
            if (button == null)
            {
                return;
            }

            var trigger = button.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }

            trigger.triggers.RemoveAll(static e =>
                e.eventID == EventTriggerType.PointerDown || e.eventID == EventTriggerType.PointerUp
                || e.eventID == EventTriggerType.PointerExit);

            AddBrowseTrigger(trigger, EventTriggerType.PointerDown, _ => BeginBrowsePan(direction));
            AddBrowseTrigger(trigger, EventTriggerType.PointerUp, _ => StopBrowsePan());
            AddBrowseTrigger(trigger, EventTriggerType.PointerExit, _ => StopBrowsePan());
        }

        private static void AddBrowseTrigger(
            EventTrigger trigger,
            EventTriggerType type,
            UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        private void BeginBrowsePan(int direction)
        {
            if (direction == 0 || IsPlaybackActive || !_slotsBuilt)
            {
                return;
            }

            if (_browsePanPx <= BrowsePanEpsilonPx)
            {
                CapturePlayheadHoldScroll();
                if (!_scanBarHomeCaptured)
                {
                    CaptureScanBarHomeFromScene();
                }
            }

            _browseHoldDir = direction;
            if (_browsePanRoutine != null)
            {
                StopCoroutine(_browsePanRoutine);
            }

            _browsePanRoutine = StartCoroutine(BrowsePanRoutine());
        }

        private void StopBrowsePan()
        {
            _browseHoldDir = 0;
            if (_browsePanRoutine != null)
            {
                StopCoroutine(_browsePanRoutine);
                _browsePanRoutine = null;
            }

            RefreshBrowseChevronVisibility();
        }

        private IEnumerator BrowsePanRoutine()
        {
            while (_browseHoldDir != 0 && !IsPlaybackActive)
            {
                var maxPan = GetBrowseMaxPanPx();
                var speed = Mathf.Max(1f, browsePanSpeedPx);
                _browsePanPx = Mathf.Clamp(
                    _browsePanPx + _browseHoldDir * speed * Time.unscaledDeltaTime,
                    0f,
                    maxPan);
                ApplyBrowseVisual();

                var atPlayhead = _browsePanPx <= BrowsePanEpsilonPx;
                var atRightLimit = _browsePanPx >= maxPan - BrowsePanEpsilonPx;
                if ((_browseHoldDir < 0 && atPlayhead) || (_browseHoldDir > 0 && atRightLimit))
                {
                    break;
                }

                yield return null;
            }

            _browsePanRoutine = null;
            _browseHoldDir = 0;
            RefreshBrowseChevronVisibility();
        }

        /// <summary>
        /// Pan needed so beat 2 of the next phrase sits on the viewport's right edge.
        /// slotsRow is left-anchored in the viewport, so right edge = contentScroll + viewportWidth
        /// (not viewport.rect.xMax, which is +width/2 when the viewport pivot is centered).
        /// </summary>
        private float GetBrowseMaxPanPx()
        {
            if (viewport == null || !_slotsBuilt)
            {
                return 0f;
            }

            if (!TryGetBrowseRightLimitContentPx(out var limitContentX))
            {
                return 0f;
            }

            var viewportWidth = GetViewportWidth();
            if (viewportWidth <= 1f)
            {
                return 0f;
            }

            // Left-anchored ScrollContent: visible content is [scroll, scroll + width].
            var scrollForLimitAtRight = limitContentX - viewportWidth;
            var maxScroll = Mathf.Max(0f, _contentWidthPx - viewportWidth);
            scrollForLimitAtRight = Mathf.Clamp(scrollForLimitAtRight, 0f, maxScroll);
            return Mathf.Max(0f, scrollForLimitAtRight - _playheadHoldScrollPx);
        }

        /// <summary>
        /// Content X of the left edge of the 2nd beat in the next phase — pan shows
        /// current phase fully plus beat 1 of N+1 at the viewport's right edge.
        /// </summary>
        private bool TryGetBrowseRightLimitContentPx(out float contentPx)
        {
            contentPx = 0f;
            var nextPhase = TimelineConstants.GetPhaseIndex(_segmentStartBeat) + 1;
            if (nextPhase >= TimelineConstants.PhaseCount)
            {
                return false;
            }

            TimelineConstants.GetPhaseBeatRange(nextPhase, out var nextStart, out var nextCount);
            if (nextCount < 2)
            {
                return false;
            }

            contentPx = PxOfAbsoluteBeat(nextStart + 1);
            return true;
        }

        private void ApplyBrowseVisual()
        {
            if (slotsRow == null || scanBar == null || !_slotsBuilt)
            {
                return;
            }

            var maxPan = GetBrowseMaxPanPx();
            _browsePanPx = Mathf.Clamp(_browsePanPx, 0f, maxPan);

            var viewportWidth = GetViewportWidth();
            var maxScroll = Mathf.Max(0f, _contentWidthPx - viewportWidth);
            var contentScroll = Mathf.Clamp(_playheadHoldScrollPx + _browsePanPx, 0f, maxScroll);
            var homeX = GetScanBarReadLineX();

            slotsRow.anchoredPosition = new Vector2(-contentScroll, _scrollContentLock.Y);
            scanBar.anchoredPosition = new Vector2(homeX - _browsePanPx, 0f);
            SyncLaneMarkersScroll();
            RefreshBrowseChevronVisibility();
        }

        private void RefreshBrowseChevronVisibility()
        {
            var browsingAllowed = !IsPlaybackActive && _slotsBuilt;
            var maxPan = browsingAllowed ? GetBrowseMaxPanPx() : 0f;
            var atPlayhead = _browsePanPx <= BrowsePanEpsilonPx;
            var atRightLimit = maxPan <= BrowsePanEpsilonPx || _browsePanPx >= maxPan - BrowsePanEpsilonPx;

            if (browseLeftButton != null)
            {
                browseLeftButton.gameObject.SetActive(browsingAllowed && !atPlayhead);
            }

            if (browseRightButton != null)
            {
                browseRightButton.gameObject.SetActive(browsingAllowed && !atRightLimit && maxPan > BrowsePanEpsilonPx);
            }
        }

        /// <summary>
        /// Only move scroll forward to the beat position — never jump backward when resuming a hold.
        /// </summary>
        private void AdvanceScrollToAbsoluteBeat(float absoluteBeat)
        {
            var desired = ScrollPxForContentAnchor(PxOfAbsoluteBeat(absoluteBeat));
            if (desired > _totalScrollPx)
            {
                _totalScrollPx = desired;
            }

            ApplyScrollVisual(_totalScrollPx);
        }

        private float GetSegmentPhaseDividerAnchorPx(int segmentIndex)
        {
            var startBeat = TimelineConstants.GetSegmentStartBeat(segmentIndex);
            var span = TimelineConstants.GetSegmentBeatCountForSegment(segmentIndex);
            var dividerBeat = startBeat + span - 1;
            return GetPhaseDividerContentPx(dividerBeat);
        }

        private float GetSegmentPhaseDividerAnchorPx()
        {
            return GetSegmentPhaseDividerAnchorPx(_roundSegmentIndex);
        }

        /// <summary>Scroll px where ScanBar meets the phase divider at segment end.</summary>
        private float GetSegmentDividerScrollPx() => ScrollPxForContentAnchor(GetSegmentPhaseDividerAnchorPx());

        private float GetSegmentStartScrollPx() => ScrollPxForContentAnchor(PxOfAbsoluteBeat(_segmentStartBeat));

        private bool HasReachedSegmentDivider() =>
            _totalScrollPx >= GetSegmentDividerScrollPx() - AnchorScrollEpsilonPx;

        private void SnapToSegmentDividerAndStop()
        {
            _localBeat = GetSegmentBeatSpan();
            SnapScrollToAnchor(GetSegmentPhaseDividerAnchorPx());
            _isPlaybackActive = false;
        }

        public bool IsPlaybackActive =>
            _isPlaybackActive && !_pausedForPlanning && !_pausedForEncounter;

        public float GetAbsolutePlaybackBeatPublic() => GetAbsolutePlaybackBeat();

        public int GetCurrentScanBeatIndex() =>
            Mathf.Clamp(Mathf.FloorToInt(GetAbsolutePlaybackBeat()), 0, TotalBeats - 1);

        /// <summary>
        /// Re-seats timeline on running music: keep _localBeat, resume on the next beat
        /// (max ~1 beat wait — shortest beat-aligned Execute delay).
        /// </summary>
        private void AnchorTimelineToNextBar()
        {
            if (ActiveMusic == null)
            {
                return;
            }

            var target = MusicBeatMapSO.SnapUpToBeat(ActiveMusic.TotalMusicalBeat + ResumeLeadBeats);
            _roundStartMusicalBeat = target - _localBeat;
        }

        private void PrepareSegmentScanStart(bool useMusicSync, bool continueFromHold = false)
        {
            SyncSegmentFromSession();

            if (continueFromHold)
            {
                _localBeat = 0f;
                _lastFiredBeat = _segmentStartBeat - 1;
                if (useMusicSync)
                {
                    AnchorTimelineToNextBar();
                }

                RefreshTelegraphsAndSlots();
                ApplyScrollVisual(_totalScrollPx);
                _lastScanLineContentPos = GetScanLineContentPos();
                RebuildCounterBeatCache();
                return;
            }

            _localBeat = 0f;
            _lastFiredBeat = _segmentStartBeat - 1;

            if (useMusicSync && ActiveMusic != null && ActiveMusic.IsPlaying)
            {
                AnchorTimelineToNextBar();
            }
            else if (_roundSegmentIndex == 0)
            {
                _roundStartMusicalBeat = 0f;
            }

            RebuildLayout();
            _totalScrollPx = GetSegmentStartScrollPx();
            ApplyScrollVisual(_totalScrollPx);
            _lastScanLineContentPos = GetScanLineContentPos();
            RebuildCounterBeatCache();
        }

        private float PxOfAbsoluteBeat(float absoluteBeat)
        {
            if (_slotOffsetPx == null || _slotWidths == null)
            {
                return 0f;
            }

            var beat = Mathf.Clamp(absoluteBeat, 0f, TotalBeats - 0.001f);
            var k = Mathf.FloorToInt(beat);
            var frac = beat - k;
            if (k >= _slotOffsetPx.Length)
            {
                return _slotOffsetPx[^1];
            }

            return _slotOffsetPx[k] + frac * _slotWidths[k];
        }

        private float PxOfLocalBeat(float localBeat)
        {
            return PxOfAbsoluteBeat(localBeat);
        }

        private void ProcessScanLineCrossings()
        {
            if (!_isPlaybackActive || _pausedForPlanning || _pausedForEncounter || !_slotsBuilt)
            {
                return;
            }

            var contentPos = GetScanLineContentPos();
            if (contentPos < 0f)
            {
                return;
            }

            if (_lastScanLineContentPos < 0f)
            {
                _lastScanLineContentPos = contentPos;
                return;
            }

            if (_lastFiredBeat < _segmentStartBeat - 1)
            {
                _lastFiredBeat = _segmentStartBeat - 1;
            }

            var segmentEnd = GetSegmentEndBeatExclusive();
            for (var beat = _lastFiredBeat + 1; beat < segmentEnd; beat++)
            {
                var hitX = GetBeatHitContentX(beat);
                if (contentPos < hitX)
                {
                    break;
                }

                TryPlayCounterEnterSfx(beat);
                FireScanBeat(beat);

                if (_pausedForEncounter || _pausedForPlanning)
                {
                    _lastScanLineContentPos = contentPos;
                    return;
                }

                if (_session != null && _session.IsEncounterOver)
                {
                    _isPlaybackActive = false;
                    _lastScanLineContentPos = contentPos;
                    return;
                }

                if (beat >= segmentEnd - 1)
                {
                    SnapToSegmentDividerAndStop();
                    _lastScanLineContentPos = contentPos;
                    return;
                }
            }

            _lastScanLineContentPos = contentPos;
        }

        private float GetScanLineContentPos()
        {
            if (!_slotsBuilt || slotsRow == null || scanBar == null)
            {
                return -1f;
            }

            return scanBar.anchoredPosition.x - slotsRow.anchoredPosition.x;
        }

        private float GetBeatHitContentX(int beat)
        {
            if (_slotOffsetPx == null || _slotWidths == null || beat < 0 || beat >= TotalBeats)
            {
                return 0f;
            }

            return _slotOffsetPx[beat] + _slotWidths[beat] * beatHitAnchorT;
        }

        private void FireScanBeat(int beat)
        {
            _lastFiredBeat = beat;
            _autoPlayBeat = beat;
            _session?.OnTimelineScanBeat(beat);
            RefreshPhaseHeader(beat);
            var introActive = _session != null && _session.IsCombatIntroActive;
            if (!introActive)
            {
                var encounter = EncounterDirector.ActiveInstance
                                ?? FindAnyObjectByType<EncounterDirector>();
                if (encounter != null && encounter.TryInterceptScanBeat(beat))
                {
                    RefreshBeat(beat);
                    RefreshPhaseAvLabel();
                    UpdateScanHighlights();
                    return;
                }

                _session?.ResolveBeatAtScan(beat);
                PlayAttackAnimationsAtBeat(beat);
            }

            RefreshBeat(beat);
            RefreshPhaseAvLabel();
            UpdateScanHighlights();

            if (_tutorialPauseAtBeat >= 0 && beat >= _tutorialPauseAtBeat)
            {
                _tutorialPauseAtBeat = -1;
                ActiveMusic?.EnterPlanningDuck();
                _pausedForPlanning = true;
                _isPlaybackActive = false;
                if (_autoPlayRoutine != null)
                {
                    StopCoroutine(_autoPlayRoutine);
                    _autoPlayRoutine = null;
                }

                _onPlanningPause?.Invoke();
            }
        }

        private void PlayAttackAnimationsAtBeat(int beatIndex)
        {
            if (_timeline == null || beatIndex < 0)
            {
                return;
            }

            var isCounterBeat = _precomputedCounterBeats.Contains(beatIndex);
            var shot = FindAnyObjectByType<PlayerSkillShotChoreographer>();

            foreach (var entry in _timeline.Agenda)
            {
                if (entry.Unit == null || entry.Unit.Side != GridSide.Player || entry.Skill == null || entry.Skill.IsGuard)
                {
                    continue;
                }

                if (!CombatCounterResolver.GetActiveBeatIndices(entry).Contains(beatIndex))
                {
                    continue;
                }

                if (isCounterBeat || EncounterDirector.IsPresenting)
                {
                    continue;
                }

                if (shot != null
                    && (shot.IsMeleeSkill(entry.Skill) || shot.IsMultiBulletSkill(entry.Skill)))
                {
                    continue;
                }

                var view = UnitView.FindForUnit(entry.Unit);
                if (entry.Skill.slotKind is SkillSlotKind.Skill or SkillSlotKind.Ultimate)
                {
                    view?.PlayAttackAnimationHold(entry.Skill);
                }
                else
                {
                    view?.PlayAttackAnimation(entry.Skill);
                }
            }
        }

        private void ApplyScrollVisual(float scrollPx)
        {
            if (slotsRow == null || scanBar == null || !_slotsBuilt)
            {
                return;
            }

            // Execute / hold path pins ScanBar to scene home; browse pan uses ApplyBrowseVisual.
            if (_browsePanPx > BrowsePanEpsilonPx && !IsPlaybackActive)
            {
                ApplyBrowseVisual();
                return;
            }

            var viewportWidth = GetViewportWidth();
            var maxScroll = Mathf.Max(0f, _contentWidthPx - viewportWidth);
            var readLineX = GetScanBarReadLineX();
            var clampedScroll = Mathf.Clamp(scrollPx, 0f, maxScroll);

            slotsRow.anchoredPosition = new Vector2(-clampedScroll, _scrollContentLock.Y);
            scanBar.anchoredPosition = new Vector2(readLineX, 0f);

            SyncLaneMarkersScroll();
            ProcessScanLineCrossings();
            UpdateScanHighlights();
            RefreshBrowseChevronVisibility();
        }

        private void UpdateScanHighlights()
        {
            if (!_slotsBuilt || scanBar == null || _slots == null || _slotOffsetPx == null)
            {
                return;
            }

            var rowX = slotsRow != null ? slotsRow.anchoredPosition.x : 0f;
            var contentPos = scanBar.anchoredPosition.x - rowX;
            var absoluteBeat = FindSlotAtContentPos(contentPos);
            var slotIndex = SlotIndexFromAbsolute(absoluteBeat);

            if (slotIndex < 0 || absoluteBeat < 0 || absoluteBeat >= TotalBeats)
            {
                if (_lastHighlightedSlotIndex >= 0)
                {
                    ClearHighlightedSlot();
                }

                return;
            }

            var width = _slotWidths[absoluteBeat];
            var inSlot = contentPos - _slotOffsetPx[absoluteBeat];
            var p = width > 0f ? inSlot / width : 0.5f;
            var intensity = p <= 0.5f ? Mathf.SmoothStep(0f, 1f, p / 0.5f) : 0f;

            if (_lastHighlightedSlotIndex >= 0 &&
                _lastHighlightedSlotIndex != slotIndex &&
                _lastHighlightedSlotIndex < _slots.Length)
            {
                _slots[_lastHighlightedSlotIndex]?.SetScanIntensity(0f);
            }

            _slots[slotIndex]?.SetScanIntensity(intensity);
            _lastHighlightedSlotIndex = slotIndex;
        }

        private void EnsureCombatSfx()
        {
            if (combatSfxController == null)
            {
                combatSfxController = FindAnyObjectByType<CombatSfxController>();
            }
        }

        private void ResetCounterSfxState()
        {
            _lastCounterSfxBeat = -1;
            _lastScanLineContentPos = -1f;
            _precomputedCounterBeats.Clear();
        }

        private void RebuildCounterBeatCache()
        {
            _precomputedCounterBeats.Clear();
            if (_timeline == null)
            {
                return;
            }

            var segmentEnd = GetSegmentEndBeatExclusive();
            for (var beat = _segmentStartBeat; beat < segmentEnd; beat++)
            {
                if (CombatCounterResolver.HasCounterOnBeat(_timeline, beat))
                {
                    _precomputedCounterBeats.Add(beat);
                }
            }
        }

        private void TryPlayCounterEnterSfx(int beatIndex)
        {
            if (beatIndex < 0 || beatIndex == _lastCounterSfxBeat || !_isPlaybackActive || _pausedForPlanning)
            {
                return;
            }

            if (!_precomputedCounterBeats.Contains(beatIndex))
            {
                return;
            }

            _lastCounterSfxBeat = beatIndex;
            var musicalBeat = _roundStartMusicalBeat + (beatIndex - _segmentStartBeat);
            var targetDsp = -1d;
            if (ActiveMusic != null &&
                ActiveMusic.TryGetDspTimeForMusicalBeat(musicalBeat, out var dspTime))
            {
                targetDsp = dspTime;
            }

            if (ActiveMusic != null &&
                ActiveMusic.TryGetMusicDeltaMs(musicalBeat, out var deltaMs))
            {
                if (Mathf.Abs(deltaMs) > 50f)
                {
                    Debug.LogWarning(
                        $"[CounterSync] hitch beat={beatIndex} musicBeat={musicalBeat:F3} deltaMs={deltaMs:F1}");
                }
                else
                {
                    Debug.Log(
                        $"[CounterSync] beat={beatIndex} musicBeat={musicalBeat:F3} deltaMs={deltaMs:F1}");
                }
            }

            if (counterPresentation != null)
            {
                counterPresentation.NotifyPerfect(beatIndex, _timeline, targetDsp);
                return;
            }

            EnsureCombatSfx();
            if (combatSfxController == null)
            {
                return;
            }

            combatSfxController.PlayPerfectCounter(targetDsp);
            PlayCounterAnimations(beatIndex);
        }

        private void HandleBlockResolved(int beatIndex, BlockTiming timing)
        {
            if (timing != BlockTiming.OnBeat)
            {
                return;
            }

            EnsureCombatSfx();
            if (combatSfxController == null)
            {
                Debug.LogWarning($"[Block] Perfect SFX skipped — no CombatSfxController @ beat {beatIndex}");
                return;
            }

            combatSfxController.PlayPerfectBlock(-1d);
            Debug.Log($"[Block] Perfect SFX @ beat {beatIndex}");
        }

        private void PlayCounterAnimations(int beatIndex)
        {
            if (_timeline == null)
            {
                return;
            }

            CombatCounterResolver.CollectCounteringPlayerUnits(_timeline, beatIndex, _counterUnitsScratch);
            foreach (var unit in _counterUnitsScratch)
            {
                UnitView.FindForUnit(unit)?.PlayCounterAnimation();
            }

            CombatCounterResolver.CollectCounteredEnemyUnits(_timeline, beatIndex, _counteredEnemyUnitsScratch);
            foreach (var unit in _counteredEnemyUnitsScratch)
            {
                UnitView.FindForUnit(unit)?.PlayBeCounteredAnimation();
            }
        }

        private int FindSlotAtContentPos(float contentPos)
        {
            if (_slotOffsetPx == null || contentPos < 0f || contentPos >= _contentWidthPx)
            {
                return -1;
            }

            var lo = 0;
            var hi = TotalBeats - 1;
            while (lo < hi)
            {
                var mid = (lo + hi + 1) >> 1;
                if (_slotOffsetPx[mid] <= contentPos)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            return lo;
        }

        // ---------------------------------------------------------------------
        // Character lanes (dòng kẻ cho từng nhân vật) + skill markers
        // ---------------------------------------------------------------------

        private void EnsureLaneLayers()
        {
            if (viewport == null)
            {
                return;
            }

            if (_laneLinesLayer == null)
            {
                var existingLines = viewport.Find("LaneLines") as RectTransform;
                if (existingLines != null)
                {
                    _laneLinesLayer = existingLines;
                    _laneLinesLayerAuthoredInScene = true;
                }
                else
                {
                    var go = new GameObject("LaneLines", typeof(RectTransform));
                    _laneLinesLayer = go.GetComponent<RectTransform>();
                    _laneLinesLayer.SetParent(viewport, false);
                    _laneLinesLayerAuthoredInScene = false;
                }
            }

            // Scene LaneLines rect is SoT — do not overwrite insets/pos when authored.
            if (!preserveSceneLayout || !_laneLinesLayerAuthoredInScene)
            {
                ApplyLaneLinesLayerInsets();
            }

            if (_footprintLayer == null)
            {
                _footprintLayer = laneFootprint;
                if (_footprintLayer == null)
                {
                    _footprintLayer = FindTimelineRect("LaneFootprint");
                }

                if (_footprintLayer != null)
                {
                    _laneFootprintAuthoredInScene = true;
                    CaptureLaneFootprintSceneRect();
                }
                else
                {
                    var go = new GameObject("LaneFootprint", typeof(RectTransform));
                    _footprintLayer = go.GetComponent<RectTransform>();
                    _footprintLayer.SetParent(viewport, false);
                    _footprintLayer.anchorMin = new Vector2(0f, 0f);
                    _footprintLayer.anchorMax = new Vector2(0f, 1f);
                    _footprintLayer.pivot = new Vector2(0f, 0.5f);
                    _footprintLayer.offsetMin = Vector2.zero;
                    _footprintLayer.offsetMax = Vector2.zero;
                    laneFootprint = _footprintLayer;
                }
            }

            if (_laneMarkersLayer == null)
            {
                var existing = viewport.Find("LaneMarkers") as RectTransform;
                if (existing != null)
                {
                    _laneMarkersLayer = existing;
                }
                else
                {
                    var go = new GameObject("LaneMarkers", typeof(RectTransform));
                    _laneMarkersLayer = go.GetComponent<RectTransform>();
                    _laneMarkersLayer.SetParent(viewport, false);
                    _laneMarkersLayer.anchorMin = new Vector2(0f, 0f);
                    _laneMarkersLayer.anchorMax = new Vector2(0f, 1f);
                    _laneMarkersLayer.pivot = new Vector2(0f, 0.5f);
                    _laneMarkersLayer.offsetMin = Vector2.zero;
                    _laneMarkersLayer.offsetMax = Vector2.zero;
                }
            }

            if (!preserveSceneLayout || !_laneFootprintAuthoredInScene)
            {
                _footprintLayer.SetAsLastSibling();
            }

            _laneMarkersLayer.SetAsLastSibling();
            OrderViewportLayers();
            BringResolveFeedbackToFront();
            if (!preserveSceneLayout || !_laneFootprintAuthoredInScene)
            {
                ApplySceneContentWidth(_footprintLayer, in _laneFootprintLock, _contentWidthPx);
            }

            if (!preserveSceneLayout)
            {
                _laneMarkersLayer.sizeDelta = new Vector2(_contentWidthPx, _laneMarkersLayer.sizeDelta.y);
            }
        }

        private void ApplyLaneLinesLayerInsets()
        {
            if (_laneLinesLayer == null)
            {
                return;
            }

            _laneLinesLayer.anchorMin = Vector2.zero;
            _laneLinesLayer.anchorMax = Vector2.one;
            _laneLinesLayer.pivot = new Vector2(0.5f, 0.5f);
            _laneLinesLayer.anchoredPosition = Vector2.zero;
            _laneLinesLayer.sizeDelta = Vector2.zero;
            // Unity Inspector: Top = -offsetMax.y, Bottom = offsetMin.y
            _laneLinesLayer.offsetMin = new Vector2(0f, laneLinesBottomInset);
            _laneLinesLayer.offsetMax = new Vector2(0f, -laneLinesTopInset);
        }

        private void OrderViewportLayers()
        {
            if (viewport == null)
            {
                return;
            }

            if (_staffBackground != null)
            {
                _staffBackground.transform.SetAsFirstSibling();
            }

            if (_bossTrackFrame != null && (!preserveSceneLayout || !_bossTrackFrameAuthoredInScene))
            {
                _bossTrackFrame.SetSiblingIndex(_staffBackground != null ? 1 : 0);
            }

            if (slotsRow != null && (!preserveSceneLayout || !_scrollContentAuthoredInScene))
            {
                var idx = 0;
                if (_staffBackground != null)
                {
                    idx++;
                }

                if (_bossTrackFrame != null)
                {
                    idx++;
                }

                slotsRow.SetSiblingIndex(idx);
            }

            // Draw order (sibling only — do not move authored rects). ScanBar stays
            // when preserveSceneLayout (scene SoT: ScanBar before LaneLines).
            if (_laneLinesLayer != null)
            {
                _laneLinesLayer.SetAsLastSibling();
            }

            if (_bossNoteClusterLayer != null)
            {
                _bossNoteClusterLayer.SetAsLastSibling();
            }

            if (_footprintLayer != null)
            {
                _footprintLayer.SetAsLastSibling();
            }

            if (_blockBarrierLayer != null)
            {
                _blockBarrierLayer.SetAsLastSibling();
            }

            if (!preserveSceneLayout && scanBar != null)
            {
                scanBar.SetAsLastSibling();
            }

            // Markers on top so skill drag on lanes is not blocked.
            if (_laneMarkersLayer != null)
            {
                _laneMarkersLayer.SetAsLastSibling();
            }
        }

        private void BuildLanes()
        {
            EnsureLaneLayers();
            EnsureBossTrackFrame();

            _laneLines.Clear();
            _laneUnits.Clear();
            _laneIndex.Clear();
            _laneShellIndices.Clear();

            ClearLaneMarkers();
            ClearFootprintDots();

            if (_session == null || _session.Grid == null || _laneLinesLayer == null)
            {
                EnsureLaneAvatarColumn();
                return;
            }

            // Line 1 (below monster) → Line N follows party card order (formation).
            var ordered = CollectAlivePartyInCardOrder();
            for (var i = 0; i < ordered.Count && i < MaxTimelinePartyLanes; i++)
            {
                var unit = ordered[i];
                var existing = _laneLinesLayer.Find($"Lane_{i}") as RectTransform;
                var lineRect = existing != null ? existing : CreateFallbackLaneRect(i);
                lineRect.gameObject.SetActive(true);
                BindLaneVisual(lineRect, unit, i);
                _laneIndex[unit] = _laneUnits.Count;
                _laneUnits.Add(unit);
                _laneLines.Add(lineRect);
                _laneShellIndices.Add(i);
            }

            for (var shell = ordered.Count; shell < MaxTimelinePartyLanes; shell++)
            {
                var extra = _laneLinesLayer.Find($"Lane_{shell}");
                if (extra != null)
                {
                    extra.gameObject.SetActive(false);
                }
            }

            LayoutLanes();
            EnsureLaneAvatarColumn();
        }

        /// <summary>Alive player units in the same order as party status cards.</summary>
        private List<CombatUnit> CollectAlivePartyInCardOrder()
        {
            var ordered = new List<CombatUnit>();
            if (_session?.Grid == null)
            {
                return ordered;
            }

            foreach (var unit in _session.Grid.PlayerUnits)
            {
                if (unit != null && unit.IsAlive)
                {
                    ordered.Add(unit);
                }
            }

            ordered.Sort(PartyCardDisplayOrder.CompareUnits);
            return ordered;
        }

        private RectTransform CreateFallbackLaneRect(int index)
        {
            var lineGo = new GameObject($"Lane_{index}", typeof(RectTransform));
            var lineRect = lineGo.GetComponent<RectTransform>();
            lineRect.SetParent(_laneLinesLayer, false);
            lineRect.anchorMin = new Vector2(0f, 0f);
            lineRect.anchorMax = new Vector2(1f, 0f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.sizeDelta = new Vector2(0f, 5f);
            lineRect.anchoredPosition = Vector2.zero;
            if (viewport != null)
            {
                SetRectYFromViewportBottom(
                    lineRect,
                    GetLaneYFromBottom(index, viewport.rect.height));
            }
            lineGo.AddComponent<Image>().raycastTarget = false;
            EnsureLaneLabel(lineRect);
            return lineRect;
        }

        private static void EnsureLaneLabel(RectTransform lineRect)
        {
            if (lineRect == null || lineRect.Find("Label") != null)
            {
                return;
            }

            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.SetParent(lineRect, false);
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(0f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(4f, 8f);
            labelRect.sizeDelta = new Vector2(90f, 14f);
            var label = labelGo.AddComponent<Text>();
            label.font = UiFontCatalog.Body;
            label.fontSize = 10;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
        }

        private void BindLaneVisual(RectTransform lineRect, CombatUnit unit, int index)
        {
            if (lineRect == null || unit == null)
            {
                return;
            }

            var hadLabel = lineRect.Find("Label") != null;
            EnsureLaneLabel(lineRect);
            var tint = unit.TimelineLaneColor;
            var lineImage = lineRect.GetComponent<Image>();
            if (lineImage == null)
            {
                lineImage = lineRect.gameObject.AddComponent<Image>();
            }

            lineImage.color = new Color(
                Mathf.Min(1f, tint.r * 1.15f + 0.08f),
                Mathf.Min(1f, tint.g * 1.15f + 0.08f),
                Mathf.Min(1f, tint.b * 1.15f + 0.08f),
                0.92f);
            lineImage.raycastTarget = false;

            var label = lineRect.Find("Label")?.GetComponent<Text>();
            if (label == null)
            {
                return;
            }

            label.raycastTarget = false;
            label.color = new Color(tint.r, tint.g, tint.b, 0.9f);
            label.text = !string.IsNullOrEmpty(unit.DisplayName)
                ? unit.DisplayName.ToUpperInvariant()
                : $"UNIT {index}";

            // Keep authored Label layout (font/size/pos); only sync name + tint when scene SoT.
            if (preserveSceneLayout && hadLabel)
            {
                return;
            }

            label.font = UiFontCatalog.Body;
            label.fontSize = 10;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void EnsureBossTrackFrame()
        {
            if (viewport == null)
            {
                WireReferences();
            }

            if (viewport == null)
            {
                return;
            }

            EnsureLaneLayers();
            DestroyLegacyBossLaneLine();

            if (_bossTrackFrame == null)
            {
                _bossTrackFrame = bossTrackFrame;
                if (_bossTrackFrame == null)
                {
                    _bossTrackFrame = FindTimelineRect("BossTrackFrame");
                }

                if (_bossTrackFrame != null)
                {
                    _bossTrackFrameAuthoredInScene = true;
                    CaptureBossTrackFrameSceneRect();
                }
                else
                {
                    var root = new GameObject("BossTrackFrame", typeof(RectTransform));
                    _bossTrackFrame = root.GetComponent<RectTransform>();
                    _bossTrackFrame.SetParent(viewport, false);
                    _bossTrackFrame.anchorMin = new Vector2(0f, 0f);
                    _bossTrackFrame.anchorMax = new Vector2(0f, 0f);
                    _bossTrackFrame.pivot = new Vector2(0f, 0.5f);
                    CreateBossTrackChild("BorderTop", _bossTrackFrame, stretch: false);
                    _bossTrackFrameAuthoredInScene = false;
                    bossTrackFrame = _bossTrackFrame;
                }
            }

            // Strip Fill — note rail is BorderTop only.
            var fill = _bossTrackFrame.Find("Fill");
            if (fill != null)
            {
                Destroy(fill.gameObject);
            }

            MigrateBorderBottomToLane0();

            if (_bossTrackFrame.Find("BorderTop") == null)
            {
                CreateBossTrackChild("BorderTop", _bossTrackFrame, stretch: false);
            }

            DetachBossTrackFromScrollContent();
            LayoutBossTrackFrame();
            OrderViewportLayers();
        }

        /// <summary>
        /// BorderBottom becomes Character Line 1 (Lane_0) under LaneLines — once, idempotent.
        /// </summary>
        private void MigrateBorderBottomToLane0()
        {
            if (_bossTrackFrame == null || _laneLinesLayer == null)
            {
                return;
            }

            var bottom = _bossTrackFrame.Find("BorderBottom") as RectTransform;
            if (bottom == null)
            {
                return;
            }

            var existingLane0 = _laneLinesLayer.Find("Lane_0") as RectTransform;
            if (existingLane0 != null)
            {
                Destroy(bottom.gameObject);
                return;
            }

            var worldY = bottom.position.y;
            bottom.SetParent(_laneLinesLayer, false);
            bottom.name = "Lane_0";
            bottom.anchorMin = new Vector2(0f, 0f);
            bottom.anchorMax = new Vector2(1f, 0f);
            bottom.pivot = new Vector2(0.5f, 0.5f);
            bottom.sizeDelta = new Vector2(0f, Mathf.Max(2f, bottom.sizeDelta.y));

            if (viewport != null)
            {
                var local = viewport.InverseTransformPoint(new Vector3(0f, worldY, 0f));
                var yFromBottom = local.y - viewport.rect.yMin;
                bottom.anchoredPosition = new Vector2(0f, yFromBottom);
            }

            EnsureLaneLabel(bottom);
        }

        private void DestroyLegacyBossLaneLine()
        {
            if (_laneLinesLayer != null)
            {
                var legacy = _laneLinesLayer.Find("BossLane");
                if (legacy != null)
                {
                    Destroy(legacy.gameObject);
                }
            }
        }

        private static Image CreateBossTrackChild(string name, RectTransform parent, bool stretch)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            if (stretch)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(0f, 2f);
            }

            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        private void DetachBossTrackFromScrollContent()
        {
            if (_bossTrackFrame == null || viewport == null || !IsNestedInSlotsRow(_bossTrackFrame))
            {
                return;
            }

            _bossTrackFrame.SetParent(viewport, true);
            _bossTrackFrameLock = default;
            CaptureBossTrackFrameSceneRect();
        }

        private void LayoutBossTrackFrame()
        {
            if (_bossTrackFrame == null || viewport == null)
            {
                return;
            }

            CaptureBossTrackFrameSceneRect();
            var top = _bossTrackFrame.Find("BorderTop") as RectTransform;

            if (preserveSceneLayout && _bossTrackFrameAuthoredInScene)
            {
                ApplySceneAuthoredPosition(_bossTrackFrame, in _bossTrackFrameLock);
                return;
            }

            var borderH = Mathf.Max(1f, bossTrackFrameBorderThickness);
            var fallbackRailY = GetNoteCoverYFromBottom(viewport.rect.height);

            _bossTrackFrame.sizeDelta = new Vector2(viewport.rect.width, borderH);
            _bossTrackFrame.anchoredPosition = new Vector2(0f, fallbackRailY);
            _bossTrackFrame.pivot = new Vector2(0f, 0.5f);

            LayoutBossTrackBorder(top, 0f, borderH);
            var topImg = top != null ? top.GetComponent<Image>() : null;
            if (topImg != null)
            {
                topImg.color = bossTrackFrameBorderTop;
            }
        }

        private static void LayoutBossTrackBorder(RectTransform border, float y, float thickness)
        {
            if (border == null)
            {
                return;
            }

            border.anchorMin = new Vector2(0f, 0.5f);
            border.anchorMax = new Vector2(1f, 0.5f);
            border.pivot = new Vector2(0.5f, 0.5f);
            border.anchoredPosition = new Vector2(0f, y);
            border.sizeDelta = new Vector2(0f, thickness);
        }

        private void EnsureLanesUpToDate()
        {
            if (_session == null || _session.Grid == null)
            {
                return;
            }

            var expected = CollectAlivePartyInCardOrder();
            if (expected.Count > MaxTimelinePartyLanes)
            {
                expected.RemoveRange(MaxTimelinePartyLanes, expected.Count - MaxTimelinePartyLanes);
            }

            var changed = expected.Count != _laneUnits.Count;
            if (!changed)
            {
                for (var i = 0; i < expected.Count; i++)
                {
                    if (_laneUnits[i] != expected[i])
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (changed)
            {
                BuildLanes();
            }
        }

        private void LayoutLanes()
        {
            if (viewport == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            CaptureSceneLaneBandIfNeeded();

            var height = viewport.rect.height;
            var partyCount = Mathf.Clamp(_laneLines.Count, 0, MaxTimelinePartyLanes);
            ResolveEvenStaffYs(partyCount, height, out var bossY, out var partyYs);
            _layoutBossRailY = bossY;

            // Redistribute active party lanes + boss rail evenly across the scene band.
            for (var i = 0; i < _laneLines.Count; i++)
            {
                if (_laneLines[i] == null)
                {
                    continue;
                }

                SetRectYFromViewportBottom(_laneLines[i], partyYs[i]);
            }

            LayoutBossTrackFrame();
        }

        private void EnsureLaneAvatarColumn()
        {
            EnsureAvatarColumnShell();
            if (laneAvatarGutter == null)
            {
                return;
            }

            leftRailLayout ??= new LeftRailLayout();
            LoadLeftRailSpritesIfNeeded();

            var keepScene = preserveSceneLayout || leftRailLayout.preserveSceneRects;
            if (leftRailLayout.forceAvatarLayout && !keepScene)
            {
                LayoutLaneAvatarGutterFlushToViewport();
            }

            _laneAvatarSlots.Clear();

            if (_laneUnits.Count == 0)
            {
                for (var i = 0; i < MaxTimelinePartyLanes; i++)
                {
                    var extra = laneAvatarGutter.Find($"LaneAvatar_{i}");
                    if (extra != null)
                    {
                        extra.gameObject.SetActive(false);
                    }
                }

                return;
            }

            var viewportHeight = viewport != null ? viewport.rect.height : 100f;
            var fallbackSlotSize = ResolveAvatarSlotSizeFromScene();
            var fallbackSlotX = ResolveAvatarSlotXFromScene();
            var activeShells = new HashSet<int>();
            for (var i = 0; i < _laneUnits.Count; i++)
            {
                var unit = _laneUnits[i];
                var shell = i < _laneShellIndices.Count ? _laneShellIndices[i] : i;
                activeShells.Add(shell);
                var existing = laneAvatarGutter.Find($"LaneAvatar_{shell}") as RectTransform;
                RectTransform slotRect;
                TimelineLaneAvatarSlotView slotView;
                if (existing != null)
                {
                    slotRect = existing;
                    slotRect.gameObject.SetActive(true);
                    slotView = slotRect.GetComponent<TimelineLaneAvatarSlotView>();
                    if (slotView == null)
                    {
                        slotView = slotRect.gameObject.AddComponent<TimelineLaneAvatarSlotView>();
                    }
                }
                else
                {
                    var slotGo = new GameObject($"LaneAvatar_{shell}", typeof(RectTransform));
                    slotRect = slotGo.GetComponent<RectTransform>();
                    slotRect.SetParent(laneAvatarGutter, false);
                    slotRect.anchorMin = new Vector2(0.5f, 0f);
                    slotRect.anchorMax = new Vector2(0.5f, 0f);
                    slotRect.pivot = new Vector2(0.5f, 0.5f);
                    slotRect.sizeDelta = new Vector2(fallbackSlotSize, fallbackSlotSize);
                    slotRect.anchoredPosition = new Vector2(fallbackSlotX, 0f);
                    slotView = slotGo.AddComponent<TimelineLaneAvatarSlotView>();
                }

                var laneLine = i < _laneLines.Count ? _laneLines[i] : null;
                var laneY = ResolveAvatarYAlignedToLane(laneLine, viewportHeight, i);

                // Scene is source of truth for size / X / anchors / FrameRing art.
                // Runtime only keeps avatar parallel with Lane_* (Y sync).
                slotRect.anchoredPosition = new Vector2(slotRect.anchoredPosition.x, laneY);

                slotView.Bind(unit, _onLaneAvatarClicked);
                slotView.ApplyFrameSpriteIfMissing(ResolveLaneAvatarFrame(unit));
                slotView.SetSelected(unit == _selectedLaneUnit);
                _laneAvatarSlots.Add(slotView);
            }

            for (var shell = 0; shell < MaxTimelinePartyLanes; shell++)
            {
                if (activeShells.Contains(shell))
                {
                    continue;
                }

                var extra = laneAvatarGutter.Find($"LaneAvatar_{shell}");
                if (extra != null)
                {
                    extra.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>Avatar slot size from authored LaneAvatar_* on scene; fallback LeftRailLayout.</summary>
        private float ResolveAvatarSlotSizeFromScene()
        {
            if (laneAvatarGutter != null)
            {
                for (var i = 0; i < MaxTimelinePartyLanes; i++)
                {
                    var rt = laneAvatarGutter.Find($"LaneAvatar_{i}") as RectTransform;
                    if (rt != null && rt.sizeDelta.x > 1f)
                    {
                        return rt.sizeDelta.x;
                    }
                }
            }

            leftRailLayout ??= new LeftRailLayout();
            return Mathf.Max(24f, leftRailLayout.avatarSlotSize);
        }

        /// <summary>Avatar slot X from authored LaneAvatar_* on scene.</summary>
        private float ResolveAvatarSlotXFromScene()
        {
            if (laneAvatarGutter != null)
            {
                for (var i = 0; i < MaxTimelinePartyLanes; i++)
                {
                    var rt = laneAvatarGutter.Find($"LaneAvatar_{i}") as RectTransform;
                    if (rt != null)
                    {
                        return rt.anchoredPosition.x;
                    }
                }
            }

            return 0f;
        }

        /// <summary>
        /// Map Lane_* world position → LaneAvatar anchored Y (bottom-anchored in gutter).
        /// InverseTransform local Y is pivot-relative; subtract rect.yMin for bottom-space.
        /// </summary>
        private float ResolveAvatarYAlignedToLane(RectTransform laneLine, float viewportHeight, int laneIndex)
        {
            if (laneLine != null && laneAvatarGutter != null)
            {
                Canvas.ForceUpdateCanvases();
                if (laneAvatarGutter.rect.height > 1f)
                {
                    var world = laneLine.TransformPoint(Vector3.zero);
                    var localY = laneAvatarGutter.InverseTransformPoint(world).y;
                    // Child anchors (0.5, 0): anchoredPosition.y is distance from gutter bottom.
                    return localY - laneAvatarGutter.rect.yMin;
                }
            }

            return GetLaneYFromBottom(laneIndex, viewportHeight);
        }

        private void EnsureAvatarColumnRoot()
        {
            if (laneAvatarGutter == null)
            {
                laneAvatarGutter = transform.Find("LaneAvatarGutter") as RectTransform;
            }

            if (laneAvatarGutter == null)
            {
                var go = new GameObject("LaneAvatarGutter", typeof(RectTransform));
                laneAvatarGutter = go.GetComponent<RectTransform>();
                laneAvatarGutter.SetParent(transform, false);
            }
        }

        private void ApplyAvatarColumnBackground()
        {
            if (laneAvatarGutter == null)
            {
                return;
            }

            LoadLeftRailSpritesIfNeeded();

            if (avatarColumnBackgroundImage == null)
            {
                avatarColumnBackgroundImage = laneAvatarGutter.Find("AvatarColumnBackground")?.GetComponent<Image>();
            }

            if (avatarColumnBackgroundImage == null)
            {
                avatarColumnBackgroundImage = GetOrCreateChildImage(laneAvatarGutter, "AvatarColumnBackground");
                if (avatarColumnBackgroundImage != null)
                {
                    avatarColumnBackgroundImage.raycastTarget = false;
                    avatarColumnBackgroundImage.type = Image.Type.Simple;
                    avatarColumnBackgroundImage.preserveAspect = false;
                    // Only stretch-fill when creating a brand-new bg; keep scene insets if authored.
                    var keepScene = preserveSceneLayout ||
                                    (leftRailLayout != null && leftRailLayout.preserveSceneRects);
                    if (!keepScene)
                    {
                        var rt = avatarColumnBackgroundImage.rectTransform;
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;
                    }
                }
            }

            if (avatarColumnBackgroundImage == null)
            {
                return;
            }

            avatarColumnBackgroundImage.transform.SetAsFirstSibling();
            if (avatarColumnBackground != null)
            {
                avatarColumnBackgroundImage.sprite = avatarColumnBackground;
                avatarColumnBackgroundImage.color = new Color(
                    1f, 1f, 1f, Mathf.Clamp01(leftRailLayout.avatarColumnBackgroundAlpha));
                avatarColumnBackgroundImage.enabled = true;
            }
            else
            {
                avatarColumnBackgroundImage.enabled = false;
            }
        }

        private Sprite ResolveLaneAvatarFrame(CombatUnit unit)
        {
            var useBossFrame = unit != null &&
                               (unit.Role == UnitRole.Boss || unit.Side == GridSide.Enemy);
            if (useBossFrame && laneAvatarBossFrameSprite != null)
            {
                return laneAvatarBossFrameSprite;
            }

            return laneAvatarRingSprite;
        }

        private void LayoutLaneAvatarGutterFlushToViewport()
        {
            if (laneAvatarGutter == null)
            {
                return;
            }

            leftRailLayout ??= new LeftRailLayout();
            var gutterW = Mathf.Max(24f, leftRailLayout.avatarGutterWidth);

            laneAvatarGutter.SetParent(transform, false);
            laneAvatarGutter.localScale = Vector3.one;
            laneAvatarGutter.localRotation = Quaternion.identity;

            if (viewport == null)
            {
                laneAvatarGutter.anchorMin = new Vector2(0f, 0f);
                laneAvatarGutter.anchorMax = new Vector2(0f, 1f);
                laneAvatarGutter.pivot = new Vector2(1f, 0.5f);
                laneAvatarGutter.sizeDelta = new Vector2(gutterW, 0f);
                laneAvatarGutter.anchoredPosition = Vector2.zero;
                return;
            }

            Canvas.ForceUpdateCanvases();

            var parent = transform as RectTransform;
            var vpLocal = parent != null
                ? (Vector2)parent.InverseTransformPoint(viewport.TransformPoint(viewport.rect.min))
                : new Vector2(viewport.anchoredPosition.x - viewport.rect.width * viewport.pivot.x, 0f);
            var vpLocalMax = parent != null
                ? (Vector2)parent.InverseTransformPoint(viewport.TransformPoint(viewport.rect.max))
                : vpLocal + viewport.rect.size;

            var vpLeft = vpLocal.x;
            var vpBottom = vpLocal.y;
            var vpHeight = Mathf.Max(1f, vpLocalMax.y - vpLocal.y);

            laneAvatarGutter.anchorMin = new Vector2(0f, 0f);
            laneAvatarGutter.anchorMax = new Vector2(0f, 0f);
            laneAvatarGutter.pivot = new Vector2(1f, 0f);
            laneAvatarGutter.sizeDelta = new Vector2(gutterW, vpHeight);
            laneAvatarGutter.anchoredPosition = new Vector2(vpLeft, vpBottom);
        }

        /// <summary>
        /// Map Lane_* world Y → bottom-anchored Y inside a viewport layer (LaneFootprint / LaneMarkers).
        /// LaneLines has Top/Bottom insets so formula GetLaneYFromBottom is not the same space.
        /// </summary>
        private float ResolveLaneYInLayer(RectTransform layer, int laneIndex, float viewportHeight)
        {
            if (layer != null &&
                laneIndex >= 0 &&
                laneIndex < _laneLines.Count &&
                _laneLines[laneIndex] != null)
            {
                Canvas.ForceUpdateCanvases();
                if (layer.rect.height > 1f)
                {
                    var world = _laneLines[laneIndex].TransformPoint(Vector3.zero);
                    var localY = layer.InverseTransformPoint(world).y;
                    // Children use bottom-left anchors (0,0): anchoredPosition.y from layer bottom.
                    return localY - layer.rect.yMin;
                }
            }

            return GetLaneYFromBottom(laneIndex, viewportHeight);
        }

        /// <summary>
        /// Capture authored vertical band once: boss rail (max) → Lane_3 / SLOT 4 (min).
        /// Party Lane_* Y is redistributed and must not redefine the band after capture.
        /// </summary>
        private void CaptureSceneLaneBandIfNeeded()
        {
            if (_sceneLaneBandCaptured || viewport == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            if (_bossTrackFrame == null)
            {
                _bossTrackFrame = FindTimelineRect("BossTrackFrame");
                if (_bossTrackFrame != null)
                {
                    _bossTrackFrameAuthoredInScene = true;
                }
            }

            if (_laneLinesLayer == null)
            {
                _laneLinesLayer = viewport.Find("LaneLines") as RectTransform;
                if (_laneLinesLayer != null)
                {
                    _laneLinesLayerAuthoredInScene = true;
                }
            }

            if (_bossTrackFrame == null)
            {
                return;
            }

            var maxY = GetViewportBottomY(_bossTrackFrame);

            // Bottom band = authored Slot 4 / Lane_3 (scene SoT), not Viewport / LaneLines edge.
            RectTransform slot4 = null;
            if (_laneLinesLayer != null)
            {
                slot4 = _laneLinesLayer.Find("Lane_3") as RectTransform;
            }

            if (slot4 == null && viewport != null)
            {
                slot4 = viewport.Find("LaneLines/Lane_3") as RectTransform;
            }

            if (slot4 == null)
            {
                return;
            }

            var minY = GetViewportBottomY(slot4);

            if (maxY - minY < 8f)
            {
                // No usable authored band — leave uncaptured; ResolveLaneBand uses serialized fallback.
                return;
            }

            _sceneLaneBandMinY = minY;
            _sceneLaneBandMaxY = maxY;
            _sceneLaneBandCaptured = true;
        }

        private float GetViewportBottomY(RectTransform rt)
        {
            if (rt == null || viewport == null)
            {
                return 0f;
            }

            var world = rt.TransformPoint(Vector3.zero);
            var local = viewport.InverseTransformPoint(world);
            return local.y - viewport.rect.yMin;
        }

        /// <summary>
        /// Set a bottom-anchored (or viewport absolute) rect's Y so its pivot sits at viewport bottom-space Y.
        /// </summary>
        private void SetRectYFromViewportBottom(RectTransform rt, float yFromBottom)
        {
            if (rt == null || viewport == null)
            {
                return;
            }

            var parent = rt.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            var world = viewport.TransformPoint(new Vector3(0f, viewport.rect.yMin + yFromBottom, 0f));
            var local = parent.InverseTransformPoint(world);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, local.y - parent.rect.yMin);
        }

        private void ResolveLaneBand(float height, out float minY, out float maxY)
        {
            if (_sceneLaneBandCaptured)
            {
                minY = _sceneLaneBandMinY;
                maxY = _sceneLaneBandMaxY;
                return;
            }

            minY = height * Mathf.Min(laneBandMinNormalizedY, laneBandMaxNormalizedY);
            var railY = ResolveNoteRailAnchoredY(height);
            maxY = Mathf.Max(minY + 8f, railY);
            if (!preserveSceneLayout)
            {
                maxY = Mathf.Max(minY + 8f, railY - Mathf.Max(8f, laneGapBelowRail));
            }
        }

        /// <summary>
        /// Staff lines across band [minY, maxY]:
        /// N=1 → boss at max, party at midpoint; N≥2 → even N+1 slots (boss + party).
        /// </summary>
        private void ResolveEvenStaffYs(
            int partyCount,
            float height,
            out float bossY,
            out float[] partyYs)
        {
            partyCount = Mathf.Clamp(partyCount, 0, MaxTimelinePartyLanes);
            partyYs = new float[Mathf.Max(0, partyCount)];
            ResolveLaneBand(height, out var minY, out var maxY);

            bossY = maxY;
            if (partyCount <= 0)
            {
                return;
            }

            // Single survivor: center between monster line and timeline bottom.
            if (partyCount == 1)
            {
                partyYs[0] = (maxY + minY) * 0.5f;
                return;
            }

            // N≥2: evenly space N+1 lines including the monster rail.
            var totalSlots = partyCount + 1;
            for (var slot = 0; slot < totalSlots; slot++)
            {
                var t = (float)slot / (totalSlots - 1);
                var y = Mathf.Lerp(maxY, minY, t);
                if (slot == 0)
                {
                    bossY = y;
                }
                else
                {
                    partyYs[slot - 1] = y;
                }
            }
        }

        private float GetLaneYFromBottom(int laneIndex, float height)
        {
            var count = Mathf.Clamp(_laneUnits.Count > 0 ? _laneUnits.Count : _laneLines.Count, 0, MaxTimelinePartyLanes);
            ResolveEvenStaffYs(count, height, out _, out var partyYs);
            if (laneIndex < 0 || laneIndex >= partyYs.Length)
            {
                ResolveLaneBand(height, out var minY, out var maxY);
                return (minY + maxY) * 0.5f;
            }

            return partyYs[laneIndex];
        }

        /// <summary>
        /// Note rail Y (BorderTop). Boss notes pin their head belly to this line.
        /// </summary>
        private float GetNoteCoverYFromBottom(float viewportHeight) =>
            ResolveNoteRailAnchoredY(viewportHeight);

        /// <summary>
        /// Prefer authored BossTrackFrame Y, then even-layout boss Y, then serialized fallbacks.
        /// </summary>
        private float ResolveNoteRailAnchoredY(float viewportHeight)
        {
            if (preserveSceneLayout)
            {
                CaptureBossTrackFrameSceneRect();
                if (_bossTrackFrameLock.Has && Mathf.Abs(_bossTrackFrameLock.Y) > 0.01f)
                {
                    return _bossTrackFrameLock.Y;
                }

                if (_bossTrackFrame == null && viewport != null)
                {
                    _bossTrackFrame = bossTrackFrame != null
                        ? bossTrackFrame
                        : FindTimelineRect("BossTrackFrame");
                    if (_bossTrackFrame != null)
                    {
                        _bossTrackFrameAuthoredInScene = true;
                        CaptureBossTrackFrameSceneRect();
                    }
                }

                if (_bossTrackFrameAuthoredInScene
                    && _bossTrackFrame != null
                    && Mathf.Abs(_bossTrackFrame.anchoredPosition.y) > 0.01f)
                {
                    return _bossTrackFrame.anchoredPosition.y;
                }
            }

            if (_layoutBossRailY > 1f)
            {
                return _layoutBossRailY;
            }

            if (bossNoteRailAnchoredY > 1f)
            {
                return bossNoteRailAnchoredY;
            }

            return viewportHeight > 1f ? viewportHeight * noteBandNormalizedY : 215f;
        }

        private float ContentXForBeat(int beat)
        {
            if (_slotOffsetPx == null || _slotWidths == null || beat < 0 || beat >= TotalBeats)
            {
                return 0f;
            }

            return _slotOffsetPx[beat] + _slotWidths[beat] * 0.5f;
        }

        private float BeatWidthForBeat(int beat)
        {
            if (_slotWidths == null || beat < 0 || beat >= TotalBeats)
            {
                return 0f;
            }

            return _slotWidths[beat];
        }

        private float ContentXForBeatFloat(float beat)
        {
            if (_slotOffsetPx == null || _slotWidths == null || TotalBeats <= 0)
            {
                return 0f;
            }

            beat = Mathf.Clamp(beat, 0f, TotalBeats - 1);
            var i = Mathf.FloorToInt(beat);
            if (i >= TotalBeats - 1)
            {
                return ContentXForBeat(TotalBeats - 1);
            }

            var frac = beat - i;
            return Mathf.Lerp(ContentXForBeat(i), ContentXForBeat(i + 1), frac);
        }

        private float ContentXForActiveCenter(SkillDefinitionSO skill, int placementBeat) =>
            ContentXForBeatFloat(SkillFootprintUtil.GetActiveVisualCenterBeat(skill, placementBeat));

        private float FindBeatFloatAtContentPos(float contentPos)
        {
            if (_slotOffsetPx == null || _slotWidths == null || TotalBeats <= 0)
            {
                return 0f;
            }

            if (contentPos <= _slotOffsetPx[0])
            {
                return 0f;
            }

            for (var i = 0; i < TotalBeats; i++)
            {
                var end = _slotOffsetPx[i] + _slotWidths[i];
                if (contentPos < end)
                {
                    if (_slotWidths[i] <= 0f)
                    {
                        return i;
                    }

                    var t = (contentPos - _slotOffsetPx[i]) / _slotWidths[i];
                    return i + Mathf.Clamp01(t);
                }
            }

            return TotalBeats - 1;
        }

        private void ClearLaneMarkers()
        {
            foreach (var kvp in _laneMarkers)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }

            _laneMarkers.Clear();
        }

        public void RefreshLaneMarkers()
        {
            EnsureLaneLayers();
            EnsureLanesUpToDate();
            EnsureBossTrackFrame();

            if (_laneMarkersLayer == null || _timeline == null || viewport == null)
            {
                return;
            }

            var height = viewport.rect.height;
            var wanted = new HashSet<(CombatUnit unit, int beat)>();
            var wantedFootprint = new HashSet<(CombatUnit unit, int beat)>();

            foreach (var entry in _timeline.Agenda)
            {
                if (entry?.Unit == null || entry.Unit.Side != GridSide.Player || entry.Skill == null)
                {
                    continue;
                }

                if (!_laneIndex.TryGetValue(entry.Unit, out var laneIdx))
                {
                    continue;
                }

                var beat = entry.BeatIndex;
                if (beat < 0 || beat >= TotalBeats)
                {
                    continue;
                }

                var laneY = ResolveLaneYInLayer(_footprintLayer, laneIdx, height);
                var markerLaneY = ResolveLaneYInLayer(_laneMarkersLayer, laneIdx, height);
                var key = (entry.Unit, beat);
                var useSkillNotes = SkillTimelineNoteResolver.ResolveActive(entry.Skill) != null;

                if (!useSkillNotes)
                {
                    wanted.Add(key);

                    var pos = new Vector2(ContentXForActiveCenter(entry.Skill, beat), markerLaneY);
                    var gapAnchor = SkillFootprintUtil.UsesGapCenterAnchor(entry.Skill);
                    if (_laneMarkers.TryGetValue(key, out var existing) && existing != null)
                    {
                        existing.SetRelocateVisualHidden(false);
                        existing.gameObject.SetActive(true);
                        existing.SetGapAnchorMode(gapAnchor);
                        existing.SetContent(entry.Unit, entry.Skill);
                        existing.SetLanePosition(pos, false);
                    }
                    else
                    {
                        var marker = CreateLaneMarker(entry.Unit, beat);
                        marker.SetGapAnchorMode(gapAnchor);
                        marker.SetContent(entry.Unit, entry.Skill);
                        marker.SetLanePosition(pos, true);
                        _laneMarkers[key] = marker;
                    }
                }

                RefreshFootprintDots(entry, laneY, wantedFootprint);
            }

            ReconcileLaneMarkers(wanted);
            ReconcileFootprintDots(wantedFootprint);
            RefreshLaneMarkerDragWiring();

            SyncLaneMarkersScroll();
        }

        /// <summary>S1/S2 xám nhỏ · mọi beat S = tròn to trên lane (hoặc nốt skill nếu có sprite).</summary>
        private void RefreshFootprintDots(AgendaEntry entry, float laneY, HashSet<(CombatUnit unit, int beat)> wanted)
        {
            var skill = entry.Skill;
            var placement = entry.BeatIndex;
            var unitColor = entry.Unit.TimelineLaneColor;
            var activeSprite = SkillTimelineNoteResolver.ResolveActive(skill);
            var standingSprite = SkillTimelineNoteResolver.ResolveStanding(skill);
            var activeSize = activeSprite != null
                ? SkillTimelineNoteResolver.ResolveActiveSize(skill, activeFootprintDotSize)
                : activeFootprintDotSize;
            var standingSize = standingSprite != null
                ? SkillTimelineNoteResolver.ResolveStandingSize(skill, footprintDotSize)
                : footprintDotSize;

            foreach (var info in SkillFootprintUtil.EnumerateFootprintBeats(skill, placement, null, entry))
            {
                if (info.BeatIndex < 0 || info.BeatIndex >= TotalBeats)
                {
                    continue;
                }

                if (info.Role == FootprintBeatRole.Active)
                {
                    if (activeSprite != null)
                    {
                        TryPlaceFootprintDot(entry.Unit, info.BeatIndex, laneY, Color.white, wanted, activeSize,
                            placement, enableDrag: true, activeSprite);
                    }
                    else
                    {
                        var color = new Color(unitColor.r, unitColor.g, unitColor.b, 0.95f);
                        TryPlaceFootprintDot(entry.Unit, info.BeatIndex, laneY, color, wanted, activeSize,
                            placement, enableDrag: true);
                    }

                    continue;
                }

                if (standingSprite != null)
                {
                    TryPlaceFootprintDot(entry.Unit, info.BeatIndex, laneY, Color.white, wanted, standingSize,
                        placement, enableDrag: false, standingSprite);
                }
                else
                {
                    TryPlaceFootprintDot(entry.Unit, info.BeatIndex, laneY, StandingDotColor, wanted, standingSize,
                        placement, enableDrag: false);
                }
            }
        }

        private void TryPlaceFootprintDot(CombatUnit unit, int beat, float laneY, Color color,
            HashSet<(CombatUnit unit, int beat)> wanted, float size, int placementBeat, bool enableDrag,
            Sprite sprite = null)
        {
            if (beat < 0 || beat >= TotalBeats)
            {
                return;
            }

            var key = (unit, beat);
            wanted.Add(key);

            var pos = new Vector2(ContentXForBeat(beat), laneY);
            if (_footprintDots.TryGetValue(key, out var dot) && dot != null)
            {
                ApplyFootprintVisual(dot, color, size, sprite);
                dot.rectTransform.anchoredPosition = pos;
                ConfigureFootprintDotInteraction(dot, unit, placementBeat, enableDrag);
                return;
            }

            var created = CreateFootprintDot(size);
            ApplyFootprintVisual(created, color, size, sprite);
            created.rectTransform.anchoredPosition = pos;
            _footprintDots[key] = created;
            ConfigureFootprintDotInteraction(created, unit, placementBeat, enableDrag);
        }

        private static void ApplyFootprintVisual(Image image, Color color, float size, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            image.rectTransform.sizeDelta = new Vector2(size, size);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = color;
                image.preserveAspect = true;
                return;
            }

            image.sprite = UiCircleSpriteUtil.Circle;
            image.color = color;
            image.preserveAspect = false;
        }

        private void ConfigureFootprintDotInteraction(Image dot, CombatUnit unit, int placementBeat, bool enableDrag)
        {
            if (dot == null)
            {
                return;
            }

            if (!enableDrag)
            {
                dot.raycastTarget = false;
                var staleHandle = dot.GetComponent<TimelineLaneSkillDragHandle>();
                if (staleHandle != null)
                {
                    Destroy(staleHandle);
                }

                return;
            }

            dot.raycastTarget = CanRelocateLaneMarker();
            var handle = dot.GetComponent<TimelineLaneSkillDragHandle>();
            if (handle == null)
            {
                handle = dot.gameObject.AddComponent<TimelineLaneSkillDragHandle>();
            }

            handle.Configure(this, unit, placementBeat);
            handle.SetInteractionEnabled(CanRelocateLaneMarker());
        }

        private void ReconcileLaneMarkers(HashSet<(CombatUnit unit, int beat)> wanted)
        {
            var stale = new List<(CombatUnit unit, int beat)>();
            foreach (var kvp in _laneMarkers)
            {
                if (!wanted.Contains(kvp.Key))
                {
                    stale.Add(kvp.Key);
                }
            }

            foreach (var key in stale)
            {
                if (_relocatePendingKey is { } pending && pending == key)
                {
                    continue;
                }

                if (_laneMarkers.TryGetValue(key, out var marker) && marker != null)
                {
                    Destroy(marker.gameObject);
                }

                _laneMarkers.Remove(key);
            }
        }

        private void ReconcileFootprintDots(HashSet<(CombatUnit unit, int beat)> wanted)
        {
            var stale = new List<(CombatUnit unit, int beat)>();
            foreach (var kvp in _footprintDots)
            {
                if (!wanted.Contains(kvp.Key))
                {
                    stale.Add(kvp.Key);
                }
            }

            foreach (var key in stale)
            {
                if (_relocatePendingKey is { } pending && pending.unit == key.unit)
                {
                    continue;
                }

                if (_footprintDots.TryGetValue(key, out var dot) && dot != null)
                {
                    Destroy(dot.gameObject);
                }

                _footprintDots.Remove(key);
            }
        }

        private Image CreateFootprintDot(float size)
        {
            var go = new GameObject("FootprintDot", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_footprintLayer, false);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);

            var img = go.AddComponent<Image>();
            img.sprite = UiCircleSpriteUtil.Circle;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            return img;
        }

        private Image CreateFootprintDot()
        {
            return CreateFootprintDot(footprintDotSize);
        }

        private void ClearFootprintDots()
        {
            foreach (var kvp in _footprintDots)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }

            _footprintDots.Clear();
        }

        private TimelineLaneMarkerView CreateLaneMarker(CombatUnit unit, int placementBeat, bool enableDrag = true)
        {
            var go = new GameObject("LaneMarker", typeof(RectTransform));
            var marker = go.AddComponent<TimelineLaneMarkerView>();
            marker.Build(_laneMarkersLayer, laneMarkerSize);
            marker.SetPlanningInteractionEnabled(enableDrag);

            if (enableDrag)
            {
                WireLaneMarkerDrag(marker, unit, placementBeat);
            }

            return marker;
        }

        private void WireLaneMarkerDrag(TimelineLaneMarkerView marker, CombatUnit unit, int placementBeat)
        {
            marker?.WireSkillDrag(this, unit, placementBeat);
        }

        private void RefreshLaneMarkerDragWiring()
        {
            var canRelocate = CanRelocateLaneMarker();

            foreach (var kvp in _laneMarkers)
            {
                if (kvp.Value == null || kvp.Value == _dropGhost)
                {
                    continue;
                }

                WireLaneMarkerDrag(kvp.Value, kvp.Key.unit, kvp.Key.beat);
            }

            foreach (var kvp in _footprintDots)
            {
                if (kvp.Value == null)
                {
                    continue;
                }

                var handle = kvp.Value.GetComponent<TimelineLaneSkillDragHandle>();
                if (handle == null)
                {
                    continue;
                }

                kvp.Value.raycastTarget = canRelocate;
                handle.SetInteractionEnabled(canRelocate);
            }
        }

        private void EnsureBlockBarrierLayer()
        {
            if (_blockBarrierLayer != null || viewport == null)
            {
                return;
            }

            var go = new GameObject("BlockBarrierLayer", typeof(RectTransform));
            _blockBarrierLayer = go.GetComponent<RectTransform>();
            _blockBarrierLayer.SetParent(viewport, false);
            _blockBarrierLayer.anchorMin = Vector2.zero;
            _blockBarrierLayer.anchorMax = Vector2.one;
            _blockBarrierLayer.offsetMin = Vector2.zero;
            _blockBarrierLayer.offsetMax = Vector2.zero;
            _blockBarrierLayer.SetAsLastSibling();
        }

        private void RefreshBlockBarriers()
        {
            EnsureBlockBarrierLayer();
            if (_blockBarrierLayer == null || _blockBarriers == null || !_slotsBuilt)
            {
                return;
            }

            _blockBarrierLayer.SetAsLastSibling();

            foreach (var img in _blockBarrierViews)
            {
                if (img != null)
                {
                    Destroy(img.gameObject);
                }
            }

            _blockBarrierViews.Clear();

            var height = viewport != null ? viewport.rect.height : 200f;
            foreach (var barrier in _blockBarriers.Barriers)
            {
                if (barrier.BeatIndex < 0 || barrier.BeatIndex >= TotalBeats)
                {
                    continue;
                }

                var go = new GameObject($"Block_{barrier.BeatIndex}", typeof(RectTransform));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(_blockBarrierLayer, false);
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(14f, height * 0.95f);
                rect.anchoredPosition = new Vector2(ContentXForBeat(barrier.BeatIndex), height * 0.5f);

                var img = go.AddComponent<Image>();
                img.color = new Color(0.15f, 1f, 0.35f, 0.95f);
                img.raycastTarget = false;
                _blockBarrierViews.Add(img);
            }

            SyncBlockBarrierScroll();
        }

        private void SyncBlockBarrierScroll()
        {
            if (slotsRow == null)
            {
                return;
            }

            var x = slotsRow.anchoredPosition.x;
            if (_blockBarrierLayer != null)
            {
                _blockBarrierLayer.anchoredPosition = new Vector2(x, 0f);
            }

            if (_bossNoteClusterLayer != null)
            {
                _bossNoteClusterLayer.anchoredPosition = new Vector2(x, 0f);
            }
        }

        private void EnsureBossNoteClusterLayer()
        {
            if (viewport == null)
            {
                return;
            }

            if (_bossNoteClusterLayer == null)
            {
                var existing = viewport.Find("BossNoteClusterLayer") as RectTransform;
                if (existing != null)
                {
                    _bossNoteClusterLayer = existing;
                }
                else
                {
                    var go = new GameObject("BossNoteClusterLayer", typeof(RectTransform));
                    _bossNoteClusterLayer = go.GetComponent<RectTransform>();
                    _bossNoteClusterLayer.SetParent(viewport, false);
                    _bossNoteClusterLayer.anchorMin = Vector2.zero;
                    _bossNoteClusterLayer.anchorMax = Vector2.one;
                    _bossNoteClusterLayer.offsetMin = Vector2.zero;
                    _bossNoteClusterLayer.offsetMax = Vector2.zero;
                }
            }

            _bossNoteClusterLayer.SetAsLastSibling();

            _bossNoteClusters = _bossNoteClusterLayer.GetComponent<BossNoteClusterView>();
            if (_bossNoteClusters == null)
            {
                _bossNoteClusters = _bossNoteClusterLayer.gameObject.AddComponent<BossNoteClusterView>();
            }
        }

        private void RebuildBossNoteClusters()
        {
            EnsureBossNoteClusterLayer();
            if (_bossNoteClusters == null || viewport == null)
            {
                return;
            }

            var height = viewport.rect.height;
            var noteY = GetNoteCoverYFromBottom(height);
            _bossNoteClusters.Configure(
                _bossNoteClusterLayer,
                NoteVisuals,
                ContentXForBeat,
                noteY,
                bossNoteNumberLayout,
                BeatWidthForBeat);
            _bossNoteClusters.Rebuild(_timeline, height);
            SyncBlockBarrierScroll();
            OrderViewportLayers();
            BringResolveFeedbackToFront();
        }

        private void SyncLaneMarkersScroll()
        {
            if (slotsRow == null)
            {
                return;
            }

            var x = slotsRow.anchoredPosition.x;
            if (_laneMarkersLayer != null)
            {
                _laneMarkersLayer.anchoredPosition = new Vector2(x, _laneMarkersLayer.anchoredPosition.y);
            }

            if (_footprintLayer != null && !IsNestedInSlotsRow(_footprintLayer))
            {
                ApplySceneFollowScrollX(_footprintLayer, in _laneFootprintLock, x);
            }

            SyncBlockBarrierScroll();
        }

        public bool TryGetPlacementBeatAtScreenPoint(Vector2 screen, SkillDefinitionSO skill, out int placementBeat)
        {
            placementBeat = -1;

            if (skill == null || _laneMarkersLayer == null || viewport == null || !_slotsBuilt)
            {
                return false;
            }

            var cam = GetUiCameraForTimeline();
            if (!RectTransformUtility.RectangleContainsScreenPoint(viewport, screen, cam))
            {
                return false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_laneMarkersLayer, screen, cam, out var local))
            {
                return false;
            }

            var centerBeat = FindBeatFloatAtContentPos(local.x);
            placementBeat = SkillFootprintUtil.ResolvePlacementBeatFromCenter(skill, centerBeat);
            return placementBeat >= 0 && placementBeat < TotalBeats;
        }

        /// <summary>Screen point → beat index nếu con trỏ nằm trên timeline (dùng cho kéo-thả skill).</summary>
        public bool TryGetBeatAtScreenPoint(Vector2 screen, out int beat)
        {
            beat = -1;

            if (_laneMarkersLayer == null || viewport == null || !_slotsBuilt)
            {
                return false;
            }

            var cam = GetUiCameraForTimeline();
            if (!RectTransformUtility.RectangleContainsScreenPoint(viewport, screen, cam))
            {
                return false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_laneMarkersLayer, screen, cam, out var local))
            {
                return false;
            }

            var idx = FindSlotAtContentPos(local.x);
            if (idx < 0)
            {
                return false;
            }

            beat = idx;
            return true;
        }

        public void ShowDropGhost(CombatUnit unit, SkillDefinitionSO skill, Vector2 screen)
        {
            EnsureLaneLayers();
            HideDropGhost();

            if (_laneMarkersLayer == null || unit == null || skill == null || viewport == null)
            {
                return;
            }

            if (!_laneIndex.TryGetValue(unit, out var laneIdx)
                || !TryGetPlacementBeatAtScreenPoint(screen, skill, out var beat))
            {
                return;
            }

            var valid = false;
            if (_timeline != null)
            {
                valid = _timeline.CanAssignAction(unit, skill, beat);
                var hoverBeat = beat;
                if (!TryGetBeatAtScreenPoint(screen, out hoverBeat))
                {
                    hoverBeat = beat;
                }

                if (!valid
                    && _relocatePendingKey.HasValue
                    && _relocatePendingKey.Value.unit == unit
                    && _timeline.CanSwapRelocate(
                        unit, skill, _relocatePendingKey.Value.beat, beat, hoverBeat))
                {
                    valid = true;
                }

                if (!valid
                    && SkillFootprintUtil.TryGetEntryAtBeat(
                        _timeline.Agenda, unit, hoverBeat, out var victim, out _)
                    && victim != null
                    && _timeline.CanAssignAction(unit, skill, beat, victim))
                {
                    valid = true;
                }

                TintOccupiedSkillsOnDrop(unit, skill, beat, hoverBeat);
            }
            var gapAnchor = SkillFootprintUtil.UsesGapCenterAnchor(skill);
            var laneY = ResolveLaneYInLayer(_footprintLayer, laneIdx, viewport.rect.height);
            var markerLaneY = ResolveLaneYInLayer(_laneMarkersLayer, laneIdx, viewport.rect.height);
            var unitColor = unit.TimelineLaneColor;
            var previewAlpha = valid ? 0.55f : 0.35f;
            var centerX = ContentXForActiveCenter(skill, beat);
            var catalog = NoteVisuals;
            var skillActiveSprite = SkillTimelineNoteResolver.ResolveActive(skill);
            var skillStandingSprite = SkillTimelineNoteResolver.ResolveStanding(skill);
            var ghostSprite = skillActiveSprite != null ? skillActiveSprite : catalog.DropGhost(valid);
            var ghostSize = skillActiveSprite != null
                ? SkillTimelineNoteResolver.ResolveActiveSize(skill, catalog.GhostDisplaySize)
                : catalog.GhostDisplaySize;
            var coverSize = catalog.CoverDisplaySize;
            var noteCoverY = GetNoteCoverYFromBottom(viewport.rect.height);

            if (skillActiveSprite == null)
            {
                if (_dropGhost == null)
                {
                    _dropGhost = CreateLaneMarker(unit, beat, enableDrag: false);
                }

                _dropGhost.gameObject.SetActive(true);
                _dropGhost.SetGapAnchorMode(gapAnchor);
                _dropGhost.SetContent(unit, skill);
                _dropGhost.SetGhost(true);
                _dropGhost.SetLanePosition(new Vector2(centerX, markerLaneY), false);
                if (!valid && !gapAnchor)
                {
                    _dropGhost.SetInvalidPreview(true);
                }
            }
            else if (_dropGhost != null)
            {
                _dropGhost.gameObject.SetActive(false);
                _dropGhost.SetInvalidPreview(false);
            }

            foreach (var info in SkillFootprintUtil.EnumerateFootprintBeats(skill, beat, unit))
            {
                if (info.BeatIndex < 0 || info.BeatIndex >= TotalBeats)
                {
                    continue;
                }

                if (info.Role == FootprintBeatRole.Active)
                {
                    if (ghostSprite != null)
                    {
                        var tint = skillActiveSprite != null
                            ? new Color(1f, 1f, 1f, previewAlpha)
                            : Color.white;
                        AddDropPreviewSprite(info.BeatIndex, laneY, ghostSprite, ghostSize, tint);
                    }
                    else
                    {
                        var color = valid
                            ? new Color(unitColor.r, unitColor.g, unitColor.b, previewAlpha)
                            : new Color(1f, 0.25f, 0.2f, previewAlpha);
                        AddDropPreviewDot(info.BeatIndex, laneY, color, activeFootprintDotSize);
                    }

                    if (_timeline != null
                        && _timeline.GetImpactTelegraphAtBeat(info.BeatIndex) is { } telegraph)
                    {
                        var remainingAfter = CombatCounterResolver.GetRemainingHitsAfterPending(
                            telegraph, _timeline, skill, beat, unit);
                        if (remainingAfter <= 0)
                        {
                            var coverSprite = valid ? catalog.CoverPerfect : catalog.CoverMiss;
                            if (coverSprite != null)
                            {
                                AddDropCoverOverlay(info.BeatIndex, noteCoverY, coverSprite, coverSize);
                            }
                        }
                    }

                    continue;
                }

                if (skillStandingSprite != null)
                {
                    AddDropPreviewSprite(
                        info.BeatIndex,
                        laneY,
                        skillStandingSprite,
                        SkillTimelineNoteResolver.ResolveStandingSize(skill, footprintDotSize),
                        new Color(1f, 1f, 1f, previewAlpha));
                }
                else
                {
                    var standingColor = valid
                        ? new Color(StandingDotColor.r, StandingDotColor.g, StandingDotColor.b, previewAlpha)
                        : new Color(1f, 0.25f, 0.2f, previewAlpha);
                    AddDropPreviewDot(info.BeatIndex, laneY, standingColor, footprintDotSize);
                }
            }
        }

        private void AddDropPreviewSprite(int beat, float laneY, Sprite sprite, float size)
        {
            AddDropPreviewSprite(beat, laneY, sprite, size, Color.white);
        }

        private void AddDropPreviewSprite(int beat, float laneY, Sprite sprite, float size, Color tint)
        {
            var dot = CreateFootprintDot(size);
            ApplyFootprintVisual(dot, tint, size, sprite);
            dot.rectTransform.anchoredPosition = new Vector2(ContentXForBeat(beat), laneY);
            dot.rectTransform.SetAsLastSibling();
            dot.gameObject.SetActive(true);
            _dropPreviewDots.Add(dot);
        }

        private void AddDropCoverOverlay(int beat, float noteY, Sprite sprite, float size)
        {
            if (sprite == null)
            {
                return;
            }

            if (_bossNoteClusters != null &&
                _bossNoteClusters.TryAttachPerfectPreview(beat, sprite, out var attached) &&
                attached != null)
            {
                _dropCoverOverlays.Add(attached);
                return;
            }

            EnsureLaneLayers();
            if (_footprintLayer == null)
            {
                return;
            }

            var markSize = _bossNoteClusters != null
                ? _bossNoteClusters.GetPerfectMarkSizeForBeat(beat, preview: true)
                : new Vector2(Mathf.Max(36f, size), Mathf.Max(36f, size));

            var go = new GameObject("DropCover", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_footprintLayer, false);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = markSize;
            rect.anchoredPosition = new Vector2(ContentXForBeat(beat), noteY);
            rect.SetAsLastSibling();

            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.type = Image.Type.Simple;
            _dropCoverOverlays.Add(img);
        }

        private void AddDropPreviewDot(int beat, float laneY, Color color, float size)
        {
            var dot = CreateFootprintDot(size);
            dot.color = color;
            dot.rectTransform.anchoredPosition = new Vector2(ContentXForBeat(beat), laneY);
            dot.rectTransform.SetAsLastSibling();
            dot.gameObject.SetActive(true);
            _dropPreviewDots.Add(dot);
        }

        private void AddDropPreviewDot(int beat, float laneY, Color color)
        {
            AddDropPreviewDot(beat, laneY, color, footprintDotSize);
        }

        public void HideDropGhost()
        {
            if (_dropGhost != null)
            {
                _dropGhost.gameObject.SetActive(false);
                _dropGhost.SetInvalidPreview(false);
            }

            foreach (var dot in _dropPreviewDots)
            {
                if (dot != null)
                {
                    Destroy(dot.gameObject);
                }
            }

            _dropPreviewDots.Clear();

            foreach (var cover in _dropCoverOverlays)
            {
                if (cover != null)
                {
                    Destroy(cover.gameObject);
                }
            }

            _dropCoverOverlays.Clear();
            _bossNoteClusters?.EndPerfectPreview();
            ClearOccupiedSkillDropTint();
        }

        private void TintOccupiedSkillsOnDrop(
            CombatUnit unit,
            SkillDefinitionSO skill,
            int placementBeat,
            int hoverBeat)
        {
            ClearOccupiedSkillDropTint();
            if (_timeline?.Agenda == null || unit == null || skill == null)
            {
                return;
            }

            if (SkillFootprintUtil.TryGetEntryAtBeat(
                    _timeline.Agenda, unit, hoverBeat, out var hovered, out _)
                && hovered?.Skill != null)
            {
                ApplyOccupiedSkillDropTint(hovered);
            }

            foreach (var entry in _timeline.Agenda)
            {
                if (entry?.Unit != unit || entry.Skill == null || entry == hovered)
                {
                    continue;
                }

                if (SkillFootprintUtil.FootprintsOverlap(
                        skill, placementBeat, unit, null,
                        entry.Skill, entry.BeatIndex, entry.Unit, entry))
                {
                    ApplyOccupiedSkillDropTint(entry);
                }
            }
        }

        private void ApplyOccupiedSkillDropTint(AgendaEntry entry)
        {
            if (entry?.Unit == null || entry.Skill == null)
            {
                return;
            }

            foreach (var info in SkillFootprintUtil.EnumerateFootprintBeats(
                entry.Skill, entry.BeatIndex, entry.Unit, entry))
            {
                var key = (entry.Unit, info.BeatIndex);
                if (!_footprintDots.TryGetValue(key, out var dot) || dot == null)
                {
                    continue;
                }

                if (!_overlapTintSaved.ContainsKey(key))
                {
                    _overlapTintSaved[key] = dot.color;
                }

                var saved = _overlapTintSaved[key];
                if (saved.a <= 0.01f)
                {
                    continue;
                }

                dot.color = new Color(DropOverlapTint.r, DropOverlapTint.g, DropOverlapTint.b, 1f);
            }

            if (_laneMarkers.TryGetValue((entry.Unit, entry.BeatIndex), out var marker) && marker != null)
            {
                marker.SetOverlapTint(DropOverlapTint, true);
                if (!_overlapTintedMarkers.Contains(marker))
                {
                    _overlapTintedMarkers.Add(marker);
                }
            }
        }

        private void ClearOccupiedSkillDropTint()
        {
            foreach (var kvp in _overlapTintSaved)
            {
                if (_footprintDots.TryGetValue(kvp.Key, out var dot) && dot != null)
                {
                    dot.color = kvp.Value;
                }
            }

            _overlapTintSaved.Clear();

            foreach (var marker in _overlapTintedMarkers)
            {
                marker?.SetOverlapTint(DropOverlapTint, false);
            }

            _overlapTintedMarkers.Clear();
        }

        private Camera GetUiCameraForTimeline()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return null;
            }

            canvas = canvas.rootCanvas;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        private void ClearHighlightedSlot()
        {
            if (_lastHighlightedSlotIndex >= 0 &&
                _slots != null &&
                _lastHighlightedSlotIndex < _slots.Length)
            {
                _slots[_lastHighlightedSlotIndex]?.SetScanIntensity(0f);
            }

            _lastHighlightedSlotIndex = -1;
        }

        private void ResetAllScanHighlights()
        {
            ClearHighlightedSlot();

            if (_slots == null)
            {
                return;
            }

            foreach (var slot in _slots)
            {
                slot?.ResetScanHighlight();
            }
        }

        private float GetScanLineX()
        {
            if (preserveSceneLayout && _scanBarHomeCaptured)
            {
                return _scanBarHomeAnchoredX;
            }

            if (preserveSceneLayout && scanBar != null)
            {
                return scanBar.anchoredPosition.x;
            }

            return TimelineLayoutLock.ClampSlotWidth(slotWidth) * 0.5f;
        }

        private void EnsureTrackLine()
        {
            if (viewport == null)
            {
                return;
            }

            if (trackLine == null)
            {
                var existing = viewport.Find("TrackLine");
                if (existing != null)
                {
                    trackLine = existing as RectTransform;
                }
            }

            if (trackLine == null)
            {
                var go = new GameObject("TrackLine", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(viewport, false);
                go.transform.SetAsFirstSibling();
                trackLine = go.GetComponent<RectTransform>();
                var img = go.GetComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.14f);
                img.raycastTarget = false;
                ApplyTrackLineLayout();
                return;
            }

            if (!preserveSceneLayout || TrackLineNeedsFunctionalLayout(trackLine))
            {
                ApplyTrackLineLayout();
            }
        }

        private static bool TrackLineNeedsFunctionalLayout(RectTransform line)
        {
            if (line == null)
            {
                return false;
            }

            var rect = line.rect;
            if (rect.height > 4f)
            {
                return true;
            }

            return line.parent is RectTransform parent && parent.rect.width > 1f &&
                rect.width < parent.rect.width * 0.9f;
        }

        private void ApplyTrackLineLayout()
        {
            if (trackLine == null)
            {
                return;
            }

            trackLine.anchorMin = new Vector2(0f, 0f);
            trackLine.anchorMax = new Vector2(1f, 0f);
            trackLine.pivot = new Vector2(0.5f, 0f);
            trackLine.anchoredPosition = new Vector2(0f, TimelineLayoutLock.TrackLineY);
            trackLine.sizeDelta = new Vector2(0f, TimelineLayoutLock.TrackLineHeight);
        }

        private float GetViewportWidth()
        {
            if (viewport == null)
            {
                return 0f;
            }

            var width = viewport.rect.width;
            if (width <= 1f)
            {
                Canvas.ForceUpdateCanvases();
                width = viewport.rect.width;
            }

            return width;
        }

        private float ReadTemplateSlotWidthRaw()
        {
            if (segmentTemplate == null)
            {
                return 0f;
            }

            var layoutElement = segmentTemplate.GetComponent<LayoutElement>();
            if (layoutElement != null && layoutElement.preferredWidth > 0f)
            {
                return layoutElement.preferredWidth;
            }

            if (segmentTemplate.TryGetComponent<RectTransform>(out var rect))
            {
                var stretchX = Mathf.Abs(rect.anchorMax.x - rect.anchorMin.x) > 0.01f;
                if (!stretchX && rect.sizeDelta.x > 0f)
                {
                    return rect.sizeDelta.x;
                }
            }

            return 0f;
        }

        private float ResolveLockedSlotWidth()
        {
            return TimelineLayoutLock.ResolveSlotWidth(
                ReadTemplateSlotWidthRaw(),
                slotWidth,
                preserveSceneLayout);
        }

        /// <summary>Editor menu / scene sync — rebuild beat slots and widths for current viewport.</summary>
        public void ForceRefitViewportSlots()
        {
            RebuildLayout();
        }

        private void RebuildLayout()
        {
            if (viewport == null || slotsRow == null || segmentTemplate == null)
            {
                return;
            }

            AlignSlotsRowInViewport();
            EnsureViewportMask();
            AlignScanBar();

            slotWidth = ResolveLockedSlotWidth();
            minSlotWidth = Mathf.Max(minSlotWidth, TimelineLayoutLock.MinSlotWidth);

            SyncSegmentFromSession();

            if (!_slotsBuilt || _slots == null || _slots.Length != UiSlotCount)
            {
                BuildAllSlots();
            }

            _pixelsPerSecond = ComputePixelsPerSecond();

            // Virtual song-length layout (floats only) keeps scroll/music math absolute.
            if (_slotWidths == null || _slotWidths.Length != TotalBeats)
            {
                _slotWidths = new float[TotalBeats];
            }

            if (_slotOffsetPx == null || _slotOffsetPx.Length != TotalBeats + 1)
            {
                _slotOffsetPx = new float[TotalBeats + 1];
            }

            DisableSlotsRowLayoutGroup();

            var uniformWidth = TimelineLayoutLock.ClampSlotWidth(slotWidth);
            var cumulative = 0f;
            for (var i = 0; i < TotalBeats; i++)
            {
                _slotWidths[i] = uniformWidth;
                _slotOffsetPx[i] = cumulative;
                cumulative += uniformWidth;
            }

            _slotOffsetPx[TotalBeats] = cumulative;
            _contentWidthPx = cumulative;

            if (!preserveSceneLayout || !_scrollContentAuthoredInScene)
            {
                ApplySceneContentWidth(slotsRow, in _scrollContentLock, cumulative);
            }
            RebindWindowSlotRects();
            EnsurePhaseDividers();

            _lastViewportWidth = viewport.rect.width;

            EnsureLaneLayers();
            LayoutLanes();
            EnsureLaneAvatarColumn();
            if (_timeline != null)
            {
                RefreshLaneMarkers();
            }
        }

        private void ApplySlotRect(BeatSegmentView slot, float width, float xOffset)
        {
            if (slot == null)
            {
                return;
            }

            width = TimelineLayoutLock.ClampSlotWidth(width);

            var layoutElement = slot.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = true;
                layoutElement.minWidth = -1f;
                layoutElement.preferredWidth = width;
                layoutElement.flexibleWidth = -1f;
            }

            var rt = slot.transform as RectTransform;
            if (rt == null)
            {
                return;
            }

            rt.localScale = Vector3.one;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(xOffset, 0f);
            rt.sizeDelta = new Vector2(width, 0f);
        }

        private void EnsurePhaseDividers()
        {
            if (phaseDividerTemplate == null && slotsRow != null)
            {
                phaseDividerTemplate = slotsRow.Find("PhaseDivider") as RectTransform;
            }

            if (phaseDividerTemplate == null || slotsRow == null)
            {
                return;
            }

            if (!Application.isPlaying)
            {
                TimelineConstants.GetPhaseBeatRange(0, out _, out var count);
                var lastBeat = Mathf.Max(0, count - 1);
                ApplyPhaseDividerRect(phaseDividerTemplate, GetPhaseDividerContentPx(lastBeat));
                phaseDividerTemplate.gameObject.SetActive(true);
                return;
            }

            if (_phaseDividers != null && _phaseDividers.Length == TimelineConstants.UiVisiblePhaseCount)
            {
                LayoutPhaseDividers();
                return;
            }

            var templateGo = phaseDividerTemplate.gameObject;
            var wasActive = templateGo.activeSelf;
            templateGo.SetActive(true);

            _phaseDividers = new RectTransform[TimelineConstants.UiVisiblePhaseCount];
            for (var i = 0; i < _phaseDividers.Length; i++)
            {
                var cloneGo = Instantiate(templateGo, slotsRow);
                cloneGo.name = $"PhaseDivider_{i}";
                cloneGo.SetActive(true);
                MarkRuntimeClone(cloneGo);
                _phaseDividers[i] = cloneGo.transform as RectTransform;
            }

            if (Application.isPlaying)
            {
                templateGo.SetActive(false);
            }
            else
            {
                templateGo.SetActive(wasActive);
            }

            LayoutPhaseDividers();
        }

        private void LayoutPhaseDividers()
        {
            if (_phaseDividers == null || _phaseDividers.Length == 0)
            {
                return;
            }

            var shown = 0;
            var windowEnd = _windowStartBeat + UiSlotCount;
            var phaseCount = TimelineConstants.PhaseCount;
            for (var phase = 0; phase < phaseCount && shown < _phaseDividers.Length; phase++)
            {
                TimelineConstants.GetPhaseBeatRange(phase, out var startBeat, out var count);
                if (count <= 0)
                {
                    continue;
                }

                var lastBeat = startBeat + count - 1;
                if (!TimelineConstants.IsPhaseDividerAfter(lastBeat)
                    || lastBeat < _windowStartBeat
                    || lastBeat >= windowEnd)
                {
                    continue;
                }

                var rt = _phaseDividers[shown];
                if (rt != null)
                {
                    ApplyPhaseDividerRect(rt, GetPhaseDividerContentPx(lastBeat));
                    rt.gameObject.SetActive(true);
                }

                shown++;
            }

            for (var i = shown; i < _phaseDividers.Length; i++)
            {
                if (_phaseDividers[i] != null)
                {
                    _phaseDividers[i].gameObject.SetActive(false);
                }
            }
        }

        private void ApplyPhaseDividerRect(RectTransform rt, float x)
        {
            if (rt == null)
            {
                return;
            }

            rt.localScale = Vector3.one;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(PhaseDividerWidthPx, 0f);
            rt.anchoredPosition = new Vector2(x, 0f);

            var layoutElement = rt.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = rt.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.ignoreLayout = true;
            layoutElement.minWidth = -1f;
            layoutElement.preferredWidth = PhaseDividerWidthPx;
            layoutElement.flexibleWidth = -1f;

            var image = rt.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
            }
        }

        private void DisableSlotsRowLayoutGroup()
        {
            if (slotsRow == null)
            {
                return;
            }

            var layout = slotsRow.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.enabled = false;
            }

            var fitter = slotsRow.GetComponent<ContentSizeFitter>();
            if (fitter != null)
            {
                fitter.enabled = false;
            }
        }

        private void BuildAllSlots()
        {
            CleanupExtraBeatChildren();
            AlignSlotsRowInViewport();
            DisableSlotsRowLayoutGroup();

            slotWidth = ResolveLockedSlotWidth();
            var templateWidth = TimelineLayoutLock.ClampSlotWidth(slotWidth);

            if (segmentTemplate == null)
            {
                _slotsBuilt = false;
                return;
            }

            // Templates Beat_0 / Beat_1 must be active while Instantiating, then hidden in Play.
            var templateGo = segmentTemplate.gameObject;
            templateGo.SetActive(true);
            if (slotsRow != null)
            {
                var beat1 = slotsRow.Find("Beat_1");
                if (beat1 != null)
                {
                    beat1.gameObject.SetActive(true);
                }
            }

            _slots = new BeatSegmentView[UiSlotCount];
            for (var i = 0; i < UiSlotCount; i++)
            {
                var cloneGo = Instantiate(templateGo, slotsRow);
                cloneGo.name = $"BeatSlot_{i}";
                cloneGo.SetActive(true);
                MarkRuntimeClone(cloneGo);
                var clone = cloneGo.GetComponent<BeatSegmentView>();
                var absBeat = AbsoluteBeatFromSlot(i);
                clone.SetDisplayBeatIndex(absBeat);
                clone.WireReferences();
                clone.SetNoteVisualCatalog(NoteVisuals);
                clone.SetNoteBandNormalizedY(noteBandNormalizedY);
                var x = absBeat >= 0 && absBeat < TotalBeats ? absBeat * templateWidth : i * templateWidth;
                ApplySlotRect(clone, templateWidth, x);
                _slots[i] = clone;
            }

            HideAuthoredBeatTemplates();
            EnsurePhaseDividers();
            _slotsBuilt = true;
        }

        /// <summary>Edit-mode shells Beat_0 / Beat_1 — hide while Play; BeatSlot_* are the live slots.</summary>
        private void HideAuthoredBeatTemplates()
        {
            if (!Application.isPlaying || slotsRow == null)
            {
                return;
            }

            var beat0 = slotsRow.Find("Beat_0");
            if (beat0 != null)
            {
                beat0.gameObject.SetActive(false);
            }

            var beat1 = slotsRow.Find("Beat_1");
            if (beat1 != null)
            {
                beat1.gameObject.SetActive(false);
            }

            if (phaseDividerTemplate != null)
            {
                phaseDividerTemplate.gameObject.SetActive(false);
            }
        }

        private static void MarkRuntimeClone(GameObject cloneGo)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                cloneGo.hideFlags = HideFlags.DontSaveInEditor;
            }
#endif
        }

        private static void DestroyBeatClone(GameObject cloneGo)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(cloneGo);
                return;
            }
#endif
            Destroy(cloneGo);
        }

        private void ResetScrollState()
        {
            _totalScrollPx = 0f;
            _localBeat = 0f;
            _lastFiredBeat = -1;
            if (slotsRow != null)
            {
                ApplySceneFollowScrollX(
                    slotsRow,
                    in _scrollContentLock,
                    _scrollContentLock.Has ? _scrollContentLock.Position.x : 0f);
            }
        }

        private void OnDestroy()
        {
            StopAutoPlay();
            if (_timeline != null)
            {
                _timeline.OnTelegraphsChanged -= HandleTelegraphsChanged;
                _timeline.OnTelegraphMoved -= HandleTelegraphMoved;
                _timeline.OnTelegraphsDelayedBatch -= HandleTelegraphsDelayedBatch;
                _timeline.OnAgendaChanged -= HandleAgendaChanged;
            }

            if (_session != null)
            {
                _session.OnScanBeat -= HandleScanBeat;
                _session.OnTelegraphsPlanned -= HandleTelegraphsPlanned;
                _session.OnEncounterEnded -= HandleEncounterEnded;
                _session.OnBlockResolved -= HandleBlockResolved;
            }
        }

        private void AlignSlotsRowInViewport()
        {
            if (slotsRow == null || viewport == null)
            {
                return;
            }

            CaptureScrollContentSceneRect();
            if (preserveSceneLayout && _scrollContentAuthoredInScene)
            {
                return;
            }

            if (slotsRow.parent != viewport)
            {
                slotsRow.SetParent(viewport, false);
            }

            slotsRow.anchorMin = new Vector2(0f, 0f);
            slotsRow.anchorMax = new Vector2(0f, 1f);
            slotsRow.pivot = new Vector2(0f, 0.5f);
            slotsRow.anchoredPosition = new Vector2(0f, _scrollContentLock.Y);
            slotsRow.sizeDelta = new Vector2(_contentWidthPx, _scrollContentLock.Has ? _scrollContentLock.Height : 0f);
        }

        private void CaptureScrollContentSceneRect()
        {
            if (slotsRow == null)
            {
                return;
            }

            if (CaptureSceneRect(slotsRow, ref _scrollContentLock))
            {
                _scrollContentAuthoredInScene = true;
            }
        }

        private void CaptureLaneFootprintSceneRect()
        {
            var target = _footprintLayer != null ? _footprintLayer : laneFootprint;
            if (target == null)
            {
                return;
            }

            if (CaptureSceneRect(target, ref _laneFootprintLock))
            {
                _laneFootprintAuthoredInScene = true;
            }
        }

        private void CaptureBossTrackFrameSceneRect()
        {
            var target = _bossTrackFrame != null ? _bossTrackFrame : bossTrackFrame;
            if (target == null)
            {
                return;
            }

            if (CaptureSceneRect(target, ref _bossTrackFrameLock))
            {
                _bossTrackFrameAuthoredInScene = true;
            }
        }

        private static bool CaptureSceneRect(RectTransform rt, ref SceneRectLock dest)
        {
            if (rt == null || dest.Has)
            {
                return dest.Has;
            }

            dest.Has = true;
            dest.AnchorMin = rt.anchorMin;
            dest.AnchorMax = rt.anchorMax;
            dest.Pivot = rt.pivot;
            dest.Position = rt.anchoredPosition;
            dest.Size = rt.sizeDelta;
            return true;
        }

        private static void ApplySceneFollowScrollX(RectTransform rt, in SceneRectLock authored, float x)
        {
            if (rt == null)
            {
                return;
            }

            var y = authored.Has ? authored.Position.y : rt.anchoredPosition.y;
            rt.anchoredPosition = new Vector2(x, y);
        }

        private static void ApplySceneAuthoredPosition(RectTransform rt, in SceneRectLock authored)
        {
            if (rt == null || !authored.Has)
            {
                return;
            }

            rt.anchoredPosition = authored.Position;
        }

        private RectTransform FindTimelineRect(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return null;
            }

            if (slotsRow != null && slotsRow.name == objectName)
            {
                return slotsRow;
            }

            return FindDeepChild(slotsRow, objectName)
                ?? FindDeepChild(viewport, objectName)
                ?? FindDeepChild(transform, objectName);
        }

        private static RectTransform FindDeepChild(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == objectName)
                {
                    return child as RectTransform;
                }

                var nested = FindDeepChild(child, objectName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private bool IsNestedInSlotsRow(RectTransform rt)
        {
            return rt != null && slotsRow != null && rt != slotsRow && rt.IsChildOf(slotsRow);
        }

        private static void ApplySceneContentWidth(RectTransform rt, in SceneRectLock authored, float width)
        {
            if (rt == null)
            {
                return;
            }

            var height = authored.Has ? authored.Size.y : rt.sizeDelta.y;
            rt.sizeDelta = new Vector2(width, height);
        }

        private struct SceneRectLock
        {
            public bool Has;
            public Vector2 AnchorMin;
            public Vector2 AnchorMax;
            public Vector2 Pivot;
            public Vector2 Position;
            public Vector2 Size;

            public float Y => Has ? Position.y : 0f;
            public float Height => Has ? Size.y : 0f;
        }

        private void AlignScanBar()
        {
            if (scanBar == null)
            {
                return;
            }

            // Hierarchy-first: keep Scene ScanBar anchors / size / X (e.g. x=26).
            if (preserveSceneLayout)
            {
                return;
            }

            scanBar.anchorMin = new Vector2(0f, 0f);
            scanBar.anchorMax = new Vector2(0f, 1f);
            scanBar.pivot = new Vector2(0.5f, 0.5f);
            scanBar.anchoredPosition = new Vector2(GetScanLineX(), 0f);
            scanBar.sizeDelta = new Vector2(
                TimelineLayoutLock.ScanBarWidth,
                TimelineLayoutLock.ScanBarVerticalInset);
        }

        private void CleanupExtraBeatChildren()
        {
            if (slotsRow == null)
            {
                return;
            }

            for (var i = slotsRow.childCount - 1; i >= 0; i--)
            {
                var child = slotsRow.GetChild(i);
                if (child.name == "Beat_0" || child.name == "Beat_1" || child.name == "PhaseDivider")
                {
                    continue;
                }

                if (child.name.StartsWith("Beat_") || child.name.StartsWith("BeatSlot_")
                    || child.name.StartsWith("PhaseDivider_"))
                {
                    DestroyBeatClone(child.gameObject);
                }
            }

            _phaseDividers = null;
        }

        private void HandleScanBeat(int beatIndex)
        {
            if (_timeline == null || !_slotsBuilt)
            {
                return;
            }

            StopAutoPlay();
            SyncSegmentFromSession();
            _localBeat = beatIndex - _segmentStartBeat;
            _totalScrollPx = PxOfAbsoluteBeat(beatIndex);
            _lastFiredBeat = beatIndex - 1;
            ApplyScrollVisual(_totalScrollPx);
        }

        public void SetPhase(CombatPhase phase)
        {
            if (phase == CombatPhase.Planning)
            {
                _autoPlayCompleted = false;
                ResetCarouselForPlanning();
                RefreshPhaseHeader(0);
            }
            else if (phaseLabel != null)
            {
                phaseLabel.text = phase.ToString().ToUpperInvariant();
            }
        }

        private void ResetCarouselForPlanning()
        {
            StopAutoPlay();
            ResetScrollState();
            _slotsBuilt = false;
            _slots = null;
            _windowStartBeat = 0;
            _autoPlayCompleted = false;
            CleanupExtraBeatChildren();

            if (segmentTemplate != null && slotsRow != null)
            {
                segmentTemplate.transform.SetAsFirstSibling();
            }

            RebuildLayout();
            PopulateAllSlots();
            ApplyScrollVisual(0f);
            RefreshPhaseHeader(0);
        }

        public void RefreshAll()
        {
            if (_timeline == null)
            {
                return;
            }

            RebuildLayout();
            PopulateAllSlots();
            RefreshPhaseHeader(_autoPlayBeat);
            RefreshPhaseAvLabel();
            ApplyScrollVisual(_totalScrollPx);
        }

        /// <summary>Refresh telegraph slots without rebuilding layout — keeps scroll position stable between segments.</summary>
        public void RefreshTelegraphsAndSlots()
        {
            if (_timeline == null)
            {
                return;
            }

            if (!_slotsBuilt)
            {
                RebuildLayout();
            }

            PopulateAllSlots();
            RefreshPhaseHeader(_autoPlayBeat);
            RefreshPhaseAvLabel();
            ApplyScrollVisual(_totalScrollPx);
        }

        public void RefreshBeat(int beatIndex, bool rebuildBossNotes = true)
        {
            if (_slots == null || _timeline == null)
            {
                return;
            }

            var slot = TryGetSlotView(beatIndex);
            if (slot != null)
            {
                PopulateSlot(slot, beatIndex);
            }

            if (rebuildBossNotes)
            {
                RebuildBossNoteClusters();
            }
        }

        public void RefreshBeatsAndBossNotes(IEnumerable<int> beatIndices)
        {
            if (_slots == null || _timeline == null)
            {
                return;
            }

            if (beatIndices != null)
            {
                foreach (var beatIndex in beatIndices)
                {
                    var slot = TryGetSlotView(beatIndex);
                    if (slot != null)
                    {
                        PopulateSlot(slot, beatIndex);
                    }
                }
            }

            RebuildBossNoteClusters();
        }

        private void PopulateAllSlots()
        {
            if (_slots == null)
            {
                return;
            }

            ClearHighlightedSlot();
            for (var i = 0; i < _slots.Length; i++)
            {
                var absBeat = AbsoluteBeatFromSlot(i);
                if (absBeat < 0 || absBeat >= TotalBeats)
                {
                    if (_slots[i] != null)
                    {
                        _slots[i].gameObject.SetActive(false);
                        _slots[i].SetEmpty();
                    }

                    continue;
                }

                if (_slots[i] != null)
                {
                    _slots[i].gameObject.SetActive(true);
                }

                PopulateSlot(_slots[i], absBeat);
            }

            ReapplySlotRectsFromCache();
            RefreshLaneMarkers();
            RebuildBossNoteClusters();
        }

        private void ReapplySlotRectsFromCache()
        {
            if (_slots == null || _slotWidths == null || _slotOffsetPx == null)
            {
                return;
            }

            for (var i = 0; i < _slots.Length; i++)
            {
                var absBeat = AbsoluteBeatFromSlot(i);
                if (absBeat < 0 || absBeat >= TotalBeats || absBeat >= _slotWidths.Length)
                {
                    continue;
                }

                ApplySlotRect(_slots[i], _slotWidths[absBeat], _slotOffsetPx[absBeat]);
            }
        }

        private void PopulateSlot(BeatSegmentView slot, int globalBeat)
        {
            if (slot == null)
            {
                return;
            }

            slot.ResetScanHighlight();
            slot.SetDisplayBeatIndex(globalBeat);
            slot.UpdatePhaseDivider();

            if (_timeline == null || globalBeat < 0 || globalBeat >= TotalBeats)
            {
                slot.SetEmpty();
                slot.CaptureLayoutBaseline();
                return;
            }

            AgendaEntry playerEntry = null;
            foreach (var e in _timeline.Agenda)
            {
                if (e.BeatIndex == globalBeat && e.Unit.Side == GridSide.Player)
                {
                    playerEntry = e;
                    break;
                }
            }

            slot.SetNoteVisualCatalog(NoteVisuals);
            slot.SetNoteBandNormalizedY(noteBandNormalizedY);
            slot.SetSuppressActiveImpactGlyph(true);
            var telegraph = _timeline.GetImpactTelegraphAtBeat(globalBeat)
                ?? _timeline.GetTelegraphAtBeat(globalBeat);
            var remainingHits = -1;
            if (telegraph != null && !telegraph.IsWindupOnly)
            {
                remainingHits = CombatCounterResolver.GetRemainingHits(telegraph, _timeline);
            }

            slot.SetSlot(playerEntry, telegraph, remainingHits);
            slot.CaptureLayoutBaseline();
        }

        public void SetScanSpeedMultiplier(float multiplier)
        {
            _scanSpeedMultiplier = Mathf.Max(0.001f, multiplier);
            ActiveMusic?.SetPlaybackSpeedMultiplier(multiplier);
        }

        public void SetSkillPanelOpen(bool open)
        {
        }

        private float GetBeatWaitDuration()
        {
            if (_scanSpeedMultiplier <= 0f)
            {
                return autoBeatInterval;
            }

            return autoBeatInterval / _scanSpeedMultiplier;
        }

        public void RefreshPhaseHeader(int beatIndex)
        {
            if (_session == null)
            {
                return;
            }

            var phaseIndex = PhaseAvTracker.ResolveTimelinePhaseIndex(beatIndex);
            if (budgetLabel != null)
            {
                budgetLabel.text = $"{phaseIndex + 1}/{TimelineConstants.PhaseCount}";
            }

            if (phaseLabel != null)
            {
                phaseLabel.text = "PHASE";
            }

            RefreshPhaseAvLabel();
        }

        public void RefreshPhaseAvLabel()
        {
        }

        public void SetAvDisplay(string text)
        {
        }

        private void OnValidate()
        {
            leftRailLayout ??= new LeftRailLayout();

            if (bossNoteNumberLayout != null)
            {
                if (bossNoteNumberLayout.variantNudges == null ||
                    bossNoteNumberLayout.variantNudges.Length != 5)
                {
                    bossNoteNumberLayout.variantNudges = new Vector2[5];
                }

                bossNoteNumberLayout.EnsureSingleHeadNormByVariant();

                if (bossNoteNumberLayout.perfectMarkScaleVsNumber >= 1.7f)
                {
                    bossNoteNumberLayout.perfectMarkScaleVsNumber = 1.35f;
                }

                if (bossNoteNumberLayout.perfectPreviewScale >= 1.3f)
                {
                    bossNoteNumberLayout.perfectPreviewScale = 1.1f;
                }

                if (bossNoteNumberLayout.perfectMarkFixedPx < 12f ||
                    bossNoteNumberLayout.perfectMarkFixedPx > 36f)
                {
                    bossNoteNumberLayout.perfectMarkFixedPx = 24f;
                }

                if (bossNoteNumberLayout.perfectMarkMinPx >= 50f ||
                    bossNoteNumberLayout.perfectMarkMinPx < 10f)
                {
                    bossNoteNumberLayout.perfectMarkMinPx = 16f;
                }

                if (bossNoteNumberLayout.perfectNeighborFill < 0.45f ||
                    bossNoteNumberLayout.perfectNeighborFill > 0.95f)
                {
                    bossNoteNumberLayout.perfectNeighborFill = 0.85f;
                }

                if (bossNoteNumberLayout.perfectBeatWidthFill < 0.55f ||
                    bossNoteNumberLayout.perfectBeatWidthFill > 1.05f)
                {
                    bossNoteNumberLayout.perfectBeatWidthFill = 0.82f;
                }

                bossNoteNumberLayout.perfectMarkScaleVsNumber =
                    Mathf.Clamp(bossNoteNumberLayout.perfectMarkScaleVsNumber, 1f, 2f);
                bossNoteNumberLayout.perfectPreviewScale =
                    Mathf.Clamp(bossNoteNumberLayout.perfectPreviewScale, 1f, 1.2f);
                bossNoteNumberLayout.perfectBeatWidthFill =
                    Mathf.Clamp(bossNoteNumberLayout.perfectBeatWidthFill, 0.55f, 1.05f);
                bossNoteNumberLayout.perfectMarkFixedPx =
                    Mathf.Clamp(bossNoteNumberLayout.perfectMarkFixedPx, 12f, 36f);
            }

            if (SuppressBossNoteClusterRebuild ||
                !Application.isPlaying ||
                _timeline == null ||
                !_slotsBuilt)
            {
                return;
            }

            RebuildBossNoteClusters();
        }
    }
}

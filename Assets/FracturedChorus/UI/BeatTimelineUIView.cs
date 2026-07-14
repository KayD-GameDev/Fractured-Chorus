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
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class BeatTimelineUIView : MonoBehaviour
    {
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform slotsRow;
        [SerializeField] private BeatSegmentView segmentTemplate;
        [SerializeField] private RectTransform scanBar;
        [SerializeField] private RectTransform trackLine;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Text phaseLabel;
        [SerializeField] private Text budgetLabel;
        [SerializeField] private Text avLabel;
        [SerializeField] private float slotWidth = 52f;
        [SerializeField] private float minSlotWidth = 14f;
        [SerializeField] private float laneMarkerSize = 26f;
        [SerializeField] private float activeFootprintDotSize = 30f;
        [Tooltip("Footprint dot size (gray S1/S2 · colored active S) around the skill chip.")]
        [SerializeField] private float footprintDotSize = 16f;
        [SerializeField] private bool autoPlayOnStart;
        [SerializeField] private float autoBeatInterval = 0.405405f;
        [SerializeField] private bool useMusicSync = true;
        [Tooltip("Vị trí hit trong mỗi slot (0 = đầu nốt, 0.5 = giữa, 1 = cuối). Width slot đã scale theo beat map.")]
        [SerializeField] [Range(0f, 1f)] private float beatHitAnchorT = 0.5f;
        [SerializeField] private CombatMusicController musicController;
        [SerializeField] private CombatSfxController combatSfxController;
        [SerializeField] private CounterPresentationDriver counterPresentation;
        [SerializeField] private TimelineNoteVisualCatalog noteVisuals = new TimelineNoteVisualCatalog();
        [Tooltip("Boss note Y trong beat (0=đáy, 1=đỉnh). Band trên, tách khỏi party lanes.")]
        [SerializeField] [Range(0.55f, 0.92f)] private float noteBandNormalizedY = 0.78f;
        [Tooltip("Party lane band — mép dưới (normalized từ đáy viewport).")]
        [SerializeField] [Range(0.05f, 0.45f)] private float laneBandMinNormalizedY = 0.12f;
        [Tooltip("Party lane band — mép trên (normalized từ đáy viewport). Phải < noteBand.")]
        [SerializeField] [Range(0.25f, 0.6f)] private float laneBandMaxNormalizedY = 0.42f;
        [SerializeField] private float bossTrackFrameHeight = 56f;
        [SerializeField] private Color bossTrackFrameFill = new Color(0.22f, 0.05f, 0.07f, 0.88f);
        [SerializeField] private Color bossTrackFrameBorderTop = new Color(0.45f, 0.98f, 1f, 0.95f);
        [SerializeField] private Color bossTrackFrameBorderBottom = new Color(0.85f, 0.45f, 1f, 0.9f);
        [SerializeField] private float bossTrackFrameBorderThickness = 2f;
        [SerializeField] private Sprite timelineStaffBackground;
        [SerializeField] [Range(0.15f, 1f)] private float timelineStaffBackgroundAlpha = 1f;
        [Tooltip("Keep Header / outer BeatTimeline frame position. Internal layout (TrackLine, ScrollContent, ScanBar) still auto-layouts.")]
        [SerializeField] private bool preserveSceneLayout = true;

        private BeatTimelineEngine _timeline;
        private CombatSession _session;
        private BeatSegmentView[] _slots;
        private float[] _slotWidths;
        private float[] _slotOffsetPx;
        private float _contentWidthPx;
        private float _pixelsPerSecond = 1f;
        private int _roundStartBeatIndex;
        private Coroutine _autoPlayRoutine;
        private bool _slotsBuilt;
        private bool _autoPlayCompleted;
        private float _lastViewportWidth;
        private int _autoPlayBeat;
        private Action _onPlanningPause;
        private Action _onRoundSegmentComplete;
        private bool _planningPauseEnabled = true;
        private float _scanSpeedMultiplier = 1f;
        private float _totalScrollPx;
        private float _localBeat;
        private int _lastFiredBeat = -1;
        private bool _isPlaybackActive;
        private bool _planningPauseArmed;
        private bool _pausedForPlanning;
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
        private float _roundStartMusicalBeat;
        private int _roundSegmentIndex;
        private int _segmentStartBeat;

        [SerializeField] private RectTransform laneAvatarGutter;

        private RectTransform _laneLinesLayer;
        private RectTransform _laneMarkersLayer;
        private RectTransform _footprintLayer;
        private RectTransform _bossTrackFrame;
        private Image _staffBackground;
        private readonly List<CombatUnit> _laneUnits = new();
        private readonly Dictionary<CombatUnit, int> _laneIndex = new();
        private readonly List<TimelineLaneAvatarSlotView> _laneAvatarSlots = new();
        private Action<CombatUnit> _onLaneAvatarClicked;
        private CombatUnit _selectedLaneUnit;

        private const float LaneAvatarSlotSize = 40f;
        private const float LaneAvatarGutterWidth = 44f;
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
        private readonly List<Image> _dropPreviewDots = new();
        private readonly List<Image> _dropCoverOverlays = new();

        private static readonly Color StandingDotColor = new Color(0.5f, 0.5f, 0.55f, 0.4f);

        // Intro-pause sau Deploy: snap cuối beat 0 vào ScanBar (anchor-based, không dùng localBeat threshold).
        private const float PhaseDividerVisualOffsetPx = 2f;
        private const float AnchorScrollEpsilonPx = 0.01f;

        private static int TotalBeats => TimelineConstants.TotalBeats;

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
                slotsRow = transform.Find("Viewport/ScrollContent") as RectTransform;
            }

            if (segmentTemplate == null && slotsRow != null)
            {
                var beat0 = slotsRow.Find("Beat_0");
                if (beat0 != null)
                {
                    segmentTemplate = beat0.GetComponent<BeatSegmentView>();
                }
            }

            if (scanBar == null)
            {
                scanBar = transform.Find("Viewport/ScanBar") as RectTransform;
            }

            if (trackLine == null && viewport != null)
            {
                trackLine = viewport.Find("TrackLine") as RectTransform;
            }

            EnsureTrackLine();
            EnsureViewportMask();
            EnsureStaffBackground();

            if (confirmButton == null)
            {
                confirmButton = transform.Find("ConfirmButton")?.GetComponent<Button>();
            }

            if (phaseLabel == null)
            {
                phaseLabel = transform.Find("Header/PhaseLabel")?.GetComponent<Text>()
                    ?? transform.Find("ConfirmButton/PhaseLabel")?.GetComponent<Text>();
            }

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

            ConfigureAvLabelLayout();
            ExpandViewportWidth();

            if (musicController == null)
            {
                musicController = FindAnyObjectByType<CombatMusicController>();
            }

            EnsureCombatSfx();
            EnsureNoteVisuals();
        }

        public TimelineNoteVisualCatalog NoteVisuals
        {
            get
            {
                EnsureNoteVisuals();
                return noteVisuals;
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

        public void Bind(BeatTimelineEngine timeline, CombatSession session,
            CombatMusicController music = null, Action onPlanningPause = null, Action onRoundSegmentComplete = null,
            CombatSfxController combatSfx = null, CounterPresentationDriver presentation = null)
        {
            if (music != null)
            {
                musicController = music;
            }

            if (combatSfx != null)
            {
                combatSfxController = combatSfx;
            }

            if (presentation != null)
            {
                counterPresentation = presentation;
            }

            _timeline = timeline;
            _session = session;
            _onPlanningPause = onPlanningPause;
            _onRoundSegmentComplete = onRoundSegmentComplete;
            if (_timeline != null)
            {
                _timeline.PlanningHorizonBeat = TimelineConstants.IntroExecuteStartBeatIndex;
            }

            WireReferences();
            RebuildLayout();

            if (_session != null)
            {
                _session.OnScanBeat -= HandleScanBeat;
                _session.OnScanBeat += HandleScanBeat;
                _session.OnTelegraphsPlanned -= HandleTelegraphsPlanned;
                _session.OnTelegraphsPlanned += HandleTelegraphsPlanned;
                _session.OnEncounterEnded -= HandleEncounterEnded;
                _session.OnEncounterEnded += HandleEncounterEnded;
            }

            BuildLanes();
            PopulateAllSlots();
            RefreshPhaseHeader(0);
            RefreshPhaseAvLabel();
            counterPresentation?.ResetPresentation();
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

        public bool CanRelocateLaneMarker()
        {
            return _session != null && _session.Phase == CombatPhase.Planning && !_session.AllowPlayerReposition;
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

        public void ClearLaneMarkerRelocatePrepare()
        {
            _relocatePendingKey = null;
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

            _autoPlayCompleted = false;
            _pausedForPlanning = false;
            _planningPauseArmed = _planningPauseEnabled;
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

        public void SetPlanningPauseEnabled(bool enabled)
        {
            _planningPauseEnabled = enabled;
        }

        public void ResetForNextPlanningSegment()
        {
            StopAutoPlay();
            _autoPlayCompleted = false;
            _pausedForPlanning = false;
            _planningPauseArmed = false;
            _localBeat = 0f;
            _lastFiredBeat = -1;
            _totalScrollPx = 0f;
            ResetScrollState();
            ApplyScrollVisual(0f);
            ResetAllScanHighlights();
            musicController?.PausePlayback();
        }

        /// <summary>Giữ scroll tại vạch trắng cuối round segment (không nhảy về beat 0).</summary>
        public void HoldAtRoundEnd()
        {
            StopAutoPlay();
            _autoPlayCompleted = true;
            _autoPlayRoutine = null;
            _pausedForPlanning = false;
            _planningPauseArmed = false;

            SyncSegmentFromSession();
            var completedSegmentIndex = Mathf.Max(0, _roundSegmentIndex - 1);
            _localBeat = TimelineConstants.GetSegmentBeatCountForSegment(completedSegmentIndex);
            SnapScrollToAnchor(GetSegmentPhaseDividerAnchorPx(completedSegmentIndex));
            ResetAllScanHighlights();
            if (_timeline != null)
            {
                _timeline.PlanningHorizonBeat = _segmentStartBeat;
            }
        }

        /// <summary>Đang tạm dừng chờ player set up skill (intro-pause).</summary>
        public bool IsPausedForPlanning => _pausedForPlanning;

        /// <summary>Phát tiếp sau intro-pause: nhạc + scan tiếp tục từ vị trí đã dừng.</summary>
        public void ResumeRoundPlayback()
        {
            if (!_pausedForPlanning)
            {
                return;
            }

            _pausedForPlanning = false;
            musicController?.ResumePlayback();
            ResetCounterSfxState();
            RebuildCounterBeatCache();

            if (_autoPlayRoutine != null)
            {
                StopCoroutine(_autoPlayRoutine);
            }

            _autoPlayRoutine = CanUseMusicSync()
                ? StartCoroutine(MusicDrivenScanRoutine(true))
                : StartCoroutine(ContinuousScanRoutine(true));
        }

        private void EnterPlanningPause()
        {
            musicController?.EnterPlanningPhase();
            _planningPauseArmed = false;
            _pausedForPlanning = true;
            _isPlaybackActive = false;
            SnapScrollToAnchor(GetIntroPauseAnchorPx());
            _localBeat = TimelineConstants.IntroExecuteStartBeatIndex;
            _lastFiredBeat = _segmentStartBeat + TimelineConstants.IntroPlanningPauseAfterBeatIndex;
            if (_timeline != null)
            {
                _timeline.PlanningHorizonBeat = TimelineConstants.IntroExecuteStartBeatIndex;
            }

            if (musicController != null && CanUseMusicSync())
            {
                _roundStartMusicalBeat = musicController.TotalMusicalBeat - TimelineConstants.IntroExecuteStartBeatIndex;
            }

            ResetAllScanHighlights();
            RefreshLaneMarkers();
            RefreshTelegraphsAndSlots();
            Debug.Log($"[BeatTimeline] Intro-pause @ beat {TimelineConstants.IntroPlanningPauseAfterBeatIndex}. Execute from beat {TimelineConstants.IntroExecuteStartBeatIndex}.");
            _onPlanningPause?.Invoke();
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
            return useMusicSync && musicController != null;
        }

        private IEnumerator MusicDrivenScanRoutine(bool resume, bool continueFromHold = false)
        {
            if (musicController == null || !musicController.IsPlaying)
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
                _localBeat = musicController.TotalMusicalBeat - _roundStartMusicalBeat;
                if (_localBeat >= GetSegmentBeatSpan())
                {
                    break;
                }

                _totalScrollPx = ScrollPxForContentAnchor(PxOfAbsoluteBeat(GetAbsolutePlaybackBeat()));
                ApplyScrollVisual(_totalScrollPx);

                if (HasReachedSegmentDivider())
                {
                    SnapToSegmentDividerAndStop();
                    break;
                }

                if (!_isPlaybackActive)
                {
                    break;
                }

                if (TryEnterIntroPlanningPause())
                {
                    break;
                }

                if (_session != null && _session.IsEncounterOver)
                {
                    break;
                }

                yield return null;
            }

            if (_pausedForPlanning)
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
                _totalScrollPx = ScrollPxForContentAnchor(PxOfAbsoluteBeat(GetAbsolutePlaybackBeat()));
                ApplyScrollVisual(_totalScrollPx);

                if (HasReachedSegmentDivider())
                {
                    SnapToSegmentDividerAndStop();
                    break;
                }

                if (!_isPlaybackActive)
                {
                    break;
                }

                if (TryEnterIntroPlanningPause())
                {
                    break;
                }

                if (_session != null && _session.IsEncounterOver)
                {
                    break;
                }

                yield return null;
            }

            if (_pausedForPlanning)
            {
                _autoPlayRoutine = null;
                yield break;
            }

            FinishRoundSegment();
        }

        private void FinishRoundSegment()
        {
            musicController?.EnterPlanningPhase();
            _isPlaybackActive = false;
            _autoPlayCompleted = true;
            _autoPlayRoutine = null;
            ResetAllScanHighlights();
            _onRoundSegmentComplete?.Invoke();
        }

        public void StopTimelinePlayback()
        {
            StopAutoPlay();
            _autoPlayCompleted = true;
            _pausedForPlanning = false;
            _planningPauseArmed = false;
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
            musicController?.StopMusic();
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

        private float GetSpanSec(int songBeatIndex)
        {
            var beatMap = musicController != null ? musicController.BeatMap : null;
            if (beatMap != null && beatMap.HasData)
            {
                return beatMap.GetBeatSpanSec(songBeatIndex);
            }

            return autoBeatInterval > 0f ? autoBeatInterval : 60f / 148f;
        }

        private float ComputePixelsPerSecond()
        {
            var beatMap = musicController != null ? musicController.BeatMap : null;
            var avgSpan = beatMap != null && beatMap.HasData
                ? beatMap.AverageBeatSpanSec()
                : autoBeatInterval;

            if (avgSpan <= 0.0001f)
            {
                avgSpan = autoBeatInterval > 0f ? autoBeatInterval : 60f / 148f;
            }

            return slotWidth / avgSpan;
        }

        private void SyncSegmentFromSession()
        {
            _roundSegmentIndex = _session != null ? _session.RoundSegmentIndex : 0;
            _segmentStartBeat = TimelineConstants.GetSegmentStartBeat(_roundSegmentIndex);
        }

        private int GetSegmentBeatSpan() => TimelineConstants.GetSegmentBeatCountForSegment(_roundSegmentIndex);

        private int GetSegmentEndBeatExclusive() => _segmentStartBeat + GetSegmentBeatSpan();

        private float GetAbsolutePlaybackBeat() => _segmentStartBeat + _localBeat;

        private const float SegmentScrollEpsilonPx = 1f;

        private float GetBeatEndContentPx(int beatIndex)
        {
            if (_slotOffsetPx == null || beatIndex < 0 || beatIndex + 1 >= _slotOffsetPx.Length)
            {
                return 0f;
            }

            return _slotOffsetPx[beatIndex + 1];
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

        /// <summary>End of intro planning-pause beat in current segment.</summary>
        private float GetIntroPauseAnchorPx() =>
            GetBeatEndContentPx(_segmentStartBeat + TimelineConstants.IntroPlanningPauseAfterBeatIndex);

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

        public bool IsPlaybackActive => _isPlaybackActive && !_pausedForPlanning;

        public float GetAbsolutePlaybackBeatPublic() => GetAbsolutePlaybackBeat();

        public int GetCurrentScanBeatIndex() =>
            Mathf.Clamp(Mathf.FloorToInt(GetAbsolutePlaybackBeat()), 0, TotalBeats - 1);

        private void PrepareSegmentScanStart(bool useMusicSync, bool continueFromHold = false)
        {
            SyncSegmentFromSession();

            if (continueFromHold)
            {
                _localBeat = 0f;
                _lastFiredBeat = _segmentStartBeat - 1;
                if (useMusicSync && musicController != null)
                {
                    _roundStartMusicalBeat = musicController.TotalMusicalBeat;
                }

                RebuildLayout();
                _totalScrollPx = GetSegmentStartScrollPx();
                RefreshTelegraphsAndSlots();
                ApplyScrollVisual(_totalScrollPx);
                _lastScanLineContentPos = GetScanLineContentPos();
                RebuildCounterBeatCache();
                return;
            }

            _localBeat = 0f;
            _lastFiredBeat = _segmentStartBeat - 1;

            if (useMusicSync && musicController != null && musicController.IsPlaying)
            {
                _roundStartMusicalBeat = musicController.TotalMusicalBeat;
            }
            else if (_roundSegmentIndex == 0)
            {
                _roundStartMusicalBeat = 0f;
            }

            RebuildLayout();
            _totalScrollPx = GetSegmentStartScrollPx();
            ApplyScrollVisual(_totalScrollPx);
            _lastScanLineContentPos = GetScanLineContentPos();
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
            if (!_isPlaybackActive || _pausedForPlanning || !_slotsBuilt)
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

        /// <summary>Intro-pause khi beat 6 qua vạch quét.</summary>
        private bool TryEnterIntroPlanningPause()
        {
            if (!_planningPauseArmed)
            {
                return false;
            }

            var pauseAfterBeat = _segmentStartBeat + TimelineConstants.IntroPlanningPauseAfterBeatIndex;
            if (_lastFiredBeat < pauseAfterBeat)
            {
                return false;
            }

            var scrollTarget = ScrollPxForContentAnchor(GetIntroPauseAnchorPx());
            if (_totalScrollPx < scrollTarget - AnchorScrollEpsilonPx)
            {
                return false;
            }

            EnterPlanningPause();
            return true;
        }

        private void FireScanBeat(int beat)
        {
            _lastFiredBeat = beat;
            _autoPlayBeat = beat;
            _session?.OnTimelineScanBeat(beat);
            RefreshPhaseHeader(beat);
            _session?.ResolveBeatAtScan(beat);
            PlayAttackAnimationsAtBeat(beat);
            RefreshBeat(beat);
            RefreshPhaseAvLabel();
            UpdateScanHighlights();
        }

        private void PlayAttackAnimationsAtBeat(int beatIndex)
        {
            if (_timeline == null || beatIndex < 0)
            {
                return;
            }

            var isCounterBeat = _precomputedCounterBeats.Contains(beatIndex);

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

                if (isCounterBeat)
                {
                    continue;
                }

                UnitView.FindForUnit(entry.Unit)?.PlayAttackAnimation(entry.Skill);
            }
        }

        private void ApplyScrollVisual(float scrollPx)
        {
            if (slotsRow == null || scanBar == null || !_slotsBuilt)
            {
                return;
            }

            var viewportWidth = GetViewportWidth();
            var maxScroll = Mathf.Max(0f, _contentWidthPx - viewportWidth);
            var readLineX = GetScanBarReadLineX();
            var clampedScroll = Mathf.Clamp(scrollPx, 0f, maxScroll);

            slotsRow.anchoredPosition = new Vector2(-clampedScroll, 0f);
            scanBar.anchoredPosition = new Vector2(readLineX, 0f);

            SyncLaneMarkersScroll();
            ProcessScanLineCrossings();
            UpdateScanHighlights();
        }

        private void UpdateScanHighlights()
        {
            if (!_slotsBuilt || scanBar == null || _slots == null || _slotOffsetPx == null)
            {
                return;
            }

            var rowX = slotsRow != null ? slotsRow.anchoredPosition.x : 0f;
            var contentPos = scanBar.anchoredPosition.x - rowX;
            var index = FindSlotAtContentPos(contentPos);

            if (index < 0)
            {
                if (_lastHighlightedSlotIndex >= 0)
                {
                    ClearHighlightedSlot();
                }

                return;
            }

            var width = _slotWidths[index];
            var inSlot = contentPos - _slotOffsetPx[index];
            var p = width > 0f ? inSlot / width : 0.5f;
            var intensity = p <= 0.5f ? Mathf.SmoothStep(0f, 1f, p / 0.5f) : 0f;

            if (_lastHighlightedSlotIndex >= 0 &&
                _lastHighlightedSlotIndex != index &&
                _lastHighlightedSlotIndex < _slots.Length)
            {
                _slots[_lastHighlightedSlotIndex]?.SetScanIntensity(0f);
            }

            _slots[index]?.SetScanIntensity(intensity);
            _lastHighlightedSlotIndex = index;
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
            if (counterPresentation != null)
            {
                counterPresentation.NotifyPerfect(beatIndex, _timeline);
                return;
            }

            EnsureCombatSfx();
            if (combatSfxController == null)
            {
                return;
            }

            combatSfxController.PlayPerfectCounter();
            PlayCounterAnimations(beatIndex);
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
                }
                else
                {
                    var go = new GameObject("LaneLines", typeof(RectTransform));
                    _laneLinesLayer = go.GetComponent<RectTransform>();
                    _laneLinesLayer.SetParent(viewport, false);
                    _laneLinesLayer.anchorMin = Vector2.zero;
                    _laneLinesLayer.anchorMax = Vector2.one;
                    _laneLinesLayer.offsetMin = Vector2.zero;
                    _laneLinesLayer.offsetMax = Vector2.zero;
                }
            }

            if (_footprintLayer == null)
            {
                var existingFootprint = viewport.Find("LaneFootprint") as RectTransform;
                if (existingFootprint != null)
                {
                    _footprintLayer = existingFootprint;
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

            _footprintLayer.SetAsLastSibling();
            _laneMarkersLayer.SetAsLastSibling();
            OrderViewportLayers();
            BringResolveFeedbackToFront();
            _footprintLayer.sizeDelta = new Vector2(_contentWidthPx, 0f);
            _laneMarkersLayer.sizeDelta = new Vector2(_contentWidthPx, 0f);
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

            if (_bossTrackFrame != null)
            {
                _bossTrackFrame.SetSiblingIndex(_staffBackground != null ? 1 : 0);
            }

            if (slotsRow != null)
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

            if (_laneLinesLayer != null)
            {
                _laneLinesLayer.SetAsLastSibling();
            }

            if (_footprintLayer != null)
            {
                _footprintLayer.SetAsLastSibling();
            }

            if (_laneMarkersLayer != null)
            {
                _laneMarkersLayer.SetAsLastSibling();
            }

            if (scanBar != null)
            {
                scanBar.SetAsLastSibling();
            }
        }

        private void BuildLanes()
        {
            EnsureLaneLayers();

            foreach (var line in _laneLines)
            {
                if (line != null)
                {
                    Destroy(line.gameObject);
                }
            }

            _laneLines.Clear();
            _laneUnits.Clear();
            _laneIndex.Clear();

            ClearLaneMarkers();
            ClearFootprintDots();

            if (_session == null || _session.Grid == null || _laneLinesLayer == null)
            {
                return;
            }

            foreach (var unit in _session.Grid.PlayerUnits)
            {
                if (unit == null || !unit.IsAlive)
                {
                    continue;
                }

                _laneIndex[unit] = _laneUnits.Count;
                _laneUnits.Add(unit);
            }

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (var i = 0; i < _laneUnits.Count; i++)
            {
                var unit = _laneUnits[i];

                var lineGo = new GameObject($"Lane_{i}", typeof(RectTransform));
                var lineRect = lineGo.GetComponent<RectTransform>();
                lineRect.SetParent(_laneLinesLayer, false);
                lineRect.anchorMin = new Vector2(0f, 0f);
                lineRect.anchorMax = new Vector2(1f, 0f);
                lineRect.pivot = new Vector2(0.5f, 0.5f);
                lineRect.sizeDelta = new Vector2(0f, 5f);
                var lineImage = lineGo.AddComponent<Image>();
                var tint = unit.PlaceholderColor;
                lineImage.color = new Color(
                    Mathf.Min(1f, tint.r * 1.15f + 0.08f),
                    Mathf.Min(1f, tint.g * 1.15f + 0.08f),
                    Mathf.Min(1f, tint.b * 1.15f + 0.08f),
                    0.92f);
                lineImage.raycastTarget = false;

                var labelGo = new GameObject("Label", typeof(RectTransform));
                var labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.SetParent(lineRect, false);
                labelRect.anchorMin = new Vector2(0f, 0.5f);
                labelRect.anchorMax = new Vector2(0f, 0.5f);
                labelRect.pivot = new Vector2(0f, 0.5f);
                labelRect.anchoredPosition = new Vector2(4f, 8f);
                labelRect.sizeDelta = new Vector2(90f, 14f);
                var label = labelGo.AddComponent<Text>();
                label.font = font;
                label.fontSize = 10;
                label.alignment = TextAnchor.MiddleLeft;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                label.color = new Color(tint.r, tint.g, tint.b, 0.9f);
                label.text = unit.DisplayName != null ? unit.DisplayName.ToUpperInvariant() : $"UNIT {i}";
                label.raycastTarget = false;

                _laneLines.Add(lineRect);
            }

            LayoutLanes();
            EnsureBossTrackFrame();
            EnsureLaneAvatarColumn();
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

            DestroyLegacyBossLaneLine();

            if (_bossTrackFrame == null)
            {
                var existing = viewport.Find("BossTrackFrame") as RectTransform;
                if (existing != null)
                {
                    _bossTrackFrame = existing;
                }
                else
                {
                    var root = new GameObject("BossTrackFrame", typeof(RectTransform));
                    _bossTrackFrame = root.GetComponent<RectTransform>();
                    _bossTrackFrame.SetParent(viewport, false);
                    _bossTrackFrame.anchorMin = new Vector2(0f, 0f);
                    _bossTrackFrame.anchorMax = new Vector2(0f, 0f);
                    _bossTrackFrame.pivot = new Vector2(0f, 0.5f);

                    CreateBossTrackChild("Fill", _bossTrackFrame, stretch: true);
                    CreateBossTrackChild("BorderTop", _bossTrackFrame, stretch: false);
                    CreateBossTrackChild("BorderBottom", _bossTrackFrame, stretch: false);
                }
            }

            LayoutBossTrackFrame();

            OrderViewportLayers();
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

        private void LayoutBossTrackFrame()
        {
            if (_bossTrackFrame == null || viewport == null)
            {
                return;
            }

            var height = Mathf.Max(24f, bossTrackFrameHeight);
            var width = Mathf.Max(_contentWidthPx, viewport.rect.width);
            var scrollX = slotsRow != null ? slotsRow.anchoredPosition.x : 0f;
            var noteY = GetNoteCoverYFromBottom(viewport.rect.height);

            _bossTrackFrame.sizeDelta = new Vector2(width, height);
            _bossTrackFrame.anchoredPosition = new Vector2(scrollX, noteY);

            var fill = _bossTrackFrame.Find("Fill")?.GetComponent<Image>();
            if (fill != null)
            {
                fill.color = bossTrackFrameFill;
            }

            var borderH = Mathf.Max(1f, bossTrackFrameBorderThickness);
            var half = height * 0.5f - borderH * 0.5f;

            LayoutBossTrackBorder(_bossTrackFrame.Find("BorderTop") as RectTransform, half, borderH);
            LayoutBossTrackBorder(_bossTrackFrame.Find("BorderBottom") as RectTransform, -half, borderH);

            var topImg = _bossTrackFrame.Find("BorderTop")?.GetComponent<Image>();
            var botImg = _bossTrackFrame.Find("BorderBottom")?.GetComponent<Image>();
            if (topImg != null)
            {
                topImg.color = bossTrackFrameBorderTop;
            }

            if (botImg != null)
            {
                botImg.color = bossTrackFrameBorderBottom;
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

            var changed = false;
            var idx = 0;
            foreach (var unit in _session.Grid.PlayerUnits)
            {
                if (unit == null || !unit.IsAlive)
                {
                    continue;
                }

                if (idx >= _laneUnits.Count || _laneUnits[idx] != unit)
                {
                    changed = true;
                    break;
                }

                idx++;
            }

            if (!changed && idx != _laneUnits.Count)
            {
                changed = true;
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

            var height = viewport.rect.height;
            for (var i = 0; i < _laneLines.Count; i++)
            {
                if (_laneLines[i] == null)
                {
                    continue;
                }

                _laneLines[i].anchoredPosition = new Vector2(0f, GetLaneYFromBottom(i, height));
            }

            LayoutBossTrackFrame();
        }

        private void EnsureLaneAvatarColumn()
        {
            if (_laneUnits.Count == 0)
            {
                return;
            }

            if (laneAvatarGutter == null)
            {
                laneAvatarGutter = transform.Find("LaneAvatarGutter") as RectTransform;
            }

            if (laneAvatarGutter == null)
            {
                var go = new GameObject("LaneAvatarGutter", typeof(RectTransform));
                laneAvatarGutter = go.GetComponent<RectTransform>();
                laneAvatarGutter.SetParent(transform, false);
                laneAvatarGutter.anchorMin = new Vector2(0f, 0f);
                laneAvatarGutter.anchorMax = new Vector2(0f, 1f);
                laneAvatarGutter.pivot = new Vector2(0f, 0.5f);
                laneAvatarGutter.sizeDelta = new Vector2(LaneAvatarGutterWidth, 0f);
                laneAvatarGutter.anchoredPosition = Vector2.zero;
            }

            foreach (var slot in _laneAvatarSlots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }

            _laneAvatarSlots.Clear();

            var viewportHeight = viewport != null ? viewport.rect.height : 100f;
            for (var i = 0; i < _laneUnits.Count; i++)
            {
                var unit = _laneUnits[i];
                var slotGo = new GameObject($"LaneAvatar_{i}", typeof(RectTransform));
                var slotRect = slotGo.GetComponent<RectTransform>();
                slotRect.SetParent(laneAvatarGutter, false);

                var laneY = GetLaneYFromBottom(i, viewportHeight);
                slotRect.anchorMin = new Vector2(0.5f, 0f);
                slotRect.anchorMax = new Vector2(0.5f, 0f);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.anchoredPosition = new Vector2(0f, laneY);
                slotRect.sizeDelta = new Vector2(LaneAvatarSlotSize, LaneAvatarSlotSize);

                var slotView = slotGo.AddComponent<TimelineLaneAvatarSlotView>();
                slotView.Bind(unit, _onLaneAvatarClicked);
                slotView.SetSelected(unit == _selectedLaneUnit);
                _laneAvatarSlots.Add(slotView);
            }
        }

        private float GetLaneYFromBottom(int laneIndex, float height)
        {
            var count = _laneUnits.Count;
            var minY = height * Mathf.Min(laneBandMinNormalizedY, laneBandMaxNormalizedY);
            var maxY = height * Mathf.Max(laneBandMinNormalizedY, laneBandMaxNormalizedY);

            if (count <= 0)
            {
                return (minY + maxY) * 0.5f;
            }

            if (count == 1)
            {
                return (minY + maxY) * 0.5f;
            }

            var t = (float)laneIndex / (count - 1);
            return Mathf.Lerp(maxY, minY, t);
        }

        private float GetNoteCoverYFromBottom(float viewportHeight) =>
            viewportHeight * noteBandNormalizedY;

        private float ContentXForBeat(int beat)
        {
            if (_slotOffsetPx == null || _slotWidths == null || beat < 0 || beat >= TotalBeats)
            {
                return 0f;
            }

            return _slotOffsetPx[beat] + _slotWidths[beat] * 0.5f;
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

                var laneY = GetLaneYFromBottom(laneIdx, height);
                var key = (entry.Unit, beat);
                wanted.Add(key);

                var pos = new Vector2(ContentXForActiveCenter(entry.Skill, beat), laneY);
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

                RefreshFootprintDots(entry, laneY, wantedFootprint);
            }

            ReconcileLaneMarkers(wanted);
            ReconcileFootprintDots(wantedFootprint);
            RefreshLaneMarkerDragWiring();

            SyncLaneMarkersScroll();
        }

        /// <summary>S1/S2 xám nhỏ · mọi beat S = tròn to trên lane.</summary>
        private void RefreshFootprintDots(AgendaEntry entry, float laneY, HashSet<(CombatUnit unit, int beat)> wanted)
        {
            var skill = entry.Skill;
            var placement = entry.BeatIndex;
            var unitColor = entry.Unit.PlaceholderColor;

            foreach (var info in SkillFootprintUtil.EnumerateFootprintBeats(skill, placement, entry.Unit))
            {
                if (info.BeatIndex < 0 || info.BeatIndex >= TotalBeats)
                {
                    continue;
                }

                if (info.Role == FootprintBeatRole.Active)
                {
                    var color = new Color(unitColor.r, unitColor.g, unitColor.b, 0.95f);
                    TryPlaceFootprintDot(entry.Unit, info.BeatIndex, laneY, color, wanted, activeFootprintDotSize, placement, enableDrag: true);
                    continue;
                }

                TryPlaceFootprintDot(entry.Unit, info.BeatIndex, laneY, StandingDotColor, wanted, footprintDotSize, placement, enableDrag: false);
            }
        }

        private void TryPlaceFootprintDot(CombatUnit unit, int beat, float laneY, Color color,
            HashSet<(CombatUnit unit, int beat)> wanted, float size, int placementBeat, bool enableDrag)
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
                dot.color = color;
                dot.rectTransform.anchoredPosition = pos;
                dot.rectTransform.sizeDelta = new Vector2(size, size);
                ConfigureFootprintDotInteraction(dot, unit, placementBeat, enableDrag);
                return;
            }

            var created = CreateFootprintDot(size);
            created.color = color;
            created.rectTransform.anchoredPosition = pos;
            _footprintDots[key] = created;
            ConfigureFootprintDotInteraction(created, unit, placementBeat, enableDrag);
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
        }

        private void RefreshBlockBarriers()
        {
            EnsureBlockBarrierLayer();
            if (_blockBarrierLayer == null || _blockBarriers == null || !_slotsBuilt)
            {
                return;
            }

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
                rect.sizeDelta = new Vector2(8f, height * 0.85f);
                rect.anchoredPosition = new Vector2(ContentXForBeat(barrier.BeatIndex), height * 0.5f);

                var img = go.AddComponent<Image>();
                img.color = new Color(0.3f, 0.75f, 1f, 0.75f);
                img.raycastTarget = false;
                _blockBarrierViews.Add(img);
            }

            SyncBlockBarrierScroll();
        }

        private void SyncBlockBarrierScroll()
        {
            if (_blockBarrierLayer == null || slotsRow == null)
            {
                return;
            }

            _blockBarrierLayer.anchoredPosition = new Vector2(slotsRow.anchoredPosition.x, 0f);
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
                _laneMarkersLayer.anchoredPosition = new Vector2(x, 0f);
            }

            if (_footprintLayer != null)
            {
                _footprintLayer.anchoredPosition = new Vector2(x, 0f);
            }

            if (_bossTrackFrame != null)
            {
                LayoutBossTrackFrame();
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

            var valid = _timeline != null && _timeline.CanAssignAction(unit, skill, beat);
            var gapAnchor = SkillFootprintUtil.UsesGapCenterAnchor(skill);
            var laneY = GetLaneYFromBottom(laneIdx, viewport.rect.height);
            var unitColor = unit.PlaceholderColor;
            var previewAlpha = valid ? 0.55f : 0.35f;
            var centerX = ContentXForActiveCenter(skill, beat);
            var catalog = NoteVisuals;
            var ghostSprite = catalog.DropGhost(valid);
            var ghostSize = catalog.GhostDisplaySize;
            var coverSprite = catalog.Cover(valid);
            var coverSize = catalog.CoverDisplaySize;
            var noteCoverY = GetNoteCoverYFromBottom(viewport.rect.height);

            if (_dropGhost == null)
            {
                _dropGhost = CreateLaneMarker(unit, beat, enableDrag: false);
            }

            _dropGhost.gameObject.SetActive(true);
            _dropGhost.SetGapAnchorMode(gapAnchor);
            _dropGhost.SetContent(unit, skill);
            _dropGhost.SetGhost(true);
            _dropGhost.SetLanePosition(new Vector2(centerX, laneY), false);
            if (!valid && !gapAnchor)
            {
                _dropGhost.SetInvalidPreview(true);
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
                        AddDropPreviewSprite(info.BeatIndex, laneY, ghostSprite, ghostSize);
                    }
                    else
                    {
                        var color = valid
                            ? new Color(unitColor.r, unitColor.g, unitColor.b, previewAlpha)
                            : new Color(1f, 0.25f, 0.2f, previewAlpha);
                        AddDropPreviewDot(info.BeatIndex, laneY, color, activeFootprintDotSize);
                    }

                    if (_timeline != null
                        && coverSprite != null
                        && _timeline.GetImpactTelegraphAtBeat(info.BeatIndex) != null)
                    {
                        AddDropCoverOverlay(info.BeatIndex, noteCoverY, coverSprite, coverSize);
                    }

                    continue;
                }

                var standingColor = valid
                    ? new Color(StandingDotColor.r, StandingDotColor.g, StandingDotColor.b, previewAlpha)
                    : new Color(1f, 0.25f, 0.2f, previewAlpha);
                AddDropPreviewDot(info.BeatIndex, laneY, standingColor, footprintDotSize);
            }
        }

        private void AddDropPreviewSprite(int beat, float laneY, Sprite sprite, float size)
        {
            var dot = CreateFootprintDot(size);
            dot.sprite = sprite;
            dot.color = Color.white;
            dot.preserveAspect = true;
            dot.rectTransform.anchoredPosition = new Vector2(ContentXForBeat(beat), laneY);
            dot.gameObject.SetActive(true);
            _dropPreviewDots.Add(dot);
        }

        private void AddDropCoverOverlay(int beat, float noteY, Sprite sprite, float size)
        {
            EnsureLaneLayers();
            if (_footprintLayer == null)
            {
                return;
            }

            var go = new GameObject("DropCover", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_footprintLayer, false);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
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
            return slotWidth * 0.5f;
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
            trackLine.anchoredPosition = new Vector2(0f, 6f);
            trackLine.sizeDelta = new Vector2(0f, 2f);
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

        private float GetTemplateSlotWidth()
        {
            const float fallback = 52f;
            var maxSane = Mathf.Max(fallback * 4f, minSlotWidth * 4f);

            if (segmentTemplate != null)
            {
                var layoutElement = segmentTemplate.GetComponent<LayoutElement>();
                if (layoutElement != null && layoutElement.preferredWidth > 0f)
                {
                    return Mathf.Clamp(layoutElement.preferredWidth, minSlotWidth, maxSane);
                }

                if (segmentTemplate.TryGetComponent<RectTransform>(out var rect))
                {
                    var stretchX = Mathf.Abs(rect.anchorMax.x - rect.anchorMin.x) > 0.01f;
                    if (!stretchX && rect.sizeDelta.x > 0f)
                    {
                        return Mathf.Clamp(rect.sizeDelta.x, minSlotWidth, maxSane);
                    }
                }
            }

            return slotWidth > 0f ? Mathf.Clamp(slotWidth, minSlotWidth, maxSane) : fallback;
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

            if (slotWidth <= 0f)
            {
                slotWidth = GetTemplateSlotWidth();
            }

            if (!_slotsBuilt || _slots == null || _slots.Length != TotalBeats)
            {
                BuildAllSlots();
            }

            _pixelsPerSecond = ComputePixelsPerSecond();
            _roundStartBeatIndex = _isPlaybackActive ? Mathf.RoundToInt(_roundStartMusicalBeat) : 0;

            if (_slotWidths == null || _slotWidths.Length != TotalBeats)
            {
                _slotWidths = new float[TotalBeats];
            }

            if (_slotOffsetPx == null || _slotOffsetPx.Length != TotalBeats + 1)
            {
                _slotOffsetPx = new float[TotalBeats + 1];
            }

            DisableSlotsRowLayoutGroup();

            var cumulative = 0f;
            for (var i = 0; i < TotalBeats; i++)
            {
                var span = GetSpanSec(_roundStartBeatIndex + i);
                var w = Mathf.Max(minSlotWidth, span * _pixelsPerSecond);
                _slotWidths[i] = w;
                _slotOffsetPx[i] = cumulative;
                ApplySlotRect(_slots[i], w, cumulative);
                cumulative += w;
            }

            _slotOffsetPx[TotalBeats] = cumulative;
            _contentWidthPx = cumulative;

            slotsRow.sizeDelta = new Vector2(cumulative, 0f);

            _lastViewportWidth = viewport.rect.width;

            EnsureLaneLayers();
            LayoutLanes();
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

            width = Mathf.Max(minSlotWidth, width);

            var layoutElement = slot.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = true;
                layoutElement.minWidth = -1f;
                layoutElement.preferredWidth = -1f;
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

            if (slotWidth <= 0f)
            {
                slotWidth = GetTemplateSlotWidth();
            }

            var templateWidth = slotWidth > 0f ? slotWidth : 52f;

            _slots = new BeatSegmentView[TotalBeats];
            _slots[0] = segmentTemplate;
            segmentTemplate.SetDisplayBeatIndex(0);
            segmentTemplate.WireReferences();
            segmentTemplate.SetNoteVisualCatalog(NoteVisuals);
            segmentTemplate.SetNoteBandNormalizedY(noteBandNormalizedY);
            ApplySlotRect(segmentTemplate, templateWidth, 0f);

            for (var i = 1; i < TotalBeats; i++)
            {
                var cloneGo = Instantiate(segmentTemplate.gameObject, slotsRow);
                cloneGo.name = $"BeatSlot_{i}";
                MarkRuntimeClone(cloneGo);
                var clone = cloneGo.GetComponent<BeatSegmentView>();
                clone.SetDisplayBeatIndex(i);
                clone.WireReferences();
                clone.SetNoteVisualCatalog(NoteVisuals);
                clone.SetNoteBandNormalizedY(noteBandNormalizedY);
                ApplySlotRect(clone, templateWidth, i * templateWidth);
                _slots[i] = clone;
            }

            _slotsBuilt = true;
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
                slotsRow.anchoredPosition = Vector2.zero;
            }
        }

        private void OnDestroy()
        {
            StopAutoPlay();
            if (_session != null)
            {
                _session.OnScanBeat -= HandleScanBeat;
                _session.OnTelegraphsPlanned -= HandleTelegraphsPlanned;
                _session.OnEncounterEnded -= HandleEncounterEnded;
            }
        }

        private void AlignSlotsRowInViewport()
        {
            if (slotsRow == null || viewport == null)
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
            slotsRow.anchoredPosition = Vector2.zero;
            slotsRow.sizeDelta = new Vector2(_contentWidthPx, 0f);
        }

        private void AlignScanBar()
        {
            if (scanBar == null)
            {
                return;
            }

            scanBar.anchorMin = new Vector2(0f, 0f);
            scanBar.anchorMax = new Vector2(0f, 1f);
            scanBar.pivot = new Vector2(0.5f, 0.5f);
            scanBar.anchoredPosition = new Vector2(GetScanLineX(), 0f);
            scanBar.sizeDelta = new Vector2(6f, -4f);
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
                if (child.name == "Beat_0")
                {
                    continue;
                }

                if (child.name.StartsWith("Beat_") || child.name.StartsWith("BeatSlot_"))
                {
                    DestroyBeatClone(child.gameObject);
                }
            }
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

        public void RefreshBeat(int beatIndex)
        {
            if (_slots == null || _timeline == null)
            {
                return;
            }

            if (beatIndex >= 0 && beatIndex < _slots.Length)
            {
                PopulateSlot(_slots[beatIndex], beatIndex);
            }
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
                PopulateSlot(_slots[i], i);
            }

            ReapplySlotRectsFromCache();
            RefreshLaneMarkers();
        }

        private void ReapplySlotRectsFromCache()
        {
            if (_slots == null || _slotWidths == null || _slotOffsetPx == null)
            {
                return;
            }

            var count = Mathf.Min(_slots.Length, _slotWidths.Length);
            for (var i = 0; i < count; i++)
            {
                ApplySlotRect(_slots[i], _slotWidths[i], _slotOffsetPx[i]);
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
            var telegraph = _timeline.GetTelegraphAtBeat(globalBeat);
            slot.SetSlot(playerEntry, telegraph);
            slot.CaptureLayoutBaseline();
        }

        public void SetScanSpeedMultiplier(float multiplier)
        {
            _scanSpeedMultiplier = Mathf.Max(0.001f, multiplier);
            musicController?.SetPlaybackSpeedMultiplier(multiplier);
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
    }
}

using FracturedChorus.Audio;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using System;
using System.Collections;
using System.Collections.Generic;
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
        [Tooltip("Footprint dot size (gray S1/S2 · colored active S) around the skill chip.")]
        [SerializeField] private float footprintDotSize = 16f;
        [SerializeField] private bool autoPlayOnStart;
        [SerializeField] private float autoBeatInterval = 0.405405f;
        [SerializeField] private bool useMusicSync = true;
        [SerializeField] private CombatMusicController musicController;
        [SerializeField] private float skillPanelOpenSpeedMultiplier = 0.25f;
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
        private Action<int> _onScanBeatReached;
        private Action _onPlanningPause;
        private Action _onConfirmPlanning;
        private float _scanSpeedMultiplier = 1f;
        private float _totalScrollPx;
        private float _localBeat;
        private int _lastFiredBeat = -1;
        private bool _isPlaybackActive;
        private bool _planningPauseArmed;
        private bool _pausedForPlanning;
        private int _lastHighlightedSlotIndex = -1;
        private float _roundStartMusicalBeat;

        private RectTransform _laneLinesLayer;
        private RectTransform _laneMarkersLayer;
        private RectTransform _footprintLayer;
        private readonly List<CombatUnit> _laneUnits = new();
        private readonly Dictionary<CombatUnit, int> _laneIndex = new();
        private readonly List<RectTransform> _laneLines = new();
        private readonly Dictionary<(CombatUnit unit, int beat), Image> _footprintDots = new();
        private readonly Dictionary<(CombatUnit unit, int beat), TimelineLaneMarkerView> _laneMarkers = new();
        private TimelineLaneMarkerView _dropGhost;

        private static readonly Color StandingDotColor = new Color(0.55f, 0.55f, 0.6f, 0.85f);

        // Intro-pause theo vị trí vạch quét (đơn vị = beat, phân số). 0.5 = vạch nằm giữa beat 0 và beat 1:
        // beat 0 đã kêu + lướt qua vạch, dừng TRƯỚC khi beat 1 chạm vạch. -1 = tắt.
        // (const để scene serialize không ghi đè — chỉnh giá trị này để dời điểm dừng.)
        private const float PlanningPauseLocalBeat = 0.5f;

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

            ConfigureAvLabelLayout();
            ExpandViewportWidth();
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
        }

        public void Bind(BeatTimelineEngine timeline, CombatSession session, Action<int> onScanBeatReached = null,
            CombatMusicController music = null, Action onPlanningPause = null, Action onConfirmPlanning = null)
        {
            if (music != null)
            {
                musicController = music;
            }

            _timeline = timeline;
            _session = session;
            _onScanBeatReached = onScanBeatReached;
            _onPlanningPause = onPlanningPause;
            _onConfirmPlanning = onConfirmPlanning;
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
        }

        public void BeginRoundPlayback()
        {
            if (_isPlaybackActive)
            {
                return;
            }

            _autoPlayCompleted = false;
            _pausedForPlanning = false;
            _planningPauseArmed = PlanningPauseLocalBeat >= 0f;

            if (_autoPlayRoutine != null)
            {
                StopCoroutine(_autoPlayRoutine);
            }

            _autoPlayRoutine = CanUseMusicSync()
                ? StartCoroutine(MusicDrivenScanRoutine(false))
                : StartCoroutine(ContinuousScanRoutine(false));
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
            _planningPauseArmed = false;
            _pausedForPlanning = true;
            _isPlaybackActive = false;
            musicController?.PausePlayback();
            ResetAllScanHighlights();
            RefreshLaneMarkers();
            Debug.Log($"[BeatTimeline] Intro-pause at localBeat={_localBeat:F2} (threshold {PlanningPauseLocalBeat}). Press Continue to resume.");
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

        private IEnumerator MusicDrivenScanRoutine(bool resume)
        {
            if (musicController == null || !musicController.IsPlaying)
            {
                yield return ContinuousScanRoutine(resume);
                yield break;
            }

            _isPlaybackActive = true;

            if (!resume)
            {
                _roundStartMusicalBeat = musicController.TotalMusicalBeat;
                RebuildLayout();
                ResetScrollState();
                ApplyScrollVisual(0f);
                ProcessCrossedBeats();
            }

            EnsureTrackLine();

            while (_isPlaybackActive)
            {
                _localBeat = musicController.TotalMusicalBeat - _roundStartMusicalBeat;
                if (_localBeat >= TotalBeats)
                {
                    break;
                }

                _totalScrollPx = PxOfLocalBeat(_localBeat);
                ApplyScrollVisual(_totalScrollPx);
                ProcessCrossedBeats();

                if (TryEnterPlanningPauseByLocalBeat())
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

            FinishPlayback();
        }

        private IEnumerator ContinuousScanRoutine(bool resume)
        {
            _isPlaybackActive = true;

            if (!resume)
            {
                _roundStartMusicalBeat = 0f;
                RebuildLayout();
                ResetScrollState();
                ApplyScrollVisual(0f);
                ProcessCrossedBeats();
            }

            EnsureTrackLine();

            while (_isPlaybackActive && _localBeat < TotalBeats)
            {
                _localBeat += Time.deltaTime / GetBeatWaitDuration();
                _totalScrollPx = PxOfLocalBeat(_localBeat);
                ApplyScrollVisual(_totalScrollPx);
                ProcessCrossedBeats();

                if (TryEnterPlanningPauseByLocalBeat())
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

            FinishPlayback();
        }

        private void FinishPlayback()
        {
            _isPlaybackActive = false;
            _autoPlayCompleted = true;
            _autoPlayRoutine = null;
            ResetAllScanHighlights();

            if (_session != null && _session.Phase == CombatPhase.Planning)
            {
                _onConfirmPlanning?.Invoke();
            }
        }

        public void StopTimelinePlayback()
        {
            StopAutoPlay();
            _autoPlayCompleted = true;
            _pausedForPlanning = false;
            _planningPauseArmed = false;
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

        private float PxOfLocalBeat(float localBeat)
        {
            if (_slotOffsetPx == null || _slotWidths == null)
            {
                return 0f;
            }

            if (localBeat <= 0f)
            {
                return 0f;
            }

            if (localBeat >= TotalBeats)
            {
                return _contentWidthPx;
            }

            var k = Mathf.FloorToInt(localBeat);
            var frac = localBeat - k;
            return _slotOffsetPx[k] + frac * _slotWidths[k];
        }

        private void ProcessCrossedBeats()
        {
            var beatIndex = Mathf.Clamp(Mathf.FloorToInt(_localBeat), 0, TotalBeats - 1);

            while (_lastFiredBeat < beatIndex)
            {
                FireScanBeat(_lastFiredBeat + 1);
                if (_session != null && _session.IsEncounterOver)
                {
                    _isPlaybackActive = false;
                    return;
                }
            }
        }

        /// <summary>
        /// Intro-pause dựa trên vị trí vạch quét (phân số): cho beat 0 kêu + lướt qua vạch, rồi dừng
        /// NGAY TRƯỚC khi beat kế chạm vạch. Trả về true nếu vừa pause.
        /// </summary>
        private bool TryEnterPlanningPauseByLocalBeat()
        {
            if (!_planningPauseArmed || PlanningPauseLocalBeat < 0f)
            {
                return false;
            }

            if (_localBeat < PlanningPauseLocalBeat)
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
            _onScanBeatReached?.Invoke(beat);
            _session?.ResolveBeatAtScan(beat);
            RefreshBeat(beat);
            RefreshPhaseAvLabel();
            UpdateScanHighlights();
        }

        private void ApplyScrollVisual(float scrollPx)
        {
            if (slotsRow == null || scanBar == null || !_slotsBuilt)
            {
                return;
            }

            var viewportWidth = GetViewportWidth();
            var maxScroll = Mathf.Max(0f, _contentWidthPx - viewportWidth);
            var readLineX = GetScanLineX();

            if (scrollPx <= maxScroll)
            {
                slotsRow.anchoredPosition = new Vector2(-scrollPx, 0f);
                scanBar.anchoredPosition = new Vector2(readLineX, 0f);
            }
            else
            {
                slotsRow.anchoredPosition = new Vector2(-maxScroll, 0f);
                var sweep = scrollPx - maxScroll;
                var scanX = Mathf.Min(readLineX + sweep, Mathf.Max(readLineX, viewportWidth - readLineX));
                scanBar.anchoredPosition = new Vector2(scanX, 0f);
            }

            SyncLaneMarkersScroll();
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
                var go = new GameObject("LaneLines", typeof(RectTransform));
                _laneLinesLayer = go.GetComponent<RectTransform>();
                _laneLinesLayer.SetParent(viewport, false);
                _laneLinesLayer.anchorMin = Vector2.zero;
                _laneLinesLayer.anchorMax = Vector2.one;
                _laneLinesLayer.offsetMin = Vector2.zero;
                _laneLinesLayer.offsetMax = Vector2.zero;
            }

            if (_footprintLayer == null)
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

            if (_laneMarkersLayer == null)
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

            _footprintLayer.SetAsLastSibling();
            _laneMarkersLayer.SetAsLastSibling();
            _footprintLayer.sizeDelta = new Vector2(_contentWidthPx, 0f);
            _laneMarkersLayer.sizeDelta = new Vector2(_contentWidthPx, 0f);
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
                lineRect.sizeDelta = new Vector2(0f, 2f);
                var lineImage = lineGo.AddComponent<Image>();
                var tint = unit.PlaceholderColor;
                lineImage.color = new Color(tint.r, tint.g, tint.b, 0.35f);
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
        }

        private float GetLaneYFromBottom(int laneIndex, float height)
        {
            var count = _laneUnits.Count;
            if (count <= 0)
            {
                return height * 0.5f;
            }

            // Lane 0 ở TRÊN cùng, cách đều theo chiều cao viewport.
            return height * (count - laneIndex) / (count + 1);
        }

        private float ContentXForBeat(int beat)
        {
            if (_slotOffsetPx == null || _slotWidths == null || beat < 0 || beat >= TotalBeats)
            {
                return 0f;
            }

            return _slotOffsetPx[beat] + _slotWidths[beat] * 0.5f;
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

                var pos = new Vector2(ContentXForBeat(beat), laneY);
                if (_laneMarkers.TryGetValue(key, out var existing) && existing != null)
                {
                    existing.SetContent(entry.Unit, entry.Skill);
                    existing.SetLanePosition(pos, false);
                }
                else
                {
                    var marker = CreateLaneMarker();
                    marker.SetContent(entry.Unit, entry.Skill);
                    marker.SetLanePosition(pos, true);
                    _laneMarkers[key] = marker;
                }

                RefreshFootprintDots(entry, laneY, wantedFootprint);
            }

            ReconcileLaneMarkers(wanted);
            ReconcileFootprintDots(wantedFootprint);

            SyncLaneMarkersScroll();
        }

        /// <summary>Vẽ điểm tròn footprint quanh chip skill: S1 (xám) trước · S phụ (màu unit) · S2 (xám) sau.</summary>
        private void RefreshFootprintDots(AgendaEntry entry, float laneY, HashSet<(CombatUnit unit, int beat)> wanted)
        {
            var skill = entry.Skill;
            var placement = entry.BeatIndex; // beat bắt đầu Using (S)
            var s1 = Mathf.Max(0, skill.standingBeatsBefore);
            var active = Mathf.Max(1, skill.activeBeats);
            var s2 = Mathf.Max(0, skill.standingBeatsAfter);
            var unitColor = entry.Unit.PlaceholderColor;

            // Standing Phase 1 (xám) — trước placement.
            for (var i = 1; i <= s1; i++)
            {
                TryPlaceFootprintDot(entry.Unit, placement - i, laneY, StandingDotColor, wanted);
            }

            // Using Phase phụ (màu unit) — các beat active sau placement (beat placement đã có chip).
            for (var i = 1; i < active; i++)
            {
                TryPlaceFootprintDot(entry.Unit, placement + i, laneY,
                    new Color(unitColor.r, unitColor.g, unitColor.b, 0.9f), wanted);
            }

            // Standing Phase 2 (xám) — sau khi hết Using.
            for (var i = 0; i < s2; i++)
            {
                TryPlaceFootprintDot(entry.Unit, placement + active + i, laneY, StandingDotColor, wanted);
            }
        }

        private void TryPlaceFootprintDot(CombatUnit unit, int beat, float laneY, Color color,
            HashSet<(CombatUnit unit, int beat)> wanted)
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
                return;
            }

            var created = CreateFootprintDot();
            created.color = color;
            created.rectTransform.anchoredPosition = pos;
            _footprintDots[key] = created;
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

        private Image CreateFootprintDot()
        {
            var go = new GameObject("FootprintDot", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_footprintLayer, false);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(footprintDotSize, footprintDotSize);

            var img = go.AddComponent<Image>();
            img.sprite = UiCircleSpriteUtil.Circle;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            return img;
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

        private TimelineLaneMarkerView CreateLaneMarker()
        {
            var go = new GameObject("LaneMarker", typeof(RectTransform));
            var marker = go.AddComponent<TimelineLaneMarkerView>();
            marker.Build(_laneMarkersLayer, laneMarkerSize);
            return marker;
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

        public void ShowDropGhost(CombatUnit unit, Vector2 screen)
        {
            EnsureLaneLayers();

            if (_laneMarkersLayer == null || unit == null || viewport == null)
            {
                HideDropGhost();
                return;
            }

            if (!_laneIndex.TryGetValue(unit, out var laneIdx) || !TryGetBeatAtScreenPoint(screen, out var beat))
            {
                HideDropGhost();
                return;
            }

            if (_dropGhost == null)
            {
                _dropGhost = CreateLaneMarker();
                _dropGhost.SetGhost(true);
            }

            _dropGhost.gameObject.SetActive(true);
            _dropGhost.SetContent(unit, null);
            _dropGhost.SetGhost(true);
            var pos = new Vector2(ContentXForBeat(beat), GetLaneYFromBottom(laneIdx, viewport.rect.height));
            _dropGhost.SetLanePosition(pos, false);
        }

        public void HideDropGhost()
        {
            if (_dropGhost != null)
            {
                _dropGhost.gameObject.SetActive(false);
            }
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
            if (segmentTemplate != null)
            {
                var layoutElement = segmentTemplate.GetComponent<LayoutElement>();
                if (layoutElement != null && layoutElement.preferredWidth > 0f)
                {
                    return layoutElement.preferredWidth;
                }

                if (segmentTemplate.TryGetComponent<RectTransform>(out var rect))
                {
                    if (rect.sizeDelta.x > 0f)
                    {
                        return rect.sizeDelta.x;
                    }

                    if (rect.rect.width > 0f)
                    {
                        return rect.rect.width;
                    }
                }
            }

            return slotWidth;
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

            var cumulative = 0f;
            for (var i = 0; i < TotalBeats; i++)
            {
                var span = GetSpanSec(_roundStartBeatIndex + i);
                var w = Mathf.Max(minSlotWidth, span * _pixelsPerSecond);
                _slotWidths[i] = w;
                _slotOffsetPx[i] = cumulative;
                ApplyWidth(_slots[i], w);
                cumulative += w;
            }

            _slotOffsetPx[TotalBeats] = cumulative;
            _contentWidthPx = cumulative;

            EnsureLayoutGroup();
            slotsRow.sizeDelta = new Vector2(cumulative, 0f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(slotsRow);

            _lastViewportWidth = viewport.rect.width;

            EnsureLaneLayers();
            LayoutLanes();
            if (_timeline != null)
            {
                RefreshLaneMarkers();
            }
        }

        private void ApplyWidth(BeatSegmentView slot, float width)
        {
            if (slot == null)
            {
                return;
            }

            var layoutElement = slot.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = slot.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredWidth = width;
            layoutElement.minWidth = width;
            layoutElement.flexibleWidth = 0f;
        }

        private void EnsureLayoutGroup()
        {
            if (slotsRow == null)
            {
                return;
            }

            var layout = slotsRow.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = slotsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layout.spacing = 0f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        private void BuildAllSlots()
        {
            CleanupExtraBeatChildren();
            AlignSlotsRowInViewport();
            EnsureLayoutGroup();

            _slots = new BeatSegmentView[TotalBeats];
            _slots[0] = segmentTemplate;
            segmentTemplate.SetDisplayBeatIndex(0);
            segmentTemplate.WireReferences();

            for (var i = 1; i < TotalBeats; i++)
            {
                var cloneGo = Instantiate(segmentTemplate.gameObject, slotsRow);
                cloneGo.name = $"BeatSlot_{i}";
                MarkRuntimeClone(cloneGo);
                var clone = cloneGo.GetComponent<BeatSegmentView>();
                clone.SetDisplayBeatIndex(i);
                clone.WireReferences();
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
            _localBeat = beatIndex;
            _totalScrollPx = PxOfLocalBeat(_localBeat);
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

            RefreshLaneMarkers();
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
            SetScanSpeedMultiplier(open ? skillPanelOpenSpeedMultiplier : 1f);
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
            if (avLabel == null)
            {
                WireReferences();
            }

            if (_session == null || avLabel == null)
            {
                return;
            }

            var tracker = _session.PhaseAv;
            avLabel.text = $"Cycle {tracker.Remaining}/{tracker.CurrentBudget}";
        }

        public void SetAvDisplay(string text)
        {
            if (avLabel != null)
            {
                avLabel.text = text;
            }
        }
    }
}

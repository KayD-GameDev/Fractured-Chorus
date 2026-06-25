using FracturedChorus.Audio;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Timeline;
using System;
using System.Collections;
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
        [SerializeField] private bool autoPlayOnStart;
        [SerializeField] private float autoBeatInterval = 0.405405f;
        [SerializeField] private bool useMusicSync = true;
        [SerializeField] private CombatMusicController musicController;
        [SerializeField] private float skillPanelOpenSpeedMultiplier = 0.25f;

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
        private float _scanSpeedMultiplier = 1f;
        private float _totalScrollPx;
        private float _localBeat;
        private int _lastFiredBeat = -1;
        private bool _isPlaybackActive;
        private int _lastHighlightedSlotIndex = -1;
        private float _roundStartMusicalBeat;

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
            if (viewport == null)
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
            CombatMusicController music = null)
        {
            if (music != null)
            {
                musicController = music;
            }

            _timeline = timeline;
            _session = session;
            _onScanBeatReached = onScanBeatReached;
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

            if (_autoPlayRoutine != null)
            {
                StopCoroutine(_autoPlayRoutine);
            }

            _autoPlayRoutine = CanUseMusicSync()
                ? StartCoroutine(MusicDrivenScanRoutine())
                : StartCoroutine(ContinuousScanRoutine());
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

        private IEnumerator MusicDrivenScanRoutine()
        {
            if (musicController == null || !musicController.IsPlaying)
            {
                yield return ContinuousScanRoutine();
                yield break;
            }

            _isPlaybackActive = true;
            _roundStartMusicalBeat = musicController.TotalMusicalBeat;
            RebuildLayout();
            ResetScrollState();
            ApplyScrollVisual(0f);
            ProcessCrossedBeats();
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

                if (_session != null && _session.IsEncounterOver)
                {
                    break;
                }

                yield return null;
            }

            FinishPlayback();
        }

        private IEnumerator ContinuousScanRoutine()
        {
            _isPlaybackActive = true;
            _roundStartMusicalBeat = 0f;
            RebuildLayout();
            ResetScrollState();
            ApplyScrollVisual(0f);
            ProcessCrossedBeats();
            EnsureTrackLine();

            while (_isPlaybackActive && _localBeat < TotalBeats)
            {
                _localBeat += Time.deltaTime / GetBeatWaitDuration();
                _totalScrollPx = PxOfLocalBeat(_localBeat);
                ApplyScrollVisual(_totalScrollPx);
                ProcessCrossedBeats();

                if (_session != null && _session.IsEncounterOver)
                {
                    break;
                }

                yield return null;
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
                FindAnyObjectByType<CombatController>()?.ConfirmPlanning();
            }
        }

        public void StopTimelinePlayback()
        {
            StopAutoPlay();
            _autoPlayCompleted = true;
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
                var clone = cloneGo.GetComponent<BeatSegmentView>();
                clone.SetDisplayBeatIndex(i);
                clone.WireReferences();
                _slots[i] = clone;
            }

            _slotsBuilt = true;
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
                    Destroy(child.gameObject);
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

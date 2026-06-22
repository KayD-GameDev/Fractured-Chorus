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
        [SerializeField] private Button confirmButton;
        [SerializeField] private Text phaseLabel;
        [SerializeField] private Text budgetLabel;
        [SerializeField] private Text avLabel;
        [SerializeField] private float slotWidth = 52f;
        [SerializeField] private float slotSpacing = 2f;
        [SerializeField] private float slideDuration = 0.12f;
        [SerializeField] private bool autoPlayOnStart = true;
        [SerializeField] private float autoBeatInterval = 0.35f;
        [SerializeField] private float skillPanelOpenSpeedMultiplier = 0.25f;

        private BeatTimelineEngine _timeline;
        private CombatSession _session;
        private BeatSegmentView[] _visibleSlots;
        private int _windowStart;
        private Coroutine _slideRoutine;
        private Coroutine _autoPlayRoutine;
        private bool _slotsBuilt;
        private bool _autoPlayCompleted;
        private float _lastViewportWidth;
        private int _autoPlayBeat;
        private Action<int> _onScanBeatReached;
        private float _layoutSpacing;
        private float _scanSpeedMultiplier = 1f;

        private void Awake()
        {
            WireReferences();
        }

        private void Start()
        {
            EnsureVisibleSlots();
            RefitSlotsToViewport();
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
                RefitSlotsToViewport();
                if (_autoPlayCompleted)
                {
                    EnsureBeatAlignedToScanBar(Mathf.Clamp(_autoPlayBeat, 0, TimelineConstants.TotalBeats - 1));
                }
                else
                {
                    RefreshVisibleWindow(_windowStart);
                    AlignScanBar();
                }
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

            HideExecuteButton();
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

        private void HideExecuteButton()
        {
            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(false);
            }

            var legacyButton = transform.Find("ConfirmButton");
            if (legacyButton != null)
            {
                legacyButton.gameObject.SetActive(false);
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

        public void Bind(BeatTimelineEngine timeline, CombatSession session, Action<int> onScanBeatReached = null)
        {
            _timeline = timeline;
            _session = session;
            _onScanBeatReached = onScanBeatReached;
            WireReferences();
            EnsureVisibleSlots();
            RefitSlotsToViewport();

            if (_session != null)
            {
                _session.OnScanBeat -= HandleScanBeat;
                _session.OnScanBeat += HandleScanBeat;
                _session.OnTelegraphsPlanned -= HandleTelegraphsPlanned;
                _session.OnTelegraphsPlanned += HandleTelegraphsPlanned;
                _session.OnEncounterEnded -= HandleEncounterEnded;
                _session.OnEncounterEnded += HandleEncounterEnded;
            }

            _windowStart = 0;
            RefreshVisibleWindow(0);
            RefreshPhaseHeader(0);
            RefreshPhaseAvLabel();
            StartAutoPlayIfNeeded();
        }

        private void StartAutoPlayIfNeeded()
        {
            if (!autoPlayOnStart || _autoPlayCompleted)
            {
                return;
            }

            if (_autoPlayRoutine != null)
            {
                StopCoroutine(_autoPlayRoutine);
            }

            _autoPlayRoutine = StartCoroutine(AutoPlayTimelineRoutine());
        }

        public void StopTimelinePlayback()
        {
            StopAutoPlay();
            _autoPlayCompleted = true;
        }

        private void HandleTelegraphsPlanned(int phaseIndex)
        {
            RefreshAll();
        }

        private void HandleEncounterEnded()
        {
            StopTimelinePlayback();
        }

        private void StopAutoPlay()
        {
            if (_autoPlayRoutine != null)
            {
                StopCoroutine(_autoPlayRoutine);
                _autoPlayRoutine = null;
            }
        }

        private IEnumerator AutoPlayTimelineRoutine()
        {
            RefitSlotsToViewport();
            ResetCarouselVisualState();
            RefreshVisibleWindow(0);

            for (var beat = 0; beat < TimelineConstants.TotalBeats; beat++)
            {
                _autoPlayBeat = beat;
                EnsureBeatAlignedToScanBar(beat);
                _session?.OnTimelineScanBeat(beat);
                RefreshPhaseHeader(beat);
                _onScanBeatReached?.Invoke(beat);
                _session?.ResolveBeatAtScan(beat);
                RefreshAll();

                if (_session != null && _session.IsEncounterOver)
                {
                    _autoPlayCompleted = true;
                    _autoPlayRoutine = null;
                    yield break;
                }

                yield return new WaitForSeconds(GetBeatWaitDuration());

                if (beat >= TimelineConstants.TotalBeats - 1)
                {
                    break;
                }

                var maxWindow = GetMaxWindowStart();
                if (beat < maxWindow)
                {
                    if (_slideRoutine != null)
                    {
                        StopCoroutine(_slideRoutine);
                    }

                    yield return SlideCarouselSteps(1);
                    AlignScanBar();
                }
            }

            _autoPlayCompleted = true;
            _autoPlayRoutine = null;

            if (_session != null && _session.Phase == CombatPhase.Planning)
            {
                FindAnyObjectByType<CombatController>()?.ConfirmPlanning();
            }
        }

        private void EnsureBeatAlignedToScanBar(int beat)
        {
            var maxWindow = GetMaxWindowStart();
            if (beat <= maxWindow)
            {
                if (_windowStart != beat)
                {
                    RefreshVisibleWindow(beat);
                }

                AlignScanBar();
                return;
            }

            if (_windowStart != maxWindow)
            {
                RefreshVisibleWindow(maxWindow);
            }

            UpdateScanBarForSlotIndex(beat - _windowStart);
        }

        private void UpdateScanBarForSlotIndex(int slotIndex)
        {
            if (scanBar == null)
            {
                return;
            }

            var clamped = Mathf.Clamp(slotIndex, 0, Mathf.Max(0, VisibleSlotCount - 1));
            var x = clamped * GetSlideStep() + GetSlotWidth() * 0.5f;
            scanBar.anchoredPosition = new Vector2(x, 0f);
        }

        private float GetSlotWidth()
        {
            if (_visibleSlots != null && _visibleSlots.Length > 0 &&
                _visibleSlots[0].TryGetComponent<RectTransform>(out var rt))
            {
                return rt.rect.width;
            }

            return slotWidth;
        }

        private int VisibleSlotCount => _visibleSlots?.Length ?? 0;

        private int GetMaxWindowStart()
        {
            return Mathf.Max(0, TimelineConstants.TotalBeats - VisibleSlotCount);
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

        private int CalculateSlotCountForViewport(float viewportWidth)
        {
            var templateWidth = GetTemplateSlotWidth();
            if (templateWidth <= 0f || viewportWidth <= 0f)
            {
                return 1;
            }

            var count = Mathf.FloorToInt((viewportWidth + slotSpacing) / (templateWidth + slotSpacing));
            return Mathf.Clamp(count, 1, TimelineConstants.TotalBeats);
        }

        private float GetSlideStep()
        {
            return GetSlotWidth() + _layoutSpacing;
        }

        private void RefitSlotsToViewport()
        {
            if (viewport == null || slotsRow == null || segmentTemplate == null)
            {
                return;
            }

            AlignSlotsRowInViewport();

            var viewportWidth = GetViewportWidth();
            if (viewportWidth <= 1f)
            {
                return;
            }

            _lastViewportWidth = viewportWidth;
            slotWidth = GetTemplateSlotWidth();

            var targetCount = CalculateSlotCountForViewport(viewportWidth);
            SyncVisibleSlotCount(targetCount);

            if (_timeline != null)
            {
                _timeline.VisibleWindowSize = VisibleSlotCount;
            }

            var rowWidth = VisibleSlotCount * slotWidth + (VisibleSlotCount - 1) * slotSpacing;
            _layoutSpacing = slotSpacing;
            if (VisibleSlotCount > 1 && rowWidth < viewportWidth - 0.5f)
            {
                _layoutSpacing = slotSpacing + (viewportWidth - rowWidth) / (VisibleSlotCount - 1);
            }

            var layout = slotsRow.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = _layoutSpacing;
            }

            slotsRow.sizeDelta = new Vector2(viewportWidth, 0f);
            ApplyTemplateWidthToAllSlots();
            LayoutRebuilder.ForceRebuildLayoutImmediate(slotsRow);
            AlignScanBar();
        }

        private void ApplyTemplateWidthToAllSlots()
        {
            if (_visibleSlots == null)
            {
                return;
            }

            foreach (var slot in _visibleSlots)
            {
                ApplySlotWidth(slot);
            }
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

            layout.spacing = _layoutSpacing > 0f ? _layoutSpacing : slotSpacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        private void SyncVisibleSlotCount(int targetCount)
        {
            if (segmentTemplate == null || slotsRow == null)
            {
                return;
            }

            targetCount = Mathf.Clamp(targetCount, 1, TimelineConstants.TotalBeats);
            EnsureLayoutGroup();

            if (!_slotsBuilt || _visibleSlots == null)
            {
                BuildVisibleSlots(targetCount);
                return;
            }

            if (_visibleSlots.Length == targetCount)
            {
                return;
            }

            if (_visibleSlots.Length < targetCount)
            {
                var expanded = new BeatSegmentView[targetCount];
                for (var i = 0; i < _visibleSlots.Length; i++)
                {
                    expanded[i] = _visibleSlots[i];
                }

                for (var i = _visibleSlots.Length; i < targetCount; i++)
                {
                    var cloneGo = Instantiate(segmentTemplate.gameObject, slotsRow);
                    cloneGo.name = $"BeatSlot_{i}";
                    var clone = cloneGo.GetComponent<BeatSegmentView>();
                    clone.SetDisplayBeatIndex(i);
                    clone.WireReferences();
                    expanded[i] = clone;
                }

                _visibleSlots = expanded;
            }
            else
            {
                for (var i = targetCount; i < _visibleSlots.Length; i++)
                {
                    if (_visibleSlots[i] != null)
                    {
                        Destroy(_visibleSlots[i].gameObject);
                    }
                }

                var trimmed = new BeatSegmentView[targetCount];
                for (var i = 0; i < targetCount; i++)
                {
                    trimmed[i] = _visibleSlots[i];
                }

                _visibleSlots = trimmed;
            }

            _windowStart = Mathf.Clamp(_windowStart, 0, GetMaxWindowStart());
            RefreshVisibleWindow(_windowStart);
        }

        private void BuildVisibleSlots(int targetCount)
        {
            CleanupExtraBeatChildren();
            AlignSlotsRowInViewport();
            EnsureLayoutGroup();

            _visibleSlots = new BeatSegmentView[targetCount];
            _visibleSlots[0] = segmentTemplate;
            segmentTemplate.SetDisplayBeatIndex(0);
            segmentTemplate.WireReferences();

            for (var i = 1; i < targetCount; i++)
            {
                var cloneGo = Instantiate(segmentTemplate.gameObject, slotsRow);
                cloneGo.name = $"BeatSlot_{i}";
                var clone = cloneGo.GetComponent<BeatSegmentView>();
                clone.SetDisplayBeatIndex(i);
                clone.WireReferences();
                _visibleSlots[i] = clone;
            }

            _slotsBuilt = true;
            _windowStart = 0;
            RefreshVisibleWindow(0);
        }

        private void ApplySlotWidth(BeatSegmentView slot)
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

            layoutElement.preferredWidth = slotWidth;
            layoutElement.minWidth = slotWidth;
            layoutElement.flexibleWidth = 0f;
        }

        private void ResetCarouselVisualState()
        {
            _windowStart = 0;
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

            var viewportWidth = Mathf.Max(viewport.rect.width, 1f);
            slotsRow.anchorMin = new Vector2(0f, 0f);
            slotsRow.anchorMax = new Vector2(0f, 1f);
            slotsRow.pivot = new Vector2(0f, 0.5f);
            slotsRow.anchoredPosition = Vector2.zero;
            slotsRow.offsetMin = Vector2.zero;
            slotsRow.offsetMax = Vector2.zero;
            slotsRow.sizeDelta = new Vector2(viewportWidth, 0f);
        }

        private void EnsureVisibleSlots()
        {
            if (segmentTemplate == null || slotsRow == null)
            {
                return;
            }

            RefitSlotsToViewport();
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
            scanBar.anchoredPosition = new Vector2(GetSlotWidth() * 0.5f, 0f);
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

            var nextWindowStart = beatIndex + 1;
            if (nextWindowStart >= TimelineConstants.TotalBeats)
            {
                return;
            }

            if (_slideRoutine != null)
            {
                StopCoroutine(_slideRoutine);
            }

            _slideRoutine = StartCoroutine(SlideCarouselSteps(1));
        }

        private IEnumerator SlideCarouselSteps(int steps)
        {
            for (var step = 0; step < steps; step++)
            {
                yield return SlideOneStep();
                _windowStart++;
                RefreshRightmostSlot();
            }

            _slideRoutine = null;
        }

        private IEnumerator SlideOneStep()
        {
            if (slotsRow == null)
            {
                yield break;
            }

            var layout = slotsRow.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.enabled = false;
            }

            var start = slotsRow.anchoredPosition;
            var end = start + Vector2.left * GetSlideStep();
            var elapsed = 0f;

            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / slideDuration);
                slotsRow.anchoredPosition = Vector2.Lerp(start, end, t);
                yield return null;
            }

            RotateSlotsLeft();
            slotsRow.anchoredPosition = start;

            if (layout != null)
            {
                layout.enabled = true;
            }
        }

        private void RotateSlotsLeft()
        {
            if (_visibleSlots == null || _visibleSlots.Length == 0)
            {
                return;
            }

            var first = _visibleSlots[0];
            for (var i = 0; i < _visibleSlots.Length - 1; i++)
            {
                _visibleSlots[i] = _visibleSlots[i + 1];
            }

            _visibleSlots[^1] = first;
            first.transform.SetAsLastSibling();
        }

        private void RefreshRightmostSlot()
        {
            if (_visibleSlots == null || _visibleSlots.Length == 0)
            {
                return;
            }

            var globalBeat = _windowStart + _visibleSlots.Length - 1;
            PopulateSlot(_visibleSlots[^1], globalBeat);
        }

        public void SetPhase(CombatPhase phase)
        {
            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(false);
            }

            if (phase == CombatPhase.Planning)
            {
                _autoPlayCompleted = false;
                ResetCarouselForPlanning();
                RefreshPhaseHeader(0);
                StartAutoPlayIfNeeded();
            }
            else if (phaseLabel != null)
            {
                phaseLabel.text = phase.ToString().ToUpperInvariant();
            }
        }

        private void ResetCarouselForPlanning()
        {
            ResetCarouselVisualState();
            _slotsBuilt = false;
            _visibleSlots = null;
            CleanupExtraBeatChildren();

            if (segmentTemplate != null && slotsRow != null)
            {
                segmentTemplate.transform.SetAsFirstSibling();
            }

            EnsureVisibleSlots();
            RefreshVisibleWindow(0);
            RefreshPhaseHeader(0);
        }

        public void RefreshAll()
        {
            if (_timeline == null)
            {
                return;
            }

            EnsureVisibleSlots();
            RefitSlotsToViewport();
            RefreshVisibleWindow(_windowStart);
            RefreshPhaseHeader(_autoPlayBeat);
            RefreshPhaseAvLabel();
        }

        public void RefreshBeat(int beatIndex)
        {
            if (_visibleSlots == null || _timeline == null)
            {
                return;
            }

            for (var i = 0; i < _visibleSlots.Length; i++)
            {
                var globalBeat = _windowStart + i;
                if (globalBeat == beatIndex)
                {
                    PopulateSlot(_visibleSlots[i], globalBeat);
                }
            }
        }

        private void RefreshVisibleWindow(int windowStart)
        {
            _windowStart = Mathf.Clamp(windowStart, 0, TimelineConstants.TotalBeats - 1);
            if (_visibleSlots == null)
            {
                return;
            }

            for (var i = 0; i < _visibleSlots.Length; i++)
            {
                PopulateSlot(_visibleSlots[i], _windowStart + i);
            }
        }

        private void PopulateSlot(BeatSegmentView slot, int globalBeat)
        {
            if (slot == null)
            {
                return;
            }

            slot.SetDisplayBeatIndex(globalBeat);
            slot.UpdatePhaseDivider();

            if (_timeline == null || globalBeat < 0 || globalBeat >= TimelineConstants.TotalBeats)
            {
                slot.SetEmpty();
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
        }

        public void SetScanSpeedMultiplier(float multiplier)
        {
            _scanSpeedMultiplier = Mathf.Max(0.001f, multiplier);
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
            avLabel.text = $"AV {tracker.Remaining}/{tracker.CurrentBudget}";
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

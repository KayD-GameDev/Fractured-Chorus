using System.Collections;
using System.Collections.Generic;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Qte;
using FracturedChorus.Meta;
using FracturedChorus.UI;
using FracturedChorus.UI.Loading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FracturedChorus.Tutorial
{
    public sealed class TutorialDirector : MonoBehaviour
    {
        public const string TrackHub = "hub";
        public const string TrackMap = "map";
        public const string TrackCombat = "combat";
        public const string TrackCadenceIntro = "cadence_intro";
        private const float CharacterHandPullBack = 0.12f;
        private const float PostQteTimelineRunSeconds = 0.01f;
        private const float CounterExplainPanelY = 82f;

        private static TutorialDirector s_instance;

        [Header("Scene authoring")]
        [SerializeField] private bool sceneBound;
        [SerializeField] private TutorialCoachView coachView;
        [SerializeField] private TutorialTrackSO cadenceIntroTrack;
        [SerializeField] private TutorialTrackSO hubTrack;
        [SerializeField] private TutorialTrackSO mapTrack;
        [SerializeField] private TutorialTrackSO combatTrack;

        private readonly List<TutorialStepSO> _queue = new List<TutorialStepSO>();
        private int _stepIndex;
        private string _completionFlag;
        private bool _slideshowTrack;
        private bool _awaitingFormationMove;
        private bool _awaitingDeploy;
        private bool _awaitingDeployThenQte;
        private bool _awaitingQte;
        private bool _awaitingSkillPanel;
        private bool _awaitingSkillPlaced;
        private BoardDragController _boundBoardDrag;
        private CombatController _boundCombat;
        private TutorialStepSO _deployQteHintStep;
        private Canvas _hostCanvas;
        private CanvasGroup _hostGroup;
        private bool _cadenceTrackActive;
        private bool _encounterTutorialMute;
        private bool _waitingPlanningSegment;
        private int _targetPlanningSegment;
        private bool _awaitingDeployUntilPlanning;
        private bool _awaitingFullTimeline;
        private bool _awaitingQteAfterDeploy;
        private bool _qteExplainPauseActive;
        private bool _holdingTimelineForCoachRead;
        private Coroutine _coachReadHoldRoutine;

        public static bool IsCadenceIntroActive =>
            s_instance != null && s_instance._cadenceTrackActive && !s_instance._encounterTutorialMute;

        public static bool IsQteExplainPauseActive =>
            s_instance != null && s_instance._qteExplainPauseActive;

        public static bool AllowsFormationDrag =>
            !IsCadenceIntroActive || s_instance._awaitingFormationMove;

        public static bool AllowsUnitSkillPanelOpen =>
            !IsCadenceIntroActive
            || s_instance._awaitingSkillPanel
            || s_instance._awaitingSkillPlaced
            || s_instance._awaitingFullTimeline;

        public static bool AllowsSkillTimelineDrop =>
            !IsCadenceIntroActive || s_instance._awaitingSkillPlaced || s_instance._awaitingFullTimeline;

        public static bool AllowsExecute =>
            !IsCadenceIntroActive
            || s_instance._awaitingDeploy
            || s_instance._awaitingDeployThenQte
            || s_instance._awaitingDeployUntilPlanning;

        public static bool BlocksSlideshowCombatUi =>
            IsCadenceIntroActive && s_instance.coachView != null && s_instance.coachView.BlocksCombatUi;

        public static bool RequiresRenFrontCell =>
            s_instance != null && s_instance._awaitingFormationMove;

        public static bool WantsQteBubbleGuide =>
            s_instance != null && s_instance._awaitingQteAfterDeploy && !s_instance._qteExplainPauseActive;

        public static bool IsRenUnit(UnitView view) =>
            view != null
            && view.Unit != null
            && string.Equals(view.Unit.DisplayName, "Ren", System.StringComparison.OrdinalIgnoreCase);

        public static bool IsRenFrontCell(GridCellMarker cell) =>
            cell != null
            && cell.Side == GridSide.Player
            && cell.Row == 1
            && cell.Column == 0;

        public static bool SuppressFormationHint =>
            s_instance != null
            && (s_instance._awaitingFormationMove
                || s_instance._awaitingDeploy
                || s_instance._awaitingDeployThenQte
                || s_instance._awaitingDeployUntilPlanning);

        public static TutorialDirector Ensure()
        {
            if (s_instance != null)
            {
                return s_instance;
            }

            var existing = FindAnyObjectByType<TutorialDirector>(FindObjectsInactive.Include);
            if (existing != null)
            {
                s_instance = existing;
                return s_instance;
            }

            var go = new GameObject("TutorialDirector", typeof(TutorialDirector));
            DontDestroyOnLoad(go);
            s_instance = go.GetComponent<TutorialDirector>();
            return s_instance;
        }

        public static TutorialDirector FindInLoadedScenes()
        {
            return FindAnyObjectByType<TutorialDirector>(FindObjectsInactive.Include);
        }

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            if (!sceneBound)
            {
                DontDestroyOnLoad(gameObject);
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            ResolveCoachReference();
        }

        public static void HideOverlay()
        {
            if (s_instance == null)
            {
                return;
            }

            s_instance.ForgetDestroyedCoach();
            s_instance.coachView?.Hide();
            s_instance.SetHostBlocking(false);
            TutorialGuidePathView.HideActive();
            TutorialFocusOverlay.Release();
            TutorialPointHandView.HideActive();
        }

        private void LateUpdate()
        {
            ForgetDestroyedCoach();
            RefreshGameplayGuide();
        }

        private void ForgetDestroyedCoach()
        {
            if (coachView == null)
            {
                coachView = null;
            }
        }

        private void RefreshGameplayGuide()
        {
            if (!_cadenceTrackActive)
            {
                coachView?.RestorePanelPosition();
                TutorialGuidePathView.HideActive();
                TutorialFocusOverlay.Release();
                TutorialPointHandView.HideActive();
                return;
            }

            if (IsCurrentStep("boss_counter_explain"))
            {
                coachView?.PlacePanelY(CounterExplainPanelY);
            }
            else
            {
                coachView?.RestorePanelPosition();
            }

            if (_awaitingFormationMove)
            {
                TutorialGuidePathView.ShowFormationArrow();
                TutorialFocusOverlay.SyncFormation();
                RefreshFormationHand();
                return;
            }

            if (_awaitingSkillPanel)
            {
                TutorialGuidePathView.HideActive();
                TutorialFocusOverlay.SyncParty();
                RefreshPartyHands();
                return;
            }

            if (IsCurrentStep("boss_counter_explain"))
            {
                TutorialGuidePathView.HideActive();
                TutorialFocusOverlay.SyncFirstImpactNote();
                RefreshCounterHand();
                return;
            }

            if (_awaitingSkillPlaced && IsCurrentStep("boss_drag_skill"))
            {
                TutorialFocusOverlay.SyncSkillDrag();
                TutorialGuidePathView.ShowSkillDragArrow();
                TutorialPointHandView.HideActive();
                return;
            }

            if (_awaitingFullTimeline)
            {
                TryCompleteFullTimeline();
            }

            TutorialGuidePathView.HideActive();
            TutorialFocusOverlay.Release();
            TutorialPointHandView.HideActive();
        }

        private void RefreshFormationHand()
        {
            var ren = FindRenView();
            var drag = _boundBoardDrag != null
                ? _boundBoardDrag
                : FindAnyObjectByType<BoardDragController>();
            if (ren == null || (drag != null && drag.IsHoldingUnit(ren)))
            {
                TutorialPointHandView.HideActive();
                return;
            }

            TutorialPointHandView.ShowAtWorld(PointBesideCharacter(ren.GetCameraBounds()));
        }

        private static void RefreshPartyHands()
        {
            var views = FindObjectsByType<UnitView>(FindObjectsInactive.Exclude);
            var points = new List<Vector3>(2);
            for (var i = 0; i < views.Length; i++)
            {
                var view = views[i];
                if (view == null || view.Side != GridSide.Player || view.Unit == null)
                {
                    continue;
                }

                points.Add(PointBesideCharacter(view.GetCameraBounds()));
            }

            TutorialPointHandView.ShowAtWorlds(points);
        }

        private static void RefreshCounterHand()
        {
            var timeline = FindAnyObjectByType<BeatTimelineUIView>();
            if (timeline == null || !timeline.TryGetFirstPhaseImpactNote(out var note) || note == null)
            {
                TutorialPointHandView.HideActive();
                return;
            }

            TutorialPointHandView.ShowAtRect(note);
        }

        private static Vector3 PointBesideCharacter(Bounds bounds)
        {
            return new Vector3(bounds.min.x - CharacterHandPullBack, bounds.center.y, bounds.center.z);
        }

        private static UnitView FindRenView()
        {
            var views = FindObjectsByType<UnitView>(FindObjectsInactive.Exclude);
            for (var i = 0; i < views.Length; i++)
            {
                if (IsRenUnit(views[i]))
                {
                    return views[i];
                }
            }

            return null;
        }

        private bool IsCurrentStep(string stepId)
        {
            if (string.IsNullOrEmpty(stepId) || _stepIndex < 0 || _stepIndex >= _queue.Count)
            {
                return false;
            }

            var step = _queue[_stepIndex];
            return step != null && step.stepId == stepId;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ForgetDestroyedCoach();
            HideOverlay();
            if (!LoadingScreenController.IsBusy)
            {
                LoadingScreenController.HideCoverNow();
            }
        }

        public void StartHubTrack()
        {
            if (!GameMetaSession.HasSession || GameMetaSession.Current.HasFlag(StoryFlagIds.TutorialHubDone))
            {
                return;
            }

            if (!TryStartTrack(hubTrack, StoryFlagIds.TutorialHubDone))
            {
                StartTrack(TrackHub, TutorialStepCatalog.HubSteps(), StoryFlagIds.TutorialHubDone);
            }
        }

        public void StartMapTrack()
        {
            if (!GameMetaSession.HasSession || GameMetaSession.Current.HasFlag(StoryFlagIds.TutorialMapDone))
            {
                return;
            }

            if (!TryStartTrack(mapTrack, StoryFlagIds.TutorialMapDone))
            {
                StartTrack(TrackMap, TutorialStepCatalog.MapSteps(), StoryFlagIds.TutorialMapDone);
            }
        }

        public void StartCombatTrack()
        {
            if (!GameMetaSession.HasSession || GameMetaSession.Current.HasFlag(StoryFlagIds.TutorialCombatDone))
            {
                return;
            }

            if (!TryStartTrack(combatTrack, StoryFlagIds.TutorialCombatDone))
            {
                StartTrack(TrackCombat, TutorialStepCatalog.CombatSteps(), StoryFlagIds.TutorialCombatDone);
            }
        }

        public void StartCadenceIntroTrack()
        {
            if (GameMetaSession.HasSession
                && GameMetaSession.Current.HasFlag(StoryFlagIds.TutorialCadenceIntroDone))
            {
                return;
            }

            var track = TutorialCadenceTrackLibrary.ResolveCadenceIntroTrack(cadenceIntroTrack);
            if (track == null || track.steps == null || track.steps.Length == 0)
            {
                Debug.LogError("[Tutorial] Cadence intro track could not be resolved.");
                return;
            }

            StartTrack(
                track.trackId,
                track.steps,
                track.completionFlag,
                track.slideshow);
        }

        private bool TryStartTrack(TutorialTrackSO track, string fallbackCompletionFlag)
        {
            if (track == null || track.steps == null || track.steps.Length == 0)
            {
                return false;
            }

            var flag = string.IsNullOrEmpty(track.completionFlag) ? fallbackCompletionFlag : track.completionFlag;
            StartTrack(track.trackId, track.steps, flag, track.slideshow);
            return true;
        }

        private void StartTrack(
            string trackId,
            IReadOnlyList<TutorialStepSO> steps,
            string completionFlag,
            bool slideshow = false)
        {
            if (steps == null || steps.Count == 0 || _queue.Count > 0)
            {
                return;
            }

            _completionFlag = completionFlag;
            _slideshowTrack = slideshow;
            _cadenceTrackActive = trackId == TrackCadenceIntro;
            if (_cadenceTrackActive)
            {
                CombatController.PlanningSegmentBegan += HandlePlanningSegmentBegan;
            }

            UnbindPracticeHooks();
            _queue.Clear();
            _queue.AddRange(steps);
            _stepIndex = 0;
            ResolveCoachReference();
            ShowCurrentStep();
            RefreshCombatUiGates();
        }

        private void ShowCurrentStep()
        {
            ResolveCoachReference();
            if (coachView == null)
            {
                Debug.LogWarning(
                    "[Tutorial] TutorialCoachView không tìm thấy — thêm TutorialCoach trong scene CombatTutorial.");
                return;
            }

            if (_stepIndex < 0 || _stepIndex >= _queue.Count)
            {
                CompleteTrack();
                return;
            }

            var step = _queue[_stepIndex];
            if (_slideshowTrack)
            {
                if (step.kind == TutorialStepKind.PracticeFormation)
                {
                    EnterFormationPractice(step);
                    return;
                }

                if (step.kind == TutorialStepKind.AwaitDeploy)
                {
                    EnterAwaitDeploy(step);
                    return;
                }

                if (step.kind == TutorialStepKind.AwaitUnitSkillPanel)
                {
                    EnterAwaitUnitSkillPanel(step);
                    return;
                }

                if (step.kind == TutorialStepKind.AwaitSkillPlaced)
                {
                    EnterAwaitSkillPlaced(step);
                    return;
                }

                if (step.kind == TutorialStepKind.AwaitDeployThenQte)
                {
                    EnterAwaitDeployThenQte(step);
                    return;
                }

                if (step.kind == TutorialStepKind.AwaitDeployUntilPlanning)
                {
                    EnterAwaitDeployUntilPlanning(step);
                    return;
                }

                if (step.kind == TutorialStepKind.BeginPlanningSandbox)
                {
                    EnterPlanningSandbox();
                    return;
                }

                var isLast = _stepIndex >= _queue.Count - 1;
                if (_encounterTutorialMute && step.kind == TutorialStepKind.Slide)
                {
                    _encounterTutorialMute = false;
                }

                RefreshFormationHintVisibility();
                coachView.ShowSlide(
                    step.bodyCopy,
                    ResolveCoachPortrait(step),
                    ResolvePanelImage(step),
                    $"{SlideOrdinal(_stepIndex)}/{SlideCount()}",
                    showBack: HasPreviousSlideshowStep(_stepIndex),
                    primaryLabel: isLast ? "Done" : "Next",
                    onBack: RetreatStep,
                    onPrimary: isLast ? CompleteTrack : AdvanceStep,
                    panelClip: TutorialCadenceTrackLibrary.LoadPanelClip(step.stepId));
                SetHostBlocking(true);
                RefreshCombatUiGates();
                return;
            }

            coachView.Show(
                step.bodyCopy,
                AdvanceStep,
                ResolveCoachPortrait(step),
                ResolvePanelImage(step),
                TutorialCadenceTrackLibrary.LoadPanelClip(step.stepId));
            SetHostBlocking(true);
            RefreshCombatUiGates();
        }

        private void EnterFormationPractice(TutorialStepSO step)
        {
            UnbindPracticeHooks();
            _awaitingFormationMove = false;
            _awaitingDeploy = false;
            RefreshFormationHintVisibility();
            coachView.ShowSlide(
                step.bodyCopy,
                ResolveCoachPortrait(step),
                ResolvePanelImage(step),
                $"{SlideOrdinal(_stepIndex)}/{SlideCount()}",
                showBack: HasPreviousSlideshowStep(_stepIndex),
                primaryLabel: "Next",
                onBack: RetreatStep,
                onPrimary: BeginSilentFormationPractice,
                panelClip: TutorialCadenceTrackLibrary.LoadPanelClip(step.stepId));
            RefreshCombatUiGates();
        }

        private void BeginSilentFormationPractice()
        {
            UnbindPracticeHooks();
            _awaitingFormationMove = true;
            _awaitingDeploy = false;
            coachView?.Hide();
            SetHostBlocking(false);
            RefreshFormationHintVisibility();
            _boundBoardDrag = FindAnyObjectByType<BoardDragController>();
            if (_boundBoardDrag != null)
            {
                _boundBoardDrag.AddFormationChangedHandler(HandleFormationMoved);
            }

            RefreshCombatUiGates();
        }

        private void EnterAwaitDeploy(TutorialStepSO step)
        {
            UnbindPracticeHooks();
            _awaitingDeploy = true;
            _awaitingDeployThenQte = false;
            _awaitingDeployUntilPlanning = false;
            _awaitingFormationMove = false;
            RefreshFormationHintVisibility();
            coachView?.Hide();
            SetHostBlocking(false);

            _boundCombat = FindAnyObjectByType<CombatController>();
            if (_boundCombat != null)
            {
                _boundCombat.PlayerDeployed += HandlePlayerDeployed;
            }

            RefreshCombatUiGates();
        }

        private void EnterPlanningSandbox()
        {
            UnbindPracticeHooks();
            _awaitingFullTimeline = true;
            _encounterTutorialMute = false;
            coachView?.Hide();
            SetHostBlocking(false);
            TutorialCombatHooks.SkillPlacedOnTimeline += HandleFullTimelineSkillPlaced;
            RefreshCombatUiGates();
            TryCompleteFullTimeline();
        }

        private void HandleFullTimelineSkillPlaced()
        {
            TryCompleteFullTimeline();
        }

        private void TryCompleteFullTimeline()
        {
            if (!_awaitingFullTimeline)
            {
                return;
            }

            var combat = FindAnyObjectByType<CombatController>();
            if (combat == null || !combat.IsTutorialPhase1CounterReady())
            {
                RefreshCombatUiGates();
                return;
            }

            _awaitingFullTimeline = false;
            TutorialCombatHooks.SkillPlacedOnTimeline -= HandleFullTimelineSkillPlaced;
            AdvanceStep();
        }

        private void EnterAwaitDeployUntilPlanning(TutorialStepSO step)
        {
            UnbindPracticeHooks();
            _awaitingDeployUntilPlanning = true;
            _awaitingDeploy = false;
            _awaitingDeployThenQte = false;
            _targetPlanningSegment = step.planningSegmentToContinue > 0 ? step.planningSegmentToContinue : 1;
            coachView?.Hide();
            SetHostBlocking(false);

            _boundCombat = FindAnyObjectByType<CombatController>();
            if (_boundCombat != null)
            {
                _boundCombat.PlayerDeployed += HandlePlayerDeployed;
            }

            RefreshCombatUiGates();
        }

        private void EnterAwaitUnitSkillPanel(TutorialStepSO step)
        {
            UnbindPracticeHooks();
            _awaitingSkillPanel = true;
            coachView?.Hide();
            SetHostBlocking(false);
            TutorialCombatHooks.SkillPanelOpened += HandleSkillPanelOpened;
            RefreshCombatUiGates();
        }

        private void EnterAwaitSkillPlaced(TutorialStepSO step)
        {
            UnbindPracticeHooks();
            _awaitingSkillPlaced = true;
            coachView?.Hide();
            SetHostBlocking(false);
            TutorialCombatHooks.SkillPlacedOnTimeline += HandleSkillPlacedOnTimeline;
            RefreshCombatUiGates();
        }

        private void EnterAwaitDeployThenQte(TutorialStepSO step)
        {
            UnbindPracticeHooks();
            _awaitingDeployThenQte = true;
            _deployQteHintStep = step;
            RefreshFormationHintVisibility();
            coachView?.Hide();
            SetHostBlocking(false);
            // PlayerDeployed only fires on the first Execute of the fight. This step is a later
            // Execute, so the bubble guide has to arm here instead of waiting for that event.
            _awaitingQte = true;
            _awaitingQteAfterDeploy = true;
            TutorialCombatHooks.QteResolved += HandleQteResolved;

            _boundCombat = FindAnyObjectByType<CombatController>();
            if (_boundCombat != null)
            {
                _boundCombat.PlayerDeployed += HandlePlayerDeployed;
            }

            RefreshCombatUiGates();
        }

        public static void NotifyQtePromptVisible()
        {
            if (s_instance == null || !s_instance._awaitingQteAfterDeploy || s_instance._qteExplainPauseActive)
            {
                return;
            }

            s_instance.BeginQteExplainPause();
        }

        private void BeginQteExplainPause()
        {
            _qteExplainPauseActive = true;
            var copy = _deployQteHintStep?.qteHintCopy;
            if (string.IsNullOrWhiteSpace(copy))
            {
                copy = "This is a QTE. Hit Space on time and you deal extra damage.";
            }

            coachView?.ShowSlide(
                copy,
                ResolveCoachPortrait(_deployQteHintStep),
                ResolvePanelImage(_deployQteHintStep),
                null,
                showBack: false,
                primaryLabel: "Next",
                onBack: null,
                onPrimary: DismissQteExplainPause,
                panelClip: TutorialCadenceTrackLibrary.LoadPanelClip(_deployQteHintStep != null ? _deployQteHintStep.stepId : null));
            SetHostBlocking(true);
        }

        private void DismissQteExplainPause()
        {
            _qteExplainPauseActive = false;
            coachView?.Hide();
            SetHostBlocking(false);
            RefreshCombatUiGates();
        }

        private void HandlePlanningSegmentBegan(int segmentIndex)
        {
            if (!_waitingPlanningSegment || segmentIndex < _targetPlanningSegment)
            {
                return;
            }

            _waitingPlanningSegment = false;
            _encounterTutorialMute = false;
            AdvanceStep();
            RefreshCombatUiGates();
        }

        private static void RefreshCombatUiGates()
        {
            var combat = FindAnyObjectByType<CombatController>();
            combat?.RefreshExecuteOverlayVisibility();
        }

        private static void RefreshFormationHintVisibility()
        {
            var combat = FindAnyObjectByType<CombatController>();
            if (combat != null)
            {
                combat.RefreshExecuteOverlayVisibility();
                return;
            }

            if (SuppressFormationHint)
            {
                FindAnyObjectByType<DeployFormationHintView>()?.Hide();
            }
        }

        private void HandleFormationMoved()
        {
            if (!_awaitingFormationMove)
            {
                return;
            }

            if (!IsRenStandingOnFrontCell())
            {
                return;
            }

            _awaitingFormationMove = false;
            TutorialFocusOverlay.Release();
            UnbindPracticeHooks();
            AdvanceStep();
        }

        private static bool IsRenStandingOnFrontCell()
        {
            var views = FindObjectsByType<UnitView>(FindObjectsInactive.Exclude);
            for (var i = 0; i < views.Length; i++)
            {
                var view = views[i];
                if (!IsRenUnit(view))
                {
                    continue;
                }

                var position = view.GridPosition;
                return position.Side == GridSide.Player && position.Row == 1 && position.Column == 0;
            }

            return false;
        }

        private void HandlePlayerDeployed()
        {
            if (_awaitingDeployUntilPlanning)
            {
                _awaitingDeployUntilPlanning = false;
                if (_boundCombat != null)
                {
                    _boundCombat.PlayerDeployed -= HandlePlayerDeployed;
                    _boundCombat = null;
                }

                _encounterTutorialMute = true;
                _waitingPlanningSegment = true;
                coachView?.Hide();
                SetHostBlocking(false);
                RefreshCombatUiGates();
                return;
            }

            if (_awaitingDeployThenQte)
            {
                _awaitingDeployThenQte = false;
                if (_boundCombat != null)
                {
                    _boundCombat.PlayerDeployed -= HandlePlayerDeployed;
                    _boundCombat = null;
                }

                _awaitingQte = true;
                _awaitingQteAfterDeploy = true;
                coachView?.Hide();
                SetHostBlocking(false);
                RefreshCombatUiGates();
                return;
            }

            if (!_awaitingDeploy)
            {
                return;
            }

            _awaitingDeploy = false;
            UnbindPracticeHooks();
            AdvanceStep();
        }

        private void HandleSkillPanelOpened()
        {
            if (!_awaitingSkillPanel)
            {
                return;
            }

            _awaitingSkillPanel = false;
            UnbindPracticeHooks();
            AdvanceStep();
        }

        private void HandleSkillPlacedOnTimeline()
        {
            if (!_awaitingSkillPlaced)
            {
                return;
            }

            _awaitingSkillPlaced = false;
            TutorialFocusOverlay.Release();
            UnbindPracticeHooks();
            AdvanceStep();
        }

        private void HandleQteResolved(CombatQteGrade grade)
        {
            if (!_awaitingQte)
            {
                return;
            }

            _awaitingQte = false;
            _awaitingQteAfterDeploy = false;
            UnbindPracticeHooks();
            AdvanceStep();
            BeginPostQteCoachReadHold();
        }

        private void BeginPostQteCoachReadHold()
        {
            if (coachView == null || !coachView.IsVisible)
            {
                return;
            }

            if (_coachReadHoldRoutine != null)
            {
                StopCoroutine(_coachReadHoldRoutine);
            }

            _coachReadHoldRoutine = StartCoroutine(HoldTimelineAfterQteRoutine());
        }

        private IEnumerator HoldTimelineAfterQteRoutine()
        {
            yield return new WaitForSeconds(PostQteTimelineRunSeconds);
            _coachReadHoldRoutine = null;
            if (coachView == null || !coachView.IsVisible)
            {
                yield break;
            }

            var timeline = FindAnyObjectByType<BeatTimelineUIView>();
            if (timeline == null)
            {
                yield break;
            }

            timeline.PauseForEncounter();
            _holdingTimelineForCoachRead = true;
        }

        public static void NotifyCoachHidden()
        {
            s_instance?.ReleasePostQteCoachReadHold();
        }

        private void ReleasePostQteCoachReadHold()
        {
            if (_coachReadHoldRoutine != null)
            {
                StopCoroutine(_coachReadHoldRoutine);
                _coachReadHoldRoutine = null;
            }

            if (!_holdingTimelineForCoachRead)
            {
                return;
            }

            _holdingTimelineForCoachRead = false;
            FindAnyObjectByType<BeatTimelineUIView>()?.ResumeAfterEncounter();
        }

        private void UnbindPracticeHooks()
        {
            if (_boundBoardDrag != null)
            {
                _boundBoardDrag.RemoveFormationChangedHandler(HandleFormationMoved);
                _boundBoardDrag = null;
            }

            if (_boundCombat != null)
            {
                _boundCombat.PlayerDeployed -= HandlePlayerDeployed;
                _boundCombat = null;
            }

            TutorialCombatHooks.SkillPanelOpened -= HandleSkillPanelOpened;
            TutorialCombatHooks.SkillPlacedOnTimeline -= HandleSkillPlacedOnTimeline;
            TutorialCombatHooks.SkillPlacedOnTimeline -= HandleFullTimelineSkillPlaced;
            TutorialCombatHooks.QteResolved -= HandleQteResolved;

            _awaitingFormationMove = false;
            _awaitingDeploy = false;
            _awaitingDeployThenQte = false;
            _awaitingQte = false;
            _awaitingSkillPanel = false;
            _awaitingSkillPlaced = false;
            _deployQteHintStep = null;
            _encounterTutorialMute = false;
            _waitingPlanningSegment = false;
            _awaitingDeployUntilPlanning = false;
            _awaitingFullTimeline = false;
            _awaitingQteAfterDeploy = false;
            _qteExplainPauseActive = false;
            TutorialCombatHooks.ResetCadenceIntroOverrides();
        }

        private void AdvanceStep()
        {
            _stepIndex++;
            if (_stepIndex >= _queue.Count)
            {
                CompleteTrack();
                return;
            }

            ShowCurrentStep();
        }

        private void RetreatStep()
        {
            if (_stepIndex <= 0)
            {
                return;
            }

            UnbindPracticeHooks();
            do
            {
                _stepIndex--;
            } while (_stepIndex > 0 && !CountsAsProgressSlide(_queue[_stepIndex].kind));

            ShowCurrentStep();
        }

        private bool HasPreviousSlideshowStep(int index)
        {
            for (var i = index - 1; i >= 0; i--)
            {
                if (CountsAsProgressSlide(_queue[i].kind))
                {
                    return true;
                }
            }

            return false;
        }

        private int SlideCount()
        {
            var count = 0;
            for (var i = 0; i < _queue.Count; i++)
            {
                if (CountsAsProgressSlide(_queue[i].kind))
                {
                    count++;
                }
            }

            return count;
        }

        private int SlideOrdinal(int index)
        {
            var ordinal = 0;
            for (var i = 0; i <= index && i < _queue.Count; i++)
            {
                if (CountsAsProgressSlide(_queue[i].kind))
                {
                    ordinal++;
                }
            }

            return Mathf.Max(1, ordinal);
        }

        private static bool CountsAsProgressSlide(TutorialStepKind kind) =>
            kind == TutorialStepKind.Slide || kind == TutorialStepKind.PracticeFormation;

        private void CompleteTrack()
        {
            if (!string.IsNullOrEmpty(_completionFlag) && GameMetaSession.HasSession)
            {
                GameMetaSession.Current.SetFlag(_completionFlag);
                GameMetaSession.Save();
            }

            UnbindPracticeHooks();
            _queue.Clear();
            _stepIndex = 0;
            _completionFlag = null;
            _slideshowTrack = false;
            _cadenceTrackActive = false;
            CombatController.PlanningSegmentBegan -= HandlePlanningSegmentBegan;
            coachView?.Hide();
            SetHostBlocking(false);
        }

        private static Sprite ResolveCoachPortrait(TutorialStepSO step)
        {
            if (step == null)
            {
                return TutorialCadenceTrackLibrary.LoadCodaPortrait();
            }

            return step.coachPortrait != null
                ? step.coachPortrait
                : TutorialCadenceTrackLibrary.LoadCodaPortrait();
        }

        private static Sprite ResolvePanelImage(TutorialStepSO step)
        {
            if (step == null || string.IsNullOrEmpty(step.stepId))
            {
                return step?.panelImage;
            }

            return step.panelImage != null
                ? step.panelImage
                : TutorialCadenceTrackLibrary.LoadPanelImage(step.stepId);
        }

        private void OnDestroy()
        {
            UnbindPracticeHooks();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (s_instance == this)
            {
                s_instance = null;
            }
        }

        private void ResolveCoachReference()
        {
            ForgetDestroyedCoach();
            if (coachView == null)
            {
                coachView = FindAnyObjectByType<TutorialCoachView>(FindObjectsInactive.Include);
            }

            if (coachView != null || sceneBound)
            {
                SetHostBlocking(false);
                return;
            }

            EnsureHostCanvas();
            coachView = TutorialCoachView.Ensure(transform);
            SetHostBlocking(false);
        }

        private void EnsureHostCanvas()
        {
            if (_hostCanvas != null)
            {
                return;
            }

            _hostCanvas = GetComponent<Canvas>();
            if (_hostCanvas == null)
            {
                _hostCanvas = gameObject.AddComponent<Canvas>();
            }

            _hostCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _hostCanvas.overrideSorting = true;
            _hostCanvas.sortingOrder = UiCanvasLayers.Tutorial;

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

            _hostGroup = GetComponent<CanvasGroup>();
            if (_hostGroup == null)
            {
                _hostGroup = gameObject.AddComponent<CanvasGroup>();
            }

            SetHostBlocking(false);
        }

        private void SetHostBlocking(bool blocking)
        {
            if (_hostGroup == null)
            {
                _hostGroup = GetComponent<CanvasGroup>();
            }

            if (_hostGroup == null)
            {
                return;
            }

            _hostGroup.alpha = 1f;
            _hostGroup.blocksRaycasts = blocking;
            _hostGroup.interactable = blocking;
        }

        private static class TutorialStepCatalog
        {
            public static List<TutorialStepSO> HubSteps() => new List<TutorialStepSO>
            {
                Step(TrackHub, "hub_menu",
                    "Open MENU (top right) to check party stats, bonds, the calendar, and save slots."),
                Step(TrackHub, "hub_town",
                    "Tap a map pin to use an activity slot. The morning quiz and the day's phase lock what's available."),
                Step(TrackHub, "hub_done",
                    "Hub basics are done. Wander the campus, then start a Cadence run when you feel ready.")
            };

            public static List<TutorialStepSO> MapSteps() => new List<TutorialStepSO>
            {
                Step(TrackMap, "map_nodes",
                    "Pick a node you can reach. Battle and Elite start a fight; the boss gate ends the sector."),
                Step(TrackMap, "map_camp",
                    "Lose a fight and you drop back to the nearest camp. HP carries between fights in a run."),
                Step(TrackMap, "map_done",
                    "You can read the map now. Head for the boss once the party feels solid.")
            };

            public static List<TutorialStepSO> CombatSteps() => new List<TutorialStepSO>
            {
                Step(TrackCombat, "combat_plan",
                    "Planning: drag units into FRONT, MID, or BACK, and drag skills onto the beat timeline. FRONT takes less damage. BACK hits harder."),
                Step(TrackCombat, "combat_standing",
                    "Standing (the gray dot) shows the boss telegraph early. You can still move while Planning is open."),
                Step(TrackCombat, "combat_execute",
                    "Press Execute to run the round. The music keeps going, and the scan jumps to the next beat. Counter the boss note on time, then close the skill window."),
                Step(TrackCombat, "combat_done",
                    "Short combat guide done. Keep the beat.")
            };

            private static TutorialStepSO Step(
                string trackId,
                string stepId,
                string body,
                Sprite coachPortrait = null,
                Sprite panelImage = null)
            {
                var step = ScriptableObject.CreateInstance<TutorialStepSO>();
                step.trackId = trackId;
                step.stepId = stepId;
                step.bodyCopy = body;
                step.requiresConfirm = true;
                step.kind = TutorialStepKind.Slide;
                step.coachPortrait = coachPortrait;
                step.panelImage = panelImage;
                return step;
            }
        }
    }
}

using System.Collections.Generic;
using FracturedChorus.Combat.Core;
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

            s_instance.coachView?.Hide();
            s_instance.SetHostBlocking(false);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
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
            if (steps == null || steps.Count == 0 || coachView != null && coachView.IsVisible)
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
                    onPrimary: isLast ? CompleteTrack : AdvanceStep);
                SetHostBlocking(true);
                RefreshCombatUiGates();
                return;
            }

            coachView.Show(step.bodyCopy, AdvanceStep, ResolveCoachPortrait(step), ResolvePanelImage(step));
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
                onPrimary: BeginSilentFormationPractice);
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
            if (combat == null || !combat.AreAllPlayerSkillsPlaced())
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
            _awaitingQteAfterDeploy = false;

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
                copy = "Đây là QTE — khi bạn bấm Space đúng lúc sẽ tăng thêm sát thương.";
            }

            coachView?.ShowSlide(
                copy,
                ResolveCoachPortrait(_deployQteHintStep),
                ResolvePanelImage(_deployQteHintStep),
                null,
                showBack: false,
                primaryLabel: "Next",
                onBack: null,
                onPrimary: DismissQteExplainPause);
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

            _awaitingFormationMove = false;
            UnbindPracticeHooks();
            AdvanceStep();
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
                TutorialCombatHooks.QteResolved += HandleQteResolved;
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
                    "Mở MENU (góc trên phải) để xem chỉ số đội, bond, lịch và slot save."),
                Step(TrackHub, "hub_town",
                    "Bấm ghim bản đồ để dùng slot hoạt động. Quiz sáng và phase lịch khóa nội dung trong ngày."),
                Step(TrackHub, "hub_done",
                    "Cơ bản Hub xong. Khám phá campus, rồi vào Cadence run khi sẵn sàng.")
            };

            public static List<TutorialStepSO> MapSteps() => new List<TutorialStepSO>
            {
                Step(TrackMap, "map_nodes",
                    "Chọn node tới được để tiến. Battle/Elite dẫn vào combat; cổng boss kết thúc sector."),
                Step(TrackMap, "map_camp",
                    "Thua trận sẽ về camp gần nhất. HP giữ giữa các trận trong run."),
                Step(TrackMap, "map_done",
                    "Điều hướng map sẵn sàng. Mở đường tới boss khi đội hình ổn.")
            };

            public static List<TutorialStepSO> CombatSteps() => new List<TutorialStepSO>
            {
                Step(TrackCombat, "combat_plan",
                    "Cửa sổ Planning: vừa kéo unit sang cột FRONT / MID / BACK, vừa kéo skill lên beat timeline. FRONT ít dính sát thương; BACK đánh mạnh hơn."),
                Step(TrackCombat, "combat_standing",
                    "Standing (chấm xám) để lộ trước telegraph boss. Đổi vị trí bất cứ lúc nào cửa sổ Planning còn mở."),
                Step(TrackCombat, "combat_execute",
                    "Bấm Execute để chạy round — nhạc không dừng, scan bắt vào ô nhịp kế tiếp. Counter nốt boss đúng beat, rồi hạ cửa sổ skill."),
                Step(TrackCombat, "combat_done",
                    "Hướng dẫn combat ngắn xong. Giữ nhịp.")
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

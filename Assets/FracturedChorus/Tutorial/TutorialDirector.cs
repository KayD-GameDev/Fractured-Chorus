using System.Collections.Generic;
using FracturedChorus.Combat.Core;
using FracturedChorus.Meta;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Tutorial
{
    public sealed class TutorialDirector : MonoBehaviour
    {
        public const string TrackHub = "hub";
        public const string TrackMap = "map";
        public const string TrackCombat = "combat";
        public const string TrackCadenceIntro = "cadence_intro";

        private const string CodaChibiBustAssetPath =
            "Assets/FracturedChorus/Art/Characters/Coda/Chibi/coda_cadence_chibi_bust_v1.png";
        private const string CodaChibiFullbodyAssetPath =
            "Assets/FracturedChorus/Art/Characters/Coda/Chibi/coda_chibi_fullbody_v1.png";
        private const string StepImageFolder =
            "Assets/FracturedChorus/Art/UI/Tutorial/Steps";

        private static TutorialDirector s_instance;
        private static Sprite s_codaChibi;

        [SerializeField] private TutorialCoachView coachView;

        private readonly List<TutorialStepSO> _queue = new List<TutorialStepSO>();
        private int _stepIndex;
        private string _completionFlag;
        private bool _slideshowTrack;
        private bool _awaitingFormationMove;
        private bool _awaitingDeploy;
        private BoardDragController _boundBoardDrag;
        private CombatController _boundCombat;

        public static bool SuppressFormationHint =>
            s_instance != null && (s_instance._awaitingFormationMove || s_instance._awaitingDeploy);

        public static TutorialDirector Ensure()
        {
            if (s_instance != null)
            {
                return s_instance;
            }

            var existing = FindAnyObjectByType<TutorialDirector>();
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

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureCoach();
        }

        public void StartHubTrack()
        {
            if (!GameMetaSession.HasSession || GameMetaSession.Current.HasFlag(StoryFlagIds.TutorialHubDone))
            {
                return;
            }

            StartTrack(TrackHub, TutorialStepCatalog.HubSteps(), StoryFlagIds.TutorialHubDone);
        }

        public void StartMapTrack()
        {
            if (!GameMetaSession.HasSession || GameMetaSession.Current.HasFlag(StoryFlagIds.TutorialMapDone))
            {
                return;
            }

            StartTrack(TrackMap, TutorialStepCatalog.MapSteps(), StoryFlagIds.TutorialMapDone);
        }

        public void StartCombatTrack()
        {
            if (!GameMetaSession.HasSession || GameMetaSession.Current.HasFlag(StoryFlagIds.TutorialCombatDone))
            {
                return;
            }

            StartTrack(TrackCombat, TutorialStepCatalog.CombatSteps(), StoryFlagIds.TutorialCombatDone);
        }

        public void StartCadenceIntroTrack()
        {
            if (GameMetaSession.HasSession
                && GameMetaSession.Current.HasFlag(StoryFlagIds.TutorialCadenceIntroDone))
            {
                return;
            }

            StartTrack(
                TrackCadenceIntro,
                TutorialStepCatalog.CadenceIntroSteps(),
                StoryFlagIds.TutorialCadenceIntroDone,
                slideshow: true);
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
            UnbindPracticeHooks();
            _queue.Clear();
            _queue.AddRange(steps);
            _stepIndex = 0;
            EnsureCoach();
            ShowCurrentStep();
        }

        private void ShowCurrentStep()
        {
            EnsureCoach();
            if (coachView == null)
            {
                CompleteTrack();
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

                var isLast = _stepIndex >= _queue.Count - 1;
                RefreshFormationHintVisibility();
                coachView.ShowSlide(
                    step.bodyCopy,
                    step.coachPortrait,
                    step.panelImage,
                    $"{SlideOrdinal(_stepIndex)}/{SlideCount()}",
                    showBack: HasPreviousSlideshowStep(_stepIndex),
                    primaryLabel: isLast ? "Done" : "Next",
                    onBack: RetreatStep,
                    onPrimary: isLast ? CompleteTrack : AdvanceStep);
                return;
            }

            coachView.Show(step.bodyCopy, AdvanceStep, step.coachPortrait, step.panelImage);
        }

        private void EnterFormationPractice(TutorialStepSO step)
        {
            UnbindPracticeHooks();
            _awaitingFormationMove = false;
            _awaitingDeploy = false;
            RefreshFormationHintVisibility();
            coachView.ShowSlide(
                step.bodyCopy,
                step.coachPortrait,
                step.panelImage,
                $"{SlideOrdinal(_stepIndex)}/{SlideCount()}",
                showBack: HasPreviousSlideshowStep(_stepIndex),
                primaryLabel: "Next",
                onBack: RetreatStep,
                onPrimary: BeginSilentFormationPractice);
        }

        private void BeginSilentFormationPractice()
        {
            UnbindPracticeHooks();
            _awaitingFormationMove = true;
            _awaitingDeploy = false;
            coachView?.Hide();
            RefreshFormationHintVisibility();
            _boundBoardDrag = FindAnyObjectByType<BoardDragController>();
            if (_boundBoardDrag != null)
            {
                _boundBoardDrag.AddFormationChangedHandler(HandleFormationMoved);
            }
        }

        private void EnterAwaitDeploy(TutorialStepSO step)
        {
            UnbindPracticeHooks();
            _awaitingDeploy = true;
            _awaitingFormationMove = false;
            RefreshFormationHintVisibility();
            if (!string.IsNullOrEmpty(step.bodyCopy))
            {
                coachView.ShowFloatingHint(step.bodyCopy);
            }
            else
            {
                coachView.Hide();
            }

            _boundCombat = FindAnyObjectByType<CombatController>();
            if (_boundCombat != null)
            {
                _boundCombat.PlayerDeployed += HandlePlayerDeployed;
            }
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
            if (!_awaitingDeploy)
            {
                return;
            }

            _awaitingDeploy = false;
            UnbindPracticeHooks();
            CompleteTrack();
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

            _awaitingFormationMove = false;
            _awaitingDeploy = false;
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
            coachView?.Hide();
        }

        private void OnDestroy()
        {
            UnbindPracticeHooks();
            if (s_instance == this)
            {
                s_instance = null;
            }
        }

        private void EnsureCoach()
        {
            if (coachView != null)
            {
                return;
            }

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            coachView = TutorialCoachView.Ensure(canvas.transform);
        }

        private static Sprite LoadCodaChibi()
        {
            if (s_codaChibi != null)
            {
                return s_codaChibi;
            }

            s_codaChibi = Resources.Load<Sprite>("Characters/Coda/coda_cadence_chibi_bust_v1")
                          ?? Resources.Load<Sprite>("Characters/Coda/coda_chibi_fullbody_v1");
            if (s_codaChibi != null)
            {
                return s_codaChibi;
            }

#if UNITY_EDITOR
            s_codaChibi = LoadSpriteAtPath(CodaChibiBustAssetPath)
                          ?? LoadSpriteAtPath(CodaChibiFullbodyAssetPath);
#endif
            return s_codaChibi;
        }

        private static Sprite LoadStepPanelImage(string stepId)
        {
            if (string.IsNullOrEmpty(stepId))
            {
                return null;
            }

            var resources = Resources.Load<Sprite>($"UI/Tutorial/Steps/{stepId}_v1");
            if (resources != null)
            {
                return resources;
            }

#if UNITY_EDITOR
            return LoadSpriteAtPath($"{StepImageFolder}/{stepId}_v1.png");
#else
            return null;
#endif
        }

#if UNITY_EDITOR
        private static Sprite LoadSpriteAtPath(string assetPath)
        {
            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                return sprite;
            }

            var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null)
            {
                return null;
            }

            return Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }
#endif

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

            public static List<TutorialStepSO> CadenceIntroSteps()
            {
                var coda = LoadCodaChibi();
                return new List<TutorialStepSO>
                {
                    StepVi("meet_danger",
                        "Hiện tại ở đây rất nguy hiểm. Tôi là Coda — tôi sẽ hướng dẫn cậu thoát khỏi đây. Nghe kỹ từng bước, đừng nóng vội.",
                        coda),
                    StepVi("formation_grid",
                        "Đầu tiên hãy đến với phần Formation — đội hình chia thành 6 ô tương ứng với BACK, MID và FRONT.",
                        coda),
                    StepVi("formation_buff_intro",
                        "Mỗi vị trí sẽ có buff khác nhau dựa vào tình huống nhất định.",
                        coda),
                    StepVi("formation_front",
                        "FRONT giảm sát thương nhận vào.",
                        coda),
                    StepVi("formation_mid_back",
                        "MID tăng sát thương. BACK tăng khả năng buff và né.",
                        coda),
                    StepVi("formation_situational",
                        "Hãy dựa vào đội hình hiện tại và tình huống để đặt sao cho hợp lý.",
                        coda),
                    PracticeVi("formation_practice",
                        "Hãy di chuyển Ren và Coda giữa các ô.",
                        coda),
                    StepVi("formation_lock",
                        "Làm tốt lắm. Khi bạn đã chốt xong vị trí, hãy nhấn Execute.",
                        coda),
                    AwaitDeployVi("formation_await_deploy",
                        "Nhấn Execute để bắt đầu round.")
                };
            }

            private static TutorialStepSO StepVi(string stepId, string bodyVi, Sprite coda)
            {
                return Step(TrackCadenceIntro, stepId, bodyVi, coda, LoadStepPanelImage(stepId));
            }

            private static TutorialStepSO PracticeVi(string stepId, string bodyVi, Sprite coda)
            {
                var step = Step(TrackCadenceIntro, stepId, bodyVi, coda, LoadStepPanelImage(stepId));
                step.kind = TutorialStepKind.PracticeFormation;
                return step;
            }

            private static TutorialStepSO AwaitDeployVi(string stepId, string bodyVi)
            {
                var step = Step(TrackCadenceIntro, stepId, bodyVi);
                step.kind = TutorialStepKind.AwaitDeploy;
                step.requiresConfirm = false;
                return step;
            }

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

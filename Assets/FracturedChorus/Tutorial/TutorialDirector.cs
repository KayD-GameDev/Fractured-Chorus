using System.Collections.Generic;
using FracturedChorus.Meta;
using UnityEngine;

namespace FracturedChorus.Tutorial
{
    public sealed class TutorialDirector : MonoBehaviour
    {
        public const string TrackHub = "hub";
        public const string TrackMap = "map";
        public const string TrackCombat = "combat";
        public const string TrackCadenceIntro = "cadence_intro";

        private const string CodaChibiAssetPath =
            "Assets/FracturedChorus/Art/Characters/Coda/Chibi/coda_chibi_fullbody_v1.png";

        private static TutorialDirector s_instance;
        private static Sprite s_codaChibi;

        [SerializeField] private TutorialCoachView coachView;

        private readonly List<TutorialStepSO> _queue = new List<TutorialStepSO>();
        private int _stepIndex;
        private string _completionFlag;

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
            StartTrack(TrackCadenceIntro, TutorialStepCatalog.CadenceIntroSteps(), StoryFlagIds.TutorialCadenceIntroDone);
        }

        private void StartTrack(string trackId, IReadOnlyList<TutorialStepSO> steps, string completionFlag)
        {
            if (steps == null || steps.Count == 0 || coachView != null && coachView.IsVisible)
            {
                return;
            }

            _completionFlag = completionFlag;
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

            if (_stepIndex >= _queue.Count)
            {
                CompleteTrack();
                return;
            }

            var step = _queue[_stepIndex];
            coachView.Show(step.bodyCopy, AdvanceStep, step.coachPortrait, step.panelImage);
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

        private void CompleteTrack()
        {
            if (!string.IsNullOrEmpty(_completionFlag) && GameMetaSession.HasSession)
            {
                GameMetaSession.Current.SetFlag(_completionFlag);
                GameMetaSession.Save();
            }

            _queue.Clear();
            _stepIndex = 0;
            _completionFlag = null;
            coachView?.Hide();
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

            s_codaChibi = Resources.Load<Sprite>("Characters/Coda/coda_chibi_fullbody_v1");
            if (s_codaChibi != null)
            {
                return s_codaChibi;
            }

#if UNITY_EDITOR
            s_codaChibi = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(CodaChibiAssetPath);
            if (s_codaChibi != null)
            {
                return s_codaChibi;
            }

            var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(CodaChibiAssetPath);
            if (tex != null)
            {
                s_codaChibi = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
#endif
            return s_codaChibi;
        }

        private static class TutorialStepCatalog
        {
            public static List<TutorialStepSO> HubSteps() => new List<TutorialStepSO>
            {
                Step(TrackHub, "hub_menu",
                    "Open MENU (top-right) to view party stats, bonds, calendar, and save slots."),
                Step(TrackHub, "hub_town",
                    "Click map pins to spend activity slots. Morning quiz and calendar phases gate what you can do each day."),
                Step(TrackHub, "hub_done",
                    "Hub basics covered. Explore campus, then enter a Cadence run when ready.")
            };

            public static List<TutorialStepSO> MapSteps() => new List<TutorialStepSO>
            {
                Step(TrackMap, "map_nodes",
                    "Select reachable nodes to advance. Battle and Elite nodes lead to combat; the boss gate ends the sector."),
                Step(TrackMap, "map_camp",
                    "After defeat you return to the nearest camp node. HP persists between fights on the run."),
                Step(TrackMap, "map_done",
                    "Map navigation ready. Clear the path to the boss when your party is set.")
            };

            public static List<TutorialStepSO> CombatSteps() => new List<TutorialStepSO>
            {
                Step(TrackCombat, "combat_deploy",
                    "Deploy phase: drag units on the front / mid / back columns. FRONT takes less damage; BACK deals more."),
                Step(TrackCombat, "combat_plan",
                    "After Deploy, drag skills onto the timeline beats. Standing phases leave you exposed to boss telegraphs."),
                Step(TrackCombat, "combat_execute",
                    "Press Execute to resolve the round. Counter boss notes on beat, then finish with your skill windows."),
                Step(TrackCombat, "combat_done",
                    "Combat tutorial complete. Good luck — keep the rhythm.")
            };

            public static List<TutorialStepSO> CadenceIntroSteps()
            {
                var coda = LoadCodaChibi();
                return new List<TutorialStepSO>
                {
                    Step(TrackCadenceIntro, "cadence_meet",
                        "Hey — I'm Coda. That beast is Kiki Ueda. Stick with me — it's you, me, and her.",
                        coda, coda),
                    Step(TrackCadenceIntro, "cadence_deploy",
                        "Deploy: drag us on FRONT / MID / BACK. FRONT soaks hits; BACK hits harder. Park Ren in MID opposite Kiki.",
                        coda, null),
                    Step(TrackCadenceIntro, "cadence_plan",
                        "Plan: drag your Basic onto the timeline beats. Skill and Ult aren't unlocked yet — keep it simple.",
                        coda, null),
                    Step(TrackCadenceIntro, "cadence_execute",
                        "Execute: press Execute to resolve the round. Counter her notes on beat, then land your skill windows.",
                        coda, null),
                    Step(TrackCadenceIntro, "cadence_done",
                        "You've got this. Drop Kiki — I'll keep coaching from the sideline.",
                        coda, null)
                };
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
                step.coachPortrait = coachPortrait;
                step.panelImage = panelImage;
                return step;
            }
        }
    }
}

using FracturedChorus.Meta;
using FracturedChorus.Narrative.Vn;
using FracturedChorus.RunMap;
using UnityEngine;

namespace FracturedChorus.Hub.FlowerWork
{
    public sealed class FlowerWorkEventController : MonoBehaviour
    {
        public const int BaseResonanceExp = 10;
        public const int BaseHarmonyExp = 4;
        public const int CorrectBonusResonanceExp = 1;

        [SerializeField] private VnRuntimeController runtime;
        [SerializeField] private FlowerWorkScenarioSO[] scenarioPool;
        [SerializeField] private bool playOnStart = true;

        private FlowerWorkScenarioSO _scenario;
        private bool _choseCorrect;
        private bool _rewardApplied;

        private void Awake()
        {
            if (runtime == null)
            {
                runtime = GetComponent<VnRuntimeController>() ?? GetComponentInChildren<VnRuntimeController>(true);
            }

            if (runtime != null)
            {
                runtime.PlayOnStart = false;
                runtime.LoadNextSceneOnEnd = false;
                runtime.ChoiceSelected += OnChoiceSelected;
                runtime.Finished += OnRuntimeFinished;
            }
        }

        private void OnDestroy()
        {
            if (runtime != null)
            {
                runtime.ChoiceSelected -= OnChoiceSelected;
                runtime.Finished -= OnRuntimeFinished;
            }
        }

        private void Start()
        {
            if (playOnStart)
            {
                BeginEvent();
            }
        }

        public void BeginEvent()
        {
            if (runtime == null)
            {
                Debug.LogError("[FlowerWork] VnRuntimeController missing.");
                ReturnToHub();
                return;
            }

            var state = GameMetaSession.Current;
            var pool = scenarioPool != null && scenarioPool.Length > 0
                ? scenarioPool
                : FlowerWorkScriptBuilder.LoadPoolFromResources();
            _scenario = FlowerWorkScriptBuilder.PickScenario(pool);
            _choseCorrect = false;
            _rewardApplied = false;

            var script = FlowerWorkScriptBuilder.Build(state, _scenario);
            runtime.SetScript(script);
            runtime.LoadNextSceneOnEnd = false;

            var date = state.Calendar.CurrentDate.ToDisplayString();
            var phase = state.Calendar.CurrentPhase == DayPhase.Evening ? "Evening" : "After School";
            runtime.SetDateHudDefaults(date, phase);
            runtime.Begin();
        }

        private void OnChoiceSelected(int choiceIndex)
        {
            if (_scenario == null)
            {
                _choseCorrect = choiceIndex == 0;
                return;
            }

            _choseCorrect = choiceIndex == Mathf.Clamp(_scenario.correctIndex, 0, 2);
        }

        private void OnRuntimeFinished()
        {
            if (_rewardApplied)
            {
                return;
            }

            _rewardApplied = true;
            ApplyRewardsAndConsumeSlot();
            HubPendingActivity.Clear();
            ReturnToHub();
        }

        private void ApplyRewardsAndConsumeSlot()
        {
            try
            {
                var state = GameMetaSession.Current;
                var resonance = BaseResonanceExp + (_choseCorrect ? CorrectBonusResonanceExp : 0);
                state.AddStatExp(SocialStatType.Resonance, resonance);
                state.AddStatExp(SocialStatType.Harmony, BaseHarmonyExp);
                state.ConsumeActivitySlot();
                GameMetaSession.Save();
            }
            catch (System.Exception error)
            {
                Debug.LogError($"[FlowerWork] Failed to apply rewards: {error}");
            }
        }

        private static void ReturnToHub()
        {
            if (!RunMapSceneLoader.LoadByName(RunMapSceneCatalog.CampusHub))
            {
                Debug.LogError("[FlowerWork] Failed to return to CampusHub.");
            }
        }
    }
}

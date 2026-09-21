using System.Collections;
using FracturedChorus.Hub;
using FracturedChorus.Meta;
using FracturedChorus.Narrative.Vn;
using FracturedChorus.RunMap;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FracturedChorus.Hub.FlowerWork
{
    public sealed class FlowerWorkEventController : MonoBehaviour
    {
        public const int BaseResonanceExp = 10;
        public const int BaseHarmonyExp = 4;
        public const int CorrectBonusResonanceExp = 1;

        [SerializeField] private VnRuntimeController runtime;
        [SerializeField] private FlowerWorkScenarioSO[] scenarioPool;
        [SerializeField] private FlowerWorkRewardNoteDirector rewardNote;
        [SerializeField] private SocialStatsOverlayUI socialStatsOverlay;
        [SerializeField] private bool playOnStart = true;

#if UNITY_EDITOR
        public enum FlowerWorkEditorPreview
        {
            Hidden = 0,
            CustomerLine = 1,
            ThinkChoice = 2,
            CorrectReply = 3,
            SocialStatsReward = 4
        }

        [SerializeField] private FlowerWorkEditorPreview editorPreview = FlowerWorkEditorPreview.ThinkChoice;
        [SerializeField] private FlowerWorkScenarioSO editorPreviewScenario;
#endif

        private FlowerWorkScenarioSO _scenario;
        private bool _choseCorrect;
        private bool _rewardApplied;
        private bool _resonanceGranted;

        private void Awake()
        {
            if (runtime == null)
            {
                runtime = GetComponent<VnRuntimeController>() ?? GetComponentInChildren<VnRuntimeController>(true);
            }

            if (rewardNote == null)
            {
                rewardNote = GetComponent<FlowerWorkRewardNoteDirector>();
                if (rewardNote == null && Application.isPlaying)
                {
                    rewardNote = gameObject.AddComponent<FlowerWorkRewardNoteDirector>();
                }
            }

            if (socialStatsOverlay == null && runtime != null && runtime.BackgroundImage != null)
            {
                socialStatsOverlay = runtime.BackgroundImage.GetComponentInChildren<SocialStatsOverlayUI>(true);
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
                runtime.BeatInterceptor = null;
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

            StopAllCoroutines();
            StartCoroutine(BeginEventAfterEntryDelay());
        }

        private IEnumerator BeginEventAfterEntryDelay()
        {
            yield return new WaitForSeconds(FlowerWorkScriptBuilder.EntryDelaySeconds);
            if (runtime == null)
            {
                yield break;
            }

            var state = GameMetaSession.Current;
            var pool = scenarioPool != null && scenarioPool.Length > 0
                ? scenarioPool
                : FlowerWorkScriptBuilder.LoadPoolFromResources();
            _scenario = FlowerWorkScriptBuilder.PickScenario(pool);
            _choseCorrect = false;
            _rewardApplied = false;
            _resonanceGranted = false;

            var script = FlowerWorkScriptBuilder.Build(state, _scenario);
            runtime.BeatInterceptor = TryInterceptBeat;
            runtime.SetScript(script);
            runtime.LoadNextSceneOnEnd = false;

            var date = state.Calendar.CurrentDate.ToDisplayString();
            var phase = state.Calendar.CurrentPhase == DayPhase.Evening ? "Evening" : "After School";
            runtime.SetDateHudDefaults(date, phase);
            runtime.Begin();
        }

        private bool TryInterceptBeat(VnBeat beat)
        {
            if (beat == null || beat.signalId != FlowerWorkSignals.GrantResonance || !_choseCorrect)
            {
                return false;
            }

            StartCoroutine(PlayCorrectRewardThenContinue());
            return true;
        }

        private void OnChoiceSelected(int choiceIndex)
        {
            if (_scenario == null)
            {
                _choseCorrect = choiceIndex == 0;
            }
            else
            {
                var max = _scenario.choices != null && _scenario.choices.Length > 0
                    ? _scenario.choices.Length - 1
                    : 2;
                _choseCorrect = choiceIndex == Mathf.Clamp(_scenario.correctIndex, 0, max);
            }
        }

        private IEnumerator PlayCorrectRewardThenContinue()
        {
            if (rewardNote == null)
            {
                GrantResonanceReward();
                runtime?.AdvanceAfterIntercept();
                yield break;
            }

            yield return rewardNote.PlayResonanceReward(
                runtime,
                GameMetaSession.Current,
                GrantResonanceReward);
            runtime?.AdvanceAfterIntercept();
        }

        private void GrantResonanceReward()
        {
            if (_resonanceGranted)
            {
                return;
            }

            _resonanceGranted = true;
            try
            {
                GameMetaSession.Current.AddStatExp(
                    SocialStatType.Resonance,
                    BaseResonanceExp + CorrectBonusResonanceExp);
                GameMetaSession.Save();
            }
            catch (System.Exception error)
            {
                Debug.LogError($"[FlowerWork] Failed to apply Resonance: {error}");
            }
        }

        private void OnRuntimeFinished()
        {
            if (_rewardApplied)
            {
                return;
            }

            _rewardApplied = true;
            ApplyRemainingRewardsAndConsumeSlot();
            HubPendingActivity.Clear();
            ReturnToHub();
        }

        private void ApplyRemainingRewardsAndConsumeSlot()
        {
            try
            {
                var state = GameMetaSession.Current;
                if (!_resonanceGranted)
                {
                    state.AddStatExp(SocialStatType.Resonance, BaseResonanceExp);
                }

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
            if (!HubNavigationEscContext.HasPending)
            {
                HubNavigationEscContext.SetReturnToTownMap();
            }

            HubNavigationEscContext.ApplyBeforeLoadCampusHub();
            if (!RunMapSceneLoader.LoadByName(RunMapSceneCatalog.CampusHub))
            {
                Debug.LogError("[FlowerWork] Failed to return to CampusHub.");
            }
        }

#if UNITY_EDITOR
        private bool _applyingEditorPreview;

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += ApplyEditorPreviewDeferred;
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EditorApplication.delayCall += ApplyEditorPreviewDeferred;
        }

        private void ApplyEditorPreviewDeferred()
        {
            if (this == null || Application.isPlaying)
            {
                return;
            }

            ApplyEditorPreview();
        }

        public void SetEditorPreview(FlowerWorkEditorPreview preview)
        {
            editorPreview = preview;
            ApplyEditorPreview();
        }

        public void ApplyEditorPreview()
        {
            if (_applyingEditorPreview || Application.isPlaying)
            {
                return;
            }

            _applyingEditorPreview = true;
            try
            {
                ResolveEditorRefs();
                var scenario = ResolveEditorPreviewScenario();
                if (runtime == null)
                {
                    return;
                }

                runtime.EditorApplyBackground(VnBgIds.FlowerShop);
                HideEditorRewardLayers();

                switch (editorPreview)
                {
                    case FlowerWorkEditorPreview.Hidden:
                        runtime.EditorHideEventUi();
                        break;
                    case FlowerWorkEditorPreview.CustomerLine:
                        runtime.EditorPreviewDialogue(
                            FlowerWorkCustomerSpeakers.ResolveSpeakerId(scenario),
                            scenario != null ? scenario.customerLine : string.Empty);
                        break;
                    case FlowerWorkEditorPreview.ThinkChoice:
                        runtime.EditorPreviewDialogue(
                            FlowerWorkCustomerSpeakers.ResolveSpeakerId(scenario),
                            scenario != null ? scenario.customerLine : string.Empty);
                        if (runtime.ChoiceView != null)
                        {
                            runtime.ChoiceView.EnsureEditorHierarchy();
                            runtime.ChoiceView.ApplyEditorPreview(
                                scenario != null ? scenario.thinkPrompt : string.Empty,
                                scenario != null ? scenario.choices : null);
                        }

                        break;
                    case FlowerWorkEditorPreview.CorrectReply:
                        runtime.EditorPreviewDialogue(
                            VnSpeakerIds.FlowerOwner,
                            scenario != null ? scenario.correctReply : string.Empty);
                        break;
                    case FlowerWorkEditorPreview.SocialStatsReward:
                        PreviewSocialStatsReward(scenario);
                        break;
                }

                EnsureCanvasVisibleForEditPreview();
                RepaintEditPreviewViews();
            }
            finally
            {
                _applyingEditorPreview = false;
            }
        }

        private void EnsureCanvasVisibleForEditPreview()
        {
            if (runtime?.BackgroundImage == null)
            {
                return;
            }

            var canvas = runtime.BackgroundImage.canvas;
            if (canvas == null)
            {
                return;
            }

            var scale = canvas.transform.localScale;
            if (scale.x == 0f || scale.y == 0f || scale.z == 0f)
            {
                canvas.transform.localScale = Vector3.one;
                EditorUtility.SetDirty(canvas);
            }
        }

        private static void RepaintEditPreviewViews()
        {
            Canvas.ForceUpdateCanvases();
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }

        private void ResolveEditorRefs()
        {
            if (runtime == null)
            {
                runtime = GetComponent<VnRuntimeController>() ?? GetComponentInChildren<VnRuntimeController>(true);
            }

            if (rewardNote == null)
            {
                rewardNote = GetComponent<FlowerWorkRewardNoteDirector>();
            }

            if (socialStatsOverlay == null && runtime != null && runtime.BackgroundImage != null)
            {
                socialStatsOverlay = runtime.BackgroundImage.GetComponentInChildren<SocialStatsOverlayUI>(true);
            }
        }

        private FlowerWorkScenarioSO ResolveEditorPreviewScenario()
        {
            if (editorPreviewScenario != null)
            {
                return editorPreviewScenario;
            }

            if (scenarioPool != null)
            {
                for (var i = 0; i < scenarioPool.Length; i++)
                {
                    if (scenarioPool[i] != null)
                    {
                        return scenarioPool[i];
                    }
                }
            }

            var loaded = FlowerWorkScriptBuilder.LoadPoolFromResources();
            return loaded != null && loaded.Length > 0 ? loaded[0] : null;
        }

        private void HideEditorRewardLayers()
        {
            if (socialStatsOverlay != null && socialStatsOverlay.IsOpen)
            {
                socialStatsOverlay.Hide();
            }

            var note = FindRewardNoteObject();
            if (note != null)
            {
                note.SetActive(false);
            }
        }

        private GameObject FindRewardNoteObject()
        {
            var canvas = runtime != null && runtime.BackgroundImage != null
                ? runtime.BackgroundImage.canvas
                : null;
            return canvas != null ? canvas.transform.Find("FlowerRewardNote")?.gameObject : null;
        }

        private void PreviewSocialStatsReward(FlowerWorkScenarioSO _)
        {
            runtime.EditorHideEventUi();
            if (runtime.SpeakerCatalog != null
                && runtime.SpeakerCatalog.TryGet(VnSpeakerIds.Ren, out var ren)
                && runtime.PortraitView != null)
            {
                runtime.PortraitView.Show(ren, "smile");
            }

            runtime.RefreshHubCornerInfoHud(true);

            if (socialStatsOverlay == null)
            {
                Debug.LogWarning("[FlowerWork] SocialStatsOverlay missing — run Ensure FlowerShopWork Edit Preview Hierarchy.");
                return;
            }

            var state = GameMetaState.CreateHubStart();
            state.SocialStats.ImportRank(SocialStatType.Resonance, 4, 0);
            state.SocialStats.ImportRank(SocialStatType.Cadence, 5, 0);
            state.SocialStats.ImportRank(SocialStatType.Pulse, 3, 0);
            state.SocialStats.ImportRank(SocialStatType.Harmony, 4, 0);
            state.SocialStats.ImportRank(SocialStatType.Rhythm, 2, 0);
            socialStatsOverlay.Show(state, allowCancel: false);

            var noteGo = FindRewardNoteObject();
            if (noteGo != null)
            {
                noteGo.SetActive(true);
                noteGo.transform.SetAsLastSibling();
            }
        }
#endif
    }
}

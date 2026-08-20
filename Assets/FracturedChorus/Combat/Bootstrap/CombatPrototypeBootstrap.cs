using FracturedChorus.Audio;
using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Cover;
using FracturedChorus.Combat.Difficulty;
using FracturedChorus.Combat.Formation;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Presentation;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using FracturedChorus.Meta;
using FracturedChorus.RunMap;
using FracturedChorus.UI;
using UnityEngine.Serialization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Combat.Bootstrap
{
    public class CombatPrototypeBootstrap : MonoBehaviour
    {
        [Header("Scene References — edit layout in Hierarchy")]
        [SerializeField] private CombatController combatController;
        [SerializeField] private BeatTimelineUIView timelineView;
        [SerializeField] private SkillPanelUIView skillPanelView;
        [SerializeField] private PartyStatusBarUIView partyStatusBarView;
        [SerializeField] private EnemyStatusBarUIView enemyStatusBarView;
        [SerializeField] private CombatExecuteOverlayUIView executeOverlay;
        [SerializeField] private Transform unitsRoot;
        [SerializeField] private Transform gridRoot;
        [SerializeField] private UnitView[] unitViews;
        [SerializeField] private Camera mainCamera;

        [Header("Encounter (optional if units already in scene)")]
        [SerializeField] private EncounterDefinitionSO encounterDefinition;
        [Tooltip("CombatTutorial scene: respect Hierarchy BG/enemies; force tutorial party/skills/coach.")]
        [SerializeField] private bool tutorialSceneMode;

        [SerializeField] private CombatMusicController musicController;
        [SerializeField] private CombatSfxController combatSfxController;
        [SerializeField] private CounterPresentationDriver counterPresentation;
        [SerializeField] private EnemyStrikeChoreographer enemyStrikeChoreographer;
        [SerializeField] private PlayerSkillShotChoreographer playerSkillShotChoreographer;
        [FormerlySerializedAs("resolveCutsceneDirector")]
        [SerializeField] private EncounterDirector encounterDirector;

        [Header("Grid layout")]
        [SerializeField] private float sideGap = HexBoardLayout.DefaultSideGap;

        [Header("Playtest start resources (Inspector only)")]
        [FormerlySerializedAs("applyDebugResourcesOnStart")]
        [SerializeField] private bool applyStartResourcesOnPlay = true;
        [FormerlySerializedAs("debugCoverGaugeOnStart")]
        [SerializeField] [Range(0, 10)] private int startCoverGauge = 8;
        [FormerlySerializedAs("debugPrepAllOnStart")]
        [SerializeField] [Range(0, 3)] private int startPrepAll = 3;

        private CombatSession _session;
        private DualGrid _grid;
        private BeatTimelineEngine _timeline;
        private Dictionary<GridPosition, Transform> _cellByPosition;
        private BoardDragController _boardDrag;
        private CoverHudView _coverHud;
        private CombatPoolRoll _deferredPooledBackgroundRoll;
        private bool _applyPooledBackgroundOnStart;

        private void Awake()
        {
            CombatInputSetup.Configure(mainCamera != null ? mainCamera : Camera.main);
            ResolveSceneReferences();
            EnsureMusicController();
            EnsureCombatSfxController();
            EnsureCounterPresentation();
            EnsureAudioListener();
            WarnIfBeatMapMismatchesTimeline();

            _grid = new DualGrid();
            _timeline = new BeatTimelineEngine();
            _session = new CombatSession();

            EnsureHoneycombGrid();
            CacheGridCellTransforms();

            var handoffEncounter = CombatEncounterHandoff.HasPendingEncounter
                ? EncounterCatalog.LoadOrCreate(CombatEncounterHandoff.EncounterId)
                : null;
            var encounter = handoffEncounter != null
                ? handoffEncounter
                : encounterDefinition != null
                    ? encounterDefinition
                    : null;

            var respectSceneVisuals = tutorialSceneMode
                                      || IsCombatTutorialScene();
            if (respectSceneVisuals && handoffEncounter == null && encounter == null)
            {
                encounter = EncounterCatalog.LoadOrCreate(EncounterCatalog.Tutorial);
                handoffEncounter = encounter;
                if (!CombatEncounterHandoff.HasPendingEncounter)
                {
                    CombatEncounterHandoff.SetPending(
                        EncounterCatalog.Tutorial,
                        RunMapSceneCatalog.CampusHub);
                }
            }

            var encounterId = handoffEncounter?.encounterId
                              ?? encounter?.encounterId
                              ?? (respectSceneVisuals ? EncounterCatalog.Tutorial : EncounterCatalog.BattleGrunts);
            var isTutorial = respectSceneVisuals || EncounterCatalog.IsTutorial(encounterId);
            var isPooledEncounter = handoffEncounter != null
                                    && CombatPoolRoll.IsPooledEncounterId(encounterId);
            var isBossEncounter = encounterId == EncounterCatalog.BossDespair;

            if (isPooledEncounter)
            {
                CombatTimelineProfile.ApplyRun();
            }
            else
            {
                CombatTimelineProfile.ApplyBoss();
            }

            if (isTutorial)
            {
                ApplyTutorialUnitVisibility();
                if (respectSceneVisuals)
                {
                    EnsureTutorialSceneVisualsVisible();
                }
                else
                {
                    ApplyTutorialBackground();
                }
            }

            if (HasSceneUnits())
            {
                if (isPooledEncounter)
                {
                    DisableSceneEnemyUnits();
                    RegisterPlayerSceneUnits(isTutorial);
                    var pooledEncounter = MergePartyIfEnemyOnly(handoffEncounter ?? encounter);
                    SpawnUnitsFromEncounter(pooledEncounter, enemiesOnly: true, tutorialBasics: isTutorial);
                }
                else
                {
                    RegisterSceneUnits(isTutorial);
                    if (handoffEncounter != null && !respectSceneVisuals)
                    {
                        ApplyHandoffToSceneEnemies(handoffEncounter);
                    }
                }
            }
            else
            {
                var full = encounter != null
                    ? MergePartyIfEnemyOnly(encounter)
                    : EncounterRuntimeFactory.CreateDemoEncounter();
                SpawnUnitsFromEncounter(full, enemiesOnly: false, tutorialBasics: isTutorial);
            }

            RefreshUnitViewsCache();

            if (isPooledEncounter)
            {
                _deferredPooledBackgroundRoll = CombatEncounterHandoff.PendingPoolRoll;
                _applyPooledBackgroundOnStart = true;
            }

            InitializeBossFormation(encounterId);

            if (CombatEncounterHandoff.HasPendingEncounter)
            {
                CombatEncounterHandoff.ConsumePendingEncounter();
            }

            _session.Initialize(_grid, _timeline);
            PartyRunHpStore.ApplyToSession(_session);
            SetupBoardDrag();

            if (combatController == null)
            {
                combatController = GetComponent<CombatController>();
                if (combatController == null)
                {
                    combatController = gameObject.AddComponent<CombatController>();
                }
            }

            var executeOverlay = ResolveExecuteOverlay();

            combatController.SetActiveEncounter(encounterId);

            ICombatMusicSync musicSync;
            if (isPooledEncounter && RunMusicSession.Instance != null && RunMusicSession.Instance.IsActive)
            {
                RunMusicSession.Instance.SetMode(RunMusicMode.Combat);
                musicSync = RunCombatMusicBridge.Attach(transform);
            }
            else
            {
                EnsureMusicController();
                if (isBossEncounter && RunMusicSession.Instance != null && RunMusicSession.Instance.IsActive)
                {
                    RunMusicSession.Instance.PauseForBoss();
                }

                musicSync = musicController;
            }

            combatController.InitializeWithMusic(_session, _timeline, timelineView, skillPanelView, musicSync,
                executeOverlay, _boardDrag);
            EnsureCombatHudCanvasSorting();

            counterPresentation?.Configure(combatSfxController, timelineView);
            timelineView?.SetCounterPresentation(counterPresentation);

            EnsureEnemyStrikeChoreographer(choreographyEnabled: true);
            EnsureUnitCombatAnimStates();
            EnsurePlayerSkillShotChoreographer();
            EnsureEncounterDirector(musicSync);

            RefreshPartyStatusBar();
            EnsureEnemyStatusBar();
            CoverHudView.HideAll();
            ApplyPlaytestStartResources();
            RunEventCombatMods.ApplyStartOfBattle(_session);

            if (skillPanelView != null && !skillPanelView.gameObject.activeSelf)
            {
                skillPanelView.Hide();
            }
        }

        private void Start()
        {
            if (!_applyPooledBackgroundOnStart)
            {
                return;
            }

            ApplyPooledCombatBackground(_deferredPooledBackgroundRoll);
            _applyPooledBackgroundOnStart = false;
        }

        private void InitializeBossFormation(string encounterId)
        {
            var profile = BossFormationProfileSO.GetDefaultForEncounter(encounterId);
            BossFormationRuntime.Initialize(profile);

            var difficulty = GameMetaSession.HasSession ? GameMetaSession.Current.Difficulty : DifficultyRuntime.Cadence;
            var mult = DifficultyRuntime.Get(difficulty);
            BossFormationRuntime.ApplyDifficultyScale(mult.PierceFrontBias);
        }

        public static bool IsCombatTutorialSceneStatic() => IsCombatTutorialScene();

        private static bool IsCombatTutorialScene()
        {
            var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            return string.Equals(sceneName, RunMapSceneCatalog.CombatTutorial,
                System.StringComparison.OrdinalIgnoreCase);
        }

        private void EnsureTutorialSceneVisualsVisible()
        {
            try
            {
                var cam = mainCamera != null ? mainCamera : Camera.main;
                var combatCanvas = GameObject.Find("CombatCanvas")?.GetComponent<Canvas>();
                if (combatCanvas != null)
                {
                    combatCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                    if (cam != null)
                    {
                        combatCanvas.worldCamera = cam;
                    }

                    combatCanvas.planeDistance = 100f;
                    combatCanvas.overrideSorting = true;
                    combatCanvas.sortingOrder = UiCanvasLayers.Hud;
                }

                var bgRoot = GameObject.Find("Background canvas");
                if (bgRoot != null)
                {
                    bgRoot.SetActive(true);
                    var bgCanvas = bgRoot.GetComponent<Canvas>();
                    if (bgCanvas != null)
                    {
                        bgCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                        if (cam != null)
                        {
                            bgCanvas.worldCamera = cam;
                        }

                        bgCanvas.planeDistance = 100f;
                        bgCanvas.sortingOrder = -1;
                    }

                    var image = bgRoot.GetComponentInChildren<Image>(true);
                    if (image != null)
                    {
                        image.gameObject.SetActive(true);
                        image.enabled = true;
                        if (image.color.a < 0.99f)
                        {
                            var c = image.color;
                            c.a = 1f;
                            image.color = c;
                        }
                    }
                }

                if (unitsRoot != null)
                {
                    unitsRoot.gameObject.SetActive(true);
                }

                var world = GameObject.Find("World");
                if (world != null)
                {
                    world.SetActive(true);
                }

                FindAnyObjectByType<CombatFocusDimmer>()?.ReleaseImmediate();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Bootstrap] Failed to restore tutorial visuals: " + e);
            }
        }

        private void ApplyTutorialUnitVisibility()
        {
            if (unitViews == null || unitViews.Length == 0)
            {
                unitViews = unitsRoot != null
                    ? unitsRoot.GetComponentsInChildren<UnitView>(true)
                    : GetComponentsInChildren<UnitView>(true);
            }

            if (unitViews == null)
            {
                return;
            }

            var survivors = new List<UnitView>(unitViews.Length);
            foreach (var view in unitViews)
            {
                if (view == null)
                {
                    continue;
                }

                if (IsExcludedFromTutorial(view))
                {
                    Destroy(view.gameObject);
                    continue;
                }

                survivors.Add(view);
            }

            unitViews = survivors.ToArray();
        }

        private static bool IsExcludedFromTutorial(UnitView view)
        {
            var key = view.DemoUnitKey?.ToLowerInvariant() ?? string.Empty;
            var preset = view.ResolvePreset();
            var unitId = preset?.unitId?.ToLowerInvariant() ?? string.Empty;
            var role = preset != null ? preset.role : UnitRole.Grunt;

            if (role == UnitRole.Boss || key.Contains("boss") || unitId.Contains("boss"))
            {
                return true;
            }

            if (key.Contains("grunt") || unitId.Contains("grunt"))
            {
                return true;
            }

            if (key.Contains("tank") || key.Contains("charlotte") || key.Contains("charlott")
                || unitId.Contains("tank") || unitId.Contains("charlotte") || unitId.Contains("charlott"))
            {
                return true;
            }

            return false;
        }

        private void ApplyTutorialBackground()
        {
            try
            {
                var bgRoot = GameObject.Find("Background canvas");
                if (bgRoot == null)
                {
                    return;
                }

                var image = bgRoot.GetComponentInChildren<Image>(true);
                if (image == null)
                {
                    return;
                }

                var sprite = LoadTutorialBackgroundSprite();
                if (sprite == null)
                {
                    return;
                }

                image.sprite = sprite;
                image.color = Color.white;
                image.preserveAspect = false;
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Bootstrap] Failed to apply tutorial background: " + e);
            }
        }

        private static Sprite LoadTutorialBackgroundSprite()
        {
            var fromResources = Resources.Load<Sprite>("Backgrounds/lumina_alley_night_rain_v1");
            if (fromResources != null)
            {
                return fromResources;
            }

            var tex = Resources.Load<Texture2D>("Backgrounds/lumina_alley_night_rain_v1");
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }

#if UNITY_EDITOR
            var editorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/FracturedChorus/Art/Backgrounds/lumina_alley_night_rain_v1.png");
            if (editorSprite != null)
            {
                return editorSprite;
            }

            var editorTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/FracturedChorus/Art/Backgrounds/lumina_alley_night_rain_v1.png");
            if (editorTex != null)
            {
                return Sprite.Create(
                    editorTex,
                    new Rect(0f, 0f, editorTex.width, editorTex.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
#endif
            return null;
        }

        private void EnsureCoverHud()
        {
            try
            {
                if (_coverHud == null)
                {
                    _coverHud = FindAnyObjectByType<CoverHudView>();
                }

                var canvasRt = ResolveCombatCanvasRoot();
                if (_coverHud == null && canvasRt != null)
                {
                    _coverHud = CoverHudView.EnsureOn(canvasRt);
                }
                else if (_coverHud != null && canvasRt != null)
                {
                    CoverHudView.EnsureOn(canvasRt);
                }

                if (_coverHud != null && partyStatusBarView != null)
                {
                    var orphan = partyStatusBarView.transform.Find("CoverHud");
                    if (orphan != null && orphan.GetComponent<CoverHudView>() != null &&
                        orphan.gameObject != _coverHud.gameObject)
                    {
                        Destroy(orphan.gameObject);
                    }
                }

                _coverHud?.Bind(_session);
                _coverHud?.Refresh();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Bootstrap] Failed to setup CoverHud: " + e);
            }
        }

        private void ApplyPlaytestStartResources()
        {
            try
            {
                if (!applyStartResourcesOnPlay || _session == null)
                {
                    _coverHud?.Refresh();
                    return;
                }

                _session.Cover?.DebugSetGauge(startCoverGauge);
                if (_session.Grid != null)
                {
                    var prep = Mathf.Clamp(startPrepAll, 0, CombatUnit.PrepCap);
                    foreach (var unit in _session.Grid.PlayerUnits)
                    {
                        unit?.SetPrepAbsolute(prep);
                    }

                    Debug.Log($"[Playtest] Start Cover={startCoverGauge}/{CoverConstants.GaugeCap} PrepAll={prep}");
                }

                _coverHud?.Refresh();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Bootstrap] Failed to apply playtest start resources: " + e);
            }
        }

        private void EnsureCombatHudCanvasSorting()
        {
            try
            {
                Canvas canvas = null;
                var named = GameObject.Find("CombatCanvas");
                if (named != null)
                {
                    canvas = named.GetComponent<Canvas>();
                }

                if (canvas == null)
                {
                    var overlay = FindAnyObjectByType<CombatExecuteOverlayUIView>(FindObjectsInactive.Include);
                    if (overlay != null)
                    {
                        canvas = overlay.GetComponentInParent<Canvas>();
                    }
                }

                if (canvas == null)
                {
                    return;
                }

                canvas.overrideSorting = true;
                if (canvas.sortingOrder < UiCanvasLayers.Hud)
                {
                    canvas.sortingOrder = UiCanvasLayers.Hud;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Bootstrap] Failed to raise CombatCanvas sorting: " + e);
            }
        }

        private RectTransform ResolveCombatCanvasRoot()
        {
            if (partyStatusBarView != null)
            {
                var parent = partyStatusBarView.transform.parent as RectTransform;
                if (parent != null)
                {
                    return parent;
                }
            }

            if (timelineView != null)
            {
                return timelineView.transform.parent as RectTransform;
            }

            var canvas = FindAnyObjectByType<Canvas>();
            return canvas != null ? canvas.transform as RectTransform : null;
        }

        private void EnsureEnemyStatusBar()
        {
            if (partyStatusBarView == null)
            {
                return;
            }

            if (enemyStatusBarView == null)
            {
                enemyStatusBarView = FindAnyObjectByType<EnemyStatusBarUIView>();
            }

            if (enemyStatusBarView == null)
            {
                Debug.LogWarning(
                    "[Bootstrap] EnemyStatusBarUI not found in scene. " +
                    "Run Fractured Chorus → Add Enemy Status Bar (Hierarchy), save the scene, then Play again.");
                return;
            }

            enemyStatusBarView.WireReferences();

            if (enemyStatusBarView.CardTemplate == null)
            {
                var partyTemplate = partyStatusBarView.CardTemplate;
                if (partyTemplate != null)
                {
                    Debug.LogWarning(
                        "[Bootstrap] EnemyStatusBarUI is missing CardTemplate — temporarily borrowing party template. " +
                        "Run Setup Enemy Cards in Hierarchy to add a dedicated CardTemplate in the scene.");
                    enemyStatusBarView.SetCardTemplate(partyTemplate);
                }
                else
                {
                    Debug.LogWarning("[Bootstrap] No CardTemplate available for enemy cards.");
                    return;
                }
            }

            AlignEnemyBarToPartyY();
            enemyStatusBarView.BindFromSession(_session);
        }

        /// <summary>Canh trục Y của thanh thẻ quái trùng với thanh thẻ party (cùng đỉnh + chiều cao).</summary>
        private void AlignEnemyBarToPartyY()
        {
            if (partyStatusBarView == null || enemyStatusBarView == null)
            {
                return;
            }

            var partyRect = partyStatusBarView.transform as RectTransform;
            var enemyRect = enemyStatusBarView.transform as RectTransform;
            if (partyRect == null || enemyRect == null)
            {
                return;
            }

            // Giữ nguyên trục X (thẻ quái ở cạnh phải), chỉ đồng bộ trục Y theo thẻ players.
            var anchorMin = enemyRect.anchorMin;
            anchorMin.y = partyRect.anchorMin.y;
            enemyRect.anchorMin = anchorMin;

            var anchorMax = enemyRect.anchorMax;
            anchorMax.y = partyRect.anchorMax.y;
            enemyRect.anchorMax = anchorMax;

            var pivot = enemyRect.pivot;
            pivot.y = partyRect.pivot.y;
            enemyRect.pivot = pivot;

            var size = enemyRect.sizeDelta;
            size.y = partyRect.sizeDelta.y;
            enemyRect.sizeDelta = size;

            var pos = enemyRect.anchoredPosition;
            pos.y = partyRect.anchoredPosition.y;
            enemyRect.anchoredPosition = pos;
        }

        private CombatExecuteOverlayUIView ResolveExecuteOverlay()
        {
            if (executeOverlay == null)
            {
                executeOverlay = FindAnyObjectByType<CombatExecuteOverlayUIView>();
            }

            executeOverlay?.WireReferences();
            return executeOverlay;
        }

        private void EnsureMusicController()
        {
            if (musicController != null)
            {
                return;
            }

            musicController = FindAnyObjectByType<CombatMusicController>();
            if (musicController != null)
            {
                return;
            }

            var audioGo = new GameObject("CombatMusic");
            audioGo.transform.SetParent(transform, false);
            musicController = audioGo.AddComponent<CombatMusicController>();
        }

        private void WarnIfBeatMapMismatchesTimeline()
        {
            var beatMap = musicController != null ? musicController.BeatMap : null;
            if (beatMap == null || !beatMap.HasData || beatMap.Clip == null)
            {
                return;
            }

            var clipBeats = beatMap.TotalBeatsForClip();
            if (clipBeats == CombatTimelineProfile.TotalBeats)
            {
                return;
            }

            Debug.LogWarning(
                $"[CombatBootstrap] Beat map yields {clipBeats} beats but CombatTimelineProfile.TotalBeats is " +
                $"{CombatTimelineProfile.TotalBeats}. Update the profile to match the current track.");
        }

        private void EnsureCombatSfxController()
        {
            if (combatSfxController != null)
            {
                return;
            }

            combatSfxController = FindAnyObjectByType<CombatSfxController>();
            if (combatSfxController != null)
            {
                return;
            }

            if (musicController != null)
            {
                combatSfxController = musicController.gameObject.AddComponent<CombatSfxController>();
                return;
            }

            var sfxGo = new GameObject("CombatSfx");
            sfxGo.transform.SetParent(transform, false);
            combatSfxController = sfxGo.AddComponent<CombatSfxController>();
        }

        private void EnsureCounterPresentation()
        {
            if (counterPresentation == null)
            {
                counterPresentation = GetComponent<CounterPresentationDriver>();
            }

            if (counterPresentation == null)
            {
                counterPresentation = FindAnyObjectByType<CounterPresentationDriver>();
            }

            if (counterPresentation == null)
            {
                counterPresentation = gameObject.AddComponent<CounterPresentationDriver>();
            }

            counterPresentation.Configure(combatSfxController, timelineView);
        }

        private void EnsureEnemyStrikeChoreographer(bool choreographyEnabled)
        {
            EnemyStrikeChoreographer.ClearOwnership();

            if (enemyStrikeChoreographer == null)
            {
                enemyStrikeChoreographer = GetComponent<EnemyStrikeChoreographer>();
            }

            if (enemyStrikeChoreographer == null)
            {
                enemyStrikeChoreographer = FindAnyObjectByType<EnemyStrikeChoreographer>();
            }

            if (enemyStrikeChoreographer == null)
            {
                if (!choreographyEnabled)
                {
                    return;
                }

                enemyStrikeChoreographer = gameObject.AddComponent<EnemyStrikeChoreographer>();
            }

            enemyStrikeChoreographer.Configure(_session, choreographyEnabled);
        }

        private void EnsureUnitCombatAnimStates()
        {
            if (unitViews == null || unitViews.Length == 0)
            {
                unitViews = unitsRoot != null
                    ? unitsRoot.GetComponentsInChildren<UnitView>(true)
                    : FindObjectsByType<UnitView>(FindObjectsInactive.Exclude);
            }

            if (unitViews == null)
            {
                return;
            }

            foreach (var view in unitViews)
            {
                view?.EnsureDefaultCombatAnimStates();
            }
        }

        private void EnsurePlayerSkillShotChoreographer()
        {
            if (playerSkillShotChoreographer == null)
            {
                playerSkillShotChoreographer = GetComponent<PlayerSkillShotChoreographer>();
            }

            if (playerSkillShotChoreographer == null)
            {
                playerSkillShotChoreographer = FindAnyObjectByType<PlayerSkillShotChoreographer>();
            }

            if (playerSkillShotChoreographer == null)
            {
                playerSkillShotChoreographer = gameObject.AddComponent<PlayerSkillShotChoreographer>();
            }

            playerSkillShotChoreographer.Configure(_session);
        }

        private void EnsureEncounterDirector(ICombatMusicSync musicSync)
        {
            if (encounterDirector == null)
            {
                encounterDirector = GetComponent<EncounterDirector>();
            }

            if (encounterDirector == null)
            {
                encounterDirector = FindAnyObjectByType<EncounterDirector>();
            }

            if (encounterDirector == null)
            {
                encounterDirector = gameObject.AddComponent<EncounterDirector>();
            }

            var dimmer = GetComponent<CombatFocusDimmer>() ?? FindAnyObjectByType<CombatFocusDimmer>();
            var letterbox = EncounterLetterboxOverlay.EnsureCreated();
            encounterDirector.Configure(
                _session,
                timelineView,
                dimmer,
                playerSkillShotChoreographer,
                musicSync,
                letterbox);
        }

        private void EnsureAudioListener()
        {
            if (FindAnyObjectByType<AudioListener>() != null)
            {
                return;
            }

            var cam = mainCamera != null ? mainCamera : Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[Bootstrap] No AudioListener and no Main Camera found.");
                return;
            }

            if (cam.GetComponent<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
                Debug.Log("[Bootstrap] Added AudioListener to Main Camera.");
            }
        }

        private void ResolveSceneReferences()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (unitsRoot == null)
            {
                var found = transform.Find("World/Units");
                unitsRoot = found != null ? found : transform.Find("Units");
            }

            if (gridRoot == null)
            {
                var found = transform.Find("World/Grid");
                gridRoot = found != null ? found : transform.Find("Grid");
            }

            if (timelineView == null)
            {
                timelineView = FindAnyObjectByType<BeatTimelineUIView>();
            }

            if (skillPanelView == null)
            {
                skillPanelView = FindAnyObjectByType<SkillPanelUIView>();
            }

            if (partyStatusBarView == null)
            {
                partyStatusBarView = FindAnyObjectByType<PartyStatusBarUIView>();
            }

            if (enemyStatusBarView == null)
            {
                enemyStatusBarView = FindAnyObjectByType<EnemyStatusBarUIView>();
            }

            if (executeOverlay == null)
            {
                executeOverlay = FindAnyObjectByType<CombatExecuteOverlayUIView>();
            }

            if (unitViews == null || unitViews.Length == 0)
            {
                unitViews = unitsRoot != null
                    ? unitsRoot.GetComponentsInChildren<UnitView>(true)
                    : GetComponentsInChildren<UnitView>(true);
            }

            timelineView?.WireReferences();
            skillPanelView?.WireReferences();
            partyStatusBarView?.WireReferences();
            executeOverlay?.WireReferences();
        }

        private bool HasSceneUnits()
        {
            return unitViews != null && unitViews.Length > 0 &&
                   System.Array.Exists(unitViews, v => v != null && v.ResolvePreset() != null);
        }

        private void SetupBoardDrag()
        {
            _boardDrag = GetComponent<BoardDragController>();
            if (_boardDrag == null)
            {
                _boardDrag = gameObject.AddComponent<BoardDragController>();
            }

            GridCellMarker[] markers = gridRoot != null
                ? gridRoot.GetComponentsInChildren<GridCellMarker>(true)
                : System.Array.Empty<GridCellMarker>();
            _boardDrag.Initialize(_session, _grid, markers, mainCamera);
            _boardDrag.SetUnitClickHandler(HandleUnitSelected);
            _boardDrag.SetFormationChangedHandler(RefreshPartyStatusBar);
            foreach (var view in unitViews)
            {
                if (view?.Unit != null && view.gameObject.activeSelf)
                {
                    view.Bind(view.Unit);
                }
            }
        }

        private void EnsureHoneycombGrid()
        {
            if (gridRoot == null)
            {
                return;
            }

            EnsureMissingGridCells();

            // Scene là nguồn chuẩn của layout (đã dựng 2×3 qua menu "Rebuild Hex Board Grid").
            // Runtime chỉ chuẩn bị visual/collider; ẩn an toàn ô ngoài phạm vi nếu còn sót.
            foreach (var marker in gridRoot.GetComponentsInChildren<GridCellMarker>(true))
            {
                if (!marker.Position.IsValid())
                {
                    marker.gameObject.SetActive(false);
                    continue;
                }

                marker.PrepareForPlay();
            }
        }

        private void EnsureMissingGridCells()
        {
            var existing = new HashSet<GridPosition>();
            Transform enemyParent = null;
            Transform playerParent = null;

            foreach (var marker in gridRoot.GetComponentsInChildren<GridCellMarker>(true))
            {
                if (marker == null)
                {
                    continue;
                }

                existing.Add(marker.Position);
                if (marker.Side == GridSide.Enemy && enemyParent == null)
                {
                    enemyParent = marker.transform.parent;
                }

                if (marker.Side == GridSide.Player && playerParent == null)
                {
                    playerParent = marker.transform.parent;
                }
            }

            for (var side = 0; side < 2; side++)
            {
                var gridSide = side == 0 ? GridSide.Player : GridSide.Enemy;
                var parent = gridSide == GridSide.Player ? playerParent : enemyParent;
                if (parent == null)
                {
                    parent = gridRoot;
                }

                for (var row = 0; row < DualGrid.Rows; row++)
                {
                    for (var col = 0; col < DualGrid.Columns; col++)
                    {
                        var pos = new GridPosition(gridSide, row, col);
                        if (existing.Contains(pos))
                        {
                            continue;
                        }

                        CreateRuntimeGridCell(parent, pos);
                        Debug.LogWarning(
                            $"[Bootstrap] Restored missing grid cell {pos.Side}_R{pos.Row}_C{pos.Column}.");
                    }
                }
            }
        }

        private static void CreateRuntimeGridCell(Transform parent, GridPosition pos)
        {
            var world = HexBoardLayout.GetWorldPosition(pos);
            var cellGo = new GameObject($"Cell_{pos.Side}_R{pos.Row}_C{pos.Column}");
            cellGo.transform.SetParent(parent, false);
            cellGo.transform.position = new Vector3(world.x, world.y, 0f);

            var marker = cellGo.AddComponent<GridCellMarker>();
            marker.Configure(pos.Side, pos.Row, pos.Column);
            marker.SetFloorSprite(HexSpriteUtil.ResolveHexagonFlatTop());
            marker.RebuildVisuals();
        }

        private void DisableSceneEnemyUnits()
        {
            if (unitViews == null || unitViews.Length == 0)
            {
                return;
            }

            var survivors = new List<UnitView>(unitViews.Length);
            foreach (var view in unitViews)
            {
                if (view == null)
                {
                    continue;
                }

                if (view.Side == GridSide.Enemy)
                {
                    if (view.Unit != null)
                    {
                        _grid.TryReleaseUnit(view.Unit);
                    }

                    Destroy(view.gameObject);
                    continue;
                }

                survivors.Add(view);
            }

            unitViews = survivors.ToArray();
        }

        private void RegisterPlayerSceneUnits(bool tutorialBasics = false)
        {
            foreach (var view in unitViews)
            {
                if (view == null || !view.gameObject.activeSelf || view.Side != GridSide.Player)
                {
                    continue;
                }

                var unitPreset = view.ResolvePreset();
                if (unitPreset == null)
                {
                    continue;
                }

                view.EnsureInteractionColliders();

                if (!TryResolveUnitGridPosition(view, out var pos))
                {
                    Debug.LogWarning($"[Bootstrap] Could not resolve grid cell for {view.name} ({view.DemoUnitKey})");
                    continue;
                }

                var unit = new CombatUnit(unitPreset, view.Side);
                if (tutorialBasics)
                {
                    PartyLoadoutApplicator.ApplyTutorialBasics(unit);
                }
                else
                {
                    PartyLoadoutApplicator.ApplyToUnit(unit);
                }

                if (!_grid.TryPlaceUnit(unit, pos))
                {
                    Debug.LogWarning($"[Bootstrap] Could not place {unitPreset.displayName} at {pos}");
                    continue;
                }

                view.PlaceOnGrid(pos);
                view.Bind(unit);
            }
        }

        private void ApplyPooledCombatBackground(CombatPoolRoll roll)
        {
            try
            {
                var bgRoot = GameObject.Find("Background canvas");
                if (bgRoot == null)
                {
                    Debug.LogWarning("[Bootstrap] Background canvas not found for pooled combat BG.");
                    return;
                }

                bgRoot.SetActive(true);
                if (bgRoot.transform.localScale == Vector3.zero)
                {
                    bgRoot.transform.localScale = Vector3.one;
                }

                var cam = mainCamera != null ? mainCamera : Camera.main;
                var bgCanvas = bgRoot.GetComponent<Canvas>();
                if (bgCanvas != null && cam != null)
                {
                    bgCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                    bgCanvas.worldCamera = cam;
                    bgCanvas.planeDistance = 100f;
                    bgCanvas.sortingOrder = -1;
                }

                StopLuxeArenaVideoPlayback(bgRoot);

                var index = roll != null ? roll.BackgroundIndex : 0;
                var sprite = CombatBackgroundPool.LoadSprite(index);
                if (sprite == null)
                {
                    Debug.LogWarning($"[Bootstrap] Pooled combat background missing for index {index}.");
                    return;
                }

                var image = EnsurePooledBackgroundImage(bgRoot.transform);
                image.sprite = sprite;
                image.color = Color.white;
                image.preserveAspect = false;
                image.raycastTarget = false;
                image.gameObject.SetActive(true);
                image.enabled = true;
                image.transform.SetAsLastSibling();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Bootstrap] Failed to apply pooled combat background: " + e);
            }
        }

        private static Image EnsurePooledBackgroundImage(Transform bgRoot)
        {
            var existing = bgRoot.Find("PooledBackground");
            if (existing != null && existing.TryGetComponent<Image>(out var existingImage))
            {
                return existingImage;
            }

            var go = new GameObject("PooledBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(bgRoot, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            var image = go.GetComponent<Image>();
            image.type = Image.Type.Simple;
            return image;
        }

        private static void StopLuxeArenaVideoPlayback(GameObject bgRoot)
        {
            foreach (var director in bgRoot.GetComponents<LuxeArenaBackgroundDirector>())
            {
                director.enabled = false;
            }

            var sceneVideo = bgRoot.transform.Find("SceneVideo");
            if (sceneVideo != null)
            {
                sceneVideo.gameObject.SetActive(false);
            }
        }

        private void RegisterSceneUnits(bool tutorialBasics = false)
        {
            foreach (var view in unitViews)
            {
                if (view == null || !view.gameObject.activeSelf)
                {
                    continue;
                }

                var unitPreset = view.ResolvePreset();
                if (unitPreset == null)
                {
                    continue;
                }

                view.EnsureInteractionColliders();

                if (!TryResolveUnitGridPosition(view, out var pos))
                {
                    Debug.LogWarning($"[Bootstrap] Could not resolve grid cell for {view.name} ({view.DemoUnitKey})");
                    continue;
                }

                var unit = new CombatUnit(unitPreset, view.Side);
                if (tutorialBasics)
                {
                    PartyLoadoutApplicator.ApplyTutorialBasics(unit);
                }
                else
                {
                    PartyLoadoutApplicator.ApplyToUnit(unit);
                }

                PartyLoadoutApplicator.ApplyDifficultyToEnemy(unit);
                if (!_grid.TryPlaceUnit(unit, pos))
                {
                    Debug.LogWarning($"[Bootstrap] Could not place {unitPreset.displayName} at {pos}");
                    continue;
                }

                view.PlaceOnGrid(pos);
                view.Bind(unit);
            }
        }

        private bool TryResolveUnitGridPosition(UnitView view, out GridPosition position)
        {
            if (view.IsPlacedOnGrid)
            {
                position = view.GridPosition;
                return true;
            }

            if (TryFindCellFromWorldPosition(view.FeetWorldPosition, view.Side, out position))
            {
                return true;
            }

            return DefaultPartyFormation.TryGetStartupCell(view.DemoUnitKey, view.Side, out position);
        }

        private bool TryFindCellFromWorldPosition(Vector3 world, GridSide side, out GridPosition position)
        {
            position = default;
            if (_cellByPosition == null || _cellByPosition.Count == 0)
            {
                return false;
            }

            GridCellMarker best = null;
            var bestDist = float.MaxValue;
            foreach (var pair in _cellByPosition)
            {
                if (pair.Key.Side != side || pair.Value == null)
                {
                    continue;
                }

                var cellPos = pair.Value.position;
                var dist = Vector2.Distance(new Vector2(world.x, world.y), new Vector2(cellPos.x, cellPos.y));
                if (dist >= 1.15f || dist >= bestDist)
                {
                    continue;
                }

                bestDist = dist;
                best = pair.Value.GetComponent<GridCellMarker>();
            }

            if (best == null)
            {
                return false;
            }

            position = best.Position;
            return true;
        }

        private void CacheGridCellTransforms()
        {
            _cellByPosition = new Dictionary<GridPosition, Transform>();
            if (gridRoot == null)
            {
                return;
            }

            foreach (var marker in gridRoot.GetComponentsInChildren<GridCellMarker>(true))
            {
                _cellByPosition[marker.Position] = marker.transform;
            }
        }

        private void ApplyHandoffToSceneEnemies(EncounterDefinitionSO handoff)
        {
            if (handoff?.units == null || unitViews == null)
            {
                return;
            }

            var enemySpawns = new List<EncounterUnitSpawn>();
            foreach (var spawn in handoff.units)
            {
                if (spawn.preset != null && spawn.side == GridSide.Enemy)
                {
                    enemySpawns.Add(spawn);
                }
            }

            var usedSpawns = new HashSet<int>();

            foreach (var view in unitViews)
            {
                if (view == null || view.Side != GridSide.Enemy || !view.gameObject.activeSelf)
                {
                    continue;
                }

                var matchIndex = FindMatchingHandoffSpawn(view, enemySpawns, usedSpawns);
                if (matchIndex < 0)
                {
                    Debug.LogWarning($"[Bootstrap] Scene enemy {view.name} has no matching handoff spawn.");
                    continue;
                }

                usedSpawns.Add(matchIndex);
                var spawn = enemySpawns[matchIndex];
                var handoffPreset = spawn.preset;
                var pos = view.IsPlacedOnGrid
                    ? view.GridPosition
                    : new GridPosition(spawn.side, spawn.row, spawn.column);

                if (view.Unit != null)
                {
                    _grid.TryReleaseUnit(view.Unit);
                }

                var unit = new CombatUnit(handoffPreset, GridSide.Enemy);
                PartyLoadoutApplicator.ApplyDifficultyToEnemy(unit);
                if (!_grid.TryPlaceUnit(unit, pos))
                {
                    Debug.LogWarning(
                        $"[Bootstrap] Could not place handoff enemy {handoffPreset.displayName} at {pos}");
                    continue;
                }

                view.PlaceOnGrid(pos);
                view.Bind(unit);
            }

            for (var i = 0; i < enemySpawns.Count; i++)
            {
                if (usedSpawns.Contains(i))
                {
                    continue;
                }

                Debug.LogWarning(
                    $"[Bootstrap] Handoff enemy {enemySpawns[i].preset.displayName} has no scene unit; not spawning.");
            }
        }

        private static int FindMatchingHandoffSpawn(
            UnitView view,
            List<EncounterUnitSpawn> spawns,
            HashSet<int> used)
        {
            var scenePreset = view.ResolvePreset();
            var sceneKey = view.DemoUnitKey;

            for (var i = 0; i < spawns.Count; i++)
            {
                if (used.Contains(i))
                {
                    continue;
                }

                var spawn = spawns[i];
                if (spawn.preset == scenePreset)
                {
                    return i;
                }

                if (!string.IsNullOrEmpty(sceneKey) && spawn.preset?.unitId == sceneKey)
                {
                    return i;
                }

                if (scenePreset != null
                    && !string.IsNullOrEmpty(scenePreset.unitId)
                    && spawn.preset?.unitId == scenePreset.unitId)
                {
                    return i;
                }
            }

            return -1;
        }

        private void RefreshUnitViewsCache()
        {
            unitViews = unitsRoot != null
                ? unitsRoot.GetComponentsInChildren<UnitView>(true)
                : GetComponentsInChildren<UnitView>(true);
        }

        private static EncounterDefinitionSO MergePartyIfEnemyOnly(EncounterDefinitionSO encounter)
        {
            if (encounter?.units == null || encounter.units.Length == 0)
            {
                return EncounterRuntimeFactory.CreateDemoEncounter();
            }

            var hasPlayer = false;
            foreach (var spawn in encounter.units)
            {
                if (spawn.preset != null && spawn.side == GridSide.Player)
                {
                    hasPlayer = true;
                    break;
                }
            }

            if (hasPlayer)
            {
                return encounter;
            }

            var merged = ScriptableObject.CreateInstance<EncounterDefinitionSO>();
            merged.encounterId = encounter.encounterId;
            var party = EncounterCatalog.IsTutorial(encounter.encounterId)
                ? EncounterRuntimeFactory.CreateTutorialPartySpawns()
                : EncounterRuntimeFactory.CreateDefaultPartySpawns();
            merged.units = MergeSpawns(party, encounter.units);
            return merged;
        }

        private static EncounterUnitSpawn[] MergeSpawns(EncounterUnitSpawn[] a, EncounterUnitSpawn[] b)
        {
            var merged = new EncounterUnitSpawn[a.Length + b.Length];
            System.Array.Copy(a, 0, merged, 0, a.Length);
            System.Array.Copy(b, 0, merged, a.Length, b.Length);
            return merged;
        }

        private void SpawnUnitsFromEncounter(EncounterDefinitionSO encounter, bool enemiesOnly, bool tutorialBasics = false)
        {
            if (encounter?.units == null)
            {
                return;
            }

            if (unitsRoot == null)
            {
                var go = new GameObject("Units");
                go.transform.SetParent(transform, false);
                unitsRoot = go.transform;
            }

            foreach (var spawn in encounter.units)
            {
                if (spawn.preset == null)
                {
                    continue;
                }

                if (enemiesOnly && spawn.side != GridSide.Enemy)
                {
                    continue;
                }

                var unit = new CombatUnit(spawn.preset, spawn.side);
                if (tutorialBasics)
                {
                    PartyLoadoutApplicator.ApplyTutorialBasics(unit);
                }
                else
                {
                    PartyLoadoutApplicator.ApplyToUnit(unit);
                }

                PartyLoadoutApplicator.ApplyDifficultyToEnemy(unit);
                var pos = new GridPosition(spawn.side, spawn.row, spawn.column);
                if (!_grid.TryPlaceUnitOrEmptyCell(unit, ref pos))
                {
                    Debug.LogWarning($"[Bootstrap] Could not place {spawn.preset.displayName} at {pos}");
                    continue;
                }

            var cellWorld = ResolveCellWorld(pos);
            var unitKey = spawn.preset?.unitId ?? "grunt";
            var isPoolUnit = CombatPoolUnitVisuals.IsPoolCombatKey(unitKey);
            UnitView view;
            if (isPoolUnit)
            {
                view = CombatPoolUnitVisuals.InstantiatePoolUnit(
                    unitKey,
                    unitsRoot,
                    cellWorld,
                    10 + pos.Row);
                if (view == null)
                {
                    continue;
                }
            }
            else
            {
                var unitGo = new GameObject($"Unit_{unit.DisplayName}");
                unitGo.transform.SetParent(unitsRoot, false);
                unitGo.transform.position = cellWorld;
                EnsureSpawnSpriteRenderer(unitGo, spawn.preset, pos.Row);
                view = unitGo.AddComponent<UnitView>();
            }

            view.ConfigureDemo(unitKey, spawn.side);
            view.PlaceOnGrid(pos);
            view.Bind(unit);
            if (isPoolUnit)
            {
                CombatPoolUnitVisuals.PlayIdle(view, unitKey);
                view.FitBodyColliderToSprite();
                CombatPoolUnitVisuals.SnapSpawnedUnitToCell(view, ResolveCellWorld(pos));
            }
            else
            {
                view.RefitBodyColliderToSprite();
            }
            }
        }

        private Vector3 ResolveCellWorld(GridPosition pos)
        {
            if (_cellByPosition != null
                && _cellByPosition.TryGetValue(pos, out var cell)
                && cell != null)
            {
                return cell.position;
            }

            return HexBoardLayout.GetWorldPosition(pos, sideGap);
        }

        private static void EnsureSpawnSpriteRenderer(GameObject unitGo, UnitPresetSO preset, int row)
        {
            var sr = unitGo.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = unitGo.AddComponent<SpriteRenderer>();
            }

            sr.sortingOrder = 10 + row;
            if (preset?.battleSprite != null)
            {
                sr.sprite = preset.battleSprite;
                sr.color = Color.white;
                return;
            }

            sr.color = preset != null ? preset.placeholderColor : Color.white;
        }

        private void HandleUnitSelected(CombatUnit unit, UnitView view)
        {
            if (skillPanelView != null && !skillPanelView.CanOpenSkillPanelNow())
            {
                return;
            }

            skillPanelView?.ToggleForUnit(unit, view);
        }

        private void RefreshPartyStatusBar()
        {
            if (partyStatusBarView == null)
            {
                return;
            }

            if (_session?.Grid != null)
            {
                partyStatusBarView.BindFromSession(_session);
            }
            else
            {
                var views = unitsRoot != null
                    ? unitsRoot.GetComponentsInChildren<UnitView>(true)
                    : GetComponentsInChildren<UnitView>(true);

                if (partyStatusBarView.BoundUnitCount > 0)
                {
                    partyStatusBarView.RefreshFormationOrderFromUnitViews(views);
                }
                else
                {
                    partyStatusBarView.BindFromUnitViews(views);
                }
            }

            // Lanes must follow party cards in the same frame as formation change.
            timelineView?.SyncPartyLanesNow();
        }
    }
}

using System.Collections;
using FracturedChorus.Audio;
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Data;
using FracturedChorus.RunMap.Core;
using FracturedChorus.RunMap.UI;
using FracturedChorus.Tutorial;
using FracturedChorus.UI;
using FracturedChorus.UI.Loading;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FracturedChorus.RunMap
{
    public class CadenceMapController : MonoBehaviour
    {
        public enum RunMapEditorPreview
        {
            MapSelect = 0,
            MapNodes = 1,
            Treasure = 2,
            Event = 3,
            Camp = 4,
            Shop = 5
        }

        public static CadenceMapController Instance { get; private set; }

        [SerializeField] private CadenceMapLayoutSO layout;
        [SerializeField] private PinkyVaultConfigSO pinkyVaultConfig;
        [SerializeField] private CadenceMacroMapView macroView;
        [SerializeField] private GameObject macroMapRoot;
        [SerializeField] private GameObject innerMapRoot;
        [SerializeField] private GameObject mapScrollView;
        [SerializeField] private GameObject legendPanel;
        [SerializeField] private RunMapController innerController;
        [SerializeField] private RunMapBootstrap bootstrap;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Button backToMacroButton;
        [SerializeField] private Button returnToHubButton;
        [SerializeField] private SceneLinkHotkeyUI campusHubHotkey;
        [SerializeField] private MapNodeIconSetSO nodeIconSet;

        [Header("Combat")]
        [SerializeField] private string bossCombatSceneName = RunMapSceneCatalog.CombatPrototype;
        [SerializeField] private float bossSceneLoadDelaySec = 0.35f;
        [SerializeField] private bool simulateBossVictoryOnReturn;

#if UNITY_EDITOR
        [Header("Edit Mode Preview")]
        [SerializeField] private RunMapEditorPreview editorPreview = RunMapEditorPreview.MapSelect;
#endif

        public CadenceRunProgress Progress => CadenceRunProgress.Session;

        private bool _loadingBossScene;
        private bool _advancingSector;
        private Coroutine _bossLoadCoroutine;
        private Coroutine _innerBootCoroutine;
        private static bool s_pendingBossVictory;
        private CanvasGroup _macroCanvasGroup;

        private void Awake()
        {
            Instance = this;
            macroView ??= GetComponentInChildren<CadenceMacroMapView>(true);
            innerController ??= GetComponentInChildren<RunMapController>(true);
            bootstrap ??= GetComponent<RunMapBootstrap>();

            if (macroView != null)
            {
                macroView.VaultSelected += HandleVaultSelected;
            }

            if (backToMacroButton != null)
            {
                backToMacroButton.onClick.AddListener(ShowMacroMap);
            }

            ResolveLayerReferences();
            EnsureCampusHubHotkey();

            if (returnToHubButton != null
                && (campusHubHotkey == null || returnToHubButton.gameObject != campusHubHotkey.gameObject))
            {
                returnToHubButton.onClick.AddListener(ReturnToCampusHub);
            }
        }

        private void Update()
        {
            if (!FracturedChorus.Meta.GameMetaSession.HasSession
                || !FracturedChorus.Meta.GameMetaSession.Current.RunSnapshot.HasActiveRun)
            {
                return;
            }

            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ReturnToCampusHub();
            }
        }

        private void EnsureCampusHubHotkey()
        {
            ResolveLayerReferences();

            if (campusHubHotkey != null)
            {
                campusHubHotkey.Bind(ReturnToCampusHub);
                campusHubHotkey.gameObject.SetActive(true);
                if (returnToHubButton == null)
                {
                    returnToHubButton = campusHubHotkey.GetComponent<Button>();
                }

                return;
            }

            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                return;
            }

            var layerAnchor = innerMapRoot != null ? innerMapRoot.transform : null;
            var overlay = SceneLinkHotkeyUI.EnsureSceneLinkOverlay(canvas.transform, layerAnchor);
            campusHubHotkey = SceneLinkHotkeyUI.Ensure(
                overlay != null ? overlay : canvas.transform,
                "Campus Hub",
                ReturnToCampusHub,
                placement: SceneLinkHotkeyPlacement.BottomLeft,
                persistInScene: true);

            if (returnToHubButton == null && campusHubHotkey != null)
            {
                returnToHubButton = campusHubHotkey.GetComponent<Button>();
            }
        }

        public void ReturnToCampusHub()
        {
            RunMapHubBridge.ReturnToCampusHub();
        }

        private void ResolveLayerReferences()
        {
            macroMapRoot ??= macroView != null ? macroView.gameObject : GameObject.Find("MacroMapLayer");
            innerMapRoot ??= GameObject.Find("InnerMapLayer");
            mapScrollView ??= GameObject.Find("MapScrollView");
            legendPanel ??= GameObject.Find("LegendPanel");

            if (macroMapRoot != null)
            {
                _macroCanvasGroup ??= macroMapRoot.GetComponent<CanvasGroup>() ?? macroMapRoot.AddComponent<CanvasGroup>();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (macroView != null)
            {
                macroView.VaultSelected -= HandleVaultSelected;
            }
        }

        private void Start()
        {
            if (macroView == null)
            {
                Debug.LogError(
                    "[Fractured Chorus] Cadence macro map chưa setup. Menu: Fractured Chorus → Run Map → Setup Cadence Macro Layer.");
                SetMacroLayerActive(false);
                if (innerController != null)
                {
                    innerController.enabled = true;
                }

                return;
            }

            if (innerController != null)
            {
                innerController.enabled = false;
            }

            ShowMacroMap();
            macroView.Build(layout);
            macroView.SetVaultUnlocked(VaultFingerId.Pinky, true);
            TutorialDirector.Ensure().StartMapTrack();

            if (s_pendingBossVictory)
            {
                s_pendingBossVictory = false;
                CombatEncounterHandoff.ClearResultFlags();
                HandleBossVictory();
                return;
            }

            if (CombatEncounterHandoff.HasResult || CombatEncounterHandoff.PendingReturnToNearestCamp)
            {
                var seed = Progress.RunSeed > 0 ? Progress.RunSeed : Random.Range(1, int.MaxValue);
                EnterInnerSector(Progress.CurrentSector, seed);
            }
        }

        public static void NotifyBossVictory()
        {
            s_pendingBossVictory = false;
            if (Instance != null)
            {
                Instance.HandleBossVictory();
            }
        }

        public static void MarkBossVictoryPending()
        {
            s_pendingBossVictory = true;
        }

        private void HandleVaultSelected(VaultFingerId finger)
        {
            if (finger != VaultFingerId.Pinky)
            {
                SetStatus("This vault is locked.");
                return;
            }

            LoadingScreenController.ShowCoverNow();

            var seed = bootstrap != null ? bootstrap.ResolveSeed() : Random.Range(1, int.MaxValue);
            Progress.BeginPinkyRun(seed);
            RunMapBgmController.StopAll();
            var beatMap = Resources.Load<MusicBeatMapSO>("Music/EternalSpark_Candence_BeatMap");
            RunMusicSession.Ensure().Begin(beatMap != null ? beatMap.Clip : null, beatMap);
            EnterInnerSector(Progress.CurrentSector, seed);
        }

        private void EnterInnerSector(PinkySectorId sector, int seed)
        {
            if (_innerBootCoroutine != null)
            {
                StopCoroutine(_innerBootCoroutine);
            }

            _innerBootCoroutine = StartCoroutine(EnterInnerSectorDeferred(sector, seed));
        }

        private IEnumerator EnterInnerSectorDeferred(PinkySectorId sector, int seed)
        {
            ShowInnerMap();

            var weights = pinkyVaultConfig != null
                ? pinkyVaultConfig.WeightsFor(sector)
                : bootstrap?.Template != null
                    ? NodeTypeAssigner.WeightsFromTemplate(bootstrap.Template)
                    : null;
            MapGraph graph;
            try
            {
                graph = MapGenerator.GenerateSector(sector, seed, weights, pinkyVaultConfig);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Fractured Chorus] GenerateSector failed: {ex.Message}\n{ex.StackTrace}");
                _innerBootCoroutine = null;
                HideLoadingOverlay();
                yield break;
            }

            if (innerController == null)
            {
                Debug.LogError("[Fractured Chorus] CadenceMapController: innerController null.");
                _innerBootCoroutine = null;
                HideLoadingOverlay();
                yield break;
            }

            innerController.enabled = true;

            var mapView = innerController.GetComponentInChildren<RunMapUIView>(true);
            if (mapView != null && !mapView.isActiveAndEnabled)
            {
                mapView.gameObject.SetActive(true);
            }

            const int maxFrames = 24;
            for (var i = 0; i < maxFrames; i++)
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
                if (mapView == null || mapView.IsViewportReady)
                {
                    break;
                }
            }

            innerController.Initialize(graph, seed);
            var hadCombatReturn = CombatEncounterHandoff.HasResult
                                  || CombatEncounterHandoff.PendingReturnToNearestCamp;
            var defeatReturn = CombatEncounterHandoff.PendingReturnToNearestCamp;
            innerController.ApplyCombatReturnHandoff();
            RunMusicSession.Instance?.SetMode(RunMusicMode.Map);

            if (mapView != null)
            {
                if (hadCombatReturn && innerController.State != null && innerController.Graph != null)
                {
                    var current = innerController.Graph.GetNode(innerController.State.CurrentNodeId);
                    if (current != null)
                    {
                        mapView.ScrollToNode(current, immediate: true);
                    }
                    else
                    {
                        mapView.EnsureScrollShowsStartOnOpen(true);
                    }
                }
                else
                {
                    mapView.EnsureScrollShowsStartOnOpen(true);
                }
            }

            var bossLabel = pinkyVaultConfig != null
                ? pinkyVaultConfig.GetSector(sector).bossLabel
                : Progress.SectorBossLabel(sector);
            var sectorTitle = pinkyVaultConfig != null
                ? pinkyVaultConfig.GetSector(sector).title
                : SectorTitle(sector);
            var mapIndex = SectorMapIndex(sector);
            SetStatus(defeatReturn
                ? $"Returned to nearest camp — set up. ({sectorTitle})"
                : hadCombatReturn
                    ? $"Victory — node cleared. Choose the next path. ({sectorTitle})"
                    : $"Pinky — Map {mapIndex}/3 · {sectorTitle} · F1 → {bossLabel}");

            _innerBootCoroutine = null;
            HideLoadingOverlay();
        }

        private static int SectorMapIndex(PinkySectorId sector) => sector switch
        {
            PinkySectorId.Pulse => 1,
            PinkySectorId.Echo => 2,
            PinkySectorId.Canticle => 3,
            _ => 0
        };

        private void ShowMacroMap()
        {
            ResolveLayerReferences();
            SetMacroLayerActive(true);
            SetInnerUiActive(false);
            HideEditPreviewRuntime();
            HideRoomOverlays();
            HideLoadingOverlay();

            if (macroMapRoot != null)
            {
                macroMapRoot.transform.SetAsLastSibling();
            }

            if (innerController != null)
            {
                innerController.enabled = false;
            }

            if (backToMacroButton != null)
            {
                backToMacroButton.gameObject.SetActive(false);
            }

            SetStatus("Cadence Macro Map — select Pinky Vault.");
        }

        private void ShowInnerMap()
        {
            ResolveLayerReferences();
            SetMacroLayerActive(false);
            SetInnerUiActive(true);
            HideEditPreviewRuntime();
            HideRoomOverlays();

            if (innerMapRoot != null)
            {
                innerMapRoot.transform.SetAsLastSibling();
            }

            if (backToMacroButton != null)
            {
                backToMacroButton.gameObject.SetActive(true);
            }
        }

        private void HideEditPreviewRuntime()
        {
            if (innerMapRoot == null)
            {
                return;
            }

            DestroyNodeEditPreview(innerMapRoot.transform);
        }

        private static void HideRoomOverlays()
        {
            foreach (var view in UnityEngine.Object.FindObjectsByType<TreasureRoomOverlayUIView>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                view.Hide();
            }

            foreach (var view in UnityEngine.Object.FindObjectsByType<EventRoomOverlayUIView>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                view.Hide();
            }

            foreach (var view in UnityEngine.Object.FindObjectsByType<CampRoomOverlayUIView>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                view.Hide();
            }

            foreach (var view in UnityEngine.Object.FindObjectsByType<ShopRoomOverlayUIView>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                view.Hide();
            }
        }

        private static void HideLoadingOverlay()
        {
            LoadingScreenController.HideCoverNow();
        }

        private static void DestroyNodeEditPreview(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            var preview = parent.Find("NodeEditPreview");
            if (preview == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(preview.gameObject);
                return;
            }
#endif
            Destroy(preview.gameObject);
        }

        private void SetMacroLayerActive(bool active)
        {
            if (macroMapRoot != null)
            {
                macroMapRoot.SetActive(active);
            }
            else if (macroView != null)
            {
                macroView.gameObject.SetActive(active);
            }

            if (_macroCanvasGroup != null)
            {
                _macroCanvasGroup.blocksRaycasts = active;
                _macroCanvasGroup.interactable = active;
                _macroCanvasGroup.alpha = active ? 1f : 0f;
            }
        }

        private void SetInnerUiActive(bool active)
        {
            if (innerMapRoot != null)
            {
                innerMapRoot.SetActive(active);
            }

            if (mapScrollView != null && (innerMapRoot == null || mapScrollView.transform.parent != innerMapRoot.transform))
            {
                mapScrollView.SetActive(active);
            }

            if (legendPanel != null && (innerMapRoot == null || legendPanel.transform.parent != innerMapRoot.transform))
            {
                legendPanel.SetActive(active);
            }
        }

        public void OnInnerBossEngaged(PinkySectorId sector)
        {
            if (_loadingBossScene || _advancingSector)
            {
                return;
            }

            if (!SectorLoadsBossScene(sector))
            {
                _advancingSector = true;
                var bossLabel = pinkyVaultConfig != null
                    ? pinkyVaultConfig.GetSector(sector).bossLabel
                    : Progress.SectorBossLabel(sector);
                var next = Progress.NextSector(sector);
                var nextTitle = next.HasValue
                    ? pinkyVaultConfig != null
                        ? pinkyVaultConfig.GetSector(next.Value).title
                        : SectorTitle(next.Value)
                    : string.Empty;
                SetStatus(string.IsNullOrEmpty(nextTitle)
                    ? $"{bossLabel} cleared."
                    : $"{bossLabel} cleared — opening {nextTitle}…");
                HandleBossVictory();
                _advancingSector = false;
                return;
            }

            _loadingBossScene = true;
            CombatEncounterHandoff.SetPending(
                encounterId: EncounterCatalog.BossDespair,
                returnScene: RunMapSceneCatalog.RunMapPrototype,
                sourceNodeId: -1);
            if (simulateBossVictoryOnReturn)
            {
                MarkBossVictoryPending();
            }

            SetStatus($"Entering battle: {Progress.SectorBossLabel(sector)}…");

            if (_bossLoadCoroutine != null)
            {
                StopCoroutine(_bossLoadCoroutine);
            }

            _bossLoadCoroutine = StartCoroutine(LoadBossCombatDeferred(sector));
        }

        private bool SectorLoadsBossScene(PinkySectorId sector)
        {
            if (pinkyVaultConfig != null)
            {
                return pinkyVaultConfig.GetSector(sector).loadBossScene;
            }

            return !Progress.HasNextSector(sector);
        }

        public bool SectorLoadsBossSceneForUi(PinkySectorId sector) => SectorLoadsBossScene(sector);

        private IEnumerator LoadBossCombatDeferred(PinkySectorId sector)
        {
            _ = sector;
            if (bossSceneLoadDelaySec > 0f)
            {
                yield return new WaitForSecondsRealtime(bossSceneLoadDelaySec);
            }

            var loaded = RunMapSceneLoader.LoadByName(bossCombatSceneName);
            if (!loaded)
            {
                _loadingBossScene = false;
                _bossLoadCoroutine = null;
                SetStatus("Failed to load combat scene.");
            }
        }

        private void HandleBossVictory()
        {
            _loadingBossScene = false;
            _bossLoadCoroutine = null;

            var cleared = Progress.CurrentSector;
            Progress.MarkSectorCleared(cleared);

            if (Progress.IsPinkyComplete)
            {
                ShowMacroMap();
                macroView?.Build(layout);
                macroView?.SetVaultUnlocked(VaultFingerId.Pinky, true);
                SetStatus("Chart Lord defeated — Pinky Vault complete.");
                return;
            }

            var next = Progress.CurrentSector;
            var seed = Progress.RunSeed + (int)next * 997;
            EnterInnerSector(next, seed);
            SetStatus($"Opening {SectorTitle(next)} — continue Dive.");
        }

        private static string SectorTitle(PinkySectorId sector) => sector switch
        {
            PinkySectorId.Pulse => "Part 1 · Pulse Lane",
            PinkySectorId.Echo => "Part 2 · Echo Lane",
            PinkySectorId.Canticle => "Part 3 · Canticle Lane",
            _ => sector.ToString()
        };

        private void SetStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message;
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

        public void SetEditorPreview(RunMapEditorPreview preview)
        {
            editorPreview = preview;
            ApplyEditorPreview();
        }

        public void ApplyEditorPreview()
        {
            if (_applyingEditorPreview)
            {
                return;
            }

            _applyingEditorPreview = true;
            try
            {
                ResolveLayerReferences();
                HideRoomOverlays();

                switch (editorPreview)
                {
                    case RunMapEditorPreview.MapSelect:
                        SetMacroLayerActive(true);
                        SetInnerUiActive(false);
                        DestroyNodeEditPreview(innerMapRoot != null ? innerMapRoot.transform : transform);
                        if (macroMapRoot != null)
                        {
                            macroMapRoot.transform.SetAsLastSibling();
                        }

                        if (backToMacroButton != null)
                        {
                            backToMacroButton.gameObject.SetActive(false);
                        }

                        break;
                    case RunMapEditorPreview.MapNodes:
                        SetMacroLayerActive(false);
                        SetInnerUiActive(true);
                        DestroyNodeEditPreview(innerMapRoot != null ? innerMapRoot.transform : transform);
                        EnsureNodeInfoSidebarForEdit();
                        ShowLayoutPreview();
                        if (innerMapRoot != null)
                        {
                            innerMapRoot.transform.SetAsLastSibling();
                        }

                        if (backToMacroButton != null)
                        {
                            backToMacroButton.gameObject.SetActive(true);
                        }

                        break;
                    case RunMapEditorPreview.Treasure:
                        SetMacroLayerActive(false);
                        SetInnerUiActive(false);
                        DestroyNodeEditPreview(innerMapRoot != null ? innerMapRoot.transform : transform);
                        if (backToMacroButton != null)
                        {
                            backToMacroButton.gameObject.SetActive(false);
                        }

                        ShowTreasureRoomEditPreview();
                        break;
                    case RunMapEditorPreview.Event:
                        SetMacroLayerActive(false);
                        SetInnerUiActive(false);
                        DestroyNodeEditPreview(innerMapRoot != null ? innerMapRoot.transform : transform);
                        if (backToMacroButton != null)
                        {
                            backToMacroButton.gameObject.SetActive(false);
                        }

                        ShowEventRoomEditPreview();
                        break;
                    case RunMapEditorPreview.Camp:
                        SetMacroLayerActive(false);
                        SetInnerUiActive(false);
                        DestroyNodeEditPreview(innerMapRoot != null ? innerMapRoot.transform : transform);
                        if (backToMacroButton != null)
                        {
                            backToMacroButton.gameObject.SetActive(false);
                        }

                        ShowCampRoomEditPreview();
                        break;
                    case RunMapEditorPreview.Shop:
                        SetMacroLayerActive(false);
                        SetInnerUiActive(false);
                        DestroyNodeEditPreview(innerMapRoot != null ? innerMapRoot.transform : transform);
                        if (backToMacroButton != null)
                        {
                            backToMacroButton.gameObject.SetActive(false);
                        }

                        ShowShopRoomEditPreview();
                        break;
                }

                EditorUtility.SetDirty(this);
                if (macroMapRoot != null)
                {
                    EditorUtility.SetDirty(macroMapRoot);
                }

                if (innerMapRoot != null)
                {
                    EditorUtility.SetDirty(innerMapRoot);
                }
            }
            finally
            {
                _applyingEditorPreview = false;
            }
        }

        private void EnsureNodeInfoSidebarForEdit()
        {
            ResolveLayerReferences();
            if (mapScrollView == null)
            {
                return;
            }

            var scrollRect = mapScrollView.GetComponent<ScrollRect>();
            var parent = scrollRect != null && scrollRect.viewport != null
                ? scrollRect.viewport
                : mapScrollView.transform;
            var existing = parent.Find("NodeInfoSidebar");
            var panel = existing != null
                ? existing.GetComponent<RunMapNodeInfoPanel>()
                : RunMapNodeInfoPanelBuilder.EnsureSidebar(parent, showEditPreview: true);
            if (panel == null)
            {
                return;
            }

            panel.ShowEditPreview();

            if (innerController != null)
            {
                var so = new SerializedObject(innerController);
                var prop = so.FindProperty("nodeInfoPanel");
                if (prop != null && prop.objectReferenceValue != panel)
                {
                    prop.objectReferenceValue = panel;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private void ShowLayoutPreview()
        {
            var preview = UnityEngine.Object.FindAnyObjectByType<RunMapLayoutScenePreview>(FindObjectsInactive.Include);
            preview?.AllowScenePreview();
        }

        private Transform ResolveCanvasRoot()
        {
            var named = GameObject.Find("RunMapCanvas");
            if (named != null)
            {
                return named.transform;
            }

            var canvas = GetComponentInChildren<Canvas>(true);
            return canvas != null ? canvas.transform : null;
        }

        private void ShowTreasureRoomEditPreview()
        {
            var overlay = TreasureRoomOverlayUIView.EnsureOnCanvas(ResolveCanvasRoot());
            overlay?.ShowEditPreview();
        }

        private void ShowEventRoomEditPreview()
        {
            var overlay = EventRoomOverlayUIView.EnsureOnCanvas(ResolveCanvasRoot());
            overlay?.ShowEditPreview();
        }

        private void ShowCampRoomEditPreview()
        {
            var overlay = CampRoomOverlayUIView.EnsureOnCanvas(ResolveCanvasRoot());
            overlay?.ShowEditPreview();
        }

        private void ShowShopRoomEditPreview()
        {
            var overlay = ShopRoomOverlayUIView.EnsureOnCanvas(ResolveCanvasRoot());
            overlay?.ShowEditPreview();
        }

        public void WireSceneEditChrome()
        {
            editorPreview = RunMapEditorPreview.MapNodes;
            ApplyEditorPreview();
            EditorUtility.SetDirty(this);
        }
#endif
    }
}

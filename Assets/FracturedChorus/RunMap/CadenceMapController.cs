using System.Collections;
using FracturedChorus.Data;
using FracturedChorus.RunMap.Core;
using FracturedChorus.RunMap.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.RunMap
{
    public class CadenceMapController : MonoBehaviour
    {
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

        [Header("Combat")]
        [SerializeField] private string bossCombatSceneName = RunMapSceneCatalog.CombatPrototype;
        [SerializeField] private float bossSceneLoadDelaySec = 0.35f;
        [SerializeField] private bool simulateBossVictoryOnReturn;

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

            if (returnToHubButton != null)
            {
                returnToHubButton.onClick.AddListener(ReturnToCampusHub);
            }

            ResolveLayerReferences();
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

            if (s_pendingBossVictory)
            {
                s_pendingBossVictory = false;
                HandleBossVictory();
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

            var seed = bootstrap != null ? bootstrap.ResolveSeed() : Random.Range(1, int.MaxValue);
            Progress.BeginPinkyRun(seed);
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
                yield break;
            }

            if (innerController == null)
            {
                Debug.LogError("[Fractured Chorus] CadenceMapController: innerController null.");
                _innerBootCoroutine = null;
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

            if (mapView != null)
            {
                mapView.ScrollToStartFloor();
            }

            var bossLabel = pinkyVaultConfig != null
                ? pinkyVaultConfig.GetSector(sector).bossLabel
                : Progress.SectorBossLabel(sector);
            var sectorTitle = pinkyVaultConfig != null
                ? pinkyVaultConfig.GetSector(sector).title
                : SectorTitle(sector);
            var mapIndex = SectorMapIndex(sector);
            SetStatus($"Pinky — Map {mapIndex}/3 · {sectorTitle} · F1 → {bossLabel}");
            _innerBootCoroutine = null;
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

            if (innerMapRoot != null)
            {
                innerMapRoot.transform.SetAsLastSibling();
            }

            if (backToMacroButton != null)
            {
                backToMacroButton.gameObject.SetActive(true);
            }
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
    }
}

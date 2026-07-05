using System.Collections;
using FracturedChorus.RunMap.Core;
using FracturedChorus.RunMap.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.RunMap
{
    public class RunMapController : MonoBehaviour
    {
        [SerializeField] private RunMapUIView mapView;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text seedLabel;

        [Header("Scene flow")]
        [SerializeField] private string bossCombatSceneName = RunMapSceneCatalog.CombatPrototype;
        [SerializeField] private float bossSceneLoadDelaySec = 0.35f;

        public MapGraph Graph { get; private set; }
        public RunState State { get; } = new RunState();

        private bool _bootStarted;
        private bool _loadingBossScene;
        private Coroutine _bossLoadCoroutine;

        private void Awake()
        {
            mapView ??= GetComponentInChildren<RunMapUIView>(true);
            BindNodeClickHandlers();
        }

        private void Start()
        {
            if (GetComponentInParent<CadenceMapController>(true) != null)
            {
                return;
            }

            if (_bootStarted || Graph != null)
            {
                return;
            }

            _bootStarted = true;
            StartCoroutine(BootRunMap());
        }

        private void OnEnable() => BindNodeClickHandlers();

        private void OnDisable()
        {
            UnbindNodeClickHandlers();

            if (_bossLoadCoroutine != null)
            {
                StopCoroutine(_bossLoadCoroutine);
                _bossLoadCoroutine = null;
            }
        }

        private void BindNodeClickHandlers()
        {
            mapView ??= GetComponentInChildren<RunMapUIView>(true);
            if (mapView == null)
            {
                return;
            }

            mapView.NodeClicked -= HandleNodeClicked;
            mapView.NodeClicked += HandleNodeClicked;
        }

        private void UnbindNodeClickHandlers()
        {
            if (mapView != null)
            {
                mapView.NodeClicked -= HandleNodeClicked;
            }
        }

        private IEnumerator BootRunMap()
        {
            mapView ??= GetComponentInChildren<RunMapUIView>(true);
            if (mapView == null)
            {
                Debug.LogError("[Fractured Chorus] RunMapController: không tìm thấy RunMapUIView trong scene.");
                yield break;
            }

            BindNodeClickHandlers();

            if (!mapView.isActiveAndEnabled)
            {
                mapView.gameObject.SetActive(true);
            }

            var bootstrap = GetComponent<RunMapBootstrap>();
            var seed = bootstrap != null ? bootstrap.ResolveSeed() : 42;
            MapGraph graph;

            try
            {
                var template = bootstrap != null ? bootstrap.Template : null;
                graph = MapGenerator.GenerateFromTemplate(template, seed);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Fractured Chorus] MapGenerator failed: {ex.Message}\n{ex.StackTrace}");
                yield break;
            }

            var procedural = bootstrap == null || bootstrap.Template == null || !bootstrap.Template.useReferenceDemoOnPlay;
            LogEliteDensity(graph);
            Debug.Log(
                $"[Fractured Chorus] Run map generated — seed {seed}, nodes {graph.Nodes.Count}, procedural={procedural}");

            const int maxFrames = 24;
            for (var i = 0; i < maxFrames; i++)
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
                if (mapView.IsViewportReady)
                {
                    break;
                }
            }

            if (!mapView.IsViewportReady)
            {
                Debug.LogWarning("[Fractured Chorus] Viewport chưa layout xong — build map anyway.");
            }

            Initialize(graph, seed);
            SyncLegendPanel();
        }

        public void Initialize(MapGraph graph, int seed)
        {
            mapView ??= GetComponentInChildren<RunMapUIView>(true);
            if (mapView == null)
            {
                Debug.LogError("[Fractured Chorus] RunMapController.Initialize: mapView null.");
                return;
            }

            BindNodeClickHandlers();

            Graph = graph;
            State.BeginRun(seed);
            mapView.BuildMap(graph);
            mapView.RefreshInteraction(graph, State);
            SyncLegendPanel(graph);
            var bossFloor = graph.Profile.BossFloor;
            UpdateLabels($"Select F1 node to start run · Boss F{bossFloor}.");
        }

        private static void SyncLegendPanel(MapGraph graph = null)
        {
            var panel = GameObject.Find("LegendPanel");
            if (panel == null)
            {
                return;
            }

            var legendView = panel.GetComponent<RunMapLegendPanelView>();
            if (legendView == null)
            {
                legendView = panel.AddComponent<RunMapLegendPanelView>();
            }

            legendView.Apply();

            if (graph?.BossNode == null)
            {
                return;
            }

            var bossRow = panel.transform.Find("Legend_Boss");
            var bossDesc = bossRow?.Find("Desc")?.GetComponent<Text>();
            if (bossDesc != null)
            {
                bossDesc.text = $"Boss — F{graph.Profile.BossFloor}";
            }
        }

        private void HandleNodeClicked(MapNodeView view)
        {
            if (Graph == null || view?.BoundNode == null)
            {
                return;
            }

            var node = view.BoundNode;
            if (!State.CanSelectNode(Graph, node))
            {
                if (node.IsBoss && State.CurrentFloor > 0 && State.CurrentFloor < Graph.Profile.FloorCount)
                {
                    UpdateLabels($"Select Camp F{Graph.Profile.FloorCount} before entering boss.");
                }
                else if (node.Floor > State.CurrentFloor + 1)
                {
                    UpdateLabels("Node too far — select next floor only.");
                }
                else
                {
                    UpdateLabels("Node not reachable — follow an adjacent path.");
                }

                return;
            }

            var isBoss = node.IsBoss || node.Type == MapNodeType.Boss;
            var reopenBoss = isBoss && State.CurrentNodeId == node.Id;

            if (!reopenBoss)
            {
                State.EnterNode(node);
                mapView.RefreshInteraction(Graph, State);
                mapView.ScrollToNode(node);
            }

            if (isBoss)
            {
                node.Cleared = true;
                mapView.RefreshInteraction(Graph, State);

                var cadence = GetComponentInParent<CadenceMapController>();
                if (cadence != null && Graph?.Profile != null)
                {
                    var sector = Graph.Profile.Sector;
                    var bossLabel = cadence.Progress.SectorBossLabel(sector);
                    if (cadence.SectorLoadsBossSceneForUi(sector))
                    {
                        Debug.Log("[Fractured Chorus] Final boss node — loading combat scene.");
                        UpdateLabels($"Entering battle: {bossLabel}…");
                    }
                    else
                    {
                        Debug.Log("[Fractured Chorus] Sector boss stub — advance to next inner map.");
                        var next = cadence.Progress.NextSector(sector);
                        UpdateLabels(next.HasValue
                            ? $"{bossLabel} cleared — switching to map {SectorMapIndex(next.Value)}/3…"
                            : $"{bossLabel} cleared.");
                    }

                    cadence.OnInnerBossEngaged(sector);
                    return;
                }

                Debug.Log("[Fractured Chorus] Boss node selected — loading combat scene.");
                UpdateLabels("Entering battle: Oni F16…");
                BeginBossCombatTransition();
                return;
            }

            UpdateLabels($"Entered {MapNodePalette.DisplayName(node.Type)} (F{node.Floor}). Select next node.");
        }

        private static int SectorMapIndex(PinkySectorId sector) => sector switch
        {
            PinkySectorId.Pulse => 1,
            PinkySectorId.Echo => 2,
            PinkySectorId.Canticle => 3,
            _ => 0
        };

        private void BeginBossCombatTransition()
        {
            if (_loadingBossScene)
            {
                return;
            }

            _loadingBossScene = true;

            if (_bossLoadCoroutine != null)
            {
                StopCoroutine(_bossLoadCoroutine);
            }

            _bossLoadCoroutine = StartCoroutine(LoadBossCombatSceneDeferred());
        }

        private IEnumerator LoadBossCombatSceneDeferred()
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
                UpdateLabels("Failed to load CombatPrototype — check Build Settings.");
            }
        }

        private static void LogEliteDensity(MapGraph graph)
        {
            if (graph == null)
            {
                return;
            }

            var total = 0;
            var elites = 0;
            foreach (var node in graph.Nodes)
            {
                if (node.IsBoss)
                {
                    continue;
                }

                total++;
                if (node.Type == MapNodeType.Elite)
                {
                    elites++;
                }
            }

            if (total == 0)
            {
                return;
            }

            var pct = 100f * elites / total;
            Debug.Log($"[Fractured Chorus] Map elite density — {elites}/{total} ({pct:F0}%), target 25–35%.");
        }

        private void UpdateLabels(string status)
        {
            if (statusLabel != null)
            {
                statusLabel.text = status;
            }

            if (seedLabel != null && Graph != null)
            {
                seedLabel.text = $"Seed {Graph.Seed}";
            }
        }

        public void WireView(RunMapUIView view, Text status, Text seed)
        {
            UnbindNodeClickHandlers();
            mapView = view;
            statusLabel = status;
            seedLabel = seed;
            BindNodeClickHandlers();
        }
    }
}

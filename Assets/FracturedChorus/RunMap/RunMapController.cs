using System.Collections;
using FracturedChorus.Audio;
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Data;
using FracturedChorus.Meta;
using FracturedChorus.Meta.Economy;
using FracturedChorus.RunMap.Core;
using FracturedChorus.RunMap.UI;
using FracturedChorus.Tutorial;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FracturedChorus.RunMap
{
    public class RunMapController : MonoBehaviour
    {
        [SerializeField] private RunMapUIView mapView;
        [SerializeField] private RunMapNodeInfoPanel nodeInfoPanel;
        [SerializeField] private TreasureRoomOverlayUIView treasureOverlay;
        [SerializeField] private TreasureRewardTableSO treasureRewards;
        [SerializeField] private EventRoomOverlayUIView eventOverlay;
        [SerializeField] private EventChoiceTableSO eventChoices;
        [SerializeField] private CampRoomOverlayUIView campOverlay;
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
        private int _previewNodeId = -1;

        private void Awake()
        {
            mapView ??= GetComponentInChildren<RunMapUIView>(true);
            BindNodeClickHandlers();

            if (nodeInfoPanel == null && mapView != null)
            {
                nodeInfoPanel = EnsureNodeInfoPanel();
            }
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
            ApplyCombatReturnHandoff();
            SyncLegendPanel();
            TutorialDirector.Ensure().StartMapTrack();
            RefreshNotesHud();
        }

        public void ApplyCombatReturnHandoff()
        {
            if (!CombatEncounterHandoff.HasResult || Graph == null)
            {
                return;
            }

            if (CombatEncounterHandoff.LastVictory)
            {
                TryClearSourceNodeAfterVictory();
                CombatEncounterHandoff.ClearResultFlags();
                RunMusicSession.Instance?.SetMode(RunMusicMode.Map);
                return;
            }

            if (!CombatEncounterHandoff.PendingReturnToNearestCamp)
            {
                CombatEncounterHandoff.ClearResultFlags();
                return;
            }

            PartyRunHpStore.RestoreFullAtCamp();

            var camp = FindNearestCampNode();
            if (camp != null)
            {
                State.EnterNode(camp);
                mapView?.RefreshInteraction(Graph, State);
                mapView?.ScrollToNode(camp);
                UpdateLabels($"Returned to Camp (F{camp.Floor}) — HP restored. Set up before continuing.");
            }
            else
            {
                UpdateLabels("Defeat — no Camp found. HP restored. Select a node to continue.");
            }

            CombatEncounterHandoff.ClearResultFlags();
        }

        private void TryClearSourceNodeAfterVictory()
        {
            if (Graph == null || CombatEncounterHandoff.SourceNodeId < 0)
            {
                return;
            }

            var node = Graph.GetNode(CombatEncounterHandoff.SourceNodeId);
            MarkNodeCleared(node);
            if (node == null)
            {
                return;
            }

            State.EnterNode(node);
            mapView?.RefreshInteraction(Graph, State);
            mapView?.ScrollToNode(node, immediate: true);
            RunMapRunSave.Persist(Graph, State);
        }

        private void MarkNodeCleared(MapNodeData node)
        {
            if (node == null || node.Cleared || node.Type == MapNodeType.Start)
            {
                return;
            }

            node.Cleared = true;
            mapView?.RefreshInteraction(Graph, State);
            RunMapRunSave.Persist(Graph, State);
        }

        private MapNodeData FindNearestCampNode()
        {
            MapNodeData best = null;
            var bestFloor = int.MinValue;
            var bossFloor = Graph.BossNode != null ? Graph.BossNode.Floor : Graph.Profile.BossFloor;
            foreach (var node in Graph.Nodes)
            {
                if (node == null || node.Type != MapNodeType.Camp || node.IsBoss)
                {
                    continue;
                }

                if (node.Floor > bossFloor)
                {
                    continue;
                }

                if (node.Floor >= bestFloor)
                {
                    bestFloor = node.Floor;
                    best = node;
                }
            }

            return best;
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
            TreasureClaimStore.ClearRun();
            EventClaimStore.ClearRun();
            RunEventCombatMods.ClearRun();
            _previewNodeId = -1;
            mapView.SetMarkerPreviewNodeId(-1);
            mapView.BuildMap(graph);

            if (!RunMapRunSave.TryRestore(graph, State) && graph.StartNode != null)
            {
                State.EnterNode(graph.StartNode);
                RunMapRunSave.Persist(graph, State);
            }

            mapView.RefreshInteraction(graph, State);
            SyncLegendPanel(graph);
            EnsureNodeInfoPanel()?.Hide();
            if (State.CurrentNodeId >= 0)
            {
                var current = graph.GetNode(State.CurrentNodeId);
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

            if (graph.StartNode != null && State.CurrentNodeId == graph.StartNode.Id)
            {
                UpdateLabels("Departure — chọn node F1. ★ Lưu tại điểm này, Camp, hoặc sau boss.");
            }
            else if (State.CurrentNodeId >= 0)
            {
                UpdateLabels($"Run restored — F{State.CurrentFloor}. Chọn node kế tiếp.");
            }
            else
            {
                var bossFloor = graph.Profile.BossFloor;
                UpdateLabels($"Select F1 node to start run · Boss F{bossFloor}.");
            }
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

        private void Update()
        {
            if (!Application.isPlaying || Graph == null)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.xKey.wasPressedThisFrame)
            {
                CancelNodePreview();
            }
        }

        private void HandleNodeClicked(MapNodeView view)
        {
            if (Graph == null || view?.BoundNode == null)
            {
                return;
            }

            var node = view.BoundNode;
            var canTravel = State.CanSelectNode(Graph, node);
            var panel = EnsureNodeInfoPanel();
            if (panel == null)
            {
                return;
            }

            panel.Show(node, canTravel, ConfirmTravelToNode, CancelNodePreview);

            if (!canTravel)
            {
                mapView.SetSelectedNode(node.Id);
                if (node.IsBoss && State.CurrentFloor > 0 && State.CurrentFloor < Graph.Profile.FloorCount)
                {
                    UpdateLabels($"Select Camp F{Graph.Profile.FloorCount} before entering boss.");
                }
                else if (node.Floor > State.CurrentFloor + 1 && !node.IsStart)
                {
                    UpdateLabels("Node too far — select next floor only.");
                }
                else if (node.Cleared)
                {
                    UpdateLabels("Node cleared.");
                }
                else if (State.CurrentNodeId == node.Id)
                {
                    UpdateLabels($"Standing at {MapNodeCatalog.Title(node.Type)} — chọn node kế.");
                }
                else
                {
                    UpdateLabels("Node not reachable — follow an adjacent path.");
                }

                return;
            }

            PreviewHopTo(node);
        }

        private void PreviewHopTo(MapNodeData node)
        {
            var fromId = _previewNodeId >= 0 ? _previewNodeId : State.CurrentNodeId;
            var from = Graph.GetNode(fromId) ?? Graph.StartNode;
            _previewNodeId = node.Id;
            mapView.SetMarkerPreviewNodeId(node.Id);
            if (from != null && from.Id != node.Id)
            {
                mapView.AnimateTravelToNode(from, node, null);
            }

            mapView.SetSelectedNode(node.Id);
        }

        private void CancelNodePreview()
        {
            var previewId = _previewNodeId;
            _previewNodeId = -1;
            mapView?.SetMarkerPreviewNodeId(-1);
            EnsureNodeInfoPanel()?.Hide();

            if (Graph == null || mapView == null)
            {
                return;
            }

            var from = Graph.GetNode(previewId);
            var home = Graph.GetNode(State.CurrentNodeId) ?? Graph.StartNode;
            if (from != null && home != null && from.Id != home.Id)
            {
                mapView.AnimateTravelToNode(from, home, null);
            }

            mapView.SetSelectedNode(-1);
        }

        private void ConfirmTravelToNode(MapNodeData node)
        {
            if (Graph == null || node == null || !State.CanSelectNode(Graph, node))
            {
                return;
            }

            _previewNodeId = -1;
            mapView.SetMarkerPreviewNodeId(-1);

            var isBoss = node.IsBoss || node.Type == MapNodeType.Boss;
            var reopenBoss = isBoss && State.CurrentNodeId == node.Id;
            CompleteTravelToNode(node, reopenBoss);
        }

        private void CompleteTravelToNode(MapNodeData node, bool reopenBoss)
        {
            var isBoss = node.IsBoss || node.Type == MapNodeType.Boss;

            if (!reopenBoss)
            {
                State.EnterNode(node);
                mapView.RefreshInteraction(Graph, State);
                mapView.ScrollToNode(node);
            }

            if (node.Type == MapNodeType.Start)
            {
                RunMapRunSave.Persist(Graph, State);
                UpdateLabels("Departure — ★ đã lưu. Chọn node F1.");
                return;
            }

            if (isBoss)
            {
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
                    MarkNodeCleared(node);
                    return;
                }

                BeginCombatForNode(node, EncounterCatalog.BossDespair, "Entering boss battle…");
                return;
            }

            if (node.Type == MapNodeType.Battle)
            {
                var roll = CombatPoolService.RollBattle(Graph?.Seed ?? 42, node.Id);
                BeginCombatForNode(
                    node,
                    EncounterCatalog.BattleGrunts,
                    "Entering battle…",
                    roll);
                return;
            }

            if (node.Type == MapNodeType.Elite)
            {
                var roll = CombatPoolService.RollElite(Graph?.Seed ?? 42, node.Id);
                BeginCombatForNode(
                    node,
                    EncounterCatalog.EliteGrunts,
                    "Entering elite battle…",
                    roll);
                return;
            }

            if (node.Type == MapNodeType.Camp)
            {
                RunMapRunSave.Persist(Graph, State);
                ResolveCampNode(node);
                return;
            }

            if (node.Type == MapNodeType.Treasure)
            {
                ResolveTreasureNode(node);
                return;
            }

            if (node.Type == MapNodeType.Relay)
            {
                ResolveRelayNode(node);
                return;
            }

            if (node.Type == MapNodeType.Event)
            {
                ResolveEventNode(node);
                return;
            }

            MarkNodeCleared(node);
            UpdateLabels($"Entered {MapNodePalette.DisplayName(node.Type)} (F{node.Floor}). Select next node.");
        }

        private RunMapNodeInfoPanel EnsureNodeInfoPanel()
        {
            if (nodeInfoPanel != null)
            {
                var scrollRect = mapView != null ? mapView.GetComponentInParent<ScrollRect>() : null;
                if (scrollRect?.viewport != null && nodeInfoPanel.transform.parent != scrollRect.viewport)
                {
                    nodeInfoPanel.transform.SetParent(scrollRect.viewport, false);
                }

                return nodeInfoPanel;
            }

            var scroll = mapView != null ? mapView.GetComponentInParent<ScrollRect>() : null;
            var parent = scroll?.viewport != null
                ? scroll.viewport
                : mapView != null ? mapView.GetComponentInParent<Canvas>()?.transform : null;

            if (mapView != null)
            {
                var staleOnContent = mapView.GetComponentInChildren<RunMapNodeInfoPanel>(true);
                if (staleOnContent != null && staleOnContent.transform.IsChildOf(mapView.transform))
                {
                    Object.Destroy(staleOnContent.gameObject);
                }
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            var staleOnCanvas = canvas != null ? canvas.transform.Find("NodeInfoSidebar") : null;
            if (staleOnCanvas != null && parent != null && staleOnCanvas.parent != parent)
            {
                Object.Destroy(staleOnCanvas.gameObject);
            }

            if (parent != null)
            {
                nodeInfoPanel = RunMapNodeInfoPanelBuilder.EnsureSidebar(parent);
            }

            return nodeInfoPanel;
        }

        private void ResolveCampNode(MapNodeData node)
        {
            var overlay = EnsureCampOverlay();
            if (overlay == null)
            {
                MarkNodeCleared(node);
                UpdateLabels($"Camp F{node.Floor} — overlay missing.");
                return;
            }

            EnsureNodeInfoPanel()?.Hide();
            overlay.Show(CampChoiceCatalog.CreateOffers(), choice => OnCampPicked(node, choice));
            UpdateLabels($"Camp F{node.Floor} — chọn hành động.");
        }

        private void OnCampPicked(MapNodeData node, CampChoiceOffer choice)
        {
            if (!choice.Available)
            {
                return;
            }

            switch (choice.Kind)
            {
                case CampChoiceKind.Heal50:
                    PartyRunHpStore.HealLivingPercent(CampChoiceCatalog.HealPercent);
                    break;
                case CampChoiceKind.ReviveOne:
                    PartyRunHpStore.ReviveOne(CampChoiceCatalog.ReviveHp);
                    break;
            }

            MarkNodeCleared(node);
            RunMapRunSave.Persist(Graph, State);
            if (GameMetaSession.HasSession)
            {
                GameMetaSession.Save();
            }

            EnsureCampOverlay()?.Hide();
            var floor = node != null ? node.Floor : 0;
            UpdateLabels($"Camp F{floor} — {choice.Title}. ★ Saved.");
        }

        private CampRoomOverlayUIView EnsureCampOverlay()
        {
            if (campOverlay != null)
            {
                return campOverlay;
            }

            var canvasGo = GameObject.Find("RunMapCanvas");
            var canvas = canvasGo != null
                ? canvasGo.GetComponent<Canvas>()
                : Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                return null;
            }

            campOverlay = CampRoomOverlayUIView.EnsureOnCanvas(canvas.transform);
            return campOverlay;
        }

        private void ResolveTreasureNode(MapNodeData node)
        {
            var overlay = EnsureTreasureOverlay();
            if (overlay == null)
            {
                MarkNodeCleared(node);
                UpdateLabels($"Treasure F{node.Floor} — overlay missing.");
                return;
            }

            EnsureNodeInfoPanel()?.Hide();
            var table = treasureRewards != null ? treasureRewards : TreasureRewardTableSO.LoadOrCreateDefault();
            var seed = (Graph != null ? Graph.Seed : 0) ^ (node.Id * 397) ^ node.Floor;
            overlay.Show(table.PickOffers(seed), reward => OnTreasurePicked(node, reward));
            UpdateLabels($"Treasure F{node.Floor} — chọn phần thưởng.");
        }

        private void OnTreasurePicked(MapNodeData node, TreasureRewardSO reward)
        {
            TreasureClaimStore.Record(reward, node != null ? node.Id : -1, node != null ? node.Floor : 0);
            MarkNodeCleared(node);
            EnsureTreasureOverlay()?.Hide();
            var title = reward != null ? reward.Title : "reward";
            var floor = node != null ? node.Floor : 0;
            UpdateLabels($"Treasure F{floor} — {title} (pending apply).");
        }

        private TreasureRoomOverlayUIView EnsureTreasureOverlay()
        {
            if (treasureOverlay != null)
            {
                return treasureOverlay;
            }

            var canvasGo = GameObject.Find("RunMapCanvas");
            var canvas = canvasGo != null
                ? canvasGo.GetComponent<Canvas>()
                : Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                return null;
            }

            treasureOverlay = TreasureRoomOverlayUIView.EnsureOnCanvas(canvas.transform);
            if (treasureOverlay != null && treasureRewards == null)
            {
                treasureRewards = TreasureRewardTableSO.LoadOrCreateDefault();
                treasureOverlay.SetRewardTable(treasureRewards);
            }

            return treasureOverlay;
        }

        private void ResolveEventNode(MapNodeData node)
        {
            var overlay = EnsureEventOverlay();
            if (overlay == null)
            {
                MarkNodeCleared(node);
                UpdateLabels($"Event F{node.Floor} — overlay missing.");
                return;
            }

            EnsureNodeInfoPanel()?.Hide();
            var table = eventChoices != null ? eventChoices : EventChoiceTableSO.LoadOrCreateDefault();
            var seed = (Graph != null ? Graph.Seed : 0) ^ (node.Id * 911) ^ (node.Floor * 17);
            overlay.Show(table.PickOffers(seed), choice => OnEventPicked(node, choice));
            UpdateLabels($"Event F{node.Floor} — chọn sự kiện.");
        }

        private void OnEventPicked(MapNodeData node, EventChoiceSO choice)
        {
            EventClaimStore.Record(choice, node != null ? node.Id : -1, node != null ? node.Floor : 0);
            if (choice != null && choice.Kind == EventChoiceKind.Notes)
            {
                if (GameMetaSession.HasSession)
                {
                    GameMetaSession.Current.Wallet.Add(Mathf.RoundToInt(choice.Magnitude));
                    GameMetaSession.Save();
                    RefreshNotesHud();
                }
            }
            else
            {
                RunEventCombatMods.ApplyChoice(choice);
            }

            MarkNodeCleared(node);
            EnsureEventOverlay()?.Hide();
            var title = choice != null ? choice.Title : "event";
            var floor = node != null ? node.Floor : 0;
            UpdateLabels($"Event F{floor} — {title}.");
        }

        private EventRoomOverlayUIView EnsureEventOverlay()
        {
            if (eventOverlay != null)
            {
                return eventOverlay;
            }

            var canvasGo = GameObject.Find("RunMapCanvas");
            var canvas = canvasGo != null
                ? canvasGo.GetComponent<Canvas>()
                : Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                return null;
            }

            eventOverlay = EventRoomOverlayUIView.EnsureOnCanvas(canvas.transform);
            if (eventOverlay != null && eventChoices == null)
            {
                eventChoices = EventChoiceTableSO.LoadOrCreateDefault();
                eventOverlay.SetChoiceTable(eventChoices);
            }

            return eventOverlay;
        }

        private void ResolveRelayNode(MapNodeData node)
        {
            if (!GameMetaSession.HasSession)
            {
                MarkNodeCleared(node);
                UpdateLabels($"Relay F{node.Floor} — shop stub.");
                return;
            }

            var wallet = GameMetaSession.Current.Wallet;
            if (!wallet.CanAfford(EconomyTable.RelayCost))
            {
                UpdateLabels($"Relay shop — need {EconomyTable.RelayCost} Notes for field kit.");
                return;
            }

            if (!wallet.Spend(EconomyTable.RelayCost))
            {
                return;
            }

            PartyRunHpStore.RestoreFullAtCamp();
            MarkNodeCleared(node);
            GameMetaSession.Save();
            RefreshNotesHud();
            UpdateLabels($"Relay F{node.Floor} — bought field kit (−{EconomyTable.RelayCost} Notes). HP restored.");
        }

        private static void RefreshNotesHud()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                NotesHudView.Ensure(canvas.transform)?.Refresh();
            }
        }

        private void BeginCombatForNode(MapNodeData node, string encounterId, string status, CombatPoolRoll roll = null)
        {
            if (string.IsNullOrWhiteSpace(encounterId))
            {
                UpdateLabels("No encounter mapped for this node.");
                return;
            }

            CombatEncounterHandoff.SetPending(
                encounterId,
                RunMapSceneCatalog.RunMapPrototype,
                node != null ? node.Id : -1,
                roll);
            RunMapRunSave.Persist(Graph, State);
            UpdateLabels(status);
            BeginBossCombatTransition();
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

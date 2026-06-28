using FracturedChorus.Data;
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

        public MapGraph Graph { get; private set; }
        public RunState State { get; private set; } = new RunState();

        private void OnEnable()
        {
            if (mapView != null)
            {
                mapView.NodeClicked += HandleNodeClicked;
            }
        }

        private void OnDisable()
        {
            if (mapView != null)
            {
                mapView.NodeClicked -= HandleNodeClicked;
            }
        }

        public void Initialize(MapGraph graph, int seed)
        {
            Graph = graph;
            State.BeginRun(seed);

            if (mapView != null)
            {
                mapView.BuildMap(graph);
                mapView.RefreshInteraction(graph, State);
            }

            UpdateLabels("Chọn node F1 để bắt đầu run.");
        }

        private void HandleNodeClicked(MapNodeView view)
        {
            if (Graph == null || view?.BoundNode == null)
            {
                return;
            }

            var node = view.BoundNode;
            if (!State.CanTravelTo(Graph, node))
            {
                UpdateLabels("Node không reachable — phải đi theo path liền kề.");
                return;
            }

            State.EnterNode(node);
            mapView.RefreshInteraction(Graph, State);
            mapView.ScrollToNode(node);

            var floorText = node.IsBoss ? "F16 Boss" : $"F{node.Floor}";
            UpdateLabels($"Đã vào {MapNodePalette.DisplayName(node.Type)} ({floorText}). Chọn node kế tiếp.");
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
            mapView = view;
            statusLabel = status;
            seedLabel = seed;
        }
    }
}

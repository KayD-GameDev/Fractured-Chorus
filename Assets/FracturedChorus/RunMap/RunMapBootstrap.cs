using FracturedChorus.Data;
using FracturedChorus.RunMap.Core;
using FracturedChorus.RunMap.UI;
using UnityEngine;

namespace FracturedChorus.RunMap
{
    public class RunMapBootstrap : MonoBehaviour
    {
        [SerializeField] private MapTemplateSO template;
        [SerializeField] private RunMapController controller;
        [SerializeField] private RunMapUIView mapView;
        [SerializeField] private int overrideSeed;
        [SerializeField] private bool useOverrideSeed;
        [SerializeField] private bool randomizeSeedOnPlay = true;
        [SerializeField] private bool respectSceneAuthoring = true;

        private void Start()
        {
            if (controller == null)
            {
                controller = GetComponent<RunMapController>();
            }

            if (mapView == null)
            {
                mapView = FindAnyObjectByType<RunMapUIView>();
            }

            if (mapView != null)
            {
                mapView.ApplyAuthoringPolicy(respectSceneAuthoring);
            }

            var seed = ResolveSeed();
            MapGraph graph;

            // Procedural map unless explicitly forced to demo reference in Inspector.
            if (template != null && template.useReferenceDemoOnPlay)
            {
                graph = MapGenerator.GenerateDemoReference(seed);
            }
            else
            {
                var pathCount = template != null ? template.pathCount : MapLayoutConstants.DefaultPathCount;
                graph = MapGenerator.Generate(seed, pathCount);
            }

            Debug.Log($"[Fractured Chorus] Run map generated — seed {seed}, nodes {graph.Nodes.Count}, procedural={template == null || !template.useReferenceDemoOnPlay}");

            if (controller != null)
            {
                controller.Initialize(graph, seed);
            }
        }

        private int ResolveSeed()
        {
            if (useOverrideSeed)
            {
                return overrideSeed;
            }

            if (template != null && !template.randomizeSeedOnPlay)
            {
                return template.defaultSeed;
            }

            if (randomizeSeedOnPlay || (template != null && template.randomizeSeedOnPlay))
            {
                return Random.Range(1, int.MaxValue);
            }

            return template != null ? template.defaultSeed : 42;
        }
    }
}

using FracturedChorus.Data;
using FracturedChorus.RunMap.Core;
using FracturedChorus.RunMap.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Tests
{
    public class MapNodeTemplateSetTests
    {
        private const string SetPath = MapNodeTemplateSetSO.DefaultAssetPath;
        private const string NodePrefabPath = "Assets/FracturedChorus/RunMap/Prefabs/MapNode.prefab";
        private const string ConnectionPrefabPath = "Assets/FracturedChorus/RunMap/Prefabs/MapConnection.prefab";

        [Test]
        public void DefaultAsset_ResolvesPrefabForEveryNodeType()
        {
            var set = AssetDatabase.LoadAssetAtPath<MapNodeTemplateSetSO>(SetPath);
            Assert.IsNotNull(set);
            Assert.IsNotNull(set.DefaultNodePrefab);
            Assert.IsNotNull(set.ConnectionPrefab);
            Assert.IsNotNull(set.IconSet);

            foreach (MapNodeType type in System.Enum.GetValues(typeof(MapNodeType)))
            {
                Assert.IsNotNull(set.ResolveNodePrefab(type), type.ToString());
            }
        }

        [Test]
        public void NodePrefab_HasRequiredHierarchy()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NodePrefabPath);
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<MapNodeView>());
            Assert.IsNotNull(prefab.GetComponent<Button>());
            Assert.IsNotNull(prefab.GetComponent<MapNodeScrollForwarder>());
            Assert.IsNotNull(prefab.transform.Find("Icon"));
            Assert.IsNotNull(prefab.transform.Find("Fill"));
            Assert.IsNotNull(prefab.transform.Find("Stroke"));
            Assert.IsNotNull(prefab.transform.Find("Label"));
        }

        [Test]
        public void ConnectionPrefab_HasLineView()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConnectionPrefabPath);
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<MapConnectionLineView>());
            Assert.IsNotNull(prefab.GetComponent<Image>());
        }

        [Test]
        public void DefaultTemplate_GeneratesFullGridPathsAndContentSize()
        {
            const string templatePath = "Assets/FracturedChorus/Data/ScriptableObjects/Presets/MapTemplate_Default.asset";
            var template = AssetDatabase.LoadAssetAtPath<MapTemplateSO>(templatePath);
            Assert.IsNotNull(template);

            var profile = new MapGenerationProfile
            {
                ColumnCount = template.columnCount,
                FloorCount = template.floorCount,
                BossFloor = template.bossFloor,
                PathCount = template.pathCount
            };
            var graph = MapGenerator.Generate(
                template.defaultSeed,
                profile,
                NodeTypeAssigner.WeightsFromTemplate(template),
                template.pathCount);

            Assert.IsNotNull(graph.StartNode);
            Assert.IsNotNull(graph.BossNode);
            Assert.Greater(graph.Nodes.Count, template.floorCount);

            var edgeCount = 0;
            foreach (var node in graph.Nodes)
            {
                edgeCount += node.Outgoing.Count;
            }

            Assert.Greater(edgeCount, template.pathCount);

            var metrics = new RunMapLayoutMetrics();
            metrics.SetProfile(graph.Profile);
            metrics.ResetToDefaults();
            metrics.ComputeContentSize(out var width, out var height);
            Assert.Greater(width, (template.columnCount - 1) * MapLayoutConstants.NodeSpacingX);
            Assert.Greater(height, template.floorCount * MapLayoutConstants.NodeSpacingY);
        }
    }
}

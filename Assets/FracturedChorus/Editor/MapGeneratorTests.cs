using System;
using System.Collections.Generic;
using System.Linq;
using FracturedChorus.RunMap.Core;
using NUnit.Framework;

namespace FracturedChorus.Tests
{
    public class MapGeneratorTests
    {
        [Test]
        public void StartingLines_SpreadAcrossRandomColumns()
        {
            var layouts = new HashSet<string>();
            var sawNonCenterCluster = false;

            for (var seed = 1; seed <= 48; seed++)
            {
                var graph = MapGenerator.Generate(seed, MapLayoutConstants.DefaultPathCount);
                var columns = new List<int>();
                foreach (var node in graph.StartNodes())
                {
                    columns.Add(node.Column);
                }

                columns.Sort();
                Assert.GreaterOrEqual(columns.Count, MapLayoutConstants.MinStartNodes);
                Assert.LessOrEqual(columns.Count, MapLayoutConstants.MaxStartNodes);
                Assert.AreEqual(columns.Count, UniqueCount(columns));

                layouts.Add(string.Join(",", columns));
                if (columns.Count != 3 || columns[0] != 2 || columns[1] != 3 || columns[2] != 4)
                {
                    sawNonCenterCluster = true;
                }
            }

            Assert.Greater(layouts.Count, 1);
            Assert.IsTrue(sawNonCenterCluster);
        }

        [Test]
        public void PrefixFloors_DoNotBranch_UntilFourthNode()
        {
            var sawBranchFromFourth = false;

            for (var seed = 1; seed <= 32; seed++)
            {
                var graph = MapGenerator.Generate(seed, MapLayoutConstants.DefaultPathCount);
                Assert.GreaterOrEqual(graph.StartNodes().Count(), 1);

                foreach (var node in graph.Nodes)
                {
                    if (node.IsBoss || node.Type == MapNodeType.Start)
                    {
                        continue;
                    }

                    if (node.Floor <= MapLayoutConstants.ExclusivePrefixFloors)
                    {
                        Assert.LessOrEqual(node.Outgoing.Count, 1, $"F{node.Floor} C{node.Column} seed {seed}");
                    }
                    else if (node.Outgoing.Count >= 2)
                    {
                        sawBranchFromFourth = true;
                    }
                }
            }

            Assert.IsTrue(sawBranchFromFourth);
        }

        [Test]
        public void PreBossFloor_HasCampPerStartingLine()
        {
            for (var seed = 1; seed <= 24; seed++)
            {
                var graph = MapGenerator.Generate(seed, MapLayoutConstants.DefaultPathCount);
                var starts = 0;
                foreach (var _ in graph.StartNodes())
                {
                    starts++;
                }

                var camps = 0;
                foreach (var node in graph.NodesOnFloor(graph.Profile.FloorCount))
                {
                    Assert.AreEqual(MapNodeType.Camp, node.Type, $"seed {seed} F{node.Floor} C{node.Column}");
                    camps++;
                }

                Assert.AreEqual(starts, camps, $"seed {seed}");
                Assert.AreEqual(MapLayoutConstants.DefaultPathCount, camps, $"seed {seed}");
            }
        }

        [Test]
        public void GeneratedMap_SpreadsAcrossColumnsInsteadOfOneLine()
        {
            var sawWideFloor = false;
            var columnSets = new HashSet<string>();

            for (var seed = 1; seed <= 32; seed++)
            {
                var graph = MapGenerator.Generate(seed, MapLayoutConstants.DefaultPathCount);
                var usedColumns = new HashSet<int>();
                var maxFloorSpan = 0;

                foreach (var node in graph.Nodes)
                {
                    if (node.IsBoss || node.Type == MapNodeType.Start)
                    {
                        continue;
                    }

                    usedColumns.Add(node.Column);
                }

                Assert.Greater(usedColumns.Count, 3);

                for (var floor = 1; floor <= graph.Profile.FloorCount; floor++)
                {
                    var min = int.MaxValue;
                    var max = int.MinValue;
                    var count = 0;
                    foreach (var node in graph.NodesOnFloor(floor))
                    {
                        count++;
                        min = Math.Min(min, node.Column);
                        max = Math.Max(max, node.Column);
                    }

                    if (count >= 2)
                    {
                        maxFloorSpan = Math.Max(maxFloorSpan, max - min);
                    }
                }

                if (maxFloorSpan >= 2)
                {
                    sawWideFloor = true;
                }

                var ordered = new List<int>(usedColumns);
                ordered.Sort();
                columnSets.Add(string.Join(",", ordered));
            }

            Assert.IsTrue(sawWideFloor);
            Assert.Greater(columnSets.Count, 1);
        }

        private static int UniqueCount(List<int> values)
        {
            var seen = new HashSet<int>();
            for (var i = 0; i < values.Count; i++)
            {
                seen.Add(values[i]);
            }

            return seen.Count;
        }
    }
}

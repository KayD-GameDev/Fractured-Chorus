using System.Collections.Generic;
using System.Linq;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.Combat.AI
{
    public class SimpleEnemyAI
    {
        /// <summary>
        /// Mỗi phase: chọn ngẫu nhiên N ô trong phase (16 ô/phase),
        /// N = số quái còn sống — mỗi quái một telegraph đỏ.
        /// </summary>
        public void PlanTelegraphsForPhase(int phaseIndex, DualGrid grid, BeatTimelineEngine timeline)
        {
            if (grid == null || timeline == null)
            {
                return;
            }

            TimelineConstants.GetPhaseBeatRange(phaseIndex, out var startBeat, out var slotCount);

            var enemies = grid.EnemyUnits.Where(u => u.IsAlive).ToList();
            timeline.ClearTelegraphsInRange(startBeat, slotCount);

            if (enemies.Count == 0)
            {
                return;
            }

            var beatPicks = PickRandomUniqueBeats(startBeat, slotCount, enemies.Count);

            for (var i = 0; i < enemies.Count && i < beatPicks.Count; i++)
            {
                var enemy = enemies[i];
                var skill = enemy.Skills.FirstOrDefault(s => s != null && s.IsAttack);
                if (skill == null)
                {
                    continue;
                }

                timeline.AddTelegraph(enemy, skill, beatPicks[i]);
            }

            Debug.Log(
                $"[EnemyAI] Phase {phaseIndex + 1}: {enemies.Count} telegraph @ beats [{string.Join(", ", beatPicks)}]");
        }

        private static List<int> PickRandomUniqueBeats(int startBeat, int slotCount, int pickCount)
        {
            var pool = new List<int>(slotCount);
            for (var i = 0; i < slotCount; i++)
            {
                pool.Add(startBeat + i);
            }

            pickCount = Mathf.Min(pickCount, pool.Count);
            var picks = new List<int>(pickCount);

            for (var i = 0; i < pickCount; i++)
            {
                var index = Random.Range(i, pool.Count);
                (pool[i], pool[index]) = (pool[index], pool[i]);
                picks.Add(pool[i]);
            }

            picks.Sort();
            return picks;
        }
    }
}

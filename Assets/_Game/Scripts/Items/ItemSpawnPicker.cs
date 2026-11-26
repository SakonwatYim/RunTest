using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>Parameters for <see cref="ItemSpawnPicker"/> (GDD 5 "ตำแหน่งเกิด Item").</summary>
    public struct ItemSpawnRules
    {
        public float MinPlayerDistance;
        public float MaxPlayerDistance;
        public float MaxAbovePlayer;
        public float WallMargin;
        public float ObstacleMargin;
        /// <summary>Tried in order: 520, then 380, then 260.</summary>
        public float[] EnemyDistances;
        public int AttemptsPerDistance;

        public static ItemSpawnRules FromConfig(GameConfig c)
        {
            var distances = new float[c.ItemMinEnemyDistances.Length];
            for (var i = 0; i < distances.Length; i++) distances[i] = c.ToWorld(c.ItemMinEnemyDistances[i]);
            return new ItemSpawnRules
            {
                MinPlayerDistance = c.ToWorld(c.ItemMinPlayerDistance),
                MaxPlayerDistance = c.ToWorld(c.ItemMaxPlayerDistance),
                MaxAbovePlayer = c.ToWorld(c.ItemMaxAbovePlayer),
                WallMargin = c.ToWorld(c.ItemMinObstacleDistance),
                ObstacleMargin = c.ToWorld(c.ItemMinObstacleDistance),
                EnemyDistances = distances,
                AttemptsPerDistance = c.ItemSpawnAttempts,
            };
        }
    }

    /// <summary>
    /// Picks an item spawn point: 380-700 from the player, never more than 380 above them,
    /// away from walls/obstacles, and at least 520 (then 380, then 260) from every enemy.
    /// Returns false when nothing fits so the caller retries ~1.5 s later.
    /// </summary>
    public static class ItemSpawnPicker
    {
        public static bool TryPick(
            Vector2 player,
            IReadOnlyList<Vector2> enemies,
            ArenaGeometry arena,
            IReadOnlyList<ObstacleCircle> obstacles,
            in ItemSpawnRules rules,
            Func<float> random01,
            out Vector2 result,
            out int distanceStep)
        {
            for (distanceStep = 0; distanceStep < rules.EnemyDistances.Length; distanceStep++)
            {
                var minEnemy = rules.EnemyDistances[distanceStep];
                var minEnemySq = minEnemy * minEnemy;
                for (var attempt = 0; attempt < rules.AttemptsPerDistance; attempt++)
                {
                    var angle = random01() * Mathf.PI * 2f;
                    var dist = Mathf.Lerp(rules.MinPlayerDistance, rules.MaxPlayerDistance, random01());
                    var p = player + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
                    if (IsValid(p, player, enemies, arena, obstacles, rules, minEnemySq))
                    {
                        result = p;
                        return true;
                    }
                }
            }

            result = default;
            distanceStep = -1;
            return false;
        }

        public static bool IsValid(Vector2 p, Vector2 player, IReadOnlyList<Vector2> enemies, ArenaGeometry arena,
            IReadOnlyList<ObstacleCircle> obstacles, in ItemSpawnRules rules, float minEnemySq)
        {
            if (p.y - player.y > rules.MaxAbovePlayer) return false;
            if (!arena.Contains(p, rules.WallMargin)) return false;
            if (ObstacleCircle.DistanceToNearest(obstacles, p) < rules.ObstacleMargin) return false;
            if (enemies != null)
            {
                for (var i = 0; i < enemies.Count; i++)
                {
                    if ((enemies[i] - p).sqrMagnitude < minEnemySq) return false;
                }
            }
            return true;
        }
    }
}

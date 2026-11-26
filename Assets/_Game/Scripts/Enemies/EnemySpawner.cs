using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>
    /// Spawns enemies off-camera inside the arena (GDD 7): budget from the difficulty table,
    /// released in pulses as Scatter / Ring / Stream, plus a Horde burst every ~60 s from 2-3
    /// directions with a 1.5 s warning. Over the 150 cap, spawns wait in a queue.
    /// </summary>
    public sealed class EnemySpawner : MonoBehaviour
    {
        struct PendingSpawn
        {
            public EnemyDef Def;
            public bool Elite;
            public float Angle; // NaN = any direction
        }

        RunContext ctx;
        readonly Queue<PendingSpawn> queue = new Queue<PendingSpawn>(256);
        readonly List<EnemySpawnEntry> unlocked = new List<EnemySpawnEntry>(8);
        readonly float[] hordeAngles = new float[3];
        int hordeAngleCount;
        float budget;
        float pulseTimer;
        float nextHordeTime;
        float hordeEndTime = -1f;
        bool hordeWarned;
        float hordeBudget;
        float wallMargin;

        public bool HordeActive => ctx != null && ctx.Run.State.Elapsed < hordeEndTime;
        public int QueuedCount => queue.Count;

        /// <summary>Directions (radians, world space) of the coming horde, raised 1.5 s before it starts.</summary>
        public event Action<float[], int> HordeWarning;

        public void Init(RunContext context)
        {
            ctx = context;
            nextHordeTime = ctx.Config.HordeFirstTime;
            pulseTimer = 1f;
            wallMargin = ctx.W(ctx.Config.SpawnWallMargin);
        }

        /// <summary>Minimum spawn distance: outside the real camera frame of this device.</summary>
        public float MinSpawnDistance
        {
            get
            {
                var cam = ctx.CameraRig.Camera;
                var halfH = cam.orthographicSize;
                var halfW = halfH * cam.aspect;
                var halfDiagonal = Mathf.Sqrt(halfH * halfH + halfW * halfW);
                return Mathf.Max(ctx.W(ctx.Config.SpawnMinDistance), halfDiagonal + ctx.W(60f));
            }
        }

        void Update()
        {
            if (ctx == null || ctx.Run.Phase != RunPhase.Playing) return;
            var dt = Time.deltaTime;
            var t = ctx.Run.State.Elapsed;
            var waves = ctx.Waves;
            var diff = waves.Evaluate(t);

            budget += diff.SpawnRate * dt;

            pulseTimer -= dt;
            if (pulseTimer <= 0f)
            {
                pulseTimer = waves.PulseInterval;
                var n = Mathf.FloorToInt(budget);
                budget -= n;
                if (t < waves.EarlyWindowEnd) n = Mathf.Min(n, Mathf.Max(0, waves.EarlyMaxEnemies - ctx.Enemies.Count - queue.Count));
                if (n > 0) EnqueuePulse(n, t);
            }

            UpdateHorde(t, dt, diff);
            Flush(diff);
        }

        void EnqueuePulse(int n, float t)
        {
            var roll = UnityEngine.Random.value;
            var waves = ctx.Waves;
            var grouped = t >= waves.EarlyWindowEnd && n >= waves.MinGroupSize;
            if (grouped && roll < waves.RingChance)
            {
                // Ring: around the player but with a 60 degree gap so there is always a way out.
                var start = UnityEngine.Random.value * Mathf.PI * 2f;
                var arc = Mathf.PI * 2f * (300f / 360f);
                for (var i = 0; i < n; i++) Enqueue(t, start + arc * i / Mathf.Max(1, n - 1));
            }
            else if (grouped && roll < waves.RingChance + waves.StreamChance)
            {
                var a = UnityEngine.Random.value * Mathf.PI * 2f;
                for (var i = 0; i < n; i++) Enqueue(t, a + UnityEngine.Random.Range(-0.15f, 0.15f));
            }
            else
            {
                for (var i = 0; i < n; i++) Enqueue(t, float.NaN);
            }
        }

        void Enqueue(float t, float angle)
        {
            var def = PickEnemy(t);
            if (def == null) return;
            var elite = t >= ctx.Waves.EliteUnlockTime && UnityEngine.Random.value < ctx.Waves.EliteChance;
            queue.Enqueue(new PendingSpawn { Def = def, Elite = elite, Angle = angle });
        }

        EnemyDef PickEnemy(float t)
        {
            unlocked.Clear();
            var total = 0f;
            foreach (var e in ctx.Waves.Enemies)
            {
                if (e.Enemy == null || t < e.UnlockTime || e.Weight <= 0f) continue;
                unlocked.Add(e);
                total += e.Weight;
            }
            if (unlocked.Count == 0) return null;
            var r = UnityEngine.Random.value * total;
            for (var i = 0; i < unlocked.Count; i++)
            {
                r -= unlocked[i].Weight;
                if (r <= 0f) return unlocked[i].Enemy;
            }
            return unlocked[unlocked.Count - 1].Enemy;
        }

        void UpdateHorde(float t, float dt, Difficulty diff)
        {
            var c = ctx.Config;
            if (!hordeWarned && t >= nextHordeTime - c.HordeWarningTime)
            {
                hordeWarned = true;
                // 2 or 3 directions, at least 90 degrees apart, never all the way round.
                hordeAngleCount = UnityEngine.Random.value < 0.5f ? 2 : 3;
                var baseAngle = UnityEngine.Random.value * Mathf.PI * 2f;
                for (var i = 0; i < hordeAngleCount; i++)
                {
                    hordeAngles[i] = baseAngle + i * Mathf.PI * 0.5f + UnityEngine.Random.Range(-0.2f, 0.2f);
                }
                HordeWarning?.Invoke(hordeAngles, hordeAngleCount);
                ctx.Audio.Play(ctx.Sfx != null ? ctx.Sfx.HordeWarning : null);
            }

            if (t >= nextHordeTime && hordeWarned && hordeEndTime < nextHordeTime)
            {
                hordeEndTime = t + UnityEngine.Random.Range(c.HordeDurationMin, c.HordeDurationMax);
            }

            if (t < hordeEndTime)
            {
                hordeBudget += diff.SpawnRate * c.HordeSpawnRateMultiplier * dt;
                while (hordeBudget >= 1f)
                {
                    hordeBudget -= 1f;
                    var a = hordeAngles[UnityEngine.Random.Range(0, hordeAngleCount)] + UnityEngine.Random.Range(-0.25f, 0.25f);
                    Enqueue(t, a);
                }
            }
            else if (hordeEndTime > 0f && t >= hordeEndTime && hordeWarned && t >= nextHordeTime)
            {
                nextHordeTime += c.HordeInterval;
                hordeWarned = false;
                hordeBudget = 0f;
            }
        }

        void Flush(Difficulty diff)
        {
            var max = ctx.Config.MaxEnemies;
            var safety = 64;
            while (queue.Count > 0 && ctx.Enemies.Count < max && safety-- > 0)
            {
                var p = queue.Peek();
                var radius = ctx.W(p.Def.Radius) * (p.Elite ? ctx.Config.EliteScale : 1f);
                if (!TryFindSpawnPoint(p.Angle, radius, out var pos)) break;
                queue.Dequeue();
                ctx.Enemies.Spawn(p.Def, pos, p.Elite, diff);
            }
        }

        /// <summary>
        /// Off-camera point in the arena, 150+ from walls and clear of obstacles. Tries the requested
        /// direction first, then fans out; NaN angle means any direction.
        /// </summary>
        public bool TryFindSpawnPoint(float angle, float radius, out Vector2 result)
        {
            var player = ctx.Player.Position;
            var minDist = MinSpawnDistance;
            var jitter = ctx.W(ctx.Config.SpawnDistanceJitter);
            var arena = ctx.Arena.Geometry;
            var obstacles = ctx.Arena.Obstacles;
            var baseAngle = float.IsNaN(angle) ? UnityEngine.Random.value * Mathf.PI * 2f : angle;

            for (var i = 0; i < 18; i++)
            {
                // 0, +20, -20, +40, -40 ... degrees
                var step = (i + 1) / 2 * (i % 2 == 0 ? 1 : -1);
                var a = baseAngle + step * 20f * Mathf.Deg2Rad;
                var d = minDist + UnityEngine.Random.value * jitter;
                var p = player + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
                if (!arena.Contains(p, wallMargin + radius)) continue;
                if (ObstacleCircle.DistanceToNearest(obstacles, p) < radius + 0.1f) continue;
                result = p;
                return true;
            }
            result = default;
            return false;
        }
    }
}

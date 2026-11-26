using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>Owns the enemy pools and the active list; ticks movement, handles damage and deaths.</summary>
    public sealed class EnemyManager : MonoBehaviour
    {
        [SerializeField] Transform poolRoot;
        [Tooltip("Instances created per enemy type at load (no Instantiate during play).")]
        [SerializeField] int prewarmPerType = 40;

        readonly List<Enemy> active = new List<Enemy>(256);
        readonly Dictionary<EnemyDef, ComponentPool<Enemy>> pools = new Dictionary<EnemyDef, ComponentPool<Enemy>>();

        public RunContext Context { get; private set; }
        public IReadOnlyList<Enemy> Active => active;
        public int Count => active.Count;
        public ContactFilter2D ObstacleFilter { get; private set; }

        /// <summary>(enemy, rewarded) - rewarded is false for self-destructs.</summary>
        public event Action<Enemy, bool> EnemyKilled;

        public void Init(RunContext ctx)
        {
            Context = ctx;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(1 << GameLayers.Obstacle);
            ObstacleFilter = filter;

            foreach (var entry in ctx.Waves.Enemies)
            {
                if (entry.Enemy == null || entry.Enemy.Prefab == null || pools.ContainsKey(entry.Enemy)) continue;
                pools[entry.Enemy] = new ComponentPool<Enemy>(entry.Enemy.Prefab, poolRoot != null ? poolRoot : transform, prewarmPerType);
            }
        }

        public Enemy Spawn(EnemyDef def, Vector2 position, bool elite, Difficulty difficulty)
        {
            if (!pools.TryGetValue(def, out var pool)) return null;
            var e = pool.Get(position);
            e.SetPosition(position);
            e.Setup(this, def, elite, difficulty);
            active.Add(e);
            return e;
        }

        /// <summary>Applies damage; kills and rewards when HP runs out. Returns true on kill.</summary>
        public bool Damage(Enemy e, float amount, Vector2 hitDirection)
        {
            if (e == null || !e.Alive) return false;
            if (!e.TakeDamage(amount, hitDirection))
            {
                Context.Audio.PlayHit();
                return false;
            }
            Kill(e, true);
            return true;
        }

        public void Kill(Enemy e, bool rewarded)
        {
            if (e == null || !e.Alive) return;
            e.MarkDead();
            var i = active.IndexOf(e);
            if (i >= 0)
            {
                active[i] = active[active.Count - 1];
                active.RemoveAt(active.Count - 1);
            }
            EnemyKilled?.Invoke(e, rewarded);
            if (pools.TryGetValue(e.Def, out var pool)) pool.Release(e);
            else e.gameObject.SetActive(false);
        }

        public Enemy FindNearest(Vector2 from, float maxRange, Enemy exclude = null)
        {
            Enemy best = null;
            var bestSq = maxRange * maxRange;
            for (var i = 0; i < active.Count; i++)
            {
                var e = active[i];
                if (e == exclude || !e.Alive) continue;
                var d = (e.Position - from).sqrMagnitude;
                if (d < bestSq)
                {
                    bestSq = d;
                    best = e;
                }
            }
            return best;
        }

        public void FillPositions(List<Vector2> into)
        {
            into.Clear();
            for (var i = 0; i < active.Count; i++) into.Add(active[i].Position);
        }

        void FixedUpdate()
        {
            if (Context == null || Context.Run.Phase != RunPhase.Playing) return;
            var dt = Time.fixedDeltaTime;
            var player = Context.Player.Position;
            for (var i = active.Count - 1; i >= 0; i--)
            {
                if (i < active.Count) active[i].Tick(dt, player);
            }
        }

        void Update()
        {
            if (Context == null || Context.Run.Phase != RunPhase.Playing) return;
            var dt = Time.deltaTime;
            var c = Context.Config;
            var player = Context.Player.Position;
            var stuckSq = c.ToWorld(c.StuckDistance) * c.ToWorld(c.StuckDistance);
            for (var i = active.Count - 1; i >= 0; i--)
            {
                var e = active[i];
                e.TickVisual(dt);
                if ((e.Position - player).sqrMagnitude > stuckSq)
                {
                    e.FarTimer += dt;
                    if (e.FarTimer > c.StuckTime && Context.Spawner.TryFindSpawnPoint(float.NaN, e.Radius, out var p))
                    {
                        e.SetPosition(p);
                        e.FarTimer = 0f;
                    }
                }
                else
                {
                    e.FarTimer = 0f;
                }
            }
        }

        public void KillAll()
        {
            for (var i = active.Count - 1; i >= 0; i--) Kill(active[i], false);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>Moves all bullets in one loop; player bullets hit enemies, enemy corks hit the player.</summary>
    public sealed class ProjectileSystem : MonoBehaviour
    {
        [SerializeField] Projectile playerProjectilePrefab;
        [SerializeField] Projectile enemyProjectilePrefab;
        [SerializeField] Transform poolRoot;
        [SerializeField] int prewarmPlayer = 200;
        [SerializeField] int prewarmEnemy = 100;
        [SerializeField] Color enemyShotColor = new Color(0.55f, 0.2f, 0.85f);
        [Tooltip("Enemy cork radius in GDD units.")]
        [SerializeField] float enemyShotRadius = 16f;

        RunContext ctx;
        ComponentPool<Projectile> playerPool;
        ComponentPool<Projectile> enemyPool;
        readonly List<Projectile> active = new List<Projectile>(512);

        public int ActiveCount => active.Count;

        public void Init(RunContext context)
        {
            ctx = context;
            var root = poolRoot != null ? poolRoot : transform;
            playerPool = new ComponentPool<Projectile>(playerProjectilePrefab, root, prewarmPlayer);
            enemyPool = new ComponentPool<Projectile>(enemyProjectilePrefab, root, prewarmEnemy);
        }

        public Projectile Fire(Vector2 origin, Vector2 direction, float speed, float damage, float radius, float range,
            int pierce, float knockback, Color color, float visualScale = 1f)
        {
            var p = playerPool.Get(origin);
            var life = speed > 0f ? range / speed : 1f;
            p.Setup(origin, direction.normalized * speed, damage, radius, life, pierce, knockback, false, color, visualScale);
            active.Add(p);
            return p;
        }

        public void FireEnemyShot(Vector2 origin, Vector2 direction, float speed, float damage)
        {
            var p = enemyPool.Get(origin);
            p.Setup(origin, direction.normalized * speed, damage, ctx.W(enemyShotRadius), 6f, 0, 0f, true, enemyShotColor, 1f);
            active.Add(p);
        }

        void Update()
        {
            if (ctx == null || ctx.Run.Phase != RunPhase.Playing) return;
            var dt = Time.deltaTime;
            var enemies = ctx.Enemies.Active;
            var arena = ctx.Arena.Geometry;
            var player = ctx.Player.Position;
            var playerRadius = ctx.Player.Radius;

            for (var i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                p.Position += p.Velocity * dt;
                p.LifeLeft -= dt;
                p.transform.position = p.Position;

                var dead = p.LifeLeft <= 0f || !arena.Contains(p.Position, -1f);

                if (!dead && p.Hostile)
                {
                    var r = p.Radius + playerRadius;
                    if ((player - p.Position).sqrMagnitude <= r * r)
                    {
                        ctx.PlayerHealth.TakeDamage(p.Damage);
                        dead = true;
                    }
                }
                else if (!dead)
                {
                    for (var j = enemies.Count - 1; j >= 0; j--)
                    {
                        if (j >= enemies.Count) continue;
                        var e = enemies[j];
                        var r = p.Radius + e.Radius;
                        if ((e.Position - p.Position).sqrMagnitude > r * r || p.HasHit(e)) continue;
                        var dir = p.Velocity.normalized;
                        if (p.Knockback > 0f) e.ApplyKnockback(dir * p.Knockback);
                        ctx.Enemies.Damage(e, p.Damage, dir);
                        p.Remember(e);
                        if (p.PierceLeft <= 0)
                        {
                            dead = true;
                            break;
                        }
                        p.PierceLeft--;
                    }
                }

                if (dead) Release(i);
            }
        }

        void Release(int index)
        {
            var p = active[index];
            active[index] = active[active.Count - 1];
            active.RemoveAt(active.Count - 1);
            (p.Hostile ? enemyPool : playerPool).Release(p);
        }

        public void ClearEnemyShots()
        {
            for (var i = active.Count - 1; i >= 0; i--)
            {
                if (active[i].Hostile) Release(i);
            }
        }
    }
}

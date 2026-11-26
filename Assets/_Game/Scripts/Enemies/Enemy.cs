using UnityEngine;

namespace RunChildRun
{
    /// <summary>
    /// One pooled enemy. Movement is driven by <see cref="EnemyManager"/> in FixedUpdate:
    /// chase with steering around obstacles, plus per-type behaviour (GDD 7).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Enemy : MonoBehaviour
    {
        [SerializeField] Rigidbody2D body;
        [SerializeField] CircleCollider2D circle;
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] Transform visual;
        [Tooltip("Gold aura shown on Elite variants.")]
        [SerializeField] GameObject eliteAura;
        [Tooltip("Shield visual for Shielded enemies; rotated to face the player.")]
        [SerializeField] Transform shield;

        static readonly RaycastHit2D[] CastHits = new RaycastHit2D[2];
        static readonly Color FlashColor = new Color(1f, 1f, 1f, 1f);

        EnemyManager owner;
        Color baseColor;
        float speed;
        Vector2 knockback;
        float flashLeft;
        float slowLeft;
        float attackTimer;
        float fuseLeft = -1f;
        float phase;
        Vector2 facing = Vector2.right;
        Vector3 baseScale = Vector3.one;
        bool initialized;

        public EnemyDef Def { get; private set; }
        public bool IsElite { get; private set; }
        public float Hp { get; private set; }
        public float MaxHp { get; private set; }
        public float Radius { get; private set; }
        public float ContactDamage { get; private set; }
        public bool Alive { get; private set; }
        public Vector2 Position => body.position;
        public float FarTimer { get; set; }
        /// <summary>Next time Orbit Blades may hit this enemy (only one orbit skill can be active).</summary>
        public float NextOrbitHitTime { get; set; }
        public int ExpValue => IsElite ? Def.Exp * owner.Context.Config.EliteExpMultiplier : Def.Exp;

        void Awake()
        {
            if (sprite != null) baseColor = sprite.color;
            baseScale = transform.localScale;
            initialized = true;
        }

        public void Setup(EnemyManager manager, EnemyDef def, bool elite, Difficulty difficulty)
        {
            if (!initialized) Awake();
            owner = manager;
            Def = def;
            IsElite = elite;
            var c = manager.Context.Config;
            MaxHp = def.Hp * difficulty.HpMultiplier * (elite ? c.EliteHpMultiplier : 1f);
            Hp = MaxHp;
            ContactDamage = def.Behaviour == EnemyBehaviour.Exploder ? 0f : def.ContactDamage;
            speed = DifficultyCurve.EnemySpeed(def.SpeedPercent, difficulty.SpeedMultiplier, c.ToWorld(c.PlayerSpeed), c.EnemySpeedCap);

            var scale = elite ? c.EliteScale : 1f;
            transform.localScale = baseScale * scale;
            Radius = c.ToWorld(def.Radius) * scale;
            if (circle != null) circle.radius = c.ToWorld(def.Radius) / Mathf.Max(0.0001f, baseScale.x);
            body.mass = def.Mass * (elite ? 2f : 1f);
            body.linearVelocity = Vector2.zero;

            knockback = Vector2.zero;
            flashLeft = 0f;
            slowLeft = 0f;
            attackTimer = def.AttackCooldown * Random.Range(0.5f, 1f);
            fuseLeft = -1f;
            phase = Random.value * Mathf.PI * 2f;
            FarTimer = 0f;
            NextOrbitHitTime = 0f;
            Alive = true;
            if (eliteAura != null) eliteAura.SetActive(elite);
            if (shield != null) shield.gameObject.SetActive(def.Behaviour == EnemyBehaviour.Shielded);
            if (sprite != null) sprite.color = baseColor;
        }

        public void Tick(float dt, Vector2 player)
        {
            if (!Alive) return;
            var pos = body.position;
            var toPlayer = player - pos;
            var dist = toPlayer.magnitude;
            var dir = dist > 0.001f ? toPlayer / dist : facing;
            var move = Vector2.zero;
            var s = speed * (slowLeft > 0f ? 0.5f : 1f);

            switch (Def.Behaviour)
            {
                case EnemyBehaviour.Chase:
                case EnemyBehaviour.Shielded:
                    move = Steer(pos, dir) * s;
                    break;

                case EnemyBehaviour.Swarm:
                    phase += dt * Def.WaveFrequency;
                    var perp = new Vector2(-dir.y, dir.x);
                    move = Steer(pos, (dir + perp * (Mathf.Sin(phase) * Def.WaveAmplitude)).normalized) * s;
                    break;

                case EnemyBehaviour.Ranged:
                    var range = owner.Context.W(Def.AttackRange);
                    if (dist > range * 0.85f) move = Steer(pos, dir) * s;
                    else if (dist < range * 0.5f) move = -dir * s * 0.6f;
                    else move = new Vector2(-dir.y, dir.x) * (s * 0.3f * Mathf.Sin(phase += dt));
                    attackTimer -= dt;
                    if (attackTimer <= 0f && dist <= range)
                    {
                        attackTimer = Def.AttackCooldown;
                        owner.Context.Projectiles.FireEnemyShot(pos + dir * Radius, dir,
                            owner.Context.W(Def.ProjectileSpeed), Def.ProjectileDamage);
                    }
                    break;

                case EnemyBehaviour.Exploder:
                    if (fuseLeft >= 0f)
                    {
                        fuseLeft -= dt;
                        if (fuseLeft <= 0f)
                        {
                            Explode();
                            return;
                        }
                    }
                    else
                    {
                        move = Steer(pos, dir) * s;
                        if (dist <= owner.Context.W(Def.ExplodeTriggerRange) + Radius) StartFuse();
                    }
                    break;
            }

            if (dir.sqrMagnitude > 0f) facing = dir;
            body.linearVelocity = move + knockback;
            knockback = Vector2.MoveTowards(knockback, Vector2.zero, dt * 30f);
        }

        Vector2 Steer(Vector2 pos, Vector2 desired)
        {
            var look = owner.Context.W(owner.Context.Config.EnemyObstacleLookAhead);
            var hits = Physics2D.CircleCast(pos, Radius * 0.9f, desired, owner.ObstacleFilter, CastHits, look);
            if (hits == 0) return desired;
            var n = CastHits[0].normal;
            var tangent = new Vector2(-n.y, n.x);
            if (Vector2.Dot(tangent, desired) < 0f) tangent = -tangent;
            return (tangent * 0.85f + desired * 0.15f).normalized;
        }

        /// <summary>Visual updates (flash, shield facing). Called from Update.</summary>
        public void TickVisual(float dt)
        {
            if (flashLeft > 0f)
            {
                flashLeft -= dt;
                if (flashLeft <= 0f && sprite != null) sprite.color = fuseLeft >= 0f ? FuseColor() : baseColor;
            }
            else if (fuseLeft >= 0f && sprite != null)
            {
                sprite.color = FuseColor();
            }
            if (slowLeft > 0f) slowLeft -= dt;
            if (shield != null && shield.gameObject.activeSelf)
            {
                shield.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg);
            }
            if (visual != null && Mathf.Abs(facing.x) > 0.1f)
            {
                var sc = visual.localScale;
                sc.x = Mathf.Abs(sc.x) * (facing.x < 0f ? -1f : 1f);
                visual.localScale = sc;
            }
        }

        Color FuseColor() => Mathf.Repeat(fuseLeft, 0.12f) < 0.06f ? Color.white : baseColor;

        /// <summary>Returns true if this hit killed the enemy. <paramref name="hitDirection"/> is the travel direction of the hit.</summary>
        public bool TakeDamage(float amount, Vector2 hitDirection)
        {
            if (!Alive || amount <= 0f) return false;
            if (Def.Behaviour == EnemyBehaviour.Shielded && hitDirection.sqrMagnitude > 0f)
            {
                // Hit from the front: the projectile travels against the facing direction.
                if (Vector2.Angle(facing, -hitDirection) < Def.ShieldArcDegrees * 0.5f) amount *= Def.ShieldDamageMultiplier;
            }
            Hp -= amount;
            flashLeft = 0.08f;
            if (sprite != null) sprite.color = FlashColor;
            return Hp <= 0f;
        }

        public void ApplyKnockback(Vector2 impulse)
        {
            knockback += impulse * (1f - Def.KnockbackResistance);
        }

        public void Slow(float seconds) => slowLeft = Mathf.Max(slowLeft, seconds);

        public void StartFuse()
        {
            if (Def.Behaviour != EnemyBehaviour.Exploder || fuseLeft >= 0f || !Alive) return;
            fuseLeft = Def.ExplodeFuse;
            body.linearVelocity = Vector2.zero;
        }

        void Explode()
        {
            var ctx = owner.Context;
            var r = ctx.W(Def.ExplodeRadius);
            ctx.Fx.Ring(body.position, r * 0.3f, r, 0.35f, new Color(1f, 0.45f, 0.2f, 0.8f));
            if ((ctx.Player.Position - body.position).sqrMagnitude <= (r + ctx.Player.Radius) * (r + ctx.Player.Radius))
            {
                ctx.PlayerHealth.TakeDamage(Def.ExplodeDamage);
            }
            ctx.CameraRig.Shake(0.12f, 0.2f);
            owner.Kill(this, false);
        }

        public void MarkDead()
        {
            Alive = false;
            body.linearVelocity = Vector2.zero;
        }

        public void SetPosition(Vector2 p)
        {
            body.position = p;
            transform.position = p;
        }
    }
}

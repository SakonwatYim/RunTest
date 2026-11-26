using System.Collections.Generic;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>Behaviour of one active skill. Created on pickup, disposed when it expires.</summary>
    public abstract class SkillRuntime
    {
        protected RunContext Ctx;
        public ActiveSkill Skill { get; private set; }

        protected SkillLevelStats S => Skill.Def.GetLevel(Skill.Level);
        protected StatBlock Stats => Ctx.Run.Stats;
        protected float Damage => S.Damage * Stats.DamageMultiplier;
        protected float Cooldown => Mathf.Max(0.05f, S.Cooldown * Stats.CooldownMultiplier);
        protected float AreaMul => Stats.AreaMultiplier;
        protected Vector2 PlayerPos => Ctx.Player.Position;
        protected float W(float gdu) => Ctx.W(gdu);

        public static SkillRuntime Create(SkillKind kind) => kind switch
        {
            SkillKind.BoltShot => new BoltShotRuntime(),
            SkillKind.Scatter => new ScatterRuntime(),
            SkillKind.OrbitBlades => new OrbitBladesRuntime(),
            SkillKind.ShockwavePulse => new ShockwaveRuntime(),
            SkillKind.ChainSpark => new ChainSparkRuntime(),
            SkillKind.MineLayer => new MineLayerRuntime(),
            _ => new BoltShotRuntime(),
        };

        public void Bind(RunContext ctx, ActiveSkill skill)
        {
            Ctx = ctx;
            Skill = skill;
            OnActivate();
        }

        protected virtual void OnActivate() { }
        public virtual void OnDeactivate() { }
        public abstract void Tick(float dt);

        protected void Hit(Enemy e, float damage, Vector2 direction, float knockback)
        {
            if (knockback > 0f && direction.sqrMagnitude > 0f) e.ApplyKnockback(direction.normalized * knockback);
            Ctx.Enemies.Damage(e, damage, direction);
        }

        protected void PlayShootSfx() => Ctx.Audio.Play(Ctx.Sfx != null ? Ctx.Sfx.Shoot : null, 0.9f + Random.value * 0.2f, 0.5f);
    }

    /// <summary>Single fast dart at the nearest enemy. Lv5 pierces one enemy.</summary>
    public sealed class BoltShotRuntime : SkillRuntime
    {
        float timer;

        public override void Tick(float dt)
        {
            timer -= dt;
            if (timer > 0f) return;
            var s = S;
            var target = Ctx.Enemies.FindNearest(PlayerPos, W(s.Range));
            if (target == null)
            {
                timer = 0.1f;
                return;
            }
            var first = Ctx.Run.ConsumeFirstShot();
            Ctx.Projectiles.Fire(PlayerPos, target.Position - PlayerPos, W(s.Speed), Damage, W(s.Area) * AreaMul,
                W(s.Range) * 1.2f, s.Pierce, W(s.Knockback), first ? Color.white : Skill.Def.Color, (first ? 2.2f : 1f) * AreaMul);
            PlayShootSfx();
            timer = Cooldown;
        }
    }

    /// <summary>3-6 short-range plastic pellets in a fan towards the nearest enemy.</summary>
    public sealed class ScatterRuntime : SkillRuntime
    {
        const float SpreadDegrees = 18f;
        float timer;

        public override void Tick(float dt)
        {
            timer -= dt;
            if (timer > 0f) return;
            var s = S;
            var range = W(s.Range);
            var target = Ctx.Enemies.FindNearest(PlayerPos, range * 1.1f);
            if (target == null)
            {
                timer = 0.1f;
                return;
            }
            var first = Ctx.Run.ConsumeFirstShot();
            var baseDir = (target.Position - PlayerPos).normalized;
            var baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
            var n = Mathf.Max(1, s.Count);
            for (var i = 0; i < n; i++)
            {
                var a = (baseAngle + (i - (n - 1) * 0.5f) * SpreadDegrees) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Ctx.Projectiles.Fire(PlayerPos, dir, W(s.Speed), Damage, W(s.Area) * AreaMul, range, s.Pierce,
                    W(s.Knockback), first ? Color.white : Skill.Def.Color, (first ? 1.8f : 1f) * AreaMul);
            }
            PlayShootSfx();
            timer = Cooldown;
        }
    }

    /// <summary>Spinning tops circling the player. No aiming.</summary>
    public sealed class OrbitBladesRuntime : SkillRuntime
    {
        readonly List<PooledSprite> blades = new List<PooledSprite>(6);
        float angle;

        protected override void OnActivate() => SyncCount();

        public override void OnDeactivate()
        {
            for (var i = 0; i < blades.Count; i++) Ctx.Fx.ReleaseSprite(SpriteFxKind.OrbitBlade, blades[i]);
            blades.Clear();
        }

        void SyncCount()
        {
            var n = Mathf.Max(1, S.Count);
            while (blades.Count < n)
            {
                var b = Ctx.Fx.GetSprite(SpriteFxKind.OrbitBlade, PlayerPos);
                b.SetColor(Skill.Def.Color);
                blades.Add(b);
            }
            while (blades.Count > n)
            {
                Ctx.Fx.ReleaseSprite(SpriteFxKind.OrbitBlade, blades[blades.Count - 1]);
                blades.RemoveAt(blades.Count - 1);
            }
        }

        public override void Tick(float dt)
        {
            SyncCount();
            var s = S;
            angle += s.Speed * dt;
            var radius = W(s.Range) * AreaMul;
            var bladeRadius = W(s.Area) * AreaMul;
            var now = Ctx.Run.State.Elapsed;
            var enemies = Ctx.Enemies.Active;
            var center = PlayerPos;
            for (var i = 0; i < blades.Count; i++)
            {
                var a = (angle + 360f * i / blades.Count) * Mathf.Deg2Rad;
                var pos = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                var b = blades[i];
                b.transform.position = pos;
                b.transform.rotation = Quaternion.Euler(0f, 0f, angle * 3f);
                b.SetDiameter(bladeRadius * 2f);

                for (var j = enemies.Count - 1; j >= 0; j--)
                {
                    if (j >= enemies.Count) continue;
                    var e = enemies[j];
                    var r = bladeRadius + e.Radius;
                    if ((e.Position - pos).sqrMagnitude > r * r || now < e.NextOrbitHitTime) continue;
                    e.NextOrbitHitTime = now + s.Cooldown;
                    Hit(e, Damage, e.Position - center, W(s.Knockback));
                }
            }
        }
    }

    /// <summary>Whistle pulse that pushes enemies away. Lv5 also damages and slows for 1 s.</summary>
    public sealed class ShockwaveRuntime : SkillRuntime
    {
        float timer = 0.2f;

        public override void Tick(float dt)
        {
            timer -= dt;
            if (timer > 0f) return;
            timer = Cooldown;
            var s = S;
            var radius = W(s.Area) * AreaMul;
            var center = PlayerPos;
            Ctx.Fx.Ring(center, radius * 0.2f, radius, 0.4f, new Color(Skill.Def.Color.r, Skill.Def.Color.g, Skill.Def.Color.b, 0.45f));
            var enemies = Ctx.Enemies.Active;
            for (var j = enemies.Count - 1; j >= 0; j--)
            {
                if (j >= enemies.Count) continue;
                var e = enemies[j];
                var d = e.Position - center;
                var r = radius + e.Radius;
                if (d.sqrMagnitude > r * r) continue;
                var dir = d.sqrMagnitude > 0.0001f ? d.normalized : Random.insideUnitCircle.normalized;
                e.ApplyKnockback(dir * W(s.Knockback));
                if (s.Special)
                {
                    e.Slow(1f);
                    if (s.Damage > 0f) Ctx.Enemies.Damage(e, Damage, dir);
                }
            }
            Ctx.Audio.Play(Ctx.Sfx != null ? Ctx.Sfx.Shockwave : null);
        }
    }

    /// <summary>Static spark that jumps between up to 3-6 enemies.</summary>
    public sealed class ChainSparkRuntime : SkillRuntime
    {
        const int MaxChain = 12;
        readonly Enemy[] chain = new Enemy[MaxChain];
        readonly Vector3[] points = new Vector3[MaxChain + 1];
        float timer;

        public override void Tick(float dt)
        {
            timer -= dt;
            if (timer > 0f) return;
            var s = S;
            var first = Ctx.Enemies.FindNearest(PlayerPos, W(s.Range));
            if (first == null)
            {
                timer = 0.1f;
                return;
            }
            var targets = Mathf.Clamp(s.Count, 1, MaxChain);
            var jump = W(s.Area) * AreaMul;
            var count = 0;
            points[0] = PlayerPos;
            var current = first;
            while (current != null && count < targets)
            {
                chain[count] = current;
                points[count + 1] = current.Position;
                count++;
                current = FindNext(current.Position, jump, count);
            }
            var dmg = Damage;
            for (var i = 0; i < count; i++)
            {
                var e = chain[i];
                Hit(e, dmg, (Vector2)points[i + 1] - (Vector2)points[i], W(s.Knockback));
                chain[i] = null;
            }
            Ctx.Fx.Line(points, count + 1, 0.18f, Skill.Def.Color);
            PlayShootSfx();
            timer = Cooldown;
        }

        Enemy FindNext(Vector2 from, float range, int count)
        {
            Enemy best = null;
            var bestSq = range * range;
            var enemies = Ctx.Enemies.Active;
            for (var i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                var already = false;
                for (var k = 0; k < count; k++)
                {
                    if (chain[k] == e)
                    {
                        already = true;
                        break;
                    }
                }
                if (already) continue;
                var d = (e.Position - from).sqrMagnitude;
                if (d < bestSq)
                {
                    bestSq = d;
                    best = e;
                }
            }
            return best;
        }
    }

    /// <summary>Drops water-balloon mines along the player's path. Lv5 leaves a slowing puddle.</summary>
    public sealed class MineLayerRuntime : SkillRuntime
    {
        struct Mine
        {
            public PooledSprite Sprite;
            public Vector2 Position;
            public float Age;
        }

        struct Puddle
        {
            public PooledSprite Sprite;
            public Vector2 Position;
            public float Left;
        }

        const float MineLifetime = 15f;
        const float ArmTime = 0.3f;
        const float PuddleLifetime = 3f;
        const float MinStepGdu = 70f;
        const float TriggerGdu = 45f;

        readonly List<Mine> mines = new List<Mine>(16);
        readonly List<Puddle> puddles = new List<Puddle>(8);
        float timer;
        Vector2 lastDrop;

        protected override void OnActivate() => lastDrop = PlayerPos + Vector2.one * 100f;

        public override void OnDeactivate()
        {
            for (var i = 0; i < mines.Count; i++) Ctx.Fx.ReleaseSprite(SpriteFxKind.Mine, mines[i].Sprite);
            for (var i = 0; i < puddles.Count; i++) Ctx.Fx.ReleaseSprite(SpriteFxKind.Puddle, puddles[i].Sprite);
            mines.Clear();
            puddles.Clear();
        }

        public override void Tick(float dt)
        {
            var s = S;
            timer -= dt;
            if (timer <= 0f && (PlayerPos - lastDrop).sqrMagnitude >= W(MinStepGdu) * W(MinStepGdu))
            {
                if (mines.Count >= Mathf.Max(1, s.Count)) RemoveMine(0, false);
                var sprite = Ctx.Fx.GetSprite(SpriteFxKind.Mine, PlayerPos);
                sprite.SetColor(Skill.Def.Color);
                sprite.SetDiameter(W(60f) * AreaMul);
                mines.Add(new Mine { Sprite = sprite, Position = PlayerPos });
                lastDrop = PlayerPos;
                timer = Cooldown;
            }

            var enemies = Ctx.Enemies.Active;
            var trigger = W(TriggerGdu);
            for (var i = mines.Count - 1; i >= 0; i--)
            {
                var m = mines[i];
                m.Age += dt;
                mines[i] = m;
                if (m.Age >= MineLifetime)
                {
                    RemoveMine(i, false);
                    continue;
                }
                if (m.Age < ArmTime) continue;
                for (var j = 0; j < enemies.Count; j++)
                {
                    var r = trigger + enemies[j].Radius;
                    if ((enemies[j].Position - m.Position).sqrMagnitude <= r * r)
                    {
                        RemoveMine(i, true);
                        break;
                    }
                }
            }

            for (var i = puddles.Count - 1; i >= 0; i--)
            {
                var p = puddles[i];
                p.Left -= dt;
                puddles[i] = p;
                if (p.Left <= 0f)
                {
                    Ctx.Fx.ReleaseSprite(SpriteFxKind.Puddle, p.Sprite);
                    puddles.RemoveAt(i);
                    continue;
                }
                var radius = W(s.Area) * AreaMul;
                for (var j = 0; j < enemies.Count; j++)
                {
                    var r = radius + enemies[j].Radius;
                    if ((enemies[j].Position - p.Position).sqrMagnitude <= r * r) enemies[j].Slow(0.2f);
                }
            }
        }

        void RemoveMine(int index, bool explode)
        {
            var m = mines[index];
            mines.RemoveAt(index);
            Ctx.Fx.ReleaseSprite(SpriteFxKind.Mine, m.Sprite);
            if (!explode) return;

            var s = S;
            var radius = W(s.Area) * AreaMul;
            Ctx.Fx.Ring(m.Position, radius * 0.3f, radius, 0.3f, new Color(0.4f, 0.75f, 1f, 0.7f));
            var enemies = Ctx.Enemies.Active;
            var dmg = Damage;
            for (var j = enemies.Count - 1; j >= 0; j--)
            {
                if (j >= enemies.Count) continue;
                var e = enemies[j];
                var r = radius + e.Radius;
                var d = e.Position - m.Position;
                if (d.sqrMagnitude <= r * r) Hit(e, dmg, d, W(s.Knockback));
            }
            if (s.Special)
            {
                var sprite = Ctx.Fx.GetSprite(SpriteFxKind.Puddle, m.Position);
                sprite.SetDiameter(radius * 2f);
                puddles.Add(new Puddle { Sprite = sprite, Position = m.Position, Left = PuddleLifetime });
            }
        }
    }
}

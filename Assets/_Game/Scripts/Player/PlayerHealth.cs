using System;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>HP, i-frames (0.6 s on hit, 1.5 s after level-up), regen and enemy contact damage.</summary>
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField] PlayerController controller;
        [Tooltip("Extra reach for contact checks, in GDD units.")]
        [SerializeField] float contactSlack = 8f;

        RunContext ctx;
        float invulnerableLeft;
        float baseMaxHp;
        float blinkTimer;

        public float Hp { get; private set; }
        public float MaxHp { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsInvulnerable => invulnerableLeft > 0f || (controller != null && controller.IsDashing);
        public float Hp01 => MaxHp > 0f ? Hp / MaxHp : 0f;

        public event Action Changed;
        public event Action<float> Damaged;
        public event Action Died;

        public void Init(RunContext context, MetaModifiers meta)
        {
            ctx = context;
            baseMaxHp = ctx.Config.PlayerMaxHp * meta.HpMultiplier;
            MaxHp = baseMaxHp;
            Hp = MaxHp;
            IsDead = false;
            invulnerableLeft = 0f;
            Changed?.Invoke();
        }

        /// <summary>Recomputes max HP after a Max HP stat level; the gained amount is healed too.</summary>
        public void RefreshMaxHp(float bonus)
        {
            var newMax = baseMaxHp + bonus;
            var delta = newMax - MaxHp;
            MaxHp = newMax;
            if (delta > 0f) Hp += delta;
            Hp = Mathf.Min(Hp, MaxHp);
            Changed?.Invoke();
        }

        public void GrantInvulnerability(float seconds) => invulnerableLeft = Mathf.Max(invulnerableLeft, seconds);

        public bool TakeDamage(float amount)
        {
            if (IsDead || IsInvulnerable || amount <= 0f || ctx == null) return false;
            if (ctx.Run.Phase != RunPhase.Playing) return false;
            var c = ctx.Config;
            if (ctx.Run.State.Elapsed < c.EarlyDamageWindow) amount *= c.EarlyDamageMultiplier;

            Hp = Mathf.Max(0f, Hp - amount);
            invulnerableLeft = c.HitIFrames;
            Damaged?.Invoke(amount);
            Changed?.Invoke();
            if (Hp <= 0f)
            {
                IsDead = true;
                Died?.Invoke();
            }
            return true;
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Hp = Mathf.Min(MaxHp, Hp + amount);
            Changed?.Invoke();
        }

        public void Revive(float hpFraction, float invulnerableSeconds)
        {
            IsDead = false;
            Hp = Mathf.Max(1f, MaxHp * hpFraction);
            invulnerableLeft = invulnerableSeconds;
            Changed?.Invoke();
        }

        void Update()
        {
            if (ctx == null || IsDead || ctx.Run.Phase != RunPhase.Playing) return;
            var dt = Time.deltaTime;

            if (invulnerableLeft > 0f) invulnerableLeft -= dt;

            var regen = ctx.Run.Stats.RegenPerSecond;
            if (regen > 0f && Hp < MaxHp) Heal(regen * dt);

            CheckContacts();
            UpdateBlink(dt);
        }

        void CheckContacts()
        {
            var enemies = ctx.Enemies.Active;
            var p = controller.Position;
            var reach = controller.Radius + ctx.W(contactSlack);
            var worst = 0f;
            for (var i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                var r = reach + e.Radius;
                if ((e.Position - p).sqrMagnitude > r * r) continue;
                if (e.Def.Behaviour == EnemyBehaviour.Exploder)
                {
                    e.StartFuse();
                    continue;
                }
                if (e.ContactDamage > worst) worst = e.ContactDamage;
            }
            if (worst > 0f) TakeDamage(worst);
        }

        void UpdateBlink(float dt)
        {
            var sprite = controller.Sprite;
            if (sprite == null) return;
            if (invulnerableLeft > 0f)
            {
                blinkTimer += dt;
                var c = sprite.color;
                c.a = Mathf.Repeat(blinkTimer, 0.16f) < 0.08f ? 0.35f : 1f;
                sprite.color = c;
            }
            else if (sprite.color.a < 1f)
            {
                var c = sprite.color;
                c.a = 1f;
                sprite.color = c;
            }
        }
    }
}

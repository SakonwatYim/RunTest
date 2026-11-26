using System;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>Joystick movement and Dash (GDD 3): 450 units in 0.2 s, 6 s cooldown, invulnerable, passes enemies.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] Rigidbody2D body;
        [SerializeField] Transform visual;
        [SerializeField] SpriteRenderer sprite;

        RunContext ctx;
        float baseSpeed;
        float metaSpeedMultiplier = 1f;
        float dashSpeed;
        float dashTimeLeft;
        float dashCooldownLeft;
        float dashCooldownTotal;
        Vector2 dashDirection;
        bool wasDashReady = true;

        public Vector2 Position => body != null ? body.position : (Vector2)transform.position;
        public Vector2 LastMoveDirection { get; private set; } = Vector2.right;
        public Vector2 Velocity => body.linearVelocity;
        public bool IsDashing => dashTimeLeft > 0f;
        public float DashCooldownLeft => dashCooldownLeft;
        public float DashCooldownTotal => dashCooldownTotal;
        public SpriteRenderer Sprite => sprite;
        public float Radius { get; private set; }

        public event Action Dashed;
        public event Action DashReady;

        public void Init(RunContext context, MetaModifiers meta)
        {
            ctx = context;
            var c = ctx.Config;
            baseSpeed = c.ToWorld(c.PlayerSpeed);
            metaSpeedMultiplier = meta.MoveSpeedMultiplier;
            dashSpeed = c.ToWorld(c.DashDistance) / Mathf.Max(0.01f, c.DashDuration);
            dashCooldownTotal = c.DashCooldown * meta.DashCooldownMultiplier;
            Radius = c.ToWorld(c.PlayerRadius);
            body.position = Vector2.zero;
            transform.position = Vector3.zero;
        }

        public float CurrentSpeed => baseSpeed * metaSpeedMultiplier * (ctx != null && ctx.Run.Stats != null ? ctx.Run.Stats.MoveSpeedMultiplier : 1f);

        void Update()
        {
            if (ctx == null || ctx.Run.Phase != RunPhase.Playing) return;

            if (dashCooldownLeft > 0f)
            {
                dashCooldownLeft -= Time.deltaTime;
                if (dashCooldownLeft <= 0f) dashCooldownLeft = 0f;
            }
            var ready = dashCooldownLeft <= 0f;
            if (ready && !wasDashReady) DashReady?.Invoke();
            wasDashReady = ready;

            if (ctx.Input.ConsumeDash()) TryDash();

            if (visual != null && Mathf.Abs(body.linearVelocity.x) > 0.05f)
            {
                var s = visual.localScale;
                s.x = Mathf.Abs(s.x) * (body.linearVelocity.x < 0f ? -1f : 1f);
                visual.localScale = s;
            }
        }

        void FixedUpdate()
        {
            if (ctx == null) return;
            if (ctx.Run.Phase != RunPhase.Playing || ctx.PlayerHealth.IsDead)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            if (dashTimeLeft > 0f)
            {
                dashTimeLeft -= Time.fixedDeltaTime;
                body.linearVelocity = dashDirection * dashSpeed;
                if (dashTimeLeft <= 0f) EndDash();
                return;
            }

            var move = ctx.Input.Move;
            if (move.sqrMagnitude > 0.0001f) LastMoveDirection = move.normalized;
            body.linearVelocity = move * CurrentSpeed;
        }

        public bool TryDash()
        {
            if (dashCooldownLeft > 0f || IsDashing || ctx.PlayerHealth.IsDead) return false;
            var move = ctx.Input.Move;
            dashDirection = move.sqrMagnitude > 0.01f ? move.normalized : LastMoveDirection;
            dashTimeLeft = ctx.Config.DashDuration;
            dashCooldownLeft = dashCooldownTotal;
            wasDashReady = false;
            SetEnemyCollision(false);
            Dashed?.Invoke();
            return true;
        }

        void EndDash()
        {
            dashTimeLeft = 0f;
            SetEnemyCollision(true);
        }

        static void SetEnemyCollision(bool enabled)
        {
            var p = GameLayers.Player;
            var e = GameLayers.Enemy;
            if (p >= 0 && e >= 0) Physics2D.IgnoreLayerCollision(p, e, !enabled);
        }

        void OnDisable()
        {
            SetEnemyCollision(true);
        }

        public void Teleport(Vector2 position)
        {
            body.position = position;
            transform.position = position;
        }
    }
}

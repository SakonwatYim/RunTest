using UnityEngine;

namespace RunChildRun
{
    /// <summary>Pooled bullet. Data only; <see cref="ProjectileSystem"/> moves it and checks hits.</summary>
    public sealed class Projectile : MonoBehaviour
    {
        public const int MaxHitMemory = 8;

        [SerializeField] SpriteRenderer sprite;

        public Vector2 Position;
        public Vector2 Velocity;
        public float Damage;
        public float Radius;
        public float LifeLeft;
        public int PierceLeft;
        public float Knockback;
        public bool Hostile;
        public readonly Enemy[] HitMemory = new Enemy[MaxHitMemory];
        public int HitCount;

        Vector3 baseScale = Vector3.one;
        bool cached;

        public void Setup(Vector2 position, Vector2 velocity, float damage, float radius, float life, int pierce,
            float knockback, bool hostile, Color color, float visualScale)
        {
            if (!cached)
            {
                baseScale = transform.localScale;
                cached = true;
            }
            Position = position;
            Velocity = velocity;
            Damage = damage;
            Radius = radius;
            LifeLeft = life;
            PierceLeft = pierce;
            Knockback = knockback;
            Hostile = hostile;
            HitCount = 0;
            for (var i = 0; i < MaxHitMemory; i++) HitMemory[i] = null;
            transform.position = position;
            transform.localScale = baseScale * visualScale;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            if (sprite != null) sprite.color = color;
        }

        public bool HasHit(Enemy e)
        {
            for (var i = 0; i < HitCount; i++)
            {
                if (HitMemory[i] == e) return true;
            }
            return false;
        }

        public void Remember(Enemy e)
        {
            if (HitCount < MaxHitMemory) HitMemory[HitCount++] = e;
        }
    }
}

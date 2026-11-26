using UnityEngine;

namespace RunChildRun
{
    public enum EnemyBehaviour { Chase, Ranged, Swarm, Exploder, Shielded }

    /// <summary>Base stats of one enemy type (GDD 7 and 12). Distances in GDD units.</summary>
    [CreateAssetMenu(menuName = "Run Child Run/Enemy", fileName = "Enemy")]
    public sealed class EnemyDef : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public EnemyBehaviour Behaviour;
        [Tooltip("Prefab (variant of Enemy_Base) that carries the art.")]
        public Enemy Prefab;

        [Header("Base stats (GDD 12)")]
        public float Hp = 10f;
        [Tooltip("Fraction of player speed (0.7 = 70%).")]
        public float SpeedPercent = 0.7f;
        public float ContactDamage = 10f;
        public float Radius = 45f;
        public int Exp = 1;
        [Range(0f, 1f)] public float KnockbackResistance;
        public float Mass = 1f;

        [Header("Ranged (Spitter)")]
        public float AttackRange = 650f;
        public float AttackCooldown = 2.5f;
        public float ProjectileSpeed = 320f;
        public float ProjectileDamage = 8f;

        [Header("Exploder")]
        public float ExplodeTriggerRange = 110f;
        public float ExplodeRadius = 190f;
        public float ExplodeFuse = 0.45f;
        public float ExplodeDamage = 25f;

        [Header("Swarm (Bat)")]
        public float WaveAmplitude = 0.7f;
        public float WaveFrequency = 2.2f;

        [Header("Shielded")]
        public float ShieldArcDegrees = 120f;
        [Range(0f, 1f)] public float ShieldDamageMultiplier = 0.1f;
    }
}

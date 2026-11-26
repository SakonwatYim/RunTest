using System;
using UnityEngine;

namespace RunChildRun
{
    public enum SkillKind { BoltShot, Scatter, OrbitBlades, ShockwavePulse, ChainSpark, MineLayer }

    /// <summary>
    /// Stats for one skill level. Distances/speeds are GDD units. Each field's meaning depends on
    /// the skill (see tooltips); unused fields are ignored.
    /// </summary>
    [Serializable]
    public struct SkillLevelStats
    {
        public float Damage;
        [Tooltip("Seconds between shots / pulses / mines. Orbit Blades: re-hit interval per enemy.")]
        public float Cooldown;
        [Tooltip("Projectiles (Scatter), blades (Orbit), chain jumps (Chain Spark), max mines (Mine Layer).")]
        public int Count;
        [Tooltip("Targeting range (Bolt/Scatter/Chain), orbit radius (Orbit).")]
        public float Range;
        [Tooltip("Projectile radius, pulse radius, chain jump radius, blast radius.")]
        public float Area;
        [Tooltip("Projectile speed, orbit degrees per second.")]
        public float Speed;
        public int Pierce;
        [Tooltip("Knockback impulse (GDD units/s).")]
        public float Knockback;
        [Tooltip("Lv5 bonus flag (Shockwave damage+slow, Mine puddle).")]
        public bool Special;
    }

    /// <summary>One attack skill (GDD 6). Runtime state lives in <see cref="ActiveSkill"/>.</summary>
    [CreateAssetMenu(menuName = "Run Child Run/Skill", fileName = "Skill")]
    public sealed class SkillDef : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public Sprite Icon;
        public Color Color = Color.white;
        public SkillKind Kind;
        [Tooltip("Can be the guaranteed first item.")]
        public bool InStarterPool;
        [Tooltip("Must be unlocked in the Hub before it can drop.")]
        public bool RequiresUnlock;
        public int UnlockCost = 600;
        [Tooltip("Index 0 = Lv1 ... index 4 = Lv5. Values are absolute, not deltas.")]
        public SkillLevelStats[] Levels = new SkillLevelStats[5];
        [TextArea] public string[] LevelDescriptions = new string[5];

        public SkillLevelStats GetLevel(int level)
        {
            if (Levels == null || Levels.Length == 0) return default;
            return Levels[Mathf.Clamp(level - 1, 0, Levels.Length - 1)];
        }
    }
}

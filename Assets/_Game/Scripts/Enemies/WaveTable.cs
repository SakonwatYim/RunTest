using System;
using UnityEngine;

namespace RunChildRun
{
    [Serializable]
    public struct DifficultyBracket
    {
        [Tooltip("Run time in seconds when this bracket starts.")]
        public float StartTime;
        [Tooltip("Enemies per second.")]
        public float SpawnRate;
        public float SpeedMultiplier;
        public float HpMultiplier;
    }

    [Serializable]
    public struct EnemySpawnEntry
    {
        public EnemyDef Enemy;
        [Tooltip("Run time in seconds when this enemy starts appearing.")]
        public float UnlockTime;
        public float Weight;
    }

    public struct Difficulty
    {
        public float SpawnRate;
        public float SpeedMultiplier;
        public float HpMultiplier;
    }

    /// <summary>Time-based difficulty (GDD 12) and enemy unlock schedule (GDD 7).</summary>
    [CreateAssetMenu(menuName = "Run Child Run/Wave Table", fileName = "WaveTable")]
    public sealed class WaveTable : ScriptableObject
    {
        public DifficultyBracket[] Brackets =
        {
            new DifficultyBracket { StartTime = 0f, SpawnRate = 0.4f, SpeedMultiplier = 1f, HpMultiplier = 1f },
            new DifficultyBracket { StartTime = 15f, SpawnRate = 1f, SpeedMultiplier = 1f, HpMultiplier = 1f },
            new DifficultyBracket { StartTime = 120f, SpawnRate = 2.5f, SpeedMultiplier = 1.1f, HpMultiplier = 1.5f },
            new DifficultyBracket { StartTime = 300f, SpawnRate = 5f, SpeedMultiplier = 1.2f, HpMultiplier = 2.5f },
            new DifficultyBracket { StartTime = 480f, SpawnRate = 8f, SpeedMultiplier = 1.3f, HpMultiplier = 4f },
            new DifficultyBracket { StartTime = 720f, SpawnRate = 12f, SpeedMultiplier = 1.4f, HpMultiplier = 6f },
        };

        [Tooltip("After the last bracket starts, these grow every minute with no ceiling.")]
        public float SpawnRatePerMinuteAfterLast = 1f;
        public float HpPerMinuteAfterLast = 0.5f;

        public EnemySpawnEntry[] Enemies = Array.Empty<EnemySpawnEntry>();

        [Header("Early game (GDD 7)")]
        public float EarlyWindowEnd = 15f;
        public int EarlyMaxEnemies = 6;

        [Header("Elite")]
        public float EliteUnlockTime = 180f;
        [Range(0f, 1f)] public float EliteChance = 0.03f;

        [Header("Spawn patterns")]
        [Tooltip("Seconds between spawn pulses; each pulse spends the accumulated budget.")]
        public float PulseInterval = 2f;
        [Range(0f, 1f)] public float RingChance = 0.2f;
        [Range(0f, 1f)] public float StreamChance = 0.2f;
        [Tooltip("Ring and Stream need at least this many enemies in the pulse.")]
        public int MinGroupSize = 5;

        public Difficulty Evaluate(float time) => DifficultyCurve.Evaluate(Brackets, SpawnRatePerMinuteAfterLast, HpPerMinuteAfterLast, time);
    }

    public static class DifficultyCurve
    {
        public static Difficulty Evaluate(DifficultyBracket[] brackets, float spawnPerMinute, float hpPerMinute, float time)
        {
            if (brackets == null || brackets.Length == 0)
                return new Difficulty { SpawnRate = 1f, SpeedMultiplier = 1f, HpMultiplier = 1f };

            var index = 0;
            for (var i = 0; i < brackets.Length; i++)
            {
                if (time >= brackets[i].StartTime) index = i;
            }
            var b = brackets[index];
            var d = new Difficulty { SpawnRate = b.SpawnRate, SpeedMultiplier = b.SpeedMultiplier, HpMultiplier = b.HpMultiplier };
            if (index == brackets.Length - 1 && time > b.StartTime)
            {
                var minutes = (time - b.StartTime) / 60f;
                d.SpawnRate += spawnPerMinute * minutes;
                d.HpMultiplier += hpPerMinute * minutes;
            }
            return d;
        }

        /// <summary>GDD 12: base speed x multiplier, but never above 105% of the player.</summary>
        public static float EnemySpeed(float baseSpeedPercent, float speedMultiplier, float playerSpeed, float cap)
        {
            return Mathf.Min(baseSpeedPercent * speedMultiplier, cap) * playerSpeed;
        }
    }
}

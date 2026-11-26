using System.Collections.Generic;
using UnityEngine;

namespace RunChildRun
{
    public enum RunPhase { Playing, LevelUp, Paused, Reviving, GameOver }

    /// <summary>All per-run numbers that are not owned by a single system.</summary>
    public sealed class RunState
    {
        public float Elapsed;
        public int Level = 1;
        public float Exp;
        public int PendingLevelUps;
        public int Kills;
        public int RerollsLeft;
        public bool Revived;
        public bool FirstItemCollected;
        public int ItemsSpawned;

        /// <summary>Highest level each skill reached this run, kept after it expires (CLAUDE.md).</summary>
        public readonly Dictionary<string, int> SkillLevels = new Dictionary<string, int>();

        /// <summary>Adds EXP and queues level-ups. Returns how many levels were gained.</summary>
        public int AddExp(float amount, GameConfig config)
        {
            if (amount <= 0f) return 0;
            Exp += amount;
            var gained = 0;
            var need = ExpCurve.ExpToNext(Level, config);
            while (Exp >= need)
            {
                Exp -= need;
                Level++;
                gained++;
                need = ExpCurve.ExpToNext(Level, config);
            }
            PendingLevelUps += gained;
            return gained;
        }

        public float ExpProgress01(GameConfig config) => Mathf.Clamp01(Exp / ExpCurve.ExpToNext(Level, config));

        /// <summary>GDD 8: ~20 coins per minute survived + 1 coin per 5 kills.</summary>
        public static int CoinsFor(float seconds, int kills, float coinsPerMinute, int killsPerCoin)
        {
            var fromTime = Mathf.FloorToInt(seconds / 60f * coinsPerMinute);
            var fromKills = killsPerCoin > 0 ? kills / killsPerCoin : 0;
            return fromTime + fromKills;
        }

        public static string FormatTime(float seconds)
        {
            var total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}

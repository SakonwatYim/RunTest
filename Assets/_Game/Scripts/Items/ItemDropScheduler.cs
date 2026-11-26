using System;

namespace RunChildRun
{
    public enum ItemSpawnReason
    {
        None,
        /// <summary>Guaranteed starter item at ~12 s. Never expires.</summary>
        First,
        /// <summary>Guaranteed attack skill at ~40 s.</summary>
        Second,
        /// <summary>Bad-luck protection: unarmed for over 20 s with nothing on the ground.</summary>
        BadLuck,
        /// <summary>Regular random item every 25-40 s.</summary>
        Random,
    }

    /// <summary>
    /// Pure timing rules for item drops (GDD 5). The caller supplies run time and state each
    /// frame, tries to place the item it is told to spawn, and calls <see cref="Postpone"/> when
    /// no valid spawn point exists.
    /// </summary>
    public sealed class ItemDropScheduler
    {
        readonly float firstTime;
        readonly float secondTime;
        readonly float intervalMin;
        readonly float intervalMax;
        readonly float badLuckTime;
        readonly float retryDelay;
        readonly Func<float> random01;

        bool firstSpawned;
        bool secondSpawned;
        float nextRandomTime = float.MaxValue;
        float unarmedTimer;
        float retryAt = -1f;
        ItemSpawnReason pending = ItemSpawnReason.None;

        public bool FirstSpawned => firstSpawned;
        public float UnarmedTimer => unarmedTimer;
        public float NextRandomTime => nextRandomTime;

        public ItemDropScheduler(float firstTime, float secondTime, float intervalMin, float intervalMax,
            float badLuckTime, float retryDelay, Func<float> random01)
        {
            this.firstTime = firstTime;
            this.secondTime = secondTime;
            this.intervalMin = intervalMin;
            this.intervalMax = intervalMax;
            this.badLuckTime = badLuckTime;
            this.retryDelay = retryDelay;
            this.random01 = random01;
        }

        public static ItemDropScheduler FromConfig(GameConfig c, Func<float> random01) =>
            new ItemDropScheduler(c.FirstItemTime, c.SecondItemTime, c.RandomItemIntervalMin,
                c.RandomItemIntervalMax, c.BadLuckUnarmedTime, c.ItemRetryDelay, random01);

        /// <param name="time">Run time in seconds (does not advance while paused).</param>
        /// <param name="dt">Delta time since last call.</param>
        /// <param name="firstCollected">The player has picked up the first item.</param>
        /// <param name="anySkillActive">At least one attack skill is running.</param>
        /// <param name="itemsOnGround">Number of uncollected items in the arena.</param>
        public ItemSpawnReason Update(float time, float dt, bool firstCollected, bool anySkillActive, int itemsOnGround)
        {
            if (firstCollected && !anySkillActive) unarmedTimer += dt;
            else unarmedTimer = 0f;

            if (pending != ItemSpawnReason.None)
            {
                if (time < retryAt) return ItemSpawnReason.None;
                return pending;
            }

            if (!firstSpawned)
            {
                return time >= firstTime ? ItemSpawnReason.First : ItemSpawnReason.None;
            }

            if (!secondSpawned && time >= secondTime) return ItemSpawnReason.Second;

            if (firstCollected && unarmedTimer > badLuckTime && itemsOnGround == 0) return ItemSpawnReason.BadLuck;

            if (secondSpawned && time >= nextRandomTime) return ItemSpawnReason.Random;

            return ItemSpawnReason.None;
        }

        /// <summary>The requested item was placed in the arena.</summary>
        public void MarkSpawned(ItemSpawnReason reason, float time)
        {
            pending = ItemSpawnReason.None;
            switch (reason)
            {
                case ItemSpawnReason.First:
                    firstSpawned = true;
                    break;
                case ItemSpawnReason.Second:
                    secondSpawned = true;
                    ScheduleNextRandom(time);
                    break;
                case ItemSpawnReason.BadLuck:
                    unarmedTimer = 0f;
                    break;
                case ItemSpawnReason.Random:
                    ScheduleNextRandom(time);
                    break;
            }
        }

        /// <summary>No valid spawn point: try the same request again after the retry delay.</summary>
        public void Postpone(ItemSpawnReason reason, float time)
        {
            pending = reason;
            retryAt = time + retryDelay;
        }

        void ScheduleNextRandom(float time)
        {
            var r = random01();
            nextRandomTime = time + intervalMin + (intervalMax - intervalMin) * r;
        }
    }
}

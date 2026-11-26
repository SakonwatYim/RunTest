using System.Collections.Generic;

namespace RunChildRun
{
    /// <summary>Runtime state of a borrowed weapon: level and time left. Separate from <see cref="SkillDef"/>.</summary>
    public sealed class ActiveSkill
    {
        public SkillDef Def;
        public int Level;
        public float RemainingTime;
        public float Duration;
        public Rarity Rarity;

        public float Remaining01 => Duration > 0f ? RemainingTime / Duration : 0f;
    }

    public enum SkillPickupKind
    {
        /// <summary>Never had it this run: starts at Lv1.</summary>
        New,
        /// <summary>Picked up while active: timer refilled, level +1.</summary>
        Refreshed,
        /// <summary>Had it before, it expired: starts at remembered level +1.</summary>
        Relearned,
    }

    public struct SkillPickupResult
    {
        public SkillPickupKind Kind;
        public ActiveSkill Skill;
        public int PreviousLevel;
        public int NewLevel;
        /// <summary>Skill that was pushed out because all slots were full (its level stays remembered).</summary>
        public ActiveSkill Replaced;

        public bool LeveledUp => NewLevel > PreviousLevel && PreviousLevel > 0;
    }

    /// <summary>
    /// The borrowed-weapon rules (CLAUDE.md "กฎ Item ชั่วคราว", GDD 5):
    /// up to N concurrent skills, each with its own timer; duplicates refill + level up;
    /// levels are remembered for the whole run; a full bar replaces the skill with least time left.
    /// </summary>
    public sealed class SkillInventory
    {
        readonly int maxSlots;
        readonly int maxLevel;
        readonly Dictionary<string, int> rememberedLevels;
        readonly List<ActiveSkill> active = new List<ActiveSkill>(8);

        public IReadOnlyList<ActiveSkill> Active => active;
        public int MaxSlots => maxSlots;
        public bool AnyActive => active.Count > 0;

        /// <param name="rememberedLevels">Usually <c>RunState.SkillLevels</c>; kept for the whole run.</param>
        public SkillInventory(int maxSlots, int maxLevel, Dictionary<string, int> rememberedLevels)
        {
            this.maxSlots = maxSlots;
            this.maxLevel = maxLevel;
            this.rememberedLevels = rememberedLevels ?? new Dictionary<string, int>();
        }

        public int GetRememberedLevel(SkillDef def) =>
            def != null && rememberedLevels.TryGetValue(def.Id, out var l) ? l : 0;

        public ActiveSkill Find(SkillDef def)
        {
            for (var i = 0; i < active.Count; i++)
            {
                if (active[i].Def == def) return active[i];
            }
            return null;
        }

        public SkillPickupResult Pickup(SkillDef def, float duration, Rarity rarity = Rarity.Common)
        {
            var existing = Find(def);
            if (existing != null)
            {
                var prev = existing.Level;
                existing.Level = System.Math.Min(prev + 1, maxLevel);
                existing.Duration = duration;
                existing.RemainingTime = duration;
                existing.Rarity = rarity;
                rememberedLevels[def.Id] = existing.Level;
                return new SkillPickupResult
                {
                    Kind = SkillPickupKind.Refreshed, Skill = existing, PreviousLevel = prev, NewLevel = existing.Level,
                };
            }

            var remembered = GetRememberedLevel(def);
            var level = remembered <= 0 ? 1 : System.Math.Min(remembered + 1, maxLevel);

            ActiveSkill replaced = null;
            if (active.Count >= maxSlots)
            {
                var minIndex = 0;
                for (var i = 1; i < active.Count; i++)
                {
                    if (active[i].RemainingTime < active[minIndex].RemainingTime) minIndex = i;
                }
                replaced = active[minIndex];
                active.RemoveAt(minIndex);
            }

            var skill = new ActiveSkill
            {
                Def = def, Level = level, Duration = duration, RemainingTime = duration, Rarity = rarity,
            };
            active.Add(skill);
            rememberedLevels[def.Id] = level;
            return new SkillPickupResult
            {
                Kind = remembered <= 0 ? SkillPickupKind.New : SkillPickupKind.Relearned,
                Skill = skill,
                PreviousLevel = remembered,
                NewLevel = level,
                Replaced = replaced,
            };
        }

        /// <summary>Counts every timer down. Expired skills are removed and appended to <paramref name="expired"/>.</summary>
        public void Tick(float dt, List<ActiveSkill> expired)
        {
            for (var i = active.Count - 1; i >= 0; i--)
            {
                var s = active[i];
                s.RemainingTime -= dt;
                if (s.RemainingTime <= 0f)
                {
                    s.RemainingTime = 0f;
                    active.RemoveAt(i);
                    expired?.Add(s);
                }
            }
        }
    }
}

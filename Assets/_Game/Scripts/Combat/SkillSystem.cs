using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>Up to 5 borrowed weapons at once, each with its own timer (paused with Time.timeScale).</summary>
    public sealed class SkillSystem : MonoBehaviour
    {
        RunContext ctx;
        SkillInventory inventory;
        readonly Dictionary<ActiveSkill, SkillRuntime> runtimes = new Dictionary<ActiveSkill, SkillRuntime>();
        readonly List<ActiveSkill> expired = new List<ActiveSkill>(5);

        public SkillInventory Inventory => inventory;
        public bool AnyActive => inventory != null && inventory.AnyActive;

        public event Action<SkillPickupResult> SkillPicked;
        public event Action<ActiveSkill> SkillExpired;
        public event Action Changed;

        public void Init(RunContext context)
        {
            ctx = context;
            inventory = new SkillInventory(ctx.Config.MaxActiveSkills, ctx.Config.MaxSkillLevel, ctx.Run.State.SkillLevels);
        }

        public SkillPickupResult Pickup(SkillDef def, Rarity rarity)
        {
            var duration = RarityTable.SkillDuration(rarity, ctx.Config.RaritySkillDurations);
            var result = inventory.Pickup(def, duration, rarity);

            if (result.Replaced != null && runtimes.TryGetValue(result.Replaced, out var old))
            {
                old.OnDeactivate();
                runtimes.Remove(result.Replaced);
            }
            if (!runtimes.ContainsKey(result.Skill))
            {
                var runtime = SkillRuntime.Create(def.Kind);
                runtime.Bind(ctx, result.Skill);
                runtimes.Add(result.Skill, runtime);
            }

            SkillPicked?.Invoke(result);
            Changed?.Invoke();
            return result;
        }

        void Update()
        {
            if (ctx == null || ctx.Run.Phase != RunPhase.Playing || inventory == null) return;
            var dt = Time.deltaTime;

            expired.Clear();
            inventory.Tick(dt, expired);
            for (var i = 0; i < expired.Count; i++)
            {
                if (runtimes.TryGetValue(expired[i], out var r))
                {
                    r.OnDeactivate();
                    runtimes.Remove(expired[i]);
                }
                SkillExpired?.Invoke(expired[i]);
            }
            if (expired.Count > 0) Changed?.Invoke();

            var active = inventory.Active;
            for (var i = 0; i < active.Count; i++)
            {
                if (runtimes.TryGetValue(active[i], out var r)) r.Tick(dt);
            }
        }
    }
}

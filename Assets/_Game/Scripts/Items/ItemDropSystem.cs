using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>
    /// Places gift boxes (first at 12 s, second at ~40 s, then every 25-40 s, bad-luck protection,
    /// Elite drops), expires them after 45 s and applies their contents on pickup (GDD 5).
    /// </summary>
    public sealed class ItemDropSystem : MonoBehaviour
    {
        [SerializeField] ItemPickup prefab;
        [SerializeField] Transform poolRoot;
        [SerializeField] int prewarm = 8;

        RunContext ctx;
        ItemDropScheduler scheduler;
        ComponentPool<ItemPickup> pool;
        ItemSpawnRules rules;
        readonly List<ItemPickup> active = new List<ItemPickup>(8);
        readonly List<Vector2> enemyPositions = new List<Vector2>(256);
        readonly List<SkillDef> skillChoices = new List<SkillDef>(8);
        readonly List<StatKind> statChoices = new List<StatKind>(7);
        readonly Func<float> random01 = () => UnityEngine.Random.value;
        int pendingEliteDrops;

        public IReadOnlyList<ItemPickup> Active => active;

        public event Action<ItemPickup> ItemSpawned;
        public event Action<ItemPickup> ItemExpired;

        public void Init(RunContext context)
        {
            ctx = context;
            scheduler = ItemDropScheduler.FromConfig(ctx.Config, random01);
            rules = ItemSpawnRules.FromConfig(ctx.Config);
            pool = new ComponentPool<ItemPickup>(prefab, poolRoot != null ? poolRoot : transform, prewarm);
            ctx.Enemies.EnemyKilled += OnEnemyKilled;
        }

        void OnDestroy()
        {
            if (ctx != null && ctx.Enemies != null) ctx.Enemies.EnemyKilled -= OnEnemyKilled;
        }

        void OnEnemyKilled(Enemy e, bool rewarded)
        {
            if (rewarded && e.IsElite && UnityEngine.Random.value < ctx.Config.EliteItemDropChance) pendingEliteDrops++;
        }

        void Update()
        {
            if (ctx == null || ctx.Run.Phase != RunPhase.Playing) return;
            var dt = Time.deltaTime;
            var state = ctx.Run.State;

            var reason = scheduler.Update(state.Elapsed, dt, state.FirstItemCollected, ctx.Skills.AnyActive, active.Count);
            if (reason != ItemSpawnReason.None)
            {
                if (TrySpawn(reason)) scheduler.MarkSpawned(reason, state.Elapsed);
                else scheduler.Postpone(reason, state.Elapsed);
            }
            else if (pendingEliteDrops > 0 && TrySpawn(ItemSpawnReason.Random))
            {
                pendingEliteDrops--;
            }

            var player = ctx.Player.Position;
            var reach = ctx.W(ctx.Config.PickupRadius) + ctx.Player.Radius;
            for (var i = active.Count - 1; i >= 0; i--)
            {
                var item = active[i];
                if ((item.Position - player).sqrMagnitude <= reach * reach)
                {
                    Collect(i);
                    continue;
                }
                if (!item.Expires) continue;
                item.Remaining -= dt;
                if (item.Remaining <= 0f)
                {
                    ItemExpired?.Invoke(item);
                    Remove(i);
                }
            }
        }

        bool TrySpawn(ItemSpawnReason reason)
        {
            ctx.Enemies.FillPositions(enemyPositions);
            if (!ItemSpawnPicker.TryPick(ctx.Player.Position, enemyPositions, ctx.Arena.Geometry, ctx.Arena.Obstacles,
                    rules, random01, out var pos, out _))
            {
                return false;
            }

            var c = ctx.Config;
            var state = ctx.Run.State;
            var kind = ChooseKind(reason);
            var rarity = reason == ItemSpawnReason.First
                ? Rarity.Common
                : RarityTable.Roll(c.RarityWeights, ctx.Run.Stats.LuckLevel, c.LuckRareWeightPerLevel, c.LuckEpicWeightPerLevel, random01());

            SkillDef skill = null;
            var stat = StatKind.MaxHp;
            Sprite icon = null;
            Color color;
            switch (kind)
            {
                case ItemKind.Skill:
                    skill = PickSkill(reason == ItemSpawnReason.First);
                    if (skill == null) return false;
                    icon = skill.Icon;
                    color = c.RarityColors[Mathf.Clamp((int)rarity, 0, c.RarityColors.Length - 1)];
                    break;
                case ItemKind.Stat:
                    ctx.Run.Stats.PickChoices(1, random01, statChoices);
                    stat = statChoices[0];
                    icon = ctx.Run.Stats.GetDef(stat)?.Icon;
                    color = c.StatItemColor;
                    break;
                default:
                    color = c.HealItemColor;
                    break;
            }

            var isFirst = reason == ItemSpawnReason.First;
            var item = pool.Get(pos);
            item.Setup(kind, rarity, skill, stat, isFirst, c.ItemGroundLifetime, color, icon);
            active.Add(item);
            state.ItemsSpawned++;
            ItemSpawned?.Invoke(item);
            return true;
        }

        ItemKind ChooseKind(ItemSpawnReason reason)
        {
            if (reason != ItemSpawnReason.Random) return ItemKind.Skill;
            var c = ctx.Config;
            var canStat = HasLevelableStat();
            if (ctx.Run.State.ItemsSpawned < c.EarlySkillWeightedItems)
            {
                return UnityEngine.Random.value < c.EarlySkillChance || !canStat ? ItemKind.Skill : ItemKind.Stat;
            }
            var heal = c.HealItemWeight * (ctx.PlayerHealth.Hp01 < c.LowHpThreshold ? c.LowHpHealWeightMultiplier : 1f);
            var stat = canStat ? c.StatItemWeight : 0f;
            var total = c.SkillItemWeight + stat + heal;
            var r = UnityEngine.Random.value * total;
            if (r < c.SkillItemWeight) return ItemKind.Skill;
            if (r < c.SkillItemWeight + stat) return ItemKind.Stat;
            return ItemKind.Heal;
        }

        bool HasLevelableStat()
        {
            for (var i = 0; i < StatBlock.StatCount; i++)
            {
                if (ctx.Run.Stats.CanLevel((StatKind)i)) return true;
            }
            return false;
        }

        SkillDef PickSkill(bool starterOnly)
        {
            skillChoices.Clear();
            var save = SaveSystem.Data;
            foreach (var s in ctx.SkillDefs)
            {
                if (s == null) continue;
                if (starterOnly && !s.InStarterPool) continue;
                if (!MetaProgression.IsSkillAvailable(save, s)) continue;
                skillChoices.Add(s);
            }
            return skillChoices.Count == 0 ? null : skillChoices[UnityEngine.Random.Range(0, skillChoices.Count)];
        }

        void Collect(int index)
        {
            var item = active[index];
            var run = ctx.Run;
            switch (item.Kind)
            {
                case ItemKind.Skill:
                    var result = ctx.Skills.Pickup(item.Skill, item.Rarity);
                    if (result.LeveledUp)
                        ctx.Hud.ShowToast($"{item.Skill.DisplayName}  Lv {result.PreviousLevel} → {result.NewLevel}", item.Color);
                    else
                        ctx.Hud.ShowToast($"{item.Skill.DisplayName}  Lv {result.NewLevel}", item.Color);
                    break;
                case ItemKind.Stat:
                    if (run.Stats.LevelUp(item.Stat)) run.ApplyStatChange(item.Stat);
                    ctx.Hud.ShowToast($"{run.Stats.GetDef(item.Stat).DisplayName} +1", item.Color);
                    break;
                case ItemKind.Heal:
                    ctx.PlayerHealth.Heal(ctx.PlayerHealth.MaxHp * ctx.Config.HealAmountFraction);
                    ctx.Hud.ShowToast("HP +", item.Color);
                    break;
            }

            var wasFirst = !run.State.FirstItemCollected && item.Kind == ItemKind.Skill;
            if (wasFirst) run.OnFirstItemCollected();
            ctx.Audio.Play(ctx.Sfx != null ? (wasFirst ? ctx.Sfx.FirstPickup : ctx.Sfx.Pickup) : null);
            Remove(index);
        }

        void Remove(int index)
        {
            var item = active[index];
            active.RemoveAt(index);
            pool.Release(item);
        }
    }
}

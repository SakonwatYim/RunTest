using UnityEngine;

namespace RunChildRun
{
    public enum MetaUpgradeKind { StartingHp, MoveSpeed, ExpGain, DashCooldown }

    /// <summary>Permanent Hub upgrade (GDD 8). All four together stay within the ~25% cap.</summary>
    [CreateAssetMenu(menuName = "Run Child Run/Meta Upgrade", fileName = "Upgrade")]
    public sealed class MetaUpgradeDef : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public Sprite Icon;
        public MetaUpgradeKind Kind;
        [Tooltip("Fraction per level: 0.02 = +2% (Dash Cooldown: -2%).")]
        public float BonusPerLevel = 0.02f;
        [Tooltip("Coin cost of each level; length = max level.")]
        public int[] Costs = { 100, 200, 350, 550, 800 };

        public int MaxLevel => Costs != null ? Costs.Length : 0;
        public int CostForNext(int currentLevel) =>
            Costs != null && currentLevel < Costs.Length ? Costs[currentLevel] : -1;
    }

    /// <summary>Modifiers a run reads from meta-progression.</summary>
    public struct MetaModifiers
    {
        public float HpMultiplier;
        public float MoveSpeedMultiplier;
        public float ExpGainMultiplier;
        public float DashCooldownMultiplier;

        public static MetaModifiers None => new MetaModifiers
        {
            HpMultiplier = 1f, MoveSpeedMultiplier = 1f, ExpGainMultiplier = 1f, DashCooldownMultiplier = 1f,
        };
    }

    public static class MetaProgression
    {
        public static MetaModifiers GetModifiers(SaveData save, MetaUpgradeDef[] upgrades)
        {
            var m = MetaModifiers.None;
            if (upgrades == null) return m;
            foreach (var u in upgrades)
            {
                if (u == null) continue;
                var bonus = save.GetUpgradeLevel(u.Id) * u.BonusPerLevel;
                switch (u.Kind)
                {
                    case MetaUpgradeKind.StartingHp: m.HpMultiplier += bonus; break;
                    case MetaUpgradeKind.MoveSpeed: m.MoveSpeedMultiplier += bonus; break;
                    case MetaUpgradeKind.ExpGain: m.ExpGainMultiplier += bonus; break;
                    case MetaUpgradeKind.DashCooldown: m.DashCooldownMultiplier = Mathf.Max(0.5f, m.DashCooldownMultiplier - bonus); break;
                }
            }
            return m;
        }

        public static bool TryBuyUpgrade(SaveData save, MetaUpgradeDef def)
        {
            var level = save.GetUpgradeLevel(def.Id);
            var cost = def.CostForNext(level);
            if (cost < 0 || save.Coins < cost) return false;
            save.Coins -= cost;
            save.SetUpgradeLevel(def.Id, level + 1);
            return true;
        }

        public static bool IsSkillAvailable(SaveData save, SkillDef def) =>
            def != null && (!def.RequiresUnlock || save.UnlockedSkills.Contains(def.Id));

        public static bool TryUnlockSkill(SaveData save, SkillDef def)
        {
            if (def == null || IsSkillAvailable(save, def) || save.Coins < def.UnlockCost) return false;
            save.Coins -= def.UnlockCost;
            save.UnlockedSkills.Add(def.Id);
            return true;
        }
    }
}

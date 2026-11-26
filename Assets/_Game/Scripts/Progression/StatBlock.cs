using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>Permanent-for-the-run stat levels and the multipliers they produce.</summary>
    public sealed class StatBlock
    {
        public const int StatCount = 7;

        readonly StatDef[] defs = new StatDef[StatCount];
        readonly int[] levels = new int[StatCount];

        public event Action<StatKind> Changed;

        public StatBlock(IReadOnlyList<StatDef> statDefs)
        {
            if (statDefs == null) return;
            for (var i = 0; i < statDefs.Count; i++)
            {
                var d = statDefs[i];
                if (d != null) defs[(int)d.Kind] = d;
            }
        }

        public StatDef GetDef(StatKind kind) => defs[(int)kind];
        public int GetLevel(StatKind kind) => levels[(int)kind];
        public int GetMaxLevel(StatKind kind) => defs[(int)kind] != null ? defs[(int)kind].MaxLevel : 0;
        public bool CanLevel(StatKind kind) => defs[(int)kind] != null && levels[(int)kind] < defs[(int)kind].MaxLevel;

        public bool LevelUp(StatKind kind)
        {
            if (!CanLevel(kind)) return false;
            levels[(int)kind]++;
            Changed?.Invoke(kind);
            return true;
        }

        float Total(StatKind kind) => defs[(int)kind] != null ? levels[(int)kind] * defs[(int)kind].ValuePerLevel : 0f;

        public float BonusMaxHp => Total(StatKind.MaxHp);
        public float MoveSpeedMultiplier => 1f + Total(StatKind.MoveSpeed);
        public float DamageMultiplier => 1f + Total(StatKind.Damage);
        /// <summary>Multiplies skill cooldowns; never below 0.4.</summary>
        public float CooldownMultiplier => Mathf.Max(0.4f, 1f - Total(StatKind.AttackSpeed));
        public float AreaMultiplier => 1f + Total(StatKind.Area);
        public float RegenPerSecond => Total(StatKind.HpRegen);
        public int LuckLevel => levels[(int)StatKind.Luck];

        /// <summary>Fills <paramref name="result"/> with up to <paramref name="count"/> distinct stats that can still level.</summary>
        public void PickChoices(int count, Func<float> random01, List<StatKind> result)
        {
            result.Clear();
            Span<int> pool = stackalloc int[StatCount];
            var n = 0;
            for (var i = 0; i < StatCount; i++)
            {
                if (CanLevel((StatKind)i)) pool[n++] = i;
            }
            while (result.Count < count && n > 0)
            {
                var pick = Mathf.Min((int)(random01() * n), n - 1);
                result.Add((StatKind)pool[pick]);
                pool[pick] = pool[n - 1];
                n--;
            }
        }
    }
}

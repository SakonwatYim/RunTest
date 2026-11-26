using UnityEngine;

namespace RunChildRun
{
    public enum StatKind { MaxHp = 0, MoveSpeed = 1, Damage = 2, AttackSpeed = 3, Area = 4, HpRegen = 5, Luck = 6 }

    /// <summary>One of the 7 run stats (GDD 6). Level-up cards and Stat items raise its level.</summary>
    [CreateAssetMenu(menuName = "Run Child Run/Stat", fileName = "Stat")]
    public sealed class StatDef : ScriptableObject
    {
        public StatKind Kind;
        public string DisplayName;
        public Sprite Icon;
        [Tooltip("{0} is replaced by ValuePerLevel (shown as % when IsPercent).")]
        public string DescriptionFormat = "+{0}";
        [Tooltip("GDD 4: max Lv5 (Luck lower).")]
        public int MaxLevel = 5;
        [Tooltip("Flat HP for MaxHp, HP/s for HpRegen, Luck steps for Luck, fraction (0.1 = 10%) otherwise.")]
        public float ValuePerLevel = 0.1f;
        public bool IsPercent = true;

        public string Describe()
        {
            var value = IsPercent ? $"{ValuePerLevel * 100f:0.#}%" : $"{ValuePerLevel:0.#}";
            return string.Format(DescriptionFormat, value);
        }
    }
}

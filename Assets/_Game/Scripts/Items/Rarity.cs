using UnityEngine;

namespace RunChildRun
{
    public enum Rarity { Common = 0, Rare = 1, Epic = 2 }

    public enum ItemKind { Skill, Stat, Heal }

    /// <summary>GDD 5 rarity table, shifted by the Luck stat.</summary>
    public static class RarityTable
    {
        /// <param name="weights">Common, Rare, Epic base weights.</param>
        /// <param name="roll01">Uniform random value in [0,1).</param>
        public static Rarity Roll(float[] weights, int luckLevel, float rarePerLuck, float epicPerLuck, float roll01)
        {
            GetWeights(weights, luckLevel, rarePerLuck, epicPerLuck, out var common, out var rare, out var epic);
            var total = common + rare + epic;
            if (total <= 0f) return Rarity.Common;
            var r = Mathf.Clamp01(roll01) * total;
            if (r < epic) return Rarity.Epic;
            if (r < epic + rare) return Rarity.Rare;
            return Rarity.Common;
        }

        public static void GetWeights(float[] weights, int luckLevel, float rarePerLuck, float epicPerLuck,
            out float common, out float rare, out float epic)
        {
            common = weights.Length > 0 ? weights[0] : 0f;
            rare = weights.Length > 1 ? weights[1] : 0f;
            epic = weights.Length > 2 ? weights[2] : 0f;
            var addRare = Mathf.Max(0, luckLevel) * rarePerLuck;
            var addEpic = Mathf.Max(0, luckLevel) * epicPerLuck;
            var moved = Mathf.Min(common, addRare + addEpic);
            var scale = addRare + addEpic > 0f ? moved / (addRare + addEpic) : 0f;
            common -= moved;
            rare += addRare * scale;
            epic += addEpic * scale;
        }

        public static float SkillDuration(Rarity rarity, float[] durations)
        {
            var i = (int)rarity;
            if (durations == null || durations.Length == 0) return 45f;
            return durations[Mathf.Clamp(i, 0, durations.Length - 1)];
        }
    }
}

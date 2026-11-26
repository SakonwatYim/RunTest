using UnityEngine;

namespace RunChildRun
{
    /// <summary>GDD 4: EXP_next(L) = 8 + 6L + 0.35L^2, plus time-based EXP rates.</summary>
    public static class ExpCurve
    {
        public static float ExpToNext(int level, float baseExp, float linear, float quadratic)
        {
            level = Mathf.Max(1, level);
            return baseExp + linear * level + quadratic * level * level;
        }

        public static float ExpToNext(int level, GameConfig config) =>
            ExpToNext(level, config.ExpBase, config.ExpLinear, config.ExpQuadratic);

        /// <summary>1/s with no active skill, 0.25/s while any skill is active.</summary>
        public static float TimeExpRate(bool anySkillActive, float unarmedRate, float armedRate) =>
            anySkillActive ? armedRate : unarmedRate;
    }
}

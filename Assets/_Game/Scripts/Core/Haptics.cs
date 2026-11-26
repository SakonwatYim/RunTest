using UnityEngine;

namespace RunChildRun
{
    /// <summary>Light vibration on hit / level up (GDD 10). Off when disabled in Settings.</summary>
    public static class Haptics
    {
        public static void Light()
        {
            if (!SaveSystem.Data.Settings.Haptics) return;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}

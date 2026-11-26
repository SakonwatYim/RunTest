using System;
using System.Collections;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>Rewarded ad placements (GDD 11). Swap the stub for a real SDK adapter later.</summary>
    public interface IRewardedAdService
    {
        bool IsReady(string placement);
        /// <summary>Shows an ad; <paramref name="onComplete"/> gets true only when the reward is earned.</summary>
        void Show(string placement, Action<bool> onComplete);
    }

    public static class AdPlacements
    {
        public const string Revive = "revive";
        public const string DoubleCoins = "double_coins";
    }

    public static class Ads
    {
        static IRewardedAdService service;

        public static IRewardedAdService Rewarded
        {
            get => service ??= new StubRewardedAdService();
            set => service = value;
        }
    }

    /// <summary>Pretends to play an ad for a short real-time delay, then grants the reward.</summary>
    public sealed class StubRewardedAdService : IRewardedAdService
    {
        public float FakeDurationSeconds = 1f;

        public bool IsReady(string placement) => true;

        public void Show(string placement, Action<bool> onComplete)
        {
            Debug.Log($"[Ads stub] Showing rewarded ad '{placement}'");
            AdStubRunner.Instance.StartCoroutine(Complete(onComplete));
        }

        IEnumerator Complete(Action<bool> onComplete)
        {
            yield return new WaitForSecondsRealtime(FakeDurationSeconds);
            onComplete?.Invoke(true);
        }
    }

}

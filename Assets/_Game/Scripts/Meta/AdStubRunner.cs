using UnityEngine;

namespace RunChildRun
{
    /// <summary>Hidden coroutine host for <see cref="StubRewardedAdService"/>.</summary>
    public sealed class AdStubRunner : MonoBehaviour
    {
        static AdStubRunner instance;

        public static AdStubRunner Instance
        {
            get
            {
                if (instance != null) return instance;
                var go = new GameObject("[AdStubRunner]") { hideFlags = HideFlags.HideAndDontSave };
                DontDestroyOnLoad(go);
                instance = go.AddComponent<AdStubRunner>();
                return instance;
            }
        }
    }
}

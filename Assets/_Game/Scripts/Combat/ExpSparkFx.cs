using System;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>
    /// Purely visual EXP particle flying from a dead enemy to the player. The EXP itself was
    /// already granted when the enemy died (no orbs to collect).
    /// </summary>
    public sealed class ExpSparkFx : MonoBehaviour
    {
        [SerializeField] float duration = 0.35f;

        Vector2 start;
        Vector2 bend;
        Transform target;
        float t;
        Action<ExpSparkFx> release;

        public void Play(Vector2 from, Transform to, Action<ExpSparkFx> onDone)
        {
            start = from;
            target = to;
            bend = UnityEngine.Random.insideUnitCircle * 1.2f;
            t = 0f;
            release = onDone;
            transform.position = from;
        }

        void Update()
        {
            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / duration);
            var end = target != null ? (Vector2)target.position : start;
            var mid = Vector2.Lerp(start, end, 0.5f) + bend;
            var a = Vector2.Lerp(start, mid, k);
            var b = Vector2.Lerp(mid, end, k);
            transform.position = Vector2.Lerp(a, b, k * k);
            if (k >= 1f) release?.Invoke(this);
        }
    }
}

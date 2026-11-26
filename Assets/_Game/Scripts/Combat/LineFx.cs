using System;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>Short-lived lightning line for Chain Spark.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class LineFx : MonoBehaviour
    {
        [SerializeField] LineRenderer line;

        float duration;
        float t;
        Color color;
        Action<LineFx> release;

        public void Play(Vector3[] points, int count, float seconds, Color tint, Action<LineFx> onDone)
        {
            line.positionCount = count;
            line.SetPositions(points);
            duration = Mathf.Max(0.01f, seconds);
            color = tint;
            t = 0f;
            release = onDone;
            SetAlpha(1f);
        }

        void Update()
        {
            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / duration);
            SetAlpha(1f - k);
            if (k >= 1f) release?.Invoke(this);
        }

        void SetAlpha(float a)
        {
            var c = color;
            c.a *= a;
            line.startColor = c;
            line.endColor = c;
        }
    }
}

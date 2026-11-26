using System;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>Expanding, fading ring (shockwave, explosions, mine blasts).</summary>
    public sealed class RingFx : MonoBehaviour
    {
        [SerializeField] SpriteRenderer sprite;
        [Tooltip("World diameter of the sprite at scale 1.")]
        [SerializeField] float spriteDiameter = 1f;

        float r0;
        float r1;
        float duration;
        float t;
        Color color;
        Action<RingFx> release;

        public void Play(Vector2 position, float startRadius, float endRadius, float seconds, Color tint, Action<RingFx> onDone)
        {
            transform.position = position;
            r0 = startRadius;
            r1 = endRadius;
            duration = Mathf.Max(0.01f, seconds);
            color = tint;
            t = 0f;
            release = onDone;
            Apply(0f);
        }

        void Update()
        {
            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / duration);
            Apply(k);
            if (k >= 1f) release?.Invoke(this);
        }

        void Apply(float k)
        {
            var r = Mathf.Lerp(r0, r1, 1f - (1f - k) * (1f - k));
            var s = r * 2f / Mathf.Max(0.0001f, spriteDiameter);
            transform.localScale = new Vector3(s, s, 1f);
            var c = color;
            c.a *= 1f - k;
            sprite.color = c;
        }
    }
}

using System;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>Small particle puff (enemy death).</summary>
    public sealed class BurstFx : MonoBehaviour
    {
        [SerializeField] ParticleSystem particles;
        [SerializeField] float lifetime = 0.6f;

        float t;
        Action<BurstFx> release;

        public void Play(Vector2 position, Color tint, Action<BurstFx> onDone)
        {
            transform.position = position;
            var main = particles.main;
            main.startColor = tint;
            particles.Clear(true);
            particles.Play(true);
            t = 0f;
            release = onDone;
        }

        void Update()
        {
            t += Time.deltaTime;
            if (t >= lifetime) release?.Invoke(this);
        }
    }
}

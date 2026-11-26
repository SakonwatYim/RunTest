using System;
using UnityEngine;

namespace RunChildRun
{
    public enum SpriteFxKind { OrbitBlade, Mine, Puddle }

    /// <summary>Pools for every visual effect. Swap the prefabs' sprites to change the art.</summary>
    public sealed class FxSystem : MonoBehaviour
    {
        [SerializeField] Transform poolRoot;
        [SerializeField] RingFx ringPrefab;
        [SerializeField] LineFx linePrefab;
        [SerializeField] BurstFx burstPrefab;
        [SerializeField] ExpSparkFx expSparkPrefab;
        [SerializeField] PooledSprite orbitBladePrefab;
        [SerializeField] PooledSprite minePrefab;
        [SerializeField] PooledSprite puddlePrefab;

        RunContext ctx;
        ComponentPool<RingFx> rings;
        ComponentPool<LineFx> lines;
        ComponentPool<BurstFx> bursts;
        ComponentPool<ExpSparkFx> sparks;
        ComponentPool<PooledSprite> blades;
        ComponentPool<PooledSprite> mines;
        ComponentPool<PooledSprite> puddles;
        Action<RingFx> releaseRing;
        Action<LineFx> releaseLine;
        Action<BurstFx> releaseBurst;
        Action<ExpSparkFx> releaseSpark;
        int sparksThisFrame;
        int sparkFrame = -1;

        public void Init(RunContext context)
        {
            ctx = context;
            var root = poolRoot != null ? poolRoot : transform;
            rings = new ComponentPool<RingFx>(ringPrefab, root, 24);
            lines = new ComponentPool<LineFx>(linePrefab, root, 8);
            bursts = new ComponentPool<BurstFx>(burstPrefab, root, 32);
            sparks = new ComponentPool<ExpSparkFx>(expSparkPrefab, root, 32);
            blades = new ComponentPool<PooledSprite>(orbitBladePrefab, root, 6);
            mines = new ComponentPool<PooledSprite>(minePrefab, root, 16);
            puddles = new ComponentPool<PooledSprite>(puddlePrefab, root, 8);
            releaseRing = r => rings.Release(r);
            releaseLine = l => lines.Release(l);
            releaseBurst = b => bursts.Release(b);
            releaseSpark = s => sparks.Release(s);
        }

        public void Ring(Vector2 position, float startRadius, float endRadius, float seconds, Color color)
        {
            rings.Get(position).Play(position, startRadius, endRadius, seconds, color, releaseRing);
        }

        public void Line(Vector3[] points, int count, float seconds, Color color)
        {
            lines.Get(Vector3.zero).Play(points, count, seconds, color, releaseLine);
        }

        public void Burst(Vector2 position, Color color)
        {
            bursts.Get(position).Play(position, color, releaseBurst);
        }

        /// <summary>Visual-only EXP particle; capped per frame (GDD 4).</summary>
        public void ExpSpark(Vector2 from)
        {
            if (sparkFrame != Time.frameCount)
            {
                sparkFrame = Time.frameCount;
                sparksThisFrame = 0;
            }
            if (sparksThisFrame >= ctx.Config.MaxExpSparksPerFrame) return;
            sparksThisFrame++;
            sparks.Get(from).Play(from, ctx.Player.transform, releaseSpark);
        }

        public PooledSprite GetSprite(SpriteFxKind kind, Vector2 position) => Pool(kind).Get(position);

        public void ReleaseSprite(SpriteFxKind kind, PooledSprite sprite) => Pool(kind).Release(sprite);

        ComponentPool<PooledSprite> Pool(SpriteFxKind kind) => kind switch
        {
            SpriteFxKind.OrbitBlade => blades,
            SpriteFxKind.Mine => mines,
            _ => puddles,
        };
    }
}

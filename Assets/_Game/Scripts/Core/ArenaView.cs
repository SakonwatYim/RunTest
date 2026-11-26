using System.Collections.Generic;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>
    /// The octagon arena laid out in the Run scene. Walls are the EdgeCollider2D on this object;
    /// obstacles are child <see cref="Obstacle"/> prefab instances.
    /// </summary>
    public sealed class ArenaView : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] Transform obstacleRoot;

        ArenaGeometry geometry;
        readonly List<ObstacleCircle> obstacles = new List<ObstacleCircle>(16);

        public ArenaGeometry Geometry
        {
            get
            {
                if (geometry == null) Build();
                return geometry;
            }
        }

        public IReadOnlyList<ObstacleCircle> Obstacles
        {
            get
            {
                if (geometry == null) Build();
                return obstacles;
            }
        }

        void Build()
        {
            geometry = new ArenaGeometry(config.ToWorld(config.ArenaHalfWidth), config.ToWorld(config.ArenaHalfHeight),
                config.ToWorld(config.ArenaCornerCut));
            obstacles.Clear();
            var root = obstacleRoot != null ? obstacleRoot : transform;
            foreach (var o in root.GetComponentsInChildren<Obstacle>())
            {
                obstacles.Add(new ObstacleCircle(o.transform.position, o.WorldRadius));
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (config == null) return;
            var g = new ArenaGeometry(config.ToWorld(config.ArenaHalfWidth), config.ToWorld(config.ArenaHalfHeight),
                config.ToWorld(config.ArenaCornerCut));
            Gizmos.color = Color.yellow;
            for (var i = 0; i < g.Vertices.Length; i++)
            {
                Gizmos.DrawLine(g.Vertices[i], g.Vertices[(i + 1) % g.Vertices.Length]);
            }
        }
#endif
    }
}

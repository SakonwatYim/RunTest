using System.Collections.Generic;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>
    /// Convex octagon arena centred on (0,0): a rectangle with 45-degree cut corners (GDD 3).
    /// Units are whatever the caller passes in (world units at runtime).
    /// </summary>
    public sealed class ArenaGeometry
    {
        public readonly Vector2[] Vertices;
        public readonly float HalfWidth;
        public readonly float HalfHeight;

        public ArenaGeometry(float halfWidth, float halfHeight, float cornerCut)
        {
            HalfWidth = halfWidth;
            HalfHeight = halfHeight;
            var c = Mathf.Clamp(cornerCut, 0f, Mathf.Min(halfWidth, halfHeight));
            // Counter-clockwise.
            Vertices = new[]
            {
                new Vector2(halfWidth, -halfHeight + c),
                new Vector2(halfWidth, halfHeight - c),
                new Vector2(halfWidth - c, halfHeight),
                new Vector2(-halfWidth + c, halfHeight),
                new Vector2(-halfWidth, halfHeight - c),
                new Vector2(-halfWidth, -halfHeight + c),
                new Vector2(-halfWidth + c, -halfHeight),
                new Vector2(halfWidth - c, -halfHeight),
            };
        }

        /// <summary>Signed distance to the closest wall; positive inside the arena.</summary>
        public float DistanceInside(Vector2 p)
        {
            var min = float.MaxValue;
            for (var i = 0; i < Vertices.Length; i++)
            {
                var a = Vertices[i];
                var b = Vertices[(i + 1) % Vertices.Length];
                var edge = (b - a).normalized;
                var inward = new Vector2(-edge.y, edge.x); // left normal of a CCW polygon points inside
                var d = Vector2.Dot(p - a, inward);
                if (d < min) min = d;
            }
            return min;
        }

        public bool Contains(Vector2 p, float margin = 0f) => DistanceInside(p) >= margin;

        public Rect Bounds => new Rect(-HalfWidth, -HalfHeight, HalfWidth * 2f, HalfHeight * 2f);
    }

    /// <summary>Circle approximation of an obstacle, used for spawn checks.</summary>
    public struct ObstacleCircle
    {
        public Vector2 Center;
        public float Radius;

        public ObstacleCircle(Vector2 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public static float DistanceToNearest(IReadOnlyList<ObstacleCircle> obstacles, Vector2 p)
        {
            var min = float.MaxValue;
            if (obstacles == null) return min;
            for (var i = 0; i < obstacles.Count; i++)
            {
                var d = Vector2.Distance(p, obstacles[i].Center) - obstacles[i].Radius;
                if (d < min) min = d;
            }
            return min;
        }
    }
}

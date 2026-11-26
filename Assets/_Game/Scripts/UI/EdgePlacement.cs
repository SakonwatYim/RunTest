using UnityEngine;

namespace RunChildRun
{
    /// <summary>
    /// Where an edge arrow goes: the point where a ray from the screen centre leaves the safe
    /// rectangle (screen minus HUD insets). Keeps arrows off the top bar, skill slots, joystick and Dash.
    /// </summary>
    public static class EdgePlacement
    {
        public static Rect AllowedRect(Rect area, float side, float top, float bottom, float arrowHalfSize)
        {
            var xMin = area.xMin + side + arrowHalfSize;
            var xMax = area.xMax - side - arrowHalfSize;
            var yMin = area.yMin + bottom + arrowHalfSize;
            var yMax = area.yMax - top - arrowHalfSize;
            return Rect.MinMaxRect(xMin, yMin, Mathf.Max(xMin, xMax), Mathf.Max(yMin, yMax));
        }

        /// <param name="direction">Direction from the area centre (any length).</param>
        /// <param name="allowed">Rect in the same space as the area, containing the centre.</param>
        public static Vector2 PointOnEdge(Vector2 direction, Rect allowed, Vector2 center)
        {
            if (direction.sqrMagnitude < 1e-6f) direction = Vector2.up;
            var tx = direction.x > 0f ? (allowed.xMax - center.x) / direction.x
                : direction.x < 0f ? (allowed.xMin - center.x) / direction.x : float.MaxValue;
            var ty = direction.y > 0f ? (allowed.yMax - center.y) / direction.y
                : direction.y < 0f ? (allowed.yMin - center.y) / direction.y : float.MaxValue;
            var t = Mathf.Max(0f, Mathf.Min(tx, ty));
            return center + direction * t;
        }
    }
}

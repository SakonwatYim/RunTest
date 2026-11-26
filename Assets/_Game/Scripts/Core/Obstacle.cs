using UnityEngine;

namespace RunChildRun
{
    /// <summary>
    /// Toy box / block / pillow / ball. The collider blocks movement; its bounds give the circle
    /// used for spawn-distance checks. Size the collider to the art.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Obstacle : MonoBehaviour
    {
        public bool IsRound;

        public float WorldRadius
        {
            get
            {
                var c = GetComponent<Collider2D>();
                if (c == null) return 0f;
                var e = c.bounds.extents;
                return IsRound ? Mathf.Max(e.x, e.y) : new Vector2(e.x, e.y).magnitude;
            }
        }
    }
}

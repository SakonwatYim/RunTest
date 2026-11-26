using UnityEngine;

namespace RunChildRun
{
    /// <summary>Plain sprite holder used for orbit blades, mines and puddles.</summary>
    public sealed class PooledSprite : MonoBehaviour
    {
        [SerializeField] SpriteRenderer sprite;
        [Tooltip("World diameter of the sprite at scale 1.")]
        [SerializeField] float spriteDiameter = 1f;

        public SpriteRenderer Sprite => sprite;

        public void SetDiameter(float worldDiameter)
        {
            var s = worldDiameter / Mathf.Max(0.0001f, spriteDiameter);
            transform.localScale = new Vector3(s, s, 1f);
        }

        public void SetColor(Color c)
        {
            if (sprite != null) sprite.color = c;
        }
    }
}

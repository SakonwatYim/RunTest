using UnityEngine;

namespace RunChildRun
{
    /// <summary>Blinking edge arrows shown 1.5 s before a Horde burst, pointing at where it comes from.</summary>
    public sealed class HordeWarningView : MonoBehaviour
    {
        [SerializeField] RectTransform area;
        [Tooltip("Pre-placed arrows (at least 3).")]
        [SerializeField] OffscreenArrowView[] arrows;
        [SerializeField] float sideInset = 54f;
        [Tooltip("Keeps clear of the top HUD row (timer, EXP bar ends at ~204).")]
        [SerializeField] float topInset = 215f;
        [Tooltip("Keeps clear of the skill slots (from ~921).")]
        [SerializeField] float bottomInset = 175f;
        [SerializeField] float arrowHalfSize = 45f;
        [SerializeField] float showSeconds = 1.8f;
        [SerializeField] Color color = new Color(1f, 0.25f, 0.2f);

        float left;
        int count;

        void Awake()
        {
            if (area == null) area = (RectTransform)transform;
            foreach (var a in arrows) a.gameObject.SetActive(false);
        }

        public void Show(float[] angles, int n)
        {
            count = Mathf.Min(n, arrows.Length);
            left = showSeconds;
            var rect = area.rect;
            var allowed = EdgePlacement.AllowedRect(rect, sideInset, topInset, bottomInset, arrowHalfSize);
            for (var i = 0; i < arrows.Length; i++)
            {
                var on = i < count;
                arrows[i].gameObject.SetActive(on);
                if (!on) continue;
                var dir = new Vector2(Mathf.Cos(angles[i]), Mathf.Sin(angles[i]));
                arrows[i].Rect.anchoredPosition = EdgePlacement.PointOnEdge(dir, allowed, rect.center);
                arrows[i].SetAngle(angles[i] * Mathf.Rad2Deg);
                arrows[i].SetColor(color);
                arrows[i].SetCountdown(-1f);
            }
        }

        void Update()
        {
            if (left <= 0f) return;
            left -= Time.deltaTime;
            var on = left > 0f && Mathf.Repeat(left, 0.3f) > 0.1f;
            for (var i = 0; i < count; i++) arrows[i].gameObject.SetActive(on);
        }
    }
}

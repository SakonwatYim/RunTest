using System.Collections.Generic;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>
    /// Arrows at the screen edge for every off-screen item (GDD 5). Keeps clear of the HUD:
    /// ~54 units from the sides, below the top HUD row and above the skill slots (Figma layout).
    /// </summary>
    public sealed class ItemIndicatorLayer : MonoBehaviour
    {
        [SerializeField] RectTransform area;
        [SerializeField] OffscreenArrowView arrowPrefab;
        [SerializeField] int prewarm = 6;
        [SerializeField] float sideInset = 54f;
        [Tooltip("Keeps clear of the top HUD row (timer, EXP bar ends at ~204).")]
        [SerializeField] float topInset = 215f;
        [Tooltip("Keeps clear of the skill slots (from ~921).")]
        [SerializeField] float bottomInset = 175f;
        [Tooltip("Extra inset for the arrow's own size.")]
        [SerializeField] float arrowHalfSize = 45f;

        readonly List<OffscreenArrowView> arrows = new List<OffscreenArrowView>(8);
        RunContext ctx;

        public void Bind(RunContext context)
        {
            ctx = context;
            if (area == null) area = (RectTransform)transform;
            for (var i = 0; i < prewarm; i++) arrows.Add(Create());
        }

        OffscreenArrowView Create()
        {
            var a = Instantiate(arrowPrefab, area);
            a.gameObject.SetActive(false);
            return a;
        }

        void LateUpdate()
        {
            if (ctx == null) return;
            var items = ctx.Items.Active;
            var cam = ctx.CameraRig.Camera;
            var rect = area.rect;
            var allowed = EdgePlacement.AllowedRect(rect, sideInset, topInset, bottomInset, arrowHalfSize);
            var used = 0;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var vp = (Vector2)cam.WorldToViewportPoint(item.Position);
                if (vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f) continue;

                if (used >= arrows.Count) arrows.Add(Create());
                var arrow = arrows[used++];
                if (!arrow.gameObject.activeSelf) arrow.gameObject.SetActive(true);

                var dir = new Vector2((vp.x - 0.5f) * rect.width, (vp.y - 0.5f) * rect.height);
                arrow.Rect.anchoredPosition = EdgePlacement.PointOnEdge(dir, allowed, rect.center);
                arrow.SetAngle(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                arrow.SetColor(item.Color);
                arrow.SetCountdown(item.Expires ? item.Remaining : -1f);
            }

            for (var i = used; i < arrows.Count; i++)
            {
                if (arrows[i].gameObject.activeSelf) arrows[i].gameObject.SetActive(false);
            }
        }
    }
}

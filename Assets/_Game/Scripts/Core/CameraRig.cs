using UnityEngine;

namespace RunChildRun
{
    /// <summary>
    /// Orthographic follow camera: constant visible height (1080 units), width from the device
    /// aspect, stops at the arena edge, with optional shake and a zoom punch.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] Camera cam;
        [SerializeField] Transform target;

        RunContext ctx;
        float baseSize = 5.4f;
        float zoom = 1f;
        float zoomTarget = 1f;
        float zoomTimer;
        float zoomDuration;
        float shakeLeft;
        float shakeAmplitude;
        bool shakeEnabled = true;

        public Camera Camera => cam;

        public void Init(RunContext context)
        {
            ctx = context;
            baseSize = ctx.W(ctx.Config.ViewHeight) * 0.5f;
            cam.orthographic = true;
            cam.orthographicSize = baseSize;
            shakeEnabled = SaveSystem.Data.Settings.ScreenShake;
            if (target == null) target = ctx.Player.transform;
            Snap();
        }

        public void ZoomPunch(float zoomFactor, float duration)
        {
            zoomTarget = zoomFactor;
            zoomDuration = Mathf.Max(0.01f, duration);
            zoomTimer = 0f;
        }

        public void Shake(float amplitude, float duration)
        {
            if (!shakeEnabled) return;
            shakeAmplitude = Mathf.Max(shakeAmplitude, amplitude);
            shakeLeft = Mathf.Max(shakeLeft, duration);
        }

        void Snap()
        {
            var p = Clamp(target.position);
            transform.position = new Vector3(p.x, p.y, -10f);
        }

        void LateUpdate()
        {
            if (ctx == null || target == null) return;
            var dt = Time.unscaledDeltaTime;

            if (zoomTimer < zoomDuration)
            {
                zoomTimer += dt;
                var k = Mathf.Clamp01(zoomTimer / zoomDuration);
                // In fast, out slow.
                zoom = Mathf.Lerp(1f, zoomTarget, k < 0.2f ? k / 0.2f : 1f - (k - 0.2f) / 0.8f);
            }
            else
            {
                zoom = 1f;
            }
            cam.orthographicSize = baseSize * zoom;

            var p = Clamp(target.position);
            if (shakeLeft > 0f)
            {
                shakeLeft -= dt;
                p += Random.insideUnitCircle * shakeAmplitude;
                if (shakeLeft <= 0f) shakeAmplitude = 0f;
            }
            transform.position = new Vector3(p.x, p.y, -10f);
        }

        Vector2 Clamp(Vector2 p)
        {
            var bounds = ctx.Arena.Geometry.Bounds;
            var halfH = cam.orthographicSize;
            var halfW = halfH * cam.aspect;
            p.x = bounds.width <= halfW * 2f ? bounds.center.x : Mathf.Clamp(p.x, bounds.xMin + halfW, bounds.xMax - halfW);
            p.y = bounds.height <= halfH * 2f ? bounds.center.y : Mathf.Clamp(p.y, bounds.yMin + halfH, bounds.yMax - halfH);
            return p;
        }
    }
}

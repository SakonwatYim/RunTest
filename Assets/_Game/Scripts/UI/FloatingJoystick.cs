using UnityEngine;
using UnityEngine.EventSystems;

namespace RunChildRun
{
    /// <summary>
    /// Floating joystick (GDD 3): appears where the thumb touches inside the left-half zone.
    /// Fixed mode keeps the base at its authored position. Analog output with a 10% dead zone.
    /// Put this on the touch-zone Image (raycast target, can be fully transparent).
    /// </summary>
    public sealed class FloatingJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] RectTransform baseRect;
        [SerializeField] RectTransform knob;
        [SerializeField] CanvasGroup visuals;
        [Tooltip("Knob travel in canvas units (half the ~260 diameter).")]
        [SerializeField] float radius = 130f;
        [Range(0f, 0.5f)] [SerializeField] float deadZone = 0.1f;
        [SerializeField] bool floating = true;

        Vector2 restPosition;
        Vector3 baseScale = Vector3.one;
        float opacity = 1f;
        int pointerId = int.MinValue;
        Canvas canvas;

        public Vector2 Value { get; private set; }

        void Awake()
        {
            restPosition = baseRect.anchoredPosition;
            baseScale = baseRect.localScale;
            canvas = GetComponentInParent<Canvas>();
        }

        public void ApplySettings(SettingsData s)
        {
            floating = !s.FixedJoystick;
            baseRect.localScale = baseScale * Mathf.Clamp(s.JoystickScale, 0.6f, 1.5f);
            opacity = Mathf.Clamp(s.JoystickOpacity, 0.2f, 1f);
            ResetStick();
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (pointerId != int.MinValue) return;
            pointerId = e.pointerId;
            if (floating && RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)baseRect.parent,
                    e.position, Cam, out var local))
            {
                baseRect.anchoredPosition = local;
            }
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != pointerId) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, e.position, Cam, out var local)) return;
            var offset = Vector2.ClampMagnitude(local, radius);
            knob.anchoredPosition = offset;
            var v = offset / radius;
            var m = v.magnitude;
            Value = m < deadZone ? Vector2.zero : v / m * ((m - deadZone) / (1f - deadZone));
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != pointerId) return;
            ResetStick();
        }

        void OnDisable() => ResetStick();

        void ResetStick()
        {
            pointerId = int.MinValue;
            Value = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            if (baseRect != null) baseRect.anchoredPosition = restPosition;
            if (visuals != null) visuals.alpha = opacity;
        }

        Camera Cam => canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
    }
}

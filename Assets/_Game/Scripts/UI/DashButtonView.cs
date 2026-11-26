using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RunChildRun
{
    /// <summary>Dash button (bottom right) with a radial cooldown overlay.</summary>
    public sealed class DashButtonView : MonoBehaviour, IPointerDownHandler
    {
        [Tooltip("Radial-filled image showing remaining cooldown.")]
        [SerializeField] Image cooldownFill;
        [SerializeField] Image icon;
        [SerializeField] float readyPulseScale = 1.12f;

        RunContext ctx;
        float pulse;
        bool wasReady = true;

        public void Bind(RunContext context) => ctx = context;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (ctx != null) ctx.Input.QueueDash();
        }

        void Update()
        {
            if (ctx == null) return;
            var p = ctx.Player;
            var k = p.DashCooldownTotal > 0f ? p.DashCooldownLeft / p.DashCooldownTotal : 0f;
            if (cooldownFill != null) cooldownFill.fillAmount = k;
            var ready = k <= 0f;
            if (icon != null)
            {
                var c = icon.color;
                c.a = ready ? 1f : 0.45f;
                icon.color = c;
            }
            if (ready && !wasReady) pulse = 1f;
            wasReady = ready;
            if (pulse > 0f)
            {
                pulse -= Time.unscaledDeltaTime * 4f;
                transform.localScale = Vector3.one * Mathf.Lerp(1f, readyPulseScale, Mathf.Sin(Mathf.Clamp01(pulse) * Mathf.PI));
            }
        }
    }
}

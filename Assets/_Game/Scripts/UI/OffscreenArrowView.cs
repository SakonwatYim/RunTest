using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RunChildRun
{
    /// <summary>Edge-of-screen arrow: rotating pointer + coloured badge with a countdown (or "!").</summary>
    public sealed class OffscreenArrowView : MonoBehaviour
    {
        [SerializeField] RectTransform pointer;
        [SerializeField] Image pointerImage;
        [SerializeField] Image badge;
        [SerializeField] TMP_Text label;

        int shownNumber = int.MinValue;

        public RectTransform Rect => (RectTransform)transform;

        public void SetColor(Color c)
        {
            if (pointerImage != null) pointerImage.color = c;
            if (badge != null) badge.color = c;
        }

        public void SetAngle(float degrees)
        {
            if (pointer != null) pointer.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }

        /// <summary>Seconds left, or a negative value to show "!".</summary>
        public void SetCountdown(float seconds)
        {
            var n = seconds < 0f ? -1 : Mathf.CeilToInt(seconds);
            if (n == shownNumber || label == null) return;
            shownNumber = n;
            label.text = n < 0 ? "!" : n.ToString();
        }
    }
}

using TMPro;
using UnityEngine;

namespace RunChildRun
{
    /// <summary>Short notice under the EXP bar ("Gift box appeared!", "Lv 2 → 3").</summary>
    public sealed class ToastView : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text label;
        [SerializeField] float holdSeconds = 1.8f;
        [SerializeField] float fadeSeconds = 0.4f;

        float left;

        void Awake()
        {
            if (group != null) group.alpha = 0f;
        }

        public void Show(string message, Color color)
        {
            label.text = message;
            label.color = color;
            left = holdSeconds + fadeSeconds;
            group.alpha = 1f;
        }

        void Update()
        {
            if (left <= 0f) return;
            left -= Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(left / fadeSeconds);
        }
    }
}

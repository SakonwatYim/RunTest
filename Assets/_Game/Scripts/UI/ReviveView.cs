using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RunChildRun
{
    /// <summary>Figma "Revive": 10 s countdown, watch an ad to revive (once per run), X to give up.</summary>
    public sealed class ReviveView : MonoBehaviour
    {
        [SerializeField] TMP_Text countdownText;
        [SerializeField] Button watchAdButton;
        [SerializeField] Button closeButton;

        RunManager run;
        float left;
        bool busy;
        int shown = -1;

        public void Bind(RunManager manager)
        {
            run = manager;
            watchAdButton.onClick.AddListener(run.WatchReviveAd);
            closeButton.onClick.AddListener(run.DeclineRevive);
        }

        public void Show(float seconds)
        {
            gameObject.SetActive(true);
            left = seconds;
            shown = -1;
            SetBusy(false);
        }

        public void Hide() => gameObject.SetActive(false);

        public void SetBusy(bool value)
        {
            busy = value;
            watchAdButton.interactable = !value;
            closeButton.interactable = !value;
        }

        void Update()
        {
            if (busy) return;
            left -= Time.unscaledDeltaTime;
            var n = Mathf.Max(0, Mathf.CeilToInt(left));
            if (n != shown)
            {
                shown = n;
                countdownText.text = n.ToString();
            }
            if (left <= 0f) run.DeclineRevive();
        }
    }
}

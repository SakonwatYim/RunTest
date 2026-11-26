using UnityEngine;
using UnityEngine.UI;

namespace RunChildRun
{
    public sealed class PauseView : MonoBehaviour
    {
        [SerializeField] Button resumeButton;
        [SerializeField] Button retryButton;
        [SerializeField] Button mainMenuButton;

        public void Bind(RunManager run)
        {
            resumeButton.onClick.AddListener(run.Resume);
            if (retryButton != null) retryButton.onClick.AddListener(run.Retry);
            mainMenuButton.onClick.AddListener(run.GoToHub);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}

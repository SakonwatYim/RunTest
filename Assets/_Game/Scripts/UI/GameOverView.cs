using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RunChildRun
{
    /// <summary>Figma "Game Over": survival time, high score, coins, kills, Watch AD x2 Coin, Play Again, Main Menu.</summary>
    public sealed class GameOverView : MonoBehaviour
    {
        [SerializeField] TMP_Text timeText;
        [SerializeField] TMP_Text highScoreText;
        [SerializeField] TMP_Text coinsText;
        [SerializeField] TMP_Text killsText;
        [Tooltip("Optional: shown when this run beat the high score.")]
        [SerializeField] GameObject newBestBadge;
        [SerializeField] Button doubleCoinsButton;
        [SerializeField] Button playAgainButton;
        [SerializeField] Button mainMenuButton;

        public void Bind(RunManager run)
        {
            doubleCoinsButton.onClick.AddListener(run.WatchDoubleCoinsAd);
            playAgainButton.onClick.AddListener(run.Retry);
            mainMenuButton.onClick.AddListener(run.GoToHub);
        }

        public void Show(float seconds, float previousBest, bool newBest, int coins, int kills, int level)
        {
            gameObject.SetActive(true);
            timeText.text = RunState.FormatTime(seconds);
            highScoreText.text = RunState.FormatTime(Mathf.Max(seconds, previousBest));
            killsText.text = kills.ToString();
            if (newBestBadge != null) newBestBadge.SetActive(newBest);
            SetCoins(coins, false);
            doubleCoinsButton.interactable = coins > 0;
        }

        public void SetDoubleCoinsBusy() => doubleCoinsButton.interactable = false;

        public void SetCoins(int coins, bool doubled)
        {
            coinsText.text = "+ " + coins.ToString("N0");
            if (doubled) doubleCoinsButton.gameObject.SetActive(false);
        }
    }
}

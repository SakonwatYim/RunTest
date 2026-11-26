using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RunChildRun
{
    /// <summary>Figma "Main Menu": coin pill, character, Play / Setting / Upgrade.</summary>
    public sealed class HubView : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] MetaUpgradeDef[] upgrades;
        [SerializeField] SkillDef[] skills;

        [Header("UI")]
        [SerializeField] TMP_Text coinText;
        [SerializeField] Button playButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button upgradeButton;
        [SerializeField] UpgradePanelView upgradePanel;
        [SerializeField] SettingsView settingsPanel;

        void Start()
        {
            Time.timeScale = 1f;
            upgradePanel.Bind(upgrades, skills);
            settingsPanel.Bind();
            upgradePanel.Closed += RefreshCoins;
            playButton.onClick.AddListener(() => SceneManager.LoadScene(RunManager.RunSceneName));
            settingsButton.onClick.AddListener(settingsPanel.Show);
            upgradeButton.onClick.AddListener(upgradePanel.Show);
            RefreshCoins();
        }

        void RefreshCoins() => coinText.text = SaveSystem.Data.Coins.ToString("N0");
    }
}

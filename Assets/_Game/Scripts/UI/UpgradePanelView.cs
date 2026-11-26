using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RunChildRun
{
    /// <summary>Figma "Upgrade": 4 permanent upgrades + 2 skill unlocks (GDD 8 MVP).</summary>
    public sealed class UpgradePanelView : MonoBehaviour
    {
        [SerializeField] Button backButton;
        [SerializeField] TMP_Text coinText;
        [SerializeField] UpgradeEntryView[] entries;

        MetaUpgradeDef[] upgrades;
        SkillDef[] unlockables;

        public event Action Closed;

        public void Bind(MetaUpgradeDef[] upgradeDefs, SkillDef[] skills)
        {
            upgrades = upgradeDefs;
            unlockables = Array.FindAll(skills, s => s != null && s.RequiresUnlock);
            backButton.onClick.AddListener(Hide);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        void Refresh()
        {
            var save = SaveSystem.Data;
            coinText.text = save.Coins.ToString("N0");
            var i = 0;
            foreach (var u in upgrades)
            {
                if (i >= entries.Length) break;
                var def = u;
                entries[i++].ShowUpgrade(def, save.GetUpgradeLevel(def.Id), save.Coins, () =>
                {
                    if (MetaProgression.TryBuyUpgrade(SaveSystem.Data, def)) SaveSystem.Save();
                    Refresh();
                });
            }
            foreach (var s in unlockables)
            {
                if (i >= entries.Length) break;
                var def = s;
                entries[i++].ShowSkillUnlock(def, MetaProgression.IsSkillAvailable(save, def), save.Coins, () =>
                {
                    if (MetaProgression.TryUnlockSkill(SaveSystem.Data, def)) SaveSystem.Save();
                    Refresh();
                });
            }
            for (; i < entries.Length; i++) entries[i].gameObject.SetActive(false);
        }
    }
}

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RunChildRun
{
    /// <summary>One row in the Upgrade panel: a permanent upgrade or a skill unlock.</summary>
    public sealed class UpgradeEntryView : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text detail;
        [SerializeField] TMP_Text cost;

        Action onBuy;

        void Awake() => button.onClick.AddListener(() => onBuy?.Invoke());

        public void ShowUpgrade(MetaUpgradeDef def, int level, int coins, Action buy)
        {
            onBuy = buy;
            SetIcon(def.Icon);
            title.text = def.DisplayName;
            var sign = def.Kind == MetaUpgradeKind.DashCooldown ? "-" : "+";
            detail.text = $"Lv {level}/{def.MaxLevel}  ({sign}{def.BonusPerLevel * level * 100f:0}%)";
            var price = def.CostForNext(level);
            cost.text = price < 0 ? "MAX" : price.ToString("N0");
            button.interactable = price >= 0 && coins >= price;
        }

        public void ShowSkillUnlock(SkillDef def, bool unlocked, int coins, Action buy)
        {
            onBuy = buy;
            SetIcon(def.Icon);
            title.text = def.DisplayName;
            detail.text = unlocked ? "Unlocked" : "New skill";
            cost.text = unlocked ? "OWNED" : def.UnlockCost.ToString("N0");
            button.interactable = !unlocked && coins >= def.UnlockCost;
        }

        void SetIcon(Sprite s)
        {
            if (icon == null) return;
            icon.sprite = s;
            icon.enabled = s != null;
        }
    }
}
